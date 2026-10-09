@echo off
setlocal enabledelayedexpansion
title Build Installer Desktop Linux 32-bit
cls
echo ===================================================================
echo   BUILD INSTALLER - LAN CHAT (LINUX 32-BIT / x86)
echo ===================================================================
echo.

:: 1. Baca versi dari version.conf
set "APP_VERSION=0.3.6"
if exist "%~dp0version.conf" (
    for /f "usebackq tokens=1,* delims==" %%A in ("%~dp0version.conf") do (
        if /i "%%A"=="VERSION" set "APP_VERSION=%%B"
    )
) else if exist "%~dp0..\version.conf" (
    for /f "usebackq tokens=1,* delims==" %%A in ("%~dp0..\version.conf") do (
        if /i "%%A"=="VERSION" set "APP_VERSION=%%B"
    )
)
echo Versi Aplikasi: v!APP_VERSION!
echo.

set "PROJ_PATH=%~dp0src\LanChat.Desktop\LanChat.Desktop.csproj"
set "OUT_DIR=%~dp0publish\desktop_linux32"
set "TMPL_DIR=%~dp0src\templates"
set "APP_DIR=!OUT_DIR!\app"

echo [1/4] Menyiapkan direktori output...
if not exist "!OUT_DIR!" mkdir "!OUT_DIR!"
if not exist "!APP_DIR!" mkdir "!APP_DIR!"

echo.
echo [2/4] Mengompilasi aplikasi Linux 32-bit (Release linux-x86)...
dotnet publish "!PROJ_PATH!" -c Release -r linux-x86 --self-contained false -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false -o "!APP_DIR!"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===================================================================
    echo   [ERROR] Kompilasi Linux 32-bit Gagal!
    echo ===================================================================
    pause
    exit /b %ERRORLEVEL%
)

:: Hapus semua file .pdb (debug symbols)
echo Membersihkan file debug symbols (*.pdb)...
del /q /f "!APP_DIR!\*.pdb" 2>nul

echo.
echo [3/4] Menyiapkan script installer Linux (install.sh dan run.sh)...
copy /y "!TMPL_DIR!\run.sh" "!APP_DIR!\run.sh" >nul
copy /y "!TMPL_DIR!\install.sh" "!APP_DIR!\install.sh" >nul

echo.
echo [4/4] Membuat arsip paket installer Linux32 (.tar.gz)...
tar -czf "!OUT_DIR!\LAN_Chat_Linux32_Setup_v!APP_VERSION!.tar.gz" -C "!APP_DIR!" . 2>nul
tar -czf "!OUT_DIR!\LAN_Chat_Linux32_Setup.tar.gz" -C "!APP_DIR!" . 2>nul
if not exist "!OUT_DIR!\LAN_Chat_Linux32_Setup_v!APP_VERSION!.tar.gz" (
    powershell -Command "Compress-Archive -Path '!APP_DIR!\*' -DestinationPath '!OUT_DIR!\LAN_Chat_Linux32_Setup_v!APP_VERSION!.zip' -Force"
)

echo.
echo ===================================================================
echo   [SUKSES] Paket Installer Linux 32-bit Berhasil Dibuat!
echo   Lokasi folder publish:
echo   !OUT_DIR!\
echo   File paket:
echo   !OUT_DIR!\LAN_Chat_Linux32_Setup_v!APP_VERSION!.tar.gz
echo ===================================================================
echo.
pause

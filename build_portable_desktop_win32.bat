@echo off
setlocal enabledelayedexpansion
title Build Portable Desktop Win32
cls
echo ===================================================================
echo   BUILD PORTABLE DESKTOP - LAN CHAT (WIN32 / x86)
echo ===================================================================
echo.

:: 1. Baca versi dari version.conf
set "APP_VERSION=0.3.6"
if exist "%~dp0version.conf" (
    for /f "usebackq tokens=1,* delims==" %%A in ("%~dp0version.conf") do (
        if /i "%%A"=="VERSION" set "APP_VERSION=%%B"
    )
)
echo Versi Aplikasi: v!APP_VERSION!
echo.

set "OUT_DIR=%~dp0publish\desktop_portable_win32"
set "PROJ_PATH=%~dp0src\LanChat.Desktop\LanChat.Desktop.csproj"

echo [1/3] Menyiapkan direktori publish...
if not exist "!OUT_DIR!" mkdir "!OUT_DIR!"

echo.
echo [2/3] Mengompilasi aplikasi Portable Win32 (Release x86)...
dotnet publish "!PROJ_PATH!" -c Release -r win-x86 --self-contained false -p:DebugType=None -p:DebugSymbols=false -p:Version=!APP_VERSION! -o "!OUT_DIR!"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===================================================================
    echo   [ERROR] Kompilasi Portable Win32 Gagal!
    echo ===================================================================
    pause
    exit /b %ERRORLEVEL%
)

:: Hapus semua file .pdb (debug symbols) agar portable bersih dan ringan
echo Membersihkan file debug symbols (*.pdb)...
del /q /f "!OUT_DIR!\*.pdb" 2>nul

:: Sertakan script buka firewall ke dalam folder portable
if exist "%~dp0buka_firewall_lan_chat.bat" (
    copy /y "%~dp0buka_firewall_lan_chat.bat" "!OUT_DIR!\allow_firewall.bat" >nul
    copy /y "%~dp0buka_firewall_lan_chat.bat" "!OUT_DIR!\buka_firewall_admin.bat" >nul
)

echo.
echo [3/3] Mengemas arsip ZIP Portable (LAN_Chat_Portable_v!APP_VERSION!_win32.zip)...
set "ZIP_FILE=!OUT_DIR!\LAN_Chat_Portable_v!APP_VERSION!_win32.zip"
if exist "!ZIP_FILE!" del /f /q "!ZIP_FILE!"
tar -a -cf "!ZIP_FILE!" -C "!OUT_DIR!" LanChat.Desktop.exe *.dll *.json Assets *.bat 2>nul

echo.
echo ===================================================================
echo   [SUKSES] Portable Win32 Berhasil Dibuat!
echo   Lokasi folder:
echo   publish\desktop_portable_win32\
echo   File aplikasi langsung:
echo   publish\desktop_portable_win32\LanChat.Desktop.exe
echo   File arsip portable (bebas .pdb):
echo   !ZIP_FILE!
echo ===================================================================
echo.
pause

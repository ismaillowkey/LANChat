@echo off
setlocal enabledelayedexpansion
title Build NSIS Installer Desktop Win32
cls
echo ===================================================================
echo   BUILD NSIS INSTALLER - LAN CHAT (WIN32 / x86)
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

:: 2. Cari path makensis.exe
set "NSIS_EXE="
where makensis >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    set "NSIS_EXE=makensis"
) else if exist "%ProgramFiles(x86)%\NSIS\makensis.exe" (
    set "NSIS_EXE=%ProgramFiles(x86)%\NSIS\makensis.exe"
) else if exist "%ProgramFiles%\NSIS\makensis.exe" (
    set "NSIS_EXE=%ProgramFiles%\NSIS\makensis.exe"
) else if exist "C:\Program Files (x86)\NSIS\makensis.exe" (
    set "NSIS_EXE=C:\Program Files (x86)\NSIS\makensis.exe"
) else if exist "C:\Program Files\NSIS\makensis.exe" (
    set "NSIS_EXE=C:\Program Files\NSIS\makensis.exe"
)

if "!NSIS_EXE!"=="" (
    echo [ERROR] NSIS makensis.exe tidak ditemukan!
    echo Silakan install NSIS dari: https://nsis.sourceforge.io/Download
    echo Atau pastikan NSIS terinstall di C:\Program Files ^(x86^)\NSIS\
    echo.
    pause
    exit /b 1
)

set "ROOT_DIR=%~dp0"
set "PROJ_PATH=%~dp0src\LanChat.Desktop\LanChat.Desktop.csproj"
set "NSI_PATH=%~dp0installer.nsi"
set "PUB_DIR=%~dp0publish\desktop_win32"
set "BUILD_DIR=%~dp0publish\desktop_win32_build"

echo [1/3] Menyiapkan direktori publish...
if not exist "!PUB_DIR!" mkdir "!PUB_DIR!"
if not exist "!BUILD_DIR!" mkdir "!BUILD_DIR!"

echo.
echo [2/3] Mengompilasi aplikasi Win32 (Release x86)...
dotnet publish "!PROJ_PATH!" -c Release -r win-x86 --self-contained false -p:DebugType=None -p:DebugSymbols=false -p:Version=!APP_VERSION! -o "!BUILD_DIR!"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===================================================================
    echo   [ERROR] Kompilasi Win32 Gagal! Pembuatan installer dibatalkan.
    echo ===================================================================
    pause
    exit /b %ERRORLEVEL%
)

:: Hapus semua file .pdb (debug symbols) agar installer bersih dan ringan
echo Membersihkan file debug symbols (*.pdb)...
del /q /f "!BUILD_DIR!\*.pdb" 2>nul

echo.
echo [3/3] Menjalankan NSIS Compiler...
echo Menggunakan: "!NSIS_EXE!"
"!NSIS_EXE!" /DAPP_VERSION="!APP_VERSION!" "!NSI_PATH!"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===================================================================
    echo   [ERROR] Pembuatan Installer NSIS Gagal!
    echo ===================================================================
    pause
    exit /b %ERRORLEVEL%
)

:: Bersihkan folder build temporary
if exist "!BUILD_DIR!" rmdir /s /q "!BUILD_DIR!"

echo.
echo ===================================================================
echo   [SUKSES] Installer Win32 Berhasil Dibuat!
echo   Lokasi file setup installer:
echo   !PUB_DIR!\LAN_Chat_Setup_v!APP_VERSION!_win32.exe
echo   Shortcut Start Menu: LAN Chat
echo ===================================================================
echo.
pause

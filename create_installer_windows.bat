@echo off
setlocal enabledelayedexpansion
title Create Installer LAN Chat Windows (NSIS)
cls
echo ===================================================================
echo   CREATE NSIS INSTALLER WINDOWS - LAN CHAT P2P
echo ===================================================================
echo.

:: 1. Cari path makensis.exe
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

echo [1/3] Menyiapkan direktori output...
if not exist "publish\Desktop" mkdir "publish\Desktop"
if not exist "publish\Installer" mkdir "publish\Installer"

echo [2/3] Mengompilasi aplikasi Desktop (Release)...
dotnet publish src\LanChat.Wpf\LanChat.Wpf.csproj -c Release -o publish\Desktop
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===================================================================
    echo   [ERROR] Kompilasi Desktop Gagal! Pembuatan installer dibatalkan.
    echo ===================================================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Menjalankan NSIS Compiler...
echo Menggunakan: "!NSIS_EXE!"
"!NSIS_EXE!" installer.nsi

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===================================================================
    echo   [ERROR] Pembuatan Installer NSIS Gagal!
    echo ===================================================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================================================
echo   [SUKSES] Installer Berhasil Dibuat!
echo   Lokasi file setup installer:
echo   publish\Installer\LAN_Chat_Setup_v0.2.3.exe
echo ===================================================================
echo.
pause

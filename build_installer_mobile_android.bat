@echo off
setlocal enabledelayedexpansion
title Build Installer Mobile Android APK
cls
echo ===================================================================
echo   BUILD INSTALLER - LAN CHAT (MOBILE ANDROID APK)
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

set "PROJ_PATH=%~dp0src\LanChat.Android\LanChat.Android.csproj"
set "OUT_DIR=%~dp0publish\mobile_android"

echo [1/3] Menyiapkan direktori output...
if not exist "!OUT_DIR!" mkdir "!OUT_DIR!"

echo.
echo [2/3] Mengompilasi paket APK Android (Release)...
echo (Proses ini membutuhkan waktu kompilasi 1-2 menit)
dotnet publish "!PROJ_PATH!" -f net10.0-android -c Release -p:AndroidPackageFormat=apk -p:ApplicationDisplayVersion=!APP_VERSION! -p:Version=!APP_VERSION! -o "!OUT_DIR!"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===================================================================
    echo   [ERROR] Pembuatan APK Android Gagal!
    echo ===================================================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Merapikan file installer APK...
if exist "!OUT_DIR!\*Signed.apk" (
    move /y "!OUT_DIR!\*Signed.apk" "!OUT_DIR!\LAN_Chat_v!APP_VERSION!.apk" >nul 2>nul
) else if exist "!OUT_DIR!\com.CompanyName.LanChat.apk" (
    move /y "!OUT_DIR!\com.CompanyName.LanChat.apk" "!OUT_DIR!\LAN_Chat_v!APP_VERSION!.apk" >nul 2>nul
)

:: Hapus file sampah kompilasi dan file mentah APK
del /q "!OUT_DIR!\com.*.apk" 2>nul
del /q "!OUT_DIR!\*.dll" 2>nul
del /q "!OUT_DIR!\*.pdb" 2>nul
del /q "!OUT_DIR!\*.json" 2>nul

echo.
echo ===================================================================
echo   [SUKSES] Paket Installer APK Android Berhasil Dibuat!
echo   Lokasi folder publish:
echo   !OUT_DIR!\
echo   File installer APK:
echo   !OUT_DIR!\LAN_Chat_v!APP_VERSION!.apk
echo ===================================================================
echo.
pause

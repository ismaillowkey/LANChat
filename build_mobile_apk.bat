@echo off
title Build LAN Chat Mobile (MAUI Android APK)
cls
echo ===================================================================
echo   BUILD LAN CHAT MOBILE APP (MAUI ANDROID APK)
echo ===================================================================
echo.
echo Sedang melakukan compile & package APK...
echo (Proses ini mungkin membutuhkan waktu 1-2 menit pertama kali)
echo.

if not exist publish\Mobile mkdir publish\Mobile

dotnet publish src\LanChat.Maui\LanChat.Maui.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=apk -o publish\Mobile

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===================================================================
    echo   [ERROR] Pembuatan APK Gagal!
    echo ===================================================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================================================
echo   [SUKSES] Build APK Berhasil!
echo   File APK tersimpan di folder: publish\Mobile\
echo ===================================================================
echo.
pause

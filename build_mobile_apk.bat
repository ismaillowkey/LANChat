@echo off
setlocal enabledelayedexpansion
title Build LAN Chat Mobile (MAUI Android APK)
cls
echo ===================================================================
echo   BUILD LAN CHAT MOBILE APP (MAUI ANDROID APK)
echo ===================================================================
echo.
echo Sedang melakukan kompilasi dan pembuatan paket APK...
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

:: Ambil file APK yang sudah Signed, ganti nama menjadi LAN_Chat_v0.2.3.apk
if exist "publish\Mobile\*Signed.apk" (
    move /y "publish\Mobile\*Signed.apk" "publish\Mobile\LAN_Chat_v0.2.3.apk" >nul 2>nul
) else if exist "publish\Mobile\com.companyname.lanchat.maui.apk" (
    move /y "publish\Mobile\com.companyname.lanchat.maui.apk" "publish\Mobile\LAN_Chat_v0.2.3.apk" >nul 2>nul
)

:: Hapus APK mentah (unsigned) dan file sampah kompilasi lainnya
del /q "publish\Mobile\com.*.apk" 2>nul
del /q "publish\Mobile\*.dll" 2>nul
del /q "publish\Mobile\*.pdb" 2>nul
del /q "publish\Mobile\*.json" 2>nul

echo.
echo ===================================================================
echo   [SUKSES] Build APK Berhasil!
echo   File APK siap install tersimpan di:
echo   publish\Mobile\LAN_Chat_v0.2.3.apk
echo ===================================================================
echo.
pause

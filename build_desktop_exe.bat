@echo off
title Build LAN Chat Desktop (WPF .NET Framework 4.7.2)
cls
echo ===================================================================
echo   BUILD LAN CHAT DESKTOP APP (WPF .NET FRAMEWORK 4.7.2)
echo ===================================================================
echo.
echo Sedang melakukan compile & publish Release...
echo.

if not exist publish\Desktop mkdir publish\Desktop

dotnet publish src\LanChat.Wpf\LanChat.Wpf.csproj -c Release -o publish\Desktop

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===================================================================
    echo   [ERROR] Kompilasi Desktop Gagal!
    echo ===================================================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================================================
echo   [SUKSES] Build Desktop Berhasil!
echo   File EXE siap dijalankan di folder: publish\Desktop\LanChat.Wpf.exe
echo ===================================================================
echo.
pause

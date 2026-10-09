@echo off
setlocal
:: Request Administrator elevation if not running as admin
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -Command "Start-Process cmd -ArgumentList '/c """%~f0"""' -Verb RunAs"
    exit /b
)

title Membuka Akses Windows Firewall - LAN Chat
cls
echo ===================================================================
echo   MEMBUKA AKSES WINDOWS FIREWALL UNTUK LAN CHAT
echo ===================================================================
echo.
echo Menambahkan aturan Firewall untuk LAN Chat...

:: 1. Aturan program LAN Chat
netsh advfirewall firewall add rule name="LAN Chat" dir=in action=allow program="%~dp0publish\desktop_portable_win32\LanChat.Desktop.exe" enable=yes profile=any >nul 2>&1
netsh advfirewall firewall add rule name="LAN Chat" dir=out action=allow program="%~dp0publish\desktop_portable_win32\LanChat.Desktop.exe" enable=yes profile=any >nul 2>&1
netsh advfirewall firewall add rule name="LAN Chat" dir=in action=allow program="%~dp0LanChat.Desktop.exe" enable=yes profile=any >nul 2>&1
netsh advfirewall firewall add rule name="LAN Chat" dir=out action=allow program="%~dp0LanChat.Desktop.exe" enable=yes profile=any >nul 2>&1

:: 2. Aturan port UDP 45450 (Discovery)
netsh advfirewall firewall add rule name="LAN Chat (UDP Discovery)" dir=in action=allow protocol=UDP localport=45450 enable=yes profile=any >nul 2>&1

:: 3. Aturan port TCP 45451-45500 (Transport Chat, Gambar & Video)
netsh advfirewall firewall add rule name="LAN Chat (TCP Transport)" dir=in action=allow protocol=TCP localport=45451-45500 enable=yes profile=any >nul 2>&1

echo.
echo ===================================================================
echo   [SUKSES] Windows Firewall Berhasil Dibuka!
echo   - UDP Port 45450 (Peer Discovery) : ALLOW
echo   - TCP Port 45451-45500 (Chat Data) : ALLOW
echo ===================================================================
echo.
pause

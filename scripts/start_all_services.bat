@echo off
TITLE AetherDesk Master System Launcher 2026
COLOR 0A
cls

echo =======================================================================
echo          AetherDesk MASTER SYSTEM LAUNCHER (ALL SERVICES)
echo =======================================================================
echo.

:: -----------------------------------------------------------------------
:: ADIM 0: Onceki surecleri temizle (port cakismasini onlemek icin)
:: -----------------------------------------------------------------------
echo [0] Onceki AetherDesk surecleri kapatiliyor...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":8080 :5000 :9000 :9090" 2^>nul') do (
    taskkill /F /PID %%a >nul 2>&1
)
timeout /t 2 /nobreak >nul
echo     Temizlendi.
echo.

:: -----------------------------------------------------------------------
:: ADIM 1: Tum servisleri arka planda baslat
:: -----------------------------------------------------------------------
echo Launching all AetherDesk ecosystem services in separate process windows...
echo.

echo [*] 1. Starting WebSocket Signaling Server (Port 8080)...
start "AetherDesk Signaling Server (Port 8080)" cmd /k "cd /d %~dp0..\signaling-server && npm run dev"

echo [*] 2. Starting SaaS Backend REST API (Port 5000)...
start "AetherDesk SaaS Backend API (Port 5000)" cmd /k "cd /d %~dp0..\saas-portal\backend && npm run dev"

echo [*] 3. Starting Web Viewer Control UI (Port 9000)...
start "AetherDesk Web Viewer (Port 9000)" cmd /k "cd /d %~dp0..\web-viewer && npm run dev"

echo [*] 4. Starting SaaS Management Console (Port 9090)...
start "AetherDesk SaaS Console (Port 9090)" cmd /k "cd /d %~dp0..\saas-portal\frontend && npm run dev"

echo [*] 5. Starting Global Internet Tunnel (Cloudflare Tunnel)...
start "AetherDesk Global Internet Access (Cloudflare Tunnel)" cmd /k "%~dp0start_global_tunnel.bat"

:: -----------------------------------------------------------------------
:: ADIM 2: Servislerin hazir olmasini bekle (8 saniye)
:: -----------------------------------------------------------------------
echo.
echo [...] Servisler baslatiliyor, lutfen bekleyiniz...
timeout /t 8 /nobreak >nul

:: -----------------------------------------------------------------------
:: ADIM 3: Tarayici sekmelerini otomatik ac
:: -----------------------------------------------------------------------
echo.
echo [*] Tarayici sekmeleri aciliyor...
start "" "http://localhost:9090"
timeout /t 1 /nobreak >nul
start "" "http://localhost:9000"

:: -----------------------------------------------------------------------
:: Ozet
:: -----------------------------------------------------------------------
echo.
echo =======================================================================
echo   TUMU BASLATILDI - Tarayici otomatik acildi!
echo =======================================================================
echo.
echo   Web Viewer UI       : http://localhost:9000
echo   SaaS Console        : http://localhost:9090
echo   WebSocket Signaling : ws://localhost:8080
echo   SaaS Backend API    : http://localhost:5000/api
echo.
echo   Giris Bilgileri:
echo   - Email   : admin@aetherdesk.com
echo   - Password: admin2026
echo.
pause

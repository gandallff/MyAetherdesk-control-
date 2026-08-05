@echo off
TITLE AetherDesk Master System Launcher 2026
COLOR 0A
cls

echo =======================================================================
echo          ⚡ AETHERDESK MASTER SYSTEM LAUNCHER (ALL SERVICES)
echo =======================================================================
echo.
echo Launching all AetherDesk ecosystem services in separate process windows...
echo.

:: 1. Launch Signaling Server (Port 8080)
echo [*] 1. Starting WebSocket Signaling Server (Port 8080)...
start "AetherDesk Signaling Server (Port 8080)" cmd /k "cd /d %~dp0signaling-server && npm run dev"

:: 2. Launch SaaS Backend REST API (Port 5000)
echo [*] 2. Starting SaaS Backend REST API (Port 5000)...
start "AetherDesk SaaS Backend API (Port 5000)" cmd /k "cd /d %~dp0saas-portal\backend && npm run dev"

:: 3. Launch Web Viewer Remote Control UI (Port 9000)
echo [*] 3. Starting Web Viewer Control UI (Port 9000)...
start "AetherDesk Web Viewer (Port 9000)" cmd /k "cd /d %~dp0web-viewer && npm run dev -- --port 9000"

:: 4. Launch SaaS Management Portal Dashboard (Port 9090)
echo [*] 4. Starting SaaS Management Console (Port 9090)...
start "AetherDesk SaaS Console (Port 9090)" cmd /k "cd /d %~dp0saas-portal\frontend && npm run dev"

:: 5. Launch Global Cloudflare Tunnel for Inter-Network Connect
echo [*] 5. Starting Global Internet Tunnel (Cloudflare Tunnel)...
start "AetherDesk Global Internet Access (Cloudflare Tunnel)" cmd /k "%~dp0start_global_tunnel.bat"

echo.
echo =======================================================================
echo  🎉 ALL 5 AETHERDESK SERVICES LAUNCHED SUCCESSFULLY!
echo =======================================================================
echo.
echo  🌐 Web Viewer Client UI : http://localhost:9000
echo  📊 SaaS Console Dashboard: http://localhost:9090
echo  📡 WebSocket Signaling   : ws://localhost:8080
echo  🔑 SaaS Backend REST API : http://localhost:5000/api
echo  🌍 Global Internet Tunnel: Active (Exposes ws://localhost:8080 to WWW)
echo.
echo  Default Admin Login Credentials:
echo  - Email   : admin@aetherdesk.com
echo  - Password: admin2026
echo.
pause

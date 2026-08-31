@echo off
TITLE AetherDesk Master System Launcher 2026
COLOR 0A
cls

echo =======================================================================
echo        AetherDesk MASTER SYSTEM LAUNCHER
echo =======================================================================
echo.

:: ── ADIM 0: Onceki surecleri temizle (Sadece AetherDesk portlari) ──────────
echo [0] AetherDesk portlari (8080, 5000, 9000, 9090) temizleniyor...
for /f "tokens=5" %%a in ('netstat -aon 2^>nul ^| findstr ":8080 :5000 :9000 :9090"') do (
    taskkill /F /PID %%a >nul 2>&1
)
timeout /t 1 /nobreak >nul
echo     AetherDesk portlari temizlendi.
echo.

:: ── ADIM 1: Servisleri ayri CMD pencerelerinde baslat ───────────────────────
echo [*] 1. Signaling Server baslatiliyor  (Port 8080)...
start "AetherDesk | Signaling  :8080" cmd /k "cd /d %~dp0..\signaling-server && npm run dev"

echo [*] 2. SaaS Backend API baslatiliyor  (Port 5000)...
start "AetherDesk | Backend    :5000" cmd /k "cd /d %~dp0..\saas-portal\backend && npm run dev"

echo [*] 3. Web Viewer UI baslatiliyor     (Port 9000)...
start "AetherDesk | Web-Viewer :9000" cmd /k "cd /d %~dp0..\web-viewer && npm run dev"

echo [*] 4. SaaS Dashboard baslatiliyor   (Port 9090)...
start "AetherDesk | SaaS-UI    :9090" cmd /k "cd /d %~dp0..\saas-portal\frontend && npm run dev"

echo.
echo =======================================================================
echo   4 servis baslatildi! Sayfa yukleniyor, lutfen bekleyiniz...
echo =======================================================================
echo.

:: ── ADIM 2: Servisler ayaga kalksın diye bekle ──────────────────────────────
timeout /t 6 /nobreak >nul

:: ── ADIM 3: SaaS Dashboard'u tarayicida ac ──────────────────────────────
echo [*] Tarayici aciliyor (SaaS Dashboard :9090)...
start "" "http://localhost:9090"

echo.
echo   Dashboard hazir olunca sayfa otomatik acilacak.
echo   Bu pencereyi kapatirsaniz servisler calismaya devam eder.
echo.
pause

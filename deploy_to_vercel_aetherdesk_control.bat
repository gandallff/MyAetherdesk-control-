@echo off
TITLE AetherDesk - Vercel Cloud Deploy (aetherdesk-control)
COLOR 0B
cls

echo =======================================================================
echo     ⚡ AETHERDESK CLOUD DEPLOYMENT - PROJE ADI: aetherdesk-control
echo =======================================================================
echo.
echo [*] Eski Vercel baglantilari temizlendi.
echo [*] Paket adi 'aetherdesk-control' olarak ayarlandi.
echo [*] Yeni Vercel yayini baslatiliyor...
echo.
cd /d "%~dp0saas-portal\frontend"

call npx vercel --prod --yes

echo.
echo =======================================================================
echo  🎉 YAYINLAMA TAMAMLANDI!
echo  🌐 Yeni Canli Adres: https://aetherdesk-control.vercel.app
echo =======================================================================
echo.
pause

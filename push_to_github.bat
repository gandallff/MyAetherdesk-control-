@echo off
TITLE AetherDesk - GitHub Push (gandallff/MyAetherdesk-control-)
COLOR 0B
cls

echo =======================================================================
echo     ⚡ AETHERDESK GITHUB REPO GUNCELLEME (MyAetherdesk-control-)
echo =======================================================================
echo.
echo [*] Hedef Repo: https://github.com/gandallff/MyAetherdesk-control-.git
echo [*] Tum dosyalar ve commitler gonderiliyor...
echo.

cd /d "%~dp0"
git remote set-url origin https://github.com/gandallff/MyAetherdesk-control-.git
git branch -M main
git add .
git commit -m "Auto Release: Full Ecosystem MyAetherdesk-control-" 2>nul
git push -u origin main

echo.
echo =======================================================================
echo  🎉 GITHUB YUKLEMESI TAMAMLANDI!
echo  🌐 Repo Adresi: https://github.com/gandallff/MyAetherdesk-control-
echo =======================================================================
echo.
pause

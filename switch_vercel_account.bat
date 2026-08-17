@echo off
TITLE AetherDesk Vercel Account Switcher (tuncaysazan035@gmail.com)
COLOR 0A
cls

echo =======================================================================
echo     ⚡ AETHERDESK VERCEL CLOUD DEPLOYMENT (tuncaysazan035@gmail.com)
echo =======================================================================
echo.
echo [*] Deploying SaaS Portal Frontend to your Vercel account...
cd /d "%~dp0saas-portal\frontend"

call npx vercel --prod --yes

echo.
echo =======================================================================
echo  🎉 DEPLOYMENT TO tuncaysazan035@gmail.com COMPLETE!
echo =======================================================================
echo.
pause

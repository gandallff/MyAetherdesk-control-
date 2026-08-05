@echo off
TITLE AetherDesk Release Package Builder 2026
COLOR 0A
cls

echo =======================================================================
echo          ⚡ AETHERDESK AUTOMATED RELEASE & DEPLOYMENT BUILDER
echo =======================================================================
echo.

set RELEASE_DIR=%CD%\AetherDesk-Distribution-Package

if exist "%RELEASE_DIR%" (
    echo [*] Cleaning existing release folder...
    rmdir /S /Q "%RELEASE_DIR%"
)

mkdir "%RELEASE_DIR%"
mkdir "%RELEASE_DIR%\SignalingServer"
mkdir "%RELEASE_DIR%\WebViewer"
mkdir "%RELEASE_DIR%\Agent"

echo [*] 1. Building Signaling Server (TypeScript)...
cd signaling-server
call npm run build
xcopy /E /I /Y dist "%RELEASE_DIR%\SignalingServer\dist"
xcopy /Y package.json "%RELEASE_DIR%\SignalingServer\"
xcopy /E /I /Y node_modules "%RELEASE_DIR%\SignalingServer\node_modules"
cd ..

echo [*] 2. Building Web Viewer (React + Tailwind)...
cd web-viewer
call npm run build
xcopy /E /I /Y dist "%RELEASE_DIR%\WebViewer\dist"
cd ..

echo [*] 3. Copying Deployment & Auto-Start Installation Scripts...
copy Install-AetherDesk-Service.bat "%RELEASE_DIR%\"
copy Run-AetherDesk-Portable.bat "%RELEASE_DIR%\"
copy AetherDesk-QuickSupport-Tool.bat "%RELEASE_DIR%\"

echo.
echo =======================================================================
echo  🎉 RELEASE PACKAGE CREATED SUCCESSFULLY!
echo  Folder Path: %RELEASE_DIR%
echo.
echo  Share this folder or ZIP file with target PCs to grant remote access!
echo =======================================================================
pause

@echo off
:: Run as Administrator check
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [!] Requesting Administrator privileges for background service installation...
    powershell -Command "Start-Process '%~0' -Verb RunAs"
    exit /b
)

TITLE AetherDesk Automatic Host Agent Installer
COLOR 0B
cls

echo =======================================================================
echo     ⚡ AETHERDESK AUTOMATIC HOST AGENT & SERVICE INSTALLER
echo =======================================================================
echo.
echo Installing AetherDesk Agent as an Auto-Booting System Service...
echo Target Installation Directory: C:\Program Files\AetherDesk

set INSTALL_DIR=C:\Program Files\AetherDesk
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
)

:: Copy Executables and Assets
echo [*] Copying binary files...
xcopy /E /I /Y "%~dp0\*" "%INSTALL_DIR%\" >nul

:: Register Windows Registry AutoRun (Startup)
echo [*] Registering Windows Auto-Start Registry key...
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "AetherDeskAgent" /t REG_SZ /d "\"%INSTALL_DIR%\Agent\aetherdesk-agent.exe\"" /f >nul

:: Launch Host Agent Service Immediately
echo [*] Launching AetherDesk Host Agent in background...
start "" "%INSTALL_DIR%\Agent\aetherdesk-agent.exe"

echo.
echo =======================================================================
echo  ✅ AETHERDESK INSTALLED SUCCESSFULLY ON THIS PC!
echo.
echo  - Auto-Start Status: ACTIVE (Boots automatically with Windows)
echo  - Default Unattended Password: aether2026
echo  - Direct IP Port: 8443
echo =======================================================================
echo.
pause

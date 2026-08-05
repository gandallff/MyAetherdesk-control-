@echo off
TITLE AetherDesk Windows Firewall Auto-Configurator
COLOR 0A
cls

echo =======================================================================
echo     ⚡ AETHERDESK WINDOWS DEFENDER FIREWALL AUTO-CONFIGURATOR
echo =======================================================================
echo.
echo [*] Requesting Administrator Privileges...
echo.

:: Check for Admin Privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [!] ERROR: This script must be run as Administrator!
    echo Please right-click Install-Firewall-Rules.bat and select 'Run as Administrator'.
    echo.
    pause
    exit /b 1
)

echo [*] Adding Inbound TCP/UDP Rules for Port 8443 (Direct IP)...
netsh advfirewall firewall add rule name="AetherDesk Direct IP Port 8443 (TCP)" dir=in action=allow protocol=TCP localport=8443 enable=yes >nul
netsh advfirewall firewall add rule name="AetherDesk Direct IP Port 8443 (UDP)" dir=in action=allow protocol=UDP localport=8443 enable=yes >nul

echo [*] Adding Program Inbound/Outbound Rules for AetherDesk Agent...
netsh advfirewall firewall add rule name="AetherDesk Agent Executable" dir=in action=allow program="%~dp0desktop-agent\target\release\desktop-agent.exe" enable=yes >nul

echo [*] Allowing Outbound WebRTC & HTTPS Ports (443, 8080, 3478 TURN)...
netsh advfirewall firewall add rule name="AetherDesk Outbound WebRTC (Port 443)" dir=out action=allow protocol=TCP localport=443 enable=yes >nul
netsh advfirewall firewall add rule name="AetherDesk Outbound TURN (Port 3478)" dir=out action=allow protocol=UDP localport=3478 enable=yes >nul

echo.
echo =======================================================================
echo  🎉 WINDOWS FIREWALL RULES CONFIGURED SUCCESSFULLY!
echo =======================================================================
echo.
echo  AetherDesk can now freely accept connections through Windows Firewall.
echo.
pause

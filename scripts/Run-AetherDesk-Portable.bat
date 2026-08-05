@echo off
TITLE AetherDesk Portable Agent
COLOR 0E
cls

echo =======================================================================
echo          ⚡ AETHERDESK PORTABLE (NO-INSTALLATION RUNNER)
echo =======================================================================
echo.
echo Starting AetherDesk Agent in Portable Mode...
echo.

if exist "%~dp0Agent\aetherdesk-agent.exe" (
    start "" "%~dp0Agent\aetherdesk-agent.exe"
) else (
    echo [*] Starting Rust Host Agent...
    cd desktop-agent
    cargo run
)

echo.
echo Host Agent is active in memory. Close window to terminate.
pause

@echo off
:: AetherDesk Silent GUI Installer Launcher
:: Completely hides CMD window and opens the modern GUI Installer Form

wscript.exe "%~dp0AetherDesk-QuickSupport-Tool.vbs" "%~dp0AetherDesk-Installer-Setup.ps1"
exit /b

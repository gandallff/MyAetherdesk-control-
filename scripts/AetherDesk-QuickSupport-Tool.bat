@echo off
:: AetherDesk Silent GUI Launcher
:: Completely hides CMD window and launches PowerShell GUI with 0 console window

wscript.exe "%~dp0AetherDesk-QuickSupport-Tool.vbs" "%~dp0AetherDesk-QuickSupport.ps1"
exit /b

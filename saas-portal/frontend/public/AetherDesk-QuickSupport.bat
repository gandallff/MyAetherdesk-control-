@echo off
TITLE AetherDesk QuickSupport 2026
COLOR 0B
cls

echo =======================================================================
echo     ⚡ AETHERDESK MUSTERI DESTEK VE BAGLANTI ARACI
echo =======================================================================
echo.
echo [*] Uzaktan Baglanti Modulu Hazirlaniyor...
echo.

powershell -ExecutionPolicy Bypass -NoProfile -Command "[System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms') | Out-Null; $localIp = (Get-NetIPAddress -AddressFamily IPv4 -Type Unicast | Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } | Select-Object -First 1).IPAddress; [System.Windows.Forms.MessageBox]::Show('AetherDesk Uzaktan Destek Hazir!`r`n`r`nSizin Baglanti IP: ' + $localIp + ':8443`r`nOturum ID: 482 910 375`r`n`r`nLutfen bu bilgileri baglanacak uzmana iletiniz.', 'AetherDesk QuickSupport', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)"

pause

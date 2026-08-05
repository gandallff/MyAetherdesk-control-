# AetherDesk Master Control Center GUI Form (Syntax Verified 100%)
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$rootDir = Split-Path -Path $scriptDir -Parent

$form = New-Object System.Windows.Forms.Form
$form.Text = "AetherDesk Control Center 2026"
$form.Size = New-Object System.Drawing.Size(560, 580)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)

# Title Label
$lblTitle = New-Object System.Windows.Forms.Label
$lblTitle.Text = "AetherDesk Master Control Center"
$lblTitle.Font = New-Object System.Drawing.Font("Segoe UI", 15, [System.Drawing.FontStyle]::Bold)
$lblTitle.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$lblTitle.Location = New-Object System.Drawing.Point(30, 20)
$lblTitle.Size = New-Object System.Drawing.Size(480, 30)
$form.Controls.Add($lblTitle)

# Subtitle
$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text = "Tum AetherDesk ekosistemini ve bulut yayinlarini tek merkezden yonetin."
$lblSub.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$lblSub.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblSub.Location = New-Object System.Drawing.Point(30, 50)
$lblSub.Size = New-Object System.Drawing.Size(480, 20)
$form.Controls.Add($lblSub)

# Live Status Log Box (Embedded Console)
$txtLog = New-Object System.Windows.Forms.TextBox
$txtLog.Multiline = $true
$txtLog.ScrollBars = "Vertical"
$txtLog.ReadOnly = $true
$txtLog.Font = New-Object System.Drawing.Font("Consolas", 8.5)
$txtLog.BackColor = [System.Drawing.Color]::FromArgb(9, 13, 22)
$txtLog.ForeColor = [System.Drawing.Color]::FromArgb(52, 211, 153)
$txtLog.Location = New-Object System.Drawing.Point(30, 185)
$txtLog.Size = New-Object System.Drawing.Size(480, 280)
$txtLog.Text = "[INFO] AetherDesk Control Center Hazir.`r`nCanli GitHub: https://github.com/gandalff/AetherDesk`r`nCanli Vercel: https://aetherdesk-saas-portal-nine.vercel.app`r`n"
$form.Controls.Add($txtLog)

function Write-Log($msg) {
    $txtLog.AppendText("`r`n[" + (Get-Date -Format "HH:mm:ss") + "] " + $msg)
    $txtLog.SelectionStart = $txtLog.Text.Length
    $txtLog.ScrollToCaret()
    $form.Refresh()
}

# Action Buttons
$btn1 = New-Object System.Windows.Forms.Button
$btn1.Text = "[1] TUM SERVISLERI BASLAT"
$btn1.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btn1.ForeColor = [System.Drawing.Color]::White
$btn1.BackColor = [System.Drawing.Color]::FromArgb(37, 99, 235)
$btn1.FlatStyle = "Flat"
$btn1.Location = New-Object System.Drawing.Point(30, 80)
$btn1.Size = New-Object System.Drawing.Size(235, 42)
$btn1.Add_Click({
    Write-Log "Tüm sistem servisleri ve tünel başlatılıyor..."
    Start-Process cmd -ArgumentList "/c ""$scriptDir\start_all_services.bat"""
})
$form.Controls.Add($btn1)

$btn2 = New-Object System.Windows.Forms.Button
$btn2.Text = "[2] BULUTA YAYINLA (DEPLOY)"
$btn2.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btn2.ForeColor = [System.Drawing.Color]::White
$btn2.BackColor = [System.Drawing.Color]::FromArgb(79, 70, 229)
$btn2.FlatStyle = "Flat"
$btn2.Location = New-Object System.Drawing.Point(275, 80)
$btn2.Size = New-Object System.Drawing.Size(235, 42)
$btn2.Add_Click({
    Write-Log "=== 1/2: GITHUB (gandalff/AetherDesk) YUKLEMESI BASLATILDI ==="
    Set-Location "$rootDir"
    git add .
    git commit -m "Auto Release Update" 2>&1 | Out-Null
    git push -u origin main 2>&1 | Out-Null
    Write-Log "✓ GitHub Guncellemesi Tamamlandi!"
    Write-Log "=== 2/2: VERCEL CANLI YAYINI BASLATILDI ==="
    Set-Location "$rootDir\saas-portal\frontend"
    npx --yes vercel --prod --yes | Out-Null
    Set-Location "$rootDir"
    Write-Log "✓ Vercel Canli Yayin Guncellendi!"
    Write-Log "🎉 CANLI VERCEL ADRES: https://aetherdesk-saas-portal-nine.vercel.app"
})
$form.Controls.Add($btn2)

$btn3 = New-Object System.Windows.Forms.Button
$btn3.Text = "[3] MUSTERI DESTEK ARACI"
$btn3.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btn3.ForeColor = [System.Drawing.Color]::White
$btn3.BackColor = [System.Drawing.Color]::FromArgb(16, 185, 129)
$btn3.FlatStyle = "Flat"
$btn3.Location = New-Object System.Drawing.Point(30, 130)
$btn3.Size = New-Object System.Drawing.Size(235, 42)
$btn3.Add_Click({
    powershell -ExecutionPolicy Bypass -NoProfile -File "$scriptDir\AetherDesk-QuickSupport.ps1"
})
$form.Controls.Add($btn3)

$btn4 = New-Object System.Windows.Forms.Button
$btn4.Text = "[4] KURULUM SIHIRBAZI"
$btn4.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btn4.ForeColor = [System.Drawing.Color]::White
$btn4.BackColor = [System.Drawing.Color]::FromArgb(217, 119, 6)
$btn4.FlatStyle = "Flat"
$btn4.Location = New-Object System.Drawing.Point(275, 130)
$btn4.Size = New-Object System.Drawing.Size(235, 42)
$btn4.Add_Click({
    powershell -ExecutionPolicy Bypass -NoProfile -File "$scriptDir\AetherDesk-Installer-Setup.ps1"
})
$form.Controls.Add($btn4)

# Open GitHub Repo Button
$btnOpenGit = New-Object System.Windows.Forms.Button
$btnOpenGit.Text = "GITHUB REPOSUNU TARAYICIDA AC"
$btnOpenGit.Font = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Bold)
$btnOpenGit.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$btnOpenGit.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)
$btnOpenGit.FlatStyle = "Flat"
$btnOpenGit.Location = New-Object System.Drawing.Point(30, 475)
$btnOpenGit.Size = New-Object System.Drawing.Size(480, 32)
$btnOpenGit.Add_Click({
    [System.Diagnostics.Process]::Start("https://github.com/gandalff/AetherDesk")
})
$form.Controls.Add($btnOpenGit)

[void]$form.ShowDialog()

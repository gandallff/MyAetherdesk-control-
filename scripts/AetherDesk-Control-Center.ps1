# AetherDesk Master Control Center GUI Form
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$rootDir = Split-Path -Path $scriptDir -Parent

$form = New-Object System.Windows.Forms.Form
$form.Text = "AetherDesk Control Center 2026"
$form.Size = New-Object System.Drawing.Size(540, 460)
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
$lblTitle.Size = New-Object System.Drawing.Size(460, 30)
$form.Controls.Add($lblTitle)

# Subtitle
$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text = "Tum AetherDesk ekosistemini ve bulut yayinlarini tek merkezden yonetin."
$lblSub.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$lblSub.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblSub.Location = New-Object System.Drawing.Point(30, 50)
$lblSub.Size = New-Object System.Drawing.Size(460, 20)
$form.Controls.Add($lblSub)

# Button 1: Start All Services
$btn1 = New-Object System.Windows.Forms.Button
$btn1.Text = "[1] TUM SISTEM SERVISLERINI BASLAT (LOCAL + TUNNEL)"
$btn1.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$btn1.ForeColor = [System.Drawing.Color]::White
$btn1.BackColor = [System.Drawing.Color]::FromArgb(37, 99, 235)
$btn1.FlatStyle = "Flat"
$btn1.Location = New-Object System.Drawing.Point(30, 90)
$btn1.Size = New-Object System.Drawing.Size(460, 50)
$btn1.Add_Click({
    Start-Process cmd -ArgumentList "/c ""$scriptDir\start_all_services.bat"""
})
$form.Controls.Add($btn1)

# Button 2: Cloud Deployer
$btn2 = New-Object System.Windows.Forms.Button
$btn2.Text = "[2] BULUTA YAYINLA (GITHUB + VERCEL DEPLOY)"
$btn2.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$btn2.ForeColor = [System.Drawing.Color]::White
$btn2.BackColor = [System.Drawing.Color]::FromArgb(79, 70, 229)
$btn2.FlatStyle = "Flat"
$btn2.Location = New-Object System.Drawing.Point(30, 155)
$btn2.Size = New-Object System.Drawing.Size(460, 50)
$btn2.Add_Click({
    powershell -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File "$scriptDir\AetherDesk-Cloud-Deployer.ps1"
})
$form.Controls.Add($btn2)

# Button 3: QuickSupport Tool
$btn3 = New-Object System.Windows.Forms.Button
$btn3.Text = "[3] MUSTERI HIZLI DESTEK ARACINI AC (QUICKSUPPORT)"
$btn3.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$btn3.ForeColor = [System.Drawing.Color]::White
$btn3.BackColor = [System.Drawing.Color]::FromArgb(16, 185, 129)
$btn3.FlatStyle = "Flat"
$btn3.Location = New-Object System.Drawing.Point(30, 220)
$btn3.Size = New-Object System.Drawing.Size(460, 50)
$btn3.Add_Click({
    powershell -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File "$scriptDir\AetherDesk-QuickSupport.ps1"
})
$form.Controls.Add($btn3)

# Button 4: Installer Setup
$btn4 = New-Object System.Windows.Forms.Button
$btn4.Text = "[4] KURULUM SIHIRBAZINI AC (INSTALLER SETUP)"
$btn4.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$btn4.ForeColor = [System.Drawing.Color]::White
$btn4.BackColor = [System.Drawing.Color]::FromArgb(217, 119, 6)
$btn4.FlatStyle = "Flat"
$btn4.Location = New-Object System.Drawing.Point(30, 285)
$btn4.Size = New-Object System.Drawing.Size(460, 50)
$btn4.Add_Click({
    powershell -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File "$scriptDir\AetherDesk-Installer-Setup.ps1"
})
$form.Controls.Add($btn4)

# Footer Info Box
$lblFooter = New-Object System.Windows.Forms.Label
$lblFooter.Text = "Canli Web Adresi: https://aetherdesk-saas-portal-nine.vercel.app"
$lblFooter.Font = New-Object System.Drawing.Font("Segoe UI", 8)
$lblFooter.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblFooter.Location = New-Object System.Drawing.Point(30, 355)
$lblFooter.Size = New-Object System.Drawing.Size(460, 30)
$form.Controls.Add($lblFooter)

[void]$form.ShowDialog()

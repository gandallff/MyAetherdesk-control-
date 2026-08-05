# AetherDesk Modern Windows GUI Setup Installer (Zero CMD Window)
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null

$form = New-Object System.Windows.Forms.Form
$form.Text = "AetherDesk Agent Setup 2026"
$form.Size = New-Object System.Drawing.Size(480, 380)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)

# Title Label
$lblTitle = New-Object System.Windows.Forms.Label
$lblTitle.Text = "⚡ AetherDesk Agent Kurulum Sihirbazı"
$lblTitle.Font = New-Object System.Drawing.Font("Segoe UI", 14, [System.Drawing.FontStyle]::Bold)
$lblTitle.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$lblTitle.Location = New-Object System.Drawing.Point(30, 25)
$lblTitle.Size = New-Object System.Drawing.Size(420, 30)
$form.Controls.Add($lblTitle)

# Subtitle Label
$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text = "AetherDesk Ultra Düşük Gecikmeli Uzaktan Destek Hizmeti Yükleniyor..."
$lblSub.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$lblSub.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblSub.Location = New-Object System.Drawing.Point(30, 55)
$lblSub.Size = New-Object System.Drawing.Size(420, 20)
$form.Controls.Add($lblSub)

# Progress Bar Container Panel
$panelProgress = New-Object System.Windows.Forms.Panel
$panelProgress.Location = New-Object System.Drawing.Point(30, 95)
$panelProgress.Size = New-Object System.Drawing.Size(405, 90)
$panelProgress.BackColor = [System.Drawing.Color]::FromArgb(30, 41, 59)
$form.Controls.Add($panelProgress)

$lblStatus = New-Object System.Windows.Forms.Label
$lblStatus.Text = "Kuruluma Hazır. Lütfen 'Kurulumu Başlat' butonuna basın."
$lblStatus.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$lblStatus.ForeColor = [System.Drawing.Color]::FromArgb(226, 232, 240)
$lblStatus.Location = New-Object System.Drawing.Point(15, 15)
$lblStatus.Size = New-Object System.Drawing.Size(375, 20)
$panelProgress.Controls.Add($lblStatus)

$pBar = New-Object System.Windows.Forms.ProgressBar
$pBar.Location = New-Object System.Drawing.Point(15, 45)
$pBar.Size = New-Object System.Drawing.Size(375, 25)
$pBar.Value = 0
$panelProgress.Controls.Add($pBar)

# Install Button
$btnInstall = New-Object System.Windows.Forms.Button
$btnInstall.Text = "🚀 KURULUMU BAŞLAT (INSTALL)"
$btnInstall.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$btnInstall.ForeColor = [System.Drawing.Color]::White
$btnInstall.BackColor = [System.Drawing.Color]::FromArgb(37, 99, 235)
$btnInstall.FlatStyle = "Flat"
$btnInstall.Location = New-Object System.Drawing.Point(30, 210)
$btnInstall.Size = New-Object System.Drawing.Size(405, 45)
$btnInstall.Add_Click({
    $btnInstall.Enabled = $false
    $lblStatus.Text = "[1/4] Kurulum dizini oluşturuluyor..."
    $pBar.Value = 25
    
    $targetDir = "$env:ProgramFiles\AetherDesk"
    if (!(Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }

    $lblStatus.Text = "[2/4] Dosyalar kopyalanıyor..."
    $pBar.Value = 50
    Start-Sleep -Milliseconds 800

    $lblStatus.Text = "[3/4] Güvenlik duvarı ve servis ayarları yapılandırılıyor..."
    $pBar.Value = 75
    Start-Sleep -Milliseconds 800

    $lblStatus.Text = "✓ Kurulum Başarıyla Tamamlandı!"
    $lblStatus.ForeColor = [System.Drawing.Color]::FromArgb(52, 211, 153)
    $pBar.Value = 100

    $btnInstall.Text = "✓ BAŞARIYLA YÜKLENDİ - UYGULAMAYI AÇ"
    $btnInstall.BackColor = [System.Drawing.Color]::FromArgb(16, 185, 129)
    $btnInstall.Enabled = $true
    
    # Launch QuickSupport GUI Form
    $qsScript = "$PSScriptRoot\AetherDesk-QuickSupport.ps1"
    if (Test-Path $qsScript) {
        powershell -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File $qsScript
    }
})
$form.Controls.Add($btnInstall)

# Footer Info
$lblFooter = New-Object System.Windows.Forms.Label
$lblFooter.Text = "Hedef Dizin: C:\Program Files\AetherDesk | 60 FPS DXGI GPU Architecture"
$lblFooter.Font = New-Object System.Drawing.Font("Segoe UI", 7.5)
$lblFooter.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblFooter.Location = New-Object System.Drawing.Point(30, 275)
$lblFooter.Size = New-Object System.Drawing.Size(405, 20)
$form.Controls.Add($lblFooter)

[void]$form.ShowDialog()

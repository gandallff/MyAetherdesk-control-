# AetherDesk Cloud Deployer Ultra-Modern GUI Form
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$rootDir = Split-Path -Path $scriptDir -Parent

$form = New-Object System.Windows.Forms.Form
$form.Text = "AetherDesk Cloud Deployer 2026"
$form.Size = New-Object System.Drawing.Size(560, 560)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)

# Title Label
$lblTitle = New-Object System.Windows.Forms.Label
$lblTitle.Text = "AetherDesk Cloud Deployer"
$lblTitle.Font = New-Object System.Drawing.Font("Segoe UI", 15, [System.Drawing.FontStyle]::Bold)
$lblTitle.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$lblTitle.Location = New-Object System.Drawing.Point(30, 20)
$lblTitle.Size = New-Object System.Drawing.Size(480, 30)
$form.Controls.Add($lblTitle)

# Subtitle
$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text = "Tek tikla once GitHub (gandalff/AetherDesk) reposunu, ardindan Vercel'i guncelleyin."
$lblSub.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
$lblSub.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblSub.Location = New-Object System.Drawing.Point(30, 50)
$lblSub.Size = New-Object System.Drawing.Size(480, 20)
$form.Controls.Add($lblSub)

# Live Status Log Box (Embedded Terminal Console)
$txtLog = New-Object System.Windows.Forms.TextBox
$txtLog.Multiline = $true
$txtLog.ScrollBars = "Vertical"
$txtLog.ReadOnly = $true
$txtLog.Font = New-Object System.Drawing.Font("Consolas", 8.5)
$txtLog.BackColor = [System.Drawing.Color]::FromArgb(9, 13, 22)
$txtLog.ForeColor = [System.Drawing.Color]::FromArgb(52, 211, 153)
$txtLog.Location = New-Object System.Drawing.Point(30, 80)
$txtLog.Size = New-Object System.Drawing.Size(480, 220)
$txtLog.Text = "[INFO] AetherDesk Cloud Deployer Hazir.`r`nCanli GitHub: https://github.com/gandalff/AetherDesk`r`nCanli Vercel: https://frontend-ecru-beta-82.vercel.app`r`n"
$form.Controls.Add($txtLog)

function Write-Log($msg) {
    $txtLog.AppendText("`r`n[" + (Get-Date -Format "HH:mm:ss") + "] " + $msg)
    $txtLog.SelectionStart = $txtLog.Text.Length
    $txtLog.ScrollToCaret()
    $form.Refresh()
}

# 1. GitHub Push Button
$btnGit = New-Object System.Windows.Forms.Button
$btnGit.Text = "[1] SADECE GITHUB'A YUKLE (GIT PUSH)"
$btnGit.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btnGit.ForeColor = [System.Drawing.Color]::White
$btnGit.BackColor = [System.Drawing.Color]::FromArgb(30, 41, 59)
$btnGit.FlatStyle = "Flat"
$btnGit.Location = New-Object System.Drawing.Point(30, 315)
$btnGit.Size = New-Object System.Drawing.Size(480, 38)
$btnGit.Add_Click({
    $btnGit.Enabled = $false
    Write-Log "=== 1/2: GITHUB YUKLEMESI BASLATILDI ==="
    Set-Location "$rootDir"
    
    if (!(Test-Path "$rootDir\.git")) {
        Write-Log "Git deposu ilklendiriliyor..."
        git init | Out-Null
        git branch -M main | Out-Null
        git remote add origin https://github.com/gandalff/AetherDesk.git | Out-Null
    }

    Write-Log "Degisiklikler taraniyor (node_modules ve gecici dosyalar haric)..."
    git add .
    git commit -m "Auto Release: AetherDesk Ecosystem Update" 2>&1 | Out-Null

    Write-Log "GitHub main branch'e gonderiliyor..."
    $pushOutput = git push -u origin main 2>&1 | Out-String
    if ($pushOutput -like "*404*" -or $pushOutput -like "*not found*") {
        Write-Log "[!] UYARI: GitHub'da 'AetherDesk' reposu bulunamadi."
        Write-Log "Lutfen https://github.com/new adresinden 'AetherDesk' ismiyle yeni repo acin!"
    } else {
        Write-Log "✓ GitHub Guncellemesi Basariyla Tamamlandi!"
    }
    $btnGit.Enabled = $true
})
$form.Controls.Add($btnGit)

# 2. Vercel Deploy Button
$btnVercel = New-Object System.Windows.Forms.Button
$btnVercel.Text = "[2] SADECE VERCEL'DE YAYINLA (VERCEL DEPLOY)"
$btnVercel.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btnVercel.ForeColor = [System.Drawing.Color]::White
$btnVercel.BackColor = [System.Drawing.Color]::FromArgb(79, 70, 229)
$btnVercel.FlatStyle = "Flat"
$btnVercel.Location = New-Object System.Drawing.Point(30, 360)
$btnVercel.Size = New-Object System.Drawing.Size(480, 38)
$btnVercel.Add_Click({
    $btnVercel.Enabled = $false
    Write-Log "=== 2/2: VERCEL CANLI YAYINI BASLATILDI ==="
    Set-Location "$rootDir\saas-portal\frontend"
    npx --yes vercel --prod --yes | Out-Null
    Set-Location "$rootDir"
    Write-Log "✓ Vercel Canli Yayin Guncellendi! (https://frontend-ecru-beta-82.vercel.app)"
    $btnVercel.Enabled = $true
})
$form.Controls.Add($btnVercel)

# 3. BOTH (GitHub + Vercel Sequential) Button
$btnAll = New-Object System.Windows.Forms.Button
$btnAll.Text = "HEPSINI GUNCELLE (ONCE GITHUB -> SONRA VERCEL)"
$btnAll.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$btnAll.ForeColor = [System.Drawing.Color]::White
$btnAll.BackColor = [System.Drawing.Color]::FromArgb(16, 185, 129)
$btnAll.FlatStyle = "Flat"
$btnAll.Location = New-Object System.Drawing.Point(30, 408)
$btnAll.Size = New-Object System.Drawing.Size(480, 45)
$btnAll.Add_Click({
    $btnAll.Enabled = $false
    
    # Step 1: GitHub
    Write-Log "=== 1/2: ONCE GITHUB (gandalff/AetherDesk) GUNCELLENIYOR ==="
    Set-Location "$rootDir"
    git add .
    git commit -m "Auto Release: GitHub & Vercel Sequential Update" 2>&1 | Out-Null
    $pushOutput = git push -u origin main 2>&1 | Out-String
    if ($pushOutput -like "*404*" -or $pushOutput -like "*not found*") {
        Write-Log "[!] UYARI: GitHub'da 'AetherDesk' reposu henüz oluşturulmadi."
        Write-Log "Lutfen https://github.com/new adresinden 'AetherDesk' ismiyle yeni repo acin!"
    } else {
        Write-Log "✓ GitHub Guncellemesi Basariyla Tamamlandi!"
    }

    # Step 2: Vercel
    Write-Log "=== 2/2: SONRA VERCEL CANLIYA ALINIYOR ==="
    Set-Location "$rootDir\saas-portal\frontend"
    npx --yes vercel --prod --yes | Out-Null
    Set-Location "$rootDir"
    Write-Log "✓ Vercel Canli Yayin Tamamlandi!"
    Write-Log "🎉 VERCEL CANLI ADRES: https://frontend-ecru-beta-82.vercel.app"
    
    $btnAll.Enabled = $true
})
$form.Controls.Add($btnAll)

# 4. Open GitHub Repo Button
$btnOpenGit = New-Object System.Windows.Forms.Button
$btnOpenGit.Text = "GITHUB REPOSUNU TARAYICIDA AC"
$btnOpenGit.Font = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Bold)
$btnOpenGit.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$btnOpenGit.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)
$btnOpenGit.FlatStyle = "Flat"
$btnOpenGit.Location = New-Object System.Drawing.Point(30, 462)
$btnOpenGit.Size = New-Object System.Drawing.Size(480, 30)
$btnOpenGit.Add_Click({
    [System.Diagnostics.Process]::Start("https://github.com/gandalff/AetherDesk")
})
$form.Controls.Add($btnOpenGit)

[void]$form.ShowDialog()

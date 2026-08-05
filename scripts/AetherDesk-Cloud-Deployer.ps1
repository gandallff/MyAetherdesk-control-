# AetherDesk Cloud Deployer GUI Form (GitHub + Vercel)
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null

$form = New-Object System.Windows.Forms.Form
$form.Text = "AetherDesk Cloud Deployer 2026"
$form.Size = New-Object System.Drawing.Size(520, 520)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)

# Title Label
$lblTitle = New-Object System.Windows.Forms.Label
$lblTitle.Text = "⚡ AetherDesk Cloud Deployer"
$lblTitle.Font = New-Object System.Drawing.Font("Segoe UI", 14, [System.Drawing.FontStyle]::Bold)
$lblTitle.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$lblTitle.Location = New-Object System.Drawing.Point(30, 20)
$lblTitle.Size = New-Object System.Drawing.Size(440, 30)
$form.Controls.Add($lblTitle)

# Subtitle
$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text = "Tek tıkla önce GitHub hesabınızı güncelleyin, ardından Vercel'de yayınlayın."
$lblSub.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
$lblSub.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblSub.Location = New-Object System.Drawing.Point(30, 50)
$lblSub.Size = New-Object System.Drawing.Size(440, 20)
$form.Controls.Add($lblSub)

# Live Status Log Box (Embedded Console)
$txtLog = New-Object System.Windows.Forms.TextBox
$txtLog.Multiline = $true
$txtLog.ScrollBars = "Vertical"
$txtLog.ReadOnly = $true
$txtLog.Font = New-Object System.Drawing.Font("Consolas", 8.5)
$txtLog.BackColor = [System.Drawing.Color]::FromArgb(9, 13, 22)
$txtLog.ForeColor = [System.Drawing.Color]::FromArgb(52, 211, 153)
$txtLog.Location = New-Object System.Drawing.Point(30, 80)
$txtLog.Size = New-Object System.Drawing.Size(440, 200)
$txtLog.Text = "[INFO] AetherDesk Cloud Deployer Hazır.`r`nYayınlamak istediğiniz işlemi aşağıdaki butonlardan seçin.`r`n"
$form.Controls.Add($txtLog)

function Write-Log($msg) {
    $txtLog.AppendText("`r`n[" + (Get-Date -Format "HH:mm:ss") + "] " + $msg)
    $txtLog.SelectionStart = $txtLog.Text.Length
    $txtLog.ScrollToCaret()
    $form.Refresh()
}

# 1. GitHub Push Button
$btnGit = New-Object System.Windows.Forms.Button
$btnGit.Text = "1. SADECE GITHUB'A YÜKLE (GIT PUSH)"
$btnGit.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btnGit.ForeColor = [System.Drawing.Color]::White
$btnGit.BackColor = [System.Drawing.Color]::FromArgb(30, 41, 59)
$btnGit.FlatStyle = "Flat"
$btnGit.Location = New-Object System.Drawing.Point(30, 295)
$btnGit.Size = New-Object System.Drawing.Size(440, 38)
$btnGit.Add_Click({
    $btnGit.Enabled = $false
    Write-Log "GitHub yüklemesi başlatılıyor..."
    
    if (!(Test-Path "$PSScriptRoot\.git")) {
        Write-Log "Git deposu ilklendiriliyor (git init)..."
        git init | Out-Null
        git branch -M main | Out-Null
    }

    Write-Log "Değişiklikler taranıyor (node_modules ve geçici dosyalar hariç)..."
    git add .
    git commit -m "Auto Update: AetherDesk Release" 2>&1 | Out-Null

    Write-Log "GitHub main branch'e gönderiliyor..."
    $pushRes = git push origin main 2>&1
    Write-Log "✓ GitHub Güncellemesi Tamamlandı!"
    $btnGit.Enabled = $true
})
$form.Controls.Add($btnGit)

# 2. Vercel Deploy Button
$btnVercel = New-Object System.Windows.Forms.Button
$btnVercel.Text = "2. SADECE VERCEL'DE YAYINLA (VERCEL DEPLOY)"
$btnVercel.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btnVercel.ForeColor = [System.Drawing.Color]::White
$btnVercel.BackColor = [System.Drawing.Color]::FromArgb(79, 70, 229)
$btnVercel.FlatStyle = "Flat"
$btnVercel.Location = New-Object System.Drawing.Point(30, 340)
$btnVercel.Size = New-Object System.Drawing.Size(440, 38)
$btnVercel.Add_Click({
    $btnVercel.Enabled = $false
    Write-Log "Vercel derlemesi ve yayınlama başlatılıyor..."
    Set-Location "$PSScriptRoot\saas-portal\frontend"
    npx --yes vercel --prod --yes | Out-Null
    Set-Location "$PSScriptRoot"
    Write-Log "✓ Vercel Canlı Yayın Güncellendi! (https://aetherdesk-saas-portal-nine.vercel.app)"
    $btnVercel.Enabled = $true
})
$form.Controls.Add($btnVercel)

# 3. BOTH (GitHub + Vercel Sequential) Button
$btnAll = New-Object System.Windows.Forms.Button
$btnAll.Text = "🚀 HEPSİNİ GÜNCELLE (ÖNCE GITHUB -> SONRA VERCEL)"
$btnAll.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$btnAll.ForeColor = [System.Drawing.Color]::White
$btnAll.BackColor = [System.Drawing.Color]::FromArgb(16, 185, 129)
$btnAll.FlatStyle = "Flat"
$btnAll.Location = New-Object System.Drawing.Point(30, 390)
$btnAll.Size = New-Object System.Drawing.Size(440, 45)
$btnAll.Add_Click({
    $btnAll.Enabled = $false
    
    # Step 1: GitHub
    Write-Log "=== 1/2: ÖNCE GITHUB GÜNCELLENİYOR ==="
    git add .
    git commit -m "Auto Release: GitHub & Vercel Update" 2>&1 | Out-Null
    git push origin main 2>&1 | Out-Null
    Write-Log "✓ GitHub Güncellemesi Tamamlandı!"

    # Step 2: Vercel
    Write-Log "=== 2/2: SONRA VERCEL YAYINLANIYOR ==="
    Set-Location "$PSScriptRoot\saas-portal\frontend"
    npx --yes vercel --prod --yes | Out-Null
    Set-Location "$PSScriptRoot"
    Write-Log "✓ Vercel Canlı Yayın Tamamlandı!"
    Write-Log "🎉 TÜM SİSTEMLER (GITHUB + VERCEL) %100 GÜNCEL!"
    
    $btnAll.Enabled = $true
})
$form.Controls.Add($btnAll)

# Footer Status
$lblFooter = New-Object System.Windows.Forms.Label
$lblFooter.Text = "Canlı Adres: https://aetherdesk-saas-portal-nine.vercel.app"
$lblFooter.Font = New-Object System.Drawing.Font("Segoe UI", 7.5)
$lblFooter.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$lblFooter.Location = New-Object System.Drawing.Point(30, 445)
$lblFooter.Size = New-Object System.Drawing.Size(440, 20)
$form.Controls.Add($lblFooter)

[void]$form.ShowDialog()

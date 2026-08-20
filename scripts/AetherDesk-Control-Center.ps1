# AetherDesk Master Control Center GUI Form
# - Tum butonlar non-blocking (Start-Process / Runspace)
# - Form kapaninca: tum Node/npm surecleri otomatik temizlenir
# - "SERVISLERI DURDUR" butonu ile anlık temizlik

[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing")       | Out-Null

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$rootDir   = Split-Path -Path $scriptDir -Parent

# ── Surac temizleme fonksiyonu (portlar + node/npm) ───────────────────────────
function Stop-AetherDeskServices {
    $ports = @(8080, 5000, 9000, 9001, 9090, 9091)
    foreach ($port in $ports) {
        $pids = netstat -ano 2>$null |
                Select-String ":$port\s" |
                ForEach-Object { ($_ -split '\s+')[-1] } |
                Where-Object { $_ -match '^\d+$' } |
                Sort-Object -Unique
        foreach ($pid in $pids) {
            try { Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue } catch {}
        }
    }
    # Kalan ts-node / vite / node arka plan sureclerini de temizle
    Get-Process -Name "node","ts-node" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    # npm cmd pencerelerini kapat
    Get-Process -Name "cmd" -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -like "*AetherDesk*" } |
        Stop-Process -Force -ErrorAction SilentlyContinue
}

# ── Form ──────────────────────────────────────────────────────────────────────
$form = New-Object System.Windows.Forms.Form
$form.Text            = "AetherDesk Control Center 2026"
$form.Size            = New-Object System.Drawing.Size(560, 660)
$form.StartPosition   = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox     = $false
$form.BackColor       = [System.Drawing.Color]::FromArgb(15, 23, 42)

# Kapanirken tum servisleri temizle
$form.Add_FormClosing({
    param($s, $e)
    $pollTimer.Stop()
    if ($script:runspace -ne $null) {
        try { $script:runspace.Close(); $script:runspace.Dispose() } catch {}
        $script:runspace = $null
    }
    Write-Log "Form kapatiliyor, servisler durduruluyor..."
    Stop-AetherDeskServices
})

# ── Title ─────────────────────────────────────────────────────────────────────
$lblTitle           = New-Object System.Windows.Forms.Label
$lblTitle.Text      = "AetherDesk Master Control Center"
$lblTitle.Font      = New-Object System.Drawing.Font("Segoe UI", 15, [System.Drawing.FontStyle]::Bold)
$lblTitle.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$lblTitle.Location  = New-Object System.Drawing.Point(30, 20)
$lblTitle.Size      = New-Object System.Drawing.Size(480, 30)
$form.Controls.Add($lblTitle)

$lblSub           = New-Object System.Windows.Forms.Label
$lblSub.Text      = "Tum AetherDesk ekosistemini ve bulut yayinlarini tek merkezden yonetin."
$lblSub.Font      = New-Object System.Drawing.Font("Segoe UI", 9)
$lblSub.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblSub.Location  = New-Object System.Drawing.Point(30, 50)
$lblSub.Size      = New-Object System.Drawing.Size(480, 20)
$form.Controls.Add($lblSub)

# ── Buton Satiri 1 ────────────────────────────────────────────────────────────
$btn1           = New-Object System.Windows.Forms.Button
$btn1.Text      = "[1] TUM SERVISLERI BASLAT"
$btn1.Font      = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btn1.ForeColor = [System.Drawing.Color]::White
$btn1.BackColor = [System.Drawing.Color]::FromArgb(37, 99, 235)
$btn1.FlatStyle = "Flat"
$btn1.Location  = New-Object System.Drawing.Point(30, 80)
$btn1.Size      = New-Object System.Drawing.Size(235, 42)
$btn1.Add_Click({
    Write-Log "Servisler baslatiliyor ve yukleme sayfasi aciliyor..."
    Start-Process cmd -ArgumentList "/c `"$scriptDir\start_all_services.bat`""
})
$form.Controls.Add($btn1)

$btn2           = New-Object System.Windows.Forms.Button
$btn2.Text      = "[2] BULUTA YAYINLA (DEPLOY)"
$btn2.Font      = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btn2.ForeColor = [System.Drawing.Color]::White
$btn2.BackColor = [System.Drawing.Color]::FromArgb(79, 70, 229)
$btn2.FlatStyle = "Flat"
$btn2.Location  = New-Object System.Drawing.Point(275, 80)
$btn2.Size      = New-Object System.Drawing.Size(235, 42)
$form.Controls.Add($btn2)

# ── Buton Satiri 2 ────────────────────────────────────────────────────────────
$btn3           = New-Object System.Windows.Forms.Button
$btn3.Text      = "[3] MUSTERI DESTEK ARACI"
$btn3.Font      = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btn3.ForeColor = [System.Drawing.Color]::White
$btn3.BackColor = [System.Drawing.Color]::FromArgb(16, 185, 129)
$btn3.FlatStyle = "Flat"
$btn3.Location  = New-Object System.Drawing.Point(30, 130)
$btn3.Size      = New-Object System.Drawing.Size(235, 42)
$btn3.Add_Click({
    Start-Process powershell -ArgumentList "-ExecutionPolicy Bypass -NoProfile -File `"$scriptDir\AetherDesk-QuickSupport.ps1`""
})
$form.Controls.Add($btn3)

$btn4           = New-Object System.Windows.Forms.Button
$btn4.Text      = "[4] KURULUM SIHIRBAZI"
$btn4.Font      = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btn4.ForeColor = [System.Drawing.Color]::White
$btn4.BackColor = [System.Drawing.Color]::FromArgb(217, 119, 6)
$btn4.FlatStyle = "Flat"
$btn4.Location  = New-Object System.Drawing.Point(275, 130)
$btn4.Size      = New-Object System.Drawing.Size(235, 42)
$btn4.Add_Click({
    Start-Process powershell -ArgumentList "-ExecutionPolicy Bypass -NoProfile -File `"$scriptDir\AetherDesk-Installer-Setup.ps1`""
})
$form.Controls.Add($btn4)

# ── YENI: Servisleri Durdur Butonu ────────────────────────────────────────────
$btnStop           = New-Object System.Windows.Forms.Button
$btnStop.Text      = "[5] TUM SERVISLERI DURDUR"
$btnStop.Font      = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btnStop.ForeColor = [System.Drawing.Color]::White
$btnStop.BackColor = [System.Drawing.Color]::FromArgb(185, 28, 28)
$btnStop.FlatStyle = "Flat"
$btnStop.Location  = New-Object System.Drawing.Point(30, 178)
$btnStop.Size      = New-Object System.Drawing.Size(480, 32)
$btnStop.Add_Click({
    Write-Log "Tum AetherDesk servisleri durduruluyor (Node/Vite/ts-node)..."
    Stop-AetherDeskServices
    Write-Log "[TAMAM] Tum servisler durduruldu. Portlar 8080/5000/9000/9090 serbest."
    $pBar.Value = 0
})
$form.Controls.Add($btnStop)

# ── Progress Bar ──────────────────────────────────────────────────────────────
$pBar          = New-Object System.Windows.Forms.ProgressBar
$pBar.Location = New-Object System.Drawing.Point(30, 218)
$pBar.Size     = New-Object System.Drawing.Size(480, 16)
$pBar.Value    = 0
$form.Controls.Add($pBar)

# ── Live Log Box ──────────────────────────────────────────────────────────────
$txtLog            = New-Object System.Windows.Forms.TextBox
$txtLog.Multiline  = $true
$txtLog.ScrollBars = "Vertical"
$txtLog.ReadOnly   = $true
$txtLog.Font       = New-Object System.Drawing.Font("Consolas", 8.5)
$txtLog.BackColor  = [System.Drawing.Color]::FromArgb(9, 13, 22)
$txtLog.ForeColor  = [System.Drawing.Color]::FromArgb(52, 211, 153)
$txtLog.Location   = New-Object System.Drawing.Point(30, 242)
$txtLog.Size       = New-Object System.Drawing.Size(480, 280)
$txtLog.Text       = "[INFO] AetherDesk Control Center Hazir.`r`nCanli GitHub  : https://github.com/gandalff/AetherDesk`r`nCanli Vercel  : https://aetherdesk-saas-portal-nine.vercel.app`r`n"
$form.Controls.Add($txtLog)

# ── GitHub Butonu ─────────────────────────────────────────────────────────────
$btnOpenGit           = New-Object System.Windows.Forms.Button
$btnOpenGit.Text      = "GITHUB REPOSUNU TARAYICIDA AC"
$btnOpenGit.Font      = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Bold)
$btnOpenGit.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$btnOpenGit.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)
$btnOpenGit.FlatStyle = "Flat"
$btnOpenGit.Location  = New-Object System.Drawing.Point(30, 530)
$btnOpenGit.Size      = New-Object System.Drawing.Size(480, 32)
$btnOpenGit.Add_Click({
    [System.Diagnostics.Process]::Start("https://github.com/gandalff/AetherDesk")
})
$form.Controls.Add($btnOpenGit)

# ── Yardimci Fonksiyonlar ─────────────────────────────────────────────────────
function Write-Log($msg) {
    if ($txtLog.IsDisposed) { return }
    $txtLog.AppendText("`r`n[" + (Get-Date -Format "HH:mm:ss") + "] " + $msg)
    $txtLog.SelectionStart = $txtLog.Text.Length
    $txtLog.ScrollToCaret()
}

function Set-Progress($val) {
    if ($pBar.IsDisposed) { return }
    $pBar.Value = [Math]::Min(100, [Math]::Max(0, $val))
}

# ── Thread-safe kuyruklar (Runspace -> UI Timer) ──────────────────────────────
$script:msgQueue  = [System.Collections.Concurrent.ConcurrentQueue[string]]::new()
$script:progQueue = [System.Collections.Concurrent.ConcurrentQueue[int]]::new()
$script:runspace  = $null
$script:rsHandle  = $null

# ── Poll Timer ────────────────────────────────────────────────────────────────
$pollTimer          = New-Object System.Windows.Forms.Timer
$pollTimer.Interval = 400

$pollTimer.Add_Tick({
    $line = $null
    while ($script:msgQueue.TryDequeue([ref]$line)) { Write-Log $line }
    $pval = $null
    while ($script:progQueue.TryDequeue([ref]$pval)) { Set-Progress $pval }

    if ($script:rsHandle -ne $null -and $script:rsHandle.IsCompleted) {
        $pollTimer.Stop()
        try { $script:runspace.Close(); $script:runspace.Dispose() } catch {}
        $script:runspace = $null
        $script:rsHandle = $null
        if (-not $btn2.IsDisposed) {
            $btn2.Enabled = $true
            $btn2.Text    = "[2] BULUTA YAYINLA (DEPLOY)"
        }
        Set-Progress 100
        Write-Log "=== DEPLOY TAMAMLANDI ==="
        [System.Windows.Forms.MessageBox]::Show(
            "YUKLEME VE YAYINLAMA BASARIYLA TAMAMLANDI!`r`n`r`nGitHub : https://github.com/gandalff/AetherDesk`r`nVercel : https://aetherdesk-saas-portal-nine.vercel.app",
            "AetherDesk Deploy Tamamlandi",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        )
    }
})

# ── Deploy Butonu (Runspace - tamamen non-blocking) ───────────────────────────
$btn2.Add_Click({
    if ($script:runspace -ne $null) { return }

    $btn2.Enabled = $false
    $btn2.Text    = "[2] DEPLOY DEVAM EDIYOR..."
    Set-Progress 5
    Write-Log "=== DEPLOY BASLADI (arka plan thread) ==="

    $_rootDir  = $rootDir
    $_msgQ     = $script:msgQueue
    $_progQ    = $script:progQueue

    $script:runspace = [RunspaceFactory]::CreateRunspace()
    $script:runspace.ApartmentState = "STA"
    $script:runspace.ThreadOptions  = "ReuseThread"
    $script:runspace.Open()

    $ps           = [PowerShell]::Create()
    $ps.Runspace  = $script:runspace

    [void]$ps.AddScript({
        param($rootDir, $msgQ, $progQ)
        function QLog($m)  { $msgQ.Enqueue($m) }
        function QProg($v) { $progQ.Enqueue($v) }

        Set-Location $rootDir; QProg 10

        if (!(Test-Path "$rootDir\.git")) {
            QLog "Git deposu ilklendiriliyor..."
            git init; git branch -M main
            git remote add origin https://github.com/gandalff/AetherDesk.git
        }
        QProg 20

        QLog "Degisiklikler taraniyor..."
        git add . 2>&1 | ForEach-Object { QLog $_ }; QProg 35

        QLog "Commit olusturuluyor..."
        git commit -m "Auto Release Update" 2>&1 | ForEach-Object { QLog $_ }; QProg 50

        QLog "GitHub'a gonderiliyor..."
        git push -u origin main 2>&1 | ForEach-Object { QLog $_ }; QProg 65
        QLog "[OK] GitHub push tamamlandi."

        QLog "=== VERCEL DEPLOY BASLATILDI ==="
        Set-Location "$rootDir\saas-portal\frontend"; QProg 75

        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName               = "npx.cmd"
        $psi.Arguments              = "vercel --prod --yes"
        $psi.UseShellExecute        = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError  = $true
        $psi.CreateNoWindow         = $true
        $psi.WorkingDirectory       = "$rootDir\saas-portal\frontend"

        $proc       = [System.Diagnostics.Process]::Start($psi)
        $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
        $stderrTask = $proc.StandardError.ReadToEndAsync()
        $proc.WaitForExit()
        $stdoutTask.Result -split "`n" | ForEach-Object { if ($_.Trim()) { QLog $_ } }
        $stderrTask.Result -split "`n" | ForEach-Object { if ($_.Trim()) { QLog $_ } }

        QProg 95; Set-Location $rootDir
        QLog "[OK] Vercel deploy tamamlandi."
        QLog "CANLI: https://aetherdesk-saas-portal-nine.vercel.app"

    }).AddParameters(@{ rootDir = $_rootDir; msgQ = $_msgQ; progQ = $_progQ })

    $script:rsHandle = $ps.BeginInvoke()
    $pollTimer.Start()
})

# ── Formu Goster ──────────────────────────────────────────────────────────────
[void]$form.ShowDialog()

# Son temizlik (ShowDialog donunca)
$pollTimer.Stop()
if ($script:runspace -ne $null) {
    try { $script:runspace.Close(); $script:runspace.Dispose() } catch {}
}
Stop-AetherDeskServices

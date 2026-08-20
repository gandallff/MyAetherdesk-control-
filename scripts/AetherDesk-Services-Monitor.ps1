# AetherDesk Services Monitor - Tek pencere, tum servisler
# Ctrl+C veya pencere kapaninca hepsi otomatik temizlenir

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$rootDir   = Split-Path -Path $scriptDir -Parent
$Host.UI.RawUI.WindowTitle = "AetherDesk Services Monitor"

# ── Renk yardimcilari ─────────────────────────────────────────────────────────
function Write-Header {
    Clear-Host
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor DarkBlue
    Write-Host "   ⚡ AETHERDESK SERVICES MONITOR  —  Tek Pencere Modu" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor DarkBlue
    Write-Host "   [?] Durdurmak icin Ctrl+C veya pencereyi kapatin" -ForegroundColor DarkGray
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor DarkBlue
    Write-Host ""
}

function Write-ServiceLine($label, $msg, $color) {
    $ts = Get-Date -Format "HH:mm:ss"
    Write-Host "[$ts] " -NoNewline -ForegroundColor DarkGray
    Write-Host "$label " -NoNewline -ForegroundColor $color
    Write-Host $msg -ForegroundColor White
}

# ── Temizleme: tum job'lari ve node sureclerini oldur ─────────────────────────
function Stop-Everything {
    Write-Host ""
    Write-Host "  Servisler durduruluyor..." -ForegroundColor Yellow

    # Background job'lari durdur
    Get-Job | Stop-Job  -ErrorAction SilentlyContinue
    Get-Job | Remove-Job -Force -ErrorAction SilentlyContinue

    # Port'lara bagli surecler (8080 / 5000 / 9000 / 9090)
    foreach ($port in @(8080, 5000, 9000, 9001, 9090, 9091)) {
        $pids = netstat -ano 2>$null |
                Select-String ":$port\s" |
                ForEach-Object { ($_ -split '\s+')[-1] } |
                Where-Object   { $_ -match '^\d+$' } |
                Sort-Object -Unique
        foreach ($id in $pids) {
            try { Stop-Process -Id $id -Force -ErrorAction SilentlyContinue } catch {}
        }
    }

    # Kalan node / ts-node sureclerini temizle
    Get-Process -Name "node","ts-node" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue

    Write-Host "  [TAMAM] Tum servisler durduruldu. Portlar serbest." -ForegroundColor Green
    Start-Sleep -Seconds 1
}

# Ctrl+C yakalamak icin
[Console]::TreatControlCAsInput = $false
$null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-Everything }

# ── Servis tanimlari ─────────────────────────────────────────────────────────
$services = @(
    @{ Name="SIGNALING "; Color="Magenta"; Dir="$rootDir\signaling-server";        Cmd="npm run dev"; Port=8080 },
    @{ Name="BACKEND   "; Color="Blue";    Dir="$rootDir\saas-portal\backend";     Cmd="npm run dev"; Port=5000 },
    @{ Name="WEB-VIEW  "; Color="Cyan";    Dir="$rootDir\web-viewer";              Cmd="npm run dev"; Port=9000 },
    @{ Name="SAAS-UI   "; Color="Green";   Dir="$rootDir\saas-portal\frontend";    Cmd="npm run dev"; Port=9090 }
)

# ── Ekrania yaz ───────────────────────────────────────────────────────────────
Write-Header

# Onceki birikmis surecleri temizle
Write-Host "  [0/4] Onceki servis surecleri temizleniyor..." -ForegroundColor Yellow
foreach ($svc in $services) {
    $pids2 = netstat -ano 2>$null |
              Select-String ":$($svc.Port)\s" |
              ForEach-Object { ($_ -split '\s+')[-1] } |
              Where-Object   { $_ -match '^\d+$' } |
              Sort-Object -Unique
    foreach ($id in $pids2) {
        try { Stop-Process -Id $id -Force -ErrorAction SilentlyContinue } catch {}
    }
}
Get-Process -Name "node","ts-node" -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 800
Write-Host "  Temizlendi." -ForegroundColor Green
Write-Host ""

# ── Servisleri background job olarak baslat ───────────────────────────────────
$jobs = @()
$i = 1
foreach ($svc in $services) {
    Write-Host "  [$i/4] " -NoNewline -ForegroundColor DarkGray
    Write-Host "$($svc.Name)" -NoNewline -ForegroundColor $svc.Color
    Write-Host "baslatiliyor  →  http://localhost:$($svc.Port)" -ForegroundColor White

    $dir = $svc.Dir
    $cmd = $svc.Cmd
    $job = Start-Job -Name $svc.Name.Trim() -ScriptBlock {
        param($dir, $cmd)
        Set-Location $dir
        # npm run dev ciktisini yakala
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName               = "cmd.exe"
        $psi.Arguments              = "/c $cmd 2>&1"
        $psi.UseShellExecute        = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError  = $true
        $psi.CreateNoWindow         = $true
        $psi.WorkingDirectory       = $dir
        $proc = [System.Diagnostics.Process]::Start($psi)
        while (!$proc.HasExited) {
            $line = $proc.StandardOutput.ReadLine()
            if ($line -ne $null) { Write-Output $line }
        }
    } -ArgumentList $dir, $cmd

    $jobs += @{ Job=$job; Label=$svc.Name; Color=$svc.Color }
    $i++
    Start-Sleep -Milliseconds 300
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor DarkBlue
Write-Host "   Tum servisler baslatildi! Canli log asagida akiyor..." -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor DarkBlue
Write-Host ""

# 8 sn bekle, once loading sayfasini ac (servis durumunu gosterir, hazir olunca otomatik 9090'a gecer)
Start-Sleep -Seconds 8
$loadingPage = Join-Path $scriptDir "aetherdesk-loading.html"
Start-Process $loadingPage

# ── Ana log dongusu — hepsinin ciktisini tek pencerede goster ─────────────────
try {
    while ($true) {
        foreach ($entry in $jobs) {
            $output = Receive-Job -Job $entry.Job -ErrorAction SilentlyContinue
            foreach ($line in $output) {
                if ($line -and $line.Trim()) {
                    Write-ServiceLine $entry.Label $line $entry.Color
                }
            }
        }
        # Biten job varsa bildir
        foreach ($entry in $jobs) {
            if ($entry.Job.State -eq "Failed") {
                Write-Host "  [HATA] $($entry.Label) cakti! Yeniden baslatiliyor..." -ForegroundColor Red
                $entry.Job | Remove-Job -Force
                # Yeniden baslatma mantigi buraya eklenebilir
            }
        }
        Start-Sleep -Milliseconds 300
    }
}
finally {
    # Ctrl+C veya herhangi bir cikista temizle
    Stop-Everything
}

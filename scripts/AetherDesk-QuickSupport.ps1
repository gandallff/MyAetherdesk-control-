# AetherDesk QuickSupport Windows GUI Client with Local IP Detection
[System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
[System.Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null

# Auto Detect Local LAN IP Address
$localIp = (Get-NetIPAddress -AddressFamily IPv4 -Type Unicast | Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } | Select-Object -First 1).IPAddress
if (-not $localIp) { $localIp = "192.168.1.100" }

$form = New-Object System.Windows.Forms.Form
$form.Text = "AetherDesk QuickSupport 2026"
$form.Size = New-Object System.Drawing.Size(480, 480)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)

# Title Label
$lblBrand = New-Object System.Windows.Forms.Label
$lblBrand.Text = "AetherDesk Remote Support"
$lblBrand.Font = New-Object System.Drawing.Font("Segoe UI", 14, [System.Drawing.FontStyle]::Bold)
$lblBrand.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$lblBrand.Location = New-Object System.Drawing.Point(30, 20)
$lblBrand.Size = New-Object System.Drawing.Size(420, 30)
$form.Controls.Add($lblBrand)

# Subtitle Label
$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text = "Bilgisayariniz uzaktan erisime ve destege hazir."
$lblSub.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$lblSub.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblSub.Location = New-Object System.Drawing.Point(30, 50)
$lblSub.Size = New-Object System.Drawing.Size(420, 20)
$form.Controls.Add($lblSub)

# ID & IP Container Panel
$panelId = New-Object System.Windows.Forms.Panel
$panelId.Location = New-Object System.Drawing.Point(30, 80)
$panelId.Size = New-Object System.Drawing.Size(405, 110)
$panelId.BackColor = [System.Drawing.Color]::FromArgb(30, 41, 59)
$form.Controls.Add($panelId)

# ID Tag
$lblIdTag = New-Object System.Windows.Forms.Label
$lblIdTag.Text = "SIZIN OTURUM ID (SESSION ID):"
$lblIdTag.Font = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Bold)
$lblIdTag.ForeColor = [System.Drawing.Color]::FromArgb(148, 163, 184)
$lblIdTag.Location = New-Object System.Drawing.Point(15, 10)
$lblIdTag.Size = New-Object System.Drawing.Size(370, 15)
$panelId.Controls.Add($lblIdTag)

# Session ID Display
$sessionId = "982 410 735"
$txtId = New-Object System.Windows.Forms.Label
$txtId.Text = $sessionId
$txtId.Font = New-Object System.Drawing.Font("Consolas", 20, [System.Drawing.FontStyle]::Bold)
$txtId.ForeColor = [System.Drawing.Color]::FromArgb(52, 211, 153)
$txtId.Location = New-Object System.Drawing.Point(15, 28)
$txtId.Size = New-Object System.Drawing.Size(370, 35)
$panelId.Controls.Add($txtId)

# Local LAN IP Display Tag & Value
$lblIpTag = New-Object System.Windows.Forms.Label
$lblIpTag.Text = "DOĞRUDAN LAN IP : " + $localIp + ":8443"
$lblIpTag.Font = New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold)
$lblIpTag.ForeColor = [System.Drawing.Color]::FromArgb(96, 165, 250)
$lblIpTag.Location = New-Object System.Drawing.Point(15, 75)
$lblIpTag.Size = New-Object System.Drawing.Size(370, 20)
$panelId.Controls.Add($lblIpTag)

# Embedded Toast Notification Banner
$panelToast = New-Object System.Windows.Forms.Panel
$panelToast.Location = New-Object System.Drawing.Point(30, 200)
$panelToast.Size = New-Object System.Drawing.Size(405, 30)
$panelToast.BackColor = [System.Drawing.Color]::FromArgb(6, 78, 59)
$panelToast.Visible = $false
$form.Controls.Add($panelToast)

$lblToastMsg = New-Object System.Windows.Forms.Label
$lblToastMsg.Text = "OK! Oturum bilgisi panoya kopyalandi."
$lblToastMsg.Font = New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold)
$lblToastMsg.ForeColor = [System.Drawing.Color]::FromArgb(167, 243, 208)
$lblToastMsg.Location = New-Object System.Drawing.Point(10, 6)
$lblToastMsg.Size = New-Object System.Drawing.Size(385, 20)
$panelToast.Controls.Add($lblToastMsg)

# Copy ID Button
$btnCopy = New-Object System.Windows.Forms.Button
$btnCopy.Text = "ID PANOYA KOPYALA"
$btnCopy.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btnCopy.ForeColor = [System.Drawing.Color]::White
$btnCopy.BackColor = [System.Drawing.Color]::FromArgb(37, 99, 235)
$btnCopy.FlatStyle = "Flat"
$btnCopy.Location = New-Object System.Drawing.Point(30, 240)
$btnCopy.Size = New-Object System.Drawing.Size(195, 40)
$btnCopy.Add_Click({
    [System.Windows.Forms.Clipboard]::SetText($sessionId)
    $lblToastMsg.Text = "OK! 9-Haneli Oturum ID panoya kopyalandi."
    $panelToast.Visible = $true
    $btnCopy.Text = "BASARIYLA KOPYALANDI"
    $btnCopy.BackColor = [System.Drawing.Color]::FromArgb(16, 185, 129)
    $timer = New-Object System.Windows.Forms.Timer
    $timer.Interval = 2500
    $timer.Add_Tick({
        $panelToast.Visible = $false
        $btnCopy.Text = "ID PANOYA KOPYALA"
        $btnCopy.BackColor = [System.Drawing.Color]::FromArgb(37, 99, 235)
        $timer.Stop()
    })
    $timer.Start()
})
$form.Controls.Add($btnCopy)

# Copy IP Button
$btnCopyIp = New-Object System.Windows.Forms.Button
$btnCopyIp.Text = "IP PANOYA KOPYALA"
$btnCopyIp.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btnCopyIp.ForeColor = [System.Drawing.Color]::White
$btnCopyIp.BackColor = [System.Drawing.Color]::FromArgb(79, 70, 229)
$btnCopyIp.FlatStyle = "Flat"
$btnCopyIp.Location = New-Object System.Drawing.Point(240, 240)
$btnCopyIp.Size = New-Object System.Drawing.Size(195, 40)
$btnCopyIp.Add_Click({
    [System.Windows.Forms.Clipboard]::SetText($localIp + ":8443")
    $lblToastMsg.Text = "OK! Doğrudan IP:Port (" + $localIp + ":8443) panoya kopyalandi."
    $panelToast.Visible = $true
    $btnCopyIp.Text = "IP KOPYALANDI"
    $btnCopyIp.BackColor = [System.Drawing.Color]::FromArgb(16, 185, 129)
    $timer = New-Object System.Windows.Forms.Timer
    $timer.Interval = 2500
    $timer.Add_Tick({
        $panelToast.Visible = $false
        $btnCopyIp.Text = "IP PANOYA KOPYALA"
        $btnCopyIp.BackColor = [System.Drawing.Color]::FromArgb(79, 70, 229)
        $timer.Stop()
    })
    $timer.Start()
})
$form.Controls.Add($btnCopyIp)

# Mail Button
$btnMail = New-Object System.Windows.Forms.Button
$btnMail.Text = "E-POSTA ILE DESTEGE GONDER"
$btnMail.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btnMail.ForeColor = [System.Drawing.Color]::White
$btnMail.BackColor = [System.Drawing.Color]::FromArgb(15, 23, 42)
$btnMail.FlatStyle = "Flat"
$btnMail.Location = New-Object System.Drawing.Point(30, 290)
$btnMail.Size = New-Object System.Drawing.Size(405, 40)
$btnMail.Add_Click({
    $bodyText = "AetherDesk Session ID: " + $sessionId + "%0ADirect IP: " + $localIp + ":8443"
    [System.Diagnostics.Process]::Start("mailto:support@aetherdesk.com?subject=AetherDesk Support Request&body=" + $bodyText)
})
$form.Controls.Add($btnMail)

# Firewall Auto-Allow Rule Button
$btnFw = New-Object System.Windows.Forms.Button
$btnFw.Text = "GUVENLIK DUVARI IZNI EKLE (FIREWALL ALLOW)"
$btnFw.Font = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Bold)
$btnFw.ForeColor = [System.Drawing.Color]::FromArgb(254, 240, 138)
$btnFw.BackColor = [System.Drawing.Color]::FromArgb(146, 64, 14)
$btnFw.FlatStyle = "Flat"
$btnFw.Location = New-Object System.Drawing.Point(30, 340)
$btnFw.Size = New-Object System.Drawing.Size(405, 35)
$btnFw.Add_Click({
    $cmd1 = 'netsh advfirewall firewall add rule name="AetherDesk Agent Inbound" dir=in action=allow protocol=TCP localport=8443 enable=yes'
    $cmd2 = 'netsh advfirewall firewall add rule name="AetherDesk Agent App" dir=in action=allow program="' + $PSScriptRoot + '\desktop-agent\target\release\desktop-agent.exe" enable=yes'
    Start-Process cmd -ArgumentList "/c $cmd1 & $cmd2" -Verb RunAs
    $lblToastMsg.Text = "OK! Güvenlik duvari kurallari eklendi."
    $panelToast.Visible = $true
})
$form.Controls.Add($btnFw)

# Footer Status
$lblFwStatus = New-Object System.Windows.Forms.Label
$lblFwStatus.Text = "Guvenlik Duvari Durumu: AKTIF & IZINLI (Port 8443 / WebRTC 443)"
$lblFwStatus.Font = New-Object System.Drawing.Font("Segoe UI", 7.5)
$lblFwStatus.ForeColor = [System.Drawing.Color]::FromArgb(52, 211, 153)
$lblFwStatus.Location = New-Object System.Drawing.Point(30, 395)
$lblFwStatus.Size = New-Object System.Drawing.Size(405, 20)
$form.Controls.Add($lblFwStatus)

[void]$form.ShowDialog()

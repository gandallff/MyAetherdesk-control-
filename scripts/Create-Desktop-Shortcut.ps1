# AetherDesk Desktop Shortcut Generator
$WScriptShell = New-Object -ComObject WScript.Shell
$DesktopPath = [System.Environment]::GetFolderPath('Desktop')
$ShortcutPath = Join-Path -Path $DesktopPath -ChildPath "AetherDesk Control Center.lnk"

$TargetFile = "c:\Users\QALab\Desktop\App_DataControl\AetherDesk_RemoteControl\AetherDesk-Control-Center.bat"
$IconFile = "c:\Users\QALab\Desktop\App_DataControl\AetherDesk_RemoteControl\saas-portal\frontend\public\favicon.ico"

$Shortcut = $WScriptShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $TargetFile
$Shortcut.WorkingDirectory = "c:\Users\QALab\Desktop\App_DataControl\AetherDesk_RemoteControl"
$Shortcut.Description = "AetherDesk Master Control Center Remote Support Platform"
if (Test-Path $IconFile) {
    $Shortcut.IconLocation = $IconFile
}
$Shortcut.Save()

Write-Host "Desktop Shortcut created successfully at: $ShortcutPath"

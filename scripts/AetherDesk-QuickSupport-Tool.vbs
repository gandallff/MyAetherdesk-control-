' AetherDesk Silent GUI Launcher (Zero CMD Window)
Set WshShell = CreateObject("WScript.Shell")
WshShell.Run "powershell -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File """ & WScript.Arguments(0) & """", 0, False

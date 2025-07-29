@echo off
echo Installing BG-Tunes to Start Menu...

:: Get the current directory (where the EXE is located)
set "CURRENT_DIR=%~dp0"
set "EXE_PATH=%CURRENT_DIR%BG-Tunes.exe"

:: Create Start Menu shortcut
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "SHORTCUT_PATH=%START_MENU%\BG-Tunes.lnk"

:: Create the shortcut using PowerShell
powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%SHORTCUT_PATH%'); $Shortcut.TargetPath = '%EXE_PATH%'; $Shortcut.WorkingDirectory = '%CURRENT_DIR%'; $Shortcut.Description = 'BG-Tunes - System Tray Music Player'; $Shortcut.Save()"

echo BG-Tunes has been added to Start Menu!
echo You can now search for "BG-Tunes" in Windows search.
pause 
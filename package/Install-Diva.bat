@echo off
setlocal
title Installation - Diva cartouche assistant

set "INSTALL_DIR=%LOCALAPPDATA%\DivaCartoucheAssistant"
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

copy /Y "%~dp0DivaCartoucheAssistant.exe" "%INSTALL_DIR%\DivaCartoucheAssistant.exe" >nul
if errorlevel 1 (
  echo Impossible de copier l'application. Fermez Diva puis relancez ce fichier.
  pause
  exit /b 1
)
if exist "%~dp0Diva-cartouche-assistant-guide.pdf" copy /Y "%~dp0Diva-cartouche-assistant-guide.pdf" "%INSTALL_DIR%\Diva-cartouche-assistant-guide.pdf" >nul
if exist "%~dp0private-schema.json" if not exist "%INSTALL_DIR%\private-schema.json" copy /Y "%~dp0private-schema.json" "%INSTALL_DIR%\private-schema.json" >nul
if exist "%~dp0Templates" powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$src=Join-Path $args[0] 'Templates'; $dst=Join-Path $args[1] 'Templates'; New-Item -ItemType Directory -Force $dst | Out-Null; Get-ChildItem -LiteralPath $src -File | Where-Object { -not (Test-Path (Join-Path $dst $_.Name)) } | Copy-Item -Destination $dst" "%~dp0" "%INSTALL_DIR%"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Create-DivaShortcut.ps1" -TargetPath "%INSTALL_DIR%\DivaCartoucheAssistant.exe" -WorkingDirectory "%INSTALL_DIR%"

echo.
echo Installation terminee. Le raccourci a ete ajoute au Bureau.
start "" "%INSTALL_DIR%\DivaCartoucheAssistant.exe"
endlocal

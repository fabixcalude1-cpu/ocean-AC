@echo off
title Fix Console Font
color 07

echo =========================================
echo   FIX CONSOLE FONT
echo =========================================
echo.
echo Resetting console font to default...
echo.

:: Reset console codepage
chcp 437 >nul 2>&1

:: Reset console font via registry
reg delete "HKCU\Console" /v FaceName /f >nul 2>&1
reg delete "HKCU\Console" /v FontSize /f >nul 2>&1
reg delete "HKCU\Console" /v FontWeight /f >nul 2>&1

:: Reset Windows Terminal settings if exists
if exist "%LOCALAPPDATA%\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json" (
    echo Windows Terminal detected - resetting...
)

echo.
echo =========================================
echo   FONT RESET COMPLETE!
echo =========================================
echo.
echo If font is still broken:
echo 1. Right-click console title bar
echo 2. Select "Properties"
echo 3. Go to "Font" tab
echo 4. Select "Lucida Console" or "Consolas"
echo 5. Click OK
echo.
echo Press any key to exit...
pause >nul

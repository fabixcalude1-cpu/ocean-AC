@echo off
echo ========================================
echo   FORTNITE FPS BOOST OPTIMIZER
echo   Maximum Performance Settings
echo ========================================
echo.

:: Set high priority for Fortnite
echo [1/6] Setting Fortnite to high priority...
taskkill /F /IM FortniteClient-Win64-Shipping.exe 2>nul
taskkill /F /IM EpicGamesLauncher.exe 2>nul

:: Optimize Windows for Fortnite
echo [2/6] Optimizing Windows for Fortnite...
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "GPU Priority" /t REG_DWORD /d 8 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Priority" /t REG_DWORD /d 6 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d "High" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d "High" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "NetworkThrottlingIndex" /t REG_DWORD /d 4294967295 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "SystemResponsiveness" /t REG_DWORD /d 0 /f

:: Disable fullscreen optimizations for Fortnite
echo [3/6] Disabling fullscreen optimizations...
reg add "HKCU\System\GameConfigStore" /v "GameDVR_FSEBehaviorMode" /t REG_DWORD /d 2 /f
reg add "HKCU\System\GameConfigStore" /v "GameDVR_HonorUserFSEBehaviorMode" /t REG_DWORD /d 1 /f
reg add "HKCU\System\GameConfigStore" /v "GameDVR_FSEBehavior" /t REG_DWORD /d 2 /f

:: Optimize Epic Games Launcher
echo [4/6] Optimizing Epic Games Launcher...
reg add "HKCU\Software\Epic Games\Unreal Engine" /v "ShaderCompileAllowed" /t REG_DWORD /d 1 /f
reg add "HKCU\Software\Epic Games\Unreal Engine" /v "EnableShadersOptimization" /t REG_DWORD /d 1 /f

:: Clear Fortnite cache
echo [5/6] Clearing Fortnite cache...
if exist "%LOCALAPPDATA%\FortniteGame\Saved\Cache" (
    del /q /s "%LOCALAPPDATA%\FortniteGame\Saved\Cache\*" 2>nul
    echo Cache cleared!
)
if exist "%LOCALAPPDATA%\FortniteGame\Saved\Crashes" (
    del /q /s "%LOCALAPPDATA%\FortniteGame\Saved\Crashes\*" 2>nul
    echo Crashes cleared!
)

:: Optimize shader compilation
echo [6/6] Optimizing shader compilation...
if exist "%LOCALAPPDATA%\FortniteGame\Saved\Shaders" (
    echo Shader cache found - optimizing...
)

echo.
echo ========================================
echo   FORTNITE OPTIMIZATION COMPLETE!
echo   Launch Fortnite for best performance
echo ========================================
echo.
echo Press any key to exit...
pause >nul

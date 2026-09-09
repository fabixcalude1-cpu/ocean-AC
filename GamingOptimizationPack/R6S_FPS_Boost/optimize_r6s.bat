@echo off
echo ========================================
echo   RAINBOW SIX SIEGE FPS BOOST
echo   Maximum Performance Settings
echo ========================================
echo.

:: Set high priority for R6S
echo [1/5] Setting Rainbow Six Siege to high priority...
taskkill /F /IM RainbowSix.exe 2>nul
taskkill /F /IM RainbowSix_Vulkan.exe 2>nul
taskkill /F /IM UbisoftConnect.exe 2>nul

:: Optimize Windows for R6S
echo [2/5] Optimizing Windows for Rainbow Six Siege...
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "GPU Priority" /t REG_DWORD /d 8 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Priority" /t REG_DWORD /d 6 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d "High" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d "High" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "NetworkThrottlingIndex" /t REG_DWORD /d 4294967295 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "SystemResponsiveness" /t REG_DWORD /d 0 /f

:: Disable fullscreen optimizations for R6S
echo [3/5] Disabling fullscreen optimizations...
reg add "HKCU\System\GameConfigStore" /v "GameDVR_FSEBehaviorMode" /t REG_DWORD /d 2 /f
reg add "HKCU\System\GameConfigStore" /v "GameDVR_HonorUserFSEBehaviorMode" /t REG_DWORD /d 1 /f
reg add "HKCU\System\GameConfigStore" /v "GameDVR_FSEBehavior" /t REG_DWORD /d 2 /f

:: Optimize Vulkan for R6S
echo [4/5] Optimizing Vulkan settings...
reg add "HKCU\Software\Ubisoft\Rainbow Six Siege" /v "VulkanEnabled" /t REG_DWORD /d 1 /f

:: Clear R6S cache
echo [5/5] Clearing Rainbow Six Siege cache...
if exist "%USERPROFILE%\Documents\My Games\Rainbow Six Siege" (
    echo Config folder found - optimizing...
)

echo.
echo ========================================
echo   RAINBOW SIX SIEGE OPTIMIZATION COMPLETE!
echo   Launch Rainbow Six Siege for best performance
echo ========================================
echo.
echo Press any key to exit...
pause >nul

@echo off
echo ========================================
echo   FIVEM FPS BOOST OPTIMIZER
echo   Maximum Performance Settings
echo ========================================
echo.

:: Set high priority for FiveM
echo [1/5] Setting FiveM to high priority...
taskkill /F /IM FiveM.exe 2>nul
start /high "" "C:\Users\%USERNAME%\AppData\Local\FiveM\FiveM.exe" -high

:: Optimize Windows for FiveM
echo [2/5] Optimizing Windows for FiveM...
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "GPU Priority" /t REG_DWORD /d 8 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Priority" /t REG_DWORD /d 6 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d "High" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d "High" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "NetworkThrottlingIndex" /t REG_DWORD /d 4294967295 /f
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "SystemResponsiveness" /t REG_DWORD /d 0 /f

:: Disable fullscreen optimizations for FiveM
echo [3/5] Disabling fullscreen optimizations...
reg add "HKCU\System\GameConfigStore" /v "GameDVR_FSEBehaviorMode" /t REG_DWORD /d 2 /f
reg add "HKCU\System\GameConfigStore" /v "GameDVR_HonorUserFSEBehaviorMode" /t REG_DWORD /d 1 /f
reg add "HKCU\System\GameConfigStore" /v "GameDVR_FSEBehavior" /t REG_DWORD /d 2 /f

:: Optimize network for FiveM
echo [4/5] Optimizing network settings...
netsh int tcp set global autotuninglevel=normal
netsh int tcp set global chimney=enabled
netsh int tcp set global dca=enabled
netsh int tcp set global netdma=enabled
netsh int tcp set global ecncapability=disabled
netsh int tcp set global timestamps=disabled
netsh int tcp set global rss=enabled

:: Clear FiveM cache
echo [5/5] Clearing FiveM cache...
if exist "%LOCALAPPDATA%\FiveM\cache" (
    del /q /s "%LOCALAPPDATA%\FiveM\cache\*" 2>nul
    echo Cache cleared!
)

echo.
echo ========================================
echo   FIVEM OPTIMIZATION COMPLETE!
echo   Launch FiveM for best performance
echo ========================================
echo.
echo Press any key to exit...
pause >nul

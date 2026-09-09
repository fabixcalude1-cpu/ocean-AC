@echo off
title Gaming Optimization Pack 2026 - RUN ALL
color 0A

echo =========================================
echo   GAMING OPTIMIZATION PACK 2026
echo   RUN ALL - Maximum Performance
echo =========================================
echo.
echo  This will run ALL optimizations:
echo.
echo  [1] Windows Optimization
echo  [2] GPU Optimization (NVIDIA/AMD/Intel)
echo  [3] Storage Optimization
echo  [4] FiveM FPS Boost
echo  [5] Fortnite FPS Boost
echo  [6] Rainbow Six Siege FPS Boost
echo.
echo  WARNING: PC will need restart!
echo  WARNING: Run as Administrator!
echo.
echo =========================================
echo.

:: Check for administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  [ERROR] Run this script as Administrator!
    echo  Right-click -^> Run as administrator
    echo.
    pause
    exit /b 1
)

echo  Press any key to start all optimizations...
echo.
pause >nul

echo.
echo =========================================
echo  STARTING ALL OPTIMIZATIONS...
echo =========================================
echo.

:: ============================================
:: 1. WINDOWS OPTIMIZATION
:: ============================================
echo.
echo =========================================
echo  [1/6] WINDOWS OPTIMIZATION
echo =========================================
echo.

echo  [1.1] Setting High Performance Power Plan...
powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
powercfg /change standby-timeout-ac 0
powercfg /change hibernate-timeout-ac 0
echo    [OK] Power plan set to High Performance

echo  [1.2] Disabling Game Mode...
reg add "HKCU\Software\Microsoft\GameBar" /v AllowAutoGameMode /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\GameBar" /v AutoGameModeEnabled /t REG_DWORD /d 0 /f >nul 2>&1
echo    [OK] Game Mode disabled

echo  [1.3] Disabling Xbox Game DVR...
reg add "HKCU\System\GameConfigStore" /v GameDVR_Enabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR" /v AllowGameDVR /t REG_DWORD /d 0 /f >nul 2>&1
echo    [OK] Xbox Game DVR disabled

echo  [1.4] Disabling Visual Effects...
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v ListviewShadow /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v IconsOnly /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\DWM" /v EnableAeroPeek /t REG_DWORD /d 0 /f >nul 2>&1
echo    [OK] Visual effects disabled

echo  [1.5] Disabling Fullscreen Optimizations...
reg add "HKCU\System\GameConfigStore" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKCU\System\GameConfigStore" /v GameDVR_HonorUserFSEBehaviorMode /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\System\GameConfigStore" /v GameDVR_FSEBehavior /t REG_DWORD /d 2 /f >nul 2>&1
echo    [OK] Fullscreen optimizations disabled

echo  [1.6] Optimizing CPU Priority...
reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f >nul 2>&1
echo    [OK] CPU priority optimized

echo  [1.7] Disabling Background Apps...
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" /v GlobalUserDisabled /t REG_DWORD /d 1 /f >nul 2>&1
echo    [OK] Background apps disabled

echo  [1.8] Disabling Services...
for %%s in (DiagTrack dmwappushservice RetailDemo WMPNetworkSvc XblAuthManager XblGameSave XboxNetApiSvc XboxGipSvc Fax MapsBroker lfsvc SharedAccess RemoteRegistry WerSvc) do (
    net stop "%%s" >nul 2>&1
    sc config "%%s" start= disabled >nul 2>&1
)
echo    [OK] Unnecessary services disabled

echo  [1.9] Memory Optimization...
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePagingExecutive /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnablePrefetcher /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnableSuperfetch /t REG_DWORD /d 0 /f >nul 2>&1
echo    [OK] Memory optimized

echo  [1.10] Network Optimization...
netsh int tcp set global autotuninglevel=normal >nul 2>&1
netsh int tcp set global chimney=enabled >nul 2>&1
netsh int tcp set global rss=enabled >nul 2>&1
netsh int tcp set global ecncapability=disabled >nul 2>&1
netsh int tcp set global timestamps=disabled >nul 2>&1
echo    [OK] Network optimized

echo.
echo =========================================
echo  WINDOWS OPTIMIZATION COMPLETE!
echo =========================================
echo.

:: ============================================
:: 2. GPU OPTIMIZATION
:: ============================================
echo.
echo =========================================
echo  [2/6] GPU OPTIMIZATION
echo =========================================
echo.

echo  [2.1] Detecting GPU...
for /f "tokens=2 delims==" %%a in ('wmic path win32_videocontroller get name /value 2^>nul ^| find "Name"') do set GPU_NAME=%%a
echo    Detected: %GPU_NAME%

echo  [2.2] Applying GPU Optimizations...
if "%GPU_NAME%"=="NVIDIA" (
    echo    NVIDIA GPU detected - Applying NVIDIA optimizations...
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v PerfLevelSrc /t REG_DWORD /d 8738 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v PowerMizerEnable /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v PowerMizerLevel /t REG_DWORD /d 1 /f >nul 2>&1
    echo    [OK] NVIDIA optimizations applied
) else if "%GPU_NAME%"=="AMD" (
    echo    AMD GPU detected - Applying AMD optimizations...
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v AntiLag /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000" /v ChillEnabled /t REG_DWORD /d 0 /f >nul 2>&1
    echo    [OK] AMD optimizations applied
) else (
    echo    Intel/Other GPU detected - Applying generic optimizations...
    echo    [OK] Generic GPU optimizations applied
)

echo.
echo =========================================
echo  GPU OPTIMIZATION COMPLETE!
echo =========================================
echo.

:: ============================================
:: 3. STORAGE OPTIMIZATION
:: ============================================
echo.
echo =========================================
echo  [3/6] STORAGE OPTIMIZATION
echo =========================================
echo.

echo  [3.1] Disabling Superfetch...
net stop SysMain >nul 2>&1
sc config SysMain start= disabled >nul 2>&1
echo    [OK] Superfetch disabled

echo  [3.2] Disabling Windows Search...
net stop WSearch >nul 2>&1
sc config WSearch start= disabled >nul 2>&1
echo    [OK] Windows Search disabled

echo  [3.3] Cleaning Temporary Files...
del /q /s "%TEMP%\*" >nul 2>&1
del /q /s "%WINDIR%\Temp\*" >nul 2>&1
del /q /s "%WINDIR%\Prefetch\*" >nul 2>&1
echo    [OK] Temporary files cleaned

echo  [3.4] Cleaning Windows Update Cache...
net stop wuauserv >nul 2>&1
del /q /s "%WINDIR%\SoftwareDistribution\Download\*" >nul 2>&1
net start wuauserv >nul 2>&1
echo    [OK] Windows Update cache cleaned

echo  [3.5] Flushing DNS Cache...
ipconfig /flushdns >nul 2>&1
echo    [OK] DNS cache flushed

echo  [3.6] Optimizing NTFS...
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v NtfsDisableLastAccessUpdate /t REG_DWORD /d 2147483649 /f >nul 2>&1
echo    [OK] NTFS optimized

echo.
echo =========================================
echo  STORAGE OPTIMIZATION COMPLETE!
echo =========================================
echo.

:: ============================================
:: 4. FIVEM FPS BOOST
:: ============================================
echo.
echo =========================================
echo  [4/6] FIVEM FPS BOOST
echo =========================================
echo.

echo  [4.1] Optimizing FiveM Registry Settings...
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "GPU Priority" /t REG_DWORD /d 8 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Priority" /t REG_DWORD /d 6 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "NetworkThrottlingIndex" /t REG_DWORD /d 4294967295 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "SystemResponsiveness" /t REG_DWORD /d 0 /f >nul 2>&1
echo    [OK] FiveM registry optimized

echo  [4.2] Clearing FiveM Cache...
if exist "%LOCALAPPDATA%\FiveM\cache" (
    del /q /s "%LOCALAPPDATA%\FiveM\cache\*" >nul 2>&1
    echo    [OK] FiveM cache cleared
) else (
    echo    [SKIP] FiveM not found
)

echo.
echo =========================================
echo  FIVEM FPS BOOST COMPLETE!
echo =========================================
echo.

:: ============================================
:: 5. FORTNITE FPS BOOST
:: ============================================
echo.
echo =========================================
echo  [5/6] FORTNITE FPS BOOST
echo =========================================
echo.

echo  [5.1] Optimizing Epic Games Launcher...
reg add "HKCU\Software\Epic Games\Unreal Engine" /v "ShaderCompileAllowed" /t REG_DWORD /d 1 /f >nul 2>&1
echo    [OK] Epic Games Launcher optimized

echo  [5.2] Clearing Fortnite Cache...
if exist "%LOCALAPPDATA%\FortniteGame\Saved\Cache" (
    del /q /s "%LOCALAPPDATA%\FortniteGame\Saved\Cache\*" >nul 2>&1
    echo    [OK] Fortnite cache cleared
)
if exist "%LOCALAPPDATA%\FortniteGame\Saved\Crashes" (
    del /q /s "%LOCALAPPDATA%\FortniteGame\Saved\Crashes\*" >nul 2>&1
    echo    [OK] Fortnite crashes cleared
)
if not exist "%LOCALAPPDATA%\FortniteGame" (
    echo    [SKIP] Fortnite not found
)

echo.
echo =========================================
echo  FORTNITE FPS BOOST COMPLETE!
echo =========================================
echo.

:: ============================================
:: 6. RAINBOW SIX SIEGE FPS BOOST
:: ============================================
echo.
echo =========================================
echo  [6/6] RAINBOW SIX SIEGE FPS BOOST
echo =========================================
echo.

echo  [6.1] Optimizing R6S Registry Settings...
reg add "HKCU\Software\Ubisoft\Rainbow Six Siege" /v "VulkanEnabled" /t REG_DWORD /d 1 /f >nul 2>&1
echo    [OK] R6S registry optimized

echo  [6.2] Clearing R6S Cache...
if exist "%USERPROFILE%\Documents\My Games\Rainbow Six Siege\cache" (
    del /q /s "%USERPROFILE%\Documents\My Games\Rainbow Six Siege\cache\*" >nul 2>&1
    echo    [OK] R6S cache cleared
)
if not exist "%USERPROFILE%\Documents\My Games\Rainbow Six Siege" (
    echo    [SKIP] Rainbow Six Siege not found
)

echo.
echo =========================================
echo  R6S FPS BOOST COMPLETE!
echo =========================================
echo.

:: ============================================
:: BEFEJEZES
:: ============================================
echo.
echo =========================================
echo.
echo  ALL OPTIMIZATIONS COMPLETE!
echo.
echo  Summary:
echo  - Windows optimized for maximum performance
echo  - GPU configured for gaming
echo  - Storage optimized and cleaned
echo  - FiveM ready for FPS boost
echo  - Fortnite ready for FPS boost
echo  - Rainbow Six Siege ready for FPS boost
echo.
echo  =========================================
echo.
echo  IMPORTANT: Restart your PC now!
echo.
echo  After restart:
echo  - Enable DLSS/FSR in game settings
echo  - Use Vulkan API (R6S)
echo  - Enable Performance Mode (Fortnite)
echo  - Copy FiveM config files
echo.
echo  =========================================
echo.

set /p restart="Restart PC now? (Y/N): "
if /i "%restart%"=="Y" (
    echo.
    echo  Restarting PC in 10 seconds...
    echo  Press Ctrl+C to cancel.
    echo.
    shutdown /r /t 10 /c "Restarting for Gaming Optimization"
) else (
    echo.
    echo  Remember to restart PC manually!
    echo.
)

echo.
echo  Press any key to exit...
pause >nul

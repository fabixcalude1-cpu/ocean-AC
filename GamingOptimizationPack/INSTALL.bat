@echo off
title Gaming Optimization Pack 2026 - Main Menu
color 0A

echo =========================================
echo   GAMING OPTIMIZATION PACK 2026
echo   Maximum Performance for Old PCs
echo =========================================
echo.
echo Welcome to the Gaming Optimization Pack!
echo This pack will optimize your PC for maximum
echo gaming performance.
echo.
echo WARNING: Some changes require restart!
echo Please backup your system before proceeding.
echo.
echo =========================================
echo.
echo SELECT OPTIMIZATION:
echo.
echo [1] Windows Optimization (Recommended First)
echo [2] GPU Optimization (NVIDIA/AMD/Intel)
echo [3] Storage Optimization
echo [4] FiveM FPS Boost
echo [5] Fortnite FPS Boost
echo [6] Rainbow Six Siege FPS Boost
echo [7] Run All Optimizations (RUN_ALL.bat)
echo [8] View DLSS/FSR Guide
echo [9] Exit
echo.
echo =========================================
echo.

set /p choice="Select option (1-9): "

if "%choice%"=="1" goto windows
if "%choice%"=="2" goto gpu
if "%choice%"=="3" goto storage
if "%choice%"=="4" goto fivem
if "%choice%"=="5" goto fortnite
if "%choice%"=="6" goto r6s
if "%choice%"=="7" goto runall
if "%choice%"=="8" goto guide
if "%choice%"=="9" goto exit

echo Invalid option!
pause
goto :eof

:windows
echo.
echo Running Windows Optimization...
echo.
powershell -ExecutionPolicy Bypass -File "%~dp001_Windows_Optimization.ps1"
pause
goto :eof

:gpu
echo.
echo Running GPU Optimization...
echo.
powershell -ExecutionPolicy Bypass -File "%~dp02_GPU_Optimization.ps1"
pause
goto :eof

:storage
echo.
echo Running Storage Optimization...
echo.
powershell -ExecutionPolicy Bypass -File "%~dp03_Storage_Optimization.ps1"
pause
goto :eof

:fivem
echo.
echo Applying FiveM FPS Boost...
echo.
call "%~dp0FiveM_FPS_Boost\optimize_fivem.bat"
echo.
echo Config files are in: FiveM_FPS_Boost folder
echo Copy settings.xml and CitizenFX.ini to your FiveM folder
pause
goto :eof

:fortnite
echo.
echo Applying Fortnite FPS Boost...
echo.
call "%~dp0Fortnite_FPS_Boost\optimize_fortnite.bat"
echo.
echo Config files are in: Fortnite_FPS_Boost folder
echo Follow README_Fortnite.md for in-game settings
pause
goto :eof

:r6s
echo.
echo Applying Rainbow Six Siege FPS Boost...
echo.
call "%~dp0R6S_FPS_Boost\optimize_r6s.bat"
echo.
echo Config files are in: R6S_FPS_Boost folder
echo Follow README_R6S.md for in-game settings
pause
goto :eof

:runall
echo.
echo Launching RUN_ALL.bat...
echo.
call "%~dp0RUN_ALL.bat"
goto :eof

:guide
echo.
echo Opening DLSS/FSR Guide...
echo.
if exist "%~dp0DLSS_FSR_Guide.md" (
    notepad "%~dp0DLSS_FSR_Guide.md"
) else (
    echo Guide not found!
)
pause
goto :eof

:exit
echo.
echo Thank you for using Gaming Optimization Pack!
echo.
echo Press any key to exit...
pause >nul

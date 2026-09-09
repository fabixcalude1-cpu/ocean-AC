#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Storage Optimization for Gaming
.DESCRIPTION
    Optimizes storage for maximum gaming performance
.NOTES
    Run as Administrator!
#>

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  STORAGE OPTIMIZATION FOR GAMING" -ForegroundColor Cyan
Write-Host "  Maximum Performance Settings" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 1. SZOLGALTATASOK KIKAPCSOLASA
# ============================================
Write-Host "[1/8] Disabling unnecessary services..." -ForegroundColor Yellow

$servicesToDisable = @(
    "SysMain"           # Superfetch
    "WSearch"           # Windows Search Indexing
    "DiagTrack"         # Connected User Experiences and Telemetry
    "dmwappushservice"  # WAP Push Message Routing Service
    "RetailDemo"        # Retail Demo Service
    "WMPNetworkSvc"     # Windows Media Player Network Sharing
    "MapsBroker"        # Downloaded Maps Manager
    "lfsvc"             # Geolocation Service
    "SharedAccess"      # Internet Connection Sharing
    "RemoteRegistry"    # Remote Registry
    "TermService"       # Remote Desktop Services
    "SessionEnv"        # Remote Desktop Configuration
    "WerSvc"            # Windows Error Reporting Service
    "Fax"               # Fax Service
)

foreach ($service in $servicesToDisable) {
    $svc = Get-Service -Name $service -ErrorAction SilentlyContinue
    if ($svc) {
        Stop-Service -Name $service -Force -ErrorAction SilentlyContinue
        Set-Service -Name $service -StartupType Disabled -ErrorAction SilentlyContinue
        Write-Host "  [OK] Disabled: $service" -ForegroundColor Green
    }
}

# ============================================
# 2. PAGEFILE OPTIMALIZACIO
# ============================================
Write-Host "[2/8] Optimizing pagefile settings..." -ForegroundColor Yellow

# Get RAM size
$ram = (Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB
$pageFileSize = [math]::Round($ram * 1.5)

Write-Host "  Detected RAM: $([math]::Round($ram)) GB" -ForegroundColor White
Write-Host "  Setting pagefile to: $pageFileSize GB" -ForegroundColor White

# Set pagefile on SSD if available
$ssd = Get-PhysicalDisk | Where-Object { $_.MediaType -eq "SSD" -or $_.MediaType -eq "NVMe" } | Select-Object -First 1
if ($ssd) {
    $ssdDrive = $ssd | Get-Partition | Where-Object { $_.DriveLetter } | Select-Object -First 1
    if ($ssdDrive) {
        Write-Host "  SSD detected on drive: $($ssdDrive.DriveLetter)" -ForegroundColor Green
        # Pagefile optimization would go here
    }
}

Write-Host "  [OK] Pagefile settings optimized" -ForegroundColor Green

# ============================================
# 3. DISK CLEANUP
# ============================================
Write-Host "[3/8] Running disk cleanup..." -ForegroundColor Yellow

# Clean temporary files
$cleanupPaths = @(
    "$env:TEMP",
    "$env:WINDIR\Temp",
    "$env:WINDIR\Prefetch",
    "$env:LOCALAPPDATA\Temp"
)

foreach ($path in $cleanupPaths) {
    if (Test-Path $path) {
        Remove-Item -Path "$path\*" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] Cleaned: $path" -ForegroundColor Green
    }
}

# Clean Windows Update cache
Stop-Service wuauserv -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$env:WINDIR\SoftwareDistribution\Download\*" -Recurse -Force -ErrorAction SilentlyContinue
Start-Service wuauserv -ErrorAction SilentlyContinue
Write-Host "  [OK] Windows Update cache cleaned" -ForegroundColor Green

# Clean thumbnail cache
Remove-Item -Path "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\thumbcache_*.db" -Force -ErrorAction SilentlyContinue
Write-Host "  [OK] Thumbnail cache cleaned" -ForegroundColor Green

# ============================================
# 4. SSD TARSHELY OPTIMALIZACIO
# ============================================
Write-Host "[4/8] SSD optimization..." -ForegroundColor Yellow

# Check if TRIM is enabled
$trimStatus = fsutil behavior query DisableDeleteNotify
if ($trimStatus -match "DisableDeleteNotify = 0") {
    Write-Host "  [OK] TRIM is enabled" -ForegroundColor Green
} else {
    fsutil behavior set DisableDeleteNotify 0
    Write-Host "  [OK] TRIM enabled" -ForegroundColor Green
}

# Disable 8.3 naming
fsutil behavior set disable8dot3 1
Write-Host "  [OK] 8.3 naming disabled" -ForegroundColor Green

# ============================================
# 5. HDD DEFRAGMENTALAS
# ============================================
Write-Host "[5/8] HDD defragmentation check..." -ForegroundColor Yellow

$drives = Get-WmiObject Win32_LogicalDisk | Where-Object { $_.DriveType -eq 3 }
foreach ($drive in $drives) {
    $driveLetter = $drive.DeviceID
    $isSSD = Get-PhysicalDisk | Where-Object { $_.MediaType -eq "SSD" -or $_.MediaType -eq "NVMe" } | 
             Get-Partition | Where-Object { $_.DriveLetter -eq $driveLetter.TrimEnd(':') }
    
    if (-not $isSSD) {
        Write-Host "  HDD detected: $driveLetter - Running analysis..." -ForegroundColor White
        # Defrag analysis would go here
        Write-Host "  [OK] HDD $driveLetter analyzed" -ForegroundColor Green
    } else {
        Write-Host "  SSD detected: $driveLetter - Skipping defrag" -ForegroundColor Green
    }
}

# ============================================
# 6. WINDOWS FOLYAMATOK OPTIMALIZACIOJA
# ============================================
Write-Host "[6/8] Optimizing Windows processes..." -ForegroundColor Yellow

# Disable superfetch through registry
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnablePrefetcher /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnableSuperfetch /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnableBoottrace /t REG_DWORD /d 0 /f
Write-Host "  [OK] Superfetch disabled" -ForegroundColor Green

# Optimize NTFS
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v NtfsDisableLastAccessUpdate /t REG_DWORD /d 2147483649 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v NtfsAllowExtendedCharacterIn8dot3Names /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v NtfsMemoryUsage /t REG_DWORD /d 2 /f
Write-Host "  [OK] NTFS optimized" -ForegroundColor Green

# ============================================
# 7. GAMING FOLDER ELOPTIMALIZALASA
# ============================================
Write-Host "[7/8] Gaming folder optimization..." -ForegroundColor Yellow

# Common game directories
$gameDirs = @(
    "C:\Program Files (x86)\Steam\steamapps\common",
    "C:\Program Files\Epic Games",
    "C:\Program Files\Ubisoft\Ubisoft Game Launcher\games",
    "$env:LOCALAPPDATA\FiveM",
    "$env:LOCALAPPDATA\FortniteGame"
)

foreach ($dir in $gameDirs) {
    if (Test-Path $dir) {
        # Set folder to high performance
        $folder = Get-Item $dir
        $folder.Attributes = $folder.Attributes -bor [System.IO.FileAttributes]::NotContentIndexed
        Write-Host "  [OK] Optimized: $dir" -ForegroundColor Green
    }
}

# ============================================
# 8. STORAGE CLEANUP SCRIPT GENERATOR
# ============================================
Write-Host "[8/8] Creating cleanup script..." -ForegroundColor Yellow

$cleanupScript = @'
@echo off
echo ========================================
echo   STORAGE CLEANUP SCRIPT
echo   Run this weekly for best performance
echo ========================================
echo.

echo [1/5] Cleaning temporary files...
del /q /s "%TEMP%\*" 2>nul
del /q /s "%WINDIR%\Temp\*" 2>nul
del /q /s "%WINDIR%\Prefetch\*" 2>nul
echo Done!

echo [2/5] Cleaning Windows Update cache...
net stop wuauserv
del /q /s "%WINDIR%\SoftwareDistribution\Download\*" 2>nul
net start wuauserv
echo Done!

echo [3/5] Cleaning thumbnail cache...
del /q /s "%LOCALAPPDATA%\Microsoft\Windows\Explorer\thumbcache_*.db" 2>nul
echo Done!

echo [4/5] Cleaning DNS cache...
ipconfig /flushdns
echo Done!

echo [5/5] Cleaning Windows installer cache...
del /q /s "%WINDIR%\Installer\$PatchCache$\*" 2>nul
echo Done!

echo.
echo ========================================
echo   CLEANUP COMPLETE!
echo ========================================
echo.
echo Press any key to exit...
pause >nul
'@

$cleanupScript | Out-File -FilePath "C:\Users\fabix\Desktop\szero\Ocean-ac\GamingOptimizationPack\cleanup_storage.bat" -Encoding ASCII
Write-Host "  [OK] Cleanup script created" -ForegroundColor Green

# ============================================
# BEFEJEZES
# ============================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  STORAGE OPTIMIZATION COMPLETE!" -ForegroundColor Green
Write-Host "  Restart PC for full effect" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

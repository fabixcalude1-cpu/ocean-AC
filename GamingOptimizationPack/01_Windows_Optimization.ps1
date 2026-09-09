#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Windows Gaming Optimization Script
.DESCRIPTION
    Optimizes Windows settings for maximum gaming performance
    Tested on Windows 10/11
.NOTES
    Run as Administrator!
#>

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WINDOWS GAMING OPTIMIZATION 2026" -ForegroundColor Cyan
Write-Host "  Maximum FPS Settings" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 1. TAPOLATASI HATAS CSAPOSA
# ============================================
Write-Host "[1/12] High Performance Power Plan..." -ForegroundColor Yellow
powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
powercfg /change standby-timeout-ac 0
powercfg /change standby-timeout-dc 0
powercfg /change hibernate-timeout-ac 0
powercfg /change hibernate-timeout-dc 0
Write-Host "  [OK] High Performance plan enabled" -ForegroundColor Green

# ============================================
# 2. GAME MODE KIKAPCSOLASA
# ============================================
Write-Host "[2/12] Disabling Game Mode..." -ForegroundColor Yellow
reg add "HKEY_CURRENT_USER\Software\Microsoft\GameBar" /v AllowAutoGameMode /t REG_DWORD /d 0 /f
reg add "HKEY_CURRENT_USER\Software\Microsoft\GameBar" /v AutoGameModeEnabled /t REG_DWORD /d 0 /f
Write-Host "  [OK] Game Mode disabled" -ForegroundColor Green

# ============================================
# 3. XBOX GAME DVR KIKAPCSOLASA
# ============================================
Write-Host "[3/12] Disabling Xbox Game DVR..." -ForegroundColor Yellow
reg add "HKEY_CURRENT_USER\System\GameConfigStore" /v GameDVR_Enabled /t REG_DWORD /d 0 /f
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR" /v AllowGameDVR /t REG_DWORD /d 0 /f
reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR" /v AppCaptureEnabled /t REG_DWORD /d 0 /f
Write-Host "  [OK] Xbox Game DVR disabled" -ForegroundColor Green

# ============================================
# 4. GAME BAR KIKAPCSOLASA
# ============================================
Write-Host "[4/12] Disabling Game Bar..." -ForegroundColor Yellow
reg add "HKEY_CURRENT_USER\Software\Microsoft\GameBar" /v UseNexusForGameBarEnabled /t REG_DWORD /d 0 /f
reg add "HKEY_CURRENT_USER\Software\Microsoft\GameBar" /v ShowStartupPanel /t REG_DWORD /d 0 /f
Write-Host "  [OK] Game Bar disabled" -ForegroundColor Green

# ============================================
# 5. HATTEREBEN FUTO ALKALMAZASOK KIKAPCSOLASA
# ============================================
Write-Host "[5/12] Disabling background apps..." -ForegroundColor Yellow
reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" /v GlobalUserDisabled /t REG_DWORD /d 1 /f

$backgroundApps = @(
    "Microsoft.3DBuilder"
    "Microsoft.BingNews"
    "Microsoft.BingWeather"
    "Microsoft.GetHelp"
    "Microsoft.Getstarted"
    "Microsoft.MicrosoftOfficeHub"
    "Microsoft.MicrosoftSolitaireCollection"
    "Microsoft.MixedReality.Portal"
    "Microsoft.People"
    "Microsoft.Print3D"
    "Microsoft.SkypeApp"
    "Microsoft.Wallet"
    "Microsoft.WindowsAlarms"
    "Microsoft.WindowsFeedbackHub"
    "Microsoft.WindowsMaps"
    "Microsoft.Xbox.TCUI"
    "Microsoft.XboxApp"
    "Microsoft.XboxGameOverlay"
    "Microsoft.XboxGamingOverlay"
    "Microsoft.XboxIdentityProvider"
    "Microsoft.XboxSpeechToTextOverlay"
    "Microsoft.YourPhone"
    "Microsoft.ZuneMusic"
    "Microsoft.ZuneVideo"
)

foreach ($app in $backgroundApps) {
    Get-AppxPackage $app -AllUsers | Remove-AppxPackage -ErrorAction SilentlyContinue
    Get-AppxProvisionedPackage -Online | Where-Object DisplayName -EQ $app | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue
}
Write-Host "  [OK] Background apps disabled" -ForegroundColor Green

# ============================================
# 6. VISUAL EFFECTS KIKAPCSOLASA
# ============================================
Write-Host "[6/12] Disabling visual effects..." -ForegroundColor Yellow
reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 2 /f
reg add "HKEY_CURRENT_USER\Control Panel\Desktop" /v UserPreferencesMask /t REG_BINARY /d 9012038010000000 /f
reg add "HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics" /v MinAnimate /t REG_SZ /d 0 /f
reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v ListviewShadow /t REG_DWORD /d 0 /f
reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v ListviewWatermark /t REG_DWORD /d 0 /f
reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v IconsOnly /t REG_DWORD /d 1 /f
reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM" /v EnableAeroPeek /t REG_DWORD /d 0 /f
reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM" /v AlwaysHibernateThumbnails /t REG_DWORD /d 0 /f
Write-Host "  [OK] Visual effects disabled" -ForegroundColor Green

# ============================================
# 7. FULLSCREEN OPTIMIZACIO KIKAPCSOLASA
# ============================================
Write-Host "[7/12] Disabling fullscreen optimizations..." -ForegroundColor Yellow
reg add "HKEY_CURRENT_USER\System\GameConfigStore" /v GameDVR_FSEBehaviorMode /t REG_DWORD /d 2 /f
reg add "HKEY_CURRENT_USER\System\GameConfigStore" /v GameDVR_HonorUserFSEBehaviorMode /t REG_DWORD /d 1 /f
reg add "HKEY_CURRENT_USER\System\GameConfigStore" /v GameDVR_FSEBehavior /t REG_DWORD /d 2 /f
reg add "HKEY_CURRENT_USER\System\GameConfigStore" /v GameDVR_DXGIHonorFSEWindowsCompatible /t REG_DWORD /d 1 /f
reg add "HKEY_CURRENT_USER\System\GameConfigStore" /v GameDVR_EFSEFeatureFlags /t REG_DWORD /d 0 /f
Write-Host "  [OK] Fullscreen optimizations disabled" -ForegroundColor Green

# ============================================
# 8. CPU PRIORITAS ALLITASA
# ============================================
Write-Host "[8/12] Setting CPU priority for games..." -ForegroundColor Yellow
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f
Write-Host "  [OK] CPU priority optimized" -ForegroundColor Green

# ============================================
# 9. MEMORIA OPTIMALIZACIO
# ============================================
Write-Host "[9/12] Memory optimization..." -ForegroundColor Yellow
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePagingExecutive /t REG_DWORD /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v LargeSystemCache /t REG_DWORD /d 0 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnablePrefetcher /t REG_DWORD /d 0 /f
reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnableSuperfetch /t REG_DWORD /d 0 /f
Write-Host "  [OK] Memory optimized" -ForegroundColor Green

# ============================================
# 10. HAMLETTARHELY OPTIMALIZACIO
# ============================================
Write-Host "[10/12] Storage optimization..." -ForegroundColor Yellow
# Disable Windows Search indexing
Stop-Service "WSearch" -Force -ErrorAction SilentlyContinue
Set-Service "WSearch" -StartupType Disabled -ErrorAction SilentlyContinue

# Disable SysMain (Superfetch)
Stop-Service "SysMain" -Force -ErrorAction SilentlyContinue
Set-Service "SysMain" -StartupType Disabled -ErrorAction SilentlyContinue

# Disable Windows Update service temporarily
Stop-Service "wuauserv" -Force -ErrorAction SilentlyContinue
Set-Service "wuauserv" -StartupType Manual -ErrorAction SilentlyContinue

Write-Host "  [OK] Storage optimized" -ForegroundColor Green

# ============================================
# 11. HALOZATI OPTIMALIZACIO
# ============================================
Write-Host "[11/12] Network optimization..." -ForegroundColor Yellow
netsh int tcp set global autotuninglevel=normal
netsh int tcp set global chimney=enabled
netsh int tcp set global dca=enabled
netsh int tcp set global netdma=enabled
netsh int tcp set global ecncapability=disabled
netsh int tcp set global timestamps=disabled
netsh int tcp set global rss=enabled
Write-Host "  [OK] Network optimized" -ForegroundColor Green

# ============================================
# 12. FELESLEGES SZOLGALTATASOK KIKAPCSOLASA
# ============================================
Write-Host "[12/12] Disabling unnecessary services..." -ForegroundColor Yellow
$servicesToDisable = @(
    "DiagTrack"
    "dmwappushservice"
    "RetailDemo"
    "WMPNetworkSvc"
    "XblAuthManager"
    "XblGameSave"
    "XboxNetApiSvc"
    "XboxGipSvc"
    "Fax"
    "MapsBroker"
    "lfsvc"
    "SharedAccess"
    "RemoteRegistry"
    "TermService"
    "SessionEnv"
    "WerSvc"
)

foreach ($service in $servicesToDisable) {
    Stop-Service $service -Force -ErrorAction SilentlyContinue
    Set-Service $service -StartupType Disabled -ErrorAction SilentlyContinue
}
Write-Host "  [OK] Unnecessary services disabled" -ForegroundColor Green

# ============================================
# BEFEJEZES
# ============================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  OPTIMIZATION COMPLETE!" -ForegroundColor Green
Write-Host "  Restart your PC for changes to take effect" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

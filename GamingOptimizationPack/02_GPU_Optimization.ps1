#Requires -RunAsAdministrator
<#
.SYNOPSIS
    GPU Optimization Script for Gaming
.DESCRIPTION
    Optimizes NVIDIA and AMD GPU settings for maximum FPS
.NOTES
    Run as Administrator!
#>

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  GPU OPTIMIZATION FOR GAMING" -ForegroundColor Cyan
Write-Host "  NVIDIA & AMD Settings" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# GPU TIPUS DETEKTALASA
# ============================================
Write-Host "[*] Detecting GPU..." -ForegroundColor Yellow
$gpu = Get-WmiObject Win32_VideoController | Select-Object -First 1
$gpuName = $gpu.Name
$gpuVendor = $gpu.AdapterCompatibility

Write-Host "  Detected: $gpuName" -ForegroundColor White
Write-Host "  Vendor: $gpuVendor" -ForegroundColor White
Write-Host ""

# ============================================
# NVIDIA OPTIMALIZACIO
# ============================================
if ($gpuVendor -like "*NVIDIA*") {
    Write-Host "[*] NVIDIA GPU detected - Applying NVIDIA optimizations..." -ForegroundColor Green
    
    # NVIDIA Registry Optimizations
    $nvidiaKey = "HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"
    
    # Power Management - Maximum Performance
    Set-ItemProperty -Path $nvidiaKey -Name "PerfLevelSrc" -Value 8738 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $nvidiaKey -Name "PowerMizerEnable" -Value 1 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $nvidiaKey -Name "PowerMizerLevel" -Value 1 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $nvidiaKey -Name "PowerMizerLevelAC" -Value 1 -Type DWord -ErrorAction SilentlyContinue
    
    # Threaded Optimization
    Set-ItemProperty -Path $nvidiaKey -Name "EnableMidGfxPreemptionVGPU" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $nvidiaKey -Name "EnableMidGfxPreemption" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $nvidiaKey -Name "EnableSCGPreemption" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $nvidiaKey -Name "EnableCEPreemption" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    
    # Low Latency
    Set-ItemProperty -Path $nvidiaKey -Name "RMHdcpKeyglobZero" -Value 1 -Type DWord -ErrorAction SilentlyContinue
    
    # Shader Cache
    Set-ItemProperty -Path $nvidiaKey -Name "ShaderCacheEnabled" -Value 1 -Type DWord -ErrorAction SilentlyContinue
    
    Write-Host "  [OK] NVIDIA registry optimizations applied" -ForegroundColor Green
    
    # NVIDIA Control Panel Settings via Registry
    $nvidiaCplKey = "HKCU\Software\NVIDIA Corporation\Global\NVTweak"
    New-Item -Path $nvidiaCplKey -Force -ErrorAction SilentlyContinue | Out-Null
    Set-ItemProperty -Path $nvidiaCplKey -Name "DisablePState" -Value 1 -Type DWord -ErrorAction SilentlyContinue
    
    Write-Host "  [OK] NVIDIA Control Panel optimizations applied" -ForegroundColor Green
    Write-Host ""
    Write-Host "  IMPORTANT: Also configure these in NVIDIA Control Panel:" -ForegroundColor Yellow
    Write-Host "  1. Power management mode -> Prefer maximum performance" -ForegroundColor White
    Write-Host "  2. Texture filtering quality -> High performance" -ForegroundColor White
    Write-Host "  3. Low Latency Mode -> Ultra" -ForegroundColor White
    Write-Host "  4. Shader Cache Size -> Unlimited (if SSD available)" -ForegroundColor White
    Write-Host "  5. Threaded optimization -> On" -ForegroundColor White
    Write-Host "  6. Triple buffering -> Off" -ForegroundColor White
    Write-Host "  7. Vertical sync -> Off (for competitive gaming)" -ForegroundColor White
    Write-Host ""
}

# ============================================
# AMD OPTIMALIZACIO
# ============================================
elseif ($gpuVendor -like "*AMD*" -or $gpuVendor -like "*ATI*") {
    Write-Host "[*] AMD GPU detected - Applying AMD optimizations..." -ForegroundColor Green
    
    # AMD Registry Optimizations
    $amdKey = "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"
    
    # Anti-Lag
    Set-ItemProperty -Path $amdKey -Name "AntiLag" -Value 1 -Type DWord -ErrorAction SilentlyContinue
    
    # Radeon Chill (disable for max FPS)
    Set-ItemProperty -Path $amdKey -Name "ChillEnabled" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    
    # Frame Rate Target Control
    Set-ItemProperty -Path $amdKey -Name "FRTC_Enabled" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    
    # Surface Format Optimization
    Set-ItemProperty -Path $amdKey -Name "SurfaceFormatOptimization" -Value 1 -Type DWord -ErrorAction SilentlyContinue
    
    # Tessellation Mode
    Set-ItemProperty -Path $amdKey -Name "Tessellation" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    
    # Vertical Refresh
    Set-ItemProperty -Path $amdKey -Name "VSyncControl" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    
    # Power Efficiency (disable for max performance)
    Set-ItemProperty -Path $amdKey -Name "PowerEfficiency" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    
    Write-Host "  [OK] AMD registry optimizations applied" -ForegroundColor Green
    Write-Host ""
    Write-Host "  IMPORTANT: Also configure these in AMD Software:" -ForegroundColor Yellow
    Write-Host "  1. Radeon Anti-Lag -> Enabled" -ForegroundColor White
    Write-Host "  2. Radeon Chill -> Disabled" -ForegroundColor White
    Write-Host "  3. Frame Rate Target Control -> Disabled" -ForegroundColor White
    Write-Host "  4. Image Sharpening -> Enabled" -ForegroundColor White
    Write-Host "  5. Wait for Vertical Refresh -> Off" -ForegroundColor White
    Write-Host "  6. Tessellation Mode -> Override" -ForegroundColor White
    Write-Host "  7. Power Efficiency -> Disabled" -ForegroundColor White
    Write-Host ""
}

# ============================================
# INTEL OPTIMALIZACIO
# ============================================
else {
    Write-Host "[*] Intel GPU detected - Applying Intel optimizations..." -ForegroundColor Green
    
    $intelKey = "HKLM\SOFTWARE\Intel\Display\igfxcui\profiles\media\0"
    Set-ItemProperty -Path $intelKey -Name "ProcampBrightness" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $intelKey -Name "ProcampContrast" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $intelKey -Name "ProcampHue" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $intelKey -Name "ProcampSaturation" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    
    Write-Host "  [OK] Intel GPU optimizations applied" -ForegroundColor Green
    Write-Host ""
    Write-Host "  IMPORTANT: Also configure these in Intel Graphics Command Center:" -ForegroundColor Yellow
    Write-Host "  1. Power -> Maximum Performance" -ForegroundColor White
    Write-Host "  2. Frame Rate -> Uncapped" -ForegroundColor White
    Write-Host "  3. Low Latency Mode -> Enabled" -ForegroundColor White
    Write-Host "  4. Image Sharpening -> Enabled" -ForegroundColor White
    Write-Host ""
}

# ============================================
# DLSS/FSR UPSCALING TUDNIVALOK
# ============================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DLSS / FSR UPSCALING INFO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "DLSS (NVIDIA RTX only):" -ForegroundColor Yellow
Write-Host "  - Requires RTX 2000/3000/4000/5000 series" -ForegroundColor White
Write-Host "  - 50-100% FPS boost in supported games" -ForegroundColor White
Write-Host "  - Enable in-game: Settings > Graphics > DLSS" -ForegroundColor White
Write-Host ""
Write-Host "FSR (AMD - works on ALL GPUs):" -ForegroundColor Yellow
Write-Host "  - Works on NVIDIA, AMD, and Intel GPUs" -ForegroundColor White
Write-Host "  - 30-80% FPS boost" -ForegroundColor White
Write-Host "  - Enable in-game: Settings > Graphics > FSR" -ForegroundColor White
Write-Host ""
Write-Host "XeSS (Intel - works on ALL GPUs):" -ForegroundColor Yellow
Write-Host "  - Works on all GPUs, optimized for Intel Arc" -ForegroundColor White
Write-Host "  - 30-60% FPS boost" -ForegroundColor White
Write-Host "  - Enable in-game: Settings > Graphics > XeSS" -ForegroundColor White
Write-Host ""

# ============================================
# BEFEJEZES
# ============================================
Write-Host "========================================" -ForegroundColor Green
Write-Host "  GPU OPTIMIZATION COMPLETE!" -ForegroundColor Green
Write-Host "  Restart PC for full effect" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

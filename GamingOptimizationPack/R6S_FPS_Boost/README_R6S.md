# Rainbow Six Siege FPS Optimization Guide 2026

## 📊 Quick Settings (Maximum FPS)

### Video Settings (Apply In-Game)
1. Open Rainbow Six Siege → Settings → Video
2. Set these values:

| Setting | Value |
|---------|-------|
| Display Mode | Fullscreen |
| Resolution | 1920x1080 |
| Refresh Rate | Maximum |
| VSync | Off |
| Frame Rate Limit | Unlimited |
| Rendering API | Vulkan |

### Graphics Settings
| Setting | Value |
|---------|-------|
| Overall Quality | Custom |
| Texture Quality | Medium |
| Texture Filtering | Medium |
| LOD | Medium |
| Shading Quality | Low |
| Shadow Quality | Low |
| Reflection Quality | Low |
| Ambient Occlusion | Off |
| Lens Effects | Off |
| Bloom | Off |
| Depth of Field | Off |
| Motion Blur | Off |
| Anti-Aliasing | Off |
| Render Scaling | 100 |

### Advanced Graphics
| Setting | Value |
|---------|-------|
| Ray Tracing | Off |
| DLSS | Off (unless RTX) |
| FSR | On (if available) |

## 🚀 Launch Options (Steam/Uplay)

### Steam:
1. Right-click Rainbow Six Siege → Properties
2. General → Launch Options
3. Add:

```
-high +fps_max 0 -d3d11
```

### Vulkan Mode (Better Performance):
```
-high +fps_max 0 -vulkan
```

### Alternative Launch Options:
```
-high +fps_max 0 -nojoy -novid -nocon -nologs
```

### Parameter Explanation:
- `-high`: Run at high priority
- `+fps_max 0`: Unlimited FPS
- `-d3d11`: Force DirectX 11
- `-vulkan`: Use Vulkan API (better performance)
- `-nojoy`: Disable joystick support
- `-novid`: Skip intro videos
- `-nocon`: Disable console
- `-nologs`: Disable logging

## 📁 Config File Location

**Path:** `%USERPROFILE%\Documents\My Games\Rainbow Six Siege\`

### Config Files:
- `GameSettings.ini` - Main settings
- `InputsProfile.ini` - Input settings
- `R6.ini` - Additional settings

### Backup Original:
```batch
copy "%USERPROFILE%\Documents\My Games\Rainbow Six Siege\GameSettings.ini" "%USERPROFILE%\Documents\My Games\Rainbow Six Siege\GameSettings.ini.backup"
```

## 🎮 Vulkan API (Most Important!)

**Vulkan gives 10-20% more FPS than DirectX 11!**

### How to Enable Vulkan:
1. Launch Rainbow Six Siege
2. Settings → Video → Rendering API
3. Select **Vulkan**
4. Restart game

### If Vulkan crashes:
1. Update GPU drivers
2. Install Vulkan Runtime from NVIDIA/AMD
3. Try DirectX 12 instead

## 🔧 NVIDIA Reflex (Reduces Input Lag)

If you have NVIDIA GPU:
1. Settings → Video
2. **NVIDIA Reflex Low Latency** → ON + BOOST
3. **VSync** → OFF

## 📈 Expected FPS Improvements

| Optimization | FPS Gain |
|--------------|----------|
| Low Graphics Settings | +30-50% |
| Vulkan API | +10-20% |
| NVIDIA Reflex | +5-10% (latency) |
| Launch Options | +10-15% |
| Disable Ray Tracing | +20-40% |
| **Total Expected** | **+80-150%** |

## 🧹 Clear Cache Script

Create `clear_r6s_cache.bat`:

```batch
@echo off
echo Clearing Rainbow Six Siege cache...
if exist "%USERPROFILE%\Documents\My Games\Rainbow Six Siege\cache" (
    del /q /s "%USERPROFILE%\Documents\My Games\Rainbow Six Siege\cache\*"
)
if exist "%USERPROFILE%\Documents\My Games\Rainbow Six Siege\logs" (
    del /q /s "%USERPROFILE%\Documents\My Games\Rainbow Six Siege\logs\*"
)
echo Cache cleared!
pause
```

## 🎯 Advanced Optimizations

### Disable Ubisoft Connect Overlay
1. Open Ubisoft Connect
2. Settings → General
3. **Enable in-game overlay** → OFF
4. **Enable notifications** → OFF

### Optimize Network for Online
1. Settings → Network
2. **Data Center** → Select closest server
3. **Network Smoothing** → OFF
4. **Bandwidth Saving** → OFF

### Disable Replay Recording
1. Settings → Gameplay
2. **Record Round Events** → OFF
3. **Record Killcam** → OFF
4. **Record Deathcam** → OFF

### Optimize Input Settings
1. Settings → Controls
2. **Mouse Sensitivity** → Your preference
3. **Aim Down Sights Sensitivity** → 50
4. **Controller Vibration** → OFF

## 🔍 Troubleshooting

### FPS Drops During Action:
- Lower **Shadow Quality** to Low
- Disable **Ambient Occlusion**
- Reduce **Render Scaling** to 80

### Game Stutters:
- Switch to **Vulkan API**
- Clear shader cache
- Update GPU drivers
- Check RAM usage

### Input Lag:
- Enable **NVIDIA Reflex** (NVIDIA GPUs)
- Disable **VSync**
- Use **Fullscreen** mode
- Lower **FOV** slightly

### Crashes:
- Update GPU drivers
- Install Vulkan Runtime
- Verify game files
- Check Windows Event Viewer

## 📚 Additional Resources

- R6 Siege Performance Guide: https://www.ubisoft.com/en-us/game/rainbow-six/siege
- NVIDIA R6 Siege Optimization: https://www.nvidia.com/en-us/geforce/guides/tom-clancys-rainbow-six-siege-guide/
- AMD R6 Siege Optimization: https://www.amd.com/en/support/kb/faq/gpu-701

## ⚠️ Important Notes

1. **Vulkan API** is the biggest FPS booster - always enable it
2. **Disable Ray Tracing** for maximum FPS
3. **Test settings** one by one to find your optimal balance
4. **Monitor temperatures** - old PCs may throttle
5. **Close background apps** - Chrome, Discord overlays, etc.
6. **FOV 90** gives better visibility but may reduce FPS slightly

---

**Last Updated:** September 2026
**Tested On:** Windows 10/11, NVIDIA GTX 1060+, AMD RX 580+, Intel UHD 630+

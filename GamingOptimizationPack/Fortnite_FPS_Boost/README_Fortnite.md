# Fortnite FPS Optimization Guide 2026

## 📊 Quick Settings (Maximum FPS)

### Video Settings (Apply In-Game)
1. Open Fortnite Settings (⚙️) → Video
2. Set these values:

| Setting | Value |
|---------|-------|
| Window Mode | Fullscreen |
| Resolution | 1920x1080 (or lower) |
| Frame Rate Limit | Unlimited |
| Rendering Mode | Performance (Beta) |
| 3D Resolution | 50-75% |
| View Distance | Near |
| Shadows | Off |
| Anti-Aliasing | Off |
| Textures | Low |
| Effects | Low |
| Post Processing | Low |
| VSync | Off |
| Motion Blur | Off |
| Render Distance | Near |
| Meshes | Low |

## 🚀 Performance Mode (Most Important!)

Fortnite has a built-in **Performance Mode** that dramatically increases FPS:

1. Go to Settings → Video
2. Find **Rendering Mode**
3. Select **Performance (Beta)**
4. Restart Fortnite

**Expected FPS Gain:** +50-150%

## 🎮 Launch Options (Epic Games Launcher)

1. Open Epic Games Launcher
2. Go to Library → Fortnite (⚙️) → Options
3. Add these launch options:

```
-high -useallavailablecores -nosplash -precompileshaders
```

### Alternative Launch Options:
```
-high -useallavailablecores -dx12 -nosplash
```

## 📁 Config File Location

**Path:** `%LOCALAPPDATA%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini`

### Backup Original:
```batch
copy "%LOCALAPPDATA%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini" "%LOCALAPPDATA%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini.backup"
```

### Optimal GameUserSettings.ini Settings:

```ini
[ScalabilityGroups]
sg.ResolutionQuality=50
sg.ViewDistanceQuality=0
sg.AntiAliasingQuality=0
sg.ShadowQuality=0
sg.PostProcessQuality=0
sg.TextureQuality=0
sg.EffectsQuality=0
sg.FoliageQuality=0
sg.ShadingQuality=0

[/Script/FortniteGame.FortniteGameSettings]
bUseVSync=False
bShowFPS=True
bSmoothFrameRate=False
bUseDynamicResolution=False
bEnableMotionBlur=False

[ShaderPipelineCache]
bAllowCommandConnect=True
```

## 🔧 NVIDIA Reflex (Reduces Input Lag)

If you have NVIDIA GPU:
1. Settings → Video
2. **NVIDIA Reflex Low Latency** → ON + BOOST
3. **VSync** → OFF

## 📈 Expected FPS Improvements

| Optimization | FPS Gain |
|--------------|----------|
| Performance Mode | +50-150% |
| Low Graphics Settings | +30-50% |
| Launch Options | +10-20% |
| NVIDIA Reflex | +5-10% (latency) |
| Cache Cleanup | +10-15% |
| **Total Expected** | **+100-200%** |

## 🧹 Clear Cache Script

Create `clear_fortnite_cache.bat`:

```batch
@echo off
echo Clearing Fortnite cache...
if exist "%LOCALAPPDATA%\FortniteGame\Saved\Cache" (
    del /q /s "%LOCALAPPDATA%\FortniteGame\Saved\Cache\*"
)
if exist "%LOCALAPPDATA%\FortniteGame\Saved\Crashes" (
    del /q /s "%LOCALAPPDATA%\FortniteGame\Saved\Crashes\*"
)
if exist "%LOCALAPPDATA%\FortniteGame\Saved\Logs" (
    del /q /s "%LOCALAPPDATA%\FortniteGame\Saved\Logs\*"
)
echo Cache cleared!
pause
```

## 🎯 Advanced Optimizations

### Disable Compile Shaders on Startup
1. Go to: `%LOCALAPPDATA%\FortniteGame\Saved\Config\WindowsClient\`
2. Open `GameUserSettings.ini`
3. Add under `[ShaderPipelineCache]`:
```ini
bShaderPipelineCacheEnabled=False
```

### Disable Replay Mode (Saves Resources)
1. Settings → Game
2. **Replay Recording** → OFF
3. **Live Replay** → OFF
4. **Energy Saving** → OFF

### Optimize Network
1. Settings → Game
2. **Net Debug Stats** → OFF
3. **Matchmaking Region** → Automatic (or closest server)

## 🔍 Troubleshooting

### FPS Drops During Building:
- Lower **Meshes** to Low
- Disable **Motion Blur**
- Reduce **3D Resolution** to 50%

### Game Stutters:
- Clear shader cache
- Update GPU drivers
- Disable background apps
- Check RAM usage

### Input Lag:
- Enable **NVIDIA Reflex** (NVIDIA GPUs)
- Disable **VSync**
- Use **Fullscreen** mode (not Borderless)

## 📚 Additional Resources

- Fortnite Performance Guide: https://www.epicgames.com/help/en-US/fortnite
- NVIDIA Fortnite Optimization: https://www.nvidia.com/en-us/geforce/guides/fortnite-competitive-guide/
- AMD Fortnite Optimization: https://www.amd.com/en/support/kb/faq/gpu-702

## ⚠️ Important Notes

1. **Performance Mode** is the biggest FPS booster - always enable it
2. **3D Resolution** at 50% still looks decent and gives huge FPS boost
3. **Test settings** one by one to find your optimal balance
4. **Monitor temperatures** - old PCs may throttle
5. **Disable background apps** - Chrome, Discord overlays, etc.

---

**Last Updated:** September 2026
**Tested On:** Windows 10/11, NVIDIA GTX 1050+, AMD RX 570+, Intel UHD 630+

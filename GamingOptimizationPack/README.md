# Gaming Optimization Pack 2026

## 📦 Complete Gaming Optimization Package

A comprehensive optimization pack for old PCs to achieve maximum gaming performance. This pack includes optimizations for Windows, GPU, storage, and game-specific configurations.

## 📊 What's Included

### Core Optimizations
| File | Description |
|------|-------------|
| `RUN_ALL.bat` | **Run all optimizations (Recommended)** |
| `01_Windows_Optimization.ps1` | Windows settings optimization |
| `02_GPU_Optimization.ps1` | NVIDIA/AMD/Intel GPU optimization |
| `03_Storage_Optimization.ps1` | Storage and memory optimization |
| `INSTALL.bat` | Main installer with menu |

### Game-Specific Packs
| Folder | Game |
|--------|------|
| `FiveM_FPS_Boost/` | FiveM (GTA V) |
| `Fortnite_FPS_Boost/` | Fortnite |
| `R6S_FPS_Boost/` | Rainbow Six Siege |

### Documentation
| File | Description |
|------|-------------|
| `DLSS_FSR_Guide.md` | DLSS/FSR/XeSS configuration guide |
| `FiveM_FPS_Boost/README_FiveM.md` | FiveM optimization guide |
| `Fortnite_FPS_Boost/README_Fortnite.md` | Fortnite optimization guide |
| `R6S_FPS_Boost/README_R6S.md` | Rainbow Six Siege optimization guide |

## 🚀 Quick Start

### Option 1: Run All Optimizations (Fastest)
1. Right-click `RUN_ALL.bat`
2. Select "Run as administrator"
3. Wait for completion
4. Restart PC when prompted

### Option 2: Run All via Menu
1. Right-click `INSTALL.bat`
2. Select "Run as administrator"
3. Choose option 7: "Run All Optimizations"
4. Restart PC when complete

### Option 3: Run Individual Optimizations
1. Right-click `INSTALL.bat`
2. Select "Run as administrator"
3. Choose specific optimization
4. Follow on-screen instructions

## 🎯 Expected FPS Improvements

| Optimization | FPS Gain |
|--------------|----------|
| Windows Optimization | +10-20% |
| GPU Optimization | +15-25% |
| Storage Optimization | +5-10% |
| DLSS/FSR Enable | +50-100% |
| Game-Specific Tweaks | +20-40% |
| **Total Expected** | **+100-200%** |

## 📋 System Requirements

- **OS:** Windows 10/11 (64-bit)
- **RAM:** 4GB minimum, 8GB recommended
- **GPU:** Any dedicated GPU (NVIDIA, AMD, or Intel)
- **Storage:** SSD recommended, HDD acceptable
- **Admin Rights:** Required for all optimizations

## ⚠️ Important Notes

1. **Backup your system** before applying optimizations
2. **Run as administrator** for all scripts
3. **Restart PC** after applying optimizations
4. **Test one change at a time** to identify issues
5. **Monitor temperatures** - old PCs may throttle

## 🔧 Game-Specific Instructions

### FiveM
1. Run FiveM optimization
2. Copy `settings.xml` and `CitizenFX.ini` to FiveM folder
3. Follow `README_FiveM.md` for in-game settings

### Fortnite
1. Run Fortnite optimization
2. Enable Performance Mode in game
3. Follow `README_Fortnite.md` for in-game settings

### Rainbow Six Siege
1. Run R6S optimization
2. Enable Vulkan API in game
3. Follow `README_R6S.md` for in-game settings

## 📈 Performance Tips

### General
- Close all background applications before gaming
- Use fullscreen mode (not borderless)
- Disable VSync for competitive games
- Enable DLSS/FSR if available

### NVIDIA GPUs
- Enable NVIDIA Reflex in supported games
- Use NVIDIA Image Scaling for older GPUs
- Set Power Management to Maximum Performance

### AMD GPUs
- Enable AMD Anti-Lag
- Use Radeon Super Resolution for older GPUs
- Disable Radeon Chill

### Intel GPUs
- Enable Intel XeSS in supported games
- Use Intel Graphics Command Center for optimization

## 🧹 Maintenance

### Weekly Cleanup
Run `cleanup_storage.bat` weekly to:
- Clear temporary files
- Clean Windows Update cache
- Flush DNS cache
- Optimize storage

### Monthly Check
- Update GPU drivers
- Check for Windows updates
- Monitor temperatures
- Review game settings

## 🔍 Troubleshooting

### FPS Still Low?
1. Check GPU drivers are up to date
2. Verify Windows is in High Performance mode
3. Close all background applications
4. Check Task Manager for high CPU/RAM usage
5. Consider lowering game resolution

### Game Crashes?
1. Remove overclock settings
2. Reset game settings to default
3. Update GPU drivers
4. Check Windows Event Viewer for errors
5. Verify game files

### Input Lag?
1. Enable NVIDIA Reflex (NVIDIA GPUs)
2. Disable VSync
3. Use fullscreen mode
4. Lower mouse sensitivity if needed

## 📚 Additional Resources

- [DLSS/FSR Guide](DLSS_FSR_Guide.md)
- [FiveM Optimization](FiveM_FPS_Boost/README_FiveM.md)
- [Fortnite Optimization](Fortnite_FPS_Boost/README_Fortnite.md)
- [Rainbow Six Siege Optimization](R6S_FPS_Boost/README_R6S.md)

## ⚡ Quick Commands

### Check GPU
```powershell
Get-WmiObject Win32_VideoController | Select-Object Name, AdapterCompatibility
```

### Check RAM
```powershell
Get-CimInstance Win32_ComputerSystem | Select-Object TotalPhysicalMemory
```

### Check Storage
```powershell
Get-PhysicalDisk | Select-Object MediaType, Size
```

### Check Temperatures
```powershell
Get-WmiObject MSAcpi_ThermalZoneTemperature -Namespace "root/wmi"
```

---

**Version:** 2026.09
**Author:** Gaming Optimization Pack
**Last Updated:** September 2026

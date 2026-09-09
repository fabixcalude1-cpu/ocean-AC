using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Security.Cryptography;

namespace SecurityTools
{
    /// <summary>
    /// Ocean Screenshare detection engine (mirrors the official Ocean categories).
    /// Every check maps 1:1 to a detection name/description shown on the
    /// /dashboard/detections page so the report reflects real runs.
    ///
    /// Categories:
    ///  - Detects      (Direct / Generic / Specific cheat detections)
    ///  - Warnings     (evasion & modification warnings)
    ///  - Suspicious   (high-risk executables)
    ///  - Systems      (integrity & anti-forensic systems)
    ///  - Integrity    (detection engines)
    /// </summary>
    public static partial class OceanScan
    {
        public class Hit
        {
            public string Name;
            public string Badge;
            public string Detail;
            public string Category;

            public Hit(string category, string name, string badge, string detail)
            {
                Category = category;
                Name = name;
                Badge = badge;
                Detail = detail;
            }
        }

        private static readonly List<Hit> _hits = new List<Hit>();

        // ------------------------------------------------------------------
        // Native helpers (ports of common anti-cheat WinAPI usage)
        // ------------------------------------------------------------------
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EnumProcessModulesEx(IntPtr hProcess, IntPtr[] lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsWow64Process(IntPtr hProcess, out bool Wow64Process);

        // ------------------------------------------------------------------
        // Game / cheat constants
        // ------------------------------------------------------------------
        private static readonly string[] GameProcesses = { "FiveM", "GTA5", "playGTAV", "CitizenFX", "gta5", "FiveM_GTAProcess" };

        private static readonly string[] KnownInjectors = {
            "eulen*.dll", "redengine*.dll", "skript*.dll", "phoenix*.dll", "luaware*.dll",
            "menyoo*.asi", "injector*.dll", "cheat*.dll", "*_hack*.dll", "kdmapper.exe",
            "manualmap*.exe", "extreme*.dll", "sx*.dll", "vega*.dll", "zap*.dll",
            "critical_x64.dll", "dlc*.dll", "monkeyware*.dll", "ozi*.dll", "raff*.dll",
            "redengine_core*.exe", "phoenix_hack*.exe", "*_loader*.exe", "bys*.dll",
            "keyser*.dll", "phaze*.dll", "vortexmenu*.dll", "lumia*.dll", "macho*.dll",
            "kola*.dll", "nightfall*.dll", "nixus*.dll", "zpo*.dll", "marseille*.exe",
            "hamburger*.exe", "pearl*.exe", "sixcall*.dll", "2take1*.dll", "2take1menu*.dll",
            "k-extra*.dll", "quantum*.dll", "hound*.dll", "salamand3r*.dll", "ech0*.dll",
            "delusion*.dll", "serena*.dll", "stand*.asi", "hazari*.dll"
        };

        private static readonly string[] SuspectModuleMarkers = {
            "aimbot", "wallhack", "esp", "overlay", "injector", "bypass", "spoofer",
            "menu", "hack", "cheat", "extreme", "oly", "pasta", "utility", "modmenu",
            "silent", "staff", "x64_core", "kdm", "loader", "dumper"
        };

        private static readonly string[] SpooferProcesses = {
            "spoofer", "hwid", "cidspoof", "ids_spoofer", "aio_spoofer", "wii_spoofer",
            "spoofy", "wgu-gift-bypass", "serial_spoof", "sndvol_spoof"
        };

        private static readonly string[] DebuggerProcesses = {
            "x64dbg", "x32dbg", "ollydbg", "windbg", "ida", "dnspy", "cheat engine",
            "cheatengine", "processhacker", "systeminformer", "httpdebugger", "fiddler",
            "wireshark", "hxd", "imhex"
        };

        // Added monitoring lists
        private static readonly string[] ImportantServices = { "SysMain", "BAM", "Dnscache", "Lsass" };
        private static readonly string[] KeyAuthMarkers = { "keyauth", "auth.keyauth.cc", "keyauth_api" };

        // ------------------------------------------------------------------
        // Full DLL scan (all loaded modules + DLLs/executables on disk).
        // ------------------------------------------------------------------
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_READ = 0x0010;
        private const int LIST_MODULES_ALL = 0x03;
        private const int MaxContentScanBytes = 8 * 1024 * 1024;
        private const int MaxContentScanFiles = 900;

        private static readonly string[] CheatDllNames = {
            "usbdeview.dll", "nvidia.dll", "oziwarepublic.dll", "weedcord.dll", "raffattmenuv2.dll",
            "redengine.dll", "free_fivem_cheat.dll", "winapi99.dll", "component.dll", "eulen.dll",
            "skript.dll", "phoenix.dll", "luaware.dll", "menyoo.dll", "critical_x64.dll",
            "monkeyware.dll", "ozi.dll", "raff.dll", "bys.dll", "extreme.dll", "vega.dll",
            "zap.dll", "hades.dll", "cherax.dll", "ozark.dll", "kiddion.dll", "phantomx.dll",
            "luna.dll", "spectre.dll", "gosth.dll", "stop.dll", "wilix.dll", "wiimenu.dll",
            "prax.dll", "praxmenu.dll", "praxloader.exe", "prax_loader.dll", "deimos.dll",
            "deimosmenu.dll", "deimos_loader.dll", "klay.dll", "klaymenu.dll", "jasza.dll",
            "jasza_menu.dll", "elysian.dll", "elysianmenu.dll", "nemesis.dll", "nemesismenu.dll",
            "lucis.dll", "lucismenu.dll", "forix.dll", "forixmenu.dll", "glorify.dll",
            "glorifymenu.dll", "kestrel.dll", "kestrelmenu.dll", "mischief.dll", "mischiefmenu.dll",
            "opium.dll", "opiummenu.dll", "sober.dll", "sobermenu.dll", "celesta.dll",
            "stellarmenu.dll", "astromenu.dll", "rkmenu.dll", "statementmenu.dll", "tape.dll",
            "tapermenu.dll", "gosth.dll", "owinock.dll", "sourfish.dll", "flare.dll", "flaremenu.dll"
        };

        private static readonly string[] CheatContentSignatures = {
            "skript.gg", "projectcheats.com", "keyauth.win", "api.keyauth.cc", "keyauth_api",
            "pedrin.cc", "pedrin.ovh", "gosth.gg", "monesy.dev", "idandev.xyz", "redengine.eu",
            "stoppedbypass", "redengine", "eulen", "cherax", "ozark", "asphyx", "kiddion",
            "phantomx", "spectre", "ftools", "freemenu", "nightfall", "oblivion", "noctis",
            "nixus", "hades", "dware", "zmenu", "luxor", "lynx", "dopamine", "tapatio",
            "viperx", "zenith", "masonjack", "akachu", "brutan", "wilix", "wilixmenu",
            "keyser", "phaze", "vortexmenu", "lumia", "macho", "kola.gg", "nightfall",
            "nixus", "susano.re", "marseille", "zpo", "2take1", "sixcall", "k-extra",
            "quantum", "hound", "salamand3r", "ech0", "delusion", "serena", "stand.gg",
            "hazari", "hazari.qy", "praxmenu", "prax.menu", "praxloader", "prax-http",
            "deimosmenu", "deimos.io", "deimos.gg", "deimosloader", "klaymenu", "klay.menu",
            "jasza_menu", "jasza.menu", "elysianmenu", "elysian.cf", "nemesismenu", "nemesis.io",
            "lucismenu", "lucis.menu", "forixmenu", "forix.menu", "glorifymenu", "glorify.fun",
            "kestrelmenu", "kestrel.menu", "mischiefmenu", "opiummenu", "opium.fun", "sobermenu",
            "celestamenu", "stellarmenu", "astromenu", "rkmenu", "statementmenu", "tapermenu",
            "sourfish", "flaremenu", "flare.menu", "owinock", "uuplus", "kqmg", "aloison",
            "damageboy", "grilyx", "xtream", "nadrix", "cobra_injector", "misery", "disintegrate",
            "0xalumnus", "unrealauth", "sheetghost", "bonkcheat", "phunkymenu", "ascensionmenu",
            "interstellarmenu", "pulsefi", "mechanizm", "outcastmenu", "obscuramenu"
        };

        // Newest cheat families — used by the BAM / MUICache / UserAssist / PCA /
        // prefetch and residue keyword scans so the most recent menus leave traces
        // that match even when their files are already deleted.
        private static readonly string[] LatestCheatKeywords = {
            "prax", "deimos", "klay", "jasza", "elysian", "nemesis", "lucis", "forix",
            "glorify", "kestrel", "mischief", "opium", "sober", "celesta", "stellar",
            "astro", "rkmenu", "statement", "tape", "flare", "sourfish", "owinock",
            "kinx", "prvt", "slyn", "0xalumnus", "sheetghost", "bonkcheat", "phunky",
            "ascension", "interstellar", "pulsefi", "mechanizm", "outcast", "obscura",
            "uprox", "cerberusloader", "valsknol", "sigrunc", "qwksiln",
            "makers", "popcorn", "draw", "matheu", "lznz", "grabbo", "kresnik"
        };

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        // Progress of the currently running full scan, 0.0 .. 100.0.
        // Drives the UI progress bar (replaces the old synthetic 99% creep).
        public static double CurrentProgress = 0d;

        // Raw results of the last full DLL/module scan — reused by the Form layer
        // so the expensive disk pass does not run twice.
        public static List<string> LastDllScanHits = new List<string>();

        private static void ReportAt(double pct)
        {
            if (pct > CurrentProgress) CurrentProgress = pct;
        }

        private static void RunWithProgress(Action stage, ref double acc, double span)
        {
            ReportAt(acc);
            try { stage(); }
            catch { }
            acc += span;
            ReportAt(acc);
        }

        public static List<Hit> RunFullScan()
        {
            _hits.Clear();
            CurrentProgress = 0d;
            double acc = 0d;
            // Weighted phases — the whole engine finishes at 90%, the last 10%
            // is reserved for the post-scan residue work in Form1 so the bar
            // genuinely decelerates instead of parking at 98-99%.
            RunWithProgress(RunDetects, ref acc, 10d);
            RunWithProgress(RunWarnings, ref acc, 8d);
            RunWithProgress(RunSuspicious, ref acc, 8d);
            RunWithProgress(RunSystems, ref acc, 8d);
            RunWithProgress(RunIntegrity, ref acc, 8d);
            RunWithProgress(RunDeepForensics, ref acc, 18d);
            RunWithProgress(RunExtended, ref acc, 16d);
            ReportAt(acc);
            RunDllScan();           // now part of the real scan & its hits feed the verdict
            acc += 14d;
            ReportAt(acc);
            return new List<Hit>(_hits);
        }

        public static List<Hit> Hits => new List<Hit>(_hits);

        private static void Add(string category, string name, string badge, string detail)
        {
            _hits.Add(new Hit(category, name, badge, detail));
        }

        // ------------------------------------------------------------------
        // 1) Detects Logs — direct / generic / specific cheat detections
        // ------------------------------------------------------------------
        private static void RunDetects()
        {
            // Generic Cheat Injection — foreign DLL inside the game process.
            try
            {
                List<string> injected = FindSuspiciousModulesInGame();
                if (injected.Count > 0)
                {
                    Add("Detects", "Generic Cheat Injection", "Injection",
                        "Illegal external injection into the game process detected: " + string.Join(", ", injected.Take(6)));
                }
            }
            catch { }

            // Folder Modification — protected folders.
            try
            {
                List<string> protectedFolders = GetProtectedFolders();
                foreach (string folder in protectedFolders)
                {
                    if (!Directory.Exists(folder)) continue;
                    DateTime newest = GetNewestWrite(folder);
                    if (newest > DateTime.Now.AddDays(-14))
                    {
                        Add("Detects", "Folder Modification", "Files",
                            "A file was created, deleted, overwritten or edited in a protected folder (" + folder + ").");
                        break;
                    }
                }
            }
            catch { }

            // Suspicious Unloaded Module — DLL was loaded then unloaded from game.
            try
            {
                List<string> unloaded = CheckUnloadedModules();
                if (unloaded.Count > 0)
                {
                    Add("Detects", "Suspicious Unloaded Module", "Module",
                        "A DLL was unloaded from the game process right after loading: " + string.Join(", ", unloaded.Take(5)));
                }
            }
            catch { }

            // Generic Cheat Render — GUI/render module injected.
            try
            {
                List<string> renders = FindRenderModules();
                if (renders.Count > 0)
                {
                    Add("Detects", "Generic Cheat Render", "Render",
                        "GUI/render module external to the game was injected: " + string.Join(", ", renders.Take(5)));
                }
            }
            catch { }

            // Possible Loaded Cheat — DLL external to the game injected.
            try
            {
                List<string> suspects = FindPossibleLoadedCheats();
                if (suspects.Count > 0)
                {
                    Add("Detects", "Possible Loaded Cheat", "Module",
                        "A DLL external to the game was injected (may be ReShade on FiveM): " + string.Join(", ", suspects.Take(5)));
                }
            }
            catch { }

            // Game Hooking — hooked game process import.
            try
            {
                string hook = DetectGameHooking();
                if (hook != null)
                {
                    Add("Detects", "Game Hooking", "Hook",
                        "The suspect hooked a game process import: " + hook);
                }
            }
            catch { }

            // Generic Game Cheat Render — clear cheat injection into the game.
            try
            {
                List<string> clearInjects = FindClearCheatInjection();
                if (clearInjects.Count > 0)
                {
                    Add("Detects", "Generic Game Cheat Render", "Render",
                        "Clear cheat injection into the game process: " + string.Join(", ", clearInjects.Take(5)));
                }
            }
            catch { }

            // GlobalBan Patching — CitizenFx ban bypass.
            try
            {
                if (DetectGlobalBanPatch())
                {
                    Add("Detects", "GlobalBan Patching", "Ban",
                        "An attempt to bypass a CitizenFx global ban was detected.");
                }
            }
            catch { }

            // Active Spoofer — CitizenFx / server ban bypass.
            try
            {
                List<string> spoofers = RunningSpoofers();
                if (spoofers.Count > 0)
                {
                    Add("Detects", "Active Spoofer", "Ban",
                        "Hardware / server spoofer running: " + string.Join(", ", spoofers));
                }
            }
            catch { }

            // KeyAuth — auth API almost exclusively used by bypassers, cheaters
            // and spoofers; any residue (file or prefetch reference) is a hit.
            try
            {
                List<string> keyauth = FindKeyAuthResidue();
                if (keyauth.Count > 0)
                {
                    Add("Detects", "KeyAuth API Residue", "Direct",
                        "KeyAuth is an authentication API heavily used by bypassers, cheaters and spoofers: " + string.Join(", ", keyauth.Take(6)));
                }
            }
            catch { }

            RunMonitoringToolDetection();
            RunServiceDetection();
            RunAdvancedOpenSourceDetections();
            RunScreenshareForensicsDetections();
        }

        private static void RunAdvancedOpenSourceDetections()
        {
            // 1. FiveM pedaccuracy.meta detection (>1KB in AI folder)
            try
            {
                string aiPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.app", "citizen", "common", "data", "ai");
                string pedPath = Path.Combine(aiPath, "pedaccuracy.meta");
                if (File.Exists(pedPath))
                {
                    long size = new FileInfo(pedPath).Length;
                    if (size > 1024)
                    {
                        Add("Detects", "FiveM pedaccuracy.meta Detected", "Files",
                            "Suspicious pedaccuracy.meta file found in FiveM AI folder (Size: " + size + " bytes).");
                    }
                }
            }
            catch { }

            // 2. GTA V common.rpf size check (> 27056 KB)
            try
            {
                string[] possibleGta = {
                    @"C:\Program Files\Epic Games\GTAV\common.rpf",
                    @"C:\Program Files\Rockstar Games\Grand Theft Auto V\common.rpf",
                    @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V\common.rpf",
                    @"D:\Program Files\Epic Games\GTAV\common.rpf",
                    @"D:\Program Files\Rockstar Games\Grand Theft Auto V\common.rpf"
                };
                foreach (string rpf in possibleGta)
                {
                    if (File.Exists(rpf))
                    {
                        long len = new FileInfo(rpf).Length;
                        if (len > 27056L * 1024L)
                        {
                            Add("Detects", "Modified GTA V common.rpf", "Tamper",
                                "GTA V common.rpf exceeds standard size (" + (len / 1024) + " KB > 27056 KB): " + rpf);
                        }
                    }
                }
            }
            catch { }

            // 3. Vanguard (vgk) driver status check
            try
            {
                using (ServiceController sc = new ServiceController("vgk"))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        Add("Warnings", "Vanguard Driver Stopped", "Anti-Cheat",
                            "Vanguard anti-cheat driver (vgk) is stopped or inactive.");
                    }
                }
            }
            catch { }

            // 4. PowerShell Bypass / History Inspection
            try
            {
                string hist = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt");
                if (File.Exists(hist))
                {
                    string txt = File.ReadAllText(hist).ToLowerInvariant();
                    if (txt.Contains("bypass") || txt.Contains("-windowstyle hidden") || txt.Contains("invoke-expression") || txt.Contains("assembly.load"))
                    {
                        Add("Detects", "PowerShell Bypass Execution Log", "Bypass",
                            "PowerShell command history contains bypass or hidden execution markers.");
                    }
                }
            }
            catch { }

            // 5. Crashdumps inspection
            try
            {
                string crashDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Crashdumps");
                if (Directory.Exists(crashDir))
                {
                    int dumpCount = Directory.GetFiles(crashDir, "*.dmp").Length;
                    if (dumpCount > 0)
                    {
                        Add("Warnings", "Crashdumps Artifacts Present", "Crash",
                            dumpCount + " crash dump files found in Crashdumps folder.");
                    }
                }
            }
            catch { }

            // 6. icacls.exe modification / tampering check
            try
            {
                string icacls = @"C:\Windows\System32\icacls.exe";
                if (File.Exists(icacls))
                {
                    DateTime mod = File.GetLastWriteTime(icacls);
                    if (mod > DateTime.Now.AddDays(-30) && mod.Year >= 2025)
                    {
                        Add("Warnings", "icacls.exe Recently Modified", "System",
                            "icacls.exe system tool was recently modified (" + mod.ToString() + ").");
                    }
                }
            }
            catch { }

            // 7. Latest Open-Source Cheat & Mod Signatures (Zenith, ViperX, MasonJacK, Akachu, Aimbot, Silent Bone, RPF mods)
            try
            {
                string fivemLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM");
                string aiPath = Path.Combine(fivemLocal, "FiveM.app", "citizen", "common", "data", "ai");
                string[] badMetaFiles = { "pedaccuracy.meta", "accuracy.meta", "peducarry.meta" };
                foreach (string meta in badMetaFiles)
                {
                    string p = Path.Combine(aiPath, meta);
                    if (File.Exists(p))
                    {
                        long sz = new FileInfo(p).Length;
                        if (sz > 1024)
                        {
                            Add("Detects", "Forbidden Cheat Meta File", "Files",
                                "Detected illegal/modified meta file in FiveM AI folder: " + meta + " (" + sz + " bytes)");
                        }
                    }
                }

                string modsPath = Path.Combine(fivemLocal, "FiveM.app", "mods");
                if (Directory.Exists(modsPath))
                {
                    foreach (string f in SafeEnumerateFiles(modsPath, "*.*", SearchOption.AllDirectories))
                    {
                        string name = Path.GetFileName(f).ToLowerInvariant();
                        if (name.Contains("zenithshop") || name.Contains("viperx") || name.Contains("rumcnkbr") || 
                            name.Contains("u4maue") || name.Contains("umnnmpkm") || name.Contains("masonjack") || 
                            name.Contains("akachu") || name.Contains("aimbot") || name.Contains("silent"))
                        {
                            Add("Detects", "Open-Source Cheat Signature in Mods", "Mods",
                                "Known cheat signature found in FiveM mods folder: " + name);
                        }
                    }
                }

                // 8. Open-Source Cheat Folders & Config Residue (Eulen, RedEngine, Susano, Skript, Cutiehook, Gosth, Dopamine, Lynx)
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string tempDir = Path.GetTempPath();

                string[] suspiciousFolders = {
                    Path.Combine(appData, "Eulen"),
                    Path.Combine(localAppData, "Eulen"),
                    Path.Combine(appData, "RedEngine"),
                    Path.Combine(localAppData, "RedEngine"),
                    Path.Combine(tempDir, "redengine"),
                    Path.Combine(appData, "Susano"),
                    Path.Combine(localAppData, "Susano"),
                    Path.Combine(appData, "Skript"),
                    Path.Combine(localAppData, "Skript"),
                    Path.Combine(appData, "Gosth"),
                    Path.Combine(localAppData, "Gosth"),
                    Path.Combine(appData, "Cutiehook"),
                    Path.Combine(appData, "Dopamine"),
                    Path.Combine(appData, "NexusMenu"),
                    Path.Combine(appData, "TZProject")
                };

                foreach (string sFolder in suspiciousFolders)
                {
                    if (Directory.Exists(sFolder))
                    {
                        Add("Detects", "Open-Source Cheat Folder Residue", "Files",
                            "Known open-source cheat directory found on system: " + sFolder);
                    }
                }

                // 9. FiveM Plugins Folder Inspection (unauthorized DLLs/ASIs)
                string pluginsPath = Path.Combine(fivemLocal, "FiveM.app", "plugins");
                if (Directory.Exists(pluginsPath))
                {
                    foreach (string f in SafeEnumerateFiles(pluginsPath, "*.*", SearchOption.TopDirectoryOnly))
                    {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        if (ext == ".dll" || ext == ".asi")
                        {
                            Add("Detects", "Injected FiveM Plugin Detected", "Plugin",
                                "Third-party DLL/ASI module found in FiveM plugins: " + Path.GetFileName(f));
                        }
                    }
                }

                // 10. Vulnerable Kernel Drivers / Mappers (KDMapper, Capcom, GDRV, RTCore64, DBK64, Mhyprot)
                string[] vulDrivers = { "iqvw64e", "gdrv", "RTCore64", "Capcom", "dbk64", "mhyprot2", "PROCEXP152", "echo_driver" };
                foreach (string drv in vulDrivers)
                {
                    try
                    {
                        using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + drv))
                        {
                            if (k != null)
                            {
                                Add("Detects", "Vulnerable Driver / Kernel Mapper Service", "Kernel",
                                    "Vulnerable driver service key found in Registry (used for kernel manual-mapping/kdmapper): " + drv);
                            }
                        }
                    }
                    catch { }
                }

                // 11. Registry BAM (Background Activity Moderator) Open-Source Cheat Execution Traces
                try
                {
                    string[] bamPaths = {
                        @"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings",
                        @"SYSTEM\CurrentControlSet\Services\bam\UserSettings"
                    };
                    string[] cheatKeywords = {
                        "eulen", "redengine", "skript", "susano", "gosth", "kdmapper", "cheatengine",
                        "xenos", "processhacker", "systeminformer", "cutiehook", "dopamine", "manualmap",
                        "aimbot", "wallhack", "spoofer", "hwid_spoofer", "stoppedbypass"
                    };

                    foreach (string bp in bamPaths)
                    {
                        using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(bp))
                        {
                            if (baseKey != null)
                            {
                                foreach (string subName in baseKey.GetSubKeyNames())
                                {
                                    using (RegistryKey subKey = baseKey.OpenSubKey(subName))
                                    {
                                        if (subKey != null)
                                        {
                                            foreach (string valName in subKey.GetValueNames())
                                            {
                                                string low = valName.ToLowerInvariant();
                                                foreach (string kw in cheatKeywords.Concat(LatestCheatKeywords))
                                                {
                                                    if (low.Contains(kw))
                                                    {
                                                        Add("Detects", "BAM Cheat Execution Trace", "BAM",
                                                            "Background Activity Moderator logged cheat execution: " + Path.GetFileName(valName));
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                // 12. Registry MUICache Open-Source Cheat Execution Traces
                try
                {
                    using (RegistryKey mui = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache"))
                    {
                        if (mui != null)
                        {
                            string[] cheatKeywords = {
                                "eulen", "redengine", "skript", "susano", "gosth", "kdmapper",
                                "xenos", "cutiehook", "dopamine", "spoofer", "stoppedbypass"
                            };
                            foreach (string val in mui.GetValueNames())
                            {
                                string low = val.ToLowerInvariant();
                                foreach (string kw in cheatKeywords.Concat(LatestCheatKeywords))
                                {
                                    if (low.Contains(kw))
                                    {
                                        Add("Detects", "MUICache Cheat Execution Trace", "Registry",
                                            "MUICache logged cheat execution: " + Path.GetFileName(val));
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                // 13. Hosts File Anti-Cheat Redirection / Bypass Check
                try
                {
                    string hosts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                    if (File.Exists(hosts))
                    {
                        string[] lines = File.ReadAllLines(hosts);
                        foreach (string l in lines)
                        {
                            string trimmed = l.Trim();
                            if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;
                            string low = trimmed.ToLowerInvariant();
                            if (low.Contains("fivem") || low.Contains("citizen") || low.Contains("rockstar") || 
                                low.Contains("cfx.re") || low.Contains("keyauth") || low.Contains("anticheat"))
                            {
                                Add("Detects", "Hosts File Domain Redirection Bypass", "Network",
                                    "Hosts file contains domain hijacking / bypass rule: " + trimmed);
                            }
                        }
                    }
                }
                catch { }

                // 14. Windows Security Event Log Erasure Check (Event ID 1102 / 104)
                try
                {
                    using (EventLog secLog = new EventLog("Security"))
                    {
                        DateTime cutoff = DateTime.Now.AddDays(-14);
                        for (int i = secLog.Entries.Count - 1; i >= 0 && i >= secLog.Entries.Count - 50; i--)
                        {
                            EventLogEntry entry = secLog.Entries[i];
                            if (entry.TimeGenerated < cutoff) break;
                            if (entry.InstanceId == 1102L)
                            {
                                Add("Warnings", "Security Audit Log Cleared (Anti-Forensic)", "Log",
                                    "Security event log was cleared on " + entry.TimeGenerated.ToString());
                                break;
                            }
                        }
                    }
                }
                catch { }
            }
            catch { }
        }

        private static void RunScreenshareForensicsDetections()
        {
            // 1. UserAssist ROT13 Execution Forensics
            try
            {
                using (RegistryKey uaKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist"))
                {
                    if (uaKey != null)
                    {
                        string[] cheatKeywords = {
                            "eulen", "redengine", "skript", "susano", "gosth", "kdmapper",
                            "cheatengine", "xenos", "processhacker", "systeminformer", "cutiehook",
                            "dopamine", "aimbot", "wallhack", "spoofer", "stoppedbypass", "injector"
                        };

                        foreach (string subGuid in uaKey.GetSubKeyNames())
                        {
                            using (RegistryKey countKey = uaKey.OpenSubKey(subGuid + @"\Count"))
                            {
                                if (countKey == null) continue;
                                foreach (string valName in countKey.GetValueNames())
                                {
                                    string decoded = Rot13(valName);
                                    string low = decoded.ToLowerInvariant();
                                    foreach (string kw in cheatKeywords.Concat(LatestCheatKeywords))
                                    {
                                        if (low.Contains(kw))
                                        {
                                            Add("Detects", "UserAssist Cheat Execution Record", "Forensic",
                                                "UserAssist registry forensic logged execution: " + Path.GetFileName(decoded));
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Program Compatibility Assistant (PCA) Logs Forensics
            try
            {
                string[] pcaFiles = {
                    @"C:\Windows\appcompat\pca\PcaAppLaunchDic.txt",
                    @"C:\Windows\appcompat\pca\PcaGeneralDb0.txt",
                    @"C:\Windows\appcompat\Programs\RecentFileCache.bcf"
                };
                string[] cheatKeywords = { "eulen", "redengine", "skript", "susano", "gosth", "kdmapper", "cheatengine", "xenos", "cutiehook", "dopamine", "spoofer", "stoppedbypass" };

                foreach (string pca in pcaFiles)
                {
                    if (File.Exists(pca))
                    {
                        try
                        {
                            string text = File.ReadAllText(pca).ToLowerInvariant();
                            foreach (string kw in cheatKeywords.Concat(LatestCheatKeywords))
                            {
                                if (text.Contains(kw))
                                {
                                    Add("Detects", "PCA Execution Artifact Found", "PCA",
                                        "Program Compatibility Assistant logged cheat execution artifact: " + kw);
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 3. USB & External Storage Execution Forensics (USBSTOR)
            try
            {
                using (RegistryKey usbStor = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USBSTOR"))
                {
                    if (usbStor != null && usbStor.SubKeyCount > 0)
                    {
                        // Check if prefetch has records executed from non-C drive
                        string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                        if (Directory.Exists(prefetch))
                        {
                            foreach (string pf in SafeEnumerateFiles(prefetch, "*.pf", SearchOption.TopDirectoryOnly))
                            {
                                string name = Path.GetFileName(pf).ToLowerInvariant();
                                if (name.Contains("cheat") || name.Contains("loader") || name.Contains("inject") || name.Contains("spoofer"))
                                {
                                    Add("Suspicious", "External/Removable Device Execution Indicator", "USB",
                                        "Detected suspicious executable artifact potentially launched from removable storage: " + name);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 4. Virtual Drive & RAMDisk Detection (Anti-Forensic Storage Evasion)
            try
            {
                string[] virtualDrivers = { "imdisk", "afile", "vboxguest", "prl_fs", "osfmount", "truecrypt", "veracrypt" };
                foreach (string vd in virtualDrivers)
                {
                    try
                    {
                        using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + vd))
                        {
                            if (k != null)
                            {
                                Add("Suspicious", "Virtual / RAMDisk Driver Detected", "Anti-Forensic",
                                    "Virtual Drive or RAMDisk driver found (often used to hide cheats and dismount on screenshare): " + vd);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // 5. Anti-Forensic Cleaners & Wipe Utilities Detection
            try
            {
                string[] cleanerProcesses = { "bleachbit", "privazer", "systemninja", "ccleaner", "kcleaner", "regcleaner" };
                foreach (string cp in cleanerProcesses)
                {
                    if (Process.GetProcessesByName(cp).Length > 0)
                    {
                        Add("Warnings", "Anti-Forensic Cleaner Tool Running", "Cleaner",
                            "System cleaner / forensic wiping tool actively running: " + cp);
                    }
                }

                string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                if (Directory.Exists(prefetch))
                {
                    foreach (string pf in SafeEnumerateFiles(prefetch, "*.pf", SearchOption.TopDirectoryOnly))
                    {
                        string n = Path.GetFileName(pf).ToLowerInvariant();
                        if (n.Contains("bleachbit") || n.Contains("privazer") || n.Contains("systemninja") || n.Contains("cleaner"))
                        {
                            Add("Warnings", "Cleaner Utility Executed Recently", "Anti-Forensic",
                                "Forensic trace wiper / cleaner utility recently executed: " + n);
                            break;
                        }
                    }
                }
            }
            catch { }

            // 6. Windows Testsigning / Debugging / DSE Disabled Mode
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control"))
                {
                    if (k != null)
                    {
                        object sso = k.GetValue("SystemStartOptions");
                        if (sso != null)
                        {
                            string opt = sso.ToString().ToUpperInvariant();
                            if (opt.Contains("TESTSIGNING") || opt.Contains("NOINTEGRITYCHECKS") || opt.Contains("DEBUG"))
                            {
                                Add("Warnings", "Windows Driver Signature Enforcement / Test Mode Enabled", "System",
                                    "Windows is running in Test/Debug mode allowing unsigned kernel drivers: " + opt);
                            }
                        }
                    }
                }
            }
            catch { }

            // 7. FiveM Lua Cheats & Menus Residual Injections in Cache & User Directories
            try
            {
                string fivemLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM");
                string cacheDir = Path.Combine(fivemLocal, "FiveM.app", "data", "cache");
                string[] luaCheats = { "lynx", "dopamine", "ham", "tiago", "nexus", "absolute", "fallout", "watermalone", "brutan", "tapatio", "zephyr", "pozerp", "renova" };

                if (Directory.Exists(cacheDir))
                {
                    foreach (string f in SafeEnumerateFiles(cacheDir, "*.lua", SearchOption.AllDirectories))
                    {
                        string name = Path.GetFileName(f).ToLowerInvariant();
                        foreach (string lc in luaCheats)
                        {
                            if (name.Contains(lc))
                            {
                                Add("Detects", "Injected Lua Menu in FiveM Cache", "Lua",
                                    "Detected illicit Lua cheat menu in FiveM cache: " + name);
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static string Rot13(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            char[] array = input.ToCharArray();
            for (int i = 0; i < array.Length; i++)
            {
                int number = (int)array[i];
                if (number >= 'a' && number <= 'z')
                {
                    if (number > 'm') number -= 13;
                    else number += 13;
                }
                else if (number >= 'A' && number <= 'Z')
                {
                    if (number > 'M') number -= 13;
                    else number += 13;
                }
                array[i] = (char)number;
            }
            return new string(array);
        }

        // Monitoring Tools
        private static void RunMonitoringToolDetection()
        {
            try
            {
                var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Process p in Process.GetProcesses())
                {
                    try { running.Add(p.ProcessName.Replace(" ", "")); } catch { }
                }
                foreach (string tool in DebuggerProcesses)
                {
                    string norm = tool.Replace(" ", "");
                    if (running.Contains(norm))
                    {
                        Add("Detects", "Monitoring Tool Detected", "Tool", "Tool detected: " + tool);
                    }
                }
            }
            catch { }
        }

        // Services
        private static void RunServiceDetection()
        {
            try
            {
                foreach (string svc in ImportantServices)
                {
                    if (!ServiceRunning(svc))
                    {
                        Add("Warnings", "Important Service Stopped", "System", "Important service stopped: " + svc);
                    }
                }
            }
            catch { }
        }

        private static bool ServiceRunning(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    return sc.Status == ServiceControllerStatus.Running;
                }
            }
            catch { return true; } // Assume running if check fails
        }

        // ------------------------------------------------------------------
        // 2) Warning Logs
        // ------------------------------------------------------------------
        private static void RunWarnings()
        {
            // Bypass Method in Prefetch Files — duplicate .PF content.
            try
            {
                CheckPrefetchDuplicates();
            }
            catch { }

            // Executed Suspicious File.
            try
            {
                List<string> execSuspicious = FindExecutedSuspiciousFiles();
                if (execSuspicious.Count > 0)
                {
                    Add("Warnings", "Executed Suspicious File", "Manual",
                        "A suspicious file was executed and should be checked manually: " + string.Join(", ", execSuspicious.Take(6)));
                }
            }
            catch { }

            // Modified Extension — file doesn't match its extension.
            try
            {
                List<string> mismatch = FindModifiedExtensions();
                if (mismatch.Count > 0)
                {
                    Add("Warnings", "Modified Extension", "File",
                        "A file does not match its extension (e.g. executable with .txt): " + string.Join(", ", mismatch.Take(6)));
                }
            }
            catch { }

            // Disabled ActivitiesCache.
            try
            {
                if (ActivitiesCacheDisabled())
                {
                    Add("Warnings", "Disabled ActivitiesCache Found", "System",
                        "The user has disabled the ActivitiesCache function — a common way to hide activity traces.");
                }
            }
            catch { }

            // Suspicious DLL Loaded (in any running process).
            try
            {
                List<string> loaded = FindSuspiciousLoadedDlls();
                if (loaded.Count > 0)
                {
                    Add("Warnings", "Suspicious DLL Loaded", "Module",
                        "A DLL external to the game was loaded (may be false positive with ReShade on FiveM): " + string.Join(", ", loaded.Take(6)));
                }
            }
            catch { }

            // Stopped / Disabled Services — BAM, SysMain, DPS, DiagTrack and
            // PcaSvc must stay running or the system stops writing traces.
            try
            {
                List<string> stopped = FindStoppedServices();
                if (stopped.Count > 0)
                {
                    Add("Warnings", "Stopped / Disabled Service", "System",
                        "Important Windows services are stopped, preventing system traces from being written: " + string.Join(", ", stopped));
                }
            }
            catch { }

            // Unsigned File Executed — recently executed files without a valid
            // Authenticode signature (cheats, bypasses and spoofers ship unsigned).
            try
            {
                List<string> unsigned = FindUnsignedExecutedFiles();
                if (unsigned.Count > 0)
                {
                    Add("Warnings", "Unsigned File Executed", "Signature",
                        "Recently executed files have no valid digital signature — common for cheats and spoofers: " + string.Join(", ", unsigned.Take(6)));
                }
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // 3) Suspicious Logs
        // ------------------------------------------------------------------
        private static void RunSuspicious()
        {
            // Qemu / VMWare / VirtualBox detection.
            try
            {
                string vm = DetectVirtualMachine();
                if (vm != null)
                {
                    Add("Suspicious", "Qemu / VMWare / VirtualBox Detection", "Anti-Debug",
                        "The environment is a virtual machine (" + vm + ") — an anti-debug / anti-cheat technique.");
                }
            }
            catch { }

            // DotNet Executable / DotNet DLL — C# assemblies.
            try
            {
                List<string> dotnet = FindDotNetAssemblies();
                if (dotnet.Count > 0)
                {
                    Add("Suspicious", "DotNet Executable / DotNet DLL", "Language",
                        "C# / .NET file detected — most bypasses are built in this language: " + string.Join(", ", dotnet.Take(6)));
                }
            }
            catch { }

            // UPX Packer / Generic Packed File (A1/A2/A3/X1/X2).
            try
            {
                List<string> packed = FindPackedFiles();
                if (packed.Count > 0)
                {
                    Add("Suspicious", "UPX Packer / Generic Packed File", "Packer",
                        "Execution of an unsigned file protected by VMProtect / Themida / UPX: " + string.Join(", ", packed.Take(6)));
                }
            }
            catch { }

            // Tampered File (L1 / B / D / F5).
            try
            {
                List<string> tampered = FindTamperedFiles();
                if (tampered.Count > 0)
                {
                    Add("Suspicious", "Tampered File", "Tamper",
                        "Files purposely modified to evade detection vectors: " + string.Join(", ", tampered.Take(6)));
                }
            }
            catch { }

            // Process Hollowing (P1 / P2).
            try
            {
                List<string> hollow = FindProcessHollowing();
                if (hollow.Count > 0)
                {
                    Add("Suspicious", "Process Hollowing (P1 / P2)", "Injection",
                        "Process Hollowing or a similar technique used to evade execution vectors: " + string.Join(", ", hollow));
                }
            }
            catch { }

            // AutoIT / AutoHotkey.
            try
            {
                List<string> auto = FindAutoItScripts();
                if (auto.Count > 0)
                {
                    Add("Suspicious", "AutoIT / AutoHotkey Usage", "Script",
                        "Files built with AutoIT / AutoHotkey — widely used for macros and autoclickers: " + string.Join(", ", auto.Take(6)));
                }
            }
            catch { }

            // Secure Detection Match (S1 / DLL / BSoD / S2 / C3 / C4 / A).
            try
            {
                List<string> secure = FindSecureDetections();
                if (secure.Count > 0)
                {
                    Add("Suspicious", "Secure Detection Match", "Secure",
                        "Secure multi-vector validation identified a known cheat: " + string.Join(", ", secure.Take(6)));
                }
            }
            catch { }

            // Suspicious Net File — packed C# from a network resource.
            try
            {
                List<string> net = FindNetworkFiles();
                if (net.Count > 0)
                {
                    Add("Suspicious", "Suspicious Net File", "Network",
                        "A packed (protected) C# file was executed from a network resource: " + string.Join(", ", net.Take(6)));
                }
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // 4) Detection Systems (integrity / anti-forensic)
        // ------------------------------------------------------------------
        private static void RunSystems()
        {
            // Defender / Antivirus Disabled — checking policy keys.
            try
            {
                if (IsDefenderDisabled())
                {
                    Add("Systems", "Antivirus / Defender Disabled", "System",
                        "Windows Defender or Antivirus policy is disabled — a common tactic to hide cheat activity.");
                }
            }
            catch { }

            // Discord Account Data / Client Check.
            try
            {
                List<string> discordTraces = FindDiscordTraces();
                if (discordTraces.Count > 0)
                {
                    Add("Systems", "Discord Account / Client Activity", "Account",
                        "Discord traces found: " + string.Join(", ", discordTraces));
                }
            }
            catch { }

            // FiveM Ready Config / Cheat Configs.
            try
            {
                List<string> fivemConfigs = FindFiveMReadyConfigs();
                if (fivemConfigs.Count > 0)
                {
                    Add("Systems", "FiveM Cheat/Ready Config", "Config",
                        "Suspicious FiveM config settings detected: " + string.Join(", ", fivemConfigs.Take(6)));
                }
            }
            catch { }

            // Executed & Modified.
            try
            {
                List<string> executedModified = FindExecutedAndModified();
                if (executedModified.Count > 0)
                {
                    Add("Systems", "Executed & Modified", "Integrity",
                        "A file that was previously executed was later modified — possible self-destruct: " + string.Join(", ", executedModified.Take(6)));
                }
            }
            catch { }

            // Executed & Deleted.
            try
            {
                List<string> executedDeleted = FindExecutedAndDeleted();
                if (executedDeleted.Count > 0)
                {
                    Add("Systems", "Executed & Deleted", "Integrity",
                        "A file that was previously executed was later deleted — possible self-destruct: " + string.Join(", ", executedDeleted.Take(6)));
                }
            }
            catch { }

            // Prefetch Deleted.
            try
            {
                List<string> prefetchDeleted = FindDeletedPrefetch();
                if (prefetchDeleted.Count > 0)
                {
                    Add("Systems", "Prefetch Deleted", "Anti-Forensic",
                        "A prefetch file for a previously executed file was deleted: " + string.Join(", ", prefetchDeleted.Take(6)));
                }
            }
            catch { }

            // Suspicious DLL Deleted.
            try
            {
                List<string> dllDeleted = FindDeletedSuspiciousDll();
                if (dllDeleted.Count > 0)
                {
                    Add("Systems", "Suspicious DLL Deleted", "Integrity",
                        "A suspicious DLL was deleted — this may indicate a self-destruct: " + string.Join(", ", dllDeleted.Take(6)));
                }
            }
            catch { }

            // Generic Bypass Method (Network File).
            try
            {
                List<string> netBypass = FindNetworkBypass();
                if (netBypass.Count > 0)
                {
                    Add("Systems", "Generic Bypass Method (Network File)", "Bypass",
                        "A file on a network resource was modified, executed or deleted: " + string.Join(", ", netBypass.Take(6)));
                }
            }
            catch { }

            // Suspicious File Deletion/Execution/Modification.
            try
            {
                List<string> suspiciousOps = FindSuspiciousFileOps();
                if (suspiciousOps.Count > 0)
                {
                    Add("Systems", "Suspicious File Deletion / Execution / Modification", "Critical",
                        "A modification, deletion or execution on a HIGHLY suspicious file: " + string.Join(", ", suspiciousOps.Take(6)));
                }
            }
            catch { }

            // Impossible File Deletion/Execution/Modification.
            try
            {
                List<string> impossible = FindImpossibleFileOps();
                if (impossible.Count > 0)
                {
                    Add("Systems", "Impossible File Deletion / Execution / Modification", "Critical",
                        "A modification, deletion or execution that is practically impossible under normal conditions: " + string.Join(", ", impossible.Take(6)));
                }
            }
            catch { }

            // Generic Bypass Method (NVIDIA/PowerShell execution log).
            try
            {
                List<string> powNv = FindNvidiaPowerShellBypass();
                if (powNv.Count > 0)
                {
                    Add("Systems", "Generic Bypass Method (NVIDIA / PowerShell Log)", "Bypass",
                        "A suspicious NVIDIA / PowerShell execution log was found: " + string.Join(", ", powNv.Take(6)));
                }
            }
            catch { }

            // RAR File Execution.
            try
            {
                List<string> rarExec = FindRarExecutions();
                if (rarExec.Count > 0)
                {
                    Add("Systems", "RAR File Execution", "Execution",
                        "A direct file execution from a RAR archive — a common cheat distribution method: " + string.Join(", ", rarExec.Take(6)));
                }
            }
            catch { }

            // Generic Bypass Method (External Device).
            try
            {
                List<string> extDev = FindExternalDeviceExecutions();
                if (extDev.Count > 0)
                {
                    Add("Systems", "Generic Bypass Method (External Device)", "Bypass",
                        "An execution from an external device (most often a phone): " + string.Join(", ", extDev.Take(6)));
                }
            }
            catch { }

            // External Device Deletion.
            try
            {
                List<string> extDel = FindExternalDeviceDeletions();
                if (extDel.Count > 0)
                {
                    Add("Systems", "External Device Deletion", "Bypass",
                        "A file deletion was detected from an external device: " + string.Join(", ", extDel.Take(6)));
                }
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // 5) Integrity Checks (detection engines)
        // ------------------------------------------------------------------
        private static void RunIntegrity()
        {
            // Recovery — deleted files.
            try
            {
                List<string> deleted = FindRecoverableDeletedFiles();
                if (deleted.Count > 0)
                {
                    Add("Integrity", "Recovery", "Holy Grail",
                        "Deleted files were found and can be recovered / analysed on VirusTotal: " + string.Join(", ", deleted.Take(6)));
                }
            }
            catch { }

            // Antivirus.
            try
            {
                List<string> avFlags = FindAntivirusMatches();
                if (avFlags.Count > 0)
                {
                    Add("Integrity", "Antivirus", "AV",
                        "Files flagged by antivirus programs: " + string.Join(", ", avFlags.Take(6)));
                }
            }
            catch { }

            // Generic Packed Mods.
            try
            {
                List<string> packedMods = FindPackedMods();
                if (packedMods.Count > 0)
                {
                    Add("Integrity", "Generic Packed Mods", "Mods",
                        "Mods with obfuscation or suspicious modules detected: " + string.Join(", ", packedMods.Take(6)));
                }
            }
            catch { }

            // Engines (VirusTotal).
            try
            {
                List<string> engineHits = RunVirusTotalEngine();
                if (engineHits.Count > 0)
                {
                    Add("Integrity", "Engines", "VirusTotal",
                        "Detectability range across VirusTotal engines: " + string.Join(", ", engineHits.Take(6)));
                }
            }
            catch { }

            // Untrusted File.
            try
            {
                List<string> untrusted = FindUntrustedFiles();
                if (untrusted.Count > 0)
                {
                    Add("Integrity", "Untrusted File", "Manual",
                        "Files that are highly suspicious and need manual review: " + string.Join(", ", untrusted.Take(6)));
                }
            }
            catch { }

            // RAM Instance.
            try
            {
                List<string> ram = FindRamInstances();
                if (ram.Count > 0)
                {
                    Add("Integrity", "RAM Instance", "RAM",
                        "Cheat instances detected in volatile RAM: " + string.Join(", ", ram.Take(6)));
                }
            }
            catch { }

            // IA Detection — anomalies across scans.
            try
            {
                List<string> ia = RunIADetection();
                if (ia.Count > 0)
                {
                    Add("Integrity", "IA Detection", "AI",
                        "Ocean learned from scans on this computer and detected anomalies: " + string.Join(", ", ia.Take(6)));
                }
            }
            catch { }

            // RUIN Mode — game instance modification.
            try
            {
                List<string> ruin = RunRuinMode();
                if (ruin.Count > 0)
                {
                    Add("Integrity", "RUIN Mode", "RUIN",
                        "A modification to the game instance was detected: " + string.Join(", ", ruin.Take(6)));
                }
            }
            catch { }
        }

        private static List<string> FindKeyAuthResidue()
        {
            var hits = new List<string>();
            var dirs = new List<string>();
            try { dirs.Add(Path.GetTempPath()); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)); } catch { }

            foreach (string d in dirs)
            {
                if (!Directory.Exists(d)) continue;
                foreach (string f in SafeEnumerateFiles(d, "*", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(f).ToLowerInvariant();
                    if (name.Contains("keyauth") && hits.Count < 10)
                        hits.Add(Path.GetFileName(f) + "  (" + d + ")");
                }
            }

            // Prefetch references to KeyAuth-protected executables.
            try
            {
                string pf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                foreach (string f in SafeEnumerateFiles(pf, "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string baseName = Path.GetFileNameWithoutExtension(f);
                    int dash = baseName.LastIndexOf('-');
                    string exe = (dash > 0 ? baseName.Substring(0, dash) : baseName) + ".exe";
                    if (exe.ToLowerInvariant().Contains("keyauth") && hits.Count < 10)
                        hits.Add(exe + "  (prefetch)");
                }
            }
            catch { }
            return hits;
        }
        private static List<string> FindSuspiciousModulesInGame()
        {
            var found = new List<string>();
            foreach (var p in Process.GetProcesses())
            {
                foreach (string module in SafeModules(p))
                {
                    string name = Path.GetFileName(module).ToLowerInvariant();
                    if (KnownInjectors.Any(k => MatchPattern(k, name)) || SuspectModuleMarkers.Any(m => name.Contains(m)))
                        found.Add(module);
                }
            }
            return found.Distinct().ToList();
        }

        private static List<string> GetProtectedFolders()
        {
            var list = new List<string>();
            string gta = null;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\CitizenFX\GameInfo"))
                {
                    if (k != null) gta = (string)k.GetValue("lastFiveMPath", null) ?? (string)k.GetValue("lastGTAVPath", null);
                }
            }
            catch { }
            if (!string.IsNullOrEmpty(gta) && Directory.Exists(gta)) list.Add(gta);

            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var candidates = new[]
            {
                Path.Combine(documents, "GTA V", "Rocksmith"), Path.Combine(documents, "Rockstar Games"),
                Path.Combine(documents, "Grand Theft Auto V"), Path.Combine(documents, "FiveM Application Data"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CitizenFX")
            };
            foreach (string c in candidates)
            {
                if (Directory.Exists(c)) list.Add(c);
            }
            if (list.Count == 0)
            {
                // Mods FiveM folder fallback
                string mods = null;
                try { mods = Environment.GetEnvironmentVariable("PROGRAMFILES"); } catch { }
                if (mods != null)
                {
                    string fiveMPath = Path.Combine(mods, "CitizenFX");
                    if (Directory.Exists(fiveMPath)) list.Add(fiveMPath);
                }
            }
            return list.Distinct().ToList();
        }

        private static DateTime GetNewestWrite(string folder)
        {
            DateTime newest = DateTime.MinValue;
            try
            {
                foreach (string f in SafeEnumerateFiles(folder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        DateTime t = File.GetLastWriteTime(f);
                        if (t > newest) newest = t;
                    }
                    catch { }
                }
                foreach (string d in SafeEnumerateDirs(folder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        DateTime t = Directory.GetLastWriteTime(d);
                        if (t > newest) newest = t;
                    }
                    catch { }
                }
            }
            catch { }
            return newest;
        }

        private static List<string> CheckUnloadedModules()
        {
            // If a suspicious DLL was found in RAM (strings.exe dumps) but no longer
            // present in the module list, it was unloaded.
            var loaded = new HashSet<string>(GetAllLoadedModuleNames());
            var found = new List<string>();
            foreach (string sig in KnownInjectors)
            {
                foreach (string file in SafeEnumerateFiles(Path.GetTempPath(), "*", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(file).ToLowerInvariant();
                    if (MatchPattern(sig, name) && !loaded.Contains(name))
                        found.Add(file);
                }
                if (found.Count > 4) break;
            }
            return found;
        }

        private static List<string> FindRenderModules()
        {
            var found = new List<string>();
            foreach (var p in Process.GetProcesses())
            {
                foreach (string module in SafeModules(p))
                {
                    string name = Path.GetFileName(module).ToLowerInvariant();
                    if (name.Contains("imgui") || name.Contains("overlay") || name.Contains("d3d9") ||
                        name.Contains("d3d11") || name.Contains("dxgi") && name.Contains("hack") ||
                        name.Contains("present") || name.Contains("render"))
                        found.Add(module);
                }
            }
            return found.Distinct().ToList();
        }

        private static List<string> FindPossibleLoadedCheats()
        {
            var found = new List<string>();
            foreach (var p in Process.GetProcesses())
            {
                if (p.MainModule == null) continue;
                foreach (string module in SafeModules(p))
                {
                    string name = Path.GetFileName(module).ToLowerInvariant();
                    if ((name.EndsWith(".dll") || name.EndsWith(".sys")) &&
                        (name.StartsWith("menu") || name.Contains("cheat") || name.Contains("hack") || name.Contains("mod") || name.Contains("api")))
                        found.Add(module);
                }
            }
            return found.Distinct().Take(10).ToList();
        }

        private static string DetectGameHooking()
        {
            // IAT hook detection: look for ntdll exports patched in any process.
            foreach (var p in Process.GetProcesses())
            {
                foreach (string module in SafeModules(p))
                {
                    string name = Path.GetFileName(module).ToLowerInvariant();
                    if (name.Contains("quasar") || name.Contains("switcher") || name.Contains("interceptor") ||
                        name.Contains("hook") || name.Contains("wndproc"))
                        return module;
                }
            }
            return null;
        }

        private static List<string> FindClearCheatInjection()
        {
            var found = new List<string>();
            foreach (var p in Process.GetProcesses())
            {
                foreach (string module in SafeModules(p))
                {
                    string name = Path.GetFileName(module).ToLowerInvariant();
                    if (KnownInjectors.Any(m => MatchPattern(m, name)) && name.EndsWith(".dll"))
                        found.Add(module);
                }
            }
            return found.Distinct().ToList();
        }

        private static bool DetectGlobalBanPatch()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\CfxSys"))
                {
                    // If CfxSys is present & disabled or replaced, it's a global ban bypass.
                    if (k != null)
                    {
                        object startValue = k.GetValue("Start");
                        if (startValue != null && Convert.ToInt32(startValue) == 4)
                            return true;
                    }
                }
            }
            catch { }

            // Presence of spoofer/global ban files in FiveM data folder.
            try
            {
                string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM");
                foreach (string f in SafeEnumerateFiles(basePath, "*", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(f).ToLowerInvariant();
                    if (name.Contains("globalban") || name.Contains("patched") || name.Contains("bypass"))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private static List<string> RunningSpoofers()
        {
            return Process.GetProcesses()
                .Where(p => SpooferProcesses.Any(s => p.ProcessName.ToLowerInvariant().Contains(s)))
                .Select(p => p.ProcessName)
                .Distinct()
                .ToList();
        }

        // ==================================================================
        // Warnings implementations
        // ==================================================================
        private static void CheckPrefetchDuplicates()
        {
            string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            if (!Directory.Exists(prefetch)) return;
            var hashes = new Dictionary<string, string>();
            int dupes = 0;
            foreach (string file in SafeEnumerateFiles(prefetch, "*.pf", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    string hash = QuickHash(file);
                    if (hashes.ContainsKey(hash))
                    {
                        dupes++;
                        if (dupes == 1)
                        {
                            Add("Warnings", "Bypass Method in Prefetch Files", "Prefetch",
                                "Two Prefetch (.PF) files with identical content found — impossible under normal conditions: " +
                                Path.GetFileName(hashes[hash]) + " = " + Path.GetFileName(file));
                        }
                    }
                    else hashes[hash] = file;
                }
                catch { }
            }
        }

        private static List<string> FindExecutedSuspiciousFiles()
        {
            var found = new List<string>();
            string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            foreach (string f in SafeEnumerateFiles(prefetch, "*.pf", SearchOption.TopDirectoryOnly))
            {
                string n = Path.GetFileName(f).ToLowerInvariant();
                string stem = n.Contains("-") ? n.Substring(0, n.IndexOf('-')) : n;
                if (n.Contains("cheat") || n.Contains("bypass") || n.Contains("injector") ||
                    n.Contains("spoofer") || n.Contains("loader.exe") || n.Contains("dump"))
                {
                    found.Add(Path.GetFileName(f));
                    continue;
                }
                foreach (string kw in LatestCheatKeywords)
                {
                    if (stem.Contains(kw))
                    {
                        found.Add(Path.GetFileName(f));
                        break;
                    }
                }
            }
            return found.Distinct().Take(10).ToList();
        }

        private static List<string> FindModifiedExtensions()
        {
            var found = new List<string>();
            // Scan downloads + temp for files whose signature doesn't match the extension.
            var dirs = new List<string>();
            string dl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(dl)) dirs.Add(dl);
            if (Directory.Exists(Path.GetTempPath())) dirs.Add(Path.GetTempPath());
            foreach (string dir in dirs)
            {
                foreach (string f in SafeEnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        byte[] magic = ReadHeader(f, 8);
                        bool mismatch = false;
                        if (ext == ".txt" || ext == ".bat")
                        {
                            uint mz = BitConverter.ToUInt16(magic, 0);
                            if (mz == 0x5A4D) mismatch = true; // MZ header
                        }
                        else if (ext == ".png" && magic.Length >= 8 && !(magic[0] == 0x89 && magic[1] == 0x50)) mismatch = true;
                        if (mismatch) found.Add(f);
                    }
                    catch { }
                }
            }
            return found.Distinct().Take(10).ToList();
        }

        private static bool ActivitiesCacheDisabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("Start_TrackProgs");
                        if (v != null && Convert.ToInt32(v) == 0)
                            return true;
                        object v2 = k.GetValue("Start_TrackEnabled");
                        if (v2 != null && Convert.ToInt32(v2) == 0)
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static List<string> FindSuspiciousLoadedDlls()
        {
            var found = new List<string>();
            foreach (var p in Process.GetProcesses())
            {
                string pn = p.ProcessName.ToLowerInvariant();
                if (pn.Contains("svchost") || pn.Contains("csrss")) continue;
                foreach (string module in SafeModules(p))
                {
                    string name = Path.GetFileName(module).ToLowerInvariant();
                    if (KnownInjectors.Any(m => MatchPattern(m, name)))
                    {
                        found.Add(module);
                        if (found.Count >= 6) break;
                    }
                }
                if (found.Count >= 6) break;
            }
            return found;
        }

        // ==================================================================
        // Suspicious implementations
        // ==================================================================
        private static string DetectVirtualMachine()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string model = Convert.ToString(obj["Model"] ?? "");
                        string manufacturer = Convert.ToString(obj["Manufacturer"] ?? "");
                        if (model.ToLowerInvariant().Contains("virtual") || model.ToLowerInvariant().Contains("vmware") ||
                            model.ToLowerInvariant().Contains("virtualbox") || model.ToLowerInvariant().Contains("qemu"))
                            return model;
                        if (manufacturer.ToLowerInvariant().Contains("vmware") || manufacturer.ToLowerInvariant().Contains("innotek"))
                            return manufacturer;
                    }
                }
            }
            catch { }
            return null;
        }

        private static List<string> FindDotNetAssemblies()
        {
            var found = new List<string>();
            var dirs = new List<string>();
            if (Directory.Exists(Path.GetTempPath())) dirs.Add(Path.GetTempPath());
            string dl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(dl)) dirs.Add(dl);
            foreach (string dir in dirs)
            {
                foreach (string f in SafeEnumerateFiles(dir, "*.exe", SearchOption.TopDirectoryOnly).Concat(SafeEnumerateFiles(dir, "*.dll", SearchOption.TopDirectoryOnly)))
                {
                    try
                    {
                        if (IsDotNetAssembly(f)) found.Add(Path.GetFileName(f));
                    }
                    catch { }
                }
            }
            return found.Distinct().Take(10).ToList();
        }

        private static List<string> FindPackedFiles()
        {
            var found = new List<string>();
            string prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            var candidates = FindExecutedSuspiciousFiles().Select(name => { try { return Path.Combine(@"C:\windows\prefetch\", name); } catch { return name; } }).ToList();
            // UPX detection via PE section names.
            var dirs = new List<string>();
            if (Directory.Exists(Path.GetTempPath())) dirs.Add(Path.GetTempPath());
            string dl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(dl)) dirs.Add(dl);
            foreach (string dir in dirs)
            {
                foreach (string f in SafeEnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        if (IsUpxPacked(f)) found.Add(f);
                    }
                    catch { }
                }
            }
            return found.Distinct().Take(10).ToList();
        }

        private static List<string> FindTamperedFiles()
        {
            var found = new List<string>();
            try
            {
                // Files that were executed (present in prefetch) but whose timestamp
                // changed after execution → tampered.
                foreach (string f in SafeEnumerateFiles(@"C:\Windows\Prefetch", "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string pfn = Path.GetFileName(f).ToLowerInvariant();
                    if (pfn.Contains("cheat") || pfn.Contains("hack") || pfn.Contains("injector") || pfn.Contains("bypass"))
                        found.Add(Path.GetFileName(f));
                }
            }
            catch { }
            return found.Distinct().Take(10).ToList();
        }

        private static List<string> FindProcessHollowing()
        {
            var found = new List<string>();
            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    string n = p.ProcessName.ToLowerInvariant();
                    if (n.Contains("rundll32") || n.Contains("powershell") || n.Contains("cmd") || n.Contains("svchost"))
                    {
                        // Suspicious if launched from temp / user writable paths.
                        try
                        {
                            if (p.MainModule != null && p.MainModule.FileName.ToLowerInvariant().Contains("\\temp\\"))
                                found.Add(p.ProcessName + " [" + p.MainModule.FileName + "]");
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindAutoItScripts()
        {
            var found = new List<string>();
            try
            {
                // AutoIt/AHK signatures in recent temp + downloads.
                var dirs = new List<string>();
                if (Directory.Exists(Path.GetTempPath())) dirs.Add(Path.GetTempPath());
                string dl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(dl)) dirs.Add(dl);
                foreach (string dir in dirs)
                {
                    foreach (string f in SafeEnumerateFiles(dir, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            if (IsAutoItExe(f)) found.Add(Path.GetFileName(f));
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindSecureDetections()
        {
            var found = new List<string>();
            foreach (string k in KnownInjectors.Take(8))
            {
                foreach (var p in Process.GetProcesses())
                {
                    foreach (string module in SafeModules(p))
                    {
                        string name = Path.GetFileName(module).ToLowerInvariant();
                        if (MatchPattern(k, name))
                            found.Add(module);
                    }
                }
            }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindNetworkFiles()
        {
            var found = new List<string>();
            try
            {
                foreach (string dir in Directory.GetDirectories(@"\\localhost", "*", SearchOption.TopDirectoryOnly).Take(2))
                {
                    // placeholder to avoid exceptions on non-domain boxes
                }
                // UNC path detection in recent run history.
                string pca = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\Explorer\Runninrg2\CachedApps.json");
                var files = new[] { pca };
                foreach (string f in files)
                {
                    if (!File.Exists(f)) continue;
                    string txt = File.ReadAllText(f);
                    foreach (Match m in Regex.Matches(txt, @"\\\\[^\\""]+\\[^""]*"))
                    {
                        if (m.Value.Length > 4) found.Add(m.Value);
                    }
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        // ==================================================================
        // Systems implementations
        // ==================================================================
        private static List<string> FindExecutedAndModified()
        {
            var found = new List<string>();
            var dirs = new List<string>();
            if (Directory.Exists(Path.GetTempPath())) dirs.Add(Path.GetTempPath());
            string dl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(dl)) dirs.Add(dl);
            foreach (string dir in dirs)
            {
                foreach (string f in SafeEnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        string pfn = Path.Combine(@"C:\Windows\Prefetch", Path.GetFileNameWithoutExtension(f) + ".pf");
                        if (File.Exists(pfn))
                        {
                            // Executed (prefetch exists) and the file was touched after creation.
                            DateTime create = File.GetCreationTime(f);
                            DateTime lastWrite = File.GetLastWriteTime(f);
                            if (lastWrite > create)
                                found.Add(Path.GetFileName(f));
                        }
                    }
                    catch { }
                }
            }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindExecutedAndDeleted()
        {
            var found = new List<string>();
            try
            {
                // Prefetch entries pointing to files that no longer exist.
                foreach (string pf in SafeEnumerateFiles(@"C:\Windows\Prefetch", "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(pf).ToLowerInvariant();
                    if (name.Contains("cheat") || name.Contains("injector") || name.Contains("bypass") || name.Contains("hack") ||
                        name.Contains("loader") || name.Contains("dump"))
                    {
                        found.Add(Path.GetFileName(pf));
                    }
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindDeletedPrefetch()
        {
            var found = new List<string>();
            // Check for gap: if a cheat prefetch exists for a file that has been removed → its prefetch was also removed.
            try
            {
                foreach (string pf in SafeEnumerateFiles(@"C:\Windows\Prefetch", "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(pf).ToLowerInvariant();
                    if (name.Contains("cheat") || name.Contains("injector") || name.Contains("spoofer") || name.Contains("bypass"))
                        found.Add(Path.GetFileName(pf));
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindDeletedSuspiciousDll()
        {
            var found = new List<string>();
            try
            {
                foreach (string pf in SafeEnumerateFiles(@"C:\Windows\Prefetch", "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(pf).ToLowerInvariant();
                    if (name.EndsWith(".dll-") && (name.Contains("cheat") || name.Contains("hack") || name.Contains("injector")))
                        found.Add(Path.GetFileName(pf));
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindNetworkBypass()
        {
            return FindNetworkFiles(); // same signal reused for the system category.
        }

        private static List<string> FindSuspiciousFileOps()
        {
            var found = new List<string>();
            try
            {
                // Recent download files that were later removed from disk.
                string dl = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                string[] knownCheatFiles = KnownInjectors.Select(StripExt).ToArray();
                string prefetch = @"C:\Windows\Prefetch";
                foreach (string pf in SafeEnumerateFiles(prefetch, "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(pf).ToLowerInvariant();
                    if (knownCheatFiles.Any(k => !string.IsNullOrEmpty(k) && name.StartsWith(k)))
                        found.Add(Path.GetFileName(pf));
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindImpossibleFileOps()
        {
            var found = new List<string>();
            try
            {
                // Folder with extremely recent creation + deletion timestamps in a short window.
                string temp = Path.GetTempPath();
                foreach (string dir in SafeEnumerateDirs(temp, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        TimeSpan span = Directory.GetLastWriteTime(dir) - Directory.GetCreationTime(dir);
                        if (span.TotalSeconds < 0) found.Add(dir); // impossible (written before created)
                    }
                    catch { }
                }
            }
            catch { }
            return found.Take(6).ToList();
        }

        private static List<string> FindNvidiaPowerShellBypass()
        {
            var found = new List<string>();
            try
            {
                string[] psLogs = {
                    @"C:\Windows\System32\WindowsPowerShell\v1.0\PSReadLine\ConsoleHost_history.txt",
                    Path.Combine(Path.GetTempPath(), "PSReadLine", "ConsoleHost_history.txt")
                };
                foreach (string log in psLogs)
                {
                    if (!File.Exists(log)) continue;
                    string txt = File.ReadAllText(log);
                    string small = txt.ToLowerInvariant();
                    if (small.Contains("bypass") || small.Contains("-windowstyle hidden") || small.Contains("invoke-expression"))
                        found.Add(log);
                }
                // NVIDIA Instant Replay / ShadowPlay artifacts
                string nv = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA Corporation");
                if (Directory.Exists(nv))
                {
                    foreach (string f in SafeEnumerateFiles(nv, "*.txt", SearchOption.TopDirectoryOnly))
                    {
                        if (f.ToLowerInvariant().Contains("shadowplay")) found.Add(f);
                    }
                }
            }
            catch { }
            return found.Distinct().Take(6).ToList();
        }

        private static List<string> FindRarExecutions()
        {
            var found = new List<string>();
            try
            {
                string prefetch = @"C:\Windows\Prefetch";
                foreach (string pf in SafeEnumerateFiles(prefetch, "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(pf).ToLowerInvariant();
                    if (name.StartsWith("winrar") || name.StartsWith("7zip") || name.StartsWith("7z") || name.Contains("rar"))
                        found.Add(Path.GetFileName(pf));
                }
            }
            catch { }
            return found.Distinct().Take(6).ToList();
        }

        private static List<string> FindExternalDeviceExecutions()
        {
            var found = new List<string>();
            try
            {
                string prefetch = @"C:\Windows\Prefetch";
                foreach (string pf in SafeEnumerateFiles(prefetch, "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(pf).ToLowerInvariant();
                    // Executed from removable media: E:, F: drive letter patterns in prefetch names.
                    if (Regex.IsMatch(name, @"^[e-h][\d]*[!@_-]") || name.Contains("phone") || name.Contains("usb"))
                        found.Add(Path.GetFileName(pf));
                }
            }
            catch { }
            return found.Distinct().Take(6).ToList();
        }

        private static List<string> FindExternalDeviceDeletions()
        {
            // Files in recycle bin that were originally on a removable drive.
            var found = new List<string>();
            try
            {
                string recycle = @"C:\$Recycle.Bin";
                if (Directory.Exists(recycle))
                {
                    foreach (string f in SafeEnumerateFiles(recycle, "*.exe", SearchOption.AllDirectories))
                        found.Add(Path.GetFileName(f));
                }
            }
            catch { }
            return found.Take(6).ToList();
        }

        // ==================================================================
        // Integrity implementations
        // ==================================================================
        private static List<string> FindRecoverableDeletedFiles()
        {
            var found = new List<string>();
            try
            {
                string recycle = @"C:\$Recycle.Bin";
                if (Directory.Exists(recycle))
                {
                    foreach (string f in SafeEnumerateFiles(recycle, "*", SearchOption.AllDirectories))
                    {
                        string n = Path.GetFileName(f).ToLowerInvariant();
                        if (n.EndsWith(".exe") || n.EndsWith(".dll") || n.EndsWith(".asi") || n.Contains("cheat"))
                            found.Add(Path.GetFileName(f));
                    }
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindAntivirusMatches()
        {
            var found = new List<string>();
            try
            {
                // Windows Defender threat log
                string[] paths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows Defender\Scans\History\Service\DetectionHistory"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows Defender\Scans\History\Results\Quick")
                };
                foreach (string path in paths)
                {
                    if (!Directory.Exists(path)) continue;
                    foreach (string f in SafeEnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        string txt = File.ReadAllText(f);
                        foreach (Match m in Regex.Matches(txt, @"ThreatName:\s*([^\n\r]+)"))
                            found.Add(m.Groups[1].Value.Trim());
                    }
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> FindPackedMods()
        {
            var found = new List<string>();
            try
            {
                string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "citizen");
                if (!Directory.Exists(basePath)) basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM");
                foreach (string f in SafeEnumerateFiles(basePath, "*.asi", SearchOption.AllDirectories).Concat(SafeEnumerateFiles(basePath, "*.rpf", SearchOption.AllDirectories)))
                {
                    string n = Path.GetFileName(f).ToLowerInvariant();
                    if (n.Contains("menu") || n.Contains("mod") || n.Contains("trainer") || n.Contains("cheat"))
                        found.Add(Path.GetFileName(f));
                }
            }
            catch { }
            return found.Distinct().Take(8).ToList();
        }

        private static List<string> RunVirusTotalEngine()
        {
            var found = new List<string>();
            // Real VirusTotal v3 API integration. Configure VT_API_KEY in the
            // environment; otherwise falls back to the local executed-file list.
            string vtKey = Environment.GetEnvironmentVariable("VT_API_KEY");
            List<string> candidates = FindExecutedSuspiciousFiles();
            if (!string.IsNullOrEmpty(vtKey))
            {
                foreach (string exe in candidates)
                {
                    try
                    {
                        if (found.Count >= 3) break;
                        string full = FindInCommonPaths(exe);
                        if (string.IsNullOrEmpty(full) || !File.Exists(full)) continue;
                        string sha = Sha256File(full);
                        if (string.IsNullOrEmpty(sha)) continue;
                        string line = QueryVirusTotal(vtKey, sha, full);
                        if (!string.IsNullOrEmpty(line)) found.Add(line);
                    }
                    catch { }
                }
            }
            if (found.Count == 0 && candidates.Count > 0) found.AddRange(candidates.Take(4));
            return found.Distinct().ToList();
        }

        private static string Sha256File(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (SHA256 sha = SHA256.Create())
                {
                    return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
                }
            }
            catch { return ""; }
        }

        private static string QueryVirusTotal(string key, string sha256, string file)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    client.Headers["x-apikey"] = key;
                    string json = client.DownloadString("https://www.virustotal.com/api/v3/files/" + sha256);
                    if (string.IsNullOrEmpty(json)) return null;
                    int mal = ParseVtInt(json, "malicious");
                    int sus = ParseVtInt(json, "suspicious");
                    int harm = ParseVtInt(json, "harmless");
                    return Path.GetFileName(file) + " [" + sha256.Substring(0, 12) + "] malicious=" + mal +
                           " suspicious=" + sus + " harmless=" + harm;
                }
            }
            catch { return null; } // 404 / quota / offline
        }

        private static int ParseVtInt(string json, string field)
        {
            try
            {
                string needle = "\"" + field + "\"";
                int idx = json.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return 0;
                int tail = json.IndexOf(',', idx);
                if (tail < 0) tail = json.Length;
                Match m = Regex.Match(json.Substring(idx, tail - idx), @":\s*(-?\d+)");
                if (!m.Success) return 0;
                return int.Parse(m.Groups[1].Value);
            }
            catch { return 0; }
        }

        private static List<string> FindUntrustedFiles()
        {
            return FindDotNetAssemblies().Take(7).ToList(); // reuse
        }

        private static List<string> FindRamInstances()
        {
            return FindSuspiciousModulesInGame(); // reuse: DLLs present in game memory
        }

        private static List<string> RunIADetection()
        {
            var found = new List<string>();
            try
            {
                // Telemetry: look for a cheat DLL loaded in multiple processes (a sign a menu is running).
                var counts = new Dictionary<string, int>();
                foreach (var p in Process.GetProcesses())
                {
                    foreach (string module in SafeModules(p))
                    {
                        string name = Path.GetFileName(module).ToLowerInvariant();
                        if (SuspectModuleMarkers.Any(m => name.Contains(m)) && name.EndsWith(".dll"))
                        {
                            counts[name] = counts.ContainsKey(name) ? counts[name] + 1 : 1;
                        }
                    }
                }
                foreach (var kv in counts.Where(kv => kv.Value >= 2))
                {
                    found.Add(Path.GetFileName(kv.Key) + " (in " + kv.Value + " processes)");
                }
            }
            catch { }
            return found.Take(6).ToList();
        }

        private static List<string> RunRuinMode()
        {
            var found = new List<string>();
            try
            {
                string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM");
                if (Directory.Exists(basePath))
                {
                    foreach (string f in SafeEnumerateFiles(basePath, "*.rpf", SearchOption.AllDirectories).Concat(SafeEnumerateFiles(basePath, "*.ytyp", SearchOption.AllDirectories)))
                    {
                        string n = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                        if (n.Contains("mod") || n.Contains("menu") || n.Contains("trainer") || n.Contains("cheat"))
                            found.Add(Path.GetFileName(f));
                    }
                }
            }
            catch { }
            return found.Distinct().Take(6).ToList();
        }

        // ------------------------------------------------------------------
        // Full DLL cheat scan — every DLL loaded in every process plus every
        // cheat-prone DLL/executable on disk. Matches both the module name
        // and the strings embedded inside the file (strings.exe-equivalent).
        // ------------------------------------------------------------------
        private static void RunDllScan()
        {
            try
            {
                // Results are cached; the Form layer promotes them into the
                // hit list (and the report) with full file/string detail.
                LastDllScanHits = new List<string>(ScanAllDllsForCheats(76d, 14d));
            }
            catch { }
        }

        public static List<string> ScanAllDllsForCheats(double basePct = double.MinValue, double span = double.MinValue)
        {
            var hits = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int contentScanned = 0;
            int processed = 0;

            List<string> loaded = GetAllLoadedDllPaths();
            int total1 = loaded.Count;

            foreach (string dll in loaded)
            {
                processed++;
                if (basePct != double.MinValue && total1 > 0)
                    ReportAt(basePct + span * 0.45 * (processed / (double)total1));
                if (string.IsNullOrEmpty(dll) || !seen.Add(dll)) continue;
                try
                {
                    if (File.Exists(dll) && FileMatchesCheatName(dll))
                    {
                        hits.Add(dll + "  (loaded / filename match)");
                    }
                    else if (!IsSystemPath(dll) && contentScanned < MaxContentScanFiles)
                    {
                        string matched;
                        if (File.Exists(dll) && FileContainsAnyCheatString(dll, out matched))
                        {
                            contentScanned++;
                            hits.Add(dll + "  (loaded / embedded \"" + matched + "\")");
                        }
                    }
                }
                catch { }
                if (hits.Count >= 30) { if (basePct != double.MinValue) ReportAt(basePct + span * 0.45); return hits; }
            }

            // Plan the disk pass up-front so the tail of the bar moves in
            // proportion to the real work left (no 98-99-100% stall).
            var diskTotal = new Dictionary<string, long>();
            foreach (string dir in GetCheatProneDirs())
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                long count = 0;
                bool recurse = dir.IndexOf("FiveM", StringComparison.OrdinalIgnoreCase) >= 0;
                SearchOption opt = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (string pat in new[] { "*.dll", "*.exe", "*.asi", "*.sys", "*.lua", "*.luac", "*.bin", "*.scr", "*.com" })
                {
                    foreach (string f in SafeEnumerateFiles(dir, pat, opt)) { count++; }
                }
                diskTotal[dir] = count;
            }
            long diskScanned = 0;
            long diskGrand = diskTotal.Values.Sum();

            foreach (string dir in GetCheatProneDirs())
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                long dirCount = diskTotal.TryGetValue(dir, out long dc) ? dc : 0;
                bool recurse = dir.IndexOf("FiveM", StringComparison.OrdinalIgnoreCase) >= 0;
                SearchOption opt = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (string pat in new[] { "*.dll", "*.exe", "*.asi", "*.sys", "*.lua", "*.luac", "*.bin", "*.scr", "*.com" })
                {
                    foreach (string f in SafeEnumerateFiles(dir, pat, opt))
                    {
                        diskScanned++;
                        if (basePct != double.MinValue && diskGrand > 0)
                            ReportAt(basePct + span * (0.45 + 0.55 * (diskScanned / (double)diskGrand)));
                        try
                        {
                            if (!seen.Add(f) || !File.Exists(f)) continue;
                            if (FileMatchesCheatName(f))
                            {
                                hits.Add(f + "  (disk / filename match)");
                            }
                            else if (!IsSystemPath(f) && contentScanned < MaxContentScanFiles)
                            {
                                string matched;
                                if (FileContainsAnyCheatString(f, out matched))
                                {
                                    contentScanned++;
                                    hits.Add(f + "  (disk / embedded \"" + matched + "\")");
                                }
                            }
                        }
                        catch { }
                        if (hits.Count >= 30)
                        {
                            if (basePct != double.MinValue) ReportAt(basePct + span);
                            return hits;
                        }
                    }
                }
            }
            return hits;
        }

        private static List<string> GetAllLoadedDllPaths()
        {
            var dlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    IntPtr h = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, p.Id);
                    if (h == IntPtr.Zero) continue;
                    try
                    {
                        uint needed = 0;
                        var modules = new IntPtr[2048];
                        if (EnumProcessModulesEx(h, modules, (uint)(modules.Length * IntPtr.Size), out needed, LIST_MODULES_ALL))
                        {
                            int count = Math.Min((int)(needed / IntPtr.Size), modules.Length);
                            var sb = new StringBuilder(1024);
                            for (int i = 0; i < count; i++)
                            {
                                if (GetModuleFileNameEx(h, modules[i], sb, (uint)sb.Capacity) == 0) continue;
                                string path = sb.ToString();
                                bool isModule = path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                                             || path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
                                if (isModule && !string.IsNullOrEmpty(path))
                                    dlls.Add(path);
                            }
                        }
                    }
                    catch { }
                    finally { CloseHandle(h); }
                }
                catch { }
            }
            // Managed fallback for any process the native pass could not open.
            foreach (var p in Process.GetProcesses())
            {
                foreach (string m in SafeModules(p))
                {
                    if (!string.IsNullOrEmpty(m) &&
                        (m.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                         m.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
                        dlls.Add(m);
                }
            }
            return dlls.ToList();
        }

        private static List<string> GetCheatProneDirs()
        {
            var dirs = new List<string>();
            try { dirs.Add(Path.GetTempPath()); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs")); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FiveM Application Data")); } catch { }
            return dirs;
        }

        private static bool IsSystemPath(string path)
        {
            try
            {
                string low = path.ToLowerInvariant();
                return low.Contains("\\windows\\") || low.StartsWith("c:\\windows")
                    || low.StartsWith("c:\\program files") || low.Contains("\\program files\\");
            }
            catch { return true; }
        }

        private static bool FileMatchesCheatName(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string name = Path.GetFileName(path).ToLowerInvariant();
            foreach (string literal in CheatDllNames)
            {
                if (name.Contains(literal)) return true;
            }
            foreach (string pattern in KnownInjectors)
            {
                if (MatchPattern(pattern, name)) return true;
            }
            return false;
        }

        private static bool FileContainsAnyCheatString(string path, out string matched)
        {
            matched = null;
            try
            {
                var fi = new FileInfo(path);
                if (!fi.Exists || fi.Length < 8 || fi.Length > MaxContentScanBytes) return false;
                byte[] data = File.ReadAllBytes(path);
                if (data.Length < 8) return false;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] >= 65 && data[i] <= 90) data[i] = (byte)(data[i] + 32);
                }
                string text = Encoding.GetEncoding(28591).GetString(data);
                foreach (string sig in CheatContentSignatures)
                {
                    if (text.IndexOf(sig, StringComparison.Ordinal) >= 0)
                    {
                        matched = sig;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        // ==================================================================
        // Low-level helpers
        // ==================================================================
        private static string StripExt(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return pattern;
            string p = pattern;
            p = p.Replace("*", "").Replace("?", "");
            int dot = p.LastIndexOf('.');
            if (dot > 0) p = p.Substring(0, dot);
            return p.ToLowerInvariant();
        }

        private static bool MatchPattern(string pattern, string name)
        {
            pattern = pattern.ToLowerInvariant();
            if (!pattern.Contains("*") && !pattern.Contains("?"))
                return name.Contains(pattern);
            return Regex.IsMatch(name, "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$");
        }

        private static List<string> GetAllLoadedModuleNames()
        {
            var names = new List<string>();
            foreach (var p in Process.GetProcesses())
            {
                foreach (string module in SafeModules(p))
                {
                    try { names.Add(Path.GetFileName(module).ToLowerInvariant()); } catch { }
                }
            }
            return names;
        }

        private static List<string> SafeModules(Process p)
        {
            var list = new List<string>();
            try
            {
                foreach (ProcessModule pm in p.Modules)
                {
                    try { list.Add(pm.FileName); } catch { }
                }
            }
            catch { }
            return list;
        }

        private static IEnumerable<string> SafeEnumerateFiles(string path, string pattern, SearchOption opt)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) yield break;
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(path, pattern, opt); }
            catch { yield break; }
            foreach (string f in files)
                yield return f;
        }

        private static IEnumerable<string> SafeEnumerateDirs(string path, string pattern, SearchOption opt)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) yield break;
            IEnumerable<string> dirs;
            try { dirs = Directory.EnumerateDirectories(path, pattern, opt); }
            catch { yield break; }
            foreach (string d in dirs)
                yield return d;
        }

        private static string QuickHash(string file)
        {
            using (var md5 = MD5.Create())
            using (var fs = File.OpenRead(file))
            {
                byte[] hash = md5.ComputeHash(fs);
                return Convert.ToBase64String(hash);
            }
        }

        private static byte[] ReadHeader(string file, int count)
        {
            byte[] buf = new byte[count];
            using (FileStream fs = File.OpenRead(file))
            {
                fs.Read(buf, 0, count);
            }
            return buf;
        }

        private static bool IsDotNetAssembly(string file)
        {
            byte[] head = ReadHeader(file, 256);
            if (head.Length < 128) return false;
            uint pe = BitConverter.ToUInt32(head, 0x3C);
            if (pe + 0x18 + 0x40 >= head.Length) return false;
            // CLR header directory (index 14) non-zero.
            int clrOffset = (int)pe + 24 + 112;
            if (clrOffset + 8 > head.Length) return false;
            uint rva = BitConverter.ToUInt32(head, clrOffset);
            uint size = BitConverter.ToUInt32(head, clrOffset + 4);
            return rva != 0 && size != 0;
        }

        private static bool IsUpxPacked(string file)
        {
            try
            {
                byte[] head = ReadHeader(file, 0x400);
                if (head.Length < 0x200) return false;
                for (int i = 0; i < head.Length - 4; i++)
                {
                    if (head[i] == 'U' && head[i + 1] == 'P' && head[i + 2] == 'X' && head[i + 3] == '!')
                        return true;
                    if (head[i] == 'U' && head[i + 1] == 'P' && head[i + 2] == 'X' && head[i + 3] == '0')
                        return true;
                }
                return false;
            }
            catch { return false; }
        }

        private static bool IsAutoItExe(string file)
        {
            try
            {
                byte[] data = ReadHeader(file, 4096);
                if (data.Length < 1024) return false;
                string sig = "This is a third-party compiled AutoIt script";
                string head = Encoding.ASCII.GetString(data);
                return head.Contains(sig) || head.Contains("AutoIt");
            }
            catch { return false; }
        }

        // ------------------------------------------------------------------
        // KeyAuth / BAM / stopped services / unsigned files helpers
        // ------------------------------------------------------------------
        private static readonly string[] CriticalServices = { "SysMain", "DPS", "BAM", "DiagTrack", "PcaSvc" };

        private static List<string> FindStoppedServices()
        {
            var stopped = new List<string>();
            foreach (string svc in CriticalServices)
            {
                try
                {
                    using (var sc = new System.ServiceProcess.ServiceController(svc))
                    {
                        if (sc.Status != System.ServiceProcess.ServiceControllerStatus.Running)
                            stopped.Add(svc + " (" + sc.Status + ")");
                    }
                }
                catch { }
            }
            return stopped;
        }

        private static List<string> FindUnsignedExecutedFiles()
        {
            var hits = new List<string>();
            try
            {
                string pf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                var candidates = new List<string>();
                foreach (string f in SafeEnumerateFiles(pf, "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string baseName = Path.GetFileNameWithoutExtension(f);
                    int dash = baseName.LastIndexOf('-');
                    string exe = ((dash > 0 ? baseName.Substring(0, dash) : baseName) + ".exe").ToLowerInvariant();
                    if (exe.Contains("chrome") || exe.Contains("firefox") || exe.Contains("edge") ||
                        exe.Contains("explorer") || exe.Contains("microsoft") || exe.Contains("oneget") ||
                        exe.Contains("windows") || exe.Contains("mrt") || exe.Contains("search"))
                        continue;
                    candidates.Add(exe.EndsWith(".exe") ? exe : exe + ".exe");
                }
                foreach (string exe in candidates.Distinct().Take(40))
                {
                    try
                    {
                        string full = FindInCommonPaths(exe);
                        if (string.IsNullOrEmpty(full) || !File.Exists(full)) continue;
                        if (!HasValidSignature(full)) hits.Add(Path.GetFileName(full));
                    }
                    catch { }
                }
            }
            catch { }
            return hits;
        }

        private static string FindInCommonPaths(string exe)
        {
            string[] roots = {
                Path.GetTempPath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop"),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            };
            foreach (string r in roots)
            {
                try
                {
                    if (string.IsNullOrEmpty(r)) continue;
                    string c = Path.Combine(r, exe);
                    if (File.Exists(c)) return c;
                }
                catch { }
            }
            return null;
        }

        public static bool HasValidSignature(string file)
        {
            try
            {
                return System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromSignedFile(file) != null;
            }
            catch
            {
                return false;
            }
        }
    private static bool IsDefenderDisabled()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("DisableAntiSpyware");
                        if (val != null && Convert.ToInt32(val) == 1) return true;
                    }
                }
                using (var sc = new System.ServiceProcess.ServiceController("WinDefend"))
                {
                    return sc.Status == System.ServiceProcess.ServiceControllerStatus.Stopped;
                }
            }
            catch { return false; }
        }

        private static List<string> FindDiscordTraces()
        {
            var traces = new List<string>();
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord", "Local Storage", "leveldb");
            if (Directory.Exists(path)) traces.Add("Discord LocalStorage/leveldb present");
            return traces;
        }

        private static List<string> FindFiveMReadyConfigs()
        {
            var hits = new List<string>();
            string[] fiveMDirs = { 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.app", "citizen"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FiveM Application Data")
            };
            foreach (string dir in fiveMDirs)
            {
                string ini = Path.Combine(dir, "CitizenFX.ini");
                if (File.Exists(ini))
                {
                    string content = File.ReadAllText(ini).ToLowerInvariant();
                    if (content.Contains("sv_pure=0") || content.Contains("r_drawmodel"))
                        hits.Add("Suspicious setting in CitizenFX.ini: " + dir);
                }
            }
            return hits;
        }

        // ------------------------------------------------------------------
        // Structured report payload helpers. These feed the web dashboard's
        // detailed report with REAL machine data (activity, Discord accounts,
        // recording software) instead of the demo placeholders it used to show.
        // ------------------------------------------------------------------
        public class ActivityEntry
        {
            public string filename;
            public string runtime;
            public string action;
            public bool signed;
        }

        public class DiscordAccount
        {
            public string username;
            public string status;
            public string[] ids;
        }

        public class RecordingTool
        {
            public string name;
            public string status;
            public string[] entries;
        }

        /// <summary>Most recently executed programs, from Windows Prefetch.</summary>
        public static List<ActivityEntry> CollectRecentActivity(int max = 25)
        {
            var list = new List<ActivityEntry>();
            try
            {
                string prefetchDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                if (!Directory.Exists(prefetchDir)) return list;
                var files = Directory.GetFiles(prefetchDir, "*.pf", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => { try { return File.GetLastWriteTime(f); } catch { return DateTime.MinValue; } })
                    .Take(max);
                foreach (string pf in files)
                {
                    try
                    {
                        string fileName = Path.GetFileName(pf);
                        // NOTEPAD.EXE-ABCDEF01.pf -> NOTEPAD.EXE
                        string name = Regex.Replace(fileName, @"-[0-9A-F]{8}\.pf$", "", RegexOptions.IgnoreCase);
                        if (string.IsNullOrEmpty(name)) name = fileName;
                        bool signed = false;
                        string candidate = FindExecutable(name);
                        if (string.IsNullOrEmpty(candidate)) candidate = FindExecutable(name + ".exe");
                        if (!string.IsNullOrEmpty(candidate)) signed = HasValidSignature(candidate);
                        list.Add(new ActivityEntry
                        {
                            filename = name,
                            runtime = File.GetLastWriteTime(pf).ToString("yyyy-MM-dd HH:mm:ss"),
                            action = "Started",
                            signed = signed
                        });
                    }
                    catch { }
                }
            }
            catch { }
            return list;
        }

        private static string FindExecutable(string fileName)
        {
            try
            {
                string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string[] roots =
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), fileName),
                    Path.Combine(win, "SysWOW64", fileName),
                    Path.Combine(win, "System32", fileName),
                    Path.Combine(win, fileName),
                    Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", fileName),
                    Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES") ?? "", fileName)
                };
                foreach (string r in roots)
                {
                    try { if (File.Exists(r)) return r; } catch { }
                }
                string pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (string dir in pathVar.Split(';'))
                {
                    try
                    {
                        string p = Path.Combine(dir.Trim(), fileName);
                        if (File.Exists(p)) return p;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        /// <summary>Real Discord account names found in the local LevelDB stores.</summary>
        public static List<DiscordAccount> CollectDiscordAccounts()
        {
            var accounts = new Dictionary<string, string>(); // username -> id
            string[] discordDirs = { "discord", "discordcanary", "discordptb", "discorddevelopment" };
            foreach (string dn in discordDirs)
            {
                try
                {
                    string leveldb = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        dn, "Local Storage", "leveldb");
                    if (!Directory.Exists(leveldb)) continue;
                    foreach (string f in Directory.GetFiles(leveldb, "*.ldb", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            string text = DecodeLdb(f);
                            var nameMatch = Regex.Match(text, "\"username\"\\s*:\\s*\"([A-Za-z0-9_ .]{2,32})\"");
                            if (!nameMatch.Success) continue;
                            string username = nameMatch.Groups[1].Value;
                            string id = "";
                            var idMatch = Regex.Match(text, "\"id\"\\s*:\\s*\"([0-9]{17,20})\"");
                            if (idMatch.Success) id = idMatch.Groups[1].Value;
                            if (!accounts.ContainsKey(username)) accounts[username] = id;
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return accounts
                .Select(kv => new DiscordAccount
                {
                    username = kv.Key,
                    status = "detected",
                    ids = string.IsNullOrEmpty(kv.Value) ? new string[0] : new[] { "ID: " + kv.Value }
                })
                .ToList();
        }

        private static string DecodeLdb(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int cap = 2 * 1024 * 1024;
                    byte[] buf = new byte[Math.Min(fs.Length > cap ? cap : (int)fs.Length, cap)];
                    int n = fs.Read(buf, 0, buf.Length);
                    var sb = new StringBuilder(n);
                    for (int i = 0; i < n; i++)
                    {
                        char c = (char)buf[i];
                        if (c == 0) continue;
                        if (c == 13 || c == 10) c = ' ';
                        sb.Append(c >= 32 && c < 127 ? c : ' ');
                    }
                    return sb.ToString();
                }
            }
            catch { return ""; }
        }

        /// <summary>Recording / screen-capture software currently running.</summary>
        public static List<RecordingTool> CollectRecordingSoftware()
        {
            var tools = new List<RecordingTool>();
            var map = new Dictionary<string, string[]>
            {
                { "NVIDIA Instant Replay", new[] { "nvcontainer", "nvsphelper64" } },
                { "Windows Game Bar / Xbox Record", new[] { "GameBar", "GameBarFTServer" } },
                { "OBS Studio", new[] { "obs64", "obs32" } },
                { "Medal", new[] { "medal" } },
                { "Overwolf / Outplayed", new[] { "overwolf" } },
                { "SteelSeries Moments", new[] { "steelseriesmoments" } }
            };
            foreach (var kv in map)
            {
                var running = kv.Value.Where(n => IsProcessRunning(n)).ToList();
                if (running.Count == 0) continue;
                tools.Add(new RecordingTool
                {
                    name = kv.Key,
                    status = "running",
                    entries = running.ToArray()
                });
            }
            return tools;
        }

        private static bool IsProcessRunning(string name)
        {
            try { return Process.GetProcessesByName(name).Length > 0; }
            catch { return false; }
        }
    }
}
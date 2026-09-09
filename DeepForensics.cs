using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SecurityTools
{
    /// <summary>
    /// Ocean Deep Forensics engine.
    /// Mirrors the collection vectors used by modern FiveM PC checkers /
    /// screenshare scanners (SGC, Ocean, sero, NPPlus, Detect, ripsaw …):
    /// once a cheat has run its trace never fully disappears — it lives on in
    /// ShimCache, BAM, Amcache, Prefetch residue, USN journal state, WER crash
    /// reports, MRU/LNK pointers, DNS cache, browser history and Driver event
    /// logs even after the files were deleted and the folders wiped.
    /// </summary>
    public static partial class OceanScan
    {
        // ------------------------------------------------------------------
        // Cheat family / marker catalogue (from public cheat data)
        // ------------------------------------------------------------------
        private static readonly string[] DeepCheatNames = {
            "eulen", "redengine", "skript", "susano", "gosth", "dopamine", "desudo",
            "cherax", "paragon", "lynxmenu", "kdot", "skeela", "seagul", "sakeru",
            "machomenu", "cutiehook", "oziware", "monkeyware", "monesy",
            "projectcheats", "stoppedbypass", "keyauth", "hamafia", "chinese.exe",
            "gemini", "latent", "reb3l", "aureus", "mundo", "krak", "topnotch",
            "keyser", "phaze", "vortexmenu", "lumia", "zpo", "marseille", "macho",
            "kola", "nightfall", "nixus", "shawarma", "2take1", "sixcall", "k-extra",
            "quantum", "hound", "salamand3r", "ech0", "delusion", "serena", "hazari",
            "prax", "praxmenu", "deimos", "klay", "jasza", "elysian", "nemesis",
            "lucis", "forix", "glorify", "kestrel", "mischief", "opium", "sober",
            "celesta", "stellarmenu", "astromenu", "rkmenu", "statementmenu", "flare",
            "sourfish", "owinock", "panilic", "canval", "estrin", "mechanizm",
            "outcast", "obscura", "0xalumnus", "sheetghost", "bonkcheat", "phunky",
            "ascension", "interstellar", "pulsefi"
        };
        private static readonly string[] ExtendedCheatDomains = {
            "susano.re", "kola.gg", "marseille", "vortex.gg", "keyser.gg", "cheatware",
            "2take1.menu", "stand.gg", "hazari.qy", "redengine.eu", "eulen.gg", "cherax.gg",
            "prax.menu", "deimos.io", "klay.menu", "elysian.cf", "nemesis.io",
            "lucis.menu", "forix.menu", "glorify.fun", "kestrel.menu", "opium.fun"
        };

        private static readonly string[] ForensicStrongMarkers = {
            "loader", "injector", "inject", "spoofer", "hwid", "kdmapper",
            "manualmap", "mapper", "cheatengine", "xenos", "cheat", "bypass",
            "crack", "keygen", "aimbot", "wallhack", "modmenu", "executor",
            "hacktool" 
        };

        private static readonly string[] BroadMarkers = {
            "loader", "injector", "inject", "spoofer", "hwid", "kdmapper",
            "manualmap", "mapper", "cheat", "bypass", "crack", "keygen",
            "aimbot", "wallhack", "modmenu", "executor", "menu", "hack",
            "esp", "overlay", "dumper", "unlocker", "macro", "autoit", "ahk"
        };

        private static readonly string[] CheatDomains = {
            "skript.gg", "pedrin.cc", "pedrin.ovh", "gosth.gg", "eulen.gg",
            "susano.dev", "redengine.eu", "projectcheats.com", "monesy.dev",
            "idandev.xyz", "stoppedbypass.com", "keyauth", "unknowncheats",
            "mpgh.net", "cracked.io", "nulled.to", "cheatengine.org",
            "x64dbg", "crackthese", "luaexecutor", "parazetamol"
        };

        private static readonly string[] VulnDriverNames = {
            "iqvw64e", "gdrv", "rtcore64", "capcom", "dbk64", "mhyprot2",
            "procexp152", "echo_driver", "kdmapper", "piddrv", "byovd",
            "aweos", "pcwdf", "smbios", "ati2mtag", "dsuedio", "nocrashdrv"
        };

        private static readonly string[] DmaSignals = {
            "pcileech", "kmbox", "kmboxlite", "fpga", "screamer", "pci leech", "dma leech"
        };

        // ------------------------------------------------------------------
        // Entry point — runs every forensic vector.
        // ------------------------------------------------------------------
        private static void RunDeepForensics()
        {
            Try(() => RunDnsCacheForensics());
            Try(() => RunEventLogForensics());
            Try(() => RunWerForensics());
            Try(() => RunMruForensics());
            Try(() => RunLnkForensics());
            Try(() => RunBrowserForensics());
            Try(() => RunShimCacheForensics());
            Try(() => TryAmcacheLoad());
            Try(() => RunBamFullPathForensics());
            Try(() => RunDriverAudit());
            Try(() => RunStartupPersistence());
            Try(() => RunScheduledTaskForensics());
            Try(() => RunHwSpoofSigns());
            Try(() => RunDmaDetection());
            Try(() => RunAntiForensicStateChecks());
            Try(() => RunRecycleBinMetaForensics());
            Try(() => RunArchiveSweep());
            Try(() => RunExeSweep());
        }

        private static void Try(Action action)
        {
            try { action(); }
            catch { }
        }

        // ==================================================================
        // DNS cache — reach-outs to cheat/command domains survive deletions.
        // ==================================================================
        private static void RunDnsCacheForensics()
        {
            try
            {
                string outText = RunCapture("ipconfig.exe", "/displaydns");
                if (string.IsNullOrEmpty(outText)) return;
                string low = outText.ToLowerInvariant();
                var found = new List<string>();
                foreach (string d in CheatDomains)
                {
                    if (low.Contains(d))
                    {
                        found.Add(d);
                        if (found.Count >= 8) break;
                    }
                }
                if (found.Count > 0)
                {
                    Add("Suspicious", "DNS Cache Points to Cheat / Command Domain", "Network",
                        "DNS cache still contains resolutions for cheat or bypass infrastructure: " + string.Join(", ", found));
                }
            }
            catch { }
        }

        // ==================================================================
        // Event logs — driver loads, process creation, log clearing, crashes.
        // ==================================================================
        private static void RunEventLogForensics()
        {
            DateTime since = DateTime.Now.AddDays(-75);

            // System: unsigned / unusual driver & service installs (7045/7040/20001).
            try
            {
                List<string> drivers = ScanEventLog("System", new long[] { 7045, 7040, 20001 },
                    since, ForensicStrongMarkers, 6);
                if (drivers.Count > 0)
                {
                    Add("Systems", "Driver / Service Tied to Suspicious Path", "Kernel",
                        "Event log shows a driver or service install referencing suspicious paths: " + string.Join(" | ", drivers));
                }
            }
            catch { }

            // Security: process creation records (4688) matching cheat markers.
            try
            {
                List<string> proc = ScanEventLog("Security", new long[] { 4688 },
                    since, ForensicStrongMarkers, 6);
                if (proc.Count > 0)
                {
                    Add("Systems", "Process Creation Audit — Cheat Marker", "Process",
                        "Security log recorded processes named after known cheat families: " + string.Join(" | ", proc));
                }
            }
            catch { }

            // Security / System cleared — both must be flagged for a screenshare.
            try
            {
                DateTime lastSecClear = LastLogClear("Security", "1102");
                DateTime lastSysClear = LastLogClear("System", "104");
                DateTime threshold = DateTime.Now.AddDays(-30);
                if (lastSecClear != DateTime.MinValue && lastSecClear > threshold)
                    Add("Warnings", "Security Log Cleared (Anti-Forensic)", "Log",
                        "The Security audit log was cleared on " + lastSecClear.ToString("yyyy-MM-dd HH:mm") + ".");
                if (lastSysClear != DateTime.MinValue && lastSysClear > threshold)
                    Add("Warnings", "System Log Cleared (Anti-Forensic)", "Log",
                        "The System event log was cleared on " + lastSysClear.ToString("yyyy-MM-dd HH:mm") + ".");
            }
            catch { }

            // Application: WER faulting modules (1000/1001/1002) referencing cheats.
            try
            {
                List<string> faults = ScanEventLog("Application", new long[] { 1000, 1001, 1002 },
                    since, ForensicStrongMarkers, 6);
                if (faults.Count > 0)
                {
                    Add("Systems", "Crash Report — Suspicious Module", "Forensic",
                        "Windows Error Reporting logs reference suspicious faulting modules: " + string.Join(" | ", faults));
                }
            }
            catch { }

            // PowerShell operational log (4104 script-block) — execution bypass traces.
            try
            {
                DateTime cutoff = DateTime.Now.AddDays(-45);
                string[] bypassKeys = { "bypass", "-windowstyle hidden", "invoke-expression", "assembly.load", "add-type" };
                var found = new List<string>();
                using (EventLog pwsh = new EventLog("Microsoft-Windows-PowerShell/Operational"))
                {
                    int count = pwsh.Entries.Count;
                    for (int i = count - 1; i >= 0 && i >= count - 3000; i--)
                    {
                        EventLogEntry e = pwsh.Entries[i];
                        if (e.TimeGenerated < cutoff) break;
                        if (e.InstanceId == 4104L)
                        {
                            string msg = e.Message.ToLowerInvariant();
                            if (bypassKeys.Any(b => msg.Contains(b)))
                            {
                                found.Add("4104 @" + e.TimeGenerated.ToString("MM-dd HH:mm"));
                                if (found.Count >= 3) break;
                            }
                        }
                    }
                }
                if (found.Count > 0)
                {
                    Add("Suspicious", "PowerShell Script Block Logging — Bypass", "Bypass",
                        "PowerShell Operational log 4104 contains bypass / hidden-execution blocks: " + string.Join(" | ", found));
                }
            }
            catch { }
        }

        private static List<string> ScanEventLog(string log, long[] eventIds, DateTime since,
            string[] markers, int cap)
        {
            var found = new List<string>();
            using (EventLog el = new EventLog(log))
            {
                int count = el.Entries.Count;
                for (int i = count - 1; i >= 0 && i >= count - 4000; i--)
                {
                    EventLogEntry e = el.Entries[i];
                    if (e.TimeGenerated < since) break;
                    if (!eventIds.Contains(e.InstanceId)) continue;
                    string msg = e.Message ?? "";
                    string low = msg.ToLowerInvariant();
                    bool matchedMarker = false;
                    foreach (string mk in markers)
                    {
                        if (low.Contains(mk)) { matchedMarker = true; break; }
                    }
                    // Also flag obvious non-system driver/image paths.
                    if (!matchedMarker &&
                        Regex.IsMatch(low, @"(\\temp\\|\\downloads\\|\\desktop\\|\\appdata\\|\\users\\)[^\\]*\.(exe|dll|sys)", RegexOptions.IgnoreCase))
                    {
                        matchedMarker = true;
                    }
                    if (matchedMarker)
                    {
                        Match m = Regex.Match(msg, @"(?i)(Path\s+:\s*|ImagePath\s+:\s*|Process Name:\s*|Faulting module name:\s*)?([A-Za-z]:\\[^\r\n]{0,140})");
                        if (!m.Success) m = Regex.Match(msg, @"[A-Za-z]:\\[^\r\n]{0,120}");
                        string token = m.Success ? m.Groups[0].Value.Trim() : ("event " + e.InstanceId);
                        if (!string.IsNullOrEmpty(token) && found.Count < cap) found.Add(token);
                    }
                }
            }
            return found.Distinct().ToList();
        }

        private static DateTime LastLogClear(string log, string eventId)
        {
            try
            {
                using (EventLog el = new EventLog(log))
                {
                    int count = el.Entries.Count;
                    for (int i = count - 1; i >= 0 && i >= count - 4000; i--)
                    {
                        EventLogEntry e = el.Entries[i];
                        if (e.InstanceId.ToString() == eventId) return e.TimeGenerated;
                    }
                }
            }
            catch { }
            return DateTime.MinValue;
        }

        // ==================================================================
        // WER crash reports survive deletion of the cheat itself.
        // ==================================================================
        private static void RunWerForensics()
        {
            var dirs = new List<string>();
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\WER\ReportArchive")); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\WER\ReportQueue")); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\WER\ReportArchive")); } catch { }
            var found = new List<string>();
            int scanned = 0;
            foreach (string d in dirs)
            {
                if (!Directory.Exists(d)) continue;
                foreach (string f in SafeEnumerateFiles(d, "*.wer", SearchOption.TopDirectoryOnly))
                {
                    if (++scanned > 1000) break;
                    try
                    {
                        string txt = File.ReadAllText(f);
                        string low = txt.ToLowerInvariant();
                        bool hit = DeepCheatNames.Any(kw => low.Contains(kw)) ||
                                   ForensicStrongMarkers.Any(mk => low.Contains(mk) &&
                                       Regex.IsMatch(txt, @"\.(exe|dll|sys)", RegexOptions.IgnoreCase));
                        if (hit && found.Count < 12)
                        {
                            Match mod = Regex.Match(txt, @"(?i)LoadedModule[^:]{0,40}:\s*([^\r\n<]+)");
                            found.Add(Path.GetFileName(f) + (mod.Success ? " [" + mod.Groups[1].Value.Trim() + "]" : ""));
                        }
                    }
                    catch { }
                }
            }
            if (found.Count > 0)
            {
                Add("Systems", "WER Crash Reports — Cheat Module Traces", "Forensic",
                    "Windows Error Reporting retained crash records pointing at suspicious modules (kept even after deletion): " +
                    string.Join(" | ", found));
            }
        }

        // ==================================================================
        // Registry MRU / typed paths / recent docs — survived deletion.
        // ==================================================================
        private static void RunMruForensics()
        {
            var branches = new List<string>
            {
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs\*",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRU",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\WordWheelQuery"
            };

            var found = new List<string>();
            try
            {
                foreach (string branch in branches)
                {
                    try
                    {
                        using (RegistryKey root = Registry.CurrentUser.OpenSubKey(branch.Replace(@"\*", "")))
                        {
                            if (root == null) continue;
                            if (branch.EndsWith(@"\*"))
                            {
                                foreach (string sub in root.GetSubKeyNames())
                                {
                                    try
                                    {
                                        using (RegistryKey sk = root.OpenSubKey(sub))
                                            ScanRegForCheatStrings(sk, found);
                                    }
                                    catch { }
                                }
                            }
                            else
                            {
                                ScanRegForCheatStrings(root, found);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            if (found.Count > 0)
            {
                Add("Suspicious", "MRU / Recent History — Cheat Artifacts", "Forensic",
                    "Explorer MRU and recent-document history still reference suspicious executables: " +
                    string.Join(" | ", found.Take(10)));
            }
        }

        private static void ScanRegForCheatStrings(RegistryKey key, List<string> found)
        {
            if (key == null) return;
            foreach (string valName in key.GetValueNames())
            {
                try
                {
                    object raw = key.GetValue(valName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (raw == null) continue;
                    string low;
                    if (raw is byte[])
                    {
                        low = DecodeBoth((byte[])raw).ToLowerInvariant();
                    }
                    else
                    {
                        low = raw.ToString().ToLowerInvariant();
                    }
                    if (DeepCheatNames.Any(kw => low.Contains(kw)) ||
                        Regex.IsMatch(low, @"[a-z]:\\[^\\]{0,60}\\(loader|inject|spoof|cheat|hack|kdm|bypass)[^\\]{0,40}\.(exe|dll|bat|ps1)"))
                    {
                        if (found.Count < 15)
                        {
                            Match m = Regex.Match(low, @"[a-z]:\\[^\r\n]{0,120}");
                            found.Add(m.Success ? m.Value : valName);
                        }
                    }
                }
                catch { }
            }
        }

        // ==================================================================
        // LNK (Recent) pointers — target path disclosed even if file removed.
        // ==================================================================
        private static void RunLnkForensics()
        {
            try
            {
                string recent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Recent");
                if (!Directory.Exists(recent)) return;
                var found = new List<string>();
                int scanned = 0;
                foreach (string f in SafeEnumerateFiles(recent, "*.lnk", SearchOption.AllDirectories))
                {
                    if (++scanned > 400) break;
                    try
                    {
                        string txt = DecodeBoth(ReadCapped(f, 384 * 1024));
                        string low = txt.ToLowerInvariant();
                        if (!Regex.IsMatch(txt, @"[A-Za-z]:\\[^\r\n]*?\.exe", RegexOptions.IgnoreCase)) continue;
                        if (DeepCheatNames.Any(kw => low.Contains(kw)) ||
                            ForensicStrongMarkers.Any(mk => low.Contains(mk)))
                        {
                            if (found.Count < 12) found.Add(Path.GetFileName(f));
                        }
                    }
                    catch { }
                }
                if (found.Count > 0)
                {
                    Add("Suspicious", "Recent Shortcuts Reference Cheat Files", "Forensic",
                        "Recent (.lnk) shortcuts still point at suspicious executables: " + string.Join(", ", found));
                }
            }
            catch { }
        }

        // ==================================================================
        // Browser download/visit history — cheat download footprints.
        // ==================================================================
        private static void RunBrowserForensics()
        {
            var dbFiles = new List<string>();
            try
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                foreach (string browser in new[] { "Google\\Chrome\\User Data", "Microsoft\\Edge\\User Data", "BraveSoftware\\Brave-Browser\\User Data" })
                {
                    string dir = Path.Combine(local, browser);
                    if (Directory.Exists(dir))
                        foreach (string h in SafeEnumerateFiles(dir, "History", SearchOption.AllDirectories))
                            if (h.IndexOf("Cache", StringComparison.OrdinalIgnoreCase) < 0) dbFiles.Add(h);
                }
            }
            catch { }
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string ff = Path.Combine(appData, @"Mozilla\Firefox\Profiles");
                if (Directory.Exists(ff))
                    foreach (string p in SafeEnumerateFiles(ff, "places.sqlite", SearchOption.AllDirectories)) dbFiles.Add(p);
            }
            catch { }

            var found = new List<string>();
            foreach (string db in dbFiles.Distinct().Take(14))
            {
                try
                {
                    string low = DecodeBoth(ReadCapped(db, 6 * 1024 * 1024)).ToLowerInvariant();
                    foreach (string d in CheatDomains)
                    {
                        if (low.Contains(d))
                        {
                            string browser = db.IndexOf("Firefox", StringComparison.OrdinalIgnoreCase) >= 0 ? "Firefox" :
                                db.IndexOf("Edge", StringComparison.OrdinalIgnoreCase) >= 0 ? "Edge" :
                                db.IndexOf("Brave", StringComparison.OrdinalIgnoreCase) >= 0 ? "Brave" : "Chrome";
                            if (found.Count < 12) found.Add(browser + " → " + d);
                            break;
                        }
                    }
                }
                catch { }
            }
            if (found.Count > 0)
            {
                Add("Suspicious", "Browser History — Cheat Sites Visited", "Browser",
                    "Browser databases contain visits to cheat / bypass sites: " + string.Join(", ", found));
            }
        }

        // ==================================================================
        // ShimCache (AppCompatCache) — execution even of deleted files.
        // ==================================================================
        private static void RunShimCacheForensics()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\AppCompatCache"))
                {
                    if (k == null) return;
                    byte[] data = k.GetValue("AppCompatCache") as byte[];
                    if (data == null || data.Length < 16) return;
                    string text = DecodeBoth(data);
                    var found = new List<string>();
                    foreach (Match m in Regex.Matches(text, @"[A-Za-z]:\\[^\x00-\x1f]{3,300}\.(exe|dll|sys)", RegexOptions.IgnoreCase))
                    {
                        string p = m.Value;
                        string name = Path.GetFileName(p).ToLowerInvariant();
                        if (DeepCheatNames.Any(kw => name.Contains(kw)) ||
                            ForensicStrongMarkers.Any(mk => name.Contains(mk)))
                        {
                            if (found.Count < 12) found.Add(p.Trim());
                        }
                    }
                    if (found.Count > 0)
                    {
                        Add("Systems", "ShimCache Execution Trail (Deleted Files)", "Forensic",
                            "AppCompatCache/ShimCache still records execution of suspicious files — including files already deleted: " +
                            string.Join(" | ", found));
                    }
                }
            }
            catch { }
        }

        // ==================================================================
        // Amcache — best-effort offline load (requires admin, always run as).
        // ==================================================================
        private static void TryAmcacheLoad()
        {
            string hive = null;
            try
            {
                hive = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    @"AppCompat\Programs\Amcache.hve");
            }
            catch { }
            if (string.IsNullOrEmpty(hive) || !File.Exists(hive)) return;

            const string mount = @"HKLM\OCEAN_AMCACHE";
            try
            {
                RunCapture("reg.exe", "load " + mount + " \"" + hive + "\"");
                string tree = RunCapture("reg.exe", "query " + mount + " /s");
                if (string.IsNullOrEmpty(tree)) return;
                string low = tree.ToLowerInvariant();
                var found = new List<string>();
                foreach (string kw in DeepCheatNames.Concat(ForensicStrongMarkers))
                {
                    if (low.Contains(kw)) { found.Add(kw); if (found.Count >= 10) break; }
                }
                if (found.Count > 0)
                {
                    Add("Systems", "Amcache Recovery — Deleted Executable Trail", "Forensic",
                        "Amcache.hve retained records of executables (even if removed from disk since): " +
                        string.Join(", ", found));
                }
            }
            catch { }
            finally
            {
                try { RunCapture("reg.exe", "unload " + mount); } catch { }
            }
        }

        // ==================================================================
        // BAM — full path recovery, marks files deleted after execution.
        // ==================================================================
        private static void RunBamFullPathForensics()
        {
            string[] baseKeys = {
                @"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings",
                @"SYSTEM\CurrentControlSet\Services\bam\UserSettings"
            };
            var found = new List<string>();
            foreach (string bp in baseKeys)
            {
                try
                {
                    using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(bp))
                    {
                        if (baseKey == null) continue;
                        foreach (string sid in baseKey.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey sk = baseKey.OpenSubKey(sid))
                                {
                                    if (sk == null) continue;
                                    foreach (string vn in sk.GetValueNames())
                                    {
                                        try
                                        {
                                            byte[] data = sk.GetValue(vn) as byte[];
                                            if (data == null) continue;
                                            string txt = DecodeBoth(data);
                                            foreach (Match m in Regex.Matches(txt, @"[A-Za-z]:\\[^\x00-\x1f]{3,300}\.(exe|dll|sys|bat|ps1)", RegexOptions.IgnoreCase))
                                            {
                                                string path = m.Value;
                                                string name = Path.GetFileName(path).ToLowerInvariant();
                                                if (DeepCheatNames.Any(kw => name.Contains(kw)) ||
                                                    ForensicStrongMarkers.Any(mk => name.Contains(mk)))
                                                {
                                                    if (found.Count < 15)
                                                        found.Add(path + (File.Exists(path) ? "" : " [executed then DELETED]"));
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
            if (found.Count > 0)
            {
                Add("Systems", "BAM Full Execution Path Recovery", "Forensic",
                    "Background Activity Moderator recovered full executed paths — including files deleted afterwards: " +
                    string.Join(" | ", found));
            }
        }

        // ==================================================================
        // Kernel driver audit — unsigned drivers, hidden paths, BYOVD.
        // ==================================================================
        private static void RunDriverAudit()
        {
            string outText = RunCapture("driverquery.exe", "/v /fo list");
            if (string.IsNullOrEmpty(outText)) return;

            var unsigned = new List<string>();
            var suspicious = new List<string>();
            int checkedDrivers = 0;
            foreach (string line in outText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Match pm = Regex.Match(line, @"^\s*Path\s+(.+)$", RegexOptions.IgnoreCase);
                if (!pm.Success) continue;
                string path = pm.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(path)) continue;
                string low = path.ToLowerInvariant();
                string name = Path.GetFileName(path).ToLowerInvariant();

                bool badName = DeepCheatNames.Any(kw => name.Contains(kw)) ||
                               VulnDriverNames.Any(v => name.Contains(v)) ||
                               name.Contains("spoof") || name.Contains("mapper");
                if (badName && suspicious.Count < 10)
                    suspicious.Add(path);

                if (Regex.IsMatch(low, @"(\\temp\\|\\downloads\\|\\desktop\\|\\users\\)"))
                {
                    if (suspicious.Count < 10) suspicious.Add(path + " [user-writable path]");
                }

                if (checkedDrivers < 60 && path.EndsWith(".sys", StringComparison.OrdinalIgnoreCase))
                {
                    checkedDrivers++;
                    try
                    {
                        if (File.Exists(path) && !HasValidSignature(path))
                        {
                            string lower = path.ToLowerInvariant();
                            if (!lower.Contains("microsoft") && !lower.Contains("intel") && !lower.Contains("nvidia") &&
                                !lower.Contains("amd") && !lower.Contains("realtek") && !lower.Contains("windows"))
                            {
                                if (unsigned.Count < 10) unsigned.Add(Path.GetFileName(path));
                            }
                        }
                    }
                    catch { }
                }
            }

            if (unsigned.Count > 0)
            {
                Add("Suspicious", "Unsigned Kernel Driver Loaded", "Kernel",
                    "Loaded kernel drivers are unsigned — enables kernel manual-mapping & cheat drivers: " +
                    string.Join(", ", unsigned));
            }
            if (suspicious.Count > 0)
            {
                Add("Suspicious", "Suspicious Kernel Driver Path / Name", "Kernel",
                    "Loaded drivers match cheat / vulnerable-driver names or user-writable paths: " +
                    string.Join(" | ", suspicious));
            }

            // Hidden drivers: kernel services whose binaries live outside the drivers store.
            var outOfStore = new List<string>();
            try
            {
                using (RegistryKey svc = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
                {
                    if (svc != null)
                    {
                        foreach (string s in svc.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey sk = svc.OpenSubKey(s))
                                {
                                    if (sk == null) continue;
                                    object type = sk.GetValue("Type");
                                    if (type == null || Convert.ToInt32(type) != 1) continue; // kernel driver
                                    object ip = sk.GetValue("ImagePath");
                                    if (ip == null) continue;
                                    string ipPath = ip.ToString().ToLowerInvariant();
                                    if (ipPath.Contains("\\temp\\") || ipPath.Contains("\\downloads\\") ||
                                        ipPath.Contains("\\desktop\\") || ipPath.Contains("\\users\\") ||
                                        Regex.IsMatch(ipPath, @"[cdefgh]:\\((?!system32\\drivers|windows\\system32|windows\\system).)*"))
                                    {
                                        if (outOfStore.Count < 10) outOfStore.Add(s + " → " + ip);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
            if (outOfStore.Count > 0)
            {
                Add("Detects", "Driver Loaded Outside Driver Store", "Kernel",
                    "Kernel-mode services registered from non-System32 locations — typical for manual-mapped cheat drivers: " +
                    string.Join(" | ", outOfStore));
            }
        }

        // ==================================================================
        // Persistence — Run keys, startup folder, IFEO, AppInit_DLLs, Winlogon.
        // ==================================================================
        private static void RunStartupPersistence()
        {
            var entries = new List<string>();
            Action<RegistryKey, string> grab = (key, source) =>
            {
                if (key == null) return;
                foreach (string v in key.GetValueNames())
                {
                    try
                    {
                        object o = key.GetValue(v, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        if (o == null) continue;
                        entries.Add(source + " → " + o.ToString());
                    }
                    catch { }
                }
            };

            try { grab(Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"), "HKCU Run"); } catch { }
            try { grab(Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce"), "HKCU RunOnce"); } catch { }
            try { grab(Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"), "HKLM Run"); } catch { }
            try { grab(Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce"), "HKLM RunOnce"); } catch { }
            try { grab(Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run"), "HKLM WOW64 Run"); } catch { }

            try
            {
                using (RegistryKey wl = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Winlogon"))
                {
                    if (wl != null)
                    {
                        foreach (string v in new[] { "Shell", "Userinit" })
                        {
                            object o = wl.GetValue(v);
                            if (o != null) entries.Add("Winlogon " + v + " → " + o);
                        }
                    }
                }
            }
            catch { }

            try
            {
                using (RegistryKey w = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Windows"))
                {
                    if (w != null)
                    {
                        object appInit = w.GetValue("AppInit_DLLs");
                        if (appInit != null && !string.IsNullOrEmpty(appInit.ToString()))
                            entries.Add("AppInit_DLLs → " + appInit);
                    }
                }
            }
            catch { }

            try
            {
                // IFEO debugger hijack — a strong loader / evasione signal.
                using (RegistryKey ifeo = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"))
                {
                    if (ifeo != null)
                    {
                        int checkedSubs = 0;
                        foreach (string sub in ifeo.GetSubKeyNames())
                        {
                            if (++checkedSubs > 400) break;
                            try
                            {
                                using (RegistryKey sk = ifeo.OpenSubKey(sub))
                                {
                                    object dbg = sk == null ? null : sk.GetValue("Debugger");
                                    if (dbg != null && !string.IsNullOrEmpty(dbg.ToString()))
                                        entries.Add("IFEO Debugger → " + sub + " → " + dbg);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }

            // Startup folders.
            try
            {
                string appStart = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs\Startup");
                foreach (string f in SafeEnumerateFiles(appStart, "*", SearchOption.TopDirectoryOnly))
                    entries.Add("StartupFolder → " + f);
            }
            catch { }
            try
            {
                string progStart = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs\StartUp");
                foreach (string f in SafeEnumerateFiles(progStart, "*", SearchOption.TopDirectoryOnly))
                    entries.Add("StartupFolder → " + f);
            }
            catch { }

            var found = new List<string>();
            foreach (string e in entries)
            {
                string low = e.ToLowerInvariant();
                bool marker = DeepCheatNames.Any(kw => low.Contains(kw)) ||
                              ForensicStrongMarkers.Any(mk => low.Contains(mk)) ||
                              Regex.IsMatch(low, @"(\\temp\\|\\downloads\\|\\desktop\\|\\appdata\\)[^\\]*\.(exe|dll|bat|ps1)");
                if (marker && found.Count < 12) found.Add(e);
            }
            if (found.Count > 0)
            {
                Add("Suspicious", "Persistent Startup Entries — Suspicious", "Persistence",
                    "Startup / autorun entries reference suspicious binaries: " + string.Join(" | ", found));
            }
        }

        private static void RunScheduledTaskForensics()
        {
            string outText = RunCapture("schtasks.exe", "/query /fo CSV /v");
            if (string.IsNullOrEmpty(outText)) return;
            var found = new List<string>();
            foreach (string line in outText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string low = line.ToLowerInvariant();
                    bool marker = line.IndexOf("path:", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                  (DeepCheatNames.Any(kw => low.Contains(kw)) ||
                                   ForensicStrongMarkers.Any(mk => low.Contains(mk)) ||
                                   Regex.IsMatch(low, @"(\\temp\\|\\downloads\\|\\desktop\\)[^\\]*\.(exe|bat|ps1)"));
                    if (marker)
                    {
                        if (found.Count < 8)
                        {
                            Match m = Regex.Match(line, @"(?i)cmd|""([^""]+\.(exe|bat|ps1))""");
                            string task = Regex.Match(line, @"(?i)^""([^""]+)""").Groups[1].Value;
                            found.Add(task + " → " + (m.Success ? m.Groups[1].Value : "suspicious task"));
                        }
                    }
                }
                catch { }
            }
            if (found.Count > 0)
            {
                Add("Suspicious", "Scheduled Task Points to Suspicious Binary", "Persistence",
                    "Scheduled tasks execute suspicious paths: " + string.Join(" | ", found));
            }
        }

        // ==================================================================
        // HWID spoofer indicators.
        // ==================================================================
        private static void RunHwSpoofSigns()
        {
            var artifacts = new List<string>();
            try
            {
                string drivers = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers");
                foreach (string f in SafeEnumerateFiles(drivers, "*.sys", SearchOption.TopDirectoryOnly))
                {
                    string n = Path.GetFileName(f).ToLowerInvariant();
                    if (n.Contains("spoof") || n.Contains("hwid") || n.Contains("0xhook") || n.Contains("th1971"))
                        artifacts.Add(f);
                }
            }
            catch { }
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList"))
                {
                    if (k != null)
                    {
                        foreach (string s in k.GetSubKeyNames())
                        {
                            string low = s.ToLowerInvariant();
                            if (low.Contains("spoof") || low.Contains("fake") || low.StartsWith("s-1-5-21-0") )
                            {
                                // Random 0 SID tails are typical break-profile left-overs; only surface "spoof/fake" ones.
                                if (low.Contains("spoof") || low.Contains("fake"))
                                {
                                    artifacts.Add("ProfileList → " + s);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // SMBIOS placeholders — masked by common spoofers.
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber, Version FROM Win32_BIOS"))
                {
                    foreach (var o in searcher.Get())
                    {
                        string serial = Convert.ToString(o["SerialNumber"] ?? "");
                        if (serial.ToLowerInvariant().Contains("to be filled") || serial.ToLowerInvariant().Contains("default string"))
                            artifacts.Add("SMBIOS serial placeholder: " + serial);
                    }
                }
            }
            catch { }

            if (artifacts.Count > 0)
            {
                Add("Suspicious", "HWID Spoofer Indicators", "Spoofer",
                    "Artifacts consistent with hardware-ID spoofing: " + string.Join(" | ", artifacts.Take(8)));
            }
        }

        // ==================================================================
        // DMA (FPGA / PCILeech / KMBOX) external cheat hardware.
        // ==================================================================
        private static void RunDmaDetection()
        {
            var found = new List<string>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_PnPEntity"))
                {
                    foreach (var o in searcher.Get())
                    {
                        string name = Convert.ToString(o["Name"] ?? "");
                        string low = name.ToLowerInvariant();
                        if (DmaSignals.Any(s => low.Contains(s)))
                        {
                            if (found.Count < 8) found.Add(name);
                        }
                    }
                }
            }
            catch { }

            try
            {
                string outText = RunCapture("sc.exe", "query");
                if (!string.IsNullOrEmpty(outText))
                {
                    foreach (string s in DmaSignals)
                    {
                        if (outText.ToLowerInvariant().Contains(s) && found.Count < 8)
                            found.Add("service: " + s);
                    }
                }
            }
            catch { }

            if (found.Count > 0)
            {
                Add("Detects", "DMA / FPGA Cheat Hardware Detected", "Hardware",
                    "Devices or services matching DMA-attack cheat hardware (PCILeech/KMBOX/FPGA): " +
                    string.Join(" | ", found));
            }
        }

        // ==================================================================
        // Anti-forensic state — disabled evidence sources get flagged.
        // ==================================================================
        private static void RunAntiForensicStateChecks()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"))
                {
                    if (k != null)
                    {
                        object pf = k.GetValue("EnablePrefetcher");
                        object sf = k.GetValue("EnableSuperfetch");
                        if ((pf != null && Convert.ToInt32(pf) == 0) || (sf != null && Convert.ToInt32(sf) == 0))
                        {
                            Add("Warnings", "Prefetch Disabled (Anti-Forensic)", "System",
                                "Prefetch/Superfetch is disabled — execution history (including deleted cheats) is not being written.");
                        }
                    }
                }
            }
            catch { }

            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\BitBucket"))
                {
                    if (k != null)
                    {
                        object nuke = k.GetValue("NukeOnDelete");
                        if (nuke != null && Convert.ToInt32(nuke) == 1)
                        {
                            Add("Warnings", "Recycle Bin Purge-on-Delete Enabled", "Anti-Forensic",
                                "Deleted cheats bypass the Recycle Bin entirely (NukeOnDelete=1), hiding removal.");
                        }
                    }
                }
            }
            catch { }

            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"))
                {
                    if (k != null)
                    {
                        object noDocs = k.GetValue("NoRecentDocsHistory");
                        if (noDocs != null && Convert.ToInt32(noDocs) == 1)
                        {
                            Add("Warnings", "Recent Documents History Disabled", "Anti-Forensic",
                                "Recent-document history is disabled — a common cleanup step after cheat use.");
                        }
                    }
                }
            }
            catch { }

            // USN change journal must exist; if missing/disabled, deleted-file
            // recovery on NTFS is impossible and that absence itself is evidence.
            try
            {
                DriveInfo c = new DriveInfo("C");
                if (c.DriveFormat.ToUpperInvariant() == "NTFS")
                {
                    string usn = RunCapture("fsutil.exe", "usn queryjournal C:");
                    if (string.IsNullOrEmpty(usn) || usn.ToLowerInvariant().IndexOf("usn journal id", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        Add("Warnings", "USN Journal Unavailable / Disabled", "Anti-Forensic",
                            "The NTFS USN change journal is missing or unreadable — deleted-cheat forensics rely on it, and its absence after a wipe is itself suspicious.");
                    }
                }
            }
            catch { }
        }

        // ==================================================================
        // Recycle Bin metadata ($I) — recover original deleted paths.
        // ==================================================================
        private static void RunRecycleBinMetaForensics()
        {
            string rb = @"C:\$Recycle.Bin";
            if (!Directory.Exists(rb)) return;
            var found = new List<string>();
            int exeCount = 0;
            foreach (string f in SafeEnumerateFiles(rb, "$I*", SearchOption.AllDirectories))
            {
                try
                {
                    byte[] data = ReadCapped(f, 1024 * 512);
                    if (data == null || data.Length < 28) continue;
                    string path = null;
                    try { path = Encoding.Unicode.GetString(data, 24, data.Length - 24); } catch { }
                    if (string.IsNullOrEmpty(path)) { try { path = Encoding.ASCII.GetString(data); } catch { } }
                    foreach (Match m in Regex.Matches(path, @"[A-Za-z]:\\[^\x00\r\n]{3,250}", RegexOptions.IgnoreCase))
                    {
                        string p = m.Value;
                        string ext = Path.GetExtension(p).ToLowerInvariant();
                        if (ext == ".exe" || ext == ".dll" || ext == ".sys" || ext == ".bat") exeCount++;
                        string name = Path.GetFileName(p).ToLowerInvariant();
                        if ((DeepCheatNames.Any(kw => name.Contains(kw)) ||
                             ForensicStrongMarkers.Any(mk => name.Contains(mk))) && found.Count < 12)
                        {
                            found.Add(p.Trim());
                        }
                        break;
                    }
                }
                catch { }
            }
            if (found.Count > 0)
            {
                Add("Systems", "Recycle Bin — Deleted Cheat Paths Recovered", "Forensic",
                    "Recycle Bin metadata restores original paths of deleted suspicious files: " +
                    string.Join(" | ", found));
            }
            else if (exeCount > 0)
            {
                Add("Systems", "Recycle Bin — Executables Pending Deletion", "Forensic",
                    exeCount + " executables recovered from Recycle Bin metadata (review manually).");
            }
        }

        // ==================================================================
        // Archives in Downloads/Temp — cheat rars/zips are infectious.
        // ==================================================================
        private static void RunArchiveSweep()
        {
            var dirs = new List<string>();
            try
            {
                string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(profile))
                {
                    foreach (string sub in new[] { "Downloads", "Desktop" })
                    {
                        string d = Path.Combine(profile, sub);
                        if (Directory.Exists(d)) dirs.Add(d);
                    }
                }
            }
            catch { }
            try { string t = Path.GetTempPath(); if (Directory.Exists(t)) dirs.Add(t); } catch { }

            var found = new List<string>();
            foreach (string dir in dirs)
            {
                foreach (string f in SafeEnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".zip" && ext != ".rar" && ext != ".7z") continue;
                    string name = Path.GetFileName(f).ToLowerInvariant();
                    bool nameHit = DeepCheatNames.Any(kw => name.Contains(kw)) ||
                                   ForensicStrongMarkers.Any(mk => name.Contains(mk));
                    if (nameHit && found.Count < 12)
                    {
                        found.Add(Path.GetFileName(f));
                        continue;
                    }
                    // Look inside zip archives (headers reveal payload names even when delete-claimed).
                    if (ext == ".zip" && found.Count < 12)
                    {
                        try
                        {
                            using (ZipArchive za = ZipFile.OpenRead(f))
                            {
                                foreach (ZipArchiveEntry e in za.Entries)
                                {
                                    string en = e.Name.ToLowerInvariant();
                                    if (en.EndsWith(".exe") || en.EndsWith(".dll") || en.EndsWith(".bat"))
                                    {
                                        if (DeepCheatNames.Any(kw => en.Contains(kw)) ||
                                            ForensicStrongMarkers.Any(mk => en.Contains(mk)))
                                        {
                                            found.Add(Path.GetFileName(f) + " [$ " + e.Name + " ]");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            if (found.Count > 0)
            {
                Add("Suspicious", "Cheat Archive on Disk", "Files",
                    "Archives whose names or contents match cheat payloads: " + string.Join(", ", found));
            }
        }

        // ==================================================================
        // Plain .exe sweep — fully crawl user areas + fixed drive roots.
        // ==================================================================
        private struct CrawlNode
        {
            public string Dir;
            public int Depth;
        }

        private static void RunExeSweep()
        {
            var exes = CrawlExecutables(5000, 3);
            if (exes.Count == 0) return;

            var hits = new List<Tuple<bool, string>>();
            foreach (string exe in exes)
            {
                try
                {
                    string name = Path.GetFileName(exe).ToLowerInvariant();
                    bool high = DeepCheatNames.Any(kw => name.Contains(kw));
                    bool med = !high && BroadMarkers.Any(mk => name.Contains(mk));
                    if (!high && !med) continue;

                    string detail = exe;
                    try { if (IsUpxPacked(exe)) detail += " [UPX]"; } catch { }
                    try { if (IsAutoItExe(exe)) detail += " [AutoIt]"; } catch { }
                    try { if (IsDotNetAssembly(exe)) detail += " [.NET]"; } catch { }
                    try { if (!HasValidSignature(exe)) detail += " [unsigned]"; } catch { }

                    hits.Add(Tuple.Create(high, detail));
                }
                catch { }
            }

            if (hits.Count > 0)
            {
                var highHits = hits.Where(h => h.Item1).Select(h => h.Item2).Take(10).ToList();
                var medHits = hits.Where(h => !h.Item1).Select(h => h.Item2).Take(12).ToList();

                if (highHits.Count > 0)
                {
                    Add("Detects", "Suspicious Executable on Disk (High Confidence)", "EXE",
                        "Executables matching known cheat families found while sweeping the disk: " +
                        string.Join(" | ", highHits));
                }
                if (medHits.Count > 0)
                {
                    Add("Suspicious", "Suspicious Executable on Disk", "EXE",
                        "Executables matching loader/injector/spoofer/cheat markers found on disk: " +
                        string.Join(" | ", medHits));
                }
                if (hits.Count > 10)
                {
                    Add("Suspicious", "Executable Sweep Totals", "EXE",
                        hits.Count + " matching executables found across user areas and fixed drives (" +
                        exes.Count + " scanned).");
                }
            }
        }

        private static List<string> CrawlExecutables(int maxFiles, int maxDepth)
        {
            var roots = new List<string>();
            try
            {
                string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(profile))
                {
                    foreach (string sub in new[] { "Desktop", "Downloads", "Documents", "Pictures", "Videos", "Music" })
                    {
                        try
                        {
                            string p = Path.Combine(profile, sub);
                            if (Directory.Exists(p)) roots.Add(p);
                        }
                        catch { }
                    }
                }
            }
            catch { }
            try { string t = Path.GetTempPath(); if (Directory.Exists(t)) roots.Add(t); } catch { }
            try
            {
                string pd = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                if (!string.IsNullOrEmpty(pd) && Directory.Exists(pd)) roots.Add(pd);
            }
            catch { }
            try
            {
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (d.DriveType == DriveType.Fixed && d.IsReady && Directory.Exists(d.RootDirectory.FullName))
                            roots.Add(Path.GetFullPath(d.RootDirectory.FullName));
                    }
                    catch { }
                }
            }
            catch { }

            string[] skipNames = {
                "windows", "system32", "program files", "program files (x86)",
                "$recycle.bin", "system volume information", "winsxs", "node_modules",
                "appdata\\local\\packages", "boot", "msocache", "config.msi",
                "recovery", "bind", "lldb", "wer"
            };

            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<CrawlNode>();
            foreach (string r in roots)
            {
                try { queue.Enqueue(new CrawlNode { Dir = r, Depth = 0 }); } catch { }
            }

            int scanned = 0;
            while (queue.Count > 0 && scanned < maxFiles)
            {
                CrawlNode node = queue.Dequeue();
                string dir = node.Dir;
                bool skip = false;
                foreach (string s in skipNames)
                {
                    if (dir.IndexOf("\\" + s + "\\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        dir.EndsWith("\\" + s, StringComparison.OrdinalIgnoreCase))
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip) continue;

                try
                {
                    foreach (string f in Directory.EnumerateFiles(dir))
                    {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        if (ext == ".exe" || ext == ".dll" || ext == ".sys" || ext == ".bat" ||
                            ext == ".cmd" || ext == ".vbs" || ext == ".ps1")
                        {
                            results.Add(f);
                            scanned++;
                            if (scanned >= maxFiles) return results.ToList();
                        }
                    }
                }
                catch { }

                if (node.Depth < maxDepth)
                {
                    try
                    {
                        foreach (string sd in Directory.EnumerateDirectories(dir))
                        {
                            try { queue.Enqueue(new CrawlNode { Dir = sd, Depth = node.Depth + 1 }); } catch { }
                        }
                    }
                    catch { }
                }
            }
            return results.ToList();
        }

        // ==================================================================
        // Low-level helpers.
        // ==================================================================
        private static string DecodeBoth(byte[] data)
        {
            if (data == null || data.Length == 0) return "";
            var sb = new StringBuilder();
            try { sb.Append(Encoding.Unicode.GetString(data)); } catch { }
            sb.Append('\n');
            try { sb.Append(Encoding.ASCII.GetString(data)); } catch { }
            sb.Append('\n');
            try { sb.Append(Encoding.Default.GetString(data)); } catch { }
            return sb.ToString();
        }

        private static byte[] ReadCapped(string path, int cap)
        {
            FileInfo fi = new FileInfo(path);
            long len = fi.Length;
            if (len <= 0) return new byte[0];
            if (len <= cap) return File.ReadAllBytes(path);
            byte[] buf = new byte[cap];
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(Math.Max(0, len - cap), SeekOrigin.Begin);
                int read = 0;
                while (read < cap)
                {
                    int r = fs.Read(buf, read, cap - read);
                    if (r <= 0) break;
                    read += r;
                }
            }
            return buf;
        }

        private static string RunCapture(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = new Process { StartInfo = psi })
                {
                    p.Start();
                    string stdout = p.StandardOutput.ReadToEnd();
                    string stderr = p.StandardError.ReadToEnd();
                    if (!p.WaitForExit(15000))
                    {
                        try { p.Kill(); } catch { }
                    }
                    return stdout + "\n" + stderr;
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
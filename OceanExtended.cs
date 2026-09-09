using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SecurityTools
{
    /// <summary>
    /// Ocean Extended detection suite.
    /// Mirrors the collection vectors used by modern screenshare scanners
    /// (SGC scorer, sero, Ocean official detection systems & DMA/Cleaner
    /// forensics):
    ///   - kernel / boot integrity (kernel debugger, testsigning, EFI audit)
    ///   - DMA FPGA / PCI & USB residue (persists after unplug)
    ///   - USN journal & timestomping forensics
    ///   - event-log service manipulation & cleaner WER traces
    ///   - active network connections resolver, browser profile forensics
    ///   - Defender exclusion / security-service health / quarantine
    ///   - loader-disguise name catalogue, ADS streams, fake signatures
    ///   - Lua content executor patterns & 2026 new-gen cheat families
    ///   - spoofed system process & game memory manual-mapping audit
    /// Every hit uses one of the 5 official categories.
    /// </summary>
    public static partial class OceanScan
    {
        // ------------------------------------------------------------------
        // Kernel mode queries
        // ------------------------------------------------------------------
        [DllImport("ntdll.dll")]
        private static extern int NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, out int ReturnLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindFirstStreamW(string lpFileName, uint InfoLevel, out WIN32_FIND_STREAM_DATA lpFindStreamData, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool FindNextStreamW(IntPtr hFindStream, out WIN32_FIND_STREAM_DATA lpFindStreamData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_STREAM_DATA
        {
            public long StreamSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
            public string cStreamName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        private const uint MEM_COMMIT = 0x1000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint MEM_PRIVATE = 0x20000;
        private const uint MEM_IMAGE = 0x1000000;

        // ------------------------------------------------------------------
        // 2026 new-gen cheat family markers (defaults to be merged at runtime).
        // ------------------------------------------------------------------
        private static readonly string[] ExtendedCheatFamilies = {
            "keyser", "phaze", "vortexmenu", "lumia", "macho", "kola",
            "nightfall", "nixus", "tzproject", "marseille", "zpo", "shawarma",
            "redengine", "2take1", "sixcall", "k-extra", "quantum", "hound",
            "salamand3r", "ech0", "delusion", "serena", "unsignedalert",
            "prax", "deimos", "klay", "jasza", "elysian", "nemesis", "lucis",
            "forix", "glorify", "kestrel", "mischief", "opium", "sober", "celesta",
            "stellar", "astro", "rkmenu", "statement", "tape", "flare", "sourfish",
            "owinock", "lovely", "qubone", "bnkwna", "emphkahk"
        };

        private static readonly string[] LoaderDisguises = {
            "teamviewer_setup_x64.exe", "teamviewer_setup.exe", "hamburger.exe",
            "hamburger (1).exe", "pearl.exe", "ibackupviewer10setup.exe",
            "ocean (2).exe", "zpo.exe", "marseille.exe", "loader_1.exe",
            "gosth.exe", "pulsarasf.exe", "verwaltung.exe", "sushi.exe",
            "microsoft.visualbasic.hpp", "sstremloader.exe", "qr.exe", "uninstall_helper.exe",
            "win.exe", "system_support.exe", "spolszczenie.exe", "palm.exe",
            "smotretel_qfx.exe", "matrix.exe", "noob.exe", "configcortex.exe",
            "googleupdatework.exe", "update_on_schedule.exe"
        };

        private static readonly string[] SecurityServices = {
            "WinDefend", "NisSrv", "SecurityHealthService"
        };

        private static readonly string[] CleanerTools = {
            "bleachbit", "privazer", "systemninja", "ccleaner", "wisecleaner",
            "glary", "ezcleaner", "cleanup", "recyclex"
        };

        private static readonly string[] EvidenceServices = {
            "sysmain", "bam", "dps", "pcasvc", "eventlog", "windefend",
            "nissrv", "securityhealthservice", "diagtrack"
        };

        // ------------------------------------------------------------------
        // Entry point — wired after RunDeepForensics().
        // ------------------------------------------------------------------
        private static void RunExtended()
        {
            Try(() => RunKernelDebuggerCheck());
            Try(() => RunBootConfigIntegrity());
            Try(() => RunFirmwareEfiAudit());
            Try(() => RunVulnerableDriverServices());
            Try(() => RunDmaRegistryResidue());
            Try(() => RunUsnForensics());
            Try(() => RunTimestompDetection());
            Try(() => RunPrefetchDeletionCheck());
            Try(() => RunServiceManipulationEvents());
            Try(() => RunCleanerWerTrace());
            Try(() => RunNetworkConnections());
            Try(() => RunBrowserProfileChecks());
            Try(() => RunDefenderExclusions());
            Try(() => RunSecurityServiceHealth());
            Try(() => RunDefenderQuarantineArtifacts());
            Try(() => RunLoaderDisguiseDetection());
            Try(() => RunAdsExecutionCheck());
            Try(() => RunFakeSignatureCheck());
            Try(() => RunLuaContentScan());
            Try(() => RunNewGenCheatResidue());
            Try(() => RunSpoofedSystemProcess());
            Try(() => RunMemoryRegionAudit());
            Try(() => RunPrefetchCheatArtifacts());
            Try(() => RunUninstallRegistryTraces());
            Try(() => RunUserAreaExecutableSweep());
            Try(() => RunFiveMModifiedFiles());
        }

        // ==================================================================
        // M1 — Kernel debugger & boot integrity.
        // ==================================================================
        private static void RunKernelDebuggerCheck()
        {
            try
            {
                int retLen = 0;
                IntPtr buf = Marshal.AllocHGlobal(4);
                try
                {
                    // SystemKernelDebuggerInformation = 0x23
                    if (NtQuerySystemInformation(0x23, buf, 4, out retLen) == 0)
                    {
                        if (Marshal.ReadByte(buf, 0) != 0)
                        {
                            Add("Systems", "Kernel Debugger Enabled", "Kernel",
                                "A kernel debugger (WinDbg / KDM / kernel-mode tracing) is attached or configured on this machine.");
                        }
                    }
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
            catch { }
        }

        private static void RunBootConfigIntegrity()
        {
            string bootmgr = RunCaptureBounded("bcdedit.exe", "/enum {bootmgr}", 8000, 4096);
            if (string.IsNullOrEmpty(bootmgr)) return;
            string low = bootmgr.ToLowerInvariant();
            var flags = new List<string>();
            if (Regex.IsMatch(low, @"\btestsigning\b\s+yes")) flags.Add("testsigning ON");
            if (Regex.IsMatch(low, @"\bnointegritychecks\b\s+yes")) flags.Add("nointegritychecks ON");
            if (Regex.IsMatch(low, @"\bnovga\b\s+yes")) flags.Add("novga ON");
            if (flags.Count > 0)
            {
                Add("Detects", "Boot Configuration Discrepancy [TYPE V]", "Tamper",
                    "Boot configuration enables developer/test overrides used to load unsigned or forged kernel code: " +
                    string.Join(", ", flags) + " — signature of driver-based cheat loaders.");
            }
        }

        private static void RunFirmwareEfiAudit()
        {
            string fw = RunCaptureBounded("bcdedit.exe", "/enum firmware", 8000, 32768);
            if (string.IsNullOrEmpty(fw)) return;
            var found = new List<string>();
            string[] suspiciousTokens = {
                "loader", "injector", "bootkit", "rootkit", "backdoor",
                "payload", "ghost", "autoit", "shell", "drop"
            };
            foreach (Match m in Regex.Matches(fw, @"(?i)path\s+([^\r\n]{1,160})"))
            {
                try
                {
                    string p = m.Groups[1].Value.Trim();
                    string pl = p.ToLowerInvariant();
                    if (!pl.EndsWith(".efi")) continue;
                    bool safe =
                        pl.Contains(@"\windows\system32\winload") ||
                        pl.Contains(@"\windows\system32\winresume") ||
                        pl.Contains(@"\windows\system32\boot\winload") ||
                        pl.Contains(@"\efi\microsoft\boot\bootmgfw") ||
                        pl.Contains(@"\efi\ubuntu\") ||
                        pl.Contains(@"\efi\grub\") ||
                        pl.Contains("memtest");
                    if (safe) continue;
                    foreach (string t in suspiciousTokens)
                    {
                        if (pl.Contains(t) && found.Count < 4)
                        {
                            found.Add(p + " [" + t + "]");
                            break;
                        }
                    }
                }
                catch { }
            }
            if (found.Count > 0)
            {
                Add("Suspicious", "EFI Entry Without Standard Backing", "Kernel",
                    "Firmware boot entries reference non-standard EFI applications consistent with a bootkit/EFI-resident loader: " +
                    string.Join(" | ", found));
            }
        }

        private static void RunVulnerableDriverServices()
        {
            string sc = RunCaptureBounded("sc.exe", "query", 8000, 8192);
            if (string.IsNullOrEmpty(sc)) return;
            string low = sc.ToLowerInvariant();
            var serviceNames = new List<string>();
            foreach (Match m in Regex.Matches(sc, @"(?im)^SERVICE_NAME:\s*(\S+)"))
                serviceNames.Add(m.Groups[1].Value.ToLowerInvariant());
            var found = serviceNames.Where(n => VulnDriverNames.Any(v => n.Contains(v))).Take(6).ToList();
            StringComparison ic = StringComparison.OrdinalIgnoreCase;
            foreach (string d in VulnDriverNames)
            {
                if (low.IndexOf(d, ic) >= 0 && found.Count < 6 && !found.Contains(d))
                    found.Add(d + " (referenced)");
            }
            if (found.Count > 0)
            {
                Add("Detects", "Vulnerable / Exploit Driver Active", "Kernel",
                    "Running services or configuration references known vulnerable / cheat-map driver images (iqvw64e, gdrv, RTCore64, mhyprot2, dbk64, kdmapper): " +
                    string.Join(" | ", found));
            }
        }

        // ==================================================================
        // M2 — DMA FPGA / PCI / USB persistent residue.
        // ==================================================================
        private static void RunDmaRegistryResidue()
        {
            var xilinx = new List<string>();
            var ftdi = new List<string>();
            try
            {
                using (RegistryKey pci = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI"))
                {
                    if (pci != null)
                    {
                        foreach (string sub in pci.GetSubKeyNames())
                        {
                            string s = sub.ToLowerInvariant();
                            if (s.Contains("ven_10ee"))
                            {
                                string desc = ReadDeviceDesc(pci, sub);
                                if (xilinx.Count < 8) xilinx.Add(sub + (desc.Length > 0 ? " — " + desc : ""));
                            }
                            else if (s.Contains("ven_0403"))
                            {
                                string desc = ReadDeviceDesc(pci, sub);
                                if (ftdi.Count < 8) ftdi.Add(sub + (desc.Length > 0 ? " — " + desc : ""));
                            }
                        }
                    }
                }
            }
            catch { }
            try
            {
                using (RegistryKey usb = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB"))
                {
                    if (usb != null)
                    {
                        foreach (string sub in usb.GetSubKeyNames())
                        {
                            string s = sub.ToLowerInvariant();
                            if (s.Contains("vid_0403") && ftdi.Count < 8)
                            {
                                string desc = ReadDeviceDesc(usb, sub);
                                ftdi.Add(sub + (desc.Length > 0 ? " — " + desc : ""));
                            }
                        }
                    }
                }
            }
            catch { }
            if (xilinx.Count > 0)
            {
                Add("Detects", "DMA FPGA PCI Device Residue (Xilinx 10EE)", "Hardware",
                    "Xilinx FPGA PCI devices are present in the persistent device registry (Enum\\PCI) — external DMA cheat hardware (PCILeech / FPGA / CaptainDMA / ZDMA). " +
                    "These entries SURVIVE the device being unplugged — their presence is the evidence: " + string.Join(" | ", xilinx));
            }
            if (ftdi.Count > 0)
            {
                Add("Suspicious", "FTDI USB Bridge Present (DMA Upload Cable)", "Hardware",
                    "FTDI (VID 0403) devices are present in the persistent device registry — the standard upload bridge for PCIe DMA attacks (also used by legit adapters; manual review advised): " +
                    string.Join(" | ", ftdi));
            }
        }

        private static string ReadDeviceDesc(RegistryKey parent, string sub)
        {
            try
            {
                using (RegistryKey k = parent.OpenSubKey(sub))
                {
                    if (k == null) return "";
                    object v = k.GetValue("DeviceDesc");
                    if (v != null)
                    {
                        string s = Convert.ToString(v).Trim();
                        if (s.Length > 0) return s;
                    }
                    foreach (string c in k.GetSubKeyNames())
                    {
                        string r = ReadDeviceDesc(k, c);
                        if (r.Length > 0) return r;
                    }
                }
            }
            catch { }
            return "";
        }

        // ==================================================================
        // M3 — USN journal, timestomping & Bypass (Prefetch) checks.
        // ==================================================================
        private static void RunUsnForensics()
        {
            string usn = RunCaptureBounded("fsutil.exe", "usn readjournal C: csv", 12000, 200000);
            if (string.IsNullOrEmpty(usn)) return;
            string low = usn.ToLowerInvariant();
            var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(usn, @"(?i)[A-Za-z]:\\[\w\s\-\.\(\)\\]{1,160}\.(exe|dll|sys|bat|cmd)"))
            {
                string p = m.Value.Trim();
                if (p.Length < 5) continue;
                string pl = p.ToLowerInvariant();
                bool strong = DeepCheatNames.Any(kw => pl.Contains(kw));
                bool med = !strong && BroadMarkers.Any(mk => pl.Contains(mk));
                if ((strong || med) && matched.Count < 8) matched.Add(Path.GetFileName(p));
            }
            if (matched.Count > 0)
            {
                bool deletedTraces = low.IndexOf("delete", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     low.IndexOf("rename", StringComparison.OrdinalIgnoreCase) >= 0;
                Add("Systems", "USN Journal — Deleted Cheat File Traces", "Forensic",
                    "The NTFS USN change journal retains records for cheat-family files even after deletion. Deletion/rename traces " +
                    (deletedTraces ? "confirmed" : "visible") + ": " + string.Join(" | ", matched));
            }
        }

        private static void RunTimestompDetection()
        {
            var dirs = new List<string>();
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); } catch { }
            try { dirs.Add(Path.GetTempPath()); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)); } catch { }
            var artifacts = new List<string>();
            DateTime now = DateTime.UtcNow;
            int scanned = 0;
            foreach (string d in dirs)
            {
                if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                foreach (string f in SafeEnumerateFiles(d, "*", SearchOption.TopDirectoryOnly))
                {
                    if (scanned++ > 700) return;
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".exe" && ext != ".dll" && ext != ".bat" && ext != ".cmd") continue;
                    try
                    {
                        DateTime created = File.GetCreationTimeUtc(f);
                        DateTime write = File.GetLastWriteTimeUtc(f);
                        string name = Path.GetFileName(f);
                        if (write > now.AddMinutes(5))
                        {
                            if (artifacts.Count < 8)
                                artifacts.Add(name + " — ITS write " + write.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
                        }
                        else if (created > write && created.Subtract(write).TotalDays > 21)
                        {
                            if (artifacts.Count < 8)
                                artifacts.Add(name + " — created after ITS write (timestomp)");
                        }
                    }
                    catch { }
                }
                if (artifacts.Count >= 8) break;
            }
            if (artifacts.Count > 0)
            {
                Add("Suspicious", "Timestomping Discrepancy (File Time Anomaly)", "Tamper",
                    "Executables in user-writable folders carry impossible timestamps (future write-times or creation-after-write) — a signature of cheat configs bundled/stomped: " +
                    string.Join(" | ", artifacts));
            }
        }

        private static void RunPrefetchDeletionCheck()
        {
            var executed = new List<string>();
            try
            {
                using (RegistryKey bam = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\bam\UserSettings"))
                {
                    if (bam != null)
                    {
                        foreach (string sid in bam.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey k = bam.OpenSubKey(sid))
                                {
                                    if (k == null) continue;
                                    foreach (string v in k.GetValueNames())
                                    {
                                        byte[] arr = k.GetValue(v) as byte[];
                                        if (arr != null && arr.Length > 0) executed.Add(DecodeBoth(arr));
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
            string prefetchDir = "";
            try { prefetchDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"); } catch { }
            var wiped = new List<string>();
            foreach (string blob in executed)
            {
                try
                {
                    foreach (string raw in blob.Split('\n'))
                    {
                        string lp = raw.Trim().Trim('\0');
                        if (lp.Length < 6 || !lp.Contains("\\")) continue;
                        if (!Regex.IsMatch(lp, @"\.(exe|dll|sys)", RegexOptions.IgnoreCase)) continue;
                        string l = lp.ToLowerInvariant();
                        bool strong = DeepCheatNames.Any(kw => l.Contains(kw));
                        bool med = !strong && BroadMarkers.Any(mk => l.Contains(mk));
                        if (!strong && !med) continue;
                        if (File.Exists(lp)) continue;
                        string exeName = Path.GetFileName(lp).ToLowerInvariant();
                        string noExt = Path.GetFileNameWithoutExtension(exeName);
                        if (noExt.Length == 0) continue;
                        string shortStem = noExt.Substring(0, Math.Min(noExt.Length, 12));
                        bool prefetchGone = true;
                        if (Directory.Exists(prefetchDir))
                        {
                            foreach (string p in SafeEnumerateFiles(prefetchDir, "*.pf", SearchOption.TopDirectoryOnly))
                            {
                                if (Path.GetFileName(p).ToLowerInvariant().StartsWith(shortStem))
                                {
                                    prefetchGone = false;
                                    break;
                                }
                            }
                        }
                        if (prefetchGone && wiped.Count < 8) wiped.Add(lp + " (file removed, Prefetch wiped)");
                    }
                }
                catch { }
            }
            if (wiped.Count > 0)
            {
                Add("Warnings", "Bypass Method (Prefetch Deletion)", "Anti-Forensic",
                    "Executed cheat binaries left BAM registry traces but their executable AND Prefetch entries are gone — evidence-source manipulation after use: " +
                    string.Join(" | ", wiped));
            }
        }

        // ==================================================================
        // M4 — Event-log service manipulation & cleaner WER traces.
        // ==================================================================
        private static void RunServiceManipulationEvents()
        {
            DateTime since = DateTime.Now.AddDays(-45);
            List<string> found = ScanEventLog("System", new long[] { 7036 }, since, EvidenceServices, 4);
            if (found.Count > 0)
            {
                Add("Systems", "Service Manipulation — Evidence Sources Restarted/Stopped", "Bypass",
                    "Service Control Manager (Event 7036) recorded state changes for logging/anti-cheat services (BAM, SysMain, DPS, PcaSvc, EventLog, WinDefend, NisSrv) — a common evasion step: " +
                    string.Join(" | ", found));
            }
        }

        private static void RunCleanerWerTrace()
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
                    if (scanned++ > 800) break;
                    try
                    {
                        string txt = File.ReadAllText(f).ToLowerInvariant();
                        foreach (string c in CleanerTools)
                        {
                            if (txt.Contains(c) && found.Count < 6)
                            {
                                Match mod = Regex.Match(txt, @"(?i)LoadedModule[^:]{0,40}:\s*([^\r\n<]+)");
                                found.Add(Path.GetFileName(f) + (mod.Success ? " [" + mod.Groups[1].Value.Trim() + "]" : ""));
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }
            if (found.Count > 0)
            {
                Add("Warnings", "Cleaner Utility Executed (WER Trace)", "Anti-Forensic",
                    "Windows Error Reporting retained crash records referencing system-cleaner tools used to wipe cheat traces: " +
                    string.Join(" | ", found));
            }
        }

        // ==================================================================
        // M5 — Active network connections & browser profile forensics.
        // ==================================================================
        private static void RunNetworkConnections()
        {
            string net = RunCaptureBounded("netstat.exe", "-ano", 10000, 262144);
            if (string.IsNullOrEmpty(net)) return;
            var remoteIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(net, @"(?im)^\s*TCP\s+\S+\s+(\S+)\s+(\S+)\s+\d+"))
            {
                string state = m.Groups[2].Value.ToUpperInvariant();
                if (state != "ESTABLISHED" && state != "SYN_SENT") continue;
                string ip = RemoteIpOnly(m.Groups[1].Value);
                if (!string.IsNullOrEmpty(ip)) remoteIps.Add(ip);
                if (remoteIps.Count >= 14) break;
            }
            var found = new List<string>();
            foreach (string ip in remoteIps)
            {
                string host = ReverseDns(ip);
                if (string.IsNullOrEmpty(host)) continue;
                string hl = host.ToLowerInvariant();
                bool hit = CheatDomains.Any(d => hl.Contains(d)) || ExtendedCheatDomains.Any(d => hl.Contains(d));
                if (hit && found.Count < 6) found.Add(ip + " → " + host);
            }
            if (found.Count > 0)
            {
                Add("Suspicious", "Active Connection to Cheat Infrastructure", "Network",
                    "Established TCP connections resolve to known cheat / bypass infrastructure: " + string.Join(" | ", found));
            }
        }

        private static string RemoteIpOnly(string remote)
        {
            try
            {
                string t = remote;
                int idx = t.LastIndexOf(':');
                if (idx > 0) t = t.Substring(0, idx);
                t = t.TrimStart('[').TrimEnd(']');
                IPAddress ignored;
                if (IPAddress.TryParse(t, out ignored)) return t;
            }
            catch { }
            return null;
        }

        private static string ReverseDns(string ip)
        {
            try
            {
                IPHostEntry e = Dns.GetHostEntry(ip);
                if (e != null && !string.IsNullOrEmpty(e.HostName))
                {
                    IPAddress a;
                    if (IPAddress.TryParse(e.HostName, out a)) return "";
                    return e.HostName;
                }
            }
            catch { }
            return "";
        }

        private static void RunBrowserProfileChecks()
        {
            var roots = new List<string>();
            try { roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data")); } catch { }
            try { roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data")); } catch { }
            try { roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"BraveSoftware\Brave-Browser\User Data")); } catch { }
            try { roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Chromium\User Data")); } catch { }
            try { roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Opera Software\Opera Stable")); } catch { }
            try { roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Vivaldi\User Data")); } catch { }
            var cleared = new List<string>();
            var downloadHit = new List<string>();
            foreach (string ud in roots)
            {
                if (string.IsNullOrEmpty(ud) || !Directory.Exists(ud)) continue;
                bool selfProfile = ud.IndexOf("Opera Stable", StringComparison.OrdinalIgnoreCase) >= 0;
                var profileDirs = SafeEnumerateDirs(ud, "Default", SearchOption.TopDirectoryOnly)
                    .Concat(SafeEnumerateDirs(ud, "Profile*", SearchOption.TopDirectoryOnly)).ToList();
                if (selfProfile) profileDirs.Add(ud);
                foreach (string prof in profileDirs)
                {
                    if (string.Equals(prof, ud, StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        string hist = Path.Combine(prof, "History");
                        bool prefsExist = File.Exists(Path.Combine(prof, "Preferences"));
                        string profName = BrowserProfileName(ud, prof);
                        if (prefsExist && (!File.Exists(hist) || new FileInfo(hist).Length < 4096))
                        {
                            if (cleared.Count < 6) cleared.Add(profName);
                        }
                        else if (File.Exists(hist) && IsWinChromeRoot(ud))
                        {
                            foreach (string kw in DeepCheatNames.Concat(new string[] { "cheat", "injector", "loader", "spoofer", "hwid" }))
                            {
                                if (ScanHistoryForKeyword(hist, kw) && downloadHit.Count < 6)
                                {
                                    downloadHit.Add(kw + " (download entry in " + profName + ")");
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            if (cleared.Count > 0)
            {
                Add("Warnings", "Browser Profile History Cleared", "Anti-Forensic",
                    "Browser profile exists but its history database is missing/tiny — history wiped after cheat-site visits: " +
                    string.Join(" | ", cleared));
            }
            if (downloadHit.Count > 0)
            {
                Add("Suspicious", "Browser Download — Cheat Filename Trace", "Network",
                    "Browser download database (downloads table) retains references to cheat-named files: " +
                    string.Join(" | ", downloadHit));
            }
        }

        private static bool IsWinChromeRoot(string ud)
        {
            return ud.IndexOf("Chrome", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   ud.IndexOf("Edge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   ud.IndexOf("Brave", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   ud.IndexOf("Vivaldi", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BrowserProfileName(string ud, string prof)
        {
            string b = "Browser";
            if (ud.IndexOf("Chrome", StringComparison.OrdinalIgnoreCase) >= 0) b = "Chrome";
            else if (ud.IndexOf("Edge", StringComparison.OrdinalIgnoreCase) >= 0) b = "Edge";
            else if (ud.IndexOf("Brave", StringComparison.OrdinalIgnoreCase) >= 0) b = "Brave";
            else if (ud.IndexOf("Opera", StringComparison.OrdinalIgnoreCase) >= 0) b = "Opera";
            else if (ud.IndexOf("Vivaldi", StringComparison.OrdinalIgnoreCase) >= 0) b = "Vivaldi";
            else if (ud.IndexOf("Chromium", StringComparison.OrdinalIgnoreCase) >= 0) b = "Chromium";
            return b + "\\" + Path.GetFileName(prof);
        }

        private static bool ScanHistoryForKeyword(string file, string keyword)
        {
            try
            {
                FileInfo fi = new FileInfo(file);
                if (fi.Length <= 0 || fi.Length > 12 * 1024 * 1024) return false;
                return DecodeBoth(File.ReadAllBytes(file)).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        // ==================================================================
        // M6 — Defender exclusions, security service health, quarantine.
        // ==================================================================
        private static void RunDefenderExclusions()
        {
            var found = new List<string>();
            string[] risky = { "temp", "downloads", "desktop", "appdata", "users" };
            string[] riskyProc = { "fivem", "gta5", "citizenfx", "cheat", "injector", "loader" };
            try
            {
                foreach (string sub in new[] { "Paths", "Processes", "Extensions" })
                {
                    using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Exclusions\" + sub))
                    {
                        if (k == null) continue;
                        foreach (string v in k.GetValueNames())
                        {
                            string val = k.GetValue(v) as string;
                            if (string.IsNullOrEmpty(val)) continue;
                            string l = val.ToLowerInvariant();
                            bool hit = risky.Any(r => l.Contains(r)) || riskyProc.Any(r => l.Contains(r));
                            if (hit && found.Count < 10) found.Add(sub + ": " + val);
                        }
                    }
                }
            }
            catch { }
            if (found.Count > 0)
            {
                Add("Systems", "Defender Exclusion — User-Writable Path", "Anti-Cheat",
                    "Windows Defender exclusions cover user-writable folders or FiveM/GTA processes — the standard transport for cheat payloads that bypass AV: " +
                    string.Join(" | ", found));
            }
        }

        private static void RunSecurityServiceHealth()
        {
            foreach (string s in SecurityServices)
            {
                if (!ServiceRunning(s))
                {
                    Add("Warnings", "Anti-Cheat Service Stopped", "Anti-Cheat",
                        s + " is not running — the anti-cheat / AV service was disabled or stopped.");
                }
            }
        }

        private static void RunDefenderQuarantineArtifacts()
        {
            try
            {
                string q = @"C:\ProgramData\Microsoft\Windows Defender\Quarantine";
                if (!Directory.Exists(q)) return;
                int count = 0;
                foreach (string f in SafeEnumerateFiles(q, "*", SearchOption.AllDirectories))
                {
                    count++;
                    if (count >= 500) break;
                }
                if (count > 0)
                {
                    Add("Systems", "Defender Quarantine Artifacts Present", "AV",
                        "Defender quarantined " + count + " item(s) — freshly-flagged payloads (frequently cheat loaders before an exclusion was granted).");
                }
            }
            catch { }
        }

        // ==================================================================
        // M7 — Loader disguises, ADS streams, fake signatures.
        // ==================================================================
        private static void RunLoaderDisguiseDetection()
        {
            var dirs = new List<string>();
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }
            try { dirs.Add(Path.GetTempPath()); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")); } catch { }
            var found = new List<string>();
            foreach (string d in dirs)
            {
                if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                foreach (string f in SafeEnumerateFiles(d, "*", SearchOption.TopDirectoryOnly))
                {
                    string n = Path.GetFileName(f).ToLowerInvariant();
                    if (LoaderDisguises.Any(x => string.Equals(x, n, StringComparison.OrdinalIgnoreCase)) && found.Count < 10)
                        found.Add(f);
                }
            }
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    string n = (p.ProcessName + ".exe").ToLowerInvariant();
                    if (LoaderDisguises.Any(x => string.Equals(x, n, StringComparison.OrdinalIgnoreCase)) && found.Count < 10)
                        found.Add(p.ProcessName + ".exe (RUNNING)");
                }
            }
            catch { }
            try
            {
                string pf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                foreach (string f in SafeEnumerateFiles(pf, "*.pf", SearchOption.TopDirectoryOnly))
                {
                    string baseName = Path.GetFileNameWithoutExtension(f);
                    int dash = baseName.LastIndexOf('-');
                    string exe = (dash > 0 ? baseName.Substring(0, dash) : baseName) + ".exe";
                    if (LoaderDisguises.Any(x => string.Equals(x, exe, StringComparison.OrdinalIgnoreCase)) && found.Count < 10)
                        found.Add(f + " (prefetch trace)");
                }
            }
            catch { }
            if (found.Count > 0)
            {
                Add("Detects", "Suspicious Loader Disguise (Known Fake Name)", "Direct",
                    "Files / process names match loaders disguised as innocent installers (TeamViewer / Hamburger / Pearl / IBackupViewer / ZPO / Marseille), the known distribution pattern: " +
                    string.Join(" | ", found));
            }
        }

        private static void RunAdsExecutionCheck()
        {
            var dirs = new List<string>();
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }
            try { dirs.Add(Path.GetTempPath()); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); } catch { }
            var found = new List<string>();
            int scanned = 0;
            foreach (string d in dirs)
            {
                if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                foreach (string f in SafeEnumerateFiles(d, "*", SearchOption.TopDirectoryOnly))
                {
                    if (scanned++ > 400) break;
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".exe" && ext != ".dll" && ext != ".bat" && ext != ".cmd") continue;
                    try
                    {
                        foreach (string stream in EnumerateStreams(f))
                        {
                            if (found.Count < 10) found.Add(Path.GetFileName(f) + ":" + stream);
                        }
                    }
                    catch { }
                }
                if (found.Count >= 10) break;
            }
            if (found.Count > 0)
            {
                Add("Suspicious", "ADS Execution (Alternate Data Stream)", "Injection",
                    "Executables carry non-standard NTFS alternate data streams used to hide payloads from the base file (legit Zone.Identifier excluded): " +
                    string.Join(" | ", found));
            }
        }

        private static IEnumerable<string> EnumerateStreams(string file)
        {
            WIN32_FIND_STREAM_DATA data;
            IntPtr h = FindFirstStreamW(file, 0, out data, 0);
            if (h == new IntPtr(-1) || h == IntPtr.Zero) yield break;
            try
            {
                do
                {
                    string n = data.cStreamName ?? "";
                    if (n == "::$DATA" || n == ":Zone.Identifier" || n == ":Zone.Identifier:$DATA" || n.Length == 0)
                        continue;
                    yield return n;
                } while (FindNextStreamW(h, out data));
            }
            finally { FindClose(h); }
        }

        private static void RunFakeSignatureCheck()
        {
            string system = SafeFolder(Environment.SpecialFolder.Windows);
            string pf = SafeFolder(Environment.SpecialFolder.ProgramFiles);
            string pfx86 = SafeFolder(Environment.SpecialFolder.ProgramFilesX86);
            var found = new List<string>();
            foreach (string exe in FindExecutedSuspiciousFiles())
            {
                try
                {
                    if (!exe.ToLowerInvariant().EndsWith(".exe")) continue;
                    string full = FindInCommonPaths(exe);
                    if (string.IsNullOrEmpty(full) || !File.Exists(full)) continue;
                    string fl = full.ToLowerInvariant();
                    if (StartsEnds(system, fl) || StartsEnds(pf, fl) || StartsEnds(pfx86, fl)) continue;
                    if (!HasValidSignature(full)) continue;
                    string subject = GetCertSubject(full);
                    if (string.IsNullOrEmpty(subject)) continue;
                    bool ms = subject.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              subject.IndexOf("Programming Dedicated Systems", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (ms && found.Count < 6) found.Add(Path.GetFileName(full) + " → " + subject);
                }
                catch { }
            }
            if (found.Count > 0)
            {
                Add("Warnings", "Bypass Method (Fake Emulated Signature)", "Signature",
                    "Executables outside Windows / Program Files are signed as Microsoft — signatures forged with emulated/revoked certs used by loaders to fool review tools: " +
                    string.Join(" | ", found));
            }
        }

        private static string SafeFolder(Environment.SpecialFolder sf)
        {
            try { return (Environment.GetFolderPath(sf) ?? "").ToLowerInvariant(); }
            catch { return ""; }
        }

        private static bool StartsEnds(string prefix, string path)
        {
            return !string.IsNullOrEmpty(prefix) && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCertSubject(string file)
        {
            try
            {
                var c = System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromSignedFile(file);
                return c != null ? (c.Subject ?? "") : "";
            }
            catch { return ""; }
        }

        // ==================================================================
        // M8 — FiveM Lua content exploit & new-gen menu residue.
        // ==================================================================
        private static void RunLuaContentScan()
        {
            var roots = new List<string>();
            try { roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")); } catch { }
            try { roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FiveM Application Data")); } catch { }
            try { roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }
            var executor = new List<string>();
            var brand = new List<string>();
            int scanned = 0;
            string[] brands = {
                "keyser", "phaze", "vortex", "lumia", "macho", "kola",
                "nightfall", "nixus", "tzproject", "hamafiak", "marseille",
                "prax", "deimos", "klay", "jasza", "elysian", "nemesis", "lucis",
                "forix", "glorify", "kestrel", "mischief", "opium", "sober",
                "celesta", "stellar", "astro", "rkmenu", "statement", "flare", "sourfish"
            };
            foreach (string r in roots)
            {
                if (string.IsNullOrEmpty(r) || !Directory.Exists(r)) continue;
                foreach (string f in SafeEnumerateFiles(r, "*.lua", SearchOption.AllDirectories))
                {
                    if (scanned++ > 500) break;
                    try
                    {
                        FileInfo fi = new FileInfo(f);
                        if (fi.Length <= 0 || fi.Length > 1024 * 1024) continue;
                        string text = File.ReadAllText(f, Encoding.UTF8);
                        string low = text.ToLowerInvariant();
                        if ((low.Contains("loadstring") || low.Contains("executecommand") || low.Contains("load(loadstring")) &&
                            executor.Count < 8)
                        {
                            executor.Add(Path.GetFileName(f) + " (" + fi.Length + " B)");
                        }
                        foreach (string b in brands)
                        {
                            if (low.Contains(b) && brand.Count < 8)
                            {
                                brand.Add(b + " in " + Path.GetFileName(f));
                                break;
                            }
                        }
                    }
                    catch { }
                }
                if (scanned >= 500) break;
            }
            if (executor.Count > 0)
            {
                Add("Suspicious", "Lua Content — Executor Pattern", "Lua",
                    "Lua scripts under FiveM cache / user folders contain executor primitives (loadstring / ExecuteCommand) — the base of 2026 FiveM menu cheats: " +
                    string.Join(" | ", executor));
            }
            if (brand.Count > 0)
            {
                Add("Suspicious", "FiveM Cache — New-Gen Menu Residue", "Lua",
                    "Lua content references next-generation cheat menus (Keyser / Phaze / Vortex / Lumia / Macho / Kola / Nightfall / Nixus / TZProject): " +
                    string.Join(" | ", brand));
            }
        }

        private static void RunNewGenCheatResidue()
        {
            var found = new List<string>();
            var dirs = new List<string>();
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }
            try { dirs.Add(Path.GetTempPath()); } catch { }
            foreach (string d in dirs)
            {
                if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                foreach (string f in SafeEnumerateFiles(d, "*", SearchOption.TopDirectoryOnly))
                {
                    string n = Path.GetFileName(f).ToLowerInvariant();
                    if (ExtendedCheatFamilies.Any(k => n.Contains(k)) && found.Count < 10)
                        found.Add(Path.GetFileName(f) + "  (" + d + ")");
                }
            }
            if (found.Count > 0)
            {
                Add("Detects", "New-Generation Cheat Residue", "Direct",
                    "Files named after 2026 FiveM cheat families (Keyser / Phaze / Vortex / Lumia / Macho / Kola / Nightfall / Nixus / Marseille / ZPO): " +
                    string.Join(" | ", found));
            }
        }

        // ==================================================================
        // M9 — Spoofed system process & memory manual-mapping audit.
        // ==================================================================
        private static void RunSpoofedSystemProcess()
        {
            string[] systemNames = { "csrss", "winlogon", "wininit", "services", "smss", "lsass", "conhost", "werfault" };
            string sys32 = SafeFolder(Environment.SpecialFolder.System);
            var found = new List<string>();
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    string n = p.ProcessName.ToLowerInvariant();
                    if (!systemNames.Contains(n)) continue;
                    string path = "";
                    try { path = p.MainModule.FileName; } catch { continue; }
                    if (string.IsNullOrEmpty(path)) continue;
                    if (StartsEnds(sys32, path.ToLowerInvariant())) continue;
                    if (found.Count < 6) found.Add(p.ProcessName + " → " + path);
                }
                catch { }
            }
            if (found.Count > 0)
            {
                Add("Detects", "Cheat Loader Disguised as System Process", "Injection",
                    "Processes impersonating system-critical names but executing from non-System32 paths (loader / hollowing pattern): " +
                    string.Join(" | ", found));
            }
        }

        private static void RunMemoryRegionAudit()
        {
            var found = new List<string>();
            foreach (string g in GameProcesses)
            {
                try
                {
                    foreach (Process p in Process.GetProcessesByName(g))
                    {
                        IntPtr addr = IntPtr.Zero;
                        int regions = 0;
                        long grand = 0;
                        int iterations = 0;
                        MEMORY_BASIC_INFORMATION mbi;
                        while ((long)addr < 0x7FFFFFFF0000L)
                        {
                            if (++iterations > 400000) break;
                            IntPtr res = VirtualQueryEx(p.Handle, addr, out mbi, (IntPtr)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
                            if (res == IntPtr.Zero) break;
                            if (mbi.State == MEM_COMMIT && mbi.Protect == PAGE_EXECUTE_READWRITE &&
                                (mbi.Type == MEM_PRIVATE || mbi.Type == MEM_IMAGE))
                            {
                                long size = (long)mbi.RegionSize;
                                if (size > 0) { regions++; grand += size; }
                            }
                            long step = (long)mbi.RegionSize;
                            if (step <= 0) step = 0x1000;
                            long next = (long)addr + step;
                            if (next <= (long)addr) break;
                            addr = new IntPtr(next);
                        }
                        if (regions >= 10 && grand > 48L * 1024L * 1024L && found.Count < 4)
                        {
                            found.Add(p.ProcessName + " (PID " + p.Id + "): " + regions + " RWX regions, ~" +
                                      (grand / (1024L * 1024L)) + " MB");
                        }
                    }
                }
                catch { }
            }
            if (found.Count > 0)
            {
                Add("Integrity", "Memory Region Audit — Manual Mapping Signature", "RAM",
                    "Game processes expose large sets of executable+write memory regions consistent with manual-mapped cheat payloads (review flagged): " +
                    string.Join(" | ", found));
            }
        }

        // ==================================================================
        // M11 — Prefetch cheat artifacts.
        // ==================================================================
        private static void RunPrefetchCheatArtifacts()
        {
            try
            {
                string prefetchDir = @"C:\Windows\Prefetch";
                if (!Directory.Exists(prefetchDir)) return;
                var markers = new[]
                {
                    "CHEATENGINE", "X64DBG", "OLLYDBG", "PROCESSHACKER", "SYSTEMINFORMER",
                    "INJECTOR", "SPOOFER", "LOADER", "MEGADUMPER", "XENOS", "EXTREME",
                    "KEYSER", "PHAZE", "LUMIA", "MACHO", "KOLA", "VORTEX", "GOSTH", "CHERAX",
                    "REDENGINE", "2TAKE1", "SIXCALL", "HYDRA", "AUTOIT3", "WINDHAWK"
                };
                string[] latest = LatestCheatKeywords;
                var hits = new List<string>();
                foreach (string f in SafeEnumerateFiles(prefetchDir, "*.pf", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        string name = Path.GetFileNameWithoutExtension(f);
                        if (markers.Any(m => name.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0) ||
                            latest.Any(m => name.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            hits.Add(f);
                        }
                    }
                    catch { }
                }
                if (hits.Count > 0)
                {
                    Add("Detects", "Prefetch Cheat Artifacts", "Prefetch",
                        "Windows Prefetch still records execution of cheat / loader / spoofer / debugger binaries: "
                        + string.Join(", ", hits.Take(12)));
                }
            }
            catch { }
        }

        // ==================================================================
        // M12 — Uninstall registry cheat traces + installed software intel.
        // ==================================================================
        private static void RunUninstallRegistryTraces()
        {
            try
            {
                var keys = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };
                var markers = new[]
                {
                    "cheat", "injector", "spoofer", "loader", "keyser", "phaze", "lumia",
                    "macho", "kola", "vortex", "gosth", "cherax", "redengine", "2take1",
                    "sixcall", "k-extra", "quantum", "x64dbg", "processhacker", "ollydbg",
                    "extreme injector", "xenos", "kdmapper", "privacy cleaner"
                };
                var cheatInstalls = new List<string>();
                int installedTotal = 0;
                foreach (string hive in keys)
                {
                    try
                    {
                        using (RegistryKey root = Registry.LocalMachine.OpenSubKey(hive))
                        {
                            if (root == null) continue;
                            foreach (string sub in root.GetSubKeyNames())
                            {
                                try
                                {
                                    using (RegistryKey k = root.OpenSubKey(sub))
                                    {
                                        if (k == null) continue;
                                        string name = (k.GetValue("DisplayName") ?? "").ToString();
                                        string icon = (k.GetValue("DisplayIcon") ?? "").ToString();
                                        installedTotal++;
                                        string hay = name + " " + icon;
                                        foreach (string m in markers)
                                        {
                                            if (hay.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                cheatInstalls.Add(name + "  [" + (string.IsNullOrEmpty(icon) ? "no path" : icon) + "]");
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
                try
                {
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                    {
                        if (k != null)
                        {
                            foreach (string sub in k.GetSubKeyNames())
                            {
                                try
                                {
                                    using (RegistryKey s = k.OpenSubKey(sub))
                                    {
                                        if (s == null) continue;
                                        installedTotal++;
                                        string name = (s.GetValue("DisplayName") ?? "").ToString();
                                        string icon = (s.GetValue("DisplayIcon") ?? "").ToString();
                                        string hay = name + " " + icon;
                                        foreach (string m in markers)
                                        {
                                            if (hay.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                cheatInstalls.Add(name + "  [" + (string.IsNullOrEmpty(icon) ? "no path" : icon) + "]");
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
                if (cheatInstalls.Count > 0)
                {
                    Add("Detects", "Uninstall Registry Cheat Trace", "Registry",
                        "Programs matching cheat / injector / spoofer / debugger names are still registered in the uninstall list (evidence the tool was installed on this PC): "
                        + string.Join(", ", cheatInstalls.Take(10)));
                }
                if (installedTotal > 0)
                {
                    Add("Systems", "Installed Software Inventory", "System",
                        installedTotal + " programs registered in the Windows uninstall list — device build intel for the review (clean installs sit below ~120 typical apps).");
                }
            }
            catch { }
        }

        // ==================================================================
        // M13 — Plain executables on user areas (recent), added to Detects.
        // ==================================================================
        private static void RunUserAreaExecutableSweep()
        {
            try
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var recentExes = new List<string>();
                var dirs = new List<string>();
                DateTime cutoff = DateTime.Now.AddDays(-45);
                try
                {
                    dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
                    dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                    dirs.Add(Path.GetTempPath());
                    dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
                }
                catch { }
                foreach (string dir in dirs)
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                    foreach (string f in SafeEnumerateFiles(dir, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            if (!seen.Add(f)) continue;
                            if (System.IO.File.GetLastWriteTime(f) >= cutoff)
                            {
                                recentExes.Add(f + "  (written " + System.IO.File.GetLastWriteTime(f).ToString("MM-dd HH:mm") + ")");
                            }
                        }
                        catch { }
                        if (recentExes.Count >= 25) break;
                    }
                    if (recentExes.Count >= 25) break;
                }
                if (recentExes.Count > 0)
                {
                    Add("Detects", "Recent Executables on User Areas", "Files",
                        "Plain executables recently written to Downloads / Desktop / Temp / Documents are listed for manual review — legit screenshare exit is ~0 on a clean build: "
                        + string.Join(" | ", recentExes));
                }
            }
            catch { }
        }

        // ==================================================================
        // M14 — FiveM modified / cheat files under the FiveM profile.
        // ==================================================================
        private static void RunFiveMModifiedFiles()
        {
            try
            {
                string fivem = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM");
                if (!Directory.Exists(fivem)) return;
                var cheatModules = new List<string>();
                var modified = new List<string>();
                DateTime cutoff = DateTime.Now.AddDays(-10);
                var markers = new[]
                {
                    "cheat", "injector", "inject", "spoofer", "spoof", "keyser", "phaze", "lumia",
                    "macho", "kola", "vortex", "gosth", "cherax", "redengine", "2take1", "sixcall",
                    "extreme", "xenos", "kdmapper", "k-extra", "quantum", "hound", "bypass",
                    "prax", "deimos", "klay", "jasza", "elysian", "nemesis", "lucis", "forix",
                    "glorify", "kestrel", "mischief", "opium", "sober", "celesta", "stellar",
                    "astro", "rkmenu", "statement", "flare", "sourfish"
                };
                foreach (string f in SafeEnumerateFiles(fivem, "*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        string ext = Path.GetExtension(f);
                        string name = Path.GetFileNameWithoutExtension(f);
                        bool dllExe = ext.Equals(".dll", StringComparison.OrdinalIgnoreCase)
                                   || ext.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                                   || ext.Equals(".asi", StringComparison.OrdinalIgnoreCase)
                                   || ext.Equals(".sys", StringComparison.OrdinalIgnoreCase)
                                   || ext.Equals(".lua", StringComparison.OrdinalIgnoreCase)
                                   || ext.Equals(".luac", StringComparison.OrdinalIgnoreCase);
                        if (!dllExe) continue;
                        bool isCache = f.IndexOf("cache", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool isCitizenCore = f.IndexOf(@"\citizen\", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool isLegitFive = name.EndsWith("-five", StringComparison.OrdinalIgnoreCase)
                                        || name.StartsWith("api-ms-", StringComparison.OrdinalIgnoreCase)
                                        || name.Equals("natives_loader", StringComparison.OrdinalIgnoreCase)
                                        || name.Equals("natives_server", StringComparison.OrdinalIgnoreCase)
                                        || name.StartsWith("citizen-", StringComparison.OrdinalIgnoreCase);
                        if (isLegitFive || isCitizenCore) continue;
                        if (markers.Any(m => name.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0) && !isCache)
                        {
                            cheatModules.Add(f);
                        }
                        else if (System.IO.File.GetLastWriteTime(f) >= cutoff && !IsSystemPath(f))
                        {
                            if (f.IndexOf("cache", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                f.IndexOf("mods", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                f.IndexOf("plugins", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                f.IndexOf("citizen", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                long size;
                                try { size = new System.IO.FileInfo(f).Length; } catch { size = 0; }
                                modified.Add(f + "  (" + size + " B, written " + System.IO.File.GetLastWriteTime(f).ToString("MM-dd HH:mm") + ")");
                            }
                        }
                    }
                    catch { }
                    if (cheatModules.Count >= 12 && modified.Count >= 12) break;
                }
                if (cheatModules.Count > 0)
                {
                    Add("Detects", "FiveM Cheat Module on Profile", "DLL",
                        "Executable / script files matching cheat, injector, spoofer or known-menu names were found under the FiveM profile: "
                        + string.Join(" | ", cheatModules.Take(10)));
                }
                if (modified.Count > 0)
                {
                    Add("Suspicious", "FiveM Modified Files (recent)", "Mods",
                        "Files under the FiveM cache / mods / plugins / citizen folders were modified in the last 10 days — legitimate base installs stay untouched: "
                        + string.Join(" | ", modified.Take(12)));
                }
            }
            catch { }
        }

        // ==================================================================
        // M10 — Weighted verdict scoring (0-100).
        // ==================================================================
        private static readonly Dictionary<string, int> BadgeWeights = new Dictionary<string, int>
        {
            { "Direct", 45 }, { "Injection", 35 }, { "Kernel", 35 }, { "Hardware", 40 },
            { "Hook", 30 }, { "Ban", 35 }, { "Bypass", 25 }, { "DLL", 30 }, { "Lua", 30 },
            { "EXE", 25 }, { "Render", 20 }, { "Module", 20 }, { "Files", 15 }, { "Prefetch", 12 },
            { "Forensic", 15 }, { "Registry", 15 }, { "BAM", 15 }, { "Network", 15 },
            { "System", 10 }, { "Log", 10 }, { "Anti-Forensic", 18 }, { "Cleaner", 12 },
            { "Tamper", 25 }, { "Spoofer", 35 }, { "Account", 10 }, { "Config", 10 },
            { "Integrity", 10 }, { "Signature", 15 }, { "VirusTotal", 20 }, { "Manual", 15 },
            { "AV", 15 }, { "Script", 15 }, { "Persistence", 15 }, { "USB", 15 }, { "Tool", 15 },
            { "Plugin", 15 }, { "Mods", 15 }, { "Crash", 10 }, { "Anti-Cheat", 18 },
            { "RAM", 20 }, { "AI", 10 }, { "RUIN", 20 }
        };

        public static int ComputeScore(List<Hit> hits, out string verdict, out List<string> reasons)
        {
            reasons = new List<string>();
            if (hits == null || hits.Count == 0)
            {
                verdict = "CLEAN";
                return 0;
            }
            int score = 0;
            foreach (Hit h in hits)
            {
                int w;
                if (h.Badge == null || !BadgeWeights.TryGetValue(h.Badge, out w)) w = 10;
                score += w;
                if (w >= 30 && reasons.Count < 8 && !string.IsNullOrEmpty(h.Name)) reasons.Add(h.Name);
            }
            score = Math.Min(100, score);
            // Anything was found -> NEVER report CLEAN. Lowest possible honest
            // verdict when hits exist is REVIEW.
            if (score >= 70) verdict = "CHEAT";
            else if (score >= 35) verdict = "SUSPICIOUS";
            else verdict = "REVIEW";
            return score;
        }

        // ==================================================================
        // Bounded external tool capture (avoids hanging on huge outputs).
        // ==================================================================
        private static string RunCaptureBounded(string exe, string args, int maxMs, int maxChars)
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
                    var sb = new StringBuilder(maxChars);
                    DataReceivedEventHandler handler = (s, ev) =>
                    {
                        if (!string.IsNullOrEmpty(ev.Data))
                        {
                            if (sb.Length < maxChars) sb.Append(ev.Data).Append('\n');
                        }
                    };
                    p.OutputDataReceived += handler;
                    p.ErrorDataReceived += handler;
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    if (!p.WaitForExit(maxMs))
                    {
                        try { p.Kill(); } catch { }
                        p.WaitForExit(2000);
                    }
                    p.CancelOutputRead();
                    return sb.ToString();
                }
            }
            catch { return ""; }
        }
    }
}
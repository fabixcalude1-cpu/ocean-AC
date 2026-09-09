using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace DuJob
{
    internal class Window
    {
        public static void StartCheckingAndClosing()
        {
            Thread thread = new Thread(CheckAndCloseWindows);
            thread.Start();
        }

        private static void CheckAndCloseWindows()
        {
            string[] windowTitlesToFind =
            {
                "dnspy", "Process Explorer", "Process Explorer", "ida64", "x64dbg", "x96dbg", "x32dbg", "x96dbg", "ILSpy",
                "JetBrains", "windbg", "gdb", "Scylla_x64", "Scylla_x86", "SharpDevelop", "monodevelop", "OllyDbg",
                "ida", "MemoryProfiler", "ANTS Performance Profiler", "JustTrace", "BugAid", "Reflector", "dotMemory","Everything","ida","ida64",
                "cheatengine-x86_64","cheatengine-i386","HTTPDebuggerSvc","HTTPDebuggerUI","Scylla_x64","Scylla_x86","MegaDumper","ExtremeDumper","de4dot","de4dot64",
                "de4dot-cex","dnSpy-x64","dnSpy-x86","dnSpy","InjectionDebugger","Dumper","DebugTool","ReverseEngineer","CodeInspector","MemoryDump","BinaryAnalyzer","ReverseDebugger",
                "CrashDumpAnalyzer","CodeExtractor","ByteScanner","DebugMonitor","DecompilerTool","APIHookDetector","PatchAnalyzer","MemorySpy","CodeTracer","BinaryInspector","DebugAssistant",
                "ReverseAnalyzer","PatchExtractor","CrashDumpReader","CodeReconstructor","ByteInspector","MalwareDebugger","HookMonitor","DecompilerToolset","MemoryProfiler","BinaryReconstructor",
                "DebuggingWizard","BinaryInspectorPro","DebugAssistantPro","OllyDbg","PatchCraft","ControlFlowExplorer","SignatureScanner","ReverseAnalyzer","AssemblyInspector","DecompilerX","CodeReveal",
                "ReversoMaster","InstructionTracer","DebugAssist","FlowAnalyzer","MemoryWatcher","BreakpointDebugger","VariableInspector","ExecutionTracker","RuntimeDebugger","ByteScanPro","MemorySnapshotter",
                "DumpExplorer","DataHarvest","BinaryDumpTool","ByteExtractor","CodeSnapshot","MemoryDumpPro","DataExtract","DumpMaster","OllyDbg","ControlFlowInspector","FunctionExtractorX","SignatureSearcher",
                "ReverseCodeExplorer","AssemblyAnalyzer","DecompilerPro","BytePatchMaster","CodeRevealX","DebugFlowMaster","BreakpointAssistant","VariableDebugger","ExecutionTrackerPro","RuntimeInspector","GorgonIDA"
                ,"HadesDebugger","PhoenixDisassembler","ElysiumAnalyzer","CerberusCracker","HydraReconstructor","ChimeraPatchKit","MedusaProfiler","NemeanDecryptor","SphinxCodeExplorer","BasiliskReconstructor","ManticoreAnalyzer",
                "LeviathanDebugger","GriffinDecryptor","HydraPatchEngine","PhoenixInjector","SphinxReverser","ChimeraTracer","CerberusDisassembler","GorgonCodeAnalyzer","LeviathanTracer","GriffinDisassembler","ChimeraReconstructor",
                "BasiliskDecryptor","SphinxCodeInjector","CerberusAnalyzer","GorgonReverser","HydraDebugger","de4dotUnpacker","KrakenDebugger","OuroborosDecryptor","SerpentDisassembler","WyvernCodeAnalyzer","FunctionExtractor","De4DotPro",
                "Deobfuscator","Net-Deobfuscator","Everything","procexp64","procexp","System Informer","Process Hacker","procexp64a","de4dot64", "HTTPDEBUGGER", "SystemInformer", "http debugger", "debugger", "disassembler", "decompiler", "fiddler",
                "wireshark", "analysis tool", "Network traffic dump tool", "petool", "Wireshark packet sniffer", "Part of Sysinternals Suite", "Network Analyzer", "HxD", "Timeline Explorer", "WinPrefetchView", "ShellBagsView", "Administrator: DRE-Files",
                "Echo User Assist Viewer", "Echo BAM Log Viewer", "Echo String Scanner", "Easy Journal Viewer", "JournalTrace", "Registry Editor", "Everything", "DIE", "dumpcap", "dnSpy", "dnSpy-x86", "cheatengine-x86_64", "cheatengine-x86_64",
                "Procmon", "Procmon64", "Procmon64a", "DotNetDataCollector32", "DotNetDataCollector64", "Filergabber", "FileGrab", "Brocesshacker", "nigga", "white", "m46asp", "UD", "CFF Explorer", "ollydbg", "ida", "ida64", "idag", "idag64", "idaw",
                "idaw64", "idaq", "idaq64", "idau", "idau64", "scylla", "scylla_x64", "scylla_x86", "protection_id", "windbg", "reshacker", "ImportREC", "IMMUNITYDEBUGGER", "MegaDumper", "dotPeek32", "dotPeek64", "dnSpy.Console", "ILSpy", "Beamer x64 [Elevated]",
                "Beamer","Detect is easy", "Die"
              };

            while (true)
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    try
                    {
                        foreach (string windowTitle in windowTitlesToFind)
                        {
                            if (process.MainWindowTitle.ToLower().Contains(windowTitle.ToLower()))
                            {
                                process.CloseMainWindow();

                                if (!process.HasExited)
                                {
                                    process.Kill();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ocorreu uma exceção: {ex.Message}");
                    }
                }

                Thread.Sleep(1000);
            }
        }
    }
}
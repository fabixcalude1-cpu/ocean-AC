using DiscordMessenger;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Management;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Linq;
using Microsoft.VisualBasic.ApplicationServices;
using System.Xml.Linq;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using static Siticone.Desktop.UI.Native.WinApi;
using static System.Net.WebRequestMethods;
using System.Net.Sockets;
using DuJob;
using System.Runtime.Remoting.Contexts;
using SecurityTools;
using System.Drawing.Drawing2D;
using System.Reflection.Emit;
using TheArtOfDev.HtmlRenderer.Adapters;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Timers;

namespace Dujob
{

    public partial class Form1 : Form
    {
        private string generatedPin;
        // API base URL of the Ocean AC web dashboard (render.com / localhost during development).
        private const string ApiBase = "https://ocean-ac.onrender.com";
        private string Result = "https://discord.com/api/webhooks/1278864715145281557/42SQ9VZFl0s5nf410-PjhK0cLdxoZhVxiLeAhhktKfxen5B8yCx6o_GYI92fL8uj40rv";
        private Random random = new Random();
        private bool isDragging;
        private bool scanInProgress;
        private int progressTicks;
        private int frame;
        private bool countdownRunning;
        private double countdownRemaining;
        private bool closePhase;
        private int closeTick;
        private List<Particle> particles = new List<Particle>();
        private Point lastCursorPosition;
        private float hoverClose = 0f;
        private float hoverMin = 0f;
        private float hoverTextBox = 0f;
        private bool isMouseOverClose = false;
        private bool isMouseOverMin = false;
        private bool isMouseOverTextBox = false;
        private PointF currentMousePos = new PointF(-100, -100);
        private PointF smoothMousePos = new PointF(-100, -100);
        private List<CursorTrail> cursorTrails = new List<CursorTrail>();
        private List<ClickRipple> clickRipples = new List<ClickRipple>();
        private string foundLsassInfo = "";
        private string foundDpsInfo = "";
        private string foundDnsCacheInfo = "";
        private string foundSysmainInfo = "";
        private string foundDiagTrackInfo = "";
        private string foundExplorerInfo = "";
        private string foundPcaSvcInfo = "";
        private string foundHistoryInfo = "";
        private string pcaclientInfo = "";
        private string additionalInfo = "";
        private string _oceanReport = "";
        private List<SecurityTools.OceanScan.Hit> _oceanHits = new List<SecurityTools.OceanScan.Hit>();
        private bool _scanEngineFailed = false;
        private int _oceanScore = 0;
        private string _oceanVerdict = "CLEAN";
        private List<string> _oceanReasons = new List<string>();
        private string _fiveMInfo = "";
        private string usedKeyId = "";
        private string usedPin = "";
        private Newtonsoft.Json.Linq.JObject _remoteCfg = null;
        private bool _remoteStrict = false;

        public Form1()
        {
            InitializeComponent();
            InitializeParticles();
            this.Text = "Ocean ac";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(700, 394);
            timer1.Interval = 16;
            timer1.Start();
            timer1.Tick += timer1_Tick;
            timer2.Interval = 16;
            timer2.Tick += timer2_Tick;
            DoubleBuffered = true;
            this.MouseDown += Form1_MouseDown;
            this.MouseMove += Form1_MouseMove;
            this.MouseUp += Form1_MouseUp;
            siticoneButton2.Click += siticoneButton2_Click;
            siticoneVProgressBar1.Visible = false;

            AttachControlMouseEvents(this);
            AttachControlMouseEvents(siticoneButton1);
            AttachControlMouseEvents(siticoneButton2);
            AttachControlMouseEvents(siticoneTextBox2);
            AttachControlMouseEvents(siticonePictureBox1);
            AttachControlMouseEvents(label2);
        }

        private void AttachControlMouseEvents(Control ctrl)
        {
            if (ctrl == null) return;
            ctrl.MouseMove += (s, e) =>
            {
                Point screenPt = ctrl.PointToScreen(e.Location);
                Point clientPt = this.PointToClient(screenPt);
                currentMousePos = clientPt;
                if (isDragging && ctrl != this)
                {
                    this.Left += screenPt.X - lastCursorPosition.X;
                    this.Top += screenPt.Y - lastCursorPosition.Y;
                    lastCursorPosition = screenPt;
                }
            };
            ctrl.MouseEnter += (s, e) =>
            {
                if (ctrl == siticoneButton1) isMouseOverClose = true;
                else if (ctrl == siticoneButton2) isMouseOverMin = true;
                else if (ctrl == siticoneTextBox2) isMouseOverTextBox = true;
            };
            ctrl.MouseLeave += (s, e) =>
            {
                if (ctrl == siticoneButton1) isMouseOverClose = false;
                else if (ctrl == siticoneButton2) isMouseOverMin = false;
                else if (ctrl == siticoneTextBox2) isMouseOverTextBox = false;
            };
            ctrl.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    Point screenPt = ctrl.PointToScreen(e.Location);
                    Point clientPt = this.PointToClient(screenPt);
                    clickRipples.Add(new ClickRipple { Center = clientPt, Radius = 3f, MaxRadius = 55f, Alpha = 1f });
                    if (ctrl == siticonePictureBox1 || ctrl == label2)
                    {
                        isDragging = true;
                        lastCursorPosition = screenPt;
                    }
                }
            };
            ctrl.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) isDragging = false;
            };
        }

        private void InitializeParticles()
        {
            int numParticles = 22;
            for (int i = 0; i < numParticles; i++)
            {
                double angle = random.NextDouble() * 2 * Math.PI;
                double speed = random.NextDouble() * 1.5 + 0.5;
                bool accent = i % 4 == 0;
                Color color = accent ? Color.FromArgb(120, 200, 162, 248) : Color.FromArgb(70, 70, 50, 160);
                particles.Add(new Particle()
                {
                    Position = new PointF(random.Next(0, ClientSize.Width), random.Next(0, ClientSize.Height)),
                    Velocity = new PointF((float)(Math.Cos(angle) * speed), (float)(Math.Sin(angle) * speed)),
                    Radius = random.Next(2, 4),
                    Color = color,
                    Brush = new SolidBrush(color)
                });
            }
        }

        private void AnimateParticles()
        {
            foreach (var particle in particles)
            {
                particle.Position = new PointF(particle.Position.X + particle.Velocity.X, particle.Position.Y + particle.Velocity.Y);
                if (particle.Position.X < 0) particle.Position = new PointF(ClientSize.Width, particle.Position.Y);
                if (particle.Position.X > ClientSize.Width) particle.Position = new PointF(0, particle.Position.Y);
                if (particle.Position.Y < 0) particle.Position = new PointF(particle.Position.X, ClientSize.Height);
                if (particle.Position.Y > ClientSize.Height) particle.Position = new PointF(particle.Position.X, 0);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_bgCache == null ||
                _bgCache.Width != ClientSize.Width ||
                _bgCache.Height != ClientSize.Height)
            {
                RebuildBackgroundCache();
            }
            e.Graphics.DrawImageUnscaled(_bgCache, 0, 0);
        }

        private Bitmap _bgCache;

        private void RebuildBackgroundCache()
        {
            try
            {
                int w = Math.Max(1, this.ClientSize.Width);
                int h = Math.Max(1, this.ClientSize.Height);
                Bitmap bmp = new Bitmap(w, h);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = new Rectangle(0, 0, w, h);

                    // Deep Purple and Pitch Black tech gradient background
                    using (LinearGradientBrush brush = new LinearGradientBrush(r,
                        Color.FromArgb(255, 14, 8, 28),
                        Color.FromArgb(255, 4, 2, 8),
                        LinearGradientMode.ForwardDiagonal))
                    {
                        ColorBlend cb = new ColorBlend(4);
                        cb.Colors = new Color[] {
                            Color.FromArgb(255, 12, 6, 24),
                            Color.FromArgb(255, 45, 16, 75),
                            Color.FromArgb(255, 16, 8, 35),
                            Color.FromArgb(255, 3, 1, 6)
                        };
                        cb.Positions = new float[] { 0f, 0.42f, 0.78f, 1f };
                        brush.InterpolationColors = cb;
                        g.FillRectangle(brush, r);
                    }

                    // Tech grid lines in purple/fuchsia
                    using (Pen gridPen = new Pen(Color.FromArgb(24, 75, 20, 140), 1f))
                    {
                        for (int x = 0; x <= w; x += 32) g.DrawLine(gridPen, x, 0, x, h);
                        for (int y = 0; y <= h; y += 32) g.DrawLine(gridPen, 0, y, w, y);
                    }

                    // Top/bottom cyber accent glow lines
                    using (LinearGradientBrush barBrush = new LinearGradientBrush(
                        new Rectangle(0, 0, w, 2),
                        Color.FromArgb(0, 160, 85, 240),
                        Color.FromArgb(180, 160, 120, 240),
                        LinearGradientMode.Horizontal))
                    {
                        ColorBlend barBlend = new ColorBlend(3);
                        barBlend.Colors = new Color[] {
                            Color.FromArgb(0, 160, 85, 240),
                            Color.FromArgb(180, 160, 120, 240),
                            Color.FromArgb(0, 160, 85, 240)
                        };
                        barBlend.Positions = new float[] { 0f, 0.5f, 1f };
                        barBrush.InterpolationColors = barBlend;
                        using (Pen barPen = new Pen(barBrush, 1.5f))
                        {
                            g.DrawLine(barPen, 0, 1, w, 1);
                            g.DrawLine(barPen, 0, h - 2, w, h - 2);
                        }
                    }

                    // Subtle border outline
                    using (Pen borderPen = new Pen(Color.FromArgb(60, 168, 85, 247), 1f))
                    {
                        g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
                    }
                }

                Bitmap old = _bgCache;
                _bgCache = bmp;
                if (old != null) old.Dispose();
            }
            catch
            {
                try
                {
                    if (_bgCache == null)
                    {
                        _bgCache = new Bitmap(Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
                        using (Graphics bg = Graphics.FromImage(_bgCache))
                        using (SolidBrush fb = new SolidBrush(Color.FromArgb(255, 3, 10, 26)))
                            bg.FillRectangle(fb, 0, 0, _bgCache.Width, _bgCache.Height);
                    }
                }
                catch { }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background floating sparks
            foreach (var particle in particles)
            {
                using (SolidBrush pb = new SolidBrush(particle.Color))
                {
                    g.FillRectangle(pb, particle.Position.X - 1.5f, particle.Position.Y - 1.5f, 3f, 3f);
                }
            }

            // --- CURSOR ANIMATIONS ---
            // 1. Dynamic Cursor Spotlight (Purple glow halo smoothly following cursor)
            if (smoothMousePos.X >= 0 && smoothMousePos.Y >= 0)
            {
                float glowRadius = 75f;
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(smoothMousePos.X - glowRadius, smoothMousePos.Y - glowRadius, glowRadius * 2f, glowRadius * 2f);
                    using (PathGradientBrush pgb = new PathGradientBrush(gp))
                    {
                        pgb.CenterColor = Color.FromArgb(32, 200, 150, 240);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 2, 6, 20) };
                        g.FillPath(pgb, gp);
                    }
                }
            }

                    // 2. Cursor Trail (Smooth glowing fading line and spark dots)
            for (int i = 0; i < cursorTrails.Count; i++)
            {
                var ct = cursorTrails[i];
                int alpha = (int)(Math.Max(0f, Math.Min(1f, ct.Alpha)) * 140);
                if (alpha > 0 && ct.Size > 0)
                {
                    using (SolidBrush trailBrush = new SolidBrush(Color.FromArgb(alpha, 168, 85, 247)))
                    {
                        g.FillEllipse(trailBrush, ct.Position.X - ct.Size / 2f, ct.Position.Y - ct.Size / 2f, ct.Size, ct.Size);
                    }
                    if (i < cursorTrails.Count - 1)
                    {
                        var next = cursorTrails[i + 1];
                        using (Pen trailPen = new Pen(Color.FromArgb(alpha / 2, 232, 121, 249), Math.Max(1f, ct.Size / 3f)))
                        {
                            g.DrawLine(trailPen, ct.Position, next.Position);
                        }
                    }
                }
            }

            // 3. Click Ripples (Cyber pulse shockwaves)
            foreach (var cr in clickRipples)
            {
                int alpha = (int)(Math.Max(0f, Math.Min(1f, cr.Alpha)) * 190);
                if (alpha > 0)
                {
                    using (Pen ripplePen = new Pen(Color.FromArgb(alpha, 168, 85, 247), 1.5f))
                    {
                        g.DrawEllipse(ripplePen, cr.Center.X - cr.Radius, cr.Center.Y - cr.Radius, cr.Radius * 2f, cr.Radius * 2f);
                    }
                }
            }

            // --- HOVER ANIMATIONS OVER CONTROLS ---
            // License text box hover/focus glowing outline
            if (hoverTextBox > 0.01f)
            {
                int alpha = (int)(hoverTextBox * 110);
                var tb = siticoneTextBox2.Bounds;
                using (Pen glowPen = new Pen(Color.FromArgb(alpha, 168, 85, 247), 2f))
                {
                    g.DrawRectangle(glowPen, tb.X - 2, tb.Y - 2, tb.Width + 4, tb.Height + 4);
                }
            }

            // Close button hover glow
            if (hoverClose > 0.01f)
            {
                int alpha = (int)(hoverClose * 130);
                var b = siticoneButton1.Bounds;
                using (Pen closeGlowPen = new Pen(Color.FromArgb(alpha, 239, 68, 68), 2f))
                {
                    g.DrawRectangle(closeGlowPen, b.X - 2, b.Y - 2, b.Width + 4, b.Height + 4);
                }
            }

            // Minimize button hover glow
            if (hoverMin > 0.01f)
            {
                int alpha = (int)(hoverMin * 130);
                var b = siticoneButton2.Bounds;
                using (Pen minGlowPen = new Pen(Color.FromArgb(alpha, 168, 85, 247), 2f))
                {
                    g.DrawRectangle(minGlowPen, b.X - 2, b.Y - 2, b.Width + 4, b.Height + 4);
                }
            }

            // Traveling light on the very top edge
            float lightX = (frame * 1.5f) % Math.Max(120f, ClientSize.Width + 60f) - 30f;
            using (Pen trail = new Pen(Color.FromArgb(50, 200, 120, 248), 2f))
            {
                g.DrawLine(trail, lightX - 36f, 2f, lightX, 2f);
            }
            using (SolidBrush core = new SolidBrush(Color.FromArgb(230, 200, 245, 255)))
            {
                g.FillEllipse(core, lightX - 2.5f, 0.5f, 5f, 5f);
            }

            // Cyber corner brackets
            int inset = 8, len = 16;
            int a = scanInProgress ? 220 : 85;
            using (Pen corner = new Pen(Color.FromArgb(a, 168, 85, 247), 1.6f))
            {
                corner.StartCap = corner.EndCap = LineCap.Square;
                int w = ClientSize.Width, h = ClientSize.Height;
                g.DrawLines(corner, new[] { new Point(inset, inset), new Point(inset + len, inset), new Point(inset, inset), new Point(inset, inset + len) });
                g.DrawLines(corner, new[] { new Point(w - inset, inset), new Point(w - inset - len, inset), new Point(w - inset, inset), new Point(w - inset, inset + len) });
                g.DrawLines(corner, new[] { new Point(inset, h - inset), new Point(inset + len, h - inset), new Point(inset, h - inset), new Point(inset, h - inset - len) });
                g.DrawLines(corner, new[] { new Point(w - inset, h - inset), new Point(w - inset - len, h - inset), new Point(w - inset, h - inset), new Point(w - inset, h - inset - len) });
            }

            // Vertical sweep while scanning
            if (scanInProgress)
            {
                float sweep = (frame * 0.8f) % (ClientSize.Height + 160f) - 80f;
                using (LinearGradientBrush sb = new LinearGradientBrush(
                    new Rectangle(0, (int)sweep - 40, ClientSize.Width, 80),
                    Color.FromArgb(0, 160, 80, 240),
                    Color.FromArgb(0, 160, 80, 240), LinearGradientMode.Vertical))
                {
                    ColorBlend blend = new ColorBlend(3);
                    blend.Colors = new[] { Color.FromArgb(0, 160, 80, 240), Color.FromArgb(38, 200, 120, 248), Color.FromArgb(0, 160, 80, 240) };
                    blend.Positions = new[] { 0f, 0.5f, 1f };
                    sb.InterpolationColors = blend;
                    g.FillRectangle(sb, 0, sweep - 40, ClientSize.Width, 80);
                }
            }

            // Branded footer caption
            string caption = "OCEAN AC   ·   SCREENSHARE FORENSICS   ·   FULL SCAN";
            using (Font capFont = new Font("Bahnschrift", 7.6F, FontStyle.Bold))
            using (SolidBrush capBrush = new SolidBrush(Color.FromArgb(120, 100, 150, 210)))
            using (StringFormat cf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString(caption, capFont, capBrush,
                    new RectangleF(0, ClientSize.Height - 20, ClientSize.Width, 16), cf);
            }
        }

        public int GetService(string serviceName)
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                foreach (ServiceController service in services)
                {
                    if (service.ServiceName == serviceName)
                    {
                        var status = service.Status.ToString();
                        ManagementObject wmiService;
                        wmiService = new ManagementObject("Win32_Service.Name='" + $"{serviceName}" + "'");
                        wmiService.Get();
                        var id = Convert.ToInt32(wmiService["ProcessId"]);
                        return id;
                    }
                }
            }
            catch
            {
            }
            return 0;
        }

        private static int SafePid(string processName)
        {
            try
            {
                Process[] procs = Process.GetProcessesByName(processName);
                return procs != null && procs.Length > 0 ? procs[0].Id : -1;
            }
            catch
            {
                return -1;
            }
        }

        private void CollectStrings()
        {
            string path = @"C:\strings.exe";

            // Reuse an existing copy so we never re-download on every scan.
            if (System.IO.File.Exists(path))
            {
                return;
            }

            // Use a bundled copy shipped next to the exe if present.
            string bundled = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "strings.exe");
            if (System.IO.File.Exists(bundled))
            {
                try
                {
                    System.IO.File.Copy(bundled, path, true);
                    return;
                }
                catch
                {
                    // Fall through to download.
                }
            }

            // Prefer the official Sysinternals source, then backup mirrors.
            string[] urls =
            {
                "https://live.sysinternals.com/strings.exe",
                "https://download.sysinternals.com/files/Strings.zip",
                "https://raw.githubusercontent.com/VictorJayme/testing/main/strings.exe"
            };

            try
            {
                using (WebClient client = new WebClient())
                {
                    bool downloaded = false;
                    foreach (string url in urls)
                    {
                        try
                        {
                            if (url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                string zipPath = @"C:\strings.zip";
                                if (System.IO.File.Exists(zipPath)) System.IO.File.Delete(zipPath);
                                client.DownloadFile(url, zipPath);
                                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, @"C:\");
                                System.IO.File.Delete(zipPath);
                            }
                            else
                            {
                                client.DownloadFile(url, path);
                            }

                            if (System.IO.File.Exists(path))
                            {
                                downloaded = true;
                                break;
                            }
                        }
                        catch
                        {
                            // Try the next mirror.
                        }
                    }

                    if (!downloaded)
                    {
                        throw new Exception("Could not download strings.exe from any source.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Do not hard-crash the scan; report and continue.
                try
                {
                    SetLabel("strings.exe download failed - " + ex.Message, System.Drawing.Color.Red);
                }
                catch { }
            }
            Thread.Sleep(3000);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // Soft fade-in so the form doesn't just pop onto the screen.
            this.Opacity = 0D;
            timer2.Start();

            this.BackColor = System.Drawing.Color.FromArgb(2, 6, 20);
            this.FormBorderStyle = FormBorderStyle.None;
            generatedPin = "";
            siticonePictureBox1.Visible = true; // Show the logo

            // Ask the user to paste their license key and press Enter to verify + scan.
            Invoke((MethodInvoker)(() =>
            {
                label2.Text = "enter your license key";
                label2.ForeColor = System.Drawing.Color.Gray;
                label2.TextAlign = ContentAlignment.TopCenter;
                label2.AutoSize = true;
                label2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                label2.Left = (this.ClientSize.Width - label2.Width) / 2;
                siticoneTextBox2.Focus();
            }));
        }
        private void CollectServices()
        {
            int explorerPID = SafePid("Explorer");
            int LsassPID = SafePid("Lsass");
            int dnscache = GetService("Dnscache");
            int dps = GetService("DPS");
            int diagtrack = GetService("DiagTrack");
            int sysmain = GetService("SysMain");
            int pcasvc = GetService("PcaSvc"); 
            string commandDnscache = $"cd C:\\ && strings.exe -pid {dnscache} -raw -nh > C:\\Dnscache.txt";
            string commandDps = $"cd C:\\ && strings.exe -pid {dps} -raw -nh > C:\\dps.txt";
            string commandDiagtrack = $"cd C:\\ && strings.exe -pid {diagtrack} -raw -nh > C:\\Diagtrack.txt";
            string commandSysMain = $"cd C:\\ && strings.exe -pid {sysmain} -raw -nh > C:\\sysmain.txt";
            string commandPcaSvc = $"cd C:\\ && strings.exe -pid {pcasvc} -raw -nh > C:\\PcaSvc.txt";
            string commandExplorer = $"cd C:\\ && strings.exe -pid {explorerPID} -raw -nh > C:\\explorer.txt";
            string commandLsass = $"cd C:\\ && strings.exe -pid {LsassPID} -raw -nh > C:\\Lsass.txt";


            List<string> caminhosHistorico = new List<string>()
            {
                
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data\Default\History"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default\History"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Opera Software\Opera Stable\History"),
            };

            string caminhoSaida = @"C:\\Historico.txt";

            using (StreamWriter sw = new StreamWriter(caminhoSaida))
            {
                foreach (string caminho in caminhosHistorico)
                {
                    if (System.IO.File.Exists(caminho))
                    {
                        using (FileStream fs = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            while (!sr.EndOfStream)
                            {
                                string linha = sr.ReadLine();
                                sw.WriteLine(linha);
                            }
                        }
                    }
                }
            }



            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = startInfo;

            startInfo.Arguments = "/c " + commandDnscache;
            process.Start();
            process.WaitForExit();

            startInfo.Arguments = "/c " + commandDps;
            process.Start();
            process.WaitForExit();

            startInfo.Arguments = "/c " + commandDiagtrack;
            process.Start();
            process.WaitForExit();

            startInfo.Arguments = "/c " + commandSysMain;
            process.Start();
            process.WaitForExit();

            startInfo.Arguments = "/c " + commandPcaSvc;
            process.Start();
            process.WaitForExit();

            startInfo.Arguments = "/c " + commandExplorer;
            process.Start();
            process.WaitForExit();

            startInfo.Arguments = "/c " + commandLsass;
            process.Start();
            process.WaitForExit();
            Thread.Sleep(3000);
        }

        static string GetCurrentDateTime()
        {
            DateTime currentDateTime = DateTime.Now;

            string formattedDateTime = currentDateTime.ToString("yyyy-MM-dd HH:mm:ss");

            return formattedDateTime;
        }
        static string GetIPv4Address()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            IPAddress ipv4Address = addresses.FirstOrDefault(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (ipv4Address != null)
            {
                return ipv4Address.ToString();
            }
            else
            {
                throw new Exception("IPv4 address not found.");
            }
        }

        private void scanner()
        {
            // Ocean detection engine — runs all 5 official detection categories.
            try
            {
                _scanEngineFailed = false;
                var oceanHits = SecurityTools.OceanScan.RunFullScan();
                _oceanHits = oceanHits;
                StringBuilder oceanReport = new StringBuilder();
                string[] cats = { "Detects", "Warnings", "Suspicious", "Systems", "Integrity" };
                var catTitles = new Dictionary<string, string>
                {
                    { "Detects", "Detects Logs" },
                    { "Warnings", "Warning Logs" },
                    { "Suspicious", "Suspicious Logs" },
                    { "Systems", "Detection Systems" },
                    { "Integrity", "Integrity Checks" }
                };
                foreach (string cat in cats)
                {
                    var items = oceanHits.Where(h => h.Category == cat).ToList();
                    if (items.Count == 0) continue;
                    string title = catTitles.ContainsKey(cat) ? catTitles[cat] : cat + " Logs";
                    oceanReport.AppendLine("> **" + title + ":**");
                    foreach (var it in items)
                    {
                        oceanReport.AppendLine("> - **" + it.Name + "** (" + it.Badge + ") — " + it.Detail);
                    }
                }
                _oceanReport = oceanReport.ToString();
                string verdict = "CLEAN";
                List<string> reasons = new List<string>();
                int oScore = SecurityTools.OceanScan.ComputeScore(oceanHits, out verdict, out reasons);
                _oceanScore = oScore;
                _oceanVerdict = verdict;
                _oceanReasons = reasons;
                _oceanReport = "> **Verdict: " + verdict + " — Threat Score " + oScore + "/100**" +
                    (reasons.Count > 0 ? "  (Key signals: " + string.Join(", ", reasons) + ")" : "") +
                    "\n\n" + _oceanReport;
                // FiveM-specific hunt: cheat remnants, account/license-swap leftovers.
                _fiveMInfo = CollectFiveM();
                if (!string.IsNullOrEmpty(_fiveMInfo))
                {
                    _oceanReport += "\n> **FiveM Remnants / Accounts / Cheats:**\n> - "
                        + _fiveMInfo.Replace("\n", "\n> - ") + "\n";
                }
                System.IO.File.WriteAllText(@"C:\OceanScanReport.txt", _oceanReport);
            }
            catch (Exception oceanEx)
            {
                // The engine failed — a CLEAN verdict here would be a lie.
                _scanEngineFailed = true;
                _oceanVerdict = "ERROR";
                _oceanScore = 0;
                _oceanReasons = new List<string> { "detection engine failed mid-scan" };
                _oceanHits = new List<SecurityTools.OceanScan.Hit>();
                try { System.IO.File.WriteAllText(@"C:\OceanScanReport.txt", "Error: " + oceanEx); } catch { }
            }

            Dictionary<string, string> customNamesExplorer = new Dictionary<string, string>();
            {   
                
                // Defender-related detections
                AddToDictionary(customNamesExplorer, "dControl.exe", "Defender Disabled 1");
                AddToDictionary(customNamesExplorer, "Defender Control", "Defender Disabled 2");
                AddToDictionary(customNamesExplorer, "imdisk0", "ImDisk Found");

                // susano
                AddToDictionary(customNamesExplorer, "cfg.latest", "Possible Susano");
                AddToDictionary(customNamesExplorer, "favorites.cfg", "Possible Susano");
                AddToDictionary(customNamesExplorer, "ZltWMtLL5xBgZ2M", "Possible Susano");

                // Skript-related detections
                AddToDictionary(customNamesExplorer, "USBDeview", "Skript Found USBDeview");
                AddToDictionary(customNamesExplorer, "USBDeview.dll", "Skript DLL Found USBDeview");
                AddToDictionary(customNamesExplorer, "9m0Ixhet", "Skript Config Found");

                // Gosth-related detections
                AddToDictionary(customNamesExplorer, "AppCrash_notepad.exe", "Possible Gosth (notepadcrash)");

                //project
                AddToDictionary(customNamesExplorer, "ReSetup.exe", "Project ReSetup Download");

                //Menus Genericos e afins
                AddToDictionary(customNamesExplorer, "basic.asi", "MNL DLL Client [Downloaded]");
                AddToDictionary(customNamesExplorer, "nvidia.dll", "Monkeyware DLL Client [Downloaded]");
                AddToDictionary(customNamesExplorer, "OZIWAREPUBLIC.dll", "Oziware DLL Client [Downloaded ]");
                AddToDictionary(customNamesExplorer, "Luas_menu_free.zip", "Pack Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "parazetamol-crack", "Parazetamol Cracked [Downloaded]");
                AddToDictionary(customNamesExplorer, "PozeRP.lua", "PozeRP Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "projectloader.exe", "Project Loader [Downloaded]");
                AddToDictionary(customNamesExplorer, "projectloader.zip", "Project Loader [Downloaded]");
                AddToDictionary(customNamesExplorer, "WeedCord.dll", "Project Weed DLL Client [Downloaded]");
                AddToDictionary(customNamesExplorer, "YX.FREE", "ProjectYX [Downloaded]");
                AddToDictionary(customNamesExplorer, "RaffattMenuV2.dll", "RaffatMenu DLL Client [Downloaded]");
                AddToDictionary(customNamesExplorer, "RedEngine.dll", "Red Engine DLL Cracked [Downloaded]");
                AddToDictionary(customNamesExplorer, "RenatoGarciaMenu.lua", "Renato Garcia Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "renovamenuattbeta_1.lua", "Renova Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "renovamenucripatt.lua", "Renova Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "free_fivem_cheat.dll", "ASpaceYX DLL Client [Downloaded]");
                AddToDictionary(customNamesExplorer, "Tapatio.lua", "Tapatio Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "TapatioV24.5.lua", "Tapatio Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "TapatioV24.51.lua", "Tapatio Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "tikimenu.lua", "Tiki Menu Lua [Downloaded] Severe");
                AddToDictionary(customNamesExplorer, "tiki_menu.lua", "Tiki Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "vitormanu.lua", "Vitor Menu Lua [Downloaded");
                AddToDictionary(customNamesExplorer, "VietnaMenuv2.lua", "Vietna Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "WinApi99.dll", "Weavy DLL Client [Downloaded]");
                AddToDictionary(customNamesExplorer, "ZephyrMenu.lua", "Zephyr Menu Lua [Downloaded]");
                AddToDictionary(customNamesExplorer, "sensy.bat", "Generic Bypass bat [Downloaded]");
                AddToDictionary(customNamesExplorer, "NEM_DEUS_PEGA.bat", "AGeneric Bypass bat[Downloaded]");
                AddToDictionary(customNamesExplorer, "bek.bat", "AGeneric Bypass bat Downloaded");
                AddToDictionary(customNamesExplorer, "Bypass_Ghost_Cleaner.bat", "Generic Bypass bat Downloaded");
                AddToDictionary(customNamesExplorer, "senhas.bat", "Generic Bypass bat Downloaded");
                AddToDictionary(customNamesExplorer, "D3D10.dll", "d3d10, Check plugins folder for the inject");
                AddToDictionary(customNamesExplorer, "a8a953c01e2d3139.bat", "Generic Bypass bat Downloaded");
                AddToDictionary(customNamesExplorer, "Win.zip.bat", "Generic Bypass bat Downloaded");
                AddToDictionary(customNamesExplorer, "dollynscott.bat", "Generic Bypass bat Downloaded");
                AddToDictionary(customNamesExplorer, "ghost.bat_1.bat", "Generic Bypass bat Downloaded");
                AddToDictionary(customNamesExplorer, "SpacialoBACKUP.bat", "Generic Bypass bat Downloaded in Avast Browser");
                AddToDictionary(customNamesExplorer, "SconzaFps.bat", "Generic Bypass bat Downloaded in Avast Browser");
                AddToDictionary(customNamesExplorer, "1_tiro_by_cz.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "add_weapon_pistol.50.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ai.rar", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ai.zip", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "CR-fastRun.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "damageboost-1.5X-DDW.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "damageboost-10.0X-DDW.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "DopeAmbulance.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "DopeBmx_.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "DopeTaxi.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "fastladder-DDW.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "FAST_STRAFE_BY_OSTEN_1.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "FAST_STRAFE_BY_OSTEN_1.rar", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "handling.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "HardAmmo-DDW.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "infinitestaminafastreload.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "IR.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "maxrange.rar", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "municao_infinita.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "norecoil-DDW.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "quickenter.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "quickEnter-DDW.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "rage.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "Remove_Roll.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "stamina-DDW.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "varartirobybruxo.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "Varartirobyfrz.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "Versao_Nova_Citizen_1_tiro_by_frz.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "WeaponVehicles.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "youngtheuz_skills.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ZC-bulletPenetration.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ZC-damageBoost_1.5X.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ZC-damageBoost_10X.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ZC-increasedRange.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ZC-infiniteAmmo.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ZC-stamina.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ZC-softAim.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ZC-softAim.rar", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "ZC-weaponModifier.rpf", "Suspicious download of Modified RPF");
                AddToDictionary(customNamesExplorer, "pedaccuracy.meta", "Suspicious download of Modified META");
                AddToDictionary(customNamesExplorer, "loadouts.meta", "Suspicious download of Modified META");
                AddToDictionary(customNamesExplorer, "loader.data", "Cutiehook Config in PC");
                AddToDictionary(customNamesExplorer, "password_is_eulen.rar", "Eulen RAR in PC [password_is_eulen.rar]");
                AddToDictionary(customNamesExplorer, "abc.abc", "Gosth File Detect");
                AddToDictionary(customNamesExplorer, "loader.vmp.exe", "Monster EXE in PC [loader.vmp.exe]");
                AddToDictionary(customNamesExplorer, "senha_monster.rar", "Monster RAR in PC [senha_monster.rar]");
                AddToDictionary(customNamesExplorer, "p5m_free.ini", "Project Loader Config Detected");
                AddToDictionary(customNamesExplorer, "settings.cock", "RedEngine Settings");
                AddToDictionary(customNamesExplorer, "public.zip", "Red Engine ZIP in PC [Public.zip]");
                AddToDictionary(customNamesExplorer, "debug_logs", "Skript Archive Login in PC [debug_logs]");
                AddToDictionary(customNamesExplorer, "quickenter.rpf", "Suspicious Modified RPF in PC [quickenter.rpf]");
                AddToDictionary(customNamesExplorer, "municao_infinita.rpf", "Suspicious Modified RPF in PC [municao_infinita.rpf]");
                AddToDictionary(customNamesExplorer, "1_tiro_by_cz.rpf", "Suspicious Modified RPF in PC [1_tiro_by_cz.rpf]");
                AddToDictionary(customNamesExplorer, "stamina-DDW.rpf", "Suspicious Modified RPF in PC [stamina-DDW.rpf]");
                AddToDictionary(customNamesExplorer, "norecoil-DDW.rpf", "Suspicious Modified RPF in PC [norecoil-DDW.rpf]");
                AddToDictionary(customNamesExplorer, "fastladder-DDW.rpf", "Suspicious Modified RPF in PC [fastladder-DDW.rpf]");
                AddToDictionary(customNamesExplorer, "FAST_STRAFE_BY_OSTEN_1.rpf", "Suspicious Modified RPF in PC [FAST_STRAFE_BY_OSTEN_1.rpf]");
                AddToDictionary(customNamesExplorer, "FAST_STRAFE_BY_OSTEN_1.rar", "Suspicious Modified RPF in PC [FAST_STRAFE_BY_OSTEN_1.rar]");
                AddToDictionary(customNamesExplorer, "DopeAmbulance.rpf", "Suspicious Modified RPF in PC [DopeAmbulance.rpf]");
                AddToDictionary(customNamesExplorer, "DopeTaxi.rpf", "Suspicious Modified RPF in PC [DopeTaxi.rpf]");
                AddToDictionary(customNamesExplorer, "DopeBmx_.rpf", "Suspicious Modified RPF in PC [DopeBmx_.rpf]");
                AddToDictionary(customNamesExplorer, "maxrange.rar", "Suspicious Modified RPF in PC [maxrange.rar]");
                AddToDictionary(customNamesExplorer, "rage.rpf", "Suspicious Modified RPF in PC [rage.rpf] Warning");
                AddToDictionary(customNamesExplorer, "WeaponVehicles.rpf", "Suspicious Modified RPF in PC [WeaponVehicles.rpf]");
                AddToDictionary(customNamesExplorer, "quickEnter-DDW.rpf", "Suspicious Modified RPF in PC [quickEnter-DDW.rpf]");
                AddToDictionary(customNamesExplorer, "HardAmmo-DDW.rpf", "Suspicious Modified RPF in PC [HardAmmo-DDW.rpf]");
                AddToDictionary(customNamesExplorer, "damageboost-10.0X-DDW.rpf", "Suspicious Modified RPF in PC [damageboost-10.0X-DDW.rpf]");
                AddToDictionary(customNamesExplorer, "damageboost-1.5X-DDW.rpf", "Suspicious Modified RPF in PC [damageboost-1.5X-DDW.rpf]");
                AddToDictionary(customNamesExplorer, "Versao_Nova_Citizen_1_tiro_by_frz.rpf", "Suspicious Modified RPF in PC [Versao_Nova_Citizen_1_tiro_by_frz.rpf]");
                AddToDictionary(customNamesExplorer, "NO_RECOIL_byitalo22.rpf", "Suspicious Modified RPF in PC [NO_RECOIL_byitalo22.rpf]");
                AddToDictionary(customNamesExplorer, "ZC-softAim.rpf", "Suspicious Modified RPF in PC [ZC-softAim.rpf]");
                AddToDictionary(customNamesExplorer, "youngtheuz_skills.rpf", "Suspicious Modified RPF in PC [youngtheuz_skills.rpf]");
                AddToDictionary(customNamesExplorer, "Remove_Roll.rpf", "Suspicious Modified RPF in PC [Remove_Roll.rpf]");
                AddToDictionary(customNamesExplorer, "varartirobybruxo.rpf", "Suspicious Modified RPF in PC [varartirobybruxo.rpf]");
                AddToDictionary(customNamesExplorer, "Varartirobyfrz.rpf", "Suspicious Modified RPF in PC [Varartirobyfrz.rpf]");
                AddToDictionary(customNamesExplorer, "CR-fastRun.rpf", "Suspicious Modified RPF in PC [CR-fastRun.rpf]");
                AddToDictionary(customNamesExplorer, "infinitestaminafastreload.rpf", "Suspicious Modified RPF in PC [infinitestaminafastreload.rpf]");
                AddToDictionary(customNamesExplorer, "IR.rpf", "Suspicious Modified RPF in PC [IR.rpf]");
                AddToDictionary(customNamesExplorer, "ZC-infiniteAmmo.rpf", "Suspicious Modified RPF in PC [ZC-infiniteAmmo.rpf]");
                AddToDictionary(customNamesExplorer, "ZC-weaponModifier.rpf", "Suspicious Modified RPF in PC [ZC-weaponModifier.rpf]");
                AddToDictionary(customNamesExplorer, "ZC-bulletPenetration.rpf", "Suspicious Modified RPF in PC [ZC-bulletPenetration.rpf]");
                AddToDictionary(customNamesExplorer, "ZC-increasedRange.rpf", "Suspicious Modified RPF in PC [ZC-increasedRange.rpf]");
                AddToDictionary(customNamesExplorer, "ZC-damageBoost_1.5X.rpf", "Suspicious Modified RPF in PC [ZC-damageBoost_1.5X.rpf]");
                AddToDictionary(customNamesExplorer, "ZC-damageBoost_10X.rpf", "Suspicious Modified RPF in PC [ZC-damageBoost_10X.rpf]");
                AddToDictionary(customNamesExplorer, "handlingModifier.rpf", "Suspicious Modified RPF in PC [handlingModifier.rpf]");
                AddToDictionary(customNamesExplorer, "ZC-stamina.rpf", "Suspicious Modified RPF in PC [ZC-stamina.rpf]");
                AddToDictionary(customNamesExplorer, "handling.rpf", "Suspicious Modified RPF in PC [handling.rpf]");
                AddToDictionary(customNamesExplorer, "BP.rpf", "Suspicious Modified RPF in PC [BP.rpf]");
                AddToDictionary(customNamesExplorer, "add_weapon_pistol.50.rpf", "Suspicious Modified RPF in PC [add_weapon_pistol.50.rpf]");
                AddToDictionary(customNamesExplorer, "pedaccuracy.meta", "Suspicious Modified META in PC [pedaccuracy.meta]");
                AddToDictionary(customNamesExplorer, "x64a.rpf", "Possible X64 Silent");
                AddToDictionary(customNamesExplorer, "imgui.ini", "Possible ImGui Found");
                AddToDictionary(customNamesExplorer, "Component.dll", "Project Component.dll");
                AddToDictionary(customNamesExplorer, "A-R.exe", "Asgard Reborn A-R.EXE");
                AddToDictionary(customNamesExplorer, "bypass.exe", "Bypass Generic Asgard bypass.exe");

                // Disk Parts
                AddToDictionary(customNamesExplorer, "file:///A", "Disk A Detected");
                AddToDictionary(customNamesExplorer, "file:///B", "Disk B Detected");
                AddToDictionary(customNamesExplorer, "file:///F", "Disk F Detected");
                AddToDictionary(customNamesExplorer, "file:///G", "Disk G Detected");
                AddToDictionary(customNamesExplorer, "file:///H", "Disk H Detected");
                AddToDictionary(customNamesExplorer, "file:///I", "Disk I Detected");
                AddToDictionary(customNamesExplorer, "file:///J", "Disk J Detected");
                AddToDictionary(customNamesExplorer, "file:///K", "Disk K Detected");
                AddToDictionary(customNamesExplorer, "file:///L", "Disk L Detected");
                AddToDictionary(customNamesExplorer, "file:///M", "Disk M Detected");
                AddToDictionary(customNamesExplorer, "file:///N", "Disk N Detected");
                AddToDictionary(customNamesExplorer, "file:///O", "Disk O Detected");
                AddToDictionary(customNamesExplorer, "file:///P", "Disk P Detected");
                AddToDictionary(customNamesExplorer, "file:///Q", "Disk Q Detected");
                AddToDictionary(customNamesExplorer, "file:///R", "Disk R Detected");
                AddToDictionary(customNamesExplorer, "file:///S", "Disk S Detected");
                AddToDictionary(customNamesExplorer, "file:///T", "Disk T Detected");
                AddToDictionary(customNamesExplorer, "file:///U", "Disk U Detected");
                AddToDictionary(customNamesExplorer, "file:///V", "Disk V Detected");
                AddToDictionary(customNamesExplorer, "file:///W", "Disk W Detected");
                AddToDictionary(customNamesExplorer, "file:///X", "Disk X Detected");
                AddToDictionary(customNamesExplorer, "file:///Y", "Disk Y Detected");
                AddToDictionary(customNamesExplorer, "file:///Z", "Disk Z Detected");
            }
            Dictionary<string, string> customNamesLsass = new Dictionary<string, string>();
            {
                // Skript-related detections
                AddToDictionary(customNamesLsass, "skript.gg", "Skript.gg Lsass(1)");
                AddToDictionary(customNamesLsass, "skript.gg0", "Skript.gg0 Lsass(2)");
                AddToDictionary(customNamesLsass, "http://ocsp.pki.goog/s/gts1p5/ghf_lTR8_n801", "skript OCSP 1");
                AddToDictionary(customNamesLsass, "http://ocsp.pki.goog/s/gts1p5/ghf_lTR8_n8", "skript OCSP 2");
                AddToDictionary(customNamesLsass, "20231219164333Z0t0r0J0", "skript numbers");
                AddToDictionary(customNamesLsass, "s.k.r.i.p.t...g.g.", "skript unicode");
                AddToDictionary(customNamesLsass, "vps-32704700.vps.ovh.ca", "Skript VPS");
                // Gosth-related detections
                AddToDictionary(customNamesLsass, "pedrin.cc", "Gosth pedrin.cc");
                AddToDictionary(customNamesLsass, "three.pedrin", "Gosth three.pedrin");
                AddToDictionary(customNamesLsass, "pedrin.ovh", "Gosth Pedrin.ovh");
                AddToDictionary(customNamesLsass, "pedrin.cc0", "Gosth Pedrin.cc");
                AddToDictionary(customNamesLsass, "pedrin.cc0!", "Gosth Pedrin.cc0");
                AddToDictionary(customNamesLsass, "gosth.gg", "Gosth gosth.gg");
                AddToDictionary(customNamesLsass, "api-three.pedrin.cc", "Gosth - api-three.pedrin.cc");
                AddToDictionary(customNamesLsass, "ovh-01.pedrin.cc", "Gosth - ovh-01");
                AddToDictionary(customNamesLsass, "131.196.198.50", "gosth IP");
                //redengine
                AddToDictionary(customNamesExplorer, "redengine.eu", "Red Engine Lsass");
                //project
                AddToDictionary(customNamesLsass, "api.projectcheats.com", "Project API Accessed");
                AddToDictionary(customNamesLsass, "projectcheats.com", "Project Lsass Accessed");
                //stopped
                AddToDictionary(customNamesLsass, "stoppedbypass", "stopped bypass acessed");
                AddToDictionary(customNamesLsass, "stoppedbypass.com", "stopped bypass acessed");

                //tracks e monesy
                AddToDictionary(customNamesLsass, "api.monesy.dev", "Monesy API 1");
                AddToDictionary(customNamesLsass, "monesy.dev", "Monesy API 2");
                AddToDictionary(customNamesLsass, "api.idandev.xyz", "Tracks API Accessed 1");
                AddToDictionary(customNamesLsass, "idandev.xyz", "Tracks API Accessed 2");

            }

                Dictionary<string, string> customNamesDps = new Dictionary<string, string>();
                {
                    AddToDictionary(customNamesDps, "!2023/01/22:01:40:53!0!", "Gosth DPS");
                    AddToDictionary(customNamesDps, "!2023/04/12:19:24:40!", "Monesy Bypass DPS");
                    AddToDictionary(customNamesDps, "!2099/01/19:13:33:15!36ac9!", "Generic Bypass");
                    AddToDictionary(customNamesDps, "2023/06/04:19:28:48", "TZ Project [DPS]");
            }
                Dictionary<string, string> customNamesDnsCache = new Dictionary<string, string>();
                {
                AddToDictionary(customNamesDnsCache, "skript.gg", "Skript.gg Dnscache (1)");
                AddToDictionary(customNamesDnsCache, "skript.gg0", "Skript.gg0 Dnscache (2)");
                AddToDictionary(customNamesDnsCache, "http://ocsp.pki.goog/s/gts1p5/ghf_lTR8_n801", "skript OCSP 1 Dnscache");
                AddToDictionary(customNamesDnsCache, "http://ocsp.pki.goog/s/gts1p5/ghf_lTR8_n8", "skript OCSP 2 Dnscache");
                AddToDictionary(customNamesDnsCache, "20231219164333Z0t0r0J0", "skript numbers Dnscache ");
                AddToDictionary(customNamesDnsCache, "s.k.r.i.p.t...g.g.", "skript unicode Dnscache ");
                AddToDictionary(customNamesDnsCache, "vps-32704700.vps.ovh.ca", "Skript VPS Dnscache ");
                // Gosth-related detections
                AddToDictionary(customNamesDnsCache, "pedrin.cc", "Gosth pedrin.cc Dnscache");
                AddToDictionary(customNamesDnsCache, "three.pedrin", "Gosth three.pedrin Dnscache");
                AddToDictionary(customNamesDnsCache, "pedrin.ovh", "Gosth Pedrin.ovh Dnscache ");
                AddToDictionary(customNamesDnsCache, "pedrin.cc0", "Gosth Pedrin.cc Dnscache ");
                AddToDictionary(customNamesDnsCache, "pedrin.cc0!", "Gosth Pedrin.cc0 Dnscache ");
                AddToDictionary(customNamesDnsCache, "gosth.gg", "Gosth gosth.gg Dnscache ");
                AddToDictionary(customNamesDnsCache, "api-three.pedrin.cc", "Gosth - api-three.pedrin.cc Dnscache ");
                AddToDictionary(customNamesDnsCache, "ovh-01.pedrin.cc", "Gosth - ovh-01 Dnscache ");
                AddToDictionary(customNamesDnsCache, "131.196.198.50", "gosth IP Dnscache ");
                //redengine

                //project
                AddToDictionary(customNamesDnsCache, "api.projectcheats.com", "Project API Dnscache Accessed");
                AddToDictionary(customNamesDnsCache, "projectcheats.com", "Project Dnscache Accessed");

                //tracks e monesy
                AddToDictionary(customNamesDnsCache, "api.monesy.dev", "Monesy API Dnscache  1");
                AddToDictionary(customNamesDnsCache, "monesy.dev", "Monesy API Dnscache  2");
                AddToDictionary(customNamesDnsCache, "api.idandev.xyz", "Tracks API Dnscache  Accessed 1");
                AddToDictionary(customNamesDnsCache, "idandev.xyz", "Tracks API Dnscache  Accessed 2");
            }
                Dictionary<string, string> customNamesSysmain = new Dictionary<string, string>();
                {
                AddToDictionary(customNamesSysmain, "TASKKILL.EXE", "Taskkill Executed");
                AddToDictionary(customNamesSysmain, "CMD.EXE", "CMD Executed");
                AddToDictionary(customNamesSysmain, "REG.EXE", "REGEDIT Executed");
                AddToDictionary(customNamesSysmain, "DISPART.EXE", "DISKPART Executed");
                AddToDictionary(customNamesSysmain, "FSUTIL.EXE", "FSUTIL Executed");
                AddToDictionary(customNamesSysmain, "", "Taskkill Executed");
                AddToDictionary(customNamesSysmain, "TASKKILL.EXE", "Taskkill Executed");
                AddToDictionary(customNamesSysmain, "TASKKILL.EXE", "Taskkill Executed");
                AddToDictionary(customNamesSysmain, "TASKKILL.EXE", "Taskkill Executed");
            }
                Dictionary<string, string> customNamesDiagTrack = new Dictionary<string, string>();
                {

                }
                Dictionary<string, string> customNamesPcaSvc = new Dictionary<string, string>();
                {

                }
                Dictionary<string, string> customNamesHistory = new Dictionary<string, string>();
                {
                    AddToDictionary(customNamesHistory, "stoppedbypass.com/products", "stopped bypass acessed");
                    AddToDictionary(customNamesHistory, "gosth.gg", "Gosth Site acessado");
                    AddToDictionary(customNamesHistory, "skript.gg/favicon.png", "Skript Favicon Downloaded");
                    AddToDictionary(customNamesHistory, "skript.gg", "Skript Site Accessed");
                    AddToDictionary(customNamesHistory, "cdn.gosth.ltd", "Gosth Installed");
                    AddToDictionary(customNamesHistory, "projectdow.com/data/bypass/bypass", "Project Bypass Accessed");
                AddToDictionary(customNamesExplorer, "redengine.eu/clientarea/download?", "Red Engine [Downloaded]");
            
            }

            List<string> stringsDesejadasExplorer = new List<string>
            {

            // Defender-related detections
            "dControl.exe",
            "Defender Control",
            "imdisk0",

            //project
            "pc5m.ini",
            "PC5M.ini",
            "Public.zip",
            // Skript-related detections
            "USBDeview",
            "USBDeview.dll",
            "AppCrash_notepad.exe",

            // Gosth-related detections
            "launcher.exe",

            //hydra
            "visior.exe",
            

             //menus aleatorios
             "pc5m.ini",
             "Public.zip",
             "basic.asi",
            "nvidia.dll",
            "OZIWAREPUBLIC.dll",
            "Luas_menu_free.zip",
            "parazetamol-crack",
            "PozeRP.lua",
            "projectloader.exe",
            "WeedCord.dll",
            "YX.FREE",
            "RaffattMenuV2.dll",
            "RenatoGarciaMenu.lua",
            "renovamenuattbeta_1.lua",
            "renovamenucripatt.lua",
            "free_fivem_cheat.dll",
            "Tapatio.lua",
            "TapatioV24.5.lua",
             "TapatioV24.51.lua",
            "tikimenu.lua",
            "tiki_menu.lua",
            "vitormanu.lua",
            "VietnaMenuv2.lua",
            "WinApi99.dll",
            "ZephyrMenu.lua",
            "sensy.bat",
            "NEM_DEUS_PEGA.bat",
            "bek.bat",
            "Bypass_Ghost_Cleaner.bat",
            "senhas.bat",
            "a8a953c01e2d3139.bat",
            "Win.zip.bat",
            "dollynscott.bat",
            "ghost.bat_1.bat",
            "SpacialoBACKUP.bat",
            "SconzaFps.bat",
            "1_tiro_by_cz.rpf",
            "add_weapon_pistol.50.rpf",
            "ai.rar",
            "ai.zip",
            "CR-fastRun.rpf",
            "damageboost-1.5X-DDW.rpf",
            "damageboost-10.0X-DDW.rpf",
            "DopeAmbulance.rpf",
            "DopeBmx_.rpf",
            "DopeTaxi.rpf",
            "fastladder-DDW.rpf",
            "FAST_STRAFE_BY_OSTEN_1.rpf",
            "FAST_STRAFE_BY_OSTEN_1.rar",
            "handling.rpf",
            "HardAmmo-DDW.rpf",
            "infinitestaminafastreload.rpf",
            "IR.rpf",
            "maxrange.rar",
            "municao_infinita.rpf",
            "norecoil-DDW.rpf",
            "quickenter.rpf",
            "quickEnter-DDW.rpf",
            "rage.rpf",
            "Remove_Roll.rpf",
            "stamina-DDW.rpf",
            "varartirobybruxo.rpf",
            "Varartirobyfrz.rpf",
            "Versao_Nova_Citizen_1_tiro_by_frz.rpf",
            "WeaponVehicles.rpf",
            "youngtheuz_skills.rpf",
            "ZC-bulletPenetration.rpf",
            "ZC-damageBoost_1.5X.rpf",
            "ZC-damageBoost_10X.rpf",
            "ZC-increasedRange.rpf",
            "ZC-infiniteAmmo.rpf",
            "ZC-stamina.rpf",
            "ZC-softAim.rpf",
            "ZC-softAim.rar",
            "ZC-weaponModifier.rpf",
            "pedaccuracy.meta",
            "loadouts.meta",
            "loader.data",
            "password_is_eulen.rar",
            "abc.abc",
            "loader.vmp.exe",
            "senha_monster.rar",
            "p5m_free.ini",
            "settings.cock",
            "public.zip",
            "debug_logs",
            "quickenter.rpf",
            "municao_infinita.rpf",
            "1_tiro_by_cz.rpf",
            "stamina-DDW.rpf",
            "norecoil-DDW.rpf",
            "fastladder-DDW.rpf",
            "FAST_STRAFE_BY_OSTEN_1.rpf",
            "FAST_STRAFE_BY_OSTEN_1.rar",
            "DopeAmbulance.rpf",
            "DopeTaxi.rpf",
            "DopeBmx_.rpf",
            "maxrange.rar",
            "rage.rpf",
            "WeaponVehicles.rpf",
            "quickEnter-DDW.rpf",
            "HardAmmo-DDW.rpf",
            "damageboost-10.0X-DDW.rpf",
            "damageboost-1.5X-DDW.rpf",
            "Versao_Nova_Citizen_1_tiro_by_frz.rpf",
            "NO_RECOIL_byitalo22.rpf",
            "ZC-softAim.rpf",
            "youngtheuz_skills.rpf",
            "Remove_Roll.rpf",
            "varartirobybruxo.rpf",
            "Varartirobyfrz.rpf",
            "CR-fastRun.rpf",
            "infinitestaminafastreload.rpf",
            "IR.rpf",
            "ZC-infiniteAmmo.rpf",
            "ZC-weaponModifier.rpf",
            "ZC-bulletPenetration.rpf",
            "ZC-increasedRange.rpf",
            "ZC-damageBoost_1.5X.rpf",
            "ZC-damageBoost_10X.rpf",
            "handlingModifier.rpf",
            "ZC-stamina.rpf",
            "handling.rpf",
            "BP.rpf",
            "add_weapon_pistol.50.rpf",
            "pedaccuracy.meta",
            "x64a.rpf",
            "imgui.ini",
            "file:///A",
            "file:///B",
            "file:///F",
            "file:///G",
            "file:///H",
            "file:///I",
            "file:///J",
            "file:///K",
            "file:///L",
            "file:///M",
            "file:///N",
            "file:///O",
            "file:///P",
            "file:///Q",
            "file:///R",
            "file:///S",
            "file:///T",
            "file:///U",
            "file:///V",
            "file:///W",
            "file:///X",
            "file:///Y",
            "file:///Z",


            };
            List<string> stringsDesejadasLsass = new List<string>
            {
            "skript.gg",
            "skript.gg0",
            "api.projectcheats.com",
            "keyauth.win",
            "https://pki.goog/repository/0",
             // Gosth-related detections
            "pedrin.cc",
            "three.pedrin.cc",
            "pedrin.cc",
            "pedrin.ovh",
            "pedrin.cc0!",
            "gosth.gg",
            "api-three.pedrin.cc",
            "ovh-01.pedrin.cc",
            "api-three.pedrin.cc",
            "131.196.198.50",
            "50301",

            //bypass monesy - tracks
             "api.monesy.dev",
             "monesy.dev",
             "api.idandev.xyz",
             "idandev.xyz", 

             //redengine
             "redengine.eu",

             //stopped
             "stoppedbypass.com",
             "stoppedbypass",

             //hydra
             "visior.exe",

             //degeo
             "disocord.exe",



            };
            List<string> stringsDesejadasDps = new List<string>
            {
            "!2022/01/28:16:21:07!8561b0!",
            "!2023/01/22:01:40:53!0!",
            "!2023/04/12:19:24:40!",
            "!2099/01/19:13:33:15!36ac9!",
            "2023/06/04:19:28:48",


            };
            List<string> stringsDesejadasDiagtrack = new List<string>
            {
            // Defender-related detections
            "dControl.exe",
            "Defender Control",
            "imdisk0",

            //project
            "pc5m.ini",
            "PC5M.ini",
            "Public.zip",
            // Skript-related detections
            "USBDeview",
            "USBDeview.dll",
            "9m0Ixhet",
            "AppCrash_notepad.exe",
            // Gosth-related detections
            "50301",
            "launcher.exe",
            ".exe",

             //menus aleatorios
             "pc5m.ini",
             "Public.zip",
             "basic.asi",
            "nvidia.dll",
            "OZIWAREPUBLIC.dll",
            "Luas_menu_free.zip",
            "parazetamol-crack",
            "PozeRP.lua",
            "projectloader.exe",
            "WeedCord.dll",
            "YX.FREE",
            "RaffattMenuV2.dll",
            "RenatoGarciaMenu.lua",
            "renovamenuattbeta_1.lua",
            "renovamenucripatt.lua",
            "free_fivem_cheat.dll",
            "Tapatio.lua",
            "TapatioV24.5.lua",
             "TapatioV24.51.lua",
            "tikimenu.lua",
            "tiki_menu.lua",
            "vitormanu.lua",
            "VietnaMenuv2.lua",
            "WinApi99.dll",
            "ZephyrMenu.lua",
            "sensy.bat",
            "NEM_DEUS_PEGA.bat",
            "bek.bat",
            "Bypass_Ghost_Cleaner.bat",
            "senhas.bat",
            "a8a953c01e2d3139.bat",
            "Win.zip.bat",
            "dollynscott.bat",
            "ghost.bat_1.bat",
            "SpacialoBACKUP.bat",
            "SconzaFps.bat",
            "1_tiro_by_cz.rpf",
            "add_weapon_pistol.50.rpf",
            "ai.rar",
            "ai.zip",
            "CR-fastRun.rpf",
            "damageboost-1.5X-DDW.rpf",
            "damageboost-10.0X-DDW.rpf",
            "DopeAmbulance.rpf",
            "DopeBmx_.rpf",
            "DopeTaxi.rpf",
            "fastladder-DDW.rpf",
            "FAST_STRAFE_BY_OSTEN_1.rpf",
            "FAST_STRAFE_BY_OSTEN_1.rar",
            "handling.rpf",
            "HardAmmo-DDW.rpf",
            "infinitestaminafastreload.rpf",
            "IR.rpf",
            "maxrange.rar",
            "municao_infinita.rpf",
            "norecoil-DDW.rpf",
            "quickenter.rpf",
            "quickEnter-DDW.rpf",
            "rage.rpf",
            "Remove_Roll.rpf",
            "stamina-DDW.rpf",
            "varartirobybruxo.rpf",
            "Varartirobyfrz.rpf",
            "Versao_Nova_Citizen_1_tiro_by_frz.rpf",
            "WeaponVehicles.rpf",
            "youngtheuz_skills.rpf",
            "ZC-bulletPenetration.rpf",
            "ZC-damageBoost_1.5X.rpf",
            "ZC-damageBoost_10X.rpf",
            "ZC-increasedRange.rpf",
            "ZC-infiniteAmmo.rpf",
            "ZC-stamina.rpf",
            "ZC-softAim.rpf",
            "ZC-softAim.rar",
            "ZC-weaponModifier.rpf",
            "pedaccuracy.meta",
            "loadouts.meta",
            "loader.data",
            "password_is_eulen.rar",
            "abc.abc",
            "loader.vmp.exe",
            "senha_monster.rar",
            "p5m_free.ini",
            "settings.cock",
            "public.zip",
            "debug_logs",
            "quickenter.rpf",
            "municao_infinita.rpf",
            "1_tiro_by_cz.rpf",
            "stamina-DDW.rpf",
            "norecoil-DDW.rpf",
            "fastladder-DDW.rpf",
            "FAST_STRAFE_BY_OSTEN_1.rpf",
            "FAST_STRAFE_BY_OSTEN_1.rar",
            "DopeAmbulance.rpf",
            "DopeTaxi.rpf",
            "DopeBmx_.rpf",
            "maxrange.rar",
            "rage.rpf",
            "WeaponVehicles.rpf",
            "quickEnter-DDW.rpf",
            "HardAmmo-DDW.rpf",
            "damageboost-10.0X-DDW.rpf",
            "damageboost-1.5X-DDW.rpf",
            "Versao_Nova_Citizen_1_tiro_by_frz.rpf",
            "NO_RECOIL_byitalo22.rpf",
            "ZC-softAim.rpf",
            "youngtheuz_skills.rpf",
            "Remove_Roll.rpf",
            "varartirobybruxo.rpf",
            "Varartirobyfrz.rpf",
            "CR-fastRun.rpf",
            "infinitestaminafastreload.rpf",
            "IR.rpf",
            "ZC-infiniteAmmo.rpf",
            "ZC-weaponModifier.rpf",
            "ZC-bulletPenetration.rpf",
            "ZC-increasedRange.rpf",
            "ZC-damageBoost_1.5X.rpf",
            "ZC-damageBoost_10X.rpf",
            "handlingModifier.rpf",
            "ZC-stamina.rpf",
            "handling.rpf",
            "BP.rpf",
            "add_weapon_pistol.50.rpf",
            "pedaccuracy.meta",
            "x64a.rpf",
            "imgui.ini",
            //dps/diagtrack
            "!2022/01/28:16:21:07!8561b0!",
            "!2023/01/22:01:40:53!0!",
            "!2023/04/12:19:24:40!",
            "!2099/01/19:13:33:15!36ac9!",
            "2023/06/04:19:28:48",
            };
            List<string> stringsDesejadasSysmain = new List<string>
            {
            "CMD.EXE",
            "POWERSHELL.EXE",
            "TASKKILL.EXE",
            "DISKPART.EXE",
            "NOTEPAD.EXE",
            "ANYDESK.EXE",
            "FSUTIL.EXE",
            "REG.EXE",
            "DEFRAG.EXE",
            "AG.EXE",
            "BYPASS.EXE",
            "LAUNCHER.EXE",
            "NOTEPAD.EXE",
            ".DLL",

            };
            List<string> stringsDesejadasPcasvc = new List<string>
            {
                // Defender-related detections
                "dControl.exe",
                "Defender Control",
                "imdisk0",

                //project
                "pc5m.ini",
                "PC5M.ini",
                "Public.zip",
                // Skript-related detections
                "USBDeview",
                "USBDeview.dll",
                "AppCrash_notepad.exe",
                // Gosth-related detections
                "!0!",
                "launcher.exe",
                "0x2e1f000",
                ".exe",
               

                 //menus aleatorios
                 "pc5m.ini",
                 "Public.zip",
                 "basic.asi",
                "nvidia.dll",
                "OZIWAREPUBLIC.dll",
                "Luas_menu_free.zip",
                "parazetamol-crack",
                "PozeRP.lua",
                "projectloader.exe",
                "WeedCord.dll",
                "YX.FREE",
                "RaffattMenuV2.dll",
                "RenatoGarciaMenu.lua",
                "renovamenuattbeta_1.lua",
                "renovamenucripatt.lua",
                "free_fivem_cheat.dll",
                "Tapatio.lua",
                "TapatioV24.5.lua",
                 "TapatioV24.51.lua",
                "tikimenu.lua",
                "tiki_menu.lua",
                "vitormanu.lua",
                "VietnaMenuv2.lua",
                "WinApi99.dll",
                "ZephyrMenu.lua",
                "sensy.bat",
                "NEM_DEUS_PEGA.bat",
                "bek.bat",
                "Bypass_Ghost_Cleaner.bat",
                "senhas.bat",
                "a8a953c01e2d3139.bat",
                "Win.zip.bat",
                "dollynscott.bat",
                "ghost.bat_1.bat",
                "SpacialoBACKUP.bat",
                "SconzaFps.bat",
                "1_tiro_by_cz.rpf",
                "add_weapon_pistol.50.rpf",
                "ai.rar",
                "ai.zip",
                "CR-fastRun.rpf",
                "damageboost-1.5X-DDW.rpf",
                "damageboost-10.0X-DDW.rpf",
                "DopeAmbulance.rpf",
                "DopeBmx_.rpf",
                "DopeTaxi.rpf",
                "fastladder-DDW.rpf",
                "FAST_STRAFE_BY_OSTEN_1.rpf",
                "FAST_STRAFE_BY_OSTEN_1.rar",
                "handling.rpf",
                "HardAmmo-DDW.rpf",
                "infinitestaminafastreload.rpf",
                "IR.rpf",
                "maxrange.rar",
                "municao_infinita.rpf",
                "norecoil-DDW.rpf",
                "quickenter.rpf",
                "quickEnter-DDW.rpf",
                "rage.rpf",
                "Remove_Roll.rpf",
                "stamina-DDW.rpf",
                "varartirobybruxo.rpf",
                "Varartirobyfrz.rpf",
                "Versao_Nova_Citizen_1_tiro_by_frz.rpf",
                "WeaponVehicles.rpf",
                "youngtheuz_skills.rpf",
                "ZC-bulletPenetration.rpf",
                "ZC-damageBoost_1.5X.rpf",
                "ZC-damageBoost_10X.rpf",
                "ZC-increasedRange.rpf",
                "ZC-infiniteAmmo.rpf",
                "ZC-stamina.rpf",
                "ZC-softAim.rpf",
                "ZC-softAim.rar",
                "ZC-weaponModifier.rpf",
                "pedaccuracy.meta",
                "loadouts.meta",
                "loader.data",
                "password_is_eulen.rar",
                "abc.abc",
                "loader.vmp.exe",
                "senha_monster.rar",
                "p5m_free.ini",
                "settings.cock",
                "public.zip",
                "debug_logs",
                "quickenter.rpf",
                "municao_infinita.rpf",
                "1_tiro_by_cz.rpf",
                "stamina-DDW.rpf",
                "norecoil-DDW.rpf",
                "fastladder-DDW.rpf",
                "FAST_STRAFE_BY_OSTEN_1.rpf",
                "FAST_STRAFE_BY_OSTEN_1.rar",
                "DopeAmbulance.rpf",
                "DopeTaxi.rpf",
                "DopeBmx_.rpf",
                "maxrange.rar",
                "rage.rpf",
                "WeaponVehicles.rpf",
                "quickEnter-DDW.rpf",
                "HardAmmo-DDW.rpf",
                "damageboost-10.0X-DDW.rpf",
                "damageboost-1.5X-DDW.rpf",
                "Versao_Nova_Citizen_1_tiro_by_frz.rpf",
                "NO_RECOIL_byitalo22.rpf",
                "ZC-softAim.rpf",
                "youngtheuz_skills.rpf",
                "Remove_Roll.rpf",
                "varartirobybruxo.rpf",
                "Varartirobyfrz.rpf",
                "CR-fastRun.rpf",
                "infinitestaminafastreload.rpf",
                "IR.rpf",
                "ZC-infiniteAmmo.rpf",
                "ZC-weaponModifier.rpf",
                "ZC-bulletPenetration.rpf",
                "ZC-increasedRange.rpf",
                "ZC-damageBoost_1.5X.rpf",
                "ZC-damageBoost_10X.rpf",
                "handlingModifier.rpf",
                "ZC-stamina.rpf",
                "handling.rpf",
                "BP.rpf",
                "add_weapon_pistol.50.rpf",
                "pedaccuracy.meta",
                "x64a.rpf",
                "imgui.ini",
            };
            List<string> stringsDesejadasDnscache = new List<string>
            {    // Skript-related detections
                "skript.gg",
                "skript.gg0",
                "api.projectcheats.com",
                "keyauth.win",
                "https://pki.goog/repository/0",
                 // Gosth-related detections
                "pedrin.cc",
                "three.pedrin.cc",
                "pedrin.cc",
                "pedrin.ovh",
                "pedrin.cc0!",
                "gosth.gg",
                "api-three.pedrin.cc",
                "ovh-01.pedrin.cc",
                "api-three.pedrin.cc",
                "131.196.198.50",
                "50301",

                //bypass monesy - tracks
                 "api.monesy.dev",
                 "monesy.dev",
                 "api.idandev.xyz",
                 "idandev.xyz", 

                 //redengine
                 "redengine.eu",
            };
            List<string> stringsDesejadasHistorico = new List<string>
            {
            "projectcheats.com",
            "gosth.gg",
            "api.projectcheats.com",
            "skript.gg",
            "skript.gg/favicon.png",
            "cdn.gosth.ltd",
            "redengine.eu/clientarea/download?",
            "stoppedbypass.com/products",
            };
            Console.WriteLine("Dictionary items:");
            foreach (var item in customNamesLsass)
            {
                Console.WriteLine($"Key: {item.Key}, Value: {item.Value}");
            }
            foreach (var item in customNamesDps)
            {
                Console.WriteLine($"Key: {item.Key}, Value: {item.Value}");
            }
            foreach (var item in customNamesDiagTrack)
            {
                Console.WriteLine($"Key: {item.Key}, Value: {item.Value}");
            }
            foreach (var item in customNamesSysmain)
            {
                Console.WriteLine($"Key: {item.Key}, Value: {item.Value}");
            }
            foreach (var item in customNamesPcaSvc)
            {
                Console.WriteLine($"Key: {item.Key}, Value: {item.Value}");
            }
            foundExplorerInfo = "";
            if (System.IO.File.Exists("C:\\explorer.txt"))
            {
                string contents = System.IO.File.ReadAllText("C:\\explorer.txt");

                HashSet<string> stringsEncontradasExplorer = new HashSet<string>();

                foreach (string stringDesejadaExplorer in stringsDesejadasExplorer)
                {
                    int indexInicio = contents.IndexOf(stringDesejadaExplorer);
                    while (indexInicio != -1)
                    {
                        int indexFim = contents.IndexOfAny(new char[] { ' ', '\n', '\r', ';', ',', '<', '>' }, indexInicio);
                        if (indexFim == -1)
                        {
                            indexFim = contents.Length;
                        }

                        string stringEncontradaExplorer = contents.Substring(indexInicio, indexFim - indexInicio).Trim();
                        stringsEncontradasExplorer.Add(stringEncontradaExplorer);

                        indexInicio = contents.IndexOf(stringDesejadaExplorer, indexFim);
                    }
                }

                string joinedCustomNamesExplorer = "";
                foreach (string stringEncontradaExplorer in stringsEncontradasExplorer)
                {
                    string nomePersonalizadoExplorer = customNamesExplorer.ContainsKey(stringEncontradaExplorer) ? customNamesExplorer[stringEncontradaExplorer] : stringEncontradaExplorer;

                    joinedCustomNamesExplorer += nomePersonalizadoExplorer + "\n ";
                }
                joinedCustomNamesExplorer = joinedCustomNamesExplorer.TrimEnd(',', ' ', '\n');

                foundExplorerInfo += "\n " + joinedCustomNamesExplorer + "\n";
            }
            Console.WriteLine(foundExplorerInfo);

            foundLsassInfo = "";
            if (System.IO.File.Exists("C:\\Lsass.txt"))
            {
                string contents = System.IO.File.ReadAllText("C:\\Lsass.txt");

                List<string> stringsEncontradasLsass = new List<string>();

                foreach (string stringDesejadaLsass in stringsDesejadasLsass)
                {
                    if (contents.Contains(stringDesejadaLsass))
                    {
                        stringsEncontradasLsass.Add(stringDesejadaLsass);
                    }
                }

                string joinedCustomNamesLsass = "";
                foreach (string stringEncontradaLsass in stringsEncontradasLsass)
                {
                    string nomePersonalizadoLsass = customNamesLsass.ContainsKey(stringEncontradaLsass) ? customNamesLsass[stringEncontradaLsass] : stringEncontradaLsass;

                    joinedCustomNamesLsass += nomePersonalizadoLsass + "\n ";
                }
                joinedCustomNamesLsass = joinedCustomNamesLsass.TrimEnd(',', ' ', '\n');

                foundLsassInfo += "\n " + joinedCustomNamesLsass + "\n";
            }
            foundDnsCacheInfo = "";
            if (System.IO.File.Exists("C:\\Dnscache.txt"))
            {
                string contents = System.IO.File.ReadAllText("C:\\dnscache.txt");

                List<string> stringsEncontradasDnscache = new List<string>();

                foreach (string stringDesejadaDnscache in stringsDesejadasDnscache)
                {
                    if (contents.Contains(stringDesejadaDnscache))
                    {
                        stringsEncontradasDnscache.Add(stringDesejadaDnscache);
                    }
                }

                string joinedCustomNamesDnsCache = "";
                foreach (string stringEncontradaDnscache in stringsEncontradasDnscache)
                {
                    string nomePersonalizadoDnscache = customNamesDnsCache.ContainsKey(stringEncontradaDnscache) ? customNamesDnsCache[stringEncontradaDnscache] : stringEncontradaDnscache;

                    joinedCustomNamesDnsCache += nomePersonalizadoDnscache + "\n ";
                }
                joinedCustomNamesDnsCache = joinedCustomNamesDnsCache.TrimEnd(',', ' ', '\n');

                foundDnsCacheInfo += "\n " + joinedCustomNamesDnsCache + "\n";
            }


            foundDpsInfo = "";
            if (System.IO.File.Exists("C:\\dps.txt"))
            {
                string contents = System.IO.File.ReadAllText("C:\\dps.txt");

                HashSet<string> stringsEncontradasDps = new HashSet<string>();

                foreach (string stringDesejadaDps in stringsDesejadasDps)
                {
                    int indexInicio = contents.IndexOf(stringDesejadaDps);
                    while (indexInicio != -1)
                    {
                        int indexFim = contents.IndexOfAny(new char[] { ' ', '\n', '\r', ';', ',', '<', '>' }, indexInicio);
                        if (indexFim == -1)
                        {
                            indexFim = contents.Length;
                        }

                        string stringEncontradaDps = contents.Substring(indexInicio, indexFim - indexInicio).Trim();
                        stringsEncontradasDps.Add(stringEncontradaDps);

                        indexInicio = contents.IndexOf(stringDesejadaDps, indexFim);
                    }
                }

                string joinedCustomNamesDps = "";
                foreach (string stringEncontradaDps in stringsEncontradasDps)
                {
                    string nomePersonalizadoDps = customNamesDps.ContainsKey(stringEncontradaDps) ? customNamesDps[stringEncontradaDps] : stringEncontradaDps;

                    joinedCustomNamesDps += nomePersonalizadoDps + "\n ";
                }
                joinedCustomNamesDps = joinedCustomNamesDps.TrimEnd(',', ' ', '\n');

                foundDpsInfo += "\n " + joinedCustomNamesDps + "\n";
            }
            Console.WriteLine(foundDpsInfo);

            foundDiagTrackInfo = "";
            if (System.IO.File.Exists("C:\\Diagtrack.txt"))
            {
                string contents = System.IO.File.ReadAllText("C:\\Diagtrack.txt");

                List<string> stringsEncontradasDiagtrack = new List<string>();

                foreach (string stringDesejadaDiagtrack in stringsDesejadasDiagtrack)
                {
                    if (contents.Contains(stringDesejadaDiagtrack))
                    {
                        stringsEncontradasDiagtrack.Add(stringDesejadaDiagtrack);
                    }
                }

                string joinedCustomNamesDiagTrack = "";
                foreach (string stringEncontradaDiagtrack in stringsEncontradasDiagtrack)
                {
                    string nomePersonalizadoDiagtrack = customNamesDiagTrack.ContainsKey(stringEncontradaDiagtrack) ? customNamesDiagTrack[stringEncontradaDiagtrack] : stringEncontradaDiagtrack;

                    joinedCustomNamesDiagTrack += nomePersonalizadoDiagtrack + "\n ";
                }
                joinedCustomNamesDiagTrack = joinedCustomNamesDiagTrack.TrimEnd(',', ' ', '\n');

                foundDiagTrackInfo += "\n " + joinedCustomNamesDiagTrack + "\n";
            }

            foundSysmainInfo = "";
            if (System.IO.File.Exists("C:\\sysmain.txt"))
            {
                string contents = System.IO.File.ReadAllText("C:\\sysmain.txt");

                List<string> stringsEncontradasSysmain = new List<string>();

                foreach (string stringDesejadaSysmain in stringsDesejadasSysmain)
                {
                    string pattern = $@"{Regex.Escape(stringDesejadaSysmain)}-\w+\.pf";
                    MatchCollection matches = Regex.Matches(contents, pattern, RegexOptions.IgnoreCase);

                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            stringsEncontradasSysmain.Add(match.Value);
                        }
                    }
                }

                string joinedCustomNamesSysmain = "";
                foreach (string stringEncontradaSysmain in stringsEncontradasSysmain)
                {
                    string nomePersonalizadoSysmain = customNamesSysmain.ContainsKey(stringEncontradaSysmain) ? customNamesSysmain[stringEncontradaSysmain] : stringEncontradaSysmain;

                    joinedCustomNamesSysmain += nomePersonalizadoSysmain + "\n";
                }
                joinedCustomNamesSysmain = joinedCustomNamesSysmain.TrimEnd(',', ' ', '\n');

                foundSysmainInfo += "\n " + joinedCustomNamesSysmain + "\n";
            }
            foundHistoryInfo = "";
            if (System.IO.File.Exists("C:\\Historico.txt"))
            {
                string contents = System.IO.File.ReadAllText("C:\\Historico.txt");

                List<string> stringsEncontradasHistorico = new List<string>();

                foreach (string stringDesejadaHistorico in stringsDesejadasHistorico)
                {
                    if (contents.Contains(stringDesejadaHistorico))
                    {
                        int indexInicio = contents.IndexOf(stringDesejadaHistorico);
                        if (indexInicio != -1)
                        {
                            int indexFim = contents.IndexOfAny(new char[] { '/', '?', ':', '.' }, indexInicio + stringDesejadaHistorico.Length);
                            if (indexFim != -1)
                            {
                                string stringEncontradaHistorico = contents.Substring(indexInicio, indexFim - indexInicio).Trim();
                                stringsEncontradasHistorico.Add(stringEncontradaHistorico);
                            }
                        }
                    }
                }

                string joinedCustomNamesHistory = "";
                foreach (string stringEncontradaHistorico in stringsEncontradasHistorico)
                {
                    string nomePersonalizadoHistorico = customNamesHistory.ContainsKey(stringEncontradaHistorico) ? customNamesHistory[stringEncontradaHistorico] : stringEncontradaHistorico;

                    joinedCustomNamesHistory += nomePersonalizadoHistorico + ", ";
                }
                joinedCustomNamesHistory = joinedCustomNamesHistory.TrimEnd(',', ' ', '\n');

                foundHistoryInfo += "\n " + joinedCustomNamesHistory + "\n";
            }
            Console.WriteLine(customNamesHistory);
            foundPcaSvcInfo = "";
            if (System.IO.File.Exists("C:\\pcasvc.txt"))
            {
                string contents = System.IO.File.ReadAllText("C:\\pcasvc.txt");

                List<string> stringsEncontradasPcasvc = new List<string>();

                foreach (string stringDesejadaPcasvc in stringsDesejadasPcasvc)
                {
                    if (contents.Contains(stringDesejadaPcasvc))
                    {
                        stringsEncontradasPcasvc.Add(stringDesejadaPcasvc);
                    }
                }

                string joinedCustomNamesPcaSvc = "";
                foreach (string stringEncontradaPcasvc in stringsEncontradasPcasvc)
                {
                    string nomePersonalizadoPcasvc = customNamesPcaSvc.ContainsKey(stringEncontradaPcasvc) ? customNamesPcaSvc[stringEncontradaPcasvc] : stringEncontradaPcasvc;

                    joinedCustomNamesPcaSvc += nomePersonalizadoPcasvc + "\n ";
                }

                joinedCustomNamesPcaSvc = joinedCustomNamesPcaSvc.TrimEnd(',', ' ');

                foundPcaSvcInfo += "\n " + joinedCustomNamesPcaSvc + "\n";
            }
            pcaclientInfo = "";

            if (System.IO.File.Exists("C:\\explorer.txt"))
            {
                string contents = System.IO.File.ReadAllText("C:\\explorer.txt");
                int index = contents.IndexOf(",PcaClient,", StringComparison.OrdinalIgnoreCase);

                while (index != -1)
                {
                    
                    int startIndex = contents.LastIndexOf("\n", index);
                    if (startIndex == -1)
                    {
                        startIndex = 0;
                    }
                    else
                    {
                        startIndex += 1; 
                    }

                   
                    int endIndex = contents.IndexOf("\n", index);
                    if (endIndex == -1)
                    {
                        endIndex = contents.Length;
                    }

                   
                    string line = contents.Substring(startIndex, endIndex - startIndex);

                    
                    string[] parts = line.Split(',');
                    if (parts.Length > 5)
                    {
                        string filePath = parts[5];
                        pcaclientInfo += $"{filePath}\n\n";
                    }
                    index = contents.IndexOf(",PcaClient,", index + 1, StringComparison.OrdinalIgnoreCase);
                }
                Console.WriteLine(pcaclientInfo);
            }
            additionalInfo = "";
            try
            {
                if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VMware, Inc.\VMware Tools") != null)
                {
                    additionalInfo += "> **VM Detected:** VMware\n";
                }
                if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Oracle\VirtualBox Guest Additions") != null)
                {
                    additionalInfo += "> **VM Detected:** VirtualBox\n";
                }
                if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\QEMU") != null)
                {
                    additionalInfo += "> **VM Detected:** QEMU\n";
                }
            }
            catch { }

            try
            {
                if (Process.GetProcessesByName("SystemInformer").Length > 0 || Process.GetProcessesByName("ProcessHacker").Length > 0)
                {
                    additionalInfo += "> **Suspicious Process:** SystemInformer/ProcessHacker\n";
                }
                if (Process.GetProcessesByName("cheatengine").Length > 0 || Process.GetProcessesByName("x64dbg").Length > 0 || Process.GetProcessesByName("ollydbg").Length > 0 || Process.GetProcessesByName("dnSpy").Length > 0)
                {
                    additionalInfo += "> **Suspicious Process:** CheatEngine/Debugger\n";
                }
                if (Process.GetProcessesByName("autoit3").Length > 0)
                {
                    additionalInfo += "> **Suspicious Process:** AutoIT\n";
                }
            }
            catch { }

            try
            {
                string[] recycleBinFiles = Directory.GetFiles(@"C:\$Recycle.Bin\", "*.*", SearchOption.TopDirectoryOnly);
                foreach (string file in recycleBinFiles)
                {
                    if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(file).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        additionalInfo += "> **Recycle Bin File:** " + file + "\n";
                    }
                }
            }
            catch { }

            try
            {
                string[] prefetchPatterns = { "*CHEAT*", "*X64DBG*", "*PROCESSHACKER*", "*SYSTEMINFORMER*" };
                foreach (string pattern in prefetchPatterns)
                {
                    string[] prefetchFiles = Directory.GetFiles(@"C:\Windows\Prefetch\", pattern, SearchOption.TopDirectoryOnly);
                    foreach (string file in prefetchFiles)
                    {
                        additionalInfo += "> **Suspicious Prefetch:** " + file + "\n";
                    }
                }
            }
            catch { }

            try
            {
                string[] stoppedServices = { "SysMain", "DiagTrack", "PcaSvc", "DPS", "Dnscache" };
                foreach (string serviceName in stoppedServices)
                {
                    using (ServiceController sc = new ServiceController(serviceName))
                    {
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            additionalInfo += "> **Service Stopped:** " + serviceName + "\n";
                        }
                    }
                }
            }
            catch { }

            try
            {
                using (RegistryKey bamKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\bam\UserSettings"))
                {
                    if (bamKey != null)
                    {
                        string[] cheatNames = { "cheatengine", "x64dbg", "ollydbg", "processhacker", "systeminformer" };
                        foreach (string subKeyName in bamKey.GetSubKeyNames())
                        {
                            using (RegistryKey subKey = bamKey.OpenSubKey(subKeyName))
                            {
                                if (subKey == null) continue;
                                foreach (string valueName in subKey.GetValueNames())
                                {
                                    foreach (string cheat in cheatNames)
                                    {
                                        if (valueName.ToLower().Contains(cheat))
                                        {
                                            additionalInfo += "> **BAM Registry:** " + valueName + "\n";
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

            try
            {
                string downloadsPath = Path.Combine(@"C:\Users\", Environment.UserName, @"Downloads");
                if (Directory.Exists(downloadsPath))
                {
                    string[] exeFiles = Directory.GetFiles(downloadsPath, "*.exe", SearchOption.TopDirectoryOnly);
                    foreach (string file in exeFiles)
                    {
                        if ((DateTime.Now - System.IO.File.GetLastWriteTime(file)).TotalHours <= 24)
                        {
                            additionalInfo += "> **Recent Download EXE:** " + file + "\n";
                        }
                    }
                }
            }
            catch { }
            SecurityTools.OceanScan.CurrentProgress = 93;

            bool dllScanHit = false;
            var dllHits = new List<string>();
            try
            {
                // Reuse the full scan's cached results — no second disk pass.
                dllHits = new List<string>(SecurityTools.OceanScan.LastDllScanHits);
                if (dllHits.Count > 0)
                {
                    dllScanHit = true;
                    _oceanReport += "\n> **Full DLL / Module Scan:**\n> - "
                        + string.Join("\n> - ", dllHits) + "\n";
                    additionalInfo += "> **Full DLL Scan (all processes + disk):**\n```"
                        + string.Join("\n", dllHits) + "```\n";
                }
            }
            catch { }

            if (_scanEngineFailed)
            {
                // Engine died mid-scan — never fabricate CLEAN / REVIEW from
                // partial data; the failure itself is reported as the verdict.
                _oceanVerdict = "ERROR";
                _oceanScore = 0;
                _oceanReasons = new List<string> { "detection engine failed mid-scan" };
            }
            else
            {
                // Everything found above (Lsass / Explorer / DPS / SysMain / DNS /
                // DiagTrack / PCA / history strings, VM, recycle bin, prefetch,
                // stopped services, BAM, recent download EXEs, DLL-scan hits) is
                // promoted into the official hit list and the verdict is recomputed —
                // the scan can then NEVER end as CLEAN while any trace exists.
                PromoteFindingsToVerdict(dllHits);
                string finalVerdict = "CLEAN";
                var finalReasons = new List<string>();
                int finalScore = SecurityTools.OceanScan.ComputeScore(_oceanHits, out finalVerdict, out finalReasons);
                _oceanScore = finalScore;
                _oceanVerdict = finalVerdict;
                _oceanReasons = finalReasons;

                // Refresh the report header so it reflects the recomputed verdict.
                try
                {
                    string verdictLine = "> **Verdict: " + finalVerdict + " — Threat Score " + finalScore + "/100**" +
                        (finalReasons.Count > 0 ? "  (Key signals: " + string.Join(", ", finalReasons) + ")" : "") + "\n\n";
                    int nl = _oceanReport.IndexOf('\n');
                    _oceanReport = verdictLine + (nl >= 0 ? _oceanReport.Substring(nl + 1) : "");
                    System.IO.File.WriteAllText(@"C:\OceanScanReport.txt", _oceanReport);
                }
                catch { }
            }
            SecurityTools.OceanScan.CurrentProgress = 97;

            if (!string.IsNullOrEmpty(foundLsassInfo) || dllScanHit || _oceanHits.Any(h => h.Category == "Detects") || _oceanVerdict != "CLEAN")
            {


                string windowsEdition = GetSystemInfoSummary();
                string pinFormatado = $"__{generatedPin}__";
                string HWID = $"__{System.Security.Principal.WindowsIdentity.GetCurrent().User.Value}__";
                string PCNAME = $"__{Dns.GetHostEntry(Environment.MachineName).HostName.ToString()}__";
                string UserS = $"__{Environment.UserName.ToString()}__";
                string currentDateTime = $"__{GetCurrentDateTime()}__";
                string ipv4Address = $"__{GetIPv4Address()}__";
                string DetectedLsass = $"```{foundLsassInfo}```";
                string DetectedDps = $"```{foundDpsInfo}```";
                string DetectedDiagtrack = $"```{foundDiagTrackInfo}```";
                string DetectedSysmain = $"```{foundSysmainInfo}```";
                string DetectedDnscache = $"```{foundDnsCacheInfo}```";
                string DetectedExplorer = $"```{foundExplorerInfo}```";
                string DetectedHistorico = $"```{foundHistoryInfo}```";
                string DetectedPcasvc = $"```{foundPcaSvcInfo}```";
                string Pcaclient = $"```{pcaclientInfo}```"; 



                string[] serviceNames = { "Sysmain", "Pcasvc", "DPS", "Diagtrack" };

                string resultString = GetServiceStatusString(serviceNames);

                var message = new DiscordMessage()
                    .SetUsername("Ocean ac - " + _oceanVerdict)
                    .SetAvatar("https://cdn.discordapp.com/attachments/1232015922424713267/1236330340323295343/pngwing.com_3.png")
                    .AddEmbed()
                        .SetTimestamp(DateTime.Now)
                        .SetTitle("\nOcean ac - " + _oceanVerdict + "  (" + _oceanScore + "/100)")
                        .SetDescription(
                            "> **User:** " + UserS +
                            "\n > **Pc Name:** " + PCNAME +
                            "\n > **Date:** " + currentDateTime +
                            "\n > **PC:** " + windowsEdition +

                            //"\n > **IP:** " + ipv4Address +
                            //"\n > **HWID:** " + HWID +
                            "\n > **Pin:** " + pinFormatado + "\n" +
                            "\n > **Pc Start Time:** " + $"__{GetSystemStartTimeAsString()}__" + "\n" + $"{resultString}" +
                            "\n > **Lsass:**" + "\n" + DetectedLsass +
                             "\n > **Explorer:**" + "\n" + DetectedExplorer +
                            "\n > **DPS:**" + "\n" + DetectedDps +
                            "\n > **Sysmain:**" + "\n" + DetectedSysmain +
                            "\n > **DnsCache:**" + "\n" + DetectedDnscache +
                            "\n > **DiagTrack:**" + "\n" + DetectedDiagtrack +
                            "\n > **PcaSvc:**" + "\n" + DetectedPcasvc +
                            "\n > **Web History:**" + "\n" + DetectedHistorico +
                             "\n > **Pcaclient:**" + "\n" + Pcaclient +
                            (string.IsNullOrEmpty(additionalInfo) ? "" : "\n > **Advanced Detections:**\n" + $"```{additionalInfo}```")

                        )
                        .SetColor(00000000)
                        .SetFooter("Ocean ac - Logs", "https://cdn.discordapp.com/attachments/1232015922424713267/1236330340323295343/pngwing.com_3.png")
                        .Build();



                message.SendMessage(Result);
                PostScanResultToApi(_oceanVerdict.ToLowerInvariant());

            }
            else
            {
                string windowsEdition = GetSystemInfoSummary();
                string pinFormatado = $"||{generatedPin}||";
                string HWID = $"__{System.Security.Principal.WindowsIdentity.GetCurrent().User.Value}__";
                string PCNAME = $"__{Dns.GetHostEntry(Environment.MachineName).HostName.ToString()}__";
                string UserS = $"__{Environment.UserName.ToString()}__";
                string currentDateTime = $"__{GetCurrentDateTime()}__";
                string ipv4Address = $"__{GetIPv4Address()}__";
                string Pcaclient = $"```{pcaclientInfo}```";


                var message = new DiscordMessage()
                   .SetUsername("Ocean ac - " + _oceanVerdict)
                   .SetAvatar("https://cdn.discordapp.com/attachments/1232015922424713267/1236330340323295343/pngwing.com_3.png")
                   .AddEmbed()
                       .SetTimestamp(DateTime.Now)
                .SetTitle("\nOcean ac - " + _oceanVerdict + "  (" + _oceanScore + "/100)")
                       .SetDescription(
                           "> **User:** " + UserS +
                            "\n > **Pc Name:** " + PCNAME +
                            "\n > **Date:** " + currentDateTime +
                            "\n > **PC:** " + windowsEdition +

                            // "\n > **IP:** " + ipv4Address +
                            //"\n > **HWID:** " + HWID +
                            "\n > **Pin:** " + pinFormatado +
                           "\n > **Resultado** " + $"__{_oceanVerdict + " - " + _oceanScore + "/100"}__" +
                            "\n > **PcaClient:** " + Pcaclient +
                            (string.IsNullOrEmpty(additionalInfo) ? "" : "\n > **Advanced Detections:**\n" + $"```{additionalInfo}```")


                       )
                       .SetColor(00000000)
                       .SetFooter("Ocean ac - Logs", "https://cdn.discordapp.com/attachments/1232015922424713267/1236330340323295343/pngwing.com_3.png")
                       .Build();



                message.SendMessage(Result);
                PostScanResultToApi(_oceanVerdict.ToLowerInvariant());
            }

            SecurityTools.OceanScan.CurrentProgress = 99;
            Thread.Sleep(3000);
            string[] filesToDelete = { @"C:\dnscache.txt", @"C:\Historico.txt", @"C:\strings.exe", @"C:\diagtrack.txt", @"C:\PcaSvc.txt", @"C:\sysmain.txt", @"C:\DPS.txt", @"C:\Lsass.txt" };

            foreach (string filePath in filesToDelete)
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }
        private void PromoteFindingsToVerdict(List<string> dllScanHits)
        {
            var promoted = new List<SecurityTools.OceanScan.Hit>();
            Action<string, string, string, string> push =
                (cat, name, badge, detail) => promoted.Add(new SecurityTools.OceanScan.Hit(cat, name, badge, detail));

            if (!string.IsNullOrEmpty(foundLsassInfo))
                push("Detects", "Lsass Memory — Cheat / Executable Strings", "RAM",
                    "Lsass dump matched cheat / poker-key / executable markers: " + TrimDetail(foundLsassInfo));
            if (!string.IsNullOrEmpty(foundExplorerInfo))
                push("Detects", "Explorer Memory — Cheat Strings", "RAM",
                    "Explorer dump matched cheat signatures: " + TrimDetail(foundExplorerInfo));
            if (!string.IsNullOrEmpty(foundDpsInfo))
                push("Suspicious", "DPS Log — Cheat Strings", "Log",
                    "Diagnostic Policy Service dump contains cheat markers: " + TrimDetail(foundDpsInfo));
            if (!string.IsNullOrEmpty(foundSysmainInfo))
                push("Suspicious", "SysMain Log — Cheat Strings", "Log",
                    "SysMain activity dump contains cheat markers: " + TrimDetail(foundSysmainInfo));
            if (!string.IsNullOrEmpty(foundDnsCacheInfo))
                push("Suspicious", "DNS Cache — Suspicious Strings", "Network",
                    "DNS cache dump contains cheat-related names: " + TrimDetail(foundDnsCacheInfo));
            if (!string.IsNullOrEmpty(foundDiagTrackInfo))
                push("Suspicious", "DiagTrack — Suspicious Strings", "Log",
                    "Connected User Experiences log contains cheat markers: " + TrimDetail(foundDiagTrackInfo));
            if (!string.IsNullOrEmpty(foundPcaSvcInfo))
                push("Detects", "PCA Service — Suspicious Strings", "Log",
                    "Program Compatibility Assistant data hints cheat-tool execution: " + TrimDetail(foundPcaSvcInfo));
            if (!string.IsNullOrEmpty(pcaclientInfo))
                push("Suspicious", "PCA Client — Suspicious Strings", "Log",
                    "PCA client feed contains suspicious entries: " + TrimDetail(pcaclientInfo));
            if (!string.IsNullOrEmpty(foundHistoryInfo))
                push("Suspicious", "Browser History — Cheat Residue", "Forensic",
                    "Browser history references cheat / loader sites: " + TrimDetail(foundHistoryInfo));

            if (dllScanHits != null && dllScanHits.Count > 0)
                push("Detects", "Cheat Modules — Full DLL / Module Scan", "DLL",
                    "Loaded modules + disk scan hit " + dllScanHits.Count + " cheat module(s): " +
                    string.Join(", ", dllScanHits.Take(4)));

            if (additionalInfo.IndexOf("VM Detected:", StringComparison.Ordinal) >= 0)
                push("Warnings", "Virtual Machine Detected", "Systems",
                    "The machine runs inside a virtualized environment (VMware / VirtualBox / QEMU).");
            if (additionalInfo.IndexOf("Suspicious Process:", StringComparison.Ordinal) >= 0)
                push("Detects", "Suspicious Process Running", "Process",
                    "ProcessHacker / SystemInformer / CheatEngine / debugger / AutoIT is currently running.");
            if (additionalInfo.IndexOf("Recycle Bin File:", StringComparison.Ordinal) >= 0)
                push("Detects", "Recycle Bin Executable Artifacts", "Files",
                    "Executable / DLL files sit in the recycle bin (cheat dumps are frequently trashed there).");
            if (additionalInfo.IndexOf("Suspicious Prefetch:", StringComparison.Ordinal) >= 0)
                push("Detects", "Suspicious Prefetch Artifacts", "Prefetch",
                    "Prefetch still records execution of cheat / debugger tooling.");
            if (additionalInfo.IndexOf("Service Stopped:", StringComparison.Ordinal) >= 0)
                push("Suspicious", "Forensic Services Manually Stopped", "Service",
                    "SysMain / DiagTrack / PcaSvc / DPS / DnsCache were stopped — a common cheat-cleaner action.");
            if (additionalInfo.IndexOf("BAM Registry:", StringComparison.Ordinal) >= 0)
                push("Detects", "BAM Registry — Suspicious Execution", "BAM",
                    "Background Activity Moderator still records suspicious tool execution.");
            if (additionalInfo.IndexOf("Recent Download EXE:", StringComparison.Ordinal) >= 0)
                push("Suspicious", "Recent Download Executables", "Files",
                    "Executables were downloaded into the user Downloads folder in the last 24h (paths in the report).");

            foreach (SecurityTools.OceanScan.Hit h in promoted)
            {
                if (!_oceanHits.Any(x => x.Name == h.Name && x.Badge == h.Badge && x.Detail == h.Detail))
                    _oceanHits.Add(h);
            }
        }

        private static string TrimDetail(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " | ");
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            s = s.Trim(' ', '|');
            return s.Length > 700 ? s.Substring(0, 700) + "…" : s;
        }

        private void PostScanResultToApi(string status)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string user = Environment.UserName;
                    string pc = Dns.GetHostEntry(Environment.MachineName).HostName;
                    string os = GetWindowsEdition();

                    List<object> MapCat(string cat)
                    {
                        return _oceanHits
                            .Where(h => h.Category == cat)
                            .Select(h => (object)new { name = h.Name, badge = h.Badge, desc = h.Detail })
                            .ToList();
                    }

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        username = user,
                        playerName = user,
                        pcName = pc,
                        os = os,
                        systemInfo = GetSystemInfoSummary(),
                        windowsEdition = os,
                        status = status,
                        result = status,
                        score = _oceanScore,
                        verdict = _oceanVerdict,
                        verdictReasons = _oceanReasons.ToArray(),
                        hwid = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value,
                        keyId = usedKeyId,
                        pin = usedPin,
                        game = "FiveM",
                        visibility = "Private",
                        ip = GetIPv4Address(),
                        ocean = _oceanReport,
                        detections = new
                        {
                            detects = MapCat("Detects"),
                            warnings = MapCat("Warnings"),
                            suspicious = MapCat("Suspicious"),
                            systems = MapCat("Systems"),
                            integrity = MapCat("Integrity")
                        },
                        activityLog = SecurityTools.OceanScan.CollectRecentActivity(25),
                        discordAccounts = SecurityTools.OceanScan.CollectDiscordAccounts(),
                        recordingSoftware = SecurityTools.OceanScan.CollectRecordingSoftware(),
                        appliedConfig = _remoteCfg
                    });
                    client.UploadString(ApiBase + "/api/scans", "POST", json);
                }
            }
            catch { }
        }
        // Hunt for FiveM-specific cheat remnants, configs and account/license
        // swap leftovers inside the FiveM data folders + Downloads + Temp.
        private string CollectFiveM()
        {
            var hits = new List<string>();
            string kb(string n) { return n.ToLowerInvariant(); }
            string[] ever = { "cherax", "ozark", "redengine", "eulen", "phantomx", "phantom-x", "asphyx", "kiddion", "ftools", "freemenu", "nightfall", "oblivion", "noctis", "nixus", "fireshield" };
            string[] fiveMOnly = { "luna", "vanguard", "twiizer", "spectre", "lienzo", "account switch", "accountswitcher", "license switch", "licensechange", "paidmenu", "hades", "dware" };

            var dirs = new List<string>();
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FiveM Application Data")); } catch { }
            try { dirs.Add(Path.GetTempPath()); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }

            foreach (string d in dirs)
            {
                if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                bool fiveMRoot = kb(d).Contains("fivem");

                var files = new List<string>();
                try { files.AddRange(Directory.EnumerateFiles(d, "*", SearchOption.TopDirectoryOnly)); } catch { }

                foreach (string kw in ever)
                {
                    foreach (string f in files)
                    {
                        if (kb(Path.GetFileName(f)).Contains(kw) && hits.Count < 12)
                            hits.Add(Path.GetFileName(f) + "  (" + d + ")");
                    }
                }
                if (fiveMRoot)
                {
                    foreach (string kw in ever.Concat(fiveMOnly))
                    {
                        foreach (string f in files)
                        {
                            if (kb(Path.GetFileName(f)).Contains(kw) && hits.Count < 12)
                                hits.Add(Path.GetFileName(f) + "  (" + d + ")");
                        }
                    }
                    // Cheat configs often hide inside cache/mount subfolders.
                    try
                    {
                        foreach (string sub in Directory.EnumerateDirectories(d, "*", SearchOption.TopDirectoryOnly))
                        {
                            string sn = kb(Path.GetFileName(sub));
                            if (ever.Any(k => sn.Contains(k)) && hits.Count < 12)
                                hits.Add(Path.GetFileName(sub) + "\\  (folder in FiveM data)");
                        }
                    }
                    catch { }
                }
            }

            return hits.Count == 0 ? "" : string.Join("\n", hits);
        }

        static void AddToDictionary(Dictionary<string, string> dicionario, string chave, string valor)
        {
            if (!dicionario.ContainsKey(chave))
            {
                dicionario.Add(chave, valor);
                Console.WriteLine($"Added: Key '{chave}', Value '{valor}'");
            }
            else
            {
                Console.WriteLine($"Key '{chave}' already exists, skipping '{valor}'...");
            }
        }
        private string GetWindowsEdition()
        {
            const string registryKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            const string registryValue = "ProductName";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey))
            {
                if (key != null)
                {
                    object value = key.GetValue(registryValue);

                    if (value != null)
                    {
                        return value.ToString();
                    }
                }
            }

            return "Edition not found";
        }

        private string GetSystemInfoSummary()
        {
            var sb = new StringBuilder();
            Action<string> seg = (s) => { if (!string.IsNullOrWhiteSpace(s)) sb.AppendLine(s); };
            try { seg("OS: " + GetWindowsEdition()); } catch { }
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k != null)
                    {
                        string display = (k.GetValue("DisplayVersion") ?? "").ToString();
                        string build = (k.GetValue("CurrentBuildNumber") ?? "").ToString();
                        string ubr = (k.GetValue("UBR") ?? "").ToString();
                        seg("Windows Build: " + (string.IsNullOrEmpty(display) ? "" : display + " ") + build +
                            (string.IsNullOrEmpty(ubr) ? "" : " (UBR " + ubr + ")"));
                        string buildLab = (k.GetValue("BuildLabEx") ?? "").ToString();
                        seg("Edition: " + (k.GetValue("EditionID") ?? "unknown"));
                    }
                }
            }
            catch { }
            try { seg("Architecture: " + (IntPtr.Size * 8) + "-bit"); } catch { }
            try
            {
                using (var mo = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in mo.Get())
                    {
                        string n = (obj["Name"] ?? "").ToString().Trim();
                        if (string.IsNullOrEmpty(n)) continue;
                        string cores = (obj["NumberOfCores"] ?? "0").ToString();
                        string threads = (obj["NumberOfLogicalProcessors"] ?? "0").ToString();
                        seg("CPU: " + n + "  (" + cores + " cores / " + threads + " threads)");
                        break;
                    }
                }
            }
            catch { }
            try
            {
                using (var mo = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in mo.Get())
                    {
                        ulong t;
                        if (ulong.TryParse((obj["TotalPhysicalMemory"] ?? "0").ToString(), out t))
                        {
                            seg("RAM: " + Math.Round(t / 1024.0 / 1024.0 / 1024.0, 1) + " GB");
                            break;
                        }
                    }
                }
            }
            catch { }
            try
            {
                var sticks = new List<string>();
                using (var mo = new ManagementObjectSearcher("SELECT Manufacturer, Capacity, Speed, DeviceLocator FROM Win32_PhysicalMemory"))
                {
                    ulong stickGb;
                    foreach (ManagementObject obj in mo.Get())
                    {
                        if (ulong.TryParse((obj["Capacity"] ?? "0").ToString(), out stickGb) && stickGb > 0)
                        {
                            string loc = (obj["DeviceLocator"] ?? "").ToString();
                            string speed = (obj["Speed"] ?? "0").ToString();
                            sticks.Add(loc + " " + Math.Round(stickGb / 1024.0 / 1024.0 / 1024.0, 0) + "GB" + (speed != "0" ? " " + speed + "MHz" : ""));
                        }
                    }
                }
                if (sticks.Count > 0) seg("RAM Sticks: " + string.Join(" | ", sticks));
            }
            catch { }
            try
            {
                using (var mo = new ManagementObjectSearcher("SELECT Name, DriverVersion FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in mo.Get())
                    {
                        string n = (obj["Name"] ?? "").ToString().Trim();
                        if (!string.IsNullOrEmpty(n))
                        {
                            seg("GPU: " + n + (string.IsNullOrEmpty((obj["DriverVersion"] ?? "").ToString()) ? "" : "  (driver " + obj["DriverVersion"] + ")"));
                        }
                    }
                }
            }
            catch { }
            try
            {
                using (var mo = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in mo.Get())
                    {
                        string m = (obj["Manufacturer"] ?? "").ToString().Trim();
                        string p = (obj["Product"] ?? "").ToString().Trim();
                        if (p.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0) p = "";
                        seg("Motherboard: " + m + (string.IsNullOrEmpty(p) ? "" : " / " + p));
                        break;
                    }
                }
            }
            catch { }
            try
            {
                using (var mo = new ManagementObjectSearcher("SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS"))
                {
                    foreach (ManagementObject obj in mo.Get())
                    {
                        string mfr = (obj["Manufacturer"] ?? "").ToString().Trim();
                        string ver = (obj["SMBIOSBIOSVersion"] ?? "").ToString().Trim();
                        string date = "";
                        try
                        {
                            object d = obj["ReleaseDate"];
                            if (d != null)
                            {
                                string raw = d.ToString();
                                string clean = raw.Replace(".000000+000", "").Trim();
                                DateTime dt;
                                if (DateTime.TryParse(clean, out dt)) date = dt.ToString("yyyy-MM-dd");
                            }
                        }
                        catch { }
                        seg("BIOS: " + mfr + " " + ver + (string.IsNullOrEmpty(date) ? "" : "  (" + date + ")"));
                        break;
                    }
                }
            }
            catch { }
            try { seg("Uptime: " + GetSystemUptimeString()); } catch { }
            try { seg("Host: " + Environment.MachineName + "  User: " + Environment.UserName); } catch { }
            try { seg("Runtime: .NET " + Environment.Version); } catch { }
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("InstallDate");
                        if (v is int)
                        {
                            DateTime install = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)v).ToLocalTime();
                            seg("Windows installed: " + install.ToString("yyyy-MM-dd HH:mm"));
                        }
                    }
                }
            }
            catch { }
            try
            {
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (!d.IsReady) continue;
                        double gb = 1024.0 * 1024.0 * 1024.0;
                        seg("Drive " + d.Name + ": " + Math.Round(d.TotalSize / gb, 0) + " GB  (free " + Math.Round(d.AvailableFreeSpace / gb, 0) + " GB)");
                    }
                    catch { }
                }
            }
            catch { }
            return sb.ToString().TrimEnd();
        }

        private string GetSystemUptimeString()
        {
            try
            {
                using (var mo = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in mo.Get())
                    {
                        object l = obj["LastBootUpTime"];
                        if (l != null)
                        {
                            DateTime boot = ManagementDateTimeConverter.ToDateTime(l.ToString());
                            TimeSpan up = DateTime.Now - boot;
                            return up.Days + "d " + up.Hours + "h " + up.Minutes + "m";
                        }
                    }
                }
            }
            catch { }
            try
            {
                TimeSpan up = TimeSpan.FromMilliseconds(Environment.TickCount);
                return up.Days + "d " + up.Hours + "h " + up.Minutes + "m";
            }
            catch { }
            return "unknown";
        }
        static string GetServiceStatusString(string[] serviceNames)
        {
            string result = "";

            foreach (var serviceName in serviceNames)
            {
                string serviceStatus = GetServiceStatus(serviceName);

                result += $"{serviceStatus}\n";
            }

            return result;
        }


        static string GetServiceStatus(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    return $" > **{serviceName}** : __{sc.Status.ToString().ToLower()}__";
                }
            }
            catch (Exception ex)
            {
                return $" > **{serviceName}** : Erro ao obter status: {ex.Message.ToLower()}";
            }
        }


        static string GetSystemStartTimeAsString()
        {
            DateTime systemStartTime = GetSystemStartTime();
            return systemStartTime.ToString("dd/MM/yyyy - HH:mm:ss");
        }
        static DateTime GetSystemStartTime()
        {

            using (PerformanceCounter uptimeCounter = new PerformanceCounter("System", "System Up Time"))
            {
                uptimeCounter.NextValue();

                float uptimeSeconds = uptimeCounter.NextValue();

                DateTime systemStartTime = DateTime.Now - TimeSpan.FromSeconds(uptimeSeconds);

                return systemStartTime;
            }
        }

        static DateTime GetWindowsInstallDate()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                if (key != null)
                {
                    long installDateValue = (long)key.GetValue("InstallDate", 0);

                    DateTime installDate = DateTime.MinValue;
                    if (installDateValue > 0)
                    {
                        installDate = DateTime.FromFileTime(installDateValue);
                    }

                    return installDate;
                }
                else
                {
                    throw new Exception("Registry key not found.");
                }
            }
        }
        private async void siticoneTextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string licenseKey = (siticoneTextBox2.Text ?? "").Trim();
                if (string.IsNullOrEmpty(licenseKey))
                {
                    SetLabel("enter your license key", System.Drawing.Color.Gray);
                    return;
                }

                SetLabel("verifying key...", System.Drawing.Color.MediumPurple);
                siticoneTextBox2.Enabled = false;

                string validatedKey = await ValidarKeyNaApiAsync(licenseKey);
                if (validatedKey == null)
                {
                    siticoneTextBox2.Enabled = true;
                    siticoneTextBox2.Focus();
                    return;
                }

                // Pull the dashboard's cloud config so the desktop UI + report
                // match what the owner configured (accent, strict, watermark...).
                await FetchScannerConfigAsync(usedKeyId);

                // Valid & marked used. Start the scan.
                siticoneTextBox2.Visible = false;
                siticonePictureBox1.Visible = false;
                SetLabel("spinning up perception grid", System.Drawing.Color.MediumPurple);
                SetLabelLocation(0, 246);

                siticoneVProgressBar1.Invoke((MethodInvoker)(() =>
                {
                    siticoneVProgressBar1.Minimum = 0;
                    siticoneVProgressBar1.Maximum = 100;
                    siticoneVProgressBar1.Value = 0;
                    siticoneVProgressBar1.Text = "0%";
                    siticoneVProgressBar1.ProgressColor = Color.FromArgb(160, 90, 240);
                    siticoneVProgressBar1.ProgressColor2 = Color.FromArgb(147, 51, 234);
                    siticoneVProgressBar1.Visible = true;
                }));

                // Let timer1 drive a smooth 1%, 2%, ... progress while scanning.
                scanInProgress = true;
                progressTicks = 0;

                // Run the scan steps on a background thread so the background
                // animation (timer1) keeps running instead of freezing.
                bool scanFailed = false;
                string stepError1 = null, stepError2 = null, stepError3 = null;
                try
                {
                    await Task.Run(() =>
                    {
                        // Each subsystem runs in its own fence: one failure no
                        // longer kills the whole scan.
                        try { CollectStrings(); }
                        catch (Exception se1) { stepError1 = se1.GetBaseException().Message; }
                        try { CollectServices(); }
                        catch (Exception se2) { stepError2 = se2.GetBaseException().Message; }
                        try { scanner(); }
                        catch (Exception se3) { stepError3 = se3.GetBaseException().Message; }
                    });
                    // Record exactly what failed so the problem is diagnosable.
                    try
                    {
                        System.IO.File.WriteAllText(@"C:\OceanScanError.txt",
                            Newtonsoft.Json.JsonConvert.SerializeObject(new
                            {
                                collectedAt = DateTime.Now.ToString("o"),
                                strings = stepError1,
                                services = stepError2,
                                scanner = stepError3
                            }, Newtonsoft.Json.Formatting.Indented));
                    }
                    catch { }
                    scanFailed = stepError1 != null && stepError2 != null && stepError3 != null;
                }
                catch (Exception scanEx)
                {
                    scanFailed = true;
                    // Never let a background scan failure crash / freeze the UI.
                    try
                    {
                        SetLabel("scan error - " + scanEx.Message, System.Drawing.Color.Red);
                    }
                    catch { }
                }
                finally
                {
                    scanInProgress = false;
                    // Send the finished report to the website so the PIN stops
                    // showing "scanning" and the live report becomes viewable.
                    try
                    {
                        // Local server call — a few ms, safer than fire-and-forget.
                        PostScanResultToApi(scanFailed ? "error" : "completed");
                    }
                    catch { }
                    siticoneVProgressBar1.Invoke((MethodInvoker)(() =>
                    {
                        if (!scanFailed)
                        {
                            siticoneVProgressBar1.Value = 100;
                            siticoneVProgressBar1.Text = "100%";
                            siticoneVProgressBar1.ProgressColor = Color.FromArgb(52, 211, 153);
                            siticoneVProgressBar1.ProgressColor2 = Color.FromArgb(16, 185, 129);
                            siticoneVProgressBar1.Visible = true;
                        }
                        else
                        {
                            siticoneVProgressBar1.Visible = false;
                        }
                    }));
                    // 5-second countdown on the bar, then the app fades and
                    // closes itself.
                    if (!scanFailed)
                    {
                        countdownRunning = true;
                        countdownRemaining = 5.0;
                    }
                }

                if (!scanFailed)
                {
                    SetLabel("verdict published — archived", System.Drawing.Color.FromArgb(74, 222, 128));
                    SetLabelLocation(0, 246);
                    SetLabelFontSize(18);
                }
            }
        }

        private async Task<string> ValidarKeyNaApiAsync(string licenseKey)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    // GET validate then POST use (uses are tracked by maxUses on the dashboard).
                    string validateUrl = ApiBase + "/api/keys/validate/" + Uri.EscapeDataString(licenseKey);
                    string json = client.DownloadString(validateUrl);
                    Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    bool valid = (bool)(obj["valid"] ?? false);
                    if (!valid)
                    {
                        string reason = (obj["reason"] ?? "invalid").ToString();
                        SetLabel("invalid key - " + reason, System.Drawing.Color.Red);
                        return null;
                    }

                    string keyId = (string)(obj["key"]["id"]);
                    usedKeyId = keyId ?? "";
                    usedPin = licenseKey ?? "";
                    // Mark the key as used.
                    using (WebClient post = new WebClient())
                    {
                        post.Headers[HttpRequestHeader.ContentType] = "application/json";
                        post.UploadString(ApiBase + "/api/keys/use/" + Uri.EscapeDataString(keyId), "POST", "{}");
                    }
                    // Tell the website this PIN has started scanning (live status).
                    try
                    {
                        using (WebClient pinStart = new WebClient())
                        {
                            pinStart.Headers[HttpRequestHeader.ContentType] = "application/json";
                            pinStart.UploadString(ApiBase + "/api/pins/start", "POST",
                                Newtonsoft.Json.JsonConvert.SerializeObject(new { code = licenseKey, keyId = keyId }));
                        }
                    }
                    catch { }

                    SetLabel("identity verified - deploying scan", System.Drawing.Color.FromArgb(52, 211, 153));
                    System.Threading.Thread.Sleep(500);
                    return licenseKey;
                }
            }
            catch (Exception ex)
            {
                SetLabel("could not verify key - " + ex.Message, System.Drawing.Color.Red);
                return null;
            }
        }

        // Fetch the owner's cloud config (saved on the dashboard Configs/Custom GUI
        // pages). Applied here: accent color on the progress bar + strict flag +
        // a config snapshot that is echoed back in the scan report.
        private async Task FetchScannerConfigAsync(string keyId)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string url = ApiBase + "/api/scanner/config?keyId=" + Uri.EscapeDataString(keyId ?? "");
                    string json = client.DownloadString(url);
                    Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    Newtonsoft.Json.Linq.JObject cfg = (obj["config"] as Newtonsoft.Json.Linq.JObject) ?? new Newtonsoft.Json.Linq.JObject();
                    _remoteCfg = cfg;
                    _remoteStrict = (bool)(cfg["strictMode"] ?? false);

                    string accent = (string)(cfg["accentColor"] ?? "");
                    System.Drawing.Color col = ParseHexColor(accent);
                    if (!col.IsEmpty)
                    {
                        siticoneVProgressBar1.Invoke((MethodInvoker)(() =>
                        {
                            try
                            {
                                siticoneVProgressBar1.ProgressColor = col;
                                siticoneVProgressBar1.ProgressColor2 = System.Drawing.Color.FromArgb(255,
                                    Math.Max(0, (int)(col.R * 0.72f)),
                                    Math.Max(0, (int)(col.G * 0.72f)),
                                    Math.Max(0, (int)(col.B * 0.72f)));
                            }
                            catch { }
                        }));
                    }
                }
            }
            catch { _remoteCfg = null; }
        }

        private static System.Drawing.Color ParseHexColor(string hex)
        {
            try
            {
                hex = (hex ?? "").Replace("#", "").Trim();
                if (hex.Length == 3)
                    hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
                if (hex.Length != 6) return System.Drawing.Color.Empty;
                return System.Drawing.Color.FromArgb(255,
                    Convert.ToInt32(hex.Substring(0, 2), 16),
                    Convert.ToInt32(hex.Substring(2, 2), 16),
                    Convert.ToInt32(hex.Substring(4, 2), 16));
            }
            catch { return System.Drawing.Color.Empty; }
        }

        private void SetLabel(string text, System.Drawing.Color color)
        {
            try
            {
                label2.Invoke((MethodInvoker)(() =>
                {
                    label2.Text = text;
                    label2.ForeColor = color;
                    label2.AutoSize = true;
                    label2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    label2.Left = (this.ClientSize.Width - label2.Width) / 2;
                }));
            }
            catch { }
        }

        private void SetLabelLocation(int x, int y)
        {
            try
            {
                label2.Invoke((MethodInvoker)(() =>
                {
                    label2.Location = x <= 0
                        ? new Point((this.ClientSize.Width - label2.Width) / 2, y)
                        : new Point(x, y);
                }));
            }
            catch { }
        }

        private void SetLabelFontSize(float size)
        {
            try
            {
                label2.Invoke((MethodInvoker)(() =>
                {
                    label2.Font = new Font(label2.Font.FontFamily, size);
                }));
            }
            catch { }
        }

        private static Color HsvToColor(double hue, double sat = 0.85, double val = 0.95)
        {
            hue = ((hue % 360) + 360) % 360;
            double c = val * sat;
            double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = val - c;
            double r, g, b;
            if (hue < 60) { r = c; g = x; b = 0; }
            else if (hue < 120) { r = x; g = c; b = 0; }
            else if (hue < 180) { r = 0; g = c; b = x; }
            else if (hue < 240) { r = 0; g = x; b = c; }
            else if (hue < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return Color.FromArgb(255,
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255));
        }

        private static Color ColorBlend(Color c1, Color c2, float factor)
        {
            factor = Math.Max(0f, Math.Min(1f, factor));
            int r = (int)(c1.R + (c2.R - c1.R) * factor);
            int g = (int)(c1.G + (c2.G - c1.G) * factor);
            int b = (int)(c1.B + (c2.B - c1.B) * factor);
            return Color.FromArgb(r, g, b);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            frame++;
            AnimateParticles();

            // Smooth hover transitions
            hoverClose += ((isMouseOverClose ? 1f : 0f) - hoverClose) * 0.2f;
            hoverMin += ((isMouseOverMin ? 1f : 0f) - hoverMin) * 0.2f;
            hoverTextBox += (((isMouseOverTextBox || (siticoneTextBox2 != null && siticoneTextBox2.Focused)) ? 1f : 0f) - hoverTextBox) * 0.2f;

            // Smooth cursor easing
            smoothMousePos.X += (currentMousePos.X - smoothMousePos.X) * 0.35f;
            smoothMousePos.Y += (currentMousePos.Y - smoothMousePos.Y) * 0.35f;

            // Dynamic button hover styling
            if (siticoneButton1 != null)
            {
                siticoneButton1.FillColor = ColorBlend(Color.FromArgb(10, 16, 32), Color.FromArgb(220, 38, 38), hoverClose);
                siticoneButton1.BorderColor = ColorBlend(Color.FromArgb(50, 25, 85), Color.FromArgb(248, 113, 113), hoverClose);
            }
            if (siticoneButton2 != null)
            {
                siticoneButton2.FillColor = ColorBlend(Color.FromArgb(10, 16, 32), Color.FromArgb(147, 51, 234), hoverMin);
                siticoneButton2.BorderColor = ColorBlend(Color.FromArgb(50, 25, 85), Color.FromArgb(200, 120, 248), hoverMin);
            }
            if (siticoneTextBox2 != null)
            {
                siticoneTextBox2.BorderColor = ColorBlend(Color.FromArgb(50, 25, 85), Color.FromArgb(200, 120, 248), hoverTextBox);
                siticoneTextBox2.FillColor = ColorBlend(Color.FromArgb(8, 14, 28), Color.FromArgb(12, 22, 44), hoverTextBox);
            }

            // Emit cursor trail
            if (currentMousePos.X >= 0 && currentMousePos.Y >= 0 &&
                (Math.Abs(currentMousePos.X - smoothMousePos.X) > 0.4f || Math.Abs(currentMousePos.Y - smoothMousePos.Y) > 0.4f))
            {
                cursorTrails.Add(new CursorTrail
                {
                    Position = smoothMousePos,
                    Alpha = 1f,
                    Size = 6f,
                    Color = Color.FromArgb(200, 120, 248)
                });
                if (cursorTrails.Count > 25) cursorTrails.RemoveAt(0);
            }

            // Fade cursor trails
            for (int i = cursorTrails.Count - 1; i >= 0; i--)
            {
                var ct = cursorTrails[i];
                ct.Alpha -= 0.05f;
                ct.Size *= 0.94f;
                if (ct.Alpha <= 0 || ct.Size <= 0.4f) cursorTrails.RemoveAt(i);
            }

            // Expand click ripples
            for (int i = clickRipples.Count - 1; i >= 0; i--)
            {
                var cr = clickRipples[i];
                cr.Radius += 2.5f;
                cr.Alpha = Math.Max(0f, 1f - (cr.Radius / cr.MaxRadius));
                if (cr.Radius >= cr.MaxRadius || cr.Alpha <= 0) clickRipples.RemoveAt(i);
            }

            if (scanInProgress)
            {
                progressTicks++;
                // Bar is driven by the ENGINE's real progress (weighted scan
                // stages). A tiny warmup floor keeps the bar alive during the
                // pre-scan collection steps (strings download etc.) without
                // ever faking a 98-99% plateau. The engine finishes at ~90%
                // and the remaining steps decelerate it gently to 100%.
                double engine = OceanScan.CurrentProgress;
                double floor = 1.0 + 5.0 * (1.0 - Math.Exp(-progressTicks / 110.0));
                double raw = Math.Max(engine, floor);
                if (raw > 99.0) raw = 99.0;
                int pv = 0;
                try
                {
                    int cur = siticoneVProgressBar1.Value;
                    int target = (int)Math.Round(raw);
                    if (target < cur + 1) target = cur + 1;          // only climb
                    if (target > 99) target = 99;                     // 100 only at true end
                    int next = target;
                    if (target > 90)
                    {
                        // The final stretch decelerates on purpose — the last
                        // 10% creeps home instead of parking at 98-99%.
                        next = cur + Math.Max(1, (int)Math.Ceiling((target - cur) * 0.20));
                        if (next > 99) next = 99;
                    }
                    if (next > cur)
                    {
                        siticoneVProgressBar1.Value = next;
                        siticoneVProgressBar1.Text = next + "%";
                    }
                    pv = siticoneVProgressBar1.Value;

                    if (pv > 85)
                    {
                        siticoneVProgressBar1.ProgressColor = Color.FromArgb(52, 211, 153);
                        siticoneVProgressBar1.ProgressColor2 = Color.FromArgb(16, 185, 129);
                    }
                    else
                    {
                        // flowing rainbow gradient while scanning
                        double hue = progressTicks / 2.0;
                        siticoneVProgressBar1.ProgressColor = HsvToColor(hue);
                        siticoneVProgressBar1.ProgressColor2 = HsvToColor(hue + 55);
                    }
                }
                catch { }
                try
                {
                    label2.Invoke((MethodInvoker)(() =>
                    {
                        label2.Text = "scanning " + pv + "%";
                        label2.ForeColor = pv > 85 ? Color.FromArgb(52, 211, 153) : Color.FromArgb(216, 180, 254);
                        label2.AutoSize = true;
                        label2.Left = (this.ClientSize.Width - label2.Width) / 2;
                    }));
                }
                catch { }
            }
            else if (countdownRunning)
            {
                countdownRemaining -= 0.016;
                int secs = (int)Math.Ceiling(Math.Max(0.0, countdownRemaining));
                try
                {
                    siticoneVProgressBar1.Text = secs <= 0 ? "" : secs.ToString();
                }
                catch { }
                if (countdownRemaining <= 0.0)
                {
                    countdownRunning = false;
                    closePhase = true;
                    closeTick = 0;
                }
            }
            else if (closePhase)
            {
                closeTick++;
                if (closeTick >= 30)
                {
                    this.Close();
                    return;
                }
                try
                {
                    this.Opacity = Math.Max(0.0, 1.0 - closeTick / 30.0);
                }
                catch { }
            }
            Invalidate();
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (this.Opacity < 1D)
            {
                this.Opacity = Math.Min(1D, this.Opacity + 0.06D);
            }
            else
            {
                this.Opacity = 1D;
                timer2.Stop();
            }
        }
        private void siticoneButton1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_bgCache != null) { _bgCache.Dispose(); _bgCache = null; }
            foreach (var p in particles)
                if (p.Brush != null) p.Brush.Dispose();
            base.OnFormClosed(e);
        }
        private void siticoneButton2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastCursorPosition = this.PointToScreen(e.Location);
                clickRipples.Add(new ClickRipple { Center = e.Location, Radius = 3f, MaxRadius = 55f, Alpha = 1f });
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            currentMousePos = e.Location;
            if (isDragging)
            {
                Point screenPt = this.PointToScreen(e.Location);
                this.Left += screenPt.X - lastCursorPosition.X;
                this.Top += screenPt.Y - lastCursorPosition.Y;
                lastCursorPosition = screenPt;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
            }
        }
        private void siticonePictureBox1_Click(object sender, EventArgs e)
        {

        }
       
        
        private void Response_Click(object sender, EventArgs e)
        {

        }

        private void Response_Click_1(object sender, EventArgs e)
        {

        }

        private void siticoneButton2_Click_1(object sender, EventArgs e)
        {


        }

        private void siticonePanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void siticoneButton3_Click(object sender, EventArgs e)
        {

        }

        private void siticonePictureBox1_Click_1(object sender, EventArgs e)
        {

        }
    }
}
    public class CursorTrail
    {
        public PointF Position { get; set; }
        public float Alpha { get; set; }
        public float Size { get; set; }
        public Color Color { get; set; }
    }

    public class ClickRipple
    {
        public PointF Center { get; set; }
        public float Radius { get; set; }
        public float MaxRadius { get; set; }
        public float Alpha { get; set; }
    }

    public class Particle
    {
        public PointF Position { get; set; }
        public PointF Velocity { get; set; }
        public int Radius { get; set; }
        public System.Drawing.Color Color { get; set; }
        public System.Drawing.Brush Brush { get; set; }
    }



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

        // Live scanner telemetry state (see PostScannerLive).
        private DateTime _liveLastPost = DateTime.MinValue;
        private int _liveLastPct = -1;
        private int _liveSentLines;
        private int frame;
        private bool countdownRunning;
        private double countdownRemaining;
        private bool closePhase;
        private int closeTick;
        private List<Particle> particles = new List<Particle>();

        /// <summary>Every particle shares this brush — they all use the same base colour.</summary>
        private static readonly SolidBrush BaseParticleBrush = new SolidBrush(Color.FromArgb(8, 5, 15, 30));
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
        // Custom detection strings supplied by the dashboard Custom Strings page.
        // Kept in one place so every in-memory scan table can be topped up with
        // them during a scan (same idea as "Custom String Found" on detect.ac).
        private readonly Dictionary<string, string> _remoteCustomStrings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
            siticoneVProgressBar1.Visible = false;   // replaced by OceanProgressBar
            BuildProgressBar();

            AttachControlMouseEvents(this);
            AttachControlMouseEvents(siticoneButton1);
            AttachControlMouseEvents(siticoneButton2);
            AttachControlMouseEvents(siticoneTextBox2);
            AttachControlMouseEvents(siticonePictureBox1);
            AttachControlMouseEvents(label2);

            motionEnabled = LoadMotionPreference();
            InitializeBackdropSquares();
            BuildMotionSwitch();
        }


        /// <summary>
        /// Swaps the designer's flat progress bar for the segmented one, in place:
        /// same bounds, so nothing about the layout moves and the scan code keeps
        /// driving it through the members it already used.
        /// </summary>
        private void BuildProgressBar()
        {
            try
            {
                oceanProgress = new Ocean_ac.OceanProgressBar();
                oceanProgress.Bounds = siticoneVProgressBar1.Bounds;
                oceanProgress.Visible = false;
                oceanProgress.ProgressColor = Color.FromArgb(160, 90, 240);
                oceanProgress.ProgressColor2 = Color.FromArgb(147, 51, 234);
                this.Controls.Add(oceanProgress);
                oceanProgress.BringToFront();
            }
            catch { }
        }

        private void BuildMotionSwitch()
        {
            try
            {
                motionSwitch = new Ocean_ac.OceanMotionSwitch();
                motionSwitch.Location = new Point(18, 14);
                motionSwitch.Checked = motionEnabled;
                motionSwitch.CheckedChanged += MotionSwitch_CheckedChanged;
                this.Controls.Add(motionSwitch);
                motionSwitch.BringToFront();
            }
            catch { }
        }

        private void MotionSwitch_CheckedChanged(object sender, EventArgs e)
        {
            motionEnabled = motionSwitch != null && motionSwitch.Checked;
            if (!motionEnabled)
            {
                // Drop everything that was mid-flight so the freeze is instant
                // and nothing keeps drifting once the switch is off.
                cursorTrails.Clear();
                clickRipples.Clear();
                logoTargetX = 0f;
                logoTargetY = 0f;
                logoTiltX = 0f;
                logoTiltY = 0f;
            }
            SaveMotionPreference(motionEnabled);
            Invalidate();
        }

        /// <summary>
        /// Motion preference lives next to the scanner config, so the app comes
        /// back up the way the user left it.
        /// </summary>
        private static string MotionPrefPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OceanUiPrefs.json"); }
        }

        private static bool LoadMotionPreference()
        {
            try
            {
                string path = MotionPrefPath;
                if (!System.IO.File.Exists(path)) return true;
                var json = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(path));
                var token = json["motion"];
                if (token == null) return true;
                return token.ToObject<bool>();
            }
            catch { return true; }
        }

        private static void SaveMotionPreference(bool enabled)
        {
            try
            {
                var json = new Newtonsoft.Json.Linq.JObject();
                json["motion"] = enabled;
                json["updated"] = DateTime.UtcNow.ToString("o");
                System.IO.File.WriteAllText(MotionPrefPath, json.ToString());
            }
            catch { }
        }

        /// <summary>White squares that drift across the black canvas.</summary>
        private void InitializeBackdropSquares()
        {
            backdropSquares.Clear();
            backdropSquareSpeed.Clear();
            for (int i = 0; i < 16; i++)
            {
                backdropSquares.Add(new PointF(random.Next(0, 760), random.Next(0, 460)));
                backdropSquareSpeed.Add((float)(0.12 + random.NextDouble() * 0.30));
            }
        }

        /// <summary>
        /// Advances the 3D logo pose. Only called while motion is on.
        ///
        /// The object does NOT spin on its own any more: at rest it sits square
        /// to the viewer and only turns while the pointer moves. Steady motion
        /// costs nothing between mouse events, and an object that never stops
        /// turning makes it impossible to actually look at the logo.
        /// </summary>
        private void AdvanceLogo()
        {
            // Pointer parallax: the chip turns toward the cursor.
            float nx = (currentMousePos.X - ClientSize.Width / 2f) / Math.Max(1f, ClientSize.Width / 2f);
            float ny = (currentMousePos.Y - ClientSize.Height / 2f) / Math.Max(1f, ClientSize.Height / 2f);
            if (nx < -1f) nx = -1f; else if (nx > 1f) nx = 1f;
            if (ny < -1f) ny = -1f; else if (ny > 1f) ny = 1f;

            logoTargetX = nx * 34f;                 // yaw, left/right
            logoTargetY = ny * 20f;                 // pitch, up/down
            logoTiltX += (logoTargetX - logoTiltX) * 0.07f;
            logoTiltY += (logoTargetY - logoTiltY) * 0.07f;
        }

        /// <summary>Drifts the white background squares. Only while motion is on.</summary>
        private void AdvanceBackdropSquares()
        {
            for (int i = 0; i < backdropSquares.Count; i++)
            {
                PointF p = backdropSquares[i];
                float speed = backdropSquareSpeed[i];
                p = new PointF(p.X + speed, p.Y + speed * 0.45f);
                if (p.X > ClientSize.Width + 6) p = new PointF(-6f, p.Y);
                if (p.Y > ClientSize.Height + 6) p = new PointF(p.X, -6f);
                backdropSquares[i] = p;
            }
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
                    if (motionEnabled)
                    {
                        clickRipples.Add(new ClickRipple { Center = clientPt, Radius = 3f, MaxRadius = 55f, Alpha = 1f });
                    }
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

        // Light-reactive particles that illuminate when mouse is near
        private void InitializeParticles()
        {
            int numParticles = 30;
            for (int i = 0; i < numParticles; i++)
            {
                double angle = random.NextDouble() * 2 * Math.PI;
                double speed = random.NextDouble() * 0.8 + 0.3;
                particles.Add(new Particle()
                {
                    Position = new PointF(random.Next(0, ClientSize.Width), random.Next(0, ClientSize.Height)),
                    Velocity = new PointF((float)(Math.Cos(angle) * speed), (float)(Math.Sin(angle) * speed)),
                    Radius = random.Next(1, 3),
                    BaseColor = Color.FromArgb(8, 5, 15, 30),
                    GlowColor = Color.FromArgb(120, 120, 60, 220),
                    Brush = BaseParticleBrush,
                    BaseBrush = BaseParticleBrush,
                    Highlighted = false
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

        // ================================================================
        //  MOTION
        //  All self-running animation (spin, drift, trails, ripples) hangs
        //  off `motionEnabled`, which the MOTION switch in the top-left
        //  corner turns ON *and* OFF. Pointer feedback (the cursor light and
        //  the hover glows) keeps working either way, because it answers the
        //  user instead of running on its own.
        // ================================================================
        private bool motionEnabled = true;
        private Ocean_ac.OceanMotionSwitch motionSwitch;

        // 3D logo pose. The renderer is stateless, so freezing motion is just
        // a matter of not advancing these values.
        /// <summary>The scan progress bar (see OceanProgressBar). The designer's own
        /// bar stays hidden; this one is placed over its bounds in the constructor.</summary>
        private Ocean_ac.OceanProgressBar oceanProgress;

        private float logoTiltX;
        private float logoTiltY;
        private float logoTargetX;
        private float logoTargetY;

        // White squares that sit on the black canvas.
        private readonly List<PointF> backdropSquares = new List<PointF>();
        private readonly List<float> backdropSquareSpeed = new List<float>();

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

                    // Pure black background - clean Apple minimal style
                    using (SolidBrush blackBrush = new SolidBrush(Color.FromArgb(255, 0, 0, 0)))
                    {
                        g.FillRectangle(blackBrush, r);
                    }

                    // Subtle dark purple-tinted vignette - very faint, only shows when light hits
                    using (LinearGradientBrush vignetteBrush = new LinearGradientBrush(r,
                        Color.FromArgb(30, 20, 10, 40),
                        Color.FromArgb(0, 0, 0, 0),
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillRectangle(vignetteBrush, r);
                    }

                    // Top/bottom thin accent lines - dark purple, barely visible until hover
                    using (Pen topLine = new Pen(Color.FromArgb(25, 100, 50, 200), 1f))
                    {
                        g.DrawLine(topLine, 0, 0, w, 0);
                    }
                    using (Pen bottomLine = new Pen(Color.FromArgb(25, 100, 50, 200), 1f))
                    {
                        g.DrawLine(bottomLine, 0, h - 1, w, h - 1);
                    }

                    // Subtle border outline - very dark purple, minimal
                    using (Pen borderPen = new Pen(Color.FromArgb(30, 80, 40, 180), 0.5f))
                    {
                        g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
                    }

                    // ---- WHITE DOTS ON THE BLACK ----
                    // A faint even grid of round white dots plus a
                    // deterministic scatter of brighter ones, so the black
                    // canvas reads as a surface instead of a void. The design
                    // is round everywhere (the site's section 16/17 in
                    // ocean-black.css does the same switch), so the marks are
                    // ellipses and not 3px squares. (The drifting live dots
                    // are painted per frame in OnPaint.)
                    const int cell = 26;
                    const int dot = 3;
                    using (SolidBrush gridDot = new SolidBrush(Color.FromArgb(13, 255, 255, 255)))
                    {
                        for (int gy = cell; gy < h - dot; gy += cell)
                        {
                            for (int gx = cell; gx < w - dot; gx += cell)
                            {
                                if (((gx / cell) * 7 + (gy / cell) * 13) % 11 == 0) continue;
                                g.FillEllipse(gridDot, gx, gy, dot, dot);
                            }
                        }
                    }
                    using (SolidBrush brightDot = new SolidBrush(Color.FromArgb(28, 255, 255, 255)))
                    {
                        for (int gy = cell; gy < h - dot; gy += cell)
                        {
                            for (int gx = cell; gx < w - dot; gx += cell)
                            {
                                if (((gx / cell) * 7 + (gy / cell) * 13) % 11 != 0) continue;
                                g.FillEllipse(brightDot, gx, gy, dot + 1, dot + 1);
                            }
                        }
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
                        using (SolidBrush fb = new SolidBrush(Color.FromArgb(255, 0, 0, 0)))
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

            // Light-reactive background particles - invisible until mouse nears
            foreach (var particle in particles)
            {
                if (particle.Highlighted)
                {
                    float pulse = (float)(Math.Sin(frame * 0.05 + particle.Position.X * 0.01) * 0.3 + 0.7);
                    int alpha = (int)(particle.Highlighted ? 120 * pulse : 0);
                    using (SolidBrush pb = new SolidBrush(Color.FromArgb(alpha, 130, 70, 230)))
                    {
                        float size = particle.Radius * 2 * pulse;
                        g.FillEllipse(pb, particle.Position.X - size / 2, particle.Position.Y - size / 2, size, size);
                    }
                }
            }

            // --- CURSOR ANIMATIONS ---
            // 1. Dynamic Cursor Spotlight - dark purple luminous glow (lights up area)
            if (smoothMousePos.X >= 0 && smoothMousePos.Y >= 0)
            {
                float glowRadius = 95f;
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(smoothMousePos.X - glowRadius, smoothMousePos.Y - glowRadius, glowRadius * 2f, glowRadius * 2f);
                    using (PathGradientBrush pgb = new PathGradientBrush(gp))
                    {
                        pgb.CenterColor = Color.FromArgb(40, 140, 80, 240);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(pgb, gp);
                    }
                }
                // Second outer larger halo for amplified light effect
                using (GraphicsPath gp2 = new GraphicsPath())
                {
                    gp2.AddEllipse(smoothMousePos.X - glowRadius * 1.5f, smoothMousePos.Y - glowRadius * 1.5f, glowRadius * 3f, glowRadius * 3f);
                    using (PathGradientBrush pgb2 = new PathGradientBrush(gp2))
                    {
                        pgb2.CenterColor = Color.FromArgb(15, 100, 50, 210);
                        pgb2.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(pgb2, gp2);
                    }
                }
            }

                    // 2. Cursor Trail - dark purple luminous trail with glow (lights up path)
            for (int i = 0; i < cursorTrails.Count; i++)
            {
                var ct = cursorTrails[i];
                float alphaNorm = Math.Max(0f, Math.Min(1f, ct.Alpha));
                int alpha = (int)(alphaNorm * 200);
                if (alpha > 0 && ct.Size > 0)
                {
                    // Outer glow halo for each trail point
                    using (SolidBrush trailGlow = new SolidBrush(Color.FromArgb((int)(alpha * 0.3), 130, 70, 230)))
                    {
                        float glowSize = ct.Size * 3;
                        g.FillEllipse(trailGlow, ct.Position.X - glowSize / 2, ct.Position.Y - glowSize / 2, glowSize, glowSize);
                    }
                    // Core bright dot
                    using (SolidBrush trailBrush = new SolidBrush(Color.FromArgb(alpha, 155, 85, 240)))
                    {
                        g.FillEllipse(trailBrush, ct.Position.X - ct.Size / 2f, ct.Position.Y - ct.Size / 2f, ct.Size, ct.Size);
                    }
                    if (i < cursorTrails.Count - 1)
                    {
                        var next = cursorTrails[i + 1];
                        // Connecting light beam
                        using (Pen trailPen = new Pen(Color.FromArgb((int)(alpha * 0.4), 140, 75, 235), Math.Max(1.5f, ct.Size / 2f)))
                        {
                            g.DrawLine(trailPen, ct.Position, next.Position);
                        }
                    }
                }
            }

            // 3. Click Ripples - expanding dark purple shockwaves with glow (lights up on click)
            foreach (var cr in clickRipples)
            {
                float alphaNorm = Math.Max(0f, Math.Min(1f, cr.Alpha));
                int alpha = (int)(alphaNorm * 220);
                if (alpha > 0)
                {
                    // Outer diffuse glow ring
                    using (Pen rippleGlow = new Pen(Color.FromArgb((int)(alpha * 0.3), 120, 60, 220), 4f))
                    {
                        g.DrawEllipse(rippleGlow, cr.Center.X - cr.Radius, cr.Center.Y - cr.Radius, cr.Radius * 2f, cr.Radius * 2f);
                    }
                    // Bright inner ring
                    using (Pen ripplePen = new Pen(Color.FromArgb(alpha, 150, 80, 238), 1.8f))
                    {
                        g.DrawEllipse(ripplePen, cr.Center.X - cr.Radius, cr.Center.Y - cr.Radius, cr.Radius * 2f, cr.Radius * 2f);
                    }
                    // Center flash point
                    using (SolidBrush flash = new SolidBrush(Color.FromArgb((int)(alpha * 0.6), 170, 100, 245)))
                    {
                        g.FillEllipse(flash, cr.Center.X - 3, cr.Center.Y - 3, 6, 6);
                    }
                }
            }

            // --- HOVER ANIMATIONS OVER CONTROLS ---
            // License text box hover/focus glowing outline - soft purple, no hard edges
            if (hoverTextBox > 0.01f)
            {
                int alpha = (int)(hoverTextBox * 140);
                var tb = siticoneTextBox2.Bounds;
                // Outer soft halo
                using (Pen glowPen = new Pen(Color.FromArgb(alpha / 3, 130, 70, 230), 4f))
                {
                    g.DrawRectangle(glowPen, tb.X - 5, tb.Y - 5, tb.Width + 10, tb.Height + 10);
                }
                // Inner subtle border - no colored edge, just a soft light
                using (Pen glowPen2 = new Pen(Color.FromArgb(alpha / 2, 160, 90, 245), 1f))
                {
                    g.DrawRectangle(glowPen2, tb.X - 2, tb.Y - 2, tb.Width + 4, tb.Height + 4);
                }
            }

            // Close button hover glow - dark purple (no red/pink)
            if (hoverClose > 0.01f)
            {
                int alpha = (int)(hoverClose * 160);
                var b = siticoneButton1.Bounds;
                // Outer glow ring (larger, soft)
                using (Pen closeGlowPen = new Pen(Color.FromArgb(alpha / 2, 130, 70, 230), 3f))
                {
                    g.DrawRectangle(closeGlowPen, b.X - 4, b.Y - 4, b.Width + 8, b.Height + 8);
                }
                // Inner bright glow
                using (Pen closeGlowPen2 = new Pen(Color.FromArgb(alpha, 160, 90, 245), 1.5f))
                {
                    g.DrawRectangle(closeGlowPen2, b.X - 2, b.Y - 2, b.Width + 4, b.Height + 4);
                }
            }

            // Minimize button hover glow - dark purple (no colored edges)
            if (hoverMin > 0.01f)
            {
                int alpha = (int)(hoverMin * 160);
                var b = siticoneButton2.Bounds;
                using (Pen minGlowPen = new Pen(Color.FromArgb(alpha / 2, 130, 70, 230), 3f))
                {
                    g.DrawRectangle(minGlowPen, b.X - 4, b.Y - 4, b.Width + 8, b.Height + 8);
                }
                using (Pen minGlowPen2 = new Pen(Color.FromArgb(alpha, 160, 90, 245), 1.5f))
                {
                    g.DrawRectangle(minGlowPen2, b.X - 2, b.Y - 2, b.Width + 4, b.Height + 4);
                }
            }

            // Traveling light on the very top edge - dark purple pulse
            float lightX = (frame * 1.2f) % Math.Max(150f, ClientSize.Width + 80f) - 40f;
            float lightAlpha = (float)(Math.Sin(frame * 0.08) * 0.3 + 0.5) * 100;
            using (Pen trail = new Pen(Color.FromArgb((int)lightAlpha, 120, 60, 210), 2.5f))
            {
                g.DrawLine(trail, lightX - 40f, 1.5f, lightX, 1.5f);
            }
            using (SolidBrush core = new SolidBrush(Color.FromArgb(180, 140, 80, 235)))
            {
                g.FillEllipse(core, lightX - 2, 0, 4, 4);
            }

            // Cyber corner brackets - Apple-style thin purple accents
            int inset = 12, len = 18;
            int a = scanInProgress ? 255 : 100;
            using (Pen corner = new Pen(Color.FromArgb(a, 130, 70, 230), 1.2f))
            {
                corner.StartCap = corner.EndCap = LineCap.Round;
                int w = ClientSize.Width, h = ClientSize.Height;
                // Top-left
                g.DrawLines(corner, new[] { new Point(inset, inset + 2), new Point(inset, inset), new Point(inset + 2, inset) });
                // Top-right
                g.DrawLines(corner, new[] { new Point(w - inset - 2, inset), new Point(w - inset, inset), new Point(w - inset, inset + 2) });
                // Bottom-left
                g.DrawLines(corner, new[] { new Point(inset, h - inset - 2), new Point(inset, h - inset), new Point(inset + 2, h - inset) });
                // Bottom-right
                g.DrawLines(corner, new[] { new Point(w - inset - 2, h - inset), new Point(w - inset, h - inset), new Point(w - inset, h - inset - 2) });
            }

            // Vertical sweep while scanning - dark purple luminous sweep (lights up screen)
            if (scanInProgress)
            {
                float sweep = (frame * 0.9f) % (ClientSize.Height + 200f) - 100f;
                // Outer soft halo
                using (LinearGradientBrush sbOuter = new LinearGradientBrush(
                    new Rectangle(0, (int)sweep - 60, ClientSize.Width, 120),
                    Color.FromArgb(0, 100, 50, 200),
                    Color.FromArgb(0, 100, 50, 200), LinearGradientMode.Vertical))
                {
                    ColorBlend blendOuter = new ColorBlend(3);
                    blendOuter.Colors = new[] { Color.FromArgb(0, 100, 50, 200), Color.FromArgb(40, 140, 80, 240), Color.FromArgb(0, 100, 50, 200) };
                    blendOuter.Positions = new[] { 0f, 0.5f, 1f };
                    sbOuter.InterpolationColors = blendOuter;
                    g.FillRectangle(sbOuter, 0, sweep - 60, ClientSize.Width, 120);
                }
                // Core bright sweep line
                using (LinearGradientBrush sbCore = new LinearGradientBrush(
                    new Rectangle(0, (int)sweep - 20, ClientSize.Width, 40),
                    Color.FromArgb(80, 150, 90, 245),
                    Color.FromArgb(80, 150, 90, 245), LinearGradientMode.Vertical))
                {
                    ColorBlend blendCore = new ColorBlend(3);
                    blendCore.Colors = new[] { Color.FromArgb(0, 150, 90, 245), Color.FromArgb(120, 170, 110, 250), Color.FromArgb(0, 150, 90, 245) };
                    blendCore.Positions = new[] { 0f, 0.5f, 1f };
                    sbCore.InterpolationColors = blendCore;
                    g.FillRectangle(sbCore, 0, sweep - 20, ClientSize.Width, 40);
                }
            }

            // --- WHITE SQUARES (drift only while motion is on) ---
            DrawBackdropDots(g);

            // --- 3D OCEAN LOGO (rendered from Ocean.ico) ---
            // Drawn last of the decorative layers so the brand object stays
            // visible above the scan sweep.
            DrawLogo3D(g);

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

        /// <summary>
        /// Lights the particles near the pointer.
        ///
        /// The glow steps through a small set of shared brushes instead of
        /// allocating a new SolidBrush per particle per frame: 30 particles at
        /// 60fps was ~1800 short-lived GDI objects a second, which is exactly the
        /// kind of churn that shows up as stutter and forces extra GCs.
        /// </summary>
        private void UpdateParticleHighlights()
        {
            foreach (var particle in particles)
            {
                float dx = smoothMousePos.X - particle.Position.X;
                float dy = smoothMousePos.Y - particle.Position.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                particle.Highlighted = dist < 120f;

                if (!particle.Highlighted)
                {
                    particle.Brush = particle.BaseBrush;
                    continue;
                }

                float intensity = Math.Max(0f, 1f - dist / 120f);
                int bucket = (int)(intensity * (GlowBrushes.Length - 1));
                if (bucket < 0) bucket = 0;
                if (bucket >= GlowBrushes.Length) bucket = GlowBrushes.Length - 1;
                particle.Brush = GlowBrushes[bucket];
            }
        }

        /// <summary>Shared glow steps (0 → strongest). Built once, reused every frame.</summary>
        private static readonly SolidBrush[] GlowBrushes = BuildGlowBrushes();

        private static SolidBrush[] BuildGlowBrushes()
        {
            SolidBrush[] brushes = new SolidBrush[9];
            for (int i = 0; i < brushes.Length; i++)
            {
                int a = (int)(i / (float)(brushes.Length - 1) * 180f);
                brushes[i] = new SolidBrush(Color.FromArgb(a, 130, 70, 230));
            }
            return brushes;
        }

        // ------------------------------------------------------------------
        // The two numbers shared with the website hero (ocean-black.css keeps
        // the same values as custom properties). They live here once, so the
        // app and the page cannot drift into two slightly different designs.
        // ------------------------------------------------------------------

        /// <summary>Hero grid step in px. Same token the page uses (`.ob-app-body`).</summary>
        private const int HeroGridStep = 22;

        /// <summary>Grid line alpha on black — the page draws 4% white.</summary>
        private const int HeroGridAlpha = 11;

        /// <summary>
        /// How much of the shorter side of the hero area the logo takes. The
        /// website sizes `.ob-logo3d` to exactly this share of its window body,
        /// which is what makes the two renderings the same design at any size.
        /// </summary>
        private const float HeroLogoShare = 0.85f;

        /// <summary>
        /// The free rectangle between the window buttons and the scan controls.
        ///
        /// Every edge is read from the real controls, so the object is always
        /// sized to the space that actually exists: nothing overlaps the scan
        /// bar or the license field, and nothing is placed by a guessed constant
        /// that only happens to be right at one window size.
        /// </summary>
        private Rectangle HeroArea()
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            // header: below the min/max + close buttons
            int top = 0;
            Control[] header = new Control[] { siticoneButton1, siticoneButton2 };
            foreach (Control c in header)
            {
                if (c != null && c.Visible && c.Bottom > top) top = c.Bottom;
            }
            top += 10;

            // footer: everything the scanner itself draws stays clear
            int bottom = h;
            Control[] footer = new Control[] { siticoneVProgressBar1, siticoneTextBox2, label2 };
            foreach (Control c in footer)
            {
                if (c == null || !c.Visible) continue;
                if (c.Top > top && c.Top < bottom) bottom = c.Top;
            }
            bottom -= 10;

            if (bottom <= top) bottom = h;

            int pad = 20;
            return new Rectangle(pad, top, Math.Max(0, w - pad * 2), Math.Max(0, bottom - top));
        }

        /// <summary>
        /// The hero body: a faint 22px grid of white squares on black — the same
        /// step the page uses. The filled checkered sheet that was here before is
        /// gone: the squared grid alone is enough, and the logo sits on plain
        /// black instead of competing with a pattern.
        ///
        /// The grid is a 22px tile baked once into a TextureBrush, so the whole
        /// background costs one FillRectangle per frame. The app used to draw a
        /// whole second window here (its own frame, its own title bar) inside the
        /// real window, which read as two UIs stacked; the app's own window is
        /// the window.
        /// </summary>
        private void DrawHeroGrid(Graphics g, Rectangle area)
        {
            try
            {
                TextureBrush brush = GridBrush();
                if (brush == null) return;

                GraphicsState st = g.Save();
                g.TranslateTransform(area.Left, area.Top);
                g.FillRectangle(brush, 0, 0, area.Width, area.Height);
                g.Restore(st);
            }
            catch { }
        }

        private TextureBrush _grid;

        /// <summary>One 22px grid cell (a 1px hairline top and left), reused every frame.</summary>
        private TextureBrush GridBrush()
        {
            if (_grid != null) return _grid;
            try
            {
                int cell = HeroGridStep;
                Bitmap tile = new Bitmap(cell, cell, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (Graphics tg = Graphics.FromImage(tile))
                using (Pen line = new Pen(Color.FromArgb(HeroGridAlpha, 255, 255, 255), 1f))
                {
                    tg.Clear(Color.Transparent);
                    tg.DrawLine(line, 0, 0, cell, 0);
                    tg.DrawLine(line, 0, 0, 0, cell);
                }
                _grid = new TextureBrush(tile, WrapMode.Tile);
            }
            catch { }
            return _grid;
        }

        /// <summary>
        /// The brand object: the 3D Ocean logo (see OceanLogo3D), centred in the
        /// measured hero area and sized to it. The pose lives on the form, which
        /// is what lets the MOTION switch stop advancing it instead of hiding it.
        /// </summary>
        private void DrawLogo3D(Graphics g)
        {
            try
            {
                Rectangle area = HeroArea();
                if (area.Width < 60 || area.Height < 40) return;

                DrawHeroGrid(g, area);

                int side = (int)Math.Round(Math.Min(area.Width, area.Height) * HeroLogoShare);
                if (side < 40) side = Math.Min(40, Math.Min(area.Width, area.Height));
                if (side > 320) side = 320;      // keep it from swallowing the window

                Rectangle bounds = new Rectangle(
                    area.Left + (area.Width - side) / 2,
                    area.Top + (area.Height - side) / 2,
                    side, side);

                // The pose is exactly the pointer's: no base spin, so at rest the
                // chip faces the viewer square on.
                Ocean_ac.OceanLogo3D.Render(g, bounds, logoTiltX, logoTiltY, 1f);
            }
            catch { }
        }

        /// <summary>
        /// White dots on the black canvas. The static grid lives in the
        /// cached background; these are the live ones, and they only drift
        /// while motion is on. Round, like every other mark in the design.
        /// </summary>
        private void DrawBackdropDots(Graphics g)
        {
            try
            {
                for (int i = 0; i < backdropSquares.Count; i++)
                {
                    PointF p = backdropSquares[i];
                    float size = 3f + (i % 4);
                    float alpha = 14f + (i % 5) * 6f;

                    // Brighten near the pointer, matching the rest of the UI.
                    if (smoothMousePos.X >= 0 && smoothMousePos.Y >= 0)
                    {
                        float dx = p.X - smoothMousePos.X;
                        float dy = p.Y - smoothMousePos.Y;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d < 150f) alpha += (150f - d) * 0.5f;
                    }
                    if (alpha > 95f) alpha = 95f;

                    using (SolidBrush sb = new SolidBrush(Color.FromArgb((int)alpha, 255, 255, 255)))
                    {
                        g.FillEllipse(sb, p.X, p.Y, size, size);
                    }
                }
            }
            catch { }
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
            // Ensure custom strings config file exists
            EnsureCustomStringsConfig();

            this.BackColor = System.Drawing.Color.FromArgb(0, 0, 0);
            this.FormBorderStyle = FormBorderStyle.None;
            generatedPin = "";
            siticonePictureBox1.Visible = false; // Remove big logo picture

            // Ask the user to paste their license key and press Enter to verify + scan.
            Invoke((MethodInvoker)(() =>
            {
                label2.Text = "enter license key";
                label2.ForeColor = System.Drawing.Color.FromArgb(100, 100, 110);
                label2.TextAlign = ContentAlignment.TopCenter;
                label2.AutoSize = true;
                label2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                label2.Left = (this.ClientSize.Width - label2.Width) / 2;
                label2.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
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

                // === CUSTOM STRINGS FROM CONFIG (interactive with user settings) ===
                // Load user-defined custom detection strings from OceanScanConfig.json
                LoadCustomStringsFromConfig(customNamesExplorer);

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
            // Dashboard-configured custom strings are merged into every scan table
            // so one keyword added on /dashboard/strings is searched on disk, in
            // process memory, in the DNS cache and in the service logs.
            LoadCustomStringsFromConfig(customNamesExplorer);
            LoadCustomStringsFromConfig(customNamesLsass);
            LoadCustomStringsFromConfig(customNamesDnsCache);
            LoadCustomStringsFromConfig(customNamesSysmain);
            LoadCustomStringsFromConfig(customNamesDps);
            LoadCustomStringsFromConfig(customNamesPcaSvc);
            LoadCustomStringsFromConfig(customNamesHistory);
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

        // ------------------------------------------------------------------
        // Live scanner feed. The website polls /api/scanner/live and renders
        // the exact stage, percentage, and findings THIS machine is producing,
        // so the dashboard stops simulating a scan. Throttled (max ~1/s plus
        // every new finding) and always run off the UI thread.
        // ------------------------------------------------------------------
        // Only one live post may be in flight: the progress animation ticks
        // every 16 ms, and queueing a task per tick floods the thread pool
        // (the UI then stutters even though each post is tiny).
        private int _livePostGate;

        private void PostScannerLive(int pct, string state)
        {
            if (string.IsNullOrEmpty(usedPin) && string.IsNullOrEmpty(usedKeyId)) return;
            bool finished = state == "complete" || state == "error";
            List<string[]> lines = SecurityTools.OceanScan.LiveSnapshot();
            int newLines = lines.Count > _liveSentLines ? lines.Count - _liveSentLines : 0;
            if (!finished)
            {
                if (newLines == 0 && pct == _liveLastPct) return;
                if ((DateTime.UtcNow - _liveLastPost).TotalMilliseconds < 900) return;
            }
            _liveLastPost = DateTime.UtcNow;
            _liveLastPct = pct;

            List<object> payloadLines = new List<object>();
            for (int i = _liveSentLines; i < lines.Count; i++)
            {
                if (lines[i] == null || lines[i].Length < 2) continue;
                payloadLines.Add(new { kind = lines[i][0], text = lines[i][1] });
            }
            _liveSentLines = lines.Count;

            string code = usedPin;
            string keyId = usedKeyId;
            string stage = SecurityTools.OceanScan.CurrentStage;
            try
            {
                using (TimedWebClient client = new TimedWebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    client.UploadString(ApiBase + "/api/scanner/live", "POST",
                        Newtonsoft.Json.JsonConvert.SerializeObject(new
                        {
                            code = code,
                            keyId = keyId,
                            pcName = Environment.MachineName,
                            player = Environment.UserName,
                            stage = stage,
                            progress = pct,
                            state = state,
                            lines = payloadLines
                        }));
                }
            }
            catch { }
        }

        private void PostScanResultToApi(string status)
        {
            try
            {
                using (TimedWebClient client = new TimedWebClient())
                {
                    client.TimeoutMs = 8000;
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
            
            // === Detection categories based on real FiveM PC checkers (Ocean, Napse, Detect, Echo) ===
            
            // 1. Known cheat DLLs and executables (direct file detection)
            string[] cheatDlls = {
                "cherax.dll", "ozark.dll", "redengine.dll", "eulen.dll", "phantomx.dll", "phantom-x.dll",
                "asphyx.dll", "kiddion.dll", "ftools.dll", "freemenu.dll", "nightfall.dll", "oblivion.dll",
                "noctis.dll", "nixus.dll", "luna.dll", "vanguard.dll", "spectre.dll", "hades.dll", "dware.dll",
                "skript.dll", "menyoo.asi", "phoenix.dll", "luaware.dll", "critical_x64.dll", "monkeyware.dll",
                "ozi.dll", "raff.dll", "bys.dll", "extreme.dll", "vega.dll", "zap.dll",
                "winapi99.dll", "projectloader.exe", "p5m_free.ini", "loader.vmp.exe",
                "basic.asi", "nvidia.dll", "oziwarepublic.dll", "weedcord.dll", "raffattmenuv2.dll",
                "free_fivem_cheat.dll", "praxloader.exe", "deimos.dll", "klay.dll",
                "jasza.dll", "elysian.dll", "nemesis.dll", "lucis.dll", "forix.dll",
                "glorify.dll", "kestrel.dll", "mischief.dll", "opium.dll", "sober.dll",
                "celesta.dll", "tape.dll", "flare.dll", "sourfish.dll", "owinock.dll",
                "2take1.dll", "sixcall.dll", "k-extra.dll", "quantum.dll", "hound.dll",
                "salamand3r.dll", "ech0.dll", "delusion.dll", "serena.dll", "stand.asi",
                "hazari.dll", "sheetghost.dll", "bonkcheat.dll", "phunkymenu.dll", "ascensionmenu.dll",
                "interstellarmenu.dll", "pulsefi.dll", "mechanizm.dll", "outcastmenu.dll", "obscuramenu.dll",
                "uprox.dll", "cerberusloader.dll", "valsknol.dll", "sigrunc.dll", "qwksiln.dll",
                "makers.dll", "popcorn.dll", "draw.dll", "matheu.dll", "lznz.dll",
                "grabbo.dll", "kresnik.dll", "kinx.dll", "prvt.dll", "slyn.dll",
                "0xalumnus.dll", "cobra_injector.dll", "misery.dll", "disintegrate.dll",
                "nadrix.dll", "xtream.dll", "grilyx.dll", "damageboy.dll", "aloison.dll",
                "kqmg.dll", "uuplus.dll", "delulu.dll", "unicorn.dll", "marseille.dll"
            };

            // 2. Known cheat config files, scripts, and data files
            string[] cheatConfigs = {
                "tapatio.lua", "tapatioV24.lua", "tikimenu.lua", "tiki_menu.lua",
                "vitormanu.lua", "vietnamenuv2.lua", "zephyrmenu.lua",
                "renatoGarciaMenu.lua", "renovamenuattbeta_1.lua", "renovamenucripatt.lua",
                "pozeRP.lua", "renovaMenu.lua", "parazetamol-crack",
                "settings.cock", "loader.data", "p5m_free.ini",
                "abc.abc", "debug_logs", "senha_monster.rar", "password_is_eulen.rar",
                "public.zip", "x64a.rpf", "imgui.ini",
                "cfg.latest", "favorites.cfg", "ZltWMtLL5xBgZ2M",
                "9m0Ixhet", "AppCrash_notepad.exe", "ReSetup.exe",
                "dControl.exe", "Defender Control", "imdisk0"
            };

            // 3. Modified game files (RPF archives - FiveM game files)
            string[] modifiedRpf = {
                "ai.rar", "ai.zip", "CR-fastRun.rpf",
                "damageboost-1.5X-DDW.rpf", "damageboost-10.0X-DDW.rpf",
                "DopeAmbulance.rpf", "DopeBmx_.rpf", "DopeTaxi.rpf",
                "fastladder-DDW.rpf", "FAST_STRAFE_BY_OSTEN_1.rpf", "FAST_STRAFE_BY_OSTEN_1.rar",
                "handling.rpf", "HardAmmo-DDW.rpf", "infinitestaminafastreload.rpf",
                "IR.rpf", "maxrange.rar", "municao_infinita.rpf",
                "norecoil-DDW.rpf", "quickenter.rpf", "quickEnter-DDW.rpf",
                "rage.rpf", "Remove_Roll.rpf", "stamina-DDW.rpf",
                "varartirobybruxo.rpf", "Varartirobyfrz.rpf",
                "Versao_Nova_Citizen_1_tiro_by_frz.rpf", "WeaponVehicles.rpf",
                "youngtheuz_skills.rpf", "ZC-bulletPenetration.rpf",
                "ZC-damageBoost_1.5X.rpf", "ZC-damageBoost_10X.rpf",
                "ZC-increasedRange.rpf", "ZC-infiniteAmmo.rpf",
                "ZC-stamina.rpf", "ZC-softAim.rpf", "ZC-softAim.rar",
                "ZC-weaponModifier.rpf", "pedaccuracy.meta", "loadouts.meta",
                "add_weapon_pistol.50.rpf", "1_tiro_by_cz.rpf",
                "CR-fastRun.rpf", "handlingModifier.rpf", "BP.rpf",
                "NO_RECOIL_byitalo22.rpf", "FAST_STRAFE_BY_OSTEN_1.rpf"
            };

            // 4. Bypass/cleaner batch files (anti-forensic tools)
            string[] bypassBats = {
                "sensy.bat", "NEM_DEUS_PEGA.bat", "bek.bat",
                "Bypass_Ghost_Cleaner.bat", "senhas.bat",
                "a8a953c01e2d3139.bat", "Win.zip.bat", "dollynscott.bat",
                "ghost.bat_1.bat", "SpacialoBACKUP.bat", "SconzaFps.bat",
                "D3D10.dll"
            };

            // 5. Suspicious processes running (detect live cheat processes)
            string[] suspiciousProcesses = {
                "cherax", "redengine", "eulen", "skript", "phoenix",
                "luaware", "menyoo", "projectloader", "kdmapper",
                "manualmap", "extreme", "sxhook", "vega", "zap",
                "critical_x64", "dlc", "monkeyware", "ozi", "raff",
                "praxloader", "deimosloader", "klayloader", "jasza_loader",
                "p5m_free", "keyser", "phaze", "vortexmenu", "lumia",
                "macho", "kola", "nightfall", "nixus", "hades",
                "delusion", "serena", "stand", "hazari", "sheetghost",
                "bonkcheat", "phunkymenu", "ascensionmenu", "interstellarmenu",
                "pulsefi", "mechanizm", "outcastmenu", "obscuramenu",
                "uprox", "cerberusloader", "valsknol", "sigrunc",
                "qwksiln", "makers", "popcorn", "draw", "matheu",
                "lznz", "grabbo", "kresnik", "kinx", "prvt",
                "slyn", "0xalumnus", "cobra_injector", "misery",
                "disintegrate", "nadrix", "xtream", "grilyx",
                "damageboy", "aloison", "kqmg", "uuplus",
                "delulu", "unicorn", "marseille", "hamburger", "pearl",
                "spoofer", "hwid", "cidspoof", "ids_spoofer",
                "aio_spoofer", "wii_spoofer", "spoofy",
                "serial_spoof", "sndvol_spoof", "wgu-gift-bypass"
            };

            // 6. Known cheat URLs/domains embedded in cheat files (string scanning)
            string[] cheatUrls = {
                "skript.gg", "projectcheats.com", "keyauth.win", "api.keyauth.cc",
                "keyauth_api", "pedrin.cc", "pedrin.ovh", "gosth.gg",
                "monesy.dev", "idandev.xyz", "redengine.eu", "stoppedbypass",
                "redengine", "eulen", "cherax", "ozark", "asphyx",
                "kiddion", "phantomx", "spectre", "ftools", "freemenu",
                "nightfall", "oblivion", "noctis", "nixus", "hades",
                "dware", "zmenu", "luxor", "lynx", "dopamine",
                "tapatio", "viperx", "zenith", "masonjack", "akachu",
                "brutan", "wilix", "wilixmenu", "keyser", "phaze",
                "vortexmenu", "lumia", "macho", "kola.gg",
                "susano.re", "marseille", "zpo", "2take1", "sixcall",
                "k-extra", "quantum", "hound", "salamand3r", "ech0",
                "delusion", "serena", "stand.gg", "hazari", "hazari.qy",
                "praxmenu", "prax.menu", "praxloader", "prax-http",
                "deimosmenu", "deimos.io", "deimos.gg", "deimosloader",
                "klaymenu", "klay.menu", "jasza_menu", "jasza.menu",
                "elysianmenu", "elysian.cf", "nemesismenu", "nemesis.io",
                "lucismenu", "lucis.menu", "forixmenu", "forix.menu",
                "glorifymenu", "glorify.fun", "kestrelmenu", "kestrel.menu",
                "mischiefmenu", "opiummenu", "opium.fun", "sobermenu",
                "celestamenu", "stellarmenu", "astromenu", "rkmenu",
                "statementmenu", "tapermenu", "sourfish", "flaremenu",
                "flare.menu", "owinock", "uuplus", "kqmg", "aloison",
                "damageboy", "grilyx", "xtream", "nadrix",
                "cobra_injector", "misery", "disintegrate",
                "0xalumnus", "unrealauth", "sheetghost", "bonkcheat",
                "phunkymenu", "ascensionmenu", "interstellarmenu",
                "pulsefi", "mechanizm", "outcastmenu", "obscuramenu",
                "napse.ac", "anticheat.ac", "detect.ac", "echo.ac"
            };

            var dirs = new List<string>();
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FiveM Application Data")); } catch { }
            try { dirs.Add(Path.GetTempPath()); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")); } catch { }
            try { dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FiveM")); } catch { }

            // === SCAN 1: File enumeration on disk ===
            foreach (string d in dirs)
            {
                if (string.IsNullOrEmpty(d) || !Directory.Exists(d)) continue;
                bool fiveMRoot = kb(d).Contains("fivem");

                var files = new List<string>();
                try { files.AddRange(Directory.EnumerateFiles(d, "*", SearchOption.TopDirectoryOnly)); } catch { }

                // Check for known cheat DLLs/exe files
                foreach (string kw in cheatDlls)
                {
                    foreach (string f in files)
                    {
                        if (kb(Path.GetFileName(f)).Contains(kw) && hits.Count < 15)
                            hits.Add("[DLL/EXE] " + Path.GetFileName(f) + "  (" + d + ")");
                    }
                }

                // Check for known config/script files
                foreach (string kw in cheatConfigs)
                {
                    foreach (string f in files)
                    {
                        if (kb(Path.GetFileName(f)).Contains(kw) && hits.Count < 12)
                            hits.Add("[CONFIG] " + Path.GetFileName(f) + "  (" + d + ")");
                    }
                }

                // Check for modified RPF files (FiveM game files)
                foreach (string kw in modifiedRpf)
                {
                    foreach (string f in files)
                    {
                        if (kb(Path.GetFileName(f)).Contains(kw) && hits.Count < 12)
                            hits.Add("[RPF] " + Path.GetFileName(f) + "  (" + d + ")");
                    }
                }

                // Check for bypass/cleaner batch files
                foreach (string kw in bypassBats)
                {
                    foreach (string f in files)
                    {
                        if (kb(Path.GetFileName(f)).Contains(kw) && hits.Count < 8)
                            hits.Add("[BYPASS] " + Path.GetFileName(f) + "  (" + d + ")");
                    }
                }

                // Check FiveM root folders for cheat-named directories
                if (fiveMRoot)
                {
                    try
                    {
                        foreach (string sub in Directory.EnumerateDirectories(d, "*", SearchOption.TopDirectoryOnly))
                        {
                            string sn = kb(Path.GetFileName(sub));
                            foreach (string kw in cheatDlls.Concat(cheatConfigs))
                            {
                                if (sn.Contains(kw) && hits.Count < 10)
                                    hits.Add("[FOLDER] " + Path.GetFileName(sub) + "\\  (in FiveM data)");
                            }
                        }
                    }
                    catch { }
                }
            }

            // === SCAN 2: Check running processes for known cheat names ===
            try
            {
                foreach (System.Diagnostics.Process proc in System.Diagnostics.Process.GetProcesses())
                {
                    string procName = kb(proc.ProcessName);
                    try
                    {
                        foreach (string kw in suspiciousProcesses)
                        {
                            if (procName.Contains(kw) && hits.Count < 20)
                            {
                                try { hits.Add("[PROCESS] " + proc.ProcessName + " (PID " + proc.Id + ") - running cheat process"); }
                                catch { }
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        try { proc.Dispose(); } catch { }
                    }
                }
            }
            catch { }

            // === SCAN 3: Check for cheat URLs/domains in recent files (string scan) ===
            // This mimics how real PC checkers scan file contents for cheat signatures
            try
            {
                var recentDocs = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                if (Directory.Exists(recentDocs))
                {
                    foreach (string file in Directory.EnumerateFiles(recentDocs, "*.lnk", SearchOption.TopDirectoryOnly).Take(50))
                    {
                        string fileName = kb(Path.GetFileName(file));
                        foreach (string kw in cheatUrls)
                        {
                            if (fileName.Contains(kw) && hits.Count < 10)
                                hits.Add("[RECENT] " + Path.GetFileName(file) + " - references cheat: " + kw);
                        }
                    }
                }
            }
            catch { }

            // === SCAN 4: Check for USN journal/Windows forensic artifacts (light version) ===
            // Real PC checkers look at USN journal entries to find deleted cheat files
            // We check for traces in prefetch and recent items
            try
            {
                string prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                if (Directory.Exists(prefetchPath))
                {
                    foreach (string pf in Directory.EnumerateFiles(prefetchPath, "*.pf", SearchOption.TopDirectoryOnly).Take(30))
                    {
                        string pfName = kb(Path.GetFileName(pf));
                        foreach (string kw in cheatDlls.Concat(suspiciousProcesses).Take(30))
                        {
                            if (pfName.Contains(kw) && hits.Count < 8)
                                hits.Add("[PREFETCH] " + Path.GetFileName(pf) + " - cheat was executed on this system");
                        }
                    }
                }
            }
            catch { }

            // === SCAN 5: Check registry for cheat references (light version) ===
            // Real PC checkers scan registry for installed cheat software markers
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Software"))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            string skn = kb(subKeyName);
                            foreach (string kw in cheatDlls.Concat(new[] { "cheat", "hack", "spoofer", "bypass", "fivem cheat" }))
                            {
                                if (skn.Contains(kw) && hits.Count < 8)
                                {
                                    try { hits.Add("[REGISTRY] HKCU\\Software\\" + subKeyName + " - possible cheat software installed"); } catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // === SCAN 6: Check for anti-forensic/cleaner tools (detects attempts to hide cheats) ===
            string[] antiForensicTools = {
                "CCleaner", "cleaner", "privacy", "trackoff", "historyclean",
                "filewiper", "evidenceeliminator", "shredder", "securedelete"
            };
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (Directory.Exists(appData))
                {
                    foreach (string dir in Directory.EnumerateDirectories(appData, "*", SearchOption.TopDirectoryOnly).Take(100))
                    {
                        string dn = kb(Path.GetFileName(dir));
                        foreach (string kw in antiForensicTools)
                        {
                            if (dn.Contains(kw) && hits.Count < 5)
                                hits.Add("[ANTIFORENSIC] " + Path.GetFileName(dir) + " - anti-forensic/cleaner tool detected");
                        }
                    }
                }
            }
            catch { }

            // === SCAN 7: Check for FiveM cheat injection via DLL in game process ===
            try
            {
                foreach (System.Diagnostics.Process proc in System.Diagnostics.Process.GetProcessesByName("FiveM"))
                {
                    try
                    {
                        hits.Add("[INJECTION CHECK] FiveM process running (PID " + proc.Id + ") - checking for injected modules...");
                        // In a full implementation, we would enumerate modules here
                        // For now, we flag that FiveM is running and needs inspection
                    }
                    catch { }
                    finally
                    {
                        try { proc.Dispose(); } catch { }
                    }
                }
            }
            catch { }

            return hits.Count == 0 ? "" : string.Join("\n", hits);
        }

        /// <summary>
        /// Loads custom detection strings from OceanScanConfig.json in the app directory.
        /// Users can edit this file to add their own detection patterns.
        /// Format: {"filename_or_string": "description"}
        /// </summary>
        private void LoadCustomStringsFromConfig(Dictionary<string, string> targetDict)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OceanScanConfig.json");
                if (System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    var customStrings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (customStrings != null)
                    {
                        foreach (var kvp in customStrings)
                        {
                            if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
                            {
                                targetDict[kvp.Key] = kvp.Value;
                                Console.WriteLine($"Loaded custom string: Key '{kvp.Key}', Value '{kvp.Value}'");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load custom strings config: {ex.Message}");
            }

            // Layer the dashboard strings on top of the local file so the website
            // editor is authoritative for the next scan.
            MergeRemoteCustomStrings(targetDict);
        }

        /// <summary>
        /// Copies every custom string that came from the dashboard config into a
        /// scan table. Runs for the disk, memory, dns-cache and service tables,
        /// which is what makes one keyword search the whole machine.
        /// </summary>
        private void MergeRemoteCustomStrings(Dictionary<string, string> targetDict)
        {
            if (targetDict == null || _remoteCustomStrings.Count == 0) return;
            foreach (var kvp in _remoteCustomStrings)
            {
                if (string.IsNullOrEmpty(kvp.Key)) continue;
                if (!targetDict.ContainsKey(kvp.Key))
                {
                    targetDict[kvp.Key] = string.IsNullOrEmpty(kvp.Value) ? ("Custom string: " + kvp.Key) : kvp.Value;
                }
            }
        }

        /// <summary>
        /// Reads the customStrings block from a dashboard config object.
        /// Accepts ["term", ...], [{term,desc,category}, ...] and {"term":"note"}.
        /// Also picks up the module switches (moduleXxx: true/false) so the
        /// Configs page UI drives which artifact collectors run.
        /// </summary>
        private void LoadRemoteCustomStrings(Newtonsoft.Json.Linq.JObject cfg)
        {
            if (cfg == null) return;
            try
            {
                var token = cfg["customStrings"] ?? cfg["strings"];
                if (token != null)
                {
                    if (token.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                    {
                        foreach (var item in (Newtonsoft.Json.Linq.JArray)token)
                        {
                            if (item == null) continue;
                            string term = null, desc = null;
                            if (item.Type == Newtonsoft.Json.Linq.JTokenType.String)
                            {
                                term = (string)item;
                            }
                            else
                            {
                                term = (string)(item["term"] ?? item["name"]);
                                desc = (string)(item["desc"] ?? item["description"]);
                            }
                            AddRemoteCustomString(term, desc);
                        }
                    }
                    else if (token.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                    {
                        foreach (var prop in ((Newtonsoft.Json.Linq.JObject)token).Properties())
                        {
                            string desc = null;
                            if (prop.Value != null && prop.Value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                                desc = (string)(prop.Value["desc"] ?? prop.Value["description"] ?? prop.Value["category"]);
                            else if (prop.Value != null)
                                desc = prop.Value.ToString();
                            AddRemoteCustomString(prop.Name, desc);
                        }
                    }
                }

                // Detection module switches: modulePrefetch, moduleAmcache, ...
                foreach (var prop in cfg.Properties())
                {
                    string name = prop.Name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (name.StartsWith("module", StringComparison.OrdinalIgnoreCase) && prop.Value != null)
                    {
                        bool on;
                        if (bool.TryParse(prop.Value.ToString(), out on))
                            SecurityTools.OceanScan.DetectionModules[name] = on;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read remote custom strings: {ex.Message}");
            }
        }

        private void AddRemoteCustomString(string term, string desc)
        {
            if (string.IsNullOrWhiteSpace(term)) return;
            term = term.Trim();
            if (term.Length < 3 || term.Length > 120) return;
            _remoteCustomStrings[term] = string.IsNullOrWhiteSpace(desc) ? ("Custom string: " + term) : desc.Trim();
        }

        /// <summary>
        /// Generates a default config file with example custom strings if none exists.
        /// </summary>
        private void EnsureCustomStringsConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OceanScanConfig.json");
                if (!System.IO.File.Exists(configPath))
                {
                    var defaultConfig = new Dictionary<string, string>
                    {
                        { "my_custom_cheat.dll", "My Custom Cheat Detection" },
                        { "my_cheats_folder", "My Cheats Folder Detection" },
                        { "my_cheat_config.ini", "My Cheat Config File" },

                        // Starter pack - mirrors the seed set on /dashboard/strings.
                        { "nvevade", "External NVIDIA overlay evader" },
                        { "streamproof", "Streamproof / capture-hiding modification" },
                        { "prefetch_cleaner", "Prefetch cleaner" },
                        { "prefetchcleaner", "Prefetch cleaner" },
                        { "bam_cleaner", "BAM / DAM trace cleaner" },
                        { "usn_cleaner", "USN Journal cleaner" },
                        { "eventlog_clear", "Event log cleaning tool" },
                        { "shellbag_cleaner", "Shellbag / MRU cleaner" },
                        { "trace_cleaner", "Anti-forensic trace cleaner" },
                        { "injector.exe", "Generic manual-mapper / injector" },
                        { "mapper.exe", "Kernel or user-mode driver mapper" },
                        { "kdmapper", "kdmapper driver mapper" },
                        { "fivem_cheat.dll", "FiveM cheat module" },
                        { "aimbot", "Aimbot module" },
                        { "triggerbot", "Triggerbot module" },
                        { "silent_aim", "Silent aim module" },
                        { "esp.dll", "ESP / wallhack module" },
                        { "magic_bullet", "Magic bullet module" },
                        { "godmode", "Godmode module" },
                        { "pcileech", "PCILeech DMA firmware" },
                        { "leechcore", "LeechCore DMA library" },
                        { "memprocfs", "MemProcFS memory reader" },
                        { "dma_card", "DMA card utility" }
                    };
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented);
                    System.IO.File.WriteAllText(configPath, json);
                    Console.WriteLine("Created default OceanScanConfig.json with example custom strings.");
                }
            }
            catch { }
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
                Ocean_ac.OceanSound.Play(Ocean_ac.OceanSound.Cue.Start);

                oceanProgress.Invoke((MethodInvoker)(() =>
                {
                    oceanProgress.Minimum = 0;
                    oceanProgress.Maximum = 100;
                    oceanProgress.Value = 0;
                    oceanProgress.Text = "0%";
                    oceanProgress.ProgressColor = Color.FromArgb(160, 90, 240);
                    oceanProgress.ProgressColor2 = Color.FromArgb(147, 51, 234);
                    oceanProgress.Visible = true;
                }));

                // Let timer1 drive a smooth 1%, 2%, ... progress while scanning.
                scanInProgress = true;
                progressTicks = 0;
                _liveSentLines = 0;
                _liveLastPct = -1;
                _liveLastPost = DateTime.MinValue;
                try { Task.Run(() => PostScannerLive(0, "scanning")); } catch { }

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
                        // Report upload, off the UI thread: a slow server must not
                        // stall the window on the last step of the scan.
                        await Task.Run(() => PostScanResultToApi(scanFailed ? "error" : "completed"));
                    }
                    catch { }
                    // Close the live feed on the website side too.
                    try
                    {
                        int endPct = scanFailed ? 0 : 100;
                        string endState = scanFailed ? "error" : "complete";
                        await Task.Run(() => PostScannerLive(endPct, endState));
                    }
                    catch { }
                    oceanProgress.Invoke((MethodInvoker)(() =>
                    {
                        if (!scanFailed)
                        {
                            oceanProgress.Value = 100;
                            oceanProgress.Text = "100%";
                            oceanProgress.ProgressColor = Color.FromArgb(52, 211, 153);
                            oceanProgress.ProgressColor2 = Color.FromArgb(16, 185, 129);
                            oceanProgress.Visible = true;
                        }
                        else
                        {
                            oceanProgress.Visible = false;
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

        /// <summary>
        /// A WebClient that gives up instead of hanging.
        ///
        /// The plain WebClient waits up to ~100 seconds on a TCP connect, and the
        /// dashboard calls used to run it straight on the UI thread: if the server
        /// was not answering, the whole window stopped repainting and the scan
        /// looked frozen. Everything network-facing now goes through this class on
        /// a worker thread, so the worst case is one short timeout.
        /// </summary>
        private sealed class TimedWebClient : WebClient
        {
            public int TimeoutMs = 4000;

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request != null) request.Timeout = TimeoutMs;
                return request;
            }
        }

        private async Task<string> ValidarKeyNaApiAsync(string licenseKey)
        {
            try
            {
                {
                    // GET validate then POST use (uses are tracked by maxUses on the dashboard).
                    string validateUrl = ApiBase + "/api/keys/validate/" + Uri.EscapeDataString(licenseKey);
                    string json = await Task.Run(() =>
                    {
                        using (TimedWebClient client = new TimedWebClient())
                        {
                            client.Headers[HttpRequestHeader.ContentType] = "application/json";
                            return client.DownloadString(validateUrl);
                        }
                    });
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
                    string useUrl = ApiBase + "/api/keys/use/" + Uri.EscapeDataString(keyId);
                    await Task.Run(() =>
                    {
                        using (TimedWebClient post = new TimedWebClient())
                        {
                            post.Headers[HttpRequestHeader.ContentType] = "application/json";
                            post.UploadString(useUrl, "POST", "{}");
                        }
                    });
                    // Tell the website this PIN has started scanning (live status).
                    try
                    {
                        string startBody = Newtonsoft.Json.JsonConvert.SerializeObject(new { code = licenseKey, keyId = keyId });
                        await Task.Run(() =>
                        {
                            using (TimedWebClient pinStart = new TimedWebClient())
                            {
                                pinStart.Headers[HttpRequestHeader.ContentType] = "application/json";
                                pinStart.UploadString(ApiBase + "/api/pins/start", "POST", startBody);
                            }
                        });
                    }
                    catch { }

                    SetLabel("identity verified - deploying scan", System.Drawing.Color.FromArgb(52, 211, 153));
                    await Task.Delay(500);
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
                {
                    string url = ApiBase + "/api/scanner/config?keyId=" + Uri.EscapeDataString(keyId ?? "");
                    string json = await Task.Run(() =>
                    {
                        using (TimedWebClient client = new TimedWebClient())
                        {
                            return client.DownloadString(url);
                        }
                    });
                    Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    Newtonsoft.Json.Linq.JObject cfg = (obj["config"] as Newtonsoft.Json.Linq.JObject) ?? new Newtonsoft.Json.Linq.JObject();
                    _remoteCfg = cfg;
                    _remoteStrict = (bool)(cfg["strictMode"] ?? false);

                    // Pull the dashboard Custom Strings + detection modules into the
                    // engine before the scan runs.
                    LoadRemoteCustomStrings(cfg);
                    Console.WriteLine($"Remote config: {_remoteCustomStrings.Count} custom string(s), " +
                        $"{SecurityTools.OceanScan.DetectionModules.Count} module switch(es).");

                    string accent = (string)(cfg["accentColor"] ?? "");
                    System.Drawing.Color col = ParseHexColor(accent);
                    if (!col.IsEmpty)
                    {
                        oceanProgress.Invoke((MethodInvoker)(() =>
                        {
                            try
                            {
                                oceanProgress.ProgressColor = col;
                                oceanProgress.ProgressColor2 = System.Drawing.Color.FromArgb(255,
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
                label2.BeginInvoke((MethodInvoker)(() =>
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
        {                try
                {
                    label2.BeginInvoke((MethodInvoker)(() =>
                    {
                        label2.Location = x <= 0

                        ? new Point((this.ClientSize.Width - label2.Width) / 2, y)
                        : new Point(x, y);
                }));
            }
            catch { }
        }

        private void SetLabelFontSize(float size)
        {                try
                {
                    label2.BeginInvoke((MethodInvoker)(() =>
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

            // Self-running animation. With the MOTION switch off none of this
            // advances, so the window is genuinely still instead of merely
            // slower.
            if (motionEnabled)
            {
                AnimateParticles();
                AdvanceLogo();
                AdvanceBackdropSquares();
            }

            // Update particle highlights based on mouse position (light-reactive)
            UpdateParticleHighlights();

            // Pulse the leading cell of the scan bar. Advance() returns straight
            // away while the bar is hidden, so this costs nothing outside a scan.
            if (oceanProgress != null) oceanProgress.Advance();

            // Hover transitions. They snap instead of easing when motion is
            // off, so hovering still lights things up without animating.
            float hoverEase = motionEnabled ? 0.15f : 1f;
            hoverClose += ((isMouseOverClose ? 1f : 0f) - hoverClose) * hoverEase;
            hoverMin += ((isMouseOverMin ? 1f : 0f) - hoverMin) * hoverEase;
            hoverTextBox += (((isMouseOverTextBox || (siticoneTextBox2 != null && siticoneTextBox2.Focused)) ? 1f : 0f) - hoverTextBox) * hoverEase;

            // Cursor easing - the light still follows the pointer with motion
            // off, it just does not lag behind it.
            float cursorEase = motionEnabled ? 0.4f : 1f;
            smoothMousePos.X += (currentMousePos.X - smoothMousePos.X) * cursorEase;
            smoothMousePos.Y += (currentMousePos.Y - smoothMousePos.Y) * cursorEase;

            // Dynamic button hover styling - all dark purple, no red/pink borders
            if (siticoneButton1 != null)
            {
                // Close button - black base, dark purple glow on hover, no colored border
                siticoneButton1.FillColor = ColorBlend(Color.FromArgb(8, 8, 10), Color.FromArgb(35, 20, 50), hoverClose);
                siticoneButton1.BorderColor = Color.FromArgb(0, 0, 0, 0); // no border color
            }
            if (siticoneButton2 != null)
            {
                // Minimize button - black base, dark purple glow on hover, no colored border
                siticoneButton2.FillColor = ColorBlend(Color.FromArgb(8, 8, 10), Color.FromArgb(35, 20, 50), hoverMin);
                siticoneButton2.BorderColor = Color.FromArgb(0, 0, 0, 0); // no border color
            }
            if (siticoneTextBox2 != null)
            {
                // TextBox - black background, dark purple glow on hover/focus, no colored border edges
                siticoneTextBox2.BorderColor = Color.FromArgb(0, 0, 0, 0); // no visible border
                siticoneTextBox2.FillColor = Color.FromArgb(10, 10, 14);
            }

            // Emit cursor trail (trails are motion, so they stop with it)
            if (motionEnabled && currentMousePos.X >= 0 && currentMousePos.Y >= 0 &&
                (Math.Abs(currentMousePos.X - smoothMousePos.X) > 0.4f || Math.Abs(currentMousePos.Y - smoothMousePos.Y) > 0.4f))
            {
                cursorTrails.Add(new CursorTrail
                {
                    Position = smoothMousePos,
                    Alpha = 1f,
                    Size = 7f,
                    Color = Color.FromArgb(160, 90, 240)
                });
                // Also emit a secondary wider glow trail for amplified light effect
                cursorTrails.Add(new CursorTrail
                {
                    Position = smoothMousePos,
                    Alpha = 0.6f,
                    Size = 14f,
                    Color = Color.FromArgb(100, 50, 200)
                });
                if (cursorTrails.Count > 25) cursorTrails.RemoveAt(0);
            }

            // Fade cursor trails
            if (motionEnabled)
            {
                for (int i = cursorTrails.Count - 1; i >= 0; i--)
                {
                    var ct = cursorTrails[i];
                    ct.Alpha -= 0.05f;
                    ct.Size *= 0.94f;
                    if (ct.Alpha <= 0 || ct.Size <= 0.4f) cursorTrails.RemoveAt(i);
                }
            }

            // Expand click ripples
            if (motionEnabled)
            {
                for (int i = clickRipples.Count - 1; i >= 0; i--)
                {
                    var cr = clickRipples[i];
                    cr.Radius += 2.5f;
                    cr.Alpha = Math.Max(0f, 1f - (cr.Radius / cr.MaxRadius));
                    if (cr.Radius >= cr.MaxRadius || cr.Alpha <= 0) clickRipples.RemoveAt(i);
                }
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
                    int cur = oceanProgress.Value;
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
                        oceanProgress.Value = next;
                        oceanProgress.Text = next + "%";
                    }
                    pv = oceanProgress.Value;

                    if (pv > 85)
                    {
                        oceanProgress.ProgressColor = Color.FromArgb(140, 80, 225);
                        oceanProgress.ProgressColor2 = Color.FromArgb(110, 50, 200);
                    }
                    else
                    {
                        // flowing dark purple gradient while scanning
                        double hue = 265 + Math.Sin(progressTicks / 3.0) * 10;
                        oceanProgress.ProgressColor = HsvToColor(hue);
                        oceanProgress.ProgressColor2 = HsvToColor(hue + 8);
                    }
                }
                catch { }
                try
                {
                    label2.BeginInvoke((MethodInvoker)(() =>
                    {
                        label2.Text = "scanning " + pv + "%";
                        label2.ForeColor = pv > 85 ? Color.FromArgb(52, 211, 153) : Color.FromArgb(216, 180, 254);
                        label2.AutoSize = true;
                        label2.Left = (this.ClientSize.Width - label2.Width) / 2;
                    }));
                }
                catch { }
                // Push the real progress + any new findings to the website.
                // Fire at most one post at a time; the feed throttles itself too.
                try
                {
                    if (System.Threading.Interlocked.CompareExchange(ref _livePostGate, 1, 0) == 0)
                    {
                        int lp = pv;
                        Task.Run(() =>
                        {
                            try { PostScannerLive(lp, "scanning"); }
                            finally { System.Threading.Interlocked.Exchange(ref _livePostGate, 0); }
                        });
                    }
                }
                catch { }
            }
            else if (countdownRunning)
            {
                countdownRemaining -= 0.016;
                int secs = (int)Math.Ceiling(Math.Max(0.0, countdownRemaining));
                try
                {
                    oceanProgress.Text = secs <= 0 ? "" : secs.ToString();
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
            // Logo picture removed - no action
        }

        private void siticonePictureBox1_Click_1(object sender, EventArgs e)
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
        public System.Drawing.Color BaseColor { get; set; }
        public System.Drawing.Color GlowColor { get; set; }
        public System.Drawing.Brush Brush { get; set; }
        /// <summary>Shared always-on brush for this particle (its BaseColor).</summary>
        public System.Drawing.Brush BaseBrush { get; set; }
        public bool Highlighted { get; set; }
    }



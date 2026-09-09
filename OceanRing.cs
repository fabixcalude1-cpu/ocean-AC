using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Ocean_ac
{
    public class OceanRing : Control
    {
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 0;
        private Color _progressColor = Color.FromArgb(168, 85, 247);
        private Color _progressColor2 = Color.FromArgb(232, 121, 249);
        private bool _showText = true;

        public OceanRing()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(150, 150);
            Font = new Font("Bahnschrift", 15F, FontStyle.Bold);
            ForeColor = Color.White;
        }

        public int Minimum { get { return _minimum; } set { _minimum = value; Invalidate(); } }
        public int Maximum { get { return _maximum; } set { _maximum = value; Invalidate(); } }
        public int Value
        {
            get { return _value; }
            set
            {
                int bounded = Math.Max(_minimum, Math.Min(_maximum, value));
                if (bounded != _value) { _value = bounded; Invalidate(); }
                else if (value != _value) { _value = bounded; }
            }
        }
        public Color ProgressColor { get { return _progressColor; } set { _progressColor = value; Invalidate(); } }
        public Color ProgressColor2 { get { return _progressColor2; } set { _progressColor2 = value; Invalidate(); } }
        public bool ShowText { get { return _showText; } set { _showText = value; Invalidate(); } }

        private float PulsePhase { get { return (Environment.TickCount % 2600) / 2600f; } }
        private float OrbitPhase { get { return (Environment.TickCount % 3200) / 3200f; } }

        private static Color HsvToColor(double hue, double sat, double val)
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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = ClientRectangle;
            int side = Math.Min(rect.Width, rect.Height);
            int thickness = Math.Max(9, side / 9);

            float margin = thickness / 2f + 3f;
            RectangleF band = new RectangleF(
                rect.Left + margin,
                rect.Top + margin,
                side - margin * 2f,
                side - margin * 2f);

            float progress = _maximum <= _minimum ? 0f :
                Math.Max(0f, Math.Min(1f, (float)(_value - _minimum) / (_maximum - _minimum)));

            bool done = _maximum > _minimum && _value >= _maximum;
            float pulse = PulsePhase;
            float orbit = OrbitPhase;

            // 1) Breathing outer glow (alpha oscillates), colored with the current accent.
            float glowAlpha = 42f + 26f * (float)Math.Sin(pulse * Math.PI * 2f);
            /*
            using (Pen glow = new Pen(Color.FromArgb((int)glowAlpha, done ? _progressColor : _progressColor),
                thickness + 8f))
            {
                glow.StartCap = glow.EndCap = LineCap.Round;
                g.DrawEllipse(glow, band);
            }
            */

            // 2) Track (dark well).
            /*
            using (Pen track = new Pen(Color.FromArgb(40, 48, 66, BandAlpha(done, pulse)), thickness))
            {
                track.StartCap = track.EndCap = LineCap.Round;
                g.DrawEllipse(track, band);
            }
            */

            // 3) Counter-orbiting comet dash (keeps it alive even at 0%).
            float comet = orbit * 360f;
            /*
            using (Pen orbitPen = new Pen(Color.FromArgb(30 + (int)(25f * (0.5f + 0.5f * (float)Math.Sin(orbit * Math.PI * 2f))), 138, 180, 255), thickness))
            {
                orbitPen.StartCap = orbitPen.EndCap = LineCap.Round;
                float dashLen = 34f + 26f * (float)Math.Sin(orbit * Math.PI * 2f);
                g.DrawArc(orbitPen, band, comet, dashLen);
            }
            */

            if (progress > 0f)
            {
                // 4) Soft glow fatter than the arc (same hue family).
                using (Pen softGlow = new Pen(Color.FromArgb(80, _progressColor2), thickness + 5f))
                {
                    softGlow.StartCap = softGlow.EndCap = LineCap.Round;
                    g.DrawArc(softGlow, band, -90f, 360f * progress);
                }

                // 5) Gradient arc.
                using (LinearGradientBrush brush = new LinearGradientBrush(band, _progressColor, _progressColor2, 70f))
                using (Pen arcPen = new Pen(brush, thickness))
                {
                    arcPen.StartCap = arcPen.EndCap = LineCap.Round;
                    g.DrawArc(arcPen, band, -90f, 360f * progress);
                }

                // 6) Sheen sliding along the arc (a bright sweep that travels with progress).
                float sheenPos = (-90f + 360f * progress * (0.15f + 0.85f * pulse)) % 360f;
                using (Pen sheen = new Pen(Color.FromArgb(150, 255, 255, 255), thickness - 3f))
                {
                    sheen.StartCap = sheen.EndCap = LineCap.Round;
                    g.DrawArc(sheen, band, sheenPos, 10f);
                }

                // 7) Bright leading tip of the arc.
                if (progress < 1f)
                {
                    float tipAngle = -90f + 360f * progress;
                    using (Pen tip = new Pen(Color.FromArgb(240, 255, 255, 255), thickness - 1f))
                    {
                        tip.StartCap = tip.EndCap = LineCap.Round;
                        g.DrawArc(tip, band, tipAngle - 1.5f, 3f);
                    }
                }
            }

            // 8) Inner disc: soft radial depth.
            /*
            RectangleF inner = band;
            float inset = thickness + 2f;
            inner.Inflate(-inset, -inset);
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(inner);
                using (PathGradientBrush pb = new PathGradientBrush(path))
                {
                    pb.CenterColor = Color.FromArgb(150, 28, 34, 48);
                    pb.SurroundColors = new Color[] { Color.FromArgb(215, 10, 13, 20) };
                    g.FillPath(pb, path);
                }
            }
            */

            // 9) Breathing inner ring.
            /*
            using (Pen innerBorder = new Pen(Color.FromArgb((int)(45f + 18f * (0.5f + 0.5f * (float)Math.Sin(pulse * Math.PI * 2f))),
                done ? 74 : 96, done ? 222 : 140, done ? 128 : 238), 1.6f))
            {
                g.DrawEllipse(innerBorder, inner);
            }
            */

            // 10) Orbiting sparkles inside the disc.
            /*
            float sparkR = inner.Width * 0.5f;
            for (int i = 0; i < 2; i++)
            {
                double a = orbit * Math.PI * 2d + i * Math.PI;
                PointF sp = new PointF(
                    inner.X + inner.Width / 2f + (float)Math.Cos(a) * sparkR * 0.62f,
                    inner.Y + inner.Height / 2f + (float)Math.Sin(a) * sparkR * 0.62f);
                float sr = 1.6f + 0.8f * (float)Math.Sin(pulse * Math.PI * 2d + i * 2d);
                using (SolidBrush sb = new SolidBrush(Color.FromArgb((int)(120 + 90 * Math.Abs(Math.Sin(pulse * Math.PI * 2d + i))),
                    done ? 74 : 34, done ? 222 : 211, done ? 128 : 238)))
                {
                    g.FillEllipse(sb, sp.X - sr, sp.Y - sr, sr * 2f, sr * 2f);
                }
            }
            */

            // 11) Center text / checkmark.
            if (_showText && !string.IsNullOrEmpty(Text))
            {
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    if (done)
                    {
                        // Animated check sweep + green % text.
                        float sw = (float)Math.Min(1f, (pulse * 1.6f));
                        using (Pen tick = new Pen(Color.FromArgb(120, 74, 222, 128), thickness - 2f))
                        {
                            tick.StartCap = tick.EndCap = LineCap.Round;
                            PointF c = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f + 6f);
                            float s = Math.Min(rect.Width, rect.Height) * 0.30f;
                            g.DrawLines(tick, new[]{
                                new PointF(c.X - s * sw, c.Y - s * 0.15f * sw),
                                new PointF(c.X - s * 0.2f * sw, c.Y + s * 0.55f * sw),
                                new PointF(c.X + s * sw, c.Y - s * 0.6f * sw) });
                        }
                        using (SolidBrush tb = new SolidBrush(Color.FromArgb(74, 222, 128)))
                        {
                            g.DrawString(Text, Font, tb, rect, sf);
                        }
                    }
                    else
                    {
                        using (SolidBrush tb = new SolidBrush(ForeColor))
                        {
                            g.DrawString(Text, Font, tb, rect, sf);
                        }
                    }
                }
            }
        }

        private static int BandAlpha(bool done, float pulse)
        {
            return done ? (int)(70f + 40f * (0.5f + 0.5f * (float)Math.Sin(pulse * Math.PI * 2f))) : 62;
        }
    }
}
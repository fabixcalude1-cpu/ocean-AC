using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Ocean_ac
{
    /// <summary>
    /// Ocean linear progress bar. Driven by real scan progress (no synthetic
    /// 99% creep): the fill tracks the engine's weighted stages and gently
    /// decelerates toward completion — no parking at 98-99-100%.
    /// </summary>
    public class OceanBar : Control
    {
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 0;
        private Color _progressColor = Color.FromArgb(168, 85, 247);
        private Color _progressColor2 = Color.FromArgb(232, 121, 249);
        private bool _showText = true;
        private Timer _anim;

        public OceanBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(420, 30);
            Font = new Font("Bahnschrift", 11F, FontStyle.Bold);
            ForeColor = Color.White;
            _anim = new Timer { Interval = 33 };
            _anim.Tick += delegate { Invalidate(); };
            _anim.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            if (_anim != null)
            {
                _anim.Stop();
                _anim.Dispose();
                _anim = null;
            }
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
            int pad = 5;
            float x = rect.X + pad;
            float w = rect.Width - pad * 2f;
            float trackH = rect.Height - pad * 2f;
            if (w <= 2f || trackH <= 2f) return;
            float radius = trackH / 2f;

            float progress = _maximum <= _minimum ? 0f :
                Math.Max(0f, Math.Min(1f, (float)(_value - _minimum) / (_maximum - _minimum)));

            bool done = _maximum > _minimum && _value >= _maximum;
            float pulse = (float)((Environment.TickCount % 2600) / 2600d);

            RectangleF trackRect = new RectangleF(x, rect.Y + pad, w, trackH);

            // 1) Track well (rounded).
            using (GraphicsPath tp = RoundedRect(trackRect, radius))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(24, 28, 40)))
            {
                g.FillPath(trackBrush, tp);
            }
            using (GraphicsPath tp = RoundedRect(trackRect, radius))
            using (Pen borderPen = new Pen(Color.FromArgb(70, 122, 60, 240), 1f))
            {
                g.DrawPath(borderPen, tp);
            }

            // 2) Ambient inner glow creeping ahead of the fill (feels alive even at low %).
            float glowW = Math.Min(w, Math.Max(0f, progress * w + radius * 2.2f));
            if (glowW > 2f)
            {
                RectangleF glowRect = new RectangleF(x, rect.Y + pad, glowW, trackH);
                using (GraphicsPath gp = RoundedRect(glowRect, radius))
                using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(36, _progressColor2)))
                {
                    g.FillPath(glowBrush, gp);
                }
            }

            // 3) Gradient fill (rounded, decelerating toward the end).
            float fillW = progress * w;
            if (fillW > 1.5f)
            {
                RectangleF fillRect = new RectangleF(x, rect.Y + pad, fillW, trackH);
                using (GraphicsPath fp = RoundedRect(fillRect, radius))
                using (LinearGradientBrush fillBrush = new LinearGradientBrush(fillRect, _progressColor, _progressColor2, 0f))
                {
                    g.FillPath(fillBrush, fp);
                }

                // 4) Animated sheen sweeping along the fill (keeps the bar moving smoothly).
                float sheenWidth = Math.Max(20f, Math.Min(52f, w * 0.12f));
                float sheenPos = fillW * (0.10f + 0.90f * pulse);
                RectangleF sheenRect = new RectangleF(x + sheenPos - sheenWidth / 2f, rect.Y + pad, sheenWidth, trackH);
                using (GraphicsPath fp = RoundedRect(fillRect, radius))
                {
                    g.SetClip(fp);
                    using (LinearGradientBrush sb = new LinearGradientBrush(
                        sheenRect, Color.FromArgb(0, 255, 255, 255), Color.FromArgb(190, 255, 255, 255), 0f))
                    {
                        g.FillRectangle(sb, sheenRect);
                    }
                    g.ResetClip();
                }

                // 5) Bright leading tip (only while still scanning).
                if (progress < 0.995f)
                {
                    float tipH = trackH * 0.52f;
                    using (SolidBrush tipBrush = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                    {
                        g.FillEllipse(tipBrush, fillRect.Right - radius, fillRect.Y + (trackH - tipH) / 2f, radius, tipH);
                    }
                }
            }

            // 6) Percentage label — right aligned, outside the fill when possible.
            if (_showText && !string.IsNullOrEmpty(Text))
            {
                float donePulse = done
                    ? (float)(0.5d + 0.5d * Math.Sin(pulse * Math.PI * 2d))
                    : 0f;
                Color textColor = done
                    ? Color.FromArgb((int)(110 + 90 * donePulse), 74, 222, 128)
                    : (progress >= 0.995f && !done ? _progressColor : ForeColor);

                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (SolidBrush tb = new SolidBrush(textColor))
                {
                    // Low glow behind the text for readability on the track.
                    using (SolidBrush halo = new SolidBrush(Color.FromArgb(160, 10, 12, 18)))
                    {
                        RectangleF haloRect = new RectangleF(rect.X, rect.Y + pad, rect.Width, trackH);
                        g.FillRectangle(halo, haloRect);
                    }
                    g.DrawString(Text, Font, tb, rect, sf);
                }
            }

            // 7) Faint segment ticks for a scanning feel.
            if (!done)
            {
                using (Pen tickPen = new Pen(Color.FromArgb(26, 255, 255, 255), 1f))
                {
                    float spacing = Math.Max(26f, w / 14f);
                    for (float sx = x + spacing; sx < x + w; sx += spacing)
                    {
                        g.DrawLine(tickPen, sx, rect.Y + pad, sx, rect.Y + pad + trackH * 0.28f);
                    }
                }
            }
        }

        private static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            radius = Math.Min(radius, Math.Min(r.Width, r.Height) / 2f);
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0.5f)
            {
                p.AddRectangle(r);
                p.CloseFigure();
                return p;
            }
            float d = radius * 2f;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
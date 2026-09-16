using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Ocean_ac
{
    /// <summary>
    /// Small iOS-style pill switch with an ON/OFF caption, used for the
    /// "motion" setting. Clicking anywhere on the control toggles it.
    ///
    /// The switch rounds off its own state with a short eased slide so the
    /// click still feels alive — but the slide is driven by the host's tick,
    /// and freezes cleanly when the host stops calling Advance(). That is what
    /// lets the switch turn motion off without relying on motion.
    /// </summary>
    public class OceanMotionSwitch : Control
    {
        private bool _checked = true;
        private float _thumb = 1f;          // 0 = off (left), 1 = on (right)
        private bool _hot;
        private readonly Timer _hoverClock;

        public OceanMotionSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.StandardClick, true);
            BackColor = Color.Transparent;
            Size = new Size(104, 26);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 7.4F, FontStyle.Bold);
            ForeColor = Color.FromArgb(120, 100, 150, 210);

            // A 1 Hz clock just for the hover glow: the main animation timer
            // stops when motion is off, and the switch must stay responsive.
            _hoverClock = new Timer();
            _hoverClock.Interval = 60;
            _hoverClock.Tick += (s, e) => Advance();
            _hoverClock.Start();
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                OnCheckedChanged(EventArgs.Empty);
                Invalidate();
            }
        }

        public event EventHandler CheckedChanged;

        protected virtual void OnCheckedChanged(EventArgs e)
        {
            EventHandler handler = CheckedChanged;
            if (handler != null) handler(this, e);
        }

        /// <summary>Eases the thumb; safe to call every frame, cheap when idle.</summary>
        public void Advance()
        {
            float target = _checked ? 1f : 0f;
            if (Math.Abs(target - _thumb) < 0.002f)
            {
                _thumb = target;
                return;
            }
            _thumb += (target - _thumb) * 0.22f;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hot = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hot = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) Checked = !Checked;
            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int trackW = 40;
            int trackH = 20;
            int trackY = (ClientSize.Height - trackH) / 2;
            Rectangle track = new Rectangle(1, trackY, trackW, trackH);

            Color edge = _checked
                ? Color.FromArgb(200, 122, 68, 192)
                : Color.FromArgb(70, 255, 255, 255);

            // Track: black fill, no loud colour unless it is switched on.
            using (GraphicsPath path = Pill(track))
            {
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(235, 6, 6, 9)))
                {
                    g.FillPath(fill, path);
                }
                if (_hot && !_checked)
                {
                    using (GraphicsPath hot = Pill(track))
                    using (PathGradientBrush glow = new PathGradientBrush(hot))
                    {
                        glow.CenterColor = Color.FromArgb(38, 122, 68, 192);
                        glow.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(glow, hot);
                    }
                }
                using (Pen pen = new Pen(edge, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Caption inside the track, on the free side of the thumb.
            string caption = _checked ? "ON" : "OFF";
            using (SolidBrush text = new SolidBrush(_checked
                ? Color.FromArgb(200, 190, 160, 255)
                : Color.FromArgb(160, 200, 198, 215)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                RectangleF area = _checked
                    ? new RectangleF(track.X + 2, track.Y, track.Width * 0.55f, track.Height)
                    : new RectangleF(track.X + track.Width * 0.45f, track.Y, track.Width * 0.55f, track.Height);
                g.DrawString(caption, Font, text, area, sf);
            }

            // Thumb: white when on, grey when off, sliding between the two.
            float pad = 2.5f;
            float diameter = trackH - pad * 2f;
            float x = track.X + pad + _thumb * (track.Width - diameter - pad * 2f);
            RectangleF thumb = new RectangleF(x, track.Y + pad, diameter, diameter);
            using (SolidBrush tb = new SolidBrush(_checked ? Color.White : Color.FromArgb(235, 150, 150, 162)))
            {
                g.FillEllipse(tb, thumb);
            }
            if (_checked)
            {
                using (GraphicsPath hp = new GraphicsPath())
                {
                    hp.AddEllipse(thumb);
                    using (PathGradientBrush glow = new PathGradientBrush(hp))
                    {
                        glow.CenterColor = Color.FromArgb(90, 157, 98, 224);
                        glow.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(glow, hp);
                    }
                }
            }

            // Label to the right of the track.
            using (SolidBrush lb = new SolidBrush(_checked
                ? Color.FromArgb(210, 216, 180, 254)
                : Color.FromArgb(150, 150, 148, 165)))
            using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                g.DrawString("MOTION", Font, lb,
                    new RectangleF(track.Right + 7f, 0f, Math.Max(10f, ClientSize.Width - track.Right - 7f), ClientSize.Height), sf);
            }
        }

        private static GraphicsPath Pill(Rectangle r)
        {
            GraphicsPath path = new GraphicsPath();
            int d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 90, 180);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 180);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _hoverClock != null)
            {
                try { _hoverClock.Stop(); _hoverClock.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
    }
}

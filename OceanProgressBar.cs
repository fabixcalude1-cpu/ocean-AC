using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Ocean_ac
{
    /// <summary>
    /// The scanner's progress bar: a strip of square cells that light up one by
    /// one, each a lit block of the checkered design instead of a smooth tube.
    /// The cell the scan is on burns brighter, and every crossing has a sound.
    ///
    /// It replaces the designer's progress control through the same members that
    /// were already in use (Minimum / Maximum / Value / Text / ProgressColor /
    /// ProgressColor2 / Visible), so the scan code drives it unchanged.
    ///
    /// Everything it draws is derived from its own size, so it stays correct at
    /// any window size and any DPI.
    /// </summary>
    public class OceanProgressBar : Control
    {
        private int _min;
        private int _max = 100;
        private int _value;
        private int _lastPlayedValue = -1;
        private float _pulse;
        private Color _c1 = Color.FromArgb(160, 90, 240);
        private Color _c2 = Color.FromArgb(147, 51, 234);
        private bool _sound = true;
        private Rectangle _muteRect = Rectangle.Empty;
        private bool _muteHot;

        public OceanProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.StandardClick, true);
            BackColor = Color.Transparent;
            Size = new Size(350, 30);
            Cursor = Cursors.Hand;
            Font = new Font("Consolas", 8.5F, FontStyle.Bold);
        }

        public int Minimum
        {
            get { return _min; }
            set { _min = value; Invalidate(); }
        }

        public int Maximum
        {
            get { return _max; }
            set { _max = value; Invalidate(); }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = value;
                if (clamped < _min) clamped = _min;
                if (clamped > _max) clamped = _max;
                if (clamped == _value) return;

                bool rising = clamped > _value;
                _value = clamped;

                if (_sound && rising)
                {
                    // a click per 5% step, and a verdict cue at the ends
                    int stepSize = Math.Max(1, (_max - _min) / 20);
                    if (_lastPlayedValue < 0 ||
                        clamped - _lastPlayedValue >= stepSize ||
                        clamped >= _max)
                    {
                        _lastPlayedValue = clamped;
                        OceanSound.Play(clamped >= _max ? OceanSound.Cue.Done : OceanSound.Cue.Tick);
                    }
                }

                Invalidate();
            }
        }

        /// <summary>Caption drawn inside the bar. Kept for designer compatibility.</summary>
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; Invalidate(); }
        }

        public Color ProgressColor
        {
            get { return _c1; }
            set { _c1 = value; Invalidate(); }
        }

        public Color ProgressColor2
        {
            get { return _c2; }
            set { _c2 = value; Invalidate(); }
        }

        /// <summary>Whether crossing a cell is audible.</summary>
        public bool SoundEnabled
        {
            get { return _sound; }
            set
            {
                _sound = value;
                OceanSound.Muted = !value;
                if (value) OceanSound.Play(OceanSound.Cue.Step);
                Invalidate();
            }
        }

        /// <summary>Advances the pulse of the leading cell. Called by the host's tick.</summary>
        public void Advance()
        {
            if (!Visible) return;
            _pulse += 0.09f;
            if (_pulse > 1000f) _pulse = 0f;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_muteRect != Rectangle.Empty && _muteRect.Contains(e.Location))
            {
                SoundEnabled = !SoundEnabled;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hot = _muteRect != Rectangle.Empty && _muteRect.Contains(e.Location);
            if (hot != _muteHot)
            {
                _muteHot = hot;
                Cursor = hot ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;
            if (w < 40 || h < 12) return;

            // layout: [ mute square ][ track ][ percent ]
            int pad = 3;
            int muteSize = Math.Max(14, h - pad * 2);
            _muteRect = new Rectangle(pad, (h - muteSize) / 2, muteSize, muteSize);

            int labelW = (int)Math.Round(h * 1.9f);
            int trackX = _muteRect.Right + pad * 2;
            int trackW = Math.Max(10, w - trackX - labelW - pad);
            int trackY = pad;
            int trackH = h - pad * 2;

            // cells: as many whole squares as fit, at least 8
            int cells = Math.Max(8, Math.Min(48, trackW / Math.Max(5, trackH / 2)));
            float cellW = trackW / (float)cells;

            double span = Math.Max(1, _max - _min);
            double filled = ((_value - _min) / span) * cells;
            if (filled < 0) filled = 0;
            if (filled > cells) filled = cells;

            // pulse of the leading cell
            float lead = (float)(0.55 + 0.45 * Math.Sin(_pulse));

            for (int i = 0; i < cells; i++)
            {
                float x = trackX + i * cellW;
                RectangleF cell = new RectangleF(x + 0.5f, trackY, Math.Max(2f, cellW - 1.4f), trackH);

                bool on = i < (int)Math.Floor(filled);
                bool isLead = !on && i < filled + 1 && filled > 0 && filled < cells;

                if (on)
                {
                    float t = i / (float)Math.Max(1, cells - 1);
                    Color a = Blend(_c1, _c2, t);
                    using (LinearGradientBrush b = new LinearGradientBrush(
                        cell, ControlPaint.Light(a), a, LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(b, cell);
                    }
                    using (SolidBrush glow = new SolidBrush(Color.FromArgb(70, a)))
                    {
                        g.FillRectangle(glow, cell.X - 1, cell.Y - 1, cell.Width + 2, cell.Height + 2);
                    }
                }
                else if (isLead)
                {
                    int a = (int)(120 * lead);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(a, _c1)))
                    {
                        g.FillRectangle(b, cell);
                    }
                }

                // empty cells keep a hairline so the strip still reads as a row of squares
                using (Pen edge = new Pen(on
                    ? Color.FromArgb(120, 255, 255, 255)
                    : Color.FromArgb(38, 255, 255, 255), 1f))
                {
                    g.DrawRectangle(edge, cell.X, cell.Y, cell.Width, cell.Height);
                }
            }

            // the mute square: filled when sound is on, hollow when off
            using (Pen pen = new Pen(Color.FromArgb(_muteHot ? 190 : 90, 157, 98, 224), 1.4f))
            {
                g.DrawRectangle(pen, _muteRect.X, _muteRect.Y, _muteRect.Width, _muteRect.Height);
            }
            int bar = _muteRect.Height / 3;
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(_sound ? 220 : 70, 200, 170, 255)))
            {
                g.FillRectangle(brush, _muteRect.X + 4, _muteRect.Y + _muteRect.Height / 2 - bar / 2,
                    Math.Max(3, _muteRect.Width - 9), bar);
            }

            // percent, drawn from Value so it can never disagree with the cells
            int pct = (int)Math.Round(((_value - _min) / span) * 100.0);
            string text = pct + "%";
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(220, 235, 228, 255)))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(text, Font, tb, new RectangleF(trackX + trackW, 0, labelW - pad, h), sf);
            }
        }

        private static Color Blend(Color a, Color b, float t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}

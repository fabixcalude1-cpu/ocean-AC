using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Ocean_ac
{
    /// <summary>
    /// The Ocean brand object (Ocean.ico, or the embedded logo.png) drawn as a
    /// real 3D solid.
    ///
    /// GDI+ has no 3D pipeline, so the solid is built by hand: the eight corners
    /// of an extruded box are rotated (yaw around Y, pitch around X),
    /// perspective-projected, and then painted back to front — back face, the
    /// four side walls, then the front face. The artwork reaches the front face
    /// as a set of thin vertical strips, each projected through the same
    /// transform, which is what gives the card true perspective (the far edge
    /// converges) instead of the flat cosine squeeze an offset copy produces.
    ///
    /// There is exactly ONE hero design, and this is the object at its centre:
    /// black canvas, white grid, one purple 3D logo. The page draws its own
    /// window around it, the desktop app uses its own window, and both take the
    /// grid step and the logo's share of the free area from the same numbers —
    /// no second window is nested inside either of them.
    ///
    /// The caller owns the rotation state, which is what makes the "motion off"
    /// switch trivial: stop advancing yaw/pitch and the same pose is redrawn.
    /// </summary>
    public static class OceanLogo3D
    {
        // ---------------------------------------------------------------- art

        private static Bitmap _art;
        private static bool _loadTried;

        /// <summary>Icon artwork. Ocean.ico first (largest frame), then the embedded logo.png.</summary>
        public static Bitmap Art
        {
            get
            {
                if (_art == null && !_loadTried)
                {
                    _loadTried = true;
                    _art = LoadArt();
                }
                return _art;
            }
        }

        private static Bitmap LoadArt()
        {
            // 1) Ocean.ico — prefer the biggest frame the file carries.
            string[] candidates = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ocean.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Ocean.ico"),
                "Ocean.ico"
            };

            foreach (string path in candidates)
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    Bitmap best = null;
                    int[] sizes = new int[] { 256, 128, 96, 64, 48, 32, 24, 16 };
                    foreach (int s in sizes)
                    {
                        try
                        {
                            using (Icon ico = new Icon(path, new Size(s, s)))
                            {
                                Bitmap frame = ico.ToBitmap();
                                if (best == null ||
                                    (long)frame.Width * frame.Height > (long)best.Width * best.Height)
                                {
                                    if (best != null) best.Dispose();
                                    best = frame;
                                }
                                else
                                {
                                    frame.Dispose();
                                }
                            }
                        }
                        catch { }
                    }
                    if (best != null) return best;
                }
                catch { }
            }

            // 2) the embedded logo.png the window used before.
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                foreach (string name in asm.GetManifestResourceNames())
                {
                    if (!name.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase)) continue;
                    using (Stream stream = asm.GetManifestResourceStream(name))
                    {
                        if (stream == null) continue;
                        using (Bitmap bmp = new Bitmap(stream)) return new Bitmap(bmp);
                    }
                }
            }
            catch { }

            try
            {
                string[] pngs = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png")
                };
                foreach (string p in pngs)
                {
                    if (!File.Exists(p)) continue;
                    using (Bitmap bmp = new Bitmap(p)) return new Bitmap(bmp);
                }
            }
            catch { }

            return null;
        }

        // ------------------------------------------------------------ palette

        private static readonly Color Purple1 = Color.FromArgb(76, 29, 149);    // deepest
        private static readonly Color Purple3 = Color.FromArgb(122, 68, 192);   // primary
        private static readonly Color Purple5 = Color.FromArgb(157, 98, 224);   // highlight

        private static Color A(Color c, float alpha)
        {
            int a = (int)Math.Round(Alpha(alpha) * c.A);
            if (a > 255) a = 255;
            if (a < 0) a = 0;
            return Color.FromArgb(a, c.R, c.G, c.B);
        }

        // ------------------------------------------------------------ vectors

        private struct V3
        {
            public float X, Y, Z;
            public V3(float x, float y, float z) { X = x; Y = y; Z = z; }
            public static V3 Lerp(V3 a, V3 b, float t)
            {
                return new V3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
            }
        }

        /// <summary>Yaw → pitch → perspective divide.</summary>
        private static PointF Project(V3 p, float yaw, float pitch, float cx, float cy, float focal)
        {
            float cY = (float)Math.Cos(yaw), sY = (float)Math.Sin(yaw);
            float cP = (float)Math.Cos(pitch), sP = (float)Math.Sin(pitch);

            float x1 = p.X * cY + p.Z * sY;
            float z1 = -p.X * sY + p.Z * cY;

            float y2 = p.Y * cP - z1 * sP;
            float z2 = p.Y * sP + z1 * cP;

            float s = focal / (focal + z2);
            if (s < 0.04f) s = 0.04f;
            if (s > 12f) s = 12f;

            return new PointF(cx + x1 * s, cy - y2 * s);
        }

        // -------------------------------------------------------- face art

        private static Bitmap _face;
        private static int _faceSize;

        /// <summary>
        /// The recessed panel that sits inside the chip's bevel: a dark violet
        /// gradient with the PNG logo glyph embossed on it.
        ///
        /// The raw artwork cannot be extruded on its own — Resources/logo.png is
        /// thin transparent line art, so an object made of it alone renders as a
        /// ghost you can see straight through. The glyph is composited onto a
        /// solid panel first, and drawn twice (a dark copy nudged down-right,
        /// then the lit copy) so it reads as embossed rather than printed.
        ///
        /// The panel is rendered once per pixel size and cached: re-rendering it
        /// every frame is what used to make the object expensive to draw.
        /// </summary>
        private static Bitmap FaceFor(int size)
        {
            size = Math.Max(24, Math.Min(768, size));
            if (_face != null && _faceSize == size) return _face;

            try
            {
                Bitmap built = BuildFaceArt(size);
                if (_face != null && _face != built) _face.Dispose();
                _face = built;
                _faceSize = size;
            }
            catch { }
            return _face;
        }

        private static Bitmap BuildFaceArt(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics fg = Graphics.FromImage(bmp))
            {
                fg.SmoothingMode = SmoothingMode.AntiAlias;
                fg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                fg.PixelOffsetMode = PixelOffsetMode.HighQuality;
                fg.Clear(Color.Transparent);

                Bitmap art = Art;
                if (art != null)
                {
                    // the mark itself, as large as the square allows
                    float box = size * 0.92f;
                    float ar = art.Width / (float)art.Height;
                    float gw = box, gh = box;
                    if (ar >= 1f) gh = box / ar; else gw = box * ar;

                    Rectangle dest = new Rectangle(
                        (int)Math.Round((size - gw) / 2f),
                        (int)Math.Round((size - gh) / 2f),
                        Math.Max(2, (int)Math.Round(gw)),
                        Math.Max(2, (int)Math.Round(gh)));

                    // lit copy: the thin line art is lifted to a clean white
                    using (ImageAttributes lit = Tint(3.6f, 40, 30, 92, 1f))
                    {
                        fg.DrawImage(art, dest, 0, 0, art.Width, art.Height,
                            GraphicsUnit.Pixel, lit);
                    }
                }
                else
                {
                    using (Font f = UiFont(size * 0.30f, FontStyle.Bold))
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
                    using (StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    })
                    {
                        fg.DrawString("OCEAN", f, tb, new RectangleF(0, 0, size, size), sf);
                    }
                }
            }
            return bmp;
        }

        // ------------------------------------------------------------- render

        /// <summary>
        /// Draws the brand object inside <paramref name="bounds"/>.
        ///
        /// The shape is a squared chip: an extruded prism whose front face is
        /// chamfered, with a recessed panel carrying the embossed logo. It is
        /// deliberately nothing like the old flat tile — the bevel, the cavity
        /// and the stepped extrusion are what give it depth from any angle.
        ///
        /// Nothing here advances on its own: the pose is exactly what the caller
        /// passes in, so the object is still until the pointer moves.
        /// </summary>
        /// <param name="yawDeg">Rotation around the vertical axis, in degrees.</param>
        /// <param name="pitchDeg">Pitch, adding a slight top-down tilt.</param>
        /// <param name="alpha">Master opacity, 0..1.</param>
        public static void Render(Graphics g, Rectangle bounds, float yawDeg, float pitchDeg, float alpha)
        {
            if (g == null || bounds.Width < 8 || bounds.Height < 8) return;
            alpha = Alpha(alpha);
            if (alpha <= 0.01f) return;

            int side = Math.Max(24, Math.Min(bounds.Width, bounds.Height));
            Bitmap face = FaceFor(side);
            if (face == null)
            {
                RenderFallback(g, bounds, alpha);
                return;
            }

            GraphicsState saved = g.Save();
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                Rectangle rect = new Rectangle(
                    bounds.X + (bounds.Width - side) / 2,
                    bounds.Y + (bounds.Height - side) / 2,
                    side, side);

                float yaw = yawDeg * (float)Math.PI / 180f;
                float pitch = pitchDeg * (float)Math.PI / 180f;

                /* The mark is extruded by stacking darker copies behind it, each
                   nudged along the direction the object is leaning. Simple, and
                   cheap: no per-strip projection, no bevel geometry. */
                float depth = side * 0.16f;
                float dx = (float)Math.Sin(yaw) * depth;
                float dy = -(float)Math.Sin(pitch) * depth * 0.7f;

                DrawBloom(g, rect.X + side / 2f, rect.Y + side / 2f, side * 0.9f, alpha);

                const int layers = 10;
                for (int i = layers; i >= 1; i--)
                {
                    float t = i / (float)layers;                 // 1 = furthest back
                    int ox = (int)Math.Round(-dx * t);
                    int oy = (int)Math.Round(-dy * t);

                    // deep violet at the back, brighter where it meets the face
                    float lift = 0.30f + 0.55f * (1f - t);
                    int r = (int)(44 + 90 * lift);
                    int gr = (int)(16 + 42 * lift);
                    int b = (int)(92 + 118 * lift);

                    using (ImageAttributes far = Tint(1f, r - 20, gr - 8, b - 40, 0.92f))
                    {
                        g.DrawImage(face,
                            new Rectangle(rect.X + ox, rect.Y + oy, side, side),
                            0, 0, face.Width, face.Height, GraphicsUnit.Pixel, far);
                    }
                }

                // the lit face on top
                using (ImageAttributes front = Tint(1f, 0, 0, 0, 1f))
                {
                    g.DrawImage(face, rect, 0, 0, face.Width, face.Height,
                        GraphicsUnit.Pixel, front);
                }

                DrawSheen(g, face, rect, yawDeg, alpha);
            }
            catch { }
            finally
            {
                try { g.Restore(saved); } catch { }
            }
        }

        private static Font UiFont(float size, FontStyle style)
        {
            try { return new Font("Bahnschrift", size, style); }
            catch
            {
                try { return new Font("Segoe UI", size, style); }
                catch { return new Font(FontFamily.GenericSansSerif, size, style); }
            }
        }

        // ------------------------------------------------------------ helpers

        /// <summary>
        /// The specular highlight sweeping the mark as it turns — the desktop twin
        /// of the hero's sheen on the website.
        ///
        /// The light is painted by blitting a slice of the glyph bitmap onto that
        /// same slice of the canvas, tinted bright and semi-transparent. Because the
        /// source and destination rectangles are the same size and position, the
        /// band can only ever land on pixels the artwork already covers: the glyph
        /// masks itself, so there is no alpha-mask pass and, more importantly, no
        /// bitmap allocated per frame (a moving band would otherwise rebuild one on
        /// every mouse move).
        /// </summary>
        private static void DrawSheen(Graphics g, Bitmap face, Rectangle rect, float yawDeg, float alpha)
        {
            if (face == null || rect.Width < 24) return;
            try
            {
                // the band travels the face as the object turns, and stops at the edges
                float f = yawDeg / 26f;
                if (f > 1f) f = 1f;
                else if (f < -1f) f = -1f;

                float bandW = rect.Width * 0.38f;
                float cx = rect.X + rect.Width * (0.5f - f * 0.40f);

                Rectangle band = new Rectangle(
                    (int)Math.Round(cx - bandW / 2f), rect.Y,
                    (int)Math.Round(bandW), rect.Height);
                band.Intersect(rect);
                if (band.Width < 3) return;

                /* The light has to show on a mark that is already white, and a
                   white band over white pixels changes nothing. So the band is
                   drawn the way light actually reads: as an aura around the part
                   of the glyph it covers. The same slice is blitted three times,
                   each one a little larger and fainter, producing the soft edge a
                   blur would give (GDI+ has no blur, and shelling out to one would
                   cost more than the whole logo). */
                int[] spread = new int[] { 22, 13, 6 };   // per-cent enlargement
                float[] glow = new float[] { 0.10f, 0.16f, 0.30f };

                for (int i = 0; i < spread.Length; i++)
                {
                    int grow = (int)Math.Round(band.Width * spread[i] / 100f);
                    Rectangle halo = new Rectangle(
                        band.X - grow / 2, band.Y - grow / 2,
                        band.Width + grow, band.Height + grow);

                    using (ImageAttributes attr = Tint(1.25f, 26, 12, 52, glow[i] * alpha))
                    {
                        g.DrawImage(face,
                            halo, band.X - rect.X, 0, band.Width, face.Height,
                            GraphicsUnit.Pixel, attr);
                    }
                }

                // and the band itself: catches the antialiased edges of the glyph,
                // where the face is not fully opaque yet and brightening still shows
                using (ImageAttributes soft = Tint(1.4f, 0, 0, 0, 0.30f * alpha))
                {
                    g.DrawImage(face,
                        band, band.X - rect.X, 0, band.Width, face.Height,
                        GraphicsUnit.Pixel, soft);
                }
            }
            catch { }
        }

        private static void DrawBloom(Graphics g, float cx, float cy, float side, float alpha)
        {
            try
            {
                float r = side * 0.95f;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(cx - r, cy - r, r * 2f, r * 2f);
                    using (PathGradientBrush pb = new PathGradientBrush(path))
                    {
                        pb.CenterColor = Color.FromArgb((int)(82 * alpha), 122, 68, 192);
                        pb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(pb, path);
                    }
                }
            }
            catch { }
        }

        private static void RenderFallback(Graphics g, Rectangle bounds, float alpha)
        {
            try
            {
                RectangleF r = new RectangleF(bounds.X + bounds.Width * 0.2f, bounds.Y + bounds.Height * 0.15f,
                    bounds.Width * 0.6f, bounds.Height * 0.7f);
                using (LinearGradientBrush b = new LinearGradientBrush(
                    new Rectangle((int)r.X, (int)r.Y, Math.Max(2, (int)r.Width), Math.Max(2, (int)r.Height)),
                    Color.FromArgb((int)(235 * alpha), 122, 68, 192),
                    Color.FromArgb((int)(235 * alpha), 44, 14, 92),
                    LinearGradientMode.ForwardDiagonal))
                using (Font f = UiFont(Math.Max(9f, bounds.Height * 0.20f), FontStyle.Bold))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb((int)(245 * alpha), 255, 255, 255)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.FillEllipse(b, r);
                    g.DrawString("OCEAN", f, tb, r, sf);
                }
            }
            catch { }
        }

        private static ImageAttributes Tint(float brightness, int addR, int addG, int addB, float alphaScale)
        {
            ColorMatrix matrix = new ColorMatrix(new float[][]
            {
                new float[] { brightness, 0f, 0f, 0f, 0f },
                new float[] { 0f, brightness, 0f, 0f, 0f },
                new float[] { 0f, 0f, brightness, 0f, 0f },
                new float[] { 0f, 0f, 0f, alphaScale, 0f },
                new float[] { addR / 255f, addG / 255f, addB / 255f, 0f, 1f }
            });
            ImageAttributes attr = new ImageAttributes();
            attr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            return attr;
        }

        private static float Alpha(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}

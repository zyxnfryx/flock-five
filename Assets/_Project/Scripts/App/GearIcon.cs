using UnityEngine;

namespace FlockFive
{
    // Settings cog. One solid ring, eight short rectangular teeth, a round hole.
    // Tooth and gap are about the same width at the tip, so it cannot read as a sun.
    // Baked once at high resolution with a 2x2 sample grid so the edges stay smooth.
    // GuiPool only pools labels, so this is the shared icon bake the button draws.
    public static class GearIcon
    {
        public const int Teeth = 8;
        public const int Resolution = 512;

        // SDF edge, before the stroke grows both ways. Stroke is the full rim.
        // Visual tip lands near 0.90, body near 0.75 of that, hole near 0.30,
        // and each tooth near 0.21 of the diameter so the tip duty stays above 0.45.
        const float Tip = 0.868f;
        const float Body = 0.741f * Tip;
        const float HoleR = 0.348f * Tip;
        const float Stroke = 0.064f;
        const float Half = 0.176f * Tip;
        const float Corner = 0.014f;
        const float Root = 0.70f * Body;

        static Texture2D _tex;
        static Color32[] _px;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (_tex != null)
            {
                Object.Destroy(_tex);
                _tex = null;
            }
            _px = null;
        }

        public static Texture2D Texture()
        {
            if (_tex != null) return _tex;
            var px = Pixels();
            var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "SettingsGear"
            };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _tex = tex;
            return tex;
        }

        public static Color32[] Pixels()
        {
            if (_px != null) return _px;
            int n = Resolution;
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            float scale = c;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Color acc = Color.clear;
                    const int k = 2;
                    float step = 1f / k;
                    for (int sy = 0; sy < k; sy++)
                    {
                        for (int sx = 0; sx < k; sx++)
                        {
                            float nx = (x + (sx + 0.5f) * step - c) / scale;
                            float ny = (y + (sy + 0.5f) * step - c) / scale;
                            acc += Sample(nx, ny);
                        }
                    }
                    acc /= (k * k);
                    px[y * n + x] = (Color32)acc;
                }
            }
            _px = px;
            return px;
        }

        // Opaque runs outside the ring. A tooth that crosses angle 0 counts once.
        public static int CountTeeth(Color32[] px, int n)
        {
            if (px == null || n < 8 || px.Length < n * n) return 0;
            float c = (n - 1) * 0.5f;
            float r = c * 0.72f;
            const int steps = 720;
            int runs = 0;
            bool prev = false;
            bool first = false;
            for (int i = 0; i < steps; i++)
            {
                float a = i * Mathf.PI * 2f / steps;
                int x = Mathf.Clamp(Mathf.RoundToInt(c + Mathf.Cos(a) * r), 0, n - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(c + Mathf.Sin(a) * r), 0, n - 1);
                bool on = px[y * n + x].a > 140;
                if (i == 0) first = on;
                if (on && !prev) runs++;
                prev = on;
            }
            if (first && prev) runs--;
            if (runs < 0) runs = 0;
            return runs;
        }

        public static bool HoleClear(Color32[] px, int n)
        {
            if (px == null || n < 8) return false;
            int c = n / 2;
            return px[c * n + c].a < 12;
        }

        // Each tooth's outer edge stays near the tip across its width (a flat top,
        // not a point). False when a tooth is missing.
        public static bool FlatTops(Color32[] px, int n)
        {
            if (px == null || n < 8) return false;
            float c = (n - 1) * 0.5f;
            const float probe = 0.045f;
            for (int i = 0; i < Teeth; i++)
            {
                float ang = Mathf.PI * 0.5f + i * (Mathf.PI * 2f / Teeth);
                float mid = EdgeRadius(px, n, c, ang, 0f);
                float side = EdgeRadius(px, n, c, ang, probe);
                if (mid < 0.80f || side < 0.80f) return false;
                if (Mathf.Abs(mid - side) > 0.06f) return false;
            }
            return true;
        }

        // Outer body radius over tooth-tip radius. About 0.75 for a solid ring.
        public static float RingRatio(Color32[] px, int n)
        {
            float tip = MeanOuter(px, n, true);
            float body = MeanOuter(px, n, false);
            return tip > 0.05f ? body / tip : 0f;
        }

        // Clear hole radius over tooth-tip radius. About 0.30.
        public static float HoleRatio(Color32[] px, int n)
        {
            float tip = MeanOuter(px, n, true);
            float hole = MeanHole(px, n);
            return tip > 0.05f ? hole / tip : 0f;
        }

        // Opaque fraction on a circle just outside the ring, where the teeth leave
        // the body. Kept for callers that still want the root duty.
        public static float DutyCycle(Color32[] px, int n)
        {
            if (px == null || n < 8) return 0f;
            float tip = MeanOuter(px, n, true);
            float body = MeanOuter(px, n, false);
            float r = body + (tip - body) * 0.12f;
            return ArcDuty(px, n, r);
        }

        // Opaque fraction on a circle near the tooth tip. Rectangular teeth are
        // narrowest in angle here, so this is the duty that has to stay chunky.
        public static float TipDuty(Color32[] px, int n)
        {
            float tip = MeanOuter(px, n, true);
            if (tip < 0.05f) return 0f;
            return ArcDuty(px, n, tip * 0.90f);
        }

        // Narrowest tooth, measured across the flat near the tip, over the
        // opaque diameter. A cog tooth is a wide block, not a ray.
        public static float MinToothWidth(Color32[] px, int n)
        {
            if (px == null || n < 8) return 0f;
            float tip = MeanOuter(px, n, true);
            if (tip < 0.05f) return 0f;
            float c = (n - 1) * 0.5f;
            float rr = tip * 0.90f;
            float min = 99f;
            for (int i = 0; i < Teeth; i++)
            {
                float ang = Mathf.PI * 0.5f + i * (Mathf.PI * 2f / Teeth);
                float dx = Mathf.Cos(ang);
                float dy = Mathf.Sin(ang);
                float tx = -dy;
                float ty = dx;
                float hi = Span(px, n, c, dx * rr, dy * rr, tx, ty, 1f);
                float lo = Span(px, n, c, dx * rr, dy * rr, tx, ty, -1f);
                float width = hi + lo;
                if (width < min) min = width;
            }
            return min / (tip * 2f);
        }

        static float ArcDuty(Color32[] px, int n, float r)
        {
            if (px == null || n < 8 || r < 0.05f) return 0f;
            float c = (n - 1) * 0.5f;
            const int steps = 720;
            int on = 0;
            for (int i = 0; i < steps; i++)
            {
                float a = i * Mathf.PI * 2f / steps;
                if (OpaqueAt(px, n, c, Mathf.Cos(a) * r, Mathf.Sin(a) * r)) on++;
            }
            return on / (float)steps;
        }

        static float Span(Color32[] px, int n, float c, float ox, float oy, float tx, float ty, float sign)
        {
            float last = 0f;
            for (int s = 0; s <= 160; s++)
            {
                float t = s * 0.003f * sign;
                if (!OpaqueAt(px, n, c, ox + tx * t, oy + ty * t)) break;
                last = Mathf.Abs(t);
            }
            return last;
        }

        static float MeanOuter(Color32[] px, int n, bool tooth)
        {
            if (px == null || n < 8) return 0f;
            float sum = 0f;
            for (int i = 0; i < Teeth; i++)
            {
                float ang = Mathf.PI * 0.5f + i * (Mathf.PI * 2f / Teeth);
                if (!tooth) ang += Mathf.PI / Teeth;
                sum += OuterRadius(px, n, ang);
            }
            return sum / Teeth;
        }

        static float MeanHole(Color32[] px, int n)
        {
            if (px == null || n < 8) return 0f;
            float sum = 0f;
            for (int i = 0; i < Teeth; i++)
            {
                float ang = Mathf.PI * 0.5f + (i + 0.5f) * (Mathf.PI * 2f / Teeth);
                sum += HoleRadius(px, n, ang);
            }
            return sum / Teeth;
        }

        static float OuterRadius(Color32[] px, int n, float ang)
        {
            float c = (n - 1) * 0.5f;
            float best = 0f;
            float dx = Mathf.Cos(ang);
            float dy = Mathf.Sin(ang);
            for (int s = 20; s <= 100; s++)
            {
                float t = s / 100f;
                if (OpaqueAt(px, n, c, dx * t, dy * t)) best = t;
            }
            return best;
        }

        static float HoleRadius(Color32[] px, int n, float ang)
        {
            float c = (n - 1) * 0.5f;
            float dx = Mathf.Cos(ang);
            float dy = Mathf.Sin(ang);
            for (int s = 0; s <= 80; s++)
            {
                float t = s / 100f;
                if (OpaqueAt(px, n, c, dx * t, dy * t)) return t;
            }
            return 0f;
        }

        static bool OpaqueAt(Color32[] px, int n, float c, float nx, float ny)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(c + nx * c), 0, n - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(c + ny * c), 0, n - 1);
            return px[y * n + x].a > 140;
        }

        static float EdgeRadius(Color32[] px, int n, float c, float ang, float ly)
        {
            float dx = Mathf.Cos(ang);
            float dy = Mathf.Sin(ang);
            float px0 = -dy;
            float py = dx;
            float best = 0f;
            for (int s = 20; s <= 100; s++)
            {
                float t = s / 100f;
                float x = dx * t + px0 * ly;
                float y = dy * t + py * ly;
                int ix = Mathf.Clamp(Mathf.RoundToInt(c + x * c), 0, n - 1);
                int iy = Mathf.Clamp(Mathf.RoundToInt(c + y * c), 0, n - 1);
                if (px[iy * n + ix].a > 140) best = t;
            }
            return best;
        }

        static Color Sample(float x, float y)
        {
            float d = GearSdf(x, y);
            float half = Stroke * 0.5f;
            float aa = 1.6f / (Resolution * 0.5f);
            float r = Mathf.Sqrt(x * x + y * y);
            // Soft shadow sits down-screen. Texture +y is up, so the shape is
            // sampled shifted up and the shadow lands below the cog.
            float sd = GearSdf(x - 0.012f, y + 0.026f);
            float shadow = Mathf.Clamp01(1f - Smooth(Mathf.InverseLerp(-0.004f, 0.040f, sd))) * 0.40f;

            if (d > half + aa)
            {
                Color under = Color.clear;
                if (r < HoleR - half * 0.35f) under = HoleShade(x, y, r);
                if (shadow < 0.012f) return under;
                var sh = new Color(0.05f, 0.025f, 0.012f, shadow);
                if (under.a < 0.004f) return sh;
                float a = under.a + sh.a * (1f - under.a);
                if (a > 0.42f) a = 0.42f;
                Color rgb = (under * under.a + sh * (sh.a * (1f - under.a))) / Mathf.Max(0.001f, under.a + sh.a * (1f - under.a));
                rgb.a = a;
                return rgb;
            }

            float e = 0.0065f;
            float gx = GearSdf(x + e, y) - GearSdf(x - e, y);
            float gy = GearSdf(x, y + e) - GearSdf(x, y - e);
            float gl = Mathf.Sqrt(gx * gx + gy * gy);
            float nx = gl > 1e-5f ? gx / gl : 0f;
            float ny = gl > 1e-5f ? gy / gl : 1f;
            // Light from the top-left. Outward normals facing it catch the bevel.
            float ldot = nx * -0.52f + ny * 0.85f;
            float light = Mathf.Clamp01(0.30f + (-x) * 0.62f + y * 0.70f);
            Color deep = new Color(0.28f, 0.13f, 0.04f, 1f);
            Color wood = new Color(0.62f, 0.38f, 0.12f, 1f);
            Color gold = new Color(1f, 0.88f, 0.46f, 1f);
            Color fill = Color.Lerp(deep, wood, Mathf.Clamp01(light * 1.05f));
            fill = Color.Lerp(fill, gold, Mathf.Clamp01((light - 0.28f) * 0.95f));
            float toothish = Mathf.Clamp01((r - (Body - 0.01f)) / 0.07f);
            fill = Color.Lerp(fill, Color.Lerp(wood, gold, 0.72f), toothish * 0.28f);

            float inside = -half - d;
            float bevel = Mathf.Clamp01(1f - inside / 0.055f);
            if (ldot >= 0f)
                fill = Color.Lerp(fill, new Color(1f, 0.97f, 0.78f, 1f), bevel * ldot * 0.82f);
            else
                fill = Color.Lerp(fill, new Color(0.16f, 0.07f, 0.02f, 1f), bevel * (-ldot) * 0.62f);

            float shade = Mathf.Clamp01(0.22f + (-x) * 0.16f + y * 0.24f);
            Color outline = Color.Lerp(new Color(0.07f, 0.03f, 0.012f, 1f), new Color(0.18f, 0.09f, 0.03f, 1f), shade);
            float fillW = Mathf.Clamp01((-half - d) / aa + 1f);
            Color rgbM = Color.Lerp(outline, fill, fillW);
            rgbM.a = Mathf.Clamp01((half + aa - d) / (aa * 2f));
            return rgbM;
        }

        // Darker ring just inside the hole. The center stays clear so the
        // garden shows through, and the alpha stays under the tooth test.
        static Color HoleShade(float x, float y, float r)
        {
            float inner = HoleR - Stroke * 0.5f;
            if (r > inner || r < inner * 0.38f) return Color.clear;
            float t = Mathf.Clamp01((r - inner * 0.38f) / (inner * 0.62f));
            float face = 1f;
            if (r > 0.001f)
                face = Mathf.Clamp01(0.20f + (-x / r) * 0.55f + (y / r) * 0.70f);
            float a = t * (0.10f + 0.62f * face) * 0.72f;
            if (a > 0.40f) a = 0.40f;
            return new Color(0.04f, 0.02f, 0.01f, a);
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        // Negative inside the solid. The stroke sits on this edge.
        static float GearSdf(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float d = Mathf.Max(HoleR - r, r - Body);
            for (int i = 0; i < Teeth; i++)
            {
                float ang = Mathf.PI * 0.5f + i * (Mathf.PI * 2f / Teeth);
                float dx = Mathf.Cos(ang);
                float dy = Mathf.Sin(ang);
                float lx = x * dx + y * dy;
                float ly = x * -dy + y * dx;
                float tooth = ToothSdf(lx, ly);
                if (tooth < d) d = tooth;
            }
            return d;
        }

        // Rounded rectangle. Constant width, so the tip is as chunky as the root.
        static float ToothSdf(float lx, float ly)
        {
            float cr = Corner;
            float cx = (Root + Tip) * 0.5f;
            float hx = (Tip - Root) * 0.5f - cr;
            float hy = Half - cr;
            if (hx < 0.008f) hx = 0.008f;
            if (hy < 0.008f) hy = 0.008f;
            float ax = Mathf.Abs(lx - cx) - hx;
            float ay = Mathf.Abs(ly) - hy;
            float ox = Mathf.Max(ax, 0f);
            float oy = Mathf.Max(ay, 0f);
            float outside = Mathf.Sqrt(ox * ox + oy * oy);
            float inside = Mathf.Min(Mathf.Max(ax, ay), 0f);
            return outside + inside - cr;
        }
    }
}

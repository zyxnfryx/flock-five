using UnityEngine;

namespace FlockFive
{
    // Settings cog. A solid ring, eight short trapezoid teeth, a clear hole,
    // and a thick dark rim. One silhouette: the teeth run into the ring.
    // Baked once at high resolution with a 2x2 sample grid so the edges stay smooth.
    // GuiPool only pools labels, so this is the shared icon bake the button draws.
    public static class GearIcon
    {
        public const int Teeth = 8;
        public const int Resolution = 512;

        // Geometric edge, before the stroke grows both ways. The stroke is about
        // 6% of the visual diameter. Body, hole, and tooth length are set so the
        // opaque silhouette lands near 0.72 / 0.28 / 0.28 of the tip radius.
        const float Tip = 0.86f;
        const float Body = 0.701f * Tip;
        const float HoleR = 0.362f * Tip;
        const float Stroke = 0.1277f * Tip;
        const float Corner = 0.022f * Tip;
        // Deep into the ring, outside the hole, so the rim does not cut a seam.
        const float Root = HoleR + Stroke + 0.03f;
        // Slight taper. Wide enough that the gold face survives the rim.
        const float HalfRoot = 0.132f;
        const float HalfTip = 0.096f;

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

        // Outer body radius over tooth-tip radius. About 0.72 for a solid ring.
        public static float RingRatio(Color32[] px, int n)
        {
            float tip = MeanOuter(px, n, true);
            float body = MeanOuter(px, n, false);
            return tip > 0.05f ? body / tip : 0f;
        }

        // Clear hole radius over tooth-tip radius. About 0.28.
        public static float HoleRatio(Color32[] px, int n)
        {
            float tip = MeanOuter(px, n, true);
            float hole = MeanHole(px, n);
            return tip > 0.05f ? hole / tip : 0f;
        }

        // Opaque fraction on a circle just outside the ring, where the teeth leave
        // the body. About half: tooth width at the base matches the gap.
        public static float DutyCycle(Color32[] px, int n)
        {
            if (px == null || n < 8) return 0f;
            float tip = MeanOuter(px, n, true);
            float body = MeanOuter(px, n, false);
            float r = body + (tip - body) * 0.12f;
            if (r < 0.05f) return 0f;
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
            if (d > half + aa) return Color.clear;

            // Top-left highlight, bottom-right shade. +y is up.
            float light = Mathf.Clamp01(0.38f + (-x) * 0.42f + y * 0.46f);
            Color wood = Color.Lerp(new Color(0.40f, 0.22f, 0.07f, 1f), new Color(0.78f, 0.52f, 0.18f, 1f), light);
            Color gold = Color.Lerp(new Color(0.62f, 0.40f, 0.10f, 1f), new Color(1f, 0.90f, 0.48f, 1f), light);
            float rad = Mathf.Sqrt(x * x + y * y);
            float metal = rad > Body - 0.01f ? 0.62f : 0.38f;
            Color fill = Color.Lerp(wood, gold, metal);

            float shade = Mathf.Clamp01(0.32f + (-x) * 0.18f + y * 0.28f);
            Color outline = Color.Lerp(new Color(0.07f, 0.035f, 0.012f, 1f), new Color(0.18f, 0.09f, 0.03f, 1f), shade);
            float fillW = Mathf.Clamp01((-half - d) / aa + 1f);
            Color rgb = Color.Lerp(outline, fill, fillW);
            rgb.a = Mathf.Clamp01((half + aa - d) / (aa * 2f));
            return rgb;
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

        static float ToothSdf(float lx, float ly)
        {
            float cr = Corner;
            float x0 = Root + cr;
            float x1 = Tip - cr;
            float hr = HalfRoot - cr;
            float ht = HalfTip - cr;
            if (hr < 0.01f) hr = 0.01f;
            if (ht < 0.01f) ht = 0.01f;
            if (x1 < x0 + 0.01f) x1 = x0 + 0.01f;
            return SdTrap(lx, ly, x0, x1, hr, ht) - cr;
        }

        // Sharp trapezoid, negative inside. Rounded by subtracting the corner radius.
        static float SdTrap(float px, float py, float x0, float x1, float hr, float ht)
        {
            // Same winding as the bake preview: each edge runs toward the previous vertex.
            float d = (px - x0) * (px - x0) + (py + hr) * (py + hr);
            float s = 1f;
            s = Edge(px, py, x0, -hr, x0, hr, s, ref d);
            s = Edge(px, py, x1, -ht, x0, -hr, s, ref d);
            s = Edge(px, py, x1, ht, x1, -ht, s, ref d);
            s = Edge(px, py, x0, hr, x1, ht, s, ref d);
            return s * Mathf.Sqrt(d);
        }

        static float Edge(float px, float py, float ax, float ay, float bx, float by, float s, ref float d)
        {
            float ex = bx - ax;
            float ey = by - ay;
            float wx = px - ax;
            float wy = py - ay;
            float denom = ex * ex + ey * ey;
            float t = denom <= 1e-12f ? 0f : Mathf.Clamp01((wx * ex + wy * ey) / denom);
            float bx2 = wx - ex * t;
            float by2 = wy - ey * t;
            float dd = bx2 * bx2 + by2 * by2;
            if (dd < d) d = dd;
            bool c1 = py >= ay;
            bool c2 = py < by;
            bool c3 = ex * wy > ey * wx;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            return s;
        }
    }
}

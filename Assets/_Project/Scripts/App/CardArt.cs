using UnityEngine;

namespace FlockFive
{
    // One shared card renderer for every album card (sleeve, carousel, inspect, collected popup).
    //   Face art  : FaceTheme painted procedurally into a small cached texture (CardTheme.cs holds the table).
    //   Frame art : honey-wood ring + brass bevel + hex-comb corners, cached per wood colour and size bucket.
    //   Ribbon    : bee-stripe nameplate ribbon behind the title.
    //   Badge     : bottom-centre oval rarity jewel (Normal amber, Holo prismatic, Inverse rainbow cycle).
    // Everything is cached Texture2D drawn with GUI.DrawTexture. Nothing here allocates per frame; bakes are
    // budgeted per frame and fall back to the old flat plate for a frame or two while they catch up.
    public static class CardArt
    {
        // ---- lettering rim (read by FlockFiveApp.StampOutlined) ----
        public static Color RimOuter = new Color(0.02f, 0.02f, 0.02f, 1f);
        public static Color RimInner = Color.white;
        public static bool RibbonOn;
        public static Color TextInk = new Color(0.16f, 0.07f, 0.02f, 1f);
        public static Color FlavorInk = new Color(0.18f, 0.08f, 0.03f, 1f);
        // Scope the lettering for one themed card: fill, rims, and the nameplate ribbon. Always paired with ResetInk.
        public static void UseInk(in FaceStyle st)
        {
            RimOuter = st.InkOuter; RimInner = st.InkInner; TextInk = st.Ink; FlavorInk = st.Ink; RibbonOn = true;
        }
        public static void ResetInk()
        {
            RimOuter = new Color(0.02f, 0.02f, 0.02f, 1f); RimInner = Color.white;
            TextInk = new Color(0.16f, 0.07f, 0.02f, 1f); FlavorInk = new Color(0.18f, 0.08f, 0.03f, 1f); RibbonOn = false;
        }

        // ---- frame geometry (fractions of the card rect; matches CardText.FaceOf) ----
        public static readonly Color FrameWood = new Color(0.40f, 0.23f, 0.09f, 1f);   // shared honey-wood
        public const float JewelA = 0.115f;     // bottom jewel bezel half-width / card width
        public const float JewelFill = 0.78f;   // live fill size / bezel size
        public const float HexR = 0.085f;       // corner comb circumradius / card width
        public const float HexC = 0.060f;       // corner comb centre inset / card width

        // ---- size buckets ----
        static readonly int[] BW = { 192, 320, 512, 768 };
        public static int BucketOf(float cardW) => cardW <= 200f ? 0 : cardW <= 420f ? 1 : cardW <= 720f ? 2 : 3;

        struct Slot { public long Key; public Texture2D Tex; public int Use; }
        const int FaceSlots = 24;
        const int FrameSlots = 10;
        static readonly Slot[] _face = new Slot[FaceSlots];
        static readonly Slot[] _frame = new Slot[FrameSlots];
        static int _serial;
        static int _bakeFrame = -1;
        static int _bakes;
        public static int BakeCount;     // lifetime, for the probe / perf log

        static bool Budget(int bucket)
        {
            int f = Time.frameCount;
            if (f != _bakeFrame) { _bakeFrame = f; _bakes = 0; }
            int cap = bucket >= 2 ? 1 : 2;
            if (_bakes >= cap) return false;
            _bakes++;
            BakeCount++;
            return true;
        }

        static int Q4(float v) => Mathf.Clamp(Mathf.RoundToInt(v * 15f), 0, 15);
        static long TintKey(Color c) => (Q4(c.r) << 8) | (Q4(c.g) << 4) | Q4(c.b);

        static Texture2D Lookup(Slot[] slots, long key)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Key != key || slots[i].Tex == null) continue;
                slots[i].Use = ++_serial;
                return slots[i].Tex;
            }
            return null;
        }

        static void Store(Slot[] slots, long key, Texture2D tex)
        {
            int at = 0, old = int.MaxValue;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Tex == null) { at = i; old = -1; break; }
                if (slots[i].Use < old) { old = slots[i].Use; at = i; }
            }
            if (slots[at].Tex != null) Object.Destroy(slots[at].Tex);
            slots[at] = new Slot { Key = key, Tex = tex, Use = ++_serial };
        }

        public static void Flush()
        {
            for (int i = 0; i < _face.Length; i++) { if (_face[i].Tex != null) Object.Destroy(_face[i].Tex); _face[i] = default; }
            for (int i = 0; i < _frame.Length; i++) { if (_frame[i].Tex != null) Object.Destroy(_frame[i].Tex); _frame[i] = default; }
        }

        // ================= public draw API =================

        // Paints face art + frame for an owned card. Returns false when the bake budget is spent
        // (the caller keeps the flat plate it already drew under this call).
        public static bool DrawBase(Rect card, Rect face, BeeKind kind, Color wood, bool inspectView)
        {
            int b = BucketOf(card.width);
            var theme = FaceThemes.Resolve(kind);
            var st = FaceThemes.Of(theme, kind.Tint);
            var fTex = FaceTex(theme, st, kind.Tint, Mathf.Min(b, 2));
            var frTex = FrameTex(FrameWood, b);   // one frame for every card and finish
            if (fTex == null || frTex == null) return false;
            GUI.color = Color.white;
            GUI.DrawTexture(face, fTex, ScaleMode.StretchToFill, false);
            GUI.DrawTexture(card, frTex, ScaleMode.StretchToFill, true);
            return true;
        }

        public static Texture2D FaceTex(FaceTheme theme, in FaceStyle st, Color tint, int fb)
        {
            long tk = st.Night ? 0L : TintKey(tint);
            long key = ((long)theme << 40) | (tk << 8) | (long)fb;
            var tex = Lookup(_face, key);
            if (tex != null) return tex;
            if (!Budget(fb)) return null;
            int w = Mathf.RoundToInt(BW[fb] * 0.91f), h = Mathf.RoundToInt(BW[fb] * 1.4f * 0.93f);
            var px = new Color32[w * h];
            PaintFace(px, w, h, st, (int)theme * 131 + 7);
            tex = Make(px, w, h, "CardFaceArt");
            Store(_face, key, tex);
            return tex;
        }

        public static Texture2D FrameTex(Color wood, int b)
        {
            long key = (TintKey(wood) << 4) | (long)b;
            var tex = Lookup(_frame, key);
            if (tex != null) return tex;
            if (!Budget(b)) return null;
            int w = BW[b], h = Mathf.RoundToInt(BW[b] * 1.4f);
            var px = new Color32[w * h];
            PaintFrame(px, w, h, wood);
            tex = Make(px, w, h, "CardFrameArt");
            Store(_frame, key, tex);
            return tex;
        }

        static Texture2D Make(Color32[] px, int w, int h, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = name
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // ================= face painter =================

        static float Sm(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        static Color Grad(in FaceStyle s, float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.38f) return Color.Lerp(s.S0, s.S1, t / 0.38f);
            if (t < 0.72f) return Color.Lerp(s.S1, s.S2, (t - 0.38f) / 0.34f);
            return Color.Lerp(s.S2, s.S3, (t - 0.72f) / 0.28f);
        }

        static Color RadGrad(in FaceStyle s, float r)
        {
            float t = Mathf.Clamp01(r / 0.70f);
            if (t < 0.22f) return Color.Lerp(s.S0, s.S1, t / 0.22f);
            if (t < 0.50f) return Color.Lerp(s.S1, s.S2, (t - 0.22f) / 0.28f);
            return Color.Lerp(s.S2, s.S3, (t - 0.50f) / 0.50f);
        }

        static Color Hue(float h)
        {
            h = h - Mathf.Floor(h);
            float r = Mathf.Clamp01(Mathf.Abs((h * 6f) % 6f - 3f) - 1f);
            float g = Mathf.Clamp01(Mathf.Abs((h * 6f + 4f) % 6f - 3f) - 1f);
            float b = Mathf.Clamp01(Mathf.Abs((h * 6f + 2f) % 6f - 3f) - 1f);
            return new Color(0.5f + 0.5f * r, 0.5f + 0.5f * g, 0.5f + 0.5f * b, 1f);
        }

        static void PaintFace(Color32[] px, int w, int h, in FaceStyle st, int seed)
        {
            float asp = (float)w / h;
            var gl = st.Glow;
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                int row = (h - 1 - y) * w;
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1);
                    Color c;
                    switch (st.Motif)
                    {
                        case FaceMotif.Rays:
                        {
                            float dx = (u - 0.5f) * asp, dy = v - 0.30f;
                            float r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                            c = Grad(st, v * 0.9f + r * 0.18f);
                            float n = Mathf.Max(2, st.Rays);
                            float ray = Mathf.Pow(Mathf.Cos(a * n) * 0.5f + 0.5f, 2.2f);
                            float ray2 = Mathf.Pow(Mathf.Cos(a * n * 0.5f + 0.7f) * 0.5f + 0.5f, 3f);
                            float fall = Mathf.Exp(-r * 2.1f) * 0.85f + 0.15f;
                            float rr = (ray * 0.34f + ray2 * 0.18f) * fall;
                            float halo = Mathf.Exp(-(r / 0.20f) * (r / 0.20f)) * 0.9f;
                            float ring = Mathf.Exp(-((r - 0.24f) / 0.006f) * ((r - 0.24f) / 0.006f)) * 0.5f;
                            c.r += rr + (halo + ring) * 1f; c.g += rr * 0.78f + (halo + ring) * 0.92f; c.b += rr * 0.30f + (halo + ring) * 0.55f;
                            break;
                        }
                        case FaceMotif.Comet:
                        {
                            c = Grad(st, v * 0.85f + (1f - u) * 0.15f);
                            float px0 = (u - 0.80f) * asp, py0 = v - 0.12f;
                            const float dxn = -0.5469f, dyn = 0.8372f;   // normalized (-0.55, 0.84)
                            float s = px0 * dxn + py0 * dyn, p = -px0 * dyn + py0 * dxn;
                            float sp = Mathf.Max(s, 0f);
                            float wd = 0.012f + 0.075f * sp / 0.95f;
                            float tail = s > 0f ? Mathf.Exp(-sp / 0.5225f) * Mathf.Exp(-(p / wd) * (p / wd)) : 0f;
                            float core = s > 0f ? Mathf.Exp(-sp / 0.665f) * Mathf.Exp(-(p / (wd * 0.28f)) * (p / (wd * 0.28f))) : 0f;
                            float rr = Mathf.Sqrt(px0 * px0 + py0 * py0);
                            float head = Mathf.Exp(-(rr / 0.045f) * (rr / 0.045f)) * 1.2f + Mathf.Exp(-(rr / 0.16f) * (rr / 0.16f)) * 0.5f;
                            float streak = Mathf.Exp(-(py0 / 0.006f) * (py0 / 0.006f)) * Mathf.Exp(-Mathf.Abs(px0) / 0.5f) * 0.55f;
                            c.r += tail * 0.62f + core * 0.8f + head + streak * 0.6f;
                            c.g += tail * 0.38f + core * 0.76f + head * 0.97f + streak * 0.75f;
                            c.b += tail * 0.16f + core * 0.64f + head * 0.85f + streak;
                            // ghosts along head -> face centre
                            for (int k = 0; k < 5; k++)
                            {
                                float t = k == 0 ? 0.35f : k == 1 ? 0.62f : k == 2 ? 0.85f : k == 3 ? 1.2f : 1.55f;
                                float rad = k == 0 ? 0.05f : k == 1 ? 0.09f : k == 2 ? 0.035f : k == 3 ? 0.12f : 0.06f;
                                float hue = k == 0 ? 0.58f : k == 1 ? 0.08f : k == 2 ? 0.85f : k == 3 ? 0.45f : 0.12f;
                                float gx = 0.80f + (0.5f - 0.80f) * t, gy = 0.12f + (0.5f - 0.12f) * t;
                                float rk = Mathf.Sqrt(((u - gx) * asp) * ((u - gx) * asp) + (v - gy) * (v - gy));
                                float e = (rk - rad) / (rad * 0.18f);
                                float g2 = Mathf.Exp(-e * e) * 0.35f + (rk < rad ? 0.10f : 0f);
                                if (g2 < 0.004f) continue;
                                var hc = Color.HSVToRGB(hue, 0.6f, 1f);
                                c.r += hc.r * g2; c.g += hc.g * g2; c.b += hc.b * g2;
                            }
                            break;
                        }
                        case FaceMotif.Ring:
                        {
                            float dx = (u - 0.5f) * asp, dy = v - 0.42f;
                            float r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                            c = RadGrad(st, r);
                            for (int i = 0; i < 4; i++)
                            {
                                float rad = i == 0 ? 0.22f : i == 1 ? 0.36f : i == 2 ? 0.52f : 0.70f;
                                float wd = i == 0 ? 0.012f : i == 1 ? 0.016f : i == 2 ? 0.02f : 0.026f;
                                float amp = i == 0 ? 0.9f : i == 1 ? 0.7f : i == 2 ? 0.5f : 0.35f;
                                float e = (r - rad) / wd;
                                float band = Mathf.Exp(-e * e);
                                if (band < 0.01f) continue;
                                var hc = Hue(a / (2f * Mathf.PI) + 0.12f * i + rad);
                                c = Color.Lerp(c, c * 0.5f, band * 0.5f);
                                c.r += hc.r * band * amp; c.g += hc.g * band * amp; c.b += hc.b * band * amp;
                            }
                            float core = Mathf.Exp(-(r / 0.07f) * (r / 0.07f)) * 0.9f;
                            c.r += core; c.g += core * 0.97f; c.b += core * 0.8f;
                            float spk = Mathf.Pow(Mathf.Cos(a * 28f) * 0.5f + 0.5f, 8f) * Mathf.Exp(-r * 3.2f) * 0.35f;
                            c.r += spk; c.g += spk * 0.8f; c.b += spk * 0.5f;
                            // prismatic foil edge
                            float e2 = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y)) / (float)w;
                            float m = Mathf.Pow(Mathf.Clamp01(1f - e2 / 0.028f), 0.6f);
                            if (m > 0f)
                            {
                                var fc = Hue((u + v) * 0.9f);
                                c = Color.Lerp(c, fc, m * 0.9f);
                            }
                            break;
                        }
                        default:   // Glow: bright family ground
                        {
                            c = Grad(st, v);
                            float dx = (u - 0.5f) * asp, dy = v - 0.30f;
                            float r = Mathf.Sqrt(dx * dx + dy * dy);
                            float g = Mathf.Exp(-(r / 0.30f) * (r / 0.30f)) * 0.55f;
                            float band = Mathf.Exp(-((u * 0.7f + v * 0.5f - 0.55f) / 0.12f) * ((u * 0.7f + v * 0.5f - 0.55f) / 0.12f)) * 0.10f;
                            c.r += gl.r * g + band; c.g += gl.g * g + band; c.b += gl.b * g + band;
                            break;
                        }
                    }
                    float vg = 1f - 0.28f * Mathf.Clamp01(((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2.4f);
                    if (!st.Night) vg = 1f - 0.12f * Mathf.Clamp01(((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2.4f);
                    px[row + x] = new Color32(B(c.r * vg), B(c.g * vg), B(c.b * vg), 255);
                }
            }
            // sparkles (deterministic, additive)
            var rng = new System.Random(seed);
            for (int i = 0; i < st.Sparks; i++)
            {
                float sx = (float)rng.NextDouble() * w, sy = (float)rng.NextDouble() * h;
                float q = (float)rng.NextDouble();
                float size = (q * q * q * 0.032f + 0.004f) * w;
                float al = 0.6f + 0.4f * (float)rng.NextDouble();
                Star(px, w, h, sx, sy, size, st.Night ? new Color(1f, 0.96f, 0.82f) : new Color(1f, 1f, 0.92f), al);
            }
        }

        static byte B(float v) => (byte)(v <= 0f ? 0 : v >= 1f ? 255 : (int)(v * 255f + 0.5f));

        static void Star(Color32[] px, int w, int h, float cx, float cy, float s, Color c, float al)
        {
            int r = Mathf.CeilToInt(s);
            if (r < 1) return;
            for (int dy = -r; dy <= r; dy++)
            {
                int yy = Mathf.RoundToInt(cy) + dy;
                if (yy < 0 || yy >= h) continue;
                for (int dx = -r; dx <= r; dx++)
                {
                    int xx = Mathf.RoundToInt(cx) + dx;
                    if (xx < 0 || xx >= w) continue;
                    float k = Mathf.Sqrt(Mathf.Abs(dx) / s) + Mathf.Sqrt(Mathf.Abs(dy) / s);
                    float v = Mathf.Max(0f, 1f - k);
                    v = v * Mathf.Sqrt(v) * al;
                    float d2 = (dx * dx + dy * dy) / (s * s);
                    v += Mathf.Exp(-d2 * 2.2f) * 0.22f * al;
                    if (v < 0.01f) continue;
                    int i = (h - 1 - yy) * w + xx;
                    var p = px[i];
                    px[i] = new Color32(
                        B(p.r / 255f + c.r * v), B(p.g / 255f + c.g * v), B(p.b / 255f + c.b * v), 255);
                }
            }
        }

        // ================= frame painter =================

        static void PaintFrame(Color32[] px, int w, int h, Color wood)
        {
            float fx0 = w * 0.045f, fx1 = w * 0.955f, fy0 = h * 0.035f, fy1 = h * 0.965f;
            float ring = Mathf.Min(fx0, fy0);
            float brassW = ring * 0.50f;
            float lipW = Mathf.Max(1.5f, w * 0.006f);
            float R = HexR * w, C = HexC * w;
            float sc = w / 192f;
            float jb = (h - 1 - fy1) * 0.78f, jcy = (h - 1) - (h - 1 - fy1) * 0.10f - jb;   // set into the bottom frame edge, rising a little into the face's empty bottom margin (clear of the text panel)
            Color brass = new Color(0.88f, 0.66f, 0.24f);
            for (int y = 0; y < h; y++)
            {
                int row = (h - 1 - y) * w;
                for (int x = 0; x < w; x++)
                {
                    float dl = fx0 - x, dr = x - fx1, dt = fy0 - y, db = y - fy1;
                    float dH = Mathf.Max(dl, dr), dV = Mathf.Max(dt, db);
                    float dFace = Mathf.Max(dH, dV);
                    float cr = 0f, cg = 0f, cb = 0f, ca = 0f;
                    if (dFace <= 0f)
                    {
                        if (dFace > -lipW * 2.2f)
                        {
                            ca = 0.40f * Mathf.Clamp01(1f + dFace / (lipW * 2.2f));
                            cr = 0.12f; cg = 0.06f; cb = 0.02f;
                        }
                    }
                    else
                    {
                        bool horiz = dV >= dH;                       // a top/bottom strip
                        bool lit = horiz ? dt >= db : dl >= dr;      // top or left edge faces the light
                        float along = horiz ? x : y;
                        float across = horiz ? y : x;
                        float ls = lit ? 1f : -1f;
                        if (dFace < brassW)
                        {
                            float t = dFace / brassW;
                            float shade = 1f + 0.46f * ls * (t - 0.5f) * 2f + 0.14f * Mathf.Sin(Mathf.PI * t);
                            if (t < 0.10f) shade *= 0.58f;
                            if (t > 0.90f) shade *= 0.62f;
                            shade = Mathf.Clamp(shade, 0.45f, 1.55f);
                            cr = brass.r * shade; cg = brass.g * shade; cb = brass.b * shade;
                            if (shade > 1.32f) { float sp = (shade - 1.32f) * 1.2f; cr += sp; cg += sp; cb += sp * 0.8f; }
                        }
                        else
                        {
                            float g = Mathf.Sin(along / sc * 0.55f + Mathf.Sin(across / sc * 0.17f) * 2.4f) * 0.5f + 0.5f;
                            float shade = 0.84f + 0.22f * g;
                            cr = wood.r * shade; cg = wood.g * shade; cb = wood.b * shade;
                            float d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                            if (d < 2.4f * sc)
                            {
                                bool hi = x < 2.4f * sc || y < 2.4f * sc;
                                float k = hi ? 1.34f : 0.58f;
                                cr *= k; cg *= k; cb *= k;
                            }
                        }
                        ca = 1f;
                    }
                    // hex-comb corners
                    for (int k = 0; k < 4; k++)
                    {
                        float hx = (k & 1) == 0 ? C : w - 1 - C;
                        float hy = (k & 2) == 0 ? C : h - 1 - C;
                        float qx = Mathf.Abs(x - hx), qy = Mathf.Abs(y - hy);
                        if (qx > R + 4f * sc || qy > R + 4f * sc) continue;
                        float dist = Mathf.Max(qy, qx * 0.8660254f + qy * 0.5f);
                        float inside = R - dist;
                        if (inside < -0.09f * R) continue;
                        if (inside <= 0f)
                        {
                            float a = 0.38f * (1f + inside / (0.09f * R));
                            OverF(ref cr, ref cg, ref cb, ref ca, 0.10f, 0.05f, 0.02f, a);
                            continue;
                        }
                        float cov = Mathf.Clamp01(inside + 0.5f);
                        float px0 = x - hx, py0 = y - hy;
                        float len = Mathf.Sqrt(px0 * px0 + py0 * py0) + 1e-4f;
                        float rimW = R * 0.20f;
                        float sr, sg, sb;
                        if (inside < rimW)
                        {
                            float facing = (px0 * -0.6f + py0 * -0.8f) / len;   // toward the light
                            float tt = inside / rimW;
                            float shade = 1f + 0.5f * facing * (1f - tt) - 0.25f * (1f - tt) * (-facing) * 0 + 0.12f * Mathf.Sin(Mathf.PI * tt);
                            shade = Mathf.Clamp(shade, 0.5f, 1.5f);
                            if (tt < 0.12f) shade *= 0.6f;
                            sr = brass.r * shade; sg = brass.g * shade; sb = brass.b * shade;
                        }
                        else
                        {
                            float gx = (px0 + R * 0.25f) / R, gy = (py0 + R * 0.35f) / R;
                            float t = Mathf.Clamp01(Mathf.Sqrt(gx * gx + gy * gy) * 0.8f);
                            sr = Mathf.Lerp(1.0f, 0.90f, t); sg = Mathf.Lerp(0.85f, 0.55f, t); sb = Mathf.Lerp(0.34f, 0.10f, t);
                            // inner outlines read as comb cells
                            float d1 = Mathf.Abs(dist - R * 0.58f), d2 = Mathf.Abs(dist - R * 0.30f);
                            float line = Mathf.Max(Mathf.Clamp01(1f - d1 / (R * 0.045f)), Mathf.Clamp01(1f - d2 / (R * 0.04f)));
                            sr = Mathf.Lerp(sr, 0.62f, line * 0.55f); sg = Mathf.Lerp(sg, 0.34f, line * 0.55f); sb = Mathf.Lerp(sb, 0.05f, line * 0.55f);
                            float gd = ((px0 + R * 0.34f) * (px0 + R * 0.34f)) / (R * R * 0.05f) + ((py0 + R * 0.42f) * (py0 + R * 0.42f)) / (R * R * 0.02f);
                            float gloss = Mathf.Exp(-gd) * 0.55f;
                            sr += gloss; sg += gloss; sb += gloss * 0.85f;
                        }
                        OverF(ref cr, ref cg, ref cb, ref ca, sr, sg, sb, cov);
                    }
                    // bottom-centre rarity jewel: brass bezel + dark socket (identical on every card; the live fill sits inside)
                    {
                        float jx = (x - (w - 1) * 0.5f) / (JewelA * w);
                        float jy = (y - jcy) / jb;
                        float jd = Mathf.Sqrt(jx * jx + jy * jy);
                        if (jd < 1.08f)
                        {
                            float cov = Mathf.Clamp01((1f - jd) * jb + 0.5f);
                            if (jd >= 1f) { cov = 0f; OverF(ref cr, ref cg, ref cb, ref ca, 0.10f, 0.05f, 0.02f, 0.40f * (1.08f - jd) / 0.08f); }
                            else if (jd > JewelFill)
                            {
                                float t = (jd - JewelFill) / (1f - JewelFill);
                                float lit = -jy;                                   // top of the bezel catches the light
                                float shade = 1f + 0.45f * lit * (1f - Mathf.Abs(t - 0.5f) * 1.2f) + 0.10f * Mathf.Sin(Mathf.PI * t);
                                if (t > 0.88f) shade *= 0.6f;
                                shade = Mathf.Clamp(shade, 0.5f, 1.5f);
                                OverF(ref cr, ref cg, ref cb, ref ca, brass.r * shade, brass.g * shade, brass.b * shade, 1f);
                            }
                            else
                            {
                                float g2 = Mathf.Clamp01(jd / JewelFill);
                                OverF(ref cr, ref cg, ref cb, ref ca, 0.20f - 0.08f * g2, 0.11f - 0.05f * g2, 0.04f, 1f);
                            }
                        }
                    }
                    if (ca <= 0.002f) continue;
                    px[row + x] = new Color32(B(cr), B(cg), B(cb), B(ca));
                }
            }
        }

        static void OverF(ref float r, ref float g, ref float b, ref float a, float sr, float sg, float sb, float sa)
        {
            float oa = sa + a * (1f - sa);
            if (oa < 0.0001f) return;
            r = (sr * sa + r * a * (1f - sa)) / oa;
            g = (sg * sa + g * a * (1f - sa)) / oa;
            b = (sb * sa + b * a * (1f - sa)) / oa;
            a = oa;
        }

        // ================= ribbon =================

        const int RibW = 256, RibH = 64;
        static Color32[] _ribPx;
        static Texture2D _ribTex;

        static void EnsureRibbon()
        {
            if (_ribPx != null) return;
            var px = new Color32[RibW * RibH];
            const float notch = 20f;
            for (int y = 0; y < RibH; y++)
            {
                float v = (y + 0.5f) / RibH;
                for (int x = 0; x < RibW; x++)
                {
                    // swallowtail ends: a V cut from each short edge
                    float edge = Mathf.Min(x, RibW - 1 - x);
                    float cutLine = notch * (1f - Mathf.Abs(v - 0.5f) * 2f);
                    float cov = Mathf.Clamp01(edge - cutLine + 0.5f);
                    if (cov <= 0f) { px[(RibH - 1 - y) * RibW + x] = default; continue; }
                    // bee stripes: slow diagonal bands
                    float s = ((x * 0.70f + y * 0.70f) / 22f) % 2f;
                    bool dark = s < 1f;
                    float soft = Mathf.Abs(((s % 1f) - 0.5f)) * 2f;           // 0 at band middle .. 1 at the seam
                    var a = new Color(0.97f, 0.73f, 0.22f);
                    var b = new Color(0.80f, 0.50f, 0.12f);
                    var c = Color.Lerp(dark ? b : a, dark ? a : b, Mathf.Clamp01((soft - 0.88f) * 8f));
                    // bevel: highlight on top, shade on bottom
                    float shade = 1f + 0.34f * (0.5f - v) * 2f * (v < 0.5f ? 1f : 0.8f);
                    if (v < 0.07f) shade *= 1.18f;
                    if (v > 0.93f) shade *= 0.66f;
                    c = new Color(c.r * shade, c.g * shade, c.b * shade);
                    // brass outline
                    float oe = Mathf.Min(Mathf.Min(v, 1f - v) * RibH, Mathf.Max(edge - cutLine, 0f));
                    if (oe < 1.6f) c = Color.Lerp(c, new Color(0.45f, 0.26f, 0.06f), 0.85f);
                    px[(RibH - 1 - y) * RibW + x] = new Color32(B(c.r), B(c.g), B(c.b), B(cov));
                }
            }
            _ribPx = px;
        }

        public static Texture2D RibbonTex()
        {
            EnsureRibbon();
            if (_ribTex == null) _ribTex = Make(_ribPx, RibW, RibH, "CardRibbon");
            return _ribTex;
        }

        public static void DrawRibbon(Rect r)
        {
            if (r.width < 6f || r.height < 4f) return;
            var t = RibbonTex();
            GUI.color = Color.white;
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true);
        }

        // Ribbon behind a title block: top-aligned to the text, a touch of padding.
        public static Rect RibbonFor(Rect titleBand, float hostWidth, float blockH)
        {
            float padX = hostWidth * 0.055f;
            float h = Mathf.Min(titleBand.height * 1.02f, blockH + titleBand.height * 0.10f);
            return new Rect(titleBand.x - padX, titleBand.y - titleBand.height * 0.02f, titleBand.width + padX * 2f, h);
        }

        // Same ribbon into a bake page (Color32, origin top-left in GUI space).
        public static void BlitRibbon(Color32[] dst, int w, int h, Rect r)
        {
            EnsureRibbon();
            int x0 = Mathf.Max(0, Mathf.FloorToInt(r.x)), x1 = Mathf.Min(w, Mathf.CeilToInt(r.xMax));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(r.y)), y1 = Mathf.Min(h, Mathf.CeilToInt(r.yMax));
            for (int y = y0; y < y1; y++)
            {
                int sy = Mathf.Clamp((int)((y + 0.5f - r.y) / r.height * RibH), 0, RibH - 1);
                for (int x = x0; x < x1; x++)
                {
                    int sx = Mathf.Clamp((int)((x + 0.5f - r.x) / r.width * RibW), 0, RibW - 1);
                    var s = _ribPx[(RibH - 1 - sy) * RibW + sx];
                    if (s.a == 0) continue;
                    int i = (h - 1 - y) * w + x;
                    var d = dst[i];
                    float sa = s.a / 255f, da = d.a / 255f, oa = sa + da * (1f - sa);
                    dst[i] = new Color32(
                        B((s.r * sa + d.r * da * (1f - sa)) / (255f * oa)),
                        B((s.g * sa + d.g * da * (1f - sa)) / (255f * oa)),
                        B((s.b * sa + d.b * da * (1f - sa)) / (255f * oa)),
                        B(oa));
                }
            }
        }

        // ================= glow / star / scrim =================

        static Texture2D _glow, _starTex, _scrim;

        public static Texture2D GlowTex()
        {
            if (_glow != null) return _glow;
            const int n = 64;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Exp(-(dx * dx + dy * dy) * 3.2f);
                    px[y * n + x] = new Color32(255, 255, 255, B(a));
                }
            _glow = Make(px, n, n, "CardGlow");
            return _glow;
        }

        public static Texture2D StarTex()
        {
            if (_starTex != null) return _starTex;
            const int n = 48;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / n * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                    float k = Mathf.Sqrt(dx) + Mathf.Sqrt(dy);
                    float v = Mathf.Max(0f, 1f - k);
                    v = v * Mathf.Sqrt(v) + Mathf.Exp(-(dx * dx + dy * dy) * 9f) * 0.35f;
                    px[y * n + x] = new Color32(255, 255, 255, B(v));
                }
            _starTex = Make(px, n, n, "CardStar");
            return _starTex;
        }

        public static Texture2D ScrimTex()
        {
            if (_scrim != null) return _scrim;
            const int n = 48;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / n * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                    float e = Mathf.Max(dx, dy);
                    float a = 1f - Sm(0.55f, 1f, e);
                    px[y * n + x] = new Color32(255, 255, 255, B(a));
                }
            _scrim = Make(px, n, n, "CardScrim");
            return _scrim;
        }

        public static void DrawScrim(Rect r, float a)
        {
            GUI.color = new Color(0.03f, 0.01f, 0.08f, a);
            GUI.DrawTexture(r, ScrimTex(), ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        public static void DrawGlow(Rect r, Color c, float a)
        {
            c.a = a;
            GUI.color = c;
            GUI.DrawTexture(r, GlowTex(), ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        // ================= rarity jewel =================

        // The jewel lives in the bottom frame edge (the frame itself is identical for every card; only this fill varies).
        // Geometry mirrors PaintFrame: centred on x, centred in the bottom ring, inside the ring so it never touches the face or text.
        public static Rect JewelRect(Rect card, float fill = JewelFill)
        {
            float h = card.height;
            float ringTop = card.y + h * 0.965f, ringBot = card.yMax;
            float ring = ringBot - ringTop, jb = ring * 0.78f;
            float cy = ringBot - ring * 0.10f - jb;
            float hw = card.width * JewelA * fill, hh = jb * fill;
            return new Rect(card.center.x - hw, cy - hh, hw * 2f, hh * 2f);
        }

        static Texture2D _ovalTex;
        static Texture2D OvalTex()
        {
            if (_ovalTex != null) return _ovalTex;
            const int W = 64, H = 32;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = ((x + 0.5f) / W) * 2f - 1f, dy = ((y + 0.5f) / H) * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((1f - d) * 14f);
                    // domed shading: lighter top-left, darker bottom rim
                    float v = Mathf.Clamp01(0.86f + 0.20f * (-dy) - 0.14f * d * d);
                    px[y * W + x] = new Color32(B(v), B(v), B(v), B(a));
                }
            _ovalTex = Make(px, W, H, "CardOval");
            return _ovalTex;
        }

        // Normal: amber. Holo: shifting prismatic. Inverse Rainbow: cycling rainbow. All get a specular glint.
        // `t` is seconds (0 on the static grid).
        public static void DrawJewel(Rect card, BeeFinish finish, float t, int slot)
        {
            var r = JewelRect(card);
            if (r.width < 4f) return;
            var oval = OvalTex();
            Color baseC;
            if (finish == BeeFinish.Normal) baseC = new Color(1f, 0.70f, 0.16f);
            else if (finish == BeeFinish.Holo) baseC = Hue(0.52f + 0.16f * Mathf.Sin(t * 1.1f + slot));
            else baseC = Hue(t * 0.30f + slot * 0.07f);
            if (finish != BeeFinish.Normal)
                DrawGlow(new Rect(r.x - r.width * 0.30f, r.y - r.height * 0.9f, r.width * 1.6f, r.height * 2.8f), baseC, 0.45f);
            GUI.color = baseC;
            GUI.DrawTexture(r, oval, ScaleMode.StretchToFill, true);
            if (finish != BeeFinish.Normal)
            {
                // second hue band slides across for the prismatic read
                var c2 = finish == BeeFinish.Holo ? Hue(0.80f - 0.16f * Mathf.Sin(t * 1.1f + slot)) : Hue(t * 0.30f + 0.5f + slot * 0.07f);
                c2.a = 0.62f;
                float k = 0.5f + 0.5f * Mathf.Sin(t * 1.6f + slot);
                var half = new Rect(r.x + r.width * (0.10f + 0.40f * k), r.y + r.height * 0.10f, r.width * 0.50f, r.height * 0.80f);
                GUI.color = c2;
                GUI.DrawTexture(half, oval, ScaleMode.StretchToFill, true);
            }
            // specular glint
            float gk = Mathf.Sin(t * 2.2f + slot * 0.9f);
            float gs = r.height * 1.3f * (0.85f + 0.15f * Mathf.Sin(t * 5f));
            float gx = r.center.x - r.width * 0.18f + r.width * 0.16f * gk, gy = r.y + r.height * 0.30f;
            GUI.color = new Color(1f, 1f, 0.96f, 0.95f);
            GUI.DrawTexture(new Rect(gx - gs * 0.5f, gy - gs * 0.5f, gs, gs), StarTex(), ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }
    }
}

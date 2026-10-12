using UnityEngine;

namespace FlockFive
{
    // Gold sparkles fountain out of LEVEL, hang at the apex, then fall as rain.
    // The garden shows through every gap. No frame is a solid or near-solid fill.
    // The clock starts when LEVEL is pressed. Load begins on that same press.
    // Until the load is ready the clock holds at the apex, so a slow load waits
    // on the fountain instead of adding time after the reveal.
    public static class LevelCurtain
    {
        public const float Duration = 1.0f;
        public const float Flood = 0.40f;
        public const int Count = 560; // polish: a real flood (was 240, read as sparse dots)

        public static bool Armed { get; private set; }
        public static bool LoadStarted { get; private set; }
        public static bool Ready { get; private set; }
        public static float StartedAt { get; private set; }

        // Release bloom. Fire stamps the clock on the committed press, the same frame
        // as Load; nothing waits on it. Negative until then, and the bloom rides
        // StartedAt instead (shot previews arm and preview without a fire).
        public static float FiredAt { get; private set; } = -1f;
        public const float BloomLife = 0.85f;

        // Button the fountain leaves. Empty until Aim.
        public static Rect Source;

        // Shot clock. Negative uses the time the caller passes.
        public static float PreviewNow = -1f;

        public struct Glint
        {
            public float X, Y, Size, Alpha;
            public byte Hot;
            public byte Tint; // 0 gold, 1 peach, 2 cream (paint only)
        }

        static Glint[] _buf;
        static Texture2D _dot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();

        public static void Reset()
        {
            Armed = false;
            LoadStarted = false;
            Ready = false;
            StartedAt = 0f;
            FiredAt = -1f;
            Source = default;
            PreviewNow = -1f;
        }

        public static void Clear() => Reset();

        // Pointer-down. Starts the fountain clock. Does not mark the load.
        public static void Arm(float now)
        {
            if (Armed) return;
            Armed = true;
            StartedAt = now;
        }

        // Where the fountain comes out of. The LEVEL flower passes its rest rect.
        public static void Aim(Rect button)
        {
            if (button.width > 2f && button.height > 2f) Source = button;
        }

        // Committed press, immediately before Load. The test reads this flag.
        public static void BeginLoad()
        {
            LoadStarted = true;
        }

        public static void MarkReady() => Ready = true;

        // Committed press, right after Load. Paint clock only. A long hold parked the
        // fountain at the apex; re-seat the clock there so the fall and reveal still
        // play instead of snapping off on release.
        public static void Fire(float now)
        {
            if (!Armed) return;
            FiredAt = now;
            if (now - StartedAt > Flood) StartedAt = now - Flood;
        }

        // Seconds since the release (or since Arm in a preview). Negative when idle.
        public static float BloomAge(float now)
        {
            if (!Armed) return -1f;
            float from = FiredAt >= 0f && PreviewNow < 0f ? FiredAt : StartedAt;
            float a = Clock(now) - from;
            return a < 0f ? 0f : a;
        }

        public static float EaseOutCubic(float u)
        {
            u = Mathf.Clamp01(u);
            float v = 1f - u;
            return 1f - v * v * v;
        }

        public static float EaseOutQuint(float u)
        {
            u = Mathf.Clamp01(u);
            float v = 1f - u;
            return 1f - v * v * v * v * v;
        }

        public static float EaseOutBack(float u)
        {
            u = Mathf.Clamp01(u);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float v = u - 1f;
            return 1f + c3 * v * v * v + c1 * v * v;
        }

        // Light envelope: 30 ms attack, then an exponential tail (tau 75 ms).
        public static float FlashEnv(float b)
        {
            if (b < 0f) return 0f;
            if (b < 0.03f) return b / 0.03f;
            return Mathf.Exp(-(b - 0.03f) / 0.075f);
        }

        public static float Clock(float now) => PreviewNow >= 0f ? PreviewNow : now;

        public static float VisualAge(float now)
        {
            if (!Armed) return 0f;
            float age = Clock(now) - StartedAt;
            if (age < 0f) age = 0f;
            if (!Ready && age > Flood) return Flood;
            if (age > Duration) return Duration;
            return age;
        }

        public static bool Finished(float now)
        {
            if (!Armed || !Ready) return false;
            // A fired bloom finishes its tail over the garden before the clear.
            if (FiredAt >= 0f && PreviewNow < 0f && now - FiredAt < BloomLife) return false;
            return Clock(now) - StartedAt >= Duration;
        }

        // burst, apex, fall, reveal. Apex sits on Flood so a held load freezes there.
        public static string BeatName(float age)
        {
            if (age < 0.22f) return "burst";
            if (age < 0.46f) return "apex";
            if (age < 0.74f) return "fall";
            return "reveal";
        }

        public static void Draw(float now)
        {
            if (!Armed) return;
            float w = Screen.width;
            float h = Screen.height;
            if (w < 2f || h < 2f) return;
            if (_buf == null || _buf.Length < Count * 2) _buf = new Glint[Count * 2];
            int n = Fill(VisualAge(now), w, h, Source, _buf);
            float bloom = BloomAge(now);
            var spark = SpriteCatalog.Sparkle;
            Texture tex = spark != null ? spark.texture : null;
            if (tex == null) tex = Dot();
            var glow = SpriteCatalog.Glow;
            Texture glowTex = glow != null && glow.texture != null ? glow.texture : tex;
            var prev = GUI.color;
            BloomSeat(w, h, out Vector2 at, out float side);
            DrawBloomUnder(bloom, at, side, glowTex);
            for (int i = 0; i < n; i++)
            {
                float a = _buf[i].Alpha;
                if (a < 0.03f) continue;
                float sz = _buf[i].Size;
                if (sz < 1.5f) continue;
                float x = _buf[i].X - sz * 0.5f;
                float y = _buf[i].Y - sz * 0.5f;
                if (_buf[i].Hot == 0)
                {
                    float g = sz * 2.1f;
                    GUI.color = new Color(1f, 0.82f, 0.28f, a * 0.34f);
                    GUI.DrawTexture(new Rect(_buf[i].X - g * 0.5f, _buf[i].Y - g * 0.5f, g, g), glowTex, ScaleMode.ScaleToFit, true);
                }
                byte tn = _buf[i].Tint;
                GUI.color = _buf[i].Hot == 1
                    ? new Color(1f, 0.98f, 0.90f, a)
                    : tn == 1 ? new Color(1f, 0.74f, 0.46f, a)
                    : tn == 2 ? new Color(1f, 0.93f, 0.72f, a)
                    : new Color(1f, 0.84f, 0.28f, a);
                GUI.DrawTexture(new Rect(x, y, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            DrawBloomOver(bloom, at, side, tex, glowTex);
            DrawEmbers(bloom, at, side, tex, glowTex);
            GUI.color = prev;
        }

        static void BloomSeat(float w, float h, out Vector2 at, out float side)
        {
            if (Source.width > 2f && Source.height > 2f)
            {
                at = new Vector2(Source.center.x, Source.y + Source.height * 0.40f);
                side = Source.width;
                return;
            }
            at = new Vector2(w * 0.5f, h * 0.74f);
            side = (w < h ? w : h) * 0.34f;
        }

        static void Blit(Vector2 at, float size, Texture tex, Color c)
        {
            if (c.a < 0.02f || size < 1f || tex == null) return;
            GUI.color = c;
            GUI.DrawTexture(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), tex, ScaleMode.ScaleToFit, true);
        }

        // Timing, b = seconds since release (Fire):
        //   0-30 ms    light swells out of the flower (attack), then a 75 ms tail
        //   0-0.42 s   hot inner ring;  0-0.62 s gold ring;  0.06-0.78 s peach/rose ring
        //   0-0.50 s   eight glint rays;  0-0.43 s centre star (ease-out-back pop)
        //   all rings ease-out-quint, so the spread is front-loaded and the tail drifts
        // Painterly palette: warm gold, cream, peach and a touch of rose. No pure white
        // except the hot cores, so it reads as gilded paint, not a lens flare.
        static readonly Color BGold = new Color(1f, 0.84f, 0.36f, 1f);
        static readonly Color BCream = new Color(1f, 0.96f, 0.84f, 1f);
        static readonly Color BPeach = new Color(1f, 0.74f, 0.52f, 1f);
        static readonly Color BRose = new Color(1f, 0.66f, 0.62f, 1f);
        static readonly Color BHot = new Color(1f, 0.99f, 0.94f, 1f);

        static void DrawBloomUnder(float b, Vector2 at, float side, Texture glowTex)
        {
            if (b < 0f || b > BloomLife) return;
            float f = FlashEnv(b);
            if (f > 0.02f)
            {
                // Two-tone light: wide amber wash under a tight cream core.
                float grow = EaseOutCubic(b / 0.25f);
                Blit(at, side * (1.6f + 2.2f * grow), glowTex, new Color(1f, 0.78f, 0.40f, 0.55f * f));
                Blit(at, side * (1.0f + 1.0f * grow), glowTex, new Color(1f, 0.95f, 0.80f, 0.80f * f));
            }
            // Wide, faint afterglow behind the rings (alpha <= 0.22, gone by 0.7 s).
            float u = b / 0.70f;
            if (u < 1f)
            {
                float r = side * (0.9f + 3.0f * EaseOutQuint(u));
                float a = 0.22f * (1f - u) * (1f - u);
                Blit(at, r * 2f, glowTex, new Color(1f, 0.86f, 0.55f, a));
            }
        }

        static void DrawBloomOver(float b, Vector2 at, float side, Texture tex, Texture glowTex)
        {
            if (b < 0f || b > BloomLife) return;
            // Three rings, staggered so the spread reads as depth: hot inner, gold main,
            // peach/rose outer lagging behind and travelling furthest.
            float u1 = b / 0.42f;
            if (u1 < 1f)
            {
                float r = side * (0.30f + 1.05f * EaseOutQuint(u1));
                SparkleFx.DrawRing(at, r, r * 0.88f, 12, b * 1.6f, side * 0.075f * (1f - 0.4f * u1),
                    Mathf.Pow(1f - u1, 1.3f), BHot, BCream, 0.25f);
            }
            float u2 = b / 0.62f;
            if (u2 < 1f)
            {
                float r = side * (0.45f + 1.9f * EaseOutQuint(u2));
                SparkleFx.DrawRing(at, r, r * 0.86f, 28, 0.11f + b * 0.8f, side * 0.115f * (1f - 0.45f * u2),
                    Mathf.Pow(1f - u2, 1.5f), BGold, BCream, 0.35f);
            }
            float u3 = (b - 0.06f) / 0.72f;
            if (u3 > 0f && u3 < 1f)
            {
                float r = side * (0.55f + 2.7f * EaseOutQuint(u3));
                SparkleFx.DrawRing(at, r, r * 0.84f, 20, 0.27f - b * 0.6f, side * 0.10f * (1f - 0.4f * u3),
                    0.85f * Mathf.Pow(1f - u3, 1.7f), BPeach, BRose, 0.45f);
            }

            // Eight rays of glint trains shooting out between the rings (spread + speed).
            float ur = b / 0.50f;
            if (ur < 1f)
            {
                float reach = side * (0.4f + 2.3f * EaseOutCubic(ur));
                float ra = Mathf.Pow(1f - ur, 1.4f);
                for (int k = 0; k < 8; k++)
                {
                    float ang = k * 0.7854f + 0.39f;
                    float cx = Mathf.Cos(ang), cy = Mathf.Sin(ang) * 0.88f;
                    for (int j = 0; j < 3; j++)
                    {
                        float d = reach * (1f - j * 0.16f);
                        var p = new Vector2(at.x + cx * d, at.y + cy * d);
                        float sz = side * (0.13f - j * 0.035f) * (1f - 0.5f * ur);
                        Blit(p, sz, tex, new Color(1f, j == 0 ? 0.97f : 0.86f, j == 0 ? 0.88f : 0.50f, ra * (1f - j * 0.28f)));
                    }
                }
            }

            // Centre star: glow halo, gold star, then a smaller cream star on top.
            float pop = EaseOutBack(b / 0.22f);
            float fade = 1f - Mathf.Clamp01((b - 0.15f) / 0.28f);
            fade = fade * fade * (3f - 2f * fade);
            if (fade > 0.01f)
            {
                Blit(at, side * 1.5f * pop, glowTex, new Color(1f, 0.88f, 0.52f, 0.55f * fade));
                Blit(at, side * 1.05f * pop, tex, new Color(BGold.r, BGold.g, BGold.b, 0.9f * fade));
                Blit(at, side * 0.62f * pop, tex, new Color(BHot.r, BHot.g, BHot.b, fade));
            }
        }

        // Individual glints in GUI pixels (y grows down). Nothing here is a screen fill.
        public static int Fill(float age, float screenW, float screenH, Rect sourcePx, Glint[] dst)
        {
            if (dst == null || dst.Length < 1 || screenW < 2f || screenH < 2f) return 0;
            float ox = 0.50f;
            float oy = 0.74f;
            if (sourcePx.width > 2f && sourcePx.height > 2f)
            {
                ox = sourcePx.center.x / screenW;
                oy = sourcePx.center.y / screenH;
            }
            if (ox < 0.04f) ox = 0.04f;
            else if (ox > 0.96f) ox = 0.96f;
            if (oy < 0.08f) oy = 0.08f;
            else if (oy > 0.94f) oy = 0.94f;
            float minSide = screenW < screenH ? screenW : screenH;
            int n = 0;
            int cap = dst.Length;
            for (int i = 0; i < Count && n < cap; i++)
            {
                if (!Place(i, age, ox, oy, out float nx, out float ny, out float alpha, out float sizeN, out bool hot))
                    continue;
                var g = new Glint
                {
                    X = nx * screenW,
                    Y = ny * screenH,
                    Size = sizeN * minSide,
                    Alpha = alpha,
                    Hot = hot ? (byte)1 : (byte)0,
                    Tint = TintOf(i)
                };
                dst[n++] = g;
            }
            return n;
        }

        // Normalized GUI space. y = 0 is the top. Up is negative vy.
        static bool Place(int i, float age, float ox, float oy,
            out float nx, out float ny, out float alpha, out float sizeN, out bool hot)
        {
            nx = ny = alpha = sizeN = 0f;
            hot = false;
            float h1 = Hash(i, 1);
            float h2 = Hash(i, 2);
            float h3 = Hash(i, 3);
            float h4 = Hash(i, 4);
            float birth = h1 * 0.16f;
            float t = age - birth;
            if (t < 0f) return false;
            float g = 11.4f * (0.78f + 0.44f * h2);
            float vy0 = -0.34f * g * (0.88f + 0.22f * h3);
            float y0 = oy + (h4 - 0.5f) * 0.045f;
            ny = y0 + vy0 * t + 0.5f * g * t * t;
            if (ny > 1.12f) return false;
            float rise = 0.28f + 0.12f * h3;
            float u = t / rise;
            if (u < 0f) u = 0f;
            else if (u > 1f) u = 1f;
            float ease = u * u * (3f - 2f * u);
            float x0 = ox + (h2 - 0.5f) * 0.08f;
            float xTarget = 0.04f + h1 * 0.92f;
            nx = x0 + (xTarget - x0) * ease;
            nx += Mathf.Sin(t * 8.5f + h2 * 6.283f) * (0.012f + 0.012f * h4);
            hot = h2 > 0.74f;
            sizeN = (hot ? 0.016f : 0.026f) + (hot ? 0.022f : 0.052f) * h4;
            float tw = 0.45f + 0.55f * Mathf.Sin(t * 16f + h4 * 11f);
            if (tw < 0f) tw = 0f;
            tw = 0.55f + 0.45f * tw * tw;
            float fadeIn = t / 0.045f;
            if (fadeIn > 1f) fadeIn = 1f;
            else if (fadeIn < 0f) fadeIn = 0f;
            float fadeOut = (1.08f - ny) / 0.16f;
            if (fadeOut > 1f) fadeOut = 1f;
            else if (fadeOut < 0f) fadeOut = 0f;
            alpha = tw * fadeIn * fadeOut;
            if (hot) alpha = alpha > 0.92f ? 0.92f : alpha * 1.15f;
            if (alpha < 0.04f) return false;
            return true;
        }

        // Mostly gold, a quarter peach, the rest cream. Stable per glint, so no flicker.
        static byte TintOf(int i)
        {
            float h = Hash(i, 5);
            return h < 0.60f ? (byte)0 : h < 0.85f ? (byte)1 : (byte)2;
        }

        // Handoff into the stage: embers thrown out with the burst slow on drag, drift
        // and settle over the new garden as the fountain rains, fading by BloomLife.
        // They are what carries the press across the cut.
        const int EmberCount = 40;

        static void DrawEmbers(float b, Vector2 at, float side, Texture tex, Texture glowTex)
        {
            if (b < 0f || b > BloomLife) return;
            const float drag = 3.5f;
            for (int i = 0; i < EmberCount; i++)
            {
                float h1 = Hash(i, 21), h2 = Hash(i, 22), h3 = Hash(i, 23), h4 = Hash(i, 24);
                float t = b - (0.03f + h1 * 0.10f);
                if (t <= 0f) continue;
                float life = BloomLife - 0.10f - h1 * 0.10f;
                float u = t / life;
                if (u >= 1f) continue;
                // Bias the throw upward so the embers arc over the flower into the scene.
                float ang = -1.5708f + (h2 - 0.5f) * 4.2f;
                float sp = side * (2.4f + 2.6f * h3);
                float travel = (1f - Mathf.Exp(-drag * t)) / drag;
                float x = at.x + Mathf.Cos(ang) * sp * travel + Mathf.Sin(t * 5f + h4 * 6.283f) * side * 0.05f;
                float y = at.y + Mathf.Sin(ang) * sp * travel + side * 0.9f * t * t;
                float tw = 0.6f + 0.4f * Mathf.Sin(t * 14f + h4 * 9f);
                float fin = t < 0.04f ? t / 0.04f : 1f;
                float a = fin * tw * Mathf.Pow(1f - u, 1.2f);
                if (a < 0.03f) continue;
                float sz = side * (0.045f + 0.055f * h4) * (1f - 0.35f * u);
                var c = h2 < 0.55f ? BGold : h2 < 0.80f ? BPeach : BCream;
                Blit(new Vector2(x, y), sz * 2.3f, glowTex, new Color(c.r, c.g, c.b, a * 0.28f));
                Blit(new Vector2(x, y), sz, tex, new Color(c.r, c.g, c.b, a));
            }
        }

        static float Hash(int i, int salt)
        {
            uint x = (uint)(i * 374761393 + salt * 668265263 + 17);
            x = (x ^ (x >> 13)) * 1274126177u;
            x ^= x >> 16;
            return (x & 65535) / 65535f;
        }

        static Texture2D Dot()
        {
            if (_dot != null) return _dot;
            const int n = 24;
            _dot = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "CurtainGlint"
            };
            var px = new Color32[n * n];
            float m = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - m) / m;
                float dy = (y - m) / m;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a;
                // Four-point glint so it reads as a sparkle, not a disc of paint.
                float cross = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                float star = Mathf.Clamp01(1f - cross * 1.35f);
                a = Mathf.Max(a * 0.85f, star * star);
                px[y * n + x] = new Color32(255, 236, 170, (byte)Mathf.RoundToInt(a * 255f));
            }
            _dot.SetPixels32(px);
            _dot.Apply(false, true);
            return _dot;
        }
    }
}

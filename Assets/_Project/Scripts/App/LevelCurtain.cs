using UnityEngine;

namespace FlockFive
{
    // Gold sparkles burst out of LEVEL toward the player: they fan out radially, lift,
    // grow as they near, then sink a touch and fade.
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
        //   0-0.79 s   forward shell: 48 glints fan out radially, lift, grow ~3x, fade late
        //   0-0.43 s   centre star (ease-out-back pop)
        //   0.03-0.85  embers: slower forward spokes that carry over the cut
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

        const int ShellCount = 48;

        static void DrawBloomOver(float b, Vector2 at, float side, Texture tex, Texture glowTex)
        {
            if (b < 0f || b > BloomLife) return;
            // Forward shell: glints thrown radially from the flower toward the player,
            // each on its own spoke (golden angle), lifting, growing ~3x as it nears.
            for (int i = 0; i < ShellCount; i++)
            {
                float h1 = Hash(i, 31), h2 = Hash(i, 32), h3 = Hash(i, 33), h4 = Hash(i, 34);
                float ang = i * 2.39996f;
                if (!SparkleFx.TowardAt(b - h1 * 0.04f, 0.45f + 0.30f * h2, at.x, at.y,
                        Mathf.Cos(ang), Mathf.Sin(ang) * 0.85f,
                        side * (1.1f + 1.5f * h3), side * 0.55f, side * 0.4f, 3.0f, out var p))
                    continue;
                var c = h4 < 0.5f ? BGold : h4 < 0.8f ? BCream : BPeach;
                SparkleFx.DrawToward(p, side * (0.05f + 0.04f * h4), c, 1f);
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
                if (!Place(i, age, ox, oy, minSide / screenW, minSide / screenH, out float nx, out float ny, out float alpha, out float sizeN, out bool hot))
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

        // Normalized GUI space, y = 0 at the top. Each glint is a forward particle
        // (SparkleFx.TowardAt) on its own spoke out of the button: it lifts, fans out and
        // grows toward the player, sinks a touch, and fades late. Spoke maths runs in
        // min-side units (sx, sy map them to normalized x / y) so the fan stays round.
        public const float ScaleTo = 3.0f;

        static bool Place(int i, float age, float ox, float oy, float sx, float sy,
            out float nx, out float ny, out float alpha, out float sizeN, out bool hot)
        {
            nx = ny = alpha = sizeN = 0f;
            hot = false;
            float h1 = Hash(i, 1);
            float h2 = Hash(i, 2);
            float h3 = Hash(i, 3);
            float h4 = Hash(i, 4);
            float h6 = Hash(i, 6);
            float t = age - h1 * 0.16f;
            if (t < 0f) return false;
            float ang = h6 * 6.2832f;
            if (!SparkleFx.TowardAt(t, 0.70f + 0.25f * h3, 0f, 0f, Mathf.Cos(ang), Mathf.Sin(ang) * 0.8f,
                    0.30f + 0.55f * h4, 0.85f + 0.25f * h3, 1.0f, ScaleTo, out var p))
                return false;
            nx = ox + p.X * sx;
            ny = oy + p.Y * sy;
            if (ny > 1.12f) return false;
            hot = h2 > 0.74f;
            sizeN = ((hot ? 0.012f : 0.016f) + (hot ? 0.010f : 0.022f) * h4) * p.Scale;
            float tw = 0.45f + 0.55f * Mathf.Sin(t * 16f + h4 * 11f);
            if (tw < 0f) tw = 0f;
            tw = 0.55f + 0.45f * tw * tw;
            alpha = tw * p.Alpha;
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

        // Handoff into the stage: embers on slower forward spokes. They fan out from the
        // flower, lift and grow as they come at the player, and fade by BloomLife over
        // the new garden. What carries the press across the cut. No sideways drift.
        const int EmberCount = 40;

        static void DrawEmbers(float b, Vector2 at, float side, Texture tex, Texture glowTex)
        {
            if (b < 0f || b > BloomLife) return;
            for (int i = 0; i < EmberCount; i++)
            {
                float h1 = Hash(i, 21), h2 = Hash(i, 22), h3 = Hash(i, 23), h4 = Hash(i, 24);
                float t = b - (0.03f + h1 * 0.10f);
                float life = BloomLife - 0.10f - h1 * 0.10f;
                float ang = h2 * 6.2832f;
                if (!SparkleFx.TowardAt(t, life, at.x, at.y, Mathf.Cos(ang), Mathf.Sin(ang) * 0.85f,
                        side * (1.6f + 2.0f * h3), side * 1.0f, side * 0.8f, 3.5f, out var p))
                    continue;
                var c = h4 < 0.55f ? BGold : h4 < 0.80f ? BPeach : BCream;
                SparkleFx.DrawToward(p, side * (0.03f + 0.035f * h4), c, 1f);
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

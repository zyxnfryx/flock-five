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

        // Button the fountain leaves. Empty until Aim.
        public static Rect Source;

        // Shot clock. Negative uses the time the caller passes.
        public static float PreviewNow = -1f;

        public struct Glint
        {
            public float X, Y, Size, Alpha;
            public byte Hot;
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
            if (n < 1) return;
            var spark = SpriteCatalog.Sparkle;
            Texture tex = spark != null ? spark.texture : null;
            if (tex == null) tex = Dot();
            var glow = SpriteCatalog.Glow;
            Texture glowTex = glow != null && glow.texture != null ? glow.texture : tex;
            var prev = GUI.color;
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
                GUI.color = _buf[i].Hot == 1
                    ? new Color(1f, 0.98f, 0.90f, a)
                    : new Color(1f, 0.84f, 0.28f, a);
                GUI.DrawTexture(new Rect(x, y, sz, sz), tex, ScaleMode.ScaleToFit, true);
            }
            GUI.color = prev;
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
                    Hot = hot ? (byte)1 : (byte)0
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

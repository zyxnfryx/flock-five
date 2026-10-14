using UnityEngine;

namespace FlockFive
{
    // Gold sparkles leave LEVEL together like the far end of a rainbow, rise off the
    // top of the screen and come back down at the player, large and bright.
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
            public byte Tint; // 0 gold, 1 peach, 2 cream, 3 rose (paint only)
            public float Bloom; // 0..1, brighter as it nears (paint only)
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

        // Perf reading for the press shot: wall time spent in Draw. Two timestamp
        // reads per paint; nothing else reads these.
        public static double DrawMsSum, DrawMsMax;
        public static int DrawCount;

        public static void Draw(float now)
        {
            if (!Armed) return;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            DrawInner(now);
            double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            DrawMsSum += ms;
            if (ms > DrawMsMax) DrawMsMax = ms;
            DrawCount++;
        }

        static void DrawInner(float now)
        {
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
                float bl = _buf[i].Bloom;
                byte tn = _buf[i].Tint;
                var c = _buf[i].Hot == 1 ? BHot
                    : tn == 1 ? new Color(1f, 0.74f, 0.46f, 1f)
                    : tn == 2 ? new Color(1f, 0.93f, 0.72f, 1f)
                    : tn == 3 ? BRose
                    : new Color(1f, 0.84f, 0.28f, 1f);
                // Warm glow that strengthens as the glint nears.
                float g = sz * 2.1f;
                GUI.color = new Color(c.r, c.g * 0.96f, c.b * 0.8f, a * (0.20f + 0.26f * bl));
                GUI.DrawTexture(new Rect(_buf[i].X - g * 0.5f, _buf[i].Y - g * 0.5f, g, g), glowTex, ScaleMode.ScaleToFit, true);
                c = Color.Lerp(c, BCream, 0.5f * bl);
                GUI.color = new Color(c.r, c.g, c.b, a);
                GUI.DrawTexture(new Rect(x, y, sz, sz), tex, ScaleMode.ScaleToFit, true);
                if (bl > 0.4f)
                {
                    float k = sz * 0.42f;
                    GUI.color = new Color(1f, 0.99f, 0.94f, a * (bl - 0.4f));
                    GUI.DrawTexture(new Rect(_buf[i].X - k * 0.5f, _buf[i].Y - k * 0.5f, k, k), tex, ScaleMode.ScaleToFit, true);
                }
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
        //   0-0.79 s   forward shell: 64 glints fan out radially, lift, grow 1x->3.5x, fade late
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

        const int ShellCount = 64;

        static void DrawBloomOver(float b, Vector2 at, float side, Texture tex, Texture glowTex)
        {
            if (b < 0f || b > BloomLife) return;
            // Release wave: every glint leaves the flower on this frame, on its own
            // landing column across the full width (mirrored, stratified lanes), up past
            // the top edge and back down at the camera.
            float W = Screen.width, H = Screen.height;
            for (int i = 0; i < ShellCount; i++)
            {
                int lane = i >> 1;
                float h2 = Hash(lane, 32), h4 = Hash(lane, 34), h7 = Hash(lane, 37);
                float tx = W * 0.5f + SparkleFx.Lane(i, ShellCount, h7, W * 0.62f);
                if (!SparkleFx.DepthArcAt(b, 0.62f + 0.14f * h2, at.x, at.y, tx,
                        -0.28f * H, 1.25f * H, 0.6f, 1.4f, 0.78f, out var p))
                    continue;
                var c = h4 < 0.45f ? BGold : h4 < 0.70f ? BCream : h4 < 0.90f ? BPeach : BRose;
                SparkleFx.DrawDepth(p, side * (0.035f + 0.03f * h4), c, 1f);
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
        // Every glint starts on the same frame; nothing is ordered by index or x.
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
            // Glint size follows a phone-width reference, so a 3:4 tablet does not read
            // as a solid fill when the near wave arrives.
            float sizeRef = minSide < screenH * 0.5625f ? minSide : screenH * 0.5625f;
            int n = 0;
            int cap = dst.Length;
            for (int i = 0; i < Count && n < cap; i++)
            {
                if (!Place(i, age, ox, oy, minSide / screenW, minSide / screenH, out float nx, out float ny, out float alpha, out float sizeN, out bool hot, out float bloom))
                    continue;
                var g = new Glint
                {
                    X = nx * screenW,
                    Y = ny * screenH,
                    Size = sizeN * sizeRef,
                    Alpha = alpha,
                    Hot = hot ? (byte)1 : (byte)0,
                    Tint = TintOf(i >> 1),
                    Bloom = bloom
                };
                dst[n++] = g;
            }
            return n;
        }

        // Normalized GUI space, y = 0 at the top. Each glint rides SparkleFx.DepthArcAt:
        // it leaves the button with all the others, rises off the top of the screen,
        // then comes back down at the camera (scale ~ 1/(1 - z)) onto its own landing
        // column. Columns are mirrored, stratified lanes across the full width.
        public const float ArcTop = -0.30f;
        public const float ArcLand = 1.25f;
        public const float ArcHalfSpan = 0.62f;
        public const float ScaleFar = 0.6f;
        public const float ScalePeak = 1.4f;
        public const float ZMax = 0.78f;

        static bool Place(int i, float age, float ox, float oy, float sx, float sy,
            out float nx, out float ny, out float alpha, out float sizeN, out bool hot, out float bloom)
        {
            nx = ny = alpha = sizeN = bloom = 0f;
            hot = false;
            if (age < 0f) return false;
            int lane = i >> 1;
            float h2 = Hash(lane, 2);
            float h3 = Hash(lane, 3);
            float h4 = Hash(lane, 4);
            float h7 = Hash(lane, 7);
            float tx = 0.5f + SparkleFx.Lane(i, Count, h7, ArcHalfSpan);
            if (!SparkleFx.DepthArcAt(age, 0.85f + 0.20f * h3, ox, oy, tx, ArcTop, ArcLand,
                    ScaleFar, ScalePeak, ZMax, out var p))
                return false;
            nx = p.X;
            ny = p.Y;
            if (ny > 1.12f) return false;
            hot = h2 > 0.74f;
            sizeN = ((hot ? 0.009f : 0.010f) + (hot ? 0.006f : 0.008f) * h4) * p.Scale;
            bloom = p.Bloom;
            float tw = 0.45f + 0.55f * Mathf.Sin(age * 16f + h4 * 11f);
            if (tw < 0f) tw = 0f;
            tw = 0.55f + 0.45f * tw * tw;
            alpha = tw * p.Alpha;
            if (hot) alpha = alpha > 0.92f ? 0.92f : alpha * 1.15f;
            if (alpha < 0.04f) return false;
            return true;
        }

        // Mostly gold, then peach, cream and a touch of rose. Stable per glint (no flicker).
        static byte TintOf(int i)
        {
            float h = Hash(i, 5);
            return h < 0.55f ? (byte)0 : h < 0.77f ? (byte)1 : h < 0.92f ? (byte)2 : (byte)3;
        }

        // Handoff into the stage: a second, slower depth wave from the release. Same
        // synchronized arc on interleaved lanes; it re-enters just behind the release
        // wave and carries the light over the new garden. Fades by BloomLife.
        const int EmberCount = 48;

        static void DrawEmbers(float b, Vector2 at, float side, Texture tex, Texture glowTex)
        {
            if (b < 0f || b > BloomLife) return;
            float W = Screen.width, H = Screen.height;
            for (int i = 0; i < EmberCount; i++)
            {
                int lane = i >> 1;
                float h3 = Hash(lane, 23), h4 = Hash(lane, 24), h7 = Hash(lane, 27);
                float tx = W * 0.5f + SparkleFx.Lane(i, EmberCount, h7, W * 0.58f);
                if (!SparkleFx.DepthArcAt(b, 0.74f + 0.08f * h3, at.x, at.y, tx,
                        -0.22f * H, 1.25f * H, 0.5f, 1.2f, 0.80f, out var p))
                    continue;
                var c = h4 < 0.50f ? BGold : h4 < 0.75f ? BPeach : h4 < 0.92f ? BCream : BRose;
                SparkleFx.DrawDepth(p, side * (0.022f + 0.022f * h4), c, 0.9f);
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

using UnityEngine;

namespace FlockFive
{
    // Gold sparkles leave LEVEL together like the far end of a rainbow, rise off the
    // top of the screen and come back down at the player, large and bright.
    // The garden shows through every gap. No frame is a solid or near-solid fill: the
    // wash is a translucent veil (combined alpha < ~0.4), never an opaque cover.
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
            public float Soft;  // 0..1, defocus as it passes the lens (paint only)
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
            PressRainbow.Fire(now, Source);
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
            bool rainbow = PressRainbow.Live();
            if (!rainbow) DrawWash(VisualAge(now), w, h, at, glowTex);
            for (int i = 0; i < n; i++)
            {
                float a = _buf[i].Alpha;
                if (a < 0.03f) continue;
                float sz = _buf[i].Size;
                if (sz < 1.5f) continue;
                byte tn = _buf[i].Tint;
                var c = _buf[i].Hot == 1 ? BHot
                    : tn == 1 ? BPeach
                    : tn == 2 ? BCream
                    : tn == 3 ? BRose
                    : BGold;
                SparkleFx.DrawDepthGlint(_buf[i].X, _buf[i].Y, sz, a, _buf[i].Bloom, _buf[i].Soft, c, tex, glowTex);
            }
            DrawBloomOver(bloom, at, side, tex, glowTex, !rainbow);
            if (!rainbow) DrawEmbers(bloom, at, side, tex, glowTex);
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
        internal static readonly Color BGold = new Color(1f, 0.84f, 0.36f, 1f);
        internal static readonly Color BCream = new Color(1f, 0.96f, 0.84f, 1f);
        internal static readonly Color BPeach = new Color(1f, 0.74f, 0.52f, 1f);
        internal static readonly Color BRose = new Color(1f, 0.66f, 0.62f, 1f);
        internal static readonly Color BHot = new Color(1f, 0.99f, 0.94f, 1f);

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

        // Full-screen wash as the near wave arrives: a warm, painterly light veil, not a
        // flat fill. Soft cream base plus three large glows in rainbow band order (rose
        // highest, peach, then gold nearest the button), all centred, so nothing reads
        // left to right. Peaks at ~0.80 s, clears by Duration to reveal the stage.
        // Combined alpha stays under ~0.4, so the scene shows through.
        public const float WashPeak = 0.80f;
        public const float WashBase = 0.20f;

        public static float WashLevel(float age)
        {
            if (age <= 0.58f || age >= Duration) return 0f;
            float u = age < WashPeak ? (age - 0.58f) / (WashPeak - 0.58f) : (Duration - age) / (Duration - WashPeak);
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }

        static void DrawWash(float age, float w, float h, Vector2 at, Texture glowTex)
        {
            float k = WashLevel(age);
            if (k < 0.01f) return;
            GUI.color = new Color(1f, 0.94f, 0.80f, WashBase * k);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            float big = (w > h ? w : h) * 1.25f;
            float cx = w * 0.5f;
            Blit(new Vector2(cx, h * 0.18f), big, glowTex, new Color(BRose.r, BRose.g, BRose.b, 0.10f * k));
            Blit(new Vector2(cx, h * 0.40f), big, glowTex, new Color(BPeach.r, BPeach.g, BPeach.b, 0.12f * k));
            Blit(new Vector2(cx, at.y - h * 0.10f), big * 0.85f, glowTex, new Color(BGold.r, BGold.g, BGold.b, 0.14f * k));
        }

        const int ShellCount = 72;

        static void DrawBloomOver(float b, Vector2 at, float side, Texture tex, Texture glowTex, bool shell)
        {
            if (b < 0f || b > BloomLife) return;
            // Release wave: every glint leaves the flower on this frame, on its own
            // landing column across the full width (mirrored, stratified lanes), up past
            // the top edge and back down at the camera.
            float W = Screen.width, H = Screen.height;
            for (int i = 0; shell && i < ShellCount; i++)
            {
                int lane = i >> 1;
                float h2 = Hash(lane, 32), h4 = Hash(lane, 34), h7 = Hash(lane, 37);
                float tx = W * 0.5f + SparkleFx.Lane(i, ShellCount, h7, W * 0.62f);
                if (!SparkleFx.DepthArcAt(b, 0.62f + 0.14f * h2, at.x, at.y, tx,
                        -0.28f * H, 1.25f * H, 0.6f, 1.4f, 0.82f, out var p))
                    continue;
                var c = h4 < 0.45f ? BGold : h4 < 0.70f ? BCream : h4 < 0.90f ? BPeach : BRose;
                SparkleFx.DrawDepth(p, side * (0.035f + 0.03f * h4), c, 1f);
            }

            // Centre star: glow halo, gold star, then a smaller cream star on top.
            float pop = EaseOutBack(b / 0.22f);
            float fade = 1f - Mathf.Clamp01((b - 0.04f) / 0.12f);
            fade = fade * fade * (3f - 2f * fade);
            if (fade > 0.01f)
            {
                Blit(at, side * 1.1f * pop, glowTex, new Color(1f, 0.88f, 0.52f, 0.55f * fade));
                Blit(at, side * 0.62f * pop, tex, new Color(BGold.r, BGold.g, BGold.b, 0.9f * fade));
                Blit(at, side * 0.36f * pop, tex, new Color(BHot.r, BHot.g, BHot.b, fade));
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
                if (!Place(i, age, ox, oy, minSide / screenW, minSide / screenH, out float nx, out float ny, out float alpha, out float sizeN, out bool hot, out float bloom, out float soft))
                    continue;
                var g = new Glint
                {
                    X = nx * screenW,
                    Y = ny * screenH,
                    Size = sizeN * sizeRef,
                    Alpha = alpha,
                    Hot = hot ? (byte)1 : (byte)0,
                    Tint = TintOf(i >> 1),
                    Bloom = bloom,
                    Soft = soft
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
        public const float ZMax = 0.82f;

        static bool Place(int i, float age, float ox, float oy, float sx, float sy,
            out float nx, out float ny, out float alpha, out float sizeN, out bool hot, out float bloom, out float soft)
        {
            nx = ny = alpha = sizeN = bloom = soft = 0f;
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
            soft = p.Soft;
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
                        -0.22f * H, 1.25f * H, 0.5f, 1.2f, 0.84f, out var p))
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

    // Release rainbow. One overlay, its own clock (stamped by LevelCurtain.Fire on the
    // committed press, same frame as Load; nothing waits on it). Painted once per
    // OnGUI, last, from FlockFiveApp.OnGUI, so it plays over the cut on any face.
    // Clock: AdPause.Unscaled on device. Under Time.captureDeltaTime (60 fps capture)
    // it counts frames instead: frame encode makes wall time run ~4x faster than the
    // captured frames, which squeezed the whole 1.55 s arc into ~25 captured frames.
    //   0.00-0.20  launch: every glint leaves the button together, fan opens to the
    //              full screen width within ~0.12 s
    //   0.06-0.50  rise: streaks up past the top edge (pairs vary in height/speed)
    //   0.50-1.25  return: re-enters from the top at staggered depths, swelling to
    //              6-22% of screen width, brightening, then defocusing
    //   0.95-1.55  wash: centred radial glows, peak alpha ~0.45, then clear
    // Nothing depends on x: lanes are mirrored pairs; per-pair hashes vary depth.
    public static class PressRainbow
    {
        public const float Life = 1.55f;
        public const int N = 128;
        const float WashUp = 0.95f, WashPeak = 1.20f;
        const int Ghosts = 6;
        const float GhostStep = 0.007f;

        static float _at = -1f;
        static int _atFrame;
        static Rect _src;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _at = -1f;

        // The only writer of the start time.
        public static void Fire(float now, Rect src)
        {
            _at = now;
            _atFrame = Time.frameCount;
            _src = src;
        }

        public static void Stop() => _at = -1f;

        // Seconds since Fire; negative when idle.
        public static float Age()
        {
            if (_at < 0f) return -1f;
            if (Time.captureDeltaTime > 0f) return (Time.frameCount - _atFrame) * Time.captureDeltaTime;
            return AdPause.Unscaled - _at;
        }

        public static bool Live() { float t = Age(); return t >= 0f && t < Life; }

        static float H01(int i, int salt)
        {
            uint h = (uint)(i * 374761393 + salt * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
        }

        static bool At(int i, float t, float W, float H, float ox, float oy, out float x, out float y,
            out float sz, out float a, out float bloom, out float soft)
        {
            x = y = sz = a = bloom = soft = 0f;
            if (t < 0f) return false;
            int pair = i >> 1;
            float hj = H01(pair, 7), hs = H01(pair, 11), hd = H01(pair, 13);
            float hr = H01(pair, 19), hb = H01(pair, 23), hy = H01(pair, 29);
            float D = Mathf.Sqrt(W * W + H * H);
            float cx = W * 0.5f;
            float tx = cx + SparkleFx.Lane(i, N, hj, W * 0.58f);
            float far = D * (0.025f + 0.010f * hs);
            // Per pair: rise length and top, return delay and entry depth.
            float riseEnd = 0.36f + 0.14f * hr;
            float yTop = -(0.18f + 0.22f * hy) * H;
            float backStart = riseEnd + 0.05f + 0.12f * hb;
            float backEnd = backStart + 0.62f + 0.10f * hd;
            if (t < riseEnd)
            {
                float r = t / riseEnd;
                float e = 1f - Mathf.Pow(1f - r, 2.4f);
                float fan = Mathf.Clamp01(r / 0.27f);
                fan = 1f - (1f - fan) * (1f - fan);
                x = ox + (tx - ox) * fan;
                y = oy + (yTop - oy) * e;
                sz = far * (1f + 0.4f * e);
                a = Mathf.Clamp01(t / 0.03f);
                bloom = 0.2f;
            }
            else if (t < backStart) return false;
            else if (t < backEnd)
            {
                float w = (t - backStart) / (backEnd - backStart);
                float entry = -(0.10f + 0.25f * hy) * H;
                y = entry + (1.25f * H - entry) * Mathf.Pow(w, 1.35f);
                x = cx + (tx - cx) * (1f + 0.55f * w);
                float near = W * (0.06f + 0.16f * hd * hd);
                float z = w * w;
                sz = far + (near - far) * z;
                a = 1f - Mathf.Clamp01((w - 0.80f) / 0.20f);
                bloom = 0.4f + 0.6f * w;
                soft = Mathf.Clamp01((w - 0.45f) / 0.45f);
            }
            else return false;
            return y > -sz && y < H + sz && x > -sz && x < W + sz;
        }

        // Returns glints drawn; t and washA for the capture log (t < 0 when idle).
        public static int Draw(out float t, out float washA)
        {
            t = Age();
            washA = 0f;
            if (t < 0f || t >= Life) { t = -1f; return 0; }
            float W = Screen.width, H = Screen.height;
            if (W < 2f || H < 2f) return 0;
            var spr = SpriteCatalog.Sparkle;
            var glow = SpriteCatalog.Glow;
            Texture star = spr != null ? spr.texture : null;
            Texture glowTex = glow != null ? glow.texture : null;
            if (star == null) return 0;
            if (glowTex == null) glowTex = star;
            var prev = GUI.color;
            float ox = _src.width > 2f ? _src.center.x : W * 0.5f;
            float oy = _src.width > 2f ? _src.y + _src.height * 0.30f : H * 0.78f;

            washA = DrawWash(t, W, H, glowTex);
            int drawn = 0;
            for (int i = 0; i < N; i++)
            {
                int pair = i >> 1;
                float h4 = H01(pair, 17);
                var c = h4 < 0.40f ? LevelCurtain.BGold : h4 < 0.62f ? LevelCurtain.BCream
                    : h4 < 0.84f ? LevelCurtain.BPeach : LevelCurtain.BRose;
                if (t < 0.50f + Ghosts * GhostStep)
                {
                    for (int g = Ghosts; g >= 1; g--)
                    {
                        float tg = t - g * GhostStep;
                        if (!At(i, tg, W, H, ox, oy, out float gx, out float gy, out float gs, out float ga, out float gb, out _))
                            continue;
                        if (gb > 0.3f) continue; // streak the rise only
                        float k = 1f - g / (float)(Ghosts + 1);
                        SparkleFx.DrawDepthGlint(gx, gy, gs * (0.55f + 0.4f * k), ga * 0.55f * k, 0f, 0.3f, c, star, glowTex);
                    }
                }
                if (!At(i, t, W, H, ox, oy, out float x, out float y, out float sz, out float a, out float bl, out float sf))
                    continue;
                SparkleFx.DrawDepthGlint(x, y, sz, a, bl, sf, c, star, glowTex);
                drawn++;
            }
            GUI.color = prev;
            return drawn;
        }

        // Warm veil as centred radial glows (no flat fill, no bands): one big cream
        // bloom on screen centre, a peach halo round it, a rose crown above. Combined
        // peak ~0.45 at the centre, falling off to the edges, so the garden shows.
        static float DrawWash(float t, float W, float H, Texture glowTex)
        {
            if (t <= WashUp || t >= Life) return 0f;
            float u = t < WashPeak ? (t - WashUp) / (WashPeak - WashUp) : (Life - t) / (Life - WashPeak);
            u = Mathf.Clamp01(u);
            float k = u * u * (3f - 2f * u);
            if (k < 0.01f) return 0f;
            float D = Mathf.Sqrt(W * W + H * H);
            float cx = W * 0.5f, cy = H * 0.5f;
            Glow(cx, cy, D * 1.30f, glowTex, LevelCurtain.BPeach, 0.16f * k);
            Glow(cx, cy, D * 0.90f, glowTex, LevelCurtain.BCream, 0.20f * k);
            Glow(cx, H * 0.30f, D * 0.70f, glowTex, LevelCurtain.BRose, 0.09f * k);
            return 0.45f * k;
        }

        static void Glow(float x, float y, float d, Texture tex, Color c, float a)
        {
            GUI.color = new Color(c.r, c.g, c.b, a);
            GUI.DrawTexture(new Rect(x - d * 0.5f, y - d * 0.5f, d, d), tex, ScaleMode.StretchToFill, true);
        }
    }
}

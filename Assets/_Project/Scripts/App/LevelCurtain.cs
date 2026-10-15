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
        // Legacy b88 beat bounds, kept so Build88CurtainTests still compiles (that curtain is replaced by PressRainbow).
        public const float BuildEnd = SparkleFx.CurtainBuildEnd;
        public const float WashEnd = SparkleFx.CurtainWashEnd;
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

        // b100: the flower art's drawn rect (sunk pose) on the frames before the held face
        // is copied. The release rainbow uses it to put the button back over its glints.
        public static Rect FaceVisual;

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

        // Held face: the splash as it looked on press-down, copied GPU-side at the end
        // of that frame. After Load swaps the world, the release rainbow keeps showing
        // this until its mask is at full coverage, then drops it (the swap).
        public static RenderTexture HeldFace;
        static bool _holdWanted;

        public static bool TakeHoldRequest()
        {
            if (!_holdWanted) return false;
            _holdWanted = false;
            return true;
        }

        static void DropHeldFace()
        {
            if (HeldFace != null) RenderTexture.ReleaseTemporary(HeldFace);
            HeldFace = null;
        }

        public static void Reset()
        {
            _holdWanted = false;
            DropHeldFace();
            Armed = false;
            LoadStarted = false;
            Ready = false;
            StartedAt = 0f;
            FiredAt = -1f;
            Source = default;
            FaceVisual = default;
            _behindFrame = -1;
            PreviewNow = -1f;
        }

        public static void Clear() => Reset();

        // Pointer-down. Starts the fountain clock. Does not mark the load.
        public static void Arm(float now)
        {
            if (Armed) return;
            Armed = true;
            StartedAt = now;
            _holdWanted = true;
        }

        // Where the fountain comes out of. The LEVEL flower passes its rest rect.
        public static void Aim(Rect button)
        {
            if (button.width > 2f && button.height > 2f) Source = button;
        }

        // Same, plus where the art is actually drawn this frame (sunk/squashed). Tracked
        // until the held face is copied so the rainbow knows where the button sits in it.
        public static void Aim(Rect button, Rect visual)
        {
            Aim(button);
            if (HeldFace == null && visual.width > 2f && visual.height > 2f) FaceVisual = visual;
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
            PressRainbow.Fire(now, Source, HeldFace, HeldFace != null ? FaceVisual : default);
            HeldFace = null; // the rainbow owns it now; it outlives the curtain
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
            // b100: the arc is normally already painted behind the button (DrawBehind, from
            // DrawFlowerPlay). Only faces with no LEVEL flower fall back to painting it here.
            if (_behindFrame != Time.frameCount) PaintArc(now, w, h, tex, glowTex);
            DrawBloomOver(bloom, at, side, tex, glowTex, !rainbow);
            if (!rainbow) DrawEmbers(bloom, at, side, tex, glowTex);
            GUI.color = prev;
        }

        static int _behindFrame = -1;

        // b100 (Brandon): the sparkle arc leaves the TOP curve of the button and flies up
        // from BEHIND it. DrawFlowerPlay calls this just before the flower art, so the art
        // (and everything painted after it) sits over the glints. Paint only.
        public static void DrawBehind(float now)
        {
            if (!Armed) return;
            float w = Screen.width, h = Screen.height;
            if (w < 2f || h < 2f) return;
            var spark = SpriteCatalog.Sparkle;
            Texture tex = spark != null ? spark.texture : null;
            if (tex == null) tex = Dot();
            var glow = SpriteCatalog.Glow;
            Texture glowTex = glow != null && glow.texture != null ? glow.texture : tex;
            var prev = GUI.color;
            PaintArc(now, w, h, tex, glowTex);
            GUI.color = prev;
            _behindFrame = Time.frameCount;
        }

        static void PaintArc(float now, float w, float h, Texture tex, Texture glowTex)
        {
            if (_buf == null || _buf.Length < Count * 2) _buf = new Glint[Count * 2];
            int n = Fill(VisualAge(now), w, h, Source, _buf);
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
            // b95 (Brandon): the burst leaves the TOP curve of the button, fanning up and over
            // (an upside-down U), not from its middle or bottom. Normalized ring: centre, radii.
            float rimRx = 0f, rimRy = 0f;
            if (sourcePx.width > 2f && sourcePx.height > 2f)
            {
                rimRx = sourcePx.width * SparkleFx.FlatRingRx / screenW;
                rimRy = sourcePx.height * SparkleFx.FlatRingRy / screenH;
                oy = (sourcePx.y + sourcePx.height * SparkleFx.FlatRingCy) / screenH;
                if (oy < 0.08f) oy = 0.08f;
                else if (oy > 0.94f) oy = 0.94f;
            }
            int n = 0;
            int cap = dst.Length;
            for (int i = 0; i < Count && n < cap; i++)
            {
                if (!Place(i, age, ox, oy, rimRx, rimRy, minSide / screenW, minSide / screenH, out float nx, out float ny, out float alpha, out float sizeN, out bool hot, out float bloom, out float soft))
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

        static bool Place(int i, float age, float ox, float oy, float rimRx, float rimRy, float sx, float sy,
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
            float gx = ox, gy = oy;
            if (rimRx > 0f)
            {
                // Lane left/right of centre maps to a seat on the upper half of the ring.
                float k = Mathf.Clamp((tx - 0.5f) / ArcHalfSpan, -1f, 1f);
                float a = k * Mathf.PI * 0.46f;
                gx = ox + Mathf.Sin(a) * rimRx;
                gy = oy - Mathf.Cos(a) * rimRy;
            }
            if (!SparkleFx.DepthArcAt(age, 0.85f + 0.20f * h3, gx, gy, tx, ArcTop, ArcLand,
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
                float ek = Mathf.Clamp((tx - W * 0.5f) / (W * 0.58f), -1f, 1f);
                float ea = ek * Mathf.PI * 0.46f;
                float egx = at.x + Mathf.Sin(ea) * side * SparkleFx.FlatRingRx;
                float egy = at.y - Mathf.Cos(ea) * side * SparkleFx.FlatRingRy;
                if (!SparkleFx.DepthArcAt(b, 0.74f + 0.08f * h3, egx, egy, tx,
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

    // Release rainbow and transition mask. One overlay, its own clock (stamped by
    // LevelCurtain.Fire on the committed press, the same frame as Load; nothing waits on
    // it), painted last by FlockFiveApp.OnGUI on every face. Brandon's sequence:
    //   0.00-0.50  emission: the button, far in the background, spews glints
    //              continuously (hash-staggered, not ordered by x); each flies up small
    //              and far and out of the top (0.40 s flight)
    //   0.44-1.36  first descent: each comes back down from the top, closer and larger
    //              (perspective), onto its own cell of a stratified grid; the veil
    //              follows the landed fraction. Emission is done by 0.50.
    //   1.38       full coverage (all landed, veil at peak). The clock is held here until
    //              a frame has painted it, then
    //   1.43       swap: the held splash is dropped (the garden loaded on the tap)
    //   1.48-2.28  second descent: the remaining glints fall to the bottom, fading, and
    //              the veil fades with them, exposing the garden progressively. One
    //              wave falls out of the covering glints, one finishes coming down from
    //              the top and across the whole screen.
    //   2.30       last sparkle gone: the stage begins (input and PlayClock resume)
    // Under capture (Time.captureDeltaTime) the clock counts frames.
    public static class PressRainbow
    {
        public const float LaunchEnd = 0.35f, FanTime = 0.15f;
        public const float EmitWindow = 0.50f, RiseDur = 0.40f, DescGap = 0.04f, FallIn = 0.42f;
        const float LandFrom = RiseDur + DescGap + FallIn; // 0.86: earliest landing
        public const float CoverAt = EmitWindow + LandFrom + 0.02f; // 1.38
        public const float SwapAt = CoverAt, RevealAt = CoverAt + 0.02f; // 1.38, 1.40 (was 1.43, 1.48)
        const float OutWindow = 0.15f, OutFall = 0.40f, L2Window = 0.15f, L2Fall = 0.55f;
        public const float RevealEnd = RevealAt + 0.80f; // 2.20
        public const float Life = RevealEnd + 0.02f;     // 2.22
        const float FailsafeSecs = 3.5f;
        const float StageMargin = 0.03f;
        public const int Cols = 8; // even: four mirrored column pairs
        const int Ghosts = 4;
        const float GhostStep = 1f / 60f;
        // fx_sparkle.png: thin four-point star, solid body ~45% of its rect.
        public const float StarBody = 0.45f;
        public const float LogEveryFrameUntil = 0.62f;
        public const float VeilPeak = 0.90f;

        static float _at = -1f;
        static int _atFrame;
        static Rect _src;
        static Rect _btn;
        static float _ringX, _ringY, _ringRx, _ringRy; // top-curve ring of the held button; Rx 0 = none
        static RenderTexture _face;
        static bool _covered;
        static float _shift, _fireReal;
        public static int SwapFrame = -1;
        public static float LastMaxRectPx, LastMaxBodyPx, LastXMin, LastXMax, LastVeil;
        public static Rect LastWashRect;
        public static int LastRows, LastN;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _at = -1f; _face = null; _btn = default; _ringRx = 0f; SwapFrame = -1; _covered = false; _shift = 0f; }

        // The only writer of the start time. Takes ownership of the held face.
        public static void Fire(float now, Rect src, RenderTexture face) => Fire(now, src, face, default);

        // btn: where the flower art sits inside the held face (b100). The rise glints
        // leave its top curve and are painted under a re-draw of that art from the face.
        public static void Fire(float now, Rect src, RenderTexture face, Rect btn)
        {
            DropFace();
            _btn = face != null && btn.width > 2f && btn.height > 2f ? btn : default;
            _at = now;
            _atFrame = Time.frameCount;
            _src = src;
            _face = face;
            SwapFrame = -1;
            _covered = false;
            _shift = 0f;
            _fireReal = Time.realtimeSinceStartup;
        }

        public static void Stop() { _at = -1f; DropFace(); }

        static void DropFace()
        {
            if (_face != null) RenderTexture.ReleaseTemporary(_face);
            _face = null;
        }

        public static bool HasFace => _face != null;

        public static float Age()
        {
            if (_at < 0f) return -1f;
            if (Time.captureDeltaTime > 0f) return (Time.frameCount - _atFrame) * Time.captureDeltaTime - _shift;
            return AdPause.Unscaled - _at - _shift;
        }

        public static bool Live() { float t = Age(); return t >= 0f && t < Life; }

        // The stage is held from the tap until the sparkle is completely gone (Life plus
        // a margin): garden input is swallowed and PlayClock stands still. One gate for
        // both. Hard failsafe: after 3 s of real time it opens regardless (an ad, pause
        // or background can freeze the sparkle clock, never the input).
        public static bool StageHeld()
        {
            if (_at < 0f) return false;
            if (Time.captureDeltaTime <= 0f && Time.realtimeSinceStartup - _fireReal > FailsafeSecs) return false;
            float t = Age();
            return t >= 0f && t < Life + StageMargin;
        }

        static float H01(int i, int salt)
        {
            uint h = (uint)(i * 374761393 + salt * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
        }

        // Grid for this screen: Cols columns, rows to fill the height with square cells.
        public static void Grid(float W, float H, out float cell, out int rows, out int n)
        {
            cell = W / Cols;
            rows = Mathf.Max(1, Mathf.CeilToInt(H / cell));
            n = rows * Cols;
        }

        // Cell of glint i: pairs (i, i^1) share a row and mirror about centre.
        static void Cell(int i, int rows, float W, float H, out float lx, out float ly)
        {
            int pair = i >> 1;
            int half = Cols / 2;
            int row = pair / half;
            int k = pair % half;                   // 0 = innermost column pair
            int col = (i & 1) == 0 ? half - 1 - k : half + k;
            float cw = W / Cols, rh = H / rows;
            // Stable per-glint jitter, +-35% of the cell, so the lattice dissolves.
            lx = (col + 0.5f + (H01(i, 51) - 0.5f) * 0.70f) * cw;
            ly = (row + 0.5f + (H01(i, 53) - 0.5f) * 0.70f) * rh;
        }

        // Second, smaller layer on the half-offset lattice (cell corners), checkerboard
        // so it is about half density. Columns 0..6 at x = (c+1) cells mirror c <-> 6-c
        // with the same parity, rows at (r+1). Cached per row count.
        static int[] _l2C, _l2R;
        static int _l2Rows = -1;
        public static int Layer2Count(int rows)
        {
            if (rows == _l2Rows && _l2C != null) return _l2C.Length;
            var cs = new System.Collections.Generic.List<int>();
            var rs = new System.Collections.Generic.List<int>();
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < Cols - 1; c++)
                    if (((c + r) & 1) == 0) { cs.Add(c); rs.Add(r); }
            _l2C = cs.ToArray();
            _l2R = rs.ToArray();
            _l2Rows = rows;
            return _l2C.Length;
        }

        static void Cell2(int j, int rows, float W, float H, out float lx, out float ly, out int mirrorKey)
        {
            int c = _l2C[j], r = _l2R[j];
            float cw = W / Cols, rh = H / rows;
            lx = (c + 1f + (H01(j, 71) - 0.5f) * 0.70f) * cw;
            ly = (r + 1f + (H01(j, 73) - 0.5f) * 0.70f) * rh;
            mirrorKey = 1000 + r * Cols + System.Math.Min(c, Cols - 2 - c);
        }

        // Per-glint look: size +-25%, rotation (the star is 90-degree symmetric), and a
        // few bright cores scattered through the mask.
        static float SizeK(int key) => 0.75f + 0.50f * H01(key, 57);
        public static float RotOf(int key) => H01(key, 59) * 90f;
        static bool CoreOf(int key) => H01(key, 61) < 0.08f;

        // Painterly palette: gold, cream, peach, rose, plus a per-pair hue drift so the
        // mask reads as brushed light, not flat tiles. Hashed by mirrored pair only.
        static readonly Color Honey = new Color(1f, 0.80f, 0.46f, 1f);
        static Color Tint(int pair)
        {
            float h = H01(pair, 17), d = H01(pair, 41);
            Color a, b;
            if (h < 0.30f) { a = LevelCurtain.BGold; b = Honey; }
            else if (h < 0.55f) { a = LevelCurtain.BCream; b = LevelCurtain.BGold; }
            else if (h < 0.80f) { a = LevelCurtain.BPeach; b = LevelCurtain.BCream; }
            else { a = LevelCurtain.BRose; b = LevelCurtain.BPeach; }
            return Color.Lerp(a, b, 0.35f * d);
        }

        // One glint's whole life, small and far to big and near:
        //   emit at e (hashed in the 0.5 s window), fly up and off the top;
        //   layer 1: fall back from the top onto its cell (first descent), hold, then
        //            fall on to the bottom, fading (second descent);
        //   layer 2: wait above, then fall the full height after the swap, fading.
        // The landing column is the cell's, so the spread is uniform; nothing is
        // ordered by x. Returns false when not drawn.
        static bool Flight(int key, float lx, float ly, float body, bool layer2, float t, float W, float H,
            float ox, float oy, out float x, out float y, out float sz, out float a, out float bloom, out float soft)
        {
            x = y = sz = a = bloom = soft = 0f;
            float e = EmitWindow * H01(key, 91);
            float age = t - e;
            if (age < 0f) return false;
            float cx = W * 0.5f;
            float D = Mathf.Sqrt(W * W + H * H);
            float far = D * (0.020f + 0.008f * H01(key, 11)); // visible body px, far away
            bool core = CoreOf(key);
            SparkleFx.Depth d;
            if (age < RiseDur)
            {
                float r = age / RiseDur;
                float tx = cx + (lx - cx) * 1.12f;
                if (_ringRx > 0f)
                {
                    // b100: leave the TOP curve of the button (an upside-down U), not its middle.
                    float k = Mathf.Clamp((lx - cx) / (W * 0.5f), -1f, 1f);
                    float ra = k * Mathf.PI * 0.46f;
                    ox = _ringX + Mathf.Sin(ra) * _ringRx;
                    oy = _ringY - Mathf.Cos(ra) * _ringRy;
                }
                float fan = Mathf.Clamp01(age / 0.20f);
                fan = 1f - (1f - fan) * (1f - fan) * (1f - fan);
                x = ox + (tx - ox) * fan;
                y = oy + (-0.10f * H - oy) * (0.25f * r + 0.75f * r * r);
                sz = far * (1f - 0.35f * r) / StarBody;
                a = Mathf.Clamp01(age / 0.03f);
                bloom = 0.25f;
            }
            else if (!layer2)
            {
                float d0 = e + RiseDur + DescGap;
                float so = RevealAt + OutWindow * H01(key, 85);
                if (t < d0) return false;
                if (t < d0 + FallIn)
                {
                    float w = (t - d0) / FallIn;
                    SparkleFx.DescentAt(w, lx, -0.08f * H, ly, far * 0.8f, body, 0.65f, out d);
                    x = d.X; y = d.Y; sz = d.Scale / StarBody;
                    a = Mathf.Clamp01(0.3f + 2.5f * w); bloom = d.Bloom;
                    soft = core ? 0.05f : d.Soft;
                }
                else if (t < so)
                {
                    x = lx; y = ly; sz = body / StarBody; a = 1f;
                    bloom = core ? 1f : 0.9f;
                    soft = core ? 0.05f : 0.5f;
                }
                else if (t < so + OutFall)
                {
                    float w = (t - so) / OutFall;
                    SparkleFx.DescentAt(w, lx, ly, H + body, body, body * 1.15f, 1.8f, out d);
                    x = d.X; y = d.Y; sz = d.Scale / StarBody;
                    float f = 1f - w;
                    a = f * f * (3f - 2f * f);
                    bloom = 0.9f;
                    soft = core ? 0.05f : 0.5f + 0.5f * w;
                }
                else return false;
            }
            else
            {
                float s2 = RevealAt + L2Window * H01(key, 83);
                if (t < s2 || t >= s2 + L2Fall) return false;
                float w = (t - s2) / L2Fall;
                SparkleFx.DescentAt(w, lx, -0.10f * H, 1.08f * H, far * 1.5f, body * 2f, 1.15f, out d);
                x = d.X; y = d.Y; sz = d.Scale / StarBody;
                float q = Mathf.Clamp01((w - 0.55f) / 0.45f);
                a = Mathf.Clamp01(w * 8f) * (1f - q * q * (3f - 2f * q));
                bloom = 0.5f + 0.4f * w;
                soft = core ? 0.05f : 0.3f + 0.5f * w;
            }
            return y > -sz && y < H + sz && x > -sz && x < W + sz;
        }

        // Put the button back over the rising glints: the held face's own pixels (art,
        // caption and all) re-drawn in thin strips that follow the flat ring, so the cap
        // keeps the button's silhouette and the glints show only outside it.
        static void PaintButtonCap(Rect screen)
        {
            if (_face == null || screen.width < 2f || screen.height < 2f) return;
            const int Strips = 28;
            float top = _ringY - _ringRy, bottom = _ringY + _ringRy;
            float sh = (bottom - top) / Strips;
            GUI.color = Color.white;
            bool flip = SystemInfo.graphicsUVStartsAtTop;
            for (int i = 0; i < Strips; i++)
            {
                float y0 = top + i * sh;
                float yc = y0 + sh * 0.5f - _ringY;
                float u = yc / _ringRy;
                float hw = _ringRx * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) + 1.5f;
                var r = new Rect(_ringX - hw, y0, hw * 2f, sh + 0.75f);
                float fx = (r.x - screen.x) / screen.width, fw = r.width / screen.width;
                float fy = (r.y - screen.y) / screen.height, fh = r.height / screen.height;
                var uv = flip ? new Rect(fx, fy + fh, fw, -fh) : new Rect(fx, 1f - fy - fh, fw, fh);
                GUI.DrawTextureWithTexCoords(r, _face, uv, false);
            }
        }

        // One mask glint: bokeh body, then the star rotated about its centre.
        static void PaintMaskGlint(float x, float y, float sz, float a, float bl, float sf, Color c, float rot,
            Texture star, Texture glowTex)
        {
            if (sf > 0.02f)
            {
                float bk = sz * StarBody * 2.4f;
                GUI.color = new Color(c.r, c.g * 0.96f, c.b * 0.88f, a * 0.50f * sf);
                GUI.DrawTexture(new Rect(x - bk * 0.5f, y - bk * 0.5f, bk, bk), glowTex, ScaleMode.StretchToFill, true);
            }
            var m = GUI.matrix;
            if (rot > 0.5f) GUIUtility.RotateAroundPivot(rot, new Vector2(x, y));
            SparkleFx.DrawDepthGlint(x, y, sz, a, bl, sf, c, star, glowTex);
            GUI.matrix = m;
        }

        // Even mask base under the glints: uniform full-screen veil, so coverage does
        // not depend on where the glints sit. Ramps with the return, holds, clears.
        public static float VeilAt(float t)
        {
            const float from = EmitWindow + LandFrom;
            if (t <= from - 0.30f || t >= RevealEnd - 0.10f) return 0f;
            float k;
            if (t < CoverAt)
            {
                // Supports the glints: alpha ramps with the landed fraction, behind it.
                float f = Mathf.Clamp01((t - (from - 0.30f)) / (CoverAt - (from - 0.30f)));
                k = f * f;
            }
            else if (t < RevealAt) k = 1f;
            else { float u = 1f - (t - RevealAt) / (RevealEnd - 0.10f - RevealAt); u = Mathf.Clamp01(u); k = u * u * (3f - 2f * u); }
            return VeilPeak * k;
        }

        static Vector2 ScreenRect(float W, float H, out Rect r)
        {
            Vector2 p0 = GUIUtility.ScreenToGUIPoint(Vector2.zero);
            Vector2 p1 = GUIUtility.ScreenToGUIPoint(new Vector2(W, H));
            r = Rect.MinMaxRect(Mathf.Min(p0.x, p1.x), Mathf.Min(p0.y, p1.y), Mathf.Max(p0.x, p1.x), Mathf.Max(p0.y, p1.y));
            return p0;
        }

        public static int Draw(out float t, out float washA)
        {
            t = Age();
            washA = 0f;
            if (t < 0f || t >= Life) { if (t >= Life) DropFace(); t = -1f; return 0; }
            if (Time.captureDeltaTime <= 0f && Time.realtimeSinceStartup - _fireReal > FailsafeSecs)
            {
                Stop(); t = -1f; return 0; // failsafe: a frozen clock never strands the overlay
            }
            // Swap gate: the clock may not pass full coverage until a frame has actually
            // painted it (every glint landed, veil at peak). Until then it is held there.
            float rawT = t;
            bool holding = !_covered && t >= CoverAt;
            if (holding) t = CoverAt;
            float W = Screen.width, H = Screen.height;
            if (W < 2f || H < 2f) return 0;
            var prev = GUI.color;
            ScreenRect(W, H, out Rect screen);

            bool faceShown = false;
            // Held splash face until the swap, so the world swap Load did on the tap
            // stays hidden until the mask is closed.
            if (t < SwapAt || !_covered && SwapFrame < 0 && t >= SwapAt)
            {
                if (_face != null)
                {
                    GUI.color = Color.white;
                    var uv = SystemInfo.graphicsUVStartsAtTop ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
                    GUI.DrawTextureWithTexCoords(screen, _face, uv, false);
                    faceShown = true;
                }
            }
            else if (SwapFrame < 0 && _covered)
            {
                SwapFrame = Time.frameCount;
                DropFace();
            }

            var spr = SpriteCatalog.Sparkle;
            var glow = SpriteCatalog.Glow;
            Texture star = spr != null ? spr.texture : null;
            Texture glowTex = glow != null ? glow.texture : null;
            if (star == null) { GUI.color = prev; return 0; }
            if (glowTex == null) glowTex = star;
            float ox = _src.width > 2f ? _src.center.x : W * 0.5f;
            float oy = _src.width > 2f ? _src.center.y : H * 0.78f;
            Grid(W, H, out float cell, out int rows, out int n);
            LastRows = rows;
            LastN = n;

            float veil = VeilAt(t);
            LastVeil = veil;
            LastWashRect = screen;
            washA = veil;
            _ringRx = 0f;
            // Top-curve ring: the held art's rect when the face is held (the cap re-draws it),
            // else the rest rect, so the origin is the top curve either way.
            Rect ringSrc = _btn.width > 2f ? _btn : _src;
            if (ringSrc.width > 2f && ringSrc.height > 2f)
            {
                _ringX = ringSrc.center.x;
                _ringY = ringSrc.y + ringSrc.height * SparkleFx.FlatRingCy;
                _ringRx = ringSrc.width * SparkleFx.FlatRingRx;
                _ringRy = ringSrc.height * SparkleFx.FlatRingRy;
            }

            int drawn = 0, drawn1 = 0;
            LastMaxRectPx = 0f;
            LastXMin = float.MaxValue;
            LastXMax = float.MinValue;
            int n2 = Layer2Count(rows);
            // b100: pass 0 paints the rising glints (they leave the button's top curve), then
            // the button is put back over them from the held face, so they fly up from BEHIND
            // it. The full-screen veil and every later glint (descent, hold, fall) go after,
            // so the wash and the covering sparkle still sit on top of everything.
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1)
                {
                    if (veil > 0.005f)
                    {
                        // Base: warm cream, never pure white. Then a soft honey glaze in the
                        // same uniform rect, so the colour is layered but every row matches.
                        GUI.color = new Color(1f, 0.93f, 0.78f, veil * 0.85f);
                        GUI.DrawTexture(screen, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                        GUI.color = new Color(1f, 0.82f, 0.55f, veil * 0.35f);
                        GUI.DrawTexture(screen, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                    }
                }
                for (int q = 0; q < n + n2; q++)
                {
                    bool l2 = q >= n;
                    int key, mk;
                    float lx, ly, body;
                    if (!l2)
                    {
                        key = q; mk = q >> 1;
                        Cell(q, rows, W, H, out lx, out ly);
                        body = cell * 0.95f * SizeK(key);
                    }
                    else
                    {
                        int j2 = q - n;
                        Cell2(j2, rows, W, H, out lx, out ly, out mk);
                        key = 5000 + j2;
                        body = cell * 0.55f * SizeK(key);
                    }
                    float eAge = t - EmitWindow * H01(key, 91);
                    bool rising = eAge >= 0f && eAge < RiseDur;
                    if (rising != (pass == 0)) continue;
                    var c = Tint(mk);
                    if (pass == 0 && t < 0.98f)
                    {
                        for (int g = Ghosts; g >= 1; g--)
                        {
                            float tg = t - g * GhostStep;
                            if (!Flight(key, lx, ly, body, l2, tg, W, H, ox, oy, out float gx, out float gy, out float gs, out float ga, out float gb, out _))
                                continue;
                            if (gb > 0.3f) continue; // streak the rise only
                            float k = 1f - g / (float)(Ghosts + 1);
                            SparkleFx.DrawDepthGlint(gx, gy, gs * (0.55f + 0.4f * k), ga * 0.55f * k, 0f, 0.3f, c, star, glowTex);
                        }
                    }
                    if (!Flight(key, lx, ly, body, l2, t, W, H, ox, oy, out float x, out float y, out float sz, out float a, out float bl, out float sf))
                        continue;
                    PaintMaskGlint(x, y, sz, a, bl, sf, c, RotOf(key), star, glowTex);
                    if (a > 0.1f && sz > LastMaxRectPx) LastMaxRectPx = sz;
                    if (x < LastXMin) LastXMin = x;
                    if (x > LastXMax) LastXMax = x;
                    drawn++;
                    if (!l2) drawn1++;
                }
                if (pass == 0 && faceShown && _btn.width > 2f) PaintButtonCap(screen);
            }
            GUI.color = prev;
            if (holding && drawn1 >= n && veil >= VeilPeak * 0.99f)
            {
                _covered = true;
                _shift += rawT - CoverAt; // clock resumes at CoverAt on the next frame
            }
            LastMaxBodyPx = LastMaxRectPx * StarBody;
            return drawn;
        }
    }
}

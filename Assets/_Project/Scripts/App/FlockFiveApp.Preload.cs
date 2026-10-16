using System.Diagnostics;
using UnityEngine;

namespace FlockFive
{
    // Splash-time preload and frame pacing.
    //
    // Preload: everything the first minutes of play would otherwise build on the frame
    // it is first needed (synthesized sound banks, the coach glove, the badger frames,
    // the tutorial dim/glow/ripple art) is built here a slice at a time while the splash
    // is idle, under a small per-frame time budget. Each piece goes through the same
    // shared cache its normal caller uses, so nothing is built twice and nothing looks
    // or sounds different. Anything not warmed yet when it is needed is still built on
    // demand exactly as before.
    //
    // Frame pacing: see FramePace. The target is re-read on resume because the display
    // mode can change while the app is away.
    public sealed partial class FlockFiveApp
    {
        // Per-frame work budget for the splash preload (milliseconds).
        const double PreloadBudgetMs = 3.0;
        // Splash repaints to let pass first, so the splash's own first frames stay clean.
        const int PreloadDelayFrames = 6;
        // Glove rows per slice. Build 83 item 8: 12 rows measured ~29ms per slice (frames
        // 44-46 hitched); 2 rows is ~5ms and the budget loop stops after it.
        const int PreloadGloveRows = 2;

        // Build 83 item 8. Cold start: the first home frames load every home sprite, lay out
        // the rails (first poker/hive draw ~40ms), and the splash preload ran 25-30ms steps
        // (sound clips, glove rows, honeycomb). Measured worst after home appeared: 566ms.
        // Fix: the home is laid out under a cover in the Unity splash color while the
        // preload runs with a large budget, then the cover fades and the home appears warm.
        // ALL of the preload runs in the gap after the Unity splash, behind the ZFX studio card.
        // Budget is per frame so the sparkle stays smooth; the plain launch-color cover is only
        // the fallback when the logo is missing. The home is laid out underneath.
        const int WarmCoverDelayFrames = 2;
        const double WarmCoverBudgetMs = 40.0; // dark screen only, nothing animates
        const float WarmCoverMaxSec = 6.0f;
        const float WarmCoverFadeSec = 0.25f;
        static readonly Color WarmCoverColor = new Color(0.12156863f, 0.12156863f, 0.1254902f, 1f);
        static float _warmCoverT0 = -1f;
        static float _warmCoverFadeT0 = -1f;
        // Build 85 (Brandon): a fixed breath after the studio card ends, before the splash
        // fades in, on top of any load time (loading already finished on the dark screen).
        public const float CardEndHoldSec = 0.5f;
        static int _warmCoverPaints;
        static bool _warmCoverOff;
        static bool _warmHeavyDone;
        static int _warmHeavyStep;
        static float _loadShown;
        static float _loadArtT0 = -1f;

#if UNITY_EDITOR
        public static float ShotLoadingProgress = -1f; // studio card time (s) for stills
#endif


        // Boot studio card (5:17 PM): the Unity splash fades to warm white, the ZFX Gaming
        // logo fades in unaltered, and a warm gold sparkle (soft darker edge so it reads on
        // white) runs along the Z's top swoosh left to right with trailing glitter. All the
        // preload runs underneath; held at least StudioMinSec, then it fades into the game.
        int _preloadOst;
        public const float StudioWhiteSec = 0.15f;
        public const float StudioLogoIn = 0.05f, StudioLogoSec = 0.30f;
        // Sparkle timing lives in StudioSync (jingle v4). These two remain for the tests.
        public const float StudioSparkIn = 0.004f, StudioSparkSec = 1.996f;
        public const float StudioMinSec = 3.0f; // the whole jingle: pop 2.000s, logo out by 3.000s
        static readonly Color StudioWhite = new Color(0.992f, 0.984f, 0.965f, 1f);

        // Top EDGE of the Z's upper swoosh in logo UV (x right, y down), left to right. Sampled
        // from the logo's alpha: the first opaque row (alpha > 0.5) of every column, outline
        // included, so the glint rides the real curve and slant up to the tip.
        static readonly Vector2[] ZSwoosh =
        {
            new Vector2(0.0220f, 0.2640f), new Vector2(0.0344f, 0.2608f), new Vector2(0.0540f, 0.2592f),
            new Vector2(0.0732f, 0.2542f), new Vector2(0.0928f, 0.2492f), new Vector2(0.1124f, 0.2425f),
            new Vector2(0.1320f, 0.2367f), new Vector2(0.1512f, 0.2292f), new Vector2(0.1708f, 0.2217f),
            new Vector2(0.1904f, 0.2125f), new Vector2(0.2100f, 0.2025f), new Vector2(0.2292f, 0.1925f),
            new Vector2(0.2488f, 0.1825f), new Vector2(0.2684f, 0.1708f), new Vector2(0.2880f, 0.1583f),
            new Vector2(0.3072f, 0.1458f), new Vector2(0.3268f, 0.1333f), new Vector2(0.3464f, 0.1183f),
            new Vector2(0.3660f, 0.1033f), new Vector2(0.3852f, 0.0883f), new Vector2(0.4048f, 0.0733f),
            new Vector2(0.4244f, 0.0592f), new Vector2(0.4270f, 0.0578f)
        };

        // Point at fraction u of the path. The producer's traced path (StudioSync.PathX/Y, 96
        // points evenly spaced by arc length, offset onto the outline) replaces the v3 samples.
        public static Vector2 SwooshPoint(float u) => StudioSync.PathAt(u);

        public static int SwooshSamples => ZSwoosh.Length;
        public static Vector2 SwooshSample(int i) => ZSwoosh[i];

        public static Rect StudioLogoRect(float W, float H, Sprite logo)
        {
            float aspect = logo != null ? logo.rect.width / Mathf.Max(1f, logo.rect.height) : 2.083f;
            float w = Mathf.Round(Mathf.Min(W * 0.80f, H * 0.36f * aspect));
            float h = Mathf.Round(w / aspect);
            return new Rect(Mathf.Round((W - w) * 0.5f), Mathf.Round((H - h) * 0.46f), w, h);
        }

        // Jingle v4 (Resources/Audio/zfx_jingle, 3.000s) starts on the frame the card shows. The
        // card animation is driven by the AUDIO's playback time through StudioSync (generated
        // from the producer's sparkle-sync.json), so the sparkle stays on the music. Missing
        // file = silent, clocked by real time. Pending Brandon's listen approval.
        public const string StudioJinglePath = "Audio/zfx_jingle";
        public static float StudioPopAt => StudioSync.PopT;
        static bool _jingleTried;
        static bool _birdsWarm;
        static AudioSource _jingleSrc;

        static void StudioJingle()
        {
            if (_jingleTried) return;
            _jingleTried = true;
#if UNITY_EDITOR
            if (EditorShotLive || ShotLoadingProgress >= 0f) return;
#endif
            var clip = Resources.Load<AudioClip>(StudioJinglePath);
            if (clip == null) return;
            var go = new GameObject("StudioJingle");
            Object.DontDestroyOnLoad(go);
            _jingleSrc = go.AddComponent<AudioSource>();
            _jingleSrc.playOnAwake = false;
            _jingleSrc.spatialBlend = 0f;
            _jingleSrc.volume = 0.9f;
            _jingleSrc.clip = clip;
            _jingleSrc.Play();
            Object.Destroy(go, clip.length + 0.5f);
        }

        // Card time: the jingle's playback position while it plays, else time since the card showed.
        static float StudioClock(float since)
        {
            if (_jingleSrc != null && _jingleSrc.isPlaying && _jingleSrc.time > 0f) return _jingleSrc.time;
            return since;
        }

        bool DrawLoadingScreen(float a, float since)
        {
            var logo = SpriteCatalog.StudioLogo;
            if (logo == null) return false;
            StudioJingle();
            if (!_birdsWarm) { _birdsWarm = true; StudioBirds.Warm(); }
            float t = StudioClock(since);
            float W = Screen.width, H = Screen.height;
            var keep = GUI.color;
            var dark = WarmCoverColor; dark.a = a;
            GUI.color = dark;
            GUI.DrawTexture(new Rect(0f, 0f, W, H), Texture2D.whiteTexture);
            var white = StudioWhite;
            white.a = a * Mathf.SmoothStep(0f, 1f, since / StudioWhiteSec);
            GUI.color = white;
            GUI.DrawTexture(new Rect(0f, 0f, W, H), Texture2D.whiteTexture);
            float la = Mathf.SmoothStep(0f, 1f, (since - StudioLogoIn) / StudioLogoSec);
            la *= 1f - Mathf.SmoothStep(0f, 1f, (t - StudioSync.FadeStart) / (StudioSync.FadeEnd - StudioSync.FadeStart));
            la *= a;
            var r = StudioLogoRect(W, H, logo);
            // Birds v5 halo: back arc under the logo, front arc over it, under the sparkle.
            // Their own fade-in from frame 0 and the card fade (x(3.0-t)/0.3) are in StudioBirds.
            StudioBirds.Draw(r, t, true, a);
            if (la > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, la);
                DrawSprite(r, logo, true);
            }
            StudioBirds.Draw(r, t, false, a);
            if (la > 0.01f) DrawSwooshSparkle(r, t, la);
            GUI.color = keep;
            return true;
        }

        void DrawSwooshSparkle(Rect r, float t, float a)
        {
            var glow = SpriteCatalog.Glow;
            Texture gt = glow != null && glow.texture != null ? glow.texture : Texture2D.whiteTexture;
            var dot = LevelCurtainDot();
            float unit = r.width * 0.017f;
            float t0 = StudioSync.KeyT[0];
            float bright = Mathf.Clamp01(StudioSync.Bright(t));
            float flash = StudioSync.Flash(t, out bool hook);
            float u = StudioSync.U(t);
            bool travelling = t >= t0 && t < StudioSync.PopT;
            float fadeIn = Mathf.Clamp01((t - t0) / 0.12f);

            // Glitter trail: sampled back along the path in time, so it bunches where the star is
            // slow and streaks where it speeds up into the tip. Stronger than v3 for phone size.
            if (t >= t0)
            {
                float tail = travelling ? 1f : Mathf.Clamp01(1f - (t - StudioSync.PopT) / 0.45f);
                for (int i = 0; i < 40; i++)
                {
                    float lagT = t - 0.012f - i * 0.018f;
                    if (lagT < t0 || lagT > StudioSync.PopT) continue;
                    float tu = StudioSync.U(lagT);
                    float h1 = Mathf.Repeat(i * 0.6180339f, 1f);
                    var p = SwooshPoint(tu);
                    float age = (t - lagT);
                    float fade = Mathf.Exp(-age / 0.32f) * tail * (0.55f + 0.45f * bright);
                    if (fade < 0.03f) continue;
                    float px = r.x + p.x * r.width + (h1 - 0.5f) * unit * 2.0f;
                    float py = r.y + p.y * r.height + (h1 * 1.0f + age * 6f) * unit * 0.7f;
                    float sz = unit * (0.75f + 0.9f * Mathf.Repeat(h1 * 7.3f, 1f)) * (0.6f + 0.4f * fade);
                    float tw = 0.65f + 0.35f * Mathf.Sin(t * 21f + i * 2.1f);
                    GUI.color = new Color(0.58f, 0.34f, 0.03f, a * fade * 0.75f);
                    GUI.DrawTexture(new Rect(px - sz * 0.72f, py - sz * 0.72f, sz * 1.44f, sz * 1.44f), dot, ScaleMode.StretchToFill, true);
                    GUI.color = new Color(1f, 0.80f, 0.22f, a * Mathf.Clamp01(fade * tw * 1.35f));
                    GUI.DrawTexture(new Rect(px - sz * 0.5f, py - sz * 0.5f, sz, sz), dot, ScaleMode.StretchToFill, true);
                }
            }

            // The traveling star, per frame from StudioSync: pointed gold 4-point star plus a
            // 45-degree layer (8-point twinkle) and a soft core. Angle, scale, ray length (hook
            // flares baked in) and sharpness follow the jingle. It stays at the tip through the
            // burst so it never shrinks at 2.000s; it is absorbed as the burst decays.
            float burst = StudioSync.Burst(t, out float grow);
            // At exactly PopT the burst is still 0 (it grows from there): keep the star at full
            // strength over the grow so there is no empty or shrunken frame at 2.000s.
            float starHold = t < StudioSync.PopT ? fadeIn : Mathf.Max(burst, 1f - Mathf.Clamp01((t - StudioSync.PopT - 0.05f) / 0.03f));
            if (t >= t0 && starHold > 0.01f)
            {
                var p = SwooshPoint(travelling ? u : 1f);
                float px = r.x + p.x * r.width, py = r.y + p.y * r.height;
                float keepA = starHold;
                DrawPointedStar(new Vector2(px, py), r.width, StudioSync.Angle(t), StudioSync.Scale(t),
                    StudioSync.Ray(t), StudioSync.Sharp(t), a * keepA * Mathf.Clamp01(0.45f + 0.55f * bright + flash * 0.3f), gt, dot);
            }
            if (burst > 0.01f) DrawSwooshPop(r, t, burst, grow, a, gt, dot);
        }

        // Tapered ray texture (horizontal, base at x=0, point at x=1). Higher sharpness = thinner,
        // more needle-like taper. Four bakes, nearest is used.
        static Texture2D[] _rayTex;
        static Texture2D RayTex(float sharp)
        {
            if (_rayTex == null) _rayTex = new Texture2D[4];
            int k = Mathf.Clamp(Mathf.RoundToInt((sharp - 0.35f) / 0.6f * 3f), 0, 3);
            if (_rayTex[k] != null) return _rayTex[k];
            float pow = 0.8f + k * 0.6f;
            const int w = 128, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "StudioRay" + k, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = (x + 0.5f) / w;
                float half = Mathf.Pow(1f - fx, pow) * 0.5f;
                float dy = Mathf.Abs((y + 0.5f) / h - 0.5f);
                float edge = (half - dy) * h;
                float al = Mathf.Clamp01(edge * 0.9f + 0.5f);
                px[y * w + x] = new Color32(255, 255, 255, (byte)(255f * al));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _rayTex[k] = tex;
            return tex;
        }

        void DrawRay(Vector2 c, float ang, float len, float wid, Texture2D tex, Color col)
        {
            if (len < 1f) return;
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, c);
            GUI.color = col;
            GUI.DrawTexture(new Rect(c.x, c.y - wid * 0.5f, len, wid), tex, ScaleMode.StretchToFill, true);
            GUI.matrix = m;
        }

        void DrawPointedStar(Vector2 c, float logoW, float angle, float scale, float ray, float sharp, float al, Texture gt, Texture2D dot)
        {
            if (al < 0.01f) return;
            float nom = StudioSync.StarNominalPx * logoW / 1024f * scale;
            float len = nom * 0.5f * ray;
            float wid = nom * 0.30f * (1.25f - sharp);
            var tex = RayTex(sharp);
            float g = nom * 1.6f;
            GUI.color = new Color(0.88f, 0.58f, 0.10f, al * 0.35f);
            GUI.DrawTexture(new Rect(c.x - g * 0.5f, c.y - g * 0.5f, g, g), gt, ScaleMode.StretchToFill, true);
            // Dark amber edge pass first so the gold reads on white, then the gold.
            for (int pass = 0; pass < 2; pass++)
            {
                var col = pass == 0 ? new Color(0.55f, 0.32f, 0.03f, al * 0.6f) : new Color(1f, 0.84f, 0.32f, al);
                float grow = pass == 0 ? 1.12f : 1f;
                for (int i = 0; i < 4; i++)
                {
                    DrawRay(c, angle + i * 90f, len * grow, wid * grow * 1.15f, tex, col);
                    DrawRay(c, angle + 45f + i * 90f, len * 0.55f * grow, wid * 0.8f * grow, tex, col);
                }
            }
            GUI.color = new Color(1f, 1f, 0.92f, al);
            float core = nom * 0.28f;
            GUI.DrawTexture(new Rect(c.x - core * 0.5f, c.y - core * 0.5f, core, core), dot, ScaleMode.StretchToFill, true);
        }

        // Starburst at the tip: 16 needle rays, 8 at the star's angle plus 8 shorter at 22.5 deg.
        // Grows (ease-out cubic), holds, decays with the audio.
        void DrawSwooshPop(Rect r, float t, float level, float grow, float a, Texture gt, Texture2D dot)
        {
            var p = SwooshPoint(1f);
            var c = new Vector2(r.x + p.x * r.width, r.y + p.y * r.height);
            float al = a * level;
            float nom = StudioSync.StarNominalPx * r.width / 1024f;
            float g = nom * 4.2f * (0.55f + 0.6f * grow);
            GUI.color = new Color(0.88f, 0.58f, 0.10f, al * 0.40f);
            GUI.DrawTexture(new Rect(c.x - g * 0.5f, c.y - g * 0.5f, g, g), gt, ScaleMode.StretchToFill, true);
            float ang = StudioSync.Angle(Mathf.Min(t, StudioSync.PopT));
            var tex = RayTex(0.95f);
            float k = t - StudioSync.PopT;
            for (int pass = 0; pass < 2; pass++)
            {
                var col = pass == 0 ? new Color(0.55f, 0.32f, 0.03f, al * 0.55f) : new Color(1f, 0.86f, 0.36f, al);
                float pg = pass == 0 ? 1.1f : 1f;
                for (int i = 0; i < 16; i++)
                {
                    bool longRay = i % 2 == 0;
                    float len = nom * (longRay ? 3.0f : 1.8f) * grow * (0.9f + 0.3f * Mathf.Clamp01(k / 0.6f)) * pg;
                    float wid = nom * (longRay ? 0.16f : 0.11f) * pg;
                    DrawRay(c, ang + i * 22.5f, len, wid, tex, col);
                }
            }
            GUI.color = new Color(1f, 1f, 0.9f, al);
            float cc = nom * 0.7f * (0.7f + 0.5f * grow);
            GUI.DrawTexture(new Rect(c.x - cc * 0.5f, c.y - cc * 0.5f, cc, cc), dot, ScaleMode.StretchToFill, true);
            // Twinkles thrown out around the pop, each blinking once.
            for (int i = 0; i < 6; i++)
            {
                float h = Mathf.Repeat(i * 0.381966f + 0.17f, 1f);
                float an = (i * 61f + 20f) * Mathf.Deg2Rad;
                float dist = nom * (1.8f + 2.2f * h) * (0.4f + 0.6f * Mathf.Clamp01(k / 0.4f));
                float tx = c.x + Mathf.Cos(an) * dist, ty = c.y + Mathf.Sin(an) * dist;
                float blink = Mathf.Sin(Mathf.Clamp01((k - 0.03f * i) / 0.45f) * Mathf.PI);
                if (blink <= 0.01f) continue;
                float ts = nom * (0.4f + 0.35f * h) * blink;
                GUI.color = new Color(0.55f, 0.32f, 0.03f, a * blink * 0.5f);
                GUI.DrawTexture(new Rect(tx - ts * 0.6f, ty - ts * 0.6f, ts * 1.2f, ts * 1.2f), dot, ScaleMode.StretchToFill, true);
                GUI.color = new Color(1f, 0.88f, 0.40f, a * blink);
                GUI.DrawTexture(new Rect(tx - ts * 0.5f, ty - ts * 0.08f, ts, ts * 0.16f), dot, ScaleMode.StretchToFill, true);
                GUI.DrawTexture(new Rect(tx - ts * 0.08f, ty - ts * 0.5f, ts * 0.16f, ts), dot, ScaleMode.StretchToFill, true);
            }
        }

        static Texture2D _sparkDot;
        static Texture2D LevelCurtainDot()
        {
            if (_sparkDot != null) return _sparkDot;
            const int n = 32;
            _sparkDot = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "StudioSpark", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float al = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(255f * al * al));
            }
            _sparkDot.SetPixels32(px);
            _sparkDot.Apply(false, true);
            return _sparkDot;
        }


        static bool WarmCoverUp => !_warmCoverOff && _warmCoverFadeT0 < 0f;

        // Drawn last in OnGUI. Lifts once the preload is done (or the cap) and the home has
        // painted a few times under it.
        void DrawWarmCover()
        {
#if UNITY_EDITOR
            if (ShotLoadingProgress >= 0f)
            {
                if (Event.current != null && Event.current.type == EventType.Repaint)
                DrawLoadingScreen(1f, ShotLoadingProgress);
                return;
            }
#endif
            if (_warmCoverOff) { StartupTrace.MarkVisible(); return; }
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
#if UNITY_EDITOR
            if (EditorShotLive) { _warmCoverOff = true; return; }
#endif
            float now = Time.realtimeSinceStartup;
            if (_warmCoverT0 < 0f) _warmCoverT0 = now;
            _warmCoverPaints++;
            // Brandon 10:42 PM: ALL loading happens on the dark screen after Made with Unity; the
            // studio card (and its jingle) starts only once the preload is done, and nothing
            // loads under the card. The card then plays its full StudioMinSec and lifts.
            // Card bird textures are built on the dark screen too (one pose per frame).
            bool birdsReady = _loadArtT0 >= 0f || StudioBirds.WarmStep();
            if (_loadArtT0 < 0f && birdsReady && (_preloadDone || !_splash || _home != HomeFace.Splash || now - _warmCoverT0 > WarmCoverMaxSec)
                && _warmCoverPaints >= 4)
            {
                _loadArtT0 = now;
                UnityEngine.Debug.Log("FF_CARD_AT dark=" + ((now - _warmCoverT0) * 1000f).ToString("0") + "ms preloadDone=" + _preloadDone);
            }
            if (_warmCoverFadeT0 < 0f && _loadArtT0 >= 0f && now - _loadArtT0 >= StudioMinSec + CardEndHoldSec)
                _warmCoverFadeT0 = now;
            float a = 1f;
            if (_warmCoverFadeT0 >= 0f)
            {
                a = 1f - Mathf.SmoothStep(0f, 1f, (now - _warmCoverFadeT0) / WarmCoverFadeSec);
                if (a < 1f) StartupTrace.MarkVisible();
                if (a <= 0f) { _warmCoverOff = true; return; }
            }
            if (_loadArtT0 >= 0f && DrawLoadingScreen(a, now - _loadArtT0)) return;
            // Fallback: the plain launch-color cover.
            var keep = GUI.color;
            var c = WarmCoverColor;
            c.a = a;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = keep;
        }

        public static bool WarmCoverLive => !_warmCoverOff;
        // b112 (Brandon): while the studio card / loading cover is up the splash is not ready and not tappable.
        // The shared input gate (SwallowResumePointer, Pressed) eats every press until the cover is gone.
        public static bool StartupInputHeld => WarmCoverLive; // editor shots set _warmCoverOff, so no editor-only term here (EditorShotLive is #if UNITY_EDITOR)
        // The studio card is up (or still to come): splash music waits until it lifts.
        public static bool StudioCardHold => !_warmCoverOff && _warmCoverFadeT0 < 0f;

        static readonly string[] _preloadBadgerArt =
        {
            "badger_idle_a", "badger_idle_b", "badger_crouch", "badger_hop_a", "badger_hop_b",
            "badger_leap_0", "badger_leap_1", "badger_leap_3", "badger_shrug",
            "hive_idle", "hive_swiped", "honey_splat",
            "bee_1", "bee_2", "bee_3", "bee_4", "bee_5",
        };

        static readonly Stopwatch _preloadClock = new Stopwatch();
        static int _preloadFrames;
        static int _preloadBadger;
        static int _preloadTex;
        static bool _preloadDone;
        static int _dealWarm;
        static int _nextBgFor = -1;
        static int _albumWarmedPage = int.MinValue;
        static bool _albumWarmDue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPreload()
        {
            _preloadFrames = 0;
            _preloadBadger = 0;
            _preloadTex = 0;
            _preloadDone = false;
            _dealWarm = 0;
            _nextBgFor = -1;
            _albumWarmedPage = int.MinValue;
            _albumWarmDue = false;
            _warmCoverT0 = -1f;
            _warmCoverFadeT0 = -1f;
            _warmCoverPaints = 0;
            _warmCoverOff = false;
            _warmHeavyDone = false;
            _warmHeavyStep = 0;
            _jingleTried = false;
            _loadShown = 0f;
            _loadArtT0 = -1f;
        }

        public static bool PreloadDone => _preloadDone;

        // One budgeted slice per repaint. Splash jobs (including HoneyArt, one bake
        // at a time) run on the home face. After that, a single phase slice warms
        // only the next season background, the next deal's colors, or the album
        // page that just popped. Themes are not pulled in here.
        void TickPreload()
        {
            if (Ads.IsShowing) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            _preloadClock.Restart();
            // Nothing loads under the studio card: it runs only on the dark screen before it.
            if (WarmCoverUp && _loadArtT0 >= 0f) { _preloadClock.Stop(); return; }
            double budget = WarmCoverUp ? WarmCoverBudgetMs : PreloadBudgetMs;
            if (!_preloadDone && _splash && _home == HomeFace.Splash)
            {
                if (++_preloadFrames <= (WarmCoverUp ? WarmCoverDelayFrames : PreloadDelayFrames))
                {
                    _preloadClock.Stop();
                    return;
                }
                // Heavy one-shot pieces (25ms each) go first, while the warm cover is up.
                // Heavy one-shot pieces, one per frame so the hummingbird keeps flapping.
                if (!_warmHeavyDone)
                {
                    using (StartupTrace.Begin("Pre.Heavy" + _warmHeavyStep))
                    {
                        switch (_warmHeavyStep++)
                        {
                            case 0: HoneycombTex(); break;
                            case 1: Sfx.WarmLoneClips(); break;
                            default: Sfx.WarmGateClip(); _warmHeavyDone = true; break;
                        }
                    }
                    _preloadClock.Stop();
                    return;
                }
                bool any = true;
                while (any && _preloadClock.Elapsed.TotalMilliseconds < budget)
                {
                    any = false;
                    if (!Sfx.WarmDone) { using (StartupTrace.Begin("Pre.Sfx")) Sfx.WarmStep(); any = true; }
                    if (_preloadClock.Elapsed.TotalMilliseconds >= budget) break;
                    if (!GloveWarmDone) { using (StartupTrace.Begin("Pre.Glove")) GloveWarmRows(PreloadGloveRows); any = true; }
                    if (_preloadClock.Elapsed.TotalMilliseconds >= budget) break;
                    if (!SfxLibrary.WarmDone) { using (StartupTrace.Begin("Pre.SfxLib")) SfxLibrary.WarmStep(); any = true; }
                    if (_preloadClock.Elapsed.TotalMilliseconds >= budget) break;
                    if (_preloadBadger < _preloadBadgerArt.Length)
                    {
                        // Load plus an offscreen, near-invisible draw so the texture is
                        // uploaded now rather than on the leap's first frame.
                        using (StartupTrace.Begin("Pre.Badger")) TouchPokerSprite(SpriteCatalog.BadgerArt(_preloadBadgerArt[_preloadBadger++]));
                        any = true;
                    }
                    if (_preloadClock.Elapsed.TotalMilliseconds >= budget) break;
                    if (_preloadTex < 4)
                    {
                        using (StartupTrace.Begin("Pre.Tex" + _preloadTex))
                        switch (_preloadTex++)
                        {
                            case 0: CoachDimTex(); break;
                            case 1: CoachGlowSprite(); break;
                            case 2: CoachRippleSprite(); break;
                            default: HoneycombTex(); break;
                        }
                        any = true;
                    }
                    if (_preloadClock.Elapsed.TotalMilliseconds >= budget) break;
                    if (!HoneyArt.WarmDone) { using (StartupTrace.Begin("Pre.Honey")) HoneyArt.WarmStep(); any = true; }
                    // b86: the Daily wheel disc/highlight/pointer bake on the dark screen too.
                    if (_wheelDisc == null && _preloadClock.Elapsed.TotalMilliseconds < budget) { using (StartupTrace.Begin("Pre.Wheel")) WarmWheelArt(); any = true; }
                    if (_preloadOst < OstrichPaint.WarmCount && _preloadClock.Elapsed.TotalMilliseconds < budget)
                    {
                        // Painted ostrich parts load and upload behind the studio card.
                        using (StartupTrace.Begin("Pre.Ostrich")) OstrichPaint.Warm(_preloadOst++);
                        any = true;
                    }
                }
                if (!any) _preloadDone = true;
            }
            if (_preloadClock.Elapsed.TotalMilliseconds < budget)
                TickPreloadPhase();
            double preloadMs = _preloadClock.Elapsed.TotalMilliseconds;
            _preloadClock.Stop();
            if (preloadMs >= 1.0)
                StartupTrace.Note("Preload", preloadMs, Time.frameCount);
        }

        // One slice. Never the whole catalog: the next backdrop, the colors a
        // deal can show, or the album page under the hive button that just popped.
        void TickPreloadPhase()
        {
            if (_hivePopping) _albumWarmDue = true;
            if (_albumWarmDue && _albumWarmedPage != _hivePage)
            {
                TouchPokerSprite(SpriteCatalog.Bee);
                if (!HoneyArt.WarmDone) HoneyArt.WarmStep();
                _albumWarmedPage = _hivePage;
                _albumWarmDue = false;
                return;
            }
            if (FinaleShow.Playing)
            {
                int next = LevelData.DisplayNumber + 1;
                if (_nextBgFor != next)
                {
                    SpriteCatalog.WarmNextScene(GardenSeason.ForLevel(next));
                    _nextBgFor = next;
                }
                return;
            }
            if (_splash && _home == HomeFace.Splash && _dealWarm < SpriteCatalog.DealColorSteps)
            {
                TouchPokerSprite(SpriteCatalog.WarmDealColor(_dealWarm));
                _dealWarm++;
            }
        }

        // Frame-rate target for the panel the app is on. Mobile ignores vSyncCount; it is
        // zeroed so the editor and desktop builds follow targetFrameRate too.
        static void ApplyFramePace()
        {
            QualitySettings.vSyncCount = 0;
            double hz = Screen.currentResolution.refreshRateRatio.value;
            int want = FramePace.Target(hz, FramePace.HighRefresh);
            if (Application.targetFrameRate != want) Application.targetFrameRate = want;
        }
    }
}

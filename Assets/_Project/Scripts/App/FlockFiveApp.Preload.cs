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
        const int WarmCoverDelayFrames = 2;
        const double WarmCoverBudgetMs = 120.0;
        const float WarmCoverMaxSec = 1.2f;
        const float WarmCoverFadeSec = 0.25f;
        static readonly Color WarmCoverColor = new Color(0.12156863f, 0.12156863f, 0.1254902f, 1f);
        static float _warmCoverT0 = -1f;
        static float _warmCoverFadeT0 = -1f;
        static int _warmCoverPaints;
        static bool _warmCoverOff;
        static bool _warmHeavyDone;

        static bool WarmCoverUp => !_warmCoverOff && _warmCoverFadeT0 < 0f;

        // Drawn last in OnGUI. Lifts once the preload is done (or the cap) and the home has
        // painted a few times under it.
        void DrawWarmCover()
        {
            if (_warmCoverOff) { StartupTrace.MarkVisible(); return; }
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
#if UNITY_EDITOR
            if (EditorShotLive) { _warmCoverOff = true; return; }
#endif
            float now = Time.realtimeSinceStartup;
            if (_warmCoverT0 < 0f) _warmCoverT0 = now;
            _warmCoverPaints++;
            if (_warmCoverFadeT0 < 0f)
            {
                bool warm = _warmHeavyDone || _preloadDone || !_splash || _home != HomeFace.Splash;
                if ((_warmCoverPaints >= 4 && warm) || now - _warmCoverT0 > WarmCoverMaxSec)
                    _warmCoverFadeT0 = now;
            }
            float a = 1f;
            if (_warmCoverFadeT0 >= 0f)
            {
                a = 1f - Mathf.SmoothStep(0f, 1f, (now - _warmCoverFadeT0) / WarmCoverFadeSec);
                if (a < 1f) StartupTrace.MarkVisible();
                if (a <= 0f) { _warmCoverOff = true; return; }
            }
            var keep = GUI.color;
            var c = WarmCoverColor;
            c.a = a;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = keep;
        }

        public static bool WarmCoverLive => !_warmCoverOff;

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
            double budget = WarmCoverUp ? WarmCoverBudgetMs : PreloadBudgetMs;
            if (!_preloadDone && _splash && _home == HomeFace.Splash)
            {
                if (++_preloadFrames <= (WarmCoverUp ? WarmCoverDelayFrames : PreloadDelayFrames))
                {
                    _preloadClock.Stop();
                    return;
                }
                // Heavy one-shot pieces (25ms each) go first, while the warm cover is up.
                if (WarmCoverUp && !_warmHeavyDone)
                {
                    using (StartupTrace.Begin("Pre.Heavy"))
                    {
                        HoneycombTex();
                        Sfx.WarmLoneClips();
                        Sfx.WarmGateClip();
                    }
                    _warmHeavyDone = true;
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

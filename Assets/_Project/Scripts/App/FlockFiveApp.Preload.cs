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
        // Glove rows per slice; a slice is well under a millisecond.
        const int PreloadGloveRows = 12;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPreload()
        {
            _preloadFrames = 0;
            _preloadBadger = 0;
            _preloadTex = 0;
            _preloadDone = false;
        }

        public static bool PreloadDone => _preloadDone;

        // Splash repaint only (OnGUI). One budgeted slice per frame, round-robin over the
        // jobs so sound and art both finish early.
        void TickPreload()
        {
            if (_preloadDone || Ads.IsShowing) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            if (++_preloadFrames <= PreloadDelayFrames) return;
            _preloadClock.Restart();
            bool any = true;
            while (any && _preloadClock.Elapsed.TotalMilliseconds < PreloadBudgetMs)
            {
                any = false;
                if (!Sfx.WarmDone) { Sfx.WarmStep(); any = true; }
                if (_preloadClock.Elapsed.TotalMilliseconds >= PreloadBudgetMs) break;
                if (!GloveWarmDone) { GloveWarmRows(PreloadGloveRows); any = true; }
                if (_preloadClock.Elapsed.TotalMilliseconds >= PreloadBudgetMs) break;
                if (!SfxLibrary.WarmDone) { SfxLibrary.WarmStep(); any = true; }
                if (_preloadClock.Elapsed.TotalMilliseconds >= PreloadBudgetMs) break;
                if (_preloadBadger < _preloadBadgerArt.Length)
                {
                    // Load plus an offscreen, near-invisible draw so the texture is
                    // uploaded now rather than on the leap's first frame.
                    TouchPokerSprite(SpriteCatalog.BadgerArt(_preloadBadgerArt[_preloadBadger++]));
                    any = true;
                }
                if (_preloadClock.Elapsed.TotalMilliseconds >= PreloadBudgetMs) break;
                if (_preloadTex < 4)
                {
                    switch (_preloadTex++)
                    {
                        case 0: CoachDimTex(); break;
                        case 1: CoachGlowSprite(); break;
                        case 2: CoachRippleSprite(); break;
                        default: HoneycombTex(); break;
                    }
                    any = true;
                }
            }
            _preloadClock.Stop();
            if (!any) _preloadDone = true;
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

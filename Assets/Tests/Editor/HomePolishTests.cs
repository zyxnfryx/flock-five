#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace FlockFive.Editor
{
    // Shipping polish. Back can leave during the bet coach. Goldie stays off the
    // VIP ribbon. Flip hints hide while the upgrade coach is up. The upgrade glove
    // stays on the glass. Top controls sit below a phone safe-area inset.
    public static class HomePolishTests
    {
        const string Out = "/tmp/flock-five-home-polish-tests.txt";

        static MethodInfo Method(System.Type type, string name, params System.Type[] args)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var found = type.GetMethod(name, flags, null, args, null);
            if (found == null)
                throw new System.MissingMethodException(type.FullName + "." + name);
            return found;
        }

        [UnityEditor.MenuItem("Flock Five/Home Polish Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                System.IO.File.WriteAllText(Out, sb.ToString());
                Debug.Log("[home-polish] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }

            Check("back-answers-during-bet-coach",
                FlockFiveApp.PokerBackAccepts(true, false, true), "bet coach");
            Check("back-stays-shut-during-a-dealt-hand",
                !FlockFiveApp.PokerBackAccepts(true, true, true), "dealt");
            Check("back-stays-shut-for-the-rest-of-the-lesson",
                !FlockFiveApp.PokerBackAccepts(false, false, true), "lesson");
            Check("back-answers-when-nothing-holds-it",
                FlockFiveApp.PokerBackAccepts(false, false, false), "free");

            CheckBird("820", 820, 1180, Check);
            CheckBird("402", 402, 874, Check);
            CheckBird("412", 412, 915, Check);

            Check("flip-hints-hide-for-upgrade-coach",
                !FlockFiveApp.ShowFlipHints(true, false), "upgrade");
            Check("flip-hints-hide-for-album-coach",
                !FlockFiveApp.ShowFlipHints(false, true), "album");
            Check("flip-hints-show-when-both-coaches-are-clear",
                FlockFiveApp.ShowFlipHints(true, true), "clear");

            CheckGlove("402", 402, 874, Check);
            CheckGlove("412", 412, 915, Check);
            CheckGlove("820", 820, 1180, Check);

            CheckMaxxedPutBack(Check);
            Check("scene-change-holds-the-second-tap",
                !FlockFiveApp.SceneInputBlocked(10f, -1f)
                && FlockFiveApp.SceneInputBlocked(10f, 10.40f)
                && FlockFiveApp.SceneInputBlocked(10.39f, 10.40f)
                && !FlockFiveApp.SceneInputBlocked(10.40f, 10.40f)
                && Mathf.Abs(FlockFiveApp.SceneInputSettle - 0.40f) < 0.001f,
                "400 ms");
            CheckSafe("820", 820, 1180, Check);
            CheckSafe("402", 402, 874, Check);
            CheckSafe("412", 412, 915, Check);
            CheckBackVeil("820", 820, 1180, new Vector2(148f, 37f), Check);
            CheckBackVeil("402", 402, 874, new Vector2(58f, 33f), Check);
            CheckBackVeil("412", 412, 915, new Vector2(60f, 34f), Check);
            CheckBadge("820", 820, 1180, Check);
            CheckBadge("402", 402, 874, Check);
            CheckBadge("412", 412, 915, Check);
            CheckUpgradeTiming(Check);
            CheckFlipOccluder(Check);
            Check("card-render-defers-inside-camera",
                CardStage.MustDeferRender(true) && !CardStage.MustDeferRender(false), "nested");
            CheckCardRender(Check);
            CheckFmod(Check);
            CheckUnityOutputInit(Check);
            CheckEmulatedNoSound(Check);
            CheckOutputThenEmulated(Check);
            CheckInvalidHandle(Check);
            CheckPlayScheduled(Check);
            CheckAudioRetry(Check);
            CheckPauseAndUnmute(Check);
            CheckUnmuteScope(Check);
            CheckScheduledRestore(Check);
            CheckScheduleHousekeeping(Check);
            CheckRebootHolds(Check);
            CheckGardenGlove(Check);
            CheckUpgradeStack(Check);
            CheckPlayerGlove(Check);
            CheckCoachRestart(Check);
            CheckCoachRestartPath(Check);
            CheckRealPress(Check);
            CheckCaptionLeave(Check);
            CheckExitGuards(Check);
            CheckHomeAndPrivacy(Check);
            CheckRestartRecue(Check);
            CheckForcePause(Check);
            CheckHomeTouchGate(Check);
            CheckGloveRoute(Check);
            CheckGloveOneHandle(Check);
            CheckFreezeLeavesRepeatGate(Check);
            CheckHop2KeepsDestination(Check);
            CheckResumeSwallow(Check);
            CheckResumeUpAfterSwallow(Check);
            CheckSwallowedReleaseScope(Check);
            CheckResumeFollowups(Check);
            CheckResumeOtherFingerHome(Check);
            CheckResumeButtonBegan(Check);
            CheckResumeHomeFires(Check);
            CheckResumeRestartFires(Check);
            CheckResumeGearFires(Check);
            CheckResumeArmClears(Check);
            CheckResumeArmClearsOnPause(Check);
            CheckGardenGearGoesToGlove(Check);
            CheckGearFeedsOnHomeFace(Check);
            CheckPointerIdExact(Check);
            CheckPlayerGloveHop(Check);
            CheckResumeFreshAndHeld(Check);
            CheckFreshResumeSurfaces(Check);
            CheckResumeBranches(Check);
            CheckGlovePressOwner(Check);
            CheckRestartClearsCaptions(Check);
            CheckHomeControlsAtSizes(Check);
            CheckHomeChainOneAtATime(Check);
            CheckPauseKeepsHeldId(Check);

            string end = fail == 0 ? "HOME_POLISH_OK" : "HOME_POLISH_FAIL " + fail;
            sb.AppendLine(end);
            System.IO.File.WriteAllText(Out, sb.ToString());
            Debug.Log("[home-polish] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void CheckBird(string tag, int screenW, int screenH, System.Action<string, bool, string> Check)
        {
            PhoneArea.PlayPixels(screenW, screenH, out int w, out int h);
            var safe = new Rect(0f, 0f, w, h);
            var medal = FlockFiveApp.SettledRailIcon(w, h, safe, false, 1);
            var ribbon = FlockFiveApp.SplashNoAdsRibbon(medal);
            float icon = Mathf.Max(48f, medal.width * 0.9f);
            bool covered = !FlockFiveApp.BirdPerchClearsVip(ribbon.center, icon, 6f, 18f, medal);
            var clear = new Vector2(medal.center.x, medal.yMax + icon + 40f);
            bool open = FlockFiveApp.BirdPerchClearsVip(clear, icon, 6f, 18f, medal);
            Check(tag + "-goldie-misses-vip-ribbon", covered && open,
                "medal " + medal + " ribbon " + ribbon);
        }

        static void CheckGlove(string tag, int screenW, int screenH, System.Action<string, bool, string> Check)
        {
            float s = DeviceFit.Ui(screenW, screenH);
            float dh = FlockFiveApp.GloveDh(s);
            float dw = FlockFiveApp.GloveAuditDw(dh);
            var aim = new Vector2(screenW * 0.42f, screenH * 0.62f);
            var off = new Vector2(-dh, aim.y - 10f);
            var start = FlockFiveApp.SeatGloveInside(off, 0f, false, dh, dw, screenW, screenH);
            float overStart = FlockFiveApp.GloveQuadOverflow(start, 0f, false, dw, dh, screenW, screenH);
            float overAim = FlockFiveApp.GloveQuadOverflow(aim, 0f, false, dw, dh, screenW, screenH);
            bool path = true;
            for (int i = 1; i <= 3; i++)
            {
                var p = Vector2.Lerp(start, aim, i / 4f);
                var seated = FlockFiveApp.SeatGloveInside(p, 0f, false, dh, dw, screenW, screenH);
                if (FlockFiveApp.GloveQuadOverflow(seated, 0f, false, dw, dh, screenW, screenH) > 0.5f)
                    path = false;
            }
            Check(tag + "-upgrade-glove-stays-on-screen",
                overStart <= 0.5f && overAim <= 0.5f && path,
                "start " + start + " over " + overStart.ToString("0.0") + " aim " + overAim.ToString("0.0"));
        }

        static void CheckSafe(string tag, int screenW, int screenH, System.Action<string, bool, string> Check)
        {
            float inset = 59f;
            float bottom = 34f;
            var safe = new Rect(0f, bottom, screenW, screenH - inset - bottom);
            float s = DeviceFit.Ui(screenW, screenH);
            var chrome = FlockFiveApp.LayoutPokerChrome(screenW, screenH, safe, s);
            var gear = FlockFiveApp.SettingsGearDrawnRect(screenW, screenH, safe);
            var backDrawn = FlockFiveApp.BackMedalDrawnRect(chrome.Back);
            var back = chrome.Back;
            float shift = FlockFiveApp.AlbumHeaderShift(back, screenW, screenH, safe, s);
            if (shift > 0f) back.y += shift;
            var header = FlockFiveApp.AlbumTitleOuter(back, screenW, screenH, safe, s);
            bool backOk = backDrawn.yMin >= inset - 0.5f;
            bool cogOk = gear.yMin >= inset - 0.5f;
            bool headerOk = header.yMin >= inset + 4f - 0.05f;
            Check(tag + "-top-controls-clear-safe-inset",
                backOk && cogOk && headerOk,
                "inset " + inset + " back " + backDrawn.yMin + " gear " + gear.yMin + " header " + header.yMin);
        }

        static void CheckBackVeil(string tag, int screenW, int screenH, Vector2 reported, System.Action<string, bool, string> Check)
        {
            float s = DeviceFit.Ui(screenW, screenH);
            var safe = new Rect(0f, 0f, screenW, screenH);
            var chrome = FlockFiveApp.LayoutPokerChrome(screenW, screenH, safe, s);
            var back = chrome.Back;
            var deal = new Rect(screenW * 0.55f, screenH * 0.72f, 120f, 120f);
            bool centerEaten = FlockFiveApp.CoachVeilEats(back.center, true, deal, false, true, false, back);
            bool dealtEaten = FlockFiveApp.CoachVeilEats(back.center, true, deal, false, true, true, back);
            bool reportedOk = !back.Contains(reported)
                || !FlockFiveApp.CoachVeilEats(reported, true, deal, false, true, false, back);
            bool emptyEaten = FlockFiveApp.CoachVeilEats(new Vector2(screenW * 0.5f, screenH * 0.5f), true, deal, false, true, false, back);
            Check(tag + "-bet-coach-veil-lets-back",
                !centerEaten && dealtEaten && reportedOk && emptyEaten && back.Contains(back.center),
                "back " + back + " reported " + reported);
        }

        static void CheckBadge(string tag, int screenW, int screenH, System.Action<string, bool, string> Check)
        {
            float s = DeviceFit.Ui(screenW, screenH);
            float hintW = screenW * 0.7f;
            float hintH = 70f * s;
            float hintY = screenH - 220f * s;
            float btnW = screenW * 0.5f;
            float btnH = 92f * s;
            float gap = 20f * s;
            FlockFiveApp.SeatHintAndBadge(screenW, screenH, hintY, hintW, hintH, btnW, btnH, gap, out var hint, out var badge);
            Check(tag + "-maxxed-clears-flip-hint",
                !hint.Overlaps(badge) && badge.yMin >= hint.yMax - 0.5f,
                "hint " + hint + " badge " + badge);
            bool putBack = FlockFiveApp.MaxxedPutBackShows(true, 0.8f, true, true)
                && !FlockFiveApp.FlipHintsVisible(0.8f, true, true, true)
                && !FlockFiveApp.MaxxedPutBackShows(true, 0.8f, false, true);
            Check(tag + "-maxxed-keeps-put-back-hint", putBack, "above the plate");
        }

        static void CheckUpgradeTiming(System.Action<string, bool, string> Check)
        {
            bool shared = false;
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                bool upgrade = FlockFiveApp.UpgradeButtonShows(t, true);
                bool hints = FlockFiveApp.FlipHintsVisible(t, true, true, upgrade);
                if (hints && upgrade) shared = true;
            }
            bool early = FlockFiveApp.FlipHintsVisible(0.45f, true, true, false)
                && !FlockFiveApp.UpgradeButtonShows(0.45f, true);
            bool later = FlockFiveApp.UpgradeButtonShows(0.7f, true)
                && !FlockFiveApp.FlipHintsVisible(0.7f, true, true, true);
            Check("upgrade-not-under-flip-hints", !shared && early && later, "shared " + shared);
            bool skipMax = !FlockFiveApp.UpgradeCoachForCard(false, true);
            bool teach = FlockFiveApp.UpgradeCoachForCard(true, true);
            Check("maxxed-skips-upgrade-coach", skipMax && teach, "skip " + skipMax);
        }

        static void CheckMaxxedPutBack(System.Action<string, bool, string> Check)
        {
            int[] w = { 820, 402, 412 };
            int[] h = { 1180, 874, 915 };
            bool taps = true;
            bool clear = true;
            for (int i = 0; i < w.Length; i++)
            {
                var card = FlockFiveApp.AlbumInspectRect(w[i], h[i]);
                bool jungle = FlockFiveApp.InspectDismissTap(new Vector2(-24f, card.center.y), card);
                bool margin = FlockFiveApp.InspectDismissTap(new Vector2(8f, 8f), card);
                bool onCard = FlockFiveApp.InspectDismissTap(card.center, card);
                if (!jungle || !margin || onCard) taps = false;
                float top = FlockFiveApp.PutBackHintTop(card.yMax, DeviceFit.Ui(w[i], h[i]));
                if (top < card.yMax + FlockFiveApp.PutBackHintClear - 0.5f) clear = false;
            }
            Check("maxxed-put-back-includes-margins", taps, "820 jungle");
            Check("put-back-hint-clears-the-frame", clear, "gap");
        }

        static void CheckFlipOccluder(System.Action<string, bool, string> Check)
        {
            var card = new Rect(40f, 80f, 220f, 300f);
            bool edge = FlockFiveApp.FlipFaceInsideOccluder(card, Mathf.PI * 0.5f);
            bool flat = FlockFiveApp.FlipFaceInsideOccluder(card, 0f);
            var occ = FlockFiveApp.FlipOccluder(card);
            Check("flip-occluder-covers-the-gap",
                edge && flat && Mathf.Abs(occ.width - card.width) < 0.5f && Mathf.Abs(occ.height - card.height) < 0.5f,
                occ.ToString());
        }

        static void CheckCardRender(System.Action<string, bool, string> Check)
        {
            CardStage.ResetRenderGuardForTest();
            bool nested = !CardStage.AllowManualRender(true, false, false);
            bool inside = !CardStage.AllowManualRender(false, true, false);
            bool twice = !CardStage.AllowManualRender(false, false, true);
            CardStage.ResetRenderGuardForTest();
            bool clear = CardStage.AllowManualRender(false, false, false);
            string path = System.IO.Path.Combine(UnityEngine.Application.dataPath, "_Project/Scripts/App/CardStage.cs");
            string src = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "";
            int draw = src.IndexOf("public static bool DrawFlip");
            int set = src.IndexOf("static void Set(");
            string body = draw >= 0 && set > draw ? src.Substring(draw, set - draw) : "";
            bool onGui = body.IndexOf(".Render(") < 0 && body.IndexOf("_defer = true") >= 0;
            Check("card-render-once-and-outside-ongui",
                nested && inside && twice && clear && onGui,
                "nested " + nested + " ongui " + onGui);
        }

        static void CheckFmod(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            bool load = !AudioGuard.IsFmodFailure("FMOD error: failed to load clip")
                && !AudioGuard.NoteFmodFailure("FMOD error: failed to load clip");
            bool decode = !AudioGuard.IsFmodFailure("FMOD_ERR_FORMAT decode error")
                && !AudioGuard.NoteFmodFailure("FMOD failed to decode the sample, error code 25");
            bool otherCode = !AudioGuard.IsFmodFailure("error code 600")
                && !AudioGuard.IsFmodFailure("FMOD returns error code 36 (FMOD_ERR_INVALID_HANDLE)");
            bool quiet = !AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 0;
            bool named = AudioGuard.IsFmodFailure("FMOD_ERR_OUTPUT_INIT")
                && AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
            bool latched = AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1;
            bool again = AudioGuard.NoteFmodFailure("FMOD returns error code 60 (FMOD_ERR_OUTPUT_INIT)");
            bool once = latched && again && AudioGuard.FmodWarnings == 1;
            int onceWarnings = AudioGuard.FmodWarnings;
            AudioGuard.OnAudioConfigurationChanged(true);
            bool config = !AudioGuard.OutputFailed;
            bool rewarn = AudioGuard.NoteFmodFailure("FMOD returns error code 60 (FMOD_ERR_OUTPUT_INIT)") && AudioGuard.FmodWarnings == 2 && AudioGuard.OutputFailed;
            AudioGuard.OnApplicationFocus(true);
            bool focus = !AudioGuard.OutputFailed;
            AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
            AudioGuard.OnApplicationFocus(false);
            AudioGuard.OnApplicationPause(true);
            bool held = AudioGuard.OutputFailed;
            AudioGuard.OnApplicationPause(false);
            bool resume = !AudioGuard.OutputFailed;
            bool plain = !AudioGuard.NoteFmodFailure("InvalidOperationException: already created");
            Check("fmod-output-init-only",
                load && decode && otherCode && quiet && named && plain,
                "warnings " + AudioGuard.FmodWarnings + " failed " + AudioGuard.OutputFailed);
            Check("fmod-output-init-warns-once", once && onceWarnings == 1, "warnings " + onceWarnings);
            Check("fmod-output-recovers",
                config && rewarn && focus && held && resume && !AudioGuard.OutputFailed,
                "config " + config + " focus " + focus + " held " + held + " resume " + resume);
            AudioGuard.ResetForTests();
        }

        static void CheckUnityOutputInit(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            const string line = "FMOD failed to initialize the output device.: \"Error initializing output device. \" (60)";
            bool seen = AudioGuard.IsFmodFailure(line);
            bool first = AudioGuard.NoteFmodFailure(line);
            bool second = AudioGuard.NoteFmodFailure(line);
            bool unity = seen && first && second && AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1;

            AudioGuard.ResetForTests();
            bool phrase = AudioGuard.IsFmodFailure("FAILED TO INITIALIZE THE OUTPUT DEVICE")
                && AudioGuard.NoteFmodFailure("FAILED TO INITIALIZE THE OUTPUT DEVICE")
                && AudioGuard.NoteFmodFailure("FAILED TO INITIALIZE THE OUTPUT DEVICE")
                && AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1;

            AudioGuard.ResetForTests();
            bool paren = AudioGuard.IsFmodFailure("FMOD (60)")
                && AudioGuard.NoteFmodFailure("FMOD (60)")
                && AudioGuard.NoteFmodFailure("FMOD (60)")
                && AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1;
            AudioGuard.ResetForTests();
            bool bare = !AudioGuard.IsFmodFailure("(60)") && !AudioGuard.NoteFmodFailure("(60)")
                && !AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 0;

            Check("unity-output-init-line-latches", unity && phrase && paren && bare,
                "unity " + unity + " phrase " + phrase + " paren " + paren + " bare " + bare);
            AudioGuard.ResetForTests();
        }

        static void CheckEmulatedNoSound(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            const string line = "FMOD failed to initialize any audio devices, running on emulated software output with no sound. Please check your audio drivers and/or hardware for malfunction.";
            bool seen = AudioGuard.IsFmodFailure(line);
            bool first = AudioGuard.NoteFmodFailure(line);
            bool second = AudioGuard.NoteFmodFailure(line);
            bool emulated = seen && first && second && AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1;

            AudioGuard.ResetForTests();
            bool soft = AudioGuard.IsFmodFailure("RUNNING ON EMULATED SOFTWARE OUTPUT WITH NO SOUND")
                && AudioGuard.NoteFmodFailure("RUNNING ON EMULATED SOFTWARE OUTPUT WITH NO SOUND")
                && AudioGuard.NoteFmodFailure("RUNNING ON EMULATED SOFTWARE OUTPUT WITH NO SOUND")
                && AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1;

            Check("emulated-no-sound-latches", emulated && soft, "emulated " + emulated + " soft " + soft);
            AudioGuard.ResetForTests();
        }

        static void CheckOutputThenEmulated(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            const string unity = "FMOD failed to initialize the output device.: \"Error initializing output device. \" (60)";
            const string emu = "FMOD failed to initialize any audio devices, running on emulated software output with no sound. Please check your audio drivers and/or hardware for malfunction.";
            bool first = AudioGuard.NoteFmodFailure(unity);
            bool second = AudioGuard.NoteFmodFailure(emu);
            bool once = first && second && AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1;
            Check("output-init-then-emulated-warns-once", once, "warnings " + AudioGuard.FmodWarnings);
            AudioGuard.ResetForTests();
        }

        static void CheckInvalidHandle(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            var go = new UnityEngine.GameObject("invalid-handle");
            try
            {
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = true;
                src.clip = ProbeClip("handle-bed");
                string[] bad =
                {
                    "FMOD returns error code 36 (FMOD_ERR_INVALID_HANDLE)",
                    "FMOD returns error code 36 (FMOD_ERR_INVALID_HANDLE) executing disconnectFrom",
                    "FMOD returns error code 36 (FMOD_ERR_INVALID_HANDLE) executing addDSP",
                    "FMOD_ERR_INVALID_HANDLE",
                    "error code 36",
                    "(600)",
                    "(36)",
                    "(6)",
                    "FMOD (600)",
                    "FMOD (36)",
                    "FMOD (6)"
                };
                bool quiet = true;
                for (int i = 0; i < bad.Length; i++)
                {
                    if (AudioGuard.IsFmodFailure(bad[i]) || AudioGuard.NoteFmodFailure(bad[i])) quiet = false;
                }
                AudioGuard.Play(src);
                quiet = quiet && !AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 0
                    && AudioGuard.SkippedCount == 0 && !AudioGuard.NeedsRestore(src);

                AudioGuard.ResetForTests();
                bool code = AudioGuard.IsFmodFailure("FMOD returns error code 60 (FMOD_ERR_OUTPUT_INIT)")
                    && AudioGuard.NoteFmodFailure("FMOD returns error code 60 (FMOD_ERR_OUTPUT_INIT)")
                    && AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1
                    && !AudioGuard.Play(src) && AudioGuard.SkippedCount == 1;

                AudioGuard.ResetForTests();
                bool symbol = AudioGuard.IsFmodFailure("FMOD_ERR_OUTPUT_INIT")
                    && AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT")
                    && AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1;

                AudioGuard.ResetForTests();
                bool wide = !AudioGuard.IsFmodFailure("error code 600")
                    && !AudioGuard.NoteFmodFailure("error code 600")
                    && !AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 0
                    && AudioGuard.SkippedCount == 0;

                Check("invalid-handle-does-not-latch", quiet && code && symbol && wide,
                    "quiet " + quiet + " code " + code + " symbol " + symbol + " wide " + wide);
            }
            finally
            {
                AudioGuard.ResetForTests();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckPlayScheduled(System.Action<string, bool, string> Check)
        {
            string root = System.IO.Path.Combine(UnityEngine.Application.dataPath, "_Project/Scripts");
            var bypass = new System.Collections.Generic.List<string>();
            int routed = 0;
            int impl = 0;
            if (System.IO.Directory.Exists(root))
            {
                var files = System.IO.Directory.GetFiles(root, "*.cs", System.IO.SearchOption.AllDirectories);
                for (int f = 0; f < files.Length; f++)
                {
                    var lines = System.IO.File.ReadAllLines(files[f]);
                    bool guard = files[f].EndsWith("AudioGuard.cs");
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].IndexOf(".PlayScheduled(", System.StringComparison.Ordinal) < 0) continue;
                        string trimmed = lines[i].TrimStart();
                        if (trimmed.StartsWith("//")) continue;
                        if (lines[i].IndexOf("AudioGuard.PlayScheduled(", System.StringComparison.Ordinal) >= 0)
                        {
                            routed++;
                            continue;
                        }
                        if (guard && lines[i].IndexOf("source.PlayScheduled(", System.StringComparison.Ordinal) >= 0)
                        {
                            impl++;
                            continue;
                        }
                        bypass.Add(System.IO.Path.GetFileName(files[f]) + ":" + (i + 1));
                    }
                }
            }
            else bypass.Add("missing " + root);
            bool ok = bypass.Count == 0 && routed >= 3 && impl == 1;
            Check("play-scheduled-goes-through-guard", ok,
                ok ? "routed " + routed : "routed " + routed + " impl " + impl + " " + string.Join(" ", bypass.ToArray()));
        }

        static AudioClip ProbeClip(string name)
        {
            return ProbeClip(name, 64);
        }

        static AudioClip ProbeClip(string name, int samples)
        {
            if (samples < 1) samples = 1;
            var clip = AudioClip.Create(name, samples, 1, 22050, false);
            clip.SetData(new float[samples], 0);
            return clip;
        }

        static void CheckAudioRetry(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            var idle = new UnityEngine.GameObject("retry-idle");
            var skippedGo = new UnityEngine.GameObject("retry-skipped");
            var otherGo = new UnityEngine.GameObject("retry-other");
            var deskGo = new UnityEngine.GameObject("retry-desk");
            MixDesk desk = null;
            try
            {
                var idleSrc = idle.AddComponent<AudioSource>();
                idleSrc.playOnAwake = false;
                idleSrc.loop = true;
                idleSrc.clip = ProbeClip("idle-bed");
                var skipped = skippedGo.AddComponent<AudioSource>();
                skipped.playOnAwake = false;
                skipped.loop = true;
                skipped.clip = ProbeClip("skipped-bed");
                var other = otherGo.AddComponent<AudioSource>();
                other.playOnAwake = false;
                other.loop = true;
                other.clip = ProbeClip("other-bed");
                desk = deskGo.AddComponent<MixDesk>();
                desk.AttachForTests();

                int before = AudioGuard.RetryPlayCount;
                AudioGuard.OnApplicationFocus(true);
                AudioGuard.OnApplicationPause(false);
                AudioGuard.OnAudioConfigurationChanged(true);
                bool quiet = AudioGuard.RetryPlayCount == before && !AudioGuard.WasRetried(idleSrc);

                AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
                bool missed = !AudioGuard.Play(skipped) && AudioGuard.SkippedCount == 1;
                AudioGuard.OnApplicationFocus(true);
                bool onlySkipped = AudioGuard.WasRetried(skipped) && !AudioGuard.WasRetried(other) && !AudioGuard.WasRetried(idleSrc);

                AudioGuard.NoteFmodFailure("error code 60");
                desk.PlantBadgerVoicesForTests(out var loop, out var cue, out var sting);
                AudioGuard.Play(loop);
                int cutAt = AudioGuard.RetryPlayCount;
                desk.BadgerCut();
                AudioGuard.OnApplicationPause(false);
                bool cutQuiet = AudioGuard.RetryPlayCount == cutAt && !AudioGuard.WasRetried(loop);

                AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
                AudioGuard.Play(loop);
                int resultAt = AudioGuard.RetryPlayCount;
                desk.BadgerResult(true);
                AudioGuard.OnAudioConfigurationChanged(false);
                bool resultQuiet = AudioGuard.RetryPlayCount == resultAt && !AudioGuard.WasRetried(loop) && !AudioGuard.WasRetried(sting);

                var paused = skipped;
                AudioGuard.NoteFmodFailure("error code 60");
                AudioGuard.Pause(paused);
                AudioGuard.Play(paused);
                int pauseAt = AudioGuard.RetryPlayCount;
                int pausedSample = paused.timeSamples;
                AudioGuard.OnApplicationFocus(true);
                bool pauseQuiet = AudioGuard.RetryPlayCount == pauseAt && !AudioGuard.WasRetried(paused) && paused.timeSamples == pausedSample;

                AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
                AudioGuard.Play(other);
                AudioGuard.SetMuted(true, false);
                int muteAt = AudioGuard.RetryPlayCount;
                AudioGuard.OnApplicationFocus(true);
                bool muteQuiet = AudioGuard.RetryPlayCount == muteAt && !AudioGuard.WasRetried(other);
                AudioGuard.SetMuted(false, false);

                var goneGo = new UnityEngine.GameObject("retry-gone");
                var gone = goneGo.AddComponent<AudioSource>();
                gone.clip = ProbeClip("gone-clip");
                UnityEngine.Object.DestroyImmediate(goneGo);
                int warns = AudioGuard.PlaybackWarnings;
                bool dead = !AudioGuard.Play(gone) && !AudioGuard.PlayScheduled(gone, 0d);
                bool once = !AudioGuard.Play(gone) && AudioGuard.PlaybackWarnings == warns + 1;

                var entrance = ProbeClip("entrance-probe");
                desk.ArmEntranceForTests(entrance);
                AudioGuard.RejectScheduled = loop;
                desk.BadgerEntrance();
                bool orphan = desk.OrphanCueStoppedForTests;

                Check("audio-retry-only-after-a-real-failure", quiet && missed && onlySkipped,
                    "quiet " + quiet + " missed " + missed + " only " + onlySkipped);
                Check("audio-retry-skips-cut-result-pause-mute",
                    cutQuiet && resultQuiet && pauseQuiet && muteQuiet,
                    "cut " + cutQuiet + " result " + resultQuiet + " pause " + pauseQuiet + " mute " + muteQuiet);
                Check("destroyed-source-returns-false-once", dead && once, "warnings " + AudioGuard.PlaybackWarnings);
                Check("entrance-stops-cue-when-loop-misses", orphan, "orphan " + orphan);
            }
            finally
            {
                AudioGuard.RejectScheduled = null;
                AudioGuard.SetMuted(false, false);
                AudioGuard.ResetForTests();
                if (desk != null) MixDesk.DetachForTests(desk);
                UnityEngine.Object.DestroyImmediate(idle);
                UnityEngine.Object.DestroyImmediate(skippedGo);
                UnityEngine.Object.DestroyImmediate(otherGo);
                UnityEngine.Object.DestroyImmediate(deskGo);
            }
        }

        static void CheckPauseAndUnmute(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            GamePause.Reset();
            var go = new UnityEngine.GameObject("pause-bed");
            MixDesk desk = null;
            try
            {
                desk = go.AddComponent<MixDesk>();
                desk.AttachForTests();
                desk.PlantStemForTests(0.24f);
                var bed = desk.StemForTests(0);
                bed.clip = ProbeClip("garden-bed");
                bed.playOnAwake = false;

                int routes = AudioGuard.PauseRoutes;
                int resumes = AudioGuard.ResumeRoutes;
                GamePause.Push();
                bool entered = AudioGuard.PauseRoutes == routes + 1 && GamePause.Paused;
                GamePause.Push();
                bool once = AudioGuard.PauseRoutes == routes + 1;
                AudioGuard.Pause(bed);
                bool marked = AudioGuard.IsPaused(bed);
                GamePause.Pop();
                bool still = GamePause.Paused && AudioGuard.IsPaused(bed) && AudioGuard.ResumeRoutes == resumes;
                GamePause.Pop();
                bool cleared = !GamePause.Paused && !AudioGuard.IsPaused(bed) && AudioGuard.ResumeRoutes == resumes + 1;

                AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
                bool missed = !AudioGuard.Play(bed) && AudioGuard.SkippedCount >= 1;
                GamePause.Push();
                int held = AudioGuard.RetryPlayCount;
                AudioGuard.OnApplicationFocus(true);
                bool deferred = AudioGuard.RetryDeferred && AudioGuard.RetryPlayCount == held && AudioGuard.SkippedCount >= 1;
                GamePause.Pop();
                bool resumed = !AudioGuard.RetryDeferred && AudioGuard.WasRetried(bed) && AudioGuard.RetryPlayCount == held + 1;

                AudioGuard.NoteFmodFailure("error code 60");
                desk.PlantStemForTests(0.24f);
                bed = desk.StemForTests(0);
                bed.clip = ProbeClip("garden-bed");
                bed.loop = true;
                bool heldDead = !AudioGuard.Play(bed) && AudioGuard.NeedsRestore(bed);
                int plays = AudioGuard.PlayCalls;
                int stops = AudioGuard.StopCalls;
                bed.timeSamples = 32000;
                AudioGuard.SetMuted(true, false);
                AudioGuard.OnAudioConfigurationChanged(true);
                bool kept = AudioGuard.StopCalls == stops && AudioGuard.PlayCalls == plays
                    && bed.timeSamples == 32000 && AudioGuard.NeedsRestore(bed);
                AudioGuard.SetMuted(false, false);
                bool brought = heldDead && kept && desk.LastReassert == 1;
                AudioGuard.Pause(bed);
                bool pausedStays = desk.ReassertBed() == 0 && AudioGuard.IsPaused(bed);
                desk.PlantStemForTests(0f);
                var quiet = desk.StemForTests(0);
                quiet.clip = ProbeClip("silent-bed");
                bool silent = desk.ReassertBed() == 0;

                Check("game-pause-routes-through-audioguard",
                    entered && once && marked && still && cleared,
                    "entered " + entered + " once " + once + " marked " + marked + " cleared " + cleared);
                Check("retry-waits-until-game-resume",
                    missed && deferred && resumed,
                    "missed " + missed + " deferred " + deferred + " resumed " + resumed);
                Check("unmute-reasserts-the-bed",
                    kept && brought && pausedStays && silent,
                    "kept " + kept + " brought " + brought + " paused " + pausedStays + " silent " + silent);
            }
            finally
            {
                AudioGuard.SetMuted(false, false);
                AudioGuard.ResetForTests();
                GamePause.Reset();
                if (desk != null) MixDesk.DetachForTests(desk);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckUnmuteScope(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            GamePause.Reset();
            var go = new UnityEngine.GameObject("unmute-scope");
            MixDesk desk = null;
            try
            {
                desk = go.AddComponent<MixDesk>();
                desk.AttachForTests();
                desk.PlantStemForTests(0.24f);
                var mid = desk.StemForTests(0);
                mid.clip = ProbeClip("mid-bed", 88200);
                mid.loop = true;
                bool started = AudioGuard.Play(mid);
                mid.timeSamples = 44100;
                int planted = mid.timeSamples;
                int plays = AudioGuard.PlayCalls;
                int stops = AudioGuard.StopCalls;
                AudioGuard.SetMuted(true, false);
                AudioGuard.SetMuted(false, false);
                bool place = started && planted == 44100
                    && AudioGuard.PlayCalls == plays && AudioGuard.StopCalls == stops
                    && mid.timeSamples == 44100 && AudioGuard.RestoreCount == 0 && !AudioGuard.NeedsRestore(mid)
                    && desk.LastReassert == 0 && !AudioGuard.ScheduledAt(mid, out _);

                AudioGuard.ResetForTests();
                desk.PlantBadgerVoicesForTests(out var loop, out var cue, out var sting);
                loop.clip = ProbeClip("badger-loop");
                cue.clip = ProbeClip("badger-cue");
                sting.clip = ProbeClip("badger-sting");
                cue.loop = false;
                sting.loop = false;
                loop.loop = true;
                AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
                AudioGuard.Play(loop);
                AudioGuard.Play(sting);
                desk.ArmCutForTests();
                AudioGuard.SetMuted(true, false);
                AudioGuard.OnAudioConfigurationChanged(true);
                AudioGuard.SetMuted(false, false);
                bool cut = desk.LastReassert == 0 && !AudioGuard.NeedsRestore(loop) && !AudioGuard.NeedsRestore(sting);

                AudioGuard.ResetForTests();
                AudioGuard.NoteFmodFailure("error code 60");
                AudioGuard.Play(loop);
                AudioGuard.Play(sting);
                desk.ArmResultForTests();
                desk.BadgerResult(true);
                AudioGuard.SetMuted(true, false);
                AudioGuard.OnAudioConfigurationChanged(true);
                AudioGuard.SetMuted(false, false);
                bool result = desk.LastReassert == 0 && !AudioGuard.NeedsRestore(loop) && !AudioGuard.NeedsRestore(sting);

                AudioGuard.ResetForTests();
                AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
                desk.PlantSplashStemForTests(false);
                var splash = desk.StemForTests(4);
                splash.clip = ProbeClip("splash-early");
                splash.loop = true;
                AudioGuard.Play(splash);
                AudioGuard.SetMuted(true, false);
                AudioGuard.OnAudioConfigurationChanged(true);
                AudioGuard.SetMuted(false, false);
                bool early = desk.LastReassert == 0 && !AudioGuard.NeedsRestore(splash);

                AudioGuard.ResetForTests();
                AudioGuard.NoteFmodFailure("error code 60");
                desk.PlantStemForTests(0.24f);
                var kept = desk.StemForTests(0);
                kept.clip = ProbeClip("kept-bed");
                kept.loop = true;
                var other = desk.AddLoopForTests(true);
                other.clip = ProbeClip("other-bed");
                var oneShot = desk.AddLoopForTests(false);
                oneShot.clip = ProbeClip("stinger");
                AudioGuard.Play(kept);
                AudioGuard.Play(oneShot);
                AudioGuard.SetMuted(true, false);
                AudioGuard.OnAudioConfigurationChanged(true);
                AudioGuard.SetMuted(false, false);
                bool only = desk.LastReassert == 1 && AudioGuard.WasStarted(kept) && !AudioGuard.WasStarted(other) && !AudioGuard.NeedsRestore(oneShot);

                Check("mute-unmute-mid-bed-keeps-position", place,
                    "place " + place + " planted " + planted + " samples " + mid.timeSamples);
                Check("unmute-during-cut-starts-nothing", cut, "cut " + cut);
                Check("unmute-after-result-starts-nothing", result, "result " + result);
                Check("unmute-before-splash-welcome-starts-nothing", early, "early " + early);
                Check("unmute-restores-only-the-stopped-bed", only, "only " + only + " reassert " + desk.LastReassert);
            }
            finally
            {
                AudioGuard.SetMuted(false, false);
                AudioGuard.ResetForTests();
                GamePause.Reset();
                if (desk != null) MixDesk.DetachForTests(desk);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static bool SameScheduledStart(AudioSource a, AudioSource b)
        {
            double ta, tb;
            return AudioGuard.ScheduledAt(a, out ta) && AudioGuard.ScheduledAt(b, out tb) && ta == tb;
        }

        static void CheckScheduledRestore(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            GamePause.Reset();
            var go = new UnityEngine.GameObject("scheduled-restore");
            MixDesk desk = null;
            try
            {
                desk = go.AddComponent<MixDesk>();
                desk.AttachForTests();
                desk.PlantStemForTests(0.24f);
                var a = desk.StemForTests(0);
                a.clip = ProbeClip("sched-a");
                a.loop = true;
                var b = desk.AddLoopForTests(true);
                b.clip = ProbeClip("sched-b");
                AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
                AudioGuard.Play(a);
                AudioGuard.Play(b);
                int plays = AudioGuard.PlayCalls;
                AudioGuard.SetMuted(true, false);
                AudioGuard.OnAudioConfigurationChanged(true);
                AudioGuard.SetMuted(false, false);
                bool unmute = desk.LastReassert == 2 && SameScheduledStart(a, b) && AudioGuard.PlayCalls == plays;

                AudioGuard.ResetForTests();
                // The unmute half already scheduled a and b. A playing voice is not
                // a missed start, so the retry half uses voices that are still silent.
                var c = desk.AddLoopForTests(true);
                c.clip = ProbeClip("sched-c");
                var d = desk.AddLoopForTests(true);
                d.clip = ProbeClip("sched-d");
                AudioGuard.NoteFmodFailure("error code 60");
                bool missedRetry = !AudioGuard.Play(c) && !AudioGuard.Play(d);
                plays = AudioGuard.PlayCalls;
                AudioGuard.OnApplicationFocus(true);
                bool retry = missedRetry && AudioGuard.WasRetried(c) && AudioGuard.WasRetried(d)
                    && SameScheduledStart(c, d) && AudioGuard.PlayCalls == plays;

                Check("restored-stems-share-one-scheduled-start", unmute && retry,
                    "unmute " + unmute + " retry " + retry);
            }
            finally
            {
                AudioGuard.SetMuted(false, false);
                AudioGuard.ResetForTests();
                GamePause.Reset();
                if (desk != null) MixDesk.DetachForTests(desk);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckScheduleHousekeeping(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            GamePause.Reset();
            var go = new UnityEngine.GameObject("schedule-housekeeping");
            MixDesk desk = null;
            try
            {
                desk = go.AddComponent<MixDesk>();
                desk.AttachForTests();
                desk.PlantStemForTests(0.24f);
                var stale = desk.StemForTests(0);
                stale.clip = ProbeClip("stale-bed");
                stale.loop = true;
                stale.timeSamples = 12000;
                var mate = desk.AddLoopForTests(true);
                mate.clip = ProbeClip("mate-bed");
                mate.timeSamples = 8000;
                int plays = AudioGuard.PlayCalls;
                int n = AudioGuard.ScheduleStemsTogether(new AudioSource[] { stale, mate });
                bool rewound = n == 2 && stale.timeSamples == 0 && mate.timeSamples == 0
                    && SameScheduledStart(stale, mate) && AudioGuard.PlayCalls == plays;

                bool logged = AudioGuard.ScheduledAt(stale, out _) && AudioGuard.ScheduledAt(mate, out _);
                AudioGuard.Release(stale);
                bool dropped = !AudioGuard.ScheduledAt(stale, out _) && AudioGuard.ScheduledAt(mate, out _);
                AudioGuard.DropRestore(mate);
                bool both = !AudioGuard.ScheduledAt(mate, out _);

                AudioGuard.ResetForTests();
                desk.PlantStemForTests(0f);
                var dawn = desk.StemForTests(0);
                dawn.clip = ProbeClip("dawn-bed");
                dawn.loop = true;
                dawn.timeSamples = 5000;
                var mid = desk.AddLoopForTests(true);
                mid.clip = ProbeClip("mid-copy");
                mid.volume = 0f;
                var last = desk.AddLoopForTests(true);
                last.clip = ProbeClip("last-copy");
                last.volume = 0f;
                plays = AudioGuard.PlayCalls;
                int booted = desk.BootPlaceForTests();
                bool boot = booted == 3 && dawn.timeSamples == 0 && mid.timeSamples == 0 && last.timeSamples == 0
                    && SameScheduledStart(dawn, mid) && SameScheduledStart(dawn, last)
                    && AudioGuard.PlayCalls == plays;

                Check("schedule-together-rewinds-stale-stems", rewound, "rewound " + rewound + " n " + n);
                Check("release-drops-schedule-log", logged && dropped && both, "logged " + logged + " dropped " + dropped);
                Check("boot-stems-share-one-scheduled-start", boot, "boot " + boot + " n " + booted);
            }
            finally
            {
                AudioGuard.SetMuted(false, false);
                AudioGuard.ResetForTests();
                GamePause.Reset();
                if (desk != null) MixDesk.DetachForTests(desk);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckGardenGlove(System.Action<string, bool, string> Check)
        {
            int[] w = { 820, 402, 412 };
            int[] h = { 1180, 874, 915 };
            bool inside = true;
            for (int i = 0; i < w.Length; i++)
            {
                float s = DeviceFit.Ui(w[i], h[i]);
                var aim = new Vector2(w[i] * 0.28f, h[i] * 0.42f);
                var branch = FlockFiveApp.LessonBranchRect(aim, s);
                var nudged = new Vector2(aim.x + branch.width, aim.y + branch.height);
                var tip = FlockFiveApp.KeepTipOnBranch(nudged, branch);
                if (!branch.Contains(tip)) inside = false;
            }
            bool tap = true;
            for (int i = 0; i < w.Length; i++)
            {
                float H = h[i];
                var glove = new Rect(w[i] * 0.3f, 100f, 70f, 80f);
                var onGlove = new Vector2(glove.center.x, H - glove.center.y);
                var mirroredScreen = new Vector2(glove.center.x, glove.center.y);
                if (!FlockFiveApp.ScreenGloveCountsAsBranch(onGlove, H, glove, true)) tap = false;
                if (FlockFiveApp.ScreenGloveCountsAsBranch(mirroredScreen, H, glove, true)) tap = false;
            }
            bool l1 = true;
            for (int i = 0; i < w.Length; i++)
            {
                float H = h[i];
                for (int step = 0; step < 5; step++)
                {
                    var branch = FlockFiveApp.L1BranchRect(step, w[i], h[i]);
                    var glove = FlockFiveApp.GloveOnBranch(branch);
                    var tip = branch.center;
                    var tipGui = new Vector2(tip.x, tip.y);
                    if (!branch.Contains(tip) || !branch.Contains(glove.center) || !branch.Contains(tipGui))
                        l1 = false;
                    if (!glove.Contains(tipGui)) l1 = false;
                    var center = new Vector2(glove.center.x, H - glove.center.y);
                    var tipScreen = new Vector2(tip.x, H - tip.y);
                    if (!FlockFiveApp.ScreenGloveCountsAsBranch(center, H, glove, true)) l1 = false;
                    if (!FlockFiveApp.ScreenGloveCountsAsBranch(tipScreen, H, glove, true)) l1 = false;
                    var mirroredScreen = new Vector2(glove.center.x, glove.center.y);
                    if (FlockFiveApp.ScreenGloveCountsAsBranch(mirroredScreen, H, glove, true)) l1 = false;
                }
            }
            bool clear = CaptionLayout.FirstClearLine.IndexOf("Five of one color clear.") >= 0;
            bool bottom = FlockFiveApp.IsGardenBottomLine(CaptionLayout.FirstClearLine);
            var restart = new Rect(700f, 40f, 64f, 64f);
            var home = CornerAnchor.TopBarHome(restart, 820f, new Rect(0f, 0f, 820f, 1180f));
            bool mirrored = Mathf.Abs(home.y - restart.y) < 0.5f && Mathf.Abs(home.width - restart.width) < 0.5f && home.x < restart.x;
            Check("glove-tip-lands-in-the-branch", inside && clear && mirrored && bottom, "820 402 412");
            Check("screen-glove-tap-hits-the-branch", tap, "820 402 412");
            Check("l1-glove-center-advances", l1, "820 402 412 steps 0-4");
        }

        static void CheckUpgradeStack(System.Action<string, bool, string> Check)
        {
            int[] w = { 820, 402, 412 };
            int[] h = { 1180, 874, 915 };
            bool clear = true;
            for (int i = 0; i < w.Length; i++)
            {
                float s = DeviceFit.Ui(w[i], h[i]);
                var card = FlockFiveApp.SeatedInspectCard(w[i], h[i], 34f, s);
                if (!FlockFiveApp.UpgradeClearsBottom(card, h[i], 34f, s)) clear = false;
            }
            Check("upgrade-clears-bottom-inset", clear, "820 402 412 inset 34");
        }

        static void CheckPlayerGlove(System.Action<string, bool, string> Check)
        {
            int[] w = { 820, 402, 412 };
            int[] h = { 1180, 874, 915 };
            bool tap = true, bobOk = true, settle = true, hud = true;
            for (int i = 0; i < w.Length; i++)
            {
                float H = h[i];
                float s = DeviceFit.Ui(w[i], H);
                float dh = FlockFiveApp.GloveDh(s);
                var branch = FlockFiveApp.L1BranchRect(1, w[i], H);
                var pivot = branch.center;
                var posed = FlockFiveApp.GloveBobPivot(pivot, 0f, 0f, false);
                var drawn = FlockFiveApp.ScreenGloveRect(posed, 0f, dh, 0f, false);
                var center = new Vector2(drawn.center.x, H - drawn.center.y);
                var tip = new Vector2(pivot.x, H - pivot.y);
                var near = new Vector2(drawn.xMax - 1.5f, drawn.center.y);
                var nearScreen = new Vector2(near.x, H - near.y);
                if (!FlockFiveApp.LessonGloveHit(center, H, drawn)) tap = false;
                if (!drawn.Contains(pivot) || !FlockFiveApp.LessonGloveHit(tip, H, drawn)) tap = false;
                if (!drawn.Contains(near) || !FlockFiveApp.LessonGloveHit(nearScreen, H, drawn)) tap = false;
                var outside = new Vector2(drawn.xMin - 4f, H - drawn.center.y);
                if (FlockFiveApp.LessonGloveHit(outside, H, drawn)) tap = false;
                float lo = 99f, hi = -99f;
                for (int k = 0; k <= 48; k++)
                {
                    float t = k / 48f * (Mathf.PI * 2f / 2.35f);
                    float b = FlockFiveApp.GloveBobPx(t, false);
                    if (b < lo) lo = b;
                    if (b > hi) hi = b;
                }
                if (hi - lo > 8.01f) bobOk = false;
                var btn = new Rect((w[i] - 180f) * 0.5f, H * 0.72f, 180f, 84f);
                var aim = FlockFiveApp.UpgradeGloveSettle(btn);
                if (!btn.Contains(aim)) settle = false;
                var restart = new Rect(w[i] - 80f, 16f, 64f, 64f);
                var home = new Rect(16f, 16f, 64f, 64f);
                bool homeHit, restartHit;
                var onRestart = new Vector2(restart.center.x, H - restart.center.y);
                var onHome = new Vector2(home.center.x, H - home.center.y);
                if (!FlockFiveApp.GardenHudPoint(onRestart, H, home, restart, out homeHit, out restartHit) || !restartHit || homeHit)
                    hud = false;
                if (!FlockFiveApp.GardenHudPoint(onHome, H, home, restart, out homeHit, out restartHit) || !homeHit || restartHit)
                    hud = false;
            }
            float held = 0f;
            for (int f = 0; f < 270; f++)
                held = FlockFiveApp.AdvanceRuleHold(held, 1f / 60f);
            bool gone = FlockFiveApp.RuleHoldDone(held) && !FlockFiveApp.RuleHoldDone(FlockFiveApp.AdvanceRuleHold(0f, 3.5f));
            Check("player-glove-tap-advances", tap, "center tip near-miss");
            Check("player-glove-bob-extent", bobOk, "peak to peak");
            Check("upgrade-settle-inside-button", settle, "820 402 412");
            Check("lesson-hud-acts", hud, "home restart");
            Check("caption-gone-after-4-5s", gone, "unscaled");
        }

        static void CheckCoachRestart(System.Action<string, bool, string> Check)
        {
            const string doneKey = "flockfive.coach.done";
            const string giftKey = "flockfive.coach.gift";
            bool hadDone = PlayerPrefs.HasKey(doneKey);
            bool hadGift = PlayerPrefs.HasKey(giftKey);
            int keepDone = PlayerPrefs.GetInt(doneKey, 0);
            int keepGift = PlayerPrefs.GetInt(giftKey, 0);
            var go = new GameObject("coach-restart");
            try
            {
                PlayerPrefs.DeleteKey(doneKey);
                PlayerPrefs.DeleteKey(giftKey);
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var t = typeof(FlockFiveApp);
                var board = LevelData.Open(0);
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_coachMoves", flags).SetValue(app, 0);
                var pick = t.GetMethod("CoachPick", flags);
                var moved = t.GetMethod("CoachMoved", flags);
                var resume = t.GetMethod("FreshCoachResume", flags);
                object[] cue = { -1, -1 };
                bool first = (bool)pick.Invoke(app, cue);
                int from = (int)cue[0], to = (int)cue[1];
                bool level1 = FlockFiveApp.Level1First(board, i => false, i => false, out int wantFrom, out int wantTo);
                t.GetField("_coachFrom", flags).SetValue(app, from);
                t.GetField("_coachTo", flags).SetValue(app, to);
                moved.Invoke(app, new object[] { from, to, 0 });
                int afterHop = (int)t.GetField("_coachMoves", flags).GetValue(app);
                bool live = (bool)t.GetField("_coach", flags).GetValue(app);
                resume.Invoke(app, null);
                int reset = (int)t.GetField("_coachMoves", flags).GetValue(app);
                object[] again = { -1, -1 };
                bool recued = (bool)pick.Invoke(app, again);
                int from2 = (int)again[0], to2 = (int)again[1];
                t.GetField("_coachFrom", flags).SetValue(app, from2);
                t.GetField("_coachTo", flags).SetValue(app, to2);
                moved.Invoke(app, new object[] { from2, to2, 0 });
                int midMoves = (int)t.GetField("_coachMoves", flags).GetValue(app);
                bool mid = (bool)t.GetField("_coach", flags).GetValue(app);
                moved.Invoke(app, new object[] { from2, to2, 0 });
                bool finished = !(bool)t.GetField("_coach", flags).GetValue(app);
                Check("restart-restores-level1-first",
                    level1 && first && from == wantFrom && to == wantTo && live && afterHop == 1
                    && reset == 0 && recued && from2 == wantFrom && to2 == wantTo
                    && mid && midMoves == 1 && finished,
                    "from " + from + "->" + from2 + " moves " + afterHop + "/" + reset + "/" + midMoves);
            }
            finally
            {
                if (hadDone) PlayerPrefs.SetInt(doneKey, keepDone);
                else PlayerPrefs.DeleteKey(doneKey);
                if (hadGift) PlayerPrefs.SetInt(giftKey, keepGift);
                else PlayerPrefs.DeleteKey(giftKey);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckRealPress(System.Action<string, bool, string> Check)
        {
            int[] cssW = { 820, 402, 412 };
            int[] cssH = { 1180, 874, 915 };
            float[] dpr = { 2f, 2f, 2f };
            bool glove = true, hud = true, release = true;
            for (int i = 0; i < cssW.Length; i++)
            {
                float s = DeviceFit.Ui(cssW[i], cssH[i]);
                float dh = FlockFiveApp.GloveDh(s);
                float bw = cssW[i] * dpr[i], bh = cssH[i] * dpr[i];
                var pivot = new Vector2(bw * 0.42f, bh * 0.48f);
                var drawn = FlockFiveApp.ScreenGloveRect(FlockFiveApp.GloveBobPivot(pivot, 0f, 0f, false), 0f, dh * dpr[i], 0f, false);
                var home = new Rect(16f, 8f, 48f, 48f);
                var restart = new Rect(bw - 64f, 8f, 48f, 48f);
                var gear = new Rect(16f, 8f, 48f, 48f);
                Vector2 Buf(Vector2 css)
                {
                    return FlockFiveApp.CanvasPointer(css, cssW[i], cssH[i], bw, bh);
                }
                var center = Buf(new Vector2(drawn.center.x / dpr[i], (bh - drawn.center.y) / dpr[i]));
                var tip = Buf(new Vector2(pivot.x / dpr[i], (bh - pivot.y) / dpr[i]));
                var nearGui = new Vector2(drawn.xMax - 1.5f, drawn.center.y);
                var near = Buf(new Vector2(nearGui.x / dpr[i], (bh - nearGui.y) / dpr[i]));
                if (FlockFiveApp.GardenPressFate(center, bh, home, restart, drawn, true, true) != FlockFiveApp.PressFate.Glove)
                    glove = false;
                if (FlockFiveApp.GardenPressFate(tip, bh, home, restart, drawn, true, true) != FlockFiveApp.PressFate.Glove)
                    glove = false;
                if (!drawn.Contains(nearGui) || FlockFiveApp.GardenPressFate(near, bh, home, restart, drawn, true, true) != FlockFiveApp.PressFate.Glove)
                    glove = false;
                var onRestart = Buf(new Vector2(restart.center.x / dpr[i], (bh - restart.center.y) / dpr[i]));
                var onHome = Buf(new Vector2(home.center.x / dpr[i], (bh - home.center.y) / dpr[i]));
                if (FlockFiveApp.GardenPressFate(onRestart, bh, home, restart, drawn, true, true) != FlockFiveApp.PressFate.Restart)
                    hud = false;
                if (FlockFiveApp.GardenPressFate(onHome, bh, home, restart, drawn, true, true) != FlockFiveApp.PressFate.Home)
                    hud = false;
                var releaseGui = FlockFiveApp.PointerToGui(onHome, bh);
                if (FlockFiveApp.GearTakesRelease(true, -1, -1, true, releaseGui, gear))
                    release = false;
                if (!FlockFiveApp.GearTakesRelease(false, -1, -1, true, releaseGui, gear))
                    release = false;
            }
            Check("real-press-glove-centre-tip-near", glove, "820 402 412 dpr");
            Check("real-press-hud-before-caption", hud, "home restart");
            Check("real-press-home-release-swallows-gear", release, "exit pointer");
        }

        static bool SameBirds(Board a, Board b)
        {
            if (a == null || b == null || a.Branches.Count != b.Branches.Count) return false;
            for (int i = 0; i < a.Branches.Count; i++)
            {
                var x = a.Branches[i];
                var y = b.Branches[i];
                if (x == null || y == null || x.Count != y.Count) return false;
                for (int k = 0; k < x.Count; k++)
                    if (!x.Birds[k].SameFlock(y.Birds[k])) return false;
            }
            return true;
        }

        static void CheckCaptionLeave(System.Action<string, bool, string> Check)
        {
            const string doneKey = "flockfive.coach.done";
            bool hadDone = PlayerPrefs.HasKey(doneKey);
            int keepDone = PlayerPrefs.GetInt(doneKey, 0);
            bool hadKit = PlayerPrefs.HasKey(RuleTips.KitKey);
            int keepKit = PlayerPrefs.GetInt(RuleTips.KitKey, 0);
            int keepStreak = Purse.Streak;
            var go = new GameObject("caption-leave");
            var limb = new GameObject("caption-limb");
            var camGo = new GameObject("caption-cam");
            try
            {
                PlayerPrefs.DeleteKey(doneKey);
                PlayerPrefs.DeleteKey(RuleTips.KitKey);
                Purse.SetStreak(0);
                GamePause.Reset();
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var t = typeof(FlockFiveApp);
                var board = LevelData.Open(0);
                var seed = board.Clone();
                bool changed = false;
                if (board.Branches.Count > 0 && board.Branches[0].Count > 0)
                {
                    board.Branches[0].Birds.RemoveAt(board.Branches[0].Count - 1);
                    changed = !SameBirds(board, seed);
                }
                var view = limb.AddComponent<BranchView>();
                for (int s = 0; s < BranchState.Cap; s++)
                {
                    var seat = new GameObject("seat");
                    seat.transform.SetParent(limb.transform, false);
                    view.Seats[s] = seat.transform;
                }
                var birdGo = new GameObject("bird");
                birdGo.transform.SetParent(limb.transform, false);
                view.Birds[0] = birdGo.AddComponent<SpriteRenderer>();
                var garden = new WorldBuilder.Garden();
                garden.Branches = new BranchView[] { view };
                garden.Cam = camGo.AddComponent<Camera>();
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_seed", flags).SetValue(app, seed);
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_coachMoves", flags).SetValue(app, 1);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetMethod("ShowRuleTip", flags).Invoke(app, new object[] { RuleTip.Kit });
                bool heldPause = GamePause.Paused;
                object[] hudOut = { default(Rect), default(Rect) };
                t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hudOut);
                var restart = (Rect)hudOut[0];
                float screenH = PhoneArea.H;
                var tap = new Vector2(restart.center.x, screenH - restart.center.y);
                t.GetMethod("TakeRuleTipPress", flags).Invoke(app, new object[] { tap });
                var snap = (System.Collections.IEnumerator)t.GetField("EditorSnapRound", flags).GetValue(app);
                int frames = 0;
                bool landed = false;
                if (snap != null)
                    frames = PumpFrames(snap, 48, null, out landed);
                var now = (Board)t.GetField("_board", flags).GetValue(app);
                var seedNow = (Board)t.GetField("_seed", flags).GetValue(app);
                int moves = (int)t.GetField("_coachMoves", flags).GetValue(app);
                t.GetMethod("CoachAdvance", flags).Invoke(app, null);
                var line = (string)t.GetField("_cueLine", flags).GetValue(app);
                bool level1 = FlockFiveApp.Level1First(seedNow, i => false, i => false, out int wantFrom, out int wantTo);
                object[] cue = { -1, -1 };
                bool picked = (bool)t.GetMethod("CoachPick", flags).Invoke(app, cue);
                int from = (int)cue[0], to = (int)cue[1];
                Check("kit-restart-restores-seed",
                    changed && heldPause && landed && SameBirds(now, seedNow) && moves == 0
                    && level1 && picked && from == wantFrom && to == wantTo
                    && !string.IsNullOrEmpty(line) && line != RuleTips.KitLine && !GamePause.Paused,
                    "held " + heldPause + " landed " + landed + " frames " + frames
                    + " moves " + moves + " line " + line);

                GamePause.Reset();
                bool streakChanged = false;
                if (now != null && now.Branches.Count > 0 && now.Branches[0].Count > 0)
                {
                    now.Branches[0].Birds.RemoveAt(now.Branches[0].Count - 1);
                    streakChanged = !SameBirds(now, seedNow);
                }
                t.GetMethod("ShowRuleTip", flags).Invoke(app, new object[] { RuleTip.Kit });
                bool streakHeld = GamePause.Paused;
                var streak = (System.Collections.IEnumerator)t.GetMethod("FinishRestart", flags).Invoke(app, new object[] { false });
                int streakFrames = 0;
                bool streakLanded = false;
                if (streak != null)
                    streakFrames = PumpFrames(streak, 48, null, out streakLanded);
                var streakBoard = (Board)t.GetField("_board", flags).GetValue(app);
                Check("streak-finish-restart-restores-seed",
                    streakChanged && streakHeld && streakLanded && SameBirds(streakBoard, seedNow) && !GamePause.Paused,
                    "held " + streakHeld + " landed " + streakLanded + " frames " + streakFrames);

                GamePause.Reset();
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_sceneFaceSet", flags).SetValue(app, true);
                var faceField = t.GetField("_sceneFace", flags);
                faceField.SetValue(app, System.Enum.ToObject(faceField.FieldType, 1));
                t.GetMethod("ArmLevelExit", flags).Invoke(app, new object[] { -1 });
                t.GetMethod("ShowRuleTip", flags).Invoke(app, new object[] { RuleTip.Kit });
                object[] hudHome = { default(Rect), default(Rect) };
                t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hudHome);
                var restart2 = (Rect)hudHome[0];
                var home = CornerAnchor.TopBarHome(restart2, PhoneArea.W, PhoneArea.Safe);
                var homeTap = new Vector2(home.center.x, screenH - home.center.y);
                t.GetMethod("TakeRuleTipPress", flags).Invoke(app, new object[] { homeTap });
                t.GetMethod("ArmSceneInputSettle", flags).Invoke(app, null);
                float until = (float)t.GetField("_sceneInputUntil", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                bool blocked = FlockFiveApp.SceneInputBlocked(AdPause.Unscaled, until);
                bool splash = (bool)t.GetField("_splash", flags).GetValue(app);
                int tutor = (int)t.GetField("_tutorPause", flags).GetValue(app);
                var tip = t.GetField("_ruleTip", flags).GetValue(app);
                bool releaseSwallowed = FlockFiveApp.SwallowLevelExit(true, -1, FlockFiveApp.PointerEdge.Release, -1);
                bool downSwallowed = FlockFiveApp.SwallowLevelExit(true, -1, FlockFiveApp.PointerEdge.Down, -1);
                bool gearTakes = FlockFiveApp.GearTakesRelease(true, -1, -1, true, home.center, home);
                Check("kit-home-releases-tutor-pause",
                    splash && tutor == 0 && tip != null && tip.ToString() == "None" && !GamePause.Paused,
                    "splash " + splash + " tutor " + tutor + " tip " + tip + " paused " + GamePause.Paused);
                Check("home-exit-release-swallows-play-down-does-not",
                    releaseSwallowed && !downSwallowed && !gearTakes && !blocked,
                    "release " + releaseSwallowed + " down " + downSwallowed + " gear " + gearTakes + " blocked " + blocked);
            }
            finally
            {
                GamePause.Reset();
                Purse.SetStreak(keepStreak);
                if (hadDone) PlayerPrefs.SetInt(doneKey, keepDone);
                else PlayerPrefs.DeleteKey(doneKey);
                if (hadKit) PlayerPrefs.SetInt(RuleTips.KitKey, keepKit);
                else PlayerPrefs.DeleteKey(RuleTips.KitKey);
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(limb);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckExitGuards(System.Action<string, bool, string> Check)
        {
            float keepResume = ResumeUntil();
            var go = new GameObject("exit-guards");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var t = typeof(FlockFiveApp);
                var held = t.GetField("_exitPointerHeld", BindingFlags.NonPublic | BindingFlags.Static);
                var frame = t.GetField("_exitPointerFrame", BindingFlags.NonPublic | BindingFlags.Static);
                var consume = t.GetMethod("ConsumeLevelExit", BindingFlags.NonPublic | BindingFlags.Static);
                t.GetMethod("ArmLevelExit", flags).Invoke(app, new object[] { -1 });
                t.GetMethod("OnApplicationFocus", flags).Invoke(app, new object[] { false });
                bool focusCleared = !(bool)held.GetValue(null);
                t.GetMethod("ArmLevelExit", flags).Invoke(app, new object[] { 4 });
                t.GetMethod("OnApplicationPause", flags).Invoke(app, new object[] { false });
                bool pauseCleared = !(bool)held.GetValue(null);

                t.GetMethod("ArmLevelExit", flags).Invoke(app, new object[] { -1 });
                frame.SetValue(null, Time.frameCount - 2);
                // Batchmode has no IMGUI, so a constructed Event's type reads back as Ignore
                // and ConsumeLevelExit never sees the press. When the type sticks, use that
                // handler. Otherwise run the same MouseDown then MouseUp decisions it runs.
                var probe = new Event { type = EventType.MouseDown, button = 0 };
                bool eventLive = probe.type == EventType.MouseDown;
                var clear = t.GetMethod("ClearLevelExit", BindingFlags.NonPublic | BindingFlags.Static);
                var tracked = t.GetField("_exitPointerId", BindingFlags.NonPublic | BindingFlags.Static);
                bool ateDown;
                if (eventLive)
                {
                    var down = new Event { type = EventType.MouseDown, button = 0 };
                    ateDown = (bool)consume.Invoke(null, new object[] { down });
                }
                else
                {
                    ateDown = false;
                    int id = (int)tracked.GetValue(null);
                    int armed = (int)frame.GetValue(null);
                    if (FlockFiveApp.DropExitOnPress(true, id, -1, armed, Time.frameCount))
                        clear.Invoke(null, null);
                }
                bool pressCleared = !(bool)held.GetValue(null) && !ateDown;
                bool ateRelease;
                if (eventLive)
                {
                    var up = new Event { type = EventType.MouseUp, button = 0 };
                    ateRelease = (bool)consume.Invoke(null, new object[] { up });
                }
                else
                {
                    int id = (int)tracked.GetValue(null);
                    ateRelease = FlockFiveApp.SwallowLevelExit((bool)held.GetValue(null), id, FlockFiveApp.PointerEdge.Release, -1);
                    if (ateRelease) clear.Invoke(null, null);
                }
                bool releaseKept = pressCleared && !ateRelease && !(bool)held.GetValue(null);
                bool leavingDown = FlockFiveApp.DropExitOnPress(true, -1, -1, 3, 3);
                bool otherFinger = FlockFiveApp.DropExitOnPress(true, 4, 8, 3, 3);

                int mouse = FlockFiveApp.ExitFingerId(-1, null, true);
                int finger = FlockFiveApp.ExitFingerId(4, new int[] { 9, 4 }, true);
                int other = FlockFiveApp.ExitFingerId(4, new int[] { 9 }, true);
                int mouseVsFinger = FlockFiveApp.ExitFingerId(-1, new int[] { 9 }, true);
                bool mouseSwallow = FlockFiveApp.SwallowLevelExit(true, -1, FlockFiveApp.PointerEdge.Release, mouse);
                bool fingerSwallow = FlockFiveApp.SwallowLevelExit(true, 4, FlockFiveApp.PointerEdge.Release, finger);
                bool otherSwallow = FlockFiveApp.SwallowLevelExit(true, 4, FlockFiveApp.PointerEdge.Release, other);
                bool mouseEatsFinger = FlockFiveApp.SwallowLevelExit(true, -1, FlockFiveApp.PointerEdge.Release, mouseVsFinger);
                Check("home-exit-clears-on-focus-loss", focusCleared && pauseCleared,
                    "focus " + focusCleared + " pause " + pauseCleared);
                Check("home-exit-clears-on-fresh-press", releaseKept && !leavingDown && otherFinger,
                    "releaseKept " + releaseKept + " press " + pressCleared + " release " + ateRelease
                    + " event " + eventLive + " leaving " + leavingDown + " other " + otherFinger);
                Check("home-exit-matches-finger-id",
                    mouse == -1 && finger == 4 && other == 9 && mouseVsFinger == 9
                    && mouseSwallow && fingerSwallow && !otherSwallow && !mouseEatsFinger,
                    "mouse " + mouse + " finger " + finger + " other " + other + " echo " + mouseVsFinger);
            }
            finally
            {
                SetResumeUntil(keepResume);
                GamePause.Reset();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckHomeAndPrivacy(System.Action<string, bool, string> Check)
        {
            var gate = FlockFiveApp.BeginHomeGate(true, -1);
            bool opened = false;
            for (int i = 0; i < 120; i++)
            {
                if (FlockFiveApp.HomeGateAllows(ref gate, false, true, -1)) opened = true;
            }
            Check("home-tap-does-not-open-settings", !opened && !gate.FreshDown, "opened " + opened);
            bool down = FlockFiveApp.HomeGateAllows(ref gate, true, false, -1);
            bool up = FlockFiveApp.HomeGateAllows(ref gate, false, true, -1);
            var gear = new Rect(16f, 8f, 48f, 48f);
            bool gearTakes = up && FlockFiveApp.GearTakesRelease(false, -1, -1, true, gear.center, gear);
            Check("home-fresh-down-then-release", down && up && gate.FreshDown && gearTakes, "down " + down + " up " + up);
            bool unsettled = !FlockFiveApp.ToggleMayFlip(false, true, true);
            bool outside = !FlockFiveApp.ToggleMayFlip(true, false, true);
            bool immediate = !FlockFiveApp.SettingsPanelSettled(0.1f);
            bool settled = FlockFiveApp.ToggleMayFlip(FlockFiveApp.SettingsPanelSettled(0.25f), true, true);
            Check("privacy-toggle-needs-settled-gesture", unsettled && outside && immediate && settled,
                "unsettled " + unsettled + " outside " + outside + " immediate " + immediate + " settled " + settled);
        }

        static void CheckHomeTouchGate(System.Action<string, bool, string> Check)
        {
            var mismatched = FlockFiveApp.BeginHomeGate(true, 4);
            bool mouseClearsTouch = FlockFiveApp.HomeGateAllows(ref mismatched, false, true, -1);
            bool stillCarried = mismatched.CarryHeld && !mouseClearsTouch;
            bool touchClears = FlockFiveApp.HomeGateAllows(ref mismatched, false, true, 4);
            bool fresh = !mismatched.CarryHeld && touchClears == false
                && FlockFiveApp.HomeGateAllows(ref mismatched, true, false, 4);
            float keepResume = ResumeUntil();
            var go = new GameObject("home-touch-gate");
            bool liveHeld = false, focusCleared = false, resumeCleared = false;
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var t = typeof(FlockFiveApp);
                var gate = t.GetField("_homeGate", flags);
                var splash = t.GetField("_splash", flags);
                var home = t.GetField("_home", flags);
                splash.SetValue(app, true);
                home.SetValue(app, System.Enum.ToObject(home.FieldType, 0));
                gate.SetValue(app, FlockFiveApp.BeginHomeGate(true, 4));
                object[] pressArgs = { Vector2.zero };
                Method(t, "Pressed", typeof(Vector2).MakeByRefType()).Invoke(null, pressArgs);
                var afterPress = (FlockFiveApp.HomeGate)gate.GetValue(app);
                liveHeld = afterPress.CarryHeld && afterPress.CarryId == 4;
                try { Method(t, "OnGUIBody").Invoke(app, null); }
                catch (System.Exception) { }
                gate.SetValue(app, FlockFiveApp.BeginHomeGate(true, 4));
                Method(t, "OnApplicationFocus", typeof(bool)).Invoke(app, new object[] { false });
                focusCleared = !((FlockFiveApp.HomeGate)gate.GetValue(app)).Active;
                gate.SetValue(app, FlockFiveApp.BeginHomeGate(true, 4));
                Method(t, "OnApplicationPause", typeof(bool)).Invoke(app, new object[] { false });
                resumeCleared = !((FlockFiveApp.HomeGate)gate.GetValue(app)).Active;
            }
            finally
            {
                SetResumeUntil(keepResume);
                UnityEngine.Object.DestroyImmediate(go);
            }
            Check("home-touch-release-clears-gate",
                stillCarried && touchClears == false && !mismatched.CarryHeld && fresh && liveHeld && focusCleared && resumeCleared,
                "carried " + stillCarried + " fresh " + fresh + " live " + liveHeld
                + " focus " + focusCleared + " resume " + resumeCleared);
        }

        static void CheckGloveOneHandle(System.Action<string, bool, string> Check)
        {
            bool mouseKeepsTouchRelease = FlockFiveApp.HomePointerId(-1, true, 4) == 4;
            bool noReleaseStaysMouse = FlockFiveApp.HomePointerId(-1, false, 4) == -1;
            bool namedFingerWins = FlockFiveApp.HomePointerId(4, true, 9) == 4;
                var go = new GameObject("glove-one-handle");
                var limb = new GameObject("one-handle-limb");
                var destGo = new GameObject("one-handle-dest");
                var rootGo = new GameObject("one-handle-root");
                int taps = -1;
                bool homeWon = false;
                bool pressCleared = false, guiCleared = false;
                string pressTrace = "";
                bool moved = false;
                string upTrace = "";
                string upEx = "";
                string hopTrace = "";
                var keep = FlockFiveApp.EditorPointerInject;
                try
                {
                    var app = go.AddComponent<FlockFiveApp>();
                    var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    var t = typeof(FlockFiveApp);
                    var board = LevelData.Open(0);
                    var before = board.Clone();
                    bool level1 = FlockFiveApp.Level1First(board, i => false, i => false, out int from, out int to);
                    var views = new BranchView[board.Branches.Count];
                    views[from] = Limb(limb, from, Vector3.zero);
                    views[to] = Limb(destGo, to, new Vector3(3f, 0f, 0f));
                    var garden = new WorldBuilder.Garden();
                    garden.Root = rootGo.transform;
                    garden.Branches = views;
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, from);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_coachMoves", flags).SetValue(app, 0);
                t.GetField("_sel", flags).SetValue(app, -1);
                garden.Cam = WorldBuilder.MakeCamera(rootGo.transform);
                t.GetField("_garden", flags).SetValue(app, garden);
                float px = PhoneArea.W > 0f ? PhoneArea.W * 0.5f : 360f;
                float py = PhoneArea.H > 0f ? PhoneArea.H * 0.45f : 520f;
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 4,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                Method(t, "UpdateBody").Invoke(app, null);
                int afterDown = EditorTaps(app);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 4,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                SeatOpenLesson(app);
                Method(t, "TakeGuiPointer").Invoke(app, null);
                taps = EditorTaps(app);
                int picked = (int)t.GetField("_sel", flags).GetValue(app);
                upTrace = FlockFiveApp.EditorGloveTraceNow.ToString() + upEx;
                // Coach aims the destination with no finger down, the way the
                // frame after a select does. The glove quad is then on that cue.
                FlockFiveApp.EditorPointerInject = default;
                Method(t, "UpdateBody").Invoke(app, null);
                int aimed = (int)t.GetField("_cueBranch", flags).GetValue(app);
                var hopBody = new Rect(px - 50f, py - 50f, 100f, 100f);
                t.GetField("_gloveBody", flags).SetValue(app, hopBody);
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                if (aimed >= 0) t.GetField("_gloveVis", flags).SetValue(app, true);
                int selBeforeHop = (int)t.GetField("_sel", flags).GetValue(app);
                int movesBeforeHop = (int)t.GetField("_coachMoves", flags).GetValue(app);
                var boardBeforeHop = ((Board)t.GetField("_board", flags).GetValue(app)).Clone();
                int tookBefore = (int)t.GetField("_gloveTookFrame", flags).GetValue(app);
                t.GetField("_gloveTookFrame", flags).SetValue(app, -1);
                int tookAfter = (int)t.GetField("_gloveTookFrame", flags).GetValue(app);
                float now = AdPause.Unscaled;
                t.GetField("_nextTap", flags).SetValue(app, now);
                SeatOpenLesson(app);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 4,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                Method(t, "TakeGuiPointer").Invoke(app, null);
                bool hopLatched = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 4,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                Method(t, "TakeGuiPointer").Invoke(app, null);
                var hopTr = FlockFiveApp.EditorGloveTraceNow;
                var live = app.EditorLiveMove;
                const int settleCap = 240;
                int settledFrames = 0;
                bool settled = live == null;
                string stalledAt = null;
                if (live != null) settledFrames = PumpFrames(live, settleCap, null, out settled, out stalledAt);
                var afterBoard = (Board)t.GetField("_board", flags).GetValue(app);
                int selAfterHop = (int)t.GetField("_sel", flags).GetValue(app);
                int moves = (int)t.GetField("_coachMoves", flags).GetValue(app);
                bool boardChanged = !SameBirds(boardBeforeHop, afterBoard);
                bool stepAdvanced = moves > movesBeforeHop;
                if (live != null && !settled)
                    hopTrace = " move never settled after " + settleCap + " frames " + stalledAt
                        + " busy=" + GardenFit.Busy;
                hopTrace += " movedParts board=" + (boardChanged ? "diff" : "same")
                    + " step=" + movesBeforeHop + "->" + moves
                    + " settled=" + settled + " frames=" + settledFrames
                    + " hop: aim=" + aimed
                    + " glove=" + hopBody.x.ToString("0") + "," + hopBody.y.ToString("0") + ","
                    + hopBody.width.ToString("0") + "," + hopBody.height.ToString("0")
                    + " pt=" + px.ToString("0") + "," + py.ToString("0")
                    + " latch=" + hopLatched
                    + " took=" + tookBefore + "->" + tookAfter
                    + " next=" + now.ToString("0.00") + " now=" + AdPause.Unscaled.ToString("0.00")
                    + " sel=" + selBeforeHop + "->" + selAfterHop
                    + " board=" + (boardChanged ? "changed" : "same")
                    + " moves=" + moves
                    + " live=" + (live != null)
                    + " " + hopTr;
                moved = settled && level1 && boardChanged && stepAdvanced;
                bool once = afterDown == 0 && taps == 1 && picked == from && moved;

                var gate = t.GetField("_homeGate", flags);
                var splash = t.GetField("_splash", flags);
                var homeFace = t.GetField("_home", flags);
                Method(t, "OnEnable").Invoke(app, null);
                splash.SetValue(app, true);
                homeFace.SetValue(app, System.Enum.ToObject(homeFace.FieldType, 0));
                gate.SetValue(app, FlockFiveApp.BeginHomeGate(true, 4));
                var beforeGate = (FlockFiveApp.HomeGate)gate.GetValue(app);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer { On = true, Up = true, Id = 4 };
                object[] pressArgs = { Vector2.zero };
                Method(t, "Pressed", typeof(Vector2).MakeByRefType()).Invoke(null, pressArgs);
                var afterGate = (FlockFiveApp.HomeGate)gate.GetValue(app);
                var tr = FlockFiveApp.LastPressTrace;
                pressCleared = !afterGate.CarryHeld;
                pressTrace = "carry " + tr.CarryId + " event " + tr.EventId + " up " + tr.Up
                    + " before " + beforeGate.CarryHeld + "/" + beforeGate.CarryId + " a" + beforeGate.Active
                    + " after " + afterGate.CarryHeld + "/" + afterGate.CarryId + " a" + afterGate.Active
                    + " block " + tr.BlockRan + " app " + tr.HasApp
                    + " pressId " + tr.PressId + " upId " + tr.UpId + " down " + tr.Down;
                gate.SetValue(app, FlockFiveApp.BeginHomeGate(true, 4));
                try { Method(t, "OnGUIBody").Invoke(app, null); }
                catch (System.Exception) { }
                guiCleared = !((FlockFiveApp.HomeGate)gate.GetValue(app)).CarryHeld;

                splash.SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, 0);
                object[] hud = { default(Rect), default(Rect) };
                Method(t, "HudShaken", typeof(Rect).MakeByRefType(), typeof(Rect).MakeByRefType()).Invoke(null, hud);
                var restart = (Rect)hud[0];
                var home = CornerAnchor.TopBarHome(restart, PhoneArea.W, PhoneArea.Safe);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(home.center.x - 30f, home.center.y - 30f, 60f, 60f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Id = -1,
                    Play = new Vector2(home.center.x, PhoneArea.H - home.center.y)
                };
                Method(t, "UpdateBody").Invoke(app, null);
                homeWon = (bool)splash.GetValue(app) && EditorTaps(app) == 0;
                Check("glove-down-and-up-handle-once", once,
                    "down " + afterDown + " total " + taps + " sel " + picked + " moved " + moved
                    + " " + upTrace + hopTrace);
                Check("home-up-id-falls-through-to-release", mouseKeepsTouchRelease && noReleaseStaysMouse && namedFingerWins,
                    "kept " + mouseKeepsTouchRelease + " mouse " + noReleaseStaysMouse + " finger " + namedFingerWins);
                Check("home-touch-up-through-pressed-and-ongui", pressCleared && guiCleared,
                    "press " + pressCleared + " gui " + guiCleared + " " + pressTrace);
                Check("glove-cuff-on-home-goes-home", homeWon, "home " + homeWon + " taps " + EditorTaps(app));

                splash.SetValue(app, false);
                garden.Cam = new GameObject("glove-cam-slide").AddComponent<Camera>();
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, from);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 4,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                Method(t, "UpdateBody").Invoke(app, null);
                bool downLatched = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 4,
                    Play = new Vector2(home.center.x, PhoneArea.H - home.center.y)
                };
                try { Method(t, "OnGUIBody").Invoke(app, null); }
                catch (System.Exception) { }
                int slidSel = (int)t.GetField("_sel", flags).GetValue(app);
                bool slidHome = (bool)splash.GetValue(app);
                bool slid = downLatched && EditorTaps(app) == 0 && slidSel < 0 && !slidHome;
                splash.SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, from);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 4,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                Method(t, "UpdateBody").Invoke(app, null);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = -1,
                    Play = new Vector2(home.center.x, PhoneArea.H - home.center.y)
                };
                Method(t, "UpdateBody").Invoke(app, null);
                bool freshHome = (bool)splash.GetValue(app) && EditorTaps(app) == 0;
                Check("glove-slide-off-does-nothing", slid && freshHome,
                    "latch " + downLatched + " slid " + slid + " fresh " + freshHome + " taps " + EditorTaps(app));

                splash.SetValue(app, false);
                garden.Cam = new GameObject("glove-cam-finger").AddComponent<Camera>();
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, from);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 4,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                Method(t, "UpdateBody").Invoke(app, null);
                bool latched = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 5,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                try { Method(t, "OnGUIBody").Invoke(app, null); }
                catch (System.Exception) { }
                bool otherFinger = EditorTaps(app) == 0
                    && (int)t.GetField("_sel", flags).GetValue(app) < 0
                    && (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                bool focusDropped = false;
                string focusEx = "";
                try
                {
                    Method(t, "OnApplicationFocus", typeof(bool)).Invoke(app, new object[] { false });
                    focusDropped = !(bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                }
                catch (System.Exception ex)
                {
                    focusEx = " " + ex.GetType().Name;
                }
                Check("glove-second-finger-up-does-not-act", latched && otherFinger && focusDropped,
                    "latched " + latched + " other " + otherFinger + " focus " + focusDropped + focusEx);

                splash.SetValue(app, false);
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, from);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = true, Up = false, Id = 4,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                Method(t, "UpdateBody").Invoke(app, null);
                bool splashLatched = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                bool splashDropped = false;
                string splashEx = "";
                try
                {
                    Method(t, "ShowSplash").Invoke(app, null);
                    splashDropped = !(bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                }
                catch (System.Exception ex)
                {
                    splashEx = " " + ex.GetType().Name;
                }
                FlockFiveApp.EditorPointerInject = new FlockFiveApp.EditorPointer
                {
                    On = true, Down = false, Up = true, Id = 5,
                    Play = new Vector2(px, PhoneArea.H - py)
                };
                int tapsBeforeUp = EditorTaps(app);
                try { Method(t, "OnGUIBody").Invoke(app, null); }
                catch (System.Exception) { }
                bool splashUpQuiet = EditorTaps(app) == tapsBeforeUp;
                Check("glove-showsplash-drops-stale-latch", splashLatched && splashDropped && splashUpQuiet,
                    "latched " + splashLatched + " dropped " + splashDropped + " quiet " + splashUpQuiet + splashEx);
            }
            finally
            {
                FlockFiveApp.EditorPointerInject = keep;
                UnityEngine.Object.DestroyImmediate(destGo);
                UnityEngine.Object.DestroyImmediate(rootGo);
                UnityEngine.Object.DestroyImmediate(limb);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // A dismiss in an earlier check arms the static lesson guard. A live
        // Level 1 glove is not under that guard (ShowSplash zeroes it).
        static void CheckFreezeLeavesRepeatGate(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("freeze-next");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                SeatOpenLesson(app);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var t = typeof(FlockFiveApp);
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_cueFreeze", flags).SetValue(app, true);
                float mark = AdPause.Unscaled;
                t.GetField("_nextTap", flags).SetValue(app, mark);
                t.GetMethod("HandleTap", flags).Invoke(app, new object[] { Vector2.zero });
                float after = (float)t.GetField("_nextTap", flags).GetValue(app);
                Check("freeze-reject-does-not-arm-next-tap", after == mark, "mark " + mark + " after " + after);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckHop2KeepsDestination(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("hop2-cue");
            var root = new GameObject("hop2-root");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                SeatOpenLesson(app);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var t = typeof(FlockFiveApp);
                var garden = new WorldBuilder.Garden();
                garden.Root = root.transform;
                garden.Cam = WorldBuilder.MakeCamera(root.transform);
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_coachMoves", flags).SetValue(app, 1);
                t.GetField("_cueBranch", flags).SetValue(app, 4);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_glovePressLatched", flags).SetValue(app, true);
                var locked = t.GetField("_locked", flags).GetValue(app);
                locked.GetType().GetMethod("Add", new[] { typeof(int) }).Invoke(locked, new object[] { 0 });
                t.GetMethod("CoachAdvance", flags).Invoke(app, null);
                int cue = (int)t.GetField("_cueBranch", flags).GetValue(app);
                bool vis = (bool)t.GetField("_gloveVis", flags).GetValue(app);
                bool latch = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                Check("hop2-latched-keeps-destination", cue == 4 && vis && latch,
                    "cue " + cue + " vis " + vis + " latch " + latch);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static float ResumeUntil()
        {
            return (float)typeof(FlockFiveApp).GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        }

        static void NoteHeld(object app, int id)
        {
            typeof(FlockFiveApp).GetMethod("NoteDownAtHide", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(app, new object[] { id });
        }

        // Editor-only and internal members. The test assembly has no InternalsVisibleTo.
        static System.Reflection.FieldInfo EditorField(string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static;
            var found = typeof(FlockFiveApp).GetField(name, flags);
            if (found == null)
                throw new System.MissingFieldException(typeof(FlockFiveApp).FullName, name);
            return found;
        }

        static bool SnapArmed(object app)
        {
            return EditorField("EditorSnapRound").GetValue(app) != null;
        }

        static int EditorTaps(object app)
        {
            return (int)EditorField("EditorHandleTaps").GetValue(app);
        }

        static void SetEditorTaps(object app, int n)
        {
            EditorField("EditorHandleTaps").SetValue(app, n);
        }

        static string GloveExit()
        {
            object trace = EditorField("EditorGloveTraceNow").GetValue(null);
            return (string)trace.GetType().GetField("Exit").GetValue(trace);
        }

        static int ReplayGlove(FlockFiveApp app, Vector2 down, int downId, Vector2 up, int upId)
        {
            var replay = typeof(FlockFiveApp).GetMethod("DevReplayGloveTap",
                BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(Vector2), typeof(int), typeof(Vector2), typeof(int) }, null);
            return (int)replay.Invoke(app, new object[] { down, downId, up, upId });
        }

        static void SetResumeUntil(float until)
        {
            typeof(FlockFiveApp).GetField("_resumeInputUntil", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, until);
        }

        static void CheckResumeSwallow(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-swallow");
            var t = typeof(FlockFiveApp);
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, 0);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_coachMoves", flags).SetValue(app, 0);
                t.GetField("_glovePressLatched", flags).SetValue(app, true);
                t.GetField("_glovePressId", flags).SetValue(app, 4);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(0f, 0f, 80f, 80f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                NoteHeld(app, 4);
                bool swallowed = (bool)Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int))
                    .Invoke(app, new object[] { new Vector2(10f, 10f), true, 4 });
                bool latch = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                int sel = (int)t.GetField("_sel", flags).GetValue(app);
                int moves = (int)t.GetField("_coachMoves", flags).GetValue(app);
                bool route = swallowed && !latch && sel == -1 && moves == 0 && EditorTaps(app) == 0
                    && GloveExit() == "resume";
                t.GetField("_glovePressLatched", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, 0);
                Method(t, "HandleTap", typeof(Vector2)).Invoke(app, new object[] { Vector2.zero });
                bool tapLatch = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                int selAfter = (int)t.GetField("_sel", flags).GetValue(app);
                int movesAfter = (int)t.GetField("_coachMoves", flags).GetValue(app);
                bool tap = !tapLatch && selAfter == 0 && movesAfter == 0 && EditorTaps(app) == 0
                    && GloveExit() == "resume";
                Check("resume-swallow-blocks-glove", route && tap,
                    "route " + swallowed + " latch " + latch + " sel " + sel + "->" + selAfter
                    + " moves " + moves + "->" + movesAfter + " taps " + EditorTaps(app)
                    + " exit " + GloveExit());
            }
            finally
            {
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckResumeUpAfterSwallow(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-up-after");
            var t = typeof(FlockFiveApp);
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                float px = PhoneArea.W > 0f ? PhoneArea.W * 0.5f : 360f;
                float py = PhoneArea.H > 0f ? PhoneArea.H * 0.45f : 520f;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, 0);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_coachMoves", flags).SetValue(app, 0);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                var point = new Vector2(px + PhoneArea.OffX, py);
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                NoteHeld(app, 4);
                var glove = Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int));
                bool down = (bool)glove.Invoke(app, new object[] { point, false, 4 });
                bool latched = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                string downExit = GloveExit();
                SetResumeUntil(float.NegativeInfinity);
                bool up = (bool)glove.Invoke(app, new object[] { point, true, 4 });
                int sel = (int)t.GetField("_sel", flags).GetValue(app);
                int moves = (int)t.GetField("_coachMoves", flags).GetValue(app);
                string upExit = GloveExit();
                bool quiet = down && downExit == "resume" && !latched
                    && up && upExit == "swallowed-up"
                    && EditorTaps(app) == 0 && sel == -1 && moves == 0;
                Check("resume-up-after-swallowed-down-does-not-hop", quiet,
                    "down " + down + " " + downExit + " latch " + latched
                    + " up " + up + " " + upExit + " taps " + EditorTaps(app)
                    + " sel " + sel + " moves " + moves);
            }
            finally
            {
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckSwallowedReleaseScope(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("swallow-scope");
            var t = typeof(FlockFiveApp);
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                float px = PhoneArea.W > 0f ? PhoneArea.W * 0.5f : 360f;
                float py = PhoneArea.H > 0f ? PhoneArea.H * 0.45f : 520f;
                var point = new Vector2(px + PhoneArea.OffX, py);
                var off = new Vector2(-1000f, -1000f);
                var glove = Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int));
                var idField = t.GetField("_gloveSwallowedId", flags);
                int Swallowed() => (int)idField.GetValue(app);
                bool Arm()
                {
                    t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                    t.GetField("_splash", flags).SetValue(app, false);
                    t.GetField("_gloveVis", flags).SetValue(app, true);
                    t.GetField("_cueBranch", flags).SetValue(app, 0);
                    t.GetField("_cueForce", flags).SetValue(app, true);
                    t.GetField("_sel", flags).SetValue(app, -1);
                    t.GetField("_coachMoves", flags).SetValue(app, 0);
                    t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                    t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                    SetEditorTaps(app, 0);
                    SetResumeUntil(AdPause.Unscaled + 0.35f);
                    NoteHeld(app, 4);
                    bool down = (bool)glove.Invoke(app, new object[] { point, false, 4 });
                    SetResumeUntil(float.NegativeInfinity);
                    return down && GloveExit() == "resume"
                        && !(bool)t.GetField("_glovePressLatched", flags).GetValue(app)
                        && Swallowed() == 4;
                }
                bool ReleaseAt(Vector2 at, int id, out string exit)
                {
                    bool up = (bool)glove.Invoke(app, new object[] { at, true, id });
                    exit = GloveExit();
                    return up;
                }

                bool focusArmed = Arm();
                string focusEx = "";
                try { Method(t, "OnApplicationFocus", typeof(bool)).Invoke(app, new object[] { false }); }
                catch (System.Exception ex) { focusEx = TapFault(ex); }
                int focusId = Swallowed();
                bool focusUp = ReleaseAt(off, 4, out string focusExit);
                bool focusCleared = focusArmed && focusId == int.MinValue
                    && !focusUp && focusExit != "swallowed-up" && EditorTaps(app) == 0;
                Check("resume-focus-loss-clears-swallow", focusCleared,
                    "focus " + focusArmed + " id " + focusId + " " + focusExit + focusEx
                    + " taps " + EditorTaps(app));

                bool homeArmed = Arm();
                string homeEx = "";
                try { Method(t, "ShowSplash").Invoke(app, null); }
                catch (System.Exception ex) { homeEx = TapFault(ex); }
                int homeId = Swallowed();
                bool homeUp = ReleaseAt(off, 4, out string homeExit);
                bool loadArmed = Arm();
                string loadEx = "";
                try { Method(t, "Load", typeof(int)).Invoke(app, new object[] { 0 }); }
                catch (System.Exception ex) { loadEx = TapFault(ex); }
                int loadId = Swallowed();
                bool loadUp = ReleaseAt(off, 4, out string loadExit);
                bool exitArmed = Arm();
                Method(t, "ClearLevelExit").Invoke(null, null);
                int exitId = Swallowed();
                bool hideArmed = Arm();
                Method(t, "CoachHideNow").Invoke(app, null);
                int hideId = Swallowed();
                bool snapArmed = Arm();
                string snapEx = "";
                try
                {
                    var snap = (System.Collections.IEnumerator)Method(t, "SnapRound").Invoke(app, null);
                    snap.MoveNext();
                }
                catch (System.Exception ex) { snapEx = TapFault(ex); }
                int snapId = Swallowed();
                bool cancelArmed = Arm();
                Method(t, "NoteSwallowedCancel", typeof(int)).Invoke(app, new object[] { 4 });
                int cancelId = Swallowed();
                t.GetField("_gloveCancelFrame", flags).SetValue(app, -1);
                bool stateClears = exitArmed && hideArmed && snapArmed && cancelArmed
                    && exitId == int.MinValue && hideId == int.MinValue
                    && snapId == int.MinValue && cancelId == int.MinValue;
                bool left = homeArmed && loadArmed && homeId == int.MinValue && loadId == int.MinValue
                    && !homeUp && homeExit != "swallowed-up"
                    && !loadUp && loadExit != "swallowed-up";
                Check("resume-swallow-clears-on-home-and-load", left,
                    "home " + homeArmed + " id " + homeId + " " + homeExit + homeEx
                    + " load " + loadArmed + " id " + loadId + " " + loadExit + loadEx);
                Check("resume-swallow-clears-on-exit-hide-snap-cancel", stateClears,
                    "exit " + exitArmed + "/" + exitId + " hide " + hideArmed + "/" + hideId
                    + " snap " + snapArmed + "/" + snapId + snapEx
                    + " cancel " + cancelArmed + "/" + cancelId);
            }
            finally
            {
                SetResumeUntil(keep);
                GamePause.Reset();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckPointerIdExact(System.Action<string, bool, string> Check)
        {
            var mouse = FlockFiveApp.BeginHomeGate(true, -1);
            bool otherNeg = FlockFiveApp.HomeGateAllows(ref mouse, false, true, -2);
            bool mouseHeld = mouse.CarryHeld && !otherNeg;
            var same = FlockFiveApp.BeginHomeGate(true, -1);
            FlockFiveApp.HomeGateAllows(ref same, false, true, -1);
            bool mouseCleared = !same.CarryHeld;
            var missing = FlockFiveApp.BeginHomeGate(true, int.MinValue);
            FlockFiveApp.HomeGateAllows(ref missing, false, true, -1);
            bool missingHeld = missing.CarryHeld;
            Check("pointer-id-exact-on-home-gate", mouseHeld && mouseCleared && missingHeld,
                "held " + mouse.CarryHeld + " cleared " + !same.CarryHeld + " missing " + missing.CarryHeld);
        }

        static void CheckPlayerGloveHop(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("player-glove-hop");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                float px = PhoneArea.W > 0f ? PhoneArea.W * 0.5f : 360f;
                float py = PhoneArea.H > 0f ? PhoneArea.H * 0.45f : 520f;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, 0);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_coachMoves", flags).SetValue(app, 0);
                var body = new Rect(px - 40f, py - 40f, 80f, 80f);
                t.GetField("_gloveBody", flags).SetValue(app, body);
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                var down = new Vector2(px + PhoneArea.OffX, py);
                var up = new Vector2(px + PhoneArea.OffX + 180f, py + 180f);
                SetEditorTaps(app, 0);
                int hops = ReplayGlove(app, down, 7, up, -1);
                int hopsAgain = ReplayGlove(app, down, 7, up, -1);
                int hopsThird = ReplayGlove(app, down, 7, up, -1);
                Check("player-touch-down-mouse-up-hops-once", hops == 1 && hopsAgain == 1 && hopsThird == 1,
                    "hops " + hops + " again " + hopsAgain + " third " + hopsThird);
                var home = HudPlayRect(1);
                var onHome = HudWindow(home);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(home.center.x - 36f, home.center.y - 36f, 72f, 72f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                t.GetField("_coachMoves", flags).SetValue(app, 1);
                int overlap = ReplayGlove(app, onHome, 7, onHome, -1);
                t.GetField("_coachMoves", flags).SetValue(app, 2);
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                int overlapNext = ReplayGlove(app, onHome, 8, onHome, -1);
                Check("player-glove-on-hud-hops-at-later-steps", overlap == 1 && overlapNext == 1,
                    "step2 " + overlap + " step3 " + overlapNext);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckResumeFreshAndHeld(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-fresh-held");
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                float px = PhoneArea.W > 0f ? PhoneArea.W * 0.5f : 360f;
                float py = PhoneArea.H > 0f ? PhoneArea.H * 0.45f : 520f;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, 0);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 40f, py - 40f, 80f, 80f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                var point = new Vector2(px + PhoneArea.OffX, py);
                SetResumeUntil(AdPause.Unscaled + 0.10f);
                SetEditorTaps(app, 0);
                int fresh = ReplayGlove(app, point, 9, point, -1);
                NoteHeld(app, 4);
                t.GetField("_glovePressLatched", flags).SetValue(app, true);
                t.GetField("_glovePressId", flags).SetValue(app, 4);
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                var glove = Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int));
                bool heldUp = (bool)glove.Invoke(app, new object[] { point, true, 4 });
                Check("resume-fresh-tap-hops-held-press-swallowed",
                    fresh == 1 && heldUp && EditorTaps(app) == 0
                    && GloveExit() == "resume",
                    "fresh " + fresh + " held " + heldUp + " " + GloveExit()
                    + " taps " + EditorTaps(app));
            }
            finally
            {
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void SetInject(bool on, bool down, Vector2 play)
        {
            var nested = typeof(FlockFiveApp).GetNestedType("EditorPointer",
                BindingFlags.Public | BindingFlags.NonPublic);
            object box = System.Activator.CreateInstance(nested);
            nested.GetField("On").SetValue(box, on);
            nested.GetField("Down").SetValue(box, down);
            nested.GetField("Up").SetValue(box, false);
            nested.GetField("Id").SetValue(box, -1);
            nested.GetField("Play").SetValue(box, play);
            EditorField("EditorPointerInject").SetValue(null, box);
        }

        static void CheckGlovePressOwner(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("glove-press-owner");
            float keep = ResumeUntil();
            var prev = Event.current;
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, 0);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_coach", flags).SetValue(app, true);
                var home = HudPlayRect(1);
                var onHome = HudWindow(home);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(home.center.x - 36f, home.center.y - 36f, 72f, 72f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetResumeUntil(float.NegativeInfinity);
                SetEditorTaps(app, 0);
                var glove = Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int));
                glove.Invoke(app, new object[] { onHome, false, 7 });
                var screen = new Vector2(home.center.x, PhoneArea.H - home.center.y);
                Method(t, "HitHud", typeof(Vector2)).Invoke(app, new object[] { screen });
                bool downQuiet = !(bool)t.GetField("_splash", flags).GetValue(app) && !SnapArmed(app);
                glove.Invoke(app, new object[] { onHome, true, -1 });
                Method(t, "HitHud", typeof(Vector2)).Invoke(app, new object[] { screen });
                bool owned = EditorTaps(app) == 1 && downQuiet
                    && !(bool)t.GetField("_splash", flags).GetValue(app) && !SnapArmed(app);
                t.GetField("_gloveEchoFrame", flags).SetValue(app, Time.frameCount - 8);
                SetEditorTaps(app, 0);
                glove.Invoke(app, new object[] { onHome, true, -1 });
                bool oneHop = EditorTaps(app) == 0 && !SnapArmed(app);
                t.GetField("_glovePressLatched", flags).SetValue(app, false);
                t.GetField("_gloveEchoFrame", flags).SetValue(app, -1);
                SetEditorTaps(app, 0);
                bool toHud = !(bool)glove.Invoke(app, new object[] { onHome, true, -1 });
                var off = new Vector2((PhoneArea.W > 0f ? PhoneArea.W : 400f) * 0.5f + PhoneArea.OffX,
                    (PhoneArea.H > 0f ? PhoneArea.H : 800f) * 0.5f);
                bool dropped = (bool)glove.Invoke(app, new object[] { off, true, -1 });
                bool loose = toHud && dropped && EditorTaps(app) == 0;
                Check("glove-press-owns-home-and-restart", owned && oneHop && loose,
                    "owned " + owned + " once " + oneHop + " loose " + loose
                    + " taps " + EditorTaps(app) + " exit " + GloveExit());
            }
            finally
            {
                Event.current = prev;
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckResumeBranches(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-branches");
            float keep = ResumeUntil();
            var prev = Event.current;
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                var restart = HudPlayRect(2);
                var play = new Vector2(restart.center.x, PhoneArea.H - restart.center.y);
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                SetInject(true, true, play);
                t.GetField("_frozen", flags).SetValue(app, true);
                string frozenEx = "";
                try { Method(t, "UpdateBody").Invoke(app, null); }
                catch (System.Exception ex) { frozenEx = TapFault(ex); }
                bool frozen = !SnapArmed(app) && !(bool)t.GetField("_splash", flags).GetValue(app) && frozenEx.Length == 0;
                t.GetField("_frozen", flags).SetValue(app, false);
                var gift = t.GetField("_gift", flags);
                gift.SetValue(app, System.Enum.ToObject(gift.FieldType, 1));
                string giftEx = "";
                try { Method(t, "UpdateBody").Invoke(app, null); }
                catch (System.Exception ex) { giftEx = TapFault(ex); }
                bool gifted = !SnapArmed(app) && giftEx.Length == 0;
                gift.SetValue(app, System.Enum.ToObject(gift.FieldType, 0));
                var hold = typeof(SeasonReveal).GetProperty("HoldsPlay", BindingFlags.Public | BindingFlags.Static);
                hold.GetSetMethod(true).Invoke(null, new object[] { true });
                string seasonEx = "";
                try { Method(t, "UpdateBody").Invoke(app, null); }
                catch (System.Exception ex) { seasonEx = TapFault(ex); }
                bool season = !SnapArmed(app) && seasonEx.Length == 0;
                var appField = t.GetField("_app", BindingFlags.NonPublic | BindingFlags.Static);
                var live = appField.GetValue(null);
                appField.SetValue(null, null);
                var home = HudPlayRect(1);
                var down = new Event { type = EventType.MouseDown, mousePosition = HudWindow(home), button = 0 };
                Event.current = down;
                Method(t, "SwallowResumePointer").Invoke(null, null);
                bool left = down.type != EventType.Used;
                appField.SetValue(null, live);
                Check("resume-branches-leave-restart",
                    frozen && gifted && season && left,
                    "frozen " + frozen + frozenEx + " gift " + gifted + giftEx
                    + " season " + season + seasonEx + " null " + left);
            }
            finally
            {
                var hold = typeof(SeasonReveal).GetProperty("HoldsPlay", BindingFlags.Public | BindingFlags.Static);
                if (hold != null) hold.GetSetMethod(true).Invoke(null, new object[] { false });
                SetInject(false, false, default);
                Event.current = prev;
                SetResumeUntil(keep);
                GamePause.Reset();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckRestartClearsCaptions(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("restart-captions");
            var key = "flockfive.coach.done";
            bool had = PlayerPrefs.HasKey(key);
            int keep = PlayerPrefs.GetInt(key, 0);
            bool hadKit = PlayerPrefs.HasKey(RuleTips.KitKey);
            int kitKeep = PlayerPrefs.GetInt(RuleTips.KitKey, 0);
            try
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.DeleteKey(RuleTips.KitKey);
                LevelData.Open(0);
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var board = LevelData.Open(0);
                t.GetField("_board", flags).SetValue(app, board);
                bool crowns = RearmFromCaption(app, t, flags, 1, RuleTips.KitLine);
                bool nice = RearmFromCaption(app, t, flags, 2, RuleTips.FeederLine);
                if (board.Branches.Count > 0 && board.Branches[0].Birds.Count > 0)
                {
                    var bird = board.Branches[0].Birds[0];
                    board.Branches[0].Birds[0] = new Bird(bird.Color, BirdKit.Crown);
                }
                t.GetField("_ruleTip", flags).SetValue(app, System.Enum.ToObject(
                    t.GetField("_ruleTip", flags).FieldType, 0));
                t.GetField("_ruleTurned", flags).SetValue(app, false);
                t.GetField("_cueLine", flags).SetValue(app, null);
                Method(t, "SettleRestartLesson").Invoke(app, null);
                bool advanced = (bool)Method(t, "RuleTipAdvance").Invoke(app, null);
                Method(t, "ShowRuleTip", typeof(RuleTip)).Invoke(app, new object[] { RuleTip.Kit });
                var tip = t.GetField("_ruleTip", flags).GetValue(app).ToString();
                string line = (string)t.GetField("_cueLine", flags).GetValue(app);
                bool held = (bool)t.GetField("_holdRuleForLesson", flags).GetValue(app);
                bool stale = !advanced && tip == "None" && line != null && line.IndexOf("Tap a branch") >= 0
                    && line.IndexOf("Crowns") < 0 && held;
                Check("restart-clears-crowns-and-rearms-step-one", crowns,
                    "crowns " + crowns);
                Check("restart-clears-nice-and-rearms-step-one", nice, "nice " + nice);
                Check("restart-after-fifth-red-does-not-replay-crowns", stale,
                    "advanced " + advanced + " tip " + tip + " line " + line + " held " + held);
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(key, keep);
                else PlayerPrefs.DeleteKey(key);
                if (hadKit) PlayerPrefs.SetInt(RuleTips.KitKey, kitKeep);
                else PlayerPrefs.DeleteKey(RuleTips.KitKey);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static bool RearmFromCaption(FlockFiveApp app, System.Type t, BindingFlags flags, int tipValue, string caption)
        {
            t.GetField("_ruleTip", flags).SetValue(app, System.Enum.ToObject(
                t.GetField("_ruleTip", flags).FieldType, tipValue));
            t.GetField("_ruleTurned", flags).SetValue(app, true);
            t.GetField("_cueLine", flags).SetValue(app, caption);
            t.GetField("_gloveVis", flags).SetValue(app, false);
            t.GetField("_cueBranch", flags).SetValue(app, -1);
            Method(t, "SettleRestartLesson").Invoke(app, null);
            var tip = t.GetField("_ruleTip", flags).GetValue(app).ToString();
            string line = (string)t.GetField("_cueLine", flags).GetValue(app);
            bool vis = (bool)t.GetField("_gloveVis", flags).GetValue(app);
            bool cue = (int)t.GetField("_cueBranch", flags).GetValue(app) >= 0;
            return tip == "None" && line != null && line.IndexOf("Tap a branch") >= 0
                && line.IndexOf("Crowns") < 0 && line.IndexOf("Nice!") < 0 && vis && cue;
        }

        static void CheckHomeControlsAtSizes(System.Action<string, bool, string> Check)
        {
            int[] w = { 820, 402, 412 };
            int[] h = { 1180, 874, 915 };
            bool clear = true;
            string detail = "";
            for (int i = 0; i < w.Length; i++)
            {
                var safe = new Rect(0f, 34f, w[i], h[i] - 34f - 59f);
                var buttons = new Rect[4];
                FlockFiveApp.FillMainButtons(w[i], h[i], safe, buttons, true);
                bool play = buttons[0].width > 2f && buttons[0].height > 2f;
                bool gear = buttons[3].width > 2f && buttons[3].height > 2f;
                bool restart = buttons[1].width > 2f && buttons[1].height > 2f;
                bool home = CornerAnchor.TopBarHome(buttons[1], w[i], safe).width > 2f;
                bool apart = play && gear && restart && home && !buttons[0].Overlaps(buttons[3]);
                var garden = new Rect[4];
                FlockFiveApp.FillMainButtons(w[i], h[i], safe, garden, false);
                bool noGear = garden[3].width <= 0f;
                if (!apart || !noGear) clear = false;
                detail += w[i] + " play " + play + " gear " + gear + " apart " + apart + " bare " + noGear + " ";
            }
            bool tappable = HomeGear.AcceptsTap(new HomeGearQuery { Home = true, ForcedTap = false });
            Check("home-chain-keeps-play-and-gear", clear && tappable, detail);
        }

        static void CheckPauseKeepsHeldId(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("pause-keeps-id");
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_glovePressLatched", flags).SetValue(app, true);
                t.GetField("_glovePressId", flags).SetValue(app, 6);
                string pauseEx = "";
                try { Method(t, "OnApplicationPause", typeof(bool)).Invoke(app, new object[] { true }); }
                catch (System.Exception ex) { pauseEx = TapFault(ex); }
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                bool holds = (bool)Method(t, "ResumeHolds", typeof(int)).Invoke(app, new object[] { 6 });
                bool fresh = (bool)Method(t, "ResumeHolds", typeof(int)).Invoke(app, new object[] { 9 });
                Check("pause-keeps-the-held-pointer", holds && !fresh, "holds " + holds + " fresh " + fresh + pauseEx);
            }
            finally
            {
                SetResumeUntil(keep);
                GamePause.Reset();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static Rect HudPlayRect(int which)
        {
            object[] hud = { default(Rect), default(Rect) };
            typeof(FlockFiveApp).GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, hud);
            var restart = (Rect)hud[0];
            var home = CornerAnchor.TopBarHome(restart, PhoneArea.W, PhoneArea.Safe);
            var gear = FlockFiveApp.SettingsGearRect(PhoneArea.W, PhoneArea.H, PhoneArea.Safe);
            var rect = which == 2 ? restart : which == 3 ? gear : home;
            if (rect.width <= 0f || rect.height <= 0f)
                throw new System.Exception("[home-polish] edit-mode hud rect " + which + " is empty: " + rect);
            return rect;
        }

        static Vector2 HudWindow(Rect play)
        {
            return new Vector2(play.center.x + PhoneArea.OffX, play.center.y);
        }

        static void CheckResumeOtherFingerHome(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-other-home");
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                float px = PhoneArea.W > 0f ? PhoneArea.W * 0.5f : 360f;
                float py = PhoneArea.H > 0f ? PhoneArea.H * 0.45f : 520f;
                var point = new Vector2(px + PhoneArea.OffX, py);
                var home = HudPlayRect(1);
                var homePt = HudWindow(home);
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, 0);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                var glove = Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int));
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                bool down = (bool)glove.Invoke(app, new object[] { point, false, 4 });
                SetResumeUntil(float.NegativeInfinity);
                bool up = (bool)glove.Invoke(app, new object[] { homePt, true, 5 });
                int id = (int)t.GetField("_gloveSwallowedId", flags).GetValue(app);
                bool onHome = FlockFiveApp.HudBeatsGlove(home.center, home, new Rect(0f, 0f, 0f, 0f), new Rect(0f, 0f, 0f, 0f), false);
                Check("resume-swallow-leaves-other-finger",
                    onHome && down && !up && GloveExit() == "no-glove"
                    && id == 4 && EditorTaps(app) == 0,
                    "down " + down + " up " + up + " "
                    + GloveExit() + " id " + id);
            }
            finally
            {
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckResumeButtonBegan(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-button-began");
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                float px = PhoneArea.W > 0f ? PhoneArea.W * 0.5f : 360f;
                float py = PhoneArea.H > 0f ? PhoneArea.H * 0.45f : 520f;
                var point = new Vector2(px + PhoneArea.OffX, py);
                var home = HudPlayRect(1);
                var homePt = HudWindow(home);
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, 0);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                var glove = Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int));
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                NoteHeld(app, 4);
                bool down = (bool)glove.Invoke(app, new object[] { homePt, false, 4 });
                string downExit = GloveExit();
                int id = (int)t.GetField("_gloveSwallowedId", flags).GetValue(app);
                SetResumeUntil(float.NegativeInfinity);
                bool up = (bool)glove.Invoke(app, new object[] { homePt, true, 4 });
                bool began = !down && downExit == "resume-hud" && id == 4
                    && !up && GloveExit() == "hud" && EditorTaps(app) == 0;
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                bool gloveDown = (bool)glove.Invoke(app, new object[] { point, false, 4 });
                SetResumeUntil(float.NegativeInfinity);
                SetEditorTaps(app, 0);
                bool slide = (bool)glove.Invoke(app, new object[] { homePt, true, 4 });
                bool slideEaten = gloveDown && slide && GloveExit() == "swallowed-up"
                    && EditorTaps(app) == 0;
                Check("resume-button-press-still-fires", began && slideEaten,
                    "down " + down + " " + downExit + " up " + up
                    + " slide " + slide + " " + GloveExit());
            }
            finally
            {
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static bool FeedHud(FlockFiveApp app, int which, Rect play, bool down, bool up)
        {
            var t = typeof(FlockFiveApp);
            object[] args = { which, play, HudWindow(play), down, up, false };
            var feed = t.GetMethod("FeedResumeHudButton",
                BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[] { typeof(int), typeof(Rect), typeof(Vector2), typeof(bool), typeof(bool), typeof(bool).MakeByRefType() },
                null);
            return (bool)feed.Invoke(app, args);
        }

        static void CheckResumeHomeFires(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-home-fires");
            float keep = ResumeUntil();
            var prev = Event.current;
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                var home = HudPlayRect(1);
                var at = HudWindow(home);
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                var down = new Event { type = EventType.MouseDown, mousePosition = at, button = 0 };
                Event.current = down;
                Method(t, "SwallowResumePointer").Invoke(null, null);
                bool left = down.type != EventType.Used;
                bool armed = FeedHud(app, 1, home, true, false);
                SetResumeUntil(float.NegativeInfinity);
                var up = new Event { type = EventType.MouseUp, mousePosition = at, button = 0 };
                Event.current = up;
                bool fired = false;
                string fireEx = "";
                try { fired = FeedHud(app, 1, home, false, true); }
                catch (System.Exception ex) { fireEx = TapFault(ex); }
                bool splash = (bool)t.GetField("_splash", flags).GetValue(app);
                Check("resume-home-press-fires", left && !armed && splash,
                    "left " + left + " type " + down.type
                    + " armed " + armed + " fired " + fired + " splash " + splash + fireEx);
            }
            finally
            {
                Event.current = prev;
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static bool FreshDownLeft(FlockFiveApp app, Vector2 at)
        {
            var down = new Event { type = EventType.MouseDown, mousePosition = at, button = 0 };
            Event.current = down;
            Method(typeof(FlockFiveApp), "SwallowResumePointer").Invoke(null, null);
            return down.type != EventType.Used;
        }

        static void CheckFreshResumeSurfaces(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("fresh-resume-surfaces");
            float keep = ResumeUntil();
            var prev = Event.current;
            int streak = Purse.Streak;
            try
            {
                Purse.SetStreak(0);
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                SetResumeUntil(AdPause.Unscaled + 0.10f);
                var home = HudPlayRect(1);
                bool homeLeft = FreshDownLeft(app, HudWindow(home));
                var restart = HudPlayRect(2);
                var screen = new Vector2(restart.center.x, PhoneArea.H - restart.center.y);
                try { Method(t, "GardenHudAct", typeof(Vector2)).Invoke(app, new object[] { screen }); }
                catch (System.Exception) { }
                bool restartLeftDown = !SnapArmed(app)
                    && !(bool)t.GetField("_restarting", flags).GetValue(app)
                    && t.GetField("_restartAsk", flags).GetValue(app).ToString() == "None";
                t.GetField("_gloveEdgeId", flags).SetValue(app, 11);
                SetEditorTaps(app, 0);
                string boardEx = "";
                try { Method(t, "HandleTap", typeof(Vector2)).Invoke(app, new object[] { Vector2.zero }); }
                catch (System.Exception ex) { boardEx = TapFault(ex); }
                bool board = EditorTaps(app) == 1
                    && GloveExit() != "resume";
                t.GetField("_splash", flags).SetValue(app, true);
                var homeFace = t.GetField("_home", flags);
                homeFace.SetValue(app, System.Enum.ToObject(homeFace.FieldType, 0));
                bool gearLeft = FreshDownLeft(app, HudWindow(HudPlayRect(3)));
                NoteHeld(app, -1);
                var heldHome = new Event { type = EventType.MouseDown, mousePosition = HudWindow(home), button = 0 };
                Event.current = heldHome;
                Method(t, "SwallowResumePointer").Invoke(null, null);
                bool heldEaten = heldHome.type == EventType.Used || heldHome.type != EventType.MouseDown;
                int[] w = { 820, 402, 412 };
                int[] h = { 1180, 874, 915 };
                bool sized = true;
                for (int i = 0; i < w.Length; i++)
                {
                    var safe = new Rect(0f, 34f, w[i], h[i] - 34f - 59f);
                    CornerAnchor.Garden(w[i], h[i], safe, out _, out _, out _, out var gardenRestart, out _);
                    var gardenHome = CornerAnchor.TopBarHome(gardenRestart, w[i], safe);
                    var gear = FlockFiveApp.SettingsGearRect(w[i], h[i], safe);
                    bool phone = w[i] < 500;
                    bool face = gardenHome.width > 2f && gear.width > 2f && !gardenHome.Overlaps(gear);
                    bool garden = gardenRestart.width > 2f;
                    if (!face || (phone && !garden)) sized = false;
                }
                Check("resume-fresh-home-gear-board",
                    homeLeft && gearLeft && heldEaten && board && sized && restartLeftDown,
                    "home " + homeLeft + " gear " + gearLeft + " held " + heldEaten
                    + " board " + board + " sized " + sized + " restart " + restartLeftDown + boardEx);
            }
            finally
            {
                Event.current = prev;
                Purse.SetStreak(streak);
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckHomeChainOneAtATime(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("home-chain");
            var prev = Event.current;
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_splash", flags).SetValue(app, true);
                var homeFace = t.GetField("_home", flags);
                homeFace.SetValue(app, System.Enum.ToObject(homeFace.FieldType, 0));
                t.GetField("_railInit", flags).SetValue(app, true);
                t.GetField("_adoptLive", flags).SetValue(app, true);
                var step = t.GetField("_adoptStep", flags);
                step.SetValue(app, System.Enum.ToObject(step.FieldType, 1));
                bool greetHolds = (bool)Method(t, "AdoptHoldsQueue").Invoke(app, null);
                t.GetField("_adoptLive", flags).SetValue(app, false);
                step.SetValue(app, System.Enum.ToObject(step.FieldType, 0));
                t.GetField("_avatarRename", flags).SetValue(app, true);
                bool nameHolds = (bool)Method(t, "AdoptHoldsQueue").Invoke(app, null);
                t.GetField("_avatarRename", flags).SetValue(app, false);
                t.GetField("_dailyIntro", flags).SetValue(app, true);
                t.GetField("_dailyIntroLive", flags).SetValue(app, true);
                t.GetField("_adoptLive", flags).SetValue(app, true);
                bool gearDuring = HomeGear.AcceptsTap((HomeGearQuery)Method(t, "HomeGearNow").Invoke(app, null));
                var level = SplashPress.LevelRect(PhoneArea.W, PhoneArea.H, PhoneArea.Safe);
                var playAt = new Vector2(level.center.x, level.center.y);
                var playDown = new Event { type = EventType.MouseDown, mousePosition = playAt, button = 0 };
                Event.current = playDown;
                string playEx = "";
                try { Method(t, "SwallowOffTargetTap").Invoke(app, null); }
                catch (System.Exception ex) { playEx = TapFault(ex); }
                bool playLeft = playDown.type != EventType.Used;
                bool greetHeld = !(bool)t.GetField("_adoptGreetDone", flags).GetValue(app);
                var missAt = new Vector2(level.xMax + 48f, 12f);
                if (level.width > 2f && level.Contains(missAt))
                    missAt = new Vector2(level.center.x, level.yMax + 48f);
                var miss = new Event { type = EventType.MouseDown, mousePosition = missAt, button = 0 };
                Event.current = miss;
                try { Method(t, "SwallowOffTargetTap").Invoke(app, null); }
                catch (System.Exception) { }
                bool greetDone = (bool)t.GetField("_adoptGreetDone", flags).GetValue(app);
                bool playCan = false;
                string canEx = "";
                try { playCan = (bool)Method(t, "LevelCanTakeTap", typeof(Rect)).Invoke(app, new object[] { level }); }
                catch (System.Exception ex) { canEx = TapFault(ex); }
                Check("home-captions-queue-and-leave-play",
                    greetHolds && nameHolds && gearDuring && playLeft && greetHeld && greetDone && playCan,
                    "greet " + greetHolds + " name " + nameHolds + " gear " + gearDuring
                    + " play " + playLeft + " held " + greetHeld + " done " + greetDone
                    + " can " + playCan + playEx + canEx);
            }
            finally
            {
                Event.current = prev;
                GamePause.Reset();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckResumeRestartFires(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-restart-fires");
            float keep = ResumeUntil();
            var prev = Event.current;
            int streak = Purse.Streak;
            try
            {
                Purse.SetStreak(0);
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                var restart = HudPlayRect(2);
                var at = HudWindow(restart);
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                var down = new Event { type = EventType.MouseDown, mousePosition = at, button = 0 };
                Event.current = down;
                Method(t, "SwallowResumePointer").Invoke(null, null);
                bool swallowed = down.type != EventType.MouseDown || down.type == EventType.Used;
                bool going = SnapArmed(app)
                    || (bool)t.GetField("_restarting", flags).GetValue(app)
                    || t.GetField("_restartAsk", flags).GetValue(app).ToString() != "None";
                Check("resume-restart-stays-swallowed", swallowed && !going,
                    "type " + down.type + " going " + going);
            }
            finally
            {
                Event.current = prev;
                Purse.SetStreak(streak);
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckResumeGearFires(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-gear-fires");
            float keep = ResumeUntil();
            var prev = Event.current;
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_splash", flags).SetValue(app, true);
                var homeFace = t.GetField("_home", flags);
                homeFace.SetValue(app, System.Enum.ToObject(homeFace.FieldType, 0));
                var gear = HudPlayRect(3);
                var at = HudWindow(gear);
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                var down = new Event { type = EventType.MouseDown, mousePosition = at, button = 0 };
                Event.current = down;
                Method(t, "SwallowResumePointer").Invoke(null, null);
                bool left = down.type != EventType.Used;
                FeedHud(app, 3, gear, true, false);
                SetResumeUntil(float.NegativeInfinity);
                var up = new Event { type = EventType.MouseUp, mousePosition = at, button = 0 };
                Event.current = up;
                bool fired = false;
                string fireEx = "";
                try { fired = FeedHud(app, 3, gear, false, true); }
                catch (System.Exception ex) { fireEx = TapFault(ex); }
                bool open = (bool)t.GetField("_settingsOpen", flags).GetValue(app);
                Check("resume-gear-press-fires", left && open,
                    "left " + left + " type " + down.type
                    + " fired " + fired + " open " + open + fireEx);
            }
            finally
            {
                Event.current = prev;
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckResumeArmClears(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-arm-clears");
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                var home = HudPlayRect(1);
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                bool armed = FeedHud(app, 1, home, true, false);
                int arm = (int)t.GetField("_resumeHudArm", flags).GetValue(app);
                Method(t, "ClearSwallowedGlove").Invoke(app, null);
                int cleared = (int)t.GetField("_resumeHudArm", flags).GetValue(app);
                SetResumeUntil(float.NegativeInfinity);
                bool fired = FeedHud(app, 1, home, false, true);
                bool splash = (bool)t.GetField("_splash", flags).GetValue(app);
                Check("resume-hud-arm-clears-with-swallow",
                    !armed && arm == 1 && cleared == 0 && !fired && !splash,
                    "arm " + arm + "->" + cleared + " fired " + fired + " splash " + splash);
            }
            finally
            {
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckResumeArmClearsOnPause(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("resume-arm-pause");
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                var home = HudPlayRect(1);
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                bool armed = FeedHud(app, 1, home, true, false);
                int arm = (int)t.GetField("_resumeHudArm", flags).GetValue(app);
                string pauseEx = "";
                try { Method(t, "OnApplicationPause", typeof(bool)).Invoke(app, new object[] { true }); }
                catch (System.Exception ex) { pauseEx = TapFault(ex); }
                int after = (int)t.GetField("_resumeHudArm", flags).GetValue(app);
                SetResumeUntil(float.NegativeInfinity);
                bool fired = FeedHud(app, 1, home, false, true);
                bool splash = (bool)t.GetField("_splash", flags).GetValue(app);
                Check("resume-hud-arm-clears-on-pause",
                    !armed && arm == 1 && after == 0 && !fired && !splash,
                    "arm " + arm + "->" + after + " fired " + fired + " splash " + splash + pauseEx);
            }
            finally
            {
                SetResumeUntil(keep);
                GamePause.Reset();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckGardenGearGoesToGlove(System.Action<string, bool, string> Check)
        {
            var go = new GameObject("garden-gear-tap");
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, 0);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_sel", flags).SetValue(app, -1);
                var gear = HudPlayRect(3);
                t.GetField("_gloveBody", flags).SetValue(app, gear);
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                var point = HudWindow(gear);
                var glove = Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int));
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                bool inDown = (bool)glove.Invoke(app, new object[] { point, false, 4 });
                string inExit = GloveExit();
                int inArm = (int)t.GetField("_resumeHudArm", flags).GetValue(app);
                bool inGear = (bool)t.GetField("_settingsOpen", flags).GetValue(app);
                SetResumeUntil(float.NegativeInfinity);
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                bool outDown = (bool)glove.Invoke(app, new object[] { point, false, 4 });
                string outExit = GloveExit();
                bool latched = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                bool outGear = (bool)t.GetField("_settingsOpen", flags).GetValue(app);
                t.GetField("_gloveBody", flags).SetValue(app, new Rect(0f, 0f, 4f, 4f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetResumeUntil(AdPause.Unscaled + 0.35f);
                bool missIn = (bool)glove.Invoke(app, new object[] { point, false, 8 });
                SetResumeUntil(float.NegativeInfinity);
                bool missOut = (bool)glove.Invoke(app, new object[] { point, false, 8 });
                bool stillGear = (bool)t.GetField("_settingsOpen", flags).GetValue(app);
                bool beats = FlockFiveApp.HudBeatsGlove(gear.center, new Rect(0f, 0f, 0f, 0f),
                    new Rect(0f, 0f, 0f, 0f), gear, false);
                bool inLatch = (bool)t.GetField("_glovePressLatched", flags).GetValue(app);
                Check("garden-gear-rect-goes-to-glove-or-board",
                    inDown && inLatch && inExit != "resume-hud" && inArm == 0 && !inGear
                    && outDown && latched && outExit != "resume-hud" && !outGear
                    && !missIn && !missOut && !stillGear && !beats
                    && EditorTaps(app) == 0,
                    "in " + inDown + " " + inExit + " arm " + inArm
                    + " out " + outDown + " " + outExit + " latch " + latched
                    + " miss " + missIn + "/" + missOut + " gear " + inGear + "/" + outGear
                    + " beats " + beats);
            }
            finally
            {
                SetResumeUntil(keep);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void CheckGearFeedsOnHomeFace(System.Action<string, bool, string> Check)
        {
            bool garden = !HomeGear.Shown(new HomeGearQuery { Home = false });
            bool home = HomeGear.Shown(new HomeGearQuery { Home = true });
            string settings = System.IO.File.ReadAllText(System.IO.Path.Combine(
                Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Settings.cs"));
            int poll = settings.IndexOf("void PollHomeGear()");
            int tick = settings.IndexOf("void TickSettingsMotion()", poll);
            string pollBody = poll >= 0 && tick > poll ? settings.Substring(poll, tick - poll) : "";
            bool splashFeeds = pollBody.Contains("FeedResumeHudButton(3");
            string app = System.IO.File.ReadAllText(System.IO.Path.Combine(
                Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            int stamp = app.IndexOf("DrawGardenStamp(s);");
            int hive = stamp >= 0 ? app.IndexOf("if (HiveReachable())", stamp) : -1;
            string level = stamp >= 0 && hive > stamp ? app.Substring(stamp, hive - stamp) : "";
            bool levelFeedsGear = level.Contains("FeedResumeHudButton(3");
            Check("gear-feeds-on-the-home-face",
                garden && home && splashFeeds && !levelFeedsGear,
                "garden " + garden + " home " + home + " splash " + splashFeeds
                + " level " + levelFeedsGear);
        }

        static void CheckResumeFollowups(System.Action<string, bool, string> Check)
        {
            bool exact = FlockFiveApp.GloveFingerMatches(-1, -1)
                && !FlockFiveApp.GloveFingerMatches(-1, -2)
                && !FlockFiveApp.GloveFingerMatches(int.MinValue, -1)
                && FlockFiveApp.GloveFingerMatches(4, 4)
                && !FlockFiveApp.GloveFingerMatches(4, 5);
            Check("glove-finger-matches-exact-id", exact, "mouse and missing id");

            var go = new GameObject("resume-follow");
            var t = typeof(FlockFiveApp);
            float keep = ResumeUntil();
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                float px = PhoneArea.W > 0f ? PhoneArea.W * 0.5f : 360f;
                float py = PhoneArea.H > 0f ? PhoneArea.H * 0.45f : 520f;
                var point = new Vector2(px + PhoneArea.OffX, py);
                var glove = Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int));
                var idField = t.GetField("_gloveSwallowedId", flags);
                int Swallowed() => (int)idField.GetValue(app);
                void Pose()
                {
                    t.GetField("_board", flags).SetValue(app, LevelData.Open(0));
                    t.GetField("_splash", flags).SetValue(app, false);
                    t.GetField("_gloveVis", flags).SetValue(app, true);
                    t.GetField("_cueBranch", flags).SetValue(app, 0);
                    t.GetField("_cueForce", flags).SetValue(app, true);
                    t.GetField("_sel", flags).SetValue(app, -1);
                    t.GetField("_coachMoves", flags).SetValue(app, 0);
                    t.GetField("_gloveBody", flags).SetValue(app, new Rect(px - 50f, py - 50f, 100f, 100f));
                    t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                    t.GetField("_glovePressLatched", flags).SetValue(app, false);
                    SetEditorTaps(app, 0);
                }
                bool Arm()
                {
                    Pose();
                    SetResumeUntil(AdPause.Unscaled + 0.35f);
                    NoteHeld(app, 4);
                    bool down = (bool)glove.Invoke(app, new object[] { point, false, 4 });
                    SetResumeUntil(float.NegativeInfinity);
                    return down && GloveExit() == "resume" && Swallowed() == 4;
                }

                bool armedFocus = Arm();
                string focusEx = "";
                try { Method(t, "OnApplicationFocus", typeof(bool)).Invoke(app, new object[] { true }); }
                catch (System.Exception ex) { focusEx = TapFault(ex); }
                int afterFocus = Swallowed();
                SetResumeUntil(float.NegativeInfinity);
                bool focusUp = (bool)glove.Invoke(app, new object[] { point, true, 4 });
                bool focusKeeps = armedFocus && afterFocus == 4 && focusUp
                    && GloveExit() == "swallowed-up"
                    && EditorTaps(app) == 0;
                Check("resume-focus-gain-keeps-swallowed-id", focusKeeps,
                    "armed " + armedFocus + " id " + afterFocus + " up " + focusUp
                    + " " + GloveExit() + focusEx
                    + " taps " + EditorTaps(app));

                Pose();
                t.GetField("_glovePressLatched", flags).SetValue(app, true);
                t.GetField("_glovePressId", flags).SetValue(app, 4);
                idField.SetValue(app, int.MinValue);
                string pauseEx = "";
                try
                {
                    Method(t, "OnApplicationPause", typeof(bool)).Invoke(app, new object[] { true });
                    Method(t, "OnApplicationPause", typeof(bool)).Invoke(app, new object[] { false });
                }
                catch (System.Exception ex) { pauseEx = TapFault(ex); }
                int held = Swallowed();
                SetResumeUntil(float.NegativeInfinity);
                Pose();
                idField.SetValue(app, held);
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                SetEditorTaps(app, 0);
                bool pauseUp = (bool)glove.Invoke(app, new object[] { point, true, 4 });
                bool heldQuiet = held == 4 && pauseUp
                    && GloveExit() == "swallowed-up"
                    && EditorTaps(app) == 0
                    && (int)t.GetField("_sel", flags).GetValue(app) == -1;
                Check("resume-held-through-pause-does-not-hop", heldQuiet,
                    "id " + held + " up " + pauseUp + " " + GloveExit()
                    + " taps " + EditorTaps(app) + pauseEx);

                bool armedTap = Arm();
                t.GetField("_gloveEdgeId", flags).SetValue(app, 4);
                t.GetField("_sel", flags).SetValue(app, 0);
                SetEditorTaps(app, 0);
                Method(t, "HandleTap", typeof(Vector2)).Invoke(app, new object[] { Vector2.zero });
                bool tapAte = armedTap && EditorTaps(app) == 0
                    && (int)t.GetField("_sel", flags).GetValue(app) == 0
                    && Swallowed() == int.MinValue
                    && GloveExit() == "swallowed-up";
                Check("resume-handletap-gates-swallowed-id", tapAte,
                    "armed " + armedTap + " taps " + EditorTaps(app)
                    + " sel " + t.GetField("_sel", flags).GetValue(app)
                    + " id " + Swallowed() + " " + GloveExit());

                bool armedCancel = Arm();
                Method(t, "NoteSwallowedCancel", typeof(int)).Invoke(app, new object[] { 9 });
                int otherCancel = Swallowed();
                Method(t, "NoteSwallowedCancel", typeof(int)).Invoke(app, new object[] { 4 });
                SetEditorTaps(app, 0);
                t.GetField("_sel", flags).SetValue(app, -1);
                bool mouseUp = (bool)glove.Invoke(app, new object[] { point, true, -1 });
                bool cancelQuiet = armedCancel && otherCancel == 4 && Swallowed() == int.MinValue
                    && mouseUp && GloveExit() == "cancel-frame"
                    && EditorTaps(app) == 0
                    && (int)t.GetField("_sel", flags).GetValue(app) == -1;
                t.GetField("_board", flags).SetValue(app, null);
                t.GetField("_gloveEdgeId", flags).SetValue(app, 5);
                SetEditorTaps(app, 0);
                Method(t, "HandleTap", typeof(Vector2)).Invoke(app, new object[] { Vector2.zero });
                bool otherOpen = GloveExit() == "board" && EditorTaps(app) == 1;
                t.GetField("_gloveCancelFrame", flags).SetValue(app, -1);
                Check("resume-cancel-leaves-other-finger", otherOpen,
                    "exit " + GloveExit() + " taps " + EditorTaps(app));
                Check("resume-cancel-and-mouse-up-does-not-hop", cancelQuiet,
                    "armed " + armedCancel + " other " + otherCancel + " up " + mouseUp
                    + " " + GloveExit()
                    + " taps " + EditorTaps(app));
            }
            finally
            {
                SetResumeUntil(keep);
                GamePause.Reset();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void SeatOpenLesson(object app)
        {
            var t = typeof(FlockFiveApp);
            t.GetField("_lessonGuardUntil", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, float.NegativeInfinity);
            t.GetField("_coachAteFrame", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(app, -1);
        }

        static string TapFault(System.Exception ex)
        {
            var inner = ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null
                ? tie.InnerException : ex;
            return " " + inner.GetType().Name + ": " + inner.Message + " | " + inner.StackTrace;
        }

        static BranchView Limb(GameObject host, int index, Vector3 at)
        {
            host.transform.position = at;
            var view = host.AddComponent<BranchView>();
            view.Index = index;
            for (int s = 0; s < BranchState.Cap; s++)
            {
                var seat = new GameObject("seat");
                seat.transform.SetParent(host.transform, false);
                view.Seats[s] = seat.transform;
                var bird = new GameObject("bird");
                bird.transform.SetParent(host.transform, false);
                view.Birds[s] = bird.AddComponent<SpriteRenderer>();
            }
            return view;
        }

        static void CheckGloveRoute(System.Action<string, bool, string> Check)
        {
            var home = new Rect(0f, 0f, 80f, 80f);
            var glove = new Rect(0f, 0f, 120f, 120f);
            var cuff = new Vector2(home.center.x, home.center.y);
            bool cuffInHome = home.Contains(cuff);
            bool cuffInGlove = glove.Contains(cuff);
            bool hudPoint = FlockFiveApp.HudBeatsGlove(cuff, home, new Rect(400f, 0f, 40f, 40f), new Rect(200f, 200f, 20f, 20f), false);
            bool gloveSkipsHud = !FlockFiveApp.GloveMayAct(true, true, false);
            bool hudWins = cuffInHome && cuffInGlove && hudPoint && gloveSkipsHud;
            bool downSkips = !FlockFiveApp.GloveMayAct(false, false, false);
            bool busyRefuses = !FlockFiveApp.GloveMayAct(true, false, true);
            bool oneEdge = FlockFiveApp.GloveMayAct(true, false, false);
            var go = new GameObject("glove-route");
            var limb = new GameObject("glove-limb");
            bool pickedOnly = false, stayed = false;
            string routeTrace = "";
            int sawDown = -2, sawUp = -2;
            bool downHit = false, upHit = false;
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var t = typeof(FlockFiveApp);
                var board = LevelData.Open(0);
                bool level1 = FlockFiveApp.Level1First(board, i => false, i => false, out int from, out _);
                if (!level1) from = 0;
                var view = Limb(limb, from, Vector3.zero);
                var garden = new WorldBuilder.Garden();
                var views = new BranchView[board.Branches.Count];
                views[from] = view;
                garden.Branches = views;
                garden.Cam = WorldBuilder.MakeCamera(go.transform);
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_splash", flags).SetValue(app, false);
                t.GetField("_gloveVis", flags).SetValue(app, true);
                t.GetField("_cueForce", flags).SetValue(app, true);
                t.GetField("_cueBranch", flags).SetValue(app, from);
                t.GetField("_sel", flags).SetValue(app, -1);
                SeatOpenLesson(app);
                float gx = PhoneArea.W > 0f ? PhoneArea.W * 0.5f : 360f;
                float gy = PhoneArea.H > 0f ? PhoneArea.H * 0.45f : 520f;
                var body = t.GetField("_gloveBody", flags);
                body.SetValue(app, new Rect(gx - 40f, gy - 45f, 80f, 90f));
                t.GetField("_gloveBodyFrame", flags).SetValue(app, Time.frameCount);
                var point = new Vector2(gx + PhoneArea.OffX, gy);
                var gloveTap = Method(t, "TryDrawnGlove", typeof(Vector2), typeof(bool), typeof(int));
                downHit = (bool)gloveTap.Invoke(app, new object[] { point, false, -1 });
                sawDown = (int)t.GetField("_sel", flags).GetValue(app);
                upHit = (bool)gloveTap.Invoke(app, new object[] { point, true, -1 });
                sawUp = (int)t.GetField("_sel", flags).GetValue(app);
                routeTrace = FlockFiveApp.EditorGloveTraceNow.ToString();
                int afterDown = sawDown;
                int afterUp = sawUp;
                pickedOnly = level1 && downHit && upHit && afterDown < 0 && afterUp == from;
                var turn = (OstrichTurnGate)t.GetField("_turnGate", flags).GetValue(app);
                turn.TryBeginMove();
                t.GetField("_sel", flags).SetValue(app, -1);
                t.GetField("_gloveTookFrame", flags).SetValue(app, -1);
                gloveTap.Invoke(app, new object[] { point, true, -1 });
                stayed = (int)t.GetField("_sel", flags).GetValue(app) < 0;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(limb);
                UnityEngine.Object.DestroyImmediate(go);
            }
            Check("glove-one-tap-does-not-pick-and-move", downSkips && oneEdge && pickedOnly,
                "down " + downSkips + " edge " + oneEdge + " pick " + pickedOnly
                + " hit " + downHit + "/" + upHit + " sel " + sawDown + "->" + sawUp + " " + routeTrace);
            Check("glove-hud-beats-cuff", hudWins,
                "home " + cuffInHome + " glove " + cuffInGlove + " hud " + hudPoint + " skip " + gloveSkipsHud
                + " cuff " + cuff.x.ToString("0") + "," + cuff.y.ToString("0"));
            Check("glove-turn-gate-refuses", busyRefuses && stayed, "stayed " + stayed);
        }

        static void CheckForcePause(System.Action<string, bool, string> Check)
        {
            bool forever = DevLevelSkip.ForcePauseBlocks("http://x/?forcePause=1", 30f);
            bool waiting = DevLevelSkip.ForcePauseBlocks("http://x/?forcePause=4", 3f);
            bool elapsed = !DevLevelSkip.ForcePauseBlocks("http://x/?forcePause=4", 4f);
            DevLevelSkip.DevPauseHeld = false;
            DevLevelSkip.DevPauseFlip();
            bool held = DevLevelSkip.CaptionHeld("http://x/", 0f);
            DevLevelSkip.DevPauseFlip();
            bool resumed = !DevLevelSkip.DevPauseHeld && !DevLevelSkip.CaptionHeld("http://x/", 0f);
            Check("force-pause-timeout-and-toggle", forever && waiting && elapsed && held && resumed,
                "forever " + forever + " waiting " + waiting + " elapsed " + elapsed + " held " + held + " resumed " + resumed);
        }

        static void CheckRestartRecue(System.Action<string, bool, string> Check)
        {
            const string doneKey = "flockfive.coach.done";
            bool hadDone = PlayerPrefs.HasKey(doneKey);
            int keepDone = PlayerPrefs.GetInt(doneKey, 0);
            bool hadKit = PlayerPrefs.HasKey(RuleTips.KitKey);
            int keepKit = PlayerPrefs.GetInt(RuleTips.KitKey, 0);
            bool hadFeeder = PlayerPrefs.HasKey(RuleTips.FeederKey);
            int keepFeeder = PlayerPrefs.GetInt(RuleTips.FeederKey, 0);
            int keepStreak = Purse.Streak;
            var go = new GameObject("restart-recue");
            var limb = new GameObject("recue-limb");
            try
            {
                PlayerPrefs.DeleteKey(doneKey);
                PlayerPrefs.DeleteKey(RuleTips.KitKey);
                PlayerPrefs.DeleteKey(RuleTips.FeederKey);
                Purse.SetStreak(0);
                GamePause.Reset();
                LevelData.Open(0);
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var t = typeof(FlockFiveApp);
                var board = LevelData.Open(0);
                var seed = board.Clone();
                var view = limb.AddComponent<BranchView>();
                for (int s = 0; s < BranchState.Cap; s++)
                {
                    var seat = new GameObject("seat");
                    seat.transform.SetParent(limb.transform, false);
                    view.Seats[s] = seat.transform;
                }
                var birdGo = new GameObject("bird");
                birdGo.transform.SetParent(limb.transform, false);
                view.Birds[0] = birdGo.AddComponent<SpriteRenderer>();
                var garden = new WorldBuilder.Garden();
                garden.Branches = new BranchView[] { view };
                garden.Cam = go.AddComponent<Camera>();
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_seed", flags).SetValue(app, seed);
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_splash", flags).SetValue(app, false);
                bool nice = RecueFrom(app, t, flags, RuleTip.Feeder);
                bool crowns = RecueFrom(app, t, flags, RuleTip.Kit);
                Check("restart-nice-and-crowns-recues-level1", nice && crowns, "nice " + nice + " crowns " + crowns);
            }
            finally
            {
                GamePause.Reset();
                Purse.SetStreak(keepStreak);
                if (hadDone) PlayerPrefs.SetInt(doneKey, keepDone);
                else PlayerPrefs.DeleteKey(doneKey);
                if (hadKit) PlayerPrefs.SetInt(RuleTips.KitKey, keepKit);
                else PlayerPrefs.DeleteKey(RuleTips.KitKey);
                if (hadFeeder) PlayerPrefs.SetInt(RuleTips.FeederKey, keepFeeder);
                else PlayerPrefs.DeleteKey(RuleTips.FeederKey);
                UnityEngine.Object.DestroyImmediate(limb);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static bool RecueFrom(FlockFiveApp app, System.Type t, BindingFlags flags, RuleTip tip)
        {
            GamePause.Reset();
            t.GetField("_splash", flags).SetValue(app, false);
            t.GetField("_restarting", flags).SetValue(app, false);
            t.GetField("_coach", flags).SetValue(app, true);
            t.GetMethod("ShowRuleTip", flags).Invoke(app, new object[] { tip });
            object[] hud = { default(Rect), default(Rect) };
            t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hud);
            var restart = (Rect)hud[0];
            var tap = new Vector2(restart.center.x, PhoneArea.H - restart.center.y);
            t.GetMethod("TakeRuleTipPress", flags).Invoke(app, new object[] { tap });
            var snap = (System.Collections.IEnumerator)t.GetField("EditorSnapRound", flags).GetValue(app);
            bool landed = false;
            if (snap != null) PumpFrames(snap, 64, null, out landed);
            var line = (string)t.GetField("_cueLine", flags).GetValue(app);
            bool glove = (bool)t.GetField("_gloveVis", flags).GetValue(app);
            var now = t.GetField("_ruleTip", flags).GetValue(app);
            bool clear = now != null && now.ToString() == "None";
            return landed && glove && clear && line == "Tap a branch to pick up its top birds."
                && line != RuleTips.KitLine && line != RuleTips.FeederLine;
        }

        static void CheckCoachRestartPath(System.Action<string, bool, string> Check)
        {
            const string doneKey = "flockfive.coach.done";
            const string giftKey = "flockfive.coach.gift";
            bool hadDone = PlayerPrefs.HasKey(doneKey);
            bool hadGift = PlayerPrefs.HasKey(giftKey);
            int keepDone = PlayerPrefs.GetInt(doneKey, 0);
            int keepGift = PlayerPrefs.GetInt(giftKey, 0);
            int keepStreak = Purse.Streak;
            var go = new GameObject("coach-restart-path");
            try
            {
                PlayerPrefs.DeleteKey(doneKey);
                PlayerPrefs.DeleteKey(giftKey);
                Purse.SetStreak(0);
                GamePause.Reset();
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var t = typeof(FlockFiveApp);
                var board = LevelData.Open(0);
                t.GetField("_board", flags).SetValue(app, board);
                t.GetField("_seed", flags).SetValue(app, board.Clone());
                var garden = new WorldBuilder.Garden();
                garden.Branches = new BranchView[0];
                t.GetField("_garden", flags).SetValue(app, garden);
                t.GetField("_coach", flags).SetValue(app, true);
                t.GetField("_coachMoves", flags).SetValue(app, 0);
                t.GetField("_splash", flags).SetValue(app, false);
                var pick = t.GetMethod("CoachPick", flags);
                var moved = t.GetMethod("CoachMoved", flags);
                object[] cue = { -1, -1 };
                bool first = (bool)pick.Invoke(app, cue);
                int from = (int)cue[0], to = (int)cue[1];
                bool level1 = FlockFiveApp.Level1First(board, i => false, i => false, out int wantFrom, out int wantTo);
                t.GetField("_coachFrom", flags).SetValue(app, from);
                t.GetField("_coachTo", flags).SetValue(app, to);
                moved.Invoke(app, new object[] { from, to, 0 });
                int afterHop = (int)t.GetField("_coachMoves", flags).GetValue(app);
                object[] hudOut = { default(Rect), default(Rect) };
                t.GetMethod("HudShaken", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, hudOut);
                var restart = (Rect)hudOut[0];
                float screenH = PhoneArea.H;
                var tap = new Vector2(restart.center.x, screenH - restart.center.y);
                bool acted = (bool)t.GetMethod("GardenHudAct", flags).Invoke(app, new object[] { tap });
                var snap = (System.Collections.IEnumerator)t.GetField("EditorSnapRound", flags).GetValue(app);
                int frames = 0;
                bool landed = false;
                int movesNow = (int)t.GetField("_coachMoves", flags).GetValue(app);
                if (acted && snap != null)
                {
                    frames = PumpFrames(snap, 64, null, out landed);
                    landed = landed && !(bool)t.GetField("_restarting", flags).GetValue(app);
                }
                int reset = (int)t.GetField("_coachMoves", flags).GetValue(app);
                object[] again = { -1, -1 };
                bool recued = (bool)pick.Invoke(app, again);
                int from2 = (int)again[0], to2 = (int)again[1];
                t.GetField("_coachFrom", flags).SetValue(app, from2);
                t.GetField("_coachTo", flags).SetValue(app, to2);
                moved.Invoke(app, new object[] { from2, to2, 0 });
                int midMoves = (int)t.GetField("_coachMoves", flags).GetValue(app);
                bool mid = (bool)t.GetField("_coach", flags).GetValue(app);
                moved.Invoke(app, new object[] { from2, to2, 0 });
                bool finished = !(bool)t.GetField("_coach", flags).GetValue(app);
                Check("restart-button-runs-snapround",
                    level1 && first && from == wantFrom && to == wantTo && afterHop == 1
                    && acted && restart.width > 2f
                    && reset == 0 && recued && from2 == wantFrom && to2 == wantTo
                    && mid && midMoves == 1 && finished
                    && landed && (frames > 0 || movesNow == 0),
                    "acted " + acted + " restart " + restart + " frames " + frames
                    + " moves " + afterHop + "/" + reset + "/" + midMoves);
            }
            finally
            {
                GamePause.Reset();
                Purse.SetStreak(keepStreak);
                if (hadDone) PlayerPrefs.SetInt(doneKey, keepDone);
                else PlayerPrefs.DeleteKey(doneKey);
                if (hadGift) PlayerPrefs.SetInt(giftKey, keepGift);
                else PlayerPrefs.DeleteKey(giftKey);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static int PumpFrames(System.Collections.IEnumerator root, int maxFrames, System.Action afterFirstFrame, out bool done)
        {
            return PumpFrames(root, maxFrames, afterFirstFrame, out done, out _);
        }

        // Nested enumerators are stepped. WaitForSeconds and any other
        // non-enumerator yield counts as one elapsed frame: the instruction
        // itself does not hold the clock, the runner does, and there is no
        // player loop here. A null yield is one frame.
        static int PumpFrames(System.Collections.IEnumerator root, int maxFrames, System.Action afterFirstFrame, out bool done, out string stalledAt)
        {
            int frames = 0;
            bool noted = false;
            object yielded = null;
            var stack = new System.Collections.Generic.Stack<System.Collections.IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0 && frames < maxFrames)
            {
                var top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                var child = top.Current as System.Collections.IEnumerator;
                if (child != null) stack.Push(child);
                else
                {
                    frames++;
                    yielded = top.Current;
                    if (!noted)
                    {
                        noted = true;
                        if (afterFirstFrame != null) afterFirstFrame();
                    }
                }
            }
            done = stack.Count == 0;
            stalledAt = done ? null : DescribeStall(yielded, stack);
            return frames;
        }

        static string DescribeStall(object yielded, System.Collections.Generic.Stack<System.Collections.IEnumerator> stack)
        {
            string current;
            if (yielded == null) current = "null";
            else
            {
                var yt = yielded.GetType();
                current = yt.FullName ?? yt.Name;
                var secs = yt.GetField("m_Seconds", BindingFlags.NonPublic | BindingFlags.Instance);
                if (secs != null) current += " seconds=" + secs.GetValue(yielded);
                else current += " value=" + yielded;
            }
            var at = new System.Text.StringBuilder();
            foreach (var e in stack)
            {
                if (at.Length > 0) at.Append(" < ");
                at.Append(e.GetType().Name);
            }
            return "current=" + current + " at=" + at;
        }

        static void CheckRebootHolds(System.Action<string, bool, string> Check)
        {
            AudioGuard.ResetForTests();
            var go = new UnityEngine.GameObject("reboot-bed");
            var strayGo = new UnityEngine.GameObject("stray-loop");
            MixDesk desk = null;
            try
            {
                desk = go.AddComponent<MixDesk>();
                desk.AttachForTests();
                desk.PlantStemForTests(0.24f);
                var bed = desk.StemForTests(0);
                bed.clip = ProbeClip("reboot-bed", 88200);
                bed.loop = true;
                AudioGuard.Play(bed);
                AudioGuard.ScheduleStemsTogether(new AudioSource[] { bed });
                bed.timeSamples = 20000;
                var stray = strayGo.AddComponent<AudioSource>();
                stray.playOnAwake = false;
                stray.loop = true;
                stray.clip = ProbeClip("stray-loop");
                stray.volume = 0.5f;
                AudioGuard.Play(stray);
                int plays = AudioGuard.PlayCalls;
                int stops = AudioGuard.StopCalls;
                MixDesk.Boot(go);
                bool stillScheduled = AudioGuard.ScheduledAt(bed, out _);
                bool bedHeld = stillScheduled && bed.timeSamples == 20000
                    && AudioGuard.PlayCalls == plays && AudioGuard.StopCalls == stops + 1;
                bool strayDown = !stray.isPlaying && stray.volume == 0f;
                Check("second-boot-keeps-the-bed", bedHeld && strayDown,
                    "scheduled " + stillScheduled + " samples " + bed.timeSamples
                    + " plays " + AudioGuard.PlayCalls + " stops " + AudioGuard.StopCalls
                    + " stray " + stray.volume);
            }
            finally
            {
                AudioGuard.ResetForTests();
                if (desk != null) MixDesk.DetachForTests(desk);
                UnityEngine.Object.DestroyImmediate(strayGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
#endif

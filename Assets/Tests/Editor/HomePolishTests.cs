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
                var down = new Event();
                down.type = EventType.MouseDown;
                bool ateDown = (bool)consume.Invoke(null, new object[] { down });
                bool pressCleared = !(bool)held.GetValue(null) && !ateDown;
                var up = new Event();
                up.type = EventType.MouseUp;
                bool ateRelease = (bool)consume.Invoke(null, new object[] { up });
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
                    "press " + pressCleared + " release " + ateRelease + " leaving " + leavingDown + " other " + otherFinger);
                Check("home-exit-matches-finger-id",
                    mouse == -1 && finger == 4 && other == 9 && mouseVsFinger == 9
                    && mouseSwallow && fingerSwallow && !otherSwallow && !mouseEatsFinger,
                    "mouse " + mouse + " finger " + finger + " other " + other + " echo " + mouseVsFinger);
            }
            finally
            {
                GamePause.Reset();
                UnityEngine.Object.DestroyImmediate(go);
            }
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
            int frames = 0;
            bool noted = false;
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
                    if (!noted)
                    {
                        noted = true;
                        if (afterFirstFrame != null) afterFirstFrame();
                    }
                }
            }
            done = stack.Count == 0;
            return frames;
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

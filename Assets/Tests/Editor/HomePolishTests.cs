#if UNITY_EDITOR
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

            CheckSafe("402", 402, 874, Check);
            CheckSafe("412", 412, 915, Check);
            CheckSafe("820", 820, 1180, Check);
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
            CheckPlayScheduled(Check);
            CheckAudioRetry(Check);
            CheckPauseAndUnmute(Check);
            CheckUnmuteScope(Check);

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
            bool headerOk = header.yMin >= inset - 0.5f;
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
            bool otherCode = !AudioGuard.IsFmodFailure("FMOD error code 360");
            bool quiet = !AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 0;
            bool named = AudioGuard.IsFmodFailure("FMOD_ERR_OUTPUT_INIT")
                && AudioGuard.NoteFmodFailure("FMOD_ERR_OUTPUT_INIT");
            bool latched = AudioGuard.OutputFailed && AudioGuard.FmodWarnings == 1;
            bool again = AudioGuard.NoteFmodFailure("FMOD failed to initialize the output device: error code 36");
            bool once = latched && again && AudioGuard.FmodWarnings == 1;
            AudioGuard.OnAudioConfigurationChanged(true);
            bool config = !AudioGuard.OutputFailed;
            bool rewarn = AudioGuard.NoteFmodFailure("error code 36") && AudioGuard.FmodWarnings == 2 && AudioGuard.OutputFailed;
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
                load && decode && otherCode && quiet && named && once && plain,
                "warnings " + AudioGuard.FmodWarnings + " failed " + AudioGuard.OutputFailed);
            Check("fmod-output-recovers",
                config && rewarn && focus && held && resume && !AudioGuard.OutputFailed,
                "config " + config + " focus " + focus + " held " + held + " resume " + resume);
            AudioGuard.ResetForTests();
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
            var clip = AudioClip.Create(name, 64, 1, 22050, false);
            clip.SetData(new float[64], 0);
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

                AudioGuard.NoteFmodFailure("error code 36");
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
                AudioGuard.NoteFmodFailure("error code 36");
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

                AudioGuard.NoteFmodFailure("error code 36");
                desk.PlantStemForTests(0.24f);
                bed = desk.StemForTests(0);
                bed.clip = ProbeClip("garden-bed");
                AudioGuard.Play(bed);
                int beforeMute = AudioGuard.RetryPlayCount;
                int skipped = AudioGuard.SkippedCount;
                AudioGuard.SetMuted(true, false);
                AudioGuard.OnAudioConfigurationChanged(true);
                bool kept = AudioGuard.RetryDeferred && AudioGuard.SkippedCount >= skipped && AudioGuard.RetryPlayCount == beforeMute;
                AudioGuard.SetMuted(false, false);
                bool brought = bed.isPlaying || desk.ReassertBed() >= 1;
                if (bed.isPlaying)
                    brought = brought && desk.ReassertBed() == 0;
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
                desk.PlantBadgerVoicesForTests(out var loop, out var cue, out var sting);
                loop.clip = ProbeClip("badger-loop");
                cue.clip = ProbeClip("badger-cue");
                sting.clip = ProbeClip("badger-sting");
                cue.loop = false;
                sting.loop = false;
                AudioGuard.Play(loop);
                AudioGuard.Play(sting);
                AudioGuard.SetMuted(true, false);
                desk.ArmCutForTests();
                AudioGuard.SetMuted(false, false);
                bool cut = desk.LastReassert == 0 && !AudioGuard.NeedsRestore(loop) && !AudioGuard.NeedsRestore(sting);

                AudioGuard.ResetForTests();
                AudioGuard.Play(loop);
                AudioGuard.Play(sting);
                desk.ArmResultForTests();
                desk.BadgerResult(true);
                AudioGuard.SetMuted(true, false);
                AudioGuard.SetMuted(false, false);
                bool result = desk.LastReassert == 0 && !AudioGuard.NeedsRestore(loop) && !AudioGuard.NeedsRestore(sting);

                AudioGuard.ResetForTests();
                desk.PlantSplashStemForTests(false);
                var splash = desk.StemForTests(4);
                splash.clip = ProbeClip("splash-early");
                AudioGuard.Play(splash);
                AudioGuard.SetMuted(true, false);
                AudioGuard.SetMuted(false, false);
                bool early = desk.LastReassert == 0 && !AudioGuard.NeedsRestore(splash);

                AudioGuard.ResetForTests();
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
                AudioGuard.SetMuted(false, false);
                bool only = desk.LastReassert == 1 && AudioGuard.WasStarted(kept) && !AudioGuard.WasStarted(other) && !AudioGuard.NeedsRestore(oneShot);

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
    }
}
#endif

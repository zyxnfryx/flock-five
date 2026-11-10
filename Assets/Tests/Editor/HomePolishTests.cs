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
            bool known = AudioGuard.IsFmodFailure("FMOD failed to initialize the output device: error code 36");
            bool plain = !AudioGuard.IsFmodFailure("NullReferenceException: Object reference not set");
            bool once = AudioGuard.NoteFmodFailure("FMOD error code 36");
            int n = AudioGuard.FmodWarnings;
            bool again = AudioGuard.NoteFmodFailure("FMOD error code 36");
            bool stayed = AudioGuard.FmodWarnings == 1 && AudioGuard.OutputFailed;
            bool ignored = !AudioGuard.NoteFmodFailure("InvalidOperationException: already created");
            Check("fmod-output-init-warns-once",
                known && plain && once && again && stayed && ignored && n == 1,
                "warnings " + AudioGuard.FmodWarnings);
            AudioGuard.ResetForTests();
        }
    }
}
#endif

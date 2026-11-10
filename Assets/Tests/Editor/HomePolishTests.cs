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
            bool phone = screenW / (float)screenH <= DeviceFit.PhoneAspect + 0.04f;
            float inset = phone ? 59f : 0f;
            float bottom = phone ? 34f : 0f;
            var safe = new Rect(0f, bottom, screenW, screenH - inset - bottom);
            float s = DeviceFit.Ui(screenW, screenH);
            var chrome = FlockFiveApp.LayoutPokerChrome(screenW, screenH, safe, s);
            var gear = FlockFiveApp.SettingsGearRect(screenW, screenH, safe);
            bool back = chrome.Back.yMin >= inset - 0.5f;
            bool cog = gear.yMin >= inset - 0.5f;
            bool header = chrome.Back.center.y >= inset - 0.5f;
            Check(tag + "-top-controls-clear-safe-inset",
                back && cog && header,
                "inset " + inset + " back " + chrome.Back.yMin + " gear " + gear.yMin);
        }
    }
}
#endif

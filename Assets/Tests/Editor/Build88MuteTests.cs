#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Mute stays above every popup. One helper seats it on home, garden, and poker.
    public static class Build88MuteTests
    {
        const string Out = "/tmp/flock-five-b88-mute-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build88mute] " + line);
            }

            float? keep = DeviceFit.TestWindow;
            try
            {
                Check("sort-above-popup",
                    MuteChrome.AbovePopup == ModalBlock.PopupSort + 1
                    && MuteChrome.Sort(MuteChrome.Face.Home) == MuteChrome.AbovePopup
                    && MuteChrome.Sort(MuteChrome.Face.Garden) == MuteChrome.AbovePopup
                    && MuteChrome.Sort(MuteChrome.Face.Poker) == MuteChrome.AbovePopup
                    && MuteChrome.Sort(MuteChrome.Face.Off) == 0
                    && MuteChrome.AboveOverlay(MuteChrome.Face.Home)
                    && MuteChrome.AboveOverlay(MuteChrome.Face.Garden)
                    && MuteChrome.AboveOverlay(MuteChrome.Face.Poker)
                    && !MuteChrome.AboveOverlay(MuteChrome.Face.Off),
                    "popup " + ModalBlock.PopupSort + " mute " + MuteChrome.AbovePopup);

                var phone = new Rect(0f, 102f, 1179f, 2277f);
                var pad = new Rect(0f, 20f, 1536f, 2008f);
                Face(Check, "iphone", 1179f, 2556f, phone);
                Face(Check, "ipad", 1536f, 2048f, pad);

                var nudgeSafe = phone;
                DeviceFit.TestWindow = 1179f / 2556f;
                var want = MuteChrome.Home(1179f, 2556f, nudgeSafe);
                var block = new Rect(want.x - 20f, want.y - 20f, want.width + 80f, want.height + 80f);
                var moved = MuteChrome.Seat(1179f, 2556f, nudgeSafe, want, block,
                    new Rect(8f, 2400f, 40f, 40f), new Rect(1100f, 2400f, 40f, 40f));
                float gap = MuteChrome.Gap(2556f);
                Check("garden-seat-nudges-off-corner",
                    moved.width > 8f
                    && (moved.y > want.y + 1f || moved.x < want.x - 1f)
                    && MuteChrome.Clear(moved, block, gap),
                    "want " + want + " moved " + moved);

                var empty = MuteChrome.Home(1179f, 2556f, new Rect(0f, 0f, 0f, 0f));
                Check("home-empty-safe-stays-on-screen",
                    empty.width > 40f && empty.height > 40f
                    && empty.xMin >= -0.5f && empty.yMin >= -0.5f
                    && empty.xMax <= 1179.5f && empty.yMax <= 2556.5f,
                    empty.ToString());

                string root = Application.dataPath;
                string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
                string settings = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Settings.cs"));
                string helper = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/MuteChrome.cs"));
                int onGui = app.IndexOf("void OnGUI()");
                int onGuiEnd = onGui >= 0 ? app.IndexOf("long _guiEndTick", onGui) : -1;
                string onGuiBody = onGui >= 0 && onGuiEnd > onGui ? app.Substring(onGui, onGuiEnd - onGui) : "";
                int pollAt = onGuiBody.IndexOf("PollMuteChrome()");
                int bodyAt = onGuiBody.IndexOf("OnGUIBody()");
                int finishFn = app.IndexOf("void FinishMuteChrome()");
                int finishEnd = finishFn >= 0 ? app.IndexOf("void OnGUI()", finishFn) : -1;
                string finish = finishFn >= 0 && finishEnd > finishFn ? app.Substring(finishFn, finishEnd - finishFn) : "";
                int reward = finish.IndexOf("DrawRewardChoiceTop()");
                int paint = finish.IndexOf("PaintMuteChrome()");
                int cover = finish.IndexOf("DrawWarmCover()");
                int pollFn = app.IndexOf("void PollMuteChrome()");
                int pollEnd = pollFn >= 0 ? app.IndexOf("void PaintMuteChrome()", pollFn) : -1;
                string poll = pollFn >= 0 && pollEnd > pollFn ? app.Substring(pollFn, pollEnd - pollFn) : "";
                int sheet = settings.IndexOf("void DrawSettingsSheet");
                int sheetEnd = sheet >= 0 ? settings.IndexOf("void DrawSettingsDim", sheet) : -1;
                string sheetBody = sheet >= 0 && sheetEnd > sheet ? settings.Substring(sheet, sheetEnd - sheet) : "";
                int hud = app.IndexOf("bool HitHud(");
                int hudEnd = hud >= 0 ? app.IndexOf("MuteChrome.Face MuteFace()", hud) : -1;
                string hudBody = hud >= 0 && hudEnd > hud ? app.Substring(hud, hudEnd - hud) : "";

                Check("paint-after-popups",
                    reward >= 0 && paint > reward && cover > paint
                    && pollAt >= 0 && bodyAt > pollAt
                    && onGuiBody.IndexOf("FinishMuteChrome()") > bodyAt,
                    "reward " + reward + " paint " + paint + " cover " + cover
                    + " poll " + pollAt + " body " + bodyAt);
                Check("settings-does-not-own-mute",
                    sheetBody.IndexOf("Mute") < 0
                    && sheetBody.IndexOf("Speaker") < 0
                    && sheetBody.IndexOf("MuteChrome") < 0
                    && app.IndexOf("PaintHomeMute") < 0
                    && app.IndexOf("PollHomeMute") < 0
                    && settings.IndexOf("PaintHomeMute") < 0
                    && settings.IndexOf("PollHomeMute") < 0
                    && poll.IndexOf("_settingsOpen") < 0
                    && poll.IndexOf("VeilBlocks") < 0,
                    "sheet " + sheet + " poll " + pollFn);
                Check("one-helper-three-faces",
                    helper.IndexOf("Home(") >= 0
                    && helper.IndexOf("Garden(") >= 0
                    && helper.IndexOf("Poker(") >= 0
                    && app.IndexOf("MuteChrome.Home(") >= 0
                    && app.IndexOf("MuteChrome.Garden(") >= 0
                    && app.IndexOf("MuteChrome.Poker(") >= 0
                    && app.IndexOf("LayoutPokerChrome(") >= 0
                    && hudBody.IndexOf("MuteRect(MuteFace())") >= 0
                    && app.IndexOf("const float SplashLogoScale = 0.91f;") >= 0,
                    "helper seats home, garden, poker");
                Check("gear-still-redraws-for-settings-only",
                    settings.IndexOf("if (!HomeGear.Shown(q) || !HomeGear.AboveDimmer(q) || _settingsOpen) return;") >= 0
                    && sheetBody.IndexOf("DrawSettingsGear(") >= 0,
                    "gear path unchanged");
            }
            catch (System.Exception ex)
            {
                Check("mute", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                DeviceFit.TestWindow = keep;
            }

            string end = fail == 0 ? "BUILD88MUTE_OK " + pass : "BUILD88MUTE_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88mute] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void Face(System.Action<string, bool, string> check, string tag, float w, float h, Rect safe)
        {
            DeviceFit.TestWindow = w / h;
            float gap = MuteChrome.Gap(h);
            var gear = FlockFiveApp.SettingsGearRect(w, h, safe);
            var home = MuteChrome.Home(w, h, safe);
            float s = DeviceFit.Ui(w, h);
            float fit = s * 0.91f;
            float titleTop = FlockFiveApp.TopHudBox(h, safe, 0f);
            float cap = 56f * fit;
            float blockH = cap * 2f + 4f * fit;
            float logoW = w * 0.72f * 0.91f;
            var word = new Rect((w - logoW) * 0.5f, titleTop, logoW, blockH);
            bool homeOk = home.width > 40f
                && Mathf.Abs(home.y - gear.y) < 0.05f
                && Mathf.Abs(home.width - gear.width) < 0.05f
                && Mathf.Abs((safe.xMax - home.xMax) - (gear.xMin - safe.xMin)) < 0.05f
                && home.center.x > w * 0.7f
                && home.yMax < h * 0.35f
                && MuteChrome.Clear(home, gear, gap)
                && MuteChrome.Clear(home, word, 4f)
                && home.xMin >= word.xMax + 8f;
            check(tag + "-home-top-right-clears-gear-and-title", homeOk,
                "mute " + home + " gear " + gear + " word " + word);

            var garden = MuteChrome.Garden(w, h, safe);
            bool gardenOk = garden.Mute.width > 40f
                && garden.Mute.center.x > w * 0.62f
                && garden.Mute.yMax < h * 0.4f
                && garden.Mute.yMax < garden.Restart.y - gap
                && garden.Mute.yMax < garden.Hive.y - gap
                && MuteChrome.Clear(garden.Mute, garden.Stamp, gap)
                && MuteChrome.Clear(garden.Mute, garden.Restart, gap)
                && MuteChrome.Clear(garden.Mute, garden.Hive, gap);
            check(tag + "-garden-clears-stamp-restart-hive", gardenOk,
                "mute " + garden.Mute + " stamp " + garden.Stamp
                + " restart " + garden.Restart + " hive " + garden.Hive);

            var poker = MuteChrome.Poker(w, h, safe);
            float drawn = FlockFiveApp.SettingsGearDrawnSize(h);
            float under = poker.Mute.y - poker.Coins.yMax;
            float tuck = Mathf.Max(4f, gap * MuteChrome.PokerTuck);
            bool pokerOk = poker.Mute.width > 16f
                && poker.Mute.width <= drawn * MuteChrome.PokerScale + 0.6f
                && poker.Mute.width < drawn * 0.8f
                && Mathf.Abs(poker.Mute.center.x - poker.Coins.center.x) < 2f
                && poker.Mute.xMax <= poker.Coins.xMax + 1f
                && under >= tuck - 0.6f
                && under <= tuck + 1.5f
                && poker.Mute.yMax <= poker.PayTab.y - gap + 0.6f
                && MuteChrome.Clear(poker.Mute, poker.Coins, 1f)
                && MuteChrome.Clear(poker.Mute, poker.Title, gap)
                && MuteChrome.Clear(poker.Mute, poker.Halo, gap)
                && MuteChrome.Clear(poker.Mute, poker.Back, gap)
                && MuteChrome.Clear(poker.Mute, poker.PayTab, gap);
            check(tag + "-poker-under-coins-above-pay-tab", pokerOk,
                "mute " + poker.Mute + " coins " + poker.Coins
                + " title " + poker.Title + " halo " + poker.Halo
                + " back " + poker.Back + " tab " + poker.PayTab
                + " under " + under.ToString("0.0") + " gap " + gap.ToString("0.0"));
        }
    }
}
#endif

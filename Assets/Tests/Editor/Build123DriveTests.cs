#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b123: streak reminder only after a claim, phone corner seats, no garden
    // multiplier chip on the play HUD, piggy poke pays nothing and shows the coin note.
    // Batch: -executeMethod FlockFive.Editor.Build123DriveTests.Run
    public static class Build123DriveTests
    {
        const string Out = "/tmp/flock-five-b123-tests.txt";

        [MenuItem("Flock Five/Build 123 Drive Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build123] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            Reminder(Check);
            Corners(Check);
            Hud(Check);
            Pig(Check);
            HiveGround(Check);
            AdNotReady(Check);

            Debug.Log("[build123] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }

        static void Reminder(System.Action<string, bool, string> Check)
        {
            long keepClock = DailyReminder.TestClock;
            try
            {
                ClearReminder();
                DailyReminder.TestClock = 1_700_000_000L;
                Check("reminder-never-due-before-a-claim", !DailyReminder.PromptDue() && !DailyReminder.ClaimedEver(),
                    "due " + DailyReminder.PromptDue());

                DailyReminder.NoteClaimed();
                bool due = DailyReminder.PromptDue();
                Check("reminder-due-after-a-claim", due && DailyReminder.ClaimedEver(),
                    "due " + due);

                var go = new GameObject("drive-reminder");
                string fault = "";
                bool closed = false;
                try
                {
                    var app = go.AddComponent<FlockFiveApp>();
                    var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    var t = typeof(FlockFiveApp);
                    t.GetField("_dailyAskOpen", flags).SetValue(app, true);
                    t.GetField("_dailyAskAt", flags).SetValue(app, -1f);
                    PlayerPrefs.DeleteKey("flockfive.welcome.pending");
                    var close = t.GetMethod("CloseDailyAsk", flags);
                    try { close.Invoke(app, new object[] { false }); }
                    catch (System.Exception ex)
                    {
                        var inner = ex.InnerException ?? ex;
                        fault = inner.GetType().Name + " " + inner.Message;
                    }
                    closed = !(bool)t.GetField("_dailyAskOpen", flags).GetValue(app);
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
                bool cooled = !DailyReminder.PromptDue();
                Check("reminder-one-decline-closes-and-sets-cooldown", closed && cooled, "closed " + closed + " cooled " + cooled + " " + fault);

                DailyReminder.TestClock += DailyReminder.AskCooldownSec - 60L;
                bool inside = !DailyReminder.PromptDue();
                DailyReminder.TestClock += 120L;
                bool after = DailyReminder.PromptDue();
                Check("reminder-not-again-within-7-days", inside && after,
                    "inside " + inside + " after " + after);

                string ui = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.DailyBonus.cs"));
                int tick = ui.IndexOf("void TickDailyBonus()");
                int idle = tick < 0 ? -1 : ui.IndexOf("public static bool ReminderOpensNow", tick);
                string tickBody = tick >= 0 && idle > tick ? ui.Substring(tick, idle - tick) : "";
                int fin = ui.IndexOf("IEnumerator FinishDailyClaim");
                int note = fin < 0 ? -1 : ui.IndexOf("void NoteWelcomeClaim", fin);
                string finBody = fin >= 0 && note > fin ? ui.Substring(fin, note - fin) : "";
                bool wiring = tickBody.IndexOf("_dailyAskOpen = true") < 0
                    && finBody.IndexOf("_dailyAskOpen = true") >= 0
                    && finBody.IndexOf("DailyReminder.PromptDue()") >= 0;
                Check("reminder-opens-only-from-post-claim", wiring,
                    "tickOpen " + (tickBody.IndexOf("_dailyAskOpen = true") >= 0)
                    + " claimOpen " + (finBody.IndexOf("_dailyAskOpen = true") >= 0));
            }
            finally
            {
                ClearReminder();
                DailyReminder.TestClock = keepClock;
            }
        }

        static void ClearReminder()
        {
            PlayerPrefs.DeleteKey("flockfive.daily.asked");
            PlayerPrefs.DeleteKey("flockfive.daily.askYes");
            PlayerPrefs.DeleteKey("flockfive.daily.askN");
            PlayerPrefs.DeleteKey("flockfive.daily.askedAt");
            PlayerPrefs.DeleteKey("flockfive.daily.claimedEver");
            PlayerPrefs.DeleteKey("flockfive.daily.osOwed");
            PlayerPrefs.DeleteKey("flockfive.welcome.pending");
        }

        // Point windows Brandon named. Safe top / bottom match the device table.
        // Phone seats are 44 pt. 820 keeps its previous size. Padding matches the hive.
        static void Corners(System.Action<string, bool, string> Check)
        {
            var devs = new[]
            {
                new[] { 402f, 874f, 62f, 34f, 45.14f },
                new[] { 412f, 915f, 59f, 34f, 47.25f },
                new[] { 360f, 780f, 32f, 16f, 44.00f },
                new[] { 820f, 1180f, 24f, 20f, 84.44f },
            };
            var prev = DeviceFit.TestWindow;
            try
            {
                for (int i = 0; i < devs.Length; i++)
                {
                    float w = devs[i][0], h = devs[i][1], top = devs[i][2], bot = devs[i][3], want = devs[i][4];
                    DeviceFit.TestWindow = w / h;
                    var safe = new Rect(0f, bot, w, h - bot - top);
                    CornerAnchor.Garden(w, h, safe, out float sc, out _, out _, out var restart, out var hive);
                    var home = CornerAnchor.TopBarHome(restart, w, safe, h);
                    float safeTop = h - safe.yMax;
                    float hivePad = safe.xMax - hive.xMax;
                    float onePt = Mathf.Max(h / 852f, 1f);
                    float norm = safeTop / h;
                    float tall = CornerAnchor.TallFor(w, h);
                    float fy = WorldBuilder.FeederYFor(tall, norm, w, h);
                    bool feeders = true;
                    for (int count = 2; count <= 3 && feeders; count++)
                        for (int slot = 0; slot < count; slot++)
                        {
                            float fx = FeederRow.X(slot, count);
                            var a = CornerAnchor.WorldToGui(w, h, fx - FeederRow.BulbHalf, fy + 1.34f);
                            var b = CornerAnchor.WorldToGui(w, h, fx + FeederRow.BulbHalf, fy - 1.47f);
                            var bulb = Rect.MinMaxRect(a.x, a.y, b.x, b.y);
                            if (bulb.Overlaps(restart) || bulb.Overlaps(home)) feeders = false;
                        }
                    float field = CornerAnchor.PlayfieldTopGui(w, h, norm);
                    string tag = ((int)w).ToString();
                    bool pads = Mathf.Abs(hivePad - CornerAnchor.HiveEdgePad(sc)) <= onePt
                        && Mathf.Abs((home.xMin - safe.xMin) - hivePad) <= onePt
                        && Mathf.Abs((home.yMin - safeTop) - hivePad) <= onePt
                        && Mathf.Abs((safe.xMax - restart.xMax) - hivePad) <= onePt
                        && Mathf.Abs((restart.yMin - safeTop) - hivePad) <= onePt;
                    bool size = Mathf.Abs(restart.width - want) <= 0.75f
                        && Mathf.Abs(restart.height - restart.width) <= 0.05f
                        && Mathf.Abs(home.width - restart.width) <= 0.05f
                        && restart.yMax <= field + 0.5f
                        && feeders;
                    bool route = tag == "820"
                        ? !CornerAnchor.TallPhone(w, h)
                        : CornerAnchor.TallPhone(w, h) && restart.width + 0.5f >= 44f * onePt;
                    Check("corner-seat-" + tag, pads && size && route,
                        "size " + restart.width.ToString("0.00") + " hive " + hivePad.ToString("0.00")
                        + " homeL " + (home.xMin - safe.xMin).ToString("0.00")
                        + " feeders " + feeders + " field " + field.ToString("0.0"));

                    var homeOld = CornerAnchor.HomeIconAt28(home);
                    var restartOld = CornerAnchor.RestartIconAt28(restart);
                    CornerAnchor.HomeMark(home, out var homeIcon, out var homeWord);
                    var restartIcon = CornerAnchor.RestartIcon(restart);
                    var restartWord = CornerAnchor.RestartWordRect(restart, sc);
                    int homePx = Mathf.Clamp(Mathf.RoundToInt(home.height * 0.30f), 13, 28);
                    if (homePx > 12) homePx -= 1;
                    int restartPx = Mathf.Max(8, Mathf.RoundToInt(9f * Mathf.Min(sc, 1.15f)));
                    bool bigger = homeIcon.width > homeOld.width + 0.5f
                        && homeIcon.height > homeOld.height + 0.5f
                        && restartIcon.width > restartOld.width + 0.5f
                        && restartIcon.height > restartOld.height + 0.5f;
                    bool sameText = CornerAnchor.HomeWordPx(home.height) == homePx
                        && CornerAnchor.RestartWordPx(sc) == restartPx
                        && Mathf.Abs(homeWord.height - homePx) <= 0.05f
                        && Mathf.Abs(restartWord.height - restartPx) <= 0.05f;
                    bool onPlate = home.Contains(homeWord.center) && restart.Contains(restartWord.center)
                        && !homeWord.Overlaps(homeIcon);
                    bool onScreen = homeWord.xMin >= -0.5f && homeWord.xMax <= w + 0.5f
                        && restartWord.xMin >= -0.5f && restartWord.xMax <= w + 0.5f
                        && homeWord.yMin >= 0f && homeWord.yMax <= h
                        && restartWord.yMin >= 0f && restartWord.yMax <= h;
                    bool labelClear = true;
                    for (int count = 2; count <= 3 && labelClear; count++)
                        for (int slot = 0; slot < count; slot++)
                        {
                            float fx = FeederRow.X(slot, count);
                            var a = CornerAnchor.WorldToGui(w, h, fx - FeederRow.BulbHalf, fy + 1.34f);
                            var b = CornerAnchor.WorldToGui(w, h, fx + FeederRow.BulbHalf, fy - 1.47f);
                            var bulb = Rect.MinMaxRect(a.x, a.y, b.x, b.y);
                            if (bulb.Overlaps(homeWord) || bulb.Overlaps(restartWord)) labelClear = false;
                        }
                    Check("corner-mark-" + tag, bigger && sameText && onPlate && onScreen && labelClear,
                        "homeIcon " + homeOld.width.ToString("0.00") + "->" + homeIcon.width.ToString("0.00")
                        + " restartIcon " + restartOld.width.ToString("0.00") + "x" + restartOld.height.ToString("0.00")
                        + "->" + restartIcon.width.ToString("0.00") + "x" + restartIcon.height.ToString("0.00")
                        + " text " + homePx + "/" + restartPx
                        + " clear " + labelClear);
                }
            }
            finally
            {
                DeviceFit.TestWindow = prev;
            }
        }

        static void Hud(System.Action<string, bool, string> Check)
        {
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            int paint = app.IndexOf("void PaintAfterPointer()");
            int curtain = paint < 0 ? -1 : app.IndexOf("void FinishLevelCurtain()", paint);
            string hud = paint >= 0 && curtain > paint ? app.Substring(paint, curtain - paint) : "";
            bool noChip = hud.IndexOf("DrawGardenStamp(") < 0 && hud.IndexOf("GardenStampBox(") < 0;
            bool plate = hud.IndexOf("DrawCornerButtons(") >= 0
                && hud.IndexOf("GUI.DrawTexture(home, Texture2D.whiteTexture)") < 0
                && app.IndexOf("DrawGardenHome(") >= 0
                && app.IndexOf("StampOutlined(restart, \"Restart\"") >= 0
                && app.IndexOf("DrawRoundedFace(home,") >= 0
                && app.IndexOf("DrawHomeGlyph(") >= 0
                && app.IndexOf("StampOutlined(word, \"Home\"") >= 0
                && app.IndexOf("home.height * 0.30f") >= 0;
            int reward = app.IndexOf("void DrawRewardStamp(");
            bool rewardKept = reward >= 0 && app.IndexOf("DrawRewardStamp(", reward + 10) >= 0;
            Check("play-hud-has-no-garden-stamp", noChip && rewardKept, "hud " + hud.Length + " chip " + !noChip);
            Check("garden-home-is-a-rounded-plate", plate, "plate " + plate);
        }

        static void Pig(System.Action<string, bool, string> Check)
        {
            PlayerPrefs.DeleteKey("flockfive.pig.pokes");
            PigPoke.Boot();
            int before = Purse.Coins;
            bool zero = true;
            string pays = "";
            for (int n = 0; n < 120; n++)
            {
                var award = PigPoke.Poke();
                if (award.Coins != 0 || award.TripleCoin || award.MilestoneCoin)
                {
                    zero = false;
                    pays += n + ":" + award.Coins + " ";
                }
            }
            int after = Purse.Coins;
            Check("piggy-poke-awards-0-coins", zero && after == before,
                "delta " + (after - before) + " " + pays);

            AdPause.ResetForTest();
            var go = new GameObject("drive-pig");
            string fault = "";
            bool tipped = false;
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var poke = typeof(FlockFiveApp).GetMethod("TryPigPoke", flags);
                poke.Invoke(app, null);
                float until = (float)typeof(FlockFiveApp).GetField("_coinTipUntil", flags).GetValue(app);
                float now = AdPause.Unscaled;
                float hold = until - now;
                tipped = hold + 0.05f >= FlockFiveApp.CoinTipSeconds && hold < FlockFiveApp.CoinTipSeconds + 1f;
                fault = "hold " + hold.ToString("0.00");
            }
            catch (System.Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                fault = inner.GetType().Name + " " + inner.Message;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            string appSrc = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            int pokeAt = appSrc.IndexOf("void TryPigPoke()");
            int next = pokeAt < 0 ? -1 : appSrc.IndexOf("static bool OnGuiOpen", pokeAt);
            string pokeBody = pokeAt >= 0 && next > pokeAt ? appSrc.Substring(pokeAt, next - pokeAt) : "";
            bool shows = pokeBody.IndexOf("ShowCoinTip()") >= 0
                && pokeBody.IndexOf("Purse.Credit") < 0
                && appSrc.IndexOf("Coins you win go in the piggy.") >= 0;
            Check("piggy-tap-sets-the-coin-tip", tipped && shows, fault + " shows " + shows);
        }

        static void HiveGround(System.Action<string, bool, string> Check)
        {
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            var prev = DeviceFit.TestWindow;
            try
            {
                winOn.SetValue(null, true);
                winW.SetValue(null, 820);
                winH.SetValue(null, 1180);
                DeviceFit.TestWindow = 820f / 1180f;
                var insets = new[] { new Vector2(0f, 0f), new Vector2(59f, 34f) };
                int[] gardens = { 2, 3, 27 };
                foreach (var ins in insets)
                {
                    safeTop.SetValue(null, ins.x);
                    safeBot.SetValue(null, ins.y);
                    float w = PhoneArea.W;
                    float h = PhoneArea.H;
                    var safe = PhoneArea.Safe;
                    CornerAnchor.Garden(w, h, safe, out _, out _, out _, out _, out var hive);
                    float tall = CornerAnchor.TallFor(w, h);
                    float low = WorldBuilder.RowY0Base(tall) - (WorldBuilder.Rows - 1) * WorldBuilder.RowGap + BranchHit.MainY0;
                    float branchBot = CornerAnchor.WorldToGui(w, h, 0f, low).y;
                    CornerAnchor.AdRowBand(w, h, out var signL, out var signR, out var woodL, out var woodR, out _, out _);
                    bool below = hive.yMin + 0.5f >= branchBot;
                    bool clearBranch = !CornerAnchor.OverlapsBirdRow(hive, w, h);
                    bool clearGift = !hive.Overlaps(signL) && !hive.Overlaps(signR)
                        && !hive.Overlaps(woodL) && !hive.Overlaps(woodR);
                    float signBot = Mathf.Max(signL.yMax, signR.yMax);
                    bool air = hive.yMin >= signBot + 10f;
                    foreach (int g in gardens)
                    {
                        Check("hive-ground-g" + g + "-" + (int)ins.x + "-" + (int)ins.y,
                            below && clearBranch && clearGift && air && hive.height > 8f,
                            "hive " + hive.yMin.ToString("0.0") + ".." + hive.yMax.ToString("0.0")
                            + " branch " + branchBot.ToString("0.0") + " sign " + signBot.ToString("0.0"));
                    }
                }
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
                DeviceFit.TestWindow = prev;
            }
        }

        static void AdNotReady(System.Action<string, bool, string> Check)
        {
            bool second = FlockFiveApp.AdFailDecision(false, false, false, false) == FlockFiveApp.AdFailAction.Close;
            bool holds = FlockFiveApp.AdNotReadyHoldSeconds >= 1f && FlockFiveApp.AdNotReadyHoldSeconds < 3f;
            bool copy = AdRetry.Line.IndexOf("Ad not ready") >= 0;
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            int body = app.IndexOf("IEnumerator WatchGiftBody()");
            int end = body < 0 ? -1 : app.IndexOf("IEnumerator GiftPopBursts", body);
            string chunk = body >= 0 && end > body ? app.Substring(body, end - body) : "";
            bool shipping = chunk.Contains("AdRetry.Offer()")
                && chunk.Contains("WaitForSecondsRealtime(AdNotReadyHoldSeconds)")
                && chunk.IndexOf("UNITY_IOS") < 0
                && chunk.IndexOf("UNITY_ANDROID") < 0;
            Check("second-gift-shows-ad-not-ready", second && holds && copy && shipping,
                "close " + second + " hold " + FlockFiveApp.AdNotReadyHoldSeconds + " line " + AdRetry.Line);
        }
    }
}
#endif

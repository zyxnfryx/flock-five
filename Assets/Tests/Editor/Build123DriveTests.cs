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
        // Phone seats are the feeder air (halo to the widest bulb), not the 72-ref
        // plate: that plate covers a bulb. 820 is the wide iPad seat, uncapped.
        static void Corners(System.Action<string, bool, string> Check)
        {
            var devs = new[]
            {
                new[] { 402f, 874f, 62f, 34f, 36.84f },
                new[] { 412f, 915f, 59f, 34f, 37.03f },
                new[] { 360f, 780f, 32f, 16f, 33.09f },
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
                    CornerAnchor.Garden(w, h, safe, out _, out _, out _, out var restart, out var hive);
                    var home = CornerAnchor.TopBarHome(restart, w, safe, h);
                    float art = CornerAnchor.HiveArtRight(hive);
                    float feeder = CornerAnchor.FeederRightGui(w, h);
                    float clear = Mathf.Max(0f, art - feeder);
                    string tag = ((int)w).ToString();
                    bool size = Mathf.Abs(restart.width - want) <= 0.75f
                        && Mathf.Abs(restart.height - restart.width) <= 0.05f
                        && Mathf.Abs(home.width - restart.width) <= 0.05f
                        && Mathf.Abs(home.height - restart.height) <= 0.05f
                        && restart.xMin + 0.51f >= feeder
                        && restart.width <= clear + 0.51f;
                    bool route = tag == "820"
                        ? !CornerAnchor.TallPhone(w, h)
                        : CornerAnchor.TallPhone(w, h);
                    Check("corner-seat-" + tag, size && route,
                        "restart " + restart.width.ToString("0.00") + " home " + home.width.ToString("0.00")
                        + " want " + want.ToString("0.00") + " tall " + CornerAnchor.TallPhone(w, h));
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
            bool plate = hud.IndexOf("DrawGardenHome(") >= 0
                && hud.IndexOf("GUI.DrawTexture(home, Texture2D.whiteTexture)") < 0
                && app.IndexOf("DrawRoundedFace(home,") >= 0
                && app.IndexOf("StampOutlined(home, \"Home\"") >= 0;
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
    }
}
#endif

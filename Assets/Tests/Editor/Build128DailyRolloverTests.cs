#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b128: Daily rollover at local midnight. The shown streak is the offer streak (saved + 1 after a
    // one-day gap, 1 after a miss), the bonus is Ready, and the same state is reached when the app
    // resumes later. Claim and persistence semantics are untouched.
    public static class Build128DailyRolloverTests
    {
        const string Out = "/tmp/flock-five-b128-tests.txt";
        static readonly string[] Keys =
        {
            "flockfive.daily.last", "flockfive.daily.last.x",
            "flockfive.daily.streak", "flockfive.daily.streak.x",
            "flockfive.daily.pay", "flockfive.daily.pay.x",
            "flockfive.daily.reclaim", "flockfive.daily.reclaim.x",
        };

        [MenuItem("Flock Five/Build 128 Daily Rollover Tests")]
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
                Debug.Log("[build128] " + line);
            }

            var had = new bool[Keys.Length];
            var str = new string[Keys.Length];
            var num = new int[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                had[i] = PlayerPrefs.HasKey(Keys[i]);
                if (!had[i]) continue;
                str[i] = PlayerPrefs.GetString(Keys[i], "\u0001");
                num[i] = PlayerPrefs.GetInt(Keys[i], int.MinValue);
            }
            try
            {
                // Open at midnight: claimed on day 1 with streak 1, the clock reaches day 2.
                DailyBonus.TestBegin(20261018, 20261018, 1);
                Check("claimed-same-day", DailyBonus.ClaimedToday && !DailyBonus.OfferReady
                    && DailyBonus.IconState == DailyBonusIconState.Flame && DailyBonus.StreakLine == "1 day streak",
                    DailyBonus.StreakLine);
                DailyBonus.TestToday = 20261019;
                bool rolled = DailyBonus.RefreshDay();
                Check("midnight-gap1-ready-offer-plus-one", rolled && DailyBonus.OfferReady && !DailyBonus.ClaimedToday
                    && DailyBonus.OfferStreak == 2 && DailyBonus.StreakLine == "2 day streak"
                    && DailyBonus.StreakDigits == "2" && DailyBonus.IconState == DailyBonusIconState.Badge
                    && DailyBonus.Streak == 1,
                    "ready=" + DailyBonus.OfferReady + " offer=" + DailyBonus.OfferStreak + " line=" + DailyBonus.StreakLine
                    + " saved=" + DailyBonus.Streak);
                Check("midnight-second-refresh-noop", !DailyBonus.RefreshDay() && DailyBonus.OfferStreak == 2, "");

                // Streak 4 -> 5 crosses a cycle edge and a digit count is irrelevant: still saved + 1.
                DailyBonus.TestBegin(20261031, 20261031, 9);
                DailyBonus.TestToday = 20261101;
                DailyBonus.RefreshDay();
                Check("month-end-gap1", DailyBonus.OfferReady && DailyBonus.OfferStreak == 10
                    && DailyBonus.StreakLine == "10 day streak", DailyBonus.StreakLine);

                // Missed a day: back to day 1, still Ready.
                DailyBonus.TestBegin(20261018, 20261018, 6);
                DailyBonus.TestToday = 20261020;
                bool rolled2 = DailyBonus.RefreshDay();
                Check("midnight-gap2-offers-day-one", rolled2 && DailyBonus.OfferReady && DailyBonus.OfferStreak == 1
                    && DailyBonus.StreakLine == "1 day streak" && DailyBonus.IconState == DailyBonusIconState.Badge,
                    "offer=" + DailyBonus.OfferStreak + " line=" + DailyBonus.StreakLine);

                // Same day: no rollover, stays claimed.
                DailyBonus.TestBegin(20261018, 20261018, 3);
                bool same = DailyBonus.RefreshDay();
                Check("same-day-stays-claimed", !same && DailyBonus.ClaimedToday && !DailyBonus.OfferReady
                    && DailyBonus.StreakLine == "3 day streak", DailyBonus.StreakLine);

                // Resume after midnight: the app was booted (and backgrounded) on day 1 and is
                // brought forward on day 2 with no tick in between. One refresh reaches Ready.
                DailyBonus.TestBegin(20261018, 20261018, 2);
                string beforeLine = DailyBonus.StreakLine;
                DailyBonus.TestToday = 20261019;
                bool resumed = DailyBonus.RefreshDay();
                Check("resume-after-midnight-refreshes", beforeLine == "2 day streak" && resumed && DailyBonus.OfferReady
                    && DailyBonus.OfferStreak == 3 && DailyBonus.StreakLine == "3 day streak",
                    beforeLine + " -> " + DailyBonus.StreakLine);

                // Cold start the next day (notification tap with the app killed): Boot lands on the same state.
                DailyBonus.TestBegin(20261019, 20261018, 2);
                Check("cold-start-next-day-ready", DailyBonus.OfferReady && DailyBonus.OfferStreak == 3
                    && DailyBonus.StreakLine == "3 day streak" && DailyBonus.IconState == DailyBonusIconState.Badge,
                    DailyBonus.StreakLine);

                // Claiming the new day persists the offer streak, then the next midnight offers +1 again.
                bool spin = DailyBonus.TryBeginSpin(false, out _, out _, out _);
                Check("claim-persists-offer-streak", spin && DailyBonus.Streak == 3 && DailyBonus.ClaimedToday
                    && DailyBonus.StreakLine == "3 day streak", "streak=" + DailyBonus.Streak);
                DailyBonus.TestToday = 20261020;
                DailyBonus.RefreshDay();
                Check("next-midnight-offers-four", DailyBonus.OfferReady && DailyBonus.OfferStreak == 4
                    && DailyBonus.Streak == 3, "offer=" + DailyBonus.OfferStreak);

                // Source wiring: the one helper runs on tick (before the pause return), focus and resume.
                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                string ui = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.DailyBonus.cs");
                int body = app.IndexOf("void UpdateBody()");
                int sync = body < 0 ? -1 : app.IndexOf("SyncDailyDay();", body);
                int pause = app.IndexOf("if (GamePause.Paused)", sync < 0 ? 0 : sync);
                Check("wiring-sync-before-pause-return", sync >= 0 && pause > sync, "");
                int focus = app.IndexOf("void OnApplicationFocus");
                int focusEnd = focus < 0 ? -1 : app.IndexOf("void Update()", focus);
                Check("wiring-sync-on-focus", focus >= 0 && focusEnd > focus
                    && app.Substring(focus, focusEnd - focus).Contains("SyncDailyDay();"), "");
                int resume = ui.IndexOf("void ResumeDailyReminder");
                Check("wiring-sync-on-resume", resume >= 0 && ui.IndexOf("SyncDailyDay();", resume) > resume, "");
                Check("wiring-helper-single-refresh-site",
                    CountOf(ui, "DailyBonus.RefreshDay()") == 1 && CountOf(app, "DailyBonus.RefreshDay()") == 0,
                    "ui=" + CountOf(ui, "DailyBonus.RefreshDay()"));
            }
            finally
            {
                DailyBonus.TestRelease();
                for (int i = 0; i < Keys.Length; i++)
                {
                    if (!had[i]) { PlayerPrefs.DeleteKey(Keys[i]); continue; }
                    if (str[i] != "\u0001" && str[i] != "") PlayerPrefs.SetString(Keys[i], str[i]);
                    else if (num[i] != int.MinValue) PlayerPrefs.SetInt(Keys[i], num[i]);
                }
                PlayerPrefs.Save();
                DailyBonus.Boot();
            }

            string end = fail == 0 ? "BUILD128_OK " + pass : "BUILD128_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build128] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static int CountOf(string s, string needle)
        {
            int n = 0, i = 0;
            while ((i = s.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
            return n;
        }
    }
}
#endif

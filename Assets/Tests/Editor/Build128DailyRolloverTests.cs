#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b128/b129: Daily availability is a 24-HOUR window from the last claim (Brandon 2026-10-18),
    // replacing the local-midnight rule. Same class and menu name as the b128 midnight suite, which
    // it supersedes. Rules under test:
    //   under 24h  Claimed, with the exact time left ("Ready in H:MM:SS")
    //   24h        Ready, offered streak = saved + 1
    //   24h..48h   (48h inclusive) continues, +1
    //   over 48h   offers day 1
    //   now < last Held (clock set backwards)
    //   old yyyymmdd pref migrates to the END of that local day (capped at now)
    public static class Build128DailyRolloverTests
    {
        const string Out = "/tmp/flock-five-b128-tests.txt";
        const long H = 3600L;
        static readonly string[] Keys =
        {
            "flockfive.daily.last", "flockfive.daily.last.x",
            "flockfive.daily.last24", "flockfive.daily.last24.x",
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
            int coins0 = Purse.Coins;
            try
            {
                Rules(Check);
                Migration(Check);
                Countdown(Check);
                Reminder(Check);
                Wiring(Check);
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
                if (Purse.Coins > coins0) Purse.TrySpend(Purse.Coins - coins0);
                DailyBonus.Boot();
            }

            string end = fail == 0 ? "BUILD128_OK " + pass : "BUILD128_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build128] " + end);
            if (fail > 0) throw new Exception(end);
        }

        static void Rules(Action<string, bool, string> Check)
        {
            long C = DailyBonus.NoonOf(20261018);

            // Just under 24h: still Claimed, one second left.
            DailyBonus.TestBeginAt(C + 24 * H - 1, C, 3);
            Check("under-24h-claimed-one-second-left", DailyBonus.ClaimedToday && !DailyBonus.OfferReady && !DailyBonus.Held
                && DailyBonus.RemainingSeconds == 1 && DailyBonus.CountdownLine == "Ready in 0:00:01"
                && DailyBonus.IconState == DailyBonusIconState.Flame
                && DailyBonus.StreakLine == "3 day streak" && DailyBonus.OfferStreak == 3,
                DailyBonus.CountdownLine + " " + DailyBonus.StreakLine);
            DailyBonus.TestBeginAt(C, C, 3);
            Check("just-claimed-shows-24-00-00", DailyBonus.ClaimedToday && DailyBonus.RemainingSeconds == 86400
                && DailyBonus.CountdownLine == "Ready in 24:00:00", DailyBonus.CountdownLine);
            DailyBonus.TestBeginAt(C + 1, C, 3);
            Check("one-second-in-23-59-59", DailyBonus.CountdownLine == "Ready in 23:59:59", DailyBonus.CountdownLine);
            DailyBonus.TestBeginAt(C + 14 * H + 54 * 60 + 57, C, 3);
            Check("mid-window-9-05-03", DailyBonus.CountdownLine == "Ready in 9:05:03", DailyBonus.CountdownLine);

            // Exactly 24h: Ready, offer +1, saved streak untouched, badge.
            DailyBonus.TestBeginAt(C + 24 * H, C, 3);
            Check("exactly-24h-ready-offer-plus-one", DailyBonus.OfferReady && !DailyBonus.ClaimedToday
                && DailyBonus.OfferStreak == 4 && DailyBonus.StreakLine == "4 day streak" && DailyBonus.StreakDigits == "4"
                && DailyBonus.Streak == 3 && DailyBonus.IconState == DailyBonusIconState.Badge
                && DailyBonus.RemainingSeconds == 0 && !DailyBonus.CountdownActive,
                "ready=" + DailyBonus.OfferReady + " offer=" + DailyBonus.OfferStreak);

            // The flip while the app runs: ticking seconds change nothing, the 24h second changes state once.
            DailyBonus.TestBeginAt(C + 100, C, 3);
            DailyBonus.TestNow = C + 200;
            bool t1 = DailyBonus.RefreshDay();
            Check("tick-inside-window-is-not-a-state-change", !t1 && DailyBonus.ClaimedToday && DailyBonus.RemainingSeconds == 86200
                && DailyBonus.CountdownLine == "Ready in 23:56:40", DailyBonus.CountdownLine);
            DailyBonus.TestNow = C + 24 * H - 1;
            bool t2 = DailyBonus.RefreshDay();
            DailyBonus.TestNow = C + 24 * H;
            string lineBefore = DailyBonus.StreakLine;
            bool t3 = DailyBonus.RefreshDay();
            bool t4 = DailyBonus.RefreshDay();
            Check("countdown-zero-flips-to-ready-once", !t2 && t3 && !t4 && lineBefore == "3 day streak"
                && DailyBonus.OfferReady && DailyBonus.OfferStreak == 4 && DailyBonus.StreakLine == "4 day streak"
                && DailyBonus.IconState == DailyBonusIconState.Badge, "t=" + t2 + t3 + t4);

            // 24h..48h continues (48h inclusive); over 48h resets to day 1.
            DailyBonus.TestBeginAt(C + 47 * H, C, 6);
            Check("47h-continues", DailyBonus.OfferReady && DailyBonus.OfferStreak == 7, "offer=" + DailyBonus.OfferStreak);
            DailyBonus.TestBeginAt(C + 48 * H, C, 6);
            Check("exactly-48h-still-continues", DailyBonus.OfferReady && DailyBonus.OfferStreak == 7, "offer=" + DailyBonus.OfferStreak);
            DailyBonus.TestBeginAt(C + 48 * H + 1, C, 6);
            Check("48h-plus-1s-resets-to-one", DailyBonus.OfferReady && DailyBonus.OfferStreak == 1
                && DailyBonus.StreakLine == "1 day streak" && DailyBonus.IconState == DailyBonusIconState.Badge && !DailyBonus.AtRisk,
                "offer=" + DailyBonus.OfferStreak);
            DailyBonus.TestBeginAt(C + 30 * H, C, 6);
            Check("window-open-streak-at-risk", DailyBonus.OfferReady && DailyBonus.AtRisk, "");
            DailyBonus.TestNow = C + 48 * H + 1;
            bool lapsed = DailyBonus.RefreshDay();
            Check("grace-lapses-while-ready-drops-to-day-one", lapsed && DailyBonus.OfferReady && DailyBonus.OfferStreak == 1
                && DailyBonus.StreakLine == "1 day streak" && !DailyBonus.AtRisk, DailyBonus.StreakLine);

            // A clock set backwards is Held: nothing to claim, no countdown, flame stays.
            DailyBonus.TestBeginAt(C - 1, C, 3);
            bool spinHeld = DailyBonus.TryBeginSpin(false, out _, out _, out _);
            Check("backwards-clock-held", DailyBonus.Held && !DailyBonus.OfferReady && !DailyBonus.ClaimedToday && !spinHeld
                && DailyBonus.RemainingSeconds == 0 && !DailyBonus.CountdownActive
                && DailyBonus.IconState == DailyBonusIconState.Flame && DailyBonus.StreakLine == "3 day streak",
                "held=" + DailyBonus.Held + " spin=" + spinHeld);
            DailyBonus.TestNow = C + 10;
            bool back = DailyBonus.RefreshDay();
            Check("held-clears-when-clock-catches-up", back && DailyBonus.ClaimedToday && !DailyBonus.Held, "");
            // Backwards while Ready never re-opens a spent window: claimed at C+24h, clock back to C+24h-1s.
            DailyBonus.TestBeginAt(C + 24 * H, C, 3);
            bool spin = DailyBonus.TryBeginSpin(false, out _, out int paidCoins, out _);
            long claimedAt = C + 24 * H;
            Check("claim-moves-window-and-persists", spin && DailyBonus.Streak == 4 && DailyBonus.ClaimedToday
                && DailyBonus.RemainingSeconds == 86400 && DailyBonus.TestSavedClaim == claimedAt.ToString()
                && DailyBonus.LastClaimUnix == claimedAt && !DailyBonus.TryBeginSpin(false, out _, out _, out _),
                "streak=" + DailyBonus.Streak + " saved=" + DailyBonus.TestSavedClaim);
            DailyBonus.MarkLanded();
            DailyBonus.TryResolve(false, out _);
            DailyBonus.TestNow = claimedAt - 1;
            DailyBonus.RefreshDay();
            Check("claim-then-clock-back-is-held-not-ready", DailyBonus.Held && !DailyBonus.OfferReady, "");

            // Cold start after a claim keeps the same window (persisted, not memory).
            DailyBonus.TestNow = claimedAt + 5 * H;
            DailyBonus.TestDropMemory();
            DailyBonus.TestBootAt(claimedAt + 5 * H);
            Check("cold-start-keeps-window", DailyBonus.ClaimedToday && DailyBonus.Streak == 4
                && DailyBonus.RemainingSeconds == 19 * 3600 && DailyBonus.CountdownLine == "Ready in 19:00:00", DailyBonus.CountdownLine);

            // Claiming inside the grace continues the streak; after it, day one.
            DailyBonus.TestBeginAt(C + 47 * H, C, 3);
            DailyBonus.TryBeginSpin(false, out _, out _, out _);
            int cont = DailyBonus.Streak;
            DailyBonus.TestBeginAt(C + 49 * H, C, 3);
            DailyBonus.TryBeginSpin(false, out _, out _, out _);
            Check("claim-streak-continue-vs-reset", cont == 4 && DailyBonus.Streak == 1, cont + "/" + DailyBonus.Streak);

            // First ever claim.
            DailyBonus.TestBeginAt(C, 0, 0);
            Check("never-claimed-ready-day-one", DailyBonus.OfferReady && DailyBonus.OfferStreak == 1 && DailyBonus.LastClaimUnix == 0, "");

            // In-flight spin: a claim just moved the window 24h, so the flip cannot land mid-spin. A clock jump
            // forward (or an app left on the card a day) must still not strand the unpaid spin.
            DailyBonus.TestBeginAt(C, 0, 0);
            DailyBonus.TryBeginSpin(false, out _, out int coins, out _);
            DailyBonus.MarkLanded();
            DailyBonus.TestNow = C + 25 * H;
            bool flipped = DailyBonus.RefreshDay();
            bool stillClaimed = DailyBonus.ClaimedToday;
            bool paid = DailyBonus.TryResolve(false, out int got);
            DailyBonus.TestNow = C + 25 * H + 1;
            bool flippedAfter = DailyBonus.RefreshDay();
            Check("unresolved-spin-is-paid-before-the-flip", !flipped && stillClaimed && paid && got == coins && flippedAfter && DailyBonus.OfferReady,
                "flipped=" + flipped + " paid=" + paid + " after=" + flippedAfter);
        }

        static void Migration(Action<string, bool, string> Check)
        {
            long Local(int y, int m, int d, int h) => new DateTimeOffset(new DateTime(y, m, d, h, 0, 0, DateTimeKind.Local)).ToUnixTimeSeconds();
            void Legacy(string stamp, int streak)
            {
                PlayerPrefs.DeleteKey("flockfive.daily.last24");
                PlayerPrefs.DeleteKey("flockfive.daily.last24.x");
                PlayerPrefs.SetString("flockfive.daily.last", stamp);
                PlayerPrefs.DeleteKey("flockfive.daily.last.x");
                PlayerPrefs.SetInt("flockfive.daily.streak", streak);
                PlayerPrefs.DeleteKey("flockfive.daily.streak.x");
            }

            // Claimed on the old day 10-17, opened on 10-18 09:00: anchored at 10-18 00:00, ready 10-19 00:00.
            Legacy("20261017", 4);
            DailyBonus.TestBootAt(Local(2026, 10, 18, 9));
            Check("migrate-claimed-yesterday-waits-until-midnight", DailyBonus.ClaimedToday && !DailyBonus.OfferReady
                && DailyBonus.RemainingSeconds == 15 * 3600 && DailyBonus.CountdownLine == "Ready in 15:00:00"
                && DailyBonus.TestSavedClaim == Local(2026, 10, 18, 0).ToString() && !PlayerPrefs.HasKey("flockfive.daily.last")
                && DailyBonus.Streak == 4, DailyBonus.CountdownLine + " saved=" + DailyBonus.TestSavedClaim);
            // The migrated claim is persisted: the next boot (new key) gives the same window.
            DailyBonus.TestBootAt(Local(2026, 10, 18, 10));
            Check("migrate-is-sticky-on-next-boot", DailyBonus.ClaimedToday && DailyBonus.RemainingSeconds == 14 * 3600, DailyBonus.CountdownLine);

            // Claimed today by the old rule: capped at now, never dated in the future (not Held).
            Legacy("20261018", 2);
            long now = Local(2026, 10, 18, 15);
            DailyBonus.TestBootAt(now);
            Check("migrate-claimed-today-capped-at-now", DailyBonus.ClaimedToday && !DailyBonus.Held
                && DailyBonus.RemainingSeconds == 86400 && DailyBonus.LastClaimUnix == now, "last=" + DailyBonus.LastClaimUnix);

            // Old gap 1 (claimed 10-16, now 10-18 noon): old rule Ready with streak+1. Anchor 10-17 00:00 -> 36h: Ready, +1.
            Legacy("20261016", 4);
            DailyBonus.TestBootAt(Local(2026, 10, 18, 12));
            Check("migrate-old-gap-1-ready-plus-one", DailyBonus.OfferReady && DailyBonus.OfferStreak == 5 && DailyBonus.Streak == 4,
                "offer=" + DailyBonus.OfferStreak);
            // Old gap 3 (claimed 10-16, now 10-19 noon): old rule reset; anchor 10-17 00:00 -> 60h: reset.
            Legacy("20261016", 4);
            DailyBonus.TestBootAt(Local(2026, 10, 19, 12));
            Check("migrate-old-gap-3-resets", DailyBonus.OfferReady && DailyBonus.OfferStreak == 1, "offer=" + DailyBonus.OfferStreak);
            // Old rule gap 2 (claimed 10-16, now 10-18 23:00) would reset; the migrated window is more generous: 47h, still +1.
            Legacy("20261016", 4);
            DailyBonus.TestBootAt(Local(2026, 10, 18, 23));
            Check("migrate-generous-never-loses-streak", DailyBonus.OfferReady && DailyBonus.OfferStreak == 5, "offer=" + DailyBonus.OfferStreak);
            // Future-dated stamp (clock moved back after a claim): becomes now, claimed, streak kept.
            Legacy("20261025", 3);
            now = Local(2026, 10, 18, 12);
            DailyBonus.TestBootAt(now);
            Check("migrate-future-stamp-becomes-now", DailyBonus.ClaimedToday && DailyBonus.LastClaimUnix == now && DailyBonus.Streak == 3, "");
            // Garbage in the old pref: no claim.
            Legacy("not-a-day", 3);
            DailyBonus.TestBootAt(Local(2026, 10, 18, 12));
            Check("migrate-garbage-is-no-claim", DailyBonus.OfferReady && DailyBonus.OfferStreak == 1 && DailyBonus.LastClaimUnix == 0, "");
            // A corrupt NEW pref falls back to no claim and is not resurrected from the (already deleted) old one.
            DailyBonus.TestBeginAt(Local(2026, 10, 18, 12), Local(2026, 10, 18, 11), 3);
            PlayerPrefs.SetString("flockfive.daily.last24", "9999999999");
            DailyBonus.TestBootAt(Local(2026, 10, 18, 12) + 1);
            Check("tampered-claim-pref-is-no-claim", DailyBonus.LastClaimUnix == 0 && DailyBonus.OfferReady, "last=" + DailyBonus.LastClaimUnix);

            // Property over every claim hour: migration never grants early and never drops a streak
            // the old calendar rule would have kept (old: Ready only on D+1, streak continues on D+1).
            bool never = true, keeps = true;
            string why = "";
            for (int h = 0; h < 24; h++)
            {
                long realClaim = Local(2026, 10, 17, 0) + h * H + 1800;
                long anchor = DailyBonus.MigratedClaim(20261017, Local(2026, 10, 19, 0) + 5);
                if (anchor + 24 * H < realClaim + 24 * H) { never = false; why += " early@" + h; }
            }
            for (int k = 0; k < 48; k++)
            {
                Legacy("20261017", 4);
                DailyBonus.TestBootAt(Local(2026, 10, 18, 0) + k * H + 1800);
                bool ok = DailyBonus.ClaimedToday || (DailyBonus.OfferReady && DailyBonus.OfferStreak == 5);
                if (!ok) { keeps = false; why += " lost@" + k; }
                // Old rule: the whole of 10-18 (k 0..23) is the continue day. The new gate is never Ready early:
                if (k < 24 && DailyBonus.OfferReady) { keeps = false; why += " early-ready@" + k; }
            }
            Check("migrate-never-early", never, why);
            Check("migrate-never-loses-streak-across-next-48h", keeps, why);
        }

        static void Countdown(Action<string, bool, string> Check)
        {
            Check("format-clock", DailyBonus.FormatClock(0) == "0:00:00" && DailyBonus.FormatClock(1) == "0:00:01"
                && DailyBonus.FormatClock(59) == "0:00:59" && DailyBonus.FormatClock(60) == "0:01:00"
                && DailyBonus.FormatClock(3599) == "0:59:59" && DailyBonus.FormatClock(3600) == "1:00:00"
                && DailyBonus.FormatClock(3725) == "1:02:05" && DailyBonus.FormatClock(36000) == "10:00:00"
                && DailyBonus.FormatClock(86399) == "23:59:59" && DailyBonus.FormatClock(86400) == "24:00:00"
                && DailyBonus.FormatClock(-5) == "0:00:00", "");
            long C = DailyBonus.NoonOf(20261018);
            DailyBonus.TestBeginAt(C + 100, C, 3);
            string a = DailyBonus.CountdownLine;
            string b = DailyBonus.CountdownLine;
            Check("countdown-text-cached-within-a-second", ReferenceEquals(a, b), a);
            DailyBonus.TestNow = C + 101;
            DailyBonus.RefreshDay();
            Check("countdown-text-changes-with-the-second", DailyBonus.CountdownLine == "Ready in 23:58:19" && !ReferenceEquals(a, DailyBonus.CountdownLine), DailyBonus.CountdownLine);
        }

        static DateTime Loc(long unix) => DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;

        static void Reminder(Action<string, bool, string> Check)
        {
            long C = DailyBonus.NoonOf(20261018);
            var bg = NotifyPlan.ComputeWindow(Loc(C + H), C, 5, true);
            Check("notify-ready-at-last-claim-plus-24h", bg.Count == 2 && bg[0].Kind == NotifyPlan.Kind.Ready
                && bg[0].When == Loc(C + 24 * H) && bg[0].Body == "Your daily coins are ready.", bg.Count.ToString());
            Check("notify-keep-relative-to-grace-deadline", bg.Count == 2 && bg[1].Kind == NotifyPlan.Kind.Keep
                && bg[1].When == Loc(C + 44 * H) && bg[1].Body == "Keep your 5 day streak!", bg.Count > 1 ? bg[1].When.ToString() : "");
            var calm = NotifyPlan.ComputeWindow(Loc(C + H), C, 1, true);
            Check("notify-no-keep-without-streak", calm.Count == 1 && calm[0].When == Loc(C + 24 * H), calm.Count.ToString());
            var none = NotifyPlan.ComputeWindow(Loc(C), 0, 3, true);
            var legacy = NotifyPlan.Compute(Loc(C), 3, false, true);
            Check("notify-never-claimed-uses-legacy-plan", none.Count == legacy.Count && none[0].When == legacy[0].When, "");
            var soon = NotifyPlan.ComputeWindow(Loc(C + 23 * H), C, 5, true);
            Check("notify-background-lead-rolls-to-next-6pm", soon[0].When != Loc(C + 24 * H) && soon[0].When.Hour == 18
                && (soon[0].When - Loc(C + 23 * H)).TotalSeconds >= NotifyPlan.BackgroundGraceSeconds, soon[0].When.ToString());
            var quit = NotifyPlan.ComputeWindow(Loc(C + 25 * H), C, 5, true, NotifyPlan.Signal.Quit);
            Check("notify-ready-now-keeps-deadline-reminder", quit.Count == 2 && quit[0].Kind == NotifyPlan.Kind.Ready
                && quit[0].When.Hour == 18 && quit[1].Kind == NotifyPlan.Kind.Keep && quit[1].When == Loc(C + 44 * H), quit.Count.ToString());
            var late = NotifyPlan.ComputeWindow(Loc(C + 46 * H), C, 5, true, NotifyPlan.Signal.Quit);
            Check("notify-keep-dropped-when-past", late.Count == 1 && late[0].Kind == NotifyPlan.Kind.Ready, late.Count.ToString());
        }

        static void Wiring(Action<string, bool, string> Check)
        {
            // Source wiring: the one helper runs on tick (before the pause return), focus and resume.
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            string ui = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.DailyBonus.cs");
            string bonus = File.ReadAllText("Assets/_Project/Scripts/App/DailyBonus.cs");
            string rem = File.ReadAllText("Assets/_Project/Scripts/App/DailyReminder.cs");
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
            int syncDef = ui.IndexOf("bool SyncDailyDay()");
            int syncEnd = syncDef < 0 ? -1 : ui.IndexOf("void ResumeDailyReminder", syncDef);
            string syncBody = syncDef < 0 || syncEnd < 0 ? "" : ui.Substring(syncDef, syncEnd - syncDef);
            Check("wiring-state-change-clears-caches-and-rearms-reminder",
                syncBody.Contains("ClearDailyTextCaches();") && syncBody.Contains("RescheduleDailyReminder();")
                && ui.Contains("DailyBonus.LastClaimUnix"), "");
            Check("wiring-countdown-painted-on-card-not-on-medal",
                ui.Contains("DailyBonus.CountdownLine") && ui.Contains("DailyBonus.CountdownActive")
                && !ui.Contains("CountdownClock") && !ui.Contains("DrawRailClock") && !ui.Contains("_railClockKey"), "");
            int railDef = ui.IndexOf("void DrawDailyRail(");
            int railEnd = railDef < 0 ? -1 : ui.IndexOf("static Rect DailyFlameRect", railDef);
            string railBody = railDef < 0 || railEnd < 0 ? "" : ui.Substring(railDef, railEnd - railDef);
            Check("wiring-medal-keeps-flame-and-ready-badge-no-countdown",
                railBody.Length > 0 && railBody.Contains("DrawDailyStreakFlame(plate)") && railBody.Contains("DrawReadyBadge(")
                && !railBody.Contains("Countdown") && !railBody.Contains("StampOutlined"), "");
            Check("wiring-tap-and-gate-behavior-unchanged",
                ui.Contains("if (!DailyBonus.OfferReady && DailyBonus.ClaimedToday) ClunkClaimed();")
                && ui.Contains("if (HomeStepRail() != RailDaily && !GatePopup(PopupKind.Daily, true)) return;"), "");
            Check("wiring-no-calendar-rule-left", bonus.IndexOf("TodayStamp") < 0 && bonus.IndexOf("DaysFrom") < 0
                && bonus.IndexOf("DateTime.Now") < 0, "");
            Check("wiring-ios-trigger-keeps-minute-and-second", rem.Contains("trigger.Minute = when.Minute;")
                && rem.Contains("trigger.Second = when.Second;"), "");
        }

        static int CountOf(string s, string needle)
        {
            int n = 0, i = 0;
            while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
            return n;
        }
    }
}
#endif

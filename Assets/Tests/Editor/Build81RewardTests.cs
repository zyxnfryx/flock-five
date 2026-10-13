#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 81. A completed rewarded ad grants once: reward-then-close, close-then-reward,
    // paused, click-through, gift closed on resume, and a pref that survives a restart.
    // A skip or a no-fill grants nothing. The wheel's skip pays the documented 1x.
    // Release configuration: FF_ADS_DEV is not defined, so these call the public LevelPlay seams.
    static class Build81RewardTests
    {
        const string Pref = "flockfive.reward.pending";

        [MenuItem("Flock Five/Build 81 Reward Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build81-reward] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var snap = PrefSnap.Take();
            var prevApply = RewardedGrant.TryApply;
            bool prevEarned = Ads.LastEarned;
            bool prevGranted = Ads.LastGranted;
            int day = 20261010;
            try
            {
                Check("release-config", !Ads.MockRewarded && !Ads.TestSuite,
                    "mock=" + Ads.MockRewarded + " suite=" + Ads.TestSuite);

                var glove = Fresh();
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 2, true, 4);
                RewardedGrant.NoteCoach(true, true);
                Ads.DeliverDisplayedCallback();
                Ads.DeliverRewardedCallback();
                Ads.DeliverClosedCallback();
                Check("branch-tutorial-glove",
                    glove.Opens == 1 && RewardedGrant.ApplyCount == 1
                    && glove.Last.TutorialGoal && glove.Last.Place == RewardedGrant.Place.Branch
                    && RewardedGrant.TutorialUp && RewardedGrant.CoachGuard
                    && !RewardedGrant.ProtectsTutorial
                    && Opened(glove, 2) && Locked(glove, 3),
                    "opens=" + glove.Opens + " apply=" + RewardedGrant.ApplyCount
                    + " goal=" + glove.Last.TutorialGoal);

                // b87 (Brandon: double grant): nothing applies while the ad is up; one grant after close.
                var during = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                AdPause.Begin();
                Ads.DeliverRewardedCallback();
                int whileUp = RewardedGrant.ApplyCount;
                Ads.DeliverRewardedCallback();
                Ads.DeliverClosedCallback();
                whileUp += RewardedGrant.ApplyCount; // still frozen: nothing yet
                AdPause.End();
                Ads.DeliverRewardedCallback();
                RewardedGrant.Flush();
                Check("b87-no-grant-while-ad-up", whileUp == 0 && RewardedGrant.ApplyCount == 1 && during.Opens == 1,
                    "whileUp=" + whileUp + " apply=" + RewardedGrant.ApplyCount + " opens=" + during.Opens);
                var after = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                Ads.DeliverClosedCallback();
                Ads.DeliverRewardedCallback();
                Ads.DeliverRewardedCallback();
                Check("b87-close-then-reward-once", RewardedGrant.ApplyCount == 1 && after.Opens == 1,
                    "apply=" + RewardedGrant.ApplyCount);
                var lost = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                AdPause.Begin();
                Ads.DeliverRewardedCallback();
                int frozen = RewardedGrant.ApplyCount;
                AdPause.End(); // watchdog: the close never came
                Check("b87-missing-close-grants-once", frozen == 0 && RewardedGrant.ApplyCount == 1 && lost.Opens == 1,
                    "frozen=" + frozen + " apply=" + RewardedGrant.ApplyCount);

                var thenClose = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                Ads.DeliverRewardedCallback();
                Ads.DeliverClosedCallback();
                Ads.DeliverRewardedCallback();
                Check("branch-reward-then-close",
                    thenClose.Opens == 1 && RewardedGrant.ApplyCount == 1
                    && Opened(thenClose, 2) && Locked(thenClose, 3)
                    && !PlayerPrefs.HasKey(Pref),
                    "opens=" + thenClose.Opens + " apply=" + RewardedGrant.ApplyCount);

                var thenReward = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                Ads.DeliverClosedCallback();
                Check("branch-close-holds", thenReward.Opens == 0 && !RewardedGrant.Earned && RewardedGrant.Closed,
                    "opens=" + thenReward.Opens + " earned=" + RewardedGrant.Earned);
                Ads.DeliverRewardedCallback();
                Check("branch-close-then-reward",
                    thenReward.Opens == 1 && RewardedGrant.ApplyCount == 1
                    && Opened(thenReward, 2) && Locked(thenReward, 3),
                    "opens=" + thenReward.Opens);

                var before = Fresh();
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 2, false, 4);
                Ads.DeliverRewardedCallback();
                Ads.DeliverDisplayedCallback();
                Ads.DeliverClosedCallback();
                Check("branch-reward-before-display",
                    before.Opens == 1 && RewardedGrant.ApplyCount == 1 && Locked(before, 3),
                    "opens=" + before.Opens);

                var paused = Fresh();
                Show(RewardedGrant.Place.Branch, 2, true);
                Ads.DeliverPause(true);
                Ads.DeliverRewardedCallback();
                Check("branch-paused-holds",
                    paused.Opens == 0 && RewardedGrant.ApplyCount == 0 && RewardedGrant.Paused
                    && RewardedGrant.HasUnapplied && RewardedGrant.ProtectsTutorial
                    && Pending(RewardedGrant.Place.Branch, 2, true) && Locked(paused, 2),
                    "pending=" + PlayerPrefs.GetString(Pref, "") + " paused=" + RewardedGrant.Paused);
                Ads.DeliverPause(false);
                Check("branch-paused-then-resume",
                    paused.Opens == 1 && RewardedGrant.ApplyCount == 1 && !RewardedGrant.Paused
                    && !PlayerPrefs.HasKey(Pref) && Opened(paused, 2) && Locked(paused, 3)
                    && paused.Last.TutorialGoal,
                    "opens=" + paused.Opens + " apply=" + RewardedGrant.ApplyCount);

                var twice = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                Ads.DeliverRewardedCallback();
                Ads.DeliverRewardedCallback();
                Check("branch-double-callback",
                    twice.Opens == 1 && RewardedGrant.ApplyCount == 1 && Locked(twice, 3),
                    "opens=" + twice.Opens + " apply=" + RewardedGrant.ApplyCount);

                var click = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                Ads.DeliverPause(true);
                Ads.DeliverFocus(false);
                Ads.DeliverRewardedCallback();
                Ads.DeliverClosedCallback();
                Check("branch-click-through-holds",
                    click.Opens == 0 && RewardedGrant.ApplyCount == 0 && RewardedGrant.Paused
                    && Pending(RewardedGrant.Place.Branch, 2, false),
                    "pending=" + PlayerPrefs.GetString(Pref, ""));
                Ads.DeliverFocus(true);
                Ads.DeliverPause(false);
                Check("branch-click-through-resume",
                    click.Opens == 1 && RewardedGrant.ApplyCount == 1 && !PlayerPrefs.HasKey(Pref)
                    && Opened(click, 2) && Locked(click, 3),
                    "opens=" + click.Opens + " apply=" + RewardedGrant.ApplyCount);

                var closedFirst = Fresh();
                Show(RewardedGrant.Place.Branch, 2, true);
                Ads.DeliverPause(true);
                RewardedGrant.UiBusy = true;
                RewardedGrant.NoteUiClosed();
                RewardedGrant.UiBusy = false;
                Ads.DeliverPause(false);
                Check("branch-resume-closes-gift-first",
                    closedFirst.Opens == 0 && RewardedGrant.GiftClosed && !RewardedGrant.Earned,
                    "opens=" + closedFirst.Opens + " closed=" + RewardedGrant.GiftClosed);
                Ads.DeliverRewardedCallback();
                Check("branch-reward-after-gift-closed",
                    closedFirst.Opens == 1 && RewardedGrant.ApplyCount == 1
                    && closedFirst.Last.TutorialGoal && Opened(closedFirst, 2) && Locked(closedFirst, 3),
                    "opens=" + closedFirst.Opens);

                var restart = Fresh();
                Show(RewardedGrant.Place.Branch, 2, true);
                Ads.DeliverPause(true);
                Ads.DeliverRewardedCallback();
                string saved = PlayerPrefs.GetString(Pref, "");
                Check("branch-persisted-while-paused",
                    restart.Opens == 0 && saved.Length > 0 && Pending(RewardedGrant.Place.Branch, 2, true),
                    saved);
                RewardedGrant.ForgetSession();
                Check("branch-restart-keeps-pref",
                    PlayerPrefs.GetString(Pref, "") == saved && !RewardedGrant.Earned && restart.Opens == 0,
                    PlayerPrefs.GetString(Pref, ""));
                RewardedGrant.Boot();
                RewardedGrant.Flush();
                Check("branch-restart-grants-once",
                    restart.Opens == 1 && RewardedGrant.ApplyCount == 1 && !PlayerPrefs.HasKey(Pref)
                    && restart.Last.TutorialGoal && Opened(restart, 2) && Locked(restart, 3),
                    "opens=" + restart.Opens + " apply=" + RewardedGrant.ApplyCount);
                RewardedGrant.ForgetSession();
                RewardedGrant.Boot();
                RewardedGrant.Flush();
                Check("branch-restart-second-launch",
                    restart.Opens == 1 && RewardedGrant.ApplyCount == 0 && Locked(restart, 3),
                    "opens=" + restart.Opens + " apply=" + RewardedGrant.ApplyCount);

                var busy = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                RewardedGrant.UiBusy = true;
                Ads.DeliverRewardedCallback();
                Ads.DeliverRewardedCallback();
                Check("branch-busy-queues",
                    busy.Opens == 0 && RewardedGrant.ApplyCount == 0 && Pending(RewardedGrant.Place.Branch, 2, false),
                    "opens=" + busy.Opens);
                RewardedGrant.UiBusy = false;
                RewardedGrant.Flush();
                RewardedGrant.Flush();
                Check("branch-busy-then-free",
                    busy.Opens == 1 && RewardedGrant.ApplyCount == 1 && !PlayerPrefs.HasKey(Pref),
                    "opens=" + busy.Opens + " apply=" + RewardedGrant.ApplyCount);

                var skip = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                Ads.DeliverClosedCallback();
                Check("branch-skip",
                    skip.Opens == 0 && RewardedGrant.ApplyCount == 0 && !RewardedGrant.Earned
                    && Locked(skip, 2) && Locked(skip, 3) && !PlayerPrefs.HasKey(Pref),
                    "opens=" + skip.Opens);

                var noFill = Fresh();
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 2, false, 4);
                Ads.DeliverFailedCallback();
                Ads.DeliverRewardedCallback();
                Check("branch-nofill",
                    noFill.Opens == 0 && !RewardedGrant.Earned && RewardedGrant.ApplyCount == 0
                    && Locked(noFill, 2) && !PlayerPrefs.HasKey(Pref),
                    "earned=" + RewardedGrant.Earned + " opens=" + noFill.Opens);

                var lateFail = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                Ads.DeliverFailedCallback();
                Ads.DeliverRewardedCallback();
                Check("branch-shown-then-reward",
                    lateFail.Opens == 1 && RewardedGrant.ApplyCount == 1 && Locked(lateFail, 3),
                    "opens=" + lateFail.Opens);

                var again = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                Ads.DeliverRewardedCallback();
                Show(RewardedGrant.Place.Branch, 3, false);
                Ads.DeliverClosedCallback();
                Check("branch-next-show-not-sticky",
                    again.Opens == 1 && !RewardedGrant.Earned && Opened(again, 2) && Locked(again, 3),
                    "opens=" + again.Opens + " earned=" + RewardedGrant.Earned);

                var latched = Fresh();
                Show(RewardedGrant.Place.Branch, 2, false);
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 3, false, 4);
                Ads.DeliverRewardedCallback();
                Check("branch-latch-pays-previous",
                    latched.Opens == 1 && latched.Last.Payload == 2 && Opened(latched, 2) && Locked(latched, 3),
                    "payload=" + latched.Last.Payload + " opens=" + latched.Opens);
                Ads.DeliverDisplayedCallback();
                Ads.DeliverClosedCallback();
                Check("branch-latch-new-show-skipped",
                    latched.Opens == 1 && Locked(latched, 3),
                    "opens=" + latched.Opens);

                CheckWheel(Check, ref day);
                CheckKeep(Check);
                CheckHeal(Check);
                CheckSource(Check);
            }
            finally
            {
                RewardedGrant.ResetEmpty();
                RewardedGrant.TryApply = prevApply;
                Ads.LastEarned = prevEarned;
                Ads.LastGranted = prevGranted;
                DailyBonus.TestRelease();
                PrefSnap.Restore(snap);
                Purse.ReloadCoins();
                RestoreStreak(snap);
                DailyBonus.Boot();
            }

            Debug.Log("[build81-reward] " + (fail == 0 ? "BUILD81_REWARD_OK " + pass : "BUILD81_REWARD_FAIL " + fail + " passed " + pass));
            if (fail > 0)
                throw new System.Exception("BUILD81_REWARD_FAIL " + fail + " passed " + pass);
        }

        static void CheckWheel(System.Action<string, bool, string> Check, ref int day)
        {
            int coins = ArmWheel(day++);
            int before = Purse.Coins;
            var box = Fresh();
            Show(RewardedGrant.Place.Wheel, coins, false);
            Ads.DeliverRewardedCallback();
            Ads.DeliverClosedCallback();
            Ads.DeliverRewardedCallback();
            int delta = Purse.Coins - before;
            Check("wheel-reward-then-close",
                coins >= 100 && delta == coins * 2 && RewardedGrant.ApplyCount == 1 && box.Opens == 0,
                "coins=" + coins + " delta=" + delta + " apply=" + RewardedGrant.ApplyCount);

            coins = ArmWheel(day++);
            before = Purse.Coins;
            box = Fresh();
            Show(RewardedGrant.Place.Wheel, coins, false);
            Ads.DeliverClosedCallback();
            Check("wheel-close-holds", Purse.Coins == before && !RewardedGrant.Earned, "delta=" + (Purse.Coins - before));
            Ads.DeliverRewardedCallback();
            delta = Purse.Coins - before;
            Check("wheel-close-then-reward",
                delta == coins * 2 && RewardedGrant.ApplyCount == 1,
                "coins=" + coins + " delta=" + delta);

            coins = ArmWheel(day++);
            before = Purse.Coins;
            box = Fresh();
            Show(RewardedGrant.Place.Wheel, coins, false);
            Ads.DeliverPause(true);
            Ads.DeliverRewardedCallback();
            Check("wheel-paused-holds",
                Purse.Coins == before && RewardedGrant.ApplyCount == 0 && Pending(RewardedGrant.Place.Wheel, coins, false),
                "pending=" + PlayerPrefs.GetString(Pref, ""));
            Ads.DeliverPause(false);
            delta = Purse.Coins - before;
            Check("wheel-paused-then-resume",
                delta == coins * 2 && RewardedGrant.ApplyCount == 1 && !PlayerPrefs.HasKey(Pref),
                "delta=" + delta);

            coins = ArmWheel(day++);
            before = Purse.Coins;
            box = Fresh();
            Show(RewardedGrant.Place.Wheel, coins, false);
            Ads.DeliverRewardedCallback();
            Ads.DeliverRewardedCallback();
            delta = Purse.Coins - before;
            Check("wheel-double-callback",
                delta == coins * 2 && RewardedGrant.ApplyCount == 1,
                "delta=" + delta + " apply=" + RewardedGrant.ApplyCount);

            coins = ArmWheel(day++);
            before = Purse.Coins;
            box = Fresh();
            Show(RewardedGrant.Place.Wheel, coins, false);
            Ads.DeliverPause(true);
            Ads.DeliverFocus(false);
            Ads.DeliverRewardedCallback();
            string saved = PlayerPrefs.GetString(Pref, "");
            RewardedGrant.ForgetSession();
            DailyBonus.TestDropMemory();
            Check("wheel-restart-keeps-pref",
                PlayerPrefs.GetString(Pref, "") == saved && Purse.Coins == before,
                saved);
            RewardedGrant.Boot();
            RewardedGrant.Flush();
            delta = Purse.Coins - before;
            Check("wheel-restart-grants-once",
                delta == coins * 2 && RewardedGrant.ApplyCount == 1 && !PlayerPrefs.HasKey(Pref),
                "delta=" + delta + " apply=" + RewardedGrant.ApplyCount);
            int held = Purse.Coins;
            RewardedGrant.ForgetSession();
            DailyBonus.TestDropMemory();
            RewardedGrant.Boot();
            RewardedGrant.Flush();
            Check("wheel-restart-second-launch",
                Purse.Coins == held && RewardedGrant.ApplyCount == 0,
                "coins=" + Purse.Coins + " apply=" + RewardedGrant.ApplyCount);

            coins = ArmWheel(day++);
            before = Purse.Coins;
            box = Fresh();
            Show(RewardedGrant.Place.Wheel, coins, false);
            Ads.DeliverDisplayedCallback();
            Ads.DeliverClosedCallback();
            bool one = DailyBonus.TryResolve(false, out int paid);
            int afterOne = Purse.Coins - before;
            Ads.DeliverRewardedCallback();
            delta = Purse.Coins - before;
            Check("wheel-skip-then-late-upgrade",
                one && paid == coins && afterOne == coins && delta == coins * 2 && RewardedGrant.ApplyCount == 1,
                "paid=" + paid + " after1=" + afterOne + " delta=" + delta);

            coins = ArmWheel(day++);
            before = Purse.Coins;
            box = Fresh();
            RewardedGrant.Begin(RewardedGrant.Place.Wheel, coins, false, 4);
            Ads.DeliverFailedCallback();
            Ads.DeliverRewardedCallback();
            bool bare = DailyBonus.TryResolve(false, out int barePaid);
            delta = Purse.Coins - before;
            Check("wheel-nofill-pays-1x",
                bare && barePaid == coins && delta == coins && RewardedGrant.ApplyCount == 0 && !RewardedGrant.Earned,
                "paid=" + barePaid + " delta=" + delta);

            coins = ArmWheel(day++);
            before = Purse.Coins;
            box = Fresh();
            Show(RewardedGrant.Place.Wheel, coins, false);
            Ads.DeliverClosedCallback();
            bool skipped = DailyBonus.TryResolve(false, out int skipPaid);
            delta = Purse.Coins - before;
            Check("wheel-skip-pays-1x",
                skipped && skipPaid == coins && delta == coins && RewardedGrant.ApplyCount == 0,
                "paid=" + skipPaid + " delta=" + delta);
        }

        static void CheckKeep(System.Action<string, bool, string> Check)
        {
            Purse.BreakStreak();
            var box = Fresh();
            Show(RewardedGrant.Place.Keep, 4, false);
            Ads.DeliverRewardedCallback();
            Ads.DeliverClosedCallback();
            Ads.DeliverRewardedCallback();
            Check("keep-reward-then-close",
                Purse.Streak == 4 && RewardedGrant.ApplyCount == 1 && box.Last.Place == RewardedGrant.Place.Keep,
                "streak=" + Purse.Streak + " apply=" + RewardedGrant.ApplyCount);

            Purse.BreakStreak();
            box = Fresh();
            Show(RewardedGrant.Place.Keep, 4, false);
            Ads.DeliverClosedCallback();
            Check("keep-close-holds", Purse.Streak == 0 && !RewardedGrant.Earned, "streak=" + Purse.Streak);
            Ads.DeliverRewardedCallback();
            Check("keep-close-then-reward",
                Purse.Streak == 4 && RewardedGrant.ApplyCount == 1,
                "streak=" + Purse.Streak);

            Purse.BreakStreak();
            box = Fresh();
            Show(RewardedGrant.Place.Keep, 4, false);
            Ads.DeliverPause(true);
            Ads.DeliverRewardedCallback();
            Check("keep-paused-holds",
                Purse.Streak == 0 && RewardedGrant.ApplyCount == 0 && Pending(RewardedGrant.Place.Keep, 4, false),
                "streak=" + Purse.Streak + " pending=" + PlayerPrefs.GetString(Pref, ""));
            Ads.DeliverPause(false);
            Check("keep-paused-then-resume",
                Purse.Streak == 4 && RewardedGrant.ApplyCount == 1 && !PlayerPrefs.HasKey(Pref),
                "streak=" + Purse.Streak);

            Purse.BreakStreak();
            box = Fresh();
            Show(RewardedGrant.Place.Keep, 4, false);
            Ads.DeliverRewardedCallback();
            Ads.DeliverRewardedCallback();
            Check("keep-double-callback",
                Purse.Streak == 4 && RewardedGrant.ApplyCount == 1,
                "streak=" + Purse.Streak + " apply=" + RewardedGrant.ApplyCount);

            Purse.BreakStreak();
            box = Fresh();
            Show(RewardedGrant.Place.Keep, 4, false);
            Ads.DeliverPause(true);
            Ads.DeliverFocus(false);
            Ads.DeliverRewardedCallback();
            string saved = PlayerPrefs.GetString(Pref, "");
            RewardedGrant.ForgetSession();
            Check("keep-restart-keeps-pref",
                PlayerPrefs.GetString(Pref, "") == saved && Purse.Streak == 0,
                saved);
            RewardedGrant.Boot();
            RewardedGrant.Flush();
            Check("keep-restart-grants-once",
                Purse.Streak == 4 && RewardedGrant.ApplyCount == 1 && !PlayerPrefs.HasKey(Pref),
                "streak=" + Purse.Streak + " apply=" + RewardedGrant.ApplyCount);
            RewardedGrant.ForgetSession();
            RewardedGrant.Boot();
            RewardedGrant.Flush();
            Check("keep-restart-second-launch",
                Purse.Streak == 4 && RewardedGrant.ApplyCount == 0,
                "streak=" + Purse.Streak + " apply=" + RewardedGrant.ApplyCount);

            Purse.BreakStreak();
            box = Fresh();
            Show(RewardedGrant.Place.Keep, 4, false);
            Ads.DeliverClosedCallback();
            Check("keep-skip",
                Purse.Streak == 0 && RewardedGrant.ApplyCount == 0 && !PlayerPrefs.HasKey(Pref),
                "streak=" + Purse.Streak);

            Purse.BreakStreak();
            box = Fresh();
            RewardedGrant.Begin(RewardedGrant.Place.Keep, 4, false, 4);
            Ads.DeliverFailedCallback();
            Check("keep-nofill",
                Purse.Streak == 0 && !RewardedGrant.Earned && RewardedGrant.ApplyCount == 0,
                "streak=" + Purse.Streak + " earned=" + RewardedGrant.Earned);
        }

        static void CheckHeal(System.Action<string, bool, string> Check)
        {
            var live = TutorialHeal.Plan(new TutorSnapshot { AdHand = true, GiftMovie = true, WatchRunning = true });
            var flying = TutorialHeal.Plan(new TutorSnapshot { AdHand = true, GiftMovie = true, RewardInFlight = true });
            var splash = TutorialHeal.Plan(new TutorSnapshot { AdHand = true, GiftCard = true, Splash = true });
            var dropped = TutorialHeal.Plan(new TutorSnapshot { AdHand = true });
            Check("heal-keeps-glove-while-watching",
                (live & TutorFix.DropAdHand) == 0 && (live & TutorFix.ClearWatchStage) == 0,
                live.ToString());
            Check("heal-keeps-glove-until-grant",
                (flying & TutorFix.DropAdHand) == 0 && (flying & TutorFix.ClearWatchStage) != 0,
                flying.ToString());
            Check("heal-splash-still-drops", splash == TutorFix.DropAdHand, splash.ToString());
            Check("heal-dead-card-drops", dropped == TutorFix.DropAdHand, dropped.ToString());
        }

        static void CheckSource(System.Action<string, bool, string> Check)
        {
            string root = Application.dataPath;
            string ads = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/Ads.cs"));
            string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
            string daily = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.DailyBonus.cs"));
            string coach = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Coach.cs"));
            string grant = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/RewardedGrant.cs"));
            int pause = ads.IndexOf("void OnApplicationPause");
            int focus = ads.IndexOf("void OnApplicationFocus");
            int rewarded = ads.IndexOf("void OnRewarded");
            bool pauseOrder = pause >= 0 && ads.IndexOf("DeliverPause(paused)", pause) > pause
                && ads.IndexOf("ArmResumeSettle()", pause) > ads.IndexOf("DeliverPause(paused)", pause);
            bool focusOrder = focus >= 0 && ads.IndexOf("DeliverFocus(focus)", focus) > focus;
            bool rewardOrder = rewarded >= 0 && ads.IndexOf("DeliverRewardedCallback()", rewarded) > rewarded;
            Check("source-callback-seams",
                pauseOrder && focusOrder && rewardOrder
                && ads.IndexOf("DeliverClosedCallback()") >= 0
                && app.IndexOf("adsOn && RewardedGrant.Earned") >= 0
                && app.IndexOf("_adHand && !keep") >= 0
                && daily.IndexOf("Ads.LastEarned") >= 0
                && daily.IndexOf("PlacementDailyDouble") >= 0
                && daily.IndexOf("RewardedGrant.Earned") >= 0
                && daily.IndexOf("ResolveDailyGrant(false)") >= 0
                && coach.IndexOf("RewardedGrant.ProtectsTutorial") >= 0
                && coach.IndexOf("RewardedGrant.NoteUiClosed()") >= 0
                && grant.IndexOf("PlayerPrefs.Save()") >= 0
                && grant.IndexOf("GiftFace") < 0
                && grant.IndexOf("Movie") < 0,
                "pause=" + pauseOrder + " focus=" + focusOrder + " reward=" + rewardOrder);
        }

        static int ArmWheel(int today)
        {
            DailyBonus.TestBegin(today, 0, 0);
            if (!DailyBonus.TryBeginSpin(false, out _, out int coins, out _)) return 0;
            DailyBonus.MarkLanded();
            return coins;
        }

        static void Show(RewardedGrant.Place place, int payload, bool tutorial)
        {
            RewardedGrant.Begin(place, payload, tutorial, 4);
            Ads.DeliverDisplayedCallback();
        }

        static bool Pending(RewardedGrant.Place place, int payload, bool tutorial)
        {
            var part = PlayerPrefs.GetString(Pref, "").Split('|');
            if (part.Length != 7) return false;
            return part[1] == ((int)place).ToString()
                && part[2] == payload.ToString()
                && part[3] == (tutorial ? "1" : "0")
                && part[5] == "1"
                && part[6] == "0"
                && PlayerPrefs.HasKey(Pref + ".x");
        }

        static bool Opened(Box box, int index)
        {
            var st = box.Board.Branches[index];
            return st.IsBonus && !st.AdLocked && st.Empty && box.Claimed[index == 2 ? 0 : 1];
        }

        static bool Locked(Box box, int index)
        {
            var st = box.Board.Branches[index];
            int ord = index == 2 ? 0 : 1;
            return st.IsBonus && st.AdLocked && !box.Claimed[ord];
        }

        static Box Fresh()
        {
            var box = new Box();
            var board = new Board();
            var ruby = new BranchState();
            var teal = new BranchState();
            for (int i = 0; i < 3; i++)
            {
                ruby.Birds.Add(new Bird(BirdColor.Ruby));
                teal.Birds.Add(new Bird(BirdColor.Teal));
            }
            board.Branches.Add(ruby);
            board.Branches.Add(teal);
            board.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            board.Branches.Add(new BranchState { IsBonus = true, AdLocked = true });
            box.Board = board;
            box.Claimed = new bool[board.Branches.Count];
            RewardedGrant.ResetEmpty();
            RewardedGrant.TryApply = t =>
            {
                box.Last = t;
                switch (t.Place)
                {
                    case RewardedGrant.Place.Branch:
                        int n = RewardEffects.Branch(box.Board, box.Claimed, t.Payload, out _);
                        if (n < 0) return false;
                        if (n > 0) box.Opens++;
                        return true;
                    case RewardedGrant.Place.Wheel:
                        return RewardEffects.Wheel(t.Payload);
                    case RewardedGrant.Place.Keep:
                        return RewardEffects.Keep(t.Payload);
                    default:
                        return false;
                }
            };
            return box;
        }

        static void RestoreStreak(PrefSnap[] slots)
        {
            bool had = false;
            int streak = 0;
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].Key != "flockfive.streak") continue;
                    had = slots[i].Had;
                    streak = slots[i].I;
                    break;
                }
            }
            if (!had)
            {
                Purse.SetStreak(0);
                PlayerPrefs.DeleteKey("flockfive.streak");
                PlayerPrefs.DeleteKey("flockfive.streak.x");
                PlayerPrefs.Save();
                return;
            }
            Purse.SetStreak(streak);
        }

        sealed class Box
        {
            public Board Board;
            public bool[] Claimed;
            public int Opens;
            public RewardedGrant.Ticket Last;
        }

        struct PrefSnap
        {
            public string Key;
            public bool Had;
            public bool Text;
            public string S;
            public int I;

            public static PrefSnap[] Take()
            {
                string[] text =
                {
                    "flockfive.login.day",
                    "flockfive.daily.last",
                    "flockfive.reward.pending"
                };
                string[] ints =
                {
                    "flockfive.coins", "flockfive.coins.x",
                    "flockfive.streak", "flockfive.streak.x",
                    "flockfive.login.n", "flockfive.login.n.x",
                    "flockfive.login.day.x",
                    "flockfive.owed", "flockfive.owed.x",
                    "flockfive.instage",
                    "flockfive.daily.streak", "flockfive.daily.streak.x",
                    "flockfive.daily.pay", "flockfive.daily.pay.x",
                    "flockfive.daily.last.x",
                    "flockfive.reward.pending.x"
                };
                var list = new System.Collections.Generic.List<PrefSnap>();
                for (int i = 0; i < text.Length; i++) list.Add(Read(text[i], true));
                for (int i = 0; i < ints.Length; i++) list.Add(Read(ints[i], false));
                return list.ToArray();
            }

            static PrefSnap Read(string key, bool text)
            {
                bool had = PlayerPrefs.HasKey(key);
                return new PrefSnap
                {
                    Key = key,
                    Had = had,
                    Text = text,
                    S = had && text ? PlayerPrefs.GetString(key, "") : "",
                    I = had && !text ? PlayerPrefs.GetInt(key, 0) : 0
                };
            }

            public static void Restore(PrefSnap[] slots)
            {
                if (slots == null) return;
                for (int i = 0; i < slots.Length; i++)
                {
                    var s = slots[i];
                    if (!s.Had) PlayerPrefs.DeleteKey(s.Key);
                    else if (s.Text) PlayerPrefs.SetString(s.Key, s.S);
                    else PlayerPrefs.SetInt(s.Key, s.I);
                }
                PlayerPrefs.Save();
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Bird Care v1: the time math (decay, offline elapsed, clock changes, vacation trigger),
    // feeding rules (free, never an ad or IAP), notification rules and the feeding lesson gate.
    public static class BirdCareTests
    {
        const string Out = "/tmp/flock-five-bird-care-tests.txt";
        const long T0 = 1_800_000_000; // 2027-01-15, any fixed instant works
        const long H = 3600;

        [MenuItem("Flock Five/Bird Care Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0;
            void Check(string name, bool ok, string detail = "")
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[bird-care] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            bool Near(double a, double b, double eps = 0.01) => Math.Abs(a - b) <= eps;

            // ---- decay ----
            Check("decay-48h-full-to-empty", Near(BirdCare.FullToEmptySeconds, 48 * H));
            Check("decay-0h", Near(BirdCare.Decayed(100, 0), 100));
            Check("decay-12h", Near(BirdCare.Decayed(100, 12 * H), 75), BirdCare.Decayed(100, 12 * H).ToString());
            Check("decay-24h", Near(BirdCare.Decayed(100, 24 * H), 50));
            Check("decay-48h", Near(BirdCare.Decayed(100, 48 * H), 0));
            Check("decay-clamps-at-zero-no-death", Near(BirdCare.Decayed(10, 60 * H), 0) && BirdCare.Decayed(0, 1e9) >= 0);
            Check("decay-negative-elapsed-ignored", Near(BirdCare.Decayed(60, -5 * H), 60));
            Check("clamp-nan", Near(BirdCare.Clamp(double.NaN), 100) && Near(BirdCare.Clamp(250), 100) && Near(BirdCare.Clamp(-3), 0));

            // ---- offline elapsed ----
            BirdCare.Clock c;
            Check("elapsed-fresh", BirdCare.OfflineElapsed(0, T0, out c) == 0 && c == BirdCare.Clock.Fresh);
            Check("elapsed-normal", Near(BirdCare.OfflineElapsed(T0, T0 + 5 * H, out c), 5 * H) && c == BirdCare.Clock.Normal);
            Check("elapsed-backward-is-zero", BirdCare.OfflineElapsed(T0, T0 - 10 * H, out c) == 0 && c == BirdCare.Clock.Backward);
            Check("elapsed-clamped", Near(BirdCare.OfflineElapsed(T0, T0 + 400L * 24 * H, out c), BirdCare.MaxElapsedSeconds) && c == BirdCare.Clock.Vacation);

            // ---- advance over sessions (app kill / relaunch = same timestamps) ----
            var s = BirdCare.State.NewBird(T0);
            BirdCare.Advance(ref s, T0 + 6 * H);
            BirdCare.Advance(ref s, T0 + 12 * H);
            var one = BirdCare.State.NewBird(T0);
            BirdCare.Advance(ref one, T0 + 12 * H);
            Check("advance-split-equals-single", Near(s.Hunger, one.Hunger) && Near(s.Hunger, 75), s.Hunger + " vs " + one.Hunger);
            BirdCare.Advance(ref s, T0 + 12 * H);
            Check("advance-same-instant-no-change", Near(s.Hunger, 75));

            // ---- clock changes ----
            var b = BirdCare.State.NewBird(T0);
            BirdCare.Advance(ref b, T0 + 12 * H);           // 75
            var clk = BirdCare.Advance(ref b, T0 - 30 * H);  // clock set back a day+
            Check("clock-back-no-decay", clk == BirdCare.Clock.Backward && Near(b.Hunger, 75) && b.LastUnix == T0 + 12 * H);
            BirdCare.Advance(ref b, T0 + 6 * H);             // still behind the high-water mark
            Check("clock-back-keeps-high-water", Near(b.Hunger, 75) && b.LastUnix == T0 + 12 * H);
            BirdCare.Advance(ref b, T0 + 24 * H);            // clock restored: only the real 12h counts
            Check("clock-restored-no-double-count", Near(b.Hunger, 50), b.Hunger.ToString());
            var f = BirdCare.State.NewBird(T0);
            f.Hunger = 30;
            clk = BirdCare.Advance(ref f, T0 + 10L * 24 * H); // clock set far forward = vacation
            Check("clock-forward-lands-happy-not-dead", clk == BirdCare.Clock.Vacation && f.Hunger >= BirdCare.WelcomeBackHunger);
            // DST / timezone: the state is unix seconds, so a local-clock jump of an hour changes nothing.
            var dstA = new DateTimeOffset(2026, 11, 1, 0, 30, 0, TimeSpan.FromHours(-7)).ToUnixTimeSeconds();
            var dstB = new DateTimeOffset(2026, 11, 1, 2, 30, 0, TimeSpan.FromHours(-8)).ToUnixTimeSeconds(); // 3 real hours
            var d = BirdCare.State.NewBird(dstA);
            BirdCare.Advance(ref d, dstB);
            Check("dst-uses-real-elapsed", Near(d.Hunger, 100 - 3 * BirdCare.DecayPerSecond * H), d.Hunger.ToString("0.000"));

            // ---- vacation / nap ----
            var v = BirdCare.State.NewBird(T0);
            clk = BirdCare.Advance(ref v, T0 + 71 * H);
            Check("under-3-days-naps", clk == BirdCare.Clock.Normal && BirdCare.MoodOf(v.Hunger) == BirdCare.Mood.Nap);
            var v2 = BirdCare.State.NewBird(T0);
            v2.Hunger = 0;
            clk = BirdCare.Advance(ref v2, T0 + 72 * H);
            Check("exactly-3-days-vacation", clk == BirdCare.Clock.Vacation && BirdCare.MoodOf(v2.Hunger) == BirdCare.Mood.Fed, v2.Hunger.ToString());
            var v3 = BirdCare.State.NewBird(T0);
            v3.Hunger = 95;
            BirdCare.Advance(ref v3, T0 + 5L * 24 * H);
            Check("vacation-never-lowers", Near(v3.Hunger, 95));

            // ---- moods ----
            Check("mood-fed", BirdCare.MoodOf(60) == BirdCare.Mood.Fed && BirdCare.MoodOf(25) == BirdCare.Mood.Fed);
            Check("mood-hungry", BirdCare.MoodOf(24.9) == BirdCare.Mood.Hungry && BirdCare.MoodOf(8) == BirdCare.Mood.Hungry);
            Check("mood-nap-is-floor", BirdCare.MoodOf(7.9) == BirdCare.Mood.Nap && BirdCare.MoodOf(0) == BirdCare.Mood.Nap);

            // ---- feeding: free, nectar, never ad or IAP ----
            var e = BirdCare.State.NewBird(T0);
            e.Hunger = 10;
            var r1 = BirdCare.Feed(ref e, T0, 0);
            Check("first-feed-free", r1.Fed && r1.Source == BirdCare.FeedSource.FirstFree && r1.NectarSpent == 0 && Near(e.Hunger, 60));
            var r2 = BirdCare.Feed(ref e, T0 + 1, 0);
            Check("no-nectar-no-feed-no-ad-path", !r2.Fed && Near(e.Hunger, 60, 0.05));
            var r3 = BirdCare.Feed(ref e, T0 + 2, 3);
            Check("nectar-feed", r3.Fed && r3.Source == BirdCare.FeedSource.Nectar && r3.NectarSpent == 1 && Near(e.Hunger, 100, 0.1));
            Check("full-cannot-feed", !BirdCare.CanFeed(e, 5));
            Check("feed-sources-are-free-only", Enum.GetNames(typeof(BirdCare.FeedSource)).Length == 2
                && Array.IndexOf(Enum.GetNames(typeof(BirdCare.FeedSource)), "Ad") < 0);
            Check("ad-bonus-only-after-free-feed", BirdCare.AdBonusOpen(e, T0 + 30) && !BirdCare.AdBonusOpen(e, T0 + 2 + 121));
            var fresh = BirdCare.State.NewBird(T0);
            Check("ad-bonus-closed-without-feed", !BirdCare.AdBonusOpen(fresh, T0 + 1));
            Check("ad-bonus-once", BirdCare.TakeAdBonus(ref e, T0 + 30) && !BirdCare.TakeAdBonus(ref e, T0 + 31));
            Check("nectar-earn-capped", BirdCare.EarnNectar(0) == 1 && BirdCare.EarnNectar(8, 5) == BirdCare.NectarCap && BirdCare.EarnNectar(-4) == 1);
            Check("hunger-eta", BirdCare.WhenHungerReaches(BirdCare.State.NewBird(T0), T0, 25) == T0 + 36 * H);

            // ---- flag + analytics ----
            BirdCareFlag.ResetForTests();
            Check("flag-default-off", !BirdCareFlag.Enabled && !BirdCareFlag.NotifyFeatureOn && BirdCareFlag.ChildDirected);
            BirdCareFlag.SetRemote(true, false, 1, true);
            Check("flag-remote-on-notify-still-off", BirdCareFlag.Enabled && !BirdCareFlag.NotifyFeatureOn && BirdCareFlag.Cadence == BirdCareNotify.Cadence.TwoDayFollowUp);
            BirdCareFlag.ResetForTests();
            BirdCareAnalytics.ResetForTests();
            string last = null, src = null;
            BirdCareAnalytics.OnEvent += (n, p) => { last = n; p.TryGetValue("source", out src); };
            BirdCareAnalytics.LogFeed(r3);
            Check("analytics-feed-nectar", last == BirdCareAnalytics.Feed && src == "nectar" && BirdCareAnalytics.FeedsNectar == 1);
            BirdCareAnalytics.LogAdBonus();
            Check("analytics-ad-bonus-separate", last == BirdCareAnalytics.AdBonus && src == "ad" && BirdCareAnalytics.AdBonuses == 1 && BirdCareAnalytics.FeedsNectar == 1);
            BirdCareAnalytics.ResetForTests();

            // ---- notifications ----
            Func<long, DateTime> local = u => DateTimeOffset.FromUnixTimeSeconds(u).UtcDateTime; // fixed zone for tests
            var st = BirdCareNotify.Defaults(true);
            var full = BirdCare.State.NewBird(T0);
            Check("notify-default-off", !BirdCareNotify.Allowed(st) && BirdCareNotify.Plan(full, T0, T0, st, local).Count == 0);
            st.FeatureOn = true; st.OptedIn = true;
            Check("notify-child-directed-needs-parent", !BirdCareNotify.Allowed(st));
            st.ParentAllowed = true;
            Check("notify-allowed-with-optin", BirdCareNotify.Allowed(st));
            Check("notify-ask-only-after-feed", !BirdCareNotify.ShouldAsk(true, false, false, false, false)
                && BirdCareNotify.ShouldAsk(true, true, false, false, false)
                && !BirdCareNotify.ShouldAsk(true, true, true, false, false)
                && !BirdCareNotify.ShouldAsk(true, true, false, true, false)
                && !BirdCareNotify.ShouldAsk(false, true, false, false, false));
            bool windowOk = true;
            for (int hr = 0; hr < 24; hr++)
            {
                var t = new DateTime(2026, 10, 19, hr, 15, 0);
                var w = BirdCareNotify.IntoWindow(t, 10, 17);
                windowOk &= w.Hour >= 10 && w.Hour < 17 && w >= t;
                var wide = BirdCareNotify.IntoWindow(t, 6, 23); // bad config is clamped: never before 9, never evening
                windowOk &= wide.Hour >= BirdCareNotify.EarliestHour && wide.Hour < BirdCareNotify.EveningHour;
            }
            Check("notify-daytime-never-evening", windowOk);
            Check("notify-evening-rolls-to-morning", BirdCareNotify.IntoWindow(new DateTime(2026, 10, 19, 19, 0, 0), 10, 17) == new DateTime(2026, 10, 20, 10, 0, 0));
            var plan = BirdCareNotify.Plan(full, T0, T0, st, local);
            var eta = local(T0 + 36 * H);
            Check("notify-once-at-hungry", plan.Count == 1 && plan[0] >= eta && plan[0].Hour >= 10 && plan[0].Hour < 17, plan.Count > 0 ? plan[0].ToString("u") : "none");
            st.Cadence = BirdCareNotify.Cadence.TwoDayFollowUp;
            var planB = BirdCareNotify.Plan(full, T0, T0, st, local);
            Check("notify-cadence-b-follow-up", planB.Count <= 2 && planB.Count >= 1 && (planB.Count == 1 || (planB[1] - planB[0]).TotalHours >= 47));
            st.Ignored = 2;
            var planBack = BirdCareNotify.Plan(Hungry(BirdCare.State.NewBird(T0)), T0, T0, st, local);
            Check("notify-backoff-after-2-ignored", planBack.Count == 0 || (planBack[0] - local(T0)).TotalSeconds >= BirdCareNotify.BackoffSeconds - 1, planBack.Count.ToString());
            st.Ignored = 0;
            Check("notify-stops-after-week-away", BirdCareNotify.Plan(full, T0 + 8L * 24 * H, T0, st, local).Count == 0);
            Check("notify-copy-warm", BirdCareNotify.Warm(BirdCareNotify.Line("Goldie", 0)) && BirdCareNotify.Warm(BirdCareNotify.Line("Goldie", 1))
                && !BirdCareNotify.Warm("Goldie misses you. Quick snack?") && !BirdCareNotify.Warm("Your bird is starving"));
            Check("notify-no-misses-you", BirdCareNotify.Line("Goldie", 0).IndexOf("miss", StringComparison.OrdinalIgnoreCase) < 0
                && BirdCareNotify.Line("Goldie", 1).IndexOf("miss", StringComparison.OrdinalIgnoreCase) < 0);

            // ---- feeding lesson: shared gate, two lines ----
            Check("lesson-required", CoachTap.Kind(CoachStep.BirdFeed) == CoachTapKind.Required && CoachTap.Forced(CoachStep.BirdFeed));
            var rule = CoachTap.Rule(CoachStep.BirdFeed);
            Check("lesson-only-target-advances", rule.Advances(TapZone.Target) && !rule.Advances(TapZone.Blank) && !rule.Advances(TapZone.Focus)
                && !rule.Advances(TapZone.Back) && rule.Pointer == CoachPointer.Glove && !CoachTap.Advances(CoachStep.BirdFeed, false));
            string line = FlockFiveApp.CareTutorLine("Goldiegoldie");
            Check("lesson-caption-two-lines", line.Split('\n').Length <= 2 && BirdCareNotify.Warm(line), line.Replace("\n", " / "));
            Check("lesson-copy-warm", BirdCareNotify.Warm(FlockFiveApp.CareHungryBubble) && BirdCareNotify.Warm(FlockFiveApp.CareYumLine));

            Debug.Log("[bird-care] DONE fail=" + fail);
        }

        static BirdCare.State Hungry(BirdCare.State s) { s.Hunger = 20; return s; }
    }
}
#endif

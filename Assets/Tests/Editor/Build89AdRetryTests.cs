#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89. Bonus branch rewarded ad: a failed or not-ready request must leave a
    // visible retry state and an AdLog line, pay nothing, and never loop silently.
    public static class Build89AdRetryTests
    {
        const string Out = "/tmp/flock-five-b89-adretry-tests.txt";

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
                Debug.Log("[build89ad] " + line);
            }

            bool hadReward = PlayerPrefs.HasKey(RewardedGrant.Pref);
            string reward = PlayerPrefs.GetString(RewardedGrant.Pref, "");
            var apply = RewardedGrant.TryApply;
            try
            {
                RewardedGrant.ResetEmpty();
                AdRetry.ResetForTests();
                int applied = 0;
                RewardedGrant.TryApply = t => { applied++; return true; };

                // Not ready / no fill: show never opens.
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 0, false, 1);
                AdRetry.Begin();
                Ads.DeliverFailedCallback("no fill / not loaded within 6s");
                Check("failed request is recorded", AdRetry.Failures == 1 && AdRetry.Reason.StartsWith("no fill"), AdRetry.Reason);
                Check("failed request pays nothing", !RewardedGrant.Earned && RewardedGrant.Failed && applied == 0 && !Ads.LastGranted, "");
                bool logged = false;
                foreach (var l in AdLog.Lines) if (l.Contains("gift ad request FAILED: no fill")) logged = true;
                Check("failure is visible in AdLog", logged, "");

                Check("no grant means retry offered", AdRetry.ShouldOffer(false) && !AdRetry.ShouldOffer(true), "");
                AdRetry.Offer();
                Check("retry state on after failure", AdRetry.Active && AdRetry.Line.Contains("try again"), AdRetry.Line);

                // A retry clears the note, and a second failure counts again.
                AdRetry.Begin();
                Check("new request clears retry state", !AdRetry.Active, "");
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 0, false, 1);
                Ads.DeliverFailedCallback("display failed: test");
                Check("second failure counts", AdRetry.Failures == 2 && AdRetry.Reason.StartsWith("display failed"), AdRetry.Reason);
                Check("failure never grants", applied == 0 && !RewardedGrant.HasUnapplied, "");

                // Retry then success still grants once.
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 0, false, 1);
                AdRetry.Begin();
                RewardedGrant.OnDisplayed();
                Ads.DeliverRewardedCallback();
                Ads.DeliverClosedCallback();
                RewardedGrant.Flush();
                Check("retry that succeeds grants once", applied == 1 && RewardedGrant.Earned, "applied=" + applied);

                AdRetry.Clear();
                Check("close clears retry state", !AdRetry.Active && AdRetry.Reason == "", "");
            }
            finally
            {
                RewardedGrant.TryApply = apply;
                RewardedGrant.ResetEmpty();
                if (hadReward) PlayerPrefs.SetString(RewardedGrant.Pref, reward);
                AdRetry.ResetForTests();
                File.AppendAllText(Out, "RESULT pass=" + pass + " fail=" + fail + "\n");
                Debug.Log("[build89ad] RESULT pass=" + pass + " fail=" + fail);
            }
        }
    }
}
#endif

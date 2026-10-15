#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 91: ad skip vs fail copy, stalled-ad limits, and the finale sling helper.
    public static class Build91Tests
    {
        const string Out = "/tmp/flock-five-b91-tests.txt";

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
                Debug.Log("[build91] " + line);
            }
            try
            {
                // Skip vs fail copy
                AdRetry.ResetForTests();
                AdRetry.Begin();
                AdRetry.NoteSkip();
                AdRetry.Offer();
                Check("skip is not counted as a failure", AdRetry.Failures == 0 && AdRetry.Skipped, "failures=" + AdRetry.Failures);
                Check("skip copy differs from failure copy", AdRetry.CurrentLine != AdRetry.Line && AdRetry.CurrentLine == AdRetry.SkipLine, AdRetry.CurrentLine);
                Check("skip copy is friendly (no 'not ready')", !AdRetry.CurrentLine.ToLower().Contains("not ready") && AdRetry.CurrentLine.Contains("whole ad"), "");
                Check("skip button is Watch", AdRetry.CurrentLabel == "Watch", AdRetry.CurrentLabel);
                Check("skip still offers retry state", AdRetry.Active && AdRetry.ShouldOffer(false), "");
                AdRetry.Begin();
                Check("new request clears skip", !AdRetry.Skipped && AdRetry.CurrentLine == AdRetry.Line, "");
                AdRetry.NoteFail("display failed: test");
                AdRetry.Offer();
                Check("real failure keeps 'try again' copy", !AdRetry.Skipped && AdRetry.CurrentLine == AdRetry.Line && AdRetry.CurrentLabel == "Retry", AdRetry.CurrentLine);
                AdRetry.NoteSkip();
                AdRetry.NoteFail("stalled after show (timeout 45s)");
                Check("failure after skip clears skip flag", !AdRetry.Skipped && AdRetry.Failures == 2, "failures=" + AdRetry.Failures);
                AdRetry.Clear();
                Check("clear resets skip", !AdRetry.Skipped, "");

                // Stalled ad limits
                Check("stall limit about 45s", Ads.StallLimit >= 40f && Ads.StallLimit <= 60f, Ads.StallLimit.ToString());
                Check("cancel offered after about 10s", Ads.CancelAfter >= 8f && Ads.CancelAfter <= 12f && Ads.CancelAfter < Ads.StallLimit, Ads.CancelAfter.ToString());
                Ads.ShowWaited = 3f;
                Ads.RequestCancelWait();
                Check("cancel ignored before 10s", !Ads.CanCancelWait && !Ads.CancelRequested, "");
                Ads.ShowWaited = 11f;
                Ads.RequestCancelWait();
                Check("cancel accepted after 10s", Ads.CanCancelWait && Ads.CancelRequested, "");
                Ads.ClearCancelWait();
                Check("cancel state clears", !Ads.CanCancelWait && !Ads.CancelRequested, "");
                AdRetry.ResetForTests();
                AdRetry.NoteFail("stalled after show (cancelled by player)");
                bool logged = false;
                foreach (var l in AdLog.Lines) if (l.Contains("FAILED: stalled after show")) logged = true;
                Check("stall failure fills reason, count and log", AdRetry.Failures == 1 && AdRetry.Reason.StartsWith("stalled") && logged, AdRetry.Reason);

                // Sling helper
                Check("sling dt is spike capped", SlingClock.Dt <= SlingClock.MaxStep + 1e-6f, SlingClock.Dt.ToString());
                Check("approach curve softer than 2.4", SlingClock.ApproachPow < 2.4f && SlingClock.ApproachPow >= 1.3f, SlingClock.ApproachPow.ToString());
                var go = new GameObject("t91");
                try
                {
                    var c1 = new GameObject("c1"); c1.transform.SetParent(go.transform);
                    c1.AddComponent<SpriteRenderer>();
                    var a = SlingClock.Srs(go.transform);
                    var c2 = new GameObject("c2"); c2.transform.SetParent(go.transform);
                    c2.AddComponent<SpriteRenderer>();
                    var b = SlingClock.Srs(go.transform);
                    Check("renderer lookup is cached", ReferenceEquals(a, b) && a.Length == 1, "len=" + a.Length);
                    SlingClock.Clear();
                    Check("cache clears", SlingClock.Srs(go.transform).Length == 2, "");
                    SlingClock.Prewarm(BirdColor.Peach, go.transform, new Transform[] { c1.transform }, new Transform[0]);
                    Check("prewarm runs without throwing", true, "");
                }
                finally { SlingClock.Clear(); Object.DestroyImmediate(go); }
                for (int p = 1; p <= 4; p++)
                    Check("peach wing pose " + p + " art exists", SpriteCatalog.BirdPose(BirdColor.Peach, p) != null, "");
                Check("peach pose 3 differs from pose 1 (real in-between)",
                    SpriteCatalog.BirdPose(BirdColor.Peach, 3) != SpriteCatalog.BirdPose(BirdColor.Peach, 1), "");
            }
            catch (System.Exception e)
            {
                fail++;
                sb.AppendLine("FAIL  exception " + e);
            }
            finally
            {
                AdRetry.ResetForTests();
                Ads.ClearCancelWait();
                File.AppendAllText(Out, sb.ToString().Length == 0 ? "" : "");
                File.AppendAllText(Out, "RESULT pass=" + pass + " fail=" + fail + "\n");
                Debug.Log("[build91] RESULT pass=" + pass + " fail=" + fail);
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b111: the Level 1 gift-ad softlock. No fill never loops (tutorial grants the branch, otherwise the offer closes),
    // the X is exempt from the glove focus gate, and the unlock banner hides while the card says "Ad not ready".
    public static class Build111Tests
    {
        const string Out = "/tmp/flock-five-b111-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build111] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // args: keepStreak, freeze, tutorial, skipped
            Check("tutorial no fill grants the branch", FlockFiveApp.AdFailDecision(false, false, true, false) == FlockFiveApp.AdFailAction.GrantBranch, "");
            Check("plain gift no fill closes", FlockFiveApp.AdFailDecision(false, false, false, false) == FlockFiveApp.AdFailAction.Close, "");
            Check("player skip keeps the Watch card", FlockFiveApp.AdFailDecision(false, false, true, true) == FlockFiveApp.AdFailAction.KeepCard
                && FlockFiveApp.AdFailDecision(false, false, false, true) == FlockFiveApp.AdFailAction.KeepCard, "");
            Check("keep streak and freeze keep their card", FlockFiveApp.AdFailDecision(true, false, false, false) == FlockFiveApp.AdFailAction.KeepCard
                && FlockFiveApp.AdFailDecision(false, true, true, false) == FlockFiveApp.AdFailAction.KeepCard, "");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("gift X exempt from the glove focus gate", coach.Contains("HomeGearExempt(p0) || GiftCloseExempt(p0)") && app.Contains("bool GiftCloseExempt(Vector2 gui)"), "");
            Check("unlock banner hidden while ad not ready", app.Contains("if (!(AdRetry.Active && !AdRetry.Skipped))"), "");
            AdRetry.ResetForTests();
            AdRetry.NoteFail("no fill");
            Check("not-ready copy unchanged", AdRetry.CurrentLine == AdRetry.Line, AdRetry.Reason);
            AdRetry.ResetForTests();
        }
    }
}
#endif

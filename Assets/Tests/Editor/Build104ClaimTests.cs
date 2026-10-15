#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b104: the Daily Claim flower has no release sparkle burst or flash. Other popup buttons keep theirs.
    public static class Build104ClaimTests
    {
        const string Out = "/tmp/flock-five-b104-claim-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build104claim] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            string daily = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.DailyBonus.cs");
            Check("release fx is gated by releaseFx", app.Contains("motion.ReleasedEdge && releaseFx"), "");
            Check("claim flower passes releaseFx false", daily.Contains("DrawPopupButton(flower, held, ready, 1f, true, false)"), "");
            // Every other DrawPopupButton call keeps the default (true): only the claim line names the flag.
            int named = 0, at = 0;
            string all = app + daily;
            foreach (var f in Directory.GetFiles("Assets/_Project/Scripts/App", "FlockFiveApp*.cs"))
                { string src = File.ReadAllText(f); at = 0; while ((at = src.IndexOf("DrawPopupButton(", at, System.StringComparison.Ordinal)) >= 0) { int end = src.IndexOf(';', at); if (end > 0 && src.Substring(at, end - at).EndsWith(", false)") && src.Substring(at, end - at).Contains("1f, true, false")) named++; at++; } }
            Check("only the claim flower disables release fx", named == 1, "calls " + named);
            Check("wheel and prize effects untouched", daily.Contains("DrawDailyWheel(") && daily.Contains("SparkleFx.DrawAt(starAt"), "");
        }
    }
}
#endif

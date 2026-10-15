#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b100: press-burst arc is painted behind the LEVEL button (top curve), and the dead wait
    // between full coverage and the swap is gone. Run via -executeMethod ...Build100PressBehindTests.Run.
    public static class Build100PressBehindTests
    {
        const string Out = "/tmp/flock-five-b100-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build100] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            Check("swap at coverage", Mathf.Approximately(PressRainbow.SwapAt, PressRainbow.CoverAt), "swap=" + PressRainbow.SwapAt);
            Check("reveal within 30 ms of coverage", PressRainbow.RevealAt - PressRainbow.CoverAt <= 0.03f, "gap=" + (PressRainbow.RevealAt - PressRainbow.CoverAt));
            Check("life shorter than 2.30", PressRainbow.Life < 2.30f, "life=" + PressRainbow.Life);
            var r = new Rect(0f, 0f, 400f, 400f);
            var top = SparkleFx.OnFlatRing(r, -Mathf.PI * 0.5f);
            Check("flat ring top is above centre", top.y < r.y + r.height * SparkleFx.FlatRingCy, "y=" + top.y);
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Pure curve checks for PokerCardAnim discard shrink/pop. No scene, no play mode.
    [InitializeOnLoad]
    static class PokerDiscardAnimTests
    {
        const string Cmd = "/tmp/flock-five-poker-discard-anim-tests";
        const string Out = "/tmp/flock-five-poker-discard-anim-tests.txt";

        static PokerDiscardAnimTests()
        {
            EditorApplication.delayCall += Maybe;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (File.Exists(Cmd)) Maybe();
        }

        static void Maybe()
        {
            if (!File.Exists(Cmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Poker Discard Anim Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[poker-discard-anim] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) { pass++; Line("PASS  " + name + "  " + detail); }
                else { fail++; Line("FAIL  " + name + "  " + detail); }
            }

            Line("poker discard anim tests start");

            Check("pre-pop-scale-1",
                Mathf.Approximately(PokerCardAnim.DiscardScale(0f), 1f)
                && Mathf.Approximately(PokerCardAnim.DiscardScale(PokerCardAnim.PopStart), 1f),
                "scale@0=" + PokerCardAnim.DiscardScale(0f)
                + " scale@start=" + PokerCardAnim.DiscardScale(PokerCardAnim.PopStart));

            Check("end-scale-exactly-0",
                PokerCardAnim.DiscardScale(1f) == 0f
                && PokerCardAnim.DiscardScale(1.5f) == 0f,
                "scale@1=" + PokerCardAnim.DiscardScale(1f)
                + " scale@1.5=" + PokerCardAnim.DiscardScale(1.5f));

            Check("end-not-visible",
                !PokerCardAnim.DiscardVisible(1f),
                "visible@1=" + PokerCardAnim.DiscardVisible(1f));

            Check("mid-visible",
                PokerCardAnim.DiscardVisible(PokerCardAnim.PopStart + PokerCardAnim.PopSpan * 0.5f),
                "visible@mid");

            // Monotone: never grows as tossU advances through the pop window.
            bool mono = true;
            float prev = PokerCardAnim.DiscardScale(PokerCardAnim.PopStart);
            for (int i = 1; i <= 40; i++)
            {
                float u = PokerCardAnim.PopStart + PokerCardAnim.PopSpan * (i / 40f);
                float s = PokerCardAnim.DiscardScale(u);
                if (s > prev + 1e-5f) { mono = false; break; }
                prev = s;
            }
            Check("scale-monotone-down", mono, "prev-end=" + prev);

            // No leftover floor: old bug was Lerp(1, 0.08) + Max(0.02, sc).
            Check("no-floor-at-end",
                PokerCardAnim.DiscardScale(1f) < 0.001f,
                "scale@1=" + PokerCardAnim.DiscardScale(1f));

            // Face flips off before the pop finishes (same 0.88 threshold as before).
            // FaceUntil is on the smoothed pop amount; walk tossU to find the edge.
            float uFaceOn = -1f, uFaceOff = -1f;
            for (int i = 0; i <= 200; i++)
            {
                float u = i / 200f;
                if (PokerCardAnim.DiscardShowFace(u)) uFaceOn = u;
                else if (uFaceOff < 0f) uFaceOff = u;
            }
            Check("face-on-then-off",
                uFaceOn >= 0f && uFaceOff > uFaceOn && uFaceOff < 1f,
                "lastOn=" + uFaceOn + " firstOff=" + uFaceOff);

            Check("linear-matches-window",
                Mathf.Approximately(PokerCardAnim.DiscardPopLinear(PokerCardAnim.PopStart), 0f)
                && Mathf.Approximately(PokerCardAnim.DiscardPopLinear(PokerCardAnim.PopStart + PokerCardAnim.PopSpan), 1f),
                "lin@start=" + PokerCardAnim.DiscardPopLinear(PokerCardAnim.PopStart)
                + " lin@end=" + PokerCardAnim.DiscardPopLinear(1f));

            Line("RESULT pass=" + pass + " fail=" + fail);
            if (fail > 0)
                Debug.LogError("[poker-discard-anim] " + fail + " failed");
        }
    }
}
#endif

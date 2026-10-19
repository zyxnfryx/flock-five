#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Brandon: the poker lesson's 'Tap the back button to return home.' caption must appear only after the
    // deal/draw has fully completed and the result is shown, never mid-draw.
    public static class PokerBackCaptionTimingTests
    {
        const string Out = "/tmp/flock-five-poker-back-caption-timing-tests.txt";

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
                Debug.Log("[pokerbackcap] " + line);
            }
            // drawn, motionBusy, winFlying, tutorBlocked
            Check("ready once the result is settled", FlockFiveApp.PokerBackCueReady(true, false, false, false), "");
            Check("not mid-draw (engine already Drawn, cards still animating)", !FlockFiveApp.PokerBackCueReady(true, true, false, false), "");
            Check("not while WIN is flying", !FlockFiveApp.PokerBackCueReady(true, false, true, false), "");
            Check("not while stamp / pay table / win motion block the tutor", !FlockFiveApp.PokerBackCueReady(true, false, false, true), "");
            Check("not before the hand is drawn (Idle / Dealt)", !FlockFiveApp.PokerBackCueReady(false, false, false, false), "");
            // The cue draw path must route through the gate (guards against a regression back to phase-only).
            string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            int i = src.IndexOf("void DrawPokerBackCue(");
            string body = i < 0 ? "" : src.Substring(i, System.Math.Min(700, src.Length - i));
            Check("DrawPokerBackCue is gated by PokerBackCueReady before the arrow and caption",
                body.IndexOf("PokerBackCueReady(") > 0
                && body.IndexOf("PokerBackCueReady(") < body.IndexOf("CoachArrowAt(")
                && body.IndexOf("PokerBackCueReady(") < body.IndexOf("DrawPokerStandardLine("), "");
            // Back stays dead until the gate (lesson forces the one hand) and the global tutorial gate is unchanged.
            Check("lesson holds Back until PokerBackGateLive", FlockFiveApp.PokerLessonHolds(true, true, false, false, false, true)
                && !FlockFiveApp.PokerLessonHolds(true, true, false, true, false, true), "");
            string end = fail == 0 ? "POKERBACKCAP_OK " + pass : "POKERBACKCAP_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif

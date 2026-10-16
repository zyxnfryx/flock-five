#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b115: the ad offer X must pass the rect-armed glove gate (GateGlove), not only the hand-armed one.
    public static class Build115Tests
    {
        const string Out = "/tmp/flock-five-b115-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build115] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            Check("glove gate lets the gift X through", FlockFiveApp.GateExemptsGiftClose(9, true), "");
            Check("glove gate still eats other off-target taps", !FlockFiveApp.GateExemptsGiftClose(9, false), "");
            Check("other gates unchanged by the exemption", !FlockFiveApp.GateExemptsGiftClose(1, true) && !FlockFiveApp.GateExemptsGiftClose(0, true), "");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            int kind = coach.IndexOf("var kind = t == EventType.MouseDown", System.StringComparison.Ordinal);
            int ex = coach.IndexOf("GateExemptsGiftClose(gate.Step, GiftCloseExempt(e.mousePosition))", kind, System.StringComparison.Ordinal);
            int any = coach.IndexOf("if (gate.AnyTap)", kind, System.StringComparison.Ordinal);
            Check("exemption sits before the rect-gate eats the press", kind > 0 && ex > kind && ex < any, "");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Interrupted-tutorial checks: every state a half-finished step can leave behind
    // must plan a fix, and every healthy state must plan none.
    // Run from the menu, or drop a file on /tmp/flock-five-heal-tests.
    [InitializeOnLoad]
    static class TutorialHealTests
    {
        const string Cmd = "/tmp/flock-five-heal-tests";
        const string Out = "/tmp/flock-five-heal-tests.txt";

        static TutorialHealTests()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(Cmd) || EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Tutorial Heal Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Check(string name, TutorSnapshot snap, TutorFix want)
            {
                var got = TutorialHeal.Plan(snap);
                bool ok = got == want;
                if (ok) pass++; else fail++;
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  want " + want + "  got " + got;
                sb.AppendLine(line);
                Debug.Log("[tutorial-heal] " + line);
            }

            Check("idle garden", new TutorSnapshot(), TutorFix.None);
            Check("idle splash", new TutorSnapshot { Splash = true }, TutorFix.None);
            Check("watch lesson on its card", new TutorSnapshot { AdHand = true, GiftCard = true }, TutorFix.None);
            Check("watch lesson, card closed", new TutorSnapshot { AdHand = true }, TutorFix.DropAdHand);
            Check("watch lesson left on the splash", new TutorSnapshot { AdHand = true, GiftCard = true, Splash = true }, TutorFix.DropAdHand);
            Check("ad stage with its coroutine", new TutorSnapshot { GiftMovie = true, WatchRunning = true, PauseHeld = true, AdShowing = true }, TutorFix.None);
            Check("ad stage, coroutine gone", new TutorSnapshot { GiftMovie = true }, TutorFix.ClearWatchStage);
            Check("sparrow lesson holds its pause", new TutorSnapshot { TutorPause = true, PauseHeld = true, SparrowCueLive = true }, TutorFix.None);
            Check("sparrow gone, pause kept", new TutorSnapshot { TutorPause = true, PauseHeld = true }, TutorFix.ReleaseTutorPause | TutorFix.ResetLeakedPause);
            Check("pause with no owner", new TutorSnapshot { PauseHeld = true }, TutorFix.ResetLeakedPause);
            Check("pause owned by an ad", new TutorSnapshot { PauseHeld = true, AdShowing = true }, TutorFix.None);
            Check("home lesson on the splash", new TutorSnapshot { Splash = true, HomeLessonLive = true }, TutorFix.None);
            Check("home lesson in a garden", new TutorSnapshot { HomeLessonLive = true }, TutorFix.DropHomeLessons);
            Check("normal gates", new TutorSnapshot { ResumeGateLeft = 0.35f, TapGateLeft = 0.45f, GiftGateLeft = 1f }, TutorFix.None);
            Check("frozen resume gate", new TutorSnapshot { ResumeGateLeft = 40f }, TutorFix.ClampGates);
            Check("frozen gift lockout", new TutorSnapshot { GiftGateLeft = 9f }, TutorFix.ClampGates);
            Check("gated step, glove posing", new TutorSnapshot { Splash = true, StepGateBlind = 0f }, TutorFix.None);
            Check("gated step, glove settling", new TutorSnapshot { Splash = true, StepGateBlind = 0.6f }, TutorFix.None);
            Check("gated step, no glove", new TutorSnapshot { Splash = true, StepGateBlind = 6f }, TutorFix.FinishGatedStep);

            // Step tap gate rule (poker back step, album page step).
            void Gate(string name, GatePointer kind, bool inTarget, bool inSwipe, bool swipe, bool want)
            {
                bool got = TutorialHeal.GateLets(kind, inTarget, inSwipe, swipe);
                bool ok = got == want;
                if (ok) pass++; else fail++;
                string line = (ok ? "PASS  " : "FAIL  ") + "gate: " + name + "  want " + want + "  got " + got;
                sb.AppendLine(line);
                Debug.Log("[tutorial-heal] " + line);
            }
            Gate("press on the glove's target", GatePointer.Down, true, false, false, true);
            Gate("release on the glove's target", GatePointer.Up, true, false, false, true);
            Gate("press on DEAL", GatePointer.Down, false, false, false, false);
            Gate("release on DEAL", GatePointer.Up, false, false, false, false);
            Gate("press on bet +", GatePointer.Down, false, false, false, false);
            Gate("drag passes", GatePointer.Drag, false, false, false, true);
            Gate("press may start a page swipe", GatePointer.Down, false, true, false, true);
            Gate("release that ends a swipe", GatePointer.Up, false, true, true, true);
            Gate("tap on an album sleeve", GatePointer.Up, false, true, false, false);

            sb.AppendLine(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[tutorial-heal] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail));
        }
    }
}
#endif

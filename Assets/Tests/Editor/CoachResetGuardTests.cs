#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Reset vs tutorial: the parade cuts a live step, holds it dark, and on landing
    // either resumes it fresh or re-arms a start that arrived mid-flight.
    // Run from the menu, or drop a file on /tmp/flock-five-reset-guard.
    [InitializeOnLoad]
    static class CoachResetGuardTests
    {
        const string Cmd = "/tmp/flock-five-reset-guard";
        const string Out = "/tmp/flock-five-reset-guard.txt";

        static CoachResetGuardTests()
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

        [MenuItem("Flock Five/Coach Reset Guard Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Check(string name, CoachResetVerb got, CoachResetVerb want)
            {
                bool ok = got == want;
                if (ok) pass++; else fail++;
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  want " + want + "  got " + got;
                sb.AppendLine(line);
                Debug.Log("[reset-guard] " + line);
            }

            var guard = new CoachResetGuard();
            Check("idle", guard.Tick(false, false, false, false, false, CoachFadePhase.None), CoachResetVerb.None);

            var cut = guard.Tick(true, true, false, true, false, CoachFadePhase.Held);
            Check("reset while caption is up", cut, CoachResetVerb.Cut | CoachResetVerb.Hold);
            Check("still flying, no resume", guard.Tick(false, true, false, false, false, CoachFadePhase.None), CoachResetVerb.Cut | CoachResetVerb.Hold);
            var landed = guard.Tick(false, false, true, true, false, CoachFadePhase.In);
            Check("land resumes fresh", landed, CoachResetVerb.Hold | CoachResetVerb.Resume);
            Check("frame after land is open", guard.Tick(false, false, false, true, false, CoachFadePhase.In), CoachResetVerb.None);

            guard = new CoachResetGuard();
            Check("reset during fade-in", guard.Tick(true, true, false, true, false, CoachFadePhase.In), CoachResetVerb.Cut | CoachResetVerb.Hold);
            Check("fade-in lands as a fresh resume", guard.Tick(false, false, true, false, false, CoachFadePhase.None), CoachResetVerb.Hold | CoachResetVerb.Resume);

            guard = new CoachResetGuard();
            Check("reset during fade-out", guard.Tick(true, true, false, true, false, CoachFadePhase.Out), CoachResetVerb.Cut | CoachResetVerb.Hold);
            Check("fade-out does not continue", guard.Tick(false, true, false, false, false, CoachFadePhase.Out), CoachResetVerb.Cut | CoachResetVerb.Hold);
            Check("fade-out lands as a fresh resume", guard.Tick(false, false, true, false, false, CoachFadePhase.None), CoachResetVerb.Hold | CoachResetVerb.Resume);

            guard = new CoachResetGuard();
            Check("start during the parade waits", guard.Tick(true, true, false, false, true, CoachFadePhase.None), CoachResetVerb.Hold | CoachResetVerb.Wait);
            Check("still waiting", guard.Tick(false, true, false, false, true, CoachFadePhase.None), CoachResetVerb.Hold | CoachResetVerb.Wait);
            Check("land re-arms the waiting start", guard.Tick(false, false, true, false, true, CoachFadePhase.None), CoachResetVerb.Hold | CoachResetVerb.Rearm);
            Check("re-arm does not repeat", guard.Tick(false, false, false, false, true, CoachFadePhase.None), CoachResetVerb.None);

            guard = new CoachResetGuard();
            guard.Tick(true, true, false, true, true, CoachFadePhase.Held);
            Check("a live step wins over a second start", guard.Tick(false, false, true, true, true, CoachFadePhase.Held), CoachResetVerb.Hold | CoachResetVerb.Resume);

            guard = new CoachResetGuard();
            guard.Tick(true, true, false, true, false, CoachFadePhase.Held);
            Check("double reset stays cut", guard.Tick(true, true, false, false, false, CoachFadePhase.None), CoachResetVerb.Cut | CoachResetVerb.Hold);
            Check("third reset still no resume", guard.Tick(true, true, false, false, false, CoachFadePhase.Out), CoachResetVerb.Cut | CoachResetVerb.Hold);
            Check("one resume after the last parade", guard.Tick(false, false, true, false, false, CoachFadePhase.None), CoachResetVerb.Hold | CoachResetVerb.Resume);

            guard = new CoachResetGuard();
            Check("land with no parade", guard.Tick(false, false, true, true, false, CoachFadePhase.Held), CoachResetVerb.None);

            guard = new CoachResetGuard();
            guard.Tick(true, true, false, true, false, CoachFadePhase.In);
            Check("reset reported together with land stays flying", guard.Tick(true, true, true, true, false, CoachFadePhase.Out), CoachResetVerb.Cut | CoachResetVerb.Hold);
            Check("that parade still resumes once", guard.Tick(false, false, true, false, false, CoachFadePhase.None), CoachResetVerb.Hold | CoachResetVerb.Resume);

            guard = new CoachResetGuard();
            guard.Tick(true, true, false, true, false, CoachFadePhase.Held);
            Check("dropped parade flag still resumes once", guard.Tick(false, false, false, true, false, CoachFadePhase.Out), CoachResetVerb.Hold | CoachResetVerb.Resume);

            guard = new CoachResetGuard();
            Check("empty reset holds, does not resume", guard.Tick(true, true, false, false, false, CoachFadePhase.None), CoachResetVerb.Hold);
            Check("empty reset lands quiet", guard.Tick(false, false, true, false, false, CoachFadePhase.None), CoachResetVerb.Hold);

            Check("hold blocks draw", CoachResetGuard.BlocksDraw(CoachResetVerb.Hold) ? CoachResetVerb.Hold : CoachResetVerb.None, CoachResetVerb.Hold);
            Check("resume frame still blocks draw", CoachResetGuard.BlocksDraw(CoachResetVerb.Hold | CoachResetVerb.Resume) ? CoachResetVerb.Hold : CoachResetVerb.None, CoachResetVerb.Hold);
            Check("idle does not block draw", CoachResetGuard.BlocksDraw(CoachResetVerb.None) ? CoachResetVerb.Hold : CoachResetVerb.None, CoachResetVerb.None);
            Check("wait blocks a start", CoachResetGuard.BlocksStart(CoachResetVerb.Hold | CoachResetVerb.Wait) ? CoachResetVerb.Wait : CoachResetVerb.None, CoachResetVerb.Wait);
            Check("resume does not block the next start", CoachResetGuard.BlocksStart(CoachResetVerb.Hold | CoachResetVerb.Resume) ? CoachResetVerb.Wait : CoachResetVerb.None, CoachResetVerb.None);

            sb.AppendLine(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[reset-guard] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail));
        }
    }
}
#endif

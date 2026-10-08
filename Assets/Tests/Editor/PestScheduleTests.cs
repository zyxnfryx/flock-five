#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Solved gardens do not gain a sparrow or a hawk. An arrive timer that
    // elapses after the solve (or after the resolve has started) is ignored.
    // Run from the menu, or drop a file on /tmp/flock-five-pest-tests.
    [InitializeOnLoad]
    static class PestScheduleTests
    {
        const string Cmd = "/tmp/flock-five-pest-tests";
        const string Out = "/tmp/flock-five-pest-tests.txt";

        static PestScheduleTests()
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

        [MenuItem("Flock Five/Pest Schedule Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[pest] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) { pass++; Line("PASS  " + name + "  " + detail); }
                else { fail++; Line("FAIL  " + name + "  " + detail); }
            }

            var solved = SolvedBoard();
            var open = OpenBoard();
            Check("fixture-solved", solved.Solved && PestSchedule.IsBoardSolved(solved, false),
                "sets " + solved.RemainingBirds);
            Check("fixture-open", !open.Solved && !PestSchedule.IsBoardSolved(open, false),
                "mixed limb");
            Check("resolve-counts", PestSchedule.IsBoardSolved(open, true), "resolve begun");

            Check("solved-no-sparrow", !PestSchedule.MaySpawn(PestSchedule.PestKind.Sparrow, solved, false),
                "sparrow");
            Check("solved-no-hawk", !PestSchedule.MaySpawn(PestSchedule.PestKind.Hawk, solved, false),
                "hawk");
            Check("resolve-no-sparrow", !PestSchedule.MaySpawn(PestSchedule.PestKind.Sparrow, open, true),
                "sparrow after resolve");
            Check("resolve-no-hawk", !PestSchedule.MaySpawn(PestSchedule.PestKind.Hawk, open, true),
                "hawk after resolve");

            Check("timer-after-solve-sparrow",
                !PestSchedule.TimerOpens(PestSchedule.PestKind.Sparrow, true, solved, false),
                "sparrow timer ignored");
            Check("timer-after-solve-hawk",
                !PestSchedule.TimerOpens(PestSchedule.PestKind.Hawk, true, solved, false),
                "hawk timer ignored");
            Check("timer-after-resolve",
                !PestSchedule.TimerOpens(PestSchedule.PestKind.Sparrow, true, open, true)
                && !PestSchedule.TimerOpens(PestSchedule.PestKind.Hawk, true, open, true),
                "both kinds");
            Check("timer-quiet-before-fire",
                !PestSchedule.TimerOpens(PestSchedule.PestKind.Sparrow, false, open, false)
                && !PestSchedule.TimerOpens(PestSchedule.PestKind.Hawk, false, open, false),
                "not yet");
            Check("timer-open-board",
                PestSchedule.TimerOpens(PestSchedule.PestKind.Sparrow, true, open, false)
                && PestSchedule.TimerOpens(PestSchedule.PestKind.Hawk, true, open, false),
                "unsolved still arrives");

            PestSchedule.BeginStage(20);
            PestSchedule.Watch(solved, () => false);
            bool sparrowSlot = PestSchedule.TryReserve();
            bool hawkSlot = PestSchedule.TryReserve();
            Check("spawner-solved", !sparrowSlot && !hawkSlot && PestSchedule.SolvedNow,
                "sparrow " + sparrowSlot + " hawk " + hawkSlot);

            PestSchedule.BeginStage(20);
            PestSchedule.Watch(open, () => true);
            bool lateSparrow = PestSchedule.TryReserve();
            bool lateHawk = PestSchedule.TryReserve();
            Check("spawner-resolve", !lateSparrow && !lateHawk, "timer path after resolve");

            PestSchedule.BeginStage(20);
            PestSchedule.Watch(open, () => false);
            bool openSlot = PestSchedule.TryReserve();
            Check("spawner-open", openSlot, "unsolved still reserves");
            if (openSlot) PestSchedule.NoteGone();

            PestSchedule.BeginStage(1);
            Line(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
        }

        static Board SolvedBoard()
        {
            var b = new Board();
            b.Branches.Add(Set(BirdColor.Ruby, BirdSex.Female));
            b.Branches.Add(Set(BirdColor.Gold, BirdSex.Male));
            b.Branches.Add(new BranchState());
            return b;
        }

        static Board OpenBoard()
        {
            var b = new Board();
            var mix = new BranchState();
            mix.Birds.Add(new Bird(BirdColor.Ruby, BirdSex.Female));
            mix.Birds.Add(new Bird(BirdColor.Gold, BirdSex.Male));
            b.Branches.Add(mix);
            return b;
        }

        static BranchState Set(BirdColor color, BirdSex sex)
        {
            var limb = new BranchState();
            for (int i = 0; i < BranchState.Cap; i++)
                limb.Birds.Add(new Bird(color, sex));
            return limb;
        }
    }
}
#endif

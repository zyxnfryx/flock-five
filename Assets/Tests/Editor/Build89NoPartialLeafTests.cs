#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89: a leaf lock covers a whole limb or none of it. Every garden on the ladder
    // (and Garden 38, authored ahead of it) is checked through the shared placement helper
    // LeafCover.SeatsToCover, the same call BranchView uses. Gardens 1-15 must stay solvable.
    // Run from the menu, with -executeMethod FlockFive.Editor.Build89NoPartialLeafTests.RunBatch,
    // or drop a file on /tmp/flock-five-b89-nopartial.
    [InitializeOnLoad]
    static class Build89NoPartialLeafTests
    {
        const string Cmd = "/tmp/flock-five-b89-nopartial";
        const string Out = "/tmp/flock-five-b89-nopartial.txt";

        static Build89NoPartialLeafTests()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(Cmd) || EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); } catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Build 89 No Partial Leaf Tests")]
        public static int Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                Debug.Log("[build89] " + line);
            }

            int lockedLimbs = 0, shortLocked = 0;
            string affected = "";
            for (int i = 0; i <= LevelData.Count; i++)
            {
                var level = i < LevelData.Count ? LevelData.Peek(i) : LevelData.Garden38Level;
                var board = i < LevelData.Count ? LevelData.Open(i) : LevelData.Open38Test();
                string bad = "";
                for (int k = 0; k < board.Branches.Count; k++)
                {
                    var br = board.Branches[k];
                    int covered = LeafCover.SeatsToCover(br);
                    if (br.TipLocked)
                    {
                        lockedLimbs++;
                        if (br.Count < BranchState.Cap) { shortLocked++; affected += " " + level.Number + ":limb" + k + "(" + br.Count + "/" + BranchState.Cap + ")"; }
                    }
                    if (LeafCover.IsPartial(br, covered))
                        bad += " limb" + k + "=" + covered + "/" + BranchState.Cap;
                    if (br.TipLocked != (covered == BranchState.Cap))
                        bad += " limb" + k + "-lock-mismatch";
                }
                Check("no-partial-leaf-" + level.Id, bad.Length == 0, bad.Length == 0 ? "ok" : bad);
            }
            Check("helper-partial-detector", LeafCover.IsPartial(new BranchState(), 3)
                && !LeafCover.IsPartial(new BranchState(), 0)
                && !LeafCover.IsPartial(new BranchState(), BranchState.Cap), "detector flags 3/5 only");
            sb.AppendLine("info lockedLimbs=" + lockedLimbs + " lockedWithEmptySeats=" + shortLocked + " (were partial before b89):" + affected);

            for (int i = 0; i < 15; i++)
            {
                var rep = GardenSolve.Search(LevelData.Open(i), 30000);
                Check("garden-" + (i + 1) + "-solvable", rep.Outlook == GardenSolve.Outlook.Winnable,
                    rep.Outlook + " moves=" + rep.Moves + " nodes=" + rep.Nodes);
            }

            string end = fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build89] " + end);
            return fail;
        }

        public static void RunBatch() => EditorApplication.Exit(Run() == 0 ? 0 : 1);
    }
}
#endif

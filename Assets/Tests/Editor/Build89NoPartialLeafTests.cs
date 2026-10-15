#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89 (QA rule A): only a full 5-bird limb may be leaf-locked. Checked on every ladder
    // garden and Garden 38 through LevelData's shared Pack rule. Gardens 1-15 stay solvable.
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

            // RULE A: leaf-locked limbs always hold BranchState.Cap (5) birds, on every ladder level
            // and Garden 38. A limb with fewer birds shows NO leaves (TipLocked false).
            int lockedLimbs = 0, dropped = 0;
            string droppedLog = "";
            for (int i = 0; i <= LevelData.Count; i++)
            {
                int idx = i;
                var level = idx < LevelData.Count ? LevelData.Peek(idx) : LevelData.Garden38Level;
                System.Func<Board> open = () => idx < LevelData.Count ? LevelData.Open(idx) : LevelData.Open38Test();

                LevelData.LeafRuleOn = false;
                var before = open();
                var beforeRep = GardenSolve.Search(before, 30000);
                LevelData.LeafRuleOn = true;
                var board = open();
                string drops = LevelData.LastLeafDrops;
                var rep = GardenSolve.Search(board, 30000);

                string bad = "";
                for (int k = 0; k < board.Branches.Count; k++)
                {
                    var br = board.Branches[k];
                    if (br.TipLocked) lockedLimbs++;
                    if (br.TipLocked && br.Count != BranchState.Cap) bad += " limb" + k + "=" + br.Count + "/" + BranchState.Cap;
                }
                if (drops.Length > 0)
                {
                    dropped += drops.Split(' ').Length;
                    droppedLog += " [G" + level.Number + ": " + drops + "]";
                }
                Check("leaf-locked-limbs-always-5-birds-" + level.Id,
                    bad.Length == 0 && LevelData.LeafLockedLimbsAreFull(board), bad.Length == 0 ? "ok" : bad);

                bool wasWin = beforeRep.Outlook == GardenSolve.Outlook.Winnable;
                bool mustWin = idx < 15 || wasWin;
                bool alreadyDone = rep.Moves == 0 && rep.Outlook == GardenSolve.Outlook.Winnable;
                Check("still-winnable-not-instant-" + level.Id,
                    (!mustWin || rep.Outlook == GardenSolve.Outlook.Winnable) && !alreadyDone,
                    "before=" + beforeRep.Outlook + "/" + beforeRep.Moves + " after=" + rep.Outlook + "/" + rep.Moves);
            }
            LevelData.LeafRuleOn = true;
            Check("helper-detector", !LevelData.LeafLockedLimbsAreFull(ShortLocked()) && LevelData.LeafLockedLimbsAreFull(new Board()),
                "detector flags a 3-bird locked limb");
            sb.AppendLine("info lockedLimbsAfter=" + lockedLimbs + " locksDropped=" + dropped + droppedLog);

            string end = fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build89] " + end);
            return fail;
        }

        static Board ShortLocked()
        {
            var b = new Board();
            var br = new BranchState();
            for (int i = 0; i < 3; i++) { br.Birds.Add(new Bird(BirdColor.Ruby, BirdKit.Bow)); br.Shrouded.Add(false); }
            br.Shrouded[2] = true;
            b.Branches.Add(br);
            return b;
        }

        public static void RunBatch() => EditorApplication.Exit(Run() == 0 ? 0 : 1);
    }
}
#endif

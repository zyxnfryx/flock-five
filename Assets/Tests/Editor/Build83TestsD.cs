#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 83 step 3: gardens 27-32 and 38 from ostrich-impl.txt, plus a fresh-start
    // playthrough: every garden 1-32 opened as a new player meets it is winnable.
    static class Build83TestsD
    {
        const int Cap = 60000;

        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log("[build83d] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }
            Check("ladder-32", LevelData.Count == 50, "count " + LevelData.Count);
            foreach (int n in new[] { 27 })
            {
                var lv = LevelData.Peek(n - 1);
                var b = LevelData.Open(n - 1);
                Check("ostrich-" + n, lv.Number == n && lv.Ostrich && lv.Feeders == 3 && b.Live.Length == 3 && b.Ostrich != null
                    && BoardValidator.CheckKitSets(b).Ok, "live " + b.Live.Length + " peck " + lv.PeckSeed);
            }
            // b110: 32 is the first spring garden and no longer an ostrich stage.
            var l32 = LevelData.Peek(31);
            Check("garden-32-spring-no-ostrich", l32.Number == 32 && !l32.Ostrich && l32.Feeders == 3, "");
            var b31 = LevelData.Open(30);
            Check("garden-31-badger-spring", LevelData.Peek(30).Number == 31 && !LevelData.Peek(30).Ostrich && b31.Live.Length == 2
                && BoardValidator.CheckKitSets(b31).Ok, "");
            var b38 = LevelData.ParseAuthored(LevelData.Garden38Spec);
            LevelData.ApplyAuthored(b38, LevelData.Garden38Level);
            var r38 = GardenSolve.Search(b38, Cap);
            Check("garden-38-authored", b38.Ostrich != null && !b38.BreezeOnCollect && b38.Live.Length == 3
                && r38.Outlook == GardenSolve.Outlook.Winnable, r38.Outlook + " nodes " + r38.Nodes);

            // Ostrich gardens carry no obstacles: no shrouded birds (inner bees / leaf locks), no breeze.
            foreach (int oi in new[] { 26, 37, 42 })
            {
                var ob = LevelData.Open(oi);
                int shroud = 0;
                foreach (var br in ob.Branches) for (int j = 0; j < br.Count; j++) if (br.IsShrouded(j)) shroud++;
                Check("ostrich-no-obstacles-" + LevelData.Peek(oi).Number,
                    LevelData.Peek(oi).Ostrich && shroud == 0 && !ob.BreezeOnCollect, "shrouded " + shroud);
            }

            // Fresh start: clear prefs-free ladder walk, 1 -> 32 in order.
            int reached = 0;
            string stuck = "";
            for (int i = 0; i < LevelData.Count; i++)
            {
                var b = LevelData.Open(i);
                bool ok = b.Won || GardenSolve.Search(b, Cap).Outlook == GardenSolve.Outlook.Winnable;
                if (!ok) { stuck = "garden " + (i + 1); break; }
                reached = i + 1;
            }
            Check("playthrough-1-50", reached == 50, "reached " + reached + " " + stuck);
            LevelData.Open(0);
            Debug.Log(fail == 0 ? "build83d tests DONE " + pass + "/" + (pass + fail) : "build83d tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif

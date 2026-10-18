#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 110. (1) Never a sparrow or a hawk on an ostrich garden, by any path.
    // (2) Garden 32 (spring starts) is no longer an ostrich garden; next one is 38.
    // (3) The ostrich body and neck sit behind the branch wood and the flock.
    // (4) Spring has real ambient motion: petals, blossoms, butterflies, boosted sway.
    public static class Build110SpringOstrichTests
    {
        const string Out = "/tmp/flock-five-b110-tests.txt";

        [MenuItem("Flock Five/Build 110 Spring + Ostrich Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build110] " + line);
            }

            try
            {
                // --- ostrich levels: pests are shut out completely ---
                int ostrichLevels = 0;
                string firstBad = "";
                for (int i = 0; i < LevelData.Count; i++)
                {
                    var b = LevelData.Open(i);
                    var lv = LevelData.Peek(i);
                    PestSchedule.BeginStage(LevelData.DisplayNumber);
                    PestSchedule.Watch(b, () => false);
                    if (!lv.Ostrich) continue;
                    ostrichLevels++;
                    bool shut = LevelData.SparrowVisits == 0 && LevelData.HawkVisits == 0
                        && PestSchedule.OstrichGarden && PestSchedule.StageFull
                        && !PestSchedule.TryReserve()
                        && !PestSchedule.MaySpawn(PestSchedule.PestKind.Sparrow, b, false)
                        && !PestSchedule.MaySpawn(PestSchedule.PestKind.Hawk, b, false)
                        && !PestSchedule.TimerOpens(PestSchedule.PestKind.Sparrow, true)
                        && !PestSchedule.TimerOpens(PestSchedule.PestKind.Hawk, true)
                        && !SparrowView.Patrol(null, null, 3, null, null).MoveNext()
                        && !HawkView.Patrol(null, null, 3, null, null).MoveNext()
                        && !SparrowView.Visit(null, null).MoveNext()
                        && !HawkView.Visit(null, null).MoveNext();
                    if (!shut && firstBad.Length == 0) firstBad = "garden " + lv.Number;
                }
                Check("ostrich-gardens-no-pests", ostrichLevels >= 1 && firstBad.Length == 0,
                    ostrichLevels + " ostrich gardens " + firstBad);

                // The gate is on the board too: a stage-authored ostrich on any level.
                LevelData.Open(19);
                PestSchedule.BeginStage(20);
                var withOstrich = new Board { Ostrich = new OstrichState() };
                PestSchedule.Watch(withOstrich, () => false);
                Check("board-ostrich-blocks",
                    PestSchedule.OstrichGarden
                    && !PestSchedule.MaySpawn(PestSchedule.PestKind.Sparrow, withOstrich, false)
                    && !PestSchedule.MaySpawn(PestSchedule.PestKind.Hawk, withOstrich, false)
                    && !PestSchedule.TryReserve(), "");
                // A fled ostrich keeps the garden closed.
                withOstrich.Ostrich.Hits = OstrichState.HitsToFlee;
                Check("fled-ostrich-still-blocks", PestSchedule.OstrichGarden && !PestSchedule.TryReserve(), "");

                // Control: a non-ostrich garden still gets its pests.
                LevelData.Open(19);
                PestSchedule.BeginStage(20);
                var plain = new Board();
                PestSchedule.Watch(plain, () => false);
                Check("plain-garden-keeps-pests",
                    !PestSchedule.OstrichGarden && LevelData.SparrowVisits > 0 && LevelData.HawkVisits > 0
                    && SparrowView.Patrol(null, null, 3, null, null).MoveNext(), "");

                // --- ostrich schedule ---
                var l32 = LevelData.Peek(31);
                Check("garden-32-no-ostrich", l32.Number == 32 && !l32.Ostrich && l32.Feeders == 3, "ostrich " + l32.Ostrich);
                Check("ostrich-ladder-27-only", LevelData.Peek(26).Ostrich && LevelData.Peek(37).Ostrich && CountOstrich() == 2, "count " + CountOstrich());
                Check("next-ostrich-38-past-32", LevelData.Garden38Level.Ostrich && LevelData.Garden38Level.Number == 38
                    && LevelData.Garden38Level.Number > GardenSeason.SpringFrom, "");
                Check("garden-38-not-badger-or-change",
                    !GardenSeason.IsBadgerLevel(38) && !GardenSeason.IsChangeLevel(38),
                    "scene " + GardenSeason.ForLevel(38));
                var b32 = LevelData.Open(31);
                Check("garden-32-board-plain", b32.Ostrich == null && b32.Live.Length == 3, "");

                // --- ostrich draw order ---
                var root = new GameObject("b110-stage");
                try
                {
                    var mark = OstrichMark.Attach(root.transform, 3);
                    var body = mark.transform.Find("Body").GetComponent<SpriteRenderer>();
                    var neck = mark.transform.Find("Neck0").GetComponent<SpriteRenderer>();
                    const int Wood = 2, Feeder = 8;
                    Check("ostrich-body-neck-behind-wood",
                        body.sortingOrder < Wood && neck.sortingOrder < Wood && OstrichMark.CollarOrder < Wood
                        && body.sortingOrder > -12, "body " + body.sortingOrder + " neck " + neck.sortingOrder);
                    Check("ostrich-head-under-flock",
                        mark.Head.sortingOrder > Feeder && mark.Head.sortingOrder < FlockSort.Perch, "head " + mark.Head.sortingOrder);
                    Check("ostrich-body-large", OstrichMark.BodyScale >= 1.5f && OstrichMark.BodyScale / 0.72f >= 2f,
                        "body scale " + OstrichMark.BodyScale + " (was 0.72)");
                }
                finally { Object.DestroyImmediate(root); }

                // --- spring motion ---
                var low = SeasonMotion.Lively(GardenScene.Spring, true);
                var desk = SeasonMotion.Lively(GardenScene.Spring, false);
                Check("spring-has-petals-blossoms-butterflies",
                    low.Petal >= 5 && low.Blossom >= 3 && low.Butterfly >= 2 && low.Cloud >= 2
                    && desk.Petal > 7 && desk.Blossom >= 6 && desk.Butterfly >= 3, "low " + low.Quads + " desk " + desk.Quads);
                Check("spring-budget-cheap", low.Quads <= 40 && desk.Quads <= 64, "low " + low.Quads + " desk " + desk.Quads);
                Check("spring-sway-boosted", FoliageSway.SpringSwayBoost > 1.5f && FoliageSway.SpringGustFloor > 0.2f, "");
                Check("season-channels-still-in-band", SeasonMotion.InBand(SeasonMotion.For(GardenScene.Spring)), "");
            }
            finally
            {
                PestSchedule.Watch(null, null);
                LevelData.Open(0);
                PestSchedule.BeginStage(1);
            }
            Debug.Log(fail == 0 ? "build110 tests DONE " + pass + "/" + (pass + fail) : "build110 tests FAIL " + fail + " failed, " + pass + " passed");
        }

        static int CountOstrich()
        {
            int n = 0;
            for (int i = 0; i < LevelData.Count; i++) if (LevelData.Peek(i).Ostrich) n++;
            return n;
        }
    }
}
#endif

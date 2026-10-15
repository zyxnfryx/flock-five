#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 88. Garden restart clears every pest through PestReset: a perched
    // sparrow, a wounded hawk, and the ostrich fight (back to its opening).
    public static class Build88PestTests
    {
        const string Out = "/tmp/flock-five-b88-pest-tests.txt";

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
                Debug.Log("[build88pest] " + line);
            }

            GameObject root = null;
            try
            {
                SparrowView.DismissForReset();
                HawkView.DismissForReset();
                PestSchedule.BeginStage(14);

                var board = new Board();
                board.Ostrich = new OstrichState
                {
                    Hits = 3,
                    PeckIndex = 4,
                    BlockSlot = 1,
                    Bonked = 9,
                    PeckSeed = 42,
                    PecksPerMove = 2
                };
                board.Displaced.Add(default);

                root = new GameObject("pest-reset");
                var mark = OstrichMark.Attach(root.transform, 3);
                mark.Sync(board);
                Check("ostrich-fled-before-reset",
                    !board.Ostrich.Alive && mark.Act == OstrichAct.Daze,
                    "hits " + board.Ostrich.Hits + " act " + mark.Act);

                bool reserved = PestSchedule.TryReserve();
                var sparrow = SparrowView.Seat(1);
                var hawk = HawkView.Seat(0, 1);
                Check("visitors-perched",
                    reserved && PestSchedule.Busy
                    && sparrow != null && sparrow.IsBlocking && sparrow.BlockingSlot == 1
                    && hawk != null && hawk.IsBlocking && hawk.HitsTaken == 1 && hawk.BlockingSlot == 0,
                    "busy " + PestSchedule.Busy + " hawkHits " + (hawk != null ? hawk.HitsTaken : -1));

                // A scrap already in the air, and a sparrow the collect has started to evict.
                sparrow.BeginEvict();
                hawk.BeginScrap();
                float ready = PestSchedule.SecondsUntilReady;
                PestReset.Apply(board);
                mark.Sync(board);

                bool sparrowGone = SparrowView.Live == null && sparrow == null;
                bool hawkGone = HawkView.Live == null && hawk == null;
                var o = board.Ostrich;
                Check("sparrow-gone", sparrowGone, SparrowView.Live == null ? "live null" : "live");
                Check("hawk-gone", hawkGone, HawkView.Live == null ? "live null" : "live");
                Check("ostrich-reset",
                    o != null && o.Alive && o.Hits == 0 && o.PeckIndex == 0 && o.Bonked == 0
                    && o.BlockSlot == -1 && o.PeckSeed == 42 && o.PecksPerMove == 2,
                    "hits " + o.Hits + " peck " + o.PeckIndex + " seed " + o.PeckSeed);
                Check("ostrich-back-on-stage",
                    mark.OnStage && mark.Act == OstrichAct.Strut,
                    "on " + mark.OnStage + " act " + mark.Act);
                Check("displaced-cleared", board.Displaced.Count == 0, "n " + board.Displaced.Count);
                Check("schedule-hold-dropped", !PestSchedule.Busy, "busy " + PestSchedule.Busy);
                Check("hold-not-rearmed",
                    Mathf.Abs(PestSchedule.SecondsUntilReady - ready) < 0.001f,
                    "ready " + PestSchedule.SecondsUntilReady.ToString("0.00")
                    + " was " + ready.ToString("0.00"));

                PestReset.Apply(board);
                Check("second-reset-stays-clear",
                    SparrowView.Live == null && HawkView.Live == null && !PestSchedule.Busy
                    && board.Ostrich.Hits == 0 && board.Ostrich.PeckSeed == 42,
                    "");
                PestReset.Apply(null);
                Check("null-board-safe", SparrowView.Live == null && HawkView.Live == null, "");

                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                int snap = app.IndexOf("IEnumerator SnapRound()");
                int next = app.IndexOf("void RefreshBonusSigns()", snap);
                string body = snap >= 0 && next > snap ? app.Substring(snap, next - snap) : "";
                int calls = 0;
                int at = 0;
                while (at >= 0)
                {
                    at = body.IndexOf("PestReset.Apply", at);
                    if (at < 0) break;
                    calls++;
                    at += 8;
                }
                Check("restart-one-shared-call",
                    calls == 1 && body.IndexOf("Ostrich.Reset") < 0
                    && body.IndexOf("FinishEvict") < 0 && body.IndexOf("EndScrap") < 0,
                    "calls " + calls);

                string reset = File.ReadAllText("Assets/_Project/Scripts/Puzzle/PestReset.cs");
                Check("reset-covers-sparrow-hawk-ostrich",
                    reset.IndexOf("board.Ostrich.Reset()") >= 0
                    && reset.IndexOf("SparrowView.DismissForReset()") >= 0
                    && reset.IndexOf("HawkView.DismissForReset()") >= 0
                    && reset.IndexOf("ClearFx()") >= 0
                    && reset.IndexOf("ReleaseHold()") >= 0,
                    "");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                SparrowView.DismissForReset();
                HawkView.DismissForReset();
                PestSchedule.BeginStage(1);
            }

            Debug.Log(fail == 0
                ? "build88pest tests DONE " + pass + "/" + (pass + fail)
                : "build88pest tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif

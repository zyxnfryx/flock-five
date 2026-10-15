#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b104: the stuck gift hint never arms on a solved board or during a pest / scatter / modal.
    public static class Build104StuckTests
    {
        const string Out = "/tmp/flock-five-b104-stuck-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build104stuck] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            var none = new StuckHintRule.Live();
            // Real stuck board: Garden 17 after its one hop (Build89 fixture).
            var dead = Build89DeadEndTests.Garden17Dead();
            var trail = new HopTrail();
            dead.TryMove(0, 13, out int run);
            trail.Note(0, 13, run, dead.JustUnveiled);
            Check("normal-stuck-still-arms", StuckHintRule.ShouldArm(dead, none, trail) && trail.Stuck(dead), "");

            var solved = new Board();
            Check("fixture-solved", SolvedRule.IsSolved(solved), "");
            Check("solved-stuck-reads-true-without-guard", new HopTrail().Stuck(solved), "the old trigger saw a solved board as stuck");
            Check("solved-never-arms", !StuckHintRule.ShouldArm(solved, none, new HopTrail()), "");
            var sparrow = new StuckHintRule.Live { PestOnStage = true };
            Check("solved+sparrow", !StuckHintRule.ShouldArm(solved, sparrow, new HopTrail()), "");
            Check("solved+hawk-resolving", !StuckHintRule.ShouldArm(solved, new StuckHintRule.Live { PestOnStage = true, PestResolving = true }, new HopTrail()), "");
            Check("stuck+sparrow-blocked", !StuckHintRule.ShouldArm(dead, sparrow, trail), "");
            Check("stuck+hawk-blocked", !StuckHintRule.ShouldArm(dead, new StuckHintRule.Live { PestOnStage = true }, trail), "");
            Check("stuck+pest-animation-blocked", !StuckHintRule.ShouldArm(dead, new StuckHintRule.Live { PestFinishing = true }, trail)
                && !StuckHintRule.ShouldArm(dead, new StuckHintRule.Live { PestResolving = true }, trail), "");
            Check("stuck+scatter-blocked", !StuckHintRule.ShouldArm(dead, new StuckHintRule.Live { Scatter = true }, trail), "");
            Check("stuck+modal-blocked", !StuckHintRule.ShouldArm(dead, new StuckHintRule.Live { Modal = true }, trail)
                && !StuckHintRule.ShouldArm(dead, new StuckHintRule.Live { Paused = true }, trail), "");
            Check("stuck+won-collecting-blocked", !StuckHintRule.ShouldArm(dead, new StuckHintRule.Live { Won = true }, trail)
                && !StuckHintRule.ShouldArm(dead, new StuckHintRule.Live { Collecting = true }, trail), "");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("nudge-and-display-use-rule", coach.Contains("if (!StuckHintAllowed()) { _deadEndNudged = false; return; }")
                && coach.Contains("!_coach && PlayClock.Now < _coachGiftUntil && !StuckHintAllowed()"), "");
        }
    }
}
#endif

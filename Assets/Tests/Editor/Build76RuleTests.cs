#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 76. Solved means every bird is in a complete set of five.
    // A dismissing lesson tap is eaten, then the board waits out the guard.
    static class Build76RuleTests
    {
        [MenuItem("Flock Five/Build 76 Rule Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build76-rules] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            CheckSolved(Check);
            CheckPestPark(Check);
            CheckDismiss(Check);
            PestSchedule.BeginStage(1);
            Debug.Log("[build76-rules] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }

        static void CheckSolved(System.Action<string, bool, string> Check)
        {
            var split = Pair(4, 1, BirdColor.Ruby);
            var stray = Pair(5, 1, BirdColor.Gold, BirdColor.Ruby);
            var shortOne = Pair(5, 4, BirdColor.Ruby, BirdColor.Teal);
            var full = Pair(5, 5, BirdColor.Ruby, BirdColor.Gold);
            var empty = new Board();
            var bare = new Board();
            bare.Branches.Add(new BranchState());
            var broken = new Board();
            broken.Branches.Add(new BranchState { Broken = true });

            Check("split-4-1", !SolvedRule.IsSolved(split) && !split.Solved, Counts(split));
            Check("five-plus-straggler", !SolvedRule.IsSolved(stray), Counts(stray));
            Check("complete-except-four", !SolvedRule.IsSolved(shortOne), Counts(shortOne));
            // Empty is solved: no bird sits outside a complete set, and a cleared
            // garden must not take a new pest. A null board is not solved.
            Check("empty-is-solved",
                SolvedRule.IsSolved(empty) && SolvedRule.IsSolved(bare) && SolvedRule.IsSolved(broken)
                && !SolvedRule.IsSolved(null) && SolvedRule.FinaleDue(empty),
                "no birds left");
            Check("true-solved", SolvedRule.IsSolved(full) && full.Solved && !SolvedRule.FinaleDue(full),
                "sets perched, show waits for the clear");

            PestSchedule.BeginStage(20);
            PestSchedule.Watch(split, () => false);
            bool near = PestSchedule.MaySpawn(PestSchedule.PestKind.Sparrow, split, false)
                && PestSchedule.MaySpawn(PestSchedule.PestKind.Hawk, stray, false)
                && !PestSchedule.IsBoardSolved(shortOne, false)
                && PestSchedule.TryReserve();
            if (near) PestSchedule.NoteGone();
            Check("near-still-schedules", near, "4+1 and a straggler stay open");

            PestSchedule.BeginStage(20);
            PestSchedule.Watch(full, () => false);
            Check("solved-closes-schedule",
                !PestSchedule.MaySpawn(PestSchedule.PestKind.Sparrow, full, false)
                && !PestSchedule.TryReserve(),
                "complete sets");
        }

        static void CheckPestPark(System.Action<string, bool, string> Check)
        {
            var b = Pair(1, 4, BirdColor.Ruby);
            b.Branches.Add(new BranchState());
            PestPark.Apply(b, 0, hawk: false);
            int fours = 0;
            int ones = 0;
            int fives = 0;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var st = b.Branches[i];
                if (st.Broken || st.Count == 0) continue;
                if (st.IsFullMatch(out _)) fives++;
                else if (st.Count == 4) fours++;
                else if (st.Count == 1) ones++;
            }
            bool parked = fives == 0 && fours == 1 && ones == 1
                && !SolvedRule.IsSolved(b)
                && !SolvedRule.FinaleDue(b)
                && !SolvedRule.WouldAutoClear(b);
            Check("pest-park-4-1", parked, "fives " + fives + " fours " + fours + " ones " + ones);

            int from = -1;
            int to = -1;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var st = b.Branches[i];
                if (st.Broken) continue;
                if (st.Count == 1) from = i;
                if (st.Count == 4) to = i;
            }
            bool hop = from >= 0 && to >= 0 && b.CanMove(from, to, out int run) && run == 1 && b.TryMove(from, to, out _);
            b.Live[0] = BirdColor.Ruby;
            int collect = hop ? b.FindCollect() : -1;
            bool cleared = hop && collect >= 0;
            if (cleared) b.ApplyCollect(collect);
            PestSchedule.BeginStage(20);
            PestSchedule.Watch(b, () => false);
            bool finale = cleared && SolvedRule.IsSolved(b) && SolvedRule.FinaleDue(b)
                && !PestSchedule.MaySpawn(PestSchedule.PestKind.Hawk, b, false)
                && !PestSchedule.TryReserve();
            Check("player-completes-five", finale, "hop " + hop + " collect " + collect + " birds " + b.RemainingBirds);
        }

        static void CheckDismiss(System.Action<string, bool, string> Check)
        {
            float now = 10f;
            var leaf = CoachDismiss.Resolve(new CoachDismiss.Input
            {
                LessonUp = true,
                Step = CoachStep.Leaf,
                OnTarget = false,
                OverBranch = true,
                OverGift = true,
                OverHud = true,
                Now = now,
            });
            Check("dismiss-tap-eaten",
                leaf.Dismissed && leaf.Consumed && !leaf.BranchPick && !leaf.GiftPopup && !leaf.HudButton
                && !leaf.DeliveredTarget,
                "gift under the leaf tap stays closed");

            float until = leaf.GuardUntil;
            var soon = CoachDismiss.Resolve(new CoachDismiss.Input
            {
                OverBranch = true,
                OverGift = true,
                OverHud = true,
                Now = now + 0.2f,
                GuardUntil = until,
            });
            Check("guard-holds",
                CoachDismiss.GuardBlocks(now + 0.2f, until)
                && !soon.BranchPick && !soon.GiftPopup && !soon.HudButton
                && Mathf.Abs(until - (now + CoachDismiss.GuardSeconds)) < 0.0001f,
                "until " + until);

            var later = CoachDismiss.Resolve(new CoachDismiss.Input
            {
                OverBranch = true,
                OverGift = true,
                OverHud = true,
                Now = now + CoachDismiss.GuardSeconds,
                GuardUntil = until,
            });
            Check("guard-lifts",
                !CoachDismiss.GuardBlocks(now + CoachDismiss.GuardSeconds, until)
                && later.BranchPick && later.GiftPopup && later.HudButton,
                "at " + CoachDismiss.GuardSeconds);

            var hop = CoachDismiss.Resolve(new CoachDismiss.Input
            {
                LessonUp = true,
                Step = CoachStep.CoachPlace,
                OnTarget = true,
                OverBranch = true,
                Now = now,
            });
            Check("required-target-delivers",
                hop.Dismissed && hop.DeliveredTarget && hop.BranchPick && !hop.Consumed && !hop.GiftPopup,
                "the guided branch still takes the tap");
        }

        static Board Pair(int a, int b, BirdColor color)
        {
            return Pair(a, b, color, color);
        }

        static Board Pair(int a, int b, BirdColor colorA, BirdColor colorB)
        {
            var board = new Board();
            board.Branches.Add(Flock(a, colorA));
            board.Branches.Add(Flock(b, colorB));
            return board;
        }

        static BranchState Flock(int n, BirdColor color)
        {
            var limb = new BranchState();
            var sex = BirdSex.Female;
            for (int i = 0; i < n; i++)
                limb.Birds.Add(new Bird(color, sex));
            return limb;
        }

        static string Counts(Board b)
        {
            return b.Branches[0].Count + "+" + b.Branches[1].Count;
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Blackbird visit. Move priority, cadence, rattled and flee thresholds,
    // hands-off limbs, and solver winnability of whatever garden Visit names. No scene.
    public static class BlackbirdFightTests
    {
        const string Out = "/tmp/flock-five-blackbird-tests.txt";

        [MenuItem("Flock Five/Blackbird Fight Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            CheckDelegate check = delegate(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[blackbird] " + line);
            };

            CheckPerch(check);
            CheckCadence(check);
            CheckGentle(check);
            CheckPriority(check);
            CheckHandsOff(check);
            CheckThresholds(check);
            CheckSchedule(check);
            CheckFlight(check);
            CheckBed(check);
            CheckFold(check);
            CheckSeeds(check);
            CheckConfiguredGarden(check);

            string tail = fail == 0 ? "BLACKBIRD OK " + pass : "BLACKBIRD FAIL " + fail + " / " + (pass + fail);
            sb.AppendLine(tail);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[blackbird] " + tail);
            if (fail > 0) throw new System.Exception(tail);
        }

        [MenuItem("Flock Five/Open Blackbird Garden")]
        public static void OpenBlackbirdGarden()
        {
            File.WriteAllText("/tmp/flock-five-blackbird", BlackbirdSchedule.Visit.First.ToString());
            if (EditorApplication.isPlaying)
            {
                var app = Object.FindAnyObjectByType<FlockFiveApp>();
                if (app != null) app.EditorOpenBlackbird();
                return;
            }
            EditorApplication.isPlaying = true;
        }

        delegate void CheckDelegate(string name, bool ok, string detail);

        static void CheckPerch(CheckDelegate check)
        {
            var b = Dressable();
            BlackbirdRival.Dress(b, 0);
            int spare = b.Blackbird.Spare;
            int perch = b.Blackbird.Perch;
            check("perch-cap", b.Branches[perch].SeatCap == 1 && b.Branches[perch].RivalPerch && b.Branches[perch].Empty,
                "cap " + b.Branches[perch].SeatCap);
            check("spare-empty", b.Branches[spare].IsSpare && b.Branches[spare].Empty && !b.Branches[spare].RivalPerch,
                "idx " + spare);
            check("five-colors", Colors(b) == 5, "mask " + b.Blackbird.ColorMask);
        }

        static void CheckCadence(CheckDelegate check)
        {
            var b = PullBoard();
            var a1 = BlackbirdRival.Commit(b);
            var a2 = BlackbirdRival.Commit(b);
            check("cadence-quiet", !a1.Moved && !a2.Moved && b.Blackbird.PlayerMoves == 2,
                "moves " + b.Blackbird.PlayerMoves);
            int before = b.Branches[0].Count;
            var a3 = BlackbirdRival.Commit(b);
            check("cadence-third", a3.Moved && a3.Why == "sorted" && a3.From == 0 && a3.To == 1
                && b.Branches[0].Count < before && b.Blackbird.PlayerMoves == 3,
                a3.Why + " " + a3.From + ">" + a3.To);
        }

        static void CheckGentle(CheckDelegate check)
        {
            var b = PullBoard();
            b.Blackbird.CalmEvery = BlackbirdSchedule.GentleCalm;
            b.Blackbird.RattleEvery = BlackbirdSchedule.GentleRattle;
            var a1 = BlackbirdRival.Commit(b);
            var a2 = BlackbirdRival.Commit(b);
            var a3 = BlackbirdRival.Commit(b);
            int before = b.Branches[0].Count;
            var a4 = BlackbirdRival.Commit(b);
            check("ramp-every-4", !a1.Moved && !a2.Moved && !a3.Moved && a4.Moved && b.Branches[0].Count < before,
                "m4 " + a4.Moved + " from " + a4.From);

            var rattled = PullBoard();
            rattled.Blackbird.CalmEvery = BlackbirdSchedule.GentleCalm;
            rattled.Blackbird.RattleEvery = BlackbirdSchedule.GentleRattle;
            rattled.Blackbird.ColorMask = 31;
            var r1 = BlackbirdRival.Commit(rattled);
            var r2 = BlackbirdRival.Commit(rattled);
            var r3 = BlackbirdRival.Commit(rattled);
            check("ramp-rattled-every-3", !r1.Moved && !r2.Moved && r3.Moved && !rattled.Blackbird.Fled,
                "m3 " + r3.Moved);
        }

        static void CheckPriority(CheckDelegate check)
        {
            // Longest sorted limb has no same-flock landing, so he takes the last hop.
            var b = new Board();
            b.SetFeeders(2);
            b.Live[0] = BirdColor.Ruby;
            b.Branches.Add(Stack(BirdColor.Gold, 4));
            b.Branches.Add(Stack(BirdColor.Teal, 1));
            b.Branches.Add(Stack(BirdColor.Teal, 2));
            BlackbirdRival.Dress(b, 0);
            b.LastHopFrom = 0;
            b.LastHopTo = 1;
            var last = BlackbirdRival.Choose(b);
            check("last-moved", last.WillMove && last.Why == "last" && last.From == 1 && last.To == 2,
                last.Why + " " + last.From + ">" + last.To);

            // Notch 0 passes when the single best limb cannot move. Notch 1 tries the next.
            var smart = new Board();
            smart.SetFeeders(2);
            smart.Live[0] = BirdColor.Ruby;
            smart.Branches.Add(Stack(BirdColor.Ruby, 4));
            smart.Branches.Add(Stack(BirdColor.Gold, 2));
            smart.Branches.Add(Stack(BirdColor.Gold, 1));
            BlackbirdRival.Dress(smart, 0);
            var pass = BlackbirdRival.Choose(smart, 0);
            var wider = BlackbirdRival.Choose(smart, 1);
            check("notch-0-pass", !pass.WillMove && pass.Why == "pass", pass.Why);
            check("notch-1-sorted", wider.WillMove && wider.Why == "sorted" && wider.From == 1 && wider.To == 2,
                wider.Why + " " + wider.From + ">" + wider.To);

            var emptyOnly = new Board();
            emptyOnly.SetFeeders(2);
            emptyOnly.Live[0] = BirdColor.Ruby;
            emptyOnly.Branches.Add(Stack(BirdColor.Gold, 3));
            emptyOnly.Branches.Add(new BranchState());
            BlackbirdRival.Dress(emptyOnly, 0);
            var dumped = BlackbirdRival.Choose(emptyOnly);
            check("no-empty-dump", !dumped.WillMove && dumped.Why == "pass", dumped.Why);
        }

        static void CheckHandsOff(CheckDelegate check)
        {
            var b = new Board();
            b.SetFeeders(2);
            b.Live[0] = BirdColor.Peach;
            b.Live[1] = BirdColor.Peach;
            var leaf = Stack(BirdColor.Violet, 3);
            leaf.AlignShroud();
            leaf.Shrouded[leaf.Count - 1] = true;
            b.Branches.Add(leaf);
            b.Branches.Add(Stack(BirdColor.Violet, 1));
            b.Branches.Add(Stack(BirdColor.Ruby, 5));
            b.Branches.Add(Stack(BirdColor.Gold, 2));
            b.Branches.Add(Stack(BirdColor.Gold, 1));
            BlackbirdRival.Dress(b, 0);
            int spare = b.Blackbird.Spare;
            int perch = b.Blackbird.Perch;
            b.Branches[spare].Birds.Add(new Bird(BirdColor.Gold, BirdKit.Plain));
            b.Branches[spare].Shrouded.Add(false);
            b.Branches[perch].Birds.Add(new Bird(BirdColor.Gold, BirdKit.Plain));
            b.Branches[perch].Shrouded.Add(false);
            int leafN = b.Branches[0].Count;
            int doneN = b.Branches[2].Count;
            int spareN = b.Branches[spare].Count;
            int perchN = b.Branches[perch].Count;
            var choice = BlackbirdRival.Choose(b);
            bool off = choice.From != 0 && choice.From != 2 && choice.From != spare && choice.From != perch
                && choice.To != 0 && choice.To != 2 && choice.To != spare && choice.To != perch;
            check("choice-skips-locked", off && choice.WillMove && choice.From == 3 && choice.To == 4,
                choice.Why + " " + choice.From + ">" + choice.To);
            for (int i = 0; i < 6; i++) BlackbirdRival.Commit(b);
            check("never-touch",
                b.Branches[0].Count == leafN && b.Branches[2].Count == doneN
                && b.Branches[spare].Count == spareN && b.Branches[perch].Count == perchN
                && b.Branches[0].TipLocked && b.Branches[2].IsFullMatch(out _),
                "leaf " + b.Branches[0].Count + " done " + b.Branches[2].Count
                + " spare " + b.Branches[spare].Count + " perch " + b.Branches[perch].Count);
        }

        static void CheckThresholds(CheckDelegate check)
        {
            var calm = PullBoard();
            check("owes-calm",
                !BlackbirdRival.Owes(calm.Blackbird, 0, 1)
                && !BlackbirdRival.Owes(calm.Blackbird, 0, 2)
                && BlackbirdRival.Owes(calm.Blackbird, 0, 3)
                && BlackbirdRival.Owes(calm.Blackbird, 2, 6)
                && !BlackbirdRival.Owes(calm.Blackbird, 2, 4),
                "every 3");

            var rattled = PullBoard();
            rattled.Blackbird.ColorMask = 31;
            check("flocks-3", BlackbirdRival.FlocksDone(rattled) == 3, "done " + BlackbirdRival.FlocksDone(rattled));
            var r1 = BlackbirdRival.Commit(rattled);
            var r2 = BlackbirdRival.Commit(rattled);
            check("rattled-every-2", !r1.Moved && r2.Moved && rattled.Blackbird.PlayerMoves == 2 && !rattled.Blackbird.Fled,
                "m1 " + r1.Moved + " m2 " + r2.Moved);

            var flee = PullBoard();
            flee.Blackbird.ColorMask = 31;
            // PullBoard holds gold and teal. Mask of five colors => three already gone.
            // Drop teal so four colors are done and the gold pull is still legal.
            flee.Branches[2].Birds.Clear();
            flee.Branches[2].Shrouded.Clear();
            check("flocks-4", BlackbirdRival.FlocksDone(flee) == 4, "done " + BlackbirdRival.FlocksDone(flee));
            int gold = flee.Branches[0].Count;
            var ditch = BlackbirdRival.Commit(flee);
            check("flee-at-4", ditch.FledNow && ditch.Moved && flee.Blackbird.Fled && flee.Blackbird.LastDitchPlayed
                && flee.Branches[0].Count < gold,
                "moved " + ditch.Moved + " fled " + flee.Blackbird.Fled);
            int left = flee.Branches[0].Count;
            var again = BlackbirdRival.Commit(flee);
            check("flee-stays", !again.Moved && !again.FledNow && flee.Branches[0].Count == left,
                "count " + flee.Branches[0].Count);
        }

        static void CheckSchedule(CheckDelegate check)
        {
            int[] want = { 13, 24, 35, 47, 58, 69, 82 };
            bool list = BlackbirdSchedule.Visit.First == want[0] && BlackbirdSchedule.Visit.Every == 11;
            var got = new StringBuilder();
            for (int k = 0; k < want.Length; k++)
            {
                int at = BlackbirdSchedule.VisitAt(k);
                if (k > 0) got.Append(',');
                got.Append(at);
                if (at != want[k] || !BlackbirdSchedule.Due(at)) list = false;
            }
            bool slides = !BlackbirdSchedule.Due(46) && !BlackbirdSchedule.Due(57)
                && !BlackbirdSchedule.Due(80) && !BlackbirdSchedule.Due(81)
                && BlackbirdSchedule.Blocked(46) && BlackbirdSchedule.Blocked(80) && BlackbirdSchedule.Blocked(81)
                && BlackbirdSchedule.Blocked(54) && BlackbirdSchedule.Blocked(65) && BlackbirdSchedule.Blocked(76)
                && !BlackbirdSchedule.Blocked(82);
            bool smart = BlackbirdSchedule.Notch(13) == 0 && BlackbirdSchedule.Notch(24) == 0
                && BlackbirdSchedule.Notch(35) == 1 && BlackbirdSchedule.Notch(47) == 2
                && BlackbirdSchedule.CalmEveryFor(13) == 4 && BlackbirdSchedule.RattleEveryFor(13) == 3
                && BlackbirdSchedule.CalmEveryFor(24) == 3 && BlackbirdSchedule.RattleEveryFor(24) == 2;
            bool clear = !BlackbirdSchedule.Blocked(13) && !BlackbirdSchedule.Blocked(24)
                && !BlackbirdSchedule.Blocked(35) && !BlackbirdSchedule.Blocked(47);
            check("schedule", list && slides && smart && clear, got.ToString());
        }

        static void CheckFlight(CheckDelegate check)
        {
            var seat = new BlackbirdRival.Point { X = 1f, Y = 2f };
            var drop0 = BlackbirdRival.Flight.Drop(seat, 0f);
            var drop1 = BlackbirdRival.Flight.Drop(seat, 1f);
            var left = BlackbirdRival.Flight.Swoop(seat, 0, 0f, 3f, 5f);
            var back = BlackbirdRival.Flight.Swoop(seat, 0, 1f, 3f, 5f);
            var bolt = BlackbirdRival.Flight.Bolt(seat, 1f, 3f);
            bool ok = drop0.Y > seat.Y + 2f
                && Near(drop1.X, seat.X) && Near(drop1.Y, seat.Y)
                && left.X < seat.X - 3f
                && Near(back.X, seat.X) && Near(back.Y, seat.Y)
                && bolt.X > seat.X + 3f;
            check("flight", ok, "drop " + drop0.Y + " left " + left.X + " bolt " + bolt.X);
        }

        static void CheckBed(CheckDelegate check)
        {
            check("bed",
                BlackbirdCue.BedFor(true, false, 0) == BlackbirdCue.Bed.Cut
                && BlackbirdCue.BedFor(false, false, 2) == BlackbirdCue.Bed.Calm
                && BlackbirdCue.BedFor(false, false, 3) == BlackbirdCue.Bed.Rush
                && BlackbirdCue.BedFor(false, true, 4) == BlackbirdCue.Bed.Silence
                && BlackbirdCue.IntroCutSec == 2.5f
                && BlackbirdCue.TellSec == 0.5f
                && BlackbirdCue.TauntSec == 1f
                && BlackbirdCue.SilenceSec > 0f
                && BlackbirdCue.RushBpm > BlackbirdCue.CalmBpm,
                "cut " + BlackbirdCue.IntroCutSec + " rush " + BlackbirdCue.RushBpm);

            float resumed = BlackbirdCue.ResumeCalm(4.25f, 1.5f, 16f);
            bool mix = resumed > 5.7f && resumed < 5.8f
                && BlackbirdCue.BedCrossfadeSec >= 0.3f && BlackbirdCue.BedCrossfadeSec <= 0.5f
                && BlackbirdCue.PlayTaunt(false) && !BlackbirdCue.PlayTaunt(true)
                && !BlackbirdCue.HearTell(true) && BlackbirdCue.HearTell(false)
                && BlackbirdCue.HoldTaps(true) && !BlackbirdCue.HoldTaps(false)
                && BlackbirdCue.BedAfterInterrupt(BlackbirdCue.Bed.Silence) == BlackbirdCue.Bed.Calm
                && BlackbirdCue.BedAfterInterrupt(BlackbirdCue.Bed.Rush) == BlackbirdCue.Bed.Rush
                && BlackbirdCue.TauntLevel > 0.5f && BlackbirdCue.TauntLevel < 0.7f
                && BlackbirdCue.TauntPitchMax <= 1.04f;
            check("mix", mix, "resume " + resumed + " fade " + BlackbirdCue.BedCrossfadeSec);
        }

        static void CheckFold(CheckDelegate check)
        {
            var via = PullBoard();
            var direct = via.Clone();
            for (int i = 0; i < 3; i++) GardenSolve.AfterMove(via);
            for (int i = 0; i < 3; i++) BlackbirdRival.Commit(direct);
            bool same = via.Blackbird.PlayerMoves == 3 && direct.Blackbird.PlayerMoves == 3
                && via.Branches[0].Count == direct.Branches[0].Count
                && via.Branches[0].Count < 3
                && via.Branches[1].Count == direct.Branches[1].Count;
            check("solver-folds-move", same,
                "src " + via.Branches[0].Count + " dst " + via.Branches[1].Count);
        }

        static void CheckSeeds(CheckDelegate check)
        {
            int wins = 0;
            int traps = 0;
            for (int s = 0; s < 4; s++)
            {
                var board = BlackbirdSeeds.Make(s);
                if ((s & 1) == 0)
                    board.Branches[board.Blackbird.Spare].AdLocked = true;
                var report = GardenSolve.Search(board, 40000);
                Debug.Log("[blackbird] seed " + s + " " + report.Outlook + " moves " + report.Moves + " nodes " + report.Nodes);
                if (report.Outlook == GardenSolve.Outlook.Winnable) wins++;
                if (report.Outlook == GardenSolve.Outlook.Tangled) traps++;
            }
            check("fifty-fifty", wins == 2 && traps >= 1 && wins + traps == 4, "wins " + wins + " traps " + traps);
            var canon = BlackbirdSeeds.Make(BlackbirdSeeds.Canonical);
            canon.Branches[canon.Blackbird.Spare].AdLocked = true;
            var canonReport = GardenSolve.Search(canon, 40000);
            check("canonical-no-spare", canonReport.Outlook == GardenSolve.Outlook.Winnable,
                canonReport.Outlook + " moves " + canonReport.Moves);
        }

        static void CheckConfiguredGarden(CheckDelegate check)
        {
            int first = BlackbirdSchedule.VisitAt(0);
            int index;
            bool found = LevelData.TryIndex(first, out index);
            var board = found ? LevelData.Open(index) : new Board();
            int shown = LevelData.Current != null ? LevelData.Current.Number : -1;
            int perch = board.Blackbird != null ? board.Blackbird.Perch : -1;
            int spare = board.Blackbird != null ? board.Blackbird.Spare : -1;
            bool shape = found && shown == first && board.Blackbird != null && board.Blackbird.Notch == 0
                && board.Blackbird.CalmEvery == BlackbirdSchedule.CalmEveryFor(first)
                && board.Blackbird.RattleEvery == BlackbirdSchedule.RattleEveryFor(first)
                && perch >= 0 && spare >= 0
                && board.Branches[perch].RivalPerch && board.Branches[perch].SeatCap == 1
                && board.Branches[spare].IsSpare && board.Branches[spare].Empty
                && board.Ostrich == null;
            check("visit-shape", shape,
                "garden " + shown + " every " + (board.Blackbird != null ? board.Blackbird.CalmEvery : 0)
                + " spare " + spare + " perch " + perch);

            if (!shape) return;
            // Garden 13 opens with 62 legal hops. The shared search dives thousands of
            // plies before it returns, so this witness is a short clear played through
            // AfterMove. His gentle reply (every 4th move, notch 0) is inside that transition.
            const string line = "11>9,4>2,2>12,2>4,3>12,4>3,4>1,8>13,9>10,7>6,10>9,11>5,11>13,10>2,6>7,7>11,9>2,5>0,0>10";
            int replies = PlayWitness(board, line, perch);
            bool won = first == 13
                && replies == 2
                && GardenSolve.Cleared(board)
                && board.Blackbird.PlayerMoves == 19
                && board.Blackbird.Notch == 0
                && board.Blackbird.CalmEvery == BlackbirdSchedule.GentleCalm
                && board.Branches[perch].Count == 0
                && board.Branches[spare].IsSpare;
            check("visit-winnable", won,
                "garden " + first + " replies " + replies + " moves " + board.Blackbird.PlayerMoves
                + " cleared " + GardenSolve.Cleared(board));
        }

        // Plays from>to hops through the solver transition. Returns how many of
        // those transitions his policy changed, or -1 when a hop was illegal
        // or a bird landed on his perch.
        static int PlayWitness(Board board, string line, int perch)
        {
            int replies = 0;
            string[] parts = line.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int gt = parts[i].IndexOf('>');
                if (gt <= 0) return -1;
                int from = int.Parse(parts[i].Substring(0, gt));
                int to = int.Parse(parts[i].Substring(gt + 1));
                if (!board.TryMove(from, to, out _)) return -1;
                var ghost = board.Clone();
                GardenSolve.Drain(ghost);
                GardenSolve.AfterMove(board);
                if (Limbs(ghost) != Limbs(board)) replies++;
                if (board.Branches[perch].Count != 0) return -1;
            }
            return replies;
        }

        static string Limbs(Board b)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                sb.Append('|');
                if (br.Broken) { sb.Append('X'); continue; }
                for (int k = 0; k < br.Count; k++)
                    sb.Append((int)br.Birds[k].Color);
            }
            sb.Append('Q');
            for (int i = 0; i < b.Queue.Count; i++) sb.Append((int)b.Queue[i]);
            return sb.ToString();
        }

        // Gold x3 onto gold x1, plus a teal that keeps a second color on the board.
        static Board PullBoard()
        {
            var b = new Board();
            b.SetFeeders(2);
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Gold;
            b.Branches.Add(Stack(BirdColor.Gold, 3));
            b.Branches.Add(Stack(BirdColor.Gold, 1));
            b.Branches.Add(Stack(BirdColor.Teal, 2));
            BlackbirdRival.Dress(b, 0);
            return b;
        }

        static Board Dressable()
        {
            var b = new Board();
            b.SetFeeders(2);
            b.Live[0] = BirdColor.Ruby;
            b.Branches.Add(Stack(BirdColor.Ruby, 1));
            b.Branches.Add(Stack(BirdColor.Gold, 1));
            b.Branches.Add(Stack(BirdColor.Teal, 1));
            b.Branches.Add(Stack(BirdColor.Violet, 1));
            b.Branches.Add(Stack(BirdColor.Peach, 1));
            return b;
        }

        static int Colors(Board b)
        {
            int mask = b.Blackbird != null ? b.Blackbird.ColorMask : 0;
            int n = 0;
            for (int c = 0; c < Palette.Max; c++)
                if ((mask & (1 << c)) != 0) n++;
            return n;
        }

        static BranchState Stack(BirdColor c, int n)
        {
            var br = new BranchState();
            var bird = new Bird(c, BirdKit.Plain);
            for (int i = 0; i < n; i++)
            {
                br.Birds.Add(bird);
                br.Shrouded.Add(false);
            }
            return br;
        }

        static bool Near(float a, float b)
        {
            float d = a - b;
            if (d < 0f) d = -d;
            return d < 0.02f;
        }
    }
}
#endif

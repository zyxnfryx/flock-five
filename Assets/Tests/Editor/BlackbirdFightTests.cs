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
            var saved = BlackbirdSchedule.Visit;
            bool returns = BlackbirdSchedule.Returns;
            try
            {
                int first = saved.First;
                int every = saved.Every < 1 ? 1 : saved.Every;
                BlackbirdSchedule.Returns = false;
                bool off = !BlackbirdSchedule.Due(first + every) && BlackbirdSchedule.Due(first)
                    && BlackbirdSchedule.Notch(first) == 0 && !BlackbirdSchedule.Due(first + 1);
                BlackbirdSchedule.Returns = true;
                int offGrid = every <= 1 ? first - 1 : first + 1;
                bool on = BlackbirdSchedule.Due(first + every) && BlackbirdSchedule.Notch(first + every) == 1
                    && BlackbirdSchedule.Due(first + 2 * every) && BlackbirdSchedule.Notch(first + 2 * every) == 2
                    && BlackbirdSchedule.Due(first + 3 * every) && BlackbirdSchedule.Notch(first + 3 * every) == 3
                    && BlackbirdSchedule.Due(first + 4 * every) && BlackbirdSchedule.Notch(first + 4 * every) == 4
                    && !BlackbirdSchedule.Due(offGrid);
                // A different debut moves every visit with it. The literals above are offsets, not a garden.
                var shifted = saved;
                shifted.First = first + 5;
                BlackbirdSchedule.Visit = shifted;
                bool moved = BlackbirdSchedule.Due(shifted.First)
                    && !BlackbirdSchedule.Due(first)
                    && BlackbirdSchedule.Notch(shifted.First + every) == 1;
                check("schedule", off && on && moved, "off " + off + " on " + on + " moved " + moved);
            }
            finally
            {
                BlackbirdSchedule.Visit = saved;
                BlackbirdSchedule.Returns = returns;
            }
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
            int first = BlackbirdSchedule.Visit.First;
            bool keep = BlackbirdSchedule.Returns;
            BlackbirdSchedule.Returns = false;
            int index;
            bool found = LevelData.TryIndex(first, out index);
            var board = found ? LevelData.Open(index) : new Board();
            BlackbirdSchedule.Returns = keep;
            int shown = LevelData.Current != null ? LevelData.Current.Number : -1;
            int perch = board.Blackbird != null ? board.Blackbird.Perch : -1;
            int spare = board.Blackbird != null ? board.Blackbird.Spare : -1;
            bool shape = found && shown == first && board.Blackbird != null && board.Blackbird.Notch == 0
                && perch >= 0 && spare >= 0
                && board.Branches[perch].RivalPerch && board.Branches[perch].SeatCap == 1
                && board.Branches[spare].IsSpare && board.Branches[spare].Empty
                && board.Ostrich == null;
            check("visit-shape", shape,
                "garden " + shown + " spare " + spare + " perch " + perch + " limbs " + board.Branches.Count);

            if (!shape) return;
            var locked = board.Clone();
            locked.Branches[locked.Blackbird.Spare].AdLocked = true;
            var report = GardenSolve.Search(locked, 40000);
            Debug.Log("[blackbird] garden " + first + " " + report.Outlook + " moves " + report.Moves + " nodes " + report.Nodes);
            check("visit-winnable", report.Outlook == GardenSolve.Outlook.Winnable,
                report.Outlook + " garden " + first);
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

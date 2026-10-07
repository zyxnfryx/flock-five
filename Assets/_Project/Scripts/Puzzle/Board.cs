using System.Collections.Generic;

namespace FlockFive
{
    public sealed class BranchState
    {
        public const int Cap = 5;
        public readonly List<Bird> Birds = new List<Bird>(Cap);
        public readonly List<bool> Shrouded = new List<bool>(Cap);
        public bool Broken;
        public bool AdLocked;
        // Bottom gift limb. A pest may snap it, but it always comes back.
        public bool IsBonus;

        public int Count => Birds.Count;
        public int Free => Cap - Count;
        public bool Empty => Count == 0;
        public Bird? Tip => Count == 0 ? (Bird?)null : Birds[Count - 1];

        public bool IsShrouded(int i) =>
            i >= 0 && i < Shrouded.Count && Shrouded[i];

        public bool TipLocked => Count > 0 && IsShrouded(Count - 1);

        public int TipRun()
        {
            if (Count == 0) return 0;
            if (IsShrouded(Count - 1)) return 0;
            var c = Birds[Count - 1];
            int n = 1;
            for (int i = Count - 2; i >= 0; i--)
            {
                if (IsShrouded(i) || !Birds[i].SameFlock(c)) break;
                n++;
            }
            return n;
        }

        public bool IsFullMatch(out BirdColor color)
        {
            color = default;
            if (Broken || Count != Cap) return false;
            for (int i = 0; i < Count; i++)
                if (IsShrouded(i)) return false;
            color = Birds[0].Color;
            for (int i = 1; i < Cap; i++)
                if (Birds[i].Color != color) return false;
            return true;
        }

        public int RevealExposed()
        {
            AlignShroud();
            if (Count == 0) return 0;
            // Only the furthest bee lifts when the tip is newly exposed.
            // A hidden run (consecutive same-color shrouded bees at the tip)
            // lifts together. Inner bees behind a different color stay.
            if (!IsShrouded(Count - 1)) return 0;
            var c = Birds[Count - 1].Color;
            int n = 0;
            for (int i = Count - 1; i >= 0; i--)
            {
                if (Birds[i].Color != c || !Shrouded[i]) break;
                Shrouded[i] = false;
                n++;
            }
            return n;
        }

        // Leaves sit on the tip. Bees may wait underneath (inner shrouds).
        public bool LiftLeaf()
        {
            AlignShroud();
            if (Count == 0 || !IsShrouded(Count - 1)) return false;
            Shrouded[Count - 1] = false;
            return true;
        }

        public void AlignShroud()
        {
            while (Shrouded.Count < Birds.Count) Shrouded.Add(false);
            while (Shrouded.Count > Birds.Count) Shrouded.RemoveAt(Shrouded.Count - 1);
        }

        public BranchState Clone()
        {
            var b = new BranchState { Broken = Broken, AdLocked = AdLocked, IsBonus = IsBonus };
            b.Birds.AddRange(Birds);
            b.Shrouded.AddRange(Shrouded);
            return b;
        }
    }

    public sealed class Board
    {
        public readonly List<BranchState> Branches = new List<BranchState>();
        public readonly BirdColor?[] Live = new BirdColor?[2];
        public readonly List<BirdColor> Queue = new List<BirdColor>();
        // Flock a pest knocked off a limb and has not seated yet. No branch is
        // reserved for them. They still count, so a win or a stuck check is not
        // decided while they are in the air.
        public readonly List<Bird> Displaced = new List<Bird>();
        public bool JustUnveiled;
        public bool BreezeOnCollect;

        public Board Clone()
        {
            var n = new Board();
            for (int i = 0; i < Branches.Count; i++)
                n.Branches.Add(Branches[i].Clone());
            n.Live[0] = Live[0];
            n.Live[1] = Live[1];
            n.Queue.AddRange(Queue);
            n.Displaced.AddRange(Displaced);
            n.BreezeOnCollect = BreezeOnCollect;
            return n;
        }

        public bool LiveHas(BirdColor c) => Live[0] == c || Live[1] == c;

        public bool IsSleeping(int i)
        {
            if ((uint)i >= (uint)Branches.Count) return false;
            return Branches[i].IsFullMatch(out var col) && !LiveHas(col);
        }

        // Five of a kind is never a hand — sleeping waits on a feeder,
        // live ones are already collecting or about to.
        public bool CanPick(int i)
        {
            if ((uint)i >= (uint)Branches.Count) return false;
            var a = Branches[i];
            if (a.Broken || a.Empty || a.AdLocked || a.TipLocked) return false;
            if (a.IsFullMatch(out _)) return false;
            return true;
        }

        public int FeederSlotFor(BirdColor c)
        {
            if (Live[0] == c) return 0;
            if (Live[1] == c) return 1;
            return -1;
        }

        public bool CanMove(int from, int to, out int run)
        {
            run = 0;
            if (from == to) return false;
            if ((uint)from >= (uint)Branches.Count || (uint)to >= (uint)Branches.Count) return false;
            var a = Branches[from];
            var b = Branches[to];
            // A locked gift is not a perch. An open bonus limb is, even when empty.
            if (a.Broken || b.Broken || a.AdLocked || b.AdLocked || a.Empty) return false;
            if (a.IsFullMatch(out _)) return false;
            // Leaf-locked limb: unusable until a feeder collect breeze lifts the tip.
            if (a.TipLocked || b.TipLocked) return false;
            if (b.Free <= 0) return false;
            if (!b.Empty && !b.Tip.Value.SameFlock(a.Tip.Value)) return false;
            run = a.TipRun();
            if (run > b.Free) run = b.Free;
            return run > 0;
        }

        // Any legal hop. An open bonus limb is a destination, including while empty.
        public bool HasHop()
        {
            int n = Branches.Count;
            for (int from = 0; from < n; from++)
            {
                var a = Branches[from];
                if (a.Broken || a.Empty || a.TipLocked) continue;
                if (a.IsFullMatch(out _)) continue;
                for (int to = 0; to < n; to++)
                    if (CanMove(from, to, out _)) return true;
            }
            return false;
        }

        public bool TryMove(int from, int to, out int run)
        {
            JustUnveiled = false;
            if (!CanMove(from, to, out run)) return false;
            var a = Branches[from];
            var b = Branches[to];
            a.AlignShroud();
            b.AlignShroud();
            // Each bird, not a copy of the tip, so a cosmetic accessory stays put.
            for (int i = 0; i < run; i++)
            {
                int at = a.Birds.Count - 1;
                var bird = a.Birds[at];
                a.Birds.RemoveAt(at);
                if (a.Shrouded.Count > a.Birds.Count)
                    a.Shrouded.RemoveAt(a.Shrouded.Count - 1);
                b.Birds.Add(bird);
                b.Shrouded.Add(false);
            }
            JustUnveiled = a.RevealExposed() > 0;
            return true;
        }

        public int FindCollect()
        {
            for (int i = 0; i < Branches.Count; i++)
            {
                if (Branches[i].Broken) continue;
                if (Branches[i].IsFullMatch(out var col) && LiveHas(col))
                    return i;
            }
            return -1;
        }

        // One feeder collect lifts one leaf set. The locked tip on the same
        // column as the feeder that just paid is the set that collect hits;
        // otherwise the locked tip nearest the feeders (higher row). Bees
        // under a leaf stay until that tip is exposed.
        public int Breeze(int feederSlot = -1)
        {
            int pick = -1;
            int best = int.MaxValue;
            int wantCol = feederSlot == 0 ? 0 : (feederSlot == 1 ? 1 : -1);
            for (int i = 0; i < Branches.Count; i++)
            {
                var br = Branches[i];
                if (br.Broken || br.Count == 0 || !br.TipLocked) continue;
                int row = i >> 1;
                int col = i & 1;
                int score = row * 4;
                if (wantCol >= 0 && col != wantCol) score += 6;
                if (score >= best) continue;
                best = score;
                pick = i;
            }
            if (pick < 0) return 0;
            return Branches[pick].LiftLeaf() ? 1 : 0;
        }

        public int ApplyCollect(int branchIndex, bool scoreFeeder = true)
        {
            var br = Branches[branchIndex];
            br.IsFullMatch(out var col);
            int slot = FeederSlotFor(col);
            br.Birds.Clear();
            br.Broken = true;
            // A pest scrap does not retire a feeder. Scoring it here would
            // sleep leftover fives and false-freeze the garden.
            if (scoreFeeder) ScoreFeeder(slot);
            else if (BreezeOnCollect) Breeze(slot);
            return slot;
        }

        // The feeder this collect already matched. The limb may already be
        // empty, so IsFullMatch can no longer name the slot.
        public void ScoreFeeder(int slot)
        {
            if (slot >= 0 && (uint)slot < (uint)Live.Length)
            {
                if (Queue.Count > 0)
                {
                    Live[slot] = Queue[0];
                    Queue.RemoveAt(0);
                }
                else Live[slot] = null;
            }
            if (BreezeOnCollect) Breeze(slot);
        }

        public bool Won
        {
            get
            {
                if (Displaced.Count > 0) return false;
                for (int i = 0; i < Branches.Count; i++)
                    if (!Branches[i].Broken && Branches[i].Count > 0) return false;
                return true;
            }
        }

        // Build 61: the one solved-board check. Nothing in the air, no leaf, no bird on a locked
        // gift, and every perch holds a single colour: only merges (and the feeder collects they
        // set off) are left, so the game finishes it. Won counts as solved. Pest paths (sparrow,
        // hawk, PestPark redistribute) all end on this board, so they all reach this check.
        public bool Solved
        {
            get
            {
                if (Displaced.Count > 0) return false;
                for (int i = 0; i < Branches.Count; i++)
                {
                    var b = Branches[i];
                    if (b.Broken || b.Count == 0) continue;
                    if (b.AdLocked) return false;
                    var c = b.Birds[0].Color;
                    for (int k = 0; k < b.Count; k++)
                    {
                        if (b.IsShrouded(k) || b.Birds[k].Color != c) return false;
                    }
                }
                return true;
            }
        }

        // Next auto-resolve merge on a Solved board: the smallest part-flock of a colour goes
        // onto that colour's biggest other part-flock with room. PestPark never lands a seat
        // that completes a five, so the last flock back from a scrap is always split (4+1,
        // 3+2...); this joins it. False when nothing is left to join.
        public bool NextSolvedMerge(out int from, out int to)
        {
            from = -1;
            to = -1;
            if (!Solved) return false;
            int n = Branches.Count;
            for (int a = 0; a < n; a++)
            {
                var src = Branches[a];
                if (src.Broken || src.Count == 0 || src.Count >= BranchState.Cap) continue;
                var c = src.Birds[0].Color;
                int bestTo = -1;
                for (int b = 0; b < n; b++)
                {
                    if (b == a) continue;
                    var dst = Branches[b];
                    if (dst.Broken || dst.AdLocked || dst.Count == 0 || dst.Free <= 0) continue;
                    if (dst.Birds[0].Color != c) continue;
                    // The smaller part moves (ties: the later limb), onto the fullest part.
                    if (dst.Count < src.Count || (dst.Count == src.Count && b < a)) continue;
                    if (bestTo < 0 || dst.Count > Branches[bestTo].Count) bestTo = b;
                }
                if (bestTo < 0) continue;
                if (from < 0 || src.Count < Branches[from].Count)
                {
                    from = a;
                    to = bestTo;
                }
            }
            return from >= 0;
        }

        // Auto-resolve hop: the top birds of a single-colour limb onto a same-colour limb, as
        // many as fit. A five is colour only, so the sex rule of a player hop does not apply.
        public bool MergeRun(int from, int to, out int run)
        {
            run = 0;
            if (from == to || (uint)from >= (uint)Branches.Count || (uint)to >= (uint)Branches.Count) return false;
            var a = Branches[from];
            var b = Branches[to];
            if (a.Broken || b.Broken || b.AdLocked || a.Count == 0 || b.Count == 0) return false;
            if (a.TipLocked || b.TipLocked || a.Birds[0].Color != b.Birds[0].Color) return false;
            run = System.Math.Min(a.Count, b.Free);
            if (run <= 0) return false;
            a.AlignShroud();
            b.AlignShroud();
            for (int i = 0; i < run; i++)
            {
                var bird = a.Birds[a.Count - 1];
                a.Birds.RemoveAt(a.Count - 1);
                a.Shrouded.RemoveAt(a.Shrouded.Count - 1);
                b.Birds.Add(bird);
                b.Shrouded.Add(false);
            }
            JustUnveiled = false;
            return true;
        }

        public int RemainingBirds
        {
            get
            {
                int n = Displaced.Count;
                for (int i = 0; i < Branches.Count; i++)
                    if (!Branches[i].Broken) n += Branches[i].Count;
                return n;
            }
        }
    }
}

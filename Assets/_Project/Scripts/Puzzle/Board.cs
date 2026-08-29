using System.Collections.Generic;

namespace FlockFive
{
    public sealed class BranchState
    {
        public const int Cap = 5;
        public readonly List<BirdColor> Birds = new List<BirdColor>(Cap);
        public readonly List<bool> Shrouded = new List<bool>(Cap);
        public bool Broken;

        public int Count => Birds.Count;
        public int Free => Cap - Count;
        public bool Empty => Count == 0;
        public BirdColor? Tip => Count == 0 ? (BirdColor?)null : Birds[Count - 1];

        public bool IsShrouded(int i) =>
            i >= 0 && i < Shrouded.Count && Shrouded[i];

        public int TipRun()
        {
            if (Count == 0) return 0;
            if (IsShrouded(Count - 1)) return 0;
            var c = Birds[Count - 1];
            int n = 1;
            for (int i = Count - 2; i >= 0; i--)
            {
                if (IsShrouded(i) || Birds[i] != c) break;
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
            color = Birds[0];
            for (int i = 1; i < Cap; i++)
                if (Birds[i] != color) return false;
            return true;
        }

        public int RevealExposed()
        {
            AlignShroud();
            if (Count == 0) return 0;
            // Bees lift only when a buried bird is now the tip. A leftover
            // outer bird of the same color still covers the swarm.
            if (!IsShrouded(Count - 1)) return 0;
            var c = Birds[Count - 1];
            int n = 0;
            for (int i = Count - 1; i >= 0; i--)
            {
                if (Birds[i] != c || !Shrouded[i]) break;
                Shrouded[i] = false;
                n++;
            }
            return n;
        }

        public void AlignShroud()
        {
            while (Shrouded.Count < Birds.Count) Shrouded.Add(false);
            while (Shrouded.Count > Birds.Count) Shrouded.RemoveAt(Shrouded.Count - 1);
        }

        public BranchState Clone()
        {
            var b = new BranchState { Broken = Broken };
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
            n.BreezeOnCollect = BreezeOnCollect;
            return n;
        }

        public bool LiveHas(BirdColor c) => Live[0] == c || Live[1] == c;

        public bool IsSleeping(int i)
        {
            if ((uint)i >= (uint)Branches.Count) return false;
            return Branches[i].IsFullMatch(out var col) && !LiveHas(col);
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
            if (a.Broken || b.Broken || a.Empty) return false;
            if (a.IsFullMatch(out var wait) && !LiveHas(wait)) return false;
            if (b.Free <= 0) return false;
            if (!b.Empty && b.Tip != a.Tip) return false;
            run = a.TipRun();
            if (run > b.Free) run = b.Free;
            return run > 0;
        }

        public bool TryMove(int from, int to, out int run)
        {
            JustUnveiled = false;
            if (!CanMove(from, to, out run)) return false;
            var a = Branches[from];
            var b = Branches[to];
            var c = a.Tip.Value;
            a.AlignShroud();
            b.AlignShroud();
            for (int i = 0; i < run; i++)
            {
                a.Birds.RemoveAt(a.Birds.Count - 1);
                if (a.Shrouded.Count > a.Birds.Count)
                    a.Shrouded.RemoveAt(a.Shrouded.Count - 1);
                b.Birds.Add(c);
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

        public int Breeze()
        {
            int n = 0;
            for (int i = 0; i < Branches.Count; i++)
            {
                var br = Branches[i];
                if (br.Broken || br.Count == 0) continue;
                if (!br.IsShrouded(br.Count - 1)) continue;
                if (br.RevealExposed() > 0) n++;
            }
            return n;
        }

        public int ApplyCollect(int branchIndex)
        {
            var br = Branches[branchIndex];
            br.IsFullMatch(out var col);
            int slot = FeederSlotFor(col);
            br.Birds.Clear();
            br.Broken = true;
            if (slot >= 0)
            {
                if (Queue.Count > 0)
                {
                    Live[slot] = Queue[0];
                    Queue.RemoveAt(0);
                }
                else Live[slot] = null;
            }
            if (BreezeOnCollect) Breeze();
            return slot;
        }

        public bool Won
        {
            get
            {
                for (int i = 0; i < Branches.Count; i++)
                    if (!Branches[i].Broken && Branches[i].Count > 0) return false;
                return true;
            }
        }

        public int RemainingBirds
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Branches.Count; i++)
                    if (!Branches[i].Broken) n += Branches[i].Count;
                return n;
            }
        }
    }
}

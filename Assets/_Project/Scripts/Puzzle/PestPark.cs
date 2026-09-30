using System.Collections.Generic;

namespace FlockFive
{
    // Where a pest flock sits after the scrap. Pure board math: no views.
    // A sparrow may drop a plain limb only when every bird still has a seat
    // that is not a five-match, and one free seat remains. A hawk never
    // drops a limb. A bonus limb always grows back.
    public static class PestPark
    {
        public struct Home
        {
            public int Branch;
            public int Seat;
            public Bird Bird;
        }

        public struct Plan
        {
            public bool Break;
            public int Spawn;
            public Home[] Homes;
        }

        public static Plan Apply(Board board, int branch, bool hawk)
        {
            var plan = Build(board, branch, hawk);
            if (board == null) return plan;
            if ((uint)branch >= (uint)board.Branches.Count) return plan;
            var src = board.Branches[branch];
            src.Birds.Clear();
            src.Shrouded.Clear();
            src.Broken = true;
            if (board.BreezeOnCollect) board.Breeze();
            Commit(board, branch, plan);
            return plan;
        }

        public static Plan Build(Board board, int branch, bool hawk)
        {
            var empty = new Plan { Homes = new Home[0] };
            if (board == null || (uint)branch >= (uint)board.Branches.Count) return empty;
            var src = board.Branches[branch];
            var flock = new List<Bird>(src.Count);
            for (int i = 0; i < src.Count; i++)
                flock.Add(src.Birds[i]);

            bool bonus = src.IsBonus;
            // Hawk and gifts keep the limb. Sparrow keeps it when the others
            // cannot hold the flock with a spare seat left over.
            bool mustKeep = hawk || bonus;
            var work = board.Clone();
            ClearSource(work, branch);

            if (!mustKeep && TryFit(work, branch, flock, sourceOpen: false, spawns: 0, out var broken))
                return broken;

            for (int spawns = 0; spawns <= flock.Count + 1; spawns++)
            {
                if (TryFit(work, branch, flock, sourceOpen: true, spawns: spawns, out var kept))
                    return kept;
            }

            return Force(work, branch, flock);
        }

        public static void Commit(Board board, int branch, Plan plan)
        {
            if (board == null || (uint)branch >= (uint)board.Branches.Count) return;
            var src = board.Branches[branch];
            src.Birds.Clear();
            src.Shrouded.Clear();
            bool keep = !plan.Break || src.IsBonus;
            src.Broken = !keep;
            for (int i = 0; i < plan.Spawn; i++)
                board.Branches.Add(new BranchState());
            if (plan.Homes == null) return;
            var order = new List<Home>(plan.Homes);
            order.Sort((a, b) =>
            {
                int c = a.Branch.CompareTo(b.Branch);
                return c != 0 ? c : a.Seat.CompareTo(b.Seat);
            });
            for (int i = 0; i < order.Count; i++)
            {
                var h = order[i];
                if ((uint)h.Branch >= (uint)board.Branches.Count) continue;
                var st = board.Branches[h.Branch];
                if (st.IsBonus) st.Broken = false;
                if (st.Broken) continue;
                st.Birds.Add(h.Bird);
                st.Shrouded.Add(false);
                st.AlignShroud();
            }
            if (src.IsBonus) src.Broken = false;
        }

        static void ClearSource(Board board, int branch)
        {
            var src = board.Branches[branch];
            src.Birds.Clear();
            src.Shrouded.Clear();
            src.Broken = true;
        }

        static bool TryFit(Board seed, int branch, List<Bird> flock, bool sourceOpen, int spawns, out Plan plan)
        {
            plan = default;
            var b = seed.Clone();
            if (sourceOpen)
            {
                b.Branches[branch].Broken = false;
                if (b.Branches[branch].IsBonus) b.Branches[branch].AdLocked = false;
            }
            for (int i = 0; i < spawns; i++)
                b.Branches.Add(new BranchState());
            if (!Place(b, flock, allowFull: false, out var homes)) return false;
            if (b.RemainingBirds > 0 && FreeSeats(b) < 1) return false;
            plan = new Plan
            {
                Break = !sourceOpen && !b.Branches[branch].IsBonus,
                Spawn = spawns,
                Homes = homes
            };
            return true;
        }

        static Plan Force(Board seed, int branch, List<Bird> flock)
        {
            var b = seed.Clone();
            b.Branches[branch].Broken = false;
            int baseCount = b.Branches.Count;
            Place(b, flock, allowFull: true, out var homes);
            return new Plan
            {
                Break = false,
                Spawn = b.Branches.Count - baseCount,
                Homes = homes ?? new Home[flock.Count]
            };
        }

        static bool Place(Board b, List<Bird> flock, bool allowFull, out Home[] homes)
        {
            homes = new Home[flock.Count];
            for (int i = 0; i < flock.Count; i++)
            {
                int home = Find(b, flock[i], allowFull);
                if (home < 0 && !allowFull) return false;
                if (home < 0)
                {
                    b.Branches.Add(new BranchState());
                    home = b.Branches.Count - 1;
                }
                var st = b.Branches[home];
                int seat = st.Count;
                st.Birds.Add(flock[i]);
                st.Shrouded.Add(false);
                st.AlignShroud();
                homes[i] = new Home { Branch = home, Seat = seat, Bird = flock[i] };
            }
            return true;
        }

        static int Find(Board b, Bird bird, bool allowFull)
        {
            int best = -1;
            int bestFree = -1;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var st = b.Branches[i];
                // Same rule as BoardValidator.FindSeat: a bonus perch is the player's.
                // Parking here used to fill a gift the moment it unlocked.
                if (st.Broken || st.AdLocked || st.IsBonus || st.Free <= 0) continue;
                if (b.IsSleeping(i)) continue;
                if (!allowFull && WouldFill(st, bird)) continue;
                if (st.Free > bestFree)
                {
                    bestFree = st.Free;
                    best = i;
                }
            }
            return best;
        }

        static bool WouldFill(BranchState st, Bird bird)
        {
            if (st == null || st.Broken) return false;
            if (st.Count + 1 != BranchState.Cap) return false;
            for (int i = 0; i < st.Count; i++)
            {
                if (st.IsShrouded(i)) return false;
                if (!st.Birds[i].SameFlock(bird)) return false;
            }
            return true;
        }

        public static int FreeSeats(Board b)
        {
            if (b == null) return 0;
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var st = b.Branches[i];
                if (st.Broken || st.AdLocked) continue;
                n += st.Free;
            }
            return n;
        }
    }
}

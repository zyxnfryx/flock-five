using System.Collections.Generic;

namespace FlockFive
{
    // Where a pest flock sits after the scrap. Pure board math: no views.
    // Hold lifts the flock when the scrap starts and picks no limb. Resolve
    // seats them when the scrap ends, on the board as it is then. Sparrow,
    // hawk, and any later pest share this path.
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

        // Lift the flock off this limb. No destination is chosen and the limb
        // is not broken. The birds stay on Board.Displaced until Resolve.
        public static void Hold(Board board, int branch)
        {
            if (board == null || (uint)branch >= (uint)board.Branches.Count) return;
            var src = board.Branches[branch];
            if (src == null) return;
            for (int i = 0; i < src.Count; i++)
                board.Displaced.Add(src.Birds[i]);
            src.Birds.Clear();
            src.Shrouded.Clear();
        }

        public static Plan Apply(Board board, int branch, bool hawk)
        {
            Hold(board, branch);
            return Resolve(board, branch, hawk);
        }

        // Seat the flock Hold lifted. Seats are chosen here, then checked
        // again as each bird lands, because the board may have changed.
        public static Plan Resolve(Board board, int branch, bool hawk)
        {
            var empty = new Plan { Homes = new Home[0] };
            if (board == null) return empty;
            var flock = new List<Bird>(board.Displaced.Count);
            for (int i = 0; i < board.Displaced.Count; i++)
                flock.Add(board.Displaced[i]);
            board.Displaced.Clear();
            if (flock.Count == 0) return empty;
            if ((uint)branch >= (uint)board.Branches.Count)
                return PlaceLost(board, flock);

            var src = board.Branches[branch];
            // Hold emptied this limb. Birds here now were played on during the scrap.
            bool arrived = src != null && src.Count > 0;
            var plan = Build(board, branch, flock, hawk || arrived, !arrived);
            if (src != null && !arrived)
            {
                src.Birds.Clear();
                src.Shrouded.Clear();
                src.Broken = true;
            }
            if (arrived) plan.Break = false;
            if (board.BreezeOnCollect) board.Breeze();
            return Commit(board, branch, plan);
        }

        public static Plan Build(Board board, int branch, bool hawk)
        {
            var empty = new Plan { Homes = new Home[0] };
            if (board == null || (uint)branch >= (uint)board.Branches.Count) return empty;
            var src = board.Branches[branch];
            var flock = new List<Bird>(src.Count);
            for (int i = 0; i < src.Count; i++)
                flock.Add(src.Birds[i]);
            return Build(board, branch, flock, hawk, true);
        }

        static Plan Build(Board board, int branch, List<Bird> flock, bool hawk, bool liftSource)
        {
            var empty = new Plan { Homes = new Home[0] };
            if (board == null || (uint)branch >= (uint)board.Branches.Count) return empty;
            var src = board.Branches[branch];
            if (flock == null) flock = new List<Bird>();

            bool bonus = src.IsBonus;
            // Hawk and gifts keep the limb. Sparrow keeps it when the others
            // cannot hold the flock with a spare seat left over. A limb the
            // player filled during the scrap is kept too.
            bool mustKeep = hawk || bonus || !liftSource;
            var work = board.Clone();
            // The flock is the list about to be seated, not a second copy in the air.
            work.Displaced.Clear();
            if (liftSource) ClearSource(work, branch);

            if (!mustKeep && TryFit(work, branch, flock, sourceOpen: false, spawns: 0, out var broken))
                return broken;

            for (int spawns = 0; spawns <= flock.Count + 1; spawns++)
            {
                if (TryFit(work, branch, flock, sourceOpen: true, spawns: spawns, out var kept))
                    return kept;
            }

            return Force(work, branch, flock);
        }

        static Plan Commit(Board board, int branch, Plan plan)
        {
            if (board == null || (uint)branch >= (uint)board.Branches.Count) return plan;
            var src = board.Branches[branch];
            // Birds already on this limb were played here after the lift. Keep them.
            bool arrived = src.Count > 0;
            if (!arrived)
            {
                src.Birds.Clear();
                src.Shrouded.Clear();
            }
            bool keep = !plan.Break || src.IsBonus || arrived;
            src.Broken = !keep;
            for (int i = 0; i < plan.Spawn; i++)
                board.Branches.Add(new BranchState());
            if (plan.Homes == null) plan.Homes = new Home[0];
            var order = new List<int>(plan.Homes.Length);
            for (int i = 0; i < plan.Homes.Length; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                var ha = plan.Homes[a];
                var hb = plan.Homes[b];
                int cmp = ha.Branch.CompareTo(hb.Branch);
                return cmp != 0 ? cmp : ha.Seat.CompareTo(hb.Seat);
            });
            int extra = 0;
            for (int s = 0; s < order.Count; s++)
            {
                int index = order[s];
                var h = plan.Homes[index];
                int home = h.Branch;
                // Capacity, color, and the no-match rule are checked on the
                // board as it stands now, not on the snapshot from Build.
                if (!CanTake(board, home, h.Bird))
                    home = Find(board, h.Bird, false);
                if (home < 0)
                {
                    board.Branches.Add(new BranchState());
                    home = board.Branches.Count - 1;
                    extra++;
                }
                var st = board.Branches[home];
                int seat = st.Count;
                st.Birds.Add(h.Bird);
                st.Shrouded.Add(false);
                st.AlignShroud();
                plan.Homes[index] = new Home { Branch = home, Seat = seat, Bird = h.Bird };
            }
            if (src.IsBonus) src.Broken = false;
            plan.Spawn += extra;
            return plan;
        }

        // Still a legal park: room, not a gift, not asleep, and not a five-match.
        static bool CanTake(Board b, int i, Bird bird)
        {
            if (b == null || (uint)i >= (uint)b.Branches.Count) return false;
            var st = b.Branches[i];
            if (st == null || st.Broken || st.AdLocked || st.IsBonus || st.Free <= 0) return false;
            if (b.IsSleeping(i)) return false;
            if (WouldFill(st, bird)) return false;
            return true;
        }

        static Plan PlaceLost(Board board, List<Bird> flock)
        {
            int baseCount = board.Branches.Count;
            Place(board, flock, allowFull: false, grow: true, out var homes);
            return new Plan
            {
                Break = false,
                Spawn = board.Branches.Count - baseCount,
                Homes = homes ?? new Home[flock.Count]
            };
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
            if (!Place(b, flock, allowFull: false, grow: false, out var homes)) return false;
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
            // A miss opens an empty limb. Never complete a color match from here.
            Place(b, flock, allowFull: false, grow: true, out var homes);
            return new Plan
            {
                Break = false,
                Spawn = b.Branches.Count - baseCount,
                Homes = homes ?? new Home[flock.Count]
            };
        }

        static bool Place(Board b, List<Bird> flock, bool allowFull, bool grow, out Home[] homes)
        {
            homes = new Home[flock.Count];
            for (int i = 0; i < flock.Count; i++)
            {
                int home = Find(b, flock[i], allowFull);
                if (home < 0 && grow)
                {
                    b.Branches.Add(new BranchState());
                    home = b.Branches.Count - 1;
                }
                if (home < 0) return false;
                var st = b.Branches[home];
                int seat = st.Count;
                st.Birds.Add(flock[i]);
                st.Shrouded.Add(false);
                st.AlignShroud();
                homes[i] = new Home { Branch = home, Seat = seat, Bird = flock[i] };
            }
            return true;
        }

        // Occupied limbs of a different color first. Same color and empty limbs
        // are the fallback, and a placement that would complete a color match is skipped.
        static int Find(Board b, Bird bird, bool allowFull)
        {
            int open = -1;
            int openFree = -1;
            int fall = -1;
            int fallFree = -1;
            int fallRank = 9;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var st = b.Branches[i];
                // Same rule as BoardValidator.FindSeat: a bonus perch is the player's.
                // Parking here used to fill a gift the moment it unlocked.
                if (st.Broken || st.AdLocked || st.IsBonus || st.Free <= 0) continue;
                if (b.IsSleeping(i)) continue;
                if (!allowFull && WouldFill(st, bird)) continue;
                bool empty = st.Count == 0;
                bool same = !empty && HoldsColor(st, bird.Color);
                if (!empty && !same)
                {
                    if (st.Free > openFree)
                    {
                        openFree = st.Free;
                        open = i;
                    }
                }
                else
                {
                    int rank = empty ? 1 : 0;
                    if (rank < fallRank || (rank == fallRank && st.Free > fallFree))
                    {
                        fallRank = rank;
                        fallFree = st.Free;
                        fall = i;
                    }
                }
            }
            return open >= 0 ? open : fall;
        }

        static bool HoldsColor(BranchState st, BirdColor color)
        {
            for (int i = 0; i < st.Count; i++)
                if (st.Birds[i].Color == color) return true;
            return false;
        }

        // A full color match clears, sex included or not. Shrouded birds do not.
        static bool WouldFill(BranchState st, Bird bird)
        {
            if (st == null || st.Broken) return false;
            if (st.Count + 1 != BranchState.Cap) return false;
            for (int i = 0; i < st.Count; i++)
            {
                if (st.IsShrouded(i)) return false;
                if (st.Birds[i].Color != bird.Color) return false;
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

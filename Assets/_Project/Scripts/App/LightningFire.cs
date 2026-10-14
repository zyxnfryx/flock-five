using UnityEngine;

namespace FlockFive
{
    // Always on. Every storm bolt rolls a fire. The chance starts at 10% and
    // climbs 5 points after each bolt that does not burn a limb, then snaps
    // back to 10% once a fire lands. There is no switch and no saved pref.
    public static class LightningFire
    {
        public const float StartChance = 0.10f;
        public const float Step = 0.05f;
        public const int SolveCap = 12000;

        // Capture hook. The next Resolve treats the roll as a hit. Target
        // rules and the solvability gate still apply. Consumed on read.
        public static bool Force;

        public struct Result
        {
            public int Index;
            public int Misses;
            public bool Fired;
        }

        public static float Chance(int misses)
        {
            if (misses < 0) misses = 0;
            float c = StartChance + Step * misses;
            return c > 1f ? 1f : c;
        }

        public static bool RollHits(int misses, float unit)
        {
            if (unit < 0f) unit = 0f;
            if (unit > 1f) unit = 1f;
            return unit < Chance(misses);
        }

        public static bool ConsumeForce()
        {
            if (!Force) return false;
            Force = false;
            return true;
        }

        // A hit with nowhere legal to burn is not a fire: the streak keeps climbing.
        public static Result Resolve(Board board, bool[] claimed, bool[] burned, int misses, float roll, bool force)
        {
            if (misses < 0) misses = 0;
            if (!force && !RollHits(misses, roll))
                return Miss(misses);
            int index = Target(board, claimed, burned);
            if (index < 0)
                return Miss(misses);
            return new Result { Index = index, Misses = 0, Fired = true };
        }

        // Unclaimed empty ad gift first, and only when losing it stays winnable.
        // A normal empty limb is considered only when no such gift is waiting.
        // Otherwise -1 and the bolt is just a bolt.
        public static int Target(Board board, bool[] claimed, bool[] burned)
        {
            if (board == null) return -1;
            bool gift = false;
            for (int i = 0; i < board.Branches.Count; i++)
            {
                if (!UnclaimedAd(board, i, claimed, burned)) continue;
                gift = true;
                if (KeepsSolvable(board, i)) return i;
            }
            if (gift) return -1;
            for (int i = 0; i < board.Branches.Count; i++)
            {
                if (!OpenNormal(board.Branches[i])) continue;
                if (KeepsSolvable(board, i)) return i;
            }
            return -1;
        }

        public static bool KeepsSolvable(Board board, int index)
        {
            if (board == null || (uint)index >= (uint)board.Branches.Count) return false;
            var st = board.Branches[index];
            if (st == null) return false;
            if (st.IsBonus)
            {
                if (st.Broken || st.Burned) return false;
            }
            else if (!OpenNormal(st))
            {
                return false;
            }
            var clone = board.Clone();
            Apply(clone.Branches[index]);
            return GardenSolve.Look(clone, SolveCap) == GardenSolve.Outlook.Winnable;
        }

        // Gift: same burn as the hard-mode strike (offer stays unclaimed).
        // Normal: the empty perch is gone. Birds are never on a fire target.
        public static void Apply(BranchState st)
        {
            if (st == null) return;
            if (st.IsBonus)
            {
                LightningStrike.Burn(st);
                return;
            }
            st.Broken = true;
            st.Birds.Clear();
            st.Shrouded.Clear();
        }

        static Result Miss(int misses)
        {
            int next = misses + 1;
            if (next > 80) next = 80;
            return new Result { Index = -1, Misses = next, Fired = false };
        }

        static bool UnclaimedAd(Board board, int i, bool[] claimed, bool[] burned)
        {
            var st = board.Branches[i];
            if (st == null || !st.IsBonus) return false;
            if (st.Broken || st.Burned || !st.AdLocked || st.Count > 0) return false;
            int ord = BonusBranches.Ordinal(board, i);
            if (ord < 0) return false;
            if (claimed != null && ord < claimed.Length && claimed[ord]) return false;
            if (burned != null && ord < burned.Length && burned[ord]) return false;
            return true;
        }

        static bool OpenNormal(BranchState st)
        {
            return st != null && !st.IsBonus && !st.Broken && !st.Burned && !st.AdLocked && st.Count == 0;
        }
    }
}

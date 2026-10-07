namespace FlockFive
{
    // Two bottom gifts per stage. Claims are ordinals, not branch indexes,
    // so a rebuilt board still unlocks the same limbs.
    public static class BonusBranches
    {
        public static int CountOn(Board b)
        {
            if (b == null) return 0;
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
                if (b.Branches[i].IsBonus) n++;
            return n;
        }

        public static int Ordinal(Board b, int branch)
        {
            if (b == null) return -1;
            int n = 0;
            int last = branch < b.Branches.Count ? branch : b.Branches.Count;
            for (int i = 0; i < last; i++)
                if (b.Branches[i].IsBonus) n++;
            return n;
        }

        // claimed[ordinal] stays set for the stage. Restart clones the locked
        // seed, then this opens only the gifts already earned.
        public static void ApplyClaims(Board b, bool[] claimed)
        {
            if (b == null || claimed == null) return;
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                if (!b.Branches[i].IsBonus) continue;
                if (n < claimed.Length && claimed[n])
                    OpenEmpty(b.Branches[i]);
                n++;
            }
        }

        // A claimed gift is a new perch. Wipe anything a park, a restore,
        // or a reused limb left on it. Only a player hop fills it after this.
        public static void OpenEmpty(BranchState st)
        {
            if (st == null) return;
            st.AdLocked = false;
            st.Broken = false;
            st.Birds.Clear();
            st.Shrouded.Clear();
        }

        // Freeze continue pays for a finished view. Ads switched off use the
        // same free grant as the gift sign. A cancel, or a show that never
        // played, pays nothing.
        public static bool ContinuePays(bool earned, bool granted, bool adsEnabled)
        {
            if (earned) return true;
            return !adsEnabled && granted;
        }

        // The gift a continue (or a sign tap) should open. A preferred limb
        // that is still locked wins. A stale or already-claimed one falls
        // through to the next locked gift. -1 when none is left.
        public static int ContinueIndex(Board b, bool[] claimed, int preferred)
        {
            if (LockedGift(b, claimed, preferred)) return preferred;
            if (b == null) return -1;
            for (int i = 0; i < b.Branches.Count; i++)
                if (LockedGift(b, claimed, i)) return i;
            return -1;
        }

        // 1 = a new empty perch, 0 = that gift was already claimed (lock cleared),
        // -1 = nothing to open. The view shake and burst stay with the caller.
        public static int Claim(Board b, bool[] claimed, int i)
        {
            if (b == null || claimed == null || (uint)i >= (uint)b.Branches.Count) return -1;
            var st = b.Branches[i];
            if (st == null || !st.IsBonus) return -1;
            int ord = Ordinal(b, i);
            if (ord < 0 || ord >= claimed.Length) return -1;
            if (claimed[ord])
            {
                st.AdLocked = false;
                st.Broken = false;
                return 0;
            }
            claimed[ord] = true;
            OpenEmpty(st);
            return 1;
        }

        static bool LockedGift(Board b, bool[] claimed, int i)
        {
            if (b == null || claimed == null || (uint)i >= (uint)b.Branches.Count) return false;
            var st = b.Branches[i];
            if (st == null || !st.IsBonus || !st.AdLocked || st.Broken) return false;
            int ord = Ordinal(b, i);
            if (ord < 0 || ord >= claimed.Length || claimed[ord]) return false;
            return true;
        }
    }
}

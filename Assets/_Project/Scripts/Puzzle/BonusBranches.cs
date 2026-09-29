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
                {
                    b.Branches[i].AdLocked = false;
                    b.Branches[i].Broken = false;
                }
                n++;
            }
        }
    }
}

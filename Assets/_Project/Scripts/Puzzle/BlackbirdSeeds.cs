namespace FlockFive
{
    // Small 5-color witnesses for the edit-mode suite. Seed 0 is the canonical
    // line (winnable with the spare locked). Even seeds win, odd seeds are traps,
    // so a pair is 50/50 and the odds are the trap.
    public static class BlackbirdSeeds
    {
        public const int Canonical = 0;

        public static Board Make(int seed)
        {
            if ((seed & 1) == 1) return Trap(seed);
            return CanonicalBoard(seed);
        }

        public static Board CanonicalBoard(int seed)
        {
            var rot = (BirdColor)((seed / 2) % Palette.Max);
            var b = new Board();
            b.SetFeeders(2);
            b.Live[0] = Shift(BirdColor.Ruby, rot);
            b.Live[1] = Shift(BirdColor.Gold, rot);
            b.Queue.Add(Shift(BirdColor.Teal, rot));
            b.Queue.Add(Shift(BirdColor.Violet, rot));
            b.Queue.Add(Shift(BirdColor.Peach, rot));
            b.Branches.Add(Stack(Paint(BirdColor.Ruby, rot), 4));
            b.Branches.Add(Stack(Paint(BirdColor.Gold, rot), 4));
            b.Branches.Add(Stack(Paint(BirdColor.Teal, rot), 4));
            b.Branches.Add(Stack(Paint(BirdColor.Violet, rot), 4));
            b.Branches.Add(Stack(Paint(BirdColor.Peach, rot), 4));
            // Tip is Ruby's fifth, then Gold, Teal, Violet, Peach toward the trunk.
            var mix = new BranchState();
            Add(mix, Paint(BirdColor.Peach, rot));
            Add(mix, Paint(BirdColor.Violet, rot));
            Add(mix, Paint(BirdColor.Teal, rot));
            Add(mix, Paint(BirdColor.Gold, rot));
            Add(mix, Paint(BirdColor.Ruby, rot));
            b.Branches.Add(mix);
            BlackbirdRival.Dress(b, 0);
            return b;
        }

        // Birds on the board, every tip leaf-locked, spare sealed. No legal hop.
        public static Board Trap(int seed)
        {
            var b = CanonicalBoard(seed & ~1);
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br.IsSpare)
                {
                    br.AdLocked = true;
                    continue;
                }
                if (br.RivalPerch || br.Count == 0) continue;
                br.AlignShroud();
                br.Shrouded[br.Count - 1] = true;
            }
            return b;
        }

        static Bird Paint(BirdColor color, BirdColor rot) => new Bird(Shift(color, rot), BirdKit.Plain);

        static BirdColor Shift(BirdColor c, BirdColor by)
        {
            int n = Palette.Shipped;
            if (n < 1) n = 1;
            return (BirdColor)(((int)c + (int)by) % n);
        }

        static BranchState Stack(Bird bird, int n)
        {
            var br = new BranchState();
            for (int i = 0; i < n; i++) Add(br, bird);
            return br;
        }

        static void Add(BranchState br, Bird bird)
        {
            br.Birds.Add(bird);
            br.Shrouded.Add(false);
        }
    }
}

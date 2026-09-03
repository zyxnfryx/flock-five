namespace FlockFive
{
    public static partial class LevelData
    {
        // Seed puzzles stay 8 limbs. Drop one trailing empty, clone the rest
        // with a +1 color shift so Violet's copy is Peach. Clone flocks need
        // their own feeder visits, so the shifted live+queue is appended.
        // Never stuff Peach into an empty Live slot (pinched stays pinched).
        static Board Pack(Board src)
        {
            var b = new Board();
            b.BreezeOnCollect = src.BreezeOnCollect;
            b.Live[0] = src.Live[0];
            b.Live[1] = src.Live[1];
            b.Queue.AddRange(src.Queue);

            int keep = src.Branches.Count;
            if (keep > 0 && src.Branches[keep - 1].Empty)
                keep--;
            if (keep < 1) keep = src.Branches.Count;

            for (int i = 0; i < keep; i++)
                b.Branches.Add(src.Branches[i].Clone());
            for (int i = 0; i < keep; i++)
                b.Branches.Add(ShiftClone(src.Branches[i]));

            EnqueueShiftedFeeders(src, b);
            OfferPeach(b);
            return b;
        }

        static BranchState ShiftClone(BranchState src)
        {
            var br = new BranchState { Broken = src.Broken };
            for (int i = 0; i < src.Birds.Count; i++)
                br.Birds.Add(Shift(src.Birds[i]));
            br.Shrouded.AddRange(src.Shrouded);
            br.AlignShroud();
            return br;
        }

        static BirdColor Shift(BirdColor c)
        {
            int n = Palette.Shipped;
            if (n < 1) n = 1;
            return (BirdColor)(((int)c + 1) % n);
        }

        static void EnqueueShiftedFeeders(Board src, Board b)
        {
            Extra(src.Live[0], b);
            Extra(src.Live[1], b);
            for (int i = 0; i < src.Queue.Count; i++)
                Extra(src.Queue[i], b);
        }

        static void Extra(BirdColor? c, Board b)
        {
            if (!c.HasValue) return;
            b.Queue.Add(Shift(c.Value));
        }

        static void OfferPeach(Board b)
        {
            if (!HasColor(b, BirdColor.Peach)) return;
            if (b.LiveHas(BirdColor.Peach)) return;
            for (int i = 0; i < b.Queue.Count; i++)
                if (b.Queue[i] == BirdColor.Peach) return;
            b.Queue.Add(BirdColor.Peach);
        }

        static bool HasColor(Board b, BirdColor c)
        {
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                for (int k = 0; k < br.Birds.Count; k++)
                    if (br.Birds[k] == c) return true;
            }
            return false;
        }
    }
}

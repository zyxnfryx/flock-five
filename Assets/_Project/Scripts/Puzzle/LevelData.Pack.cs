namespace FlockFive
{
    public static partial class LevelData
    {
        // Seed puzzles stay 8 limbs. Drop one trailing empty, clone the rest
        // with a +1 color shift so Violet's copy is Peach. Clone flocks need
        // their own feeder visits, so the shifted live+queue is appended.
        // Never stuff Peach into an empty Live slot (pinched stays pinched).
        static Board Pack(Board src, Level level = null)
        {
            bool solo = level != null && level.Solo;
            var b = new Board();
            b.BreezeOnCollect = src.BreezeOnCollect;
            b.CopyFeedersFrom(src);
            b.Queue.AddRange(src.Queue);

            int keep = src.Branches.Count;
            if (keep > 0 && src.Branches[keep - 1].Empty)
                keep--;
            if (keep < 1) keep = src.Branches.Count;

            if (solo)
            {
                // Single-color garden: the seed is the garden. No clones, no
                // shifted feeders, no Peach call; kits come from the maker.
                for (int i = 0; i < src.Branches.Count; i++)
                    b.Branches.Add(src.Branches[i].Clone());
            }
            else
            {
                for (int i = 0; i < keep; i++)
                    b.Branches.Add(src.Branches[i].Clone());
                for (int i = 0; i < keep; i++)
                    b.Branches.Add(ShiftClone(src.Branches[i]));
                EnqueueShiftedFeeders(src, b);
                OfferPeach(b);
                FlockKit.Assign(b, level != null ? level.Number : 0, level != null ? level.Kits : null);
            }
            // Kits are identity now: a bee over its own flock is not hiding anything.
            UnhideSameColorBees(b);
            EnforceFullLeafLimbs(b, level);
            OfferGift(b);
            return b;
        }

        // Build 89 RULE A (QA): only a FULL limb (BranchState.Cap birds) may carry leaf cover.
        // A limb with empty seats never shows leaves, so its birds are never half-covered. A
        // leaf-locked limb cannot be hopped onto (Board hop rules), so a limb that starts full stays
        // full until its leaf lifts. Authored short locks lose the lock (the tip bird is simply
        // visible); no bird is added, so flock counts and feeder queues are untouched.
        public static bool LeafRuleOn = true;      // test switch: false = the pre-b89 data
        public static string LastLeafDrops = "";   // "limb3(4/5) limb7(3/5)" for the last Pack

        static void EnforceFullLeafLimbs(Board b, Level level)
        {
            LastLeafDrops = "";
            if (b == null || !LeafRuleOn) return;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (!br.TipLocked || br.Count >= BranchState.Cap) continue;
                LastLeafDrops += (LastLeafDrops.Length > 0 ? " " : "") + "limb" + i + "(" + br.Count + "/" + BranchState.Cap + ")";
                br.Shrouded[br.Count - 1] = false;
            }
        }

        // The invariant, for tests and tools: every leaf-locked limb is a full limb.
        public static bool LeafLockedLimbsAreFull(Board b)
        {
            if (b == null) return true;
            for (int i = 0; i < b.Branches.Count; i++)
                if (!LeafLockFullLimb(b.Branches[i])) return false;
            return true;
        }

        // The per-limb rule, one place: a limb may show leaves only if it holds exactly Cap
        // birds (an unlocked limb has no leaves, so it passes). BranchView and the tests use it.
        public static bool LeafLockFullLimb(BranchState br) =>
            br == null || !br.TipLocked || br.Count == BranchState.Cap;

        static void OfferGift(Board b)
        {
            // Two bottom gifts. Each stays locked until its own ad.
            b.Branches.Add(new BranchState { AdLocked = true, IsBonus = true });
            b.Branches.Add(new BranchState { AdLocked = true, IsBonus = true });
        }

        static BranchState ShiftClone(BranchState src)
        {
            var br = new BranchState { Broken = src.Broken };
            for (int i = 0; i < src.Birds.Count; i++)
            {
                var bird = src.Birds[i];
                bird.Color = Shift(bird.Color);
                br.Birds.Add(bird);
            }
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
            if (src.Live != null)
            {
                for (int i = 0; i < src.Live.Length; i++)
                    Extra(src.Live[i], b);
            }
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
                    if (br.Birds[k].Color == c) return true;
            }
            return false;
        }
    }
}

namespace FlockFive
{
    // Gardens 33-37 (Build 111). Authored by the garden generator (seed + Pack, run-order kits) and
    // verified with GardenSolve (Winnable, no gift) and TrapProbe. Genseeds are recorded per maker.
    public static partial class LevelData
    {
        // gen seed 22033, solver 195/195 moves/nodes, sensible tangle 50%
        static Board Garden33()
        {
            var b = Eight(
                Row(BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Violet, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Teal),
                new BranchState());
            b.Live[0] = BirdColor.Teal;
            b.Live[1] = BirdColor.Ruby;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[3], 1);
            HideInner(b.Branches[4], 2);
            return Prep(b);
        }

        // gen seed 2052, solver 36/36 moves/nodes, sensible tangle 50%
        static Board Garden34()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Gold, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Ruby, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Teal, BirdColor.Teal),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[0], 1);
            HideInner(b.Branches[4], 3);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[1]);
            return b;
        }

        // gen seed 3088, solver 773/773 moves/nodes, sensible tangle 6%
        static Board Garden35()
        {
            var b = Eight(
                Row(BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Gold, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Ruby, BirdColor.Gold),
                new BranchState());
            b.Live[0] = BirdColor.Teal;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[3], 3);
            return Prep(b);
        }

        // gen seed 4006, solver 3544/3544 moves/nodes, sensible tangle 50%
        static Board Garden36()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Teal, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Gold),
                Row(BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Gold, BirdColor.Violet),
                new BranchState());
            b.Live[0] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[1], 1);
            HideInner(b.Branches[4], 1);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            return b;
        }

        // gen seed 5067, solver 89/89 moves/nodes, sensible tangle 50%
        static Board Garden37()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Teal, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Teal, BirdColor.Ruby, BirdColor.Teal, BirdColor.Violet),
                new BranchState());
            b.Live[0] = BirdColor.Teal;
            b.Live[1] = BirdColor.Violet;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[5], 2);
            HideInner(b.Branches[2], 1);
            HideInner(b.Branches[6], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            return b;
        }

    }
}

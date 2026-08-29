namespace FlockFive
{
    public sealed class Level
    {
        public int Number;
        public string Id;
        public string Title;
        public System.Func<Board> Make;
    }

    public static class LevelData
    {
        public static int Index { get; private set; }
        public static Level Current { get; private set; }

        static readonly Level[] All =
        {
            new Level { Number = 1, Id = "dawn-garden", Title = "Dawn Garden", Make = DawnGarden },
            new Level { Number = 2, Id = "bee-thicket", Title = "Bee Thicket", Make = BeeThicket },
            new Level { Number = 3, Id = "noon-queue", Title = "Noon Queue", Make = NoonQueue },
            new Level { Number = 4, Id = "dusk-scatter", Title = "Dusk Scatter", Make = DuskScatter },
            new Level { Number = 5, Id = "moonrise-nap", Title = "Moonrise Nap", Make = MoonriseNap },
            new Level { Number = 6, Id = "night-lattice", Title = "Night Lattice", Make = NightLattice },
            new Level { Number = 7, Id = "dew-arcade", Title = "Dew Arcade", Make = DewArcade },
            new Level { Number = 8, Id = "pollen-court", Title = "Pollen Court", Make = PollenCourt },
            new Level { Number = 9, Id = "hive-porch", Title = "Hive Porch", Make = HivePorch },
            new Level { Number = 10, Id = "amber-grove", Title = "Amber Grove", Make = AmberGrove },
            new Level { Number = 11, Id = "sun-hive", Title = "Sun Hive", Make = SunHive },
            new Level { Number = 12, Id = "thistle-well", Title = "Thistle Well", Make = ThistleWell },
            new Level { Number = 13, Id = "nectar-pinch", Title = "Nectar Pinch", Make = NectarPinch },
            new Level { Number = 14, Id = "twin-swarm", Title = "Twin Swarm", Make = TwinSwarm },
            new Level { Number = 15, Id = "last-light", Title = "Last Light", Make = LastLight }
        };

        public static int Count => All.Length;
        public static bool HasNext => Index + 1 < All.Length;

        public static Board Slice() => Open(0);

        public static Board Open(int index)
        {
            if (All.Length == 0) return new Board();
            Index = index < 0 ? 0 : (index >= All.Length ? All.Length - 1 : index);
            Current = All[Index];
            return Current.Make();
        }

        static Board Eight(params BranchState[] filled)
        {
            var b = new Board();
            for (int i = 0; i < 8; i++)
                b.Branches.Add(i < filled.Length ? filled[i] : new BranchState());
            return b;
        }

        static Board Prep(Board b, bool keepTipShrouds = false)
        {
            for (int i = 0; i < b.Branches.Count; i++)
            {
                if (b.Branches[i].IsFullMatch(out _))
                    b.Branches[i].Birds.RemoveAt(b.Branches[i].Birds.Count - 1);
                b.Branches[i].AlignShroud();
                if (!keepTipShrouds && b.Branches[i].Count > 0)
                    b.Branches[i].Shrouded[b.Branches[i].Count - 1] = false;
            }
            return b;
        }

        static Board DawnGarden()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Gold;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[3], 2);
            return Prep(b);
        }

        static Board BeeThicket()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[1], 2);
            HideInner(b.Branches[2], 1);
            HideInner(b.Branches[3], 1);
            return Prep(b);
        }

        static Board NoonQueue()
        {
            var b = Eight(
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Teal;
            b.Live[1] = BirdColor.Violet;
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[1], 2);
            return Prep(b);
        }

        static Board DuskScatter()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Ruby, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Violet;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Teal);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[1], 2);
            return Prep(b);
        }

        // Pair stacks plus a mixed row. Two empties. Live Gold + Violet so
        // finishing Ruby/Teal first puts them to sleep until a feeder hangs.
        static Board MoonriseNap()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Violet;
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Teal);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[1], 2);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 2);
            return Prep(b);
        }

        // Six occupied limbs, two empty. Mixed threes and fours. Live Teal + Ruby.
        static Board NightLattice()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Teal;
            b.Live[1] = BirdColor.Ruby;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[0], 3);
            HideInner(b.Branches[1], 3);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 2);
            return Prep(b);
        }

        // Five fours, three empties. Cyclic mix, heavier bees than Night Lattice.
        static Board DewArcade()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby, BirdColor.Gold),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Violet;
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Teal);
            HideInner(b.Branches[0], 3);
            HideInner(b.Branches[1], 3);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 2);
            HideInner(b.Branches[4], 3);
            return Prep(b);
        }

        // Six occupied limbs. Pair-runs plus leftovers. Live Teal + Gold.
        static Board PollenCourt()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Ruby, BirdColor.Gold),
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Gold),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Teal;
            b.Live[1] = BirdColor.Gold;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[0], 3);
            HideInner(b.Branches[1], 3);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 2);
            HideInner(b.Branches[4], 2);
            return Prep(b);
        }

        // Dawn-shaped rows with one stubborn Gold tip. Ruby still hoppable.
        // Flock Ruby and the breeze lifts the Gold swarm.
        static Board HivePorch()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Teal, BirdColor.Ruby),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Gold;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Teal);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[2], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[1]);
            return b;
        }

        // Two stubborn tips (Gold + Violet stacks). Ruby still hoppable.
        static Board AmberGrove()
        {
            var b = Eight(
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Teal, BirdColor.Ruby),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 1);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            HideTip(b.Branches[1]);
            return b;
        }

        // Two stubborn mixed tips, denser inner bees. Live Gold hoppable.
        static Board SunHive()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[1], 2);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[1]);
            HideTip(b.Branches[2]);
            return b;
        }

        // One live feeder. Inner bees, no stubborn tip. Ruby hoppable.
        static Board ThistleWell()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Gold),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = null;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[1], 2);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 2);
            return Prep(b);
        }

        // One feeder plus one stubborn Gold tip. Ruby still hoppable.
        static Board NectarPinch()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = null;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[1]);
            return b;
        }

        // Pinched nectar, two stubborn tips. Gold hoppable on turn 1.
        static Board TwinSwarm()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Teal, BirdColor.Gold),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = null;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[2], 1);
            HideInner(b.Branches[3], 1);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            HideTip(b.Branches[1]);
            return b;
        }

        // Both mechanics, mixed fours, two stubborn tips, one feeder. Not cruel.
        static Board LastLight()
        {
            var b = Eight(
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Ruby, BirdColor.Gold, BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Ruby, BirdColor.Violet),
                new BranchState(),
                new BranchState(),
                new BranchState());
            b.Live[0] = BirdColor.Violet;
            b.Live[1] = null;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[1], 2);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            HideTip(b.Branches[2]);
            return b;
        }

        static BranchState Row(params BirdColor[] birds)
        {
            var br = new BranchState();
            for (int i = 0; i < birds.Length; i++)
            {
                br.Birds.Add(birds[i]);
                br.Shrouded.Add(false);
            }
            return br;
        }

        static void HideInner(BranchState br, int count)
        {
            br.AlignShroud();
            int n = count;
            if (n > br.Count - 1) n = br.Count - 1;
            if (n < 0) n = 0;
            for (int i = 0; i < n; i++)
                br.Shrouded[i] = true;
        }

        static void HideTip(BranchState br)
        {
            br.AlignShroud();
            if (br.Count > 0)
                br.Shrouded[br.Count - 1] = true;
        }
    }
}

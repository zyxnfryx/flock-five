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
            new Level { Number = 4, Id = "dusk-scatter", Title = "Dusk Scatter", Make = DuskScatter }
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

        static Board Prep(Board b)
        {
            for (int i = 0; i < b.Branches.Count; i++)
            {
                if (b.Branches[i].IsFullMatch(out _))
                    b.Branches[i].Birds.RemoveAt(b.Branches[i].Birds.Count - 1);
                b.Branches[i].AlignShroud();
                if (b.Branches[i].Count > 0)
                    b.Branches[i].Shrouded[b.Branches[i].Count - 1] = false;
            }
            return b;
        }

        // Combo seed: live feeders are Ruby + Gold, queue is Teal then Violet.
        // Completing Teal or Violet first puts them to sleep. Collecting Ruby
        // (or Gold) then hangs the next feeder and those nappers fly — combo.
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

        // Shorter stacks, more bees. Live Gold + Teal so the obvious Ruby/Violet
        // five-stacks are naps until a feeder hangs.
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

        // Same almost-full rows as dawn, feeders flipped: live Teal + Violet.
        // Ruby/Gold flocks nap first; hanging Teal/Violet can combo them awake.
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

        // No almost-full color rows. Mixed stacks, two bees on the first two limbs,
        // three empty perches. Live Ruby + Violet.
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
    }
}

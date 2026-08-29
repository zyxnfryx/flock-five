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
            new Level { Number = 1, Id = "dawn-garden", Title = "Dawn Garden", Make = DawnGarden }
        };

        public static Board Slice() => Open(0);

        public static Board Open(int index)
        {
            if (All.Length == 0) return new Board();
            Index = index < 0 ? 0 : (index >= All.Length ? All.Length - 1 : index);
            Current = All[Index];
            return Current.Make();
        }

        // Combo seed: live feeders are Ruby + Gold, queue is Teal then Violet.
        // Completing Teal or Violet first puts them to sleep. Collecting Ruby
        // (or Gold) then hangs the next feeder and those nappers fly — combo.
        static Board DawnGarden()
        {
            var b = new Board();
            b.Branches.Add(Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby));
            b.Branches.Add(Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold));
            b.Branches.Add(Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal));
            b.Branches.Add(Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet));
            b.Branches.Add(Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby));
            b.Branches.Add(new BranchState());
            b.Branches.Add(new BranchState());
            b.Branches.Add(new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Gold;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);

            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[3], 2);

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

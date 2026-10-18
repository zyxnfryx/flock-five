namespace FlockFive
{
    public sealed class Level
    {
        public int Number;
        public string Id;
        public string Title;
        public System.Func<Board> Make;
        // Single-color garden: Pack skips Shift clones, shifted feeders and OfferPeach,
        // and keeps the kits the maker painted (FlockKit.Paint).
        public bool Solo;
        // Authored kits for the packed garden (FlockKit.PaintMap format). Null = FlockKit run order.
        public string Kits;
        // 1-3 feeders. 2 is every current garden. An ostrich stage uses 3.
        public int Feeders = 2;
        // Present from the open. Not a timed sparrow or hawk visit.
        public bool Ostrich;
        public int PeckSeed;
        // 0 keeps OstrichState.DefaultPecks (2). A positive value overrides.
        public int PecksPerMove;
    }

    public static partial class LevelData
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
            new Level { Number = 15, Id = "last-light", Title = "Last Light", Make = LastLight },
            new Level { Number = 16, Id = "garden-16", Title = "Garden 16", Make = Garden16,
                Kits = "NCCCB/NNNCN/BBNBB/NNNCN/CBBB/CCNN/BC/CCNBB/BBBBN/BBCCC/BBNCC/NNNN/CCCC/BN" },
            new Level { Number = 17, Id = "garden-17", Title = "Garden 17", Make = Garden17,
                Kits = "CCCCN/NNNN/CCCNN/CCCNN/CCCCN/B/NNBBB/CBBBB/NBBB/BCNNC/NBBNC/CNNBB/N/BBCBB" },
            new Level { Number = 18, Id = "garden-18", Title = "Garden 18", Make = Garden18,
                Kits = "NBB/BBCBC/CCCCB/CCCBC/BBCBB/NCC/CCCC/NCN/CNNNB/BNNBN/NNNNC/NNBBN/NNN/BBBB" },
            new Level { Number = 19, Id = "garden-19", Title = "Garden 19", Make = Garden19,
                Kits = "BBBCN/BBBNC/NCCB/BNBB/BBBN/NCCN/BBBB/CCCNN/BCCCC/BCCN/CNNN/NNNC/BCCC/NNNN" },
            new Level { Number = 20, Id = "garden-20", Title = "Garden 20", Make = Garden20,
                Kits = "N/CCCCB/NCBBB/NNNNN/CBBB/CBCBB/CNBNB/B/NNNNN/BCCCN/CCCCN/CCCC/CCCNC/CBNNN" },
            new Level { Number = 21, Id = "garden-21", Title = "Garden 21", Make = Garden21,
                Kits = "NNCNC/CCB/NBCCB/CCC/NBBB/CCCBB/BNCNC/NNBCC/BNN/CNNBC/BBB/NNBN/BBBNB/CNCNN" },
            new Level { Number = 22, Id = "garden-22", Title = "Garden 22", Make = Garden22,
                Kits = "CNNCC/BBBBB/CCCNN/NNNCB/BBCCC/NBNBC///NNNNC/CCNCC/CBBCN/NNNCN/CBBBB/BBBBN//" },
            new Level { Number = 23, Id = "garden-23", Title = "Garden 23", Make = Garden23 },
            new Level { Number = 24, Id = "garden-24", Title = "Garden 24", Make = Garden24,
                Kits = "NCBCN/CBBNB/NCNCB/BCBCC/NCCC/NBB/BBC//NCCCN/NNNNB/NBCBB/CCBNN/CNBB/NNN/BCB/" },
            new Level { Number = 25, Id = "garden-25", Title = "Garden 25", Make = Garden25,
                Kits = "NNBN/CNCB/BBCN/BBBB/BNNBB/CCCC/CCNCB/CBNN/NBNN/NNNC/NNNN/NCCBN/BBBB/NNBNC" },
            new Level { Number = 26, Id = "garden-26", Title = "Garden 26", Make = Garden26, Solo = true },
            // Build 83: ostrich debut (3 feeders) at 27; b82's solo 27-29 move to 28-30 unchanged.
            new Level { Number = 27, Id = "garden-27", Title = "Garden 27", Make = Garden27Ostrich, Solo = true,
                Feeders = 3, Ostrich = true, PeckSeed = 686559 },
            new Level { Number = 28, Id = "garden-28", Title = "Garden 28", Make = Garden27, Solo = true },
            new Level { Number = 29, Id = "garden-29", Title = "Garden 29", Make = Garden28, Solo = true },
            new Level { Number = 30, Id = "garden-30", Title = "Garden 30", Make = Garden29, Solo = true },
            new Level { Number = 31, Id = "garden-31", Title = "Garden 31", Make = Garden31 },
            // Build 110: garden 32 is where the backdrop turns to spring, so the ostrich no
            // longer debuts again there. Same authored board and 3 feeders, no ostrich.
            // Ostrich gardens on the ladder: 27. Next ostrich garden: 38 (Garden38Level, not wired yet).
            new Level { Number = 32, Id = "garden-32", Title = "Garden 32", Make = Garden32Ostrich, Solo = true,
                Feeders = 3, Ostrich = false, PeckSeed = 832449 },
            // Build 111: gardens 33-37 (LevelData.Ladder33.cs) and the ostrich's second garden, 38.
            // 36 is a honey badger garden (plain board). 38 is not a badger or season-change level.
            new Level { Number = 33, Id = "garden-33", Title = "Garden 33", Make = Garden33 },
            new Level { Number = 34, Id = "garden-34", Title = "Garden 34", Make = Garden34 },
            new Level { Number = 35, Id = "garden-35", Title = "Garden 35", Make = Garden35 },
            new Level { Number = 36, Id = "garden-36", Title = "Garden 36", Make = Garden36 },
            new Level { Number = 37, Id = "garden-37", Title = "Garden 37", Make = Garden37 },
            new Level { Number = 38, Id = "garden-38", Title = "Garden 38", Make = Garden38Ostrich, Solo = true,
                Feeders = 3, Ostrich = true, PeckSeed = 879204 },
            new Level { Number = 39, Id = "garden-39", Title = "Garden 39", Make = Garden39 },
            new Level { Number = 40, Id = "garden-40", Title = "Garden 40", Make = Garden40 },
            new Level { Number = 41, Id = "garden-41", Title = "Garden 41", Make = Garden41 },
            new Level { Number = 42, Id = "garden-42", Title = "Garden 42", Make = Garden42 },
            new Level { Number = 43, Id = "garden-43", Title = "Garden 43", Make = Garden43Ostrich, Solo = true,
                Feeders = 3, Ostrich = true, PeckSeed = 304115 },
            new Level { Number = 44, Id = "garden-44", Title = "Garden 44", Make = Garden44 },
            new Level { Number = 45, Id = "garden-45", Title = "Garden 45", Make = Garden45 },
            new Level { Number = 46, Id = "garden-46", Title = "Garden 46", Make = Garden46 },
            new Level { Number = 47, Id = "garden-47", Title = "Garden 47", Make = Garden47 },
            new Level { Number = 48, Id = "garden-48", Title = "Garden 48", Make = Garden48 },
            new Level { Number = 49, Id = "garden-49", Title = "Garden 49", Make = Garden49 },
            new Level { Number = 50, Id = "garden-50", Title = "Garden 50", Make = Garden50 }
        };

        // Garden 38 (ostrich + bees + leaf-lock) is the ladder entry numbered 38.
        public static Level Garden38Level => Peek(37);

        // Authored ostrich boards (ostrich-impl.txt), notation shared with the C# harness and
        // ostrich.py: limbs ' / ' trunk->tip; bird = [?=shrouded]Color(RGTVP)+Kit(NCB); '-' empty.
        public const string Garden27Spec = "VCVCTCRC / VCTBRCTB / GCTC / GCRCGCTB / GCTCGCRC / TCTCTB / TBVCRCVC | live=GTT queue=RV";
        public const string Garden32Spec = "RBVCTCRB / ?TCGC / VCGCGC / VCVCVC / RBTBRCTB / GCTCRCRB / TBRCRB / RCGCTC / ?TBRCTCTB / - | live=TTV queue=GRR";
        public const string Garden38Spec = "TBRN / TBGCTB / GCGC / GCTBVC / VCRNVCVC?RN / ?TB?RNTNTN / TNTN / ?TNGCRNVC / - | live=TTG queue=VR breeze";

        static Board Garden27Ostrich() => ParseAuthored(Garden27Spec);
        static Board Garden32Ostrich() => ParseAuthored(Garden32Spec);
        public const string Garden43Spec = "GNRC / ?GCRBRNRC / GNRC / RNGC / ?RBRNRNRNGC / RBRCGNRCRB / RBGNGC / GCGN / - | live=RRG queue=RG";
        static Board Garden43Ostrich() => ParseAuthored(Garden43Spec);
        static Board Garden38Ostrich() => ParseAuthored(Garden38Spec);

        public static Board ParseAuthored(string spec)
        {
            const string C = "RGTVP", K = "NCB";
            var parts = spec.Split('|');
            var b = new Board();
            foreach (var limb in parts[0].Split(new[] { " / " }, System.StringSplitOptions.None))
            {
                var br = new BranchState();
                var t = limb.Trim();
                if (t != "-")
                {
                    for (int i = 0; i < t.Length;)
                    {
                        bool hid = t[i] == '?';
                        if (hid) i++;
                        br.Birds.Add(new Bird((BirdColor)C.IndexOf(t[i]), (BirdKit)K.IndexOf(t[i + 1])));
                        br.Shrouded.Add(hid);
                        i += 2;
                    }
                }
                b.Branches.Add(br);
            }
            string tail = parts.Length > 1 ? parts[1] : "";
            string live = Field(tail, "live=");
            string queue = Field(tail, "queue=");
            b.SetFeeders(live.Length > 0 ? live.Length : 2);
            for (int i = 0; i < live.Length; i++) b.Live[i] = (BirdColor)C.IndexOf(live[i]);
            for (int i = 0; i < queue.Length; i++) b.Queue.Add((BirdColor)C.IndexOf(queue[i]));
            b.BreezeOnCollect = tail.Contains("breeze");
            return b;
        }

        static string Field(string tail, string key)
        {
            int at = tail.IndexOf(key, System.StringComparison.Ordinal);
            if (at < 0) return "";
            at += key.Length;
            int end = tail.IndexOf(' ', at);
            return end < 0 ? tail.Substring(at).Trim() : tail.Substring(at, end - at);
        }

        // Badger + spring garden: no new mechanic. Two live feeders, bees on one limb. Gold calls twice.
        static Board Garden31()
        {
            var b = Eight(
                Row(BirdColor.Gold, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Violet, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Gold),
                Row(BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Ruby),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[5], 3);
            return Prep(b);
        }

        // Test hook: Garden 38 packed the same way Open packs a ladder level.
        public static Board Open38Test() => Pack(Garden38Level.Make(), Garden38Level);

        public static int Count => All.Length;
        public static bool HasNext => All.Length > 0;
        const string PrefNext = "flockfive.next";

        // Climb is unbounded. Layouts cycle through every garden (29 in build 82).
        // The flower shows LEVEL 16, 17, … instead of wrapping to Dawn Garden as level 1.
        public static int DisplayNumber => Index + 1;

        public static int NextPlay
        {
            get
            {
                int n = UnityEngine.PlayerPrefs.GetInt(PrefNext, 0);
                return n < 0 ? 0 : n;
            }
        }

        public static Level Peek(int index)
        {
            if (All.Length == 0) return null;
            if (index < 0) index = 0;
            return All[index % All.Length];
        }

        public static void RememberClear()
        {
            UnityEngine.PlayerPrefs.SetInt(PrefNext, Index + 1);
            UnityEngine.PlayerPrefs.Save();
        }

        const string PrefJoke = "flockfive.joke.v2.";
        static readonly string[] JokeIntro =
        {
            "",
            "Easy",
            "",
            "Super Easy",
            "",
            "Super Duper Easy"
        };
        static readonly string[] JokePool =
        {
            "Easy",
            "Super Easy",
            "Super Duper Easy"
        };

        public static string JokeEase(int index)
        {
            if (index < 0) index = 0;
            if (index < JokeIntro.Length) return JokeIntro[index];
            string key = PrefJoke + index;
            if (UnityEngine.PlayerPrefs.HasKey(key))
                return UnityEngine.PlayerPrefs.GetString(key, "");
            string prev = JokeEase(index - 1);
            string prev2 = JokeEase(index - 2);
            string pick = RollJoke(prev, prev2);
            UnityEngine.PlayerPrefs.SetString(key, pick);
            UnityEngine.PlayerPrefs.Save();
            return pick;
        }

        static string RollJoke(string prev, string prev2)
        {
            bool blank = UnityEngine.Random.value < 0.55f;
            string pick = blank ? "" : JokePool[UnityEngine.Random.Range(0, JokePool.Length)];
            if (pick.Length > 0 && pick == prev && pick == prev2)
            {
                int k = UnityEngine.Random.Range(0, JokePool.Length);
                pick = JokePool[k] == pick ? "" : JokePool[k];
            }
            return pick;
        }

        public static Board Slice() => Open(0);

        public static Board Open(int index)
        {
            if (All.Length == 0) return new Board();
            Index = index < 0 ? 0 : index;
            Current = All[Index % All.Length];
            var b = Pack(Current.Make(), Current);
            // Garden 16 is one empty limb short once three sparrow snaps are in play.
            if (DisplayNumber == 16)
                InsertEmptyLimb(b);
            ApplyAuthored(b, Current);
            return b;
        }

        // Feeders and the ostrich come from the level, after Pack has copied the maker.
        public static void ApplyAuthored(Board b, Level level)
        {
            if (b == null || level == null) return;
            int n = level.Feeders;
            if (n < 1) n = b.Live != null && b.Live.Length > 0 ? b.Live.Length : 2;
            GardenGen.Apply(b, new GardenGen.Knobs
            {
                Feeders = n,
                Ostrich = level.Ostrich,
                PeckSeed = level.PeckSeed,
                PecksPerMove = level.PecksPerMove
            });
        }

        static void InsertEmptyLimb(Board b)
        {
            int at = b.Branches.Count;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                if (!b.Branches[i].IsBonus) continue;
                at = i;
                break;
            }
            b.Branches.Insert(at, new BranchState());
        }

        // Predetermined pest budget. Hops stay winnable with a feeder blocked
        // (a collect evicts); LevelSolve fuzzes scrap-parking so a pest fight
        // cannot overflow a perch or auto-clear a same-color Cap.
        public static int SparrowVisits
        {
            get
            {
                if (Current != null && Current.Ostrich) return 0;
                int n = DisplayNumber;
                if (n <= 2) return 0;
                if (n <= 6) return 1;
                if (n <= 10) return 2;
                if (n <= 13) return 2;
                return 3;
            }
        }

        public static int HawkVisits
        {
            get
            {
                if (Current != null && Current.Ostrich) return 0;
                return DisplayNumber <= 10 ? 0 : 1;
            }
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
            UnhideSameColorBees(b);
            for (int i = 0; i < b.Branches.Count; i++)
            {
                if (b.Branches[i].IsFullMatch(out _))
                    b.Branches[i].Birds.RemoveAt(b.Branches[i].Birds.Count - 1);
                b.Branches[i].AlignShroud();
            }
            return b;
        }

        // Bees hide a different flock (color and kit), never a same-flock completion
        // under a visible run. Hopping 3 gold off a limb should not unveil 2 more
        // of that flock that you then hop back. Tip leaves (HideTip) stay locked.
        static void UnhideSameColorBees(Board b)
        {
            if (b == null) return;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                br.AlignShroud();
                for (int k = 0; k < br.Count - 1; k++)
                {
                    if (!br.IsShrouded(k)) continue;
                    for (int j = k + 1; j < br.Count; j++)
                    {
                        if (br.IsShrouded(j)) continue;
                        if (!br.Birds[k].SameFlock(br.Birds[j])) continue;
                        br.Shrouded[k] = false;
                        break;
                    }
                }
                // Bees cover one solid stack from the trunk out. Never leave a lone
                // hidden bird between visible ones: once a bird below is showing,
                // every bee above it lifts too (a tip leaf is separate and stays).
                bool seen = false;
                for (int k = 0; k < br.Count - 1; k++)
                {
                    if (!br.IsShrouded(k)) { seen = true; continue; }
                    if (seen) br.Shrouded[k] = false;
                }
            }
        }

        // Same-sex flocks: hops need color AND sex, so every bird of a color
        // shares a sex (otherwise a 5-stack can never gather). Colors cycle
        // Female / Male / Neutral so a stage is never all one gender.
        static Board DawnGarden()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Ruby),
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

        // Soft landing. Light bees, both feeders live. Topping up Gold on the bee limb too early tangles.
        static Board Garden16()
        {
            var b = Eight(
                Row(BirdColor.Violet, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Ruby, BirdColor.Gold),
                Row(BirdColor.Ruby, BirdColor.Teal),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Violet;
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Teal);
            HideInner(b.Branches[2], 3);
            HideInner(b.Branches[5], 3);
            return Prep(b);
        }

        // Violet stacks. Their clones are Peach, so Peach calls twice. Packing Gold before the bees show tangles.
        static Board Garden17()
        {
            var b = Eight(
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Violet, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Violet, BirdColor.Teal, BirdColor.Teal),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Violet;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[0], 2);
            HideInner(b.Branches[4], 1);
            return Prep(b);
        }

        // Three stubborn tips. A live color still hops on turn 1, but a blind second move can tangle.
        static Board Garden18()
        {
            var b = Eight(
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Teal, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Gold, BirdColor.Gold, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Violet, BirdColor.Ruby, BirdColor.Gold),
                Row(BirdColor.Violet, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[2], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[1]);
            HideTip(b.Branches[3]);
            HideTip(b.Branches[5]);
            return b;
        }

        // Pinched nectar, long queue. Build 82: one Teal set became Ruby (Violet was 4 sets after Pack).
        static Board Garden19()
        {
            var b = Eight(
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Gold, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Gold, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = null;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[2], 1);
            return Prep(b);
        }

        // Five flocks, no empty limb. Clears without the gift, but stacking Gold on bees tangles.
        static Board Garden20()
        {
            var b = Eight(
                Row(BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Gold, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Ruby, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Teal, BirdColor.Gold, BirdColor.Teal, BirdColor.Teal),
                new BranchState());
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 3);
            return Prep(b);
        }

        // Shift density. Build 82: one Gold set became Teal (Gold was 4 sets after Pack).
        static Board Garden21()
        {
            var b = Eight(
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Gold, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Violet, BirdColor.Gold, BirdColor.Teal, BirdColor.Gold),
                new BranchState());
            b.Live[0] = BirdColor.Violet;
            b.Live[1] = BirdColor.Ruby;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[1], 2);
            HideInner(b.Branches[5], 3);
            return Prep(b);
        }

        // Two stubborn tips sitting on thick inner bees. Splitting Teal into the empty limb tangles.
        static Board Garden22()
        {
            var b = Eight(
                Row(BirdColor.Violet, BirdColor.Violet, BirdColor.Violet, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Gold, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Gold, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Ruby, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Ruby, BirdColor.Gold, BirdColor.Violet),
                new BranchState(),
                new BranchState());
            b.Branches.Add(new BranchState()); // ease: third empty limb
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[2], 3);
            HideInner(b.Branches[5], 3);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[2]);
            HideTip(b.Branches[5]);
            return b;
        }

        // Regenerated (GardenGen band T1, gen seed 2755557184). Two live feeders, bees on two limbs; run-order kits (no Kits map).
        static Board Garden23()
        {
            var b = Eight(
                Row(BirdColor.Teal),
                Row(BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Teal, BirdColor.Teal, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Violet, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Ruby;
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[6], 2);
            HideInner(b.Branches[5], 3);
            return Prep(b);
        }

        // Mixed rows under bees and a leaf. Same flock or no hop; pairing Gold too early tangles.
        static Board Garden24()
        {
            var b = Eight(
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Ruby, BirdColor.Teal, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Ruby, BirdColor.Gold, BirdColor.Violet, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Violet, BirdColor.Ruby),
                Row(BirdColor.Teal, BirdColor.Violet, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Gold, BirdColor.Violet),
                new BranchState());
            b.Branches.Add(new BranchState()); // ease: third empty limb
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Gold);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[2], 3);
            HideInner(b.Branches[4], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            return b;
        }

        // Dawn-shaped rows with the late kit: pinch, two stubborn tips, bees. Build 82: one Gold set became Teal.
        static Board Garden25()
        {
            var b = Eight(
                Row(BirdColor.Teal, BirdColor.Ruby, BirdColor.Teal, BirdColor.Violet),
                Row(BirdColor.Violet, BirdColor.Ruby, BirdColor.Violet, BirdColor.Teal),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Gold, BirdColor.Gold),
                Row(BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby),
                Row(BirdColor.Gold, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Ruby, BirdColor.Violet),
                Row(BirdColor.Teal, BirdColor.Teal, BirdColor.Teal, BirdColor.Teal),
                Row(BirdColor.Gold, BirdColor.Gold, BirdColor.Ruby, BirdColor.Violet, BirdColor.Teal),
                new BranchState());
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = null;
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Teal);
            b.Queue.Add(BirdColor.Ruby);
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[0], 3);
            HideInner(b.Branches[1], 3);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[0]);
            HideTip(b.Branches[1]);
            return b;
        }

        // Single-color series (Build 82). One color, three kits, four limbs, no
        // empty limb; Level.Solo keeps Pack from cloning. Kits are painted here,
        // before Prep, so Prep's bee and five rules see real flocks.

        // Ruby. Three kits, inner bees only. Both feeders Ruby.
        static Board Garden26()
        {
            var b = new Board();
            b.Branches.Add(SoloRow(BirdColor.Ruby, "NNCN"));
            b.Branches.Add(SoloRow(BirdColor.Ruby, "CB"));
            b.Branches.Add(SoloRow(BirdColor.Ruby, "BCCB"));
            b.Branches.Add(SoloRow(BirdColor.Ruby, "BNNBC"));
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Ruby;
            b.Queue.Add(BirdColor.Ruby);
            HideInner(b.Branches[2], 3);
            HideInner(b.Branches[3], 4);
            return Prep(b);
        }

        // Teal. Heavier bees on three limbs.
        static Board Garden27()
        {
            var b = new Board();
            b.Branches.Add(SoloRow(BirdColor.Teal, "NNCNB"));
            b.Branches.Add(SoloRow(BirdColor.Teal, "BNC"));
            b.Branches.Add(SoloRow(BirdColor.Teal, "BCCB"));
            b.Branches.Add(SoloRow(BirdColor.Teal, "CNB"));
            b.Live[0] = BirdColor.Teal;
            b.Live[1] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Teal);
            HideInner(b.Branches[0], 4);
            HideInner(b.Branches[2], 3);
            HideInner(b.Branches[3], 2);
            return Prep(b);
        }

        // Gold. Adds one leaf tip; each collect breezes.
        static Board Garden28()
        {
            var b = new Board();
            b.Branches.Add(SoloRow(BirdColor.Gold, "BBNC"));
            b.Branches.Add(SoloRow(BirdColor.Gold, "NNC"));
            b.Branches.Add(SoloRow(BirdColor.Gold, "BCNN"));
            b.Branches.Add(SoloRow(BirdColor.Gold, "CBBC"));
            b.Live[0] = BirdColor.Gold;
            b.Live[1] = BirdColor.Gold;
            b.Queue.Add(BirdColor.Gold);
            HideInner(b.Branches[0], 3);
            HideInner(b.Branches[1], 2);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[3]);
            return b;
        }

        // Violet. Adds the pinched feeder (one live slot) on top of bees and a leaf.
        static Board Garden29()
        {
            var b = new Board();
            b.Branches.Add(SoloRow(BirdColor.Violet, "CC"));
            b.Branches.Add(SoloRow(BirdColor.Violet, "CCNCN"));
            b.Branches.Add(SoloRow(BirdColor.Violet, "BBN"));
            b.Branches.Add(SoloRow(BirdColor.Violet, "NBBBN"));
            b.Live[0] = BirdColor.Violet;
            b.Live[1] = null;
            b.Queue.Add(BirdColor.Violet);
            b.Queue.Add(BirdColor.Violet);
            HideInner(b.Branches[2], 2);
            HideInner(b.Branches[3], 4);
            b.BreezeOnCollect = true;
            Prep(b, keepTipShrouds: true);
            HideTip(b.Branches[3]);
            return b;
        }

        // One color with authored kits, trunk to tip ("C" Crown, "B" Bow, "N" bare).
        static BranchState SoloRow(BirdColor color, string kits)
        {
            var br = new BranchState();
            for (int i = 0; i < kits.Length; i++)
            {
                br.Birds.Add(new Bird(color));
                br.Shrouded.Add(false);
            }
            FlockKit.Paint(br, kits);
            return br;
        }

        static BranchState Row(params BirdColor[] birds)
        {
            var br = new BranchState();
            for (int i = 0; i < birds.Length; i++)
            {
                br.Birds.Add(new Bird(birds[i]));
                br.Shrouded.Add(false);
            }
            return br;
        }

        // Inner shrouds are bees. They hide a different color than the visible
        // birds sitting on them. Same-color under a visible run is stripped in
        // Prep (UnhideSameColorBees). They may sit under a leaf tip (HideTip)
        // until a feeder collect breeze lifts the leaf.
        static void HideInner(BranchState br, int count)
        {
            br.AlignShroud();
            int n = count;
            if (n > br.Count - 1) n = br.Count - 1;
            if (n < 0) n = 0;
            for (int i = 0; i < n; i++)
                br.Shrouded[i] = true;
        }

        // Leaf lock on the tip. Locks the limb until BreezeOnCollect.
        static void HideTip(BranchState br)
        {
            br.AlignShroud();
            if (br.Count > 0)
                br.Shrouded[br.Count - 1] = true;
        }
    }
}

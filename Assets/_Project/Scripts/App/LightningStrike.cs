namespace FlockFive
{
    // Storm strike on an unused gift. b110 (Brandon): it is ON by default now, because
    // nobody ever saw it. The switch is still PlayerPrefs PrefKey, falling back to
    // RemoteDefault so a remote config can flip the same constant (an explicit 0 stays off).
    // A strike burns one UNUSED ad/gift branch. It never touches a normal limb,
    // and it never offers a restore. Force is the dev hook for a capture.
    public static class LightningStrike
    {
        public const string PrefKey = "flockfive.lightning";
        public const bool RemoteDefault = true;
        public const int MaxPerLevel = 1;
        // b126 (Brandon): staged by GardenStorm.StageFor. Callers pass the 0-based LevelData INDEX; the stage is
        // keyed by garden number (index + 1). First strike on garden 3 (index 2); gardens 1-2 never strike.
        public const int MinGarden = GardenStorm.FirstStrikeGarden - 1;
        public static bool GardenEligible(int index) => GardenStorm.StrikeAllowed(index + 1);

        // b126 (Brandon): the intro strike belongs to garden 3 only. One gate: at least IntroDelay seconds in AND
        // visible rain (Wet > IntroWet; glass drops spawn from StormGlass.WetOn 0.22). Garden 3's short onset puts
        // full rain on screen by then, still before a realistic fastest 9-move clear (~6.3 s). Never carried into
        // another garden: from garden 4 on there is no owed strike, only the normal dice.
        public const float IntroDelay = 5.0f;
        public const float IntroWet = 0.3f;
        public static bool IntroOwed(int index, bool everStruck) =>
            !everStruck && GardenStorm.StageFor(index + 1) == GardenStorm.StormStage.FirstStrike;

        // No strike while a pest/sparrow lesson line is up, and none on the sparrow's first garden until its
        // intro has been seen (so the first lightning and the sparrow intro never share a garden).
        public static bool Blocked(int index, bool pestLineUp, bool sparrowIntroDone) =>
            pestLineUp || (index + 1 == PestSchedule.FirstSparrowGarden && !sparrowIntroDone);

        public enum Plan { None = 0, Intro = 1, Roll = 2 }

        // The one strike decision per tick (TickLightning and the tests both call it). calm = PestStageFree.
        public static Plan Decide(int index, bool everStruck, bool storming, float gardenPlay, bool calm,
            bool pestLineUp, bool sparrowIntroDone, bool won, float wet = 0f)
        {
            if (won || !GardenEligible(index)) return Plan.None;
            if (Blocked(index, pestLineUp, sparrowIntroDone)) return Plan.None;
            if (IntroOwed(index, everStruck))
                return calm && gardenPlay >= IntroDelay && wet > IntroWet ? Plan.Intro : Plan.None;
            return storming ? Plan.Roll : Plan.None;
        }

        // b113 (Brandon): after a strike there is a cooldown, then the chance ramps garden by garden until the next
        // one. since = eligible gardens entered since the last strike, counting the current one (the garden right
        // after a strike is 1). since <= CooldownGardens: no strike. Then StartChance, +StepChance per garden, capped.
        // Before the first strike ever the guarantee (FirstStrikeOwed) is what fires, not this curve.
        public const int CooldownGardens = 3;
        public const float StartChance = 0.15f;
        public const float StepChance = 0.15f;
        public const float CapChance = 0.90f;
        public const string SinceKey = "flockfive.lightning.since";

        public static float ChanceFor(int since)
        {
            if (since <= CooldownGardens) return 0f;
            float c = StartChance + (since - CooldownGardens - 1) * StepChance;
            return c > CapChance ? CapChance : c;
        }

        public static int Since => PrefGuard.GetInt(SinceKey, 0);

        // One eligible garden entered. Persisted.
        public static int NoteGarden()
        {
            int n = Since + 1;
            if (n > 1000) n = 1000;
            PrefGuard.SetInt(SinceKey, n);
            UnityEngine.PlayerPrefs.Save();
            return n;
        }
        // The first strike a player ever gets is guaranteed (still needs a storm, an unused gift, and a
        // solvable board). This pref counts strikes so the guarantee is spent once.
        public const string StruckKey = "flockfive.lightning.struck";

        public static bool FirstStrikeOwed => UnityEngine.PlayerPrefs.GetInt(StruckKey, 0) == 0;

        public static void MarkStruck()
        {
            UnityEngine.PlayerPrefs.SetInt(StruckKey, 1);
            PrefGuard.SetInt(SinceKey, 0); // the cooldown starts now
            UnityEngine.PlayerPrefs.Save();
        }

        // One-shot. The capture sets this; the next TryPick consumes it.
        public static bool Force;

        public static int Plays { get; private set; }

        public static bool Enabled
        {
            get
            {
                if (!UnityEngine.PlayerPrefs.HasKey(PrefKey)) return RemoteDefault;
                return UnityEngine.PlayerPrefs.GetInt(PrefKey, 0) != 0;
            }
            set
            {
                UnityEngine.PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                UnityEngine.PlayerPrefs.Save();
            }
        }

        public static bool RollHits(float unit, float chance)
        {
            if (unit < 0f) unit = 0f;
            if (unit > 1f) unit = 1f;
            return unit < chance;
        }

        // -1 when this storm does not strike. Force still obeys the cap,
        // the target rules, and the solvability gate.
        // owed is the one guaranteed first strike: it skips the dice only. Off, dry, capped,
        // no target, and unsolvable all still win over it.
        public static int TryPick(Board board, bool[] claimed, bool[] burned, bool storming, int already, float roll, bool force, bool owed = false, int garden = MinGarden, float chance = 1f)
        {
            if (!GardenEligible(garden)) return -1; // never garden 1, not even forced
            if (already >= MaxPerLevel) return -1;
            if (!force)
            {
                if (!Enabled || !storming) return -1;
                if (!owed && !RollHits(roll, chance)) return -1;
            }
            int index = Target(board, claimed, burned);
            if (index < 0) return -1;
            if (!KeepsSolvable(board, index)) return -1;
            return index;
        }

        // First unused locked empty bonus. Never a normal limb, never occupied,
        // never one the player already claimed or that lightning already burned.
        public static int Target(Board board, bool[] claimed, bool[] burned)
        {
            if (board == null) return -1;
            for (int i = 0; i < board.Branches.Count; i++)
            {
                var st = board.Branches[i];
                if (st == null || !st.IsBonus) continue;
                if (st.Broken || st.Burned || !st.AdLocked || st.Count > 0) continue;
                int ord = BonusBranches.Ordinal(board, i);
                if (ord < 0) continue;
                if (claimed != null && ord < claimed.Length && claimed[ord]) continue;
                if (burned != null && ord < burned.Length && burned[ord]) continue;
                return i;
            }
            return -1;
        }

        // The offer is not a perch today. Burning it only bricks the level when
        // the board is already stuck and that gift was the way out. Unknown
        // fails closed: we do not strike what we cannot prove is still winnable.
        public static bool KeepsSolvable(Board board, int index)
        {
            if (board == null || (uint)index >= (uint)board.Branches.Count) return false;
            var st = board.Branches[index];
            if (st == null || !st.IsBonus) return false;
            var clone = board.Clone();
            Burn(clone.Branches[index]);
            return GardenSolve.Look(clone, 12000) == GardenSolve.Outlook.Winnable;
        }

        // Marks the offer gone. Does not claim it: a claim would open the perch.
        public static void Burn(BranchState st)
        {
            if (st == null || !st.IsBonus) return;
            st.Burned = true;
            st.Broken = true;
            st.AdLocked = true;
            st.Birds.Clear();
            st.Shrouded.Clear();
        }

        public static void ApplyBurns(Board board, bool[] burned)
        {
            if (board == null || burned == null) return;
            int n = 0;
            for (int i = 0; i < board.Branches.Count; i++)
            {
                if (!board.Branches[i].IsBonus) continue;
                if (n < burned.Length && burned[n])
                    Burn(board.Branches[i]);
                n++;
            }
        }

        public static bool ConsumeForce()
        {
            if (!Force) return false;
            Force = false;
            return true;
        }

        public static void NotePlay() => Plays++;
    }
}

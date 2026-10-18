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
        // b110: was 0.45 (and the whole feature was off). One roll per storm window, at most one strike per garden.
        public const float Chance = 0.60f;
        public const int MaxPerLevel = 1;
        // The first strike a player ever gets is guaranteed (still needs a storm, an unused gift, and a
        // solvable board). This pref counts strikes so the guarantee is spent once.
        public const string StruckKey = "flockfive.lightning.struck";

        public static bool FirstStrikeOwed => UnityEngine.PlayerPrefs.GetInt(StruckKey, 0) == 0;

        public static void MarkStruck()
        {
            UnityEngine.PlayerPrefs.SetInt(StruckKey, 1);
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

        public static bool RollHits(float unit)
        {
            if (unit < 0f) unit = 0f;
            if (unit > 1f) unit = 1f;
            return unit < Chance;
        }

        // -1 when this storm does not strike. Force still obeys the cap,
        // the target rules, and the solvability gate.
        // owed is the one guaranteed first strike: it skips the dice only. Off, dry, capped,
        // no target, and unsolvable all still win over it.
        public static int TryPick(Board board, bool[] claimed, bool[] burned, bool storming, int already, float roll, bool force, bool owed = false)
        {
            if (already >= MaxPerLevel) return -1;
            if (!force)
            {
                if (!Enabled || !storming) return -1;
                if (!owed && !RollHits(roll)) return -1;
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

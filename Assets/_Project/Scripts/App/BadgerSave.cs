using UnityEngine;

namespace FlockFive
{
    // The only path to the unpaid-badger flag. `flockfive.badger` through PrefGuard
    // (same stamp as coins). 0 = clear. Any other value is the cleared display level
    // that is unpaid (15, 20, 25, ...). Boot and ShowSplash only read Pending.
    // Nothing in Phase 2 calls Clear; Phase 5 does on a win.
    public static class BadgerSave
    {
        public const string Pref = "flockfive.badger";
        // Bitmask of BadgerPower values that already claimed their tutorial first-use freebie.
        // Survives a lose/retry of the first fight so a second visit does not get free twice.
        public const string TutFreePref = "flockfive.badger.tutfree";

        // 0 when there is no flag, or while the kill switch is off.
        public static int Pending => PendingFor(BadgerSchedule.Enabled);

        public static int PendingFor(bool enabled)
        {
            if (!enabled) return 0;
            int v = PrefGuard.GetInt(Pref, 0);
            return v > 0 ? v : 0;
        }

        // Writes the flag for a cleared display level. No-op while the switch is off.
        public static bool Set(int clearedDisplay) => Set(clearedDisplay, BadgerSchedule.Enabled);

        public static bool Set(int clearedDisplay, bool enabled)
        {
            if (!enabled || clearedDisplay <= 0) return false;
            PrefGuard.SetInt(Pref, clearedDisplay);
            PlayerPrefs.Save();
            return true;
        }

        // The SettleIfIdle hook: flag only a level that is due.
        public static bool SetIfDue(int clearedDisplay) => SetIfDue(clearedDisplay, BadgerSchedule.Enabled);

        public static bool SetIfDue(int clearedDisplay, bool enabled)
        {
            if (!BadgerSchedule.ShouldFlag(clearedDisplay, enabled)) return false;
            return Set(clearedDisplay, enabled);
        }

        public static void Clear()
        {
            PrefGuard.SetInt(Pref, 0);
            PlayerPrefs.Save();
        }

        // Appearance 1 only, and only while this power has not claimed its free first use.
        public static bool IsTutorialFirstUse(BadgerPower power, int appearance)
        {
            if (appearance > 1) return false;
            if (BadgerSchedule.BasePrice(power) <= 0) return false;
            return !TutorialFreeClaimed(power);
        }

        public static bool TutorialFreeClaimed(BadgerPower power)
        {
            int bit = TutFreeBit(power);
            if (bit == 0) return true;
            int mask = PrefGuard.GetInt(TutFreePref, 0);
            return (mask & bit) != 0;
        }

        public static void ClaimTutorialFree(BadgerPower power)
        {
            int bit = TutFreeBit(power);
            if (bit == 0) return;
            int mask = PrefGuard.GetInt(TutFreePref, 0) | bit;
            PrefGuard.SetInt(TutFreePref, mask);
            PlayerPrefs.Save();
        }

        // Tests: restore every power to "still free once" (mask 0).
        public static void ClearTutorialFreeForTest()
        {
            PrefGuard.SetInt(TutFreePref, 0);
            PlayerPrefs.Save();
        }

        // Tests: burn every free so appearance-1 fights charge normal prices.
        public static void ExhaustTutorialFreeForTest()
        {
            int mask = 0;
            mask |= TutFreeBit(BadgerPower.X2);
            mask |= TutFreeBit(BadgerPower.X3);
            mask |= TutFreeBit(BadgerPower.HotSauce);
            mask |= TutFreeBit(BadgerPower.FreezeSpray);
            PrefGuard.SetInt(TutFreePref, mask);
            PlayerPrefs.Save();
        }

        static int TutFreeBit(BadgerPower power)
        {
            int ix = (int)power;
            if (ix < 1 || ix > 4) return 0;
            return 1 << ix;
        }
    }
}

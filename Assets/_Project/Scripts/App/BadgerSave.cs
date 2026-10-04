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
    }
}

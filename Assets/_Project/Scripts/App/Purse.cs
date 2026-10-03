using System;
using System.Globalization;
using UnityEngine;

namespace FlockFive
{
    // Screen money. Under 10,000 is the full number with a thousands separator
    // (1,000, 9,999). From 10,000, K drops a trailing .0 (10K, 11.6K).
    // M and B keep one decimal under 10 and drop a trailing .0.
    // Floors, so a label never reads higher than the amount.
    // The glyph stays "$": the IMGUI font has no U+20A3, so that sign would be tofu.
    public static class Money
    {
        public static string Format(int n) => Format((long)n);

        static long _formatKey;
        static string _formatText;
        static bool _formatReady;

        public static string Format(long n)
        {
            if (_formatReady && n == _formatKey) return _formatText;
            _formatKey = n;
            _formatText = FormatFresh(n);
            _formatReady = true;
            return _formatText;
        }

        static string FormatFresh(long n)
        {
            if (n < 0) return "-" + FormatFresh(n == long.MinValue ? long.MaxValue : -n);
            if (n < 10000L) return "$" + n.ToString("N0", CultureInfo.InvariantCulture);
            if (n < 1000000L) return "$" + KText(n) + "K";
            long unit = 1000000L;
            string suffix = "M";
            if (n >= 1000000000L)
            {
                unit = 1000000000L;
                suffix = "B";
            }
            return "$" + Scaled(n, unit) + suffix;
        }

        // Floored tenths. 10000 → 10, 11600 → 11.6, 12349 → 12.3.
        static string KText(long n)
        {
            long tenths = n / 100L;
            long whole = tenths / 10L;
            long frac = tenths % 10L;
            if (frac == 0L) return whole.ToString(CultureInfo.InvariantCulture);
            return whole.ToString(CultureInfo.InvariantCulture) + "." + frac.ToString(CultureInfo.InvariantCulture);
        }

        static string Scaled(long n, long unit)
        {
            long whole = n / unit;
            if (whole >= 10L) return whole.ToString(CultureInfo.InvariantCulture);
            long tenths = whole * 10L + (n % unit) * 10L / unit;
            long frac = tenths % 10L;
            if (frac == 0L) return (tenths / 10L).ToString(CultureInfo.InvariantCulture);
            return (tenths / 10L).ToString(CultureInfo.InvariantCulture) + "." + frac.ToString(CultureInfo.InvariantCulture);
        }
    }

    public static class Purse
    {
        const string PrefCoins = "flockfive.coins";
        const string PrefStreak = "flockfive.streak";
        const string PrefStage = "flockfive.instage";
        const string PrefLoginDay = "flockfive.login.day";
        const string PrefLoginN = "flockfive.login.n";
        const string PrefOwed = "flockfive.owed";
        // Clear pay before streak and login. Level 1 is 25, then +5 a level (20 + 5n).
        public static int StageBase(int level)
        {
            if (level < 1) level = 1;
            return 20 + 5 * level;
        }

        public static int StagePay => StageBase(LevelData.DisplayNumber);

        public static int Coins { get; private set; }
        public static int Streak { get; private set; }
        public static int Pending { get; set; }
        // Stage-clear coins waiting for the reward sign's pay step. Not the balance.
        static int Owed;
        public static int LoginDays { get; private set; }
        public static int LoginMul => Mathf.Clamp(LoginDays, 1, 3);
        public static int LastStagePay { get; private set; }
        public static int LastStreak { get; private set; }
        public static int LastLogin { get; private set; }
        public static int LastWin { get; private set; }
        public static int Multiplier => StreakTier.Display(Streak);

        public static void Boot()
        {
            Coins = Mathf.Max(0, PrefGuard.GetInt(PrefCoins, 0));
            int saved = Mathf.Max(0, PrefGuard.GetInt(PrefStreak, 0));
            Streak = StreakTier.Normalize(saved);
            if (Streak != saved) PrefGuard.SetInt(PrefStreak, Streak);
            LoginDays = Mathf.Max(1, PrefGuard.GetInt(PrefLoginN, 1));
            Owed = Mathf.Max(0, PrefGuard.GetInt(PrefOwed, 0));
            TickLogin();
            bool inStage = PlayerPrefs.GetInt(PrefStage, 0) == 1;
            if (inStage)
            {
                Streak = 0;
                PrefGuard.SetInt(PrefStreak, 0);
                PlayerPrefs.SetInt(PrefStage, 0);
                PlayerPrefs.Save();
            }
        }

        static void TickLogin()
        {
            string today = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string last = PrefGuard.GetString(PrefLoginDay, "");
            if (last == today)
            {
                LoginDays = Mathf.Max(1, PrefGuard.GetInt(PrefLoginN, 1));
                return;
            }
            int days = 1;
            DateTime prev;
            if (last.Length == 8
                && DateTime.TryParseExact(last, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out prev))
            {
                double gap = (DateTime.UtcNow.Date - prev.Date).TotalDays;
                if (gap >= 0.5 && gap < 1.85)
                    days = Mathf.Min(PrefGuard.GetInt(PrefLoginN, 1) + 1, 7);
            }
            LoginDays = days;
            PrefGuard.SetString(PrefLoginDay, today);
            PrefGuard.SetInt(PrefLoginN, LoginDays);
            PlayerPrefs.Save();
        }

        public static void BeginStage()
        {
            PlayerPrefs.SetInt(PrefStage, 1);
            PlayerPrefs.Save();
        }

        public static int AwardClear()
        {
            TickLogin();
            Streak = StreakTier.Next(Streak);
            int basePay = StageBase(LevelData.DisplayNumber);
            LastStagePay = basePay;
            LastStreak = Streak;
            LastLogin = LoginMul;
            int pay = basePay * Streak * LastLogin;
            LastWin = pay;
            // The reward sign credits this once, after its roll (or amount fade).
            Pending = pay;
            Owed = pay;
            PrefGuard.SetInt(PrefOwed, pay);
            PrefGuard.SetInt(PrefStreak, Streak);
            PlayerPrefs.SetInt(PrefStage, 0);
            PlayerPrefs.Save();
            return pay;
        }

        // Single stage-clear credit. A second call is a no-op until the next clear.
        public static bool CommitStreakPay()
        {
            if (Owed <= 0) return false;
            int pay = Owed;
            Owed = 0;
            Pending = 0;
            Coins += pay;
            PrefGuard.SetInt(PrefCoins, Coins);
            PrefGuard.SetInt(PrefOwed, 0);
            PlayerPrefs.Save();
            return true;
        }

        public static void SetStreak(int tier)
        {
            Streak = StreakTier.Normalize(tier);
            PrefGuard.SetInt(PrefStreak, Streak);
            PlayerPrefs.Save();
        }

        public static void CueWin(int streak, int win)
        {
            Streak = StreakTier.Normalize(streak);
            if (Streak < 1) Streak = 1;
            int basePay = StageBase(LevelData.DisplayNumber);
            LastStagePay = basePay;
            LastStreak = Streak;
            LastLogin = LoginMul;
            LastWin = win > 0 ? win : basePay * Streak * LastLogin;
            Pending = LastWin;
        }

        public static void BreakStreak()
        {
            Streak = 0;
            PrefGuard.SetInt(PrefStreak, 0);
            PlayerPrefs.Save();
        }

        public static bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Coins -= amount;
            PrefGuard.SetInt(PrefCoins, Coins);
            PlayerPrefs.Save();
            return true;
        }

        public static void Credit(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            PrefGuard.SetInt(PrefCoins, Coins);
            PlayerPrefs.Save();
        }
    }

    // Local checksum only. Do not treat these values as a server economy.
    static class PrefGuard
    {
        const int Mix = unchecked((int)0x5F10C5A3);

        public static int GetInt(string key, int fallback)
        {
            int v = PlayerPrefs.GetInt(key, fallback);
            string hk = key + ".x";
            if (!PlayerPrefs.HasKey(hk))
            {
                PlayerPrefs.SetInt(hk, Stamp(key, v));
                PlayerPrefs.Save();
                return v;
            }
            if (PlayerPrefs.GetInt(hk, 0) != Stamp(key, v)) return fallback;
            return v;
        }

        public static void SetInt(string key, int v)
        {
            PlayerPrefs.SetInt(key, v);
            PlayerPrefs.SetInt(key + ".x", Stamp(key, v));
        }

        public static string GetString(string key, string fallback)
        {
            string v = PlayerPrefs.GetString(key, fallback);
            string hk = key + ".x";
            if (!PlayerPrefs.HasKey(hk))
            {
                PlayerPrefs.SetInt(hk, Stamp(key, v));
                PlayerPrefs.Save();
                return v;
            }
            if (PlayerPrefs.GetInt(hk, 0) != Stamp(key, v)) return fallback;
            return v;
        }

        public static void SetString(string key, string v)
        {
            PlayerPrefs.SetString(key, v);
            PlayerPrefs.SetInt(key + ".x", Stamp(key, v));
        }

        static int Stamp(string key, int v)
        {
            int h = MixStr(Mix, key);
            h ^= v;
            h *= 16777619;
            return h;
        }

        static int Stamp(string key, string v) => MixStr(MixStr(Mix, key), v);

        static int MixStr(int h, string s)
        {
            if (s == null) return h;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619;
            }
            return h;
        }
    }
}

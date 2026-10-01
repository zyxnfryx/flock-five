using System;
using System.Globalization;
using UnityEngine;

namespace FlockFive
{
    // Screen money. Under 1,000 is a plain integer. Then K, M, and B.
    // One decimal while the unit is under 10, and a trailing .0 is dropped.
    // Floors, so a label never reads higher than the amount.
    public static class Money
    {
        public static string Format(int n) => Format((long)n);

        public static string Format(long n)
        {
            if (n < 0) return "-" + Format(n == long.MinValue ? long.MaxValue : -n);
            if (n < 1000L) return "$" + n.ToString(CultureInfo.InvariantCulture);
            long unit = 1000L;
            string suffix = "K";
            if (n >= 1000000000L)
            {
                unit = 1000000000L;
                suffix = "B";
            }
            else if (n >= 1000000L)
            {
                unit = 1000000L;
                suffix = "M";
            }
            return "$" + Scaled(n, unit) + suffix;
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
        public static int LoginDays { get; private set; }
        public static int LoginMul => Mathf.Clamp(LoginDays, 1, 3);
        public static int LastStagePay { get; private set; }
        public static int LastStreak { get; private set; }
        public static int LastLogin { get; private set; }
        public static int LastWin { get; private set; }
        public static int Multiplier => Streak < 1 ? 1 : Streak;

        public static void Boot()
        {
            Coins = Mathf.Max(0, PrefGuard.GetInt(PrefCoins, 0));
            Streak = Mathf.Max(0, PrefGuard.GetInt(PrefStreak, 0));
            LoginDays = Mathf.Max(1, PrefGuard.GetInt(PrefLoginN, 1));
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
            Streak = Streak + 1;
            int basePay = StageBase(LevelData.DisplayNumber);
            LastStagePay = basePay;
            LastStreak = Streak;
            LastLogin = LoginMul;
            int pay = basePay * Streak * LastLogin;
            LastWin = pay;
            Coins += pay;
            Pending = pay;
            PrefGuard.SetInt(PrefCoins, Coins);
            PrefGuard.SetInt(PrefStreak, Streak);
            PlayerPrefs.SetInt(PrefStage, 0);
            PlayerPrefs.Save();
            return pay;
        }

        public static void CueWin(int streak, int win)
        {
            Streak = Mathf.Max(1, streak);
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

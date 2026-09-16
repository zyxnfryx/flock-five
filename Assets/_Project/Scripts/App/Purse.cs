using System.Globalization;
using UnityEngine;

namespace FlockFive
{
    public static class Purse
    {
        const string PrefCoins = "flockfive.coins";
        const string PrefStreak = "flockfive.streak";
        const string PrefStage = "flockfive.instage";
        const int Pay = 16;

        public static int Coins { get; private set; }
        public static int Streak { get; private set; }
        public static int Pending { get; set; }
        public static int Multiplier => Streak < 1 ? 1 : Streak;
        public static string Cash => Coins >= 1000000 ? Compact(Coins) : Dollars(Coins);

        public static string Dollars(int n)
        {
            if (n < 0) return "-" + Dollars(-n);
            return "$" + n.ToString("N0", CultureInfo.InvariantCulture);
        }

        // Tight labels: $999 / $1,234 / $12.5K / $1.2M
        public static string Compact(int n)
        {
            if (n < 0) return "-" + Compact(-n);
            if (n < 10000) return Dollars(n);
            double v;
            string unit;
            if (n < 1000000)
            {
                v = n / 1000.0;
                unit = "K";
            }
            else
            {
                v = n / 1000000.0;
                unit = "M";
            }
            string num = v >= 100
                ? Mathf.RoundToInt((float)v).ToString(CultureInfo.InvariantCulture)
                : v.ToString("0.##", CultureInfo.InvariantCulture);
            return "$" + num + unit;
        }

        public static void Boot()
        {
            Coins = Mathf.Max(0, PrefGuard.GetInt(PrefCoins, 0));
            Streak = Mathf.Max(0, PrefGuard.GetInt(PrefStreak, 0));
            bool inStage = PlayerPrefs.GetInt(PrefStage, 0) == 1;
            if (inStage)
            {
                Streak = 0;
                PrefGuard.SetInt(PrefStreak, 0);
                PlayerPrefs.SetInt(PrefStage, 0);
                PlayerPrefs.Save();
            }
        }

        public static void BeginStage()
        {
            PlayerPrefs.SetInt(PrefStage, 1);
            PlayerPrefs.Save();
        }

        public static int AwardClear()
        {
            Streak = Streak + 1;
            int pay = Pay * Streak;
            Coins += pay;
            Pending = pay;
            PrefGuard.SetInt(PrefCoins, Coins);
            PrefGuard.SetInt(PrefStreak, Streak);
            PlayerPrefs.SetInt(PrefStage, 0);
            PlayerPrefs.Save();
            return pay;
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

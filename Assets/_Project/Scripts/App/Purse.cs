using System;
using System.Globalization;
using UnityEngine;

namespace FlockFive
{
    // Screen money. Under 10,000 is the full number with a thousands separator
    // (1,000, 9,999). From 10,000, K, M and B ALWAYS carry exactly one decimal
    // (10.0K, 11.6K, 123.4K, 1.0M, 25.0M, 250.0B) so the label keeps one width and
    // the digits do not jiggle while a count-up rolls through values.
    // Floors, so a label never reads higher than the amount: 999,999 is 999.9K, never
    // 1000.0K, and the next coin reads 1.0M.
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
            if (n < 1000000L) return "$" + Scaled(n, 1000L) + "K";
            if (n < 1000000000L) return "$" + Scaled(n, 1000000L) + "M";
            return "$" + Scaled(n, 1000000000L) + "B";
        }

        // Floored tenths, always "whole.d". 10000 -> 10.0, 11600 -> 11.6, 12349 -> 12.3.
        static string Scaled(long n, long unit)
        {
            long tenths = n / (unit / 10L);
            return (tenths / 10L).ToString(CultureInfo.InvariantCulture) + "." + (tenths % 10L).ToString(CultureInfo.InvariantCulture);
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
        const string PrefClearDouble = "flockfive.clear.double";
        // Clear pay before streak and login. Level 1 is 25, then +5 a level (20 + 5n).
        public static int StageBase(int level)
        {
            if (level < 1) level = 1;
            return 20 + 5 * level;
        }

        // Shared clear-reward table. AwardClear and a badger win both read this so the
        // amounts never drift. Display is the cleared level (15, 20, 25, ...).
        public static int ClearRewardFor(int display) => StageBase(display);

        public static int StagePay => StageBase(LevelData.DisplayNumber);

        public static int Coins { get; private set; }
        public static int Streak { get; private set; }
        public static int Pending { get; set; }
        // Stage-clear coins waiting for the reward sign's pay step. Not the balance.
        static int Owed;
        public static int OwedPay => Owed; // b85: the held REWARD popup reads its prize
        static bool _clearDouble;
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
            _clearDouble = PlayerPrefs.GetInt(PrefClearDouble, 0) == 1;
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
            int basePay = ClearRewardFor(LevelData.DisplayNumber);
            LastStagePay = basePay;
            LastStreak = Streak;
            LastLogin = LoginMul;
            int pay = ApplyClearDouble(basePay * Streak * LastLogin);
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

        // b87: the 2X ad top-up joins the held stage pay, so the player gets ONE grant
        // (one coin fly, one sound) after the ad. False when nothing is held.
        public static bool AddOwed(int extra)
        {
            if (extra <= 0 || Owed <= 0) return false;
            Owed += extra;
            Pending = Owed;
            LastWin = Owed;
            PrefGuard.SetInt(PrefOwed, Owed);
            PlayerPrefs.Save();
            return true;
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
            int basePay = ClearRewardFor(LevelData.DisplayNumber);
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

        // What AwardClear will pay before a watched 2x. Same formula, no write.
        public static int PeekClearPay()
        {
            int streak = StreakTier.Next(Streak);
            int login = LoginMul < 1 ? 1 : LoginMul;
            return ClearRewardFor(LevelData.DisplayNumber) * streak * login;
        }

        public static bool ClearDoubleArmed => _clearDouble;

        // The clear-double reward. The next AwardClear pays twice and clears this.
        public static bool ArmClearDouble()
        {
            _clearDouble = true;
            PlayerPrefs.SetInt(PrefClearDouble, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static int ApplyClearDouble(int pay)
        {
            if (!_clearDouble || pay <= 0) return pay;
            _clearDouble = false;
            PlayerPrefs.DeleteKey(PrefClearDouble);
            PlayerPrefs.Save();
            return pay * 2;
        }

        public static void Credit(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            PrefGuard.SetInt(PrefCoins, Coins);
            PlayerPrefs.Save();
        }

        // Editor tests restore the coin pref, then pull that value back into the static.
        public static void ReloadCoins()
        {
            Coins = Mathf.Max(0, PrefGuard.GetInt(PrefCoins, 0));
        }

        // Shared display hold. A reveal that plays after the credit (poker win, fullcard)
        // credits and saves at once, so a kill can never lose the coins, but the purse
        // label keeps showing the old total until the feature calls ReleaseDisplay at its
        // reveal; the HUD then rolls up to the new total. Not saved: a relaunch shows Coins.
        public static int HeldDisplay { get; private set; }

        // What the coin HUD may show right now: the saved balance minus coins on hold.
        public static int DisplayCoins => Mathf.Max(0, Coins - Mathf.Max(0, HeldDisplay));

        // Credit + save now, reveal later.
        public static void CreditHeld(int amount)
        {
            if (amount <= 0) return;
            Credit(amount);
            HoldDisplay(amount);
        }

        public static void HoldDisplay(int amount)
        {
            if (amount <= 0) return;
            HeldDisplay += amount;
        }

        // Reveal up to amount of the held coins (all of them when amount < 0).
        // Returns how much was actually released.
        public static int ReleaseDisplay(int amount = -1)
        {
            if (HeldDisplay <= 0)
            {
                HeldDisplay = 0;
                return 0;
            }
            int n = amount < 0 ? HeldDisplay : Mathf.Min(amount, HeldDisplay);
            HeldDisplay -= n;
            return n;
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

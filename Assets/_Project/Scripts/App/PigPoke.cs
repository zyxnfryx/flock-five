using System;
using UnityEngine;

namespace FlockFive
{
    // Splash piggy poke cadence: oink always; coin every 3 pokes (max 3/day); bonus at every 100th lifetime poke.
    // Each coin payout snaps to the nearest multiple of 5 (minimum 5).
    public static class PigPoke
    {
        const string PrefPokes = "flockfive.pig.pokes";
        const string PrefDay = "flockfive.pig.day";
        const string PrefDayAwards = "flockfive.pig.dayAwards";
        const string PrefDayStreak = "flockfive.pig.dayStreak";

        public static int Lifetime { get; private set; }
        public static int DayAwards { get; private set; }
        public static int DayStreak { get; private set; }
        static string _dayKey = "";
        static bool _booted;

        public struct Result
        {
            public bool TripleCoin;
            public bool MilestoneCoin;
            public int Coins;
        }

        public static void Boot()
        {
            Lifetime = Mathf.Max(0, PlayerPrefs.GetInt(PrefPokes, 0));
            _dayKey = PlayerPrefs.GetString(PrefDay, "");
            DayAwards = Mathf.Clamp(PlayerPrefs.GetInt(PrefDayAwards, 0), 0, 3);
            DayStreak = Mathf.Clamp(PlayerPrefs.GetInt(PrefDayStreak, 0), 0, 2);
            _booted = true;
            EnsureDay();
        }

        static void EnsureBoot()
        {
            if (!_booted) Boot();
        }

        static string TodayKey() => DateTime.Today.ToString("yyyyMMdd");

        static void EnsureDay()
        {
            string today = TodayKey();
            if (_dayKey == today) return;
            _dayKey = today;
            DayAwards = 0;
            DayStreak = 0;
            PlayerPrefs.SetString(PrefDay, _dayKey);
            PlayerPrefs.SetInt(PrefDayAwards, 0);
            PlayerPrefs.SetInt(PrefDayStreak, 0);
            PlayerPrefs.Save();
        }

        static int SnapFive(int amount)
        {
            if (amount <= 0) return 0;
            int rem = amount % 5;
            int snapped = rem >= 3 ? amount + (5 - rem) : amount - rem;
            return snapped < 5 ? 5 : snapped;
        }

        public static Result Poke()
        {
            EnsureBoot();
            EnsureDay();

            Lifetime++;
            PlayerPrefs.SetInt(PrefPokes, Lifetime);

            bool triple = false;
            if (DayAwards < 3)
            {
                DayStreak++;
                if (DayStreak >= 3)
                {
                    DayStreak = 0;
                    DayAwards++;
                    triple = true;
                }
                PlayerPrefs.SetInt(PrefDayStreak, DayStreak);
                PlayerPrefs.SetInt(PrefDayAwards, DayAwards);
            }

            bool milestone = Lifetime > 0 && (Lifetime % 100) == 0;

            int coins = 0;
            if (triple)
            {
                int pay = SnapFive(1);
                Purse.Credit(pay);
                coins += pay;
            }
            if (milestone)
            {
                int pay = SnapFive(1);
                Purse.Credit(pay);
                coins += pay;
            }

            PlayerPrefs.Save();
            return new Result
            {
                TripleCoin = triple,
                MilestoneCoin = milestone,
                Coins = coins
            };
        }
    }
}

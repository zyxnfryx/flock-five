using System;
using UnityEngine;

namespace FlockFive
{
    // Splash piggy poke cadence: oink always; coin every 3 pokes (max 3/day); +1 at every 100th lifetime poke.
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
                Purse.Credit(1);
                coins++;
            }
            if (milestone)
            {
                Purse.Credit(1);
                coins++;
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

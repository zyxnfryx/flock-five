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

        public static void Boot()
        {
            Coins = Mathf.Max(0, PlayerPrefs.GetInt(PrefCoins, 0));
            Streak = Mathf.Max(0, PlayerPrefs.GetInt(PrefStreak, 0));
            bool inStage = PlayerPrefs.GetInt(PrefStage, 0) == 1;
            if (inStage)
            {
                Streak = 0;
                PlayerPrefs.SetInt(PrefStreak, 0);
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
            PlayerPrefs.SetInt(PrefCoins, Coins);
            PlayerPrefs.SetInt(PrefStreak, Streak);
            PlayerPrefs.SetInt(PrefStage, 0);
            PlayerPrefs.Save();
            return pay;
        }

        public static void BreakStreak()
        {
            Streak = 0;
            PlayerPrefs.SetInt(PrefStreak, 0);
            PlayerPrefs.Save();
        }
    }
}

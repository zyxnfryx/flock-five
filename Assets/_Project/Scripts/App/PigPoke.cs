using UnityEngine;

namespace FlockFive
{
    // Splash piggy poke. Oink and a wiggle only. Pokes never pay coins.
    public static class PigPoke
    {
        const string PrefPokes = "flockfive.pig.pokes";

        public static int Lifetime { get; private set; }
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
            _booted = true;
        }

        public static Result Poke()
        {
            if (!_booted) Boot();
            Lifetime++;
            PlayerPrefs.SetInt(PrefPokes, Lifetime);
            PlayerPrefs.Save();
            return default;
        }
    }
}

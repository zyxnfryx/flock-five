using UnityEngine;

namespace FlockFive
{
    // Which mascot colour busts through the finale logo. Rotates once per finale and
    // persists across launches so every colour gets a turn.
    public static class BustTurn
    {
        const string Key = "flockfive.finale.bust";
        static readonly BirdColor[] Order =
            { BirdColor.Peach, BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet };

        public static int Count => Order.Length;

        public static BirdColor At(int i)
        {
            int n = Order.Length;
            return Order[((i % n) + n) % n];
        }

        // Colour for this finale; advances the saved index for the next one.
        public static BirdColor Next()
        {
            int i = PlayerPrefs.GetInt(Key, 0);
            var col = At(i);
            PlayerPrefs.SetInt(Key, (i + 1) % Order.Length);
            PlayerPrefs.Save();
            return col;
        }

        // The other four colours, in palette order, for the lockup slots.
        public static BirdColor[] Crew(BirdColor buster)
        {
            var crew = new BirdColor[Order.Length - 1];
            int k = 0;
            var pal = new[] { BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Peach };
            for (int i = 0; i < pal.Length && k < crew.Length; i++)
                if (pal[i] != buster) crew[k++] = pal[i];
            return crew;
        }
    }
}

using UnityEngine;

namespace FlockFive
{
    // Level-hive cards. The player swipes, taps a side, or taps a dot.
    // Nothing advances on a timer. The lit dot is the card nearest center.
    public static class HiveCarousel
    {
        public const float Edge = 0.28f;

        public static float Wrap(float d, int count)
        {
            if (count <= 1) return 0f;
            float n = count;
            return Mathf.Repeat(d + n * 0.5f, n) - n * 0.5f;
        }

        public static int Nearest(float shown, int count)
        {
            if (count <= 1) return 0;
            int best = 0;
            float bestA = 999f;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Abs(Wrap(i - shown, count));
                if (a < bestA)
                {
                    bestA = a;
                    best = i;
                }
            }
            return best;
        }

        // -1 previous, +1 next, 0 open the centered card.
        // localX is 0 at the left of the stage.
        public static int TapDir(float localX, float width, int count)
        {
            if (count <= 1 || width < 8f) return 0;
            float u = localX / width;
            if (u < Edge) return -1;
            if (u > 1f - Edge) return 1;
            return 0;
        }
    }
}

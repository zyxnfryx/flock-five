using UnityEngine;

namespace FlockFive
{
    // Stage-clear multiplier. The only tiers are x1, x2, x3, x4, x5, and x10.
    // Saved 0 stays 0. 1-5 stay. 6-9 become 5. 10 stays 10. Above 10 becomes 10.
    public static class StreakTier
    {
        public static int Normalize(int raw)
        {
            if (raw <= 0) return 0;
            if (raw <= 5) return raw;
            if (raw < 10) return 5;
            return 10;
        }

        // Next clear: 0→1, 1→2, 2→3, 3→4, 4→5, 5→10, 10 stays 10.
        public static int Next(int raw)
        {
            int n = Normalize(raw);
            if (n <= 0) return 1;
            if (n < 5) return n + 1;
            if (n == 5) return 10;
            return 10;
        }

        public static int Display(int raw)
        {
            int n = Normalize(raw);
            return n < 1 ? 1 : n;
        }

        public static bool AtStake(int raw) => Normalize(raw) >= 2;

        // Shared stamp ink. x1 neutral, x2 red, x3 green, x4 blue, x5 purple,
        // x10 shimmering gold. 6-9 follow Display (x5). Not a diamond.
        public static void BadgeColors(int raw, out Color fill, out Color edge, out bool shimmer)
        {
            int mul = Display(raw);
            shimmer = mul >= 10;
            if (mul >= 10)
            {
                fill = new Color(0.96f, 0.78f, 0.28f, 0.92f);
                edge = new Color(0.42f, 0.26f, 0.04f, 1f);
            }
            else if (mul >= 5)
            {
                fill = new Color(0.62f, 0.28f, 0.78f, 0.92f);
                edge = new Color(0.28f, 0.08f, 0.36f, 1f);
            }
            else if (mul == 4)
            {
                fill = new Color(0.22f, 0.42f, 0.86f, 0.92f);
                edge = new Color(0.06f, 0.12f, 0.32f, 1f);
            }
            else if (mul == 3)
            {
                fill = new Color(0.18f, 0.62f, 0.28f, 0.92f);
                edge = new Color(0.04f, 0.24f, 0.08f, 1f);
            }
            else if (mul == 2)
            {
                fill = new Color(0.78f, 0.10f, 0.12f, 0.92f);
                edge = new Color(0.36f, 0.02f, 0.04f, 1f);
            }
            else
            {
                fill = new Color(0.78f, 0.74f, 0.68f, 0.92f);
                edge = new Color(0.28f, 0.24f, 0.20f, 1f);
                shimmer = false;
            }
        }

        // Six pips: tiers 1..5 light 1..5, x10 lights the sixth. No plus chip.
        public static int PipCount(int raw)
        {
            int n = Normalize(raw);
            if (n >= 10) return 6;
            if (n <= 0) return 0;
            return n;
        }
    }
}

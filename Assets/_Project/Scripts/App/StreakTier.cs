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

        // Forwards the shared chip palette. StreakBadge.Colors is the only ink.
        public static void BadgeColors(int raw, out Color fill, out Color edge, out bool shimmer)
        {
            var ink = StreakBadge.Colors(raw);
            fill = ink.Fill;
            edge = ink.Edge;
            shimmer = ink.Shimmer;
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

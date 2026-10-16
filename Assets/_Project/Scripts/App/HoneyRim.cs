using UnityEngine;

namespace FlockFive
{
    // Honeycomb wax. Every finish stays in the warm honey family.
    // Normal, Holo, and Inverse Rainbow differ by depth, never by a cool rim.
    public static class HoneyRim
    {
        public static Color For(BeeFinish finish)
        {
            if (finish == BeeFinish.InverseRainbow)
                return new Color(0.62f, 0.32f, 0.08f, 1f);
            if (finish == BeeFinish.Holo)
                return new Color(0.96f, 0.68f, 0.18f, 1f);
            return new Color(0.84f, 0.54f, 0.14f, 1f);
        }

        public static bool Warm(Color c) =>
            c.r > c.b + 0.18f && c.r > 0.45f && c.g > c.b;
    }
}

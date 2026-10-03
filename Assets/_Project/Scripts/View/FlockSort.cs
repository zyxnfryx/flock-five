using UnityEngine;

namespace FlockFive
{
    // Bees stay strictly above every bird state, including restart silhouettes.
    public static class FlockSort
    {
        public const int Shroud = 7;
        public const int Perch = 12;
        public const int Lift = 40;
        public const int Fly = 42;
        public const int Bee = 80;

        public static int Flight(int pop)
        {
            if (pop < 0) pop = 0;
            int order = Fly + pop;
            // Face, zzz, bang, and sparkle sit a few steps above the body.
            int cap = Bee - 6;
            if (order > cap) order = cap;
            return order;
        }

        // One writer for bird draw order. Call it after a sprite assign: a new
        // sprite must not leave the renderer at 0, behind wood and the leaf curtain.
        // The splash avatar is IMGUI and has no renderer; it stays in front by
        // painting after the perch leaves, outside that GUI group.
        public static void Apply(SpriteRenderer sr, int order)
        {
            if (sr == null) return;
            if (sr.sortingOrder != order) sr.sortingOrder = order;
        }
    }
}

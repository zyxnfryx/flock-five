using UnityEngine;

namespace FlockFive
{
    // Wood frame and cream plate for an album card. The badger tile reads the same
    // two colors, so a sleeve and its fight tile stay one tint.
    public static class CardPlate
    {
        public static Color Wood(Color tint, bool owned) => owned
            ? Color.Lerp(new Color(0.28f, 0.16f, 0.07f), tint, 0.35f)
            : new Color(0.14f, 0.12f, 0.10f, 0.92f);

        public static Color Face(Color tint, bool owned) => owned
            ? Color.Lerp(new Color(0.98f, 0.92f, 0.72f), tint, 0.18f)
            : new Color(0.22f, 0.20f, 0.18f, 0.95f);
    }
}

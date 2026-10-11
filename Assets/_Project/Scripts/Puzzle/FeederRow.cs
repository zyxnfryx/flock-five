namespace FlockFive
{
    // Portrait feeder row. One helper for the garden, the solver's leaf rule,
    // and the generator. Slot 0 stays left and slot 1 stays right so a two-feeder
    // garden does not move. A third feeder sits in the middle.
    public static class FeederRow
    {
        public const int Min = 1;
        public const int Max = 3;
        // Shipped pair. Centers are one bulb apart, so the two dishes just miss.
        public const float Pair = 1.22f;
        // Opaque half-width of feeder_*.png at 180 PPU times FeederView.Scale.
        public const float BulbHalf = 1.216f;
        // Three dishes need a wider step or the bulbs overlap. Still inside the
        // 9:16 playfield (half width ~5.96).
        public const float Triple = 2.56f;
        public const float PhoneTall = 9f / 19.5f;
        public const float Tablet = 3f / 4f;

        public static int Clamp(int n)
        {
            if (n < Min) return Min;
            if (n > Max) return Max;
            return n;
        }

        public static float X(int slot, int count)
        {
            count = Clamp(count);
            if ((uint)slot >= (uint)count) return 0f;
            if (count <= 1) return 0f;
            if (count == 2) return slot == 0 ? -Pair : Pair;
            if (slot == 2) return 0f;
            return slot == 0 ? -Triple : Triple;
        }

        // Horizontal half of the playfield that stays inside the safe area.
        // A wider device pillarboxes the 9:16 playfield, so the inset does not
        // shrink it. A taller phone spans the screen; a side inset does.
        public static float SafeHalf(float deviceAspect, float sideInsetNorm)
        {
            float play = WorldBuilder.PlayHalfWidth();
            if (deviceAspect >= WorldBuilder.PortraitAspect) return play;
            if (sideInsetNorm < 0f) sideInsetNorm = 0f;
            if (sideInsetNorm > 0.45f) sideInsetNorm = 0.45f;
            return play * (1f - 2f * sideInsetNorm);
        }

        public static bool Fits(int count, float safeHalf)
        {
            count = Clamp(count);
            if (safeHalf <= BulbHalf) return false;
            float width = BulbHalf * 2f;
            for (int i = 0; i < count; i++)
            {
                float x = X(i, count);
                if (x - BulbHalf < -safeHalf - 0.001f) return false;
                if (x + BulbHalf > safeHalf + 0.001f) return false;
                for (int j = i + 1; j < count; j++)
                {
                    float gap = x - X(j, count);
                    if (gap < 0f) gap = -gap;
                    if (gap + 0.02f < width) return false;
                }
            }
            return true;
        }
    }
}

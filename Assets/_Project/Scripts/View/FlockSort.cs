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
    }
}

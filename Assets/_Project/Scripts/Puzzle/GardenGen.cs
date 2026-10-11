namespace FlockFive
{
    // Shared knobs for a generated garden. Band OS is three feeders plus an
    // ostrich. The hash covers that shape and not the peck seed, so a seed
    // redraw does not look like a new board.
    public static class GardenGen
    {
        public const int BandOsFeeders = 3;

        public struct Knobs
        {
            public int Feeders;
            public bool Ostrich;
            public int PeckSeed;
            public int PecksPerMove;
        }

        public static int ResolvedFeeders(Knobs k)
        {
            int n = k.Feeders < 1 ? 2 : FeederRow.Clamp(k.Feeders);
            if (k.Ostrich && n < 3) n = 3;
            return n;
        }

        public static void Apply(Board b, Knobs k)
        {
            if (b == null) return;
            b.SetFeeders(ResolvedFeeders(k));
            if (!k.Ostrich)
            {
                b.Ostrich = null;
                return;
            }
            int pecks = k.PecksPerMove > 0 ? k.PecksPerMove : OstrichState.DefaultPecks;
            b.Ostrich = new OstrichState { PeckSeed = k.PeckSeed, PecksPerMove = pecks };
        }

        public static ulong Hash(Board b, Knobs k)
        {
            ulong h = 14695981039346656037UL;
            Mix(ref h, (ulong)ResolvedFeeders(k));
            Mix(ref h, k.Ostrich ? 1UL : 0UL);
            if (b != null)
            {
                var copy = b.Clone();
                copy.Ostrich = null;
                string key = GardenSolve.Key(copy);
                for (int i = 0; i < key.Length; i++) Mix(ref h, key[i]);
            }
            return h;
        }

        static void Mix(ref ulong h, ulong v)
        {
            h ^= v;
            h *= 1099511628211UL;
        }
    }
}

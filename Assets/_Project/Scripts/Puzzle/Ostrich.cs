using System.Collections.Generic;

namespace FlockFive
{
    // Shared peck RNG. The mix matches the Python port bit for bit:
    // s = seed + 0x9E3779B97F4A7C15 * (index + 1), then one SplitMix64 step.
    // Index is the peck counter (two pecks per move), not a second Unity RNG.
    public struct SplitMix64
    {
        ulong s;

        public SplitMix64(long seed, int idx)
        {
            unchecked { s = (ulong)seed + 0x9E3779B97F4A7C15UL * (ulong)(idx + 1); }
        }

        public ulong Next()
        {
            unchecked
            {
                s += 0x9E3779B97F4A7C15UL;
                ulong z = s;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public int Below(int n) => n <= 1 ? 0 : (int)(Next() % (ulong)n);

        public void Shuffle<T>(List<T> a)
        {
            if (a == null) return;
            for (int i = a.Count - 1; i > 0; i--)
            {
                int j = Below(i + 1);
                var t = a[i];
                a[i] = a[j];
                a[j] = t;
            }
        }
    }

    // Stage-authored pest. Lives on the board so a clone, a restart, and the
    // solver key all see the same fight. Null means this garden has no ostrich.
    public sealed class OstrichState
    {
        public const int HitsToFlee = 3;
        public const int DefaultPecks = 2;

        public int Hits;
        public int PeckIndex;
        public ulong Bonked;
        public long PeckSeed;
        public int PecksPerMove = DefaultPecks;
        // Feeder the head is blocking. -1 until a match names one.
        public int BlockSlot = -1;

        public bool Alive => Hits < HitsToFlee;

        public OstrichState Clone() => (OstrichState)MemberwiseClone();

        // Restart puts the bird back at full health. The seed stays.
        public void Reset()
        {
            Hits = 0;
            PeckIndex = 0;
            Bonked = 0;
            BlockSlot = -1;
        }
    }

    public static class OstrichPest
    {
        // One completed set, any live feeder. The branch stays. The third hit
        // flees and the normal drain collects every ready set.
        public static void Bonk(Board b, int branch, int slot)
        {
            if (b == null) return;
            var o = b.Ostrich;
            if (o == null || !o.Alive) return;
            if ((uint)branch >= (uint)b.Branches.Count) return;
            if (branch < 64 && (o.Bonked & (1UL << branch)) != 0) return;
            var br = b.Branches[branch];
            if (br == null || br.Broken || !br.IsFullMatch(out _)) return;
            o.Hits++;
            if (branch < 64) o.Bonked |= 1UL << branch;
            if (slot >= 0) o.BlockSlot = slot;
            if (!o.Alive) GardenSolve.Drain(b);
        }

        // Full matches in limb order, while the ostrich is still up.
        public static void Settle(Board b)
        {
            if (b == null) return;
            var o = b.Ostrich;
            if (o != null && o.Alive)
            {
                for (int i = 0; i < b.Branches.Count && o.Alive; i++)
                {
                    var br = b.Branches[i];
                    if (br == null || br.Broken) continue;
                    if (i < 64 && (o.Bonked & (1UL << i)) != 0) continue;
                    if (!br.IsFullMatch(out _)) continue;
                    b.ApplyCollect(i);
                }
            }
            if (b.Ostrich == null || !b.Ostrich.Alive)
                GardenSolve.Drain(b);
        }

        // TryMove already happened. Bonk that set, peck, bonk again.
        public static void AfterMove(Board b)
        {
            if (b == null || b.Ostrich == null) return;
            Settle(b);
            var o = b.Ostrich;
            int n = o.PecksPerMove;
            if (n < 0) n = 0;
            for (int k = 0; k < n && o.Alive; k++)
            {
                OstrichPeck.Scatter(b);
                Settle(b);
            }
            Settle(b);
        }
    }

    public static class OstrichPeck
    {
        // One unhidden tip bird hops to another limb with room.
        // Never a full set, never a five-run. PeckIndex advances even when
        // nothing legal was found, so the next peck stays on the shared stream.
        public static bool Scatter(Board b)
        {
            if (b == null || b.Ostrich == null || !b.Ostrich.Alive) return false;
            var o = b.Ostrich;
            var rng = new SplitMix64(o.PeckSeed, o.PeckIndex);
            o.PeckIndex++;

            var src = new List<int>();
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null || br.Broken || br.AdLocked || br.Count == 0) continue;
                if (br.TipLocked || br.IsShrouded(br.Count - 1)) continue;
                if (br.IsFullMatch(out _)) continue;
                src.Add(i);
            }
            rng.Shuffle(src);
            for (int s = 0; s < src.Count; s++)
            {
                int from = src[s];
                var a = b.Branches[from];
                var bird = a.Birds[a.Count - 1];
                var dst = new List<int>();
                for (int t = 0; t < b.Branches.Count; t++)
                {
                    var br = b.Branches[t];
                    if (t == from || br == null || br.Broken || br.AdLocked) continue;
                    if (br.Free <= 0 || br.IsFullMatch(out _)) continue;
                    dst.Add(t);
                }
                rng.Shuffle(dst);
                for (int d = 0; d < dst.Count; d++)
                {
                    var tb = b.Branches[dst[d]];
                    a.AlignShroud();
                    tb.AlignShroud();
                    tb.Birds.Add(bird);
                    tb.Shrouded.Add(false);
                    if (!tb.IsFullMatch(out _) && tb.TipRun() < 5)
                    {
                        a.Birds.RemoveAt(a.Count - 1);
                        if (a.Shrouded.Count > a.Birds.Count)
                            a.Shrouded.RemoveAt(a.Shrouded.Count - 1);
                        if (a.RevealExposed() > 0) b.JustUnveiled = true;
                        return true;
                    }
                    tb.Birds.RemoveAt(tb.Count - 1);
                    if (tb.Shrouded.Count > tb.Birds.Count)
                        tb.Shrouded.RemoveAt(tb.Shrouded.Count - 1);
                }
            }
            return false;
        }
    }
}

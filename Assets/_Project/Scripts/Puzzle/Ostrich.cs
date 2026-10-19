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
        // Limb whose full set landed the latest hit. -1 until a set bonks. The view
        // strikes it when the last hit comes with no peck, so the fight is never skipped.
        public int LastBonkBranch = -1;

        public bool Alive => Hits < HitsToFlee;

        public OstrichState Clone() => (OstrichState)MemberwiseClone();

        // Restart puts the bird back at full health. The seed stays.
        public void Reset()
        {
            Hits = 0;
            PeckIndex = 0;
            Bonked = 0;
            BlockSlot = -1;
            LastBonkBranch = -1;
        }
    }

    // One peck, as the board saw it. Counts are the limb sizes just before the hop,
    // so a view can check its own stacks still match before it animates the bird.
    public struct PeckHit
    {
        public int From;
        public int FromCount;
        public int To;
        public int ToCount;
        public Bird Bird;
    }

    public static class OstrichPest
    {
        // Root cause of the "five in a row just sits there" report: Bonked remembers a limb forever. A
        // bonked set stays on its limb while the ostrich lives; if the player breaks it up and later
        // completes a set on that same limb, the stale bit made Bonk/Settle skip it: no hit, no collect.
        // A limb that is no longer a full set forgets its bonk, so the next completion counts.
        public static void ForgetBrokenSets(Board b)
        {
            var o = b?.Ostrich;
            if (o == null || o.Bonked == 0) return;
            for (int i = 0; i < b.Branches.Count && i < 64; i++)
            {
                if ((o.Bonked & (1UL << i)) == 0) continue;
                var br = b.Branches[i];
                if (br != null && !br.Broken && !br.IsFullMatch(out _)) o.Bonked &= ~(1UL << i);
            }
        }

        // True when a living ostrich has a complete set that has not been counted yet.
        public static bool HasPendingSet(Board b)
        {
            var o = b?.Ostrich;
            if (o == null || !o.Alive) return false;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null || br.Broken || !br.IsFullMatch(out _)) continue;
                if (i < 64 && (o.Bonked & (1UL << i)) != 0) continue;
                return true;
            }
            return false;
        }

        // One completed set, any live feeder. The branch stays. The third hit
        // flees and the normal drain collects every ready set.
        public static void Bonk(Board b, int branch, int slot)
        {
            if (b == null) return;
            var o = b.Ostrich;
            if (o == null || !o.Alive) return;
            if ((uint)branch >= (uint)b.Branches.Count) return;
            ForgetBrokenSets(b);
            if (branch < 64 && (o.Bonked & (1UL << branch)) != 0) return;
            var br = b.Branches[branch];
            if (br == null || br.Broken || !br.IsFullMatch(out _)) return;
            o.Hits++;
            if (branch < 64) o.Bonked |= 1UL << branch;
            if (slot >= 0) o.BlockSlot = slot;
            o.LastBonkBranch = branch;
            if (!o.Alive) GardenSolve.Drain(b);
        }

        // Full matches in limb order, while the ostrich is still up.
        public static void Settle(Board b)
        {
            if (b == null) return;
            var o = b.Ostrich;
            ForgetBrokenSets(b);
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
        public static void AfterMove(Board b) => AfterMove(b, null, out _);

        // Same board math. Every landed peck is appended to log (may be null) so the
        // view can play it. finaleBranch is >= 0 when the ostrich died in this call
        // without a single peck: the view must still stage a strike on that set before
        // the ostrich leaves. -1 otherwise.
        public static void AfterMove(Board b, List<PeckHit> log, out int finaleBranch)
        {
            finaleBranch = -1;
            if (b == null || b.Ostrich == null) return;
            var o = b.Ostrich;
            bool aliveBefore = o.Alive;
            int logged = 0;
            int hitsBefore = o.Hits;
            Settle(b);
            int n = o.PecksPerMove;
            if (n < 0) n = 0;
            // Precedence (Brandon): a move that completes a set scores and hits the ostrich first,
            // and he does not attack on that move. His pecks only follow a move that landed no hit.
            if (o.Hits > hitsBefore) n = 0;
            for (int k = 0; k < n && o.Alive; k++)
            {
                if (OstrichPeck.Scatter(b, out var hit))
                {
                    logged++;
                    if (log != null) log.Add(hit);
                }
                Settle(b);
            }
            Settle(b);
            if (aliveBefore && !o.Alive && logged == 0) finaleBranch = o.LastBonkBranch;
        }
    }

    public static class OstrichPeck
    {
        // One unhidden tip bird hops to another limb with room.
        // Never a full set, never a five-run. PeckIndex advances even when
        // nothing legal was found, so the next peck stays on the shared stream.
        public static bool Scatter(Board b) => Scatter(b, out _);

        public static bool Scatter(Board b, out PeckHit hit)
        {
            hit = default;
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
                    int fromCount = a.Count;
                    int toCount = tb.Count;
                    tb.Birds.Add(bird);
                    tb.Shrouded.Add(false);
                    if (!tb.IsFullMatch(out _) && tb.TipRun() < 5)
                    {
                        a.Birds.RemoveAt(a.Count - 1);
                        if (a.Shrouded.Count > a.Birds.Count)
                            a.Shrouded.RemoveAt(a.Shrouded.Count - 1);
                        if (a.RevealExposed() > 0) b.JustUnveiled = true;
                        hit = new PeckHit { From = from, FromCount = fromCount, To = dst[d], ToCount = toCount, Bird = bird };
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

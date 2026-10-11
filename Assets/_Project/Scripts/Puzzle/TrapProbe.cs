using System;
using System.Collections.Generic;
using System.Text;

namespace FlockFive
{
    // How often a short-sighted hop order freezes a garden, and whether a freeze
    // is hidden info (the same hop wins under another arrangement of the bees).
    // Shared by the ease-band tests. Seeded, so a run is the same on every machine.
    public static class TrapProbe
    {
        public const int NodeCap = 250000;
        // Screen for "this hop is already a loss" before the full 250k confirm.
        public const int ScreenCap = 12000;
        public const int EaseSeed = 831825;
        public const int EaseSteps = 120;
        public const int EaseCap = 2500;

        public enum Play
        {
            Random = 0,
            MaxScore = 1,
            Stack = 2,
            Near = 3,
            Mix = 4,
            // Uniform among hops that keep a whole limb off an empty perch.
            Sensible = 5
        }

        // Uniform legal hop. The ease band counts a line as lost when the solver
        // cannot show a win inside EaseCap (a hard win is still a tangle for a
        // short-sighted player).
        public const Play EasePlay = Play.Random;

        // An ostrich garden is exempt from the hidden-info trap probe.
        // The pecks already scramble the line.
        public static bool Exempt(Level level) => level != null && level.Ostrich;

        public struct Report
        {
            public int Runs;
            public int Fatal;
            public int HiddenSigs;
            public int Open;
            public float Rate;
        }

        public struct ProofHit
        {
            public bool Found;
            public string Path;
            public int From;
            public int To;
            public int[] Alt;
        }

        struct Mix
        {
            ulong _s;

            public Mix(int seed)
            {
                _s = (ulong)(uint)seed;
                if (_s == 0) _s = 0x9E3779B97F4A7C15UL;
            }

            public uint Next()
            {
                ulong z = (_s += 0x9E3779B97F4A7C15UL);
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return (uint)(z ^ (z >> 31));
            }

            public int Range(int n) => n <= 1 ? 0 : (int)(Next() % (uint)n);
        }

        struct Slot
        {
            public int Branch;
            public int Index;
        }

        public static Report Measure(Board start, int runs, int seed, Play play, bool huntSigs = false, bool outlook = false)
            => Measure(start, runs, seed, play, huntSigs, outlook, ScreenCap, 0, null);

        public static Report Measure(Board start, int runs, int seed, Play play, bool huntSigs, bool outlook, int outlookCap)
            => Measure(start, runs, seed, play, huntSigs, outlook, outlookCap, 0, null);

        // maxMs stops the sample early so a slow garden can report what it finished.
        // progress(run, fatalSoFar) fires after every run when the caller wants a heartbeat.
        public static Report Measure(Board start, int runs, int seed, Play play, bool huntSigs, bool outlook,
            int outlookCap, int maxMs, Action<int, int> progress, bool closed = false)
        {
            var report = new Report { Runs = runs < 0 ? 0 : runs };
            if (start == null || report.Runs == 0) return report;
            int sigs = 0;
            int done = 0;
            var cache = outlook ? new Dictionary<string, GardenSolve.Outlook>() : null;
            var watch = maxMs > 0 ? System.Diagnostics.Stopwatch.StartNew() : null;
            for (int r = 0; r < report.Runs; r++)
            {
                if (watch != null && watch.ElapsedMilliseconds > maxMs) break;
                bool hunt = huntSigs && sigs == 0;
                if (FatalGame(start, play, unchecked(seed + r * 7919), hunt, outlook, outlookCap, cache, closed, out bool hidden, out bool open))
                {
                    report.Fatal++;
                    if (hidden) sigs++;
                }
                else if (open) report.Open++;
                done++;
                progress?.Invoke(done, report.Fatal);
            }
            report.Runs = done;
            report.HiddenSigs = sigs;
            report.Rate = done == 0 ? 0f : report.Fatal / (float)done;
            return report;
        }

        // A random playout that the solver calls a loss, plus a hidden reshuffle
        // that makes that same hop win. Used to refresh the recorded trap proofs.
        public static ProofHit FindPlayed(Board start, int seed, int runs, int outlookCap)
            => FindPlayed(start, seed, runs, outlookCap, null, 0);

        public static ProofHit FindPlayed(Board start, int seed, int runs, int outlookCap, Action<int> onRun, int witnessCap)
        {
            var hit = new ProofHit();
            if (start == null || runs <= 0) return hit;
            int saved = AltWinCap;
            AltWinCap = witnessCap > 0 ? witnessCap : (outlookCap < 1 ? 1 : outlookCap);
            try
            {
                var cache = new Dictionary<string, GardenSolve.Outlook>();
                var hops = new List<GardenSolve.HopChoice>(32);
                var path = new List<int>(64);
                for (int r = 0; r < runs && !hit.Found; r++)
                {
                    onRun?.Invoke(r);
                    var mix = new Mix(unchecked(seed + r * 7919));
                    var b = start.Clone();
                    GardenSolve.Drain(b);
                    path.Clear();
                    for (int step = 0; step < 60; step++)
                    {
                        if (b.Won) break;
                        GardenSolve.CollectHops(b, hops);
                        if (hops.Count == 0) break;
                        var hop = hops[mix.Range(hops.Count)];
                        var state = b.Clone();
                        if (!b.TryMove(hop.From, hop.To, out _)) break;
                        GardenSolve.Drain(b);
                        bool lost = !b.Won && !b.HasHop();
                        if (!lost && !b.Won && b.HasHop())
                        {
                            string key = GardenSolve.Key(b);
                            if (!cache.TryGetValue(key, out var look))
                            {
                                look = GardenSolve.Search(b, outlookCap).Outlook;
                                cache[key] = look;
                            }
                            lost = look == GardenSolve.Outlook.Tangled;
                        }
                        if (!lost)
                        {
                            path.Add(hop.From);
                            path.Add(hop.To);
                            continue;
                        }
                        if (!TryWitness(state, hop.From, hop.To, out int[] alt)) break;
                        string text = PathOf(path);
                        if (!Confirm(start, text, hop.From, hop.To, alt)) break;
                        hit.Found = true;
                        hit.Path = text;
                        hit.From = hop.From;
                        hit.To = hop.To;
                        hit.Alt = alt;
                        break;
                    }
                }
            }
            finally
            {
                AltWinCap = saved;
            }
            return hit;
        }

        // One reachable hidden-info trap: a winnable perch, a hop that is stuck,
        // and a reshuffle of the hidden birds that makes that same hop winnable.
        // The ad gift also has to rescue the stuck hop.
        public static ProofHit FindProof(Board start, int seed, int deviations, int randomWalks)
        {
            var hit = new ProofHit();
            if (start == null) return hit;
            var seen = new HashSet<string>();
            var hops = new List<GardenSolve.HopChoice>(32);
            var path = new List<int>(64);
            int witnesses = 0;
            const int witnessCap = 240;

            var greedy = new Mix(seed);
            if (Walk(start, hops, path, seen, ref witnesses, witnessCap, ref hit, -1, ref greedy))
                return hit;
            for (int d = 0; d < deviations && witnesses < witnessCap && !hit.Found; d++)
            {
                var mix = new Mix(seed + 10007 * (d + 1));
                Walk(start, hops, path, seen, ref witnesses, witnessCap, ref hit, d, ref mix);
            }
            var rand = new Mix(seed ^ 0x51A7);
            for (int w = 0; w < randomWalks && witnesses < witnessCap && !hit.Found; w++)
                Walk(start, hops, path, seen, ref witnesses, witnessCap, ref hit, -2, ref rand);
            return hit;
        }

        public static bool Confirm(Board start, string path, int from, int to, int[] alt)
        {
            if (start == null || alt == null) return false;
            var s = start.Clone();
            GardenSolve.Drain(s);
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var tok in path.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var ft = tok.Split('>');
                    if (ft.Length != 2) return false;
                    s = Apply(s, int.Parse(ft[0]), int.Parse(ft[1]));
                    if (s == null) return false;
                }
            }
            if (!Wins(s)) return false;
            var post = Apply(s, from, to);
            if (post == null || post.Won) return false;
            if (GardenSolve.Search(post, NodeCap).Outlook != GardenSolve.Outlook.Tangled) return false;
            var painted = s.Clone();
            int j = 0;
            for (int i = 0; i < painted.Branches.Count; i++)
            {
                var br = painted.Branches[i];
                if (br.Broken) continue;
                for (int k = 0; k < br.Count; k++)
                {
                    if (!br.IsShrouded(k)) continue;
                    if (j >= alt.Length) return false;
                    br.Birds[k] = new Bird((BirdColor)(alt[j] / 3), (BirdKit)(alt[j] % 3));
                    j++;
                }
            }
            if (j != alt.Length) return false;
            if (!Wins(Apply(painted, from, to))) return false;
            return Wins(Apply(Unlock(s), from, to));
        }

        static bool Walk(Board start, List<GardenSolve.HopChoice> hops, List<int> path, HashSet<string> seen,
            ref int witnesses, int witnessCap, ref ProofHit hit, int deviate, ref Mix mix)
        {
            var b = start.Clone();
            GardenSolve.Drain(b);
            path.Clear();
            for (int depth = 0; depth < 36; depth++)
            {
                if (b.Won) return false;
                GardenSolve.CollectHops(b, hops);
                if (hops.Count == 0) return false;
                string key = GardenSolve.Key(b);
                if (seen.Add(key) && Scan(start, b, hops, path, ref witnesses, witnessCap, ref hit))
                    return true;
                if (witnesses >= witnessCap) return false;
                int pick = 0;
                if (deviate == -2) pick = mix.Range(hops.Count);
                else if (deviate >= 0 && depth == deviate && hops.Count > 1) pick = 1;
                var step = hops[pick];
                if (!b.TryMove(step.From, step.To, out _)) return false;
                GardenSolve.Drain(b);
                path.Add(step.From);
                path.Add(step.To);
            }
            return false;
        }

        static bool Scan(Board start, Board state, List<GardenSolve.HopChoice> hops, List<int> path,
            ref int witnesses, int witnessCap, ref ProofHit hit)
        {
            for (int h = 0; h < hops.Count; h++)
            {
                if (witnesses >= witnessCap) return false;
                var post = Apply(state, hops[h].From, hops[h].To);
                if (post == null || post.Won) continue;
                if (post.HasHop())
                {
                    var look = GardenSolve.Search(post, ScreenCap);
                    if (look.Outlook != GardenSolve.Outlook.Tangled) continue;
                }
                witnesses++;
                if (!TryWitness(state, hops[h].From, hops[h].To, out int[] alt)) continue;
                var candidate = new ProofHit
                {
                    Found = true,
                    Path = PathOf(path),
                    From = hops[h].From,
                    To = hops[h].To,
                    Alt = alt
                };
                if (!Confirm(start, candidate.Path, candidate.From, candidate.To, candidate.Alt)) continue;
                hit = candidate;
                return true;
            }
            return false;
        }

        static int AltWinCap = 1;

        static bool FatalGame(Board start, Play play, int seed, bool huntSig, bool outlook, int outlookCap,
            Dictionary<string, GardenSolve.Outlook> cache, bool closed, out bool hidden, out bool open)
        {
            hidden = false;
            open = false;
            var b = start.Clone();
            GardenSolve.Drain(b);
            var hops = new List<GardenSolve.HopChoice>(32);
            var mix = new Mix(seed);
            Board prev = null;
            int from = -1, to = -1;
            for (int step = 0; step < EaseSteps; step++)
            {
                if (b.Won) return false;
                GardenSolve.CollectHops(b, hops);
                if (hops.Count == 0)
                {
                    if (huntSig && prev != null)
                        hidden = TryWitness(prev, from, to, out _);
                    return true;
                }
                var stepHop = Pick(play, b, hops, ref mix);
                if (huntSig) prev = b.Clone();
                from = stepHop.From;
                to = stepHop.To;
                if (!b.TryMove(from, to, out _)) return false;
                GardenSolve.Drain(b);
                if (!outlook || b.Won || !b.HasHop()) continue;
                string key = GardenSolve.Key(b);
                GardenSolve.Outlook look;
                if (cache == null || !cache.TryGetValue(key, out look))
                {
                    look = GardenSolve.Search(b, outlookCap).Outlook;
                    if (cache != null) cache[key] = look;
                }
                bool lost = look == GardenSolve.Outlook.Tangled
                    || (closed && look == GardenSolve.Outlook.Unknown);
                if (!lost) continue;
                if (huntSig) hidden = TryWitness(prev, from, to, out _);
                return true;
            }
            open = true;
            return false;
        }

        static GardenSolve.HopChoice Pick(Play play, Board b, List<GardenSolve.HopChoice> hops, ref Mix mix)
        {
            if (play == Play.Sensible) return SensibleHop(b, hops, ref mix);
            if (play == Play.Random || hops.Count == 1) return hops[mix.Range(hops.Count)];
            int best = hops[0].Score;
            if (play == Play.MaxScore)
            {
                int n = 1;
                while (n < hops.Count && hops[n].Score == best) n++;
                return hops[mix.Range(n)];
            }
            if (play == Play.Stack)
            {
                int n = 0;
                while (n < hops.Count && hops[n].Score >= 40) n++;
                if (n == 0) n = hops.Count;
                return hops[mix.Range(n)];
            }
            if (play == Play.Near)
            {
                int floor = best - 20;
                int n = 0;
                while (n < hops.Count && hops[n].Score >= floor) n++;
                if (n == 0) n = hops.Count;
                return hops[mix.Range(n)];
            }
            if (mix.Range(10) < 3) return hops[mix.Range(hops.Count)];
            int top = 1;
            while (top < hops.Count && hops[top].Score == best) top++;
            return hops[mix.Range(top)];
        }

        // A whole limb poured onto an empty perch is the move sensible play skips.
        static GardenSolve.HopChoice SensibleHop(Board b, List<GardenSolve.HopChoice> hops, ref Mix mix)
        {
            int keep = 0;
            var idx = new int[hops.Count];
            for (int i = 0; i < hops.Count; i++)
            {
                var src = b.Branches[hops[i].From];
                var dst = b.Branches[hops[i].To];
                if (dst.Empty && src.Count == src.TipRun()) continue;
                idx[keep++] = i;
            }
            if (keep == 0) return hops[mix.Range(hops.Count)];
            return hops[idx[mix.Range(keep)]];
        }

        static bool TryWitness(Board state, int from, int to, out int[] alt)
        {
            alt = null;
            var all = new List<Slot>();
            var inner = new List<int>();
            Shrouds(state, all, inner);
            if (inner.Count >= 2 && TryOrders(state, all, inner, from, to, out alt)) return true;
            if (all.Count >= 2 && (inner.Count < 2 || all.Count != inner.Count))
            {
                var every = new List<int>(all.Count);
                for (int i = 0; i < all.Count; i++) every.Add(i);
                if (TryOrders(state, all, every, from, to, out alt)) return true;
            }
            return false;
        }

        static bool TryOrders(Board state, List<Slot> all, List<int> which, int from, int to, out int[] alt)
        {
            alt = null;
            int k = which.Count;
            var birds = new Bird[k];
            var origin = new Bird[k];
            for (int i = 0; i < k; i++)
            {
                var s = all[which[i]];
                birds[i] = state.Branches[s.Branch].Birds[s.Index];
                origin[i] = birds[i];
            }
            // A short hidden set can be tried in full. Deeper witness caps stay on
            // swaps, 3-cycles, and a handful of shuffles so one fatal cannot fan out.
            int enumerate = AltWinCap <= 1 ? 7 : 5;
            if (k <= enumerate) return Permute(state, all, which, birds, origin, from, to, 0, out alt);
            for (int i = 0; i < k; i++)
            {
                for (int j = i + 1; j < k; j++)
                {
                    Swap(birds, i, j);
                    if (!Same(birds, origin) && Accept(state, all, which, birds, from, to, out alt)) return true;
                    Swap(birds, i, j);
                }
            }
            if (Cycles(state, all, which, birds, from, to, out alt)) return true;
            var mix = new Mix(from * 997 + to * 13 + k * 31 + all.Count);
            for (int n = 0; n < 48; n++)
            {
                Array.Copy(origin, birds, k);
                for (int i = k - 1; i > 0; i--) Swap(birds, i, mix.Range(i + 1));
                if (Same(birds, origin)) continue;
                if (Accept(state, all, which, birds, from, to, out alt)) return true;
            }
            return false;
        }

        static bool Permute(Board state, List<Slot> all, List<int> which, Bird[] birds, Bird[] origin,
            int from, int to, int i, out int[] alt)
        {
            alt = null;
            if (i == birds.Length)
            {
                if (Same(birds, origin)) return false;
                return Accept(state, all, which, birds, from, to, out alt);
            }
            for (int j = i; j < birds.Length; j++)
            {
                Swap(birds, i, j);
                if (Permute(state, all, which, birds, origin, from, to, i + 1, out alt)) return true;
                Swap(birds, i, j);
            }
            return false;
        }

        static bool Cycles(Board state, List<Slot> all, List<int> which, Bird[] birds,
            int from, int to, out int[] alt)
        {
            alt = null;
            int k = birds.Length;
            for (int i = 0; i < k; i++)
            {
                for (int j = i + 1; j < k; j++)
                {
                    for (int m = j + 1; m < k; m++)
                    {
                        var bi = birds[i];
                        var bj = birds[j];
                        var bm = birds[m];
                        birds[i] = bm; birds[j] = bi; birds[m] = bj;
                        if (Accept(state, all, which, birds, from, to, out alt)) return true;
                        birds[i] = bj; birds[j] = bm; birds[m] = bi;
                        if (Accept(state, all, which, birds, from, to, out alt)) return true;
                        birds[i] = bi; birds[j] = bj; birds[m] = bm;
                    }
                }
            }
            return false;
        }

        static bool Accept(Board state, List<Slot> all, List<int> which, Bird[] birds, int from, int to, out int[] alt)
        {
            alt = null;
            var painted = state.Clone();
            for (int i = 0; i < which.Count; i++)
            {
                var s = all[which[i]];
                painted.Branches[s.Branch].Birds[s.Index] = birds[i];
            }
            var post = Apply(painted, from, to);
            if (post == null || post.Won) { if (post != null && post.Won) { alt = Encode(painted); return true; } return false; }
            if (GardenSolve.Search(post, AltWinCap).Outlook != GardenSolve.Outlook.Winnable) return false;
            alt = Encode(painted);
            return true;
        }

        static void Shrouds(Board b, List<Slot> all, List<int> inner)
        {
            all.Clear();
            inner.Clear();
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null || br.Broken) continue;
                for (int k = 0; k < br.Count; k++)
                {
                    if (!br.IsShrouded(k)) continue;
                    if (k < br.Count - 1) inner.Add(all.Count);
                    all.Add(new Slot { Branch = i, Index = k });
                }
            }
        }

        static int[] Encode(Board b)
        {
            var list = new List<int>();
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null || br.Broken) continue;
                for (int k = 0; k < br.Count; k++)
                {
                    if (!br.IsShrouded(k)) continue;
                    list.Add((int)br.Birds[k].Color * 3 + (int)br.Birds[k].Kit);
                }
            }
            return list.ToArray();
        }

        static bool FastWin(Board b)
        {
            if (b == null) return false;
            if (b.Won) return true;
            return GardenSolve.Search(b, 1).Outlook == GardenSolve.Outlook.Winnable;
        }

        static bool Wins(Board b)
        {
            if (b == null) return false;
            if (b.Won) return true;
            return GardenSolve.Search(b, NodeCap).Outlook == GardenSolve.Outlook.Winnable;
        }

        static Board Apply(Board s, int from, int to)
        {
            if (s == null) return null;
            var b = s.Clone();
            if (!b.TryMove(from, to, out _)) return null;
            GardenSolve.Drain(b);
            return b;
        }

        static Board Unlock(Board b)
        {
            var n = b.Clone();
            for (int i = 0; i < n.Branches.Count; i++) n.Branches[i].AdLocked = false;
            return n;
        }

        static string PathOf(List<int> moves)
        {
            var sb = new StringBuilder();
            for (int i = 0; i + 1 < moves.Count; i += 2)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(moves[i]).Append('>').Append(moves[i + 1]);
            }
            return sb.ToString();
        }

        static void Swap(Bird[] birds, int i, int j)
        {
            var t = birds[i];
            birds[i] = birds[j];
            birds[j] = t;
        }

        static bool Same(Bird[] a, Bird[] b)
        {
            for (int i = 0; i < a.Length; i++)
                if (a[i].Color != b[i].Color || a[i].Kit != b[i].Kit) return false;
            return true;
        }
    }
}

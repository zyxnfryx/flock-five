using System;
using System.Collections.Generic;
using System.Text;

namespace FlockFive
{
    // A hop order must not lose the round. Outlook.Tangled means this perch
    // is proven stuck (no legal hop, or the search emptied without a win).
    // Unknown means we ran out of nodes — fail open, never call it a loss.
    // The ice card does not use Tangled. It waits until HasHop is false, so an
    // empty open bonus (a legal destination) keeps the garden in play.
    public static class GardenSolve
    {
        public enum Outlook
        {
            Winnable,
            Tangled,
            Unknown
        }

        public struct Report
        {
            public Outlook Outlook;
            public int Moves;
            public int Nodes;
        }

        public static Outlook Look(Board board, int nodeCap = 20000) =>
            Search(board, nodeCap).Outlook;

        public static Report Search(Board board, int nodeCap = 20000)
        {
            if (board == null) return new Report { Outlook = Outlook.Tangled };
            var start = board.Clone();
            Drain(start);
            if (Cleared(start)) return new Report { Outlook = Outlook.Winnable, Moves = 0, Nodes = 1 };
            if (!start.HasHop() && start.FindCollect() < 0)
                return new Report { Outlook = Outlook.Tangled, Moves = 0, Nodes = 1 };

            var hops = new List<Hop>(32);
            var greedy = start.Clone();
            int gMoves = 0;
            for (int step = 0; step < 120; step++)
            {
                if (!BestHop(greedy, hops, out int from, out int to)) break;
                if (!greedy.TryMove(from, to, out _)) break;
                gMoves++;
                AfterMove(greedy);
                if (Cleared(greedy))
                    return new Report { Outlook = Outlook.Winnable, Moves = gMoves, Nodes = step + 1 };
            }

            var seen = new HashSet<Stamp>();
            var stack = new Stack<(Board board, int depth)>();
            stack.Push((start, 0));
            seen.Add(StampOf(start));
            int nodes = 0;

            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                nodes++;
                if (nodes > nodeCap)
                    return new Report { Outlook = Outlook.Unknown, Moves = cur.depth, Nodes = nodes };

                FillHops(cur.board, hops);
                for (int m = hops.Count - 1; m >= 0; m--)
                {
                    var next = cur.board.Clone();
                    if (!next.TryMove(hops[m].from, hops[m].to, out _)) continue;
                    AfterMove(next);
                    if (Cleared(next))
                        return new Report
                        {
                            Outlook = Outlook.Winnable,
                            Moves = cur.depth + 1,
                            Nodes = nodes
                        };
                    if (!seen.Add(StampOf(next))) continue;
                    stack.Push((next, cur.depth + 1));
                }
            }

            return new Report { Outlook = Outlook.Tangled, Nodes = nodes };
        }

        public static void Drain(Board b)
        {
            if (b == null) return;
            // Opening sets bonk the ostrich. They do not peck: pecks follow a move.
            if (b.Ostrich != null && b.Ostrich.Alive)
            {
                OstrichPest.Settle(b);
                return;
            }
            for (int n = 0; n < 32; n++)
            {
                int i = b.FindCollect();
                if (i < 0) return;
                b.ApplyCollect(i);
            }
        }

        // The hop is already on the board. An ostrich bonks and pecks.
        // Anything else collects the ready sets.
        public static void AfterMove(Board b)
        {
            if (b == null) return;
            if (b.Ostrich != null && b.Ostrich.Alive)
                OstrichPest.AfterMove(b);
            else
                Drain(b);
        }

        // The garden is clear only once a living ostrich has fled.
        public static bool Cleared(Board b) =>
            b != null && b.Won && (b.Ostrich == null || !b.Ostrich.Alive);

        static bool BestHop(Board b, List<Hop> hops, out int from, out int to)
        {
            FillHops(b, hops);
            from = -1;
            to = -1;
            if (hops.Count == 0) return false;
            from = hops[0].from;
            to = hops[0].to;
            return true;
        }

        struct Hop
        {
            public int from;
            public int to;
            public int score;
        }

        // Two independent mixes of the same fields Key writes. The search
        // visited-set stays allocation-free; a collision would have to hit both.
        struct Stamp : IEquatable<Stamp>
        {
            public ulong A;
            public ulong B;
            public bool Equals(Stamp other) => A == other.A && B == other.B;
            public override bool Equals(object obj) => obj is Stamp other && Equals(other);
            public override int GetHashCode() =>
                unchecked((int)A ^ (int)(A >> 32) ^ (int)B ^ (int)(B >> 32));
        }

        static Stamp StampOf(Board b)
        {
            ulong a = 14695981039346656037UL;
            ulong h = 0x6C62272E07BB0142UL;
            void Mix(int v)
            {
                a ^= (ulong)(uint)v;
                a *= 1099511628211UL;
                h ^= (ulong)(uint)v;
                h *= 0xBF58476D1CE4E5B9UL;
                h ^= h >> 31;
            }
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br.Broken) { Mix(1); continue; }
                if (br.AdLocked) { Mix(2); continue; }
                Mix(3);
                for (int k = 0; k < br.Count; k++)
                {
                    Mix(10 + (int)br.Birds[k].Color);
                    Mix(20 + (int)br.Birds[k].Kit);
                    Mix(br.IsShrouded(k) ? 31 : 30);
                }
            }
            Mix(4);
            if (b.Live != null)
            {
                for (int f = 0; f < b.Live.Length; f++)
                    Mix(b.Live[f].HasValue ? 40 + (int)b.Live[f].Value : 49);
            }
            Mix(5);
            for (int i = 0; i < b.Queue.Count; i++) Mix(50 + (int)b.Queue[i]);
            if (b.Ostrich != null)
            {
                var o = b.Ostrich;
                Mix(60 + o.Hits);
                Mix((int)(o.Bonked & 0xffffffffUL));
                Mix((int)(o.Bonked >> 32));
                // Peck index drops out of the key once the bird has fled.
                Mix(o.Alive ? 80 + (o.PeckIndex & 0xffff) : 79);
            }
            return new Stamp { A = a, B = h };
        }

        static readonly Comparison<Hop> ByScore = (x, y) => y.score.CompareTo(x.score);

        static void FillHops(Board b, List<Hop> hops)
        {
            hops.Clear();
            int n = b.Branches.Count;
            for (int from = 0; from < n; from++)
            {
                var br = b.Branches[from];
                if (br.Broken || br.Empty || br.TipLocked) continue;
                if (br.IsFullMatch(out _)) continue;
                for (int to = 0; to < n; to++)
                {
                    if (!b.CanMove(from, to, out int run) || run <= 0) continue;
                    hops.Add(new Hop { from = from, to = to, score = Score(b, from, to) });
                }
            }
            hops.Sort(ByScore);
        }

        static int Score(Board b, int from, int to)
        {
            if (b.Ostrich != null && b.Ostrich.Alive)
                return OstrichScore(b, from, to);
            var a = b.Branches[from];
            var dst = b.Branches[to];
            int run = a.TipRun();
            int s = 0;
            if (!dst.Empty) s += 40;
            if (dst.Count + run == BranchState.Cap) s += 80;
            if (a.Count == run) s += 20;
            return s;
        }

        // Build onto a run first. An empty perch is last. Same order the
        // ostrich harness searches, and only while the bird is still up.
        static int OstrichScore(Board b, int from, int to)
        {
            var a = b.Branches[from];
            var dst = b.Branches[to];
            int run = a.TipRun();
            bool whole = run == a.Count;
            if (dst.Empty) return whole ? 0 : 10;
            return 100 + dst.TipRun() + run;
        }

        public struct HopChoice
        {
            public int From;
            public int To;
            public int Score;
        }

        // Legal hops, highest score first. Same score the solver uses for its greedy hop.
        public static void CollectHops(Board b, System.Collections.Generic.List<HopChoice> into)
        {
            if (into == null) return;
            into.Clear();
            if (b == null) return;
            int n = b.Branches.Count;
            for (int from = 0; from < n; from++)
            {
                var a = b.Branches[from];
                if (a.Broken || a.Empty || a.TipLocked) continue;
                if (a.IsFullMatch(out _)) continue;
                for (int to = 0; to < n; to++)
                {
                    if (!b.CanMove(from, to, out int run) || run <= 0) continue;
                    into.Add(new HopChoice { From = from, To = to, Score = Score(b, from, to) });
                }
            }
            into.Sort((x, y) => y.Score.CompareTo(x.Score));
        }

        public static string Key(Board b)
        {
            var sb = new StringBuilder(256);
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br.Broken) { sb.Append('X'); continue; }
                if (br.AdLocked) { sb.Append('G'); continue; }
                sb.Append('|');
                for (int k = 0; k < br.Count; k++)
                {
                    sb.Append((int)br.Birds[k].Color);
                    sb.Append((int)br.Birds[k].Kit);
                    sb.Append(br.IsShrouded(k) ? 'h' : '.');
                }
            }
            sb.Append('L');
            if (b.Live != null)
            {
                for (int f = 0; f < b.Live.Length; f++)
                    sb.Append(b.Live[f].HasValue ? ((int)b.Live[f].Value).ToString() : "-");
            }
            sb.Append('Q');
            for (int i = 0; i < b.Queue.Count; i++)
                sb.Append((int)b.Queue[i]);
            if (b.Ostrich != null)
            {
                var o = b.Ostrich;
                sb.Append('#').Append(o.Hits).Append(',').Append(o.Bonked).Append(',');
                sb.Append(o.Alive ? o.PeckIndex : -1);
            }
            return sb.ToString();
        }
    }
}

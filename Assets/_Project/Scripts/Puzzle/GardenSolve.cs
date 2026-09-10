using System.Collections.Generic;
using System.Text;

namespace FlockFive
{
    // A hop order must not lose the round. Outlook.Tangled means this perch
    // is proven stuck (no legal hop, or the search emptied without a win).
    // Unknown means we ran out of nodes — fail open, never call it a loss.
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
            if (start.Won) return new Report { Outlook = Outlook.Winnable, Moves = 0, Nodes = 1 };
            if (!start.HasHop() && start.FindCollect() < 0)
                return new Report { Outlook = Outlook.Tangled, Moves = 0, Nodes = 1 };

            var greedy = start.Clone();
            int gMoves = 0;
            for (int step = 0; step < 120; step++)
            {
                if (!BestHop(greedy, out int from, out int to)) break;
                if (!greedy.TryMove(from, to, out _)) break;
                gMoves++;
                Drain(greedy);
                if (greedy.Won)
                    return new Report { Outlook = Outlook.Winnable, Moves = gMoves, Nodes = step + 1 };
            }

            var seen = new HashSet<string>();
            var stack = new Stack<(Board board, int depth)>();
            stack.Push((start, 0));
            seen.Add(Key(start));
            int nodes = 0;

            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                nodes++;
                if (nodes > nodeCap)
                    return new Report { Outlook = Outlook.Unknown, Moves = cur.depth, Nodes = nodes };

                var hops = ListHops(cur.board);
                for (int m = hops.Count - 1; m >= 0; m--)
                {
                    var next = cur.board.Clone();
                    if (!next.TryMove(hops[m].from, hops[m].to, out _)) continue;
                    Drain(next);
                    if (next.Won)
                        return new Report
                        {
                            Outlook = Outlook.Winnable,
                            Moves = cur.depth + 1,
                            Nodes = nodes
                        };
                    if (!seen.Add(Key(next))) continue;
                    stack.Push((next, cur.depth + 1));
                }
            }

            return new Report { Outlook = Outlook.Tangled, Nodes = nodes };
        }

        public static void Drain(Board b)
        {
            if (b == null) return;
            for (int n = 0; n < 32; n++)
            {
                int i = b.FindCollect();
                if (i < 0) return;
                b.ApplyCollect(i);
            }
        }

        static bool BestHop(Board b, out int from, out int to)
        {
            from = -1;
            to = -1;
            int best = int.MinValue;
            var hops = ListHops(b);
            for (int i = 0; i < hops.Count; i++)
            {
                int s = Score(b, hops[i].from, hops[i].to);
                if (s <= best) continue;
                best = s;
                from = hops[i].from;
                to = hops[i].to;
            }
            return from >= 0;
        }

        struct Hop
        {
            public int from;
            public int to;
        }

        static List<Hop> ListHops(Board b)
        {
            var hops = new List<Hop>(32);
            int n = b.Branches.Count;
            for (int from = 0; from < n; from++)
            {
                var a = b.Branches[from];
                if (a.Broken || a.Empty || a.TipLocked) continue;
                if (a.IsFullMatch(out var wait) && !b.LiveHas(wait)) continue;
                for (int to = 0; to < n; to++)
                {
                    if (!b.CanMove(from, to, out int run) || run <= 0) continue;
                    hops.Add(new Hop { from = from, to = to });
                }
            }
            hops.Sort((x, y) => Score(b, y.from, y.to).CompareTo(Score(b, x.from, x.to)));
            return hops;
        }

        static int Score(Board b, int from, int to)
        {
            var a = b.Branches[from];
            var dst = b.Branches[to];
            int run = a.TipRun();
            int s = 0;
            if (!dst.Empty) s += 40;
            if (dst.Count + run == BranchState.Cap) s += 80;
            if (a.Count == run) s += 20;
            return s;
        }

        static string Key(Board b)
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
                    sb.Append((int)br.Birds[k].Sex);
                    sb.Append(br.IsShrouded(k) ? 'h' : '.');
                }
            }
            sb.Append('L');
            sb.Append(b.Live[0].HasValue ? ((int)b.Live[0].Value).ToString() : "-");
            sb.Append(b.Live[1].HasValue ? ((int)b.Live[1].Value).ToString() : "-");
            sb.Append('Q');
            for (int i = 0; i < b.Queue.Count; i++)
                sb.Append((int)b.Queue[i]);
            return sb.ToString();
        }
    }
}

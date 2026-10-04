using System.Collections.Generic;
using System.Text;

namespace FlockFive
{
    // Bird conservation. Every color on the board must match the stage seed.
    // Short birds are put back on a real perch; nothing is dropped for space.
    public static class BoardValidator
    {
        public struct Report
        {
            public bool Ok;
            public string Message;
        }

        public static int[] Counts(Board b)
        {
            var n = new int[Palette.Max];
            if (b == null) return n;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null || br.Broken) continue;
                for (int k = 0; k < br.Count; k++)
                {
                    int c = (int)br.Birds[k].Color;
                    if ((uint)c < (uint)n.Length) n[c]++;
                }
            }
            // In the air between a pest scrap and Resolve. Still this garden's birds.
            for (int i = 0; i < b.Displaced.Count; i++)
            {
                int c = (int)b.Displaced[i].Color;
                if ((uint)c < (uint)n.Length) n[c]++;
            }
            return n;
        }

        public static bool Same(int[] have, int[] expect)
        {
            int n = Palette.Max;
            if (have == null || expect == null) return false;
            for (int i = 0; i < n; i++)
            {
                int h = i < have.Length ? have[i] : 0;
                int e = i < expect.Length ? expect[i] : 0;
                if (h != e) return false;
            }
            return true;
        }

        public static Report Check(Board b, int[] expect)
        {
            var sb = new StringBuilder();
            bool ok = true;
            if (b == null)
                return new Report { Ok = false, Message = "no board" };
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null)
                {
                    ok = false;
                    sb.Append("null branch ").Append(i).Append("; ");
                    continue;
                }
                if (br.IsBonus && br.Broken)
                {
                    ok = false;
                    sb.Append("bonus ").Append(i).Append(" stayed broken; ");
                }
                if (br.Count > BranchState.Cap)
                {
                    ok = false;
                    sb.Append("branch ").Append(i).Append(" over cap; ");
                }
                if (!br.Broken)
                {
                    for (int k = 0; k < br.Count; k++)
                    {
                        if (k >= BranchState.Cap)
                        {
                            ok = false;
                            sb.Append("orphan seat ").Append(i).Append(':').Append(k).Append("; ");
                        }
                    }
                }
                else if (br.Count > 0)
                {
                    ok = false;
                    sb.Append("birds left on broken ").Append(i).Append("; ");
                }
            }
            var have = Counts(b);
            if (!Same(have, expect))
            {
                ok = false;
                sb.Append("counts");
                for (int c = 0; c < Palette.Max; c++)
                {
                    int h = c < have.Length ? have[c] : 0;
                    int e = expect != null && c < expect.Length ? expect[c] : 0;
                    if (h != e) sb.Append(' ').Append((BirdColor)c).Append(h).Append('/').Append(e);
                }
                sb.Append("; ");
            }
            return new Report { Ok = ok, Message = ok ? "ok" : sb.ToString() };
        }

        // Pull birds off broken limbs, then fill any color that is short.
        public static int Restore(Board b, int[] expect)
        {
            if (b == null || expect == null) return 0;
            int placed = 0;
            var pile = new List<Bird>();
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null) continue;
                if (br.IsBonus && br.Broken) br.Broken = false;
                if (!br.Broken || br.Count == 0) continue;
                pile.AddRange(br.Birds);
                br.Birds.Clear();
                br.Shrouded.Clear();
            }
            var have = Counts(b);
            for (int i = 0; i < pile.Count; i++)
            {
                int c = (int)pile[i].Color;
                if ((uint)c < (uint)have.Length) have[c]++;
            }
            for (int c = 0; c < Palette.Max && c < expect.Length; c++)
            {
                int shortBy = expect[c] - (c < have.Length ? have[c] : 0);
                var sex = SexOn(b, pile, (BirdColor)c);
                for (int k = 0; k < shortBy; k++)
                    pile.Add(new Bird((BirdColor)c, sex));
            }
            for (int i = 0; i < pile.Count; i++)
            {
                int home = FindSeat(b, pile[i], allowFull: false);
                if (home < 0) home = FindSeat(b, pile[i], allowFull: true);
                if (home < 0)
                {
                    b.Branches.Add(new BranchState());
                    home = b.Branches.Count - 1;
                }
                var st = b.Branches[home];
                st.Birds.Add(pile[i]);
                st.Shrouded.Add(false);
                st.AlignShroud();
                placed++;
            }
            return placed;
        }

        static BirdSex SexOn(Board b, List<Bird> pile, BirdColor color)
        {
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var br = b.Branches[i];
                if (br == null || br.Broken) continue;
                for (int k = 0; k < br.Count; k++)
                    if (br.Birds[k].Color == color) return br.Birds[k].Sex;
            }
            for (int i = 0; i < pile.Count; i++)
                if (pile[i].Color == color) return pile[i].Sex;
            return LevelData.SexFor(color);
        }

        static int FindSeat(Board b, Bird bird, bool allowFull)
        {
            int best = -1;
            int bestFree = -1;
            for (int i = 0; i < b.Branches.Count; i++)
            {
                var st = b.Branches[i];
                // Gifts open empty. A short flock must not land on one just because
                // it has the most free seats (that is how a new bonus arrived full).
                if (st == null || st.Broken || st.AdLocked || st.IsBonus || st.Free <= 0) continue;
                if (!allowFull && st.Count + 1 == BranchState.Cap)
                {
                    bool fill = true;
                    for (int k = 0; k < st.Count; k++)
                    {
                        if (st.IsShrouded(k) || !st.Birds[k].SameFlock(bird)) { fill = false; break; }
                    }
                    if (fill) continue;
                }
                if (st.Free > bestFree)
                {
                    bestFree = st.Free;
                    best = i;
                }
            }
            return best;
        }
    }
}

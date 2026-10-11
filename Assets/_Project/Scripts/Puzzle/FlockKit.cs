using System.Collections.Generic;

namespace FlockFive
{
    // The only place garden birds get a kit. Build 82: Crown, Bow, Plain (bare).
    // A color may fill at most three sets on one garden, one set per kit.
    public static class FlockKit
    {
        public const int MaxSetsPerColor = 3;
        public static readonly BirdKit[] Order = { BirdKit.Crown, BirdKit.Bow, BirdKit.Plain };

        public enum Seat
        {
            Head,
            Tuck
        }

        public static BirdKit Worn(Bird bird) => bird.Kit;
        public static bool BehindBody(BirdKit worn) => worn == BirdKit.Bow;
        public static Seat SeatOf(BirdKit worn) => worn == BirdKit.Bow ? Seat.Tuck : Seat.Head;

        // Kit of set `set` (0-based) of `color` on garden `levelNumber`. The start of
        // the Crown/Bow/Plain cycle rotates with level and color, so a one-set color
        // is not always Crown and the same color reads differently garden to garden.
        public static BirdKit KitFor(int levelNumber, BirdColor color, int set)
        {
            int rot = ((levelNumber + (int)color) % 3 + 3) % 3;
            if (set < 0) set = 0;
            if (set >= MaxSetsPerColor) set = MaxSetsPerColor - 1;
            return Order[(set + rot) % 3];
        }

        // Deterministic per garden. With a level kit map (Level.Kits) the packed
        // garden is painted from it (PaintMap). Without one, for each color, cut
        // the packed garden into runs (consecutive birds of that color on one
        // limb, trunk to tip, branch 0..n). Runs are dealt first-fit, in board
        // order, into the color's sets of five so a run stays one kit whenever it
        // fits; a run that fits nowhere whole is split bird by bird into the first
        // sets with room. Set s wears KitFor(levelNumber, color, s). Pack calls
        // this once, after the Shift clones and before the gifts. Nothing else
        // writes Bird.Kit except Paint (single-color gardens paint in the maker).
        public static void Assign(Board b, int levelNumber, string kitMap = null)
        {
            if (b == null) return;
            if (!string.IsNullOrEmpty(kitMap))
            {
                PaintMap(b, kitMap);
                return;
            }
            for (int c = 0; c < Palette.Max; c++)
            {
                var runs = new List<int[]>(); // {branch, start, length}
                int total = 0;
                for (int i = 0; i < b.Branches.Count; i++)
                {
                    var br = b.Branches[i];
                    if (br == null) continue;
                    int k = 0;
                    while (k < br.Birds.Count)
                    {
                        if ((int)br.Birds[k].Color != c) { k++; continue; }
                        int start = k;
                        while (k < br.Birds.Count && (int)br.Birds[k].Color == c) k++;
                        runs.Add(new[] { i, start, k - start });
                        total += k - start;
                    }
                }
                if (total == 0) continue;
                int sets = (total + BranchState.Cap - 1) / BranchState.Cap;
                var room = new int[sets];
                for (int s = 0; s < sets; s++) room[s] = BranchState.Cap;
                for (int r = 0; r < runs.Count; r++)
                {
                    var run = runs[r];
                    var br = b.Branches[run[0]];
                    int whole = -1;
                    for (int s = 0; s < sets; s++)
                        if (room[s] >= run[2]) { whole = s; break; }
                    for (int j = 0; j < run[2]; j++)
                    {
                        int s = whole;
                        if (s < 0)
                        {
                            s = sets - 1;
                            for (int t = 0; t < sets; t++) if (room[t] > 0) { s = t; break; }
                        }
                        room[s]--;
                        var bird = br.Birds[run[1] + j];
                        bird.Kit = KitFor(levelNumber, (BirdColor)c, s);
                        br.Birds[run[1] + j] = bird;
                    }
                }
            }
        }

        // Level-authored kits for the packed garden: one group per branch in board
        // order, separated by '/', one letter per bird trunk to tip (C Crown,
        // B Bow, N bare). Branches past the map (gifts) are left alone.
        public static void PaintMap(Board b, string kitMap)
        {
            var limbs = kitMap.Split('/');
            for (int i = 0; i < b.Branches.Count && i < limbs.Length; i++)
                Paint(b.Branches[i], limbs[i]);
        }

        // Authored kits (single-color gardens). "C" Crown, "B" Bow, "N" bare, one
        // letter per bird trunk to tip. Same validator rules apply afterwards.
        public static void Paint(BranchState br, string kits)
        {
            if (br == null || kits == null) return;
            for (int k = 0; k < br.Birds.Count && k < kits.Length; k++)
            {
                var bird = br.Birds[k];
                char ch = kits[k];
                bird.Kit = ch == 'C' ? BirdKit.Crown : ch == 'B' ? BirdKit.Bow : BirdKit.Plain;
                br.Birds[k] = bird;
            }
        }

        public static char Letter(BirdKit k) => k == BirdKit.Crown ? 'C' : k == BirdKit.Bow ? 'B' : 'N';
    }
}

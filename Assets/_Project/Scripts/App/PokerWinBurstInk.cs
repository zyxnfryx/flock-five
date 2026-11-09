using UnityEngine;

namespace FlockFive
{
    // Celebration "WIN $X" while it sits on the gold burst.
    // StampLight (the resting banner, the rank title, the rest of the poker chrome)
    // is a 2px black disc with a 1px white keyline. On the burst that keyline and the
    // remaining pixel of black wash into the rays — TestFlight 121, "WIN $100".
    // This stroke is one black disc, a share of the hero point size, even on the
    // diagonals because it is a LabelRim disc. It is computed from the fitted hero
    // size and then held: the sample count has to stay the same from Layout to
    // Repaint, and a screen-pixel disc stays registered to the fill while the word
    // punches and flies. The yellow fill is painted on top, unchanged.
    public static class PokerWinBurstInk
    {
        // 0.06 of the hero point size. On the portrait targets that is about
        // 7px in the iPad 820 column (hero ~112), 4px on iPhone 402 (~68) and
        // 412 (~70), and 12px on a native 1206-wide still (hero ~204).
        public const float OutlineEm = 0.06f;
        public const int MinPx = 4;
        // StampLight's black radius. The burst stroke is thicker than this.
        public const int ChromePx = 2;

        public static int OutlinePx(int heroPx)
        {
            if (heroPx < 1) heroPx = 1;
            int px = Mathf.RoundToInt(heroPx * OutlineEm);
            if (px < MinPx) px = MinPx;
            return LabelRim.Radius(px);
        }
    }
}

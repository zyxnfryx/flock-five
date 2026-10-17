using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // One arbiter for "which limb did this tap mean". Pure geometry: no scene lookups, so EditMode
    // tests can feed it candidates. Rank, in order:
    //   0. glove: during a tutorial the limb the glove points at wins any tap inside its tight rect
    //      (+GloveTol) or within GlovePad of one of its pads. StepTapGate is not involved.
    //   1. a tap inside a gift sign wins (the sign is drawn over every wood: sort 11 vs 2);
    //   2. otherwise the nearest tight visual rect (opaque wood art, NOT the sprite canvas);
    //      qualifies within WoodTol (sign: SignTol);
    //   3. ties: nearest pad (centre or seat).
    // A tap that qualifies for nothing returns -1 and the caller falls back to its old pad radius.
    public static class BranchHit
    {
        // Opaque alpha box of branch.png / branch_gift.png (1280x720 @140 PPU, wood scale 0.64 x 0.50 / 0.52),
        // in branch-local world units, un-mirrored (left limb). Right limbs mirror X (Wood.flipX).
        // Measured from the PNG alpha; BranchHitTests re-measures them so art changes fail loudly.
        public const float MainX0 = -2.697f, MainX1 = 2.720f, MainY0 = -0.557f, MainY1 = 0.564f;
        public const float GiftX0 = -2.697f, GiftX1 = 2.720f, GiftY0 = -0.579f, GiftY1 = 0.516f;

        public const float WoodTol = 0.12f;   // finger slop beyond the painted limb
        public const float SignTol = 0.25f;   // was 0.42 on a rotated AABB
        public const float GloveTol = 0.45f;
        public const float GlovePad = 1.25f;  // ~ one row pitch (1.12 .. 1.5)

        public struct Cand
        {
            public int Index;
            public bool Gift;
            public Rect Wood;
            public bool HasSign;
            public Rect Sign;
            public Vector2[] Pads;
        }

        public static Rect WoodRect(Vector2 center, bool fromRight, bool gift, float sx = 1f, float sy = 1f)
        {
            float x0 = gift ? GiftX0 : MainX0, x1 = gift ? GiftX1 : MainX1;
            float y0 = gift ? GiftY0 : MainY0, y1 = gift ? GiftY1 : MainY1;
            if (fromRight) { float t = x0; x0 = -x1; x1 = -t; }
            return Rect.MinMaxRect(center.x + x0 * sx, center.y + y0 * sy, center.x + x1 * sx, center.y + y1 * sy);
        }

        public static float RectDist(Rect r, Vector2 p)
        {
            float dx = Mathf.Max(Mathf.Max(r.xMin - p.x, 0f), p.x - r.xMax);
            float dy = Mathf.Max(Mathf.Max(r.yMin - p.y, 0f), p.y - r.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static float NearestPadSqr(Cand c, Vector2 p)
        {
            float best = float.MaxValue;
            if (c.Pads == null) return best;
            for (int i = 0; i < c.Pads.Length; i++)
            {
                float d = (c.Pads[i] - p).sqrMagnitude;
                if (d < best) best = d;
            }
            return best;
        }

        // Returns the winning candidate's Index, or -1. gift tells which kind won.
        public static int Pick(Vector2 world, IList<Cand> cands, int glove, out bool gift)
        {
            gift = false;
            if (cands == null) return -1;
            if (glove >= 0)
            {
                for (int i = 0; i < cands.Count; i++)
                {
                    var c = cands[i];
                    if (c.Index != glove) continue;
                    float d = RectDist(c.Wood, world);
                    if (c.HasSign) d = Mathf.Min(d, RectDist(c.Sign, world));
                    if (d <= GloveTol || NearestPadSqr(c, world) <= GlovePad * GlovePad)
                    {
                        gift = c.Gift;
                        return c.Index;
                    }
                }
            }
            int best = -1;
            bool bestGift = false;
            float bestD = float.MaxValue, bestPad = float.MaxValue;
            for (int i = 0; i < cands.Count; i++)
            {
                var c = cands[i];
                float dw = RectDist(c.Wood, world);
                float d = dw;
                bool ok = dw <= WoodTol;
                if (c.HasSign)
                {
                    float ds = RectDist(c.Sign, world);
                    if (ds <= 0f) { d = -1f; ok = true; }
                    else if (ds <= SignTol) { d = Mathf.Min(d, ds); ok = true; }
                }
                if (!ok) continue;
                float pad = NearestPadSqr(c, world);
                if (d < bestD || (Mathf.Approximately(d, bestD) && pad < bestPad))
                {
                    bestD = d; bestPad = pad; best = c.Index; bestGift = c.Gift;
                }
            }
            gift = best >= 0 && bestGift;
            return best;
        }
    }
}

#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120: the hold ring is a gold annulus. A band around the whole wing-tip box
    // measured ~270×180 on 820 and crossed the play edge on 402/412. The band now
    // follows the bird footprint and the outer stroke is clamped inside the board.
    static class SelectHighlightTests
    {
        // Opaque half-extents (alpha > 32) of the imported ruby sheet, 220 PPU, local units.
        // rest, wings across (_1), wings high (_2), lowered (_3), half-raised (_4).
        static readonly float[] HalfX = { 678f / 440f, 1120f / 440f, 939f / 440f, 1027f / 440f, 1040f / 440f };
        static readonly float[] HalfY = { 743f / 440f, 661f / 440f, 1143f / 440f, 661f / 440f, 814f / 440f };

        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[select-highlight] FAIL " + what); } }

            for (float p = 0f; p <= 1f; p += 0.25f)
                Check(BirdIdle.SelectRingAlpha(1f, p) >= BirdIdle.SelectRingAlphaFloor, "ring fades out at pulse " + p);
            Check(BirdIdle.SelectRingAlpha(0f, 1f) == 0f, "ring shows when not selected");
            Check(BirdIdle.SelectRingAlpha(0.5f, 1f) < BirdIdle.SelectRingAlpha(1f, 1f), "ring does not fade in");
            Check(BirdIdle.SelectRingAlpha(1f, 0f) <= BirdIdle.SelectRingAlpha(1f, 1f), "pulse dips below the floor");
            Check(!BirdIdle.SelectRingShown(0f) && !BirdIdle.SelectRingShown(BirdIdle.SelectRingOff), "ring stays after drop");
            Check(BirdIdle.SelectRingShown(1f), "ring hidden while held");

            Check(BirdIdle.SelectRingFoot > 0.75f && BirdIdle.SelectRingFoot < 0.95f, "footprint is the whole mesh box or a speck");
            Check(BirdIdle.SelectRingHole > 0.8f && BirdIdle.SelectRingHole < 0.93f, "band is a fat donut or a hairline");
            Check(BirdIdle.SelectRingEdge > BirdIdle.SelectRingHole && BirdIdle.SelectRingEdge < 1f, "ring edge leaves the sprite");
            Check(BirdIdle.SelectRingDarkMul > 1f && BirdIdle.SelectRingDarkMul < 1.2f, "dark stroke balloons past the bird");
            Check(BirdIdle.SelectRingPad > 0f && BirdIdle.SelectRingPad < 0.16f, "pad rebuilds the off-bird halo");

            for (int i = 0; i < HalfX.Length; i++)
            {
                BirdIdle.SelectRingExtents(HalfX[i], HalfY[i], out float hx, out float hy);
                Check(hx < HalfX[i] && hy < HalfY[i], "ring larger than the pose " + i);
                Check(hx > HalfX[i] * BirdIdle.SelectRingFoot && hy > HalfY[i] * BirdIdle.SelectRingFoot, "ring lost the footprint " + i);
                Check(hx * BirdIdle.SelectRingDarkMul < HalfX[i] && hy * BirdIdle.SelectRingDarkMul < HalfY[i], "dark stroke past the pose " + i);
                Check(BirdIdle.SelectRingInner(hx) < HalfX[i] && BirdIdle.SelectRingInner(hy) < HalfY[i], "hole wider than pose " + i);
                Check(BirdIdle.SelectRingClear(hx) < HalfX[i] && BirdIdle.SelectRingClear(hy) < HalfY[i], "feather opens beside pose " + i);
            }

            // Tip bird whose stroke crosses the left play edge slides back on, same size.
            float inset = BirdIdle.SelectRingBoardInset;
            float wantX = -1f + inset + 0.8f;
            BirdIdle.SelectRingClamp(-0.4f, 0.2f, 0.8f, 0.3f, -1f, 2f, -2f, 2f, out float cx, out float cy, out float chx, out float chy);
            Check(Mathf.Abs(cx - wantX) < 0.001f && Mathf.Abs(cy - 0.2f) < 0.001f, "clamp left the ring off the board");
            Check(Mathf.Abs(chx - 0.8f) < 0.001f && Mathf.Abs(chy - 0.3f) < 0.001f, "clamp shrunk a ring that fits");
            Check(cx - chx >= -1f - 0.001f && cx + chx <= 2f + 0.001f, "clamped ring still crosses the board");

            BirdIdle.SelectRingClamp(0f, 0.25f, 0.4f, 0.3f, -2f, 2f, -2f, 2f, out cx, out cy, out chx, out chy);
            Check(cx == 0f && Mathf.Abs(cy - 0.25f) < 0.001f && chx == 0.4f && chy == 0.3f, "clamp moved a ring that was already on the board");

            float room = 1f - inset;
            BirdIdle.SelectRingClamp(0.5f, 0f, 3f, 0.2f, -1f, 1f, -2f, 2f, out cx, out cy, out chx, out chy);
            Check(Mathf.Abs(chx - room) < 0.001f && chx < 3f, "clamp did not shrink a ring wider than the board");
            Check(cx - chx >= -1f - 0.001f && cx + chx <= 1f + 0.001f, "shrunk ring still crosses the board");

            Debug.Log("[select-highlight] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif

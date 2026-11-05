#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120: a held flock showed flight pose and intermittent sparkles, no lasting outline.
    // The glow disc (local scale 2.05) sat inside the flap sheet. The hold is now a gold
    // annulus outside the current pose, steady while the group stays lifted.
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

            Check(BirdIdle.SelectRingHole > 0.5f && BirdIdle.SelectRingHole < 0.85f, "band is a disc or a hairline");
            Check(BirdIdle.SelectRingEdge > BirdIdle.SelectRingHole && BirdIdle.SelectRingEdge < 1f, "ring edge leaves the sprite");
            Check(BirdIdle.SelectRingDarkMul > 1f && BirdIdle.SelectRingDarkMul < 1.4f, "dark stroke is not outside the gold");
            Check(BirdIdle.SelectRingPad >= 0.2f, "pad does not clear the flap sheet");

            for (int i = 0; i < HalfX.Length; i++)
            {
                BirdIdle.SelectRingExtents(HalfX[i], HalfY[i], out float hx, out float hy);
                Check(BirdIdle.SelectRingInner(hx) < HalfX[i], "hole wider than pose " + i + " x");
                Check(BirdIdle.SelectRingInner(hy) < HalfY[i], "hole taller than pose " + i + " y");
                Check(BirdIdle.SelectRingClear(hx) < HalfX[i], "feather opens beside pose " + i);
                Check(BirdIdle.SelectRingClear(hy) < HalfY[i], "feather opens above pose " + i);
                Check(hx >= HalfX[i] + BirdIdle.SelectRingPad && hy >= HalfY[i] + BirdIdle.SelectRingPad, "ring inside pose " + i);
                Check(hx * BirdIdle.SelectRingDarkMul > hx && hy * BirdIdle.SelectRingDarkMul > hy, "dark stroke inside gold " + i);
            }

            // The old disc was 2.05 local units across. Wings-across is wider than that on both sides.
            BirdIdle.SelectRingExtents(HalfX[1], HalfY[1], out float wideX, out _);
            Check(wideX > 2.05f && HalfX[1] > 2.05f * 0.5f, "wings-across still swallows a 2.05 disc");

            Debug.Log("[select-highlight] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif

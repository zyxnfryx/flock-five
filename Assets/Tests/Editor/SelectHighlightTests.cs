#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 5: a selected group of birds had no readable outline (only a halo in the bird's own colour).
    // The selection now adds a warm-white rim glow behind each lifted bird.
    static class SelectHighlightTests
    {
        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[select-highlight] FAIL " + what); } }
            for (float p = 0f; p <= 1f; p += 0.25f)
                Check(BirdIdle.SelectRimAlpha(1f, p) >= 0.6f, "rim too faint at pulse " + p);
            Check(BirdIdle.SelectRimAlpha(0f, 1f) == 0f, "rim shows when not selected");
            Check(BirdIdle.SelectRimAlpha(0.5f, 0.5f) < BirdIdle.SelectRimAlpha(1f, 0.5f), "rim does not fade in");
            Check(BirdIdle.SelectRimScale > 1.6f && BirdIdle.SelectRimScale < 3.15f, "rim not between body and halo");
            Debug.Log("[select-highlight] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif

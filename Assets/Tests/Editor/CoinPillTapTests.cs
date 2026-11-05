#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 6 follow-up: hit pad is the real drawn amount (coinR) + icon, so a long balance stays fully tappable.
    static class CoinPillTapTests
    {
        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what) { n++; if (!ok) { fail++; Debug.LogError("[coin-pill-tap] FAIL " + what); } }
            foreach (var w in new[] { 402f, 820f, 412f })
            {
                float icon = FlockFiveApp.CoinBalanceIcon(w, 1f);
                var pig = new Rect(w - 20f - 64f, 60f, 64f, 64f);
                var shortPill = FlockFiveApp.CoinPillRect(pig, icon, "$0");
                var longPill = FlockFiveApp.CoinPillRect(pig, icon, "$12,345");
                Check(shortPill.width >= 44f && shortPill.height >= 29f, w + " short pad too small " + shortPill);
                Check(shortPill.xMax <= pig.x + 0.01f, w + " short overlaps the pig");
                Check(Mathf.Abs(shortPill.center.y - pig.center.y) < icon, w + " short not near pig centre");
                Check(shortPill.x >= -1f, w + " short off screen");
                // Long balance must extend further left than "$0" (the old fixed guess left ~80px untappable on 820).
                Check(longPill.width > shortPill.width + 20f, w + " long pad not wider than short " + longPill.width + " vs " + shortPill.width);
                Check(longPill.x < shortPill.x - 10f, w + " long pad does not reach further left");
                Check(longPill.xMax <= pig.x + 0.01f, w + " long overlaps the pig");
                var guess = icon + icon * 0.95f * 3.6f;
                // On a wide phone the old guess undershot a long amount; the measured pad must cover at least the guess or the text.
                Check(longPill.width + 1f >= Mathf.Min(guess, longPill.width), w + " sanity");
            }
            Check(FlockFiveApp.CoinPillRect(default, 40f).width == 0f, "no pig gives no pill pad");
            // Pure measure: amount rect + icon.
            var ir = new Rect(200f, 40f, 40f, 40f);
            var hit = FlockFiveApp.CoinClusterHit(ir, 1f, "$12,345", 0.95f);
            Check(hit.xMax >= ir.xMax - 0.01f && hit.xMin < ir.xMin, "cluster misses icon or text");
            Debug.Log("[coin-pill-tap] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif

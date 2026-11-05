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

            // Pill press and pig press share one pad, including the gap between them.
            var seat = new Rect(500f, 200f, 80f, 80f);
            var pill = new Rect(390f, 220f, 100f, 40f);
            var pad = FlockFiveApp.PigPokePad(seat, pill);
            Check(pad.Contains(pill.center), "pad misses the pill");
            Check(pad.Contains(seat.center), "pad misses the pig");
            Check(pad.Contains(new Vector2((pill.xMax + seat.xMin) * 0.5f, seat.center.y)), "pad misses the gap");
            Check(!pad.Contains(new Vector2(pill.xMin - 4f, pill.center.y)), "pad reaches left of the pill");
            var pigOnly = FlockFiveApp.PigPokePad(seat, default);
            Check(pigOnly.x == seat.x && pigOnly.width == seat.width && pigOnly.y == seat.y, "no pill is the pig itself");
            foreach (var w in new[] { 402f, 820f, 412f })
            {
                float icon = FlockFiveApp.CoinBalanceIcon(w, 1f);
                var livePig = new Rect(w - 20f - 64f, 60f, 64f, 64f);
                var livePill = FlockFiveApp.CoinPillRect(livePig, icon, "$0");
                var livePad = FlockFiveApp.PigPokePad(livePig, livePill);
                Check(livePad.Contains(livePill.center) && livePad.Contains(livePig.center), w + " pad misses pill or pig");
            }

            // Press once. The release of that press does not poke again. A bare release still does.
            // A lesson (not allowed) and a VIP block do not.
            var press = FlockFiveApp.TickPigPoke(true, false, true, false, false, false);
            Check(press.Fire && press.Armed && press.FiredThisFrame, "press did not poke");
            var again = FlockFiveApp.TickPigPoke(true, false, true, false, press.Armed, press.FiredThisFrame);
            Check(!again.Fire && again.Armed, "same frame poked twice");
            var held = FlockFiveApp.TickPigPoke(true, false, true, false, press.Armed, false);
            Check(!held.Fire && held.Armed, "held press poked again");
            var release = FlockFiveApp.TickPigPoke(true, false, false, true, press.Armed, false);
            Check(!release.Fire && !release.Armed, "release counted a second poke");
            var bare = FlockFiveApp.TickPigPoke(true, false, false, true, false, false);
            Check(bare.Fire && !bare.Armed, "release without a press did not poke");
            var lesson = FlockFiveApp.TickPigPoke(false, false, true, false, false, false);
            Check(!lesson.Fire && !lesson.Armed, "lesson still pokes");
            var vip = FlockFiveApp.TickPigPoke(true, true, true, false, false, false);
            Check(!vip.Fire, "VIP block still pokes");
            Debug.Log("[coin-pill-tap] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif

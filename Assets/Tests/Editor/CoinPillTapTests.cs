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

            // One pad at the real inspector windows. Amount, icon, visible gap, and pig all hit.
            // 820 is wider than 9:16, so the game column is the pillar width, not 820.
            var windows = new[] { new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(820f, 1180f) };
            foreach (var win in windows)
            {
                float gw = win.x;
                if (win.x / win.y > 9f / 16f + 0.03f)
                    gw = Mathf.Min(win.x, Mathf.Round(win.y * 9f / 16f));
                float ui = Mathf.Max(win.y / 720f, 1f);
                float rail = FlockFiveApp.SplashRailSize(gw);
                float icon = FlockFiveApp.CoinBalanceIcon(gw, ui);
                float railGap = Mathf.Max(24f, rail * 0.30f);
                var livePig = new Rect(gw - 20f - rail, win.y * 0.335f - rail - railGap, rail, rail);
                var iconR = FlockFiveApp.CoinIconRect(livePig, icon);
                var livePill = FlockFiveApp.CoinPillRect(livePig, icon, "$0");
                var livePad = FlockFiveApp.PigPokePad(livePig, livePill);
                var gapC = FlockFiveApp.PigVisibleGapCenter(livePig, iconR);
                Check(livePad.Contains(livePill.center), win.x + " amount outside pad");
                Check(livePad.Contains(iconR.center), win.x + " icon outside pad");
                Check(livePad.Contains(gapC), win.x + " gap outside pad " + gapC + " pad " + livePad);
                Check(livePad.Contains(livePig.center), win.x + " pig outside pad");
                Check(!livePad.Contains(new Vector2(livePill.xMin - rail, livePig.center.y)), win.x + " pad swallows the left rail");
            }

            // 360ms, deepest squash on the first frame, gone at the end. Not a 1-frame sine zero.
            Check(FlockFiveApp.PigPokeDur >= 0.30f && FlockFiveApp.PigPokeDur <= 0.40f, "poke duration " + FlockFiveApp.PigPokeDur);
            float e0 = FlockFiveApp.PigPokeEnvAt(0f);
            var s0 = FlockFiveApp.PigPokeScale(e0);
            Check(e0 > 0.99f && s0.x >= 1.25f && s0.y <= 0.70f, "first frame is not a visible squash " + s0);
            float em = FlockFiveApp.PigPokeEnvAt(FlockFiveApp.PigPokeDur * 0.5f);
            var sm = FlockFiveApp.PigPokeScale(em);
            Check(em > 0.5f && sm.x > 1.10f && sm.y < 0.85f, "mid poke already settled " + sm);
            Check(FlockFiveApp.PigPokeEnvAt(FlockFiveApp.PigPokeDur) == 0f, "poke does not end");
            Check(FlockFiveApp.PigPokeScale(0f) == new Vector2(1f, 1f), "rest scale");

            // Press once. The release does not poke again. A release outside the pad clears the arm
            // so the next press (the pig, after a gap slip) still pokes. A lesson and VIP do not.
            var press = FlockFiveApp.TickPigPoke(true, false, true, false, false, false, false);
            Check(press.Fire && press.Armed && press.FiredThisFrame, "press did not poke");
            var again = FlockFiveApp.TickPigPoke(true, false, true, false, false, press.Armed, press.FiredThisFrame);
            Check(!again.Fire && again.Armed, "same frame poked twice");
            var held = FlockFiveApp.TickPigPoke(true, false, true, false, false, press.Armed, false);
            Check(!held.Fire && held.Armed, "held press poked again");
            var release = FlockFiveApp.TickPigPoke(true, false, false, true, true, press.Armed, false);
            Check(!release.Fire && !release.Armed, "release counted a second poke");
            var slipped = FlockFiveApp.TickPigPoke(true, false, false, true, false, press.Armed, false);
            Check(!slipped.Fire && !slipped.Armed, "outside release stuck the latch");
            var next = FlockFiveApp.TickPigPoke(true, false, true, false, false, slipped.Armed, false);
            Check(next.Fire && next.Armed, "press after a slipped release did not poke");
            var bare = FlockFiveApp.TickPigPoke(true, false, false, true, true, false, false);
            Check(bare.Fire && !bare.Armed, "release without a press did not poke");
            var lesson = FlockFiveApp.TickPigPoke(false, false, true, false, false, false, false);
            Check(!lesson.Fire && !lesson.Armed, "lesson still pokes");
            var vip = FlockFiveApp.TickPigPoke(true, true, true, false, false, false, false);
            Check(!vip.Fire, "VIP block still pokes");

            // 820 pillar: the same pad hits in rail space, screen space, and a double-shifted pointer.
            // Phones pass offX 0, so a screen-sized shift must not count.
            var pad820 = new Rect(400f, 240f, 220f, 90f);
            var railPt = new Vector2(pad820.center.x, pad820.center.y);
            Check(FlockFiveApp.PigPokeHits(pad820, railPt, 78), "820 rail pointer misses");
            Check(FlockFiveApp.PigPokeHits(pad820, new Vector2(railPt.x + 78f, railPt.y), 78), "820 screen pointer misses");
            Check(FlockFiveApp.PigPokeHits(pad820, new Vector2(railPt.x - 78f, railPt.y), 78), "820 double-shift pointer misses");
            Check(!FlockFiveApp.PigPokeHits(pad820, new Vector2(railPt.x, railPt.y + 200f), 78), "820 far pointer hits");
            Check(FlockFiveApp.PigPokeHits(pad820, railPt, 0), "phone rail pointer misses");
            Check(!FlockFiveApp.PigPokeHits(pad820, new Vector2(railPt.x + 78f, railPt.y), 0), "phone treats a screen shift as a hit");

            // Tip plate sits under the coin row and stops before the pig. Same rule at every width.
            foreach (var win in windows)
            {
                float gw = win.x;
                if (win.x / win.y > 9f / 16f + 0.03f)
                    gw = Mathf.Min(win.x, Mathf.Round(win.y * 9f / 16f));
                float ui = Mathf.Max(win.y / 720f, 1f);
                float rail = FlockFiveApp.SplashRailSize(gw);
                float icon = FlockFiveApp.CoinBalanceIcon(gw, ui);
                float railGap = Mathf.Max(24f, rail * 0.30f);
                var livePig = new Rect(gw - 20f - rail, win.y * 0.335f - rail - railGap, rail, rail);
                var plate = FlockFiveApp.PigExplainPlate(livePig, icon, 160f, 18f, ui);
                float counterBottom = livePig.center.y + icon * 0.5f;
                Check(plate.y >= counterBottom - 0.5f, win.x + " tip overlaps the $ row");
                Check(plate.xMax <= livePig.x - 10f, win.x + " tip runs into the pig");
            }
            Debug.Log("[coin-pill-tap] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif

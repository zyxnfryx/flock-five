#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 6: the $0 coin pill looked tappable and did nothing. Its tap pad is the drawn pill (left of the
    // pig) and a tap there pokes the pig, like the pig itself.
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
                var pill = FlockFiveApp.CoinPillRect(pig, icon);
                Check(pill.width >= 44f && pill.height >= 29f, w + " pill pad too small " + pill);
                Check(pill.xMax <= pig.x, w + " pill overlaps the pig");
                Check(Mathf.Abs(pill.center.y - pig.center.y) < 0.5f, w + " pill not level with the pig");
                Check(pill.x >= 0f, w + " pill off screen");
            }
            Check(FlockFiveApp.CoinPillRect(default, 40f).width == 0f, "no pig gives no pill pad");
            Debug.Log("[coin-pill-tap] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 83 pass c3: REWARD popup choice (10d-10i), shared offer sizing (10c), clear caption (10b),
    // mid-clear offer removed (10d), home caption at the top (11/11b).
    static class Build83TestsC3
    {
        [MenuItem("Flock Five/Build 83c3 Tests")]
        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log("[build83c3] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }
            var face = new Rect(160f, 600f, 860f, 520f);
            var two = FlockFiveApp.LayoutRewardChoice(face, 1179f, 2556f, true);
            var one = FlockFiveApp.LayoutRewardChoice(face, 1179f, 2556f, false);
            Check("reward-2x-primary-wider", two.HasSecondary && two.Secondary.width < two.Primary.width && two.Secondary.height <= two.Primary.height,
                "pri " + two.Primary + " sec " + two.Secondary);
            Check("reward-collect-under-2x", two.Secondary.y > two.Primary.yMax && Mathf.Abs(two.Secondary.center.x - two.Primary.center.x) < 0.5f, "");
            Check("reward-below-card", two.Primary.y >= face.yMax, "");
            Check("reward-noad-single-collect", !one.HasSecondary && Mathf.Abs(one.Primary.width - two.Primary.width) < 0.5f, "single primary");
            Check("reward-words", FlockFiveApp.RewardDoubleWord == "2X" && FlockFiveApp.RewardCollectWord == "Collect", "");
            Check("noad-cooldown", !Ads.RewardRepeatAvailable(true, true, 99, 100.0, 99.0), "cooling blocks");
            Check("noad-grace", !Ads.RewardRepeatAvailable(true, true, 0, 100000.0, 0.0), "grace blocks");
            Check("noad-unloaded", !Ads.RewardRepeatAvailable(true, false, 99, 100000.0, 0.0), "");
            Check("ad-ok", Ads.RewardRepeatAvailable(true, true, 99, 100000.0, 0.0), "");
            Check("repeat-place", Ads.PlaceFor(Ads.PlacementRewardRepeat) == RewardedGrant.Place.Repeat, "");
            var clr = FlockFiveApp.LayoutClearOffer(1179f, 2556f);
            var vip = FlockFiveApp.LayoutVipSoft(1179f, 2556f);
            Check("offer-shared-sizing", clr.Skip.width < clr.Watch.width && vip.Skip.width < vip.Go.width && vip.Skip.y > vip.Go.yMax
                && Mathf.Abs(vip.Skip.center.x - vip.Go.center.x) < 0.5f, "");
            Check("clear-caption-question", FlockFiveApp.ClearDoubleLine == "Double your coins?", FlockFiveApp.ClearDoubleLine);
            string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("midclear-offer-removed", !src.Contains("yield return OfferClearDouble()"), "");
            Check("reward-holds", src.Contains("RewardChoicePending()"), "tuck gated on the choice");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("home-caption-top-default", !coach.Contains("y = y0 + (y1 - y0 - h) * 0.36f"), "band top-anchored");
            Debug.Log(fail == 0
                ? "build83c3 tests DONE " + pass + "/" + (pass + fail)
                : "build83c3 tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif

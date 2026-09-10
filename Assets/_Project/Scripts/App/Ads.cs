using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Post-win interstitial seam. No network SDK is wired yet — this yields
    // immediately so the four-garden ladder still plays. Drop in AdMob /
    // LevelPlay later behind ShowInterstitial without changing FlockFiveApp.
    public static class Ads
    {
        public static bool Enabled = true;

        public static IEnumerator Interstitial()
        {
            if (!Enabled) yield break;
#if UNITY_EDITOR
            yield break;
#else
            yield return ShowInterstitial();
#endif
        }

        public static IEnumerator Rewarded()
        {
            if (!Enabled)
            {
                yield return new WaitForSeconds(0.35f);
                yield break;
            }
            yield return ShowRewarded();
        }

        static IEnumerator ShowInterstitial()
        {
            // TODO: load/show a real interstitial once an ads account exists.
            // Placement: after FinaleShow (or the late-dusk toast), before the
            // next garden. No banners — they fight the garden HUD.
            yield break;
        }

        static IEnumerator ShowRewarded()
        {
            // TODO: real rewarded placement. Until an ads account exists the
            // garden still plays the short movie beat, then grants the perch.
            yield return new WaitForSeconds(2.15f);
        }
    }
}

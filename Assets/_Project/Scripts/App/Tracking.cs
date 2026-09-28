using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FlockFive
{
    // Apple App Tracking Transparency: ask once, after the first garden clear,
    // before the first interstitial, so ads know the player's choice.
    public static class Tracking
    {
        const string AskedKey = "flockfive.att.asked";

        public static IEnumerator AskOnce()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (PlayerPrefs.GetInt(AskedKey, 0) == 1) yield break;
            PlayerPrefs.SetInt(AskedKey, 1);
            PlayerPrefs.Save();
            if (FlockFive_TrackingStatus() != 0) yield break;
            FlockFive_RequestTracking();
            float t = 0f;
            yield return null;
            while (FlockFive_TrackingStatus() == 0 && t < 90f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
#else
            yield break;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern int FlockFive_TrackingStatus();

        [DllImport("__Internal")]
        static extern void FlockFive_RequestTracking();
#endif
    }
}

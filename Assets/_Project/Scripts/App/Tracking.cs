using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FlockFive
{
    // Apple App Tracking Transparency. AdConsent waits for the native
    // completion (or a timeout) before LevelPlay init. Editor and Android
    // skip the request. AskOnce still runs after the first garden clear and
    // does nothing once this launch already asked or the status is decided.
    public static class Tracking
    {
        const string AskedKey = "flockfive.att.asked";

        public static bool Completed { get; private set; }
        public static int CompletedStatus { get; private set; }

        public static bool AttRequired
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Completed = false;
            CompletedStatus = 0;
        }

        // UnitySendMessage target is the Ads host. status is the ATTrackingManager code.
        public static void NoteComplete(string status)
        {
            Completed = true;
            int n;
            CompletedStatus = int.TryParse(status, out n) ? n : -1;
        }

        public static IEnumerator WaitForAds(AdConsent.AttSession session)
        {
            if (session == null) yield break;
            session.Start();
            if (!AttRequired) yield break;
#if UNITY_IOS && !UNITY_EDITOR
            float focus = 0f;
            while (!Application.isFocused && focus < 2f)
            {
                focus += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
            if (FlockFive_TrackingStatus() != 0)
                session.Callback = true;
            else
            {
                PlayerPrefs.SetInt(AskedKey, 1);
                PlayerPrefs.Save();
                FlockFive_RequestTracking();
            }
            while (!session.Finished)
            {
                if (Completed || FlockFive_TrackingStatus() != 0)
                    session.Callback = true;
                float dt = Time.unscaledDeltaTime;
                if (dt < 0f || dt > 0.5f) dt = 0f;
                session.Tick(dt, AdConsent.AttTimeoutSeconds);
                if (session.Finished) yield break;
                yield return null;
            }
#else
            yield break;
#endif
        }

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
            while (FlockFive_TrackingStatus() == 0 && !Completed && t < 90f)
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

using System.Runtime.InteropServices;
using UnityEngine;

namespace FlockFive
{
    // Apple App Tracking Transparency. The system prompt is not shown at launch.
    // AdConsent asks later, on the home screen, and only then calls RequestWhenActive.
    // The native request still waits until UIApplication is active. Editor and Android
    // skip the request. Completion is delivered to AdsHost.OnAttComplete.
    public static class Tracking
    {
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

        // True when iOS has not recorded a choice yet. Editor and Android stay
        // "undecided" so the in-game pre-prompt can be exercised without a device.
        public static bool Undecided()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { return FlockFive_TrackingStatus() == 0; }
            catch (System.Exception) { return false; }
#else
            return true;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Completed = false;
            CompletedStatus = 0;
        }

        // UnitySendMessage target is the Ads host. status is the ATTrackingManager code.
        // 0 not determined, 1 restricted, 2 denied, 3 authorized.
        public static void NoteComplete(string status)
        {
            Completed = true;
            int n;
            CompletedStatus = int.TryParse(status, out n) ? n : -1;
        }

        // Native request only. Does not spin, does not block input, does not write prefs.
        // FlockFiveTracking.mm waits until the app is active before the system dialog.
        public static void RequestWhenActive()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { FlockFive_RequestTracking(); }
            catch (System.Exception e)
            {
                Debug.LogWarning("ATT request skipped: " + e.Message);
            }
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

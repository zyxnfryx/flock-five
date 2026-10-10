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
            // Wall clock, not unscaledDeltaTime. A hitch or a stalled player
            // loop must not freeze the 45s fallback short of LevelPlay init.
            // The native call waits until UIApplication is active.
            float start = Time.realtimeSinceStartup;
            bool asked = false;
            while (!session.Finished)
            {
                if (!asked)
                {
                    asked = true;
                    int status = 0;
                    bool known = false;
                    try
                    {
                        status = FlockFive_TrackingStatus();
                        known = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("ATT status unavailable: " + e.Message);
                    }
                    if (known && status != 0)
                        session.Callback = true;
                    else
                    {
                        try
                        {
                            PlayerPrefs.SetInt(AskedKey, 1);
                            PlayerPrefs.Save();
                            FlockFive_RequestTracking();
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning("ATT request skipped: " + e.Message);
                        }
                    }
                }
                else if (Completed)
                    session.Callback = true;
                else
                {
                    try
                    {
                        if (FlockFive_TrackingStatus() != 0)
                            session.Callback = true;
                    }
                    catch (System.Exception) { }
                }

                float elapsed = Time.realtimeSinceStartup - start;
                if (float.IsNaN(elapsed) || float.IsInfinity(elapsed) || elapsed < 0f)
                    elapsed = session.Elapsed;
                session.CatchUp(elapsed, AdConsent.AttTimeoutSeconds);
                if (session.Finished) yield break;
                yield return null;
            }
            if (!session.Finished)
                session.CatchUp(AdConsent.AttTimeoutSeconds, AdConsent.AttTimeoutSeconds);
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

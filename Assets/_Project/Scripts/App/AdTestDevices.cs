using System;
using System.Collections;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace FlockFive
{
    // Test ads only on our own phones. At LevelPlay init the device's
    // advertising ID (IDFA on iOS, GAID on Android) is checked against this
    // allowlist; a match turns on LevelPlay test mode. Everyone else, and any
    // device whose ID is unavailable or zeroed (ATT not granted, limit ad
    // tracking), gets normal ads.
    public static class AdTestDevices
    {
        public static readonly string[] Ios =
        {
            "AE170331-2D8E-46C9-B6E6-A72F4CE430BA",
            "54EEF4C4-C1A2-4792-A7EE-E5F695957D1A",
            "15314A07-CC91-455D-91C4-E73F125DC0BC",
        };

        public static readonly string[] Android =
        {
            "af3a2d10-4cb3-4099-920c-35f21f466305",
        };

        public const float ResolveTimeout = 1.5f;

        public static bool Active { get; private set; }

        public static bool IsTestDevice(string adId)
        {
            if (string.IsNullOrEmpty(adId)) return false;
            string id = adId.Trim();
            if (id.Replace("0", "").Replace("-", "").Length == 0) return false;
            foreach (var s in Ios) if (string.Equals(s, id, StringComparison.OrdinalIgnoreCase)) return true;
            foreach (var s in Android) if (string.Equals(s, id, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // Call before LevelPlay.Init. Returns whether test mode was turned on.
        public static bool Apply(string adId)
        {
            Active = IsTestDevice(adId);
            if (!Active) return false;
            try
            {
                LevelPlay.SetMetaData("is_test_suite", "enable");
                LevelPlay.SetAdaptersDebug(true);
                AdLog.Add("ad test mode on (allowlisted device)");
            }
            catch (Exception e) { Debug.LogWarning("Ad test mode skipped: " + e.Message); }
            return Active;
        }

        // Looks up the advertising ID, then runs init exactly once: on the
        // callback, or after ResolveTimeout if the platform never answers.
        public static void Resolve(MonoBehaviour host, Action<string> init)
        {
            bool done = false;
            Action<string> once = id => { if (done) return; done = true; init(id); };
            bool asked = false;
            try
            {
                asked = Application.RequestAdvertisingIdentifierAsync((id, tracking, err) => once(id));
            }
            catch (Exception) { asked = false; }
            if (!asked) { once(null); return; }
            if (!done && host != null) host.StartCoroutine(Timeout(once));
        }

        static IEnumerator Timeout(Action<string> once)
        {
            yield return new WaitForSecondsRealtime(ResolveTimeout);
            once(null);
        }
    }
}

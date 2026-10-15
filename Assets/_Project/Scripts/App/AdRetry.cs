using UnityEngine;

namespace FlockFive
{
    // Shared rewarded-ad failure state. When a rewarded request cannot show (SDK not
    // inited, no unit, no fill, load failed, display failed, never appeared) the
    // offer card stays up in a gentle "ad not ready, try again" state instead of
    // flickering straight back to a bare Watch button. Nothing is granted by a failure.
    public static class AdRetry
    {
        public const string Line = "Ad not ready.\nPlease try again.";
        public const string ButtonLabel = "Retry";

        public static bool Active { get; private set; }
        public static string Reason { get; private set; } = "";
        public static int Failures { get; private set; }

        // A new request starts clean.
        public static void Begin()
        {
            Active = false;
            Reason = "";
        }

        // Every failed request lands here (Ads.DeliverFailedCallback). Visible in AdLog.
        public static void NoteFail(string reason)
        {
            Failures++;
            Reason = string.IsNullOrEmpty(reason) ? "unknown" : reason;
            AdLog.Add("gift ad request FAILED: " + Reason + " (failure #" + Failures + ", retry offered)");
        }

        // The card calls this once the request ended without a grant.
        public static void Offer() { Active = true; }

        public static void Clear()
        {
            Active = false;
            Reason = "";
        }

        public static void ResetForTests()
        {
            Active = false;
            Reason = "";
            Failures = 0;
        }

        // A request that ended with no grant must show the retry state, never loop silently.
        public static bool ShouldOffer(bool paid) { return !paid; }
    }
}

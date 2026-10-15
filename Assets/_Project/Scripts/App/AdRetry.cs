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
        // b91: the player closed the ad early. Friendly, not an error.
        public const string SkipLine = "Watch the whole ad\nto get your branch.";
        public const string SkipButtonLabel = "Watch";

        // True when the last request ended because the player left the ad early.
        public static bool Skipped { get; private set; }
        public static string CurrentLine => Skipped ? SkipLine : Line;
        public static string CurrentLabel => Skipped ? SkipButtonLabel : ButtonLabel;

        public static bool Active { get; private set; }
        public static string Reason { get; private set; } = "";
        public static int Failures { get; private set; }

        // A new request starts clean.
        public static void Begin()
        {
            Active = false;
            Skipped = false;
            Reason = "";
        }

        // Closed early with no reward. Not a failure: no count, no error copy.
        public static void NoteSkip()
        {
            Skipped = true;
            Reason = "skipped (closed early)";
            AdLog.Add("gift ad skipped by player (closed before reward); not counted as a failure");
        }

        // Every failed request lands here (Ads.DeliverFailedCallback). Visible in AdLog.
        public static void NoteFail(string reason)
        {
            Failures++;
            Skipped = false;
            Reason = string.IsNullOrEmpty(reason) ? "unknown" : reason;
            AdLog.Add("gift ad request FAILED: " + Reason + " (failure #" + Failures + ", retry offered)");
        }

        // The card calls this once the request ended without a grant.
        public static void Offer() { Active = true; }

        public static void Clear()
        {
            Active = false;
            Skipped = false;
            Reason = "";
        }

        public static void ResetForTests()
        {
            Active = false;
            Skipped = false;
            Reason = "";
            Failures = 0;
        }

        // A request that ended with no grant must show the retry state, never loop silently.
        public static bool ShouldOffer(bool paid) { return !paid; }
    }
}

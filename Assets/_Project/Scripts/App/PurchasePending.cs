using System;

namespace FlockFive
{
    // b89: one "waiting on the store" state shared by Go VIP and Restore purchase.
    // Begin when the request goes to the store; Finish from the store callback.
    // While Active the VIP popup shows the wait on the button and takes no taps
    // (buy, restore, X and the outside tap are all ignored), so nothing is asked twice.
    // Nothing here grants VIP: ownership still comes only from NoAds.Grant.
    // Two safety exits keep the state from sticking:
    //   - Timeout: 60 s of wall-clock time since Begin (the clock keeps running in
    //     the background, so a long trip away counts too).
    //   - Resume grace: the app was paused (OnApplicationPause) and came back, and
    //     the store still has not answered 10 s of foreground time later. On iOS the
    //     StoreKit sheet and the password / Face ID prompt also pause the app, so the
    //     grace only counts while we are in front: each new pause stops it, and each
    //     resume starts it over. 10 s leaves room for the receipt to arrive after the
    //     sheet closes; the 60 s timeout is the hard bound either way.
    public static class PurchasePending
    {
        public enum Kind { None, Buy, Restore }
        public enum Outcome { None, Success, Cancelled, Failed, Deferred, TimedOut, Interrupted }

        public const double TimeoutSeconds = 60.0;
        public const double ResumeGraceSeconds = 10.0;

        static Kind _kind;
        static double _since;
        static double _resumeAt = -1.0;
        static bool _away;
        static Func<double> _clockOverride;

        public static Kind Current => _kind;
        public static bool Active => _kind != Kind.None;
        public static Outcome Last { get; private set; }
        public static int Serial { get; private set; }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ResetForTest();

        public static void ResetForTest()
        {
            _kind = Kind.None;
            _since = 0.0;
            _resumeAt = -1.0;
            _away = false;
            Last = Outcome.None;
            Serial = 0;
        }

        public static void SetClockForTest(Func<double> clock) => _clockOverride = clock;

        static double Now => _clockOverride != null
            ? _clockOverride()
            : (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;

        // False (and no change) while another request is still waiting.
        public static bool Begin(Kind kind)
        {
            if (kind == Kind.None || Active) return false;
            _kind = kind;
            _since = Now;
            _resumeAt = -1.0;
            _away = false;
            return true;
        }

        // Ends the wait. A callback for the other kind is ignored (a restored item's
        // ProcessPurchase must not end a Buy wait early, and vice versa), unless
        // `kind` is None. Returns true when this call ended the wait.
        public static bool Finish(Outcome outcome, Kind kind = Kind.None)
        {
            if (!Active || outcome == Outcome.None) return false;
            if (kind != Kind.None && kind != _kind) return false;
            _kind = Kind.None;
            _resumeAt = -1.0;
            _away = false;
            Last = outcome;
            Serial++;
            return true;
        }

        public static void NoteBackground()
        {
            if (!Active) return;
            _away = true;
            _resumeAt = -1.0; // another sheet / trip away: the grace waits for the next resume
        }

        public static void NoteResume()
        {
            if (!Active || !_away) return;
            _away = false;
            _resumeAt = Now;
        }

        // Call every frame (NoAdsHost.Update and the popup). Returns the outcome it
        // forced, or None.
        public static Outcome Tick()
        {
            if (!Active) return Outcome.None;
            double now = Now;
            if (now < _since) _since = now; // clock stepped back: restart, never stick
            if (now - _since >= TimeoutSeconds)
            {
                Finish(Outcome.TimedOut);
                return Outcome.TimedOut;
            }
            if (_resumeAt >= 0.0 && now - _resumeAt >= ResumeGraceSeconds)
            {
                Finish(Outcome.Interrupted);
                return Outcome.Interrupted;
            }
            return Outcome.None;
        }

        // Short popup note for an outcome; null means none. Cancel stays quiet.
        public static string NoteFor(Outcome o, Kind k)
        {
            switch (o)
            {
                case Outcome.Deferred: return "Waiting for approval";
                // b119 (Brandon): no store-timeout note. The timeout just frees the buttons;
                // a late StoreKit answer (already-owned / restore sheet) still grants via ProcessPurchase.
                case Outcome.TimedOut: return null;
                case Outcome.Failed: return k == Kind.Restore ? "Couldn't restore" : "Purchase didn't go through";
                default: return null;
            }
        }

        public static string WaitLabel(bool android) => android ? "Waiting for Google Play\u2026" : "Waiting for App Store\u2026";
    }
}

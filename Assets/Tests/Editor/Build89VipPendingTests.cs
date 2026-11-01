#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;
using K = FlockFive.PurchasePending.Kind;
using O = FlockFive.PurchasePending.Outcome;

namespace FlockFive.Editor
{
    // b89: Go VIP / Restore purchase pending state (PurchasePending + NoAdsStoreEvents).
    // Cancel, fail, success, deferred, timeout, app background, and no double requests.
    // Store callbacks go through the same NoAdsStoreEvents the device host calls.
    static class Build89VipPendingTests
    {
        static double _now;

        [MenuItem("Flock Five/Build 89 VIP Pending Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build89-vippending] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            bool owned = NoAds.Owned;
            int savedPref = PlayerPrefs.GetInt("flockfive.noads", 0);
            try
            {
                PurchasePending.SetClockForTest(() => _now);
                CheckSuccess(Check);
                CheckCancel(Check);
                CheckFail(Check);
                CheckDeferred(Check);
                CheckTimeout(Check);
                CheckBackground(Check);
                CheckGuards(Check);
                CheckRestore(Check);
                CheckWiring(Check);
            }
            finally
            {
                PurchasePending.SetClockForTest(null);
                PurchasePending.ResetForTest();
                NoAds.SetOwnedForTest(owned);
                PlayerPrefs.SetInt("flockfive.noads", savedPref);
                PlayerPrefs.Save();
            }

            if (fail > 0) throw new System.Exception("BUILD89VIPPENDING_FAIL " + fail + " passed " + pass);
            Debug.Log("BUILD89VIPPENDING_OK " + pass);
        }

        static void Fresh()
        {
            PurchasePending.ResetForTest();
            NoAds.SetOwnedForTest(false);
            _now = 1000.0;
        }

        static bool Idle() => !PurchasePending.Active && !NoAds.Busy && PurchasePending.Current == K.None;

        static void CheckSuccess(System.Action<string, bool, string> Check)
        {
            Fresh();
            bool began = PurchasePending.Begin(K.Buy);
            bool waiting = PurchasePending.Active && NoAds.Busy && PurchasePending.Current == K.Buy;
            // Success path as ProcessPurchase runs it.
            NoAds.Grant();
            bool ended = PurchasePending.Finish(O.Success, K.Buy);
            Check("success-grants-and-clears",
                began && waiting && ended && Idle() && NoAds.Owned && PurchasePending.Last == O.Success,
                "began=" + began + " waiting=" + waiting + " owned=" + NoAds.Owned + " last=" + PurchasePending.Last);

            Fresh();
            PurchasePending.Begin(K.Buy);
            NoAdsStoreEvents.Failed(NoAds.ProductId, PurchaseFailureReason.DuplicateTransaction);
            Check("duplicate-owned-counts-as-success", Idle() && NoAds.Owned && PurchasePending.Last == O.Success,
                "owned=" + NoAds.Owned + " last=" + PurchasePending.Last);
        }

        static void CheckCancel(System.Action<string, bool, string> Check)
        {
            Fresh();
            PurchasePending.Begin(K.Buy);
            int serial = NoAds.RestoreSerial;
            NoAdsStoreEvents.Failed(NoAds.ProductId, PurchaseFailureReason.UserCancelled);
            Check("cancel-returns-normal-quiet",
                Idle() && !NoAds.Owned && PurchasePending.Last == O.Cancelled && NoAds.RestoreSerial == serial,
                "owned=" + NoAds.Owned + " last=" + PurchasePending.Last + " noteChanged=" + (NoAds.RestoreSerial != serial));
            _now += 61.0;
            Check("cancel-nothing-sticks", PurchasePending.Tick() == O.None && Idle(), "tick after cancel is a no-op");
        }

        static void CheckFail(System.Action<string, bool, string> Check)
        {
            Fresh();
            PurchasePending.Begin(K.Buy);
            int serial = NoAds.RestoreSerial;
            NoAdsStoreEvents.Failed(NoAds.ProductId, PurchaseFailureReason.PaymentDeclined);
            Check("fail-returns-normal-with-note",
                Idle() && !NoAds.Owned && PurchasePending.Last == O.Failed
                && NoAds.RestoreSerial == serial + 1 && NoAds.RestoreNote == "Purchase didn't go through",
                "owned=" + NoAds.Owned + " note=" + NoAds.RestoreNote);

            Fresh();
            PurchasePending.Begin(K.Buy);
            NoAdsStoreEvents.Failed(null, PurchaseFailureReason.ProductUnavailable);
            Check("fail-null-product", Idle() && !NoAds.Owned, "no throw, no grant");
        }

        static void CheckDeferred(System.Action<string, bool, string> Check)
        {
            Fresh();
            PurchasePending.Begin(K.Buy);
            NoAdsStoreEvents.Deferred();
            Check("deferred-returns-normal-no-grant",
                Idle() && !NoAds.Owned && PurchasePending.Last == O.Deferred && NoAds.RestoreNote == "Waiting for approval",
                "owned=" + NoAds.Owned + " note=" + NoAds.RestoreNote);
            // Parent approves later: ProcessPurchase grants then, with no wait open.
            NoAds.Grant();
            bool late = PurchasePending.Finish(O.Success, K.Buy);
            Check("deferred-approval-later-grants", NoAds.Owned && !late && Idle(), "late success grants, no stuck wait");
        }

        static void CheckTimeout(System.Action<string, bool, string> Check)
        {
            Fresh();
            PurchasePending.Begin(K.Buy);
            _now += PurchasePending.TimeoutSeconds - 0.5;
            bool holds = PurchasePending.Tick() == O.None && PurchasePending.Active;
            _now += 1.0;
            NoAdsStoreEvents.Tick();
            Check("timeout-60s-returns-normal",
                holds && Idle() && !NoAds.Owned && PurchasePending.Last == O.TimedOut
                && string.IsNullOrEmpty(NoAds.RestoreNote),
                "holds59=" + holds + " last=" + PurchasePending.Last + " note=" + NoAds.RestoreNote);
            Check("timeout-is-about-60s", System.Math.Abs(PurchasePending.TimeoutSeconds - 60.0) < 0.01, "" + PurchasePending.TimeoutSeconds);

            // A failure that lands after the timeout must not reopen anything.
            NoAdsStoreEvents.Failed(NoAds.ProductId, PurchaseFailureReason.UserCancelled);
            // b119: a late grant after the timeout clears any note.
            NoAdsStoreEvents.Failed(NoAds.ProductId, PurchaseFailureReason.PaymentDeclined); // posts a note
            bool hadNote = !string.IsNullOrEmpty(NoAds.RestoreNote);
            NoAds.Grant();
            Check("b119-grant-clears-note", hadNote && string.IsNullOrEmpty(NoAds.RestoreNote), "note=" + NoAds.RestoreNote);
            NoAds.SetOwnedForTest(false);
            Check("timeout-late-cancel-ignored", Idle() && PurchasePending.Last == O.TimedOut, "last=" + PurchasePending.Last);

            Fresh();
            PurchasePending.Begin(K.Buy);
            _now -= 500.0; // clock stepped back
            PurchasePending.Tick();
            _now += PurchasePending.TimeoutSeconds + 0.1;
            Check("timeout-survives-clock-step", PurchasePending.Tick() == O.TimedOut && Idle(), "still ends");
        }

        static void CheckBackground(System.Action<string, bool, string> Check)
        {
            // Background and back, store never answers: grace ends it.
            Fresh();
            PurchasePending.Begin(K.Buy);
            PurchasePending.NoteBackground();
            _now += 30.0;
            PurchasePending.NoteResume();
            bool graceHolds = PurchasePending.Tick() == O.None && PurchasePending.Active;
            _now += PurchasePending.ResumeGraceSeconds + 0.1;
            Check("background-resume-unsticks",
                graceHolds && PurchasePending.Tick() == O.Interrupted && Idle() && !NoAds.Owned,
                "graceHolds=" + graceHolds + " last=" + PurchasePending.Last);

            // Background and back, the purchase lands during the grace: success wins.
            Fresh();
            PurchasePending.Begin(K.Buy);
            PurchasePending.NoteBackground();
            _now += 5.0;
            PurchasePending.NoteResume();
            _now += 1.0;
            NoAds.Grant();
            PurchasePending.Finish(O.Success, K.Buy);
            Check("background-resume-then-success", Idle() && NoAds.Owned && PurchasePending.Last == O.Success, "");

            // iOS: sheet closes (resume), then the password prompt pauses again.
            // The grace must not run out while the prompt is up.
            Fresh();
            PurchasePending.Begin(K.Buy);
            PurchasePending.NoteBackground();
            _now += 3.0;
            PurchasePending.NoteResume();
            _now += 2.0;
            PurchasePending.NoteBackground();
            _now += PurchasePending.ResumeGraceSeconds + 5.0;
            bool heldAway = PurchasePending.Tick() == O.None && PurchasePending.Active;
            PurchasePending.NoteResume();
            _now += PurchasePending.ResumeGraceSeconds - 1.0;
            bool heldFront = PurchasePending.Tick() == O.None && PurchasePending.Active;
            NoAds.Grant();
            PurchasePending.Finish(O.Success, K.Buy);
            Check("second-pause-restarts-grace", heldAway && heldFront && Idle() && NoAds.Owned,
                "heldAway=" + heldAway + " heldFront=" + heldFront);

            // Away longer than the timeout: first tick back ends it.
            Fresh();
            PurchasePending.Begin(K.Buy);
            PurchasePending.NoteBackground();
            _now += 600.0;
            PurchasePending.NoteResume();
            Check("background-past-timeout", PurchasePending.Tick() == O.TimedOut && Idle(), "");

            // Apple's sheet is only a focus loss: no pause, so no grace arms.
            Fresh();
            PurchasePending.Begin(K.Buy);
            PurchasePending.NoteResume();
            _now += 20.0;
            Check("focus-only-keeps-waiting", PurchasePending.Tick() == O.None && PurchasePending.Active, "sheet up 20 s");
            PurchasePending.ResetForTest();
        }

        static void CheckGuards(System.Action<string, bool, string> Check)
        {
            Fresh();
            bool first = PurchasePending.Begin(K.Buy);
            bool second = PurchasePending.Begin(K.Buy);
            bool restore = PurchasePending.Begin(K.Restore);
            // Public entry points refuse while waiting (no host needed to see it).
            NoAds.Buy();
            NoAds.Restore();
            Check("no-double-requests",
                first && !second && !restore && PurchasePending.Current == K.Buy && !NoAds.Owned,
                "second=" + second + " restore=" + restore + " owned=" + NoAds.Owned);
            // A restored item's callback does not end a Buy wait, and vice versa.
            bool wrong = PurchasePending.Finish(O.Success, K.Restore);
            Check("kind-matched-finish", !wrong && PurchasePending.Current == K.Buy, "");
            PurchasePending.ResetForTest();
        }

        static void CheckRestore(System.Action<string, bool, string> Check)
        {
            Fresh();
            bool began = PurchasePending.Begin(K.Restore);
            bool buyBlocked = !PurchasePending.Begin(K.Buy) && NoAds.Busy;
            NoAdsStoreEvents.Failed(NoAds.ProductId, PurchaseFailureReason.UserCancelled); // a Buy-only answer
            bool stillRestoring = PurchasePending.Current == K.Restore;
            _now += PurchasePending.TimeoutSeconds + 1.0;
            NoAdsStoreEvents.Tick();
            Check("restore-shares-pending-and-timeout",
                began && buyBlocked && stillRestoring && Idle() && PurchasePending.Last == O.TimedOut,
                "buyBlocked=" + buyBlocked + " still=" + stillRestoring + " last=" + PurchasePending.Last);
            Check("restore-fail-note", PurchasePending.NoteFor(O.Failed, K.Restore) == "Couldn't restore"
                && PurchasePending.NoteFor(O.Cancelled, K.Buy) == null, "");
        }

        static void CheckWiring(System.Action<string, bool, string> Check)
        {
            string noAds = System.IO.File.ReadAllText("Assets/_Project/Scripts/App/NoAds.cs");
            string offer = System.IO.File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.VipOffer.cs");
            bool order = noAds.IndexOf("PurchasePending.Begin(PurchasePending.Kind.Buy)") >= 0
                && noAds.IndexOf("PurchasePending.Begin(PurchasePending.Kind.Buy)") < noAds.IndexOf("_ctl.InitiatePurchase(p)");
            Check("wiring-begin-before-initiate", order, "");
            Check("wiring-deferred-listeners",
                noAds.Contains("RegisterPurchaseDeferredListener(OnDeferred)") && noAds.Contains("SetDeferredPurchaseListener(OnDeferred)"), "");
            Check("wiring-pause-hooks", noAds.Contains("PurchasePending.NoteBackground()") && noAds.Contains("PurchasePending.NoteResume()"), "");
            Check("wiring-popup-guard", offer.Contains("if (waiting) return;") && offer.Contains("DrawBuyWaiting("), "");
            Check("wiring-product-id-unchanged", NoAds.ProductId == "com.zfxgames.flockfive.noads", NoAds.ProductId);
        }
    }
}
#endif

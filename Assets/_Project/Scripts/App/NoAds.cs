using System.Collections;
using UnityEngine;
using UnityEngine.Purchasing;

namespace FlockFive
{
    public static class NoAds
    {
        public const string ProductId = "com.zfxgames.flockfive.noads";
        const string Pref = "flockfive.noads";

        public static bool Owned { get; private set; }
        public static string RestoreNote { get; private set; }
        public static int RestoreSerial { get; private set; }

        static NoAdsHost _host;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Owned = false;
            RestoreNote = null;
            RestoreSerial = 0;
            _host = null;
        }

        // Store-localized price only. Empty until the store answers. Never a hardcoded dollar amount.
        public static string PriceLabel()
        {
            if (_host == null) return "";
            return _host.StorePrice();
        }

        public static void Warm()
        {
            Owned = PlayerPrefs.GetInt(Pref, 0) == 1;
            Ensure();
        }

        public static void Grant()
        {
            Owned = true;
            PlayerPrefs.SetInt(Pref, 1);
            PlayerPrefs.Save();
        }

        // Store requests while PurchasePending is Active are dropped (no double asks).
        public static bool Busy => PurchasePending.Active;

#if UNITY_EDITOR
        // Edit-mode tests and stills: memory only, never touches the saved flag.
        public static void SetOwnedForTest(bool owned) => Owned = owned;
#endif

        public static void Buy()
        {
            if (Owned || Busy) return;
            Ensure();
            if (_host != null) _host.Buy();
#if UNITY_EDITOR
            else Grant();
#endif
        }

        public static void Restore()
        {
            if (Busy) return;
            Ensure();
            if (_host == null)
            {
                NoteRestore("Couldn't reach the store");
                return;
            }
            _host.Restore();
        }

        // Ends the shared wait and posts its note (if any). Safe when nothing waits.
        internal static void EndWait(PurchasePending.Outcome o, PurchasePending.Kind k)
        {
            if (!PurchasePending.Finish(o, k)) return;
            string note = PurchasePending.NoteFor(o, k);
            if (!string.IsNullOrEmpty(note)) NoteRestore(note);
        }

        internal static void NoteRestore(string msg)
        {
            RestoreNote = msg;
            RestoreSerial++;
        }

        static void Ensure()
        {
            if (_host != null) return;
            var found = Object.FindObjectsByType<NoAdsHost>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            NoAdsHost keep = null;
            if (found != null)
            {
                for (int i = 0; i < found.Length; i++)
                {
                    var h = found[i];
                    if (h == null) continue;
                    if (keep == null) keep = h;
                    else Object.Destroy(h.gameObject);
                }
            }
            if (keep == null)
            {
                var go = new GameObject("NoAds");
                Object.DontDestroyOnLoad(go);
                keep = go.AddComponent<NoAdsHost>();
            }
            else
                Object.DontDestroyOnLoad(keep.gameObject);
            _host = keep;
            _host.Boot();
        }

        internal static bool OwnsHost(NoAdsHost h) => _host == h;
        internal static void DropHost() => _host = null;
    }

    // One store path for both platforms (Unity IAP 4.14, v4 API). Only the restore
    // call differs: IAppleExtensions on iOS, IGooglePlayStoreExtensions on Android.
    // Everything here fails quiet: no store, no product, or a slow store only sets a
    // short note for the offer panel. Ads are never blocked or delayed by this host.
    // No receipt validation: ownership is granted from the store callback and kept
    // in PlayerPrefs, same as before.
    sealed class NoAdsHost : MonoBehaviour, IStoreListener
    {
#pragma warning disable 0414, 0649 // editor build skips the store, so these look unused there
        IStoreController _ctl;
        IExtensionProvider _ext;
        bool _starting;
        bool _restoring;
        volatile bool _restoreDone;
        bool _restoreOk;
#pragma warning restore 0414, 0649

        public void Boot()
        {
#if !UNITY_EDITOR
            if (_ctl != null || _starting) return;
            _starting = true;
            try
            {
                var mod = StandardPurchasingModule.Instance();
                var b = ConfigurationBuilder.Instance(mod);
                b.AddProduct(NoAds.ProductId, ProductType.NonConsumable);
#if UNITY_ANDROID
                b.Configure<IGooglePlayConfiguration>().SetDeferredPurchaseListener(OnDeferred);
#endif
                UnityPurchasing.Initialize(this, b);
            }
            catch (System.Exception e)
            {
                _starting = false;
                Debug.LogWarning("[NoAds] store init failed: " + e.Message);
            }
#endif
        }

        public void Buy()
        {
            if (NoAds.Owned) return;
#if UNITY_EDITOR
            NoAds.Grant();
#else
            if (_ctl == null)
            {
                NoAds.NoteRestore("Store isn't ready. Try again in a moment");
                Boot(); // retry after a failed init; no-op while one is still pending
                return;
            }
            try
            {
                var p = _ctl.products.WithID(NoAds.ProductId);
                if (p == null || !p.availableToPurchase)
                {
                    NoAds.NoteRestore("Not available right now");
                    return;
                }
                // Pending first: the store may answer synchronously inside InitiatePurchase.
                if (!PurchasePending.Begin(PurchasePending.Kind.Buy)) return;
                _ctl.InitiatePurchase(p);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[NoAds] purchase start failed: " + e.Message);
                PurchasePending.Finish(PurchasePending.Outcome.Failed, PurchasePending.Kind.Buy);
                NoAds.NoteRestore("Couldn't start the purchase");
            }
#endif
        }

        public string StorePrice()
        {
            if (_ctl == null) return "";
            var p = _ctl.products.WithID(NoAds.ProductId);
            if (p == null || p.metadata == null) return "";
            var label = p.metadata.localizedPriceString;
            return string.IsNullOrEmpty(label) ? "" : label;
        }

        public void Restore()
        {
#if UNITY_EDITOR
            NoAds.NoteRestore(NoAds.Owned ? "Restored" : "Nothing to restore");
#else
            if (_restoring) return;
            if (_ext == null || _ctl == null)
            {
                NoAds.NoteRestore("Couldn't reach the store");
                Boot();
                return;
            }
            if (!PurchasePending.Begin(PurchasePending.Kind.Restore)) return;
            _restoring = true;
            _restoreDone = false;
            _restoreOk = false;
            try
            {
#if UNITY_IOS
                var ext = _ext.GetExtension<IAppleExtensions>();
                if (ext != null) ext.RestoreTransactions((ok, err) => { _restoreOk = ok; _restoreDone = true; });
#elif UNITY_ANDROID
                var ext = _ext.GetExtension<IGooglePlayStoreExtensions>();
                if (ext != null) ext.RestoreTransactions((ok, err) => { _restoreOk = ok; _restoreDone = true; });
#else
                object ext = null;
#endif
                if (ext == null)
                {
                    _restoring = false;
                    PurchasePending.Finish(PurchasePending.Outcome.Success, PurchasePending.Kind.Restore);
                    NoAds.NoteRestore(NoAds.Owned ? "Restored" : "Nothing to restore");
                    return;
                }
                StartCoroutine(RestoreSettle());
            }
            catch (System.Exception e)
            {
                _restoring = false;
                Debug.LogWarning("[NoAds] restore failed: " + e.Message);
                NoAds.EndWait(PurchasePending.Outcome.Failed, PurchasePending.Kind.Restore);
            }
#endif
        }

        // The store's restore callback can land before or after ProcessPurchase for the
        // restored item, so wait for the callback, then give ProcessPurchase a moment
        // before deciding what to tell the player. Capped so the note always appears.
        IEnumerator RestoreSettle()
        {
            float t = 0f;
            while (!_restoreDone && t < 8f && PurchasePending.Current == PurchasePending.Kind.Restore)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            bool done = _restoreDone;
            bool ok = _restoreOk;
            if (done && ok)
            {
                float settle = 0f;
                while (!NoAds.Owned && settle < 1.5f)
                {
                    settle += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            _restoring = false;
            // Timeout / app resume already ended the wait (and posted its note): stay quiet.
            if (!PurchasePending.Finish(done && ok ? PurchasePending.Outcome.Success : PurchasePending.Outcome.Failed,
                    PurchasePending.Kind.Restore))
                yield break;
            if (!done || !ok) NoAds.NoteRestore(NoAds.Owned ? "Restored" : (done ? "Couldn't restore" : "Couldn't reach the store"));
            else NoAds.NoteRestore(NoAds.Owned ? "Restored" : "Nothing to restore");
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _ctl = controller;
            _ext = extensions;
            _starting = false;
#if UNITY_IOS
            try { extensions.GetExtension<IAppleExtensions>()?.RegisterPurchaseDeferredListener(OnDeferred); }
            catch (System.Exception e) { Debug.LogWarning("[NoAds] deferred listener: " + e.Message); }
#endif
            var p = controller.products.WithID(NoAds.ProductId);
            if (p != null && p.hasReceipt) NoAds.Grant();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            _starting = false;
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            _starting = false;
            Debug.LogWarning("[NoAds] store init failed: " + error + " " + message);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            if (args.purchasedProduct.definition.id == NoAds.ProductId)
            {
                NoAds.Grant();
                // A restored item during Restore is settled by RestoreSettle instead.
                PurchasePending.Finish(PurchasePending.Outcome.Success, PurchasePending.Kind.Buy);
            }
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            NoAdsStoreEvents.Failed(product != null ? product.definition.id : null, reason);
        }

        // Ask to Buy (iOS) / pending payment (Play). Not owned yet: no grant. If the
        // approval lands later, ProcessPurchase grants then.
        void OnDeferred(Product product)
        {
            NoAdsStoreEvents.Deferred();
        }

        void Update()
        {
            NoAdsStoreEvents.Tick();
        }

        // Background and the StoreKit sheet both pause; PurchasePending only counts its
        // resume grace while the app is in front again.
        void OnApplicationPause(bool paused)
        {
            if (paused) PurchasePending.NoteBackground();
            else PurchasePending.NoteResume();
        }

        void OnDestroy()
        {
            if (NoAds.OwnsHost(this)) NoAds.DropHost();
        }
    }

    // Store answers that end the wait, kept off the MonoBehaviour so edit-mode
    // tests drive the same code the device does.
    public static class NoAdsStoreEvents
    {
        public static void Failed(string productId, PurchaseFailureReason reason)
        {
            if (reason == PurchaseFailureReason.UserCancelled)
            {
                NoAds.EndWait(PurchasePending.Outcome.Cancelled, PurchasePending.Kind.Buy);
                return;
            }
            // Play answers "item already owned" with DuplicateTransaction.
            if (reason == PurchaseFailureReason.DuplicateTransaction && productId == NoAds.ProductId)
            {
                NoAds.Grant();
                PurchasePending.Finish(PurchasePending.Outcome.Success, PurchasePending.Kind.Buy);
                return;
            }
            // A late failure (after the timeout) still gets its note.
            PurchasePending.Finish(PurchasePending.Outcome.Failed, PurchasePending.Kind.Buy);
            NoAds.NoteRestore(PurchasePending.NoteFor(PurchasePending.Outcome.Failed, PurchasePending.Kind.Buy));
        }

        public static void Deferred()
        {
            NoAds.EndWait(PurchasePending.Outcome.Deferred, PurchasePending.Kind.Buy);
        }

        public static void Tick()
        {
            var kind = PurchasePending.Current;
            var forced = PurchasePending.Tick();
            if (forced == PurchasePending.Outcome.None) return;
            string note = PurchasePending.NoteFor(forced, kind);
            if (!string.IsNullOrEmpty(note)) NoAds.NoteRestore(note);
        }
    }
}

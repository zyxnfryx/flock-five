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

        public static void Buy()
        {
            if (Owned) return;
            Ensure();
            if (_host != null) _host.Buy();
#if UNITY_EDITOR
            else Grant();
#endif
        }

        public static void Restore()
        {
            Ensure();
            if (_host == null)
            {
                NoteRestore("Couldn't reach the store");
                return;
            }
            _host.Restore();
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
                _ctl.InitiatePurchase(p);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[NoAds] purchase start failed: " + e.Message);
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
                    NoAds.NoteRestore(NoAds.Owned ? "Restored" : "Nothing to restore");
                    return;
                }
                StartCoroutine(RestoreSettle());
            }
            catch (System.Exception e)
            {
                _restoring = false;
                Debug.LogWarning("[NoAds] restore failed: " + e.Message);
                NoAds.NoteRestore("Couldn't restore");
            }
#endif
        }

        // The store's restore callback can land before or after ProcessPurchase for the
        // restored item, so wait for the callback, then give ProcessPurchase a moment
        // before deciding what to tell the player. Capped so the note always appears.
        IEnumerator RestoreSettle()
        {
            float t = 0f;
            while (!_restoreDone && t < 8f)
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
            if (!done || !ok) NoAds.NoteRestore(NoAds.Owned ? "Restored" : (done ? "Couldn't restore" : "Couldn't reach the store"));
            else NoAds.NoteRestore(NoAds.Owned ? "Restored" : "Nothing to restore");
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _ctl = controller;
            _ext = extensions;
            _starting = false;
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
                NoAds.Grant();
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            if (reason == PurchaseFailureReason.UserCancelled) return;
            // Play answers "item already owned" with DuplicateTransaction.
            if (reason == PurchaseFailureReason.DuplicateTransaction && product != null
                && product.definition.id == NoAds.ProductId)
            {
                NoAds.Grant();
                return;
            }
            NoAds.NoteRestore("Purchase didn't go through");
        }

        void OnDestroy()
        {
            if (NoAds.OwnsHost(this)) NoAds.DropHost();
        }
    }
}

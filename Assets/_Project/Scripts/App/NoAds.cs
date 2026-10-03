using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif

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

    sealed class NoAdsHost : MonoBehaviour
#if UNITY_PURCHASING
        , IStoreListener
#endif
    {
#if UNITY_PURCHASING
        IStoreController _ctl;
        IExtensionProvider _ext;
#endif

        public void Boot()
        {
#if UNITY_PURCHASING
            if (_ctl != null) return;
            var mod = StandardPurchasingModule.Instance();
            var b = ConfigurationBuilder.Instance(mod);
            b.AddProduct(NoAds.ProductId, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, b);
#endif
        }

        public void Buy()
        {
            if (NoAds.Owned) return;
#if UNITY_PURCHASING
            if (_ctl == null) return;
            _ctl.InitiatePurchase(NoAds.ProductId);
#elif UNITY_EDITOR
            NoAds.Grant();
#endif
        }

        public string StorePrice()
        {
#if UNITY_PURCHASING
            if (_ctl == null) return "";
            var p = _ctl.products.WithID(NoAds.ProductId);
            if (p == null || p.metadata == null) return "";
            var label = p.metadata.localizedPriceString;
            return string.IsNullOrEmpty(label) ? "" : label;
#else
            return "";
#endif
        }

        public void Restore()
        {
#if UNITY_PURCHASING
            if (_ext == null || _ctl == null)
            {
                NoAds.NoteRestore("Couldn't reach the store");
                return;
            }
            var apple = _ext.GetExtension<IAppleExtensions>();
            if (apple == null)
            {
                NoAds.NoteRestore(NoAds.Owned ? "Restored" : "Nothing to restore");
                return;
            }
            apple.RestoreTransactions(ok =>
            {
                if (!ok) NoAds.NoteRestore("Couldn't restore");
                else NoAds.NoteRestore(NoAds.Owned ? "Restored" : "Nothing to restore");
            });
#elif UNITY_EDITOR
            NoAds.NoteRestore(NoAds.Owned ? "Restored" : "Nothing to restore");
#else
            NoAds.NoteRestore("Couldn't reach the store");
#endif
        }

#if UNITY_PURCHASING
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _ctl = controller;
            _ext = extensions;
            var p = controller.products.WithID(NoAds.ProductId);
            if (p != null && p.hasReceipt) NoAds.Grant();
        }

        public void OnInitializeFailed(InitializationFailureReason error) { }

        public void OnInitializeFailed(InitializationFailureReason error, string message) { }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            if (args.purchasedProduct.definition.id == NoAds.ProductId)
                NoAds.Grant();
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) { }
#endif

        void OnDestroy()
        {
            if (NoAds.OwnsHost(this)) NoAds.DropHost();
        }
    }
}

using System.Collections;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace FlockFive
{
    // Rewarded bonus_branch stays opt-in. Interstitial is stage-clear only
    // (session clears 2, 5, 8…) and No Ads IAP silences that path alone.
    // No banners. Paste interstitial ad unit ids when LevelPlay has them.
    public static class Ads
    {
        public static bool Enabled = true;
        public static bool LastGranted;

        public const string PlacementBonus = "bonus_branch";
        public const string PlacementClear = "stage_clear";

        // Session garden clears this process. Resets on cold launch, not ladder.
        public static int SessionClears;

#if UNITY_IOS
        public const string AppKey = "282d0b97d";
        public const string RewardedUnitId = "kjzd8hybcb9wklmz";
        // Paste from LevelPlay → Ad units → Interstitial when the unit exists.
        public const string InterstitialUnitId = "";
#elif UNITY_ANDROID
        public const string AppKey = "282d36bdd";
        public const string RewardedUnitId = "dakjzwgzszpcx3k2";
        public const string InterstitialUnitId = "";
#else
        // Editor / standalone: same iOS app so Play Mode can init.
        public const string AppKey = "282d0b97d";
        public const string RewardedUnitId = "kjzd8hybcb9wklmz";
        public const string InterstitialUnitId = "";
#endif

        public static bool HasKeys =>
            !string.IsNullOrEmpty(AppKey) && !string.IsNullOrEmpty(RewardedUnitId);

        public static bool HasInterstitial =>
            !string.IsNullOrEmpty(AppKey) && !string.IsNullOrEmpty(InterstitialUnitId);

        static AdsHost _host;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _host = null;
            LastGranted = false;
            SessionClears = 0;
        }

        public static void Warm() => Ensure();

        public static bool CadenceHit()
        {
            int n = SessionClears;
            return n >= 2 && (n - 2) % 3 == 0;
        }

        public static IEnumerator Interstitial()
        {
            SessionClears++;
            if (!Enabled) yield break;
            if (NoAds.Owned) yield break;
            if (!CadenceHit()) yield break;
            Ensure();
            if (_host == null) yield break;
            yield return _host.RunInterstitial();
        }

        public static IEnumerator Rewarded()
        {
            LastGranted = false;
            if (!Enabled)
            {
                LastGranted = true;
                yield return new WaitForSeconds(0.35f);
                yield break;
            }
            Ensure();
            if (_host == null)
            {
                yield return Simulate();
                LastGranted = true;
                yield break;
            }
            yield return _host.RunRewarded();
        }

        internal static IEnumerator Simulate()
        {
            yield return new WaitForSeconds(2.15f);
        }

        static void Ensure()
        {
            if (_host != null) return;
            var found = Object.FindObjectsByType<AdsHost>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            AdsHost keep = null;
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
                var go = new GameObject("Ads");
                Object.DontDestroyOnLoad(go);
                keep = go.AddComponent<AdsHost>();
            }
            else
                Object.DontDestroyOnLoad(keep.gameObject);
            _host = keep;
            _host.Boot();
        }

        internal static bool OwnsHost(AdsHost h) => _host == h;

        internal static void DropHost() => _host = null;
    }

    sealed class AdsHost : MonoBehaviour
    {
        LevelPlayRewardedAd _rv;
        LevelPlayInterstitialAd _int;
        bool _inited;
        bool _initFailed;
        bool _waiting;
        bool _didReward;
        bool _intWaiting;

        public void Boot()
        {
            if (!Ads.HasKeys) return;
            if (_inited)
            {
                if (_rv == null) CreateRewarded();
                if (_int == null) CreateInterstitial();
                return;
            }
            LevelPlay.OnInitSuccess -= OnInitOk;
            LevelPlay.OnInitFailed -= OnInitFail;
            LevelPlay.OnInitSuccess += OnInitOk;
            LevelPlay.OnInitFailed += OnInitFail;
            LevelPlay.Init(Ads.AppKey);
        }

        public IEnumerator RunRewarded()
        {
            Ads.LastGranted = false;
            _didReward = false;

            if (!Ads.HasKeys)
            {
                yield return Ads.Simulate();
                Ads.LastGranted = true;
                yield break;
            }

            float t = 0f;
            while (!_inited && !_initFailed && t < 8f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_inited)
            {
#if UNITY_EDITOR
                yield return Ads.Simulate();
                Ads.LastGranted = true;
#endif
                yield break;
            }

            if (_rv == null) CreateRewarded();
            if (_rv == null) yield break;

            if (!_rv.IsAdReady())
            {
                _rv.LoadAd();
                t = 0f;
                while (!_rv.IsAdReady() && t < 12f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (!_rv.IsAdReady())
            {
#if UNITY_EDITOR
                yield return Ads.Simulate();
                Ads.LastGranted = true;
#endif
                yield break;
            }

            _waiting = true;
            _didReward = false;
            Ads.LastGranted = false;
            _rv.ShowAd(Ads.PlacementBonus);
            t = 0f;
            while (_waiting && t < 180f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Ads.LastGranted = _didReward;
            _rv.LoadAd();
        }

        void OnInitOk(LevelPlayConfiguration config)
        {
            _inited = true;
            _initFailed = false;
            CreateRewarded();
            CreateInterstitial();
        }

        void OnInitFail(LevelPlayInitError error)
        {
            _initFailed = true;
        }

        void CreateRewarded()
        {
            if (_rv != null || string.IsNullOrEmpty(Ads.RewardedUnitId)) return;
            _rv = new LevelPlayRewardedAd(Ads.RewardedUnitId);
            _rv.OnAdRewarded += OnRewarded;
            _rv.OnAdClosed += OnClosed;
            _rv.OnAdLoadFailed += OnLoadFail;
            _rv.OnAdDisplayFailed += OnDisplayFail;
            _rv.LoadAd();
        }

        public IEnumerator RunInterstitial()
        {
            if (!Ads.HasInterstitial) yield break;
            float t = 0f;
            while (!_inited && !_initFailed && t < 4f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!_inited) yield break;
            if (_int == null) CreateInterstitial();
            if (_int == null) yield break;
            if (!_int.IsAdReady()) yield break;
            _intWaiting = true;
            _int.ShowAd(Ads.PlacementClear);
            t = 0f;
            while (_intWaiting && t < 180f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            _int.LoadAd();
        }

        void CreateInterstitial()
        {
            if (_int != null || string.IsNullOrEmpty(Ads.InterstitialUnitId)) return;
            _int = new LevelPlayInterstitialAd(Ads.InterstitialUnitId);
            _int.OnAdClosed += OnIntClosed;
            _int.OnAdLoadFailed += OnIntLoadFail;
            _int.OnAdDisplayFailed += OnIntDisplayFail;
            _int.LoadAd();
        }

        void OnIntClosed(LevelPlayAdInfo info)
        {
            _intWaiting = false;
        }

        void OnIntLoadFail(LevelPlayAdError error)
        {
            _intWaiting = false;
        }

        void OnIntDisplayFail(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            _intWaiting = false;
        }

        void OnRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            _didReward = true;
            Ads.LastGranted = true;
        }

        void OnClosed(LevelPlayAdInfo info)
        {
            _waiting = false;
        }

        void OnLoadFail(LevelPlayAdError error)
        {
            _waiting = false;
        }

        void OnDisplayFail(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            _waiting = false;
        }

        void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= OnInitOk;
            LevelPlay.OnInitFailed -= OnInitFail;
            if (_rv != null)
            {
                _rv.OnAdRewarded -= OnRewarded;
                _rv.OnAdClosed -= OnClosed;
                _rv.OnAdLoadFailed -= OnLoadFail;
                _rv.OnAdDisplayFailed -= OnDisplayFail;
            }
            if (_int != null)
            {
                _int.OnAdClosed -= OnIntClosed;
                _int.OnAdLoadFailed -= OnIntLoadFail;
                _int.OnAdDisplayFailed -= OnIntDisplayFail;
            }
            if (Ads.OwnsHost(this)) Ads.DropHost();
        }
    }
}


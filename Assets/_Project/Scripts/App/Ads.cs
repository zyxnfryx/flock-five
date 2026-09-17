using System.Collections;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace FlockFive
{
    // Bonus-branch rewarded goes through Rewarded(). Interstitial stays a
    // no-op until a post-win placement is designed. No banners.
    // Paste LevelPlay iOS/Android app keys + rewarded ad unit ids below.
    public static class Ads
    {
        public static bool Enabled = true;
        public static bool LastGranted;

        public const string PlacementBonus = "bonus_branch";

#if UNITY_IOS
        public const string AppKey = "282d0b97d";
        public const string RewardedUnitId = "kjzd8hybcb9wklmz";
#elif UNITY_ANDROID
        public const string AppKey = "";
        public const string RewardedUnitId = "";
#else
        // Editor / standalone: same iOS app so Play Mode can init.
        public const string AppKey = "282d0b97d";
        public const string RewardedUnitId = "kjzd8hybcb9wklmz";
#endif

        public static bool HasKeys =>
            !string.IsNullOrEmpty(AppKey) && !string.IsNullOrEmpty(RewardedUnitId);

        static AdsHost _host;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _host = null;
            LastGranted = false;
        }

        public static void Warm() => Ensure();

        public static IEnumerator Interstitial()
        {
            if (!Enabled) yield break;
#if UNITY_EDITOR
            yield break;
#else
            yield return ShowInterstitial();
#endif
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

        static IEnumerator ShowInterstitial()
        {
            yield break;
        }

        internal static bool OwnsHost(AdsHost h) => _host == h;

        internal static void DropHost() => _host = null;
    }

    sealed class AdsHost : MonoBehaviour
    {
        LevelPlayRewardedAd _rv;
        bool _inited;
        bool _initFailed;
        bool _waiting;
        bool _didReward;

        public void Boot()
        {
            if (!Ads.HasKeys) return;
            if (_inited)
            {
                if (_rv == null) CreateRewarded();
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
            if (Ads.OwnsHost(this)) Ads.DropHost();
        }
    }
}


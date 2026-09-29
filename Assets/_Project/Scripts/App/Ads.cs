using System.Collections;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace FlockFive
{
    // Rewarded bonus_branch stays opt-in. Interstitial is stage-clear only
    // (clears 2, 5, 8…) and No Ads IAP silences that path alone. No banners.
    // TestFlight ad log: every ad request / show / close plus app pause and
    // resume, stamped with local time and what the player was doing. Hold
    // four fingers for 2 seconds to open the list; tap to close. Kept across
    // relaunches (last 60 lines). Remove with TestSuite before launch.
    public static class AdLog
    {
        const string Pref = "flockfive.adlog";
        const int Max = 60;
        static readonly System.Collections.Generic.List<string> _lines = new System.Collections.Generic.List<string>();
        static bool _loaded, _dirty;
        public static System.Func<string> Context;

        public static System.Collections.Generic.List<string> Lines { get { Load(); return _lines; } }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            var raw = PlayerPrefs.GetString(Pref, "");
            if (!string.IsNullOrEmpty(raw)) _lines.AddRange(raw.Split('\n'));
        }

        public static void Add(string what)
        {
            string ctx = "?";
            try { if (Context != null) ctx = Context(); } catch { }
            lock (_lines)
            {
                Load();
                _lines.Add(System.DateTime.Now.ToString("MM/dd HH:mm:ss") + "  " + what + "  [" + ctx + "]");
                while (_lines.Count > Max) _lines.RemoveAt(0);
                _dirty = true;
            }
            Debug.Log("[AdLog] " + what + " [" + ctx + "]");
        }

        // Main thread only.
        public static void Flush()
        {
            if (!_dirty) return;
            lock (_lines)
            {
                PlayerPrefs.SetString(Pref, string.Join("\n", _lines));
                PlayerPrefs.Save();
                _dirty = false;
            }
        }

        public static void Clear()
        {
            lock (_lines) { Load(); _lines.Clear(); _dirty = true; }
            Flush();
        }
    }

    public static class Ads
    {
        public static bool Enabled = true;

        // TestFlight only: hidden LevelPlay test suite (hold three fingers
        // on the screen for 2 seconds). Set false before App Store launch.
        public const bool TestSuite = true;
        public static bool LastGranted;

        public const string PlacementBonus = "bonus_branch";
        public const string PlacementClear = "stage_clear";

        const string PrefClears = "flockfive.session_clears";

        // Garden clears toward 2, 5, 8… Survives quit/relaunch. Not ladder instage.
        public static int SessionClears;

#if UNITY_IOS
        public const string AppKey = "282d0b97d";
        public const string RewardedUnitId = "kjzd8hybcb9wklmz";
        public const string InterstitialUnitId = "gjnd3xxjtz2lpag1"; // stage_clear
#elif UNITY_ANDROID
        public const string AppKey = "282d36bdd";
        public const string RewardedUnitId = "dakjzwgzszpcx3k2";
        public const string InterstitialUnitId = "97jpjr0pna1yghuh"; // stage_clear
#else
        // Editor / standalone: iOS ids so Play Mode can init interstitial too
        public const string AppKey = "282d0b97d";
        public const string RewardedUnitId = "kjzd8hybcb9wklmz";
        public const string InterstitialUnitId = "gjnd3xxjtz2lpag1";
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
            SessionClears = PlayerPrefs.GetInt(PrefClears, 0);
        }

        public static void Warm()
        {
            SessionClears = PlayerPrefs.GetInt(PrefClears, SessionClears);
            Ensure();
        }

        public static bool CadenceHit()
        {
            int n = SessionClears;
            return n >= 2 && (n - 2) % 3 == 0;
        }

        public static IEnumerator Interstitial()
        {
            SessionClears++;
            PlayerPrefs.SetInt(PrefClears, SessionClears);
            PlayerPrefs.Save();
            bool hit = CadenceHit();
            AdLog.Add("clear #" + SessionClears + (hit ? " -> ad turn" : " -> no ad turn")
                + (!Enabled ? " (ads off)" : "") + (NoAds.Owned ? " (No Ads owned)" : ""));
            if (!Enabled) yield break;
            if (NoAds.Owned) yield break;
            if (!hit) yield break;
            Ensure();
            if (_host == null) { AdLog.Add("interstitial skipped: no ad host"); yield break; }
            yield return _host.RunInterstitial();
        }

        public static IEnumerator Rewarded()
        {
            AdLog.Add("gift ad requested (gift tapped)");
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
        bool _shown;
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
            if (Ads.TestSuite) LevelPlay.SetMetaData("is_test_suite", "enable");
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
            while (!_inited && !_initFailed && t < 5f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_inited)
            {
                AdLog.Add("gift ad unavailable (ads not ready) -> gift granted anyway");
                Ads.LastGranted = true;
                yield break;
            }

            if (_rv == null) CreateRewarded();
            if (_rv == null)
            {
                AdLog.Add("gift ad unavailable (no ad unit) -> gift granted anyway");
                Ads.LastGranted = true;
                yield break;
            }

            if (!_rv.IsAdReady())
            {
                _rv.LoadAd();
                t = 0f;
                while (!_rv.IsAdReady() && t < 6f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (!_rv.IsAdReady())
            {
                AdLog.Add("gift ad did not load -> gift granted anyway");
                Ads.LastGranted = true;
                _rv.LoadAd();
                yield break;
            }

            _waiting = true;
            _shown = false;
            _didReward = false;
            Ads.LastGranted = false;
            AdLog.Add("gift ad show called");
            _rv.ShowAd(Ads.PlacementBonus);
            t = 0f;
            while (_waiting && t < 180f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            // Permissive: only withhold the gift when an ad really played and the
            // player closed it before the reward. Any failure still grants it.
            Ads.LastGranted = _didReward || !_shown;
            if (!_didReward && !_shown) AdLog.Add("gift ad failed to show -> gift granted anyway");
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
            _rv.OnAdDisplayed += OnRvShown;
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
            if (!_inited) { AdLog.Add("interstitial skipped: SDK not ready"); yield break; }
            if (_int == null) CreateInterstitial();
            if (_int == null) yield break;
            if (!_int.IsAdReady()) { AdLog.Add("interstitial skipped: no ad loaded"); yield break; }
            _intWaiting = true;
            AdLog.Add("interstitial show called");
            _int.ShowAd(Ads.PlacementClear);
            t = 0f;
            while (_intWaiting && t < 180f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_intWaiting) AdLog.Add("interstitial wait TIMED OUT after 180s; game resumed");
            _intWaiting = false;
            _int.LoadAd();
        }

        void CreateInterstitial()
        {
            if (_int != null || string.IsNullOrEmpty(Ads.InterstitialUnitId)) return;
            _int = new LevelPlayInterstitialAd(Ads.InterstitialUnitId);
            _int.OnAdClosed += OnIntClosed;
            _int.OnAdLoadFailed += OnIntLoadFail;
            _int.OnAdDisplayFailed += OnIntDisplayFail;
            _int.OnAdDisplayed += OnIntShown;
            _int.LoadAd();
        }

        void OnIntShown(LevelPlayAdInfo info)
        {
            AdLog.Add("INTERSTITIAL ON SCREEN" + (_intWaiting ? "" : " (NOT requested by game!)"));
        }

        void OnIntClosed(LevelPlayAdInfo info)
        {
            AdLog.Add("interstitial closed");
            _intWaiting = false;
        }

        void OnIntLoadFail(LevelPlayAdError error)
        {
            if (_intWaiting) AdLog.Add("interstitial load failed while waiting");
            _intWaiting = false;
        }

        void OnIntDisplayFail(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            AdLog.Add("interstitial display failed");
            _intWaiting = false;
        }

        void OnRvShown(LevelPlayAdInfo info)
        {
            _shown = true;
            AdLog.Add("GIFT AD ON SCREEN" + (_waiting ? "" : " (NOT requested by game!)"));
        }

        void OnRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            _didReward = true;
            Ads.LastGranted = true;
        }

        void OnClosed(LevelPlayAdInfo info)
        {
            AdLog.Add("gift ad closed" + (_didReward ? " (rewarded)" : " (no reward)"));
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

        float _suiteHold, _logHold;
        bool _showLog, _logArm;
        Vector2 _logScroll;

        void OnApplicationPause(bool paused)
        {
            AdLog.Add(paused ? "app left foreground" : "app back in foreground");
            if (paused) AdLog.Flush();
        }

        void Update()
        {
            AdLog.Flush();
            if (!Ads.TestSuite) return;
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts == null) { _suiteHold = 0f; _logHold = 0f; return; }
            int down = 0;
            var touches = ts.touches;
            for (int i = 0; i < touches.Count; i++)
                if (touches[i].press.isPressed) down++;

            if (_showLog) return; // OnGUI handles close

            // Four fingers for 2s opens the ad log (checked before the suite).
            if (down >= 4)
            {
                _suiteHold = -999f;
                _logHold += Time.unscaledDeltaTime;
                if (_logHold >= 2f)
                {
                    _logHold = -999f;
                    _showLog = true;
                    _logArm = false;
                    _logScroll = new Vector2(0f, 1e6f);
                }
                return;
            }
            _logHold = 0f;

            if (!_inited) return;
            if (down == 3)
            {
                _suiteHold += Time.unscaledDeltaTime;
                if (_suiteHold >= 2f)
                {
                    _suiteHold = -999f; // once per hold
                    AdLog.Add("ad test suite opened (3-finger hold)");
                    LevelPlay.LaunchTestSuite();
                }
            }
            else if (down == 0) _suiteHold = 0f;
        }

        void OnGUI()
        {
            if (!_showLog) return;
            float s = Screen.width / 400f;
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = 400f, h = Screen.height / s;
            GUI.color = new Color(0f, 0f, 0f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var st = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            st.normal.textColor = Color.white;
            var lines = AdLog.Lines;
            GUI.Label(new Rect(8, 40, w - 16, 20), "AD LOG (" + lines.Count + ")  ·  tap Close when done", st);
            _logScroll = GUI.BeginScrollView(new Rect(4, 64, w - 8, h - 130), _logScroll,
                new Rect(0, 0, w - 30, Mathf.Max(lines.Count * 30f, 10f)));
            for (int i = 0; i < lines.Count; i++)
                GUI.Label(new Rect(4, i * 30f, w - 34, 30f), lines[i], st);
            GUI.EndScrollView();
            if (GUI.Button(new Rect(8, h - 58, 120, 44), "Close")) _showLog = false;
            if (GUI.Button(new Rect(w - 128, h - 58, 120, 44), _logArm ? "Tap again" : "Clear log"))
            {
                if (_logArm) { AdLog.Clear(); _logArm = false; }
                else _logArm = true;
            }
            GUI.matrix = old;
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
                _rv.OnAdDisplayed -= OnRvShown;
            }
            if (_int != null)
            {
                _int.OnAdClosed -= OnIntClosed;
                _int.OnAdLoadFailed -= OnIntLoadFail;
                _int.OnAdDisplayFailed -= OnIntDisplayFail;
                _int.OnAdDisplayed -= OnIntShown;
            }
            if (Ads.OwnsHost(this)) Ads.DropHost();
        }
    }
}


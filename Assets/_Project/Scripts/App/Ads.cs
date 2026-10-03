using System.Collections;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace FlockFive
{
    // Rewarded bonus_branch stays opt-in. Stage-clear interstitial is clears
    // 2, 5, 8… Video poker has its own every-10-hands interstitial on the
    // same show path. No Ads silences both. No banners.
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

    // Nested pause for an ad (or anything else that must freeze the garden).
    // Time.timeScale and the Unity listener are restored to the values Push saved.
    // PlayClock, haptics, and the weather ticks read Paused for unscaled work.
    public static class GamePause
    {
        static int _depth;
        static float _scale = 1f;
        static bool _audio;

        public static bool Paused => _depth > 0;

        public static void Push()
        {
            if (_depth == 0)
            {
                _scale = Time.timeScale;
                _audio = AudioListener.pause;
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            _depth++;
        }

        public static void Pop()
        {
            if (_depth == 0) return;
            _depth--;
            if (_depth > 0) return;
            Time.timeScale = _scale;
            AudioListener.pause = _audio;
        }

        public static void Reset()
        {
            if (_depth > 0)
            {
                Time.timeScale = _scale;
                AudioListener.pause = _audio;
            }
            _depth = 0;
        }
    }

    public static class Ads
    {
        public static bool Enabled = true;

        // TestFlight only: hidden LevelPlay test suite (hold three fingers
        // on the screen for 2 seconds). Set false before App Store launch.
        public const bool TestSuite = true;
        public static bool LastGranted;
        // True only when a rewarded ad actually paid (or the editor simulate). No-fill stays false.
        public static bool LastEarned;

        public const string PlacementBonus = "bonus_branch";
        public const string PlacementClear = "stage_clear";

        const string PrefClears = "flockfive.session_clears";
        const string PrefPokerHands = "flockfive.ads.pokerhands";
        public const int PokerHandCadence = 10;

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
        static int _showDepth;
        static float _shownAt = -99f;

        public static bool IsLoading { get; private set; }
        public static bool IsShowing => _showDepth > 0;
        public static bool IsBusy => IsLoading || IsShowing;
        public static bool JustShown => Time.unscaledTime - _shownAt < 0.75f;
        public static float ShownFor => Time.unscaledTime - _shownAt;

        public static void NoteLoad(bool on) => IsLoading = on;

        public static void BeginShow()
        {
            if (_showDepth == 0)
            {
                _shownAt = Time.unscaledTime;
                GamePause.Push();
            }
            _showDepth++;
        }

        public static void EndShow()
        {
            if (_showDepth == 0) return;
            _showDepth--;
            if (_showDepth == 0) GamePause.Pop();
        }

        // Missed close, focus return, or a host that died mid-ad. Idempotent.
        public static void ForceClear()
        {
            IsLoading = false;
            while (_showDepth > 0) EndShow();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _host = null;
            LastGranted = false;
            LastEarned = false;
            SessionClears = PlayerPrefs.GetInt(PrefClears, 0);
            IsLoading = false;
            _showDepth = 0;
            _shownAt = -99f;
            GamePause.Reset();
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

        // Completed video-poker hands. Independent of SessionClears.
        public static int PokerHands => PlayerPrefs.GetInt(PrefPokerHands, 0);

        public static bool PokerAdDue => PokerHands >= PokerHandCadence;

        public static void NotePokerHand()
        {
            int n = PokerHands + 1;
            PlayerPrefs.SetInt(PrefPokerHands, n);
            PlayerPrefs.Save();
            AdLog.Add("poker hand #" + n + (n >= PokerHandCadence ? " -> ad turn" : " -> counting"));
        }

        // One interstitial after each 10th completed hand. Resets the counter
        // on that turn whether the ad is shown or skipped (ads off / No Ads).
        public static IEnumerator PokerInterstitial()
        {
            int n = PokerHands;
            bool owned = NoAds.Owned;
            bool allow = Enabled && !owned && n >= PokerHandCadence;
            AdLog.Add("poker hands " + n
                + (allow ? " -> show" : " -> skipped")
                + (!Enabled ? " (ads off)" : "")
                + (owned ? " (No Ads owned)" : ""));
            if (n >= PokerHandCadence)
            {
                PlayerPrefs.SetInt(PrefPokerHands, 0);
                PlayerPrefs.Save();
            }
            if (!allow) yield break;
            Ensure();
            if (_host == null) { AdLog.Add("poker interstitial skipped: no ad host"); yield break; }
            yield return _host.RunInterstitial();
        }

        public static IEnumerator Rewarded()
        {
            AdLog.Add("gift ad requested (gift tapped)");
            LastGranted = false;
            LastEarned = false;
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
                LastEarned = true;
                yield break;
            }
            yield return _host.RunRewarded();
        }

        internal static IEnumerator Simulate()
        {
            BeginShow();
            try
            {
                float t = 0f;
                while (t < 2.15f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            finally
            {
                EndShow();
            }
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
        bool _orphanRv;
        bool _orphanInt;

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
            Ads.LastEarned = false;
            _didReward = false;
            bool loading = false;
            bool showing = false;
            try
            {
                if (!Ads.HasKeys)
                {
                    yield return Ads.Simulate();
                    Ads.LastGranted = true;
                    Ads.LastEarned = true;
                    yield break;
                }

                Ads.NoteLoad(true);
                loading = true;
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

                Ads.NoteLoad(false);
                loading = false;
                _waiting = true;
                _shown = false;
                _didReward = false;
                Ads.LastGranted = false;
                Ads.BeginShow();
                showing = true;
                AdLog.Add("gift ad show called");
                _rv.ShowAd(Ads.PlacementBonus);
                t = 0f;
                while (_waiting && t < 180f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (_waiting) AdLog.Add("gift ad wait TIMED OUT after 180s; game resumed");
                // Permissive: only withhold the gift when an ad really played and the
                // player closed it before the reward. Any failure still grants it.
                Ads.LastGranted = _didReward || !_shown;
                Ads.LastEarned = _didReward;
                if (!_didReward && !_shown) AdLog.Add("gift ad failed to show -> gift granted anyway");
                _rv.LoadAd();
            }
            finally
            {
                _waiting = false;
                if (loading) Ads.NoteLoad(false);
                if (showing) Ads.EndShow();
            }
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
            bool loading = false;
            bool showing = false;
            try
            {
                Ads.NoteLoad(true);
                loading = true;
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
                Ads.NoteLoad(false);
                loading = false;
                _intWaiting = true;
                Ads.BeginShow();
                showing = true;
                AdLog.Add("interstitial show called");
                _int.ShowAd(Ads.PlacementClear);
                t = 0f;
                while (_intWaiting && t < 180f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (_intWaiting) AdLog.Add("interstitial wait TIMED OUT after 180s; game resumed");
                _int.LoadAd();
            }
            finally
            {
                _intWaiting = false;
                if (loading) Ads.NoteLoad(false);
                if (showing) Ads.EndShow();
            }
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
            bool owned = _intWaiting;
            AdLog.Add("INTERSTITIAL ON SCREEN" + (owned ? "" : " (NOT requested by game!)"));
            if (owned || _orphanInt) return;
            _orphanInt = true;
            Ads.BeginShow();
        }

        void OnIntClosed(LevelPlayAdInfo info)
        {
            AdLog.Add("interstitial closed");
            _intWaiting = false;
            if (!_orphanInt) return;
            _orphanInt = false;
            Ads.EndShow();
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
            bool owned = _waiting;
            AdLog.Add("GIFT AD ON SCREEN" + (owned ? "" : " (NOT requested by game!)"));
            if (owned || _orphanRv) return;
            _orphanRv = true;
            Ads.BeginShow();
        }

        void OnRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            _didReward = true;
            Ads.LastGranted = true;
            Ads.LastEarned = true;
        }

        void OnClosed(LevelPlayAdInfo info)
        {
            AdLog.Add("gift ad closed" + (_didReward ? " (rewarded)" : " (no reward)"));
            _waiting = false;
            if (!_orphanRv) return;
            _orphanRv = false;
            Ads.EndShow();
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

        bool _leftApp;

        void OnApplicationPause(bool paused)
        {
            AdLog.Add(paused ? "app left foreground" : "app back in foreground");
            if (paused)
            {
                _leftApp = true;
                AdLog.Flush();
                return;
            }
            // The ad activity took the foreground. Coming back means it is gone,
            // even when the close callback never arrived. Ignore the flicker
            // some SDKs emit in the first moments of ShowAd.
            if (_leftApp && Ads.IsShowing && !Ads.JustShown)
                Ads.ForceClear();
            _leftApp = false;
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus || !_leftApp || !Ads.IsShowing || Ads.JustShown) return;
            Ads.ForceClear();
            _leftApp = false;
        }

        void Update()
        {
            AdLog.Flush();
            if (Ads.IsShowing && Ads.ShownFor > 185f)
                Ads.ForceClear();
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
            _orphanRv = false;
            _orphanInt = false;
            if (Ads.OwnsHost(this)) Ads.DropHost();
            Ads.ForceClear();
        }
    }
}


using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using Unity.Services.LevelPlay;
using Object = UnityEngine.Object;

namespace FlockFive
{
    // Rewarded bonus_branch stays opt-in. Stage-clear interstitial is clears
    // 2, 5, 8… except a new player sees none before lifetime clear 6, and none
    // within 75s of any ad (rewarded included). A watched clear-double skips
    // that clear's interstitial. No Ads silences interstitials only.
    // Video poker uses the same interstitial gates. No banners.
    // Ad log: every ad request / show / close plus app pause and resume,
    // stamped with local time and what the player was doing. Kept across
    // relaunches (last 60 lines). The on-screen four-finger list is compiled
    // only with FF_ADS_DEV.
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

        // A tutorial step froze the garden on purpose (the sparrow lesson). Its glove,
        // caption fade and tap gates still have to run, so PlayClock keeps counting
        // under this pause. An ad on screen still stops the clock.
        public static bool TutorHold;

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

        // Build 61: the unpaused time scale (1, or the solved-board auto-resolve speed). Under a
        // pause it becomes the value Pop restores, so a freeze never strands either one.
        public static float BaseScale => _depth > 0 ? _scale : (Time.timeScale > 0f ? Time.timeScale : (_scale > 0f ? _scale : 1f));

        public static void SetBaseScale(float scale)
        {
            if (_depth > 0) _scale = scale;
            else Time.timeScale = scale;
        }

        public static void Reset()
        {
            if (_depth > 0)
            {
                Time.timeScale = _scale;
                AudioListener.pause = _audio;
            }
            _depth = 0;
            TutorHold = false;
        }

        // Pop restores the pause bit Push saved. A dialog that paused the
        // listener first makes that bit true, and the game stays silent.
        // Restore clears the saved bit and the live pause together.
        public static void UnstickAudio()
        {
            _audio = false;
            AudioListener.pause = false;
            AudioListener.volume = 1f;
        }
    }

    public static class Ads
    {
        public static bool Enabled = true;

        // Release leaves the LevelPlay test suite off. The is_test_suite
        // metadata, the three-finger suite launch, and the four-finger
        // on-screen ad log are compiled only when FF_ADS_DEV is set. That
        // define is not added to Player Settings.
#if FF_ADS_DEV
        public const bool TestSuite = true;
        public const bool MockRewarded = true;
#else
        public const bool TestSuite = false;
        public const bool MockRewarded = false;
#endif
        public static bool LastGranted;
        // True only when a rewarded ad actually paid (or the editor simulate). No-fill stays false.
        public static bool LastEarned;

        public const string PlacementBonus = "bonus_branch";
        public const string PlacementClear = "stage_clear";
        public const string PlacementClearDouble = "clear_double";
        public const string PlacementDailyDouble = "daily_double";
        public const string PlacementKeep = "keep_multiplier";
        public const string PlacementRewardRepeat = "reward_repeat";

        // Build 83 item 10i: the REWARD popup's 2X needs a loaded rewarded ad that is
        // not held back by the cooldown or the new-player grace.
        public static bool RewardRepeatAvailable(bool enabled, bool ready, int clears, double now, double lastAnyAd)
        {
            return enabled && ready && !GraceBlocks(clears) && !Cooling(now, lastAnyAd);
        }

        public static bool RewardRepeatReady =>
            RewardRepeatAvailable(Enabled, RewardedReady, SessionClears, AdClock, LastAnyAd);
        public static string ActivePlacement = PlacementBonus;

        const string PrefClears = "flockfive.session_clears";
        const string PrefPokerHands = "flockfive.ads.pokerhands";
        const string PrefLastAd = "flockfive.ads.lastany";
        public const string PrefVipSoft = "flockfive.vip.soft";
        public const int PokerHandCadence = 10;
        // No interstitial on lifetime clears 1–5. Clear 6 is the first that may.
        public const int InterstitialGraceClears = 6;
        public const double InterstitialCooldownSec = 75;

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
        static bool _skipClearInterstitial;
        static bool _intDisplayed;
        static double _lastAnyAd;
        static bool _clockSet;
        static double _clock;
        static int _softDay;
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // -1 follows the real rewarded unit. 0 forces not ready. 1 forces ready (stills).
        static int _readyOverride = -1;
        // Foreground seconds the current show has been up. The resume frame's
        // unscaled step is the whole suspension, so it must not move this.
        static float _shownForeground;
        static float _settleLeft = -1f;

        public static bool IsLoading { get; private set; }
        public static bool IsShowing => _showDepth > 0;

        public static bool RewardedReady
        {
            get
            {
                if (!Enabled) return false;
                if (_readyOverride == 0) return false;
                if (_readyOverride > 0) return true;
                return _host != null && _host.AdReady();
            }
        }

        public static void SetReadyOverride(int mode) => _readyOverride = mode;

        // Drops a displayed-interstitial bit left by another suite. Does not touch VIP.
        public static void ResetTransientForTests() => _intDisplayed = false;
        public static bool IsBusy => IsLoading || IsShowing;
        public static bool JustShown => _showDepth > 0 && _shownForeground < 0.75f;
        public static float ShownFor => _shownForeground;

        // Steps the ad waits. Runs during GamePause (the show freezes PlayClock)
        // and drops a suspension spike so a resume cannot finish the wait.
        internal static float ForegroundDt()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f || dt > 0.5f) return 0f;
            return dt;
        }

        public static void NoteLoad(bool on) => IsLoading = on;

        public static void BeginShow()
        {
            if (_showDepth == 0)
            {
                _shownForeground = 0f;
                _settleLeft = -1f;
                GamePause.Push();
                AdPause.Begin();
            }
            _showDepth++;
            HoldAdAudio();
        }

        // One hold for every ShowAd. MasterGain is 0 while IsShowing, so the
        // Settings slider and mute multiply back in on release. The listener
        // pause stops one-shots already in flight. Reward callbacks must not
        // release this; close, fail, and skip do, through EndShow / SkipShow.
        public static void HoldAdAudio()
        {
            if (!IsShowing) return;
            AudioGuard.Hold("ad");
            if (!AudioListener.pause) AudioListener.pause = true;
            if (MixDesk.Live != null) MixDesk.Live.ApplyAdSilence();
        }

        public static void Present(Action show)
        {
            BeginShow();
            try
            {
                if (show != null) show();
            }
            catch (Exception e)
            {
                Debug.LogWarning("ad show failed: " + e.Message);
                EndShow();
                return;
            }
            HoldAdAudio();
        }

        // Player skipped, or the wait gave up. Same release as close and fail.
        public static void SkipShow()
        {
            if (IsShowing) EndShow();
        }

        public static void EndShow()
        {
            if (_showDepth == 0) return;
            _showDepth--;
            if (_showDepth > 0) return;
            _settleLeft = -1f;
            GamePause.Pop();
            // Pop can put back a pause that was already true when the ad began.
            AudioGuard.Restore();
            // Everything restored; only now does a reward earned during the ad apply.
            AdPause.End();
        }

        // Home is interactive. Never the first frames. Then either half a
        // second with no pointer, or two seconds even if the player is still
        // touching. LevelPlay.Init waits for this so it cannot hitch the
        // opening frames or leave the audio session ducked under them.
        public static bool IdleDue(int framesSeen, float sinceHome, float quietFor, bool pointerDown)
        {
            if (framesSeen < 2) return false;
            if (sinceHome >= 2f) return true;
            return !pointerDown && quietFor >= 0.5f;
        }

        public static bool PointerHeld()
        {
            try
            {
                var ts = UnityEngine.InputSystem.Touchscreen.current;
                if (ts != null)
                {
                    var touches = ts.touches;
                    for (int i = 0; i < touches.Count; i++)
                        if (touches[i].press.isPressed) return true;
                }
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed) return true;
            }
            catch (System.Exception) { }
            return false;
        }

        // Missed close, focus return, or a host that died mid-ad. Idempotent.
        public static void ForceClear()
        {
            IsLoading = false;
            _settleLeft = -1f;
            while (_showDepth > 0) EndShow();
        }

        // Coming back while a show is up. A late reward callback still has a
        // beat; then one release lets the owning coroutine finish the grant.
        public static void ArmResumeSettle()
        {
            if (!IsShowing || JustShown) return;
            if (_settleLeft < 0f) _settleLeft = 0.35f;
        }

        public static void TickShowClock()
        {
            AdPause.Hold();
            // The ad token times out in 10s. A show is longer than that, so the
            // hold is refreshed every tick until close, fail, or skip.
            if (IsShowing) HoldAdAudio();
            if (_showDepth > 0)
                _shownForeground += ForegroundDt();
            if (_settleLeft < 0f) return;
            _settleLeft -= ForegroundDt();
            if (_settleLeft > 0f) return;
            _settleLeft = -1f;
            if (_host != null) _host.ReleaseAfterResume();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _host = null;
            LastGranted = false;
            LastEarned = false;
            ActivePlacement = PlacementBonus;
            _readyOverride = -1;
            SessionClears = PlayerPrefs.GetInt(PrefClears, 0);
            IsLoading = false;
            _showDepth = 0;
            _shownForeground = 0f;
            _settleLeft = -1f;
            _skipClearInterstitial = false;
            _intDisplayed = false;
            VipSoftPending = false;
            _clockSet = false;
            _clock = 0;
            _softDay = 0;
            LoadLastAd();
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
            return CadenceHit(n);
        }

        public static bool CadenceHit(int lifetimeClears)
        {
            return lifetimeClears >= 2 && (lifetimeClears - 2) % 3 == 0;
        }

        // Unix seconds. Tests pin this; play mode follows UTC.
        public static double AdClock
        {
            get
            {
                if (_clockSet) return _clock;
                return (DateTime.UtcNow - UnixEpoch).TotalSeconds;
            }
        }

        public static double LastAnyAd => _lastAnyAd;

        // Set when an interstitial actually closed on screen. The clear flow
        // reads it once and may show the soft Go VIP card.
        public static bool VipSoftPending { get; private set; }

        public static void SetClockForTest(double unix)
        {
            _clock = unix;
            _clockSet = true;
        }

        public static void ClearClockForTest()
        {
            _clockSet = false;
            _clock = 0;
        }

        public static void SetLastAnyForTest(double unix) => _lastAnyAd = unix;

        public static void SetSoftDayForTest(int stamp) => _softDay = stamp;

        public static int SoftDay
        {
            get
            {
                if (_softDay > 0) return _softDay;
                var n = DateTime.Now;
                return n.Year * 10000 + n.Month * 100 + n.Day;
            }
        }

        // Rewarded clear-double is optional and is not silenced by VIP.
        public static bool ClearRewardOffered(bool enabled, bool rewardedReady)
        {
            return enabled && rewardedReady;
        }

        // The watched clear-double skips only the interstitial for this clear.
        public static void MarkClearRewardWatched() => _skipClearInterstitial = true;

        public static bool TakeClearSkip()
        {
            bool skip = _skipClearInterstitial;
            _skipClearInterstitial = false;
            return skip;
        }

        public static bool GraceBlocks(int lifetimeClears)
        {
            return lifetimeClears < InterstitialGraceClears;
        }

        public static bool Cooling(double now, double lastAnyAd)
        {
            return lastAnyAd > 0 && now - lastAnyAd < InterstitialCooldownSec;
        }

        // Shared interstitial gate. VIP suppresses interstitials only.
        // rewardSkip is the watched clear-double. Cadence is the caller's.
        public static bool InterstitialBlocked(int lifetimeClears, bool vip, bool enabled, bool rewardSkip, double now, double lastAnyAd)
        {
            if (!enabled) return true;
            if (vip) return true;
            if (rewardSkip) return true;
            if (GraceBlocks(lifetimeClears)) return true;
            if (Cooling(now, lastAnyAd)) return true;
            return false;
        }

        public static bool StageInterstitialDue(int lifetimeClears, bool vip, bool enabled, bool rewardSkip, double now, double lastAnyAd)
        {
            if (InterstitialBlocked(lifetimeClears, vip, enabled, rewardSkip, now, lastAnyAd)) return false;
            return CadenceHit(lifetimeClears);
        }

        public static bool PokerInterstitialDue(int lifetimeClears, int hands, bool vip, bool enabled, double now, double lastAnyAd)
        {
            if (hands < PokerHandCadence) return false;
            return !InterstitialBlocked(lifetimeClears, vip, enabled, false, now, lastAnyAd);
        }

        // Not for VIP owners. A different calendar day may ask again.
        public static bool VipSoftDue(bool owned, int today, int savedDay)
        {
            if (owned || today <= 0) return false;
            return savedDay != today;
        }

        public static void NoteAnyAd()
        {
            double now = AdClock;
            if (now < 0) now = 0;
            _lastAnyAd = now;
            PlayerPrefs.SetString(PrefLastAd, now.ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        public static void NoteInterstitialDisplayed()
        {
            _intDisplayed = true;
            NoteAnyAd();
        }

        static void LoadLastAd()
        {
            string raw = PlayerPrefs.GetString(PrefLastAd, "");
            double v;
            if (!string.IsNullOrEmpty(raw)
                && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out v)
                && v > 0)
                _lastAnyAd = v;
            else
                _lastAnyAd = 0;
        }

        // True once, when a shown interstitial may ask. Marks the day.
        public static bool TryConsumeVipSoft()
        {
            if (!VipSoftPending) return false;
            VipSoftPending = false;
            int today = SoftDay;
            if (!VipSoftDue(NoAds.Owned, today, PlayerPrefs.GetInt(PrefVipSoft, 0))) return false;
            PlayerPrefs.SetInt(PrefVipSoft, today);
            PlayerPrefs.Save();
            return true;
        }

        public static IEnumerator Interstitial()
        {
            SessionClears++;
            PlayerPrefs.SetInt(PrefClears, SessionClears);
            PlayerPrefs.Save();
            bool skip = TakeClearSkip();
            bool vip = NoAds.Owned;
            bool due = StageInterstitialDue(SessionClears, vip, Enabled, skip, AdClock, LastAnyAd);
            AdLog.Add("clear #" + SessionClears + (due ? " -> ad turn" : " -> no ad turn")
                + (!Enabled ? " (ads off)" : "")
                + (vip ? " (No Ads owned)" : "")
                + (skip ? " (2x watched)" : "")
                + (GraceBlocks(SessionClears) ? " (grace)" : "")
                + (Cooling(AdClock, LastAnyAd) ? " (cooldown)" : ""));
            if (!due) yield break;
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
            bool allow = PokerInterstitialDue(SessionClears, n, owned, Enabled, AdClock, LastAnyAd);
            AdLog.Add("poker hands " + n
                + (allow ? " -> show" : " -> skipped")
                + (!Enabled ? " (ads off)" : "")
                + (owned ? " (No Ads owned)" : "")
                + (GraceBlocks(SessionClears) ? " (grace)" : "")
                + (Cooling(AdClock, LastAnyAd) ? " (cooldown)" : ""));
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

        public static IEnumerator Rewarded() => Rewarded(PlacementBonus, 0, false);

        public static IEnumerator Rewarded(string placement) => Rewarded(placement, 0, false);

        // payload is the branch index, the wheel's 1x coins, or the streak to keep.
        // tutorialGoal finishes the glove step when this reward lands.
        public static IEnumerator Rewarded(string placement, int payload, bool tutorialGoal)
        {
            ActivePlacement = string.IsNullOrEmpty(placement) ? PlacementBonus : placement;
            AdLog.Add("gift ad requested (" + ActivePlacement + ")");
            LastGranted = false;
            LastEarned = false;
            if (!Enabled)
            {
                LastGranted = true;
                yield return new WaitForSeconds(0.35f);
                yield break;
            }
            RewardedGrant.Begin(PlaceFor(ActivePlacement), payload, tutorialGoal, LevelData.DisplayNumber);
            Ensure();
            if (_host == null)
            {
#if FF_ADS_DEV
                yield return Ads.Simulate();
                DeliverDisplayedCallback();
                DeliverRewardedCallback();
#else
                DeliverFailedCallback();
#endif
                LastEarned = RewardedGrant.Earned;
                LastGranted = RewardedGrant.Earned;
                RewardedGrant.Flush();
                yield break;
            }
            yield return _host.RunRewarded();
            LastEarned = RewardedGrant.Earned;
            LastGranted = RewardedGrant.Earned;
            RewardedGrant.Flush();
        }

        public static RewardedGrant.Place PlaceFor(string placement)
        {
            if (placement == PlacementDailyDouble) return RewardedGrant.Place.Wheel;
            if (placement == PlacementKeep) return RewardedGrant.Place.Keep;
            if (placement == PlacementClearDouble) return RewardedGrant.Place.Clear;
            if (placement == PlacementRewardRepeat) return RewardedGrant.Place.Repeat;
            return RewardedGrant.Place.Branch;
        }

        // LevelPlay OnAdRewarded. Safe before or after OnAdClosed, and while paused.
        public static void DeliverRewardedCallback()
        {
            bool newly = RewardedGrant.OnRewarded();
            if (RewardedGrant.Earned)
            {
                LastEarned = true;
                LastGranted = true;
            }
            AdLog.Add(newly ? "gift ad reward callback (earned)" : "gift ad reward callback (duplicate)");
            AudioGuard.Restore();
        }

        // LevelPlay OnAdClosed. Does not decide the grant.
        public static void DeliverClosedCallback()
        {
            RewardedGrant.OnClosed();
            AdLog.Add("gift ad closed" + (RewardedGrant.Earned ? " (rewarded)" : " (no reward yet)"));
            AudioGuard.Restore();
        }

        // Interstitial close and display failure. Same audio undo as a rewarded close.
        // A real on-screen close (not a load or display failure) may ask Go VIP once.
        public static void DeliverInterstitialClosed()
        {
            AudioGuard.Restore();
            bool shown = _intDisplayed;
            _intDisplayed = false;
            if (!shown || NoAds.Owned) return;
            VipSoftPending = true;
        }

        public static void DeliverDisplayedCallback()
        {
            RewardedGrant.OnDisplayed();
            NoteAnyAd();
        }

        // No-fill or a display error. Pays nothing.
        public static void DeliverFailedCallback()
        {
            RewardedGrant.OnFailed();
            if (!RewardedGrant.Earned)
            {
                LastEarned = false;
                LastGranted = false;
            }
            AdLog.Add("gift ad failed");
            AudioGuard.Restore();
        }

        public static void DeliverPause(bool paused)
        {
            RewardedGrant.OnPause(paused);
        }

        public static void DeliverFocus(bool focus)
        {
            RewardedGrant.OnFocus(focus);
        }

#if FF_ADS_DEV
        internal static IEnumerator Simulate()
        {
            try
            {
                Present(() => { });
                float t = 0f;
                while (t < 2.15f)
                {
                    t += ForegroundDt();
                    yield return null;
                }
            }
            finally
            {
                EndShow();
            }
        }
#endif

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
            keep.gameObject.name = "Ads";
            keep.gameObject.SetActive(true);
            _host = keep;
            if (keep != null) AdConsent.Begin(keep);
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

        public bool AdReady()
        {
            try
            {
                return _inited && _rv != null && _rv.IsAdReady();
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public void Boot()
        {
            if (!Ads.HasKeys) return;
            var trace = StartupTrace.Begin("Ads.Boot");
            try
            {
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
#if FF_ADS_DEV
                LevelPlay.SetMetaData("is_test_suite", "enable");
#endif
                AdTestDevices.Resolve(this, InitLevelPlay);
            }
            catch (System.Exception e)
            {
                _initFailed = true;
                Debug.LogWarning("LevelPlay init skipped: " + e.Message);
            }
            finally
            {
                trace.Dispose();
                // Init returns on this thread and the Unity Ads / IronSource
                // binaries may already have changed the session. Put it back
                // before the next frame plays. The async success callback
                // reasserts again after the ad units load.
                AudioGuard.Reassert();
            }
        }

        // Runs once the advertising ID is known (or timed out), so an
        // allowlisted test device can turn on test mode before init.
        void InitLevelPlay(string adId)
        {
            try
            {
                AdTestDevices.Apply(adId);
                using (StartupTrace.Begin("LevelPlay.Init"))
                    LevelPlay.Init(Ads.AppKey);
            }
            catch (System.Exception e)
            {
                _initFailed = true;
                Debug.LogWarning("LevelPlay init skipped: " + e.Message);
            }
            finally
            {
                AudioGuard.Reassert();
            }
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
#if FF_ADS_DEV
                    yield return Ads.Simulate();
                    Ads.DeliverDisplayedCallback();
                    Ads.DeliverRewardedCallback();
#else
                    Ads.DeliverFailedCallback();
#endif
                    yield break;
                }

                Ads.NoteLoad(true);
                loading = true;
                float t = 0f;
                while (!_inited && !_initFailed && t < 5f)
                {
                    t += Ads.ForegroundDt();
                    yield return null;
                }

                if (!_inited)
                {
                    AdLog.Add("gift ad unavailable (ads not ready)");
                    Ads.DeliverFailedCallback();
                    yield break;
                }

                if (_rv == null) CreateRewarded();
                if (_rv == null)
                {
                    AdLog.Add("gift ad unavailable (no ad unit)");
                    Ads.DeliverFailedCallback();
                    yield break;
                }

                if (!_rv.IsAdReady())
                {
                    _rv.LoadAd();
                    AfterSdk();
                    t = 0f;
                    while (!_rv.IsAdReady() && t < 6f)
                    {
                        t += Ads.ForegroundDt();
                        yield return null;
                    }
                }

                if (!_rv.IsAdReady())
                {
                    AdLog.Add("gift ad did not load");
                    Ads.DeliverFailedCallback();
                    _rv.LoadAd();
                    AfterSdk();
                    yield break;
                }

                Ads.NoteLoad(false);
                loading = false;
                _waiting = true;
                _shown = false;
                _didReward = false;
                Ads.LastGranted = false;
                showing = true;
                AdLog.Add("gift ad show called");
                Ads.Present(() => _rv.ShowAd(Ads.ActivePlacement));
                AfterSdk();
                t = 0f;
                while (_waiting && t < 180f)
                {
                    t += Ads.ForegroundDt();
                    yield return null;
                }
                if (_waiting) AdLog.Add("gift ad wait TIMED OUT after 180s; game resumed");
                // The reward callback grants. Close, resume, and this wait ending do not.
                // A show that never appeared is a failure and pays nothing.
                if (!RewardedGrant.Earned && !_shown) Ads.DeliverFailedCallback();
                Ads.LastEarned = RewardedGrant.Earned;
                Ads.LastGranted = RewardedGrant.Earned;
                _rv.LoadAd();
                AfterSdk();
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
            try
            {
                using (StartupTrace.Begin("Ads.CreateUnits"))
                {
                    CreateRewarded();
                    CreateInterstitial();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("LevelPlay ads skipped: " + e.Message);
            }
            AfterSdk();
        }

        void OnInitFail(LevelPlayInitError error)
        {
            _initFailed = true;
            AfterSdk();
        }

        // Category and full listener gain after the SDK touches the session.
        void AfterSdk() => AudioGuard.Reassert();

        void CreateRewarded()
        {
            if (_rv != null || string.IsNullOrEmpty(Ads.RewardedUnitId)) return;
            LevelPlayRewardedAd created = null;
            try
            {
                created = new LevelPlayRewardedAd(Ads.RewardedUnitId);
                if (created == null) return;
                created.OnAdRewarded += OnRewarded;
                created.OnAdClosed += OnClosed;
                created.OnAdLoadFailed += OnLoadFail;
                created.OnAdDisplayFailed += OnDisplayFail;
                created.OnAdDisplayed += OnRvShown;
                created.LoadAd();
                AfterSdk();
                _rv = created;
            }
            catch (System.Exception e)
            {
                if (created != null)
                {
                    created.OnAdRewarded -= OnRewarded;
                    created.OnAdClosed -= OnClosed;
                    created.OnAdLoadFailed -= OnLoadFail;
                    created.OnAdDisplayFailed -= OnDisplayFail;
                    created.OnAdDisplayed -= OnRvShown;
                }
                Debug.LogWarning("LevelPlay rewarded skipped: " + e.Message);
            }
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
                    t += Ads.ForegroundDt();
                    yield return null;
                }
                if (!_inited) { AdLog.Add("interstitial skipped: SDK not ready"); yield break; }
                if (_int == null) CreateInterstitial();
                if (_int == null) yield break;
                if (!_int.IsAdReady()) { AdLog.Add("interstitial skipped: no ad loaded"); yield break; }
                Ads.NoteLoad(false);
                loading = false;
                _intWaiting = true;
                showing = true;
                AdLog.Add("interstitial show called");
                Ads.Present(() => _int.ShowAd(Ads.PlacementClear));
                AfterSdk();
                t = 0f;
                while (_intWaiting && t < 180f)
                {
                    t += Ads.ForegroundDt();
                    yield return null;
                }
                if (_intWaiting) AdLog.Add("interstitial wait TIMED OUT after 180s; game resumed");
                _int.LoadAd();
                AfterSdk();
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
            LevelPlayInterstitialAd created = null;
            try
            {
                created = new LevelPlayInterstitialAd(Ads.InterstitialUnitId);
                if (created == null) return;
                created.OnAdClosed += OnIntClosed;
                created.OnAdLoadFailed += OnIntLoadFail;
                created.OnAdDisplayFailed += OnIntDisplayFail;
                created.OnAdDisplayed += OnIntShown;
                created.LoadAd();
                AfterSdk();
                _int = created;
            }
            catch (System.Exception e)
            {
                if (created != null)
                {
                    created.OnAdClosed -= OnIntClosed;
                    created.OnAdLoadFailed -= OnIntLoadFail;
                    created.OnAdDisplayFailed -= OnIntDisplayFail;
                    created.OnAdDisplayed -= OnIntShown;
                }
                Debug.LogWarning("LevelPlay interstitial skipped: " + e.Message);
            }
        }

        void OnIntShown(LevelPlayAdInfo info)
        {
            bool owned = _intWaiting;
            Ads.NoteInterstitialDisplayed();
            AdLog.Add("INTERSTITIAL ON SCREEN" + (owned ? "" : " (NOT requested by game!)"));
            if (owned || _orphanInt) return;
            _orphanInt = true;
            Ads.BeginShow();
        }

        void OnIntClosed(LevelPlayAdInfo info)
        {
            AdLog.Add("interstitial closed");
            _intWaiting = false;
            Ads.DeliverInterstitialClosed();
            if (!_orphanInt) return;
            _orphanInt = false;
            Ads.EndShow();
        }

        void OnIntLoadFail(LevelPlayAdError error)
        {
            if (_intWaiting) AdLog.Add("interstitial load failed while waiting");
            bool waiting = _intWaiting;
            _intWaiting = false;
            if (waiting) Ads.DeliverInterstitialClosed();
        }

        void OnIntDisplayFail(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            AdLog.Add("interstitial display failed");
            _intWaiting = false;
            Ads.DeliverInterstitialClosed();
        }

        void OnRvShown(LevelPlayAdInfo info)
        {
            _shown = true;
            Ads.DeliverDisplayedCallback();
            bool owned = _waiting;
            AdLog.Add("GIFT AD ON SCREEN" + (owned ? "" : " (NOT requested by game!)"));
            if (owned || _orphanRv) return;
            _orphanRv = true;
            Ads.BeginShow();
        }

        void OnRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            _didReward = true;
            Ads.DeliverRewardedCallback();
        }

        void OnClosed(LevelPlayAdInfo info)
        {
            Ads.DeliverClosedCallback();
            _waiting = false;
            if (!_orphanRv) return;
            _orphanRv = false;
            Ads.EndShow();
        }

        void OnLoadFail(LevelPlayAdError error)
        {
            if (_waiting && !_shown) Ads.DeliverFailedCallback();
            _waiting = false;
        }

        // One exit for a show the player left. The coroutine that called ShowAd
        // still writes LastGranted once. Orphan shows have no coroutine, so
        // they end here. A second call does not pop the pause again.
        internal void ReleaseAfterResume()
        {
            bool reward = _waiting;
            bool inter = _intWaiting;
            _waiting = false;
            _intWaiting = false;
            if (_orphanRv)
            {
                _orphanRv = false;
                Ads.EndShow();
            }
            if (_orphanInt)
            {
                _orphanInt = false;
                Ads.EndShow();
            }
            if (!reward && !inter && Ads.IsShowing)
                Ads.ForceClear();
        }

        void OnDisplayFail(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Ads.DeliverFailedCallback();
            _waiting = false;
        }

        bool _leftApp;

        void OnApplicationPause(bool paused)
        {
            AdPause.OnAppPause(paused);
            AdLog.Add(paused ? "app left foreground" : "app back in foreground");
            Ads.DeliverPause(paused);
            if (paused)
            {
                _leftApp = true;
                AdLog.Flush();
                return;
            }
            // The ad activity took the foreground. Coming back usually means it is
            // gone, even when the close callback never arrived. JustShown still
            // ignores the flicker some SDKs emit in the first moments of ShowAd.
            // ArmResumeSettle waits one beat so a late reward is not dropped.
            if (_leftApp && Ads.IsShowing)
                Ads.ArmResumeSettle();
            _leftApp = false;
            AudioGuard.Restore();
        }

        void OnApplicationFocus(bool focus)
        {
            Ads.DeliverFocus(focus);
            if (focus) AudioGuard.Restore();
            if (!focus || !_leftApp || !Ads.IsShowing) return;
            Ads.ArmResumeSettle();
            _leftApp = false;
        }

        void Update()
        {
            AdLog.Flush();
            Ads.TickShowClock();
            if (Ads.IsShowing && Ads.ShownFor > 185f)
            {
                ReleaseAfterResume();
                Ads.ForceClear();
            }
#if FF_ADS_DEV
            DevGestures();
#endif
        }

        // ATT completion from FlockFiveTracking.mm (UnitySendMessage "Ads").
        public void OnAttComplete(string status)
        {
            try { Tracking.NoteComplete(status); }
            catch (System.Exception e)
            {
                Debug.LogWarning("ATT callback skipped: " + e.Message);
            }
            try { AdConsent.NoteAttAnswered(); }
            catch (System.Exception e)
            {
                Debug.LogWarning("ATT consent update skipped: " + e.Message);
            }
        }

        void OnGUI()
        {
            AdConsent.DrawPrompt();
#if FF_ADS_DEV
            DrawDevLog();
#endif
        }

#if FF_ADS_DEV
        float _suiteHold, _logHold;
        bool _showLog, _logArm;
        Vector2 _logScroll;

        void DevGestures()
        {
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

        void DrawDevLog()
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
#endif

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


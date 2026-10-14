using System;
using System.Globalization;
using UnityEngine;

namespace FlockFive
{
    // One place that puts the mix back after an ad, a permission dialog, or an
    // iOS audio-session interruption.
    //
    // Nothing in the game writes AudioListener.volume. GamePause is the only
    // writer of AudioListener.pause, and Pop puts back whatever Push saved. A
    // system dialog (ATT) or the LevelPlay / Unity Ads SDK can pause the
    // listener and leave that saved bit true, and the SDK also reconfigures
    // AVAudioSession (category, DuckOthers, setActive:NO). The ear hears that
    // as a volume that drops and never returns. Restore() is the undo.
    //
    // ProjectSettings muteOtherAudioSources is 0 and Prepare IOS For Recording
    // is 0, so the session is AVAudioSessionCategoryAmbient: the silent switch
    // wins, and the category options stay empty (no DuckOthers).
    public static class AudioGuard
    {
        public const float DuckTimeoutSec = 10f;
        // Any live token. One level, not a stack, so two popups cannot multiply.
        public const float DuckGain = 0.25f;
        // 0 = Ambient. 1 = SoloAmbient. Both honor the silent switch.
        public const int SessionSolo = 0;

        // No settings screen writes these. They are the user mix Restore returns to.
        public static bool MusicEnabled = true;
        public static bool SfxEnabled = true;
        public static float MusicVolume = 1f;
        public static float SfxVolume = 1f;

        // Tests advance this instead of waiting out DuckTimeoutSec.
        public static Func<double> Clock;

        public static int ActivateCalls { get; private set; }
        public static int LiveTokens => _n;

        struct Tok
        {
            public int Id;
            public string Reason;
            public double Until;
            public bool Live;
        }

        const int MaxTok = 24;
        static readonly Tok[] _tok = new Tok[MaxTok];
        static int _n;
        static int _seq;

        public static float TokenScale => _n > 0 ? DuckGain : 1f;

        public static float MusicScale
        {
            get
            {
                float user = MusicEnabled ? MusicVolume : 0f;
                if (user < 0f) user = 0f;
                if (user > 1f) user = 1f;
                return user * TokenScale;
            }
        }

        public static float SfxScale
        {
            get
            {
                float user = SfxEnabled ? SfxVolume : 0f;
                if (user < 0f) user = 0f;
                if (user > 1f) user = 1f;
                return user * TokenScale;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _n = 0;
            _seq = 0;
            MusicEnabled = true;
            SfxEnabled = true;
            MusicVolume = 1f;
            SfxVolume = 1f;
            Clock = null;
            ActivateCalls = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            LoadMuted();
            EnsureHost();
            InstallNative();
        }

        // b87: one shared mute for the home speaker button and anything else that
        // toggles audio. Muted silences all music and SFX and is persisted.
        public const string MuteKey = "flockfive.audio.muted";
        public static bool Muted => !MusicEnabled && !SfxEnabled;
        public static event Action<bool> MutedChanged;

        public static void LoadMuted()
        {
            bool m = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            MusicEnabled = !m;
            SfxEnabled = !m;
        }

        public static void SetMuted(bool m, bool persist = true)
        {
            bool was = Muted;
            MusicEnabled = !m;
            SfxEnabled = !m;
            if (persist) { PlayerPrefs.SetInt(MuteKey, m ? 1 : 0); PlayerPrefs.Save(); }
            if (was != m) MutedChanged?.Invoke(m);
        }

        public static void ToggleMuted() => SetMuted(!Muted);

        public static void ResetForTests()
        {
            _n = 0;
            _seq = 0;
            MusicEnabled = true;
            SfxEnabled = true;
            MusicVolume = 1f;
            SfxVolume = 1f;
            Clock = null;
            ActivateCalls = 0;
        }

        static double Now()
        {
            if (Clock != null) return Clock();
            return Time.unscaledTimeAsDouble;
        }

        // One duck. A second Hold with the same reason refreshes the timeout
        // instead of stacking. Tick drops it after DuckTimeoutSec. Restore drops
        // it immediately. Scene and popup close drop their own reasons.
        public static int PushDuck(string reason)
        {
            if (string.IsNullOrEmpty(reason)) reason = "popup";
            if (_n >= MaxTok) DropOldest();
            int id = ++_seq;
            if (id < 1) id = _seq = 1;
            _tok[_n++] = new Tok
            {
                Id = id,
                Reason = reason,
                Until = Now() + DuckTimeoutSec,
                Live = true
            };
            return id;
        }

        public static void Hold(string reason)
        {
            if (string.IsNullOrEmpty(reason)) reason = "popup";
            double until = Now() + DuckTimeoutSec;
            for (int i = 0; i < _n; i++)
            {
                if (!_tok[i].Live || _tok[i].Reason != reason) continue;
                var t = _tok[i];
                t.Until = until;
                _tok[i] = t;
                return;
            }
            PushDuck(reason);
        }

        public static void Release(int id)
        {
            if (id <= 0) return;
            bool any = false;
            for (int i = 0; i < _n; i++)
            {
                if (_tok[i].Id != id || !_tok[i].Live) continue;
                var t = _tok[i];
                t.Live = false;
                _tok[i] = t;
                any = true;
            }
            if (any) Compact();
        }

        public static void ReleaseReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return;
            bool any = false;
            for (int i = 0; i < _n; i++)
            {
                if (!_tok[i].Live || _tok[i].Reason != reason) continue;
                var t = _tok[i];
                t.Live = false;
                _tok[i] = t;
                any = true;
            }
            if (any) Compact();
        }

        public static void Tick()
        {
            if (_n == 0) return;
            double now = Now();
            bool any = false;
            for (int i = 0; i < _n; i++)
            {
                if (!_tok[i].Live || _tok[i].Until > now) continue;
                var t = _tok[i];
                t.Live = false;
                _tok[i] = t;
                any = true;
            }
            if (any) Compact();
        }

        static void DropOldest()
        {
            if (_n <= 0) return;
            var t = _tok[0];
            t.Live = false;
            _tok[0] = t;
            Compact();
        }

        static void Compact()
        {
            int w = 0;
            for (int i = 0; i < _n; i++)
            {
                if (!_tok[i].Live) continue;
                _tok[w++] = _tok[i];
            }
            _n = w;
        }

        // Full undo. Clears every duck token, puts the bed back on the user
        // mix, unsticks the listener, and reactivates the game session.
        public static void Restore()
        {
            _n = 0;
            if (MixDesk.Live != null) MixDesk.Live.ResetToUser();
            GamePause.UnstickAudio();
            ActivateNative();
        }

        // Session and listener gain only. An ad that is still up keeps the
        // pause GamePause pushed; the category still comes back to Ambient.
        public static void Reassert()
        {
            AudioListener.volume = 1f;
            if (!GamePause.Paused)
                AudioListener.pause = false;
            ActivateNative();
        }

        // Popup and scene close. Drops dialog ducks. Leaves an in-flight ad
        // pause alone. The session is healed by Restore on the dialog's own
        // completion, not on every level load.
        public static void OnSceneOrPopupClosed()
        {
            ReleaseReason("settings");
            ReleaseReason("sheet");
            ReleaseReason("gdpr");
            ReleaseReason("att");
            ReleaseReason("att-system");
            ReleaseReason("popup");
            if (!Ads.IsShowing) ReleaseReason("ad");
            AudioListener.volume = 1f;
            if (!GamePause.Paused)
                AudioListener.pause = false;
        }

        // UnitySendMessage from FlockFiveTracking.mm. "began" is ignored.
        // Interruption end always restores, even when ShouldResume is absent.
        public static void OnAudioInterruption(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return;
            if (kind.IndexOf("ended", StringComparison.Ordinal) < 0) return;
            Restore();
        }

        public static string Describe()
        {
            var culture = CultureInfo.InvariantCulture;
            float vol = 0f;
            int pause = 0;
            int dsp = 0;
            int rate = 0;
            int spk = 0;
            bool ok = false;
            try
            {
                vol = AudioListener.volume;
                pause = AudioListener.pause ? 1 : 0;
                var c = AudioSettings.GetConfiguration();
                dsp = c.dspBufferSize;
                rate = c.sampleRate;
                spk = (int)c.speakerMode;
                ok = true;
            }
            catch (Exception) { }
            float freeze = MixDesk.Live != null ? MixDesk.Live.FreezeDuck : -1f;
            string freezeText = freeze < 0f ? "na" : freeze.ToString("0.00", culture);
            if (!ok) return "unread";
            return "vol=" + vol.ToString("0.00", culture)
                + " pause=" + pause.ToString(culture)
                + " duck=" + TokenScale.ToString("0.00", culture)
                + " tokens=" + _n.ToString(culture)
                + " freeze=" + freezeText
                + " dsp=" + dsp.ToString(culture)
                + " rate=" + rate.ToString(culture)
                + " spk=" + spk.ToString(culture);
        }

        static void EnsureHost()
        {
            AudioGuardHost host = null;
            var found = UnityEngine.Object.FindObjectsByType<AudioGuardHost>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found != null)
            {
                for (int i = 0; i < found.Length; i++)
                {
                    if (found[i] == null) continue;
                    if (host == null) host = found[i];
                    else UnityEngine.Object.Destroy(found[i].gameObject);
                }
            }
            if (host == null)
            {
                var go = new GameObject("AudioGuard");
                UnityEngine.Object.DontDestroyOnLoad(go);
                host = go.AddComponent<AudioGuardHost>();
            }
            else
                UnityEngine.Object.DontDestroyOnLoad(host.gameObject);
            host.gameObject.name = "AudioGuard";
        }

        static void InstallNative()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { FlockFive_AudioInstall(); }
            catch (Exception e) { Debug.LogWarning("audio session observer skipped: " + e.Message); }
#endif
        }

        static void ActivateNative()
        {
            ActivateCalls++;
#if UNITY_IOS && !UNITY_EDITOR
            try { FlockFive_AudioActivate(SessionSolo); }
            catch (Exception e) { Debug.LogWarning("audio session activate skipped: " + e.Message); }
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void FlockFive_AudioInstall();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void FlockFive_AudioActivate(int solo);
#endif
    }

    // UnitySendMessage target. The GameObject name is AudioGuard.
    public sealed class AudioGuardHost : MonoBehaviour
    {
        public void OnAudioInterruption(string kind) => AudioGuard.OnAudioInterruption(kind);

        void Update() => AudioGuard.Tick();
    }
}

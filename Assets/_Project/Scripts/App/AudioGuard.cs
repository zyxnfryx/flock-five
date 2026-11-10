using System;
using System.Collections.Generic;
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

        // MusicVolume and SfxVolume stay at 1 unless a test says otherwise.
        // Settings writes MasterVolume (the slider) and the shared mute.
        // Restore returns to that mix. Nothing here writes AudioListener.volume.
        public static bool MusicEnabled = true;
        public static bool SfxEnabled = true;
        public static float MusicVolume = 1f;
        public static float SfxVolume = 1f;
        // Settings slider, 0..1. Mute does not write this, so Sound On puts it back.
        public static float MasterVolume = 1f;

        // Tests advance this instead of waiting out DuckTimeoutSec.
        public static Func<double> Clock;

        public static int ActivateCalls { get; private set; }
        public static int LiveTokens => _n;
        public static int FmodWarnings { get; private set; }
        public static bool OutputFailed => _fmodNoted;

        public static int PlaybackWarnings { get; private set; }
        public static int RetryPlayCount { get; private set; }
        public static int PauseRoutes { get; private set; }
        public static int ResumeRoutes { get; private set; }
        public static bool RetryDeferred => _retryDeferred;
        public static int SkippedCount => _skipped.Count;
        public static int RestoreCount => _restore.Count;
        public static int PlayCalls { get; private set; }
        public static int StopCalls { get; private set; }

        // Test seam: the next PlayScheduled on this source returns false once.
        public static AudioSource RejectScheduled;

        static readonly List<AudioSource> _skipped = new List<AudioSource>(8);
        static readonly List<AudioSource> _restore = new List<AudioSource>(8);
        static readonly List<AudioSource> _started = new List<AudioSource>(8);
        static readonly List<int> _paused = new List<int>(8);
        static readonly List<int> _retried = new List<int>(8);
        static readonly List<int> _schedIds = new List<int>(8);
        static readonly List<double> _schedAt = new List<double>(8);
        static readonly List<string> _logged = new List<string>(8);
        static bool _retryDeferred;
        static bool _reassertPending;

        // Playback after a failed output init. Never throws: a destroyed source
        // or a clip error logs once and returns false so Update keeps going.
        public static bool Play(AudioSource source)
        {
            if (!Alive(source))
            {
                NoteDestroyed(source);
                return false;
            }
            if (OutputFailed)
            {
                RememberSkip(source);
                if (source.loop) HoldRestore(source);
                return false;
            }
            try
            {
                PlayCalls++;
                source.Play();
                Release(source);
                if (source.loop) NoteStarted(source);
                return true;
            }
            catch (Exception ex)
            {
                NotePlayback(source, ex);
                return false;
            }
        }

        // Same gate as Play. A failed output remembers the source; a later reset retries only that set.
        public static bool PlayScheduled(AudioSource source, double time)
        {
            if (!Alive(source))
            {
                NoteDestroyed(source);
                return false;
            }
            if (!ReferenceEquals(RejectScheduled, null) && RejectScheduled == source)
            {
                RejectScheduled = null;
                return false;
            }
            if (OutputFailed)
            {
                RememberSkip(source);
                if (source.loop) HoldRestore(source);
                return false;
            }
            try
            {
                source.PlayScheduled(time);
                Release(source);
                if (source.loop) NoteStarted(source);
                return true;
            }
            catch (Exception ex)
            {
                NotePlayback(source, ex);
                return false;
            }
        }

        // One dspTime for every stem in the batch, so the layers stay sample-locked.
        // Each voice still goes through PlayScheduled: catch, log once, and hold
        // the bed again if the output is still dead.
        public static int ScheduleStemsTogether(IEnumerable<AudioSource> sources)
        {
            if (sources == null) return 0;
            var batch = new List<AudioSource>();
            foreach (var src in sources)
            {
                if (Alive(src)) batch.Add(src);
            }
            if (batch.Count == 0) return 0;
            for (int i = 0; i < batch.Count; i++)
            {
                try { batch[i].timeSamples = 0; }
                catch (Exception ex) { NotePlayback(batch[i], ex); }
            }
            double start = AudioSettings.dspTime + 0.05;
            int n = 0;
            for (int i = 0; i < batch.Count; i++)
            {
                if (!PlayScheduled(batch[i], start)) continue;
                RememberSchedule(batch[i], start);
                n++;
            }
            return n;
        }

        public static bool ScheduledAt(AudioSource source, out double start)
        {
            start = 0;
            int id = Identity(source);
            if (id == 0) return false;
            for (int i = _schedIds.Count - 1; i >= 0; i--)
            {
                if (_schedIds[i] != id) continue;
                start = _schedAt[i];
                return true;
            }
            return false;
        }

        static void DropSchedule(int id)
        {
            if (id == 0) return;
            for (int i = _schedIds.Count - 1; i >= 0; i--)
            {
                if (_schedIds[i] != id) continue;
                _schedIds.RemoveAt(i);
                if (i < _schedAt.Count) _schedAt.RemoveAt(i);
            }
        }

        static void RememberSchedule(AudioSource source, double start)
        {
            int id = Identity(source);
            if (id == 0) return;
            DropSchedule(id);
            _schedIds.Add(id);
            _schedAt.Add(start);
            for (int i = 0; i < _retried.Count; i++)
                if (_retried[i] == id) return;
            _retried.Add(id);
        }

        // Stop, cut, swap, or a place change. The source is not started again.
        public static void Release(AudioSource source)
        {
            if (ReferenceEquals(source, null)) return;
            int id = Identity(source);
            DropFrom(_skipped, source, id);
            DropFrom(_restore, source, id);
            DropFrom(_started, source, id);
            DropSchedule(id);
        }

        public static void HoldRestore(AudioSource source)
        {
            if (!Alive(source) || !source.loop) return;
            if (!Listed(_restore, source)) _restore.Add(source);
        }

        public static void DropRestore(AudioSource source)
        {
            if (ReferenceEquals(source, null)) return;
            int id = Identity(source);
            DropFrom(_restore, source, id);
            DropFrom(_started, source, id);
            DropSchedule(id);
        }

        public static bool NeedsRestore(AudioSource source) => Listed(_restore, source);

        public static bool WasStarted(AudioSource source) => Listed(_started, source);

        public static void NoteStarted(AudioSource source)
        {
            if (!Alive(source) || !source.loop) return;
            if (!Listed(_started, source)) _started.Add(source);
        }

        public static void ForgetStarted(AudioSource source)
        {
            if (ReferenceEquals(source, null)) return;
            DropFrom(_started, source, Identity(source));
        }

        public static void NoteStopped()
        {
            StopCalls++;
        }

        // Game pause. One path: mark the sources that are actually speaking, then
        // GamePause freezes the listener. A tutor bed that ignores the listener stays up.
        public static void Pause()
        {
            PauseRoutes++;
            if (MixDesk.Live != null) MixDesk.Live.PauseForGame();
            Sfx.PauseForGame();
        }

        // Resume clears the pause set and plays a retry that waited out the pause.
        public static void Resume()
        {
            ResumeRoutes++;
            if (MixDesk.Live != null) MixDesk.Live.ResumeForGame();
            Sfx.ResumeForGame();
            _paused.Clear();
            if (_reassertPending && !Muted && !GamePause.Paused)
            {
                _reassertPending = false;
                if (MixDesk.Live != null) MixDesk.Live.ReassertBed();
            }
            if (_retryDeferred && !Muted && !GamePause.Paused)
            {
                _retryDeferred = false;
                RetrySkipped();
            }
        }

        public static bool IsPaused(AudioSource source) => HeldPaused(source);

        // A single source, paused on purpose. Recovery must not Play() it back to 0.
        public static void Pause(AudioSource source)
        {
            if (!Alive(source)) return;
            int id = Identity(source);
            if (id != 0)
            {
                bool seen = false;
                for (int i = 0; i < _paused.Count; i++)
                    if (_paused[i] == id) { seen = true; break; }
                if (!seen) _paused.Add(id);
            }
            Release(source);
            try { source.Pause(); }
            catch (Exception ex) { NotePlayback(source, ex); }
        }

        public static void Resume(AudioSource source)
        {
            if (!Alive(source)) return;
            int id = Identity(source);
            if (id != 0)
            {
                for (int i = _paused.Count - 1; i >= 0; i--)
                    if (_paused[i] == id) _paused.RemoveAt(i);
            }
            try { source.UnPause(); }
            catch (Exception ex) { NotePlayback(source, ex); }
        }

        public static bool WasRetried(AudioSource source)
        {
            int id = Identity(source);
            if (id == 0) return false;
            for (int i = 0; i < _retried.Count; i++)
                if (_retried[i] == id) return true;
            return false;
        }

        public static void NoteClipFailure(AudioClip clip, Exception ex)
        {
            string name = clip != null && !string.IsNullOrEmpty(clip.name) ? clip.name : "clip";
            if (ex != null && NoteFmodFailure(ex.Message)) return;
            LogOnce("clip:" + name, "AudioGuard skipped " + name + ".");
        }

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
        static bool _fmodNoted;
        static bool _fmodHooked;
        static bool _configHooked;
        static int _fmodFrame = -1;
        static int _otherErrorFrame = -1;
        static bool _consoleOwnedByError;

        public static float TokenScale => _n > 0 ? DuckGain : 1f;

        public static float MusicScale
        {
            get
            {
                float user = MusicEnabled ? MusicVolume : 0f;
                if (user < 0f) user = 0f;
                if (user > 1f) user = 1f;
                // MasterGain is the slider, times a duck, and 0 while an ad is up.
                return user * MixDesk.MasterGain;
            }
        }

        public static float SfxScale
        {
            get
            {
                float user = SfxEnabled ? SfxVolume : 0f;
                if (user < 0f) user = 0f;
                if (user > 1f) user = 1f;
                return user * MixDesk.MasterGain;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            UnhookOutputRecovery();
            _n = 0;
            _seq = 0;
            MusicEnabled = true;
            SfxEnabled = true;
            MusicVolume = 1f;
            SfxVolume = 1f;
            MasterVolume = 1f;
            Clock = null;
            ActivateCalls = 0;
            FmodWarnings = 0;
            _fmodNoted = false;
            _fmodFrame = -1;
            _otherErrorFrame = -1;
            _consoleOwnedByError = false;
            ClearSkipBook();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            LoadMuted();
            LoadVolume();
            EnsureHost();
            InstallNative();
            HookOutputRecovery();
        }

        // Log hook plus the route-change callback. Idempotent. OnDisable and domain
        // reload both call Unhook so a destroyed host does not keep the delegate.
        public static void HookOutputRecovery()
        {
            if (!_fmodHooked)
            {
                Application.logMessageReceived += OnAudioLog;
                _fmodHooked = true;
            }
            if (!_configHooked)
            {
                AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
                _configHooked = true;
            }
        }

        public static void UnhookOutputRecovery()
        {
            if (_fmodHooked)
            {
                Application.logMessageReceived -= OnAudioLog;
                _fmodHooked = false;
            }
            if (_configHooked)
            {
                AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
                _configHooked = false;
            }
        }

        // Only FMOD_ERR_OUTPUT_INIT (code 36). A clip load, a decode error, or any
        // other FMOD code must not silence the session.
        public static bool IsFmodFailure(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;
            if (message.IndexOf("FMOD_ERR_OUTPUT_INIT", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return ErrorCodeIs(message, "36");
        }

        static bool ErrorCodeIs(string message, string code)
        {
            const string key = "error code ";
            int from = 0;
            while (from < message.Length)
            {
                int i = message.IndexOf(key, from, StringComparison.OrdinalIgnoreCase);
                if (i < 0) return false;
                int s = i + key.Length;
                int e = s;
                while (e < message.Length && char.IsDigit(message[e])) e++;
                if (e - s == code.Length && string.CompareOrdinal(message, s, code, 0, code.Length) == 0)
                    return true;
                from = i + key.Length;
            }
            return false;
        }

        public static bool NoteFmodFailure(string message)
        {
            if (!IsFmodFailure(message)) return false;
            _fmodFrame = Time.frameCount;
            if (!_fmodNoted)
            {
                _fmodNoted = true;
                FmodWarnings++;
                Debug.LogWarning("FMOD audio failed (error code " + FmodCode(message) + "). Sound stays off until the output returns.");
            }
            SettleDevConsole();
            return true;
        }

        // Headphones, a route change, or coming back to the app. Retry only when
        // this run had actually given up on the output, and only the sources that missed.
        public static void OnAudioConfigurationChanged(bool deviceWasChanged) => ClearOutputFailure();

        public static void OnApplicationFocus(bool focus)
        {
            if (focus) ClearOutputFailure();
        }

        public static void OnApplicationPause(bool paused)
        {
            if (!paused) ClearOutputFailure();
        }

        public static void ClearOutputFailure()
        {
            bool wasDown = _fmodNoted;
            _fmodNoted = false;
            _fmodFrame = -1;
            if (!wasDown) return;
            RetrySkipped();
        }

        static void RetrySkipped()
        {
            _retried.Clear();
            if (GamePause.Paused || Muted)
            {
                _retryDeferred = true;
                return;
            }
            _retryDeferred = false;
            var batch = new List<AudioSource>(_skipped);
            _skipped.Clear();
            var ready = new List<AudioSource>();
            for (int i = 0; i < batch.Count; i++)
            {
                var s = batch[i];
                if (!Alive(s) || !s.loop) continue;
                if (s.isPlaying) continue;
                if (HeldPaused(s)) continue;
                if (MixDesk.Live != null && (MixDesk.Live.BlocksSkippedRetry(s) || MixDesk.Live.SplashStemBlocked(s))) continue;
                ready.Add(s);
            }
            RetryPlayCount += ScheduleStemsTogether(ready);
        }

        static void RememberSkip(AudioSource source)
        {
            if (!Alive(source)) return;
            if (!Listed(_skipped, source)) _skipped.Add(source);
        }

        static bool Listed(List<AudioSource> list, AudioSource source)
        {
            if (!Alive(source)) return false;
            for (int i = 0; i < list.Count; i++)
                if (list[i] == source) return true;
            return false;
        }

        static void DropFrom(List<AudioSource> list, AudioSource source, int id)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var s = list[i];
                if (ReferenceEquals(s, null) || s == null || s == source || (id != 0 && Identity(s) == id))
                    list.RemoveAt(i);
            }
        }

        static bool HeldPaused(AudioSource source)
        {
            int id = Identity(source);
            if (id == 0) return false;
            for (int i = 0; i < _paused.Count; i++)
                if (_paused[i] == id) return true;
            return false;
        }

        static bool Alive(AudioSource source)
        {
            return !ReferenceEquals(source, null) && source != null;
        }

        static int Identity(AudioSource source)
        {
            if (ReferenceEquals(source, null)) return 0;
            try { return source.GetInstanceID(); }
            catch (Exception) { return 0; }
        }

        static void NoteDestroyed(AudioSource source)
        {
            if (ReferenceEquals(source, null)) return;
            int id = Identity(source);
            string key = id != 0 ? "destroyed:" + id.ToString(CultureInfo.InvariantCulture) : "destroyed";
            LogOnce(key, "AudioGuard skipped a destroyed source.");
        }

        static void NotePlayback(AudioSource source, Exception ex)
        {
            if (ex != null && NoteFmodFailure(ex.Message)) return;
            if (!Alive(source))
            {
                NoteDestroyed(source);
                return;
            }
            string name = "source";
            try
            {
                if (source.clip != null && !string.IsNullOrEmpty(source.clip.name))
                    name = source.clip.name;
            }
            catch (Exception) { name = "source"; }
            LogOnce("clip:" + name, "AudioGuard skipped " + name + ".");
        }

        static void LogOnce(string key, string message)
        {
            for (int i = 0; i < _logged.Count; i++)
                if (_logged[i] == key) return;
            _logged.Add(key);
            PlaybackWarnings++;
            Debug.LogWarning(message);
        }

        static void ClearSkipBook()
        {
            _skipped.Clear();
            _restore.Clear();
            _started.Clear();
            _paused.Clear();
            _retried.Clear();
            _schedIds.Clear();
            _schedAt.Clear();
            _logged.Clear();
            RejectScheduled = null;
            PlaybackWarnings = 0;
            RetryPlayCount = 0;
            PlayCalls = 0;
            StopCalls = 0;
            PauseRoutes = 0;
            ResumeRoutes = 0;
            _retryDeferred = false;
            _reassertPending = false;
        }

        public static void SettleDevConsole()
        {
#if DEVELOPMENT_BUILD
            // Keep the Development Console shut after a non-fatal FMOD init failure.
            // A later real exception takes the console back and we stop hiding it.
            if (!_fmodNoted || _consoleOwnedByError) return;
            if (_otherErrorFrame == Time.frameCount) return;
            Debug.developerConsoleVisible = false;
#endif
        }

        static string FmodCode(string message)
        {
            if (message.IndexOf("FMOD_ERR_OUTPUT_INIT", StringComparison.OrdinalIgnoreCase) >= 0) return "36";
            const string key = "error code ";
            int i = message.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i >= 0)
            {
                int s = i + key.Length;
                int e = s;
                while (e < message.Length && char.IsDigit(message[e])) e++;
                if (e > s) return message.Substring(s, e - s);
            }
            return "36";
        }

        static void OnAudioLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (IsFmodFailure(condition) || IsFmodFailure(stackTrace))
                NoteFmodFailure(string.IsNullOrEmpty(condition) ? stackTrace : condition);
            else
            {
                _otherErrorFrame = Time.frameCount;
                _consoleOwnedByError = true;
            }
        }

        // One shared mute. The Settings sheet is the only control that writes it.
        // Muted silences music and SFX. The slider level is left alone.
        public const string MuteKey = "flockfive.audio.muted";
        public const string VolumeKey = "flockfive.audio.volume";
        public static bool Muted => !MusicEnabled && !SfxEnabled;
        public static event Action<bool> MutedChanged;

        public static void LoadMuted()
        {
            bool m = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            MusicEnabled = !m;
            SfxEnabled = !m;
        }

        public static void LoadVolume()
        {
            float v = PlayerPrefs.GetFloat(VolumeKey, 1f);
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            MasterVolume = v;
        }

        public static void SetMuted(bool m, bool persist = true)
        {
            bool was = Muted;
            MusicEnabled = !m;
            SfxEnabled = !m;
            if (persist) { PlayerPrefs.SetInt(MuteKey, m ? 1 : 0); PlayerPrefs.Save(); }
            // Volume only. MusicScale is already 0 while MusicEnabled is off, so a
            // playing bed keeps its place in the loop. Nothing is stopped.
            if (was && !m)
            {
                if (GamePause.Paused) _reassertPending = true;
                else
                {
                    if (MixDesk.Live != null) MixDesk.Live.ReassertBed();
                    if (_retryDeferred)
                    {
                        _retryDeferred = false;
                        RetrySkipped();
                    }
                }
            }
            if (was != m) MutedChanged?.Invoke(m);
        }

        public static void ToggleMuted() => SetMuted(!Muted);

        // Persist the slider. Does not touch the mute bit, and does not write
        // AudioListener.volume. An ad duck or ad mute multiplies on top in MixDesk.
        public static void SetMasterVolume(float v, bool persist = true)
        {
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            MasterVolume = v;
            if (!persist) return;
            PlayerPrefs.SetFloat(VolumeKey, v);
            PlayerPrefs.Save();
        }

        public static void ResetForTests()
        {
            _n = 0;
            _seq = 0;
            MusicEnabled = true;
            SfxEnabled = true;
            MusicVolume = 1f;
            SfxVolume = 1f;
            MasterVolume = 1f;
            Clock = null;
            ActivateCalls = 0;
            FmodWarnings = 0;
            _fmodNoted = false;
            _fmodFrame = -1;
            _otherErrorFrame = -1;
            _consoleOwnedByError = false;
            ClearSkipBook();
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
        // An ad that is still up keeps its token and the pause. The reward
        // callback and a focus return both come through here, and neither
        // may talk over the ad. Close, fail, and skip call this only after
        // the show depth has already dropped.
        public static void Restore()
        {
            if (Ads.IsShowing)
            {
                DropExcept("ad");
                Ads.HoldAdAudio();
                return;
            }
            _n = 0;
            if (MixDesk.Live != null) MixDesk.Live.ResetToUser();
            GamePause.UnstickAudio();
            ActivateNative();
        }

        static void DropExcept(string keep)
        {
            bool any = false;
            for (int i = 0; i < _n; i++)
            {
                if (!_tok[i].Live || _tok[i].Reason == keep) continue;
                var t = _tok[i];
                t.Live = false;
                _tok[i] = t;
                any = true;
            }
            if (any) Compact();
        }

        // Session and listener gain only. An ad that is still up keeps the
        // pause; the category still comes back to Ambient.
        public static void Reassert()
        {
            AudioListener.volume = 1f;
            if (Ads.IsShowing)
            {
                Ads.HoldAdAudio();
                ActivateNative();
                return;
            }
            if (AdAudioHold.MayUnpauseListener)
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
            if (AdAudioHold.MayUnpauseListener)
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
        static AudioGuardHost _alive;

        public void OnAudioInterruption(string kind) => AudioGuard.OnAudioInterruption(kind);

        void OnEnable()
        {
            if (_alive != null && _alive != this) return;
            _alive = this;
            AudioGuard.HookOutputRecovery();
        }

        void OnDisable()
        {
            if (_alive != this) return;
            _alive = null;
            AudioGuard.UnhookOutputRecovery();
        }

        void OnApplicationFocus(bool focus) => AudioGuard.OnApplicationFocus(focus);

        void OnApplicationPause(bool paused) => AudioGuard.OnApplicationPause(paused);

        void Update() => AudioGuard.Tick();
    }
}

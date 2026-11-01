using UnityEngine;

namespace FlockFive
{
    public enum MixLayer { Bed, Mid, Lead }

    public sealed class MixDesk : MonoBehaviour
    {
        public static MixDesk Live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Live = null;
        }

        // Chirps live 6–8 kHz; beds sit under ~1.2 kHz. Do not dip the garden.
        public const float DuckChirp = 1f;
        public const float DuckWhoosh = 0.88f;
        public const float DuckBreak = 0.78f;
        // The bed never ducks below this, whatever a caller asks for, and always slews
        // back to exactly 1. A held clip once asked for 0.62, a drop the ear heard.
        public const float DuckFloor = 0.70f;

        AudioSource[] _stems;
        // Build 61: the one music duck every tutorial freeze uses (HoldTutorPause is the only
        // writer of GamePause.TutorHold). The garden freezes on the spot, the music fades to 0
        // over FreezeFadeSeconds and back to full over FreezeFadeSeconds once the freeze ends.
        // The level is derived from live state every frame (freeze held and no ad showing), not
        // latched by events, so an ad, a heal, GamePause.Reset, backgrounding or a scene reload
        // can never leave it down: whenever no tutorial freeze holds, it slews back to 1.
        public const float FreezeFadeSeconds = 0.6f;
        float _freezeDuck = 1f;
        float[] _stemBase;
        bool _freezeThrough;
        float _freezeLast = -1f;
        float _leadUntil;
        float _leadDuck = 1f;
        float _duckSlew = 1f;
        float _capNow;
        float _moonLiftUntil;
        float _comboUntil = -99f;
        bool _splash;
        float _splashMix;
        float _splashGate = -1f;
        bool _splashLoopOn;
        // Honey badger contest bed (stems 6-8). Exclusive over garden, combo, splash and
        // rain while on, like the poker bed's SetPoker pattern (e225f35). Same BedDuck, so
        // the "badger" sting, CardBump and every Lead still duck it.
        AudioClip _bgLoopClip, _bgEntranceClip, _bgWinClip, _bgLoseClip;
        bool _badger;
        float _badgerMix;
        float _badgerFade = 1f;
        float _badgerFadeTarget = 1f;
        // Loop pitch and tempo. 1 at rest. Desperate and the faster-riff twist raise it, never past BossBeat.PitchCap.
        // Heard pitch slews. It does not step on the frame the target changes.
        float _badgerPitch = 1f;
        float _badgerPitchFrom = 1f;
        float _badgerPitchTo = 1f;
        float _badgerPitchSlew = 1f;
        bool _bgCueLive;
        // dspTime when the entrance cue hands the seat to the loop. Pitch stays at 1 until then.
        double _bgHandoffDsp = -1;
        bool _bgWasPaused;
        float _bgHandBackAt = -1f;
        // Elapsed seconds since the final-blow cut. Negative means the voices are not ramping.
        float _bgCutT = -1f;
        float _bgCutLv, _bgCutCv, _bgCutSv;
        // Edit-mode tests feed TickBadger a dt. Negative uses the real unscaled delta.
        float _badgerDt = -1f;
        const float BadgerCap = 0.40f;
        // Stingers play once at the bed's cap, not ducked: they ARE the moment.
        const float BadgerStingCap = 0.40f;
        const float BadgerFadeSeconds = 0.30f;
        // Heard pitch arrives over this span (inside 0.3–0.5 s). A faster-riff visit
        // must not jump +68 cents on the frame the loop takes the seat.
        public const float BadgerPitchSlewSeconds = 0.40f;
        // Final-blow stop. Loop, cue and sting reach 0 over the next one or two
        // TickBadger ticks, then Stop. The frame does not wait on the ramp.
        public const float BadgerCutRampSeconds = 0.008f;
        const float BadgerBarSeconds = 1.6f;   // 150 BPM, 4/4
        const int BadgerLoopStem = 6;
        const int BadgerCueStem = 7;
        const int BadgerStingStem = 8;
        const float SplashPageHold = 0.45f;
        const int Rate = 22050;
        const float PlaceCap = 0.24f;
        const float PlaceMax = 0.28f;
        const float ComboCap = 0.04f;
        const float SplashCap = 0.52f;
        const float RainCap = 0.17f;
        const float ComboWindow = 4f;
        const float ComboIn = 0.35f;
        const float Bpm = 84f;
        const float Beat = 60f / Bpm;
        // Blackbird: one occupant. Cut and silence zero the tune. Rush swaps in
        // the same porch motif at BlackbirdCue.RushBpm, then the calm clip returns
        // from the playhead where it left, not from bar 0. Stem 1 only carries
        // the outgoing clip during the ramp crossfade.
        AudioClip _calmBed;
        AudioClip _rushBed;
        BlackbirdCue.Bed _bbBed;
        float _bbIn = 1f;
        float _bbOut;
        float _calmPlayhead;
        float _calmLeftUnscaled = -1f;
        bool _calmParked;

        public BlackbirdCue.Bed BlackbirdBed => _bbBed;
        const int NBars = 16;
        const int SplashBars = 10;
        const int TeaseBar = 5;
        const int PayoffBar = 11;

        public static void Boot(GameObject host)
        {
            Reclaim(host);
            if (Live == null) return;
            Live.Build();
            KillForeignLoops();
        }

        static void Reclaim(GameObject host)
        {
            var desks = Object.FindObjectsByType<MixDesk>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            MixDesk keep = null;
            if (Live != null) keep = Live;
            if (keep == null && host != null) keep = host.GetComponent<MixDesk>();
            if (keep == null && desks != null)
            {
                for (int i = 0; i < desks.Length; i++)
                {
                    if (desks[i] == null) continue;
                    keep = desks[i];
                    break;
                }
            }
            if (keep == null && host != null)
                keep = host.AddComponent<MixDesk>();
            Live = keep;
            if (desks == null) return;
            for (int i = 0; i < desks.Length; i++)
            {
                var d = desks[i];
                if (d == null || d == keep) continue;
                d.Hush();
                if (keep != null && d.gameObject == keep.gameObject)
                    Object.Destroy(d);
                else
                    Object.Destroy(d.gameObject);
            }
        }

        void Hush()
        {
            if (_stems == null) return;
            for (int i = 0; i < _stems.Length; i++)
            {
                var a = _stems[i];
                if (a == null) continue;
                a.Stop();
                a.volume = 0f;
            }
        }

        void PruneExtraLoops()
        {
            var all = GetComponents<AudioSource>();
            for (int i = 0; i < all.Length; i++)
            {
                var s = all[i];
                if (s == null || !s.loop) continue;
                bool ours = false;
                if (_stems != null)
                {
                    for (int k = 0; k < _stems.Length; k++)
                    {
                        if (_stems[k] == s) { ours = true; break; }
                    }
                }
                if (ours) continue;
                s.Stop();
                s.volume = 0f;
                Object.Destroy(s);
            }
        }

        static void KillForeignLoops()
        {
            var all = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var s = all[i];
                if (s == null || !s.loop) continue;
                bool ours = false;
                if (Live != null && Live._stems != null)
                {
                    for (int k = 0; k < Live._stems.Length; k++)
                    {
                        if (Live._stems[k] == s) { ours = true; break; }
                    }
                }
                if (ours) continue;
                s.Stop();
                s.volume = 0f;
            }
        }

        void OnDestroy()
        {
            Hush();
            if (Live == this) Live = null;
        }

        public bool LeadHot => AdPause.Unscaled < _leadUntil;

        public bool AllowMid => AdPause.Unscaled >= _leadUntil;

        public bool AllowBed => AdPause.Unscaled >= _leadUntil + 1.6f;

        public float BedDuck => LeadHot ? _leadDuck : 1f;

        public float FreezeDuck => _freezeDuck;
        public float SlewDuck => _duckSlew;
        public float LeadDuck => _leadDuck;

        // Shared master. The Settings slider times any live duck token. An ad
        // that is showing forces 0, so the slider cannot talk over the ad.
        // Mute is the channel enables in AudioGuard, not this value: Sound On
        // returns the same slider. Stems multiply it through MusicScale.
        // One-shots multiply it through SfxScale. Never writes the listener.
        public static float MasterGain
        {
            get
            {
                if (Ads.IsShowing) return 0f;
                float slider = AudioGuard.MasterVolume;
                if (slider < 0f) slider = 0f;
                if (slider > 1f) slider = 1f;
                return slider * AudioGuard.TokenScale;
            }
        }

        // Editor tests attach a desk without synthesizing the beds.
        public void AttachForTests() => Live = this;

        // Ads.HoldAdAudio. Stem levels go to 0 for the show. _stemBase stays,
        // so ResetToUser on close puts the slider and mute back.
        public void ApplyAdSilence()
        {
            if (_stems == null) return;
            for (int i = 0; i < _stems.Length; i++)
            {
                var a = _stems[i];
                if (a != null) a.volume = 0f;
            }
        }

        public void PlantStemForTests(float volume)
        {
            AudioSource src = _stems != null && _stems.Length > 0 ? _stems[0] : null;
            if (src == null) src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = volume;
            _stems = new AudioSource[] { src };
            _stemBase = new float[] { volume };
        }

        public float StemVolumeForTests()
        {
            if (_stems == null || _stems.Length == 0 || _stems[0] == null) return -1f;
            return _stems[0].volume;
        }

        public void TickForTests() => LateUpdate();

        public bool BadgerOnForTests => _badger;

        // Drive the badger seat with a chosen dt. Edit mode does not advance unscaled time.
        public float TickBadgerForTests(float dt)
        {
            _badgerDt = dt < 0f ? 0f : dt;
            try { return TickBadger(1f); }
            finally { _badgerDt = -1f; }
        }

        // Three probe voices on the badger stems, already playing. Listener stays muted by the caller.
        public void PlantBadgerVoicesForTests(out AudioSource loop, out AudioSource cue, out AudioSource sting)
        {
            if (_stems == null || _stems.Length < 9)
            {
                var old = _stems;
                _stems = new AudioSource[9];
                if (old != null)
                    for (int i = 0; i < old.Length && i < 9; i++)
                        _stems[i] = old[i];
            }
            var clip = AudioClip.Create("badger-cut-probe", 22050, 1, 22050, false);
            clip.SetData(new float[22050], 0);
            loop = PlantBadgerVoice(BadgerLoopStem, clip, true);
            cue = PlantBadgerVoice(BadgerCueStem, clip, false);
            sting = PlantBadgerVoice(BadgerStingStem, clip, false);
        }

        AudioSource PlantBadgerVoice(int i, AudioClip clip, bool loop)
        {
            var s = _stems[i];
            if (s == null) s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            s.clip = clip;
            s.volume = 0.4f;
            s.ignoreListenerPause = true;
            if (s.isPlaying) s.Stop();
            s.Play();
            _stems[i] = s;
            return s;
        }

        public static void DetachForTests(MixDesk desk)
        {
            if (Live == desk) Live = null;
        }

        public void ForceDuckForTests(float freeze, float slew, float lead, float leadFor)
        {
            _freezeDuck = freeze;
            _duckSlew = slew;
            _leadDuck = lead;
            _leadUntil = AdPause.Unscaled + Mathf.Max(0f, leadFor);
        }

        // Popup and ad ducks go back to the user mix. A tutorial freeze that is
        // still held keeps fading; everything else snaps to full.
        public void ResetToUser()
        {
            _leadDuck = 1f;
            _leadUntil = AdPause.Unscaled;
            _duckSlew = 1f;
            if (!TutorFreezeHeld)
            {
                _freezeDuck = 1f;
                if (_freezeThrough) MusicThroughPause(false);
            }
            if (_stems == null || _stemBase == null || TutorFreezeHeld) return;
            float user = AudioGuard.MusicScale;
            for (int i = 0; i < _stems.Length && i < _stemBase.Length; i++)
            {
                var a = _stems[i];
                if (a != null) a.volume = _stemBase[i] * _freezeDuck * user;
            }
        }

        public void MarkLead(float seconds, float duckRemain = DuckChirp)
        {
            if (duckRemain < DuckFloor) duckRemain = DuckFloor;
            bool wasHot = LeadHot;
            float until = AdPause.Unscaled + Mathf.Max(0.05f, seconds);
            if (until > _leadUntil) _leadUntil = until;
            _leadDuck = wasHot ? Mathf.Min(_leadDuck, duckRemain) : duckRemain;
        }

        // Drop a lead window this caller opened. A longer lead keeps the seat.
        public void EndLead(float until)
        {
            if (until <= 0f) return;
            if (_leadUntil > until + 0.05f) return;
            _leadUntil = AdPause.Unscaled;
            _leadDuck = 1f;
        }

        public void MoonLift(float seconds = 2.4f)
        {
            _moonLiftUntil = AdPause.Unscaled + Mathf.Max(0.4f, seconds);
        }

        public void ComboWarm()
        {
            _comboUntil = AdPause.Unscaled + ComboWindow;
        }

        public void SetSplash(bool on)
        {
            if (on && !_splash) ArmSplashWelcome();
            if (!on) CutSplashWelcome();
            _splash = on;
        }

        void ArmSplashWelcome()
        {
            _splashMix = 1f; // mute garden immediately — no stray bed before the band
            _splashGate = AdPause.Unscaled + SplashPageHold;
            _splashLoopOn = false;
            if (_stems != null && _stems.Length > 4 && _stems[4] != null)
            {
                _stems[4].Stop();
                _stems[4].time = 0f;
            }
        }

        void CutSplashWelcome()
        {
            _splashGate = -1f;
            _splashLoopOn = false;
            if (_stems != null && _stems.Length > 4 && _stems[4] != null)
            {
                _stems[4].Stop();
                _stems[4].time = 0f;
            }
        }

        void Build()
        {
            var garden = LoadBed("Audio/Bed/garden-theme", MakeDawn);
            var combo = MakeCombo();
            var theme = LoadBed("Audio/Bed/splash-theme", MakeSplash);
            var rain = MakeRain();
            _bgLoopClip = LoadBadgerClip("Audio/Bed/badger-battle");
            _bgEntranceClip = LoadBadgerClip("Audio/Bed/badger-entrance");
            _bgWinClip = LoadBadgerClip("Audio/Bed/badger-win");
            _bgLoseClip = LoadBadgerClip("Audio/Bed/badger-lose");
            if (_stems == null || _stems.Length < 9)
            {
                var old = _stems;
                _stems = new AudioSource[9];
                if (old != null)
                    for (int i = 0; i < old.Length && i < 9; i++)
                        _stems[i] = old[i];
            }
            for (int i = 0; i < _stems.Length; i++)
                if (_stems[i] == null) _stems[i] = null;
            SwapClip(0, garden, true);
            _calmBed = garden;
            SwapClip(1, garden, false);
            SwapClip(2, garden, false);
            SwapClip(3, combo, true);
            SwapClip(4, theme, false);
            if (_stems[4] != null)
            {
                _stems[4].Stop();
                _stems[4].time = 0f;
                _stems[4].volume = 0f;
            }
            SwapClip(5, rain, true);
            SwapClip(BadgerLoopStem, _bgLoopClip, false);
            SwapClip(BadgerCueStem, _bgEntranceClip, false);
            SwapClip(BadgerStingStem, null, false);
            // Entrance and stingers are one-shots that live on the desk (never loop).
            if (_stems[BadgerCueStem] != null) _stems[BadgerCueStem].loop = false;
            if (_stems[BadgerStingStem] != null) _stems[BadgerStingStem].loop = false;
            _badger = false;
            _badgerMix = 0f;
            _bgCueLive = false;
            _bgHandBackAt = -1f;
            PruneExtraLoops();
        }

        static AudioClip LoadBadgerClip(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            // PlayScheduled needs the samples resident before the leap, not on first Play.
            // preloadAudioData is already on for these clips. LoadAudioData does not
            // touch the listener or the session; the span is here so a device log can
            // show that next to LevelPlay.Init.
            if (clip != null && clip.loadState != AudioDataLoadState.Loaded)
            {
                using (StartupTrace.Begin("MixDesk.LoadAudioData"))
                    clip.LoadAudioData();
            }
            return clip;
        }

        // ---- honey badger contest bed ----

        public bool BadgerReady => _bgLoopClip != null && _stems != null && _stems.Length > BadgerStingStem;

        int BadgerBarSamples(AudioClip c) => c == null ? 0 : Mathf.RoundToInt(BadgerBarSeconds * c.frequency);

        // The leap starts (flower tap or garden clear). The entrance cue is scored to the
        // BadgerLeap + BadgerOpening clocks and already contains loop bar 1 (6.40-8.00 s), so the
        // loop is scheduled sample-accurately at the end of the cue from its bar-2 offset.
        public void BadgerEntrance()
        {
            if (!BadgerReady) return;
            StopBadgerSources();
            _badger = true;
            // The place bed under the leap (garden or splash) fades out over 0.3 s while
            // the cue speaks at full level from the first hop.
            _badgerFade = 1f;
            _badgerFadeTarget = 1f;
            _bgHandBackAt = -1f;
            var loop = _stems[BadgerLoopStem];
            var cue = _stems[BadgerCueStem];
            if (loop == null) return;
            if (_bgEntranceClip == null || cue == null)
            {
                _bgHandoffDsp = -1;
                RestLoopPitch(loop);
                loop.timeSamples = 0;
                loop.Play();
                return;
            }
            double t0 = AudioSettings.dspTime + 0.03;
            double handoff = t0 + _bgEntranceClip.samples / (double)_bgEntranceClip.frequency;
            cue.clip = _bgEntranceClip;
            cue.timeSamples = 0;
            cue.volume = BadgerCap * BedDuck;
            cue.PlayScheduled(t0);
            RestLoopPitch(loop);
            loop.timeSamples = BadgerBarSamples(loop.clip);
            loop.PlayScheduled(handoff);
            _bgHandoffDsp = handoff;
            _bgCueLive = true;
        }

        // Hard cut for the final blow. Loop, cue and sting ramp to 0 over the next
        // one or two ticks, then stop. The seat stays claimed so the garden does not rush
        // back during the beat of silence. The win stinger is a later BadgerResult call.
        // Returns on this frame so the sharp hit can fire beside the cut.
        public void BadgerCut()
        {
            _badger = true;
            _badgerMix = 1f;
            _badgerFade = 0f;
            _badgerFadeTarget = 0f;
            _bgCueLive = false;
            _bgHandoffDsp = -1;
            _bgHandBackAt = -1f;
            _badgerPitch = 1f;
            BeginBadgerCutRamp();
        }

        // Loop only. Cue and stingers stay at pitch 1. Clamped to the producer cap.
        public void SetBadgerPitch(float pitch)
        {
            _badgerPitch = BossBeat.ClampPitch(pitch);
        }

        public float BadgerPitch => _badgerPitch;

        // The contest is up. Normally the entrance already owns the seat (no-op). Without a
        // leap (editor drop file, missing cue) the loop starts here with a short fade-in.
        public void BadgerBattle()
        {
            if (!BadgerReady) return;
            var loop = _stems[BadgerLoopStem];
            if (loop == null) return;
            if (_badger && (_bgCueLive || loop.isPlaying)) return;
            _badger = true;
            _bgHandBackAt = -1f;
            _badgerFade = 0f;
            _badgerFadeTarget = 1f;
            _bgHandoffDsp = -1;
            RestLoopPitch(loop);
            loop.timeSamples = 0;
            loop.Play();
        }

        // Result: the bed fades out over 0.3 s and the matching stinger plays once. After the
        // stinger the seat hands back to the bed that was up before (the splash). Returns false
        // when the clip is missing so the caller keeps its old one-shot (fanfare / Deny).
        public bool BadgerResult(bool won)
        {
            // The win stinger follows the cut by a beat. If a ramp is still open, finish it
            // before this Play, or the next tick would Stop the new stinger.
            if (_bgCutT >= 0f) FinishBadgerCut();
            var clip = won ? _bgWinClip : _bgLoseClip;
            if (!BadgerReady || clip == null) return false;
            _badgerFadeTarget = 0f;
            var s = _stems[BadgerStingStem];
            if (s == null) return false;
            s.Stop();
            s.loop = false;
            s.clip = clip;
            s.timeSamples = 0;
            s.volume = BadgerStingCap;
            s.Play();
            // Lead seat for the stinger: Mid decorations wait; DuckChirp (1) so nothing dips.
            MarkLead(clip.length, DuckChirp);
            _bgHandBackAt = AdPause.Unscaled + clip.length + 0.25f;
            return true;
        }

        // Off: exit, back, Continue, an aborted leap, or a level load. 0.3 s fade, then the
        // loop and cue stop and the previous bed (splash or garden) fades back in. A stinger
        // already playing rings out.
        public void SetBadger(bool on)
        {
            if (on)
            {
                BadgerBattle();
                return;
            }
            _badger = false;
            _bgHandBackAt = -1f;
        }

        void StopBadgerSources()
        {
            _bgCutT = -1f;
            if (_stems == null || _stems.Length <= BadgerCueStem) return;
            var loop = _stems[BadgerLoopStem];
            var cue = _stems[BadgerCueStem];
            if (loop != null) loop.Stop();
            if (cue != null) cue.Stop();
            _bgCueLive = false;
            _bgHandoffDsp = -1;
            RestLoopPitch(loop);
        }

        // Target the loop may chase. While the entrance cue still owns the seat, stay at 1
        // so bar 1 (inside the cue) and the loop share a tempo.
        public static float PitchTarget(float requested, bool cueLive, bool badgerOn)
        {
            if (!badgerOn || cueLive) return 1f;
            return BossBeat.ClampPitch(requested);
        }

        // u is 0 when the target changes and 1 when the slew finishes. Never a one-frame snap.
        public static float SlewPitch(float from, float to, float u)
        {
            if (u <= 0f) return from;
            if (u >= 1f) return to;
            return Mathf.Lerp(from, to, u);
        }

        // 1 at the start of a final-blow cut, 0 once BadgerCutRampSeconds has elapsed.
        public static float CutGain(float elapsed)
        {
            float dur = BadgerCutRampSeconds;
            if (dur <= 0f) return 0f;
            if (elapsed <= 0f) return 1f;
            if (elapsed >= dur) return 0f;
            return 1f - elapsed / dur;
        }

        void RestLoopPitch(AudioSource loop)
        {
            _badgerPitchFrom = 1f;
            _badgerPitchTo = 1f;
            _badgerPitchSlew = 1f;
            if (loop != null) loop.pitch = 1f;
        }

        AudioSource BadgerStem(int i)
        {
            if (_stems == null || i < 0 || i >= _stems.Length) return null;
            return _stems[i];
        }

        // Arm the ramp and return. Voices that are already silent stop now. Live voices
        // keep playing at their current gain until TickBadger walks CutGain down to 0.
        void BeginBadgerCutRamp()
        {
            var loop = BadgerStem(BadgerLoopStem);
            var cue = BadgerStem(BadgerCueStem);
            var sting = BadgerStem(BadgerStingStem);
            _bgCutLv = loop != null ? loop.volume : 0f;
            _bgCutCv = cue != null ? cue.volume : 0f;
            _bgCutSv = sting != null ? sting.volume : 0f;
            bool live = (loop != null && loop.isPlaying) || (cue != null && cue.isPlaying) || (sting != null && sting.isPlaying);
            RestLoopPitch(loop);
            if (!live)
            {
                FinishBadgerCut();
                return;
            }
            _bgCutT = 0f;
        }

        // One tick of the cut. A 16 ms frame finishes the 8 ms ramp; a shorter tick holds
        // the voices at CutGain so the mixer can read it. Never spins.
        void AdvanceBadgerCut(float dt)
        {
            if (_bgCutT < 0f) return;
            if (dt < 0f) dt = 0f;
            _bgCutT += dt;
            float g = CutGain(_bgCutT);
            if (g > 0f)
            {
                var loop = BadgerStem(BadgerLoopStem);
                var cue = BadgerStem(BadgerCueStem);
                var sting = BadgerStem(BadgerStingStem);
                if (loop != null) loop.volume = _bgCutLv * g;
                if (cue != null) cue.volume = _bgCutCv * g;
                if (sting != null) sting.volume = _bgCutSv * g;
                return;
            }
            FinishBadgerCut();
        }

        void FinishBadgerCut()
        {
            _bgCutT = -1f;
            var loop = BadgerStem(BadgerLoopStem);
            var cue = BadgerStem(BadgerCueStem);
            var sting = BadgerStem(BadgerStingStem);
            SilenceStem(loop);
            SilenceStem(cue);
            SilenceStem(sting);
            _bgCueLive = false;
            RestLoopPitch(loop);
        }

        static void SilenceStem(AudioSource s)
        {
            if (s == null) return;
            s.volume = 0f;
            s.Stop();
        }

        bool CueOwnsPitch(AudioSource cue)
        {
            // Scheduled handoff is the clock. isPlaying covers a cue that is still speaking
            // if dspTime was stolen by a listener pause. Either one keeps the loop at 1.
            if (_bgHandoffDsp > 0 && AudioSettings.dspTime < _bgHandoffDsp) return true;
            return _bgCueLive && cue != null && cue.isPlaying;
        }

        void ApplyLoopPitch(AudioSource loopSrc, AudioSource cue, float dt)
        {
            if (loopSrc == null) return;
            float want = PitchTarget(_badgerPitch, CueOwnsPitch(cue), _badger);
            float heard = loopSrc.pitch;
            if (Mathf.Abs(heard - want) <= 0.0001f)
            {
                _badgerPitchFrom = want;
                _badgerPitchTo = want;
                _badgerPitchSlew = 1f;
                return;
            }
            if (_badgerPitchSlew >= 1f || Mathf.Abs(_badgerPitchTo - want) > 0.0001f)
            {
                _badgerPitchFrom = heard;
                _badgerPitchTo = want;
                _badgerPitchSlew = 0f;
            }
            float step = dt;
            if (step < 0f) step = 0f;
            if (step > 0.05f) step = 0.05f;
            float dur = BadgerPitchSlewSeconds;
            _badgerPitchSlew += dur <= 0f ? 1f : step / dur;
            if (_badgerPitchSlew > 1f) _badgerPitchSlew = 1f;
            loopSrc.pitch = SlewPitch(_badgerPitchFrom, _badgerPitchTo, _badgerPitchSlew);
        }

        // GamePause (AudioListener.pause) freezes the sources but not dspTime, so a loop still
        // waiting on its schedule would start out of step. Re-aim it from the cue's own clock.
        void ResyncBadgerEntrance()
        {
            if (!_bgCueLive || _bgEntranceClip == null) return;
            var cue = _stems[BadgerCueStem];
            var loop = _stems[BadgerLoopStem];
            if (cue == null || loop == null || !cue.isPlaying) return;
            double left = (_bgEntranceClip.samples - cue.timeSamples) / (double)_bgEntranceClip.frequency;
            if (left <= 0.02) return;
            loop.Stop();
            loop.timeSamples = BadgerBarSamples(loop.clip);
            double at = AudioSettings.dspTime + left;
            loop.PlayScheduled(at);
            _bgHandoffDsp = at;
        }

        // Returns the share of the place seat the badger holds (0..1) and drives its stems.
        float TickBadger(float duck)
        {
            if (_stems == null || _stems.Length <= BadgerStingStem) return 0f;
            if (_bgHandBackAt > 0f && AdPause.Unscaled >= _bgHandBackAt)
            {
                _bgHandBackAt = -1f;
                _badger = false;
            }
            float dt = _badgerDt >= 0f ? _badgerDt : AdPause.UnscaledDelta;
            if (dt < 0f) dt = 0f;
            float want = _badger ? 1f : 0f;
            _badgerMix = Mathf.MoveTowards(_badgerMix, want, dt / BadgerFadeSeconds);
            _badgerFade = Mathf.MoveTowards(_badgerFade, _badgerFadeTarget, dt / BadgerFadeSeconds);
            var cue = _stems[BadgerCueStem];
            var loopSrc = _stems[BadgerLoopStem];
            if (_bgCueLive && cue != null && !cue.isPlaying && cue.timeSamples == 0
                && _bgEntranceClip != null && loopSrc != null && loopSrc.isPlaying)
                _bgCueLive = false;
            // The cut owns loop, cue and sting until it reaches 0. SetStem would snap the
            // loop (fade is already 0) and put the cue back to full.
            bool cutting = _bgCutT >= 0f;
            if (cutting) AdvanceBadgerCut(dt);
            else
            {
                // On: badger stems at full (no fade on the first hop). Off: they ride the 0.3 s ramp down.
                float level = _badger ? 1f : _badgerMix;
                SetStem(BadgerLoopStem, level * _badgerFade * BadgerCap * duck);
                SetStem(BadgerCueStem, level * BadgerCap * duck);
            }
            ApplyLoopPitch(loopSrc, cue, dt);
            if (!_badger && _badgerMix <= 0f && _bgCutT < 0f) StopBadgerSources();
            return _badgerMix;
        }

        void SwapClip(int i, AudioClip clip, bool start)
        {
            var a = _stems[i];
            // Destroyed Unity objects compare == null; clear the slot and remake.
            if (a == null)
            {
                _stems[i] = TakeOrMakeLoop(clip, start);
                return;
            }
            if (a.clip != clip)
            {
                a.Stop();
                a.clip = clip;
            }
            if (start)
            {
                if (!a.isPlaying) a.Play();
            }
            else if (a.isPlaying)
                a.Stop();
        }

        static AudioClip LoadBed(string path, System.Func<AudioClip> make)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null) return clip;
            return make();
        }

        AudioSource TakeOrMakeLoop(AudioClip clip, bool start)
        {
            var all = GetComponents<AudioSource>();
            for (int i = 0; i < all.Length; i++)
            {
                var s = all[i];
                if (s == null || !s.loop) continue;
                bool used = false;
                if (_stems != null)
                {
                    for (int k = 0; k < _stems.Length; k++)
                    {
                        if (_stems[k] == s) { used = true; break; }
                    }
                }
                if (used) continue;
                BindLoop(s, clip, start);
                return s;
            }
            return MakeLoop(clip, start);
        }

        AudioSource MakeLoop(AudioClip clip, bool start)
        {
            var a = gameObject.AddComponent<AudioSource>();
            BindLoop(a, clip, start);
            return a;
        }

        static void BindLoop(AudioSource a, AudioClip clip, bool start)
        {
            a.playOnAwake = false;
            a.loop = true;
            a.spatialBlend = 0f;
            a.volume = 0f;
            if (a.clip != clip)
            {
                if (a.isPlaying) a.Stop();
                a.clip = clip;
            }
            if (start)
            {
                if (!a.isPlaying) a.Play();
            }
            else if (a.isPlaying)
                a.Stop();
        }

        // Call before GamePause.Push for a tutorial freeze: the stems are set to ride through the
        // listener pause first, so they keep playing and fade instead of stopping dead.
        public static void TutorFreeze(bool on)
        {
            if (Live == null) return;
            if (on) Live.MusicThroughPause(true);
            // Off needs nothing here: TickFreezeDuck sees the freeze gone and fades back up.
        }

        public const float TutorFreezeMusic = 1f;
        static bool TutorFreezeHeld => GamePause.Paused && GamePause.TutorHold && !Ads.IsShowing;

        void MusicThroughPause(bool on)
        {
            _freezeThrough = on;
            if (_stems == null) return;
            for (int i = 0; i < _stems.Length; i++)
            {
                var a = _stems[i];
                if (a != null && a.ignoreListenerPause != on) a.ignoreListenerPause = on;
            }
        }

        // Shared freeze duck. Runs first in LateUpdate, paused or not.
        void TickFreezeDuck()
        {
            bool held = TutorFreezeHeld;
            // An ad never plays over the bed: silence now, and let the listener pause own it.
            if (Ads.IsShowing && _freezeThrough) _freezeDuck = 0f;
            if (held != _freezeThrough) MusicThroughPause(held);
            else if (held) MusicThroughPause(true);  // a stem rebuilt mid-freeze rides through too
            float now = Time.realtimeSinceStartup;
            // Real time between ticks, capped: a resume from background must not jump the fade.
            float dt = _freezeLast < 0f ? 0f : Mathf.Clamp(now - _freezeLast, 0f, 0.1f);
            _freezeLast = now;
            // b86 (Brandon): the sparrow tutorial pause keeps the music playing at full level
            // (it rides through the listener pause); ads still silence it above.
            _freezeDuck = Mathf.MoveTowards(_freezeDuck, held ? TutorFreezeMusic : 1f, dt / FreezeFadeSeconds);
        }

        void OnApplicationPause(bool paused)
        {
            // Back from the background: restart the fade clock; the level follows live state.
            if (!paused) _freezeLast = -1f;
        }

        void ApplyFreezeDuck()
        {
            if (_stems == null || _stemBase == null) return;
            for (int i = 0; i < _stems.Length && i < _stemBase.Length; i++)
            {
                var a = _stems[i];
                if (a != null) a.volume = _stemBase[i] * _freezeDuck * AudioGuard.MusicScale;
            }
        }

        void LateUpdate()
        {
            TickFreezeDuck();
            // GamePause returns before the bed is rewritten. An ad still has to
            // zero the stems here, or a stolen listener pause lets the bed play.
            if (Ads.IsShowing)
            {
                ApplyAdSilence();
                if (GamePause.Paused) _bgWasPaused = true;
                return;
            }
            if (GamePause.Paused)
            {
                _bgWasPaused = true;
                // Tutorial freeze: the bed keeps playing and fades on the level it had.
                if (_freezeThrough) ApplyFreezeDuck();
                return;
            }
            if (_bgWasPaused)
            {
                _bgWasPaused = false;
                ResyncBadgerEntrance();
            }
            if (!LeadHot) _leadDuck = 1f;
            if (_stems == null) return;

            float target = BedDuck;
            // Gentle both ways: a 0.3 duck takes ~0.1 s in and ~0.2 s out, no snap.
            float rate = target < _duckSlew ? 3f : 1.5f;
            _duckSlew = Mathf.MoveTowards(_duckSlew, target, AdPause.UnscaledDelta * rate);

            bool moon = AdPause.Unscaled < _moonLiftUntil;
            float capWant = moon ? PlaceMax : PlaceCap;
            // The moon lift eases the bed cap over half a second, no step in level.
            if (_capNow <= 0f) _capNow = capWant;
            _capNow = Mathf.MoveTowards(_capNow, capWant, AdPause.UnscaledDelta * (PlaceMax - PlaceCap) * 2f);
            float cap = _capNow;
            float duck = _duckSlew;
            float splashT = _splash ? 1f : 0f;
            float splashRate = splashT > _splashMix ? 1.7f : 2.8f;
            _splashMix = Mathf.MoveTowards(_splashMix, splashT, AdPause.UnscaledDelta * splashRate);
            float badgerSeat = TickBadger(duck);
            float others = 1f - badgerSeat;
            float gardenMix = (1f - _splashMix) * others;
            TickSplashWelcome();
            // One garden occupant: the soft porch theme. Dawn/mid/last stay
            // loaded as fallbacks but silent so two tunes never sit together.
            // Blackbird cut/silence take the tune (and the combo fifth) to 0.
            // Rush is the same motif on stem 0. Stem 1 holds the outgoing clip
            // only while the ramp crossfades (BedCrossfadeSec), then it stops.
            bool tuneOn = _bbBed == BlackbirdCue.Bed.Calm || _bbBed == BlackbirdCue.Bed.Rush;
            if (!tuneOn)
            {
                _bbIn = 0f;
                _bbOut = 0f;
            }
            else
            {
                float step = AdPause.UnscaledDelta / BlackbirdCue.BedCrossfadeSec;
                if (step < 0f) step = 0f;
                _bbIn = Mathf.MoveTowards(_bbIn, 1f, step);
                _bbOut = Mathf.MoveTowards(_bbOut, 0f, step);
            }
            if (_bbOut <= 0.001f) StopBlackbirdOutgoing();
            float comboMul = _bbBed == BlackbirdCue.Bed.Calm ? 1f : 0f;
            SetStem(0, cap * duck * gardenMix * _bbIn);
            SetStem(1, cap * duck * gardenMix * _bbOut);
            SetStem(2, 0f);
            SetStem(3, ComboGain() * ComboCap * duck * gardenMix * comboMul);
            float splashVol = _splashMix * SplashCap * duck * others;
            SetStem(4, _splashLoopOn ? splashVol : 0f);
            // Non-melodic place air. Not a fourth flute bed.
            float rainMul = _bbBed == BlackbirdCue.Bed.Silence ? 0f : 1f;
            SetStem(5, GardenStorm.Wet * RainCap * duck * gardenMix * rainMul);
        }

        // App calls this when the visit changes phase. Rush is the same D-major
        // porch line as dawn, at a shorter beat, and it replaces stem 0.
        // Calm comes back on the playhead it left. The ramp crossfades the stem
        // over BedCrossfadeSec. Cut and silence drop the level immediately.
        public void SetBlackbirdBed(BlackbirdCue.Bed bed)
        {
            var prev = _bbBed;
            _bbBed = bed;
            if (bed == prev) return;
            if (_stems == null || _stems.Length == 0 || _stems[0] == null)
            {
                if (bed == BlackbirdCue.Bed.Cut || bed == BlackbirdCue.Bed.Silence)
                {
                    _bbIn = 0f;
                    _bbOut = 0f;
                }
                return;
            }
            if (_calmBed == null) _calmBed = _stems[0].clip;
            if (bed == BlackbirdCue.Bed.Cut || bed == BlackbirdCue.Bed.Silence)
            {
                if (prev == BlackbirdCue.Bed.Rush)
                    AdoptBlackbirdClip(_calmBed, true, false);
                _bbIn = 0f;
                _bbOut = 0f;
                StopBlackbirdOutgoing();
                return;
            }
            if (bed == BlackbirdCue.Bed.Rush)
            {
                AdoptBlackbirdClip(RushBed(), false, true);
                return;
            }
            if (prev == BlackbirdCue.Bed.Rush)
            {
                AdoptBlackbirdClip(_calmBed, true, true);
                return;
            }
            // Cut or silence lifting back to calm. The clip is already the porch
            // tune and its playhead was never rewound. Volume slews back up.
        }

        void AdoptBlackbirdClip(AudioClip want, bool resumeCalm, bool crossfade)
        {
            var a = _stems[0];
            if (want == null || a == null) return;
            float outTime = a.time;
            AudioClip outgoing = a.clip;
            bool leavingCalm = _calmBed != null && outgoing == _calmBed;
            if (leavingCalm)
            {
                _calmPlayhead = outTime;
                _calmLeftUnscaled = AdPause.Unscaled;
                _calmParked = true;
            }
            if (crossfade && outgoing != null && outgoing != want && _stems.Length > 1 && _stems[1] != null && a.isPlaying)
            {
                var b = _stems[1];
                b.loop = true;
                b.pitch = 1f;
                b.Stop();
                b.clip = outgoing;
                b.time = outTime;
                b.Play();
                _bbOut = _bbIn > 0.02f ? _bbIn : 1f;
            }
            else
                StopBlackbirdOutgoing();
            float t = 0f;
            if (resumeCalm) t = TakeCalmResume(want);
            a.loop = true;
            a.pitch = 1f;
            a.Stop();
            a.clip = want;
            a.time = t;
            a.Play();
            if (crossfade) _bbIn = 0f;
        }

        float TakeCalmResume(AudioClip want)
        {
            float length = want != null ? want.length : 0f;
            float elapsed = _calmParked && _calmLeftUnscaled >= 0f ? AdPause.Unscaled - _calmLeftUnscaled : 0f;
            _calmParked = false;
            return BlackbirdCue.ResumeCalm(_calmPlayhead, elapsed, length);
        }

        void StopBlackbirdOutgoing()
        {
            _bbOut = 0f;
            if (_stems == null || _stems.Length < 2 || _stems[1] == null) return;
            var b = _stems[1];
            if (b.isPlaying) b.Stop();
            if (_calmBed != null && b.clip != _calmBed)
            {
                b.clip = _calmBed;
                b.time = 0f;
            }
        }

        AudioClip RushBed()
        {
            if (_rushBed == null) _rushBed = MakeBlackbirdRush();
            return _rushBed;
        }

        // Dawn's pizz walk (38 / 45, payoff 50) and the same FirstFour flute,
        // squeezed onto a shorter beat. One flute, no pad, no second tune.
        static AudioClip MakeBlackbirdRush()
        {
            float beat = 60f / BlackbirdCue.RushBpm;
            const int bars = 8;
            int n = UnityEngine.Mathf.RoundToInt(bars * 4f * beat * Rate);
            var bus = new float[n * 2];
            for (int bar = 0; bar < bars; bar++)
            {
                float tBar = bar * 4f * beat;
                bool pay = bar >= 5;
                float g = pay ? 0.28f : 0.18f;
                PlaceWrap(bus, Pizz(38f), tBar, -0.18f, g);
                PlaceWrap(bus, Pizz(45f), tBar + 2f * beat, 0.16f, g * 0.92f);
                if (pay) PlaceWrap(bus, Pizz(50f), tBar + 3f * beat, 0.22f, g * 0.55f);
            }
            MixNotesWrap(bus, FirstFour, 0.38f, -0.08f, beat, 5f * 4f);
            // No edge fade. A baked dip would return every loop (~16.5s at 116 BPM).
            // The ramp fades stem volume instead, over BedCrossfadeSec.
            return PeakClip("blackbird-rush", bus, 0.40f);
        }

        static void MixNotesWrap(float[] bus, Note[] notes, float gain, float pan, float beat, float phraseBeat)
        {
            if (notes == null) return;
            for (int i = 0; i < notes.Length; i++)
            {
                var nt = notes[i];
                int len = UnityEngine.Mathf.RoundToInt(nt.Dur * beat * Rate);
                if (len < 8) continue;
                PlaceWrap(bus, Flute(len, nt.Midi), (phraseBeat + nt.Start) * beat, pan, gain);
            }
        }

        static void PlaceWrap(float[] stereo, float[] mono, float t0, float pan, float gain)
        {
            int frames = stereo.Length / 2;
            if (frames <= 0 || mono == null) return;
            int i0 = Mathf.RoundToInt(t0 * Rate);
            float p = 0.5f * (pan + 1f);
            float gl = Mathf.Cos(p * Mathf.PI * 0.5f) * gain;
            float gr = Mathf.Sin(p * Mathf.PI * 0.5f) * gain;
            for (int i = 0; i < mono.Length; i++)
            {
                int f = i0 + i;
                f %= frames;
                if (f < 0) f += frames;
                int o = f * 2;
                stereo[o] += mono[i] * gl;
                stereo[o + 1] += mono[i] * gr;
            }
        }

        void TickSplashWelcome()
        {
            if (!_splash) return;
            if (_splashGate <= 0f || AdPause.Unscaled < _splashGate) return;
            // Only the jingle plays on the studio card; the splash music starts after it.
            if (FlockFiveApp.StudioCardHold) return;
            _splashGate = -1f;
            if (_stems != null && _stems.Length > 4 && _stems[4] != null)
            {
                _stems[4].time = 0f;
                _stems[4].Play();
                _splashLoopOn = true;
            }
        }

        float ComboGain()
        {
            float age = AdPause.Unscaled - (_comboUntil - ComboWindow);
            if (age < 0f || AdPause.Unscaled >= _comboUntil) return 0f;
            if (age < ComboIn) return age / ComboIn;
            return 1f - (age - ComboIn) / (ComboWindow - ComboIn);
        }

        void SetStem(int i, float vol)
        {
            if (_stemBase == null || _stemBase.Length != _stems.Length) _stemBase = new float[_stems.Length];
            _stemBase[i] = vol;
            var a = _stems[i];
            if (a == null)
            {
                _stems[i] = null;
                return;
            }
            // MusicScale folds in MasterGain: slider, duck, and ad silence.
            a.volume = vol * _freezeDuck * AudioGuard.MusicScale;
        }

        static int LoopN() => Mathf.RoundToInt(NBars * 4f * Beat * Rate);

        static float Hz(float midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

        static float OnePoleA(float fc)
        {
            if (fc <= 0f) return 0f;
            return 1f - Mathf.Exp(-2f * Mathf.PI * fc / Rate);
        }

        static void OnePoleLp(float[] x, float fc)
        {
            float a = OnePoleA(fc);
            float acc = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                acc += a * (x[i] - acc);
                x[i] = acc;
            }
        }

        static void Place(float[] stereo, float[] mono, float t0, float pan, float gain)
        {
            int i0 = Mathf.RoundToInt(t0 * Rate);
            if (i0 >= stereo.Length / 2) return;
            float p = 0.5f * (pan + 1f);
            float gl = Mathf.Cos(p * Mathf.PI * 0.5f) * gain;
            float gr = Mathf.Sin(p * Mathf.PI * 0.5f) * gain;
            int n = Mathf.Min(mono.Length, stereo.Length / 2 - i0);
            for (int i = 0; i < n; i++)
            {
                int o = (i0 + i) * 2;
                stereo[o] += mono[i] * gl;
                stereo[o + 1] += mono[i] * gr;
            }
        }

        static float[] Flute(int n, float midi)
        {
            // Hollow tube + brief chiff. Tongued, not an organ pad.
            var y = new float[n];
            float f0 = Hz(midi);
            float phase = 0f;
            float a = OnePoleA(1080f);
            float aAir = OnePoleA(1400f);
            float lp = 0f, air = 0f;
            float dur = n / (float)Rate;
            float rel = Mathf.Min(0.20f, dur * 0.28f);
            int h = Mathf.RoundToInt(midi * 913f + 17f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env;
                if (t < 0.012f) env = t / 0.012f;
                else if (t < 0.16f) env = Mathf.Lerp(1f, 0.58f, (t - 0.012f) / 0.148f);
                else if (t > dur - rel) env = 0.58f * Mathf.Max(0f, 1f - (t - (dur - rel)) / rel);
                else env = 0.58f;
                env *= 0.94f + 0.06f * Mathf.Sin(2f * Mathf.PI * 1.6f * t);
                float vib = 1f + 0.0031f * Mathf.Sin(2f * Mathf.PI * 4.7f * t);
                phase += 2f * Mathf.PI * f0 * vib / Rate;
                float s = Mathf.Sin(phase) + 0.07f * Mathf.Sin(2f * phase) + 0.025f * Mathf.Sin(3f * phase);
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nz = (h / 1073741824f) - 1f;
                air += aAir * (nz - air);
                float chiff = t < 0.018f ? air * (1f - t / 0.018f) * 0.22f : 0f;
                lp += a * ((s + chiff) * env - lp);
                y[i] = lp;
            }
            return y;
        }

        static float[] Pizz(float midi)
        {
            // Karplus–Strong plucked string. Not a sine pad, not a xylophone bar.
            float f0 = Hz(midi);
            int delay = Mathf.Clamp(Mathf.RoundToInt(Rate / f0), 6, Rate / 30);
            float dur = midi < 50f ? 0.30f : midi < 62f ? 0.20f : 0.15f;
            int n = Mathf.CeilToInt(dur * Rate);
            var y = new float[n];
            int h = Mathf.RoundToInt(midi * 1103f + 91f);
            int exc = Mathf.Min(delay, Mathf.RoundToInt(0.0035f * Rate));
            for (int i = 0; i < delay && i < n; i++)
            {
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nz = (h / 1073741824f) - 1f;
                y[i] = i < exc ? nz : 0f;
            }
            float damp = midi < 48f ? 0.9968f : midi < 60f ? 0.9942f : 0.9915f;
            for (int i = delay; i < n; i++)
            {
                float a = y[i - delay];
                float b = i - delay - 1 >= 0 ? y[i - delay - 1] : a;
                y[i] = 0.5f * (a + b) * damp;
            }
            float bodyA = OnePoleA(midi < 48f ? 620f : 920f);
            float lp = 0f;
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = t < 0.0018f ? t / 0.0018f : Mathf.Exp(-(t - 0.0018f) * (midi < 48f ? 8.5f : 13f));
                phase += 2f * Mathf.PI * f0 / Rate;
                float body = Mathf.Sin(phase) * env * 0.18f;
                lp += bodyA * (y[i] * 0.88f + body - lp);
                y[i] = lp;
            }
            return y;
        }

        static void PlacePizz(float[] bus, float midi, float tSec, float pan, float gain)
        {
            Place(bus, Pizz(midi), tSec, pan, gain);
        }

        struct Note { public float Start, Dur, Midi; public Note(float s, float d, float m) { Start = s; Dur = d; Midi = m; } }

        static readonly Note[] Motif =
        {
            new Note(0f, 2f, 62f), new Note(2f, 1f, 64f), new Note(3f, 1f, 66f),
            new Note(4f, 2f, 69f), new Note(6f, 1f, 66f), new Note(7f, 1f, 64f),
            new Note(8f, 2f, 66f), new Note(10f, 1f, 64f), new Note(11f, 1f, 62f),
            new Note(12f, 2f, 57f), new Note(14f, 2f, 62f)
        };

        static readonly Note[] FirstFour =
        {
            new Note(0f, 2f, 62f), new Note(2f, 1f, 64f), new Note(3f, 1f, 66f), new Note(4f, 2f, 69f)
        };

        static void MixNotes(float[] bus, Note[] notes, float gain, float pan, params float[] phraseBeats)
        {
            for (int p = 0; p < phraseBeats.Length; p++)
            {
                for (int i = 0; i < notes.Length; i++)
                {
                    var nt = notes[i];
                    int n = Mathf.RoundToInt(nt.Dur * Beat * Rate);
                    if (n < 8) continue;
                    Place(bus, Flute(n, nt.Midi), (phraseBeats[p] + nt.Start) * Beat, pan, gain);
                }
            }
        }

        static void ApplyFades(float[] stereo)
        {
            int frames = stereo.Length / 2;
            int ni = Mathf.Min(Mathf.RoundToInt(0.4f * Rate), frames);
            int no = Mathf.Min(Mathf.RoundToInt(0.55f * Rate), frames);
            for (int i = 0; i < ni; i++)
            {
                float w = i / (float)ni;
                stereo[i * 2] *= w;
                stereo[i * 2 + 1] *= w;
            }
            for (int i = 0; i < no; i++)
            {
                float w = (no - 1 - i) / (float)Mathf.Max(1, no - 1);
                int f = frames - no + i;
                stereo[f * 2] *= w;
                stereo[f * 2 + 1] *= w;
            }
        }

        static AudioClip PeakClip(string name, float[] stereo, float peak)
        {
            float p = 1e-6f;
            for (int i = 0; i < stereo.Length; i++)
            {
                float v = Mathf.Abs(stereo[i]);
                if (v > p) p = v;
            }
            float g = peak / p;
            for (int i = 0; i < stereo.Length; i++)
                stereo[i] = Mathf.Clamp(stereo[i] * g, -0.98f, 0.98f);
            int frames = stereo.Length / 2;
            var clip = AudioClip.Create(name, frames, 2, Rate, false);
            clip.SetData(stereo, 0);
            return clip;
        }

        enum Climb { Dawn, Mid, Last }

        static readonly float[] Walk = { 38f, 45f, 42f, 45f };

        static void PizzWalk(float[] bus, Climb climb)
        {
            for (int bar = 0; bar < NBars; bar++)
            {
                float tBar = bar * 4f * Beat;
                bool tease = bar >= TeaseBar;
                bool pay = bar >= PayoffBar;
                if (climb == Climb.Dawn)
                {
                    float dawnG = pay ? 0.28f : tease ? 0.20f : 0.15f;
                    PlacePizz(bus, 38f, tBar, -0.18f, dawnG);
                    PlacePizz(bus, 45f, tBar + 2f * Beat, 0.16f, dawnG * 0.92f);
                    if (pay) PlacePizz(bus, 50f, tBar + 3f * Beat, 0.22f, dawnG * 0.55f);
                    continue;
                }

                float walkG = !tease ? 0.18f : pay ? 0.30f : 0.24f;
                for (int q = 0; q < 4; q++)
                    PlacePizz(bus, Walk[q], tBar + q * Beat, q % 2 == 0 ? -0.22f : 0.20f, walkG);

                if (climb == Climb.Mid && pay)
                    PlacePizz(bus, 57f, tBar + 2f * Beat, 0.30f, 0.14f);

                if (climb == Climb.Last)
                {
                    float ge = pay ? 0.20f : tease ? 0.14f : 0.09f;
                    for (int q = 0; q < 4; q++)
                        PlacePizz(bus, Walk[(q + 2) % 4] + 12f, tBar + (q + 0.5f) * Beat, 0.28f, ge);
                    if (pay)
                    {
                        PlacePizz(bus, 50f, tBar, -0.08f, 0.24f);
                        PlacePizz(bus, 54f, tBar, 0.12f, 0.18f);
                        PlacePizz(bus, 57f, tBar, 0.32f, 0.14f);
                    }
                }
            }
        }

        static void Bloom(float[] bus, Climb climb)
        {
            float teaseAt = TeaseBar * 4f;
            float payAt = PayoffBar * 4f;
            if (climb == Climb.Dawn)
            {
                MixNotes(bus, FirstFour, 0.38f, -0.08f, payAt);
                return;
            }

            MixNotes(bus, FirstFour, climb == Climb.Mid ? 0.26f : 0.20f, -0.1f, teaseAt);
            float leadG = climb == Climb.Mid ? 0.34f : 0.40f;
            MixNotes(bus, Motif, leadG, -0.06f, payAt);
            if (climb == Climb.Last)
            {
                // Answer in pizzicato, not a second flute tune.
                PlacePizz(bus, 54f, (payAt + 8f) * Beat, 0.38f, 0.20f);
                PlacePizz(bus, 57f, (payAt + 9.5f) * Beat, 0.34f, 0.18f);
                PlacePizz(bus, 50f, (payAt + 14f) * Beat, -0.12f, 0.24f);
                PlacePizz(bus, 38f, (payAt + 14f) * Beat, -0.28f, 0.20f);
            }
        }

        static AudioClip MakeRain()
        {
            // Steady garden rain: dense leaf-hits, no wind whoosh, no loop swell.
            int n = Mathf.RoundToInt(8.0f * Rate);
            var bus = new float[n * 2];
            var hiss = new float[n];
            int h = 91331;
            float hp = 0f, lp = 0f, bp = 0f;
            float aHp = OnePoleA(820f);
            float aLp = OnePoleA(1680f);
            float aBp = OnePoleA(1100f);
            for (int i = 0; i < n; i++)
            {
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float nz = (h / 1073741824f) - 1f;
                hp += aHp * (nz - hp);
                float hi = nz - hp;
                lp += aLp * (hi - lp);
                bp += aBp * (lp - bp);
                hiss[i] = lp - 0.35f * bp;
            }
            for (int i = 0; i < n; i++)
            {
                bus[i * 2] += hiss[i] * 0.30f;
                bus[i * 2 + 1] += hiss[(i * 17 + n / 3) % n] * 0.30f;
            }

            int drops = 1480;
            int hh = 44117;
            for (int d = 0; d < drops; d++)
            {
                hh = (hh * 1103515245 + 12345) & 0x7fffffff;
                int at = (int)((hh / 2147483647f) * n);
                if (at < 0) at = 0;
                hh = (hh * 1103515245 + 12345) & 0x7fffffff;
                int len = 90 + (hh % 220);
                hh = (hh * 1103515245 + 12345) & 0x7fffffff;
                float pan = (hh / 1073741824f) - 1f;
                hh = (hh * 1103515245 + 12345) & 0x7fffffff;
                float amp = 0.13f + 0.20f * (hh / 2147483647f);
                hh = (hh * 1103515245 + 12345) & 0x7fffffff;
                int src = hh % n;
                float gl = Mathf.Cos((pan + 1f) * 0.5f * Mathf.PI * 0.5f);
                float gr = Mathf.Sin((pan + 1f) * 0.5f * Mathf.PI * 0.5f);
                for (int k = 0; k < len; k++)
                {
                    float e = Mathf.Exp(-k / 38f) * Mathf.Clamp01(k / 4f);
                    float s = hiss[(src + k) % n] * e * amp;
                    int o = ((at + k) % n) * 2;
                    bus[o] += s * gl;
                    bus[o + 1] += s * gr;
                }
            }

            FlattenRms(bus, 0.12f);
            LoopSeam(bus, 0.028f);
            return PeakClip("garden-rain", bus, 0.46f);
        }

        static void FlattenRms(float[] stereo, float target)
        {
            int frames = stereo.Length / 2;
            int win = Mathf.Max(32, Rate / 40);
            float acc = 0f;
            for (int i = 0; i < win && i < frames; i++)
            {
                float l = stereo[i * 2], r = stereo[i * 2 + 1];
                acc += l * l + r * r;
            }
            for (int i = 0; i < frames; i++)
            {
                int add = i + win;
                int rem = i - win;
                if (add < frames)
                {
                    float l = stereo[add * 2], r = stereo[add * 2 + 1];
                    acc += l * l + r * r;
                }
                if (rem >= 0)
                {
                    float l = stereo[rem * 2], r = stereo[rem * 2 + 1];
                    acc -= l * l + r * r;
                }
                float rms = Mathf.Sqrt(Mathf.Max(1e-8f, acc / (2f * win)));
                float g = target / rms;
                if (g > 2.4f) g = 2.4f;
                if (g < 0.45f) g = 0.45f;
                stereo[i * 2] *= g;
                stereo[i * 2 + 1] *= g;
            }
        }

        static void LoopSeam(float[] stereo, float seconds)
        {
            int frames = stereo.Length / 2;
            int m = Mathf.Min(Mathf.RoundToInt(seconds * Rate), frames / 8);
            if (m < 4) return;
            for (int i = 0; i < m; i++)
            {
                float w = i / (float)(m - 1);
                float a = Mathf.Sin(w * Mathf.PI * 0.5f);
                float b = Mathf.Cos(w * Mathf.PI * 0.5f);
                int head = i;
                int tail = frames - m + i;
                float l = stereo[head * 2] * a + stereo[tail * 2] * b;
                float r = stereo[head * 2 + 1] * a + stereo[tail * 2 + 1] * b;
                stereo[head * 2] = l;
                stereo[head * 2 + 1] = r;
                stereo[tail * 2] = l;
                stereo[tail * 2 + 1] = r;
            }
        }

        static AudioClip MakeDawn()
        {
            int n = LoopN();
            var bus = new float[n * 2];
            PizzWalk(bus, Climb.Dawn);
            Bloom(bus, Climb.Dawn);
            ApplyFades(bus);
            return PeakClip("dawn-garden", bus, 0.40f);
        }

        static AudioClip MakeMid()
        {
            int n = LoopN();
            var bus = new float[n * 2];
            PizzWalk(bus, Climb.Mid);
            Bloom(bus, Climb.Mid);
            ApplyFades(bus);
            return PeakClip("mid-climb", bus, 0.48f);
        }

        static AudioClip MakeLast()
        {
            int n = LoopN();
            var bus = new float[n * 2];
            PizzWalk(bus, Climb.Last);
            Bloom(bus, Climb.Last);
            ApplyFades(bus);
            return PeakClip("last-light", bus, 0.56f);
        }

        static AudioClip MakeCombo()
        {
            // Quiet pizz fifth on the same seat — never a pad drone.
            int bars = 4;
            int n = Mathf.RoundToInt(bars * 4f * Beat * Rate);
            var bus = new float[n * 2];
            for (int q = 0; q < bars * 4; q++)
            {
                float t = q * Beat;
                PlacePizz(bus, 38f, t, -0.12f, 0.16f);
                PlacePizz(bus, 45f, t, 0.14f, 0.12f);
            }
            ApplyFades(bus);
            return PeakClip("combo-fifth", bus, 0.28f);
        }

        static AudioClip MakeSplash()
        {
            // Proud title piece — same D-major flute + pizz family, ONE flute melody.
            // Intro → statement → bridge → payoff → cadence. Harmony lives in pizz
            // (thirds / sixths / answers), never a second flute, pad, kick, or choir.
            int n = Mathf.RoundToInt(SplashBars * 4f * Beat * Rate);
            var bus = new float[n * 2];

            for (int bar = 0; bar < SplashBars; bar++)
            {
                float tBar = bar * 4f * Beat;
                bool intro = bar < 2;
                bool bridge = bar == 5;
                bool payoff = bar >= 6;

                // Bass floor: low D (26) + octave (38). Climb into payoff.
                float bassG = intro ? 0.18f : bridge ? 0.28f : payoff ? 0.52f : 0.36f;
                PlacePizz(bus, 26f, tBar, -0.34f, bassG);
                PlacePizz(bus, 26f, tBar + 2f * Beat, -0.30f, bassG * (payoff ? 0.95f : 0.72f));
                if (!intro)
                    PlacePizz(bus, 38f, tBar, -0.22f, bassG * 0.55f);

                // Walking pizz — sparse intro, full payoff; bridge thins then lands.
                float walkG = intro ? 0.12f : bridge ? 0.16f : payoff ? 0.40f : 0.26f;
                int walkQs = intro ? 2 : 4; // intro: downs only
                for (int q = 0; q < walkQs; q++)
                {
                    int qi = intro ? q * 2 : q;
                    PlacePizz(bus, Walk[qi % 4], tBar + qi * Beat, qi % 2 == 0 ? -0.22f : 0.20f, walkG);
                }

                // High octave sparkle — soft until payoff.
                float ge = intro ? 0.03f : bridge ? 0.08f : payoff ? 0.22f : 0.12f;
                for (int q = 0; q < 4; q++)
                    PlacePizz(bus, Walk[(q + 2) % 4] + 12f, tBar + (q + 0.5f) * Beat, 0.28f, ge);

                // Chord blooms (D–F#–A). Mid-bar bloom only in payoff.
                if (!intro)
                {
                    float cg = bridge ? 0.14f : payoff ? 0.32f : 0.20f;
                    PlacePizz(bus, 50f, tBar, -0.08f, cg);
                    PlacePizz(bus, 54f, tBar, 0.12f, cg * 0.78f);
                    PlacePizz(bus, 57f, tBar, 0.30f, cg * 0.58f);
                    if (payoff)
                    {
                        PlacePizz(bus, 50f, tBar + 2f * Beat, -0.06f, cg * 0.80f);
                        PlacePizz(bus, 54f, tBar + 2f * Beat, 0.14f, cg * 0.60f);
                        PlacePizz(bus, 57f, tBar + 2f * Beat, 0.28f, cg * 0.48f);
                        PlacePizz(bus, 45f, tBar + 2f * Beat, 0.18f, 0.16f); // A under
                    }
                }
            }

            // Flute arc (one melody): tease → statement → payoff.
            MixNotes(bus, FirstFour, 0.30f, -0.08f, 0f);       // intro tease
            MixNotes(bus, Motif, 0.40f, -0.06f, 8f);            // statement @ bar 2
            MixNotes(bus, Motif, 0.56f, -0.04f, 24f);           // payoff @ bar 6

            // Pizz harmony under statement Motif (sixths below) — not a second flute.
            float s1 = 8f;
            PlacePizz(bus, 54f, (s1 + 0f) * Beat, 0.32f, 0.14f);   // under D4
            PlacePizz(bus, 55f, (s1 + 2f) * Beat, 0.30f, 0.12f);   // under E4
            PlacePizz(bus, 57f, (s1 + 3f) * Beat, 0.28f, 0.12f);   // under F#4
            PlacePizz(bus, 61f, (s1 + 4f) * Beat, 0.34f, 0.13f);   // under A4
            PlacePizz(bus, 49f, (s1 + 12f) * Beat, -0.20f, 0.14f); // under A3 land

            // Bridge breath into payoff: thin chord hit then swell (bar 5).
            float tBr = 5f * 4f * Beat;
            PlacePizz(bus, 26f, tBr + 3f * Beat, -0.34f, 0.34f);
            PlacePizz(bus, 50f, tBr + 3f * Beat, -0.08f, 0.22f);
            PlacePizz(bus, 57f, tBr + 3f * Beat, 0.30f, 0.18f);

            // Payoff Motif: pizz thirds above + low answers in the gaps.
            float s2 = 24f;
            PlacePizz(bus, 66f, (s2 + 0f) * Beat, 0.36f, 0.16f);   // F# under/with D4
            PlacePizz(bus, 67f, (s2 + 2f) * Beat, 0.34f, 0.14f);   // G with E4
            PlacePizz(bus, 69f, (s2 + 3f) * Beat, 0.32f, 0.14f);   // A with F#4
            PlacePizz(bus, 73f, (s2 + 4f) * Beat, 0.38f, 0.15f);   // C# with A4
            PlacePizz(bus, 54f, (s2 + 8f) * Beat, 0.40f, 0.24f);   // answer gap
            PlacePizz(bus, 57f, (s2 + 9.5f) * Beat, 0.36f, 0.22f);
            PlacePizz(bus, 59f, (s2 + 10f) * Beat, 0.30f, 0.16f);  // B color
            PlacePizz(bus, 50f, (s2 + 14f) * Beat, -0.12f, 0.28f);
            PlacePizz(bus, 38f, (s2 + 14f) * Beat, -0.28f, 0.24f);
            PlacePizz(bus, 26f, (s2 + 14f) * Beat, -0.36f, 0.32f);

            // Cadence into loop: land home on last bar.
            float tLast = 9f * 4f * Beat;
            PlacePizz(bus, 26f, tLast + 2f * Beat, -0.34f, 0.36f);
            PlacePizz(bus, 38f, tLast + 2f * Beat, -0.22f, 0.28f);
            PlacePizz(bus, 50f, tLast + 3f * Beat, -0.06f, 0.24f);
            PlacePizz(bus, 45f, tLast + 3f * Beat, 0.18f, 0.20f);
            PlacePizz(bus, 57f, tLast + 3f * Beat, 0.30f, 0.16f);

            LoopSeam(bus, 0.58f);
            return PeakClip("splash-theme", bus, 0.62f);
        }


    }
}


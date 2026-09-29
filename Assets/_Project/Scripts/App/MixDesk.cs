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

        AudioSource[] _stems;
        float _leadUntil;
        float _leadDuck = 1f;
        float _duckSlew = 1f;
        float _moonLiftUntil;
        float _comboUntil = -99f;
        bool _splash;
        float _splashMix;
        float _splashGate = -1f;
        bool _splashLoopOn;
        float _rainEnv;
        float _rainSwell = 1f;
        float _rainDuck = 1f;
        float _rainDuckUntil = -99f;
        float _rainPhaseA;
        float _rainPhaseB;
        float _rainHzA;
        float _rainHzB;
        const float SplashPageHold = 0.45f;
        const int Rate = 22050;
        const float PlaceCap = 0.24f;
        const float PlaceMax = 0.28f;
        const float ComboCap = 0.04f;
        const float SplashCap = 0.52f;
        // 25% under the old 0.17 fader. Clip bed peaks near 0.40 and sparse taps near 0.96,
        // so a full swell is ~0.05 bed / ~0.12 tap — about 40% of a score ching (0.64 × ~0.50)
        // and under the garden place cap. Same AudioSource path as every other stem, so
        // AudioListener volume and MasterLoudness apply (no private bus).
        const float RainCap = 0.128f;
        const float RainFade = 1.5f;
        const float RainDuckGain = 0.70f;
        const float ComboWindow = 4f;
        const float ComboIn = 0.35f;
        const float Bpm = 84f;
        const float Beat = 60f / Bpm;
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

        public bool LeadHot => Time.unscaledTime < _leadUntil;

        public bool AllowMid => Time.unscaledTime >= _leadUntil;

        public bool AllowBed => Time.unscaledTime >= _leadUntil + 1.6f;

        public float BedDuck => LeadHot ? _leadDuck : 1f;

        public void MarkLead(float seconds, float duckRemain = DuckChirp)
        {
            bool wasHot = LeadHot;
            float until = Time.unscaledTime + Mathf.Max(0.05f, seconds);
            if (until > _leadUntil) _leadUntil = until;
            _leadDuck = wasHot ? Mathf.Min(_leadDuck, duckRemain) : duckRemain;
        }

        public void MoonLift(float seconds = 2.4f)
        {
            _moonLiftUntil = Time.unscaledTime + Mathf.Max(0.4f, seconds);
        }

        public void ComboWarm()
        {
            _comboUntil = Time.unscaledTime + ComboWindow;
        }

        // Score, combo, and the collect crunch. Rain only — the garden bed stays put.
        public void DuckRain(float seconds = 0.50f)
        {
            float until = Time.unscaledTime + Mathf.Max(0.2f, seconds);
            if (until > _rainDuckUntil) _rainDuckUntil = until;
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
            _splashGate = Time.unscaledTime + SplashPageHold;
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
            if (_stems == null || _stems.Length < 6)
            {
                var old = _stems;
                _stems = new AudioSource[6];
                if (old != null)
                    for (int i = 0; i < old.Length && i < 6; i++)
                        _stems[i] = old[i];
            }
            for (int i = 0; i < _stems.Length; i++)
                if (_stems[i] == null) _stems[i] = null;
            SwapClip(0, garden, true);
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
            if (_stems[5] != null)
            {
                _stems[5].ignoreListenerVolume = false;
                _stems[5].ignoreListenerPause = false;
            }
            PruneExtraLoops();
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

        void LateUpdate()
        {
            if (!LeadHot) _leadDuck = 1f;
            if (_stems == null) return;

            float target = BedDuck;
            float rate = target < _duckSlew ? 10f : 4f;
            _duckSlew = Mathf.MoveTowards(_duckSlew, target, Time.unscaledDeltaTime * rate);

            bool moon = Time.unscaledTime < _moonLiftUntil;
            float cap = moon ? PlaceMax : PlaceCap;
            float duck = _duckSlew;
            float splashT = _splash ? 1f : 0f;
            float splashRate = splashT > _splashMix ? 1.7f : 2.8f;
            _splashMix = Mathf.MoveTowards(_splashMix, splashT, Time.unscaledDeltaTime * splashRate);
            float gardenMix = 1f - _splashMix;
            TickSplashWelcome();
            // One garden occupant: the soft porch theme. Dawn/mid/last stay
            // loaded as fallbacks but silent so two tunes never sit together.
            SetStem(0, cap * duck * gardenMix);
            SetStem(1, 0f);
            SetStem(2, 0f);
            SetStem(3, ComboGain() * ComboCap * duck * gardenMix);
            float splashVol = _splashMix * SplashCap * duck;
            SetStem(4, _splashLoopOn ? splashVol : 0f);
            // Non-melodic place air. Not a fourth flute bed. Own fade and duck,
            // so a hop does not pump the rain and thunder is left alone.
            TickRain(Time.unscaledDeltaTime);
            SetStem(5, _rainEnv * _rainSwell * _rainDuck * RainCap * gardenMix);
        }

        void TickRain(float dt)
        {
            if (dt < 0f) dt = 0f;
            _rainEnv = Mathf.MoveTowards(_rainEnv, GardenStorm.Want, dt / RainFade);
            if (_rainHzA <= 0f) _rainHzA = 1f / 9.5f;
            if (_rainHzB <= 0f) _rainHzB = 1f / 13f;
            _rainPhaseA += dt * _rainHzA;
            _rainPhaseB += dt * _rainHzB;
            if (_rainPhaseA >= 1f)
            {
                _rainPhaseA -= 1f;
                _rainHzA = 1f / Random.Range(6f, 15f);
            }
            if (_rainPhaseB >= 1f)
            {
                _rainPhaseB -= 1f;
                _rainHzB = 1f / Random.Range(7f, 14f);
            }
            float a = 0.5f + 0.5f * Mathf.Sin(_rainPhaseA * Mathf.PI * 2f);
            float b = 0.5f + 0.5f * Mathf.Sin(_rainPhaseB * Mathf.PI * 2f);
            _rainSwell = Mathf.Lerp(0.78f, 1f, a * 0.62f + b * 0.38f);

            float duckTarget = Time.unscaledTime < _rainDuckUntil ? RainDuckGain : 1f;
            float rate = duckTarget < _rainDuck ? (0.30f / 0.05f) : (0.30f / 0.22f);
            _rainDuck = Mathf.MoveTowards(_rainDuck, duckTarget, dt * rate);
        }

        void TickSplashWelcome()
        {
            if (!_splash) return;
            if (_splashGate <= 0f || Time.unscaledTime < _splashGate) return;
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
            float age = Time.unscaledTime - (_comboUntil - ComboWindow);
            if (age < 0f || Time.unscaledTime >= _comboUntil) return 0f;
            if (age < ComboIn) return age / ComboIn;
            return 1f - (age - ComboIn) / (ComboWindow - ComboIn);
        }

        void SetStem(int i, float vol)
        {
            var a = _stems[i];
            if (a == null)
            {
                _stems[i] = null;
                return;
            }
            a.volume = vol;
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
            // Near patter under ~4.5 kHz, a quieter far bed under ~1.2 kHz, sparse taps.
            // 16s so the loop is not a short hiss phrase. No RMS flatten — that was the wall.
            const float seconds = 16f;
            int n = Mathf.RoundToInt(seconds * Rate);
            var nearL = new float[n];
            var nearR = new float[n];
            var far = new float[n];
            FillNoise(nearL, 91331);
            FillNoise(nearR, 48271);
            FillNoise(far, 17389);
            OnePoleHp(nearL, 260f);
            OnePoleHp(nearR, 260f);
            // 4th-order lowpass, cutoff 4.5 kHz. One-poles at this rate still pass hiss.
            BiquadLp(nearL, 4500f, 0.707f);
            BiquadLp(nearL, 4500f, 0.707f);
            BiquadLp(nearR, 4500f, 0.707f);
            BiquadLp(nearR, 4500f, 0.707f);
            OnePoleHp(far, 90f);
            // Heavier bed, knee ~1.2 kHz.
            BiquadLp(far, 1700f, 0.707f);
            BiquadLp(far, 1700f, 0.707f);
            BiquadLp(far, 1700f, 0.707f);
            ScalePeak(nearL, 0.30f);
            ScalePeak(nearR, 0.30f);
            ScalePeak(far, 0.16f);

            var bus = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                // Two cycles across the loop so the seam meets. Runtime swell does the rest.
                float breathe = 0.94f + 0.06f * Mathf.Sin(2f * Mathf.PI * (i / (float)n) * 2f);
                bus[i * 2] = (nearL[i] + far[i]) * breathe;
                bus[i * 2 + 1] = (nearR[i] + far[i]) * breathe;
            }
            ScalePeak(bus, 0.40f);

            int hh = 44117;
            float t = 0.40f;
            while (t < seconds - 0.28f)
            {
                hh = Lcg(hh);
                t += 0.55f + (hh / 2147483647f) * 1.15f;
                if (t >= seconds - 0.28f) break;
                hh = Lcg(hh);
                float pitch = 0.85f + (hh / 2147483647f) * 0.30f;
                hh = Lcg(hh);
                float amp = 0.34f + (hh / 2147483647f) * 0.28f;
                hh = Lcg(hh);
                float pan = (hh / 1073741824f) - 1f;
                DropTap(bus, n, t, pitch, amp, pan, ref hh);
                hh = Lcg(hh);
                if ((hh & 255) < 76)
                {
                    hh = Lcg(hh);
                    float dt = 0.055f + (hh / 2147483647f) * 0.09f;
                    hh = Lcg(hh);
                    float pitch2 = 0.85f + (hh / 2147483647f) * 0.30f;
                    hh = Lcg(hh);
                    float amp2 = amp * (0.45f + 0.35f * (hh / 2147483647f));
                    hh = Lcg(hh);
                    float pan2 = (hh / 1073741824f) - 1f;
                    if (t + dt < seconds - 0.22f)
                        DropTap(bus, n, t + dt, pitch2, amp2, pan2, ref hh);
                }
            }

            LoopSeam(bus, 0.14f);
            float peak = 1e-6f;
            for (int i = 0; i < bus.Length; i++)
            {
                float v = bus[i] < 0f ? -bus[i] : bus[i];
                if (v > peak) peak = v;
            }
            // Do not lift a quiet bed up to a hot peak. Only shave overs.
            if (peak > 0.96f) peak = 0.96f;
            return PeakClip("garden-rain", bus, peak);
        }

        static int Lcg(int h) => (h * 1103515245 + 12345) & 0x7fffffff;

        static void FillNoise(float[] dst, int seed)
        {
            int h = seed | 1;
            for (int i = 0; i < dst.Length; i++)
            {
                h = Lcg(h);
                dst[i] = (h / 1073741824f) - 1f;
            }
        }

        static void BiquadLp(float[] x, float fc, float q)
        {
            float w0 = 2f * Mathf.PI * fc / Rate;
            float cos = Mathf.Cos(w0);
            float alpha = Mathf.Sin(w0) / (2f * q);
            float a0 = 1f + alpha;
            float b0 = (1f - cos) * 0.5f / a0;
            float b1 = (1f - cos) / a0;
            float b2 = b0;
            float a1 = -2f * cos / a0;
            float a2 = (1f - alpha) / a0;
            float z1 = 0f, z2 = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                float inp = x[i];
                float y = b0 * inp + z1;
                z1 = b1 * inp - a1 * y + z2;
                z2 = b2 * inp - a2 * y;
                x[i] = y;
            }
        }

        static void OnePoleHp(float[] x, float fc)
        {
            float a = OnePoleA(fc);
            float acc = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                float x0 = x[i];
                acc += a * (x0 - acc);
                x[i] = x0 - acc;
            }
        }

        static void ScalePeak(float[] x, float peak)
        {
            float p = 1e-6f;
            for (int i = 0; i < x.Length; i++)
            {
                float v = x[i] < 0f ? -x[i] : x[i];
                if (v > p) p = v;
            }
            float g = peak / p;
            for (int i = 0; i < x.Length; i++)
                x[i] *= g;
        }

        static void DropTap(float[] bus, int frames, float time, float pitch, float amp, float pan, ref int h)
        {
            pitch = Mathf.Clamp(pitch, 0.85f, 1.15f);
            int len = Mathf.RoundToInt(0.072f * Rate / pitch);
            if (len < 16) len = 16;
            var grain = new float[len];
            int hh = h | 1;
            float body = 0f;
            float a = OnePoleA(Mathf.Clamp(1480f * pitch, 1100f, 1800f));
            float phase = 0f;
            float dPhase = 2f * Mathf.PI * (390f * pitch) / Rate;
            float decay = (0.024f * Rate) / pitch;
            for (int k = 0; k < len; k++)
            {
                hh = Lcg(hh);
                float nz = (hh / 1073741824f) - 1f;
                body += a * (nz - body);
                phase += dPhase;
                float att = k < 8 ? k / 8f : 1f;
                float env = att * Mathf.Exp(-k / decay);
                grain[k] = (body * 0.82f + Mathf.Sin(phase) * 0.18f) * env;
            }
            h = hh;
            ScalePeak(grain, amp);
            int i0 = Mathf.RoundToInt(time * Rate);
            float p = 0.5f * (pan + 1f);
            float gl = Mathf.Cos(p * Mathf.PI * 0.5f);
            float gr = Mathf.Sin(p * Mathf.PI * 0.5f);
            for (int k = 0; k < len; k++)
            {
                int i = i0 + k;
                if ((uint)i >= (uint)frames) continue;
                int o = i * 2;
                bus[o] += grain[k] * gl;
                bus[o + 1] += grain[k] * gr;
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


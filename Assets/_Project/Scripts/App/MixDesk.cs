using UnityEngine;

namespace FlockFive
{
    public enum MixLayer { Bed, Mid, Lead }

    public sealed class MixDesk : MonoBehaviour
    {
        public static MixDesk Live;
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
        const int NBars = 16;
        const int SplashBars = 10;
        const int TeaseBar = 5;
        const int PayoffBar = 11;

        public static void Boot(GameObject host)
        {
            if (Live == null)
            {
                Live = host.GetComponent<MixDesk>();
                if (Live == null) Live = host.AddComponent<MixDesk>();
            }
            Live.Build();
        }

        void OnDestroy()
        {
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

        public void SetSplash(bool on)
        {
            _splash = on;
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
            SwapClip(0, garden);
            SwapClip(1, garden);
            SwapClip(2, garden);
            SwapClip(3, combo);
            SwapClip(4, theme);
            SwapClip(5, rain);
        }

        void SwapClip(int i, AudioClip clip)
        {
            var a = _stems[i];
            // Destroyed Unity objects compare == null; clear the slot and remake.
            if (a == null)
            {
                _stems[i] = MakeLoop(clip);
                return;
            }
            a.clip = clip;
            if (!a.isPlaying) a.Play();
        }

        static AudioClip LoadBed(string path, System.Func<AudioClip> make)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null) return clip;
            return make();
        }

        AudioSource MakeLoop(AudioClip clip)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.loop = true;
            a.spatialBlend = 0f;
            a.clip = clip;
            a.volume = 0f;
            a.Play();
            return a;
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
            // One garden occupant: the soft porch theme. Dawn/mid/last stay
            // loaded as fallbacks but silent so two tunes never sit together.
            SetStem(0, cap * duck * gardenMix);
            SetStem(1, 0f);
            SetStem(2, 0f);
            SetStem(3, ComboGain() * ComboCap * duck * gardenMix);
            SetStem(4, _splashMix * SplashCap * duck);
            // Non-melodic place air. Not a fourth flute bed.
            SetStem(5, GardenStorm.Wet * RainCap * duck * gardenMix);
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

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
        const int Rate = 22050;
        const float PlaceCap = 0.24f;
        const float PlaceMax = 0.28f;
        const float ComboCap = 0.04f;
        const float ComboWindow = 4f;
        const float ComboIn = 0.35f;
        const float Bpm = 84f;
        const float Beat = 60f / Bpm;
        const int NBars = 16;
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

        void Build()
        {
            var dawn = LoadBed("Audio/Bed/dawn-garden", MakeDawn);
            var mid = LoadBed("Audio/Bed/mid-climb", MakeMid);
            var last = LoadBed("Audio/Bed/last-light", MakeLast);
            var combo = MakeCombo();
            if (_stems == null)
            {
                _stems = new AudioSource[4];
                _stems[0] = MakeLoop(dawn);
                _stems[1] = MakeLoop(mid);
                _stems[2] = MakeLoop(last);
                _stems[3] = MakeLoop(combo);
                return;
            }
            SwapClip(0, dawn);
            SwapClip(1, mid);
            SwapClip(2, last);
            SwapClip(3, combo);
        }

        void SwapClip(int i, AudioClip clip)
        {
            var a = _stems[i];
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

            float d = SkyCycle.Dusk;
            // One bed occupant; short handoff so two flute lines never sit together.
            float day = 1f - Mathf.SmoothStep(0.12f, 0.32f, d);
            float dusk = Mathf.SmoothStep(0.18f, 0.38f, d) * (1f - Mathf.SmoothStep(0.55f, 0.75f, d));
            float night = Mathf.SmoothStep(0.62f, 0.82f, d);
            bool moon = Time.unscaledTime < _moonLiftUntil;
            if (moon) night += 0.35f;
            float sum = day + dusk + night;
            if (sum < 0.001f) { day = 1f; sum = 1f; }
            day /= sum;
            dusk /= sum;
            night /= sum;

            float cap = moon ? PlaceMax : PlaceCap;
            float duck = _duckSlew;
            SetStem(0, day * cap * duck);
            SetStem(1, dusk * cap * duck);
            SetStem(2, night * cap * duck);
            SetStem(3, ComboGain() * ComboCap * duck);
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
            if (_stems[i] != null) _stems[i].volume = vol;
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
    }
}

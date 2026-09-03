using UnityEngine;

namespace FlockFive
{
    public enum MixLayer { Bed, Mid, Lead }

    public sealed class MixDesk : MonoBehaviour
    {
        public static MixDesk Live;
        public const float DuckChirp = 0.18f;
        public const float DuckWhoosh = 0.15f;
        public const float DuckBreak = 0.08f;

        AudioSource[] _stems;
        float _leadUntil;
        float _leadDuck = 1f;
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
        const int NBars = 8;

        public static void Boot(GameObject host)
        {
            if (Live != null) return;
            Live = host.GetComponent<MixDesk>();
            if (Live == null) Live = host.AddComponent<MixDesk>();
            Live.Build();
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
            _stems = new AudioSource[4];
            _stems[0] = MakeLoop(LoadBed("Audio/Bed/dawn-garden", MakeDawn));
            _stems[1] = MakeLoop(LoadBed("Audio/Bed/mid-climb", MakeMid));
            _stems[2] = MakeLoop(LoadBed("Audio/Bed/last-light", MakeLast));
            _stems[3] = MakeLoop(MakeCombo());
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

            float d = SkyCycle.Dusk;
            float day = 1f - Mathf.SmoothStep(0f, 0.42f, d);
            float night = Mathf.SmoothStep(0.38f, 1f, d);
            float dusk = Mathf.Clamp01(1f - Mathf.Abs(d - 0.5f) * 2.15f);
            bool moon = Time.unscaledTime < _moonLiftUntil;
            if (moon) night += 0.35f;
            float sum = day + dusk + night;
            if (sum < 0.001f) { day = 1f; sum = 1f; }
            day /= sum;
            dusk /= sum;
            night /= sum;

            float cap = moon ? PlaceMax : PlaceCap;
            float duck = BedDuck;
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
            var y = new float[n];
            float f0 = Hz(midi);
            float phase = 0f;
            float a = OnePoleA(1180f);
            float lp = 0f;
            float dur = n / (float)Rate;
            float rel = Mathf.Min(0.22f, dur * 0.35f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float env;
                if (u < 0.018f / dur) env = u / (0.018f / dur);
                else if (t < 0.018f + 0.12f)
                {
                    float d = (t - 0.018f) / 0.12f;
                    env = Mathf.Lerp(1f, 0.72f, d);
                }
                else if (t > dur - rel) env = 0.72f * (1f - (t - (dur - rel)) / rel);
                else env = 0.72f;
                env *= 0.88f + 0.12f * Mathf.Sin(2f * Mathf.PI * 1.7f * t + 0.4f);
                float vib = 1f + 0.00347f * Mathf.Sin(2f * Mathf.PI * 4.6f * t);
                phase += 2f * Mathf.PI * f0 * vib / Rate;
                float s = Mathf.Sin(phase) + 0.16f * Mathf.Sin(2f * phase) + 0.03f * Mathf.Sin(3f * phase);
                lp += a * (s * env - lp);
                y[i] = lp;
            }
            return y;
        }

        static float[] PadVoice(int n, float cutoff, float amp, float air)
        {
            var y = new float[n];
            float[] fs = { Hz(50f), Hz(54f), Hz(57f), Hz(62f), Hz(45f) };
            float[] am = { 0.55f, 0.38f, 0.50f, 0.22f, 0.18f };
            float[] det = { 0.997f, 1.003f, 0.9985f, 1.002f, 1.001f };
            float a = OnePoleA(cutoff);
            float a2 = OnePoleA(2400f);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int k = 0; k < 5; k++)
                {
                    float f = fs[k] * det[k];
                    s += am[k] * Mathf.Sin(2f * Mathf.PI * f * t);
                    s += am[k] * 0.07f * Mathf.Sin(2f * Mathf.PI * f * 2f * t);
                }
                s *= 0.78f + 0.22f * Mathf.Sin(2f * Mathf.PI * 0.055f * t + 0.3f);
                if (air > 0f)
                {
                    float airS = 0.6f * Mathf.Sin(2f * Mathf.PI * Hz(62f) * t)
                        + 0.4f * Mathf.Sin(2f * Mathf.PI * Hz(57f) * t);
                    s += air * airS * (0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.08f * t));
                }
                lp += a * (s - lp);
                lp2 += a2 * (lp - lp2);
                y[i] = lp2;
            }
            float peak = 1e-6f;
            for (int i = 0; i < n; i++)
            {
                float v = Mathf.Abs(y[i]);
                if (v > peak) peak = v;
            }
            float g = amp / peak;
            for (int i = 0; i < n; i++) y[i] *= g;
            return y;
        }

        static float[] BassNote(int n, float midi)
        {
            var y = new float[n];
            float f0 = Hz(midi);
            float a = OnePoleA(420f);
            float lp = 0f;
            float dur = n / (float)Rate;
            float rel = Mathf.Min(0.18f, dur * 0.25f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env;
                if (t < 0.012f) env = t / 0.012f;
                else if (t < 0.092f) env = Mathf.Lerp(1f, 0.82f, (t - 0.012f) / 0.08f);
                else if (t > dur - rel) env = 0.82f * Mathf.Max(0f, 1f - (t - (dur - rel)) / rel);
                else env = 0.82f;
                float s = Mathf.Sin(2f * Mathf.PI * f0 * t) + 0.12f * Mathf.Sin(4f * Mathf.PI * f0 * t);
                lp += a * (s * env - lp);
                y[i] = lp;
            }
            return y;
        }

        static float[] Kick()
        {
            int n = Mathf.CeilToInt(0.22f * Rate);
            var y = new float[n];
            float a = OnePoleA(380f);
            float lp = 0f;
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float freq = 68f * Mathf.Exp(-t * 16f) + 36f;
                phase += 2f * Mathf.PI * freq / Rate;
                float env = Mathf.Exp(-t * 11f);
                lp += a * (Mathf.Sin(phase) * env - lp);
                y[i] = lp;
            }
            return y;
        }

        static float[] WoodTick(int seed)
        {
            int n = Mathf.CeilToInt(0.048f * Rate);
            var y = new float[n];
            float a = OnePoleA(1280f);
            float an = OnePoleA(750f);
            float lp = 0f, nz = 0f;
            int h = seed;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 105f);
                h = (h * 1103515245 + 12345) & 0x7fffffff;
                float noise = (h / 1073741824f) - 1f;
                nz += an * (noise - nz);
                float s = Mathf.Sin(2f * Mathf.PI * 485f * t) + 0.55f * nz;
                lp += a * (s * env - lp);
                y[i] = lp;
            }
            return y;
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

        static readonly Note[] CounterOnce =
        {
            new Note(8f, 1.5f, 54f), new Note(9.5f, 1.5f, 57f)
        };

        static readonly Note[] CounterFull =
        {
            new Note(4.5f, 1f, 54f), new Note(5.5f, 1.5f, 57f),
            new Note(9f, 1.5f, 59f), new Note(10.5f, 1.5f, 57f),
            new Note(12.5f, 1.5f, 54f), new Note(14f, 2f, 50f)
        };

        static float ThirdAbove(float m)
        {
            if (m == 62f) return 66f;
            if (m == 64f) return 67f;
            if (m == 66f) return 69f;
            if (m == 69f) return 73f;
            if (m == 57f) return 61f;
            return m;
        }

        static float SixthBelow(float m)
        {
            if (m == 62f) return 54f;
            if (m == 64f) return 55f;
            if (m == 66f) return 57f;
            if (m == 69f) return 61f;
            if (m == 57f) return 49f;
            return m;
        }

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
            int ni = Mathf.Min(Mathf.RoundToInt(0.3f * Rate), frames);
            int no = Mathf.Min(Mathf.RoundToInt(0.8f * Rate), frames);
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

        static AudioClip MakeDawn()
        {
            int n = LoopN();
            var bus = new float[n * 2];
            var pad = PadVoice(n, 340f, 0.22f, 0f);
            Place(bus, pad, 0f, -0.25f, 1f);
            Place(bus, pad, 0f, 0.28f, 0.85f);
            MixNotes(bus, FirstFour, 0.30f, -0.12f, 0f);
            MixNotes(bus, FirstFour, 0.24f, -0.12f, 16f);
            MixNotes(bus, CounterOnce, 0.055f, 0.35f, 0f);
            ApplyFades(bus);
            return PeakClip("dawn-garden", bus, 0.38f);
        }

        static AudioClip MakeMid()
        {
            int n = LoopN();
            var bus = new float[n * 2];
            var pad = PadVoice(n, 500f, 0.10f, 0.10f);
            Place(bus, pad, 0f, -0.22f, 1f);
            Place(bus, pad, 0f, 0.24f, 0.9f);
            MixNotes(bus, Motif, 0.20f, -0.08f, 0f, 16f);
            int half = Mathf.RoundToInt(2f * Beat * Rate);
            var d2 = BassNote(half, 38f);
            var a2 = BassNote(half, 45f);
            var k = Kick();
            for (int bar = 0; bar < NBars; bar++)
            {
                float tBar = bar * 4f * Beat;
                Place(bus, d2, tBar, 0f, 0.28f);
                Place(bus, a2, tBar + 2f * Beat, 0f, 0.24f);
                Place(bus, k, tBar, 0f, 0.18f);
                Place(bus, WoodTick(11 + bar * 17), tBar + 2f * Beat, 0.18f, 0.08f);
            }
            ApplyFades(bus);
            return PeakClip("mid-climb", bus, 0.45f);
        }

        static AudioClip MakeLast()
        {
            int n = LoopN();
            var bus = new float[n * 2];
            var pad = PadVoice(n, 500f, 0.11f, 0.10f);
            Place(bus, pad, 0f, -0.22f, 1f);
            Place(bus, pad, 0f, 0.24f, 0.9f);
            MixNotes(bus, Motif, 0.22f, -0.08f, 0f, 16f);
            var thirds = new Note[Motif.Length];
            var sixths = new Note[Motif.Length];
            for (int i = 0; i < Motif.Length; i++)
            {
                thirds[i] = new Note(Motif[i].Start, Motif[i].Dur, ThirdAbove(Motif[i].Midi));
                sixths[i] = new Note(Motif[i].Start, Motif[i].Dur, SixthBelow(Motif[i].Midi));
            }
            MixNotes(bus, thirds, 0.10f, 0.30f, 0f, 16f);
            MixNotes(bus, sixths, 0.07f, -0.34f, 0f, 16f);
            MixNotes(bus, CounterFull, 0.12f, 0.42f, 0f, 16f);
            int half = Mathf.RoundToInt(2f * Beat * Rate);
            var d2 = BassNote(half, 38f);
            var a2 = BassNote(half, 45f);
            var k = Kick();
            for (int bar = 0; bar < NBars; bar++)
            {
                float tBar = bar * 4f * Beat;
                Place(bus, d2, tBar, 0f, 0.24f);
                Place(bus, a2, tBar + 2f * Beat, 0f, 0.20f);
                Place(bus, k, tBar, 0f, 0.16f);
                Place(bus, k, tBar + 2f * Beat, 0f, 0.12f);
                Place(bus, WoodTick(23 + bar * 19), tBar + 1.5f * Beat, 0.18f, 0.11f);
                Place(bus, WoodTick(41 + bar * 13), tBar + 3.5f * Beat, 0.18f, 0.11f);
            }
            ApplyFades(bus);
            return PeakClip("last-light", bus, 0.56f);
        }

        static AudioClip MakeCombo()
        {
            const float dur = 8f;
            int n = Mathf.CeilToInt(44100 * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / 44100f;
                float s = Mathf.Sin(2f * Mathf.PI * 78f * t) * 0.55f;
                s += Mathf.Sin(2f * Mathf.PI * 117f * t) * 0.38f;
                float env = 0.92f + 0.08f * Mathf.Sin(t * 0.4f);
                data[i] = s * env * 0.08f;
            }
            var clip = AudioClip.Create("combo-fifth", n, 1, 44100, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

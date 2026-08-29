using UnityEngine;

namespace FlockFive
{
    public static class Sfx
    {
        static AudioSource[] _voices;
        static int _v;
        static AudioClip[] _chirps;
        static AudioClip[] _flaps;
        static AudioClip[] _celebrates;
        static AudioClip[] _breaks;
        static AudioClip[] _lifts;
        static AudioClip _deny;
        static AudioClip[] _booms;
        static AudioClip[] _snoozes;
        static AudioClip[] _hums;
        static AudioClip[] _scatters;
        static int _lastChirp = -1;
        static int _lastFlap = -1;
        static int _lastSnooze = -1;
        static int _lastHum = -1;
        static int _lastScatter = -1;
        static int _lastCelebrate = -1;
        static int _lastBreak = -1;
        static int _lastLift = -1;
        static float _humGate;
        static float _flapGate;
        static SfxHost _host;
        const int Rate = 44100;

        public static void Warm() => Ensure();

        static void Ensure()
        {
            if (_voices != null) return;
            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<SfxHost>();
            MixDesk.Boot(go);
            _voices = new AudioSource[20];
            for (int i = 0; i < _voices.Length; i++)
            {
                var a = go.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.spatialBlend = 0f;
                a.volume = 1f;
                _voices[i] = a;
            }
            _chirps = LoadBank("Audio/Select", 10, i => MakeChirp(1100 + i * 97));
            _flaps = new AudioClip[14];
            for (int i = 0; i < _flaps.Length; i++)
                _flaps[i] = MakeFlap(i, 2200 + i * 131);
            _celebrates = null;
            _breaks = LoadBank("Audio/Break", 12, i => MakeBreak(i, 4100 + i * 47));
            _lifts = LoadBank("Audio/Whoosh", 12, i => MakeLift(i, 4700 + i * 43));
            _deny = MakeDeny();
            _snoozes = LoadBank("Audio/Snooze", 12, i => MakeSnooze(i, 5100 + i * 53));
            _hums = new AudioClip[8];
            for (int i = 0; i < _hums.Length; i++)
                _hums[i] = MakeHum(i, 6200 + i * 41);
            _scatters = new AudioClip[8];
            for (int i = 0; i < _scatters.Length; i++)
                _scatters[i] = MakeScatter(i, 7300 + i * 37);
            _booms = new AudioClip[5];
            for (int i = 0; i < _booms.Length; i++)
                _booms[i] = MakeBoom(3400 + i * 71);
        }

        static AudioSource Voice()
        {
            Ensure();
            var a = _voices[_v];
            _v = (_v + 1) % _voices.Length;
            return a;
        }

        static void Shot(AudioClip clip, float pitch, float vol, MixLayer layer, float leadDuck = MixDesk.DuckChirp)
        {
            if (clip == null) return;
            if (layer == MixLayer.Lead)
            {
                if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.55f, leadDuck);
            }
            else if (layer == MixLayer.Mid)
            {
                if (MixDesk.Live != null && !MixDesk.Live.AllowMid) return;
            }
            else if (layer == MixLayer.Bed)
            {
                if (MixDesk.Live != null && !MixDesk.Live.AllowBed) return;
                if (MixDesk.Live != null) vol *= MixDesk.Live.BedDuck;
            }
            var a = Voice();
            a.pitch = pitch;
            a.PlayOneShot(clip, vol);
        }

        public static bool QuietMid => MixDesk.Live == null || MixDesk.Live.AllowMid;

        public static void Chirp()
        {
            Ensure();
            int i = Next(_chirps.Length, ref _lastChirp);
            // Real hummingbird chips: do not pitch them up.
            Shot(_chirps[i], Random.Range(0.99f, 1.01f), 0.66f, MixLayer.Lead);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.7f, MixDesk.DuckChirp);
        }

        public static void Flap() => FlapAt(0.32f, Random.Range(0.94f, 1.03f), 0.02f, MixLayer.Mid);

        public static void FlapSoft() => FlapAt(0.18f, Random.Range(0.95f, 1.02f), 0.03f, MixLayer.Mid);

        public static void FlapHard() => FlapAt(0.62f, Random.Range(0.92f, 1.03f), 0.012f, MixLayer.Lead);

        static void FlapAt(float vol, float pitch, float gate, MixLayer layer)
        {
            Ensure();
            if (Time.unscaledTime - _flapGate < gate) return;
            if (layer == MixLayer.Mid && MixDesk.Live != null && !MixDesk.Live.AllowMid) return;
            _flapGate = Time.unscaledTime;
            int i = Next(_flaps.Length, ref _lastFlap);
            Shot(_flaps[i], pitch, vol, layer);
        }

        public static void Flaps(int n) => FlapTrain(n, 0.05f, 0.5f);

        public static void Takeoff(int n) => FlapTrain(n, 0.042f, 0.66f);

        public static void Land(int n) => FlapTrain(n, 0.058f, 0.58f);

        static void FlapTrain(int n, float gap, float vol)
        {
            Ensure();
            n = Mathf.Clamp(n, 1, 5);
            if (_host == null) return;
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.22f + 0.05f * n, MixDesk.DuckChirp);
            _host.StartCoroutine(FlapTrainCo(n, gap, vol));
        }

        static System.Collections.IEnumerator FlapTrainCo(int n, float gap, float vol)
        {
            for (int k = 0; k < n; k++)
            {
                FlapAt(vol + 0.03f * k, Random.Range(0.93f, 1.03f), 0.01f, MixLayer.Lead);
                if (k < n - 1) yield return new WaitForSeconds(gap);
            }
        }

        public static void GardenWake()
        {
            Ensure();
            if (_host == null) return;
            _host.StartCoroutine(FlapTrainCo(4, 0.07f, 0.38f));
        }

        public static void Celebrate()
        {
            // No gong / Pavlov bell. Flock payoff is whoosh + wood crunch.
        }

        public static void Combo(int size)
        {
            Ensure();
            size = Mathf.Clamp(size, 2, Palette.Max);
            if (_host == null) return;
            _host.StartCoroutine(ComboCo(size));
            if (size >= 3) Rumble();
        }

        static System.Collections.IEnumerator ComboCo(int size)
        {
            yield return new WaitForSeconds(0.06f);
            FeederDone();
            if (size >= 3)
            {
                yield return new WaitForSeconds(0.1f);
                FeederDone();
            }
        }

        public static void Deny()
        {
            Ensure();
            Shot(_deny, Random.Range(0.92f, 1.04f), 0.72f, MixLayer.Lead);
        }

        public static void Crack() => Break();

        public static void Break()
        {
            Ensure();
            int i = Next(_breaks.Length, ref _lastBreak);
            Shot(_breaks[i], Random.Range(0.98f, 1.02f), 1f, MixLayer.Lead, MixDesk.DuckBreak);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.9f, MixDesk.DuckBreak);
            Rumble();
        }

        public static void FeederDone()
        {
            Ensure();
            int i = Next(_lifts.Length, ref _lastLift);
            Shot(_lifts[i], Random.Range(0.98f, 1.02f), 0.64f, MixLayer.Lead, MixDesk.DuckWhoosh);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.32f, MixDesk.DuckWhoosh);
        }

        public static void Sleep()
        {
            Ensure();
            int i = Next(_snoozes.Length, ref _lastSnooze);
            Shot(_snoozes[i], Random.Range(0.98f, 1.02f), 0.64f, MixLayer.Lead);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.5f, MixDesk.DuckChirp);
        }

        public static void Snooze(float vol = 0.28f)
        {
            Ensure();
            if (MixDesk.Live != null && !MixDesk.Live.AllowMid) return;
            int i = Next(_snoozes.Length, ref _lastSnooze);
            Shot(_snoozes[i], Random.Range(0.97f, 1.03f), vol, MixLayer.Mid);
        }

        public static void Flock() => FeederDone();

        public static void Moonrise()
        {
            Ensure();
            if (MixDesk.Live != null) MixDesk.Live.MoonLift();
        }

        public static void Firework()
        {
            Ensure();
            int i = Random.Range(0, _booms.Length);
            Shot(_booms[i], Random.Range(0.94f, 1.03f), 0.7f, MixLayer.Lead);
        }

        public static void Rumble()
        {
            if (!Application.isMobilePlatform) return;
            try { Handheld.Vibrate(); }
            catch (System.Exception) { }
        }

        public static void Buzz() => BeeScatter();

        public static void BeeHum()
        {
            Ensure();
            if (MixDesk.Live != null && !MixDesk.Live.AllowMid) return;
            if (Time.unscaledTime - _humGate < 2.6f) return;
            _humGate = Time.unscaledTime;
            int i = Next(_hums.Length, ref _lastHum);
            Shot(_hums[i], Random.Range(0.94f, 1.03f), Random.Range(0.12f, 0.18f), MixLayer.Mid);
        }

        public static void BeeScatter()
        {
            Ensure();
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.55f, MixDesk.DuckChirp);
            int n = 2;
            for (int k = 0; k < n; k++)
            {
                int i = Next(_scatters.Length, ref _lastScatter);
                Shot(_scatters[i], Random.Range(0.94f, 1.03f), 0.32f + 0.04f * k, MixLayer.Lead);
            }
        }

        static int Next(int n, ref int last)
        {
            int i = Random.Range(0, n);
            if (i == last) i = (i + 1 + Random.Range(0, n - 1)) % n;
            last = i;
            return i;
        }

        static AudioClip[] LoadBank(string path, int fallback, System.Func<int, AudioClip> make)
        {
            var clips = Resources.LoadAll<AudioClip>(path);
            if (clips != null && clips.Length > 0)
            {
                System.Array.Sort(clips, (a, b) => string.CompareOrdinal(a.name, b.name));
                return clips;
            }
            var syn = new AudioClip[fallback];
            for (int i = 0; i < fallback; i++)
                syn[i] = make(i);
            return syn;
        }

        static float Hash(int n)
        {
            n = (n << 13) ^ n;
            return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
        }

        static float Soft(ref float y, int seed, int i, float a)
        {
            y += a * (Hash(seed + i) - y);
            return y;
        }

        static AudioClip MakeChirp(int seed)
        {
            float f0 = Mathf.Lerp(380f, 620f, (Hash(seed) + 1f) * 0.5f);
            float f1 = f0 * Mathf.Lerp(1.06f, 1.18f, (Hash(seed + 3) + 1f) * 0.5f);
            float dur = Mathf.Lerp(0.12f, 0.2f, (Hash(seed + 5) + 1f) * 0.5f);
            float slide = Mathf.Lerp(0.03f, 0.08f, (Hash(seed + 7) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float env = u < 0.12f ? u / 0.12f : Mathf.Pow(1f - (u - 0.12f) / 0.88f, 1.35f);
                float f = Mathf.Lerp(f0, f1, Mathf.SmoothStep(0f, 1f, u));
                f *= 1f + slide * Mathf.Sin(t * 12f);
                float s = Mathf.Sin(2f * Mathf.PI * f * t);
                s += 0.12f * Mathf.Sin(4f * Mathf.PI * f * t);
                data[i] = s * env * 0.26f;
            }
            return Clip("chirp" + seed, data);
        }

        static AudioClip MakeFlap(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.07f, 0.16f, (Hash(seed) + 1f) * 0.5f);
            float thump = Mathf.Lerp(88f, 190f, (Hash(seed + 2) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float noise = Soft(ref lp, seed * 17, i, 0.12f);
                float s;
                switch (kind % 4)
                {
                    case 0:
                    {
                        float e1 = Mathf.Exp(-u * 14f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.45f));
                        float e2 = Mathf.Exp(-(u - 0.38f) * 16f) * Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - 0.32f) / 0.5f)));
                        float env = e1 * 0.7f + e2 * 0.55f;
                        s = noise * env * 0.22f + Mathf.Sin(2f * Mathf.PI * thump * t) * env * 0.72f;
                        break;
                    }
                    case 1:
                    {
                        float a = Mathf.Exp(-u * 16f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.32f));
                        float b = Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - 0.36f) / 0.4f))) * Mathf.Exp(-(u - 0.36f) * 14f);
                        float env = a * 0.75f + b * 0.7f;
                        s = noise * env * 0.18f + Mathf.Sin(2f * Mathf.PI * thump * 1.15f * t) * env * 0.75f;
                        break;
                    }
                    case 2:
                    {
                        float env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 0.7f) * Mathf.Exp(-u * 6f);
                        s = noise * env * 0.28f + Mathf.Sin(2f * Mathf.PI * (thump * 0.55f) * t) * env * 0.55f;
                        break;
                    }
                    default:
                    {
                        float env = Mathf.Exp(-u * 22f) * (u < 0.05f ? u / 0.05f : 1f);
                        s = noise * env * 0.2f + Mathf.Sin(2f * Mathf.PI * (thump * 1.4f) * t) * env * 0.72f;
                        break;
                    }
                }
                data[i] = s * 0.34f;
            }
            return Clip("flap" + seed, data);
        }

        static float Pulse(float freq, float t, float duty)
        {
            float p = freq * t;
            p -= Mathf.Floor(p);
            return p < duty ? 1f : -1f;
        }

        static void PutSweep(float[] data, float start, float len, float f0, float f1, float amp, float duty)
        {
            int n = data.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate - start;
                if (t < 0f || t > len) continue;
                float u = t / len;
                float env = (u < 0.06f ? u / 0.06f : 1f) * Mathf.Pow(1f - u, 1.25f);
                float f = f0 * Mathf.Pow(f1 / Mathf.Max(1f, f0), u);
                data[i] += Pulse(f, t, duty) * env * amp;
            }
        }

        static void PutThump(float[] data, float start, float f0, float amp)
        {
            int n = data.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate - start;
                if (t < 0f || t > 0.12f) continue;
                float u = t / 0.12f;
                float env = (u < 0.04f ? u / 0.04f : 1f) * Mathf.Exp(-u * 9f);
                float f = f0 * (1f - 0.45f * u);
                data[i] += Pulse(f, t, 0.5f) * env * amp;
            }
        }

        static void PutClick(float[] data, float start, int seed, float amp)
        {
            int n = data.Length;
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate - start;
                if (t < 0f || t > 0.018f) continue;
                float env = 1f - t / 0.018f;
                float nz = Soft(ref lp, seed, i, 0.55f);
                data[i] += nz * env * amp;
            }
        }

        static AudioClip MakeCelebrate(int kind, int seed)
        {
            // 8-bit NES jump-hit: pulse sweep + body thump. Harder than a sine arpeggio.
            float dur = 0.42f;
            if (kind == 2 || kind == 6 || kind == 9) dur = 0.52f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            switch (kind % 12)
            {
                case 0: // classic barrel hop
                    PutClick(data, 0f, seed, 0.35f);
                    PutThump(data, 0f, 92f, 0.55f);
                    PutSweep(data, 0.012f, 0.22f, 196f, 523f, 0.48f, 0.25f);
                    break;
                case 1: // lower, fatter
                    PutClick(data, 0f, seed + 3, 0.3f);
                    PutThump(data, 0f, 74f, 0.62f);
                    PutSweep(data, 0.01f, 0.26f, 147f, 392f, 0.5f, 0.25f);
                    break;
                case 2: // double hop
                    PutThump(data, 0f, 88f, 0.5f);
                    PutSweep(data, 0.01f, 0.16f, 175f, 440f, 0.42f, 0.25f);
                    PutClick(data, 0.18f, seed + 7, 0.28f);
                    PutThump(data, 0.18f, 100f, 0.42f);
                    PutSweep(data, 0.19f, 0.2f, 220f, 523f, 0.46f, 0.25f);
                    break;
                case 3: // octave slap
                    PutClick(data, 0f, seed + 11, 0.4f);
                    PutThump(data, 0f, 82f, 0.58f);
                    PutSweep(data, 0.008f, 0.18f, 131f, 262f, 0.5f, 0.5f);
                    PutSweep(data, 0.05f, 0.16f, 262f, 523f, 0.32f, 0.25f);
                    break;
                case 4: // NES stair (period jumps)
                    PutThump(data, 0f, 96f, 0.5f);
                    PutSweep(data, 0.00f, 0.07f, 196f, 196f, 0.42f, 0.25f);
                    PutSweep(data, 0.07f, 0.07f, 262f, 262f, 0.44f, 0.25f);
                    PutSweep(data, 0.14f, 0.16f, 330f, 330f, 0.46f, 0.25f);
                    break;
                case 5: // boing
                    PutClick(data, 0f, seed + 17, 0.32f);
                    PutThump(data, 0f, 70f, 0.6f);
                    PutSweep(data, 0.01f, 0.12f, 165f, 440f, 0.48f, 0.125f);
                    PutSweep(data, 0.12f, 0.16f, 440f, 247f, 0.36f, 0.125f);
                    break;
                case 6: // stutter hops
                    PutThump(data, 0f, 85f, 0.45f);
                    PutSweep(data, 0.00f, 0.09f, 196f, 330f, 0.4f, 0.25f);
                    PutSweep(data, 0.10f, 0.09f, 220f, 370f, 0.42f, 0.25f);
                    PutSweep(data, 0.20f, 0.14f, 247f, 494f, 0.46f, 0.25f);
                    break;
                case 7: // heavy body
                    PutClick(data, 0f, seed + 23, 0.45f);
                    PutThump(data, 0f, 58f, 0.72f);
                    PutThump(data, 0.02f, 110f, 0.4f);
                    PutSweep(data, 0.02f, 0.24f, 123f, 349f, 0.5f, 0.5f);
                    break;
                case 8: // 25% + thin 12.5% layer
                    PutThump(data, 0f, 90f, 0.52f);
                    PutSweep(data, 0.01f, 0.22f, 175f, 466f, 0.46f, 0.25f);
                    PutSweep(data, 0.04f, 0.18f, 220f, 523f, 0.22f, 0.125f);
                    break;
                case 9: // land then hop
                    PutClick(data, 0f, seed + 29, 0.38f);
                    PutThump(data, 0f, 64f, 0.68f);
                    PutThump(data, 0.08f, 88f, 0.4f);
                    PutSweep(data, 0.1f, 0.24f, 147f, 392f, 0.5f, 0.25f);
                    break;
                case 10: // wide slow rise
                    PutThump(data, 0f, 78f, 0.58f);
                    PutSweep(data, 0.01f, 0.3f, 110f, 349f, 0.5f, 0.25f);
                    break;
                default: // stacked duties
                    PutClick(data, 0f, seed + 31, 0.34f);
                    PutThump(data, 0f, 86f, 0.55f);
                    PutSweep(data, 0.01f, 0.2f, 196f, 494f, 0.4f, 0.5f);
                    PutSweep(data, 0.01f, 0.2f, 196f, 494f, 0.28f, 0.25f);
                    break;
            }

            for (int i = 0; i < n; i++)
            {
                float x = data[i] * 1.55f;
                data[i] = x / (1f + Mathf.Abs(x));
            }
            return ClipPunch("neshop" + kind, data);
        }

        static AudioClip MakeDeny()
        {
            int n = Mathf.CeilToInt(Rate * 0.16f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / 0.16f;
                float f = Mathf.Lerp(220f, 110f, u);
                float env = (1f - u) * (1f - u);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.28f;
            }
            return Clip("deny", data);
        }

        static AudioClip MakeBreak(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.28f, 0.48f, (Hash(seed) + 1f) * 0.5f);
            if (kind % 4 == 2) dur += 0.12f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float thunk = Mathf.Lerp(42f, 78f, (Hash(seed + 2) + 1f) * 0.5f);
            float snap = Mathf.Lerp(160f, 280f, (Hash(seed + 5) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float air = Soft(ref lp, seed, i, 0.08f);
                float s;
                switch (kind % 6)
                {
                    case 0: // deep thunk then snap
                    {
                        float body = Mathf.Sin(2f * Mathf.PI * thunk * t * (1f - u * 0.35f)) * Mathf.Exp(-u * 6.5f);
                        float crack = Mathf.Sin(2f * Mathf.PI * snap * t) * Mathf.Exp(-(u - 0.08f) * 18f) * (u > 0.06f ? 1f : 0f);
                        s = body * 0.85f + crack * 0.45f + air * 0.12f * Mathf.Exp(-u * 10f);
                        break;
                    }
                    case 1: // double snap
                    {
                        float a = Mathf.Sin(2f * Mathf.PI * snap * t) * Mathf.Exp(-u * 16f);
                        float b = Mathf.Sin(2f * Mathf.PI * (snap * 0.78f) * t) * Mathf.Exp(-(u - 0.09f) * 14f) * (u > 0.08f ? 1f : 0f);
                        float body = Mathf.Sin(2f * Mathf.PI * thunk * t) * Mathf.Exp(-u * 8f);
                        s = body * 0.7f + a * 0.5f + b * 0.4f + air * 0.1f * Mathf.Exp(-u * 9f);
                        break;
                    }
                    case 2: // creak then crunch
                    {
                        float creak = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 70f, u) * t) * (u < 0.45f ? 0.45f : 0f);
                        float body = Mathf.Sin(2f * Mathf.PI * thunk * t) * Mathf.Exp(-(u - 0.38f) * 7f) * (u > 0.35f ? 1f : 0f);
                        float crack = Mathf.Sin(2f * Mathf.PI * snap * t) * Mathf.Exp(-(u - 0.4f) * 16f) * (u > 0.38f ? 1f : 0f);
                        s = creak + body * 0.8f + crack * 0.5f + air * 0.1f * Mathf.Exp(-u * 8f);
                        break;
                    }
                    case 3: // chunky three-hit
                    {
                        float h0 = Mathf.Exp(-u * 18f);
                        float h1 = Mathf.Exp(-(u - 0.07f) * 16f) * (u > 0.06f ? 1f : 0f);
                        float h2 = Mathf.Exp(-(u - 0.15f) * 12f) * (u > 0.14f ? 1f : 0f);
                        s = Mathf.Sin(2f * Mathf.PI * thunk * t) * (h0 * 0.7f + h1 * 0.55f + h2 * 0.4f);
                        s += Mathf.Sin(2f * Mathf.PI * snap * t) * h1 * 0.35f;
                        s += air * 0.1f * Mathf.Exp(-u * 9f);
                        break;
                    }
                    case 4: // fat wood pop
                    {
                        float body = Mathf.Sin(2f * Mathf.PI * (thunk * 0.85f) * t * (1f - u * 0.5f)) * Mathf.Exp(-u * 5.5f);
                        float pop = Mathf.Sin(2f * Mathf.PI * (snap * 0.7f) * t) * Mathf.Exp(-u * 11f);
                        s = body * 0.9f + pop * 0.4f + air * 0.14f * Mathf.Exp(-u * 7f);
                        break;
                    }
                    default: // splinter
                    {
                        float body = Mathf.Sin(2f * Mathf.PI * thunk * t) * Mathf.Exp(-u * 7f);
                        float crack = Mathf.Sin(2f * Mathf.PI * snap * 1.1f * t) * Mathf.Abs(Mathf.Sin(t * 28f)) * Mathf.Exp(-u * 13f);
                        s = body * 0.75f + crack * 0.4f + air * 0.16f * Mathf.Exp(-u * 8f);
                        break;
                    }
                }
                data[i] = Mathf.Clamp(s * 0.48f, -0.95f, 0.95f);
            }
            return Clip("break" + seed, data);
        }

        static AudioClip MakeLift(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.32f, 0.48f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(140f, 220f, (Hash(seed + 2) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.7f)) * Mathf.Exp(-u * 1.8f);
                float air = Soft(ref lp, seed, i, 0.07f);
                float f = Mathf.Lerp(f0, f0 * 1.18f, u);
                float s;
                switch (kind % 4)
                {
                    case 0:
                        s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.75f + air * 0.12f;
                        break;
                    case 1:
                        s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.55f;
                        s += Mathf.Sin(2f * Mathf.PI * (f * 1.25f) * t) * 0.22f;
                        s += air * 0.1f;
                        break;
                    case 2:
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.8f, f0 * 1.1f, u * u) * t);
                        s += air * 0.14f;
                        break;
                    default:
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.9f) * t) * (1f - u * 0.3f);
                        s += air * 0.1f * (1f - u);
                        break;
                }
                data[i] = s * env * 0.28f;
            }
            return Clip("lift" + seed, data);
        }

        static AudioClip MakeSnooze(int kind, int seed)
        {
            float dur = kind == 3 ? 0.55f : Mathf.Lerp(0.22f, 0.42f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(180f, 320f, (Hash(seed + 2) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float env = Mathf.Sin(Mathf.PI * u);
                float air = Soft(ref lp, seed, i, 0.08f);
                float s = 0f;
                switch (kind % 10)
                {
                    case 0: // dove coo
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0, f0 * 0.82f, u) * t);
                        s += 0.28f * Mathf.Sin(4f * Mathf.PI * f0 * 0.82f * t);
                        env *= u < 0.45f ? 1f : 0.55f + 0.45f * Mathf.Sin((u - 0.45f) / 0.55f * Mathf.PI);
                        break;
                    case 1: // falling sigh
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 1.15f, f0 * 0.62f, u) * t);
                        s += 0.06f * air;
                        break;
                    case 2: // tiny peep
                        env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.55f));
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 1.15f) * t);
                        s += 0.1f * Mathf.Sin(4f * Mathf.PI * f0 * 1.15f * t);
                        break;
                    case 3: // soft snore
                    {
                        float f = 70f + 18f * Mathf.Sin(t * 6f);
                        s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.82f;
                        s += air * 0.12f * (0.5f + 0.5f * Mathf.Sin(t * 8f));
                        env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.4f);
                        break;
                    }
                    case 4: // sleepy trill
                        s = Mathf.Sin(2f * Mathf.PI * (f0 + 18f * Mathf.Sin(t * 11f)) * t);
                        break;
                    case 5: // double coo
                    {
                        float gate = u < 0.42f ? Mathf.Sin(u / 0.42f * Mathf.PI) : (u > 0.52f ? Mathf.Sin((u - 0.52f) / 0.48f * Mathf.PI) : 0f);
                        env = gate;
                        float ff = u < 0.45f ? f0 : f0 * 0.88f;
                        s = Mathf.Sin(2f * Mathf.PI * ff * t) + 0.22f * Mathf.Sin(4f * Mathf.PI * ff * t);
                        break;
                    }
                    case 6: // breath
                        s = air * 0.22f + Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.7f;
                        env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.6f);
                        break;
                    case 7: // whistle down
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 1.12f, f0 * 0.82f, u * u) * t);
                        break;
                    case 8: // hmm
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.55f) * t);
                        s += 0.4f * Mathf.Sin(2f * Mathf.PI * (f0 * 0.82f) * t);
                        break;
                    default: // three little notes
                    {
                        float[] notes = { f0, f0 * 1.12f, f0 * 0.9f };
                        int ni = u < 0.33f ? 0 : (u < 0.66f ? 1 : 2);
                        float local = (u % 0.33f) / 0.33f;
                        env = Mathf.Sin(Mathf.PI * local) * 0.85f;
                        s = Mathf.Sin(2f * Mathf.PI * notes[ni] * t);
                        break;
                    }
                }
                data[i] = s * env * 0.2f;
            }
            return Clip("snooze" + kind, data);
        }

        static AudioClip MakeHum(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.16f, 0.28f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(160f, 280f, (Hash(seed + 3) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.15f);
                float noise = Soft(ref lp, seed * 5, i, 0.1f);
                float s;
                switch (kind % 8)
                {
                    case 0: // warm bumble
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.55f + 12f * Mathf.Sin(t * 18f)) * t);
                        s += noise * 0.18f;
                        break;
                    case 1: // wing whir
                        s = noise * (0.22f + 0.18f * Mathf.Sin(2f * Mathf.PI * 72f * t));
                        s += Mathf.Sin(2f * Mathf.PI * f0 * 0.4f * t) * 0.7f;
                        break;
                    case 2: // golden pip
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.95f) * t);
                        s += 0.12f * Mathf.Sin(4f * Mathf.PI * f0 * 0.95f * t);
                        env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 0.6f));
                        break;
                    case 3: // two-tone hum
                        s = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.55f;
                        s += Mathf.Sin(2f * Mathf.PI * (f0 * 1.25f) * t) * 0.4f;
                        s += noise * 0.1f;
                        break;
                    case 4: // airy zip
                        s = noise * 0.16f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.85f, f0 * 1.08f, u) * t) * 0.7f;
                        break;
                    case 5: // flutter
                        s = noise * 0.12f * Mathf.Abs(Mathf.Sin(t * 22f));
                        s += Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.7f;
                        break;
                    case 6: // round drone
                        s = Mathf.Sin(2f * Mathf.PI * (190f + 20f * Mathf.Sin(t * 9f)) * t);
                        s += noise * 0.16f;
                        break;
                    default: // bright tick-hum
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.9f + 12f * Mathf.Sin(t * 10f)) * t);
                        s += noise * 0.08f;
                        break;
                }
                data[i] = s * env * 0.2f;
            }
            return Clip("hum" + kind, data);
        }

        static AudioClip MakeScatter(int kind, int seed)
        {
            float dur = Mathf.Lerp(0.22f, 0.38f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float f0 = Mathf.Lerp(220f, 360f, (Hash(seed + 4) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = Mathf.Clamp01(t / dur);
                float env = Mathf.Pow(1f - u, 1.2f) * (u < 0.08f ? u / 0.08f : 1f);
                float noise = Soft(ref lp, seed * 7, i, 0.1f);
                float s;
                switch (kind % 8)
                {
                    case 0: // whoosh up
                        s = noise * 0.14f;
                        s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.7f, f0 * 1.12f, u) * t) * 0.72f;
                        break;
                    case 1: // sparkle ticks
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.95f) * t) * Mathf.Abs(Mathf.Sin(t * 18f));
                        s += noise * 0.08f;
                        break;
                    case 2: // lift chord
                        s = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.62f;
                        s += Mathf.Sin(2f * Mathf.PI * f0 * 1.25f * t) * 0.22f;
                        break;
                    case 3: // airy fade
                        s = noise * 0.12f * (0.4f + 0.6f * u);
                        s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.8f, f0 * 1.05f, u) * t) * 0.7f;
                        env = Mathf.Sin(Mathf.PI * u);
                        break;
                    case 4: // low buzz recede
                        s = noise * 0.18f;
                        s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.45f, f0 * 0.28f, u) * t) * 0.7f;
                        env = Mathf.Pow(1f - u, 1.1f) * (u < 0.08f ? u / 0.08f : 1f);
                        break;
                    case 5: // zip away
                        s = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.75f, f0 * 1.1f, u * u) * t);
                        s += noise * 0.1f * (1f - u);
                        break;
                    case 6: // whoosh away
                        s = noise * 0.16f * (1f - u);
                        s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(f0 * 0.55f, f0 * 0.32f, u) * t) * 0.68f;
                        env = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.65f)) * Mathf.Exp(-u * 1.4f);
                        break;
                    default: // round drone
                        s = Mathf.Sin(2f * Mathf.PI * (f0 * 0.4f + 8f * Mathf.Sin(t * 7f)) * t) * 0.7f;
                        s += noise * 0.14f;
                        env = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 1.1f);
                        break;
                }
                data[i] = s * env * 0.24f;
            }
            return Clip("scatter" + kind, data);
        }

        static AudioClip MakeBoom(int seed)
        {
            float dur = Mathf.Lerp(0.28f, 0.42f, (Hash(seed) + 1f) * 0.5f);
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float thump = Mathf.Lerp(48f, 88f, (Hash(seed + 2) + 1f) * 0.5f);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float env = Mathf.Exp(-u * 7f);
                float crack = Soft(ref lp, seed * 11, i, 0.16f) * Mathf.Exp(-u * 11f);
                float body = Mathf.Sin(2f * Mathf.PI * thump * t * (1f - u * 0.4f)) * env;
                data[i] = (body * 0.82f + crack * 0.18f) * 0.36f;
            }
            return Clip("boom" + seed, data);
        }

        static AudioClip MakeMoonrise()
        {
            float dur = 1.35f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float[] notes = { 392f, 523.25f, 659.25f };
            float[] at = { 0f, 0.28f, 0.58f };
            for (int k = 0; k < notes.Length; k++)
            {
                float f = notes[k];
                float start = at[k];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate - start;
                    if (t < 0f) continue;
                    float env = Mathf.Exp(-t * 1.8f) * (t < 0.02f ? t / 0.02f : 1f);
                    float s = Mathf.Sin(2f * Mathf.PI * f * t);
                    s += 0.12f * Mathf.Sin(4f * Mathf.PI * f * t);
                    data[i] += s * env * 0.14f;
                }
            }
            return Clip("moon", data);
        }

        static AudioClip Clip(string name, float[] data) => ClipLp(name, data, 0.2f);

        static AudioClip ClipPunch(string name, float[] data) => ClipLp(name, data, 0.42f);

        static AudioClip ClipLp(string name, float[] data, float a)
        {
            float y = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                y += a * (data[i] - y);
                data[i] = Mathf.Clamp(y, -0.95f, 0.95f);
            }
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }

    sealed class SfxHost : MonoBehaviour { }
}

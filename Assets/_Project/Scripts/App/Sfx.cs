using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioSource[] _voices;
        static int _v;
        static AudioClip[] _chirps;
        static AudioClip[] _flaps;
        static AudioClip[] _celebrates;
        static AudioClip[] _breaks;
        static AudioClip[] _lifts;
        static AudioClip[] _chings;
        static AudioClip _deny;
        static AudioClip[] _booms;
        static AudioClip[] _snoozes;
        static AudioClip[] _hums;
        static AudioClip[] _scatters;
        static int _lastFlap = -1;
        static int _lastSnooze = -1;
        static int _lastHum = -1;
        static int _lastScatter = -1;
        static int _lastCelebrate = -1;
        static int _lastBreak = -1;
        static int _lastLift = -1;
        static int _lastChing = -1;
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
            _chirps = LoadVoices();
            _flaps = new AudioClip[14];
            for (int i = 0; i < _flaps.Length; i++)
                _flaps[i] = MakeFlap(i, 2200 + i * 131);
            _celebrates = null;
            _breaks = LoadBank("Audio/Break", 12, i => MakeBreak(i, 4100 + i * 47));
            _lifts = LoadBank("Audio/Whoosh", 12, i => MakeLift(i, 4700 + i * 43));
            _chings = LoadBank("Audio/Ching", 12, MakeChing);
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

        public static void Chirp() => Chirp(BirdColor.Ruby);

        public static void Chirp(BirdColor c)
        {
            Ensure();
            int i = (int)c;
            if (i < 0 || i >= _chirps.Length) i = 0;
            Shot(_chirps[i], 1f, 0.66f, MixLayer.Lead);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.7f, MixDesk.DuckChirp);
        }

        static AudioClip[] LoadVoices()
        {
            var clips = new AudioClip[Palette.Max];
            var bank = Resources.LoadAll<AudioClip>("Audio/Select");
            if (bank != null && bank.Length > 0)
                System.Array.Sort(bank, (a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < clips.Length; i++)
            {
                var col = (BirdColor)i;
                string n = VoiceName(col);
                clips[i] = Resources.Load<AudioClip>("Audio/Select/" + n)
                        ?? Resources.Load<AudioClip>("Audio/Chirp/" + n);
                if (clips[i] == null && bank != null)
                {
                    for (int k = 0; k < bank.Length; k++)
                    {
                        if (bank[k] == null) continue;
                        if (bank[k].name.ToLowerInvariant().Contains(n))
                        {
                            clips[i] = bank[k];
                            break;
                        }
                    }
                }
                // One real chip per color from the 12-clip NPS bank.
                // Ruby 00, Gold 01, Teal 02, Violet 03, Peach 04. Never synth.
                if (clips[i] == null && bank != null && i < bank.Length)
                    clips[i] = bank[i];
            }
            return clips;
        }

        static string VoiceName(BirdColor c)
        {
            switch (c)
            {
                case BirdColor.Ruby: return "ruby";
                case BirdColor.Gold: return "gold";
                case BirdColor.Teal: return "teal";
                case BirdColor.Peach: return "peach";
                default: return "violet";
            }
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
            if (MixDesk.Live != null) MixDesk.Live.ComboWarm();
            if (_host == null) return;
            _host.StartCoroutine(ComboCo(size));
            if (size >= 3) Rumble();
        }

        static System.Collections.IEnumerator ComboCo(int size)
        {
            yield return new WaitForSeconds(0.06f);
            FeederLeave();
            if (size >= 3)
            {
                yield return new WaitForSeconds(0.1f);
                FeederLeave();
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
            int i = Next(_chings.Length, ref _lastChing);
            Shot(_chings[i], Random.Range(0.98f, 1.02f), 0.64f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.4f, MixDesk.DuckChirp);
        }

        public static void FeederLeave()
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

        public static void Flock() => FeederLeave();

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
    }

    sealed class SfxHost : MonoBehaviour { }
}

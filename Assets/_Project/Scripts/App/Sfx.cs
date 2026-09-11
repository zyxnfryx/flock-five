using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioSource[] _voices;
        static int _v;
        static AudioClip[] _chirps;
        static AudioClip[] _flaps;
        static AudioClip[] _flutters;
        static AudioClip[] _breaks;
        static AudioClip[] _lifts;
        static AudioClip[] _chings;
        static AudioClip[] _clinks;
        static AudioClip[] _rattles;
        static AudioClip[] _yells;
        static AudioClip[] _pops;
        static AudioClip[] _jingles;
        static AudioClip _deny;
        static AudioClip _pageTurn;
        static AudioClip[] _booms;
        static AudioClip[] _snoozes;
        static AudioClip[] _hums;
        static AudioClip[] _scatters;
        static AudioClip[] _thunders;
        static int _lastFlap = -1;
        static int _lastSnooze = -1;
        static int _lastHum = -1;
        static int _lastScatter = -1;
        static int _lastThunder = -1;
        static int _lastBreak = -1;
        static int _lastLift = -1;
        static int _lastChing = -1;
        static int _lastClink = -1;
        static int _lastRattle = -1;
        static int _lastYell = -1;
        static float _humGate;
        static float _flapGate;
        static SfxHost _host;
        const int Rate = 44100;

        public static void Warm() => Ensure();

        static bool VoicesAlive()
        {
            if (_voices == null || _host == null) return false;
            for (int i = 0; i < _voices.Length; i++)
                if (_voices[i] == null) return false;
            return true;
        }

        static void Ensure()
        {
            // Domain-reload-off: static array can outlive destroyed AudioSources.
            if (VoicesAlive()) return;
            _voices = null;
            _host = null;
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
            _flaps = new AudioClip[8];
            for (int i = 0; i < _flaps.Length; i++)
                _flaps[i] = MakeFlap(i, 2200 + i * 131);
            _flutters = new AudioClip[10];
            for (int i = 0; i < _flutters.Length; i++)
                _flutters[i] = MakeFlutter(i, 3300 + i * 47);
            _breaks = LoadBank("Audio/Break", 12, i => MakeBreak(i, 4100 + i * 47));
            _lifts = LoadBank("Audio/Whoosh", 12, i => MakeLift(i, 4700 + i * 43));
            _chings = LoadBank("Audio/Ching", 12, MakeChing);
            _clinks = LoadBank("Audio/Coin", 12, MakeCoin);
            _rattles = LoadBank("Audio/Rattle", 20, MakeFeederRattle);
            _yells = LoadBank("Audio/Yell", 6, MakeSparrowYell);
            _pops = new AudioClip[5];
            for (int i = 0; i < _pops.Length; i++)
                _pops[i] = MakePop(i, 8800 + i * 29);
            _jingles = new AudioClip[7];
            for (int i = 0; i < _jingles.Length; i++)
                _jingles[i] = MakeComboJingle(i + 2);
            _deny = MakeDeny();
            _pageTurn = MakePageTurn();
            _snoozes = LoadBank("Audio/Snooze", 12, i => MakeSnooze(i, 5100 + i * 53));
            _hums = new AudioClip[8];
            for (int i = 0; i < _hums.Length; i++)
                _hums[i] = MakeHum(i, 6200 + i * 41);
            _scatters = new AudioClip[12];
            for (int i = 0; i < _scatters.Length; i++)
                _scatters[i] = MakeScatter(i, 7300 + i * 37);
            _booms = new AudioClip[5];
            for (int i = 0; i < _booms.Length; i++)
                _booms[i] = MakeBoom(3400 + i * 71);
            _thunders = new AudioClip[12];
            for (int i = 0; i < _thunders.Length; i++)
                _thunders[i] = MakeThunder(i, 2800 + i * 67);
            var gated = Resources.Load<AudioClip>("Audio/Gate/gate_go");
            _gate = gated != null ? gated : MakeGate();
            _oink = MakeOink();
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
            if (_chirps == null || _chirps.Length == 0) return;
            int i = (int)c;
            if (i < 0 || i >= _chirps.Length) i = 0;
            Shot(_chirps[i], 1f, 0.72f, MixLayer.Lead);
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

        public static void FlapHard() => PlayFlap(1f, Random.Range(0.97f, 1.03f), MixLayer.Lead);

        static void FlapAt(float vol, float pitch, float gate, MixLayer layer)
        {
            Ensure();
            if (Time.unscaledTime - _flapGate < gate) return;
            if (layer == MixLayer.Mid && MixDesk.Live != null && !MixDesk.Live.AllowMid) return;
            _flapGate = Time.unscaledTime;
            PlayFlap(vol, pitch, layer);
        }

        static void PlayFlap(float vol, float pitch, MixLayer layer)
        {
            Ensure();
            if (_flaps == null || _flaps.Length == 0) return;
            int i = Next(_flaps.Length, ref _lastFlap);
            Shot(_flaps[i], pitch, vol, layer, MixDesk.DuckWhoosh);
        }

        public static void Flaps(int n) => FlockFlutter(n);

        public static void Takeoff(int n) => FlockFlutter(n);

        public static void Land(int n) { }

        public static void FlockFlutter(int birds, bool settle = false)
        {
            Ensure();
            birds = Mathf.Clamp(birds, 1, 5);
            if (_host == null) return;
            if (MixDesk.Live != null)
                MixDesk.Live.MarkLead(0.4f + 0.05f * birds, 0.84f);
            _host.StartCoroutine(FlockFlutterCo(birds, settle));
        }

        static System.Collections.IEnumerator FlockFlutterCo(int birds, bool settle)
        {
            float stagger = 0.12f;
            float vol = settle ? 0.48f : 0.66f;
            for (int b = 0; b < birds; b++)
            {
                PlayFlutter(vol, Random.Range(0.93f, 0.98f));
                if (b < birds - 1) yield return new WaitForSeconds(stagger);
            }
        }

        static void PlayFlutter(float vol, float pitch)
        {
            Ensure();
            if (_flutters == null || _flutters.Length == 0) return;
            int i = Next(_flutters.Length, ref _lastFlap);
            Shot(_flutters[i], pitch, vol, MixLayer.Lead, 0.84f);
        }

        public static void GardenWake()
        {
            Ensure();
            if (_host == null) return;
            _host.StartCoroutine(WakeFlaps());
        }

        static System.Collections.IEnumerator WakeFlaps()
        {
            yield return new WaitForSeconds(0.18f);
            PlayFlap(0.22f, 1f, MixLayer.Mid);
            yield return new WaitForSeconds(0.14f);
            PlayFlap(0.16f, 1.02f, MixLayer.Mid);
        }

        public static void Celebrate()
        {
            // No gong / Pavlov bell. Flock payoff is whoosh + wood crunch.
        }

        public static void Combo(int size)
        {
            Ensure();
            size = Mathf.Clamp(size, 2, Palette.ComboMax);
            if (MixDesk.Live != null) MixDesk.Live.ComboWarm();
            int i = Mathf.Min(size, 8) - 2;
            float pitch = size <= 8 ? 1f : Mathf.Min(1.04f, 1f + 0.008f * (size - 8));
            if (_jingles != null && i >= 0 && i < _jingles.Length)
            {
                Shot(_jingles[i], pitch, 0.76f, MixLayer.Lead, MixDesk.DuckWhoosh);
                if (MixDesk.Live != null)
                    MixDesk.Live.MarkLead(0.42f + 0.14f * i, MixDesk.DuckWhoosh);
            }
            if (size >= 3) Rumble();
        }

        public static void Deny()
        {
            Ensure();
            Shot(_deny, Random.Range(0.92f, 1.04f), 0.72f, MixLayer.Lead);
        }

        public static void PageTurn()
        {
            Ensure();
            Shot(_pageTurn, Random.Range(0.96f, 1.04f), 0.78f, MixLayer.Lead);
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

        public static void Clink()
        {
            Ensure();
            if (_clinks == null || _clinks.Length == 0) return;
            int i = Next(_clinks.Length, ref _lastClink);
            Shot(_clinks[i], Random.Range(0.98f, 1.04f), 0.78f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.18f, MixDesk.DuckChirp);
        }

        // Feeder poke / sparrow perch rattle — glass+wood pool, not Deny/Clink/Ching.
        public static void FeederRattle()
        {
            Ensure();
            if (_rattles == null || _rattles.Length == 0) return;
            int i = Next(_rattles.Length, ref _lastRattle);
            Shot(_rattles[i], Random.Range(0.98f, 1.04f), Random.Range(0.70f, 0.78f), MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(Random.Range(0.22f, 0.30f), MixDesk.DuckChirp);
        }

        // Collect smack / sparrow yell — harsh short squawk (visual elsewhere).
        public static void SparrowYell()
        {
            Ensure();
            if (_yells == null || _yells.Length == 0) return;
            int i = Next(_yells.Length, ref _lastYell);
            Shot(_yells[i], Random.Range(0.98f, 1.04f), Random.Range(0.75f, 0.85f), MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.28f, MixDesk.DuckChirp);
        }

        // Hawk cry stub — reuse SparrowYell bank for now (MP: deeper hawk cry later).
        public static void HawkCry() => SparrowYell();

        public static void ScorePop(int i)
        {
            Ensure();
            if (_pops == null || _pops.Length == 0) return;
            int k = Mathf.Clamp(i, 0, _pops.Length - 1);
            Shot(_pops[k], 1f, 0.70f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.2f, MixDesk.DuckChirp);
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
            Shot(_snoozes[i], Random.Range(0.98f, 1.02f), 0.82f, MixLayer.Lead);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.5f, MixDesk.DuckChirp);
        }

        public static void Snooze(float vol = 0.40f)
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

        // Distant garden rumble + crack variety. Mid, never Lead. Skips if a hop is speaking.
        public static bool Thunder()
        {
            Ensure();
            if (MixDesk.Live != null && !MixDesk.Live.AllowMid) return false;
            if (_thunders == null || _thunders.Length == 0) return false;
            int i = Next(_thunders.Length, ref _lastThunder);
            // Far rolls quieter; close cracks a touch hotter — still under chirps.
            float near = (i % 3 == 0) ? 1f : 0f;
            float vol = Mathf.Lerp(0.42f, 0.72f, near * 0.55f + Random.value * 0.45f);
            float pitch = Random.Range(0.88f, 1.04f); // skill: no pitch-up past ~1.04
            Shot(_thunders[i], pitch, vol, MixLayer.Mid);
            return true;
        }

        public static void Buzz() => BeeScatter();

        public static void BeeHum()
        {
            Ensure();
            if (MixDesk.Live != null && !MixDesk.Live.AllowMid) return;
            if (Time.unscaledTime - _humGate < 2.6f) return;
            _humGate = Time.unscaledTime;
            int i = Next(_hums.Length, ref _lastHum);
            Shot(_hums[i], Random.Range(0.94f, 1.03f), Random.Range(0.28f, 0.36f), MixLayer.Mid);
        }

        public static void BeeScatter()
        {
            Ensure();
            if (_scatters == null || _scatters.Length == 0) return;
            int i = Next(_scatters.Length, ref _lastScatter);
            Shot(_scatters[i], Random.Range(0.98f, 1.02f), 0.70f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.32f, MixDesk.DuckChirp);
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

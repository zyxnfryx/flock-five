using UnityEngine;

namespace FlockFive
{
    public static partial class Sfx
    {
        static AudioSource[] _voices;
        static int _v;
        // Clip banks build on first use (or one clip per splash frame via WarmStep), not
        // all at once in Start. Same Make* calls and seeds as before, so every clip is
        // bit-identical; only when it is made moves. Each name below is a property, so
        // every existing play site still reads a complete bank.
        static readonly SfxBank _chirpsB = new SfxBank(LoadVoices);
        static readonly SfxBank _flapsB = new SfxBank(null, 8, i => MakeFlap(i, 2200 + i * 131));
        static readonly SfxBank _fluttersB = new SfxBank(null, 10, i => MakeFlutter(i, 3300 + i * 47));
        static readonly SfxBank _breaksB = new SfxBank("Audio/Break", 12, i => MakeBreak(i, 4100 + i * 47));
        static readonly SfxBank _liftsB = new SfxBank("Audio/Whoosh", 12, i => MakeLift(i, 4700 + i * 43));
        static readonly SfxBank _chingsB = new SfxBank("Audio/Ching", 12, MakeChing);
        static readonly SfxBank _clinksB = new SfxBank("Audio/Coin", 12, MakeCoin);
        static readonly SfxBank _rattlesB = new SfxBank("Audio/Rattle", 20, MakeFeederRattle);
        static readonly SfxBank _yellsB = new SfxBank("Audio/Yell", 6, MakeSparrowYell);
        static readonly SfxBank _hawksB = new SfxBank("Audio/Hawk", 8, MakeHawkCry);
        static readonly SfxBank _oinksB = new SfxBank("Audio/Oink", 8, MakeOink);
        static readonly SfxBank _popsB = new SfxBank("Audio/Pop", 12, i => MakePop(i, 8800 + i * 29));
        static readonly SfxBank _jinglesB = new SfxBank(null, 7, i => MakeComboJingle(i + 2));
        static readonly SfxBank _denyB = new SfxBank(null, 1, i => MakeDeny());
        static readonly SfxBank _pageTurnB = new SfxBank(null, 1, i => MakePageTurn());
        static readonly SfxBank _betInB = new SfxBank(null, 6, MakeCoinIn);
        static readonly SfxBank _betOutB = new SfxBank(null, 6, MakeCoinOut);
        static readonly SfxBank _snoozesB = new SfxBank("Audio/Snooze", 12, i => MakeSnooze(i, 5100 + i * 53));
        static readonly SfxBank _humsB = new SfxBank(null, 8, i => MakeHum(i, 6200 + i * 41));
        static readonly SfxBank _scattersB = new SfxBank(null, 12, i => MakeScatter(i, 7300 + i * 37));
        static readonly SfxBank _boomsB = new SfxBank(null, 5, i => MakeBoom(3400 + i * 71));
        static readonly SfxBank _thundersB = new SfxBank(null, 12, i => MakeThunder(i, 2800 + i * 67));
        // Warm order: what the splash and the first garden reach first.
        static readonly SfxBank[] _warmOrder =
        {
            _chirpsB, _popsB, _clinksB, _flapsB, _fluttersB, _denyB, _pageTurnB, _humsB,
            _breaksB, _liftsB, _chingsB, _oinksB, _jinglesB, _snoozesB, _scattersB,
            _betInB, _betOutB, _rattlesB, _yellsB, _hawksB, _boomsB, _thundersB
        };
        static int _warmBank;
        static bool _warmGate;
        static AudioClip[] _chirps => _chirpsB.Get();
        static AudioClip[] _flaps => _flapsB.Get();
        static AudioClip[] _flutters => _fluttersB.Get();
        static AudioClip[] _breaks => _breaksB.Get();
        static AudioClip[] _lifts => _liftsB.Get();
        static AudioClip[] _chings => _chingsB.Get();
        static AudioClip[] _clinks => _clinksB.Get();
        static AudioClip[] _rattles => _rattlesB.Get();
        static AudioClip[] _yells => _yellsB.Get();
        static AudioClip[] _hawks => _hawksB.Get();
        static AudioClip[] _oinks => _oinksB.Get();
        static AudioClip[] _pops => _popsB.Get();
        static AudioClip _celebrate;
        static AudioClip[] _jingles => _jinglesB.Get();
        static AudioClip _deny => _denyB.Get()[0];
        static AudioClip _pageTurn => _pageTurnB.Get()[0];
        static AudioClip[] _betIn => _betInB.Get();
        static AudioClip[] _betOut => _betOutB.Get();
        static AudioClip[] _booms => _boomsB.Get();
        static AudioClip[] _snoozes => _snoozesB.Get();
        static AudioClip[] _hums => _humsB.Get();
        static AudioClip[] _scatters => _scattersB.Get();
        static AudioClip[] _thunders => _thundersB.Get();
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
        static int _lastHawk = -1;
        static int _lastOink = -1;
        static int _lastPop = -1;
        static int _lastBetIn = -1;
        static int _lastBetOut = -1;
        static float _humGate;
        static float _flapGate;
        static SfxHost _host;
        const int Rate = 44100;

        public static void Warm() => Ensure();

        public static bool WarmDone => _warmBank >= _warmOrder.Length && _warmGate && LoneClipsWarm;

        // One small piece of synthesis or one clip's audio data per call. The splash calls
        // this once per frame, so the first chirp, gate or thunder is never a hitch.
        public static void WarmStep()
        {
            while (_warmBank < _warmOrder.Length)
            {
                if (_warmOrder[_warmBank].Step()) return;
                _warmBank++;
            }
            if (!_warmGate)
            {
                _warmGate = true;
                if (_gate == null) _gate = LoadGate();
                return;
            }
            WarmLoneClip();
        }

        static bool LoneClipsWarm =>
            _celebrate != null && _lullaby != null && _fwWhistle != null && _fwCrackle != null && _feederArrive != null;

        static void WarmLoneClip()
        {
            if (_celebrate == null) { _celebrate = MakeCelebrate(); return; }
            if (_lullaby == null) { _lullaby = MakeLullaby(); return; }
            if (_fwWhistle == null) { _fwWhistle = MakeFwWhistle(); return; }
            if (_fwCrackle == null) { _fwCrackle = MakeFwCrackle(); return; }
            if (_feederArrive == null) _feederArrive = MakeFeederArrive();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _voices = null;
            _host = null;
            _v = 0;
        }

        static bool VoicesAlive()
        {
            if (_voices == null || _host == null) return false;
            for (int i = 0; i < _voices.Length; i++)
                if (_voices[i] == null) return false;
            return true;
        }

        static void Ensure()
        {
            // Domain-reload-off: statics and leftover DDOL Sfx GOs can both survive a bounce.
            if (!VoicesAlive())
            {
                RebuildHost();
                if (_host != null) MixDesk.Boot(_host.gameObject);
                return;
            }
            if (MixDesk.Live == null && _host != null)
                MixDesk.Boot(_host.gameObject);
        }

        static void RebuildHost()
        {
            _voices = null;
            var found = Object.FindObjectsByType<SfxHost>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SfxHost keep = null;
            if (found != null)
            {
                for (int i = 0; i < found.Length; i++)
                {
                    var h = found[i];
                    if (h == null) continue;
                    if (keep == null) keep = h;
                    else Object.Destroy(h.gameObject);
                }
            }
            if (keep == null)
            {
                var go = new GameObject("Sfx");
                Object.DontDestroyOnLoad(go);
                keep = go.AddComponent<SfxHost>();
            }
            else
                Object.DontDestroyOnLoad(keep.gameObject);
            _host = keep;
            BindVoices();
            // Banks are no longer synthesized here (it held the first frame). They build
            // on first play, or ahead of time one clip per splash frame (WarmStep).
        }

        static void BindVoices()
        {
            var go = _host.gameObject;
            var all = go.GetComponents<AudioSource>();
            var list = new System.Collections.Generic.List<AudioSource>(20);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].loop) continue;
                list.Add(all[i]);
            }
            while (list.Count < 20)
            {
                var a = go.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.loop = false;
                a.spatialBlend = 0f;
                a.volume = 1f;
                list.Add(a);
            }
            _voices = new AudioSource[20];
            for (int i = 0; i < 20; i++)
            {
                var a = list[i];
                a.playOnAwake = false;
                a.loop = false;
                a.spatialBlend = 0f;
                ShareListener(a);
                _voices[i] = a;
            }
        }

        // GamePause mutes with AudioListener.pause. MasterLoudness is the listener gain.
        static void ShareListener(AudioSource a)
        {
            if (a == null) return;
            a.ignoreListenerPause = false;
            a.ignoreListenerVolume = false;
        }

        static AudioSource Voice()
        {
            Ensure();
            var a = _voices[_v];
            _v = (_v + 1) % _voices.Length;
            return a;
        }

        // One-shot on the voice pool. Does not mark Lead.
        static void PlayVoice(AudioClip clip, float pitch, float vol)
        {
            if (clip == null) return;
            var a = Voice();
            a.pitch = pitch;
            a.PlayOneShot(clip, vol);
        }

        static void Shot(AudioClip clip, float pitch, float vol, MixLayer layer, float leadDuck = MixDesk.DuckChirp)
        {
            if (clip == null) return;
            // One level rule for every one-shot, so no sound jumps out of the mix.
            vol = SfxLibrary.Level(vol);
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
            PlayVoice(clip, pitch, vol);
        }

        public static void PlayProc(AudioClip clip, float pitch, float vol, MixLayer layer)
        {
            Ensure();
            if (pitch > 1.04f) pitch = 1.04f;
            if (pitch < 0.92f) pitch = 0.92f;
            Shot(clip, pitch, vol, layer);
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
            // One bird (pest dives, single flaps) has no stagger — don't spawn a coroutine.
            if (birds <= 1)
            {
                PlayFlutter(settle ? 0.48f : 0.66f, Random.Range(0.93f, 0.98f));
                return;
            }
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
            // Poker full-house+ sting. Wood-pluck D-major cadence, not a gong.
            Ensure();
            if (_celebrate == null) _celebrate = MakeCelebrate();
            Shot(_celebrate, 1f, 0.80f, MixLayer.Lead, MixDesk.DuckWhoosh);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.72f, MixDesk.DuckWhoosh);
        }

        public static void Combo(int size) => Combo(size, 0.76f);

        public static void Combo(int size, float vol)
        {
            Ensure();
            size = Mathf.Clamp(size, 2, Palette.ComboMax);
            if (MixDesk.Live != null) MixDesk.Live.ComboWarm();
            // No spoken voices anywhere in the game (testers heard a man's voice).
            {
                int i = Mathf.Min(size, 8) - 2;
                float pitch = size <= 8 ? 1f : Mathf.Min(1.04f, 1f + 0.008f * (size - 8));
                if (_jingles != null && i >= 0 && i < _jingles.Length)
                {
                    Shot(_jingles[i], pitch, vol, MixLayer.Lead, MixDesk.DuckWhoosh);
                    if (MixDesk.Live != null)
                        MixDesk.Live.MarkLead(0.42f + 0.14f * i, MixDesk.DuckWhoosh);
                }
            }
        }

        public static void BeeFound()
        {
            Ensure();
            if (_jingles == null || _jingles.Length == 0) return;
            Shot(_jingles[0], 1f, 0.52f, MixLayer.Lead, MixDesk.DuckWhoosh);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.28f, MixDesk.DuckWhoosh);
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
        }

        public static void FeederDone()
        {
            Ensure();
            int i = Next(_chings.Length, ref _lastChing);
            Shot(_chings[i], Random.Range(0.98f, 1.02f), 0.64f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.4f, MixDesk.DuckChirp);
        }

        // Latest payout one-shot. RewardPay waits until this moment, then fades.
        static float _payoutUntil;

        public static bool PayoutBusy => PlayClock.Now < _payoutUntil;

        public static void ClearPayout() => _payoutUntil = 0f;

        static void MarkPayout(AudioClip clip, float pitch)
        {
            if (clip == null) return;
            float span = clip.length;
            if (pitch > 0.05f) span /= pitch;
            float end = PlayClock.Now + span;
            if (end > _payoutUntil) _payoutUntil = end;
        }

        public static void Clink()
        {
            Ensure();
            if (_clinks == null || _clinks.Length == 0) return;
            int i = Next(_clinks.Length, ref _lastClink);
            var clip = _clinks[i];
            float pitch = Random.Range(0.98f, 1.04f);
            Shot(clip, pitch, 0.78f, MixLayer.Lead, MixDesk.DuckChirp);
            MarkPayout(clip, pitch);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.18f, MixDesk.DuckChirp);
        }

        // The bet banks are complete whenever they are read (SfxBank), so this only
        // forces them in for callers that want the cost paid early.
        static void WarmBetClips()
        {
            _betInB.Get();
            _betOutB.Get();
        }

        public static void BetUp()
        {
            Ensure();
            WarmBetClips();
            int i = Next(_betIn.Length, ref _lastBetIn);
            Shot(_betIn[i], Random.Range(0.98f, 1.02f), 0.82f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.22f, MixDesk.DuckChirp);
        }

        public static void BetDown()
        {
            Ensure();
            WarmBetClips();
            int i = Next(_betOut.Length, ref _lastBetOut);
            Shot(_betOut[i], Random.Range(0.98f, 1.02f), 0.78f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.24f, MixDesk.DuckChirp);
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

        // Hawk pest cry — deeper / longer bank than SparrowYell (Audio/Hawk).
        public static void HawkCry() => PlayHawkCry(Random.Range(0.98f, 1.04f));

        // Wounded hawk: same bank, held at the mix pitch ceiling.
        public static void HawkCryHot() => PlayHawkCry(1.04f);

        // Defeated limp: same kee, lower and quieter, still under the pitch ceiling.
        public static void HawkCryHurt()
        {
            Ensure();
            if (_hawks == null || _hawks.Length == 0) return;
            int i = Next(_hawks.Length, ref _lastHawk);
            Shot(_hawks[i], 0.90f, Random.Range(0.62f, 0.70f), MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.45f, MixDesk.DuckChirp);
        }

        static void PlayHawkCry(float pitch)
        {
            Ensure();
            if (_hawks == null || _hawks.Length == 0) return;
            int i = Next(_hawks.Length, ref _lastHawk);
            Shot(_hawks[i], Mathf.Clamp(pitch, 0.96f, 1.04f), Random.Range(0.78f, 0.88f), MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(Random.Range(0.32f, 0.40f), MixDesk.DuckChirp);
        }

        // Splash pig poke — cute soft snort-oink (Audio/Oink). Not Clink coin.
        public static void Oink()
        {
            Ensure();
            if (_oinks == null || _oinks.Length == 0) return;
            int i = Next(_oinks.Length, ref _lastOink);
            Shot(_oinks[i], Random.Range(0.98f, 1.04f), Random.Range(0.70f, 0.82f), MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(Random.Range(0.18f, 0.28f), MixDesk.DuckChirp);
        }

        public static void ScorePop(int i)
        {
            Ensure();
            if (_pops == null || _pops.Length == 0) return;
            int k = Mathf.Clamp(i, 0, _pops.Length - 1);
            Shot(_pops[k], 1f, 0.70f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.2f, MixDesk.DuckChirp);
        }

        public static void CardPop()
        {
            Ensure();
            if (_pops == null || _pops.Length == 0) return;
            int i = Next(_pops.Length, ref _lastPop);
            Shot(_pops[i], Random.Range(0.98f, 1.03f), 0.78f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.16f, MixDesk.DuckChirp);
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

        static AudioClip _lullaby;

        // Quiet mid. Plays under a collect crunch and does not take the lead seat.
        public static void Lullaby()
        {
            Ensure();
            if (_lullaby == null) _lullaby = MakeLullaby();
            Shot(_lullaby, Random.Range(0.97f, 1.03f), 0.19f, MixLayer.Mid);
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

        public static float FireworkGain = 1f;
        static AudioClip _fwWhistle;
        static AudioClip _fwCrackle;
        static float _fwCrackleAt;

        public static void FireworkLaunch()
        {
            Ensure();
            if (_fwWhistle == null) _fwWhistle = MakeFwWhistle();
            float g = Mathf.Clamp01(FireworkGain);
            if (g < 0.04f) return;
            Shot(_fwWhistle, Random.Range(0.96f, 1.05f), 0.38f * g, MixLayer.Mid);
        }

        public static void Firework()
        {
            Ensure();
            float g = Mathf.Clamp01(FireworkGain);
            if (g < 0.04f) return;
            if (Time.unscaledTime - _fwCrackleAt < 0.14f) return;
            _fwCrackleAt = Time.unscaledTime;
            if (_fwCrackle == null) _fwCrackle = MakeFwCrackle();
            float vol = 0.26f * g;
            float pitch = Random.Range(0.92f, 1.04f);
            if (MixDesk.Live != null && !MixDesk.Live.AllowMid)
            {
                PlayVoice(_fwCrackle, pitch, vol * 0.45f);
                return;
            }
            Shot(_fwCrackle, pitch, vol, MixLayer.Mid);
        }

        static AudioClip MakeFwWhistle()
        {
            const float dur = 0.22f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                float f = Mathf.Lerp(420f, 1380f, u);
                float e = Mathf.Sin(Mathf.PI * u) * (1f - u);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * e * 0.45f;
            }
            return Clip("fw-whistle", data);
        }

        public static void Rumble() => Haptics.Play(Haptics.Tier.Medium);

        // Distant garden rumble + crack variety. Mid, never Lead. Skips if a hop is speaking.
        // Clips 0,3,6,9 are the close crack; 1,4,7,10 the roll; 2,5,8,11 the far growl.
        public static bool Thunder()
        {
            if (!ThunderOk()) return false;
            int i = Next(_thunders.Length, ref _lastThunder);
            float near = (i % 3 == 0) ? 1f : 0f;
            float vol = Mathf.Lerp(0.42f, 0.72f, near * 0.55f + Random.value * 0.45f);
            return ThunderShot(i, vol);
        }

        public static bool ThunderCrack(float power)
        {
            if (!ThunderOk()) return false;
            float vol = Mathf.Lerp(0.52f, 0.72f, Mathf.Clamp01(power));
            return ThunderShot(ThunderOf(0), vol);
        }

        public static bool ThunderRoll(float power)
        {
            if (!ThunderOk()) return false;
            int family = power >= 0.72f ? 1 : 2;
            float vol = Mathf.Lerp(0.38f, 0.62f, Mathf.Clamp01(power));
            return ThunderShot(ThunderOf(family), vol);
        }

        static bool ThunderOk()
        {
            Ensure();
            if (MixDesk.Live != null && !MixDesk.Live.AllowMid) return false;
            return _thunders != null && _thunders.Length > 0;
        }

        static int ThunderOf(int family)
        {
            int n = _thunders.Length;
            int i = _lastThunder;
            for (int k = 0; k < n; k++)
            {
                i++;
                if (i >= n) i = 0;
                if ((i % 3) == family) break;
            }
            _lastThunder = i;
            return i;
        }

        static bool ThunderShot(int i, float vol)
        {
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

        static AudioClip _feederArrive;

        // First "!" of a combo and every bird "!" alert. One soft SfxLibrary pluck pair,
        // Lead. Bed left alone (DuckChirp). Clip peak 0.78 at SfxLibrary.StingVolume:
        // 0.30 * 0.78 * MasterLoudness ×2 ≈ 0.47, well under the 0.72 knee and quieter
        // than the other alerts. Pitch stays put.
        public static void RowAlert()
        {
            Ensure();
            var clip = SfxLibrary.Sting();
            if (clip == null) return;
            Shot(clip, Random.Range(0.99f, 1.01f), SfxLibrary.StingVolume, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.48f, MixDesk.DuckChirp);
        }

        // Feeder drop. Short wood pluck, one Lead seat, not a chime loop.
        public static void FeederArrive()
        {
            Ensure();
            if (_feederArrive == null) _feederArrive = MakeFeederArrive();
            Shot(_feederArrive, Random.Range(0.98f, 1.02f), 0.48f, MixLayer.Lead, MixDesk.DuckChirp);
            if (MixDesk.Live != null) MixDesk.Live.MarkLead(0.18f, MixDesk.DuckChirp);
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

        internal static bool OwnsHost(SfxHost h) => _host == h;

        internal static void DropHost()
        {
            _host = null;
            _voices = null;
        }
    }

    sealed class SfxHost : MonoBehaviour
    {
        void OnDestroy()
        {
            var srcs = GetComponents<AudioSource>();
            for (int i = 0; i < srcs.Length; i++)
            {
                if (srcs[i] == null) continue;
                srcs[i].Stop();
                srcs[i].volume = 0f;
            }
            if (Sfx.OwnsHost(this)) Sfx.DropHost();
        }
    }

    // One clip bank, built on demand. Get() always returns the complete bank (it
    // finishes whatever is left), so a play site never sees a partial array. Step()
    // does one small unit of work for the splash warm-up: the Resources lookup, one
    // synthesized clip, or one imported clip's audio data (those import with
    // preloadAudioData off, so the first Play used to read the file).
    sealed class SfxBank
    {
        readonly string _path;
        readonly int _count;
        readonly System.Func<int, AudioClip> _make;
        readonly System.Func<AudioClip[]> _load;
        AudioClip[] _clips;
        int _made;
        int _data;
        bool _synth;

        public SfxBank(string path, int count, System.Func<int, AudioClip> make)
        {
            _path = path;
            _count = count;
            _make = make;
        }

        public SfxBank(System.Func<AudioClip[]> load)
        {
            _load = load;
        }

        bool Built => _clips != null && (!_synth || _made >= _clips.Length);

        void Begin()
        {
            if (_clips != null) return;
            if (_load != null)
            {
                _clips = _load() ?? new AudioClip[0];
                return;
            }
            if (_path != null)
            {
                var found = Resources.LoadAll<AudioClip>(_path);
                if (found != null && found.Length > 0)
                {
                    System.Array.Sort(found, (a, b) => string.CompareOrdinal(a.name, b.name));
                    _clips = found;
                    return;
                }
            }
            _synth = true;
            _made = 0;
            _clips = new AudioClip[_count];
        }

        public AudioClip[] Get()
        {
            if (_clips != null && !_synth) return _clips;
            Begin();
            while (_synth && _made < _clips.Length)
            {
                _clips[_made] = _make(_made);
                _made++;
            }
            return _clips;
        }

        // True when it did something; false when the bank is fully warm.
        public bool Step()
        {
            if (_clips == null)
            {
                Begin();
                return true;
            }
            if (_synth && _made < _clips.Length)
            {
                _clips[_made] = _make(_made);
                _made++;
                return true;
            }
            if (!Built) return true;
            while (_data < _clips.Length)
            {
                var c = _clips[_data++];
                if (c == null || c.loadState != AudioDataLoadState.Unloaded) continue;
                c.LoadAudioData();
                return true;
            }
            return false;
        }
    }
}

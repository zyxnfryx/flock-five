using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Named procedural clips. Lazy, cached. Play goes through Sfx so mute and mix still apply.
    public static class SfxLibrary
    {
        const int Rate = 44100;
        const float FanfareCap = 0.34f;
        static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        static float _grooveAt = -99f;
        static bool _claimGroove;
        static bool _inPlay;

        // Garden play locks the groove. The daily-claim card is the only seat that opens it.
        public static void NoteGarden(bool playing)
        {
            _inPlay = playing;
            if (playing) SeatGroove(false);
        }

        public static void SeatGroove(bool on)
        {
            _claimGroove = on && !_inPlay;
            if (!_claimGroove) StopHeld();
        }

        // The ONE level rule for every one-shot (Sfx.Shot and Sfx.PlayHeld route through
        // it). Up to SfxCeiling a volume passes as written. Above it the excess is
        // squeezed to a quarter, so a 1.0 break or a 0.96 celebrate lands a hair over
        // the rest instead of jumping out of the mix. Steady loudness, no clipping.
        public const float SfxCeiling = 0.80f;

        public static float Level(float vol)
        {
            if (vol <= 0f) return 0f;
            if (vol <= SfxCeiling) return vol;
            return SfxCeiling + (vol - SfxCeiling) * 0.25f;
        }

        // Level the "!" sting plays at, with its clip peak (0.78) it sits under every
        // other alert in the mix. RowAlert is the only caller.
        public const float StingVolume = 0.30f;

        public static void Play(string name, float volume = 0.5f, float pitchVariance = 0f)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (name == "firework")
            {
                Sfx.Firework();
                return;
            }
            if (name == "thud")
            {
                Sfx.CardBump();
                return;
            }
            if (name == "sting")
            {
                Sfx.RowAlert();
                return;
            }
            if (name == "groove")
            {
                if (!_claimGroove) return;
                if (Time.unscaledTime - _grooveAt < 30f) return;
                _grooveAt = Time.unscaledTime;
                var groove = Clip(name);
                if (groove == null) return;
                Sfx.PlayHeld(groove, Mathf.Clamp(volume <= 0f ? 0.30f : volume, 0.05f, 0.42f), groove.length);
                return;
            }
            var clip = Clip(name);
            if (clip == null) return;
            float pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            float vol = volume <= 0f ? 0.2f : volume;
            if (name == "fanfare" && vol > FanfareCap) vol = FanfareCap;
            var layer = name == "tick" ? MixLayer.Mid : MixLayer.Lead;
            Sfx.PlayProc(clip, pitch, vol, layer);
        }

        public static void StopHeld() => Sfx.StopHeld();

        // Wake "!" sting. One play at the start of a combo, never mid-chain.
        // The caller opens, seals, and closes. Cowbell does not use this gate.
        static bool _chain;
        static bool _startFree;

        public static void OpenCombo()
        {
            _chain = true;
            _startFree = true;
        }

        public static void SealComboStart()
        {
            _startFree = false;
        }

        public static void CloseCombo()
        {
            _chain = false;
            _startFree = false;
        }

        public static bool TakeWakeSting()
        {
            if (!_chain || !_startFree) return false;
            _startFree = false;
            return true;
        }

        // The one "!" clip. Every alert plays it through Sfx.RowAlert.
        public static AudioClip Sting() => Clip("sting");

        static AudioClip Clip(string name)
        {
            AudioClip clip;
            if (_clips.TryGetValue(name, out clip) && clip != null) return clip;
            if (name == "cowbell") clip = MakeCowbell();
            else if (name == "fanfare") clip = MakeFanfare();
            else if (name == "groove") clip = MakeGroove();
            else if (name == "tick") clip = MakeTick();
            else if (name == "sting") clip = MakeSting();
            else return null;
            _clips[name] = clip;
            return clip;
        }

        // Two-tone dry clank. Short, quiet, not a bell loop.
        static AudioClip MakeCowbell()
        {
            const float dur = 0.16f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 28f);
                float s = Mathf.Sin(2f * Mathf.PI * 560f * t);
                s += 0.72f * Square(845f, t);
                data[i] = s * env * 0.34f;
            }
            return Bake("cowbell", data, 0.22f);
        }

        // Short rising brass-like sting. Original, one shot, no choir.
        static AudioClip MakeFanfare()
        {
            float[] notes = { 196f, 220f, 247f, 294f };
            const float dur = 0.72f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float u = t / dur;
                int step = Mathf.Clamp(Mathf.FloorToInt(u * notes.Length), 0, notes.Length - 1);
                float f = notes[step];
                float local = u * notes.Length - step;
                float env = Mathf.Sin(Mathf.Clamp01(local) * Mathf.PI) * Mathf.Exp(-u * 1.4f);
                float s = Mathf.Sin(2f * Mathf.PI * f * t);
                s += 0.28f * Mathf.Sin(2f * Mathf.PI * f * 2f * t);
                s += 0.12f * Mathf.Sin(2f * Mathf.PI * f * 3f * t);
                data[i] = s * env * 0.40f;
            }
            return Bake("fanfare", data, 0.18f);
        }

        // Original low minor figure. Not a known song. One shot, about five seconds.
        static AudioClip MakeGroove()
        {
            float[] riff = { 146.83f, 174.61f, 130.81f, 164.81f, 116.54f, 146.83f };
            const float beat = 0.62f;
            const float dur = 5.0f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                int step = Mathf.FloorToInt(t / beat);
                float f = riff[step % riff.Length];
                float into = t - step * beat;
                float gEnv = Mathf.Exp(-into * 10f) * Mathf.Clamp01(into / 0.012f);
                float g = Square(f, t) * 0.55f + Mathf.Sin(2f * Mathf.PI * f * 0.5f * t) * 0.35f;
                float bell = 0f;
                if (into < 0.09f)
                {
                    float be = Mathf.Exp(-into * 32f);
                    bell = (Mathf.Sin(2f * Mathf.PI * 560f * t) + 0.6f * Square(845f, t)) * be;
                }
                float kick = 0f;
                if ((step % 2) == 0 && into < 0.08f)
                    kick = Mathf.Sin(2f * Mathf.PI * 58f * t) * Mathf.Exp(-into * 36f);
                data[i] = (g * gEnv * 0.42f + bell * 0.22f + kick * 0.30f) * 0.55f;
            }
            return Bake("groove", data, 0.16f);
        }

        static AudioClip MakeTick()
        {
            const float dur = 0.05f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * 520f * t) * Mathf.Exp(-t * 70f) * 0.45f;
            }
            return Bake("tick", data, 0.2f);
        }

        // Original "!" cue, soft and round. Two plucked-string notes rise a fifth
        // (E5 to B5), the second 85 ms after the first. Each note is a sine with a
        // gentle 2nd and 3rd partial that die fast (warm pluck, nothing above ~3 kHz
        // for long), a slightly detuned twin for width, a 9 ms eased attack so there
        // is no click or bite, and a smooth exponential decay. 0.46 s. Peak 0.78.
        // Bake low-passes the sum again, which rounds the top further.
        static AudioClip MakeSting()
        {
            const float dur = 0.46f;
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            float[] freq = { 659.25f, 987.77f };
            float[] start = { 0f, 0.085f };
            float[] gain = { 0.72f, 1f };
            float[] decay = { 11f, 7.5f };
            for (int k = 0; k < freq.Length; k++)
            {
                int i0 = Mathf.RoundToInt(start[k] * Rate);
                float f = freq[k];
                for (int i = i0; i < n; i++)
                {
                    float t = (i - i0) / (float)Rate;
                    float u = t / 0.009f;
                    float atk = u >= 1f ? 1f : u * u * (3f - 2f * u);
                    float env = atk * Mathf.Exp(-t * decay[k]);
                    float w = 2f * Mathf.PI * f * t;
                    float tone = Mathf.Sin(w)
                        + 0.30f * Mathf.Sin(2f * w) * Mathf.Exp(-t * 16f)
                        + 0.09f * Mathf.Sin(3f * w) * Mathf.Exp(-t * 28f)
                        + 0.45f * Mathf.Sin(w * 1.0021f);
                    data[i] += tone * env * gain[k];
                }
            }
            int tail = Mathf.RoundToInt(0.04f * Rate);
            for (int i = 0; i < tail && i < n; i++)
                data[n - 1 - i] *= i / (float)tail;
            PeakUnder(data, 0.78f);
            return Bake("sting", data, 0.5f);
        }

        static float BlepSaw(float phase, float dt)
        {
            float s = 2f * phase - 1f;
            s -= PolyBlep(phase, dt);
            return s;
        }

        static float PolyBlep(float phase, float dt)
        {
            if (dt <= 0f) return 0f;
            if (phase < dt)
            {
                phase /= dt;
                return phase + phase - phase * phase - 1f;
            }
            if (phase > 1f - dt)
            {
                phase = (phase - 1f) / dt;
                return phase * phase + phase + phase + 1f;
            }
            return 0f;
        }

        static void PeakUnder(float[] data, float peak)
        {
            float m = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float a = data[i];
                if (a < 0f) a = -a;
                if (a > m) m = a;
            }
            if (m < 0.00001f) return;
            float g = peak / m;
            for (int i = 0; i < data.Length; i++)
                data[i] *= g;
        }

        static float Square(float freq, float t)
        {
            float s = Mathf.Sin(2f * Mathf.PI * freq * t);
            if (s > 0.2f) return 0.65f;
            if (s < -0.2f) return -0.65f;
            return s;
        }

        static AudioClip Bake(string name, float[] data, float lp)
        {
            float y = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                y += lp * (data[i] - y);
                data[i] = Mathf.Clamp(y, -0.95f, 0.95f);
            }
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}

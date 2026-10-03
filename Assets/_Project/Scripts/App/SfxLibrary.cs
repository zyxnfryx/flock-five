using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Named procedural clips. Lazy, cached. Play goes through Sfx so mute and mix still apply.
    public static class SfxLibrary
    {
        const int Rate = 44100;
        static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        static float _grooveAt = -99f;

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
            if (name == "groove")
            {
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

        static AudioClip Clip(string name)
        {
            AudioClip clip;
            if (_clips.TryGetValue(name, out clip) && clip != null) return clip;
            if (name == "cowbell") clip = MakeCowbell();
            else if (name == "fanfare") clip = MakeFanfare();
            else if (name == "groove") clip = MakeGroove();
            else if (name == "tick") clip = MakeTick();
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

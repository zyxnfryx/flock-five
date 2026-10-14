using UnityEngine;

namespace FlockFive
{
    // The one shared master stage. Lives on the AudioListener, so it sees the final mix:
    // MixDesk stems (garden, splash, rain, combo, badger), every Sfx one-shot, and the
    // studio jingle (which plays on its own AudioSource). No AudioMixer.
    //   - Transparent below -4.7 dBFS (gain exactly 1).
    //   - Soft knee (6 dB wide) into a -1.7 dBFS sample ceiling (~-1.5 dBTP), 2 ms look-ahead, 150 ms release.
    //   - A smooth saturator above the ceiling catches what the look-ahead misses, so the
    //     output never hard-clips at 0 dBFS. Adds 2 ms of latency to the whole mix.
    // This protects small Android/iPhone speakers when the bed, a Lead sting and a jingle
    // stack at max volume. It does not change notes, pitch, or the balance of the mix.
    public sealed class MasterSoftClip : MonoBehaviour
    {
        public static MasterSoftClip Live;

        public const float CeilingDb = -1.7f; // sample ceiling; keeps true peak at or under about -1.5 dBTP
        public const float KneeDb = 6f;
        const float LookMs = 2f;
        
        const int MaxChannels = 8;
        const float ReleaseMs = 150f;

        // Read-only meter for debug overlays and tests. Written on the audio thread.
        public static volatile float GainReductionDb;

        float _ceil, _att, _rel, _env, _gain = 1f;
        float[] _ring;
        int _look, _pos;
        bool _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Live = null;
            GainReductionDb = 0f;
        }

        // Puts the stage on the active listener once. Cheap to call again.
        public static void Ensure()
        {
            if (Live != null && Live.isActiveAndEnabled) return;
            var listener = Object.FindFirstObjectByType<AudioListener>();
            if (listener == null) return;
            var stage = listener.GetComponent<MasterSoftClip>();
            if (stage == null) stage = listener.gameObject.AddComponent<MasterSoftClip>();
            Live = stage;
        }

        void Awake() => Setup(AudioSettings.outputSampleRate);

        void OnEnable()
        {
            Live = this;
            AudioSettings.OnAudioConfigurationChanged += OnConfig;
        }

        void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnConfig;
            if (Live == this) Live = null;
        }

        void OnConfig(bool deviceChanged) => Setup(AudioSettings.outputSampleRate);

        public void Setup(int rate)
        {
            if (rate <= 0) rate = 48000;
            _ceil = Mathf.Pow(10f, CeilingDb / 20f);
            _look = Mathf.Max(1, Mathf.RoundToInt(rate * LookMs / 1000f));
            // Gain falls with a time constant of a fifth of the look-ahead, so it is ~99% there
            // before the peak that asked for it leaves the delay line.
            _att = 1f - Mathf.Exp(-5f / _look);
            _rel = 1f - Mathf.Exp(-1f / (rate * ReleaseMs / 1000f));
            _env = 0f;
            _gain = 1f;
            _ring = new float[_look * MaxChannels];
            _pos = 0;
            _ready = true;
        }

        // Static gain curve in dB (hard ratio, soft knee centred on the ceiling).
        public static float CurveDb(float inDb)
        {
            float lo = CeilingDb - KneeDb * 0.5f;
            if (inDb <= lo) return inDb;
            float hi = CeilingDb + KneeDb * 0.5f;
            if (inDb >= hi) return CeilingDb;
            float d = inDb - lo;
            return inDb - d * d / (2f * KneeDb);
        }

        // Pure DSP on a buffer, so tests and the offline harness run the exact runtime code.
        public void Process(float[] data, int channels)
        {
            if (!_ready || data == null || channels < 1 || channels > MaxChannels) return;
            float ceil = _ceil, att = _att, rel = _rel, env = _env, gs = _gain;
            var ring = _ring;
            int look = _look, pos = _pos;
            float lo = Mathf.Pow(10f, (CeilingDb - KneeDb * 0.5f) / 20f);
            float minG = 1f;
            for (int o = 0; o + channels <= data.Length; o += channels)
            {
                float peak = 0f;
                for (int c = 0; c < channels; c++)
                {
                    float a = data[o + c];
                    if (a < 0f) a = -a;
                    if (a > peak) peak = a;
                }
                // Instant-attack peak envelope on the incoming sample, slow release.
                env = peak > env ? peak : env + rel * (peak - env);
                float g = 1f;
                if (env > lo)
                {
                    float inDb = 20f * Mathf.Log10(env);
                    g = Mathf.Pow(10f, (CurveDb(inDb) - inDb) / 20f);
                }
                gs += (g < gs ? att : rel) * (g - gs);
                g = gs;
                if (g < minG) minG = g;
                int r = pos * MaxChannels;
                for (int c = 0; c < channels; c++)
                {
                    // Gain from the incoming sample, applied to the one LookMs older.
                    float late = ring[r + c];
                    ring[r + c] = data[o + c];
                    float y = late * g;
                    float a = y < 0f ? -y : y;
                    if (a > ceil)
                    {
                        // Smooth saturation between the ceiling and full scale.
                        float room = 1f - ceil;
                        a = ceil + room * Tanh((a - ceil) / room);
                        y = y < 0f ? -a : a;
                    }
                    data[o + c] = y;
                }
                if (++pos >= look) pos = 0;
            }
            _env = env;
            _gain = gs;
            _pos = pos;
            GainReductionDb = 20f * Mathf.Log10(Mathf.Max(minG, 1e-6f));
        }

        static float Tanh(float x)
        {
            if (x > 9f) return 1f;
            float e = Mathf.Exp(2f * x);
            return (e - 1f) / (e + 1f);
        }

        void OnAudioFilterRead(float[] data, int channels) => Process(data, channels);
    }
}

using UnityEngine;

namespace FlockFive
{
    // Final-mix makeup gain on the AudioListener. iPhone speakers read quiet at the
    // game's native levels, so lift everything together, then soft-limit so summed
    // peaks round off instead of clipping.
    [RequireComponent(typeof(AudioListener))]
    public sealed class MasterLoudness : MonoBehaviour
    {
        public const float Gain = 2.0f;   // +6 dB
        const float Knee = 0.72f;

        void OnAudioFilterRead(float[] data, int channels)
        {
            const float room = 1f - Knee;
            for (int i = 0; i < data.Length; i++)
            {
                float s = data[i] * Gain;
                float a = s < 0f ? -s : s;
                if (a > Knee)
                {
                    float over = (a - Knee) / room;
                    // tanh-shaped: slope 1 at the knee, never passes 1.0.
                    float e = (float)System.Math.Exp(-2f * over);
                    a = Knee + room * (1f - e) / (1f + e);
                    s = s < 0f ? -a : a;
                }
                data[i] = s;
            }
        }
    }
}

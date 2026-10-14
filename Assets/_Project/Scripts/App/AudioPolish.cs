using UnityEngine;

namespace FlockFive
{
    // Shared smoothing stage for every procedural music clip (MixDesk beds, rain, combo
    // fifth; Sfx combo jingles and celebrate). Same chain the authored beds were mastered
    // with offline (Tools/smooth.py), so code-built and authored music sound like one mix:
    //   1. high shelf -3 dB above ~4.8 kHz
    //   2. dynamic de-ess, 3-8 kHz band vs full band, at most -7 dB
    //   3. dynamic presence dip at ~2.4 kHz (phone speaker resonance), loud peaks only, at most -4 dB
    //   4. transient softening, fast vs slow envelope, at most -3.5 dB
    // No notes, timing or length change. Runs once when a clip is built, never per frame.
    public static class AudioPolish
    {
        struct Biquad
        {
            public float b0, b1, b2, a1, a2;
            public float x1, x2, y1, y2;

            public float Run(float x)
            {
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return y;
            }

            public void Reset() { x1 = x2 = y1 = y2 = 0f; }

            public static Biquad HighShelf(float fc, float gainDb, int rate, float slope = 0.7f)
            {
                double A = System.Math.Pow(10.0, gainDb / 40.0);
                double w = 2.0 * System.Math.PI * fc / rate;
                double c = System.Math.Cos(w), s = System.Math.Sin(w);
                double al = s / 2.0 * System.Math.Sqrt((A + 1.0 / A) * (1.0 / slope - 1.0) + 2.0);
                double sa = 2.0 * System.Math.Sqrt(A) * al;
                double a0 = (A + 1) - (A - 1) * c + sa;
                return new Biquad
                {
                    b0 = (float)(A * ((A + 1) + (A - 1) * c + sa) / a0),
                    b1 = (float)(-2 * A * ((A - 1) + (A + 1) * c) / a0),
                    b2 = (float)(A * ((A + 1) + (A - 1) * c - sa) / a0),
                    a1 = (float)(2 * ((A - 1) - (A + 1) * c) / a0),
                    a2 = (float)(((A + 1) - (A - 1) * c - sa) / a0)
                };
            }

            public static Biquad BandPass(float fc, float q, int rate)
            {
                double w = 2.0 * System.Math.PI * fc / rate;
                double al = System.Math.Sin(w) / (2.0 * q), c = System.Math.Cos(w);
                double a0 = 1 + al;
                return new Biquad
                {
                    b0 = (float)(al / a0), b1 = 0f, b2 = (float)(-al / a0),
                    a1 = (float)(-2 * c / a0), a2 = (float)((1 - al) / a0)
                };
            }
        }

        // Peak follower with separate attack and release (ms). Same as smooth.py follow().
        struct Follower
        {
            public float att, rel, v;
            public Follower(float attMs, float relMs, int rate)
            {
                att = 1f - Mathf.Exp(-1f / (rate * attMs / 1000f));
                rel = 1f - Mathf.Exp(-1f / (rate * relMs / 1000f));
                v = 0f;
            }
            public float Run(float x) { v += (x > v ? att : rel) * (x - v); return v; }
        }

        const float ShelfHz = 4800f, ShelfDb = -3f;
        const float DeEssHz = 5200f, DeEssQ = 0.75f, DeEssThrDb = -14f, DeEssMaxDb = 7f;
        const float PresHz = 2400f, PresQ = 0.9f, PresThrDb = -20f, PresRatio = 2f, PresMaxDb = 4f;
        const float TransRatio = 1.5f, TransMaxDb = 3.5f;

        static float Db(float db) => Mathf.Pow(10f, db / 20f);

        // data is interleaved; channels 1 or 2. Envelopes are linked across channels.
        // loop: run the filters over the last ~2 s first (output discarded) so the state at
        // sample 0 matches the end of the clip and the loop point stays seamless.
        public static void Apply(float[] data, int channels, int rate, bool loop = false)
        {
            if (data == null || data.Length == 0) return;
            if (channels < 1) channels = 1;
            if (channels > 2) channels = 2;
            int frames = data.Length / channels;
            var shelf = new Biquad[channels];
            var ds = new Biquad[channels];
            var pr = new Biquad[channels];
            for (int c = 0; c < channels; c++)
            {
                shelf[c] = Biquad.HighShelf(ShelfHz, ShelfDb, rate);
                ds[c] = Biquad.BandPass(DeEssHz, DeEssQ, rate);
                pr[c] = Biquad.BandPass(PresHz, PresQ, rate);
            }
            var eb = new Follower(1f, 60f, rate);
            var ef = new Follower(1f, 60f, rate);
            var gds = new Follower(0.5f, 40f, rate);
            var ep = new Follower(2f, 80f, rate);
            var gpr = new Follower(1f, 60f, rate);
            var fast = new Follower(0.3f, 25f, rate);
            var slow = new Follower(12f, 140f, rate);
            var gtr = new Follower(0.4f, 30f, rate);
            float dsThr = Db(DeEssThrDb), dsFloor = Db(-DeEssMaxDb);
            float prThr = Db(PresThrDb), prFloor = Db(-PresMaxDb), prExp = 1f - 1f / PresRatio;
            float trFloor = Db(-TransMaxDb);
            float[] y = new float[2], band = new float[2];

            int pre = loop ? Mathf.Min(frames, 2 * rate) : 0;
            for (int f = -pre; f < frames; f++)
            {
                int o = (f < 0 ? f + frames : f) * channels;
                float aY = 0f, aB = 0f;
                for (int c = 0; c < channels; c++)
                {
                    y[c] = shelf[c].Run(data[o + c]);
                    band[c] = ds[c].Run(y[c]);
                    aY = Mathf.Max(aY, Mathf.Abs(y[c]));
                    aB = Mathf.Max(aB, Mathf.Abs(band[c]));
                }
                // 2. de-ess
                float vb = eb.Run(aB), vf = ef.Run(aY);
                float lim = dsThr * vf + 1e-9f;
                float g = vb > lim ? lim / (vb + 1e-12f) : 1f;
                if (g < dsFloor) g = dsFloor;
                g = 1f - gds.Run(1f - g);
                float aP = 0f;
                for (int c = 0; c < channels; c++)
                {
                    y[c] -= band[c] * (1f - g);
                    band[c] = pr[c].Run(y[c]);
                    aP = Mathf.Max(aP, Mathf.Abs(band[c]));
                }
                // 3. presence dip
                float vp = ep.Run(aP);
                float gp = vp > prThr ? Mathf.Pow(prThr / (vp + 1e-12f), prExp) : 1f;
                if (gp < prFloor) gp = prFloor;
                gp = 1f - gpr.Run(1f - gp);
                float aT = 0f;
                for (int c = 0; c < channels; c++)
                {
                    y[c] -= band[c] * (1f - gp);
                    aT = Mathf.Max(aT, Mathf.Abs(y[c]));
                }
                // 4. transient softening
                float vfa = fast.Run(aT), vsl = slow.Run(aT) * TransRatio;
                float gt = vfa > vsl ? Mathf.Sqrt(vsl / (vfa + 1e-12f)) : 1f;
                if (gt < trFloor) gt = trFloor;
                gt = 1f - gtr.Run(1f - gt);
                if (f < 0) continue;
                for (int c = 0; c < channels; c++) data[o + c] = y[c] * gt;
            }
        }
    }
}

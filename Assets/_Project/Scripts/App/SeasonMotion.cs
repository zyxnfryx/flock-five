using System;
using System.Diagnostics;

namespace FlockFive
{
    // One tune for every painted backdrop, including the home wash (summer).
    // Channels sit at 1.5–2× the build-81 motion. Gameplay limbs, birds, and
    // bees do not read this. BackgroundMotion.On holds the paint still.
    public static class SeasonMotion
    {
        public const float LivelyMin = 1.5f;
        public const float LivelyMax = 2f;

        public struct Tune
        {
            public float Sway;
            public float Gust;
            public float GustHz;
            public float WaveHz;
            public float Life;
            public float Drift;
            public float Cloud;
            public float Shimmer;
            public float RainGust;
        }

        // Quad counts at 1×. Lively multiplies them. The bench times both.
        public struct Budget
        {
            public int Snow;
            public int Leaf;
            public int Petal;
            public int Fly;
            public int Seed;
            public int Wisp;
            public int Tumble;
            public int Cloud;
            public int Glint;
            // Build 110 (spring): a second, peach blossom sheet and a few flapping butterflies.
            public int Blossom;
            public int Butterfly;
            // Oct 2026: tumbling ground leaves (every season; winter drifts snow) and faint gust streaks.
            public int Ground;
            public int Streak;

            public int Quads
            {
                get
                {
                    return Snow + Leaf + Petal + Fly + Seed + Wisp + Tumble + Cloud + Glint + Blossom + Butterfly + Ground + Streak;
                }
            }
        }

        public static GardenScene Scene = GardenScene.Summer;

        public const int GroundHigh = 16;
        public const int GroundLow = 8;

        // Kept so the bench result cannot be optimized out.
        public static float Sink;

        public static Tune For(GardenScene scene)
        {
            // Summer is also the home painting.
            if (scene == GardenScene.Desert)
                return Make(1.60f, 1.90f, 1.80f, 1.55f, 1.65f, 1.85f, 1.70f, 1.60f, 1.80f);
            if (scene == GardenScene.Fall)
                return Make(1.80f, 1.70f, 1.60f, 1.65f, 1.85f, 1.70f, 1.55f, 1.50f, 1.75f);
            if (scene == GardenScene.Winter)
                return Make(1.50f, 1.55f, 1.50f, 1.50f, 1.75f, 1.55f, 1.50f, 1.55f, 1.60f);
            if (scene == GardenScene.Spring)
                return Make(1.85f, 1.65f, 1.70f, 1.80f, 1.80f, 1.65f, 1.70f, 1.90f, 1.55f);
            return Make(1.70f, 1.60f, 1.65f, 1.70f, 1.70f, 1.60f, 1.75f, 1.80f, 1.65f);
        }

        public static bool InBand(Tune t)
        {
            return Band(t.Sway) && Band(t.Gust) && Band(t.GustHz) && Band(t.WaveHz)
                && Band(t.Life) && Band(t.Drift) && Band(t.Cloud) && Band(t.Shimmer)
                && Band(t.RainGust);
        }

        public static Budget Base(GardenScene scene, bool low)
        {
            var b = new Budget();
            if (scene == GardenScene.Winter) b.Snow = low ? 8 : 18;
            else if (scene == GardenScene.Fall) b.Leaf = low ? 3 : 6;
            else if (scene == GardenScene.Spring)
            {
                // Build 110: was 3/7 tiny faint petals, which read as no motion at all.
                b.Petal = low ? 5 : 9;
                b.Blossom = low ? 3 : 6;
                b.Butterfly = low ? 2 : 3;
            }
            else if (scene == GardenScene.Desert)
            {
                b.Wisp = low ? 3 : 6;
                b.Tumble = 1;
            }
            else
            {
                b.Fly = low ? 3 : 6;
                b.Seed = low ? 2 : 5;
            }
            b.Cloud = low ? 2 : 3;
            b.Glint = low ? 2 : 4;
            if (scene != GardenScene.Desert)
            {
                b.Ground = low ? GroundLow : GroundHigh;
                b.Streak = low ? 2 : 3;
            }
            return b;
        }

        public static Budget Lively(GardenScene scene, bool low)
        {
            var t = For(scene);
            var b = Base(scene, low);
            int cap = low ? 16 : 32;
            b.Snow = Scale(b.Snow, t.Life, cap);
            b.Leaf = Scale(b.Leaf, t.Life, cap);
            b.Petal = Scale(b.Petal, t.Life, cap);
            b.Fly = Scale(b.Fly, t.Life, cap);
            b.Seed = Scale(b.Seed, t.Life, cap);
            b.Wisp = Scale(b.Wisp, t.Life, cap);
            b.Tumble = b.Tumble;
            // Ground leaves and streaks are a fixed count (not scaled by Life).
            b.Blossom = Scale(b.Blossom, t.Life, cap);
            b.Butterfly = Scale(b.Butterfly, t.Life, low ? 3 : 5);
            b.Cloud = Scale(b.Cloud, t.Cloud, low ? 4 : 6);
            b.Glint = Scale(b.Glint, t.Shimmer, low ? 4 : 8);
            return b;
        }

        // ---- Shared wind (Brandon, Oct 2026: leaves blow along the ground, most of all as rain comes) ----
        // One wind for the whole painting. FoliageSway, falling leaves, ground leaves and rain slant all read
        // Wind, so a gust moves everything together. It blows right to left (WindDir), the way rain leans.
        // StormGust is GardenStorm's pre-rain ramp: it rises WindLead seconds before the rain does.
        public const float WindLead = 3f;
        public const float WindDir = -1f;
        public const float BreezeMin = 0.08f;
        public const float BreezeMax = 0.30f;
        // Winter drifts snow on a softer wind (no storm there, see GardenStorm.UsesSnow).
        public const float WinterGustCap = 0.45f;
        public static float StormGust;
        public static float Wind { get; private set; }

        // Calm breeze: a slow swell with a short puff every ~9 s. Pure, so tests can sample it.
        public static float Breeze(float t)
        {
            double swell = 0.5 + 0.5 * Math.Sin(t * 0.23);
            double u = t % 9.3;
            double puff = u > 7.6 ? Math.Sin((u - 7.6) / 1.7 * Math.PI) : 0.0;
            double b = BreezeMin + (BreezeMax - BreezeMin) * (0.55 * swell + 0.45 * puff);
            return (float)b;
        }

        // Breeze under the storm gust: at full gust the wind is 1.
        public static float WindAt(float breeze, float stormGust)
        {
            if (stormGust < 0f) stormGust = 0f; else if (stormGust > 1f) stormGust = 1f;
            float w = breeze + (1f - breeze) * stormGust;
            return w < 0f ? 0f : (w > 1f ? 1f : w);
        }

        // Called once per frame by GardenLife (gardens and home). Still painting = no wind.
        public static void TickWind(float t)
        {
            if (!BackgroundMotion.On) { Wind = 0f; return; }
            float g = StormGust;
            if (Scene == GardenScene.Winter && g > WinterGustCap) g = WinterGustCap;
            Wind = WindAt(Breeze(t), g);
        }

        // Tests / stills: pin the wind.
        public static void SetWindForTest(float w) { Wind = w < 0f ? 0f : (w > 1f ? 1f : w); }

        // Rain slant grows with the wind (1 calm .. 1.6 at full gust).
        public static float WindLean(float wind) => 1f + 0.6f * (wind < 0f ? 0f : (wind > 1f ? 1f : wind));

        // 1 when the player has asked the painting to hold still.
        public static float RainLean()
        {
            if (!BackgroundMotion.On) return 1f;
            return For(Scene).RainGust;
        }

        // Rain lean with the shared wind on top (rain streaks slant harder in a gust).
        public static float RainLeanWindy()
        {
            if (!BackgroundMotion.On) return 1f;
            return For(Scene).RainGust * WindLean(Wind);
        }

        // Worst of `frames` stepped budgets, in microseconds. Warmup is not counted.
        public static float WorstMicros(bool lively, bool low, int frames)
        {
            if (frames < 1) frames = 1;
            Run(lively, low, 6);
            long worst = 0;
            for (int f = 0; f < frames; f++)
            {
                long t0 = Stopwatch.GetTimestamp();
                Sink += Run(lively, low, 1);
                long dt = Stopwatch.GetTimestamp() - t0;
                if (dt > worst) worst = dt;
            }
            double us = worst * (1000000.0 / Stopwatch.Frequency);
            return (float)us;
        }

        static float Run(bool lively, bool low, int frames)
        {
            int n = 0;
            var scenes = new[]
            {
                GardenScene.Summer, GardenScene.Desert, GardenScene.Fall,
                GardenScene.Winter, GardenScene.Spring
            };
            for (int s = 0; s < scenes.Length; s++)
            {
                var b = lively ? Lively(scenes[s], low) : Base(scenes[s], low);
                if (b.Quads > n) n = b.Quads;
            }
            float speed = lively ? For(GardenScene.Summer).Drift : 1f;
            float sink = 0f;
            for (int f = 0; f < frames; f++)
            {
                float t = f * 0.016f;
                for (int i = 0; i < n; i++)
                {
                    float phase = i * 0.37f;
                    float sway = (float)Math.Sin(t * 0.7f * speed + phase);
                    float x = i * 0.17f + (0.22f * speed + sway * 0.18f) * 0.016f;
                    float y = i * 0.11f - 0.4f * speed * 0.016f;
                    float ang = t * speed + phase;
                    float cs = (float)Math.Cos(ang);
                    float sn = (float)Math.Sin(ang);
                    sink += (x * cs - y * sn) + (x * sn + y * cs);
                }
            }
            return sink;
        }

        static int Scale(int baseline, float scale, int cap)
        {
            if (baseline <= 0) return 0;
            int n = (int)Math.Round(baseline * scale);
            if (n < baseline) n = baseline;
            if (n > cap) n = cap;
            return n;
        }

        static bool Band(float v) => v >= LivelyMin && v <= LivelyMax;

        static Tune Make(float sway, float gust, float gustHz, float waveHz, float life, float drift, float cloud, float shimmer, float rain)
        {
            return new Tune
            {
                Sway = sway,
                Gust = gust,
                GustHz = gustHz,
                WaveHz = waveHz,
                Life = life,
                Drift = drift,
                Cloud = cloud,
                Shimmer = shimmer,
                RainGust = rain
            };
        }
    }
}

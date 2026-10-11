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

            public int Quads
            {
                get
                {
                    return Snow + Leaf + Petal + Fly + Seed + Wisp + Tumble + Cloud + Glint;
                }
            }
        }

        public static GardenScene Scene = GardenScene.Summer;

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
            else if (scene == GardenScene.Spring) b.Petal = low ? 3 : 7;
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
            b.Cloud = Scale(b.Cloud, t.Cloud, low ? 4 : 6);
            b.Glint = Scale(b.Glint, t.Shimmer, low ? 4 : 8);
            return b;
        }

        // 1 when the player has asked the painting to hold still.
        public static float RainLean()
        {
            if (!BackgroundMotion.On) return 1f;
            return For(Scene).RainGust;
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

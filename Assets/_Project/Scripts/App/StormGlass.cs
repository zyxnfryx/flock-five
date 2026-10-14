using UnityEngine;

namespace FlockFive
{
    // Rain on the screen glass, not the world sheet. A few beads while a garden
    // is storming. Each one lives 1–2s, creeps down, and stays off birds and buttons.
    public static class StormGlass
    {
        public const int Cap = 4;
        public const float LifeMin = 1f;
        public const float LifeMax = 2f;
        public const float WetOn = 0.22f;
        public const float RadiusMin = 22f;
        public const float RadiusMax = 44f;
        public const float SlideMin = 20f;
        public const float SlideMax = 42f;
        public const float Drift = 12f;
        public const float ClearPx = 16f;
        public const float GapMin = 0.55f;
        public const float GapMax = 1.15f;
        const int SpawnTries = 14;
        const float RefH = 2556f;

        public struct Bead
        {
            public float X;
            public float Y;
            public float Radius;
            public float Age;
            public float Life;
            public float Vx;
            public float Vy;
        }

        // Screen pixels, origin bottom-left, same space as Camera.WorldToScreenPoint.
        public struct Block
        {
            public float X;
            public float Y;
            public float W;
            public float H;
        }

        public struct Rng
        {
            public uint State;

            public float Next01()
            {
                uint x = State == 0u ? 0xA341316Cu : State;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                State = x;
                return (x & 16777215u) / 16777216f;
            }

            public float Range(float a, float b) => a + (b - a) * Next01();
        }

        public static bool Storming(bool garden, float wet) => garden && wet >= WetOn;

        // IMGUI rects are top-left. Blocks are bottom-left so they match the sim.
        public static Block FromGui(Rect gui, float screenH)
        {
            return new Block
            {
                X = gui.x,
                Y = screenH - (gui.y + gui.height),
                W = gui.width,
                H = gui.height
            };
        }

        public static float Alpha(float age, float life)
        {
            if (life <= 0f || age <= 0f || age >= life) return 0f;
            float inn = Mathf.Clamp01(age / 0.14f);
            float tail = Mathf.Clamp01((life - age) / 0.42f);
            return inn < tail ? inn : tail;
        }

        // The bead lands small and settles. Keep-out uses the full radius.
        public static float ShownRadius(float age, float radius)
        {
            float u = age <= 0f ? 0f : Mathf.Clamp01(age / 0.09f);
            float k = 1f - (1f - u) * (1f - u);
            return radius * (0.58f + 0.42f * k);
        }

        public static int Tick(
            Bead[] beads, int count, float dt,
            bool garden, float wet,
            float areaX, float areaY, float areaW, float areaH,
            Block[] blocks, int blockCount,
            ref Rng rng, ref float spawnIn)
        {
            if (!garden)
            {
                spawnIn = 0.05f;
                return 0;
            }
            if (dt < 0f) dt = 0f;
            if (dt > 0.1f) dt = 0.1f;
            int n = count;
            if (n < 0) n = 0;
            if (beads == null) n = 0;
            if (n > Cap) n = Cap;
            float scale = areaH / RefH;
            if (scale < 0.65f) scale = 0.65f;
            else if (scale > 1.4f) scale = 1.4f;

            for (int i = 0; i < n;)
            {
                beads[i].Age += dt;
                if (beads[i].Age >= beads[i].Life)
                {
                    n = DropAt(beads, n, i);
                    continue;
                }
                float x = beads[i].X + beads[i].Vx * dt;
                float y = beads[i].Y + beads[i].Vy * dt;
                float rad = beads[i].Radius;
                if (Fits(x, y, rad, areaX, areaY, areaW, areaH, blocks, blockCount, beads, n, i))
                {
                    beads[i].X = x;
                    beads[i].Y = y;
                }
                else
                {
                    if (Fits(x, beads[i].Y, rad, areaX, areaY, areaW, areaH, blocks, blockCount, beads, n, i))
                        beads[i].X = x;
                    if (Fits(beads[i].X, y, rad, areaX, areaY, areaW, areaH, blocks, blockCount, beads, n, i))
                        beads[i].Y = y;
                }
                // A bird that walks onto a bead retires it. The glass stays clear.
                if (!Fits(beads[i].X, beads[i].Y, rad, areaX, areaY, areaW, areaH, blocks, blockCount, beads, n, i))
                {
                    n = DropAt(beads, n, i);
                    continue;
                }
                i++;
            }

            if (wet < WetOn)
            {
                spawnIn = 0.05f;
                return n;
            }
            spawnIn -= dt;
            if (spawnIn > 0f || n >= Cap || beads == null) return n;
            if (TrySpawn(beads, ref n, areaX, areaY, areaW, areaH, scale, blocks, blockCount, ref rng))
                spawnIn = rng.Range(GapMin, GapMax);
            else
                spawnIn = 0.18f;
            return n;
        }

        static int DropAt(Bead[] beads, int n, int i)
        {
            n--;
            if (i < n) beads[i] = beads[n];
            return n;
        }

        static bool TrySpawn(
            Bead[] beads, ref int n,
            float areaX, float areaY, float areaW, float areaH, float scale,
            Block[] blocks, int blockCount, ref Rng rng)
        {
            float rad = rng.Range(RadiusMin, RadiusMax) * scale;
            float life = rng.Range(LifeMin, LifeMax);
            float vx = rng.Range(-Drift, Drift) * scale;
            float vy = -rng.Range(SlideMin, SlideMax) * scale;
            float inset = rad + 4f;
            float x0 = areaX + inset;
            float y0 = areaY + inset;
            float x1 = areaX + areaW - inset;
            float y1 = areaY + areaH - inset;
            if (x1 <= x0 || y1 <= y0) return false;
            for (int t = 0; t < SpawnTries; t++)
            {
                float x = rng.Range(x0, x1);
                float y = rng.Range(y0, y1);
                if (!Fits(x, y, rad, areaX, areaY, areaW, areaH, blocks, blockCount, beads, n, -1))
                    continue;
                beads[n] = new Bead
                {
                    X = x,
                    Y = y,
                    Radius = rad,
                    Age = 0f,
                    Life = life,
                    Vx = vx,
                    Vy = vy
                };
                n++;
                return true;
            }
            return false;
        }

        static bool Fits(
            float x, float y, float rad,
            float areaX, float areaY, float areaW, float areaH,
            Block[] blocks, int blockCount,
            Bead[] beads, int beadCount, int self)
        {
            if (x - rad < areaX || y - rad < areaY) return false;
            if (x + rad > areaX + areaW || y + rad > areaY + areaH) return false;
            float keep = rad + ClearPx;
            if (blocks != null)
            {
                int bn = blockCount;
                if (bn > blocks.Length) bn = blocks.Length;
                for (int i = 0; i < bn; i++)
                {
                    if (blocks[i].W <= 0f || blocks[i].H <= 0f) continue;
                    if (CircleHits(x, y, keep, blocks[i])) return false;
                }
            }
            if (beads != null)
            {
                for (int i = 0; i < beadCount; i++)
                {
                    if (i == self) continue;
                    float dx = x - beads[i].X;
                    float dy = y - beads[i].Y;
                    float need = keep + beads[i].Radius;
                    if (dx * dx + dy * dy < need * need) return false;
                }
            }
            return true;
        }

        static bool CircleHits(float x, float y, float rad, Block b)
        {
            float nx = x < b.X ? b.X : (x > b.X + b.W ? b.X + b.W : x);
            float ny = y < b.Y ? b.Y : (y > b.Y + b.H ? b.Y + b.H : y);
            float dx = x - nx;
            float dy = y - ny;
            return dx * dx + dy * dy < rad * rad;
        }
    }
}

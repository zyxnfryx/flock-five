using UnityEngine;

namespace FlockFive
{
    // Rain on the screen glass, storm gardens only. Drops hit, gather into a
    // lens, then slide. The caller owns the bead and speck arrays. Tick only
    // writes those slots, so a frame does not allocate.
    public static class StormGlass
    {
        public const int BeadCap = 16;
        public const int SpeckCap = 28;
        public const int DrawCap = BeadCap + SpeckCap;
        public const int Cap = BeadCap;
        public const byte Slide = 0;
        public const byte Sticky = 1;
        public const bool BlocksTaps = false;

        public const float LifeMin = 2.8f;
        public const float LifeMax = 5.6f;
        public const float StickyLifeMin = 3.4f;
        public const float StickyLifeMax = 6.4f;
        public const float WetOn = 0.22f;
        public const float RadiusMin = 8f;
        public const float RadiusMax = 88f;
        public const float RadiusCap = 130f;
        public const float SplatDur = 0.36f;
        public const float CoverKeep = 0.18f;
        public const float CoverFade = 0.32f;
        public const float GapMin = 0.055f;
        public const float GapMax = 0.12f;
        public const float StickyCreep = 10f;

        const int SpawnTries = 18;
        const float RefH = 2556f;
        const float TrailStep = 10f;
        const float Touch = 0.92f;

        public struct Bead
        {
            public float X;
            public float Y;
            public float Radius;
            public float Age;
            public float Life;
            public float Vx;
            public float Vy;
            public float Phase;
            public float Wobble;
            public float Drift;
            public float Trail;
            public byte Kind;
        }

        // A residual droplet left on the glass. It does not slide.
        public struct Speck
        {
            public float X;
            public float Y;
            public float Radius;
            public float Age;
            public float Life;
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

        // A roll under CoverKeep is the only way a drop lands on a bird or button.
        public static bool KeepOver(float roll) => roll < CoverKeep;

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

        public static float Alpha(float age, float life) => Alpha(age, life, Slide);

        public static float Alpha(float age, float life, byte kind)
        {
            if (life <= 0f || age < 0f || age >= life) return 0f;
            if (kind == Sticky)
            {
                float u = age / life;
                return 1f - u * u;
            }
            float tail = (life - age) / 0.55f;
            if (tail > 1f) return 1f;
            if (tail < 0f) return 0f;
            return tail;
        }

        public static float SpeckAlpha(float age, float life)
        {
            if (life <= 0f || age < 0f || age >= life) return 0f;
            float inn = age / 0.05f;
            if (inn > 1f) inn = 1f;
            float tail = (life - age) / (life * 0.72f);
            if (tail > 1f) tail = 1f;
            if (tail < 0f) tail = 0f;
            float a = inn < tail ? inn : tail;
            return a * 0.92f;
        }

        // Impact is a small flat splat. It gathers into a round bead.
        public static void Shape(float age, float radius, out float rx, out float ry, out float splat)
        {
            float u = SplatDur <= 0f ? 1f : age / SplatDur;
            if (u < 0f) u = 0f;
            else if (u > 1f) u = 1f;
            float k = u * u * (3f - 2f * u);
            splat = 1f - k;
            float wide = 1f + 0.48f * splat;
            float flat = 1f - 0.40f * splat;
            float spread = 1f + 0.10f * splat;
            rx = radius * wide * spread;
            ry = radius * flat * spread;
        }

        public static float ShownRadius(float age, float radius)
        {
            Shape(age, radius, out float rx, out float ry, out _);
            return rx > ry ? rx : ry;
        }

        // 1 on open glass, CoverFade when the center sits on a bird or button.
        public static float CoverMul(float x, float y, Block[] blocks, int blockCount)
        {
            if (!Over(x, y, blocks, blockCount)) return 1f;
            return CoverFade;
        }

        public static bool Over(float x, float y, Block[] blocks, int blockCount)
        {
            if (blocks == null || blockCount <= 0) return false;
            int n = blockCount;
            if (n > blocks.Length) n = blocks.Length;
            for (int i = 0; i < n; i++)
            {
                var b = blocks[i];
                if (b.W <= 0f || b.H <= 0f) continue;
                if (x >= b.X && x <= b.X + b.W && y >= b.Y && y <= b.Y + b.H)
                    return true;
            }
            return false;
        }

        public static int Tick(
            Bead[] beads, int count, float dt,
            bool garden, float wet,
            float areaX, float areaY, float areaW, float areaH,
            Block[] blocks, int blockCount,
            ref Rng rng, ref float spawnIn,
            Speck[] specks, ref int speckN)
        {
            if (!garden)
            {
                spawnIn = 0.05f;
                speckN = 0;
                return 0;
            }
            if (dt < 0f) dt = 0f;
            if (dt > 0.1f) dt = 0.1f;
            int n = count;
            if (n < 0) n = 0;
            if (beads == null) n = 0;
            if (n > BeadCap) n = BeadCap;
            if (n > 0 && n > beads.Length) n = beads.Length;
            float scale = areaH / RefH;
            if (scale < 0.65f) scale = 0.65f;
            else if (scale > 1.4f) scale = 1.4f;

            if (specks != null)
            {
                if (speckN < 0) speckN = 0;
                if (speckN > specks.Length) speckN = specks.Length;
                if (speckN > SpeckCap) speckN = SpeckCap;
                for (int s = 0; s < speckN;)
                {
                    specks[s].Age += dt;
                    if (specks[s].Age >= specks[s].Life)
                    {
                        speckN--;
                        if (s < speckN) specks[s] = specks[speckN];
                        continue;
                    }
                    s++;
                }
            }
            else speckN = 0;

            for (int i = 0; i < n;)
            {
                beads[i].Age += dt;
                if (beads[i].Age >= beads[i].Life)
                {
                    n = DropAt(beads, n, i);
                    continue;
                }
                StepMotion(ref beads[i], dt, scale);
                float ox = beads[i].X;
                float oy = beads[i].Y;
                beads[i].X += beads[i].Vx * dt;
                beads[i].Y += beads[i].Vy * dt;
                float rad = beads[i].Radius;
                float minX = areaX + rad;
                float maxX = areaX + areaW - rad;
                if (beads[i].X < minX)
                {
                    beads[i].X = minX;
                    if (beads[i].Drift < 0f) beads[i].Drift = -beads[i].Drift;
                }
                else if (beads[i].X > maxX)
                {
                    beads[i].X = maxX;
                    if (beads[i].Drift > 0f) beads[i].Drift = -beads[i].Drift;
                }
                if (beads[i].Y + rad < areaY)
                {
                    n = DropAt(beads, n, i);
                    continue;
                }
                float top = areaY + areaH - rad;
                if (beads[i].Y > top) beads[i].Y = top;
                if (beads[i].Kind == Slide)
                    LayTrail(ref beads[i], ox, oy, scale, specks, ref speckN);
                i++;
            }

            n = Merge(beads, n, scale);

            if (wet < WetOn)
            {
                spawnIn = 0.05f;
                return n;
            }
            spawnIn -= dt;
            if (spawnIn > 0f || n >= BeadCap || beads == null) return n;
            if (TrySpawn(beads, ref n, areaX, areaY, areaW, areaH, scale, blocks, blockCount, ref rng, specks, ref speckN))
                spawnIn = rng.Range(GapMin, GapMax);
            else
                spawnIn = 0.16f;
            return n;
        }

        static void StepMotion(ref Bead b, float dt, float scale)
        {
            float wob = b.Wobble;
            if (wob < 0f) wob = 0f;
            b.Phase += dt * (1.7f + wob * 0.015f);
            if (b.Kind == Sticky)
            {
                b.Vx = Mathf.Sin(b.Phase) * 6f * scale;
                b.Vy = -StickyCreep * scale;
                return;
            }
            // The phase rate stays high enough that a planted bead still
            // switches sides inside a short window. The gain is what makes
            // the wander a visible meander on a phone-height pane.
            float side = Mathf.Sin(b.Phase) * wob * 3.2f * scale;
            float side2 = Mathf.Sin(b.Phase * 0.41f + 1.3f) * wob * 0.7f * scale;
            b.Vx = b.Drift * scale + side + side2;
            b.Vy -= Accel(b.Radius, scale) * dt;
            float term = Terminal(b.Radius, scale);
            if (b.Vy < -term) b.Vy = -term;
        }

        // Reference px/s. Bigger beads pull harder and top out faster.
        public static float Terminal(float radiusPx, float scale)
        {
            if (scale < 0.01f) scale = 0.01f;
            float r = radiusPx / scale;
            float u = (r - 8f) / 56f;
            if (u < 0f) u = 0f;
            else if (u > 1f) u = 1f;
            return (42f + u * u * 240f) * scale;
        }

        static float Accel(float radiusPx, float scale)
        {
            if (scale < 0.01f) scale = 0.01f;
            float r = radiusPx / scale;
            float u = (r - 8f) / 56f;
            if (u < 0f) u = 0f;
            else if (u > 1f) u = 1f;
            return (70f + u * 380f) * scale;
        }

        static void LayTrail(ref Bead b, float ox, float oy, float scale, Speck[] specks, ref int speckN)
        {
            float dx = b.X - ox;
            float dy = b.Y - oy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist < 0.01f) return;
            b.Trail += dist;
            float step = TrailStep * scale;
            if (step < 8f) step = 8f;
            if (b.Trail < step) return;
            b.Trail -= step;
            if (b.Trail > step) b.Trail = step * 0.5f;
            float inv = 1f / dist;
            float back = b.Radius * 0.2f;
            float sr = b.Radius * 0.22f;
            float lo = 18f * scale;
            float hi = 36f * scale;
            if (sr < lo) sr = lo;
            if (sr > hi) sr = hi;
            float life = 1.15f + (b.Radius / (RadiusCap * scale)) * 0.7f;
            if (life > 2.2f) life = 2.2f;
            AddSpeck(specks, ref speckN, b.X - dx * inv * back, b.Y - dy * inv * back, sr, life);
        }

        static int Merge(Bead[] beads, int n, float scale)
        {
            if (beads == null) return 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n;)
                {
                    float dx = beads[i].X - beads[j].X;
                    float dy = beads[i].Y - beads[j].Y;
                    float touch = (beads[i].Radius + beads[j].Radius) * Touch;
                    if (dx * dx + dy * dy > touch * touch)
                    {
                        j++;
                        continue;
                    }
                    Absorb(ref beads[i], beads[j], scale);
                    n = DropAt(beads, n, j);
                }
            }
            return n;
        }

        static void Absorb(ref Bead a, Bead b, float scale)
        {
            float a1 = a.Radius * a.Radius;
            float a2 = b.Radius * b.Radius;
            float sum = a1 + a2;
            if (sum < 1e-3f) return;
            a.X = (a.X * a1 + b.X * a2) / sum;
            a.Y = (a.Y * a1 + b.Y * a2) / sum;
            // A little fuller than the two areas, so the join reads on the pane.
            float nr = Mathf.Sqrt(sum) * 1.18f;
            float cap = RadiusCap * scale;
            if (nr > cap) nr = cap;
            float prev = a.Vy < 0f ? -a.Vy : a.Vy;
            float other = b.Vy < 0f ? -b.Vy : b.Vy;
            if (other > prev) prev = other;
            bool bothSticky = a.Kind == Sticky && b.Kind == Sticky;
            a.Radius = nr;
            if (b.Wobble > a.Wobble) a.Wobble = b.Wobble;
            if (a2 > a1)
            {
                a.Phase = b.Phase;
                a.Drift = b.Drift;
            }
            float stickyCut = 16f * scale;
            if (bothSticky && nr < stickyCut)
            {
                a.Kind = Sticky;
                a.Vy = -StickyCreep * scale;
                a.Vx = 0f;
            }
            else
            {
                a.Kind = Slide;
                float term = Terminal(nr, scale);
                float faster = prev * 1.35f;
                if (faster < term * 0.45f) faster = term * 0.45f;
                if (faster > term) faster = term;
                if (faster < prev + 0.5f && term > prev + 0.5f)
                    faster = prev + (term - prev) * 0.35f;
                if (faster > term) faster = term;
                a.Vy = -faster;
            }
            float remA = a.Life - a.Age;
            float remB = b.Life - b.Age;
            if (remB > remA) a.Life = a.Age + remB;
            if (a.Age < SplatDur) a.Age = SplatDur;
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
            Block[] blocks, int blockCount, ref Rng rng,
            Speck[] specks, ref int speckN)
        {
            if (n >= BeadCap || n >= beads.Length) return false;
            float roll = rng.Next01();
            float radRef;
            if (roll < 0.36f) radRef = rng.Range(9f, 16f);
            else if (roll < 0.70f) radRef = rng.Range(28f, 52f);
            else radRef = rng.Range(60f, RadiusMax);
            byte kind = Slide;
            float life;
            if (radRef < 15.5f && rng.Next01() < 0.42f)
            {
                kind = Sticky;
                life = rng.Range(StickyLifeMin, StickyLifeMax);
            }
            else life = rng.Range(LifeMin, LifeMax);
            float rad = radRef * scale;
            float inset = rad + 4f;
            float x0 = areaX + inset;
            float y0 = areaY + inset;
            float x1 = areaX + areaW - inset;
            float y1 = areaY + areaH - inset;
            if (x1 <= x0 || y1 <= y0) return false;
            float yLo = y0 + (y1 - y0) * 0.12f;
            // One cover roll for the whole attempt. Retries look for open glass,
            // so a bird does not fill up, and a fully covered pane stays thin.
            bool allowCover = KeepOver(rng.Next01());
            for (int t = 0; t < SpawnTries; t++)
            {
                // Three lanes in the open middle, so a fat bead runs down
                // the clear column and catches the ones ahead of it.
                float x;
                if (rng.Next01() < 0.80f)
                {
                    int lane = (int)(rng.Next01() * 3f);
                    if (lane > 2) lane = 2;
                    float span = x1 - x0;
                    float band0 = x0 + span * 0.20f;
                    float band1 = x0 + span * 0.80f;
                    float laneX = band0 + (band1 - band0) * ((lane + 0.5f) / 3f);
                    x = laneX + rng.Range(-22f, 22f) * scale;
                    if (x < x0) x = x0;
                    else if (x > x1) x = x1;
                }
                else x = rng.Range(x0, x1);
                float y = rng.Range(yLo, y1);
                if (Over(x, y, blocks, blockCount) && !allowCover)
                    continue;
                if (!ClearOf(x, y, rad, beads, n))
                    continue;
                float drift = kind == Sticky ? rng.Range(-4f, 4f) : rng.Range(-22f, 22f);
                float wobble = kind == Sticky ? rng.Range(4f, 10f) : rng.Range(32f, 72f);
                float vy = kind == Sticky ? -StickyCreep : -rng.Range(4f, 12f);
                beads[n] = new Bead
                {
                    X = x,
                    Y = y,
                    Radius = rad,
                    Age = 0f,
                    Life = life,
                    Vx = 0f,
                    Vy = vy * scale,
                    Phase = rng.Range(0f, 6.28318f),
                    Wobble = wobble,
                    Drift = drift,
                    Trail = 0f,
                    Kind = kind
                };
                n++;
                Burst(specks, ref speckN, x, y, rad, scale, ref rng);
                return true;
            }
            return false;
        }

        static bool ClearOf(float x, float y, float rad, Bead[] beads, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float dx = x - beads[i].X;
                float dy = y - beads[i].Y;
                float need = (rad + beads[i].Radius) * 0.72f;
                if (dx * dx + dy * dy < need * need) return false;
            }
            return true;
        }

        static void Burst(Speck[] specks, ref int speckN, float x, float y, float rad, float scale, ref Rng rng)
        {
            for (int k = 0; k < 4; k++)
            {
                float ang = rng.Range(0f, 6.28318f);
                float d = rad * rng.Range(0.9f, 1.35f);
                float sr = rng.Range(9f, 16f) * scale;
                AddSpeck(specks, ref speckN,
                    x + Mathf.Cos(ang) * d, y + Mathf.Sin(ang) * d,
                    sr, rng.Range(0.40f, 0.75f));
            }
        }

        static void AddSpeck(Speck[] specks, ref int n, float x, float y, float rad, float life)
        {
            if (specks == null || rad <= 0f || life <= 0f) return;
            int cap = specks.Length;
            if (cap > SpeckCap) cap = SpeckCap;
            if (cap <= 0) return;
            int i;
            if (n < cap)
            {
                i = n;
                n++;
            }
            else
            {
                i = 0;
                float oldest = specks[0].Age;
                int limit = n < cap ? n : cap;
                for (int k = 1; k < limit; k++)
                {
                    if (specks[k].Age > oldest)
                    {
                        oldest = specks[k].Age;
                        i = k;
                    }
                }
            }
            specks[i] = new Speck
            {
                X = x,
                Y = y,
                Radius = rad,
                Age = 0f,
                Life = life
            };
        }
    }
}

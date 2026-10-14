#if UNITY_EDITOR
using UnityEngine;

namespace FlockFive.Editor
{
    // Glass beads exist only on a storming garden, miss birds and buttons, and
    // each one is gone within 1–2 seconds.
    public static class StormGlassTests
    {
        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[storm-glass] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            Check("only-storm-gardens",
                !StormGlass.Storming(false, 1f) && !StormGlass.Storming(true, 0f)
                && !StormGlass.Storming(true, StormGlass.WetOn - 0.01f)
                && StormGlass.Storming(true, StormGlass.WetOn),
                "wet " + StormGlass.WetOn.ToString("0.00"));

            const float w = 1179f;
            const float h = 2556f;
            var safe = new Rect(0f, 102f, w, h - 180f);
            float? keep = DeviceFit.TestWindow;
            DeviceFit.TestWindow = w / h;
            CornerAnchor.Garden(w, h, safe, out _, out _, out _, out var restart, out var hive);
            var stamp = FlockFiveApp.GardenStampBox(w, h, safe, DeviceFit.Ui(w, h));
            DeviceFit.TestWindow = keep;
            var blocks = new StormGlass.Block[6];
            blocks[0] = StormGlass.FromGui(restart, h);
            blocks[1] = StormGlass.FromGui(hive, h);
            blocks[2] = StormGlass.FromGui(stamp, h);
            // Two perched birds in the middle of the glass.
            blocks[3] = new StormGlass.Block { X = 180f, Y = 980f, W = 340f, H = 420f };
            blocks[4] = new StormGlass.Block { X = 640f, Y = 1100f, W = 380f, H = 460f };
            int blockN = 5;

            var beads = new StormGlass.Bead[StormGlass.Cap];
            var again = new StormGlass.Bead[StormGlass.Cap];
            int hits = 0;
            int spawned = 0;
            int lifeBad = 0;
            int off = 0;
            float lifeLo = 99f;
            float lifeHi = 0f;
            int seeds = 24;
            for (int s = 0; s < seeds; s++)
            {
                var rng = new StormGlass.Rng { State = (uint)(s * 9973 + 11) };
                float spawn = 0f;
                int n = 0;
                for (int step = 0; step < 160; step++)
                {
                    n = StormGlass.Tick(beads, n, 1f / 60f, true, 1f, 0f, 0f, w, h, blocks, blockN, ref rng, ref spawn);
                    for (int i = 0; i < n; i++)
                    {
                        if (beads[i].Age == 0f)
                        {
                            spawned++;
                            if (beads[i].Life < StormGlass.LifeMin || beads[i].Life > StormGlass.LifeMax)
                                lifeBad++;
                            if (beads[i].Life < lifeLo) lifeLo = beads[i].Life;
                            if (beads[i].Life > lifeHi) lifeHi = beads[i].Life;
                        }
                        if (beads[i].X - beads[i].Radius < -0.01f || beads[i].Y - beads[i].Radius < -0.01f
                            || beads[i].X + beads[i].Radius > w + 0.01f || beads[i].Y + beads[i].Radius > h + 0.01f)
                            off++;
                        if (Touches(beads[i].X, beads[i].Y, beads[i].Radius, blocks, blockN))
                            hits++;
                    }
                }
            }
            Check("storm-spawns-beads", spawned > 40, "spawned " + spawned);
            Check("life-is-one-to-two", lifeBad == 0 && lifeLo >= StormGlass.LifeMin && lifeHi <= StormGlass.LifeMax,
                "bad " + lifeBad + " range " + lifeLo.ToString("0.00") + ".." + lifeHi.ToString("0.00"));
            Check("clear-of-birds-and-buttons", hits == 0 && off == 0, "hits " + hits + " off " + off);

            int dryN = 0;
            var dryRng = new StormGlass.Rng { State = 3u };
            float drySpawn = 0f;
            for (int step = 0; step < 180; step++)
                dryN = StormGlass.Tick(beads, dryN, 1f / 30f, true, 0f, 0f, 0f, w, h, blocks, blockN, ref dryRng, ref drySpawn);
            int homeN = 0;
            var homeRng = new StormGlass.Rng { State = 9u };
            float homeSpawn = 0f;
            for (int step = 0; step < 180; step++)
                homeN = StormGlass.Tick(beads, homeN, 1f / 30f, false, 1f, 0f, 0f, w, h, blocks, 0, ref homeRng, ref homeSpawn);
            Check("dry-and-home-stay-clear", dryN == 0 && homeN == 0, "dry " + dryN + " home " + homeN);

            var wall = new StormGlass.Block[] { new StormGlass.Block { X = 0f, Y = 0f, W = w, H = h } };
            int wallN = 0;
            var wallRng = new StormGlass.Rng { State = 42u };
            float wallSpawn = 0f;
            for (int step = 0; step < 120; step++)
                wallN = StormGlass.Tick(beads, wallN, 1f / 30f, true, 1f, 0f, 0f, w, h, wall, 1, ref wallRng, ref wallSpawn);
            Check("blocked-glass-spawns-none", wallN == 0, "n " + wallN);

            bool slid = false;
            string slideDetail = "no bead";
            for (uint seed = 1; seed < 80 && !slid; seed++)
            {
                var rng = new StormGlass.Rng { State = seed * 17u };
                float spawn = 0f;
                int n = 0;
                for (int step = 0; step < 90 && n == 0; step++)
                    n = StormGlass.Tick(beads, n, 1f / 60f, true, 1f, 0f, 0f, w, h, blocks, 0, ref rng, ref spawn);
                if (n == 0) continue;
                float x0 = beads[0].X;
                float y0 = beads[0].Y;
                if (y0 < 500f || y0 > h - 500f) continue;
                for (int step = 0; step < 30; step++)
                    n = StormGlass.Tick(beads, n, 1f / 60f, true, 1f, 0f, 0f, w, h, blocks, 0, ref rng, ref spawn);
                if (n == 0) continue;
                float dx = beads[0].X - x0;
                float dy = beads[0].Y - y0;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                slid = dist > 6f && dist < 40f && dy < 0f;
                slideDetail = "dist " + dist.ToString("0.0") + " dy " + dy.ToString("0.0");
            }
            Check("slides-a-little", slid, slideDetail);

            bool lived = false;
            string lifeDetail = "no bead";
            for (uint seed = 1; seed < 40 && !lived; seed++)
            {
                var rng = new StormGlass.Rng { State = seed * 91u };
                float spawn = 0f;
                int n = 0;
                for (int step = 0; step < 90 && n == 0; step++)
                    n = StormGlass.Tick(beads, n, 1f / 60f, true, 1f, 0f, 0f, w, h, blocks, 0, ref rng, ref spawn);
                if (n != 1) continue;
                float life = beads[0].Life;
                float t = 0f;
                int guard = 0;
                while (n > 0 && guard++ < 80)
                {
                    n = StormGlass.Tick(beads, n, 0.05f, true, 0f, 0f, 0f, w, h, blocks, 0, ref rng, ref spawn);
                    if (n > 0) t += 0.05f;
                }
                float elapsed = t + 0.05f;
                lived = n == 0 && elapsed >= life - 0.001f && elapsed <= life + 0.06f;
                lifeDetail = "t " + elapsed.ToString("0.00") + " life " + life.ToString("0.00");
            }
            Check("fades-within-life", lived, lifeDetail);

            float a0 = StormGlass.Alpha(0f, 1.4f);
            float amid = StormGlass.Alpha(0.5f, 1.4f);
            float aEnd = StormGlass.Alpha(1.4f, 1.4f);
            Check("alpha-fades", a0 == 0f && amid > 0.8f && aEnd == 0f,
                "0 " + a0.ToString("0.00") + " mid " + amid.ToString("0.00") + " end " + aEnd.ToString("0.00"));

            var rngA = new StormGlass.Rng { State = 99u };
            var rngB = new StormGlass.Rng { State = 99u };
            float sA = 0f, sB = 0f;
            int nA = 0, nB = 0;
            for (int step = 0; step < 40; step++)
            {
                nA = StormGlass.Tick(beads, nA, 1f / 60f, true, 1f, 0f, 0f, w, h, blocks, blockN, ref rngA, ref sA);
                nB = StormGlass.Tick(again, nB, 1f / 60f, true, 1f, 0f, 0f, w, h, blocks, blockN, ref rngB, ref sB);
            }
            bool same = nA == nB && nA > 0;
            for (int i = 0; same && i < nA; i++)
                same = beads[i].X == again[i].X && beads[i].Y == again[i].Y && beads[i].Life == again[i].Life;
            Check("same-seed-repeats", same, "n " + nA + "/" + nB);

            // Leaving the garden drops every bead on the next tick.
            int left = nA;
            float leftSpawn = 0f;
            var leftRng = new StormGlass.Rng { State = 1u };
            left = StormGlass.Tick(beads, left, 0.016f, false, 1f, 0f, 0f, w, h, blocks, blockN, ref leftRng, ref leftSpawn);
            Check("leave-garden-clears", left == 0 && nA > 0, "before " + nA + " after " + left);

            string end = fail == 0 ? "STORM_GLASS_OK " + pass : "STORM_GLASS_FAIL " + fail + " passed " + pass;
            Debug.Log("[storm-glass] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static bool Touches(float x, float y, float rad, StormGlass.Block[] blocks, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var b = blocks[i];
                float nx = x < b.X ? b.X : (x > b.X + b.W ? b.X + b.W : x);
                float ny = y < b.Y ? b.Y : (y > b.Y + b.H ? b.Y + b.H : y);
                float dx = x - nx;
                float dy = y - ny;
                if (dx * dx + dy * dy <= rad * rad) return true;
            }
            return false;
        }
    }
}
#endif

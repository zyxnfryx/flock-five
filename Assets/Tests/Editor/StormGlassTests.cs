#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace FlockFive.Editor
{
    // Storm glass: more drops, only while a garden is storming, pooled, and
    // they never own a raycast. Birds and buttons get fewer, fainter drops.
    public static class StormGlassTests
    {
        const float W = 1179f;
        const float H = 2556f;

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

            Check("keeps-few-over-cover",
                StormGlass.KeepOver(StormGlass.CoverKeep * 0.5f)
                && !StormGlass.KeepOver(StormGlass.CoverKeep + 0.05f)
                && StormGlass.CoverFade < 0.5f && StormGlass.CoverFade > 0.05f,
                "keep " + StormGlass.CoverKeep.ToString("0.00") + " fade " + StormGlass.CoverFade.ToString("0.00"));

            var blocks = UiBlocks();
            var half = new StormGlass.Block[] { new StormGlass.Block { X = 0f, Y = 0f, W = W * 0.5f, H = H } };
            var beads = new StormGlass.Bead[StormGlass.BeadCap];
            var again = new StormGlass.Bead[StormGlass.BeadCap];
            var specks = new StormGlass.Speck[StormGlass.SpeckCap];
            var specksB = new StormGlass.Speck[StormGlass.SpeckCap];

            int peak = 0;
            int speckPeak = 0;
            int overCap = 0;
            int spawned = 0;
            int stickies = 0;
            int lifeBad = 0;
            float lifeLo = 99f;
            float lifeHi = 0f;
            float radLo = 999f;
            float radHi = 0f;
            int inside = 0;
            int outside = 0;
            int seeds = 28;
            for (int s = 0; s < seeds; s++)
            {
                var rng = new StormGlass.Rng { State = (uint)(s * 9973 + 11) };
                float spawn = 0f;
                int n = 0;
                int speckN = 0;
                for (int step = 0; step < 200; step++)
                {
                    n = Step(beads, n, 1f / 60f, true, 1f, half, 1, ref rng, ref spawn, specks, ref speckN);
                    if (n > StormGlass.BeadCap || speckN > StormGlass.SpeckCap) overCap++;
                    if (n > peak) peak = n;
                    if (speckN > speckPeak) speckPeak = speckN;
                    for (int i = 0; i < n; i++)
                    {
                        if (beads[i].Age != 0f) continue;
                        spawned++;
                        if (beads[i].Kind == StormGlass.Sticky) stickies++;
                        bool sticky = beads[i].Kind == StormGlass.Sticky;
                        float lo = sticky ? StormGlass.StickyLifeMin : StormGlass.LifeMin;
                        float hi = sticky ? StormGlass.StickyLifeMax : StormGlass.LifeMax;
                        if (beads[i].Life < lo - 0.001f || beads[i].Life > hi + 0.001f) lifeBad++;
                        if (beads[i].Life < lifeLo) lifeLo = beads[i].Life;
                        if (beads[i].Life > lifeHi) lifeHi = beads[i].Life;
                        if (beads[i].Radius < radLo) radLo = beads[i].Radius;
                        if (beads[i].Radius > radHi) radHi = beads[i].Radius;
                        if (beads[i].X < W * 0.5f) inside++;
                        else outside++;
                    }
                }
            }
            Check("caps-hold", overCap == 0 && peak <= StormGlass.BeadCap && speckPeak <= StormGlass.SpeckCap,
                "peak " + peak + " specks " + speckPeak + " over " + overCap);
            Check("pool-fills", peak >= 10 && speckPeak >= 8,
                "beads " + peak + " specks " + speckPeak);
            Check("spawns-many", spawned > 120, "spawned " + spawned);
            Check("sizes-vary", radLo < 16f && radHi > 55f && radHi <= StormGlass.RadiusMax + 0.01f,
                "r " + radLo.ToString("0.0") + ".." + radHi.ToString("0.0"));
            Check("some-stick", stickies > 8, "sticky " + stickies);
            Check("life-in-range", lifeBad == 0 && lifeLo >= StormGlass.LifeMin - 0.001f && lifeHi <= StormGlass.StickyLifeMax + 0.001f,
                "bad " + lifeBad + " " + lifeLo.ToString("0.00") + ".." + lifeHi.ToString("0.00"));
            float inFrac = (inside + outside) > 0 ? (float)inside / (inside + outside) : 1f;
            Check("thinner-on-a-covered-half", inside > 4 && outside > inside && inFrac < 0.40f && inFrac > 0.02f,
                "in " + inside + " out " + outside + " frac " + inFrac.ToString("0.00"));

            int birdIn = 0, birdOut = 0;
            float blockArea = 0f;
            for (int i = 0; i < 5; i++) blockArea += blocks[i].W * blocks[i].H;
            for (int s = 0; s < 20; s++)
            {
                var rng = new StormGlass.Rng { State = (uint)(s * 1301 + 3) };
                float spawn = 0f;
                int n = 0;
                int speckN = 0;
                for (int step = 0; step < 160; step++)
                {
                    n = Step(beads, n, 1f / 60f, true, 1f, blocks, 5, ref rng, ref spawn, specks, ref speckN);
                    for (int i = 0; i < n; i++)
                    {
                        if (beads[i].Age != 0f) continue;
                        if (StormGlass.Over(beads[i].X, beads[i].Y, blocks, 5)) birdIn++;
                        else birdOut++;
                    }
                }
            }
            float areaFrac = blockArea / (W * H);
            float birdFrac = (birdIn + birdOut) > 0 ? (float)birdIn / (birdIn + birdOut) : 1f;
            Check("thinner-over-birds-and-buttons", birdOut > birdIn * 2 && birdFrac < areaFrac * 0.6f,
                "in " + birdIn + " out " + birdOut + " frac " + birdFrac.ToString("0.000")
                + " area " + areaFrac.ToString("0.000"));

            var one = new StormGlass.Block[] { new StormGlass.Block { X = 100f, Y = 100f, W = 200f, H = 180f } };
            float overA = StormGlass.CoverMul(150f, 160f, one, 1);
            float openA = StormGlass.CoverMul(20f, 20f, one, 1);
            Check("fainter-over-blocks", overA == StormGlass.CoverFade && openA == 1f,
                "over " + overA.ToString("0.00") + " open " + openA.ToString("0.00"));

            int dryN = 0;
            var dryRng = new StormGlass.Rng { State = 3u };
            float drySpawn = 0f;
            int drySpeck = 4;
            for (int step = 0; step < 180; step++)
                dryN = Step(beads, dryN, 1f / 30f, true, 0f, blocks, 5, ref dryRng, ref drySpawn, specks, ref drySpeck);
            int homeN = 0;
            var homeRng = new StormGlass.Rng { State = 9u };
            float homeSpawn = 0f;
            int homeSpeck = 3;
            for (int step = 0; step < 180; step++)
                homeN = Step(beads, homeN, 1f / 30f, false, 1f, blocks, 0, ref homeRng, ref homeSpawn, specks, ref homeSpeck);
            Check("dry-and-home-stay-clear", dryN == 0 && homeN == 0 && homeSpeck == 0,
                "dry " + dryN + " home " + homeN + " specks " + homeSpeck);

            int wallN = 0;
            int openN = 0;
            var wallRng = new StormGlass.Rng { State = 42u };
            var openRng = new StormGlass.Rng { State = 42u };
            float wallSpawn = 0f;
            float openSpawn = 0f;
            int wallSpeck = 0;
            int openSpeck = 0;
            int coveredSpawns = 0;
            int clearSpawns = 0;
            var wall = new StormGlass.Block[] { new StormGlass.Block { X = 0f, Y = 0f, W = W, H = H } };
            for (int step = 0; step < 240; step++)
            {
                int was = wallN;
                wallN = Step(beads, wallN, 1f / 60f, true, 1f, wall, 1, ref wallRng, ref wallSpawn, specks, ref wallSpeck);
                if (wallN > was) coveredSpawns += wallN - was;
                was = openN;
                openN = Step(again, openN, 1f / 60f, true, 1f, blocks, 0, ref openRng, ref openSpawn, specksB, ref openSpeck);
                if (openN > was) clearSpawns += openN - was;
            }
            Check("covered-glass-spawns-fewer",
                coveredSpawns > 0 && clearSpawns > coveredSpawns * 2,
                "covered " + coveredSpawns + " clear " + clearSpawns);

            StormGlass.Shape(0f, 30f, out float rx0, out float ry0, out float splat0);
            StormGlass.Shape(StormGlass.SplatDur, 30f, out float rx1, out float ry1, out float splat1);
            Check("gathers-from-a-splat",
                splat0 > 0.9f && rx0 > ry0 * 1.6f && ry0 > 8f
                && splat1 < 0.05f && Mathf.Abs(rx1 - ry1) < 0.2f && rx1 > 28f && rx1 < 32f,
                "splat " + splat0.ToString("0.00") + " " + rx0.ToString("0.0") + "x" + ry0.ToString("0.0")
                + " bead " + splat1.ToString("0.00") + " " + rx1.ToString("0.0"));

            beads[0] = new StormGlass.Bead
            {
                X = 500f, Y = 1600f, Radius = 24f, Age = 0.4f, Life = 4f,
                Vy = -20f, Phase = 0.4f, Wobble = 40f, Drift = 0f, Kind = StormGlass.Slide
            };
            var wanderRng = new StormGlass.Rng { State = 1u };
            float wanderSpawn = 1f;
            int wanderSpeck = 0;
            int wn = 1;
            float prevX = beads[0].X;
            bool left = false, right = false;
            float y0 = beads[0].Y;
            for (int step = 0; step < 100; step++)
            {
                wn = Step(beads, wn, 1f / 60f, true, 0f, blocks, 0, ref wanderRng, ref wanderSpawn, specks, ref wanderSpeck);
                if (wn < 1) break;
                float dx = beads[0].X - prevX;
                if (dx > 0.05f) right = true;
                if (dx < -0.05f) left = true;
                prevX = beads[0].X;
            }
            float drop = wn > 0 ? y0 - beads[0].Y : 0f;
            Check("slides-and-meanders", wn == 1 && left && right && drop > 30f,
                "drop " + drop.ToString("0.0") + " left " + left + " right " + right);

            beads[0] = new StormGlass.Bead
            {
                X = 500f, Y = 1800f, Radius = 40f, Age = 0.5f, Life = 4f,
                Vy = -90f, Kind = StormGlass.Slide, Wobble = 0f, Drift = 0f
            };
            var trailRng = new StormGlass.Rng { State = 8u };
            float trailSpawn = 1f;
            int trailSpeck = 0;
            int tn = 1;
            for (int step = 0; step < 40; step++)
                tn = Step(beads, tn, 1f / 60f, true, 0f, blocks, 0, ref trailRng, ref trailSpawn, specks, ref trailSpeck);
            float speckLo = 99f;
            for (int i = 0; i < trailSpeck; i++)
                if (specks[i].Radius < speckLo) speckLo = specks[i].Radius;
            Check("lays-a-wet-trail", tn == 1 && trailSpeck >= 4 && speckLo >= 16f,
                "specks " + trailSpeck + " r " + speckLo.ToString("0.0"));

            beads[0] = new StormGlass.Bead
            {
                X = 220f, Y = 1900f, Radius = 12f, Age = 0.5f, Life = 4f,
                Vy = -5f, Kind = StormGlass.Slide
            };
            beads[1] = new StormGlass.Bead
            {
                X = 900f, Y = 1900f, Radius = 44f, Age = 0.5f, Life = 4f,
                Vy = -5f, Kind = StormGlass.Slide
            };
            var growRng = new StormGlass.Rng { State = 2u };
            float growSpawn = 1f;
            int growSpeck = 0;
            int gn = 2;
            for (int step = 0; step < 25; step++)
                gn = Step(beads, gn, 1f / 60f, true, 0f, blocks, 0, ref growRng, ref growSpawn, specks, ref growSpeck);
            float smallVy = 0f, bigVy = 0f;
            for (int i = 0; i < gn; i++)
            {
                if (beads[i].Radius < 20f) smallVy = beads[i].Vy;
                if (beads[i].Radius > 30f) bigVy = beads[i].Vy;
            }
            Check("speeds-up-as-it-grows", gn == 2 && bigVy < smallVy - 20f && bigVy < -40f,
                "small " + smallVy.ToString("0.0") + " big " + bigVy.ToString("0.0"));

            beads[0] = new StormGlass.Bead
            {
                X = 400f, Y = 1500f, Radius = 26f, Age = 0.5f, Life = 4f,
                Vy = -8f, Kind = StormGlass.Slide
            };
            float sped0 = beads[0].Vy;
            var fallRng = new StormGlass.Rng { State = 3u };
            float fallSpawn = 1f;
            int fallSpeck = 0;
            int fn = 1;
            for (int step = 0; step < 18; step++)
                fn = Step(beads, fn, 1f / 60f, true, 0f, blocks, 0, ref fallRng, ref fallSpawn, specks, ref fallSpeck);
            Check("grows-faster-down", fn == 1 && beads[0].Vy < sped0 - 12f,
                "vy " + sped0.ToString("0.0") + " -> " + beads[0].Vy.ToString("0.0"));

            beads[0] = new StormGlass.Bead
            {
                X = 400f, Y = 1400f, Radius = 22f, Age = 0.5f, Life = 4f,
                Vy = -40f, Kind = StormGlass.Slide
            };
            beads[1] = new StormGlass.Bead
            {
                X = 430f, Y = 1390f, Radius = 18f, Age = 0.45f, Life = 4f,
                Vy = -36f, Kind = StormGlass.Slide
            };
            float prevSpeed = 40f;
            float prevRad = 22f;
            var mergeRng = new StormGlass.Rng { State = 4u };
            float mergeSpawn = 1f;
            int mergeSpeck = 0;
            int mn = Step(beads, 2, 1f / 60f, true, 0f, blocks, 0, ref mergeRng, ref mergeSpawn, specks, ref mergeSpeck);
            Check("merge-grows-and-speeds-up",
                mn == 1 && beads[0].Radius > prevRad + 4f && -beads[0].Vy > prevSpeed + 2f
                && beads[0].Kind == StormGlass.Slide,
                "n " + mn + " r " + beads[0].Radius.ToString("0.0") + " vy " + beads[0].Vy.ToString("0.0"));

            beads[0] = new StormGlass.Bead
            {
                X = 600f, Y = 1200f, Radius = 11f, Age = 0.3f, Life = 4f,
                Vy = 0f, Kind = StormGlass.Sticky, Wobble = 0f, Drift = 0f
            };
            float sx = beads[0].X;
            float sy = beads[0].Y;
            float aEarly = StormGlass.Alpha(0.3f, 4f, StormGlass.Sticky);
            var stickRng = new StormGlass.Rng { State = 5u };
            float stickSpawn = 1f;
            int stickSpeck = 0;
            int sn = 1;
            for (int step = 0; step < 90; step++)
                sn = Step(beads, sn, 1f / 60f, true, 0f, blocks, 0, ref stickRng, ref stickSpawn, specks, ref stickSpeck);
            float moved = 0f;
            float aLate = 0f;
            if (sn == 1)
            {
                float dx = beads[0].X - sx;
                float dy = beads[0].Y - sy;
                moved = Mathf.Sqrt(dx * dx + dy * dy);
                aLate = StormGlass.Alpha(beads[0].Age, beads[0].Life, beads[0].Kind);
            }
            Check("sticky-stays-and-fades",
                sn == 1 && beads[0].Kind == StormGlass.Sticky && moved < 22f && aLate < aEarly - 0.08f && aLate > 0.2f,
                "moved " + moved.ToString("0.0") + " a " + aEarly.ToString("0.00") + " -> " + aLate.ToString("0.00"));

            beads[0] = new StormGlass.Bead
            {
                X = 500f, Y = 1400f, Radius = 20f, Age = 0f, Life = 1.2f, Kind = StormGlass.Slide
            };
            var fadeRng = new StormGlass.Rng { State = 6u };
            float fadeSpawn = 1f;
            int fadeSpeck = 0;
            int fadeN = 1;
            float elapsed = 0f;
            int guard = 0;
            while (fadeN > 0 && guard++ < 80)
            {
                fadeN = Step(beads, fadeN, 0.05f, true, 0f, blocks, 0, ref fadeRng, ref fadeSpawn, specks, ref fadeSpeck);
                if (fadeN > 0) elapsed += 0.05f;
            }
            elapsed += 0.05f;
            Check("fades-within-life", fadeN == 0 && elapsed >= 1.2f - 0.001f && elapsed <= 1.26f,
                "t " + elapsed.ToString("0.00"));

            float a0 = StormGlass.Alpha(0f, 1.4f);
            float amid = StormGlass.Alpha(0.5f, 1.4f);
            float aEnd = StormGlass.Alpha(1.4f, 1.4f);
            float stickyMid = StormGlass.Alpha(2f, 4f, StormGlass.Sticky);
            Check("alpha-holds-then-ends", a0 > 0.9f && amid > 0.9f && aEnd == 0f && stickyMid < 0.85f && stickyMid > 0.5f,
                "0 " + a0.ToString("0.00") + " mid " + amid.ToString("0.00") + " end " + aEnd.ToString("0.00")
                + " sticky " + stickyMid.ToString("0.00"));

            long warmBefore = System.GC.GetAllocatedBytesForCurrentThread();
            var allocRng = new StormGlass.Rng { State = 77u };
            float allocSpawn = 0f;
            int allocN = 0;
            int allocSpeck = 0;
            for (int step = 0; step < 40; step++)
                allocN = Step(beads, allocN, 1f / 60f, true, 1f, half, 1, ref allocRng, ref allocSpawn, specks, ref allocSpeck);
            beads[0] = new StormGlass.Bead
            {
                X = 300f, Y = 1000f, Radius = 20f, Age = 0.4f, Life = 3f, Vy = -30f, Kind = StormGlass.Slide
            };
            beads[1] = new StormGlass.Bead
            {
                X = 318f, Y = 990f, Radius = 16f, Age = 0.4f, Life = 3f, Vy = -28f, Kind = StormGlass.Slide
            };
            allocN = Step(beads, 2, 1f / 60f, true, 0f, half, 1, ref allocRng, ref allocSpawn, specks, ref allocSpeck);
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int step = 0; step < 240; step++)
                allocN = Step(beads, allocN, 1f / 60f, true, 1f, half, 1, ref allocRng, ref allocSpawn, specks, ref allocSpeck);
            long after = System.GC.GetAllocatedBytesForCurrentThread();
            Check("tick-allocates-nothing", after == before && allocN <= StormGlass.BeadCap,
                "bytes " + (after - before) + " warm " + (before - warmBefore) + " n " + allocN);

            var rngA = new StormGlass.Rng { State = 99u };
            var rngB = new StormGlass.Rng { State = 99u };
            float sA = 0f, sB = 0f;
            int nA = 0, nB = 0, spA = 0, spB = 0;
            for (int step = 0; step < 50; step++)
            {
                nA = Step(beads, nA, 1f / 60f, true, 1f, blocks, 5, ref rngA, ref sA, specks, ref spA);
                nB = Step(again, nB, 1f / 60f, true, 1f, blocks, 5, ref rngB, ref sB, specksB, ref spB);
            }
            bool same = nA == nB && nA > 0 && spA == spB;
            for (int i = 0; same && i < nA; i++)
                same = beads[i].X == again[i].X && beads[i].Y == again[i].Y && beads[i].Life == again[i].Life
                    && beads[i].Kind == again[i].Kind;
            for (int i = 0; same && i < spA; i++)
                same = specks[i].X == specksB[i].X && specks[i].Y == specksB[i].Y;
            Check("same-seed-repeats", same, "n " + nA + "/" + nB + " specks " + spA + "/" + spB);

            int leftN = nA;
            float leftSpawn = 0f;
            var leftRng = new StormGlass.Rng { State = 1u };
            int leftSpeck = spA;
            leftN = Step(beads, leftN, 0.016f, false, 1f, blocks, 5, ref leftRng, ref leftSpawn, specks, ref leftSpeck);
            Check("leave-garden-clears", leftN == 0 && leftSpeck == 0 && nA > 0,
                "before " + nA + " after " + leftN + " specks " + leftSpeck);

            bool noPointer = typeof(StormGlassRig).GetInterface("IPointerClickHandler") == null
                && typeof(StormGlassRig).GetInterface("IPointerDownHandler") == null
                && typeof(StormGlassRig).GetInterface("ICanvasRaycastFilter") == null;
            var reqs = typeof(StormGlassRig).GetCustomAttributes(typeof(RequireComponent), true);
            int cols = 0, cols2 = 0, kids = 0;
            var go = new GameObject("storm-glass-ray");
            try
            {
                go.SetActive(false);
                go.AddComponent<StormGlassRig>();
                cols = go.GetComponents<Collider>().Length;
                cols2 = go.GetComponents<Collider2D>().Length;
                kids = go.transform.childCount;
                var ui = System.Type.GetType("UnityEngine.UI.Graphic, UnityEngine.UI");
                if (ui != null)
                {
                    var found = go.GetComponentsInChildren(ui, true);
                    if (found != null) kids += found.Length;
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            Check("draws-no-raycast-target",
                StormGlass.BlocksTaps == false && noPointer && (reqs == null || reqs.Length == 0)
                && cols == 0 && cols2 == 0 && kids == 0,
                "col " + cols + " col2 " + cols2 + " kids " + kids);

            string end = fail == 0 ? "STORM_GLASS_OK " + pass : "STORM_GLASS_FAIL " + fail + " passed " + pass;
            Debug.Log("[storm-glass] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static int Step(
            StormGlass.Bead[] beads, int n, float dt, bool garden, float wet,
            StormGlass.Block[] blocks, int blockN,
            ref StormGlass.Rng rng, ref float spawn,
            StormGlass.Speck[] specks, ref int speckN)
        {
            return StormGlass.Tick(
                beads, n, dt, garden, wet,
                0f, 0f, W, H,
                blocks, blockN, ref rng, ref spawn, specks, ref speckN);
        }

        static StormGlass.Block[] UiBlocks()
        {
            var safe = new Rect(0f, 102f, W, H - 180f);
            float? keep = DeviceFit.TestWindow;
            DeviceFit.TestWindow = W / H;
            CornerAnchor.Garden(W, H, safe, out _, out _, out _, out var restart, out var hive);
            var stamp = FlockFiveApp.GardenStampBox(W, H, safe, DeviceFit.Ui(W, H));
            DeviceFit.TestWindow = keep;
            var blocks = new StormGlass.Block[6];
            blocks[0] = StormGlass.FromGui(restart, H);
            blocks[1] = StormGlass.FromGui(hive, H);
            blocks[2] = StormGlass.FromGui(stamp, H);
            blocks[3] = new StormGlass.Block { X = 180f, Y = 980f, W = 340f, H = 420f };
            blocks[4] = new StormGlass.Block { X = 640f, Y = 1100f, W = 380f, H = 460f };
            return blocks;
        }
    }
}
#endif

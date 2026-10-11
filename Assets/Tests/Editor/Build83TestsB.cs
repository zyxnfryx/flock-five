#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 83 pass b83b. Feeders are a list of 1-3. The ostrich bonks a
    // completed set and pecks with the shared SplitMix stream.
    [InitializeOnLoad]
    public static class Build83TestsB
    {
        const string Cmd = "/tmp/flock-five-b83b-tests";
        const string Out = "/tmp/flock-five-b83b-tests.txt";
        const string ShotDir = "/Users/zfxgames/wkspaces/birdshot/gb/b83out";

        static Build83TestsB()
        {
            EditorApplication.delayCall += Maybe;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (File.Exists(Cmd)) Maybe();
        }

        static void Maybe()
        {
            if (!File.Exists(Cmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Build 83b Engine Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++;
                else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                UnityEngine.Debug.Log("[build83b] " + line);
            }

            CheckRules(Check);
            try { CheckShots(Check); }
            catch (System.Exception ex)
            {
                Check("stills", false, ex.GetType().Name + " " + ex.Message);
            }
            string end = fail == 0 ? "BUILD83B_OK " + pass : "BUILD83B_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            UnityEngine.Debug.Log("[build83b] " + end);
        }

        static void CheckRules(System.Action<string, bool, string> Check)
        {
            ulong mix = new SplitMix64(1, 0).Next();
            Check("splitmix", mix == 0xbeeb8da1658eec67UL, mix.ToString("x"));
            ulong debut = new SplitMix64(686559, 0).Next();
            Check("splitmix-peck-seed", debut == 0x3816b731eb2c2d9bUL, debut.ToString("x"));
            Check("pecks-default", new OstrichState().PecksPerMove == 2, "default");

            var dawn = LevelData.Open(0);
            Check("dawn-live",
                dawn.Live != null && dawn.Live.Length == 2
                && dawn.Live[0] == BirdColor.Ruby && dawn.Live[1] == BirdColor.Gold
                && dawn.Ostrich == null,
                dawn.Live == null ? "no live" : "len " + dawn.Live.Length);
            string dawnKey = GardenSolve.Key(dawn);
            Check("dawn-key", dawnKey.Contains("L01Q") && !dawnKey.Contains("#"), dawnKey);

            int shapeBad = 0;
            for (int i = 0; i < LevelData.Count; i++)
            {
                var b = LevelData.Open(i);
                if (b.Live == null || b.Live.Length < 1 || b.Live.Length > 3 || b.Ostrich != null)
                    shapeBad++;
            }
            Check("gardens-feeders-1-to-3", shapeBad == 0, "bad " + shapeBad + " of " + LevelData.Count);

            var pinched = new Board();
            pinched.Live[0] = BirdColor.Ruby;
            pinched.Live[1] = null;
            pinched.SetFeeders(2);
            Check("set-feeders-keeps", pinched.Live.Length == 2 && pinched.Live[0] == BirdColor.Ruby && !pinched.Live[1].HasValue, "len " + pinched.Live.Length);

            var src = new Board();
            src.SetFeeders(3);
            src.Live[0] = BirdColor.Ruby;
            src.Live[1] = BirdColor.Gold;
            src.Live[2] = BirdColor.Teal;
            var copied = new Board();
            copied.CopyFeedersFrom(src);
            var cloned = src.Clone();
            Check("copy-three",
                copied.Live.Length == 3 && copied.Live[2] == BirdColor.Teal
                && cloned.Live.Length == 3 && cloned.Live[2] == BirdColor.Teal
                && copied.FeederSlotFor(BirdColor.Teal) == 2
                && copied.LiveHas(BirdColor.Gold) && !copied.LiveHas(BirdColor.Violet),
                "slot " + copied.FeederSlotFor(BirdColor.Teal));

            var one = new Level { Feeders = 1 };
            var oneBoard = new Board();
            oneBoard.Live[0] = BirdColor.Ruby;
            oneBoard.Live[1] = BirdColor.Gold;
            LevelData.ApplyAuthored(oneBoard, one);
            Check("authored-one", oneBoard.Live.Length == 1 && oneBoard.Live[0] == BirdColor.Ruby && oneBoard.Ostrich == null, "len " + oneBoard.Live.Length);

            var ostLevel = new Level { Ostrich = true, PeckSeed = 686559 };
            var ostBoard = new Board();
            ostBoard.Live[0] = BirdColor.Ruby;
            ostBoard.Live[1] = BirdColor.Gold;
            LevelData.ApplyAuthored(ostBoard, ostLevel);
            Check("authored-ostrich",
                ostBoard.Live.Length == 3 && ostBoard.Live[0] == BirdColor.Ruby && ostBoard.Live[1] == BirdColor.Gold
                && !ostBoard.Live[2].HasValue && ostBoard.Ostrich != null && ostBoard.Ostrich.Alive
                && ostBoard.Ostrich.PeckSeed == 686559 && ostBoard.Ostrich.PecksPerMove == 2,
                ostBoard.Ostrich == null ? "no ostrich" : "hits " + ostBoard.Ostrich.Hits);

            Check("trap-exempt",
                TrapProbe.Exempt(ostLevel) && !TrapProbe.Exempt(one) && !TrapProbe.Exempt(null),
                "ostrich exempt");

            var hashBoard = src.Clone();
            var k1 = new GardenGen.Knobs { Feeders = 3, Ostrich = true, PeckSeed = 1 };
            var k2 = new GardenGen.Knobs { Feeders = 3, Ostrich = true, PeckSeed = 99 };
            var k3 = new GardenGen.Knobs { Feeders = 2, Ostrich = false, PeckSeed = 1 };
            ulong h1 = GardenGen.Hash(hashBoard, k1);
            ulong h2 = GardenGen.Hash(hashBoard, k2);
            ulong h3 = GardenGen.Hash(hashBoard, k3);
            Check("gen-hash", h1 == h2 && h1 != h3 && GardenGen.ResolvedFeeders(k1) == 3, h1.ToString("x") + " " + h3.ToString("x"));

            Check("row-two", FeederRow.X(0, 2) == -FeederRow.Pair && FeederRow.X(1, 2) == FeederRow.Pair, "pair");
            Check("row-three",
                FeederRow.X(0, 3) == -FeederRow.Triple && FeederRow.X(1, 3) == FeederRow.Triple && FeederRow.X(2, 3) == 0f,
                "triple");
            float phone = FeederRow.SafeHalf(FeederRow.PhoneTall, 0f);
            float tablet = FeederRow.SafeHalf(FeederRow.Tablet, 0f);
            Check("row-fits",
                FeederRow.Fits(1, phone) && FeederRow.Fits(2, phone) && FeederRow.Fits(3, phone)
                && FeederRow.Fits(3, tablet) && FeederRow.Fits(3, FeederRow.SafeHalf(FeederRow.PhoneTall, 0.05f)),
                "phone " + phone.ToString("0.00") + " tablet " + tablet.ToString("0.00"));

            CheckBreeze(Check);
            CheckBonk(Check);
            CheckPecks(Check);
            CheckSolve(Check);
            int fuzz = FuzzScatter(10000);
            Check("scatter-never-matches", fuzz == 0, "bad " + fuzz);
        }

        static void CheckBreeze(System.Action<string, bool, string> Check)
        {
            var left = LeafBoard();
            int nLeft = left.Breeze(0);
            var third = LeafBoard();
            int nThird = third.Breeze(2);
            var right = new Board();
            right.Branches.Add(Leaf(BirdColor.Ruby));
            right.Branches.Add(OpenBird(BirdColor.Gold));
            right.Branches.Add(OpenBird(BirdColor.Teal));
            right.Branches.Add(Leaf(BirdColor.Violet));
            var near = right.Clone();
            Check("breeze-left-column", nLeft == 1 && !left.Branches[2].TipLocked && left.Branches[1].TipLocked, "lifted " + nLeft);
            Check("breeze-third-nearest", nThird == 1 && !third.Branches[1].TipLocked && third.Branches[2].TipLocked, "lifted " + nThird);
            Check("breeze-right-column", right.Breeze(1) == 1 && !right.Branches[3].TipLocked && right.Branches[0].TipLocked, "col 1");
            Check("breeze-third-beats-column", near.Breeze(2) == 1 && !near.Branches[0].TipLocked && near.Branches[3].TipLocked, "nearest");

            var pay = LeafBoard();
            pay.SetFeeders(3);
            pay.Live[0] = BirdColor.Ruby;
            pay.Live[1] = BirdColor.Gold;
            pay.Live[2] = BirdColor.Teal;
            pay.BreezeOnCollect = true;
            pay.Branches.Add(Five(BirdColor.Teal));
            int slot = pay.ApplyCollect(3);
            Check("collect-third-lifts-nearest",
                slot == 2 && pay.Branches[3].Broken && !pay.Branches[1].TipLocked && pay.Branches[2].TipLocked,
                "slot " + slot);
        }

        static void CheckBonk(System.Action<string, bool, string> Check)
        {
            var b = LeafBoard();
            b.SetFeeders(3);
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Gold;
            b.Live[2] = BirdColor.Teal;
            b.Queue.Add(BirdColor.Violet);
            b.BreezeOnCollect = true;
            b.Branches.Add(Five(BirdColor.Teal));
            b.Ostrich = new OstrichState { PeckSeed = 11, PecksPerMove = 2 };
            int slot = b.ApplyCollect(3);
            Check("bonk-keeps-set",
                slot == 2 && !b.Branches[3].Broken && b.Branches[3].Count == 5
                && b.Ostrich.Hits == 1 && b.Ostrich.BlockSlot == 2 && b.Ostrich.Alive
                && b.Live[2] == BirdColor.Teal && b.Queue.Count == 1
                && b.Branches[1].TipLocked && b.FindCollect() < 0,
                "hits " + b.Ostrich.Hits + " slot " + b.Ostrich.BlockSlot);

            var fight = new Board();
            fight.SetFeeders(3);
            fight.Live[0] = BirdColor.Ruby;
            fight.Live[1] = BirdColor.Gold;
            fight.Live[2] = BirdColor.Teal;
            fight.Queue.Add(BirdColor.Violet);
            fight.Branches.Add(Five(BirdColor.Ruby));
            fight.Branches.Add(Five(BirdColor.Gold));
            fight.Branches.Add(Five(BirdColor.Teal));
            fight.Ostrich = new OstrichState { PeckSeed = 3, PecksPerMove = 2 };
            fight.ApplyCollect(0);
            fight.ApplyCollect(1);
            Check("two-hits-stay",
                fight.Ostrich.Hits == 2 && fight.Ostrich.Alive && !fight.Branches[0].Broken
                && fight.Live[0] == BirdColor.Ruby && fight.Queue.Count == 1
                && fight.Ostrich.BlockSlot == 1,
                "hits " + fight.Ostrich.Hits);
            string aliveKey = GardenSolve.Key(fight);
            fight.ApplyCollect(2);
            string fledKey = GardenSolve.Key(fight);
            Check("third-hit-flees",
                !fight.Ostrich.Alive && fight.Ostrich.Hits == 3 && fight.Won
                && fight.Branches[0].Broken && fight.Branches[1].Broken && fight.Branches[2].Broken
                && fight.Live[0] == BirdColor.Violet && GardenSolve.Cleared(fight)
                && fledKey.Contains("#3,7,-1") && aliveKey.Contains("#2,") && !aliveKey.Contains(",-1"),
                fledKey);
        }

        static void CheckPecks(System.Action<string, bool, string> Check)
        {
            var hidden = new Board();
            var leaf = new BranchState();
            leaf.Birds.Add(new Bird(BirdColor.Ruby));
            leaf.Shrouded.Add(true);
            hidden.Branches.Add(leaf);
            hidden.Branches.Add(new BranchState());
            hidden.Ostrich = new OstrichState { PeckSeed = 5 };
            bool hid = OstrichPeck.Scatter(hidden);
            Check("peck-skips-hidden",
                !hid && hidden.Branches[0].Count == 1 && hidden.Branches[0].IsShrouded(0)
                && hidden.Branches[1].Count == 0 && hidden.Ostrich.PeckIndex == 1,
                "moved " + hid);

            var blocked = new Board();
            blocked.Branches.Add(Four(BirdColor.Ruby));
            blocked.Branches.Add(Four(BirdColor.Ruby));
            blocked.Ostrich = new OstrichState { PeckSeed = 9 };
            bool made = OstrichPeck.Scatter(blocked);
            Check("peck-rejects-match",
                !made && blocked.Branches[0].Count == 4 && blocked.Branches[1].Count == 4
                && blocked.Ostrich.PeckIndex == 1,
                "moved " + made);

            var hop = new Board();
            var tip = new BranchState();
            tip.Birds.Add(new Bird(BirdColor.Ruby));
            tip.Shrouded.Add(false);
            hop.Branches.Add(tip);
            hop.Branches.Add(new BranchState());
            hop.Ostrich = new OstrichState { PeckSeed = 4 };
            bool did = OstrichPeck.Scatter(hop);
            bool full = hop.Branches[1].IsFullMatch(out _);
            Check("peck-moves-unhidden",
                did && hop.Branches[0].Count == 0 && hop.Branches[1].Count == 1 && !full,
                "moved " + did);

            var seed = PeckBoard();
            var once = seed.Clone();
            once.TryMove(1, 2, out _);
            GardenSolve.AfterMove(once);
            var twice = seed.Clone();
            twice.Ostrich.Reset();
            twice.TryMove(1, 2, out _);
            GardenSolve.AfterMove(twice);
            var other = seed.Clone();
            other.Ostrich.PeckIndex = 3;
            Check("peck-two-per-move",
                once.Ostrich.Alive && once.Ostrich.PeckIndex == 2 && once.Ostrich.Hits == 0,
                "index " + once.Ostrich.PeckIndex + " hits " + once.Ostrich.Hits);
            Check("restart-same-pecks",
                GardenSolve.Key(once) == GardenSolve.Key(twice)
                && GardenSolve.Key(seed) != GardenSolve.Key(other),
                GardenSolve.Key(once));
        }

        static void CheckSolve(System.Action<string, bool, string> Check)
        {
            var b = new Board();
            b.SetFeeders(3);
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Gold;
            b.Live[2] = BirdColor.Teal;
            b.Branches.Add(Five(BirdColor.Ruby));
            b.Branches.Add(Five(BirdColor.Gold));
            b.Branches.Add(Four(BirdColor.Teal));
            b.Branches.Add(One(BirdColor.Teal));
            b.Ostrich = new OstrichState { PeckSeed = 686559, PecksPerMove = 2 };
            var report = GardenSolve.Search(b, 4000);
            Check("solve-ostrich",
                report.Outlook == GardenSolve.Outlook.Winnable && report.Moves == 1,
                report.Outlook + " moves " + report.Moves + " nodes " + report.Nodes);

            var order = new Board();
            order.Ostrich = new OstrichState { PecksPerMove = 2 };
            order.Branches.Add(One(BirdColor.Ruby));
            order.Branches.Add(Four(BirdColor.Ruby));
            order.Branches.Add(new BranchState());
            var hops = new List<GardenSolve.HopChoice>();
            GardenSolve.CollectHops(order, hops);
            Check("solve-build-first", hops.Count > 0 && hops[0].To != 2, hops.Count == 0 ? "none" : hops[0].From + ">" + hops[0].To);
        }

        static int FuzzScatter(int trials)
        {
            int bad = 0;
            for (int t = 0; t < trials; t++)
            {
                var rng = new SplitMix64(1000L + t, 3);
                var b = new Board();
                b.SetFeeders(2);
                b.Live[0] = BirdColor.Ruby;
                int limbs = 3 + (int)(rng.Next() % 3UL);
                var before = new string[limbs];
                var wasFull = new bool[limbs];
                var wasTip = new bool[limbs];
                for (int i = 0; i < limbs; i++)
                {
                    var br = new BranchState();
                    int n = (int)(rng.Next() % 5UL);
                    if (rng.Next() % 7UL == 0) n = 5;
                    var col = (BirdColor)(rng.Next() % 5UL);
                    var kit = (BirdKit)(rng.Next() % 3UL);
                    bool tipHidden = rng.Next() % 5UL == 0;
                    for (int k = 0; k < n; k++)
                    {
                        br.Birds.Add(new Bird(col, kit));
                        br.Shrouded.Add(k == n - 1 && tipHidden);
                    }
                    if (rng.Next() % 11UL == 0) br.AdLocked = true;
                    b.Branches.Add(br);
                    before[i] = Limb(br);
                    wasFull[i] = br.IsFullMatch(out _);
                    wasTip[i] = br.TipLocked;
                }
                b.Ostrich = new OstrichState { PeckSeed = t + 1, PecksPerMove = 2 };
                int birds = Birds(b);
                OstrichPeck.Scatter(b);
                if (Birds(b) != birds || b.Ostrich.PeckIndex != 1) bad++;
                for (int i = 0; i < limbs; i++)
                {
                    var br = b.Branches[i];
                    string now = Limb(br);
                    if (br.IsFullMatch(out _) && !wasFull[i]) bad++;
                    if (br.TipRun() >= 5 && !wasFull[i]) bad++;
                    // A locked, gift, or finished limb is not a source. A leaf
                    // may still receive a bird on top, so its old seats stay.
                    if ((br.AdLocked || wasFull[i]) && now != before[i]) bad++;
                    if (wasTip[i] && !now.StartsWith(before[i])) bad++;
                }
            }
            return bad;
        }

        static Board LeafBoard()
        {
            var b = new Board();
            b.Branches.Add(OpenBird(BirdColor.Ruby));
            b.Branches.Add(Leaf(BirdColor.Gold));
            b.Branches.Add(Leaf(BirdColor.Teal));
            return b;
        }

        static Board PeckBoard()
        {
            var b = new Board();
            b.SetFeeders(2);
            b.Live[0] = BirdColor.Ruby;
            b.Live[1] = BirdColor.Gold;
            var mixed = new BranchState();
            mixed.Birds.Add(new Bird(BirdColor.Ruby));
            mixed.Birds.Add(new Bird(BirdColor.Gold));
            mixed.Shrouded.Add(false);
            mixed.Shrouded.Add(false);
            var teal = One(BirdColor.Teal);
            var violet = new BranchState();
            violet.Birds.Add(new Bird(BirdColor.Violet));
            violet.Birds.Add(new Bird(BirdColor.Violet));
            violet.Shrouded.Add(false);
            violet.Shrouded.Add(false);
            b.Branches.Add(mixed);
            b.Branches.Add(teal);
            b.Branches.Add(new BranchState());
            b.Branches.Add(violet);
            b.Ostrich = new OstrichState { PeckSeed = 686559, PecksPerMove = 2 };
            return b;
        }

        static BranchState OpenBird(BirdColor c)
        {
            var br = new BranchState();
            br.Birds.Add(new Bird(c));
            br.Shrouded.Add(false);
            return br;
        }

        static BranchState Leaf(BirdColor c)
        {
            var br = new BranchState();
            br.Birds.Add(new Bird(c));
            br.Shrouded.Add(true);
            return br;
        }

        static BranchState One(BirdColor c) => OpenBird(c);

        static BranchState Four(BirdColor c)
        {
            var br = new BranchState();
            for (int i = 0; i < 4; i++)
            {
                br.Birds.Add(new Bird(c));
                br.Shrouded.Add(false);
            }
            return br;
        }

        static BranchState Five(BirdColor c)
        {
            var br = Four(c);
            br.Birds.Add(new Bird(c));
            br.Shrouded.Add(false);
            return br;
        }

        static int Birds(Board b)
        {
            int n = 0;
            for (int i = 0; i < b.Branches.Count; i++) n += b.Branches[i].Count;
            return n;
        }

        static string Limb(BranchState br)
        {
            var sb = new StringBuilder();
            sb.Append(br.AdLocked ? 'A' : '.');
            for (int i = 0; i < br.Count; i++)
            {
                sb.Append((int)br.Birds[i].Color);
                sb.Append((int)br.Birds[i].Kit);
                sb.Append(br.IsShrouded(i) ? 'h' : '.');
            }
            return sb.ToString();
        }

        static void CheckShots(System.Action<string, bool, string> Check)
        {
            Directory.CreateDirectory(ShotDir);
            var camGo = new GameObject("b83b-cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.aspect = 9f / 16f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.62f, 0.38f);
            cam.transform.position = new Vector3(0f, 1.2f, -10f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            cam.enabled = true;
            const int w = 540;
            const int h = 960;
            const float y = 2.1f;
            try
            {
                for (int n = 1; n <= 3; n++)
                {
                    int head = n == 3 ? 2 : -1;
                    var root = BuildRow(n, head, y, 0);
                    var tex = Render(cam, w, h);
                    File.WriteAllBytes(ShotDir + "/b83-feeders-" + n + ".png", tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    Object.DestroyImmediate(root);
                }
                var block = BuildRow(3, 1, y, 1);
                var blockTex = Render(cam, w, h);
                File.WriteAllBytes(ShotDir + "/b83-ostrich-block.png", blockTex.EncodeToPNG());
                Object.DestroyImmediate(blockTex);
                Object.DestroyImmediate(block);

                float left = FeederRow.X(0, 3);
                float mid = FeederRow.X(2, 3);
                float right = FeederRow.X(1, 3);
                var frames = new List<Texture2D>();
                float[] xs = { left, (left + mid) * 0.5f, mid, (mid + right) * 0.5f, right, right, right, right };
                float[] ys = { y, y, y, y, y, y, 4.6f, 8.4f };
                int[] hits = { 0, 0, 1, 1, 2, 2, 3, 3 };
                for (int i = 0; i < xs.Length; i++)
                {
                    var root = BuildRow(3, -1, y, 0);
                    var mark = OstrichMark.Attach(root.transform, 3);
                    bool show = i < xs.Length - 1;
                    mark.Pose(xs[i], ys[i], hits[i], show);
                    var tex = Render(cam, w, h);
                    File.WriteAllBytes(ShotDir + "/b83-ostrich-swing-" + i.ToString("00") + ".png", tex.EncodeToPNG());
                    frames.Add(tex);
                    Object.DestroyImmediate(root);
                }
                WriteStrip(frames, ShotDir + "/b83-ostrich-swing-strip.png");
                for (int i = 0; i < frames.Count; i++) Object.DestroyImmediate(frames[i]);
                string ff = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
                string mp4 = ShotDir + "/b83-ostrich-swing.mp4";
                var psi = new ProcessStartInfo
                {
                    FileName = ff,
                    Arguments = "-y -framerate 8 -i \"" + ShotDir + "/b83-ostrich-swing-%02d.png\" -vf pad=ceil(iw/2)*2:ceil(ih/2)*2 -c:v libx264 -pix_fmt yuv420p \"" + mp4 + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc != null) proc.WaitForExit(20000);
                }
                bool png = File.Exists(ShotDir + "/b83-feeders-3.png") && File.Exists(ShotDir + "/b83-ostrich-block.png");
                bool clip = File.Exists(mp4) && new FileInfo(mp4).Length > 1000;
                Check("stills", png && clip, ShotDir);
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }
        }

        static GameObject BuildRow(int n, int headSlot, float y, int hits)
        {
            var root = new GameObject("row-" + n);
            for (int i = 0; i < n; i++)
            {
                var spr = SpriteCatalog.Feeder((BirdColor)(i % 5));
                WorldBuilder.Sprite("Feeder" + i, spr, new Vector3(FeederRow.X(i, n), y, 0f), FeederView.Scale, 8, root.transform);
            }
            if (headSlot >= 0)
            {
                var mark = OstrichMark.Attach(root.transform, n);
                mark.Pose(FeederRow.X(headSlot, n), y, hits, true);
            }
            return root;
        }

        static Texture2D Render(Camera cam, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            return tex;
        }

        static void WriteStrip(List<Texture2D> frames, string path)
        {
            int n = frames.Count;
            int w = frames[0].width;
            int h = frames[0].height;
            var dst = new Texture2D(w * n, h, TextureFormat.RGB24, false);
            for (int i = 0; i < n; i++)
                dst.SetPixels(i * w, 0, w, h, frames[i].GetPixels());
            dst.Apply();
            File.WriteAllBytes(path, dst.EncodeToPNG());
            Object.DestroyImmediate(dst);
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // The far wing of a kit bird draws behind the bow or crown. On a
    // right-facing bird that wing is its left wing; facing left, its right wing.
    // The near wing stays on the body sheet.
    public static class Build88WingTests
    {
        const string Out = "/tmp/flock-five-b88-wing-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build88wing] " + line);
            }
            try
            {
                Check("far-wing-right-facing-is-left",
                    BirdIdle.FarWingSide(false) == BirdIdle.WingSide.Left, "right-facing");
                Check("far-wing-left-facing-is-right",
                    BirdIdle.FarWingSide(true) == BirdIdle.WingSide.Right, "left-facing");

                int body = FlockSort.Perch;
                BirdIdle.KitDrawOrder(BirdKit.Bow, body, out int bowFar, out int bowTop);
                Check("bow-far-behind-topper", bowFar < bowTop && bowTop == body - 1,
                    "far " + bowFar + " top " + bowTop + " body " + body);
                Check("bow-near-stays-in-front", bowTop < body, "near wing is the body");

                BirdIdle.KitDrawOrder(BirdKit.Crown, body, out int crownFar, out int crownTop);
                Check("crown-far-behind-topper", crownFar < crownTop && crownTop == body + 1,
                    "far " + crownFar + " top " + crownTop + " body " + body);
                Check("crown-near-stays-behind-topper", body < crownTop && crownFar < body,
                    "near wing stays on the body");

                var kinds = new int[3];
                int n = BirdIdle.KitPaintSequence(BirdKit.Bow, true, kinds);
                Check("paint-bow-far-then-topper-then-body",
                    n == 3 && kinds[0] == BirdIdle.KitLayerFar && kinds[1] == BirdIdle.KitLayerTopper
                    && kinds[2] == BirdIdle.KitLayerBody, n + " " + kinds[0] + "," + kinds[1] + "," + kinds[2]);
                n = BirdIdle.KitPaintSequence(BirdKit.Crown, true, kinds);
                Check("paint-crown-far-then-body-then-topper",
                    n == 3 && kinds[0] == BirdIdle.KitLayerFar && kinds[1] == BirdIdle.KitLayerBody
                    && kinds[2] == BirdIdle.KitLayerTopper, n + " " + kinds[0] + "," + kinds[1] + "," + kinds[2]);
                n = BirdIdle.KitPaintSequence(BirdKit.Bow, false, kinds);
                Check("paint-bow-rest-unchanged",
                    n == 2 && kinds[0] == BirdIdle.KitLayerTopper && kinds[1] == BirdIdle.KitLayerBody,
                    n + " " + kinds[0] + "," + kinds[1]);
                n = BirdIdle.KitPaintSequence(BirdKit.Crown, false, kinds);
                Check("paint-crown-rest-unchanged",
                    n == 2 && kinds[0] == BirdIdle.KitLayerBody && kinds[1] == BirdIdle.KitLayerTopper,
                    n + " " + kinds[0] + "," + kinds[1]);

                var ruby2 = SpriteCatalog.BirdPose(BirdColor.Ruby, 2);
                Check("ruby-flap2-loaded", ruby2 != null, ruby2 != null ? ruby2.name : "null");
                if (ruby2 != null)
                    CheckMask(Check, ruby2, "ruby2");

                for (int f = 0; f <= 4; f++)
                {
                    if (f == 2) continue;
                    var spr = f == 0 ? SpriteCatalog.Bird(BirdColor.Ruby) : SpriteCatalog.BirdPose(BirdColor.Ruby, f);
                    bool split = spr != null && SpriteCatalog.TryFarWingMask(spr, out _, out _, out _);
                    Check("no-split-ruby-" + f, spr != null && !split, spr == null ? "missing" : "split " + split);
                }

                int rubyCount = 0;
                if (ruby2 != null && SpriteCatalog.TryFarWingMask(ruby2, out var rubyMask, out _, out _))
                    for (int i = 0; i < rubyMask.Length; i++) if (rubyMask[i] != 0) rubyCount++;
                var colors = new[] { BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Peach };
                for (int i = 0; i < colors.Length; i++)
                {
                    var spr = SpriteCatalog.BirdPose(colors[i], 2);
                    byte[] mask = null;
                    bool ok = false;
                    if (spr != null && SpriteCatalog.TryFarWingMask(spr, out var gotMask, out _, out _))
                    {
                        mask = gotMask;
                        ok = mask != null;
                    }
                    int count = 0;
                    if (ok)
                        for (int p = 0; p < mask.Length; p++) if (mask[p] != 0) count++;
                    bool close = rubyCount > 0 && count > rubyCount * 8 / 10 && count < rubyCount * 12 / 10;
                    Check("flap2-splits-" + colors[i], ok && close, "px " + count + " ruby " + rubyCount);
                }

                string root = Application.dataPath;
                string idle = File.ReadAllText(Path.Combine(root, "_Project/Scripts/View/BirdIdle.cs"));
                string avatar = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Avatar.cs"));
                string finale = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FinaleShow.cs"));
                string home = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
                Check("garden-uses-draw-order",
                    idle.Contains("KitDrawOrder(worn, bodyOrder") && idle.Contains("PlaceFarWing"),
                    "BirdIdle");
                Check("avatar-uses-paint-sequence",
                    avatar.Contains("KitPaintSequence(worn, split"),
                    "DrawDressedBird");
                Check("home-adopt-name-share-avatar",
                    home.Contains("DrawAvatarBird(") && avatar.Contains("DrawAvatarBird(")
                    && avatar.Contains("DrawColorSwatch"),
                    "home, adopt, name picker");
                Check("finale-uses-birdidle", finale.Contains("BirdIdle"), "finale birds");
            }
            catch (System.Exception ex)
            {
                Check("wing", false, ex.GetType().Name + " " + ex.Message);
            }
            string end = fail == 0 ? "BUILD88WING_OK " + pass : "BUILD88WING_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88wing] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void CheckMask(System.Action<string, bool, string> check, Sprite pose, string tag)
        {
            bool got = SpriteCatalog.TryFarWingMask(pose, out var mask, out int w, out int h);
            check(tag + "-has-far-wing", got && mask != null && w > 8 && h > 8, w + "x" + h);
            if (!got || mask == null) return;
            var dome = SpriteCatalog.HeadDome(pose);
            float ppu = pose.pixelsPerUnit > 1f ? pose.pixelsPerUnit : 220f;
            int domeX = Mathf.RoundToInt(dome.x * ppu + pose.pivot.x - 0.5f);
            int domeY = Mathf.RoundToInt(dome.y * ppu + pose.pivot.y - 0.5f);
            int farN = 0, nearKept = 0, nearFar = 0, beakFar = 0;
            int maxY = 0;
            long sumX = 0;
            int tailX = domeX - Mathf.RoundToInt(1.15f * ppu);
            int beakX = domeX + Mathf.RoundToInt(0.35f * ppu);
            var tex = pose.texture;
            var px = tex.GetPixels32();
            int tw = tex.width;
            int ox = Mathf.RoundToInt(pose.rect.x);
            int oy = Mathf.RoundToInt(pose.rect.y);
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    bool wing = mask[row + x] != 0;
                    bool opaque = px[(oy + y) * tw + ox + x].a > 28;
                    if (wing)
                    {
                        farN++;
                        sumX += x;
                        if (y > maxY) maxY = y;
                        if (x > beakX) beakFar++;
                    }
                    else if (opaque && x < tailX && y > domeY)
                    {
                        nearKept++;
                        if (wing) nearFar++;
                    }
                }
            }
            bool side = farN > 8000 && sumX < (long)domeX * farN && maxY > domeY;
            check(tag + "-far-is-head-side-left-wing", side,
                "n " + farN + " meanX " + (farN > 0 ? (sumX / farN).ToString() : "-") + " dome " + domeX + " top " + maxY);
            check(tag + "-near-wing-not-split", nearKept > 500 && nearFar == 0, "kept " + nearKept);
            check(tag + "-beak-not-far-wing", beakFar == 0, "beak " + beakFar);
            bool parted = SpriteCatalog.TrySplitFarWing(pose, out var far, out var body);
            check(tag + "-split-sprites",
                parted && far != null && body != null && far.pixelsPerUnit == pose.pixelsPerUnit
                && Mathf.Abs(far.rect.width - pose.rect.width) < 1.5f,
                parted ? far.name : "no");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b110 hit-box audit. Same house style as Build123BadgerTutorialTests: Run() writes PASS/FAIL lines.
    // Pure geometry (BranchHit) plus two guards that read the source and the art.
    public static class BranchHitTests
    {
        const string Out = "/tmp/flock-five-branchhit-tests.txt";
        static readonly float[] SeatX = { 220f, 458f, 695f, 933f, 1148f };
        static readonly float[] SeatYMain = { 0.3364f, 0.3757f, 0.2669f, 0.0776f, 0.0847f };
        static readonly float[] SeatYGift = { 0.3382f, 0.3791f, 0.2659f, 0.0690f, 0.0847f };

        static BranchHit.Cand Make(int idx, Vector2 c, bool fromRight, bool gift)
        {
            var pads = new List<Vector2> { c };
            for (int s = 0; s < 5; s++)
            {
                float x = (SeatX[s] - 640f) * 0.64f / 140f;
                pads.Add(new Vector2(c.x + (fromRight ? -x : x), c.y + (gift ? SeatYGift : SeatYMain)[s]));
            }
            var cand = new BranchHit.Cand { Index = idx, Gift = gift, Wood = BranchHit.WoodRect(c, fromRight, gift) };
            if (gift)
            {
                float dir = fromRight ? 1f : -1f;
                var sc = new Vector2(c.x - 1.08f * dir, c.y + 0.48f);
                pads.Add(sc);
                cand.HasSign = true;
                cand.Sign = Rect.MinMaxRect(sc.x - 0.76f, sc.y - 0.42f, sc.x + 0.76f, sc.y + 0.42f); // rotated AABB, as sr.bounds
            }
            cand.Pads = pads.ToArray();
            return cand;
        }

        static List<BranchHit.Cand> Column(float pitch, bool fromRight, out Vector2 gift, out Vector2 above)
        {
            float x = fromRight ? 3.21f : -3.21f;
            gift = new Vector2(x, -7.52f);
            above = new Vector2(x, -7.52f + pitch);
            return new List<BranchHit.Cand> { Make(14, gift, fromRight, true), Make(12, above, fromRight, false) };
        }

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
                Debug.Log("[branchhit] " + line);
            }

            foreach (float pitch in new[] { 1.5f, 1.12f })
            foreach (bool right in new[] { false, true })
            {
                string tag = "pitch" + pitch + (right ? "-R" : "-L");
                var c = Column(pitch, right, out var gift, out var above);
                // Every point on the limb above that is clear of the sign belongs to it.
                int stolen = 0, n = 0;
                var w = c[1].Wood;
                for (int i = 0; i <= 20; i++)
                for (int j = 0; j <= 10; j++)
                {
                    var p = new Vector2(Mathf.Lerp(w.xMin, w.xMax, i / 20f), Mathf.Lerp(w.yMin, w.yMax, j / 10f));
                    if (c[0].Sign.Contains(p)) continue;
                    n++;
                    BranchHit.Pick(p, c, -1, out bool g);
                    if (g) stolen++;
                }
                Check("above-limb-not-stolen-" + tag, stolen == 0, stolen + "/" + n);

                BranchHit.Pick(gift + new Vector2(right ? -0.4f : 0.4f, 0f), c, -1, out bool onWood);
                Check("gift-wood-opens-" + tag, onWood, "");
                BranchHit.Pick(c[0].Sign.center, c, -1, out bool onSign);
                Check("gift-sign-opens-" + tag, onSign, "");
                // Outer end of the limb above, just under its painted wood: inside the old canvas+0.36 reach
                // (gift y +1.70) at both pitches, clear of the sign (inner side).
                var old = new Vector2(above.x + (right ? 1.5f : -1.5f), above.y - 0.3f);
                BranchHit.Pick(old, c, -1, out bool oldHit);
                Check("old-canvas-reach-point-is-above-limb-" + tag, !oldHit, "");
                // Glove on the limb above wins in the overlap strip next to the sign.
                var strip = new Vector2(c[0].Sign.center.x, above.y - 0.50f);
                int gw = BranchHit.Pick(strip, c, 12, out bool gg);
                Check("glove-limb-wins-" + tag, gw == 12 && !gg, "got " + gw);
                // Glove on the gift wins a tap a little off the sign (beyond SignTol).
                int gg2 = BranchHit.Pick(c[0].Sign.center + new Vector2(0f, 0.6f + BranchHit.SignTol), c, 14, out bool g3);
                Check("glove-gift-wins-" + tag, gg2 == 14 && g3, "got " + gg2);
            }

            // Mirror: flipping X must give the same winner.
            {
                var l = Column(1.12f, false, out _, out _);
                var r = Column(1.12f, true, out _, out _);
                int diff = 0;
                for (float y = -8.8f; y <= -5.2f; y += 0.07f)
                for (float x = 0f; x <= 6.6f; x += 0.11f)
                {
                    int a = BranchHit.Pick(new Vector2(-x, y), l, -1, out bool ga);
                    int b = BranchHit.Pick(new Vector2(x, y), r, -1, out bool gb);
                    if (a != b || ga != gb) diff++;
                }
                Check("mirror-symmetric", diff == 0, "diff " + diff);
            }

            // Empty air hits nothing (caller falls back to its pad radius).
            {
                var c = Column(1.5f, false, out _, out _);
                Check("air-hits-nothing", BranchHit.Pick(new Vector2(0f, 0f), c, -1, out _) < 0, "");
            }

            // Gift absent from the list (Broken, Burned, opened): the limb above is the only owner.
            {
                var c = Column(1.12f, false, out var gift, out var above);
                c.RemoveAt(0);
                int hit = BranchHit.Pick(above + new Vector2(0f, -0.4f), c, -1, out bool g);
                Check("no-gift-candidate-no-gift", hit == 12 && !g, "got " + hit);
            }

            // Art guard: constants must still match the PNG alpha.
            foreach (var art in new[] { ("branch", false), ("branch_gift", true) })
            {
                string path = "Assets/_Project/Art/Resources/Sprites/" + art.Item1 + ".png";
                if (!File.Exists(path)) { Check("art-" + art.Item1, false, "missing " + path); continue; }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
                var px = tex.GetPixels32();
                int w = tex.width, h = tex.height, x0 = w, x1 = -1, y0 = h, y1 = -1;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 8) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
                Object.DestroyImmediate(tex);
                float sy = art.Item2 ? 0.52f : 0.50f;
                float lx0 = (x0 - w * 0.5f) * 0.64f / 140f, lx1 = (x1 + 1 - w * 0.5f) * 0.64f / 140f;
                float ly0 = (y0 - h * 0.5f) * sy / 140f, ly1 = (y1 + 1 - h * 0.5f) * sy / 140f;
                float ex0 = art.Item2 ? BranchHit.GiftX0 : BranchHit.MainX0, ex1 = art.Item2 ? BranchHit.GiftX1 : BranchHit.MainX1;
                float ey0 = art.Item2 ? BranchHit.GiftY0 : BranchHit.MainY0, ey1 = art.Item2 ? BranchHit.GiftY1 : BranchHit.MainY1;
                bool ok = Mathf.Abs(lx0 - ex0) < 0.05f && Mathf.Abs(lx1 - ex1) < 0.05f && Mathf.Abs(ly0 - ey0) < 0.05f && Mathf.Abs(ly1 - ey1) < 0.05f;
                Check("art-alpha-matches-" + art.Item1, ok, lx0 + "," + ly0 + "," + lx1 + "," + ly1);
            }

            // Source guards: no canvas-bounds gift test, gift test still ahead of HitBranch only through the arbiter.
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("no-spritehit-canvas-test", !app.Contains("SpriteHit(v.Wood") && !app.Contains("static bool BonusSpot"), "");
            Check("hitgiftsign-uses-arbiter", app.Contains("BranchHit.Pick(world, BuildHitCands()"), "");
            Check("stepgate-untouched", File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs").Contains("bool StepTapGate(out StepGate gate)"), "");

            File.AppendAllText(Out, "PASS " + pass + " FAIL " + fail + "\n");
            Debug.Log("[branchhit] PASS " + pass + " FAIL " + fail);
        }
    }
}
#endif

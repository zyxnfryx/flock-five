#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 item 2, tightened: a guided Level 1 step advances on the cued limb's painted wood
    // or on a bird perched on that wood. A press just past the tip, or on the limb above
    // (the old seat radius reached both), does not.
    static class CoachTargetTapTests
    {
        struct GardenMap
        {
            public float Off, X0, Y0, Pw, Ph, Aspect, Col, Row0;
        }

        // PhoneArea pillar + PortraitLock, without a live Screen. Inspector pixels are
        // full-device, origin top-left. The play camera is the centered 9:16 area on an
        // iPad and a vertically letterboxed 9:16 view on a taller phone.
        static GardenMap MapOf(float w, float h)
        {
            const float phone = 9f / 16f;
            bool pillar = h > 1f && w / h > phone + 0.03f;
            float aw, ah, off;
            if (pillar)
            {
                aw = Mathf.Min(w, Mathf.RoundToInt(h * phone));
                off = (w - aw) * 0.5f;
                ah = h;
            }
            else
            {
                aw = w;
                ah = h;
                off = 0f;
            }
            float awin = ah > 1f ? aw / ah : phone;
            float aspect = awin <= phone + 0.03f ? phone : Mathf.Min(awin, 0.75f);
            float tall = Mathf.Max(1f, phone / Mathf.Max(0.05f, awin));
            float x0, y0, pw, ph;
            if (Mathf.Abs(awin - aspect) < 0.03f)
            {
                x0 = 0f;
                y0 = 0f;
                pw = aw;
                ph = ah;
            }
            else if (awin > aspect)
            {
                pw = aw * aspect / awin;
                x0 = (aw - pw) * 0.5f;
                y0 = 0f;
                ph = ah;
            }
            else
            {
                ph = ah * awin / aspect;
                y0 = (ah - ph) * 0.5f;
                x0 = 0f;
                pw = aw;
            }
            float col = WorldBuilder.CamOrtho * aspect - WorldBuilder.WoodHalf + 0.18f;
            return new GardenMap
            {
                Off = off,
                X0 = x0,
                Y0 = y0,
                Pw = pw,
                Ph = ph,
                Aspect = aspect,
                Col = col,
                Row0 = WorldBuilder.RowY0For(tall),
            };
        }

        static Vector2 WorldOf(float x, float yTop, float screenH, GardenMap m)
        {
            float sx = x - m.Off;
            float sy = screenH - yTop;
            float nx = (sx - m.X0) / Mathf.Max(1f, m.Pw);
            float ny = (sy - m.Y0) / Mathf.Max(1f, m.Ph);
            float half = WorldBuilder.CamOrtho * m.Aspect;
            return new Vector2(
                (nx - 0.5f) * 2f * half,
                WorldBuilder.CamRestY + (ny - 0.5f) * 2f * WorldBuilder.CamOrtho);
        }

        static Vector2 LimbAt(GardenMap m, int index)
        {
            int row = index / 2;
            bool right = (index & 1) == 1;
            return new Vector2(right ? m.Col : -m.Col, m.Row0 - row * WorldBuilder.RowGap);
        }

        static bool OnLimb(Vector2 p, Vector2 center, bool fromRight, bool gift, int birds)
        {
            var wood = BranchHit.WoodRect(center, fromRight, gift);
            if (BranchHit.RectDist(wood, p) <= BranchHit.WoodTol) return true;
            for (int s = 0; s < birds; s++)
            {
                var seat = WorldBuilder.SeatLocal(s, gift, fromRight);
                var body = center + seat + new Vector2(0f, BranchView.RestLift);
                if (BranchHit.CoachBird(p, body, fromRight, wood, 1f)) return true;
            }
            return false;
        }

        public static void Run()
        {
            int fail = 0, n = 0;
            void Check(bool ok, string what)
            {
                n++;
                if (!ok) { fail++; Debug.LogError("[coach-target-tap] FAIL " + what); }
            }

            // Step 1 is branch 0 (three reds, top-left). These presses sit 13–20 px past the
            // visible tip and used to advance. The wood centre and the top bird still do.
            var step1 = new[]
            {
                new Vector3(820f, 1180f, 0f),
                new Vector3(402f, 874f, 0f),
                new Vector3(412f, 915f, 0f),
            };
            var past = new[]
            {
                new Vector2(395f, 330f),
                new Vector2(195f, 262f),
                new Vector2(198f, 278f),
            };
            for (int i = 0; i < step1.Length; i++)
            {
                float w = step1[i].x, h = step1[i].y;
                var map = MapOf(w, h);
                var center = LimbAt(map, 0);
                var p = WorldOf(past[i].x, past[i].y, h, map);
                string tag = w.ToString("0") + " step1";
                Check(!OnLimb(p, center, false, false, 3), tag + " past-end advanced");
                var wood = BranchHit.WoodRect(center, false, false);
                var mid = new Vector2((wood.xMin + wood.xMax) * 0.5f, (wood.yMin + wood.yMax) * 0.5f);
                Check(OnLimb(mid, center, false, false, 3), tag + " wood centre missed");
                var tipBird = center + WorldBuilder.SeatLocal(2, false, false) + new Vector2(0f, BranchView.RestLift);
                Check(OnLimb(tipBird, center, false, false, 3), tag + " top bird missed");
                var inside = new Vector2(wood.xMax - 0.08f, center.y);
                Check(OnLimb(inside, center, false, false, 3), tag + " inner tip missed");
            }

            // 412 step 3. The cue is branch 4, the red tip on the third left-column limb
            // (the inspector's "branch 3"). (95,334) lands on the limb above, branch 2.
            // (95,411) is in the gap under the cue. Neither advances. The red bird itself still does.
            {
                var map = MapOf(412f, 915f);
                var aimed = WorldOf(95f, 334f, 915f, map);
                var under = WorldOf(95f, 411f, 915f, map);
                var above = LimbAt(map, 2);
                var target = LimbAt(map, 4);
                Check(OnLimb(aimed, above, false, false, 4), "412 step3 tap missed the limb it aimed at");
                Check(!OnLimb(aimed, target, false, false, 4), "412 step3 branch-above tap advanced the cue");
                Check(!OnLimb(under, target, false, false, 4), "412 step3 gap under the cue advanced");
                var red = target + WorldBuilder.SeatLocal(3, false, false) + new Vector2(0f, BranchView.RestLift);
                Check(OnLimb(red, target, false, false, 4), "412 step3 red bird missed");
                var sky = new Vector2(0f, map.Row0 + 3f);
                Check(!OnLimb(sky, target, false, false, 4), "412 empty sky advanced");
            }

            // Art guard: the perched-bird box still matches bird_ruby.png.
            {
                string path = "Assets/_Project/Art/Resources/Sprites/bird_ruby.png";
                bool found = File.Exists(path);
                Check(found, "bird_ruby.png missing");
                if (found)
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
                    var px = tex.GetPixels32();
                    int tw = tex.width, th = tex.height, x0 = tw, x1 = -1, y0 = th, y1 = -1;
                    for (int y = 0; y < th; y++)
                    for (int x = 0; x < tw; x++)
                        if (px[y * tw + x].a > 8)
                        {
                            if (x < x0) x0 = x;
                            if (x > x1) x1 = x;
                            if (y < y0) y0 = y;
                            if (y > y1) y1 = y;
                        }
                    Object.DestroyImmediate(tex);
                    float sc = BranchView.BirdScale.x;
                    float ppu = 220f;
                    // GetPixels32 is bottom-up, same edge rule as the wood alpha guard.
                    float lx0 = (x0 - tw * 0.5f) * sc / ppu;
                    float lx1 = (x1 + 1 - tw * 0.5f) * sc / ppu;
                    float ly0 = (y0 - th * 0.5f) * sc / ppu;
                    float ly1 = (y1 + 1 - th * 0.5f) * sc / ppu;
                    bool ok = Mathf.Abs(lx0 - BranchHit.BirdBodyX0) < 0.02f
                        && Mathf.Abs(lx1 - BranchHit.BirdBodyX1) < 0.02f
                        && Mathf.Abs(ly0 - BranchHit.BirdBodyY0) < 0.02f
                        && Mathf.Abs(ly1 - BranchHit.BirdBodyY1) < 0.02f;
                    Check(ok, "bird body " + lx0.ToString("0.000") + "," + ly0.ToString("0.000")
                        + " .. " + lx1.ToString("0.000") + "," + ly1.ToString("0.000"));
                }
            }

            Debug.Log("[coach-target-tap] " + (fail == 0 ? "PASS " + n + "/" + n : "FAIL " + fail + "/" + n));
        }
    }
}
#endif

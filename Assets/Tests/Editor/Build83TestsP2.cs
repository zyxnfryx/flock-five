#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 83 polish pass 2: edge cases across the build's changes, plus the kit seating sheet.
    static class Build83TestsP2
    {
        const string ShotDir = "/Users/zfxgames/wkspaces/birdshot/gb/b83out";

        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log("[build83p2] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            // 2X/Collect: no ad when not loaded, in grace, or cooling down.
            Check("2x-needs-ad-loaded", !Ads.RewardRepeatAvailable(true, false, 99, 1000.0, 0.0), "");
            Check("2x-off-when-ads-disabled", !Ads.RewardRepeatAvailable(false, true, 99, 1000.0, 0.0), "");
            Check("2x-grace-blocks", !Ads.RewardRepeatAvailable(true, true, 0, 1000.0, 0.0) || !Ads.GraceBlocks(0), "");
            Check("2x-cooldown-blocks", !Ads.RewardRepeatAvailable(true, true, 99, 100.0, 99.0), "");
            string rc = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.RewardChoice.cs");
            Check("2x-double-tap-single-ad", rc.Contains("_rewardChoice = 1;") && rc.Contains("_rewardChoice != 0) { _rewardChoiceBorn"), "choice latches before the ad");
            Check("2x-ad-lost-watchdog", rc.Contains("RewardAdWatchdog"), "ad never returns (pause/kill) -> Collect");

            // Studio card timing: sparkle and pop finish inside the hold.
            float sparkEnd = FlockFiveApp.StudioSparkIn + FlockFiveApp.StudioSparkSec * 1.70f;
            Check("studio-hold-covers-sparkle", FlockFiveApp.StudioMinSec >= sparkEnd - 0.05f && FlockFiveApp.StudioMinSec >= 1.2f,
                "hold " + FlockFiveApp.StudioMinSec + " spark end " + sparkEnd.ToString("0.00"));
            var a = FlockFiveApp.StudioLogoRect(1179f, 2556f, SpriteCatalog.StudioLogo);
            var b = FlockFiveApp.StudioLogoRect(1536f, 2048f, SpriteCatalog.StudioLogo);
            Check("studio-logo-fits", a.xMin >= 0f && a.xMax <= 1179f && b.xMin >= 0f && b.xMax <= 1536f && b.height <= 2048f * 0.4f,
                "phone " + a + " ipad " + b);
            Check("studio-logo-loaded", SpriteCatalog.StudioLogo != null, "");

            // Bow/crown seat on the dome for every color, kit, flap frame and facing.
            int bad = 0, total = 0; string worst = "";
            foreach (BirdColor c in System.Enum.GetValues(typeof(BirdColor)))
            foreach (var kitKind in new[] { BirdKit.Crown, BirdKit.Bow })
            for (int f = 0; f < 5; f++)
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 1;
                var body = f == 0 ? SpriteCatalog.Bird(c) : SpriteCatalog.BodyPose(f);
                var kit = SpriteCatalog.KitSprite(c, kitKind);
                if (body == null || kit == null) continue;
                BirdIdle.KitTouch(kitKind, f, left, body, kit, out float tx, out float ty);
                var dome = SpriteCatalog.HeadDome(body);
                float dx = tx - (left ? -dome.x : dome.x);
                float head = Mathf.Max(0.05f, SpriteCatalog.HeadSpan(body));
                total++;
                if (Mathf.Abs(dx) > head * 0.18f) { bad++; worst = c + " " + kitKind + " f" + f + (left ? " L" : " R") + " dx " + dx.ToString("0.000"); }
            }
            Check("kits-centered-on-dome", total > 0 && bad == 0, bad + " of " + total + " off " + worst);
            Sheet();

            // Ostrich restart: a fresh open gives a fresh ostrich and 3 feeders.
            var o1 = LevelData.Open(26); if (o1.Ostrich != null) o1.Ostrich.Hits = 2;
            var o2 = LevelData.Open(26);
            Check("ostrich-restart-reset", o2.Ostrich != null && o2.Ostrich.Hits == 0 && o2.Ostrich != o1.Ostrich && o2.Live.Length == 3, "");
            LevelData.Open(0);

            Debug.Log(fail == 0 ? "build83p2 tests DONE " + pass + "/" + (pass + fail) : "build83p2 tests FAIL " + fail + " failed, " + pass + " passed");
        }

        // Zoomed sheet: rows = color x kit, cols = frame x facing.
        static void Sheet()
        {
            const int cell = 150;
            var colors = (BirdColor[])System.Enum.GetValues(typeof(BirdColor));
            int rows = colors.Length * 2, cols = 10;
            var camGo = new GameObject("kitcam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 0.95f; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.82f, 0.86f, 0.78f); cam.transform.position = new Vector3(0f, 0.55f, -10f);
            var sheet = new Texture2D(cols * cell, rows * cell, TextureFormat.RGB24, false);
            var rt = new RenderTexture(cell, cell, 24);
            for (int r = 0; r < rows; r++)
            for (int k = 0; k < cols; k++)
            {
                var c = colors[r / 2]; var kitKind = r % 2 == 0 ? BirdKit.Crown : BirdKit.Bow;
                int f = k / 2; bool left = k % 2 == 1;
                var body = f == 0 ? SpriteCatalog.Bird(c) : SpriteCatalog.BodyPose(f);
                var kit = SpriteCatalog.KitSprite(c, kitKind);
                var bgo = new GameObject("b"); var bsr = bgo.AddComponent<SpriteRenderer>(); bsr.sprite = body; bsr.flipX = left;
                var kgo = new GameObject("k"); kgo.transform.SetParent(bgo.transform, false);
                var ksr = kgo.AddComponent<SpriteRenderer>(); ksr.sprite = kit; ksr.sortingOrder = 1;
                BirdIdle.KitAnchor(kitKind, f, left, body, kit, out float x, out float y, out float s, out float tilt, out bool flip);
                kgo.transform.localPosition = new Vector3(x, y, -0.01f);
                kgo.transform.localScale = new Vector3(s, s, 1f);
                kgo.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                ksr.flipX = flip;
                // Frame the head and kit: top 60% of the body plus the kit.
                var bb = bsr.bounds; bb.Encapsulate(ksr.bounds);
                float top = bb.max.y, bot = bsr.bounds.center.y;
                float half = Mathf.Max((top - bot) * 0.5f, bb.extents.x * 0.5f) * 1.12f;
                cam.orthographicSize = half;
                cam.transform.position = new Vector3(bb.center.x, (top + bot) * 0.5f, -10f);
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                var t = new Texture2D(cell, cell, TextureFormat.RGB24, false);
                t.ReadPixels(new Rect(0, 0, cell, cell), 0, 0); t.Apply();
                sheet.SetPixels(k * cell, (rows - 1 - r) * cell, cell, cell, t.GetPixels());
                Object.DestroyImmediate(t); Object.DestroyImmediate(bgo);
            }
            cam.targetTexture = null; RenderTexture.active = null;
            sheet.Apply();
            File.WriteAllBytes(ShotDir + "/b83-kits-sheet.png", sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet); Object.DestroyImmediate(rt); Object.DestroyImmediate(camGo);
        }
    }
}
#endif

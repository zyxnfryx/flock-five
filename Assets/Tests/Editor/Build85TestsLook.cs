#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 85: kit seat back on the head; foliage-only sway; rigid cloud drift.
    public static class Build85TestsLook
    {
        const string Out = "/tmp/flock-five-b85-look-tests.txt";
        const string Shot = "/Users/zfxgames/wkspaces/birdshot/gb/b85";

        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build85look] " + line);
            }
            Directory.CreateDirectory(Shot);
            try
            {
                // Kit seat: behind the eye on every color, kit, frame and facing; same offset
                // relative to the rest head on every flap.
                int bad = 0, total = 0; string worst = "";
                foreach (BirdColor c in System.Enum.GetValues(typeof(BirdColor)))
                foreach (var kk in new[] { BirdKit.Crown, BirdKit.Bow })
                for (int f = 0; f < 5; f++)
                for (int side = 0; side < 2; side++)
                {
                    bool left = side == 1;
                    var body = f == 0 ? SpriteCatalog.Bird(c) : SpriteCatalog.BodyPose(f);
                    var kit = SpriteCatalog.KitSprite(c, kk);
                    if (body == null || kit == null) continue;
                    var dome = SpriteCatalog.HeadDome(body);
                    BirdIdle.KitTouch(kk, f, left, body, kit, out float tx, out _);
                    float rel = left ? -tx - dome.x : tx - dome.x; // + toward the beak
                    float head = Mathf.Max(0.05f, SpriteCatalog.HeadSpan(SpriteCatalog.BodyPose(0)));
                    total++;
                    if (rel > -head * 0.08f) { bad++; worst = c + " " + kk + " f" + f + (left ? "L" : "R") + " rel " + rel.ToString("0.000"); }
                }
                Check("kit-seat-behind-dome", total > 0 && bad == 0, bad + "/" + total + " " + worst);
                float kb = BirdIdle.KitBack, kd = BirdIdle.KitBackDrop;
                BirdIdle.KitBack = 0f; BirdIdle.KitBackDrop = 0f;
                Build83TestsP2.Sheet(Shot + "/kit-before.png");
                BirdIdle.KitBack = kb; BirdIdle.KitBackDrop = kd;
                Build83TestsP2.Sheet(Shot + "/kit-after.png");
                Check("kit-sheets", File.Exists(Shot + "/kit-after.png"), "before/after sheets");

                // Sway mask: every swaying season has a mask; import is linear, mipless, clamped.
                foreach (var sc in new[] { GardenScene.Summer, GardenScene.Spring, GardenScene.Fall, GardenScene.Desert, GardenScene.Winter })
                {
                    string p = FoliageSway.MaskPath(sc);
                    if (p == null) { Check("mask-" + sc, sc == GardenScene.Winter, "winter: foreground boughs only"); continue; }
                    var tex = Resources.Load<Texture2D>(p);
                    var imp = tex != null ? UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(tex)) as UnityEditor.TextureImporter : null;
                    bool ok = tex != null && imp != null && !imp.sRGBTexture && !imp.mipmapEnabled && imp.wrapMode == TextureWrapMode.Clamp;
                    Check("mask-" + sc, ok, tex == null ? "missing" : tex.width + "x" + tex.height + " srgb=" + (imp != null && imp.sRGBTexture) + " mips=" + (imp != null && imp.mipmapEnabled));
                }

                // Clip: before (whole-painting sway) | after (foliage only), every season + home.
                Clip();
                Check("sway-clip", File.Exists(Shot + "/sway/f0000.png"), "frames written");
            }
            catch (System.Exception ex) { Check("look", false, ex.GetType().Name + " " + ex.Message + " " + ex.StackTrace); }
            finally { FoliageSway.TestTime = 0f; FoliageSway.TestMask = null; }
            string end = fail == 0 ? "BUILD85LOOK_OK " + pass : "BUILD85LOOK_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build85look] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void Clip()
        {
            string dir = Shot + "/sway";
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var scenes = new[] { GardenScene.Summer, GardenScene.Spring, GardenScene.Fall, GardenScene.Desert, GardenScene.Winter };
            const int W = 270, H = 480, fps = 30, secs = 4;
            var camGo = new GameObject("swaycam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            var rt = new RenderTexture(W, H, 24);
            var frame = new Texture2D(W * scenes.Length, H * 2, TextureFormat.RGB24, false);
            var tmp = new Texture2D(W, H, TextureFormat.RGB24, false);
            var gos = new GameObject[scenes.Length];
            var sways = new FoliageSway[scenes.Length];
            for (int k = 0; k < scenes.Length; k++)
            {
                var spr = scenes[k] == GardenScene.Summer ? SpriteCatalog.GardenBg : SpriteCatalog.GardenBgFor(scenes[k]);
                gos[k] = new GameObject("bg" + k);
                gos[k].transform.position = new Vector3(k * 100f, 0f, 0f);
                var sr = gos[k].AddComponent<SpriteRenderer>(); sr.sprite = spr;
                sways[k] = gos[k].AddComponent<FoliageSway>();
                sways[k].Boot(scenes[k]);
            }
            int n = fps * secs;
            for (int i = 0; i < n; i++)
            {
                FoliageSway.TestTime = 6.3f + i / (float)fps; // into the gust
                for (int row = 0; row < 2; row++)
                {
                    FoliageSway.TestMask = row == 0 ? Texture2D.whiteTexture : null;
                    for (int k = 0; k < scenes.Length; k++)
                    {
                        sways[k].PushForTest();
                        var b = gos[k].GetComponent<SpriteRenderer>().bounds;
                        cam.orthographicSize = b.extents.y;
                        cam.transform.position = new Vector3(b.center.x, b.center.y, -10f);
                        cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                        tmp.ReadPixels(new Rect(0, 0, W, H), 0, 0); tmp.Apply();
                        frame.SetPixels(k * W, (1 - row) * H, W, H, tmp.GetPixels());
                    }
                }
                frame.Apply();
                File.WriteAllBytes(dir + "/f" + i.ToString("0000") + ".png", frame.EncodeToPNG());
            }
            cam.targetTexture = null; RenderTexture.active = null;
            foreach (var g in gos) Object.DestroyImmediate(g);
            Object.DestroyImmediate(camGo); Object.DestroyImmediate(rt); Object.DestroyImmediate(frame); Object.DestroyImmediate(tmp);
        }
    }
}
#endif

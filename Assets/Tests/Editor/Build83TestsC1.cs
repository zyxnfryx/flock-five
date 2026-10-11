#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 83 pass b83c1. Painterly ostrich frames, the calm background rule,
    // and the first-encounter lesson on the shared forced-lesson gate.
    [InitializeOnLoad]
    public static class Build83TestsC1
    {
        const string Cmd = "/tmp/flock-five-b83c1-tests";
        const string Out = "/tmp/flock-five-b83c1-tests.txt";
        const string ShotDir = "/Users/zfxgames/wkspaces/birdshot/gb/b83out";

        static Build83TestsC1()
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

        [MenuItem("Flock Five/Build 83c1 Ostrich Presentation")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++;
                else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                UnityEngine.Debug.Log("[build83c1] " + line);
            }

            CheckFrames(Check);
            CheckLesson(Check);
            CheckHonk(Check);
            CheckStage(Check);
            try { CheckShots(Check); }
            catch (System.Exception ex)
            {
                Check("shots", false, ex.GetType().Name + " " + ex.Message);
            }
            string end = fail == 0 ? "BUILD83C1_OK " + pass : "BUILD83C1_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            UnityEngine.Debug.Log("[build83c1] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void CheckFrames(System.Action<string, bool, string> Check)
        {
            int total = OstrichAnim.TotalFrames();
            Check("frame-count", total >= 80, "frames " + total);
            Check("idle-blink", OstrichAnim.BlinkFrame(7) && OstrichAnim.BlinkFrame(8) && !OstrichAnim.BlinkFrame(2), "7-8");
            Check("idle-ruffle", OstrichAnim.RuffleFrame(12) && !OstrichAnim.RuffleFrame(3), "10-15");
            var wind = OstrichAnim.At(OstrichAct.Peck, 3, false);
            var strike = OstrichAnim.At(OstrichAct.Peck, 6, false);
            Check("peck-phases", wind.Wind > strike.Wind && strike.Strike > wind.Strike,
                "wind " + wind.Wind.ToString("0.00") + " strike " + strike.Strike.ToString("0.00"));
            var calm = OstrichAnim.At(OstrichAct.Idle, 4, true);
            var live = OstrichAnim.At(OstrichAct.Idle, 4, false);
            float cb = calm.Bob < 0f ? -calm.Bob : calm.Bob;
            float lb = live.Bob < 0f ? -live.Bob : live.Bob;
            Check("calm-bob", cb < lb * 0.5f, "calm " + cb.ToString("0.000") + " live " + lb.ToString("0.000"));
            Check("hit-damage",
                OstrichAnim.At(OstrichAct.Hit1, 2, false).Damage == 1
                && OstrichAnim.At(OstrichAct.Hit2, 2, false).Damage == 2
                && OstrichAnim.At(OstrichAct.Daze, 2, false).Damage == 3,
                "1 2 3");

            int openDark = OstrichPaint.DarkCount(OstrichPaint.Head(OstrichAct.Idle, 0));
            int shutDark = OstrichPaint.DarkCount(OstrichPaint.Head(OstrichAct.Idle, 7));
            Check("blink-pixels", shutDark < openDark, "open " + openDark + " shut " + shutDark);
            int healthy = OstrichPaint.OpaqueCount(OstrichPaint.Body(OstrichAct.Idle, 0));
            int torn = OstrichPaint.OpaqueCount(OstrichPaint.Body(OstrichAct.Hit2, 3));
            Check("missing-feathers", torn < healthy, "healthy " + healthy + " torn " + torn);
            int a = OstrichPaint.OpaqueCount(OstrichPaint.Body(OstrichAct.Idle, 0));
            int b = OstrichPaint.OpaqueCount(OstrichPaint.Body(OstrichAct.Idle, 12));
            bool differ = a != b;
            if (!differ)
            {
                var pa = OstrichPaint.Body(OstrichAct.Idle, 0).texture.GetPixels32();
                var pb = OstrichPaint.Body(OstrichAct.Idle, 12).texture.GetPixels32();
                for (int i = 0; i < pa.Length; i += 17)
                {
                    if (pa[i].r != pb[i].r || pa[i].a != pb[i].a) { differ = true; break; }
                }
            }
            Check("idle-frames-differ", differ, "a " + a + " b " + b);
        }

        static void CheckLesson(System.Action<string, bool, string> Check)
        {
            string shown = CaptionLayout.HeadlineGap(CaptionLayout.OstrichLine);
            bool gap = shown.Contains("!\n\n") && shown.Contains("three");
            Check("caption-gap", gap, shown.Replace("\n", "/"));
            bool forced = CoachTap.Forced(CoachStep.Ostrich)
                && !CoachTap.AnyTap(CoachStep.Ostrich)
                && !CoachTap.Advances(CoachStep.Ostrich, false)
                && CoachTap.Advances(CoachStep.Ostrich, true);
            Check("forced-target", forced, CoachTap.Kind(CoachStep.Ostrich).ToString());
            var veil = ModalBlock.RaiseLesson(200f, 400f, new Rect(80f, 100f, 30f, 30f));
            bool blocks = ModalBlock.BlocksPoint(veil, new Vector2(12f, 12f));
            bool allows = !ModalBlock.BlocksPoint(veil, new Vector2(90f, 112f));
            var shut = ModalBlock.RaiseLesson(200f, 400f, default);
            bool closed = ModalBlock.BlocksPoint(shut, new Vector2(90f, 112f));
            Check("lesson-veil", blocks && allows && closed, "cut " + allows + " shut " + closed);
            string modal = File.ReadAllText(Application.dataPath + "/_Project/Scripts/App/FlockFiveApp.Modal.cs");
            Check("veil-wired",
                modal.Contains("PestCueOstrich") && modal.Contains("CoachStep.Ostrich") && modal.Contains("RaiseLesson"),
                "modal");
        }

        static void CheckHonk(System.Action<string, bool, string> Check)
        {
            var clip = SfxLibrary.HonkClip();
            bool sized = clip != null && clip.length > 0.2f && clip.length < 0.55f;
            float step = 1f;
            if (clip != null)
            {
                var data = new float[clip.samples];
                clip.GetData(data, 0);
                double sum = 0;
                for (int i = 1; i < data.Length; i++)
                {
                    float d = data[i] - data[i - 1];
                    if (d < 0f) d = -d;
                    sum += d;
                }
                step = (float)(sum / (data.Length - 1));
            }
            Check("honk-mid",
                sized && step < 0.08f && SfxLibrary.HonkVolume <= 0.28f,
                "len " + (clip != null ? clip.length.ToString("0.00") : "0") + " step " + step.ToString("0.000"));
        }

        static void CheckStage(System.Action<string, bool, string> Check)
        {
            var root = new GameObject("b83c1-stage");
            try
            {
                var mark = OstrichMark.Attach(root.transform, 3);
                var body = mark.transform.Find("Body").GetComponent<SpriteRenderer>();
                var neck = mark.transform.Find("Neck0").GetComponent<SpriteRenderer>();
                bool order = body.sortingOrder == OstrichMark.BodyOrder
                    && body.sortingOrder < 8
                    && neck.sortingOrder == OstrichMark.NeckOrder
                    && neck.sortingOrder > 8
                    && neck.sortingOrder < FlockSort.Perch
                    && mark.Head.sortingOrder == OstrichMark.HeadOrder
                    && mark.Head.sortingOrder > neck.sortingOrder
                    && mark.Head.sortingOrder < FlockSort.Perch;
                Check("behind-flock", order,
                    "body " + body.sortingOrder + " neck " + neck.sortingOrder + " head " + mark.Head.sortingOrder);

                var board = new Board();
                board.Ostrich = new OstrichState { PeckSeed = 3, PecksPerMove = 2 };
                mark.Sync(board, true);
                mark.Tick(0.2f);
                board.Ostrich.PeckIndex = 1;
                mark.Sync(board, true);
                mark.Tick(0.3f);
                bool held = mark.Act == OstrichAct.Idle && mark.Calm;
                mark.SetCalm(false);
                mark.Tick(0.05f);
                Check("calm-holds-peck", held && mark.Act == OstrichAct.Peck, "held " + held + " act " + mark.Act);

                var fight = OstrichMark.Attach(root.transform, 3);
                var hit = new Board();
                hit.Ostrich = new OstrichState();
                fight.Sync(hit, false);
                hit.Ostrich.Hits = 1;
                fight.Sync(hit, false);
                bool hit1 = fight.Act == OstrichAct.Hit1;
                hit.Ostrich.Hits = 2;
                fight.Sync(hit, false);
                float t = 0f;
                while (fight.Act == OstrichAct.Hit1 && t < 3f)
                {
                    fight.Tick(0.1f);
                    t += 0.1f;
                }
                Check("escalating-hits", hit1 && fight.Act == OstrichAct.Hit2, "first " + hit1 + " then " + fight.Act);

                hit.Ostrich.Hits = 3;
                fight.Sync(hit, false);
                t = 0f;
                while (fight.Act == OstrichAct.Daze && t < 4f)
                {
                    fight.Tick(0.1f);
                    t += 0.1f;
                }
                bool fleeing = fight.Act == OstrichAct.Flee;
                while (fight.OnStage && fight.Act == OstrichAct.Flee && t < 8f)
                {
                    fight.Tick(0.1f);
                    t += 0.1f;
                }
                Check("flee", fleeing && !fight.OnStage, "fled " + fleeing + " on " + fight.OnStage);

                var back = OstrichMark.Attach(root.transform, 3);
                var again = new Board();
                again.Ostrich = new OstrichState { PeckSeed = 9 };
                back.Sync(again, false);
                again.Ostrich.Hits = 2;
                again.Ostrich.PeckIndex = 4;
                back.Sync(again, false);
                again.Ostrich.Reset();
                back.Sync(again, false);
                Check("return-taunt", back.Act == OstrichAct.Strut, back.Act.ToString());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void CheckShots(System.Action<string, bool, string> Check)
        {
            Directory.CreateDirectory(ShotDir);
            var camGo = new GameObject("b83c1-cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.55f;
            cam.aspect = 9f / 16f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.52f, 0.30f);
            cam.transform.position = new Vector3(0f, 1.35f, -10f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            const int w = 450;
            const int h = 800;
            const float feederY = 3.55f;
            GameObject stage = null;
            try
            {
                stage = BuildGarden(feederY);
                var mark = OstrichMark.Attach(stage.transform, 3);
                string[] names =
                {
                    "idle", "peck-wind", "peck-strike", "hit1", "hit2", "daze", "flee", "strut", "honk", "shake"
                };
                OstrichAct[] acts =
                {
                    OstrichAct.Idle, OstrichAct.Peck, OstrichAct.Peck, OstrichAct.Hit1, OstrichAct.Hit2,
                    OstrichAct.Daze, OstrichAct.Flee, OstrichAct.Strut, OstrichAct.Honk, OstrichAct.Shake
                };
                int[] frames = { 4, 3, 6, 3, 3, 4, 8, 4, 3, 3 };
                float[] xs = { 0f, 0f, 0f, FeederRow.X(0, 3), FeederRow.X(1, 3), 0f, 1.4f, 0f, 0f, FeederRow.X(0, 3) };
                int birds = 0;
                for (int i = 0; i < names.Length; i++)
                {
                    mark.Preview(acts[i], frames[i], xs[i], feederY, false);
                    var tex = Render(cam, w, h);
                    string path = ShotDir + "/b83-ostrich-" + names[i] + ".png";
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    if (HasCream(tex)) birds++;
                    Object.DestroyImmediate(tex);
                }
                WriteIdleStrip(cam, mark, feederY, ShotDir + "/b83-ostrich-idle-strip.png");
                WriteLesson(cam, mark, stage.transform, feederY, w, h);
                bool clip = WriteFight(cam, mark, feederY, w, h);
                Check("stills", birds == names.Length && clip, "poses " + birds + " clip " + clip);
            }
            finally
            {
                if (stage != null) Object.DestroyImmediate(stage);
                Object.DestroyImmediate(camGo);
            }
        }

        static GameObject BuildGarden(float feederY)
        {
            var root = new GameObject("garden");
            for (int i = 0; i < 3; i++)
            {
                var spr = SpriteCatalog.Feeder((BirdColor)i);
                WorldBuilder.Sprite("Feeder" + i, spr, new Vector3(FeederRow.X(i, 3), feederY, 0f), FeederView.Scale, 8, root.transform);
            }
            var bird = SpriteCatalog.Bird(BirdColor.Ruby);
            WorldBuilder.Sprite("Flock", bird, new Vector3(-1.1f, -0.35f, 0f), 0.42f, FlockSort.Perch, root.transform);
            WorldBuilder.Sprite("FlockB", bird, new Vector3(1.15f, -0.55f, 0f), 0.42f, FlockSort.Perch, root.transform);
            var wood = SpriteCatalog.Branch;
            WorldBuilder.Sprite("Wood", wood, new Vector3(0f, -0.85f, 0f), 0.55f, 2, root.transform);
            return root;
        }

        static void WriteIdleStrip(Camera cam, OstrichMark mark, float y, string path)
        {
            var cells = new List<Texture2D>();
            for (int i = 0; i < 8; i++)
            {
                int frame = i * 2;
                mark.Preview(OstrichAct.Idle, frame, 0f, y, false);
                cells.Add(Render(cam, 180, 320));
            }
            int w = 180 * cells.Count;
            var strip = new Texture2D(w, 320, TextureFormat.RGB24, false);
            for (int i = 0; i < cells.Count; i++)
            {
                strip.SetPixels(i * 180, 0, 180, 320, cells[i].GetPixels());
                Object.DestroyImmediate(cells[i]);
            }
            strip.Apply();
            File.WriteAllBytes(path, strip.EncodeToPNG());
            Object.DestroyImmediate(strip);
        }

        static void WriteLesson(Camera cam, OstrichMark mark, Transform parent, float y, int w, int h)
        {
            mark.Preview(OstrichAct.Idle, 2, 0f, y, true);
            var plateSprite = RoundPlate(560, 200, 52);
            var plate = WorldBuilder.Sprite("Plate", plateSprite, new Vector3(0f, -2.85f, 0f), 1f, 60, parent);
            string shown = CaptionLayout.HeadlineGap(CaptionLayout.OstrichLine);
            StampCaption(cam, parent, shown, new Vector3(0f, -2.85f, -0.4f));

            var finger = SpriteCatalog.HandIndex; // the game glove sprite, uncropped (no hard edge)
            if (finger != null)
            {
                // The sheet's fingertip sits above-left of the pivot. Feather the
                // cropped edges so the lesson does not show a hard rectangle.
                const float glove = 1.15f;
                var aim = mark.Aim.position;
                var pivot = aim + new Vector3(0.42f + 0.60f * glove, -0.05f - 1.92f * glove, 0f);
                WorldBuilder.Sprite("Glove", finger, pivot, glove, 80, parent);
            }
            var tex = Render(cam, w, h);
            File.WriteAllBytes(ShotDir + "/b83-ostrich-lesson.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(plate);
        }

        // Same built-in font the banner stills stamp. TextMesh did not show on this plate.
        static void StampCaption(Camera cam, Transform parent, string shown, Vector3 center)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null || string.IsNullOrEmpty(shown) || cam == null) return;
            const int size = 22;
            font.RequestCharactersInTexture(shown, size, FontStyle.Bold);
            var atlas = font.material != null ? font.material.mainTexture : null;
            if (atlas == null) return;
            float ppu = (cam.orthographicSize * 2f) / 800f;
            var lines = shown.Replace("\r", "").Split('\n');
            float step = size * ppu * 1.05f;
            float y = center.y + (lines.Length - 1) * step * 0.5f;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 0)
                    StampLine(parent, font, atlas, lines[i], size, ppu, center.x, y, center.z);
                y -= step;
            }
        }

        static void StampLine(Transform parent, Font font, Texture atlas, string text, int size, float ppu, float cx, float y, float z)
        {
            float width = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                CharacterInfo info;
                if (!font.GetCharacterInfo(text[i], out info, size, FontStyle.Bold)) continue;
                width += info.advance;
            }
            float pen = cx - width * ppu * 0.5f;
            float baseline = y - size * ppu * 0.35f;
            var verts = new List<Vector3>(text.Length * 4);
            var uvs = new List<Vector2>(text.Length * 4);
            var tris = new List<int>(text.Length * 6);
            var colors = new List<Color>(text.Length * 4);
            for (int i = 0; i < text.Length; i++)
            {
                CharacterInfo info;
                if (!font.GetCharacterInfo(text[i], out info, size, FontStyle.Bold)) continue;
                float x0 = pen + info.minX * ppu;
                float x1 = pen + info.maxX * ppu;
                float y0 = baseline + info.minY * ppu;
                float y1 = baseline + info.maxY * ppu;
                pen += info.advance * ppu;
                if (info.maxX <= info.minX || info.maxY <= info.minY) continue;
                int v = verts.Count;
                verts.Add(new Vector3(x0, y0, z));
                verts.Add(new Vector3(x1, y0, z));
                verts.Add(new Vector3(x0, y1, z));
                verts.Add(new Vector3(x1, y1, z));
                uvs.Add(info.uvBottomLeft);
                uvs.Add(info.uvBottomRight);
                uvs.Add(info.uvTopLeft);
                uvs.Add(info.uvTopRight);
                for (int k = 0; k < 4; k++) colors.Add(Color.white);
                tris.Add(v);
                tris.Add(v + 2);
                tris.Add(v + 1);
                tris.Add(v + 2);
                tris.Add(v + 3);
                tris.Add(v + 1);
            }
            if (verts.Count == 0) return;
            var mesh = new Mesh { name = "LessonLine" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            var go = new GameObject("Lesson");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(font.material);
            mat.mainTexture = atlas;
            mat.color = Color.white;
            mat.renderQueue = 4000;
            mr.sharedMaterial = mat;
            mr.sortingOrder = 120;
        }

        static bool WriteFight(Camera cam, OstrichMark mark, float y, int w, int h)
        {
            string dir = "/tmp/b83c1-fight";
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            const int frames = 64;
            const float span = 8f;
            for (int i = 0; i < frames; i++)
            {
                float t = i / (float)(frames - 1) * span;
                FightPose(mark, t, y);
                var tex = Render(cam, w, h);
                File.WriteAllBytes(dir + "/frame-" + i.ToString("000") + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            string ff = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
            string mp4 = ShotDir + "/b83-ostrich-fight.mp4";
            var psi = new ProcessStartInfo
            {
                FileName = ff,
                Arguments = "-y -framerate 8 -i \"" + dir + "/frame-%03d.png\" -vf pad=ceil(iw/2)*2:ceil(ih/2)*2 -c:v libx264 -pix_fmt yuv420p \"" + mp4 + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var proc = Process.Start(psi))
            {
                if (proc != null) proc.WaitForExit(30000);
            }
            bool ok = File.Exists(mp4) && new FileInfo(mp4).Length > 2000;
            if (ok && Directory.Exists(dir)) Directory.Delete(dir, true);
            return ok;
        }

        // The index art is a hard vertical crop. Soften the cut edges for the still.
        static Sprite SoftIndex()
        {
            string path = Application.dataPath + "/_Project/Art/Resources/Sprites/fx_hand_index.png";
            if (!File.Exists(path)) return SpriteCatalog.HandIndex;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path))) return SpriteCatalog.HandIndex;
            var px = tex.GetPixels32();
            int w = tex.width;
            int h = tex.height;
            int minX = w;
            int maxX = 0;
            int minY = h;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (px[y * w + x].a < 40) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                }
            }
            if (maxX <= minX) return SpriteCatalog.HandIndex;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (px[i].a < 8) continue;
                    float side = Mathf.Min(x - minX, maxX - x) / 16f;
                    float palm = (y - minY) / 28f;
                    float fade = Mathf.Clamp01(side) * Mathf.Clamp01(palm);
                    fade = fade * fade * (3f - 2f * fade);
                    px[i].a = (byte)(px[i].a * fade);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            tex.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 200f);
            sprite.name = "soft-index";
            return sprite;
        }

        // Same near-black rounded plate DrawCoachPanel bakes for a lesson.
        static Sprite RoundPlate(int w, int h, int rad)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            float r = rad;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Abs(x + 0.5f - w * 0.5f) - (w * 0.5f - r);
                    float cy = Mathf.Abs(y + 0.5f - h * 0.5f) - (h * 0.5f - r);
                    float ox = Mathf.Max(cx, 0f);
                    float oy = Mathf.Max(cy, 0f);
                    float dist = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(cx, cy), 0f) - r;
                    float a = dist < -1.2f ? 1f : (dist > 1.2f ? 0f : (1.2f - dist) / 2.4f);
                    byte alpha = (byte)(a * 247f);
                    px[y * w + x] = new Color32(4, 3, 2, alpha);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        static void FightPose(OstrichMark mark, float t, float y)
        {
            OstrichAct act;
            int frame;
            float x = 0f;
            if (t < 1.1f) { act = OstrichAct.Idle; frame = Mathf.FloorToInt(t / 1.1f * 16f); }
            else if (t < 2.1f) { act = OstrichAct.Shake; frame = Mathf.FloorToInt((t - 1.1f) / 1f * 10f); x = FeederRow.X(0, 3); }
            else if (t < 3.2f) { act = OstrichAct.Peck; frame = Mathf.FloorToInt((t - 2.1f) / 1.1f * 12f); x = FeederRow.X(0, 3); }
            else if (t < 4.2f) { act = OstrichAct.Hit1; frame = Mathf.FloorToInt((t - 3.2f) / 1f * 8f); x = FeederRow.X(0, 3); }
            else if (t < 5.2f) { act = OstrichAct.Peck; frame = Mathf.FloorToInt((t - 4.2f) / 1f * 12f); x = FeederRow.X(1, 3); }
            else if (t < 6.1f) { act = OstrichAct.Hit2; frame = Mathf.FloorToInt((t - 5.2f) / 0.9f * 8f); x = FeederRow.X(1, 3); }
            else if (t < 7.0f) { act = OstrichAct.Daze; frame = Mathf.FloorToInt((t - 6.1f) / 0.9f * 10f); }
            else { act = OstrichAct.Flee; frame = Mathf.FloorToInt((t - 7.0f) / 1f * 14f); x = 0.4f + (t - 7f) * 2.6f; }
            int n = OstrichAnim.Count(act);
            if (frame < 0) frame = 0;
            if (frame >= n) frame = n - 1;
            mark.Preview(act, frame, x, y, false);
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

        static bool HasCream(Texture2D tex)
        {
            var px = tex.GetPixels32();
            int n = 0;
            for (int i = 0; i < px.Length; i += 24)
            {
                var c = px[i];
                if (c.r > 170 && c.g > 140 && c.b > 90 && c.r > c.g - 10) n++;
            }
            return n > 6;
        }
    }
}
#endif

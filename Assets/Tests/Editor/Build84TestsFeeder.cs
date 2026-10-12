#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 84: FeederArrivalFx (per-bird squash-bounce + set-complete pop) and its clip.
    public static class Build84TestsFeeder
    {
        const string Out = "/tmp/flock-five-b84-feeder-tests.txt";
        const string ShotDir = "/Users/zfxgames/wkspaces/birdshot/gb/b83out";

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
                UnityEngine.Debug.Log("[build84f] " + line);
            }

            float sq = 0f, st = 9f;
            for (float u = 0f; u <= 1f; u += 0.005f) { var v = FeederView.SquashScale(u); sq = Mathf.Max(sq, v.x); st = Mathf.Min(st, v.y); }
            Check("bird-squash-1.12", Mathf.Abs(sq - 1.12f) < 0.01f && st < 0.9f && FeederView.SquashScale(1f) == Vector2.one, "peak x" + sq.ToString("0.000") + " y" + st.ToString("0.000"));
            float pk = 0f;
            for (float u = 0f; u <= 1f; u += 0.005f) pk = Mathf.Max(pk, FeederView.SetPopScale(u));
            Check("set-pop-1.22", Mathf.Abs(pk - 1.22f) < 0.01f && FeederView.SetPopScale(1f) == 1f, "peak " + pk.ToString("0.000"));
            Check("bird-0.14s-set-0.30s", Mathf.Approximately(FeederArrivalFx.BirdSec, 0.14f) && FeederArrivalFx.SetSec >= 0.28f && FeederArrivalFx.SetSec <= 0.32f, "durations");
            Check("set-pop-before-retire", FeederArrivalFx.SetSec <= FeederArrivalFx.SetLead + 0.16f, "pop ends before PullAway (lead + RetireFeeder 0.16s)");
            var t = typeof(FeederView);
            Check("dead-pulse-cheer-removed", t.GetMethod("Pulse") == null && t.GetMethod("Cheer") == null, "FeederView.Pulse/Cheer gone");
            var quiet = typeof(FlockFiveApp).GetProperty("FeederFxQuiet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Check("finale-hive-gate", quiet != null, "FeederFxQuiet gate (finale / hive inspect)");
            try { Clip(Check); } catch (System.Exception ex) { Check("clip", false, ex.GetType().Name + " " + ex.Message); }

            string end = fail == 0 ? "BUILD84F_OK " + pass : "BUILD84F_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            UnityEngine.Debug.Log("[build84f] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        // 3-bird collect into the middle feeder, 60fps: per-bird squash at each land, then the
        // set pop, then a second 3-bird collect at the 1.44x auto-resolve pace.
        static void Clip(System.Action<string, bool, string> Check)
        {
            var camGo = new GameObject("b84-cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 4.2f; cam.aspect = 9f / 16f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.45f, 0.62f, 0.38f);
            cam.transform.position = new Vector3(0f, 0.6f, -10f);
            string dir = "/tmp/b84-feeder";
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            var root = new GameObject("b84-row");
            FeederView mid = null;
            for (int i = 0; i < 3; i++)
            {
                var go = WorldBuilder.Sprite("Feeder" + i, SpriteCatalog.Feeder((BirdColor)(i + 1)), new Vector3(FeederRow.X(i, 3), 1.6f, 0f), FeederView.Scale, 8, root.transform);
                if (i == 1) { mid = go.AddComponent<FeederView>(); mid.Art = go.GetComponent<SpriteRenderer>(); mid.PlantHere(); }
            }
            cam.transform.position = new Vector3(mid.transform.position.x, 0.4f, -10f);
            cam.orthographicSize = 3.0f;
            var birds = new SpriteRenderer[3];
            for (int k = 0; k < 3; k++)
                birds[k] = WorldBuilder.Sprite("Bird" + k, SpriteCatalog.Bird(BirdColor.Teal), Vector3.zero, 0.9f, 12, root.transform).GetComponent<SpriteRenderer>();
            float peak = 0f;
            int f = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                float speed = pass == 0 ? 1f : 1.44f;
                mid.BeginScore();
                var scored = new bool[3];
                bool setDone = false;
                float t = 0f, setAt = -1f;
                const float stagger = 0.05f, flyDur = 0.45f;
                while (t < 1.25f)
                {
                    float dt = speed / 60f;
                    t += dt;
                    for (int k = 0; k < 3; k++)
                    {
                        float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f - k * stagger) / flyDur));
                        var start = new Vector3(mid.transform.position.x - 1.3f + k * 1.3f, -2.0f, 0f);
                        var dest = mid.RestMouth + new Vector3((k - 1) * 0.3f, 0f, 0f);
                        var ctrl = (start + dest) * 0.5f + new Vector3(0f, 1.3f, 0f);
                        birds[k].transform.position = (1 - u) * (1 - u) * start + 2 * (1 - u) * u * ctrl + u * u * dest;
                        birds[k].sprite = SpriteCatalog.BirdFrame(BirdColor.Teal, t * 3f + k, true);
                        var c = birds[k].color; c.a = 1f - Mathf.InverseLerp(0.82f, 1f, u); birds[k].color = c;
                        if (!scored[k] && u >= 0.92f) { scored[k] = true; mid.Squash(); mid.ScoreTick(); }
                    }
                    if (!setDone && scored[2] && t > 0.15f + 2 * stagger + flyDur) { setDone = true; setAt = t; mid.SetPop(); }
                    mid.Step(dt, t);
                    peak = Mathf.Max(peak, mid.transform.localScale.x / FeederView.Scale);
                    var tex = Render(cam, 540, 960);
                    File.WriteAllBytes(dir + "/f" + f.ToString("000") + ".png", tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    f++;
                }
                mid.SnapHome();
            }
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(camGo);
            Check("clip-set-peak", peak > 1.15f && peak < 1.40f, "rendered feeder peak x" + peak.ToString("0.00"));
            string ff = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
            string mp4 = ShotDir + "/b84-feeder-arrival.mp4";
            var psi = new ProcessStartInfo
            {
                FileName = ff,
                Arguments = "-y -framerate 60 -i \"" + dir + "/f%03d.png\" -c:v libx264 -pix_fmt yuv420p \"" + mp4 + "\"",
                UseShellExecute = false, CreateNoWindow = true
            };
            using (var proc = Process.Start(psi)) { if (proc != null) proc.WaitForExit(60000); }
            Check("clip-written", File.Exists(mp4) && new FileInfo(mp4).Length > 5000, mp4);
        }

        static Texture2D Render(Camera cam, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            return tex;
        }
    }
}
#endif

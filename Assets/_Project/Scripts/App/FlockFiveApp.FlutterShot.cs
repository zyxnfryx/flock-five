#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        const int FlutterW = 1170;
        const int FlutterH = 2532;
        const string FlutterFrames = "/tmp/flock-peach-flutter-frames";
        const string FlutterOut = "/Users/zfxgames/wkspaces/fin-flutter-out";
        const string FlutterLog = "/tmp/flock-peach-flutter.log";
        const string FlutterDone = "/tmp/flock-peach-flutter-done";

        bool _flutterLive;
        bool _peachFlutterOnce;
        int _flutterFrames;
        float _bestUp = -1f;
        float _bestMid = -1f;

        IEnumerator ShotPeachFlutter()
        {
            if (_peachFlutterOnce) yield break;
            _peachFlutterOnce = true;
            EditorShotLive = true;
            UnityEditor.EditorApplication.isPaused = false;
            Application.runInBackground = true;
            Time.timeScale = 1f;
            // Real frames. captureFramerate would step through the hit-stop instead of showing it.
            Time.captureFramerate = 0;
            AudioListener.volume = 0f;

            try { if (Directory.Exists(FlutterFrames)) Directory.Delete(FlutterFrames, true); } catch { }
            Directory.CreateDirectory(FlutterFrames);
            Directory.CreateDirectory(FlutterOut);
            try { if (File.Exists(FlutterDone)) File.Delete(FlutterDone); } catch { }

            var log = new StringBuilder();
            log.AppendLine("peach flutter capture");
            Load(0);
            yield return new WaitForSecondsRealtime(0.35f);
            StopPests();
            _busy = true;
            _won = true;

            _flutterLive = true;
            _flutterFrames = 0;
            var rec = StartCoroutine(RecordFlutter(log));
            if (_garden.Root != null)
                yield return FinaleShow.Play(_garden, this);
            else
                log.AppendLine("no garden");
            _flutterLive = false;
            yield return rec;

            string mp4 = Path.Combine(FlutterOut, "b88-peach-flutter.mp4");
            EncodeFlutter(FlutterFrames, mp4, log);
            File.WriteAllText(FlutterLog, log.ToString());
            Debug.Log(log.ToString());
            File.WriteAllText(FlutterDone, "1");
            EditorShotLive = false;
            UnityEditor.EditorApplication.Exit(0);
        }

        IEnumerator RecordFlutter(StringBuilder log)
        {
            float prev = Time.unscaledTime;
            float next = prev;
            const float step = 1f / 60f;
            var concat = new StringBuilder();
            concat.Append("ffconcat version 1.0\n");
            string last = null;
            int guard = 0;
            while (_flutterLive && guard < 1800)
            {
                yield return new WaitForEndOfFrame();
                guard++;
                if (!_flutterLive) break;
                float now = Time.unscaledTime;
                if (_flutterFrames > 0 && now + 0.0005f < next) continue;
                var cam = _garden.Cam != null ? _garden.Cam : Camera.main;
                var tex = GrabFlutter(cam, log);
                if (tex == null) continue;
                float dt = _flutterFrames == 0 ? step : Mathf.Clamp(now - prev, 0.001f, 0.25f);
                prev = now;
                next = now + step;
                string name = _flutterFrames.ToString("D4") + ".jpg";
                string path = Path.Combine(FlutterFrames, name);
                File.WriteAllBytes(path, tex.EncodeToJPG(86));
                NoteFlutterStill(tex, log);
                Destroy(tex);
                concat.Append("file '").Append(path).Append("'\n");
                concat.Append("duration ").Append(dt.ToString("0.0000", CultureInfo.InvariantCulture)).Append("\n");
                last = path;
                _flutterFrames++;
            }
            if (last != null) concat.Append("file '").Append(last).Append("'\n");
            File.WriteAllText(Path.Combine(FlutterFrames, "frames.txt"), concat.ToString());
            log.Append("frames ").Append(_flutterFrames).Append(" up ").Append(_bestUp.ToString("0.00"))
                .Append(" mid ").Append(_bestMid.ToString("0.00")).AppendLine();
        }

        void NoteFlutterStill(Texture2D tex, StringBuilder log)
        {
            var peach = GameObject.Find("MascotPeach");
            if (peach == null) return;
            var idle = peach.GetComponent<BirdIdle>();
            var sr = peach.GetComponent<SpriteRenderer>();
            if (idle == null || !idle.FlapDriven) return;
            float scale = peach.transform.localScale.x;
            float alpha = sr != null ? sr.color.a : 1f;
            int pose = idle.WingPose;
            if (pose != 1 && pose != 2) return;
            if (alpha < 0.5f || scale < 0.9f || scale > 4.4f) return;
            float score = scale <= 2.2f ? scale : 2.2f - (scale - 2.2f) * 0.35f;
            if (pose == 1 && score > _bestUp)
            {
                _bestUp = score;
                File.WriteAllBytes(Path.Combine(FlutterOut, "b88-peach-flutter-1.png"), tex.EncodeToPNG());
                log.Append("still up scale ").Append(scale.ToString("0.00")).AppendLine();
            }
            else if (pose == 2 && score > _bestMid)
            {
                _bestMid = score;
                File.WriteAllBytes(Path.Combine(FlutterOut, "b88-peach-flutter-2.png"), tex.EncodeToPNG());
                log.Append("still mid scale ").Append(scale.ToString("0.00")).AppendLine();
            }
        }

        static bool _loggedGrab;

        static Texture2D GrabFlutter(Camera cam, StringBuilder log)
        {
            Texture2D shot = null;
            try { shot = ScreenCapture.CaptureScreenshotAsTexture(); }
            catch (System.Exception e) { log.Append("screen ").AppendLine(e.Message); }
            if (shot != null && shot.height > shot.width * 1.2f && !Blank(shot))
            {
                var fit = FitPortrait(shot);
                if (!ReferenceEquals(fit, shot)) Destroy(shot);
                return fit;
            }
            int sw = shot != null ? shot.width : 0;
            int sh = shot != null ? shot.height : 0;
            if (shot != null) Destroy(shot);
            var rendered = RenderFlutterCam(cam);
            if (rendered != null && !Blank(rendered))
            {
                if (!_loggedGrab)
                {
                    _loggedGrab = true;
                    log.Append("screen ").Append(sw).Append('x').Append(sh).AppendLine(" fell back to camera");
                }
                return rendered;
            }
            if (rendered != null) Destroy(rendered);
            return null;
        }

        static Texture2D RenderFlutterCam(Camera cam)
        {
            if (cam == null) return null;
            var rt = new RenderTexture(FlutterW, FlutterH, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0.05f, 0.06f, 0.10f, 1f));
            RenderTexture.active = prevActive;

            var prevTarget = cam.targetTexture;
            var prevRect = cam.rect;
            var prevAspect = cam.aspect;
            cam.targetTexture = null;
            float want = WorldBuilder.PortraitAspect;
            float window = FlutterW / (float)FlutterH;
            cam.aspect = want;
            if (window < want - 0.01f)
            {
                float rh = window / want;
                cam.rect = new Rect(0f, (1f - rh) * 0.5f, 1f, rh);
            }
            else cam.rect = new Rect(0f, 0f, 1f, 1f);

            Texture2D tex = null;
            try
            {
                var req = new RenderPipeline.StandardRequest
                {
                    destination = rt,
                    mipLevel = 0,
                    slice = 0,
                    face = CubemapFace.Unknown
                };
                RenderPipeline.SubmitRenderRequest(cam, req);
                tex = new Texture2D(FlutterW, FlutterH, TextureFormat.RGB24, false);
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, FlutterW, FlutterH), 0, 0);
                tex.Apply();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("flutter render: " + e.Message);
                if (tex != null) Destroy(tex);
                tex = null;
            }
            RenderTexture.active = prevActive;
            cam.targetTexture = prevTarget;
            cam.rect = prevRect;
            cam.aspect = prevAspect;
            rt.Release();
            Destroy(rt);
            return tex;
        }

        static Texture2D FitPortrait(Texture2D src)
        {
            if (src.width == FlutterW && src.height == FlutterH) return src;
            float sa = src.width / (float)Mathf.Max(1, src.height);
            int dw, dh;
            if (sa >= FlutterW / (float)FlutterH)
            {
                dw = FlutterW;
                dh = Mathf.Max(1, Mathf.RoundToInt(FlutterW / sa));
            }
            else
            {
                dh = FlutterH;
                dw = Mathf.Max(1, Mathf.RoundToInt(FlutterH * sa));
            }
            var rt = RenderTexture.GetTemporary(dw, dh, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var scaled = new Texture2D(dw, dh, TextureFormat.RGB24, false);
            scaled.ReadPixels(new Rect(0, 0, dw, dh), 0, 0);
            scaled.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            var canvas = new Texture2D(FlutterW, FlutterH, TextureFormat.RGB24, false);
            var bg = new Color(0.05f, 0.06f, 0.10f);
            var fill = new Color[FlutterW * FlutterH];
            for (int i = 0; i < fill.Length; i++) fill[i] = bg;
            canvas.SetPixels(fill);
            canvas.SetPixels((FlutterW - dw) / 2, (FlutterH - dh) / 2, dw, dh, scaled.GetPixels());
            canvas.Apply();
            Destroy(scaled);
            return canvas;
        }

        static bool Blank(Texture2D t)
        {
            if (t == null || t.width < 8 || t.height < 8) return true;
            float mean = 0f;
            float acc2 = 0f;
            int n = 0;
            int x0 = t.width / 2;
            int y0 = t.height / 2;
            for (int dy = -30; dy <= 30; dy += 15)
            {
                for (int dx = -15; dx <= 15; dx += 15)
                {
                    var c = t.GetPixel(Mathf.Clamp(x0 + dx, 0, t.width - 1), Mathf.Clamp(y0 + dy, 0, t.height - 1));
                    float v = c.maxColorComponent;
                    mean += v;
                    acc2 += v * v;
                    n++;
                }
            }
            mean /= n;
            float var = acc2 / n - mean * mean;
            return mean < 0.02f && var < 0.00005f;
        }

        static void EncodeFlutter(string frames, string mp4, StringBuilder log)
        {
            string list = Path.Combine(frames, "frames.txt");
            if (!File.Exists(list) || new FileInfo(list).Length < 16)
            {
                log.AppendLine("no frames");
                return;
            }
            string ffmpeg = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = "-y -f concat -safe 0 -i \"" + list + "\" -fps_mode vfr -c:v libx264 -pix_fmt yuv420p -crf 18 -movflags +faststart \"" + mp4 + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            try
            {
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    if (p == null) { log.AppendLine("ffmpeg missing"); return; }
                    string err = p.StandardError.ReadToEnd();
                    p.WaitForExit(180000);
                    log.Append("ffmpeg ").Append(p.ExitCode).AppendLine();
                    if (p.ExitCode != 0) log.AppendLine(err.Length > 500 ? err.Substring(err.Length - 500) : err);
                }
            }
            catch (System.Exception e)
            {
                log.Append("ffmpeg ").AppendLine(e.Message);
            }
            if (File.Exists(mp4) && new FileInfo(mp4).Length > 1000)
            {
                try { if (Directory.Exists(frames)) Directory.Delete(frames, true); } catch { }
            }
        }
    }
}
#endif

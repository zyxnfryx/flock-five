#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void MuteStormDropShot()
        {
            if (!File.Exists("/tmp/flock-five-storm-drops")) return;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

        // Storm garden, iPhone portrait. Glass beads at 60fps, plus one close crop.
        public IEnumerator ShotStormDrops()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            AudioListener.volume = 0f;
            AudioListener.pause = true;

            int next = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int coach = PlayerPrefs.GetInt(CoachKey, 0);
            bool hadCoach = PlayerPrefs.HasKey(CoachKey);
            int leaf = PlayerPrefs.GetInt(CoachLeafKey, 0);
            bool hadLeaf = PlayerPrefs.HasKey(CoachLeafKey);
            int hiveGarden = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveGardenKey);
            int bee = PlayerPrefs.GetInt(CoachBeeKey, 0);
            bool hadBee = PlayerPrefs.HasKey(CoachBeeKey);
            int sparrow = PlayerPrefs.GetInt(CoachSparrowKey, 0);
            bool hadSparrow = PlayerPrefs.HasKey(CoachSparrowKey);
            int hawk = PlayerPrefs.GetInt(CoachHawkKey, 0);
            bool hadHawk = PlayerPrefs.HasKey(CoachHawkKey);
            int ostrich = PlayerPrefs.GetInt(CoachOstrichKey, 0);
            bool hadOstrich = PlayerPrefs.HasKey(CoachOstrichKey);
            bool hadBolt = PlayerPrefs.HasKey(LightningStrike.PrefKey);
            int bolt = PlayerPrefs.GetInt(LightningStrike.PrefKey, 0);
            const string seenKey = "flockfive.season.seen";
            bool hadSeen = PlayerPrefs.HasKey(seenKey);
            string seen = PlayerPrefs.GetString(seenKey, "");

            const string mp4 = "/Users/zfxgames/wkspaces/post88/storm-drops-v2.mp4";
            const string still = "/Users/zfxgames/wkspaces/post88/storm-drops-v2.png";
            const string frames = "/tmp/storm-drop-frames";
            const string look = "/tmp/drops3-look";
            var lives = new StormGlassRig.Live[StormGlass.Cap];
            try
            {
                Directory.CreateDirectory("/Users/zfxgames/wkspaces/post88");
                if (Directory.Exists(frames)) Directory.Delete(frames, true);
                Directory.CreateDirectory(frames);
                if (Directory.Exists(look)) Directory.Delete(look, true);
                Directory.CreateDirectory(look);

                LightningStrike.Enabled = false;
                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                Screen.SetResolution(1179, 2556, false);
                yield return new WaitForSecondsRealtime(0.6f);
                MuteShotAudio();
                SeasonReveal.Mark(1);
                PrepGardenShot(6);
                HoldGardenQuiet();
                yield return null;
                HoldGardenQuiet();
                if (GardenStorm.Instance != null) GardenStorm.Instance.JumpToStorm();
                yield return null;
                HoldGardenQuiet();

                float wait = 0f;
                while (wait < 8f && StormGlassRig.CopyLive(lives) < 8)
                {
                    MuteShotAudio();
                    HoldGardenQuiet();
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
                UnityEngine.Debug.Log("STORM_DROP_WAIT " + wait.ToString("0.00")
                    + " wet " + GardenStorm.Wet.ToString("0.00")
                    + " beads " + StormGlassRig.CopyLive(lives));

                Time.captureDeltaTime = 1f / 60f;
                float bestScore = 0f;
                int saved = 0;
                int sawSplat = 0;
                int sawSlide = 0;
                float maxR = 0f;
                const int total = 360;
                for (int f = 0; f < total; f++)
                {
                    yield return null;
                    HoldGardenQuiet();
                    MuteShotAudio();
                    yield return new WaitForEndOfFrame();
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    if (tex == null) continue;
                    File.WriteAllBytes(frames + "/f" + f.ToString("0000") + ".jpg", tex.EncodeToJPG(92));
                    saved++;
                    int liveN = StormGlassRig.CopyLive(lives);
                    float score = 0f;
                    int pick = -1;
                    int splatN = 0;
                    int slideN = 0;
                    for (int i = 0; i < liveN; i++)
                    {
                        var bead = lives[i];
                        if (bead.Radius > maxR) maxR = bead.Radius;
                        if (bead.Splat > 0.62f && bead.Alpha > 0.35f) splatN++;
                        if (bead.Splat < 0.18f && bead.Vy < -36f && bead.Alpha > 0.45f) slideN++;
                        if (bead.Splat < 0.2f && bead.Alpha > 0.5f && bead.Vy < -28f && bead.Kind == StormGlass.Slide)
                        {
                            float s = bead.Alpha * bead.Radius * (-bead.Vy);
                            if (s > score)
                            {
                                score = s;
                                pick = i;
                            }
                        }
                    }
                    if (splatN > 0) sawSplat++;
                    if (slideN > 0) sawSlide++;
                    if (f > 40 && slideN > 0 && liveN >= 6 && score > bestScore && pick >= 0)
                    {
                        bestScore = score;
                        File.WriteAllBytes(still, tex.EncodeToPNG());
                        SaveCloseup(tex, lives[pick], look + "/lens.png");
                    }
                    if (f == 180 && !File.Exists(still))
                        File.WriteAllBytes(still, tex.EncodeToPNG());
                    if (f == 20 || f == 80 || f == 140 || f == 200 || f == 260 || f == 320)
                        File.WriteAllBytes(look + "/f" + f.ToString("0000") + ".png", tex.EncodeToPNG());
                    Object.Destroy(tex);
                }
                UnityEngine.Debug.Log("STORM_DROP_LOOK splatFrames " + sawSplat
                    + " slideFrames " + sawSlide + " maxR " + maxR.ToString("0.0")
                    + " score " + bestScore.ToString("0"));
                Time.captureDeltaTime = 0f;
                string ff = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
                var psi = new ProcessStartInfo
                {
                    FileName = ff,
                    Arguments = "-y -framerate 60 -i \"" + frames + "/f%04d.jpg\" -vf pad=ceil(iw/2)*2:ceil(ih/2)*2 -c:v libx264 -pix_fmt yuv420p -crf 18 -r 60 \"" + mp4 + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc != null) proc.WaitForExit(120000);
                }
                File.WriteAllText("/tmp/storm-drop-clip-done.txt",
                    "STORM_DROP_CLIP frames " + saved + " score " + bestScore.ToString("0")
                    + " splat " + sawSplat + " slide " + sawSlide
                    + " maxR " + maxR.ToString("0.0")
                    + " still " + (File.Exists(still) ? "yes" : "no") + "\n");
                UnityEngine.Debug.Log("STORM_DROP_CLIP_DONE " + saved);
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                if (hadNext) PlayerPrefs.SetInt("flockfive.next", next);
                else PlayerPrefs.DeleteKey("flockfive.next");
                if (hadCoach) PlayerPrefs.SetInt(CoachKey, coach);
                else PlayerPrefs.DeleteKey(CoachKey);
                if (hadLeaf) PlayerPrefs.SetInt(CoachLeafKey, leaf);
                else PlayerPrefs.DeleteKey(CoachLeafKey);
                if (hadHive) PlayerPrefs.SetInt(CoachHiveGardenKey, hiveGarden);
                else PlayerPrefs.DeleteKey(CoachHiveGardenKey);
                if (hadBee) PlayerPrefs.SetInt(CoachBeeKey, bee);
                else PlayerPrefs.DeleteKey(CoachBeeKey);
                if (hadSparrow) PlayerPrefs.SetInt(CoachSparrowKey, sparrow);
                else PlayerPrefs.DeleteKey(CoachSparrowKey);
                if (hadHawk) PlayerPrefs.SetInt(CoachHawkKey, hawk);
                else PlayerPrefs.DeleteKey(CoachHawkKey);
                if (hadOstrich) PlayerPrefs.SetInt(CoachOstrichKey, ostrich);
                else PlayerPrefs.DeleteKey(CoachOstrichKey);
                if (hadBolt) PlayerPrefs.SetInt(LightningStrike.PrefKey, bolt);
                else PlayerPrefs.DeleteKey(LightningStrike.PrefKey);
                if (hadSeen) PlayerPrefs.SetString(seenKey, seen);
                else PlayerPrefs.DeleteKey(seenKey);
                PlayerPrefs.Save();
                EditorShotLive = false;
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }

        static void SaveCloseup(Texture2D tex, StormGlassRig.Live bead, string path)
        {
            float sx = tex.width / Mathf.Max(1f, PhoneArea.W);
            float sy = tex.height / Mathf.Max(1f, PhoneArea.H);
            int cx = Mathf.RoundToInt(bead.X * sx);
            int cy = Mathf.RoundToInt(bead.Y * sy);
            int rad = Mathf.Max(8, Mathf.RoundToInt(bead.Radius * sy));
            int box = rad * 6;
            int x = Mathf.Clamp(cx - box / 2, 0, Mathf.Max(0, tex.width - 2));
            int y = Mathf.Clamp(cy - box / 2, 0, Mathf.Max(0, tex.height - 2));
            int w = Mathf.Min(box, tex.width - x);
            int h = Mathf.Min(box, tex.height - y);
            if (w < 8 || h < 8) return;
            var crop = new Texture2D(w, h, TextureFormat.RGBA32, false);
            crop.SetPixels(tex.GetPixels(x, y, w, h));
            crop.Apply(false);
            const int scale = 4;
            var big = new Texture2D(w * scale, h * scale, TextureFormat.RGBA32, false);
            var src = crop.GetPixels32();
            var dst = new Color32[w * scale * h * scale];
            for (int row = 0; row < h * scale; row++)
            {
                int sy0 = row / scale;
                for (int col = 0; col < w * scale; col++)
                    dst[row * w * scale + col] = src[sy0 * w + col / scale];
            }
            big.SetPixels32(dst);
            big.Apply(false);
            File.WriteAllBytes(path, big.EncodeToPNG());
            Object.Destroy(crop);
            Object.Destroy(big);
        }
    }
}
#endif

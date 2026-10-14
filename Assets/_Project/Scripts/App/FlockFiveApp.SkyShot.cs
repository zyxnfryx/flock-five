#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void MuteSkyShot()
        {
            if (!File.Exists("/tmp/flock-five-b88-sky")) return;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

        // Night garden, then the next stage. The sky walks forward through dawn.
        public IEnumerator ShotSkyTransition()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;

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
            PlayerPrefs.SetInt(CoachBeeKey, 1);
            PlayerPrefs.SetInt(CoachSparrowKey, 1);
            PlayerPrefs.SetInt(CoachHawkKey, 1);
            PlayerPrefs.SetInt(CoachOstrichKey, 1);
            PlayerPrefs.Save();
            const string mp4 = "/Users/zfxgames/wkspaces/wingout/b88-sky-transition.mp4";
            const string frames = "/tmp/sky-frames";
            var phases = new StringBuilder();
            try
            {
                Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout");
                if (Directory.Exists(frames)) Directory.Delete(frames, true);
                Directory.CreateDirectory(frames);
                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                Screen.SetResolution(1179, 2556, false);
                yield return new WaitForSecondsRealtime(0.6f);
                MuteShotAudio();

                SkyCycle.ParkNextPresent();
                PrepGardenShot(5);
                HoldGardenQuiet();
                yield return null;
                HoldGardenQuiet();
                yield return new WaitForEndOfFrame();

                float from = SkyCycle.PhaseForLevel(5);
                float to = SkyCycle.PhaseForLevel(6);
                int hold = 90;
                int glide = Mathf.CeilToInt(SkyCycle.GlideSeconds(from, to) * 60f) + 72;
                Time.captureDeltaTime = 1f / 60f;
                int w = 0, h = 0;
                for (int f = 0; f < hold + glide; f++)
                {
                    if (f == hold) PrepGardenShot(6);
                    // After Update, before the frame is drawn, so a lesson glove cannot pop in.
                    yield return null;
                    HoldGardenQuiet();
                    MuteShotAudio();
                    yield return new WaitForEndOfFrame();
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    if (tex == null) continue;
                    w = tex.width;
                    h = tex.height;
                    File.WriteAllBytes(frames + "/f" + f.ToString("0000") + ".jpg", tex.EncodeToJPG(82));
                    Object.Destroy(tex);
                    phases.Append(f.ToString("000")).Append(' ').Append(SkyCycle.Phase.ToString("0.000"))
                        .Append(' ').Append(SkyCycle.Dusk.ToString("0.000")).Append('\n');
                }
                Time.captureDeltaTime = 0f;
                File.WriteAllText("/tmp/sky-clip-phases.txt", phases.ToString());
                string ff = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
                var psi = new ProcessStartInfo
                {
                    FileName = ff,
                    Arguments = "-y -framerate 60 -i \"" + frames + "/f%04d.jpg\" -vf pad=ceil(iw/2)*2:ceil(ih/2)*2 -c:v libx264 -pix_fmt yuv420p -r 60 \"" + mp4 + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc != null) proc.WaitForExit(120000);
                }
                File.WriteAllText("/tmp/sky-clip-done.txt", "SKY_CLIP " + w + "x" + h + " frames " + (hold + glide) + "\n");
                UnityEngine.Debug.Log("SKY_CLIP_DONE " + w + "x" + h);
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
                PlayerPrefs.Save();
                EditorShotLive = false;
                ShotKeepAwake = false;
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }
    }
}
#endif

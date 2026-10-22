#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Settings open, b89 row order. One device per launch; the editor
        // menu writes "tag w h" into the trigger file.
        public IEnumerator ShotBuild89Settings()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AdConsent.EditorHoldAtt = false;
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            AudioListener.volume = 0f;
            AudioListener.pause = true;

            string tag = "iphone"; int w = 1179, h = 2556;
            const string trig = "/tmp/flock-five-b89-settings-arg";
            if (File.Exists(trig))
            {
                var p = File.ReadAllText(trig).Trim().Split(' ');
                if (p.Length == 3) { tag = p[0]; int.TryParse(p[1], out w); int.TryParse(p[2], out h); }
            }
            Debug.Log("B89SHOT start " + tag + " " + w + "x" + h);
            const string dir = "/Users/zfxgames/wkspaces/wingout";
            Directory.CreateDirectory(dir);
            try
            {
                if (EditorGameSize != null) EditorGameSize(w, h);
                yield return new WaitForSecondsRealtime(1.0f);
                MuteShotAudio();
                Debug.Log("B89SHOT sized " + PhoneArea.W + "x" + PhoneArea.H);
                ShowSplash();
                float t = 0f;
                while (t < 0.8f)
                {
                    PoseBuild79(true, false);
                    MuteShotAudio();
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                PoseBuild79(true, false);
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                Debug.Log("B89SHOT saving");
                SaveFrame(dir + "/b89-settings-order-" + tag + ".png");
                File.WriteAllText("/tmp/flock-five-b89-settings-done", tag + " " + PhoneArea.W + "x" + PhoneArea.H + "\n");
            }
            finally
            {
                EditorShotLive = false;
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
#endif

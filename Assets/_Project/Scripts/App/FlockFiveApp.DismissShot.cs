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
        // Spin stop through the coin flight, the balance count, and the ease-out.
        public IEnumerator ShotDailyDismiss()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;

            int next = PlayerPrefs.GetInt("flockfive.next", 0);
            int daily = PlayerPrefs.GetInt(CoachDailyKey, 0);
            int hive = PlayerPrefs.GetInt(CoachHiveKey, 0);
            int poker = PlayerPrefs.GetInt(CoachPokerKey, 0);
            int welcome = PlayerPrefs.GetInt(WelcomeBonusKey, 0);
            const string mp4 = "/Users/zfxgames/wkspaces/wingout/b88-daily-dismiss.mp4";
            const string frames = "/tmp/b88-daily-frames";
            try
            {
                Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout");
                if (Directory.Exists(frames)) Directory.Delete(frames, true);
                Directory.CreateDirectory(frames);
                PlayerPrefs.SetInt("flockfive.next", 2);
                PlayerPrefs.SetInt(CoachDailyKey, 1);
                PlayerPrefs.SetInt(CoachHiveKey, 1);
                PlayerPrefs.SetInt(CoachPokerKey, 1);
                PlayerPrefs.SetInt(WelcomeBonusKey, 1);
                PlayerPrefs.Save();
                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                Screen.SetResolution(1179, 2556, false);
                yield return new WaitForSecondsRealtime(0.4f);
                MuteShotAudio();
                DailyBonus.TestBegin(20261013, 0, 2);
                ShowSplash();
                _dailyIntro = false;
                _hiveIntro = false;
                _pokerIntro = false;
                _adoptLive = false;
                _welcomeOpen = false;
                _welcomeQueued = false;
                _welcomeGlove = false;
                _dailyAskOpen = false;
                _dailyOpen = false;
                _dailyShotQuiet = false;
                yield return WaitStudioCardDown("DAILY_DISMISS_HOME");
                _dailyOpen = true;
                _dailyPopAt = AdPause.Unscaled;
                _dailyPhase = DailyWheelPhase.Ready;
                _dailyIntroLive = true;
                StartDailySpin();
                _dailyIntroLive = false;
                _dailyClaimFollowUp = false;
                float spinT0 = Time.realtimeSinceStartup;
                while (_dailyPhase != DailyWheelPhase.Landed && Time.realtimeSinceStartup - spinT0 < 12f)
                {
                    MuteShotAudio();
                    yield return null;
                }
                if (WarmCoverLive) yield return WaitStudioCardDown("DAILY_DISMISS_STILL");
                Time.captureDeltaTime = 1f / 60f;
                int n = 0;
                int tail = 0;
                int w = 0, h = 0;
                while (n < 720 && tail < 12)
                {
                    yield return null;
                    MuteShotAudio();
                    yield return new WaitForEndOfFrame();
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    if (tex == null) continue;
                    w = tex.width;
                    h = tex.height;
                    File.WriteAllBytes(frames + "/f" + n.ToString("0000") + ".jpg", tex.EncodeToJPG(82));
                    Object.Destroy(tex);
                    n++;
                    if (!_dailyOpen) tail++;
                }
                Time.captureDeltaTime = 0f;
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
                File.WriteAllText("/tmp/flock-five-b88-dismiss-done", "OK " + w + "x" + h + " frames " + n + "\n");
                UnityEngine.Debug.Log("DAILY_DISMISS_CLIP " + w + "x" + h + " frames " + n);
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                DailyBonus.TestRelease();
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt(CoachDailyKey, daily);
                PlayerPrefs.SetInt(CoachHiveKey, hive);
                PlayerPrefs.SetInt(CoachPokerKey, poker);
                PlayerPrefs.SetInt(WelcomeBonusKey, welcome);
                PlayerPrefs.Save();
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        // The studio card paints last. A single unscaled hitch must not skip this wait.
        IEnumerator WaitStudioCardDown(string tag)
        {
            float t0 = Time.realtimeSinceStartup;
            int calm = 0;
            while (calm < 8 && Time.realtimeSinceStartup - t0 < 22f)
            {
                if (!WarmCoverLive) calm++;
                else calm = 0;
                MuteShotAudio();
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.12f);
            UnityEngine.Debug.Log(tag + " cover " + (Time.realtimeSinceStartup - t0).ToString("0.00") + " warm " + WarmCoverLive);
        }
    }
}
#endif

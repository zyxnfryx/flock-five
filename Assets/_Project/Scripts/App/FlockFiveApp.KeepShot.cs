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
        float _streakShotClock = -1f;
        float _streakShotVeil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void MuteKeepShot()
        {
            if (!File.Exists("/tmp/flock-five-b88-streak")) return;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

        // Wait popup on iPhone and iPad, then the at-risk badge, a short ad
        // cover, and the heal / sheen / pop once that cover lifts.
        public IEnumerator ShotKeepStreak()
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

            int next = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int coach = PlayerPrefs.GetInt(CoachKey, 0);
            bool hadCoach = PlayerPrefs.HasKey(CoachKey);
            int leaf = PlayerPrefs.GetInt(CoachLeafKey, 0);
            bool hadLeaf = PlayerPrefs.HasKey(CoachLeafKey);
            int hiveGarden = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveGardenKey);
            int streak = PlayerPrefs.GetInt("flockfive.streak", 0);
            bool hadStreak = PlayerPrefs.HasKey("flockfive.streak");
            const string dir = "/Users/zfxgames/wkspaces/wingout";
            const string frames = "/tmp/streak-keep-frames";
            const string mp4 = dir + "/b88-keep-streak.mp4";
            Directory.CreateDirectory(dir);
            try
            {
                if (Directory.Exists(frames)) Directory.Delete(frames, true);
                Directory.CreateDirectory(frames);
                yield return null;
                MuteShotAudio();
                UnityEngine.Debug.Log("KEEP_STREAK phase=garden");
                PrepGardenShot(5);
                HoldGardenQuiet();
                ArmKeepPopup();
                yield return new WaitForSecondsRealtime(0.45f);
                UnityEngine.Debug.Log("KEEP_STREAK phase=iphone");

                SetShotSize(1179, 2556);
                yield return new WaitForSecondsRealtime(0.55f);
                yield return PoseKeepStill();
                SaveFrame(dir + "/b88-keep-streak-iphone.png");
                UnityEngine.Debug.Log("KEEP_STREAK phase=ipad");

                SetShotSize(1536, 2048);
                yield return new WaitForSecondsRealtime(0.55f);
                yield return PoseKeepStill();
                SaveFrame(dir + "/b88-keep-streak-ipad.png");

                SetShotSize(1179, 2556);
                yield return new WaitForSecondsRealtime(0.35f);
                int atRisk = 96;
                int ad = 40;
                int restore = Mathf.CeilToInt(StreakBadge.RestoreSeconds * 60f);
                int hold = 24;
                int total = atRisk + ad + restore + hold;
                Time.captureDeltaTime = 1f / 60f;
                UnityEngine.Debug.Log("KEEP_STREAK phase=frames " + total);
                int w = 0, h = 0;
                for (int f = 0; f < total; f++)
                {
                    if (f == atRisk || f == atRisk + ad || (f > 0 && f % 60 == 0))
                        UnityEngine.Debug.Log("KEEP_STREAK frame " + f);
                    yield return null;
                    HoldGardenQuiet();
                    ArmKeepPopup();
                    PoseKeepFrame(f, atRisk, ad, restore);
                    MuteShotAudio();
                    yield return new WaitForEndOfFrame();
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    if (tex == null) continue;
                    w = tex.width;
                    h = tex.height;
                    File.WriteAllBytes(frames + "/f" + f.ToString("0000") + ".jpg", tex.EncodeToJPG(82));
                    Object.Destroy(tex);
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
                File.WriteAllText("/tmp/flock-five-b88-streak-done", "OK " + w + "x" + h + " frames " + total + "\n");
                UnityEngine.Debug.Log("KEEP_STREAK_SHOT " + w + "x" + h + " frames " + total);
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                _streakShotClock = -1f;
                _streakShotVeil = 0f;
                _streakHealAge = -1f;
                _restartAsk = RestartAsk.None;
                if (hadNext) PlayerPrefs.SetInt("flockfive.next", next);
                else PlayerPrefs.DeleteKey("flockfive.next");
                if (hadCoach) PlayerPrefs.SetInt(CoachKey, coach);
                else PlayerPrefs.DeleteKey(CoachKey);
                if (hadLeaf) PlayerPrefs.SetInt(CoachLeafKey, leaf);
                else PlayerPrefs.DeleteKey(CoachLeafKey);
                if (hadHive) PlayerPrefs.SetInt(CoachHiveGardenKey, hiveGarden);
                else PlayerPrefs.DeleteKey(CoachHiveGardenKey);
                if (hadStreak) Purse.SetStreak(streak);
                else Purse.BreakStreak();
                PlayerPrefs.Save();
                EditorShotLive = false;
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }

        void ArmKeepPopup()
        {
            _splash = false;
            _gift = GiftFace.None;
            _freezeOffer = false;
            _restartSavedMul = 3;
            _restartAsk = RestartAsk.Keep;
            Purse.SetStreak(3);
            MuteShotAudio();
        }

        static void SetShotSize(int w, int h)
        {
            if (EditorGameSize != null) EditorGameSize(w, h);
            Screen.SetResolution(w, h, false);
        }

        IEnumerator PoseKeepStill()
        {
            _streakHealAge = -1f;
            _streakShotVeil = 0f;
            _streakShotClock = StillClock();
            ArmKeepPopup();
            yield return null;
            HoldGardenQuiet();
            ArmKeepPopup();
            _streakShotClock = StillClock();
            _streakHealAge = -1f;
            _streakShotVeil = 0f;
            yield return new WaitForEndOfFrame();
            MuteShotAudio();
        }

        static float StillClock()
        {
            float bestT = 0.34f;
            float bestRoll = 99f;
            for (int i = 0; i <= 200; i++)
            {
                float t = i * 0.01f;
                var risk = StreakBadge.AtRisk(t);
                if (risk.Crack < 0.80f) continue;
                float roll = Mathf.Abs(risk.Roll);
                if (roll < bestRoll)
                {
                    bestRoll = roll;
                    bestT = t;
                }
            }
            return bestT;
        }

        void PoseKeepFrame(int f, int atRisk, int ad, int restore)
        {
            int adEnd = atRisk + ad;
            int healEnd = adEnd + restore;
            if (f < atRisk)
            {
                _streakShotVeil = 0f;
                _streakHealAge = -1f;
                _streakShotClock = f / 60f;
                return;
            }
            if (f < adEnd)
            {
                int into = f - atRisk;
                int left = adEnd - f;
                float fadeIn = Mathf.Clamp01(into / 6f);
                float fadeOut = Mathf.Clamp01(left / 8f);
                _streakShotVeil = Mathf.Min(fadeIn, fadeOut);
                _streakHealAge = -1f;
                _streakShotClock = (atRisk - 1) / 60f;
                return;
            }
            _streakShotVeil = 0f;
            _streakShotClock = -1f;
            if (f < healEnd)
                _streakHealAge = (f - adEnd) / 60f;
            else
                _streakHealAge = StreakBadge.RestoreSeconds;
        }
    }
}
#endif

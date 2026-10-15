#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Settings open, Sound On beside a mid slider. iPhone, then iPad.
        public IEnumerator ShotBuild88Mute()
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

            bool hadVol = PlayerPrefs.HasKey(AudioGuard.VolumeKey);
            float vol = PlayerPrefs.GetFloat(AudioGuard.VolumeKey, 1f);
            bool hadMute = PlayerPrefs.HasKey(AudioGuard.MuteKey);
            int mute = PlayerPrefs.GetInt(AudioGuard.MuteKey, 0);

            const string dir = "/Users/zfxgames/wkspaces/wingout";
            Directory.CreateDirectory(dir);
            try
            {
                AudioGuard.SetMuted(false, false);
                AudioGuard.SetMasterVolume(0.62f, false);

                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                yield return new WaitForSecondsRealtime(1.0f);
                MuteShotAudio();
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
                SaveFrame(dir + "/b88-settings-mute-iphone.png");

                if (EditorGameSize != null) EditorGameSize(1536, 2048);
                yield return new WaitForSecondsRealtime(0.8f);
                PoseBuild79(true, false);
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                SaveFrame(dir + "/b88-settings-mute-ipad.png");

                File.WriteAllText("/tmp/flock-five-b88-mute-done", "OK\n");
            }
            finally
            {
                if (hadVol) PlayerPrefs.SetFloat(AudioGuard.VolumeKey, vol);
                else PlayerPrefs.DeleteKey(AudioGuard.VolumeKey);
                if (hadMute) PlayerPrefs.SetInt(AudioGuard.MuteKey, mute);
                else PlayerPrefs.DeleteKey(AudioGuard.MuteKey);
                PlayerPrefs.Save();
                AudioGuard.ResetForTests();
                AudioGuard.LoadMuted();
                AudioGuard.LoadVolume();
                EditorShotLive = false;
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        // The bee-thicket lesson paints over this garden. Call after Update and
        // before the frame is drawn.
        void HoldGardenQuiet()
        {
            PlayerPrefs.SetInt("flockfive.coach.hive.garden", 1);
            _hiveLevelCue = false;
            _hiveLevelLive = false;
            _cueHand = false;
            _cueLine = null;
            _gloveVis = false;
            _coachFade = 0f;
            _shotBeeLesson = false;
            MuteShotAudio();
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Home settings, one garden, and the poker table. The speaker stays up on each.
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

            int streak = PlayerPrefs.GetInt("flockfive.streak", 0);
            int next = PlayerPrefs.GetInt("flockfive.next", 0);
            int hive = PlayerPrefs.GetInt("flockfive.coach.hive", 0);
            int poker = PlayerPrefs.GetInt("flockfive.coach.poker", 0);
            int page = PlayerPrefs.GetInt("flockfive.coach.pokerpage", 0);
            int dealt = PlayerPrefs.GetInt("flockfive.coach.pokerdealt", 0);
            int back = PlayerPrefs.GetInt("flockfive.coach.pokerback", 0);
            int coach = PlayerPrefs.GetInt("flockfive.coach.done", 0);
            int leaf = PlayerPrefs.GetInt("flockfive.coach.leaf", 0);
            int hiveGarden = PlayerPrefs.GetInt("flockfive.coach.hive.garden", 0);

            const string dir = "/Users/zfxgames/wkspaces/wingout";
            Directory.CreateDirectory(dir);
            try
            {
                PlayerPrefs.SetInt("flockfive.coach.poker", 1);
                PlayerPrefs.SetInt("flockfive.coach.pokerpage", 1);
                PlayerPrefs.SetInt("flockfive.coach.pokerdealt", 1);
                PlayerPrefs.SetInt("flockfive.coach.pokerback", 1);
                PlayerPrefs.Save();

                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                yield return new WaitForSecondsRealtime(1.2f);
                MuteShotAudio();
                ShowSplash();
                float t = 0f;
                while (t < 0.7f)
                {
                    PoseBuild79(true, false);
                    MuteShotAudio();
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                PoseBuild79(true, false);
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                SaveFrame(dir + "/b88-mute-over-settings.png");

                PoseBuild79(false, false);
                Purse.SetStreak(3);
                PlayerPrefs.SetInt("flockfive.coach.hive", 1);
                PlayerPrefs.Save();
                PrepGardenShot(4);
                yield return QuietGarden(2.2f);
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                SaveFrame(dir + "/b88-mute-garden-iphone.png");

                QuietPoker();
                MuteShotAudio();
                yield return new WaitForSecondsRealtime(1.4f);
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                SaveFrame(dir + "/b88-mute-poker-iphone.png");

                if (EditorGameSize != null) EditorGameSize(1536, 2048);
                yield return new WaitForSecondsRealtime(1.2f);
                Purse.SetStreak(3);
                PlayerPrefs.SetInt("flockfive.coach.hive", 1);
                PrepGardenShot(4);
                yield return QuietGarden(2.2f);
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                SaveFrame(dir + "/b88-mute-garden-ipad.png");

                QuietPoker();
                MuteShotAudio();
                yield return new WaitForSecondsRealtime(1.4f);
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                SaveFrame(dir + "/b88-mute-poker-ipad.png");

                File.WriteAllText("/tmp/flock-five-b88-mute-done", "OK\n");
            }
            finally
            {
                Purse.SetStreak(streak);
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt("flockfive.coach.hive", hive);
                PlayerPrefs.SetInt("flockfive.coach.poker", poker);
                PlayerPrefs.SetInt("flockfive.coach.pokerpage", page);
                PlayerPrefs.SetInt("flockfive.coach.pokerdealt", dealt);
                PlayerPrefs.SetInt("flockfive.coach.pokerback", back);
                PlayerPrefs.SetInt("flockfive.coach.done", coach);
                PlayerPrefs.SetInt("flockfive.coach.leaf", leaf);
                PlayerPrefs.SetInt("flockfive.coach.hive.garden", hiveGarden);
                PlayerPrefs.Save();
                EditorShotLive = false;
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        // The bee-thicket lesson paints over this garden. Hold it down so the
        // still shows the speaker against the stamp, the hive, and the feeders.
        IEnumerator QuietGarden(float seconds)
        {
            float t = 0f;
            while (t < seconds)
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
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        void QuietPoker()
        {
            ShowSplash();
            _home = HomeFace.Poker;
            _splash = true;
            _settingsOpen = false;
            _settingsPhase = SettingsPhase.Closed;
            _pokerPayOpen = false;
            _pokerPayAnim = 0f;
            _pokerIntro = false;
            _pokerIntroLive = false;
            _hiveIntro = false;
            _hiveIntroLive = false;
            _dailyIntro = false;
            _dailyIntroLive = false;
            _adoptLive = false;
            _welcomeOpen = false;
            _welcomeGlove = false;
            _dailyOpen = false;
            _dailyAskOpen = false;
            _streakSlide = -1f;
            DismissStreakSign();
            VipOffer.Close();
            AdConsent.DismissTransient();
            AdConsent.EditorHoldAtt = false;
        }
    }
}
#endif

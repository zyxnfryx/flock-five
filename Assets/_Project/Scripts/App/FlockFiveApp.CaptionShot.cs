#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Adopt greet, sparrow banner, and the bee banner (iPhone, then iPad).
        public IEnumerator ShotBuild88Caption()
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
            int daily = PlayerPrefs.GetInt("flockfive.coach.daily", 0);
            int noads = PlayerPrefs.GetInt("flockfive.noads", 0);
            int sparrow = PlayerPrefs.GetInt(CoachSparrowKey, 0);
            bool hadSparrow = PlayerPrefs.HasKey(CoachSparrowKey);
            int hiveGarden = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveGardenKey);
            const string dir = "/Users/zfxgames/wkspaces/wingout";
            Directory.CreateDirectory(dir);
            File.WriteAllText("/tmp/cap-shot.txt", "");
            try
            {
                PlayerPrefs.SetInt("flockfive.next", next < 1 ? 1 : next);
                PlayerPrefs.SetInt("flockfive.coach.daily", 0);
                PlayerPrefs.SetInt("flockfive.noads", 0);
                PlayerPrefs.Save();
                SetNoAdsOwned(false);

                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                yield return new WaitForSecondsRealtime(0.8f);
                MuteShotAudio();
                ShowSplash();
                _shotBeeLesson = false;
                _shotBirdHome = true;
                yield return new WaitForSecondsRealtime(1.8f);
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                LogCaptionShot("adopt");
                SaveFrame(dir + "/b88-caption-size-adopt.png");

                _shotBirdHome = false;
                _adoptLive = false;
                _cueLine = null;
                _gloveVis = false;
                PlayerPrefs.SetInt(CoachSparrowKey, 0);
                PlayerPrefs.Save();
                PrepGardenShot(0);
                GamePause.Reset();
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(1.2f);
                GamePause.Reset();
                Time.timeScale = 1f;
                MuteShotAudio();
                if (_garden.Feeders != null && _garden.Root != null && SparrowView.Live == null)
                    StartCoroutine(SparrowView.Visit(_garden.Feeders, _garden.Root));
                float arrive = 0f;
                while (arrive < 4f)
                {
                    var live = SparrowView.Live;
                    if (live != null && live.Settled && live.IsBlocking) break;
                    arrive += AdPause.UnscaledDelta;
                    yield return null;
                }
                if (SparrowView.Live != null && _pestCue != PestCueSparrow)
                    BeginPestCue(PestCueSparrow);
                _coachFade = 1f;
                float shown = 0f;
                while (shown < 0.7f)
                {
                    if (SparrowView.Live != null && _pestCue != PestCueSparrow)
                        BeginPestCue(PestCueSparrow);
                    _coachFade = 1f;
                    shown += AdPause.UnscaledDelta;
                    yield return null;
                }
                _coachFade = 1f;
                MuteShotAudio();
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                LogCaptionShot("sparrow");
                SaveFrame(dir + "/b88-caption-size-sparrow.png");

                _pestCue = 0;
                _cueLine = null;
                _cueHand = false;
                _gloveVis = false;
                _coachFade = 0f;
                ReleaseTutorPause();
                GamePause.Reset();
                Time.timeScale = 1f;
                if (SparrowView.Live != null)
                    Destroy(SparrowView.Live.gameObject);

                PlayerPrefs.SetInt(CoachHiveGardenKey, 0);
                PlayerPrefs.Save();
                _shotBeeLesson = true;
                _shotGlovePin = true;
                yield return new WaitForSecondsRealtime(1.3f);
                _coachFade = 1f;
                MuteShotAudio();
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                LogCaptionShot("bee-iphone");
                SaveFrame(dir + "/b88-bee-banner-iphone.png");

                if (EditorGameSize != null) EditorGameSize(1536, 2048);
                _shotBeeLesson = true;
                _coachFade = 1f;
                yield return new WaitForSecondsRealtime(1.5f);
                _coachFade = 1f;
                MuteShotAudio();
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                LogCaptionShot("bee-ipad");
                SaveFrame(dir + "/b88-bee-banner-ipad.png");
                File.WriteAllText("/tmp/flock-five-b88-caption-done", "OK\n");
            }
            finally
            {
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt("flockfive.coach.daily", daily);
                PlayerPrefs.SetInt("flockfive.noads", noads);
                if (hadSparrow) PlayerPrefs.SetInt(CoachSparrowKey, sparrow);
                else PlayerPrefs.DeleteKey(CoachSparrowKey);
                if (hadHive) PlayerPrefs.SetInt(CoachHiveGardenKey, hiveGarden);
                else PlayerPrefs.DeleteKey(CoachHiveGardenKey);
                PlayerPrefs.Save();
                SetNoAdsOwned(noads == 1);
                _shotBirdHome = false;
                _shotBeeLesson = false;
                _shotGlovePin = false;
                _adoptLive = false;
                _pestCue = 0;
                EditorShotLive = false;
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        void LogCaptionShot(string tag)
        {
            string line = tag + " " + Screen.width + "x" + Screen.height
                + " band " + _bannerLast
                + " last " + BannerLast
                + " skip " + BannerSkip
                + " uses " + BannerUses + " snug " + SnugUses
                + " line " + (_cueLine ?? "");
            File.AppendAllText("/tmp/cap-shot.txt", line + "\n");
            Debug.Log("[caption-shot] " + line);
        }
    }
}
#endif

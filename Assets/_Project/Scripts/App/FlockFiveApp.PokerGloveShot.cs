#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Landed glove, from the left, clear of the poker caption. iPhone then iPad.
        public IEnumerator ShotPokerGlove()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            _shotGlovePin = true;

            int next = PlayerPrefs.GetInt("flockfive.next", 0);
            int daily = PlayerPrefs.GetInt(CoachDailyKey, 0);
            int hive = PlayerPrefs.GetInt(CoachHiveKey, 0);
            int poker = PlayerPrefs.GetInt(CoachPokerKey, 0);
            int welcome = PlayerPrefs.GetInt(WelcomeBonusKey, 0);
            const string dir = "/Users/zfxgames/wkspaces/wingout";
            try
            {
                Directory.CreateDirectory(dir);
                PlayerPrefs.SetInt("flockfive.next", 2);
                PlayerPrefs.SetInt(CoachDailyKey, 1);
                PlayerPrefs.SetInt(CoachHiveKey, 1);
                PlayerPrefs.SetInt(CoachPokerKey, 0);
                PlayerPrefs.SetInt(WelcomeBonusKey, 1);
                PlayerPrefs.Save();
                yield return ShotPokerGloveAt(1179, 2556, dir + "/b88-poker-glove-iphone.png", "iphone");
                yield return ShotPokerGloveAt(1536, 2048, dir + "/b88-poker-glove-ipad.png", "ipad");
                File.WriteAllText("/tmp/flock-five-b88-poker-glove-done", "OK\n");
            }
            finally
            {
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt(CoachDailyKey, daily);
                PlayerPrefs.SetInt(CoachHiveKey, hive);
                PlayerPrefs.SetInt(CoachPokerKey, poker);
                PlayerPrefs.SetInt(WelcomeBonusKey, welcome);
                PlayerPrefs.Save();
                _shotGlovePin = false;
                _pokerIntroLive = false;
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        IEnumerator ShotPokerGloveAt(int w, int h, string path, string tag)
        {
            if (EditorGameSize != null) EditorGameSize(w, h);
            Screen.SetResolution(w, h, false);
            yield return new WaitForSecondsRealtime(0.4f);
            MuteShotAudio();
            ShowSplash();
            _dailyIntro = false;
            _dailyIntroLive = false;
            _hiveIntro = false;
            _hiveIntroLive = false;
            _adoptLive = false;
            _welcomeOpen = false;
            _welcomeQueued = false;
            _welcomeGlove = false;
            _dailyOpen = false;
            _dailyAskOpen = false;
            yield return WaitStudioCardDown("POKER_GLOVE_HOME " + tag);
            ArmPokerIntro();
            float gloveT0 = Time.realtimeSinceStartup;
            while ((!_pokerIntroLive || !_gloveVis) && Time.realtimeSinceStartup - gloveT0 < 8f)
            {
                MuteShotAudio();
                yield return null;
            }
            _shotGlovePin = true;
            yield return new WaitForSecondsRealtime(0.4f);
            yield return new WaitForEndOfFrame();
            MuteShotAudio();
            SaveFrame(path);
            var box = SplashPokerRect();
            var aim = GloveTargetAt(box, PokerGloveU, PokerGloveV);
            var hand = PokerPointHand(aim, DeviceFit.Ui());
            var plate = _tutorPlate;
            string line = tag + " " + Screen.width + "x" + Screen.height
                + " live " + _pokerIntroLive + " glove " + _gloveVis
                + " fromLeft " + _gloveFromLeft + " ang " + _gloveShownAng.ToString("0")
                + " aim " + aim + " hand " + hand + " plate " + plate
                + " button " + box
                + " clear " + (!plate.Overlaps(hand) && !(plate.yMax > box.yMin && box.yMax > plate.yMin));
            File.AppendAllText("/tmp/b88-poker-glove-shot.txt", line + "\n");
            UnityEngine.Debug.Log("[poker-glove] " + line);
        }
    }
}
#endif

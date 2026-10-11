#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        bool _shot83;

        // Device stills of eased gardens 18-25. Editor capture only.
        public IEnumerator ShotBuild83Gardens()
        {
            if (_shot83) yield break;
            _shot83 = true;
            MuteShotAudio();
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            const string dir = "/Users/zfxgames/wkspaces/birdshot/gb/b83out";
            Directory.CreateDirectory(dir);
            int nextKeep = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int coachKeep = PlayerPrefs.GetInt(CoachKey, 0);
            bool hadCoach = PlayerPrefs.HasKey(CoachKey);
            int leafKeep = PlayerPrefs.GetInt(CoachLeafKey, 0);
            bool hadLeaf = PlayerPrefs.HasKey(CoachLeafKey);
            int hiveKeep = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveGardenKey);
            Screen.SetResolution(1179, 2556, false);
            for (int number = 18; number <= 25; number++)
            {
                PrepGardenShot(number - 1);
                MuteShotAudio();
                yield return new WaitForSecondsRealtime(2.8f);
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                SaveFrame(dir + "/b83-garden-" + number + ".png");
            }
            if (hadNext) PlayerPrefs.SetInt("flockfive.next", nextKeep);
            else PlayerPrefs.DeleteKey("flockfive.next");
            if (hadCoach) PlayerPrefs.SetInt(CoachKey, coachKeep);
            else PlayerPrefs.DeleteKey(CoachKey);
            if (hadLeaf) PlayerPrefs.SetInt(CoachLeafKey, leafKeep);
            else PlayerPrefs.DeleteKey(CoachLeafKey);
            if (hadHive) PlayerPrefs.SetInt(CoachHiveGardenKey, hiveKeep);
            else PlayerPrefs.DeleteKey(CoachHiveGardenKey);
            PlayerPrefs.Save();
            Debug.Log("B83_GARDEN_STILLS_DONE");
            EditorShotLive = false;
            EditorApplication.Exit(0);
        }
    }
}
#endif

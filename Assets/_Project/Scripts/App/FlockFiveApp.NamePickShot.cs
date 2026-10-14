#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Home name picker ("Please give me a name!") on Samsung 20:9, iPhone and iPad.
        public IEnumerator ShotNamePicker()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AdConsent.EditorHoldAtt = false;
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            string dir = File.Exists("/tmp/flock-five-namepick-dir") ? File.ReadAllText("/tmp/flock-five-namepick-dir").Trim() : "/tmp/ff-namepick";
            Directory.CreateDirectory(dir);
            int adopted = PlayerPrefs.GetInt(AvatarAdoptedPref, 0), named = PlayerPrefs.GetInt(AvatarNamedPref, -1);
            bool hadAdopted = PlayerPrefs.HasKey(AvatarAdoptedPref), hadNamed = PlayerPrefs.HasKey(AvatarNamedPref);
            try
            {
                PlayerPrefs.SetInt(AvatarAdoptedPref, 1);
                PlayerPrefs.SetInt(AvatarNamedPref, 0);
                var sizes = new[] { (1080, 2400, "samsung"), (1179, 2556, "iphone"), (1536, 2048, "ipad") };
                foreach (var (w, h, tag) in sizes)
                {
                    if (EditorGameSize != null) EditorGameSize(w, h);
                    yield return new WaitForSecondsRealtime(1.0f);
                    ShowSplash();
                    float t = 0f;
                    while (t < 0.8f)
                    {
                        PoseBuild79(false, false);
                        _avatarRename = false;
                        OpenAvatarRename();
                        MuteShotAudio();
                        t += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    SaveFrame(dir + "/namepicker-" + tag + ".png");
                    File.AppendAllText(dir + "/namepicker-rects.txt", tag + " " + w + "x" + h + " plate=" + _avatarRenameR + "\n");
                }
                File.WriteAllText("/tmp/flock-five-namepick-done", "OK\n");
            }
            finally
            {
                if (hadAdopted) PlayerPrefs.SetInt(AvatarAdoptedPref, adopted); else PlayerPrefs.DeleteKey(AvatarAdoptedPref);
                if (hadNamed) PlayerPrefs.SetInt(AvatarNamedPref, named); else PlayerPrefs.DeleteKey(AvatarNamedPref);
                PlayerPrefs.Save();
                CloseAvatarRename();
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

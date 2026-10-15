#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // b89: first-garden coach captions (pickup, place) and the bee banner,
        // each with its glove, on iPhone, iPad and Android.
        public IEnumerator ShotGloveCaption()
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
            int coach = PlayerPrefs.GetInt(CoachKey, 0);
            int hiveGarden = PlayerPrefs.GetInt(CoachHiveGardenKey, 0);
            const string dir = "/Users/zfxgames/wkspaces/wingout/b89";
            Directory.CreateDirectory(dir);
            const string log = "/tmp/flock-five-b89-glove-log.txt";
            File.WriteAllText(log, "");
            int[][] res = { new[] { 1179, 2556 }, new[] { 1536, 2048 }, new[] { 1080, 2400 } };
            string[] dev = { "iphone", "ipad", "android" };
            try
            {
                for (int d = 0; d < 3; d++)
                {
                    if (EditorGameSize != null) EditorGameSize(res[d][0], res[d][1]);
                    yield return new WaitForSecondsRealtime(1.0f);
                    PlayerPrefs.SetInt(CoachKey, 0);
                    PlayerPrefs.SetInt("flockfive.next", 0);
                    PlayerPrefs.Save();
                    _splash = false;
                    Load(0);
                    GamePause.Reset();
                    Time.timeScale = 1f;
                    float t = 0f;
                    while (t < 3f && !(_coach && _cueHand && _sel < 0 && _cueLine != null)) { t += AdPause.UnscaledDelta; yield return null; }
                    yield return new WaitForSecondsRealtime(1.6f);
                    _coachFade = 1f;
                    yield return GloveCaptionStill(dir, dev[d], "pickup", log);

                    if (_coachFrom >= 0) Select(_coachFrom);
                    t = 0f;
                    while (t < 3f && _cueLine != CaptionLayout.CoachPlaceLine) { t += AdPause.UnscaledDelta; yield return null; }
                    yield return new WaitForSecondsRealtime(1.6f);
                    _coachFade = 1f;
                    yield return GloveCaptionStill(dir, dev[d], "place", log);

                    _coach = false; _cueLine = null; _cueHand = false; _gloveVis = false; _coachFade = 0f;
                    PlayerPrefs.SetInt(CoachHiveGardenKey, 0);
                    PlayerPrefs.Save();
                    PrepGardenShot(0);
                    PlayerPrefs.SetInt(CoachHiveGardenKey, 0);
                    _shotBeeLesson = true;
                    _shotGlovePin = true;
                    yield return new WaitForSecondsRealtime(1.6f);
                    _coachFade = 1f;
                    yield return GloveCaptionStill(dir, dev[d], "bee", log);
                    _shotBeeLesson = false;
                    _shotGlovePin = false;
                }
                File.WriteAllText("/tmp/flock-five-b89-glove-done", "OK\n");
            }
            finally
            {
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt(CoachKey, coach);
                PlayerPrefs.SetInt(CoachHiveGardenKey, hiveGarden);
                PlayerPrefs.Save();
                _shotBeeLesson = false;
                _shotGlovePin = false;
                EditorShotLive = false;
                ShotKeepAwake = false;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        IEnumerator GloveCaptionStill(string dir, string dev, string tag, string log)
        {
            MuteShotAudio();
            // The glove fades in on its own cue: wait (up to 6 s) until it has been
            // drawn at full strength for a few frames before taking the still.
            float t = 0f; int seen = 0;
            while (t < 6f && seen < 6)
            {
                _coachFade = 1f;
                seen = GloveBodyFresh().width >= 2f && GloveBodyAlpha > 0.95f ? seen + 1 : 0;
                t += AdPause.UnscaledDelta;
                yield return null;
            }
            yield return new WaitForEndOfFrame();
            bool hit = CaptionGloveHit >= Time.frameCount - 2;
            var hand = GloveBodyFresh();
            File.AppendAllText(log, dev + " " + tag + " " + Screen.width + "x" + Screen.height
                + " line " + (_cueLine ?? "").Replace("\n", " ") + " plate " + _capTop.Plate + " kind " + _capTop.Kind
                + " glove " + hand + " alpha " + GloveBodyAlpha.ToString("0.00") + " gloveDrawn " + (hand.width >= 2f) + " gloveOverPlate " + hit
                + " banner " + BannerLast + " repaints " + CaptionRepaints + "\n");
            SaveFrame(dir + "/glove-caption-" + tag + "-" + dev + ".png");
        }
    }
}
#endif

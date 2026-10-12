#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Build 83 c2/c3 stills: REWARD popup with 2X + Collect and the no-ad Collect (10g-10i),
        // home caption at the top (11b), and the sparkle curtain frame strip (5/5b).
        // Run on its own host so app-side StopAllCoroutines (sign tuck, splash) cannot end the shot.
        static ShotHost83 _host83;

        // Stills: the shared coach glove (same raster the lessons draw) with its fingertip.
        public static Texture2D ShotGloveTex(out float tipU, out float tipV)
        {
            tipU = _gloveTipU; tipV = _gloveTipV;
            return CoachGloveCurl(0f);
        }
        public IEnumerator ShotBuild83c()
        {
            if (_host83 == null)
            {
                _host83 = new GameObject("ShotHost83").AddComponent<ShotHost83>();
                _host83.StartCoroutine(ShotBuild83cBody());
            }
            yield break;
        }

        IEnumerator ShotBuild83cBody()
        {
            if (_shot83) yield break;
            _shot83 = true;
            MuteShotAudio();
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            const string dir = "/Users/zfxgames/wkspaces/birdshot/gb/b83out";
            Directory.CreateDirectory(dir);
            Screen.SetResolution(1179, 2556, false);
            int streakKeep = PlayerPrefs.GetInt("flockfive.streak", 0);
            yield return new WaitForSecondsRealtime(1.5f);


            Debug.Log("B83C caption start");
            ShowSplash();
            _home = HomeFace.Splash;
            _splash = true;
            _shotBirdHome = true;
            yield return new WaitForSecondsRealtime(1.6f);
            MuteShotAudio();
            yield return new WaitForEndOfFrame();
            SaveFrame(dir + "/b83-home-caption-top.png");
            Debug.Log("B83C caption saved");
            _shotBirdHome = false;
            _adoptLive = false;

            for (int pass = 0; pass < 2; pass++)
            {
                ShotRewardAd = pass == 0 ? 0 : 1;
                PrefGuard.SetInt("flockfive.streak", 5);
                PlayerPrefs.SetInt("flockfive.instage", 0);
                Debug.Log("B83C reward " + pass + " splash");
                ShowSplash();
                Purse.CueWin(5, Purse.StagePay * 5 * Mathf.Max(1, Purse.LoginMul));
                ArmStreakSlide();
                RewardPays(out _, out _, out int toPay);
                _rewardRepeatAmt = toPay;
                Debug.Log("B83C reward " + pass + " armed " + toPay);
                float guard = 0f;
                int fr = 0;
                while (!RewardChoiceUp && guard < 10f)
                {
                    guard += Time.unscaledDeltaTime;
                    if ((++fr % 30) == 0) Debug.Log("B83C wait f" + fr + " g" + guard.ToString("0.0") + " slide " + _streakSlide.ToString("0.00"));
                    MuteShotAudio();
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.5f);
                yield return new WaitForEndOfFrame();
                SaveFrame(dir + (pass == 1 ? "/b83-reward-2x.png" : "/b83-reward-noad-collect.png"));
                Debug.Log("B83C reward pass " + pass + " held=" + RewardChoiceUp + " after " + guard.ToString("0.0") + "s");
                _rewardChoice = 2;
                yield return new WaitForSecondsRealtime(1.2f);
                DismissStreakSign();
            }
            ShotRewardAd = -1;
            PrepGardenShot(4);
            yield return new WaitForSecondsRealtime(2.5f);
            float W = Screen.width, H = Screen.height;
            LevelCurtain.Reset();
            LevelCurtain.Aim(new Rect(W * 0.5f - W * 0.17f, H * 0.80f, W * 0.34f, W * 0.34f));
            LevelCurtain.Arm(0f);
            LevelCurtain.BeginLoad();
            LevelCurtain.MarkReady();
            float[] ages = { 0.10f, 0.34f, 0.60f, 0.90f };
            string[] names = { "burst", "apex", "fall", "reveal" };
            for (int i = 0; i < ages.Length; i++)
            {
                Debug.Log("B83C curtain " + i);
                LevelCurtain.PreviewNow = ages[i];
                yield return null;
                yield return new WaitForEndOfFrame();
                SaveFrame(dir + "/b83-curtain-" + i + "-" + names[i] + ".png");
            }
            LevelCurtain.Reset();
            Debug.Log("B83C curtain done");
            PrefGuard.SetInt("flockfive.streak", streakKeep);
            PlayerPrefs.Save();
            Debug.Log("B83C_STILLS_DONE");
            EditorShotLive = false;
            EditorApplication.Exit(0);
        }
    }

    public sealed class ShotHost83 : MonoBehaviour { }
}
#endif

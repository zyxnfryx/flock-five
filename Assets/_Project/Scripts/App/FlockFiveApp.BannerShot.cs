#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Home adopt greet on iPhone and iPad. The band should hide the No Ads ribbon.
        public IEnumerator ShotBuild88Banner()
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
            const string dir = "/Users/zfxgames/wkspaces/wingout";
            Directory.CreateDirectory(dir);
            File.WriteAllText("/tmp/banner-shot.txt", "");
            try
            {
                PlayerPrefs.SetInt("flockfive.next", next < 1 ? 1 : next);
                PlayerPrefs.SetInt("flockfive.coach.daily", 0);
                PlayerPrefs.SetInt("flockfive.noads", 0);
                PlayerPrefs.Save();
                SetNoAdsOwned(false);

                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                yield return new WaitForSecondsRealtime(0.9f);
                MuteShotAudio();
                ShowSplash();
                _shotBirdHome = true;
                yield return new WaitForSecondsRealtime(1.7f);
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                LogBannerShot("iphone");
                SaveFrame(dir + "/b88-adopt-banner-iphone.png");

                if (EditorGameSize != null) EditorGameSize(1536, 2048);
                yield return new WaitForSecondsRealtime(1.3f);
                MuteShotAudio();
                _shotBirdHome = true;
                yield return new WaitForSecondsRealtime(1.3f);
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                LogBannerShot("ipad");
                SaveFrame(dir + "/b88-adopt-banner-ipad.png");
                File.WriteAllText("/tmp/flock-five-b88-banner-done", "OK\n");
            }
            finally
            {
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt("flockfive.coach.daily", daily);
                PlayerPrefs.SetInt("flockfive.noads", noads);
                PlayerPrefs.Save();
                SetNoAdsOwned(noads == 1);
                _shotBirdHome = false;
                _adoptLive = false;
                EditorShotLive = false;
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        void LogBannerShot(string tag)
        {
            var safe = Screen.safeArea;
            var badge = AdoptVipBadgeRect(Screen.width, Screen.height, safe);
            var live = SplashNoAdsRect();
            var ribbon = live.width > 2f ? SplashNoAdsRibbon(live) : default;
            bool covers = _bannerLast.xMin <= badge.xMin + 0.5f
                && _bannerLast.yMin <= badge.yMin + 0.5f
                && _bannerLast.xMax >= badge.xMax - 0.5f
                && _bannerLast.yMax >= badge.yMax - 0.5f;
            string line = tag + " " + Screen.width + "x" + Screen.height
                + " band " + _bannerLast + " badge " + badge
                + " live " + live + " ribbon " + ribbon
                + " covers " + covers + " owned " + NoAds.Owned;
            File.AppendAllText("/tmp/banner-shot.txt", line + "\n");
            Debug.Log("[banner-shot] " + line);
        }

        static void SetNoAdsOwned(bool owned)
        {
            var prop = typeof(NoAds).GetProperty("Owned", BindingFlags.Public | BindingFlags.Static);
            var set = prop != null ? prop.GetSetMethod(true) : null;
            if (set != null) set.Invoke(null, new object[] { owned });
        }
    }
}
#endif

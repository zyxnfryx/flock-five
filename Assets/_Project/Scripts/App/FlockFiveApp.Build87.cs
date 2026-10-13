#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Build 87: iPhone / iPad side-by-side stills (home, garden levels).
        static ShotHost83 _host87;
        public static System.Action<int, int> EditorGameSize;
        public static string Shot87Tag = "";

        public IEnumerator ShotBuild87()
        {
            if (_host87 == null)
            {
                _host87 = new GameObject("ShotHost87").AddComponent<ShotHost83>();
                _host87.StartCoroutine(ShotBuild87Body());
            }
            yield break;
        }

        static ShotHost83 _host87v;
        public IEnumerator ShotBuild87Video()
        {
            if (_host87v == null)
            {
                _host87v = new GameObject("ShotHost87v").AddComponent<ShotHost83>();
                _host87v.StartCoroutine(ShotBuild87VideoBody());
            }
            yield break;
        }

        static void SaveHalf(string path)
        {
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) return;
            int w = tex.width / 2, h = tex.height / 2;
            var rt = RenderTexture.GetTemporary(w, h, 0);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var o = new Texture2D(w, h, TextureFormat.RGB24, false);
            o.ReadPixels(new Rect(0, 0, w, h), 0, 0); o.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, o.EncodeToJPG(90));
            Object.Destroy(o); Object.Destroy(tex);
        }

        // The fight's peck feedback, fired on a beat so before/after both show hits.
        IEnumerator PestTaps87()
        {
            for (int i = 0; i < 7; i++) { yield return new WaitForSeconds(0.65f); CamShake.PestTap(); }
        }

        IEnumerator Rec87(string dir, float secs)
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            // Fixed 30 fps game clock while recording, so slow PNG saves do not drop motion.
            Time.captureDeltaTime = 1f / 30f;
            int frames = Mathf.RoundToInt(secs * 30f);
            float u0 = Time.unscaledTime;
            for (int f = 0; f < frames; f++)
            {
                MuteShotAudio();
                yield return new WaitForEndOfFrame();
                SaveHalf(dir + "/f" + f.ToString("0000") + ".jpg");
            }
            Time.captureDeltaTime = 0f;
            File.WriteAllText(dir + "/fps.txt", "30 unscaledSpan=" + (Time.unscaledTime - u0).ToString("0.00"));
        }

        IEnumerator ShotBuild87VideoBody()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            string root = "/Users/zfxgames/wkspaces/birdshot/gb/b87/clips";
            Directory.CreateDirectory(root);
            int nextKeep = PlayerPrefs.GetInt("flockfive.next", 0);
            if (EditorGameSize != null) EditorGameSize(1536, 2048);
            yield return new WaitForSecondsRealtime(1.2f);
            // Foliage sway per season, build-86 mask + soft blend vs b87 cutouts.
            {
                int animKeep = PlayerPrefs.GetInt(BackgroundMotion.PrefKey, 1); bool animHad = PlayerPrefs.HasKey(BackgroundMotion.PrefKey);
                PlayerPrefs.SetInt(BackgroundMotion.PrefKey, 1);
                string[] names = { "home", "summer", "desert", "fall", "winter", "spring" };
                GardenScene[] sc = { GardenScene.Summer, GardenScene.Summer, GardenScene.Desert, GardenScene.Fall, GardenScene.Winter, GardenScene.Spring };
                string[] old = { "garden", "garden", "oasis", "fall", null, "spring" };
                for (int k = 0; k < names.Length; k++)
                {
                    int lvl = 0;
                    for (int L = 0; L < 400; L++) if (GardenSeason.ForLevel(L + 1) == sc[k]) { lvl = L + 2; break; } // past the season reveal
                    for (int pass = 0; pass < 2; pass++)
                    {
                        Texture2D om = null;
                        if (pass == 0)
                        {
                            if (old[k] != null && File.Exists("/tmp/sway86/bg_" + old[k] + "_sway.png"))
                            { om = new Texture2D(2, 2); om.LoadImage(File.ReadAllBytes("/tmp/sway86/bg_" + old[k] + "_sway.png")); }
                            FoliageSway.TestMask = om != null ? (Texture)om : Texture2D.whiteTexture;
                            FoliageSway.TestLegacy = true;
                        }
                        if (k == 0) ShowSplash(); else PrepGardenShot(lvl);
                        yield return new WaitForSecondsRealtime(3f);
                        yield return Rec87(root + "/sway-" + names[k] + "-" + (pass == 0 ? "before" : "after"), 4f);
                        FoliageSway.TestMask = null; FoliageSway.TestLegacy = false;
                    }
                }
                if (animHad) PlayerPrefs.SetInt(BackgroundMotion.PrefKey, animKeep); else PlayerPrefs.DeleteKey(BackgroundMotion.PrefKey);
            }
            bool swayOnly = File.Exists("/tmp/flock-five-b87v-swayonly");
            // Sparrow fight at iPad aspect: old punch vs the new crisp tap.
            for (int pass = 0; pass < (swayOnly ? 0 : 2); pass++)
            {
                CamShake.LegacyPestPunch = pass == 0;
                PrepGardenShot(6);
                yield return new WaitForSecondsRealtime(1.5f);
                if (_garden.Feeders != null && _garden.Root != null)
                    StartCoroutine(SparrowView.Visit(_garden.Feeders, _garden.Root));
                StartCoroutine(PestTaps87());
                yield return Rec87(root + "/sparrow-" + (pass == 0 ? "before" : "after"), 5f);
            }
            CamShake.LegacyPestPunch = false;
            // Streak popup: tap Collect, popup tucks as the coins fly (before = old hold).
            for (int pass = 0; pass < (swayOnly ? 0 : 2); pass++)
            {
                StreakTuckOnFly = pass == 1;
                int streakKeep = PlayerPrefs.GetInt("flockfive.streak", 0);
                ShotRewardAd = 1;
                PrefGuard.SetInt("flockfive.streak", 2);
                PlayerPrefs.SetInt("flockfive.instage", 0);
                ShowSplash();
                yield return new WaitForSecondsRealtime(0.6f);
                EditorShotLive = false;
                Purse.AwardClear();
                ArmStreakSlide();
                float g = 0f;
                while (!RewardChoiceUp && g < 10f) { g += Time.unscaledDeltaTime; MuteShotAudio(); yield return null; }
                if (pass == 1) yield return Rec87(root + "/watchad", 3f);
                _rewardChoice = 2;
                yield return Rec87(root + "/streak-" + (pass == 0 ? "before" : "after"), 3.5f);
                DismissStreakSign();
                EditorShotLive = true;
                ShotRewardAd = -1;
                PrefGuard.SetInt("flockfive.streak", streakKeep);
            }
            StreakTuckOnFly = true;
            PlayerPrefs.SetInt("flockfive.next", nextKeep);
            PlayerPrefs.Save();
            File.WriteAllText("/tmp/flock-five-b87v.txt", "B87V_DONE\n");
            EditorShotLive = false;
            ShotKeepAwake = false;
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        IEnumerator ShotBuild87Body()
        {
            ShotKeepAwake = true;
            MuteShotAudio();
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            string dir = "/Users/zfxgames/wkspaces/birdshot/gb/b87/stills" + Shot87Tag;
            Directory.CreateDirectory(dir);
            int nextKeep = PlayerPrefs.GetInt("flockfive.next", 0);
            int[][] res = { new[] { 1179, 2556 }, new[] { 1536, 2048 } };
            string[] dev = { "iphone", "ipad" };
            for (int d = 0; d < 2; d++)
            {
                if (EditorGameSize != null) EditorGameSize(res[d][0], res[d][1]); else Screen.SetResolution(res[d][0], res[d][1], false);
                yield return new WaitForSecondsRealtime(1.2f);
                ShowSplash();
                MuteShotAudio();
                yield return new WaitForSecondsRealtime(2.5f);
                yield return new WaitForEndOfFrame();
                SaveFrame(dir + "/home-" + dev[d] + ".png");
                // Reward popup with the 2X Watch Ad button.
                {
                    int streakKeep = PlayerPrefs.GetInt("flockfive.streak", 0);
                    ShotRewardAd = 1;
                    PrefGuard.SetInt("flockfive.streak", 2);
                    PlayerPrefs.SetInt("flockfive.instage", 0);
                    ShowSplash();
                    yield return new WaitForSecondsRealtime(0.6f);
                    EditorShotLive = false;
                    Purse.AwardClear();
                    ArmStreakSlide();
                    float g = 0f;
                    while (!RewardChoiceUp && g < 10f) { g += Time.unscaledDeltaTime; MuteShotAudio(); yield return null; }
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return new WaitForEndOfFrame();
                    SaveFrame(dir + "/reward-" + dev[d] + ".png");
                    _rewardChoice = 2;
                    yield return new WaitForSecondsRealtime(3f);
                    DismissStreakSign();
                    EditorShotLive = true;
                    ShotRewardAd = -1;
                    PrefGuard.SetInt("flockfive.streak", streakKeep);
                }
                foreach (int lv in new[] { 4, 19, 23 })
                {
                    PrepGardenShot(lv);
                    MuteShotAudio();
                    yield return new WaitForSecondsRealtime(2.8f);
                    yield return new WaitForEndOfFrame();
                    SaveFrame(dir + "/garden" + (lv + 1) + "-" + dev[d] + ".png");
                    if (lv == 19)
                    {
                        _hiveInspect = 0; _hiveInspectT = 1f; _hiveInspectClosing = false;
                        yield return new WaitForSecondsRealtime(0.8f);
                        yield return new WaitForEndOfFrame();
                        SaveFrame(dir + "/beecard-" + dev[d] + ".png");
                        _hiveInspect = -1;
                    }
                }
                // Leaves lesson on a fresh stage: first level that has leaves.
                for (int lv = 1; lv < 40; lv++)
                {
                    PrepGardenShot(lv);
                    yield return new WaitForSecondsRealtime(0.3f);
                    if (FirstLeaf() < 0) continue;
                    PlayerPrefs.SetInt(CoachLeafKey, 0);
                    ArmLeafIntro();
                    float w = 0f;
                    while (w < 3.5f) { w += Time.unscaledDeltaTime; MuteShotAudio(); yield return null; }
                    yield return new WaitForEndOfFrame();
                    SaveFrame(dir + "/leaves" + (lv + 1) + "-" + dev[d] + (_gloveVis ? "-glove" : "-noglove") + ".png");
                    DismissLeafIntro();
                    break;
                }
            }
            PlayerPrefs.SetInt("flockfive.next", nextKeep);
            PlayerPrefs.Save();
            File.WriteAllText("/tmp/flock-five-b87.txt", "B87_DONE\n");
            EditorShotLive = false;
            ShotKeepAwake = false;
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }
}
#endif

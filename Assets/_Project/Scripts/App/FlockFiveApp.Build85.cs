#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Build 85: REWARD popup holds. Records the no-ad (single green Collect) flow and the
        // 2X layout, and checks the balance does not move before the tap.
        static ShotHost83 _host85;
        static bool _shot85;
        const string Res85 = "/tmp/flock-five-b85-reward.txt";

        public IEnumerator ShotBuild85Reward()
        {
            if (_host85 == null)
            {
                _host85 = new GameObject("ShotHost85").AddComponent<ShotHost83>();
                _host85.StartCoroutine(ShotBuild85RewardBody());
            }
            yield break;
        }

        IEnumerator ShotBuild85RewardBody()
        {
            if (_shot85) yield break;
            _shot85 = true;
            var sb = new StringBuilder();
            int fail = 0;
            void Check(string n, bool ok, string d) { if (!ok) fail++; sb.AppendLine((ok ? "PASS  " : "FAIL  ") + n + "  " + d); File.WriteAllText(Res85, sb.ToString()); Debug.Log("[b85reward] " + (ok ? "PASS " : "FAIL ") + n + " " + d); }
            ShotKeepAwake = true;
            MuteShotAudio();
            EditorShotLive = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            const string dir = "/Users/zfxgames/wkspaces/birdshot/gb/b85/reward";
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            Screen.SetResolution(1179, 2556, false);
            int streakKeep = PlayerPrefs.GetInt("flockfive.streak", 0);
            yield return new WaitForSecondsRealtime(1.5f);
            int frame = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                ShotRewardAd = pass == 0 ? 0 : 1;
                PrefGuard.SetInt("flockfive.streak", 2);
                PlayerPrefs.SetInt("flockfive.instage", 0);
                ShowSplash();
                yield return new WaitForSecondsRealtime(0.6f);
                // Real owed clear pay, live pay path (EditorShotLive off): the hold is the only thing stopping it.
                EditorShotLive = false;
                Purse.AwardClear();
                int coins0 = Purse.Coins, shown0 = CoinsShown();
                ArmStreakSlide();
                float g = 0f;
                int maxMoved = 0; bool flewEarly = false;
                while (!RewardChoiceUp && g < 10f)
                {
                    g += Time.unscaledDeltaTime;
                    maxMoved = Mathf.Max(maxMoved, Mathf.Abs(Purse.Coins - coins0), Mathf.Abs(CoinsShown() - shown0));
                    flewEarly |= _flies.Count > 0;
                    MuteShotAudio();
                    yield return new WaitForEndOfFrame();
                    if (pass == 0) SaveFrame(dir + "/f" + (frame++).ToString("0000") + ".png");
                }
                // Hold 1.5s with the buttons up: nothing may move.
                float h = 0f;
                while (h < 1.5f)
                {
                    h += Time.unscaledDeltaTime;
                    maxMoved = Mathf.Max(maxMoved, Mathf.Abs(Purse.Coins - coins0), Mathf.Abs(CoinsShown() - shown0));
                    flewEarly |= _flies.Count > 0;
                    yield return new WaitForEndOfFrame();
                    if (pass == 0) SaveFrame(dir + "/f" + (frame++).ToString("0000") + ".png");
                }
                string tag = pass == 0 ? "noad" : "2x";
                Check("held-" + tag, RewardChoiceUp && g < 10f, "buttons up after " + g.ToString("0.0") + "s, hasAd=" + _rewardHasAd);
                Check("balance-unchanged-before-tap-" + tag, maxMoved == 0 && !flewEarly, "coins " + coins0 + " moved " + maxMoved + " flew " + flewEarly);
                if (pass == 1) SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/reward-2x-held.png");
                _rewardChoice = 2; // tap Collect
                float p = 0f; bool flew = false;
                while (p < 3.0f)
                {
                    p += Time.unscaledDeltaTime;
                    flew |= _flies.Count > 0;
                    yield return new WaitForEndOfFrame();
                    if (pass == 0) SaveFrame(dir + "/f" + (frame++).ToString("0000") + ".png");
                }
                Check("pays-after-tap-" + tag, flew && Purse.Coins > coins0, "coins " + coins0 + " -> " + Purse.Coins);
                DismissStreakSign();
                EditorShotLive = true;
                yield return new WaitForSecondsRealtime(0.5f);
            }
            ShotRewardAd = -1;
            PrefGuard.SetInt("flockfive.streak", streakKeep);
            sb.AppendLine(fail == 0 ? "B85REWARD_OK" : "B85REWARD_FAIL " + fail);
            File.WriteAllText(Res85, sb.ToString());
            EditorShotLive = false;
            _shot85 = false;
            ShotKeepAwake = false;
            if (File.Exists("/tmp/flock-five-b85-exit")) { UnityEditor.EditorApplication.isPlaying = false; UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.Exit(0); }
            Debug.Log("[b85reward] DONE fail=" + fail);
        }
    }
}
#endif

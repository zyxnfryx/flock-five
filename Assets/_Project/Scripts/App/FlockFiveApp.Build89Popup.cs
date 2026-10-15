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
        // Build 89: post-stage order in play mode. Rewards popup alone (a Go VIP open is
        // tried on top and must wait), coins land, home is clean, then Go VIP alone.
        static ShotHost83 _host89p;
        const string Res89p = "/tmp/flock-five-b89-popup.txt";
        public const string Dir89p = "/Users/zfxgames/wkspaces/b89out/popup-exclusive";

        public IEnumerator ShotPopupExclusive()
        {
            if (_host89p == null)
            {
                _host89p = new GameObject("ShotHost89p").AddComponent<ShotHost83>();
                _host89p.StartCoroutine(ShotPopupExclusiveBody());
            }
            yield break;
        }

        IEnumerator ShotPopupExclusiveBody()
        {
            var sb = new StringBuilder();
            int fail = 0;
            void Check(string n, bool ok, string d) { if (!ok) fail++; sb.AppendLine((ok ? "PASS  " : "FAIL  ") + n + "  " + d); File.WriteAllText(Res89p, sb.ToString()); Debug.Log("[b89popup] " + (ok ? "PASS " : "FAIL ") + n + " " + d); }
            ShotKeepAwake = true;
            MuteShotAudio();
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            Directory.CreateDirectory(Dir89p);
            int[][] res = { new[] { 1179, 2556 }, new[] { 2048, 2732 }, new[] { 1080, 2400 } };
            string[] dev = { "iphone", "ipad", "android" };
            int streakKeep = PlayerPrefs.GetInt("flockfive.streak", 0);
            for (int d = 0; d < 3; d++)
            {
                EditorShotLive = true;
                if (EditorGameSize != null) EditorGameSize(res[d][0], res[d][1]); else Screen.SetResolution(res[d][0], res[d][1], false);
                yield return new WaitForSecondsRealtime(1.2f);
                ShotRewardAd = 1;
                PrefGuard.SetInt("flockfive.streak", 2);
                PlayerPrefs.SetInt("flockfive.instage", 0);
                ShowSplash();
                yield return new WaitForSecondsRealtime(0.6f);
                EditorShotLive = false;
                Purse.AwardClear();
                // A stage clear queued the post-stage steps.
                _postStagePending = true;
                _rewardTookDouble = false;
                ArmStreakSlide();
                float g = 0f;
                while (!RewardChoiceUp && g < 10f) { g += Time.unscaledDeltaTime; MuteShotAudio(); yield return null; }
                // The bug: Go VIP opening on top. Both the automatic and the tap path must wait.
                VipOffer.Show(false);
                VipOffer.Show(true);
                yield return null;
                Check("vip-waits-on-reward-" + dev[d], !VipOffer.IsOpen && PopupQueue.VipWanted, "open=" + VipOffer.IsOpen + " queued=" + PopupQueue.VipWanted);
                Check("no-ad-over-reward-" + dev[d], !_postStageRunning, "running=" + _postStageRunning);
                yield return new WaitForSecondsRealtime(0.5f);
                yield return new WaitForEndOfFrame();
                SaveFrame(Dir89p + "/1-streak-" + dev[d] + ".png");
                _rewardChoice = 2; // Collect
                float t = 0f;
                bool vipDuringCoins = false;
                while ((_streakSlide >= 0f || RewardPayBusy()) && t < 8f)
                {
                    if (t > 4f && _streakSlide >= 0f) DismissStreakSign();
                    vipDuringCoins |= VipOffer.IsOpen;
                    t += Time.unscaledDeltaTime; MuteShotAudio(); yield return null;
                }
                if (_streakSlide >= 0f) DismissStreakSign();
                Check("vip-not-during-coins-" + dev[d], !vipDuringCoins, "");
                // Interstitial step (no ad SDK in the editor; it logs its turn).
                float w = 0f;
                while ((_postStagePending || _postStageRunning) && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
                Check("post-stage-ran-after-reward-" + dev[d], !_postStagePending && !_postStageRunning, "");
                Check("no-stray-fx-on-home-" + dev[d], SparkleFx.LiveBits() == 0, "bits=" + SparkleFx.LiveBits());
                yield return new WaitForEndOfFrame();
                SaveFrame(Dir89p + "/2-home-" + dev[d] + ".png");
                PopupQueue.RequestVip();
                float v = 0f;
                while (!VipOffer.IsOpen && v < 4f) { v += Time.unscaledDeltaTime; yield return null; }
                Check("vip-opens-alone-" + dev[d], VipOffer.IsOpen && _streakSlide < 0f && OpenModal() == ModalBlock.Id.Vip, "modal=" + OpenModal());
                yield return new WaitForSecondsRealtime(3.6f);
                yield return new WaitForEndOfFrame();
                SaveFrame(Dir89p + "/3-vip-" + dev[d] + ".png");
                VipOffer.Close();
                PopupQueue.DropVip();
                yield return new WaitForSecondsRealtime(0.5f);
            }
            ShotRewardAd = -1;
            PrefGuard.SetInt("flockfive.streak", streakKeep);
            sb.AppendLine(fail == 0 ? "B89POPUP_OK" : "B89POPUP_FAIL " + fail);
            File.WriteAllText(Res89p, sb.ToString());
            EditorShotLive = false;
            ShotKeepAwake = false;
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }
}
#endif

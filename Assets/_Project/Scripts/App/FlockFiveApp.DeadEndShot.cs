#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        public const string DeadEndShotCmd = "/tmp/flock-five-b89-deadend";
        const string DeadEndShotDir = "/Users/zfxgames/wkspaces/birdshot/b89de";

        // Build 89 still: Garden 17, one flock hopped there, back, and there
        // again. The second take-back brings the gift glove and glow back on
        // the ad branch. iPhone, iPad, then Android.
        public IEnumerator ShotDeadEndNudge()
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
            int noads = PlayerPrefs.GetInt("flockfive.noads", 0);
            Directory.CreateDirectory(DeadEndShotDir);
            File.WriteAllText(DeadEndShotDir + "/shot.txt", "");
            var sizes = new[] { new[] { 1179, 2556 }, new[] { 1536, 2048 }, new[] { 1080, 2400 } };
            var names = new[] { "iphone", "ipad", "android" };
            try
            {
                PlayerPrefs.SetInt("flockfive.noads", 0);
                SetNoAdsOwned(false);
                for (int s = 0; s < sizes.Length; s++)
                {
                    if (EditorGameSize != null) EditorGameSize(sizes[s][0], sizes[s][1]);
                    yield return new WaitForSecondsRealtime(0.8f);
                    PrepGardenShot(16);
                    GamePause.Reset();
                    Time.timeScale = 1f;
                    yield return new WaitForSecondsRealtime(1.4f);
                    int f = -1, t = -1;
                    for (int a = 0; a < _board.Branches.Count && f < 0; a++)
                    {
                        for (int b = 0; b < _board.Branches.Count; b++)
                        {
                            if (!_board.CanMove(a, b, out int run)) continue;
                            var c = _board.Clone();
                            c.TryMove(a, b, out _);
                            if (c.JustUnveiled || c.FindCollect() >= 0) continue;
                            if (!c.CanMove(b, a, out int back) || back != run) continue;
                            f = a;
                            t = b;
                            break;
                        }
                    }
                    for (int hop = 0; hop < 3 && f >= 0; hop++)
                    {
                        _pestsArmed = false;
                        if (hop % 2 == 0) yield return DoMove(f, t);
                        else yield return DoMove(t, f);
                        _pestsArmed = false;
                        yield return new WaitForSecondsRealtime(0.25f);
                    }
                    float held = 0f;
                    while (held < 1.1f)
                    {
                        held += AdPause.UnscaledDelta;
                        yield return null;
                    }
                    MuteShotAudio();
                    yield return new WaitForEndOfFrame();
                    string line = names[s] + " " + Screen.width + "x" + Screen.height + " hop " + f + "<->" + t
                        + " rev " + _hopTrail.Reversals + " gift " + GiftBranch()
                        + " giftLeft " + (_coachGiftUntil - PlayClock.Now).ToString("0.00")
                        + " cue " + (_cueLine ?? "") + " glove " + _gloveVis + " fade " + _coachFade.ToString("0.00");
                    File.AppendAllText(DeadEndShotDir + "/shot.txt", line + "\n");
                    Debug.Log("[deadend-shot] " + line);
                    SaveFrame(DeadEndShotDir + "/deadend-nudge-" + names[s] + ".png");
                }
                File.WriteAllText(DeadEndShotCmd + "-done", "OK\n");
            }
            finally
            {
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt("flockfive.noads", noads);
                PlayerPrefs.Save();
                SetNoAdsOwned(noads == 1);
                EditorShotLive = false;
                ShotKeepAwake = false;
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Garden with a perched sparrow, then the restart parade, then the
        // settled garden. The still is the garden after that restart.
        public IEnumerator ShotPestReset()
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
            Time.timeScale = 1f;

            const string dir = "/Users/zfxgames/wkspaces/wingout";
            const string path = dir + "/b88-pest-reset.png";
            Directory.CreateDirectory(dir);
            bool had = false;
            try
            {
                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                Purse.SetStreak(0);
                _restartAsk = RestartAsk.None;
                PrepGardenShot(6);
                // Patrol must not land a second visitor while this still settles.
                PestSchedule.SetTutorialPause(true);
                float warm = 0f;
                while (warm < 1.6f)
                {
                    MuteShotAudio();
                    warm += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (_garden.Feeders != null && _garden.Root != null && SparrowView.Live == null)
                    StartCoroutine(SparrowView.Visit(_garden.Feeders, _garden.Root));
                float arrive = 0f;
                while (arrive < 4f)
                {
                    var live = SparrowView.Live;
                    if (live != null && live.Settled && live.IsBlocking) break;
                    arrive += Time.unscaledDeltaTime;
                    MuteShotAudio();
                    yield return null;
                }
                had = SparrowView.Live != null && SparrowView.Live.Settled;
                Debug.Log("pest-reset sparrow before " + had + " wait " + arrive.ToString("0.00"));
                Purse.SetStreak(0);
                _restartAsk = RestartAsk.None;
                yield return SnapRound();
                float settle = 0f;
                while (settle < 0.45f)
                {
                    MuteShotAudio();
                    settle += Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                MuteShotAudio();
                bool gone = SparrowView.Live == null && HawkView.Live == null;
                Debug.Log("pest-reset after sparrow " + (SparrowView.Live == null) + " hawk " + (HawkView.Live == null));
                SaveFrame(path);
                File.WriteAllText("/tmp/flock-five-wt-b88-pest-done",
                    (had && gone ? "OK" : "FAIL") + " had=" + had + " gone=" + gone + "\n");
            }
            finally
            {
                EditorShotLive = false;
                ShotKeepAwake = false;
                AudioListener.volume = 0f;
                AudioListener.pause = true;
                try { File.Delete("/tmp/flock-five-wt-b88-pest-arm"); } catch { }
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // b89 wheel Collect sprinkle timing: spin lands, Collect (same call as the button),
        // then frames at a fixed 60 until the card has eased out. Logs frame numbers for the
        // tap, first and last coin landing, and the card close. Editor capture only.
        public IEnumerator ShotWheelCoinSpeed()
        {
            string outDir = File.Exists("/tmp/flock-five-b89-coinspeed-arg")
                ? File.ReadAllText("/tmp/flock-five-b89-coinspeed-arg").Trim()
                : "/Users/zfxgames/wkspaces/b89coin/run";
            ShotKeepAwake = true;
            MuteShotAudio();
            GamePause.Reset();
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            EditorApplication.isPaused = false;
            Application.runInBackground = true;
            int next = PlayerPrefs.GetInt("flockfive.next", 0);
            int daily = PlayerPrefs.GetInt(CoachDailyKey, 0);
            int hive = PlayerPrefs.GetInt(CoachHiveKey, 0);
            int poker = PlayerPrefs.GetInt(CoachPokerKey, 0);
            int welcome = PlayerPrefs.GetInt(WelcomeBonusKey, 0);
            string frames = outDir + "/frames";
            var log = new System.Text.StringBuilder();
            try
            {
                if (Directory.Exists(frames)) Directory.Delete(frames, true);
                Directory.CreateDirectory(frames);
                PlayerPrefs.SetInt("flockfive.next", 2);
                PlayerPrefs.SetInt(CoachDailyKey, 1);
                PlayerPrefs.SetInt(CoachHiveKey, 1);
                PlayerPrefs.SetInt(CoachPokerKey, 1);
                PlayerPrefs.SetInt(WelcomeBonusKey, 1);
                PlayerPrefs.Save();
                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                Screen.SetResolution(1179, 2556, false);
                yield return new WaitForSecondsRealtime(0.4f);
                MuteShotAudio();
                DailyBonus.TestBegin(20261013, 0, 2);
                ShowSplash();
                _dailyIntro = false; _hiveIntro = false; _pokerIntro = false;
                _adoptLive = false; _welcomeOpen = false; _welcomeQueued = false; _welcomeGlove = false;
                _dailyAskOpen = false; _dailyOpen = false; _dailyShotQuiet = false;
                yield return WaitStudioCardDown("B89_COIN_HOME");
                _dailyOpen = true;
                _dailyPopAt = AdPause.Unscaled;
                _dailyPhase = DailyWheelPhase.Ready;
                StartDailySpin();
                _dailyTutorSpin = false;
                _dailyClaimFollowUp = false;
                float spinT0 = Time.realtimeSinceStartup;
                while (_dailyPhase != DailyWheelPhase.Landed && Time.realtimeSinceStartup - spinT0 < 12f)
                {
                    MuteShotAudio();
                    yield return null;
                }
                if (WarmCoverLive) yield return WaitStudioCardDown("B89_COIN_STILL");
                Time.captureDeltaTime = 1f / 60f;
                int n = 0, tail = 0, w = 0, h = 0;
                int tapAt = -1, firstLand = -1, lastLand = -1, closeAt = -1, sprites = 0, coins = 0;
                int prevFlies = 0;
                while (n < 900 && tail < 12)
                {
                    if (n == 20 && tapAt < 0)
                    {
                        int before = Purse.Coins;
                        ResolveDailyGrant(false); // the Collect button's own call
                        tapAt = n;
                        sprites = _flies.Count;
                        coins = Purse.Coins - before;
                        prevFlies = sprites;
                    }
                    yield return null;
                    MuteShotAudio();
                    yield return new WaitForEndOfFrame();
                    if (tapAt >= 0 && lastLand < 0)
                    {
                        if (_flies.Count < prevFlies && firstLand < 0) firstLand = n;
                        if (_flies.Count == 0 && _coinInFlight == 0) lastLand = n;
                        prevFlies = _flies.Count;
                    }
                    if (tapAt >= 0 && !_dailyOpen && closeAt < 0) closeAt = n;
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    if (tex != null)
                    {
                        w = tex.width; h = tex.height;
                        File.WriteAllBytes(frames + "/f" + n.ToString("0000") + ".jpg", tex.EncodeToJPG(80));
                        Object.Destroy(tex);
                    }
                    n++;
                    if (tapAt >= 0 && !_dailyOpen) tail++;
                }
                Time.captureDeltaTime = 0f;
                string Sec(int f) => f < 0 ? "n/a" : ((f - tapAt) / 60f).ToString("0.000") + "s (" + (f - tapAt) + "f)";
                log.AppendLine("frames " + n + " " + w + "x" + h + " @60");
                log.AppendLine("collect_tap_frame " + tapAt + " coins " + coins + " sprites " + sprites);
                log.AppendLine("first_coin_lands " + Sec(firstLand));
                log.AppendLine("last_coin_lands " + Sec(lastLand));
                log.AppendLine("card_closed " + Sec(closeAt));
                File.WriteAllText(outDir + "/timing.txt", log.ToString());
                string ff = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
                var psi = new ProcessStartInfo
                {
                    FileName = ff,
                    Arguments = "-y -framerate 60 -i \"" + frames + "/f%04d.jpg\" -vf scale=590:-2 -c:v libx264 -pix_fmt yuv420p -r 60 \"" + outDir + "/clip.mp4\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi)) { if (proc != null) proc.WaitForExit(120000); }
                UnityEngine.Debug.Log("B89_COIN_SPEED " + log.ToString().Replace('\n', ' '));
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                DailyBonus.TestRelease();
                PlayerPrefs.SetInt("flockfive.next", next);
                PlayerPrefs.SetInt(CoachDailyKey, daily);
                PlayerPrefs.SetInt(CoachHiveKey, hive);
                PlayerPrefs.SetInt(CoachPokerKey, poker);
                PlayerPrefs.SetInt(WelcomeBonusKey, welcome);
                PlayerPrefs.Save();
                ShotKeepAwake = false;
                File.WriteAllText(outDir + "/done", log.Length > 0 ? "OK\n" : "FAIL\n");
            }
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // One finger on the home poker button, coach glove up. The clip is that
        // tap opening poker. The hand must not still be on the rail afterwards.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void MutePokerTapShot()
        {
            if (!File.Exists("/tmp/flock-five-b88-poker-tap")) return;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

        static bool _shotPokerTapOn;

        public IEnumerator ShotPokerTap()
        {
            // A second trigger while the first clip is running would tap twice.
            if (_shotPokerTapOn) yield break;
            _shotPokerTapOn = true;
            const string mp4 = "/Users/zfxgames/wkspaces/wingout/b88-poker-tap.mp4";
            const string frames = "/tmp/b88-poker-tap-frames";
            const string log = "/tmp/flock-five-b88-poker-tap.txt";
            ShotKeepAwake = true;
            MuteShotAudio();
            Application.runInBackground = true;
            UnityEditor.EditorApplication.isPaused = false;
            int nextKeep = PlayerPrefs.GetInt("flockfive.next", 0);
            bool hadNext = PlayerPrefs.HasKey("flockfive.next");
            int pokerKeep = PlayerPrefs.GetInt(CoachPokerKey, 0);
            bool hadPoker = PlayerPrefs.HasKey(CoachPokerKey);
            int dailyKeep = PlayerPrefs.GetInt(CoachDailyKey, 0);
            bool hadDaily = PlayerPrefs.HasKey(CoachDailyKey);
            int hiveKeep = PlayerPrefs.GetInt(CoachHiveKey, 0);
            bool hadHive = PlayerPrefs.HasKey(CoachHiveKey);
            int adoptKeep = PlayerPrefs.GetInt(AvatarAdoptedPref, 0);
            bool hadAdopt = PlayerPrefs.HasKey(AvatarAdoptedPref);
            int welcomeKeep = PlayerPrefs.GetInt(WelcomeBonusKey, 0);
            bool hadWelcome = PlayerPrefs.HasKey(WelcomeBonusKey);
            void Log(string line)
            {
                File.AppendAllText(log, line + "\n");
                UnityEngine.Debug.Log("[pokertap] " + line);
            }
            Touchscreen ts = null;
            try
            {
                File.WriteAllText(log, "");
                Directory.CreateDirectory("/Users/zfxgames/wkspaces/wingout");
                if (Directory.Exists(frames)) Directory.Delete(frames, true);
                Directory.CreateDirectory(frames);
                if (EditorGameSize != null) EditorGameSize(1179, 2556);
                Screen.SetResolution(1179, 2556, false);
                // A modal (package errors) freezes the player, then one frame
                // reports the whole stall. Cap the step so the splash still settles.
                for (float settle = 0f; settle < 6f;)
                {
                    settle += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    MuteShotAudio();
                    yield return null;
                }
                MuteShotAudio();
                PlayerPrefs.SetInt("flockfive.next", 1);
                PlayerPrefs.SetInt(AvatarAdoptedPref, 1);
                PlayerPrefs.SetInt(AvatarNamedPref, 1);
                PlayerPrefs.SetString(AvatarNamePref, "Pip");
                PlayerPrefs.SetInt(CoachDailyKey, 1);
                PlayerPrefs.SetInt(CoachHiveKey, 1);
                PlayerPrefs.SetInt(CoachPokerKey, 0);
                PlayerPrefs.SetInt(WelcomeBonusKey, 1);
                PlayerPrefs.SetInt(WelcomePendingKey, 0);
                PlayerPrefs.Save();
                ShowSplash();
                _streakSlide = -1f;
                // Boot already armed daily and hive from the saved prefs. Those
                // in-memory flags block the poker hand even after the prefs flip.
                NoteDailyIntroLeft();
                NoteHiveIntroLeft();
                _dailyIntro = false;
                _dailyIntroLive = false;
                _dailyOpen = false;
                _hiveIntro = false;
                _hiveIntroLive = false;
                _welcomeQueued = false;
                _welcomeOpen = false;
                _welcomeGlove = false;
                _adoptLive = false;
                _adoptOwed = false;
                _adoptStep = AdoptStep.Off;
                // ShowSplash stamps the poker lesson done if boot already saw it.
                PlayerPrefs.SetInt(CoachPokerKey, 0);
                ArmPokerIntro();
                float wait = 0f;
                while ((!_pokerIntroLive || !_gloveVis || _gloveAlpha < 0.9f) && wait < 20f)
                {
                    wait += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    MuteShotAudio();
                    if (_home != HomeFace.Splash) _home = HomeFace.Splash;
                    _dailyIntro = false;
                    _dailyIntroLive = false;
                    _hiveIntro = false;
                    _hiveIntroLive = false;
                    _welcomeQueued = false;
                    _welcomeOpen = false;
                    _welcomeGlove = false;
                    // Arm resets the fade, so only call it when the lesson is off.
                    // A stamped key would make that call clear the lesson.
                    if (!_pokerIntro)
                    {
                        PlayerPrefs.SetInt(CoachPokerKey, 0);
                        ArmPokerIntro();
                    }
                    yield return null;
                }
                bool gloveUp = _pokerIntroLive && _gloveVis && _gloveAlpha >= 0.9f;
                Log("glove live=" + _pokerIntroLive + " vis=" + _gloveVis + " a=" + _gloveAlpha.ToString("0.00")
                    + " home=" + _home + " wait=" + wait.ToString("0.00")
                    + " up=" + gloveUp
                    + " splash=" + _splash + " rail=" + _railInit
                    + " poker=" + _pokerIntro + " daily=" + _dailyIntro + " hive=" + _hiveIntro
                    + " adopt=" + _adoptLive + "/" + _adoptStep + " owed=" + _adoptOwed
                    + " welcome=" + _welcomeQueued + "/" + _welcomeOpen + "/" + _welcomeGlove
                    + " shot=" + EditorShotLive + " next=" + LevelData.NextPlay
                    + " pause=" + GamePause.Paused + " ads=" + Ads.IsBusy
                    + " tap=" + RailTapRect(RailPoker));
                if (!gloveUp)
                {
                    Log("POKER_TAP_FAIL");
                    yield break;
                }
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                ts = InputSystem.AddDevice<Touchscreen>();
                ts.MakeCurrent();
                Time.captureDeltaTime = 1f / 60f;
                int w = 0, h = 0, n = 0;
                bool opened = false;
                const int lead = 24;
                const int total = 150;
                for (int f = 0; f < total; f++)
                {
                    if (f == lead)
                    {
                        var r = RailTapRect(RailPoker);
                        var p = new Vector2(r.center.x, Screen.height - r.center.y);
                        // Leave the event queued. The next player-loop update delivers
                        // it; an update from this coroutine would clear the press first.
                        InputSystem.QueueStateEvent(ts, new TouchState
                        {
                            touchId = 1,
                            phase = UnityEngine.InputSystem.TouchPhase.Began,
                            position = p,
                        });
                        Log("down " + p + " rect " + r);
                    }
                    if (f == lead + 4)
                    {
                        var r = RailTapRect(RailPoker);
                        var p = new Vector2(r.center.x, Screen.height - r.center.y);
                        InputSystem.QueueStateEvent(ts, new TouchState
                        {
                            touchId = 1,
                            phase = UnityEngine.InputSystem.TouchPhase.Ended,
                            position = p,
                        });
                    }
                    yield return null;
                    MuteShotAudio();
                    yield return new WaitForEndOfFrame();
                    if (_home == HomeFace.Poker) opened = true;
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    if (tex == null) continue;
                    w = tex.width;
                    h = tex.height;
                    File.WriteAllBytes(frames + "/f" + n.ToString("0000") + ".jpg", tex.EncodeToJPG(80));
                    Object.Destroy(tex);
                    n++;
                }
                Time.captureDeltaTime = 0f;
                // Home coach must stay down. The deal lesson on the poker table
                // is the next step, not the home hand coming back.
                bool homeHand = _home != HomeFace.Poker && (_gloveVis || _gloveAlpha > 0.05f);
                bool rearm = _pokerIntroLive || homeHand;
                Log("opened=" + opened + " home=" + _home + " intro=" + _pokerIntroLive
                    + " glove=" + _gloveVis + " a=" + _gloveAlpha.ToString("0.00")
                    + " rearm=" + rearm + " frames=" + n + " " + w + "x" + h);
                string ff = File.Exists("/opt/homebrew/bin/ffmpeg") ? "/opt/homebrew/bin/ffmpeg" : "ffmpeg";
                var psi = new ProcessStartInfo
                {
                    FileName = ff,
                    Arguments = "-y -framerate 60 -i \"" + frames + "/f%04d.jpg\" -vf pad=ceil(iw/2)*2:ceil(ih/2)*2 -c:v libx264 -pix_fmt yuv420p -r 60 \"" + mp4 + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc != null) proc.WaitForExit(120000);
                }
                try { if (Directory.Exists(frames)) Directory.Delete(frames, true); } catch { }
                Log(opened && !rearm ? "POKER_TAP_OK" : "POKER_TAP_FAIL");
            }
            finally
            {
                _shotPokerTapOn = false;
                Time.captureDeltaTime = 0f;
                ShotKeepAwake = false;
                if (ts != null) InputSystem.RemoveDevice(ts);
                if (hadNext) PlayerPrefs.SetInt("flockfive.next", nextKeep);
                else PlayerPrefs.DeleteKey("flockfive.next");
                if (hadPoker) PlayerPrefs.SetInt(CoachPokerKey, pokerKeep);
                else PlayerPrefs.DeleteKey(CoachPokerKey);
                if (hadDaily) PlayerPrefs.SetInt(CoachDailyKey, dailyKeep);
                else PlayerPrefs.DeleteKey(CoachDailyKey);
                if (hadHive) PlayerPrefs.SetInt(CoachHiveKey, hiveKeep);
                else PlayerPrefs.DeleteKey(CoachHiveKey);
                if (hadAdopt) PlayerPrefs.SetInt(AvatarAdoptedPref, adoptKeep);
                else PlayerPrefs.DeleteKey(AvatarAdoptedPref);
                if (hadWelcome) PlayerPrefs.SetInt(WelcomeBonusKey, welcomeKeep);
                else PlayerPrefs.DeleteKey(WelcomeBonusKey);
                PlayerPrefs.Save();
                MuteShotAudio();
                if (File.Exists("/tmp/flock-five-b88-poker-tap-exit"))
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                    UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.Exit(0);
                }
            }
        }
    }
}
#endif

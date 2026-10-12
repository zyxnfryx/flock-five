#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FlockFive
{
    public partial class FlockFiveApp
    {
        // Build 85 blocker repro: fresh save, level 1 cleared, back home, the Daily lesson glove
        // up, a real touch on the Daily button -> the card must open and the wheel lesson start.
        // Lessons run live here (EditorShotLive stays false).
        static ShotHost83 _hostDaily;
        public static bool ShotKeepAwake; // live-lesson shots: keep the editor unpaused
        const string ResDaily = "/tmp/flock-five-b85-daily.txt";

        public IEnumerator ShotBuild85Daily()
        {
            if (_hostDaily == null)
            {
                _hostDaily = new GameObject("ShotHost85Daily").AddComponent<ShotHost83>();
                _hostDaily.StartCoroutine(ShotBuild85DailyBody());
            }
            yield break;
        }

        string DailyDiag()
        {
            var g = HomePressGates(RailDaily);
            return "introLive=" + _dailyIntroLive + " intro=" + _dailyIntro + " open=" + _dailyOpen + " ask=" + _dailyAskOpen
                + " welcome=" + _welcomeOpen + "/" + _welcomeQueued + "/" + _welcomeGlove + " streak=" + _streakSlide.ToString("0.00")
                + " gap=" + _rewardGap + " adopt=" + _adoptLive + "/" + _adoptStep + "/owed" + _adoptOwed + " room=" + SplashLessonRoom() + " turn=" + SplashDailyTurn()
                + " rail=" + RailLive(RailDaily) + " glove=" + _gloveVis + " popupBlocked=" + PopupBlocked(PopupKind.Daily)
                + " gates[paused=" + g.Paused + " swallow=" + g.ResumeSwallow + " show=" + g.ShowHolds + " ate=" + g.CoachAte
                + " guard=" + g.LessonGuard + " hard=" + g.HardModal + " soft=" + g.SoftModal + " railLesson=" + g.RailLesson
                + "] stepGate=" + StepGateBlocks(SplashDailyTapRect()) + " step=" + HomeStepRail() + " phase=" + _dailyPhase
                + " samples=" + _homeSamples.Count + (_homeSamples.Count > 0 ? "@" + _homeSamples[0].Pos + _homeSamples[0].Phase : "") + " scr=" + Screen.width + "x" + Screen.height + " gloveAct=" + _gloveAct + " tsPhase=" + (Touchscreen.current != null ? Touchscreen.current.primaryTouch.phase.ReadValue().ToString() : "none") + " tap=" + SplashDailyTapRect() + " ads=" + Ads.IsBusy + " pause=" + GamePause.Paused + " ts=" + Time.timeScale;
        }

        Touchscreen _greetTs;
        IEnumerator GreetTap()
        {
            _greetTs = InputSystem.AddDevice<Touchscreen>();
            var p = new Vector2(Screen.width * 0.5f, Screen.height * 0.35f);
            InputSystem.QueueStateEvent(_greetTs, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Began, position = p });
            yield return null; yield return null;
            InputSystem.QueueStateEvent(_greetTs, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = p });
            yield return null; yield return null;
            InputSystem.RemoveDevice(_greetTs);
            _greetTs = null;
        }

        IEnumerator ShotBuild85DailyBody()
        {
            var sb = new StringBuilder();
            void Log(string s) { sb.AppendLine(s); File.WriteAllText(ResDaily, sb.ToString()); Debug.Log("[b85daily] " + s); }
            ShotKeepAwake = true;
            MuteShotAudio();
            Application.runInBackground = true;
            UnityEditor.EditorApplication.isPaused = false;
            Screen.SetResolution(1179, 2556, false);
            yield return new WaitForSecondsRealtime(1.0f);
            // Fresh save with level 1 cleared, as on a first install after the first garden.
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt("flockfive.next", 1);
            PlayerPrefs.Save();
            DailyBonus.Boot();
            ArmAdopt();
            ShowSplash();
            ArmDailyIntro();
            Log("armed: " + DailyDiag());
            float t = 0f;
            while (!(_dailyIntroLive && _gloveVis && _coachFade >= 1f) && t < 150f)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                if (Mathf.Repeat(t, 2f) < Time.unscaledDeltaTime) Log("wait " + t.ToString("0.0") + ": " + DailyDiag());
                // Clear earlier home beats (reward sign, adopt) the way a player would.
                if (_streakSlide >= 0f && _rewardChoice == 0 && RewardChoiceUp) _rewardChoice = 2;
                // Greet taps: the adopt fly/greet steps advance on any tap, like a player would.
                if (AdoptGreetUp() && t > 3f) { AdvanceAdoptGreet(); Log("greet tapped: " + DailyDiag()); }
                if (!_adoptLive && !_dailyIntro && !_dailyOpen && AvatarAdopted() && PlayerPrefs.GetInt(CoachDailyKey, 0) == 0)
                {
                    ArmDailyIntro();
                    Log("re-armed daily after adopt: " + DailyDiag());
                }
                // Adopt lesson: name the bird at the Look step (the player's Done).
                if (_adoptLive && _adoptStep == AdoptStep.Look)
                {
                    MarkAdopted(_adoptCol, "Pip");
                    _avatarRenameText = "Pip";
                    CommitAvatarRename();
                    // The device arms the Daily lesson on this return home (level 1 cleared).
                    if (!_dailyIntro) { PlayerPrefs.SetInt(CoachDailyKey, 0); _hiveIntroLive = false; _pokerIntroLive = false; ArmDailyIntro(); if (!_dailyIntro) _dailyIntro = true; }
                    Log("adopt done: " + DailyDiag());
                }
                yield return null;
            }
            Log("glove up after " + t.ToString("0.0") + "s: " + DailyDiag());
            var r = SplashDailyTapRect();
            var p = new Vector2(r.center.x, Screen.height - r.center.y);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var ts = InputSystem.AddDevice<Touchscreen>(); ts.MakeCurrent();
            InputSystem.QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = p });
            for (int i = 0; i < 4; i++) { yield return null; Log("down" + i + ": " + DailyDiag()); }
            InputSystem.QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = p });
            yield return null;
            Log("up: " + DailyDiag());
            InputSystem.QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.None, position = p });
            float w = 0f;
            while (!_dailyOpen && w < 2f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1.0f);
            yield return new WaitForEndOfFrame();
            SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/daily-after-tap.png");
            Log("after: " + DailyDiag());
            Log("events: " + DbgDaily);
            bool ok = _dailyOpen && _dailyIntroLive;
            Log(ok ? "B85DAILY_OK card open, wheel lesson live" : "B85DAILY_FAIL card did not open");
            InputSystem.RemoveDevice(ts);
            ShotKeepAwake = false;
            if (File.Exists("/tmp/flock-five-b85-exit")) { UnityEditor.EditorApplication.isPlaying = false; UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.Exit(0); }
        }
    }
}
#endif

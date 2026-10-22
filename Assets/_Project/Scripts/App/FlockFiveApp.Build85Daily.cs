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
                + " samples=" + _homeSamples.Count + (_homeSamples.Count > 0 ? "@" + _homeSamples[0].Pos + _homeSamples[0].Phase : "") + " scr=" + PhoneArea.W + "x" + PhoneArea.H + " gloveAct=" + _gloveAct + " tsPhase=" + (Touchscreen.current != null ? Touchscreen.current.primaryTouch.phase.ReadValue().ToString() : "none") + " tap=" + SplashDailyTapRect() + " ads=" + Ads.IsBusy + " pause=" + GamePause.Paused + " ts=" + Time.timeScale;
        }

        Touchscreen _greetTs;
        int _dailyFirstN;
        IEnumerator GreetTap()
        {
            _greetTs = InputSystem.AddDevice<Touchscreen>();
            var p = new Vector2(PhoneArea.W * 0.5f, PhoneArea.H * 0.35f);
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
            yield return new WaitForSecondsRealtime(6.0f); // past the studio card
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
                if (_dailyIntroLive && _dailyFirstN < 45)
                {
                    yield return new WaitForEndOfFrame();
                    if (_dailyFirstN == 0) System.IO.Directory.CreateDirectory("/Users/zfxgames/wkspaces/birdshot/gb/b85/dailyfirst");
                    SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/dailyfirst/f" + (_dailyFirstN++).ToString("00") + ".png");
                    continue;
                }
                // Clear earlier home beats (reward sign, adopt) the way a player would.
                if (_streakSlide >= 0f && _rewardChoice == 0 && RewardChoiceUp) _rewardChoice = 2;
                // Greet taps: the adopt fly/greet steps advance on any tap, like a player would.
                if (AdoptGreetUp() && t > 9f && !StudioCardHold) { yield return new WaitForEndOfFrame(); SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/b85-caption-adopt-greet.png"); AdvanceAdoptGreet(); Log("greet tapped: " + DailyDiag()); }
                if (!_adoptLive && !_dailyIntro && !_dailyOpen && AvatarAdopted() && PlayerPrefs.GetInt(CoachDailyKey, 0) == 0)
                {
                    ArmDailyIntro();
                    Log("re-armed daily after adopt: " + DailyDiag());
                }
                // Adopt lesson: name the bird at the Look step (the player's Done).
                if (_adoptLive && _adoptStep == AdoptStep.Look)
                {
                    yield return new WaitForSecondsRealtime(1.2f); yield return new WaitForEndOfFrame(); SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/b85-caption-adopt-look.png");
                    MarkAdopted(_adoptCol, "Pip");
                    _avatarRenameText = "Pip";
                    CommitAvatarRename();
                    // The device arms the Daily lesson on this return home (level 1 cleared).
                    if (!_dailyIntro) { PlayerPrefs.SetInt(CoachDailyKey, 0); _hiveIntroLive = false; _pokerIntroLive = false; ArmDailyIntro(); if (!_dailyIntro) _dailyIntro = true; }
                    Log("adopt done: " + DailyDiag());
                }
                yield return null;
            }
            // b86: keep recording the caption's first frames (no banner flash).
            while (_dailyFirstN < 45) { yield return new WaitForEndOfFrame(); SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/dailyfirst/f" + (_dailyFirstN++).ToString("00") + ".png"); }
            yield return new WaitForSecondsRealtime(0.6f); yield return new WaitForEndOfFrame(); SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/b85-caption-daily-rail.png");
            Log("glove up after " + t.ToString("0.0") + "s: " + DailyDiag());
            var r = SplashDailyTapRect();
            var p = new Vector2(r.center.x, PhoneArea.H - r.center.y);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var ts = InputSystem.AddDevice<Touchscreen>(); ts.MakeCurrent();
            for (int i = 0; i < 10; i++) { yield return new WaitForEndOfFrame(); System.IO.Directory.CreateDirectory("/Users/zfxgames/wkspaces/birdshot/gb/b85/prepress"); SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/prepress/f" + i.ToString("00") + ".png"); }
            InputSystem.QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = p });
            System.IO.Directory.CreateDirectory("/Users/zfxgames/wkspaces/birdshot/gb/b85/press");
            for (int i = 0; i < 6; i++) { yield return new WaitForEndOfFrame(); SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/press/f" + i.ToString("00") + ".png"); Log("down" + i + ": " + DailyDiag()); }
            InputSystem.QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = p });
            yield return null;
            Log("up: " + DailyDiag());
            Log("press frames saved");
            System.IO.Directory.CreateDirectory("/Users/zfxgames/wkspaces/birdshot/gb/b85/wheelfirst");
            for (int wf = 0; wf < 24; wf++) { yield return new WaitForEndOfFrame(); SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/wheelfirst/f" + wf.ToString("00") + ".png"); }
            InputSystem.QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.None, position = p });
            float w = 0f;
            while (!_dailyOpen && w < 2f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1.0f);
            yield return new WaitForEndOfFrame();
            SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/daily-after-tap.png");
            SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/b85-caption-daily-claim.png");
            Log("after: " + DailyDiag());
            Log("events: " + DbgDaily);
            bool ok = _dailyOpen && _dailyIntroLive;
            Log(ok ? "B85DAILY_OK card open, wheel lesson live" : "B85DAILY_FAIL card did not open");
            // b86: 60fps-paced spin and payout clip (frame per frame, timing logged).
            if (ok)
            {
                System.IO.Directory.CreateDirectory("/Users/zfxgames/wkspaces/birdshot/gb/b85/spin");
                StartDailySpin();
                float t0 = Time.realtimeSinceStartup; int n = 0;
                while (Time.realtimeSinceStartup - t0 < 9f && n < 700) { yield return new WaitForEndOfFrame(); SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/spin/f" + (n++).ToString("000") + ".png"); }
                Log("SPINCLIP frames " + n + " sec " + (Time.realtimeSinceStartup - t0).ToString("0.00") + " phase " + _dailyPhase + " coins " + DailyBonus.SpinCoins);
                SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/b86-wheel-landed.png");
                ResolveDailyGrant(false);
                yield return new WaitForSecondsRealtime(2.5f);
                // Poker intro caption (snug, clear of the disc, glove from below).
                _dailyIntroLive = false; _dailyIntro = false; _dailyOpen = false;
                PlayerPrefs.SetInt(CoachDailyKey, 1);
                ArmPokerIntro();
                yield return new WaitForSecondsRealtime(3.0f); yield return new WaitForEndOfFrame();
                SaveFrame("/Users/zfxgames/wkspaces/birdshot/gb/b85/b86-caption-poker.png");
                Log("poker intro live " + _pokerIntroLive + " banner " + BannerSkip);
            }
            InputSystem.RemoveDevice(ts);
            ShotKeepAwake = false;
            if (File.Exists("/tmp/flock-five-b85-exit")) { UnityEditor.EditorApplication.isPlaying = false; UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.Exit(0); }
        }
    }
}
#endif

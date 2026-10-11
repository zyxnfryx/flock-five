#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 81 audio. Listener gain, pause, and the MixDesk duck come back after
    // an ad, the ATT pre-prompt, the system ATT answer, the consent prompt,
    // a pause/resume, and an audio-session interruption end. A duck token that
    // nobody releases expires on its own.
    static class Build81AudioTests
    {
        [MenuItem("Flock Five/Build 81 Audio Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build81-audio] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            bool hadAsk = PlayerPrefs.HasKey(AdConsent.AttAskKey);
            int ask = PlayerPrefs.GetInt(AdConsent.AttAskKey, 0);
            bool hadSnooze = PlayerPrefs.HasKey(AdConsent.AttSnoozeKey);
            int snooze = PlayerPrefs.GetInt(AdConsent.AttSnoozeKey, 0);
            bool hadReward = PlayerPrefs.HasKey(RewardedGrant.Pref);
            string reward = PlayerPrefs.GetString(RewardedGrant.Pref, "");
            var apply = RewardedGrant.TryApply;
            try
            {
                RewardedGrant.ResetEmpty();
                RewardedGrant.TryApply = null;
                GamePause.Reset();
                AudioGuard.ResetForTests();
                AdConsent.ResetTransient();

                Check("reward-restores", Restores(() => Ads.DeliverRewardedCallback()), "reward");
                Check("close-restores", Restores(() => Ads.DeliverClosedCallback()), "close");
                Check("fail-restores", Restores(() => Ads.DeliverFailedCallback()), "fail");
                Check("interstitial-restores", Restores(() => Ads.DeliverInterstitialClosed()), "interstitial");

                Check("consent-allow-restores", Restores(() => AdConsent.AcceptGdpr()), "allow");
                AdConsent.ResetTransient();
                Check("consent-not-now-restores", Restores(() => AdConsent.DeclineGdpr()), "decline");
                AdConsent.ResetTransient();

                Check("att-preprompt-continue-restores", Restores(() => AdConsent.ContinueAtt()), "continue");
                Check("att-preprompt-not-now-restores", Restores(() => AdConsent.NotNow(3)), "not now");
                Check("att-system-restores", Restores(() => Tracking.NoteComplete("2")), "status 2");

                Check("resume-restores", Restores(() => FlockFiveApp.NoteAppResume()), "focus");

                AudioGuard.ResetForTests();
                Dirty();
                int activations = AudioGuard.ActivateCalls;
                AudioGuard.OnAudioInterruption("began");
                bool beganHolds = AudioListener.pause && AudioListener.volume < 0.5f
                    && AudioGuard.LiveTokens > 0 && AudioGuard.ActivateCalls == activations;
                AudioGuard.OnAudioInterruption("ended");
                Check("interruption-end-restores",
                    beganHolds && Healed(),
                    "beganHeld=" + beganHolds + " " + State());

                Check("scene-close-drops-popup-duck", SceneCloseDropsDuck(), "popup token");
                Check("ad-duck-survives-scene-until-close", AdDuckUntilEndShow(), "show");
                Check("leaked-duck-expires", LeakedExpires(), "10s");
                Check("restore-clears-token", RestoreClearsEarly(), "stale");
                Check("user-mix-survives-restore", UserMixSurvives(), "toggles");
                Check("stuck-pause-bit-clears", StuckPauseClears(), "saved pause");
                Check("mixdesk-duck-resets", MixResets(), "freeze/slew/lead");
                Check("startup-audio-log", StartupLogSeesVolume(), StartupTrace.LastAudioLog ?? "");
                Check("idle-not-on-first-frames", IdleGate(), "frames");
                Check("no-audiosettings-reset", !AudioResetCall(out string resetAt), resetAt ?? "none");
                Check("native-session", NativeSession(out string nativeDetail), nativeDetail);
                Check("init-after-idle", InitAfterIdle(out string idleDetail), idleDetail);
                Check("level-stays-instant", LevelStaysInstant(out string levelDetail), levelDetail);
            }
            finally
            {
                GamePause.Reset();
                AudioGuard.ResetForTests();
                AudioListener.volume = 1f;
                AudioListener.pause = false;
                AdConsent.ResetTransient();
                RewardedGrant.TryApply = apply;
                RewardedGrant.ResetEmpty();
                if (hadReward) PlayerPrefs.SetString(RewardedGrant.Pref, reward);
                else PlayerPrefs.DeleteKey(RewardedGrant.Pref);
                if (hadAsk) PlayerPrefs.SetInt(AdConsent.AttAskKey, ask);
                else PlayerPrefs.DeleteKey(AdConsent.AttAskKey);
                if (hadSnooze) PlayerPrefs.SetInt(AdConsent.AttSnoozeKey, snooze);
                else PlayerPrefs.DeleteKey(AdConsent.AttSnoozeKey);
                StartupTrace.ResetWindow();
                var stray = Object.FindObjectsByType<MixDesk>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (stray != null)
                {
                    for (int i = 0; i < stray.Length; i++)
                    {
                        if (stray[i] == null || stray[i].gameObject.name != "audio-guard-mix") continue;
                        MixDesk.DetachForTests(stray[i]);
                        Object.DestroyImmediate(stray[i].gameObject);
                    }
                }
            }

            Debug.Log("[build81-audio] " + pass + " passed, " + fail + " failed");
        }

        static void Dirty()
        {
            AudioGuard.ResetForTests();
            AudioListener.volume = 0.15f;
            AudioListener.pause = true;
            AudioGuard.PushDuck("popup");
        }

        static bool Healed()
        {
            return Mathf.Abs(AudioListener.volume - 1f) < 0.001f
                && !AudioListener.pause
                && AudioGuard.LiveTokens == 0
                && Mathf.Abs(AudioGuard.TokenScale - 1f) < 0.001f
                && AudioGuard.ActivateCalls > 0;
        }

        static string State()
        {
            return "vol=" + AudioListener.volume.ToString("0.00")
                + " pause=" + AudioListener.pause
                + " tokens=" + AudioGuard.LiveTokens
                + " scale=" + AudioGuard.TokenScale.ToString("0.00")
                + " activate=" + AudioGuard.ActivateCalls;
        }

        static bool Restores(System.Action act)
        {
            Dirty();
            act();
            bool ok = Healed();
            AudioGuard.ResetForTests();
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            return ok;
        }

        static bool SceneCloseDropsDuck()
        {
            AudioGuard.ResetForTests();
            AudioGuard.PushDuck("popup");
            AudioGuard.PushDuck("settings");
            AudioListener.volume = 0.4f;
            AudioGuard.OnSceneOrPopupClosed();
            return AudioGuard.LiveTokens == 0 && Mathf.Abs(AudioListener.volume - 1f) < 0.001f;
        }

        static bool AdDuckUntilEndShow()
        {
            GamePause.Reset();
            AudioGuard.ResetForTests();
            Ads.BeginShow();
            AudioGuard.PushDuck("popup");
            AudioGuard.OnSceneOrPopupClosed();
            bool held = Ads.IsShowing && AudioGuard.LiveTokens == 1 && AudioListener.pause;
            Ads.EndShow();
            bool back = !Ads.IsShowing && Healed();
            GamePause.Reset();
            return held && back;
        }

        static bool LeakedExpires()
        {
            AudioGuard.ResetForTests();
            double now = 100;
            AudioGuard.Clock = () => now;
            AudioGuard.PushDuck("popup");
            bool ducked = AudioGuard.LiveTokens == 1 && AudioGuard.TokenScale < 0.5f;
            now += 9.9;
            AudioGuard.Tick();
            bool still = AudioGuard.LiveTokens == 1;
            now += 0.2;
            AudioGuard.Tick();
            bool gone = AudioGuard.LiveTokens == 0 && Mathf.Abs(AudioGuard.TokenScale - 1f) < 0.001f;
            AudioGuard.Clock = null;
            return ducked && still && gone;
        }

        static bool RestoreClearsEarly()
        {
            AudioGuard.ResetForTests();
            double now = 0;
            AudioGuard.Clock = () => now;
            AudioGuard.PushDuck("popup");
            AudioListener.volume = 0.2f;
            AudioListener.pause = true;
            AudioGuard.Restore();
            bool ok = AudioGuard.LiveTokens == 0 && Healed();
            AudioGuard.Clock = null;
            return ok;
        }

        static bool UserMixSurvives()
        {
            AudioGuard.ResetForTests();
            AudioGuard.MusicEnabled = false;
            AudioGuard.SfxVolume = 0.5f;
            AudioGuard.PushDuck("popup");
            bool ducked = AudioGuard.MusicScale == 0f && AudioGuard.SfxScale < 0.2f && AudioGuard.SfxScale > 0.1f;
            AudioListener.volume = 0.2f;
            AudioListener.pause = true;
            AudioGuard.Restore();
            bool back = AudioGuard.LiveTokens == 0
                && AudioGuard.MusicScale == 0f
                && Mathf.Abs(AudioGuard.SfxScale - 0.5f) < 0.001f
                && Mathf.Abs(AudioListener.volume - 1f) < 0.001f
                && !AudioListener.pause;
            AudioGuard.ResetForTests();
            return ducked && back;
        }

        static bool StuckPauseClears()
        {
            GamePause.Reset();
            AudioGuard.ResetForTests();
            AudioListener.pause = true;
            AudioListener.volume = 0.2f;
            GamePause.Push();
            bool saved = AudioListener.pause;
            Ads.DeliverClosedCallback();
            bool restored = !AudioListener.pause && Mathf.Abs(AudioListener.volume - 1f) < 0.001f;
            GamePause.Pop();
            bool stays = !AudioListener.pause && Mathf.Abs(AudioListener.volume - 1f) < 0.001f;
            GamePause.Reset();
            return saved && restored && stays;
        }

        static bool MixResets()
        {
            var go = new GameObject("audio-guard-mix");
            var desk = go.AddComponent<MixDesk>();
            desk.AttachForTests();
            desk.ForceDuckForTests(0.1f, 0.2f, 0.72f, 4f);
            AudioGuard.Restore();
            bool ok = Mathf.Abs(desk.FreezeDuck - 1f) < 0.001f
                && Mathf.Abs(desk.SlewDuck - 1f) < 0.001f
                && Mathf.Abs(desk.LeadDuck - 1f) < 0.001f
                && !desk.LeadHot;
            MixDesk.DetachForTests(desk);
            Object.DestroyImmediate(go);
            return ok;
        }

        static bool StartupLogSeesVolume()
        {
            StartupTrace.ResetWindow();
            AudioListener.volume = 1f;
            var span = StartupTrace.Begin("AudioTestStep");
            AudioListener.volume = 0.3f;
            span.Dispose();
            string line = StartupTrace.LastAudioLog ?? "";
            AudioListener.volume = 1f;
            StartupTrace.ResetWindow();
            return line.IndexOf("step=AudioTestStep") >= 0
                && line.IndexOf("changed=1") >= 0
                && line.IndexOf("vol=1.00") >= 0
                && line.IndexOf("vol=0.30") >= 0
                && line.IndexOf("dsp=") >= 0
                && line.IndexOf("rate=") >= 0;
        }

        static bool IdleGate()
        {
            return !Ads.IdleDue(0, 5f, 5f, false)
                && !Ads.IdleDue(1, 5f, 5f, false)
                && Ads.IdleDue(2, 0.1f, 0.5f, false)
                && !Ads.IdleDue(2, 0.1f, 0.49f, false)
                && !Ads.IdleDue(2, 0.1f, 5f, true)
                && Ads.IdleDue(2, 2f, 0f, true)
                && Ads.IdleDue(8, 2f, 0f, false);
        }

        static bool AudioResetCall(out string where)
        {
            where = null;
            string root = Path.Combine(Application.dataPath, "_Project/Scripts");
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = File.ReadAllText(files[i]);
                if (text.IndexOf("AudioSettings.Reset(") < 0) continue;
                where = files[i];
                return true;
            }
            return false;
        }

        static bool NativeSession(out string detail)
        {
            string path = Path.Combine(Application.dataPath, "Plugins/iOS/FlockFiveTracking.mm");
            string src = File.Exists(path) ? File.ReadAllText(path) : "";
            bool ok = src.IndexOf("AVAudioSessionInterruptionNotification") >= 0
                && src.IndexOf("AVAudioSessionCategoryAmbient") >= 0
                && src.IndexOf("AVAudioSessionCategorySoloAmbient") >= 0
                && src.IndexOf("withOptions:0") >= 0
                && src.IndexOf("setActive:YES") >= 0
                && src.IndexOf("OnAudioInterruption") >= 0
                && src.IndexOf("UnitySendMessage(\"AudioGuard\"") >= 0
                && src.IndexOf("AVAudioSessionCategoryOptionDuckOthers") < 0
                && src.IndexOf("AVAudioSessionCategoryPlayback") < 0
                && src.IndexOf("AVAudioSessionCategoryOptionMixWithOthers") < 0;
            detail = ok ? "Ambient, no duck options" : "missing session glue";
            return ok;
        }

        static bool InitAfterIdle(out string detail)
        {
            string path = Path.Combine(Application.dataPath, "_Project/Scripts/App/AdConsent.cs");
            string src = File.ReadAllText(path);
            int run = src.IndexOf("static IEnumerator Run");
            int apply = run < 0 ? -1 : src.IndexOf("ApplyStoredDoNotSell()", run);
            int idle = run < 0 ? -1 : src.IndexOf("Ads.IdleDue", run);
            int boot = run < 0 ? -1 : src.IndexOf("host.Boot()", run);
            string ads = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/Ads.cs"));
            int init = ads.IndexOf("LevelPlay.Init(");
            bool ok = apply >= 0 && idle > apply && boot > idle && init > 0
                && ads.IndexOf("AudioGuard.Reassert()") > 0
                && ads.IndexOf("AudioGuard.Restore()") > 0;
            detail = "apply=" + apply + " idle=" + idle + " boot=" + boot + " init=" + init;
            return ok;
        }

        static bool LevelStaysInstant(out string detail)
        {
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            string settings = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Settings.cs"));
            int smoke = settings.IndexOf("void MaybeSmokeLaunch");
            int next = smoke < 0 ? -1 : settings.IndexOf("void PollHomeAtt", smoke);
            string body = smoke >= 0 && next > smoke ? settings.Substring(smoke, next - smoke) : "";
            bool ok = app.IndexOf("FF_LEVEL_STARTED") >= 0
                && body.IndexOf("PressLevelButton") >= 0
                && body.IndexOf("DismissTransient") >= 0
                && body.IndexOf("IdleDue") < 0
                && body.IndexOf("LevelPlay.Init") < 0;
            detail = ok ? "smoke still loads level 1" : "smoke waits on ads";
            return ok;
        }
    }
}
#endif

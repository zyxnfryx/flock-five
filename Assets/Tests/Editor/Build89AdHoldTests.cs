#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89. TestFlight 88: an ad tapped through to the App Store, then back to the
    // app, played the music over the ad that was still open, and paid the reward twice.
    // One AdAudioHold covers every ad. Resume and unpause paths respect it; only close,
    // fail, skip, or the safety window release it. One grant per show token.
    public static class Build89AdHoldTests
    {
        const string Out = "/tmp/flock-five-b89-adhold-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build89hold] " + line);
            }

            float keepScale = Time.timeScale;
            bool oldPause = AudioListener.pause;
            float oldListener = AudioListener.volume;
            bool hadReward = PlayerPrefs.HasKey(RewardedGrant.Pref);
            string reward = PlayerPrefs.GetString(RewardedGrant.Pref, "");
            var apply = RewardedGrant.TryApply;
            bool busy = RewardedGrant.UiBusy;
            try
            {
                Quiet();
                RewardedGrant.TryApply = null;
                RewardedGrant.ResetEmpty();

                // 1. Ad open across an app pause/resume keeps music and SFX muted.
                Ads.Present(() => { });
                bool before = Silent();
                AppBackground();
                AppForeground();
                bool afterResume = Silent() && AdAudioHold.Held;
                // A late extra resume source: a stolen GamePause.Pop and an unstick.
                GamePause.Pop();
                GamePause.UnstickAudio();
                AudioGuard.Reassert();
                AudioGuard.OnSceneOrPopupClosed();
                bool afterStolen = Silent() && AdAudioHold.Held;
                Check("ad-open-across-resume-stays-muted", before && afterResume && afterStolen,
                    "before=" + before + " resume=" + afterResume + " stolen=" + afterStolen + " " + State());

                // 2. The ad's close restores the audio.
                Ads.DeliverClosedCallback();
                bool stillHeldOnCallback = Silent();
                Ads.EndShow();
                Check("close-restores-audio", stillHeldOnCallback && Back(),
                    "heldOnCallback=" + stillHeldOnCallback + " " + State());

                // 2b. Same for an interstitial across a resume (Android focus bounce too).
                Quiet();
                Ads.Present(() => { });
                FocusBounce();
                bool intHeld = Silent();
                Ads.DeliverInterstitialClosed();
                Ads.EndShow();
                Check("interstitial-focus-bounce-held-then-restored", intHeld && Back(), State());

                // 3. A failed show restores the audio.
                Quiet();
                Ads.Present(() => { throw new System.Exception("display failed (test)"); });
                Check("failed-show-restores-audio", Back() && !AdAudioHold.Held, State());

                Quiet();
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 0, false, 1);
                Ads.Present(() => { Ads.DeliverFailedCallback(); });
                bool failHeld = Silent();
                AppForeground();
                Ads.EndShow();
                Check("fail-callback-restores-audio", failHeld && Back(), State());
                RewardedGrant.ResetEmpty();

                // 4. Safety release when no close ever arrives.
                Quiet();
                Ads.Present(() => { });
                bool early = !AdAudioHold.Tick(AdAudioHold.SafetySeconds - 1f) && Silent();
                bool due = AdAudioHold.Tick(2f);
                if (due) Ads.ForceClear();
                Check("safety-release-without-close", early && due && Back(), State());

                Quiet();
                Ads.Present(() => { });
                AdAudioHold.Tick(10f);
                AppForeground();
                bool notYet = !AdAudioHold.Tick(AdAudioHold.ResumeSafetySeconds - 1f);
                bool resumeDue = AdAudioHold.Tick(2f);
                if (resumeDue) Ads.ForceClear();
                Check("safety-window-tightens-after-resume", notYet && resumeDue && Back(), State());

                // 5. One grant per show, however many paths ask.
                Quiet();
                RewardedGrant.ResetEmpty();
                int grants = 0;
                RewardedGrant.TryApply = t =>
                {
                    grants++;
                    // The branch reveal flushes from inside the apply.
                    RewardedGrant.Flush();
                    return true;
                };
                RewardedGrant.UiBusy = false;
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 0, false, 1);
                Ads.Present(() => { Ads.DeliverDisplayedCallback(); });
                Ads.DeliverRewardedCallback();
                AppBackground();
                AppForeground();
                Ads.DeliverRewardedCallback();     // second reward after the store hop
                RewardedGrant.Flush();
                int duringAd = grants;
                Ads.DeliverClosedCallback();
                Ads.EndShow();
                RewardedGrant.Flush();
                Ads.DeliverRewardedCallback();     // a very late duplicate
                AppForeground();
                RewardedGrant.Flush();
                Check("reward-grants-once-per-show", duringAd == 0 && grants == 1 && RewardedGrant.ApplyCount == 1,
                    "duringAd=" + duringAd + " grants=" + grants + " applyCount=" + RewardedGrant.ApplyCount);

                // Next show still pays (the token ledger is per show, not global).
                RewardedGrant.Begin(RewardedGrant.Place.Branch, 0, false, 1);
                Ads.Present(() => { Ads.DeliverDisplayedCallback(); });
                Ads.DeliverRewardedCallback();
                Ads.DeliverClosedCallback();
                Ads.EndShow();
                RewardedGrant.Flush();
                Check("next-show-still-pays-once", grants == 2, "grants=" + grants);
            }
            catch (System.Exception ex)
            {
                Check("adhold", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Quiet();
                RewardedGrant.TryApply = apply;
                RewardedGrant.UiBusy = busy;
                RewardedGrant.ResetEmpty();
                if (hadReward) PlayerPrefs.SetString(RewardedGrant.Pref, reward);
                else PlayerPrefs.DeleteKey(RewardedGrant.Pref);
                PlayerPrefs.Save();
                Time.timeScale = keepScale;
                AudioListener.pause = oldPause;
                AudioListener.volume = oldListener;
                AudioGuard.ResetForTests();
                AudioGuard.LoadMuted();
                AudioGuard.LoadVolume();
            }

            string end = fail == 0 ? "BUILD89HOLD_OK " + pass : "BUILD89HOLD_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build89hold] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        // What the Ads host, FlockFiveApp and GardenStorm do on OnApplicationPause(true).
        static void AppBackground()
        {
            AdPause.OnAppPause(true);
            Ads.DeliverPause(true);
            Ads.DeliverFocus(false);
        }

        // OnApplicationPause(false) + OnApplicationFocus(true), every listener.
        static void AppForeground()
        {
            AdPause.OnAppPause(false);
            Ads.DeliverPause(false);
            if (Ads.IsShowing) Ads.ArmResumeSettle();
            AudioGuard.Restore();
            Ads.DeliverFocus(true);
            AudioGuard.Restore();
            FlockFiveApp.NoteAppResume();
        }

        static void FocusBounce()
        {
            Ads.DeliverFocus(false);
            Ads.DeliverFocus(true);
            if (Ads.IsShowing) Ads.ArmResumeSettle();
            AudioGuard.Restore();
        }

        static void Quiet()
        {
            if (Ads.IsShowing) Ads.ForceClear();
            AdAudioHold.ResetForTest();
            GamePause.Reset();
            AdPause.ResetForTest();
            AudioGuard.ResetForTests();
            AudioGuard.SetMasterVolume(1f, false);
            AudioGuard.SetMuted(false, false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            AudioListener.volume = 1f;
        }

        static bool Silent()
        {
            return Ads.IsShowing
                && MixDesk.MasterGain == 0f
                && AudioGuard.MusicScale == 0f
                && AudioGuard.SfxScale == 0f
                && AudioListener.pause;
        }

        static bool Back()
        {
            return !Ads.IsShowing
                && !AdAudioHold.Held
                && !AdPause.Active
                && !AudioListener.pause
                && MixDesk.MasterGain > 0.99f
                && AudioGuard.MusicScale > 0.99f
                && AudioGuard.SfxScale > 0.99f;
        }

        static string State()
        {
            return "show=" + Ads.IsShowing
                + " hold=" + AdAudioHold.Count
                + " pause=" + AudioListener.pause
                + " gain=" + MixDesk.MasterGain.ToString("0.00")
                + " music=" + AudioGuard.MusicScale.ToString("0.00");
        }
    }
}
#endif

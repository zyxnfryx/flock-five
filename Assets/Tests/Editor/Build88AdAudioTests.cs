#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 88. Every ShowAd goes through Ads.Present. Music and SFX stay
    // silent for the whole show, including the reward callback, and come back
    // on close, fail, and skip. The Settings slider and mute are not rewritten.
    public static class Build88AdAudioTests
    {
        const string Out = "/tmp/flock-five-b88-adaudio-tests.txt";

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
                Debug.Log("[build88ad] " + line);
            }

            float keepScale = Time.timeScale;
            float oldListener = 1f;
            bool oldPause = false;
            bool hadVol = PlayerPrefs.HasKey(AudioGuard.VolumeKey);
            float oldVol = PlayerPrefs.GetFloat(AudioGuard.VolumeKey, 1f);
            bool hadMute = PlayerPrefs.HasKey(AudioGuard.MuteKey);
            int oldMute = PlayerPrefs.GetInt(AudioGuard.MuteKey, 0);
            bool hadReward = PlayerPrefs.HasKey(RewardedGrant.Pref);
            string reward = PlayerPrefs.GetString(RewardedGrant.Pref, "");
            bool hadRewardX = PlayerPrefs.HasKey(RewardedGrant.Pref + ".x");
            string rewardX = PlayerPrefs.GetString(RewardedGrant.Pref + ".x", "");
            var apply = RewardedGrant.TryApply;
            try
            {
                oldListener = AudioListener.volume;
                oldPause = AudioListener.pause;
                RewardedGrant.TryApply = null;
                RewardedGrant.ResetEmpty();
                Ads.ResetTransientForTests();
                Quiet();

                string ads = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/Ads.cs"));
                bool wired = ads.IndexOf("Present(() => _rv.ShowAd(Ads.ActivePlacement))", System.StringComparison.Ordinal) >= 0
                    && ads.IndexOf("Present(() => _int.ShowAd(Ads.PlacementClear))", System.StringComparison.Ordinal) >= 0
                    && ads.IndexOf("Present(() => { })", System.StringComparison.Ordinal) >= 0;
                Check("show-sites-share-present", wired, "rewarded, interstitial, simulate");

                Cycle(Check, "rewarded", () =>
                {
                    Ads.DeliverRewardedCallback();
                    return Silent();
                }, () =>
                {
                    Ads.DeliverClosedCallback();
                    return Silent();
                });
                Cycle(Check, "interstitial", null, () =>
                {
                    Ads.DeliverInterstitialClosed();
                    return Silent();
                });

                Arm(0.37f, true);
                bool muteDuring = false;
                Ads.Present(() =>
                {
                    muteDuring = Silent()
                        && AudioGuard.Muted
                        && Mathf.Abs(AudioGuard.MasterVolume - 0.37f) < 0.001f;
                    Ads.DeliverRewardedCallback();
                    AudioGuard.Reassert();
                    muteDuring = muteDuring && Silent() && AudioGuard.Muted;
                });
                Ads.EndShow();
                bool muteHeld = AudioGuard.Muted
                    && Mathf.Abs(AudioGuard.MasterVolume - 0.37f) < 0.001f
                    && Mathf.Abs(MixDesk.MasterGain - 0.37f) < 0.001f
                    && AudioGuard.MusicScale == 0f
                    && AudioGuard.SfxScale == 0f
                    && !Ads.IsShowing
                    && !AudioListener.pause;
                AudioGuard.SetMuted(false, false);
                bool sliderBack = !AudioGuard.Muted
                    && Mathf.Abs(AudioGuard.MasterVolume - 0.37f) < 0.001f
                    && Mathf.Abs(AudioGuard.MusicScale - 0.37f) < 0.001f
                    && Mathf.Abs(AudioGuard.SfxScale - 0.37f) < 0.001f
                    && Mathf.Abs(MixDesk.MasterGain - 0.37f) < 0.001f;
                Check("user-mute-preserved", muteDuring && muteHeld && sliderBack,
                    "during=" + muteDuring + " held=" + muteHeld + " back=" + AudioGuard.MusicScale.ToString("0.00"));

                Check("bed-stays-silent-if-pause-stolen", BedHolds(out string bedDetail), bedDetail);
            }
            catch (System.Exception ex)
            {
                Check("adaudio", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                if (Ads.IsShowing) Ads.ForceClear();
                Ads.ResetTransientForTests();
                GamePause.Reset();
                AdPause.ResetForTest();
                Time.timeScale = keepScale;
                AudioListener.pause = oldPause;
                AudioListener.volume = oldListener;
                RewardedGrant.TryApply = apply;
                RewardedGrant.ResetEmpty();
                if (hadReward) PlayerPrefs.SetString(RewardedGrant.Pref, reward);
                else PlayerPrefs.DeleteKey(RewardedGrant.Pref);
                if (hadRewardX) PlayerPrefs.SetString(RewardedGrant.Pref + ".x", rewardX);
                else PlayerPrefs.DeleteKey(RewardedGrant.Pref + ".x");
                if (hadVol) PlayerPrefs.SetFloat(AudioGuard.VolumeKey, oldVol);
                else PlayerPrefs.DeleteKey(AudioGuard.VolumeKey);
                if (hadMute) PlayerPrefs.SetInt(AudioGuard.MuteKey, oldMute);
                else PlayerPrefs.DeleteKey(AudioGuard.MuteKey);
                PlayerPrefs.Save();
                AudioGuard.ResetForTests();
                AudioGuard.LoadMuted();
                AudioGuard.LoadVolume();
            }

            string end = fail == 0 ? "BUILD88AD_OK " + pass : "BUILD88AD_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88ad] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void Cycle(System.Action<string, bool, string> check, string kind, System.Func<bool> mid, System.Func<bool> closing)
        {
            Arm(0.64f, false);
            bool during = false;
            Ads.Present(() =>
            {
                during = Silent();
                if (mid != null) during = during && mid();
            });
            bool held = during && Silent();
            if (closing != null) held = held && closing();
            check(kind + "-silent-during", held, "silent=" + held + " showing=" + Ads.IsShowing);
            Ads.EndShow();
            check(kind + "-restored-after-close", Back(0.64f, false), State());

            Arm(0.64f, false);
            Ads.Present(() =>
            {
                if (kind == "rewarded") Ads.DeliverFailedCallback();
                else Ads.DeliverInterstitialClosed();
            });
            bool failHeld = Silent();
            Ads.EndShow();
            check(kind + "-restored-after-fail", failHeld && Back(0.64f, false), State());

            Arm(0.64f, false);
            Ads.Present(() => { });
            bool skipHeld = Silent();
            Ads.SkipShow();
            check(kind + "-restored-after-skip", skipHeld && Back(0.64f, false), State());
        }

        static bool BedHolds(out string detail)
        {
            detail = "";
            var prev = MixDesk.Live;
            var go = new GameObject("b88-ad-stem");
            var desk = go.AddComponent<MixDesk>();
            desk.AttachForTests();
            try
            {
                desk.PlantStemForTests(0.55f);
                Arm(1f, false);
                Ads.BeginShow();
                desk.PlantStemForTests(0.55f);
                AudioListener.pause = false;
                desk.TickForTests();
                bool zero = desk.StemVolumeForTests() < 0.001f && MixDesk.MasterGain == 0f;
                Ads.HoldAdAudio();
                bool paused = zero && AudioListener.pause && desk.StemVolumeForTests() < 0.001f;
                Ads.EndShow();
                bool back = paused
                    && !Ads.IsShowing
                    && Mathf.Abs(desk.StemVolumeForTests() - 0.55f) < 0.001f
                    && Mathf.Abs(MixDesk.MasterGain - 1f) < 0.001f;
                detail = "zero=" + zero + " paused=" + paused + " stem=" + desk.StemVolumeForTests().ToString("0.00");
                return back;
            }
            finally
            {
                if (Ads.IsShowing) Ads.EndShow();
                MixDesk.DetachForTests(desk);
                Object.DestroyImmediate(go);
                if (prev != null && prev != desk) prev.AttachForTests();
                Quiet();
            }
        }

        static void Quiet()
        {
            if (Ads.IsShowing) Ads.ForceClear();
            GamePause.Reset();
            AdPause.ResetForTest();
            AudioGuard.ResetForTests();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            AudioListener.volume = 1f;
        }

        static void Arm(float slider, bool muted)
        {
            Quiet();
            AudioGuard.SetMasterVolume(slider, false);
            AudioGuard.SetMuted(muted, false);
        }

        static bool Silent()
        {
            return Ads.IsShowing
                && MixDesk.MasterGain == 0f
                && AudioGuard.MusicScale == 0f
                && AudioGuard.SfxScale == 0f
                && AudioListener.pause;
        }

        static bool Back(float slider, bool muted)
        {
            float hear = muted ? 0f : slider;
            return !Ads.IsShowing
                && !AdPause.Active
                && !AudioListener.pause
                && Mathf.Abs(Time.timeScale - 1f) < 0.001f
                && AudioGuard.Muted == muted
                && Mathf.Abs(AudioGuard.MasterVolume - slider) < 0.001f
                && Mathf.Abs(MixDesk.MasterGain - slider) < 0.001f
                && Mathf.Abs(AudioGuard.MusicScale - hear) < 0.001f
                && Mathf.Abs(AudioGuard.SfxScale - hear) < 0.001f;
        }

        static string State()
        {
            return "show=" + Ads.IsShowing
                + " pause=" + AudioListener.pause
                + " gain=" + MixDesk.MasterGain.ToString("0.00")
                + " music=" + AudioGuard.MusicScale.ToString("0.00")
                + " vol=" + AudioGuard.MasterVolume.ToString("0.00")
                + " mute=" + AudioGuard.Muted
                + " scale=" + Time.timeScale.ToString("0.00");
        }
    }
}
#endif

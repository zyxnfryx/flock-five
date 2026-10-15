#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b94: a two-stage ad (stage one closes early, stage two shows, then closes) keeps music and
    // SFX silent the whole time and restores exactly once, after the last stage.
    public static class Build94AdStageTests
    {
        const string Out = "/tmp/flock-five-b94-adstage-tests.txt";

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
                Debug.Log("[build94adstage] " + line);
            }
            bool oldPause = AudioListener.pause;
            try
            {
                Quiet();
                int rel0 = Ads.FinalReleases;
                // Stage 1 (e.g. interstitial) and the request that owns the sequence.
                Ads.BeginShow();
                Ads.BeginShow();
                Check("stage1 silent", Silent(), "");
                Ads.EndStage(); // stage 1 closed early
                Ads.DeliverClosedCallback();
                Check("early close keeps silence", Silent() && Ads.FinalReleases == rel0, "");
                // The owner's wait ends on that close: the last open stage enters the gap.
                Ads.EndStage();
                Check("gap keeps silence", Silent() && Ads.StageGapOpen, "");
                Ads.AdvanceStageGap(0.6f);
                Check("silent mid gap", Silent() && Ads.FinalReleases == rel0, "");
                // Stage 2 shows inside the gap: same sequence, nothing was restored in between.
                Ads.BeginShow();
                Check("stage2 continues, never restored", Silent() && !Ads.StageGapOpen && Ads.FinalReleases == rel0, "");
                Ads.AdvanceStageGap(5f); // no gap is open, so time alone cannot release it
                Check("time alone does not release a live stage", Silent(), "");
                Ads.DeliverRewardedCallback();
                Check("reward callback keeps silence", Silent(), "");
                Ads.EndStage(); // final close
                Ads.DeliverClosedCallback();
                Check("final close waits out the gap", Silent() && Ads.FinalReleases == rel0, "");
                Ads.AdvanceStageGap(Ads.StageGapSeconds + 0.1f);
                Check("restored once after the end", Back() && Ads.FinalReleases == rel0 + 1, State());
                Ads.AdvanceStageGap(5f);
                Ads.EndStage();
                Check("no second restore", Back() && Ads.FinalReleases == rel0 + 1, "");

                // Error / timeout paths restore immediately and fully.
                Ads.BeginShow();
                Ads.BeginShow();
                Ads.EndStage();
                Ads.ForceClear();
                Check("force clear restores immediately", Back(), State());
                Ads.BeginShow();
                Ads.EndShow(); // fail / skip path
                Check("fail path restores immediately", Back(), State());
                // Safety release (close never arrived) through the hold's own window.
                Ads.BeginShow();
                Ads.EndStage();
                Ads.AdvanceStageGap(10f);
                Check("lost close still restores", Back(), State());
            }
            catch (System.Exception ex)
            {
                Check("ad stage", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Quiet();
                AudioListener.pause = oldPause;
            }
            string end = fail == 0 ? "BUILD94ADSTAGE_OK " + pass : "BUILD94ADSTAGE_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
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

        static string State() => "showing=" + Ads.IsShowing + " gain=" + MixDesk.MasterGain + " pause=" + AudioListener.pause + " hold=" + AdAudioHold.Count;

        static bool Silent() => Ads.IsShowing && MixDesk.MasterGain == 0f && AudioGuard.MusicScale == 0f
            && AudioGuard.SfxScale == 0f && AudioListener.pause;

        static bool Back() => !Ads.IsShowing && !AdAudioHold.Held && !AdPause.Active && !AudioListener.pause
            && MixDesk.MasterGain > 0.99f && AudioGuard.MusicScale > 0.99f && AudioGuard.SfxScale > 0.99f;
    }
}
#endif

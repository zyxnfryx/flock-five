#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b126: Brandon's Level 24 ad-branch hang. An ad hold nobody owns is released (TutorialHeal.ForceClearShow),
    // a stage exit leaves no pause behind, and WatchGift's finally closes a dead Wait/Movie face.
    public static class Build126Tests
    {
        const string Out = "/tmp/flock-five-b126-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build126] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // TutorialHeal.Plan orphan cases.
            var orphan = new TutorSnapshot { PauseHeld = true, AdShowing = true, OrphanShowSeconds = 3.5f };
            Check("orphan ad hold past 3 s force-clears", (TutorialHeal.Plan(orphan) & TutorFix.ForceClearShow) != 0, "");
            orphan.OrphanShowSeconds = 2.9f;
            Check("orphan ad hold under 3 s waits", TutorialHeal.Plan(orphan) == TutorFix.None, "");
            var owned = new TutorSnapshot { PauseHeld = true, AdShowing = true, OrphanShowSeconds = 60f, WatchRunning = true };
            Check("running WatchGift owns the hold", TutorialHeal.Plan(owned) == TutorFix.None, "");
            owned = new TutorSnapshot { PauseHeld = true, AdShowing = true, OrphanShowSeconds = 60f, RewardInFlight = true };
            Check("reward in flight owns the hold", TutorialHeal.Plan(owned) == TutorFix.None, "");
            Check("no ad showing never force-clears", (TutorialHeal.Plan(new TutorSnapshot { OrphanShowSeconds = 99f }) & TutorFix.ForceClearShow) == 0, "");
            Check("ForceClearShow is bit 128, limit 3 s", (int)TutorFix.ForceClearShow == 128 && TutorialHeal.MaxOrphanShow == 3f, "");

            // Stage exit clears the pause.
            float scale0 = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                Ads.ForceClear();
                GamePause.Reset();
                Ads.BeginShow();
                Check("show pauses", GamePause.Paused && AdPause.Active && Time.timeScale == 0f, "");
                Ads.EndStage();
                Check("stage gap holds the pause", GamePause.Paused && Ads.StageGapOpen, "");
                Ads.AdvanceStageGap(Ads.StageGapSeconds + 0.1f);
                Check("stage exit clears pause", !GamePause.Paused && !AdPause.Active && !Ads.IsShowing && Time.timeScale == 1f, "scale " + Time.timeScale);
                Ads.BeginShow();
                Ads.ForceClear();
                Check("ForceClear clears pause and hold", !GamePause.Paused && !Ads.IsShowing && !AdAudioHold.Held, "");
                Ads.BeginShow();
                GamePause.Push();
                Ads.ForceClear();
                GamePause.Reset();
                Check("Reset clears a stolen depth", !GamePause.Paused && Time.timeScale == 1f, "");
                Check("idle Wait face offers Cancel after 3 s", ShowWaitedCheck(), "");
            }
            finally
            {
                Ads.ForceClear();
                GamePause.Reset();
                Time.timeScale = scale0;
                Ads.ShowWaited = -1f;
            }

            // WatchGift finally and the surrounding wiring (text check, Build111 style).
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            int fin = app.IndexOf("_watchGiftLive = false;");
            string tail = fin < 0 ? "" : app.Substring(fin, System.Math.Min(700, app.Length - fin));
            Check("WatchGift finally clears Wait/Movie", tail.Contains("_gift == GiftFace.Wait || _gift == GiftFace.Movie")
                && tail.Contains("_gift = GiftFace.None;") && tail.Contains("_busy = false;")
                && tail.Contains("_swallowTapsUntil = GateNow + 0.45f;") && tail.Contains("_suppressGiftUntil = GateNow + 1f;"), "");
            Check("gates on the ad-free unscaled clock", app.Contains("static float GateNow => AdPause.Unscaled;")
                && !app.Contains("PlayClock.Now < _swallowTapsUntil") && !app.Contains("PlayClock.Now >= _resumeInputUntil"), "");
            Check("heal applies ForceClearShow", coach.Contains("TutorFix.ForceClearShow") && coach.Contains("Ads.ForceClear();") && coach.Contains("GamePause.Reset();"), "");
            Check("Wait face spinner always", !app.Contains("if (Ads.IsLoading && !Ads.IsShowing && !Ads.MockRewarded)\n                    DrawAdSpinner"), "");
        }

        static bool ShowWaitedCheck()
        {
            Ads.ForceClear();
            Ads.ShowWaited = 3.1f;
            bool idle = Ads.CanCancelWait;
            Ads.BeginShow();
            bool up = Ads.CanCancelWait;
            Ads.ForceClear();
            Ads.ShowWaited = 10.1f;
            return idle && !up;
        }
    }
}
#endif

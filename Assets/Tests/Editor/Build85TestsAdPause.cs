#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 85: AdPause freezes the game under every ad.
    public static class Build85TestsAdPause
    {
        const string Out = "/tmp/flock-five-b85-adpause-tests.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                UnityEngine.Debug.Log("[build85ad] " + line);
            }
            float keepScale = Time.timeScale;
            bool keepAudio = AudioListener.pause;
            GameObject go = null;
            try
            {
                AdPause.ResetForTest();
                GamePause.Reset();
                Time.timeScale = 1f;

                // SkyCycle day/night across a simulated 40s ad (the cycle is 66s: an unfrozen ad
                // would carry daytime to night).
                go = new GameObject("b85-sky");
                var sky = go.AddComponent<SkyCycle>();
                typeof(SkyCycle).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).SetValue(null, sky);
                var t0 = typeof(SkyCycle).GetField("_t0", BindingFlags.NonPublic | BindingFlags.Instance);
                t0.SetValue(sky, PlayClock.Now - 12f);
                float duskBefore = SkyCycle.Dusk;
                float clockBefore = PlayClock.Now;
                Ads.BeginShow();
                Check("ad-open-timescale-0", Time.timeScale == 0f && AudioListener.pause && AdPause.Active, "timeScale " + Time.timeScale + " listener paused " + AudioListener.pause);
                // Something unbalanced pops the pause mid-ad (tutorial heal on background).
                GamePause.Pop();
                for (int i = 0; i < 400; i++) { AdPause.Hold(); PlayClock.StepForTest(0.1f); }
                Check("skycycle-frozen-across-ad", Mathf.Approximately(SkyCycle.Dusk, duskBefore) && Mathf.Approximately(PlayClock.Now, clockBefore),
                    "dusk " + duskBefore.ToString("0.000") + " -> " + SkyCycle.Dusk.ToString("0.000") + ", clock +" + (PlayClock.Now - clockBefore).ToString("0.00") + "s");
                Check("held-through-stolen-pop", Time.timeScale == 0f && AudioListener.pause, "timeScale stays 0, audio paused");
                Check("unscaled-delta-zero", AdPause.UnscaledDelta == 0f, "AdPause.UnscaledDelta during ad");
                Ads.EndShow();
                Check("ad-close-restored", !AdPause.Active && Time.timeScale == 1f && !AudioListener.pause, "timeScale " + Time.timeScale);
                PlayClock.StepForTest(0.25f);
                Check("clock-runs-after", Mathf.Abs(PlayClock.Now - clockBefore - 0.25f) < 1e-3f, "clock +" + (PlayClock.Now - clockBefore).ToString("0.000"));

                // Grant waits for restore: a Flush during the ad defers; EndShow applies it.
                Ads.BeginShow();
                bool deferred = !RewardedGrant.Flush();
                bool activeAtGrant = AdPause.Active;
                Ads.EndShow();
                Check("grant-after-restore", deferred && activeAtGrant && !AdPause.Active, "Flush during ad defers until EndShow");

                // Watchdog / fail path: ForceClear restores too.
                Ads.BeginShow();
                Ads.ForceClear();
                Check("forceclear-restores", !AdPause.Active && Time.timeScale == 1f, "watchdog/fail");

                // App resume while an ad is up re-holds the freeze.
                Ads.BeginShow();
                Time.timeScale = 1f;
                AdPause.OnAppPause(false);
                Check("app-resume-holds", Time.timeScale == 0f, "OnApplicationPause(false) during ad");
                Ads.ForceClear();
            }
            catch (System.Exception ex) { Check("adpause", false, ex.GetType().Name + " " + ex.Message); }
            finally
            {
                Ads.ForceClear();
                AdPause.ResetForTest();
                GamePause.Reset();
                Time.timeScale = keepScale;
                AudioListener.pause = keepAudio;
                if (go != null) Object.DestroyImmediate(go);
                typeof(SkyCycle).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).SetValue(null, null);
            }
            string end = fail == 0 ? "BUILD85AD_OK " + pass : "BUILD85AD_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            UnityEngine.Debug.Log("[build85ad] " + end);
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif

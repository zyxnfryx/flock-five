#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 85 blocker: fresh install after level 1, Daily visible under the adopt lesson,
    // a tap on it stranded the player. The Daily button must stay out of the rail until its
    // own lesson is live, whatever session level 1 was cleared in.
    public static class Build85TestsDaily
    {
        const string Out = "/tmp/flock-five-b85-daily-tests.txt";
        static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string n, bool ok, string d)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + n + "  " + d;
                if (ok) pass++; else fail++;
                sb.AppendLine(line); File.WriteAllText(Out, sb.ToString()); Debug.Log("[build85daily] " + line);
            }
            int keepNext = PlayerPrefs.GetInt("flockfive.next", 0);
            int keepCoach = PlayerPrefs.GetInt("flockfive.coach.daily", 0);
            int keepAdopt = PlayerPrefs.GetInt("flockfive.avatar.adopted", 0);
            var go = new GameObject("b85daily");
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var t = typeof(FlockFiveApp);
                void Set(string f, object v) => t.GetField(f, F).SetValue(app, v);
                float Goal() => (float)t.GetMethod("SplashDailyGoal", F).Invoke(app, null);
                PlayerPrefs.SetInt("flockfive.next", 1);
                PlayerPrefs.SetInt("flockfive.coach.daily", 0);

                // Relaunch after level 1 (the rail lock was never set this session).
                Set("_dailyRailLocked", false);
                Set("_dailyIntro", true); Set("_dailyIntroLive", false);
                Set("_adoptOwed", true); Set("_adoptLive", true);
                Check("card-end-hold", FlockFiveApp.CardEndHoldSec >= 0.45f && FlockFiveApp.CardEndHoldSec <= 0.8f, "hold " + FlockFiveApp.CardEndHoldSec);
                Check("daily-hidden-under-adopt", Goal() == 0f, "goal " + Goal());
                Set("_adoptLive", false); Set("_adoptOwed", false);
                Check("daily-hidden-until-its-lesson", Goal() == 0f, "goal " + Goal());
                Set("_dailyIntroLive", true);
                Check("daily-shown-for-its-lesson", Goal() == 1f, "goal " + Goal());
                Set("_dailyIntro", false); Set("_dailyIntroLive", false);
                PlayerPrefs.SetInt("flockfive.coach.daily", 1);
                Check("daily-shown-after-lesson", Goal() == 1f, "goal " + Goal());
                // Same session unlock (the old path) still hides it.
                PlayerPrefs.SetInt("flockfive.coach.daily", 0);
                Set("_dailyRailLocked", true); Set("_dailyIntro", true);
                Check("daily-hidden-same-session", Goal() == 0f, "goal " + Goal());

                // Device sequence after naming: Daily lesson live, its glove on the gift. The
                // gift tap opens the card even while a tutorial popup gate is up, and the claim
                // step (wheel intro) is next.
                Set("_dailyRailLocked", false);
                Set("_splash", true);
                t.GetField("_home", F).SetValue(app, System.Enum.Parse(t.GetField("_home", F).FieldType, "Splash"));
                Set("_dailyIntro", true); Set("_dailyIntroLive", true); Set("_dailyOpen", false);
                Set("_albumTutorOn", true); // a stray tutorial flag that used to gate the open
                try { t.GetMethod("OpenDailyCard", F).Invoke(app, null); }
                catch (TargetInvocationException e) when (e.InnerException is System.InvalidOperationException) { } // Sfx needs play mode; the open is already set
                bool open = (bool)t.GetField("_dailyOpen", F).GetValue(app);
                int step = (int)t.GetMethod("LiveCoachStep", F).Invoke(app, null);
                Check("daily-lesson-tap-opens-card", open, "open=" + open);
                Check("daily-claim-step-next", step == (int)CoachStep.DailyClaim, "step " + (CoachStep)step);
                // ROOT CAUSE regression: the press fades the glove; the veil hole must stay on the
                // gift for the release, or the Consent gate eats it (build 84 strand).
                Set("_albumTutorOn", false); Set("_dailyOpen", false);
                var tap = new Rect(4f, 4f, 40f, 40f);
                Set("_gloveTapRect", tap); Set("_gloveTapRectOn", true); Set("_gloveTapFrame", Time.frameCount);
                bool Veil(Rect r) => (bool)t.GetMethod("VeilBlocks", F).Invoke(app, new object[] { r });
                bool openHole = !Veil(tap);
                Set("_gloveTapRectOn", false); Set("_gloveTapFrame", -1); // glove faded on the press
                bool keptHole = !Veil(tap);
                bool elsewhere = Veil(new Rect(Screen.width * 0.5f, Screen.height * 0.5f, 20f, 20f));
                Check("daily-veil-hole-survives-glove-fade", openHole && keptHole && elsewhere,
                    "hole " + openHole + " after fade " + keptHole + " rest veiled " + elsewhere);
                Set("_dailyIntro", false); Set("_dailyIntroLive", false);

                // Kit seat: one seat for every flying flap frame (no jerk with the wings).
                float Jerk(bool stable, out string w)
                {
                    BirdIdle.KitStableSeat = stable; float worstD = 0f; w = "";
                    foreach (BirdColor c in System.Enum.GetValues(typeof(BirdColor)))
                    foreach (var kk in new[] { BirdKit.Crown, BirdKit.Bow })
                    for (int side = 0; side < 2; side++)
                    {
                        float x1 = 0f, y1 = 0f; bool have = false;
                        for (float ft = 0f; ft < 1.2f; ft += 0.017f)
                        {
                            SpriteCatalog.FlapPair(c, BirdSex.Neutral, ft, true, out var body, out _, out _);
                            var kit = SpriteCatalog.KitSprite(c, kk);
                            if (body == null || kit == null) continue;
                            BirdIdle.KitAnchor(kk, 0, side == 1, body, SpriteCatalog.Bird(c), kit, out float x, out float y, out _, out _, out _);
                            if (!have) { x1 = x; y1 = y; have = true; continue; }
                            float d = Mathf.Max(Mathf.Abs(x - x1), Mathf.Abs(y - y1));
                            if (d > worstD) { worstD = d; w = c + " " + kk + (side == 1 ? " L" : " R"); }
                        }
                    }
                    BirdIdle.KitStableSeat = true;
                    return worstD;
                }
                float before = Jerk(false, out string wb), after = Jerk(true, out string wa);
                // 0.04 local units is ~1% of the bird (the _2 canvas is 32 px larger).
                Check("kit-steady-across-flaps", after <= 0.04f && after < before * 0.5f,
                    "max jump " + after.ToString("0.000") + " (" + wa + ") was " + before.ToString("0.000") + " (" + wb + ")");
            }
            catch (System.Exception ex) { var ie = ex.InnerException ?? ex; Check("daily", false, ie.GetType().Name + " " + ie.Message + " " + ie.StackTrace); }
            finally
            {
                Object.DestroyImmediate(go);
                PlayerPrefs.SetInt("flockfive.next", keepNext);
                PlayerPrefs.SetInt("flockfive.coach.daily", keepCoach);
                PlayerPrefs.SetInt("flockfive.avatar.adopted", keepAdopt);
                PlayerPrefs.Save();
            }
            string end = fail == 0 ? "BUILD85DAILY_OK " + pass : "BUILD85DAILY_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end); File.WriteAllText(Out, sb.ToString()); Debug.Log("[build85daily] " + end);
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif

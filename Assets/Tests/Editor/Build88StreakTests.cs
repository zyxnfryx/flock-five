#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // The keep-multiplier card has no caution triangle. The shared badge
    // breathes and cracks while the streak is at risk, and after the ad
    // closes the keep lands and the badge seals, sweeps, and pops.
    public static class Build88StreakTests
    {
        const string Out = "/tmp/flock-five-b88-streak-tests.txt";
        const string StreakPref = "flockfive.streak";

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
                Debug.Log("[build88streak] " + line);
            }

            float keepScale = Time.timeScale;
            bool keepAudio = AudioListener.pause;
            var prevApply = RewardedGrant.TryApply;
            bool hadStreak = PlayerPrefs.HasKey(StreakPref);
            int streakWas = PlayerPrefs.GetInt(StreakPref, 0);
            bool hadPend = PlayerPrefs.HasKey(RewardedGrant.Pref);
            string pend = PlayerPrefs.GetString(RewardedGrant.Pref, "");
            bool hadPendX = PlayerPrefs.HasKey(RewardedGrant.Pref + ".x");
            string pendX = PlayerPrefs.GetString(RewardedGrant.Pref + ".x", "");
            try
            {
                CheckLook(Check);
                CheckMotion(Check);
                CheckGrant(Check);
            }
            catch (System.Exception ex)
            {
                Check("streak", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                Ads.ForceClear();
                AdPause.ResetForTest();
                GamePause.Reset();
                Time.timeScale = keepScale;
                AudioListener.pause = keepAudio;
                RewardedGrant.TryApply = prevApply;
                RewardedGrant.ResetEmpty();
                if (hadPend) PlayerPrefs.SetString(RewardedGrant.Pref, pend);
                else PlayerPrefs.DeleteKey(RewardedGrant.Pref);
                if (hadPendX) PlayerPrefs.SetString(RewardedGrant.Pref + ".x", pendX);
                else PlayerPrefs.DeleteKey(RewardedGrant.Pref + ".x");
                if (hadStreak) Purse.SetStreak(streakWas);
                else
                {
                    Purse.BreakStreak();
                    PlayerPrefs.DeleteKey(StreakPref);
                    PlayerPrefs.DeleteKey(StreakPref + ".x");
                }
                PlayerPrefs.Save();
            }

            string end = fail == 0 ? "BUILD88STREAK_OK " + pass : "BUILD88STREAK_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88streak] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void CheckLook(System.Action<string, bool, string> Check)
        {
            string root = Application.dataPath;
            string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
            string place = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Place.cs"));
            string badge = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/StreakBadge.cs"));
            string rewards = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Rewards.cs"));
            int caution = app.IndexOf("void DrawStreakCaution(");
            int gift = app.IndexOf("void DrawGiftOffer(");
            string body = caution >= 0 && gift > caution ? app.Substring(caution, gift - caution) : "";
            bool noIcon = body.IndexOf("DrawCautionMark") < 0
                && body.IndexOf("CautionMark") < 0
                && app.IndexOf("CautionMark") < 0
                && place.IndexOf("CautionMark") < 0
                && badge.IndexOf("CautionMark") < 0;
            Check("no-caution-icon", noIcon && body.IndexOf("StreakBadge.AtRisk") >= 0
                && body.IndexOf("StreakBadge.Restored") >= 0,
                "caution " + (body.IndexOf("CautionMark") >= 0) + " at-risk " + (body.IndexOf("AtRisk") >= 0));

            int garden = app.IndexOf("void DrawGardenStamp(");
            int restart = app.IndexOf("void DrawRestartAsk(");
            string gardenBody = garden >= 0 && restart > garden ? app.Substring(garden, restart - garden) : "";
            Check("garden-badge-stays-calm",
                gardenBody.IndexOf("AtRisk") < 0 && gardenBody.IndexOf("Restored") < 0
                && gardenBody.IndexOf("DrawRewardStamp") >= 0,
                "garden");

            StreakBadge.CrackStats(out float frac, out int run);
            bool hair = frac > 0.012f && frac < 0.09f && run >= 2 && run <= 14;
            Check("hairline-crack", hair, "frac " + frac.ToString("0.000") + " run " + run);

            int watch = app.IndexOf("IEnumerator WatchKeepStreak()");
            int yieldAd = watch >= 0 ? app.IndexOf("yield return Ads.Rewarded", watch) : -1;
            int heal = watch >= 0 ? app.IndexOf("BeginStreakHeal()", watch) : -1;
            int apply = rewards.IndexOf("bool ApplyKeepTicket(");
            int repeat = rewards.IndexOf("bool ApplyRepeatTicket(");
            string ticket = apply >= 0 && repeat > apply ? rewards.Substring(apply, repeat - apply) : "";
            Check("heal-follows-grant",
                yieldAd > watch && heal > yieldAd
                && ticket.IndexOf("BeginStreakHeal()") >= 0
                && ticket.IndexOf("RestartAsk.Keep") >= 0,
                "yield " + yieldAd + " heal " + heal);
        }

        static void CheckMotion(System.Action<string, bool, string> Check)
        {
            var calm = StreakBadge.Risk.Calm;
            var a = StreakBadge.AtRisk(0.20f);
            var b = StreakBadge.AtRisk(0.20f + Mathf.PI / 4.6f);
            bool anim = Mathf.Abs(a.Scale - b.Scale) > 0.01f
                && Mathf.Abs(a.Roll - b.Roll) > 0.4f
                && a.Crack > 0.55f && b.Crack > 0.55f
                && a.Sheen < 0.01f && a.Glow < 0.01f;
            Check("at-risk-animates", anim && Mathf.Abs(calm.Scale - 1f) < 0.001f && calm.Crack < 0.001f,
                "scale " + a.Scale.ToString("0.000") + "->" + b.Scale.ToString("0.000")
                + " roll " + a.Roll.ToString("0.00") + "->" + b.Roll.ToString("0.00")
                + " crack " + a.Crack.ToString("0.00"));

            float tHeal = -1f, tSheen = -1f, tGlow = -1f;
            float mendAtHeal = 0f;
            var end = StreakBadge.Restored(StreakBadge.RestoreSeconds);
            for (int i = 0; i <= 180; i++)
            {
                float t = i / 100f;
                var r = StreakBadge.Restored(t);
                if (tHeal < 0f && r.Crack < 0.15f && r.Sheen < 0.15f && r.Glow < 0.1f)
                {
                    tHeal = t;
                    mendAtHeal = r.Mend;
                }
                if (tHeal >= 0f && tSheen < 0f && t > tHeal && r.Sheen > 0.7f && r.Glow < 0.15f)
                    tSheen = t;
                if (tSheen >= 0f && tGlow < 0f && t > tSheen && r.Glow > 0.7f && r.Crack < 0.05f)
                    tGlow = t;
            }
            var mid = StreakBadge.Restored(0.29f);
            bool order = tHeal >= 0f && tSheen > tHeal && tGlow > tSheen
                && mid.Mend > 0.7f && mid.Crack > 0.25f && mid.Sheen < 0.05f
                && end.Crack < 0.01f && end.Sheen < 0.01f && end.Glow < 0.01f
                && Mathf.Abs(end.Scale - 1f) < 0.001f;
            Check("heal-sheen-pop", order,
                "heal " + tHeal.ToString("0.00") + " sheen " + tSheen.ToString("0.00")
                + " glow " + tGlow.ToString("0.00") + " mend " + mendAtHeal.ToString("0.00")
                + " mid " + mid.Crack.ToString("0.00") + "/" + mid.Mend.ToString("0.00"));
        }

        static void CheckGrant(System.Action<string, bool, string> Check)
        {
            Ads.ForceClear();
            AdPause.ResetForTest();
            GamePause.Reset();
            Time.timeScale = 1f;
            RewardedGrant.ResetEmpty();
            RewardedGrant.TryApply = t =>
                t.Place == RewardedGrant.Place.Keep && RewardEffects.Keep(t.Payload);
            Purse.BreakStreak();
            RewardedGrant.Begin(RewardedGrant.Place.Keep, 3, false, 1);
            Ads.DeliverDisplayedCallback();
            Ads.BeginShow();
            Ads.DeliverRewardedCallback();
            int whileUp = RewardedGrant.ApplyCount;
            bool held = Purse.Streak == 0 && whileUp == 0 && RewardedGrant.Earned;
            Ads.DeliverClosedCallback();
            bool stillHeld = Purse.Streak == 0 && RewardedGrant.ApplyCount == 0 && AdPause.Active;
            Ads.EndShow();
            bool granted = Purse.Streak == 3 && RewardedGrant.ApplyCount == 1 && !AdPause.Active;
            Ads.DeliverRewardedCallback();
            RewardedGrant.Flush();
            bool once = Purse.Streak == 3 && RewardedGrant.ApplyCount == 1;
            Check("keep-after-ad-close", held && stillHeld && granted && once,
                "whileUp " + whileUp + " streak " + Purse.Streak + " apply " + RewardedGrant.ApplyCount
                + " held " + held + " closed " + stillHeld + " granted " + granted);
        }
    }
}
#endif

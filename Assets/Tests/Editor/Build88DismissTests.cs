#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FlockFive.Editor
{
    // Daily card stays up through the coin flight and the balance count, then
    // eases out with PopupMotion.ExitScale / ExitAlpha. The pay is redrawn
    // above the 0.98 scrim.
    public static class Build88DismissTests
    {
        const string Out = "/tmp/flock-five-b88-dismiss-tests.txt";
        static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string n, bool ok, string d)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + n + "  " + d;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build88dismiss] " + line);
            }
            try
            {
                FlockFiveApp.DailyExitMotion(0f, out float s0, out float a0);
                FlockFiveApp.DailyExitMotion(FlockFiveApp.DailyCloseDur * 0.5f, out float sMid, out float aMid);
                FlockFiveApp.DailyExitMotion(FlockFiveApp.DailyCloseDur, out float s1, out float a1);
                Check("exit-starts-open",
                    Mathf.Abs(s0 - 1f) < 0.001f && a0 > 0.98f,
                    "s " + s0.ToString("0.000") + " a " + a0.ToString("0.000"));
                Check("exit-eases",
                    sMid < s0 && sMid > s1 && aMid < a0 && aMid > a1
                    && Mathf.Abs(s1 - PopupMotion.ExitTo) < 0.001f && a1 < 0.02f,
                    "mid " + sMid.ToString("0.000") + "/" + aMid.ToString("0.000")
                    + " end " + s1.ToString("0.000") + "/" + a1.ToString("0.000"));

                string root = Application.dataPath;
                string daily = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.DailyBonus.cs"));
                string rewards = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Rewards.cs"));
                int paint = daily.IndexOf("ModalBlock.Paint(ModalBlock.Id.Daily");
                int over = daily.IndexOf("DrawDailyPayOverScrim(");
                int grant = daily.IndexOf("void ResolveDailyGrant(");
                int grantEnd = daily.IndexOf("public static void DailyExitMotion(", grant);
                string grantBody = grant >= 0 && grantEnd > grant ? daily.Substring(grant, grantEnd - grant) : "";
                int close = rewards.IndexOf("void CloseDailyAfterReward(");
                int closeEnd = rewards.IndexOf("}", close + 10);
                string closeBody = close >= 0 && closeEnd > close ? rewards.Substring(close, closeEnd - close) : "";
                Check("pay-redraws-above-scrim",
                    paint >= 0 && over > paint && daily.IndexOf("DailyExitMotion(") > 0
                    && daily.IndexOf("PopupMotion.ExitScale") > 0
                    && daily.IndexOf("PopupMotion.ExitAlpha") > 0,
                    "paint " + paint + " over " + over);
                Check("grant-holds-the-card",
                    grantBody.IndexOf("BeginRewardPay(coins)") > 0
                    && grantBody.IndexOf("_dailyPayHold = true") > grantBody.IndexOf("BeginRewardPay(coins)")
                    && grantBody.IndexOf("_dailyOpen = false") < 0,
                    "hold");
                Check("double-pay-holds-the-card",
                    closeBody.IndexOf("_dailyPayHold = true") > 0
                    && closeBody.IndexOf("_dailyOpen = false") < 0,
                    "close");

                int keepCoins = PlayerPrefs.GetInt("flockfive.coins", 0);
                int keepCoinsX = PlayerPrefs.GetInt("flockfive.coins.x", 0);
                bool hadCoins = PlayerPrefs.HasKey("flockfive.coins");
                bool hadCoinsX = PlayerPrefs.HasKey("flockfive.coins.x");
                int keepPay = PlayerPrefs.GetInt("flockfive.daily.pay", 0);
                int keepPayX = PlayerPrefs.GetInt("flockfive.daily.pay.x", 0);
                bool hadPay = PlayerPrefs.HasKey("flockfive.daily.pay");
                bool hadPayX = PlayerPrefs.HasKey("flockfive.daily.pay.x");
                void RestorePurse()
                {
                    if (hadCoins) PlayerPrefs.SetInt("flockfive.coins", keepCoins);
                    else PlayerPrefs.DeleteKey("flockfive.coins");
                    if (hadCoinsX) PlayerPrefs.SetInt("flockfive.coins.x", keepCoinsX);
                    else PlayerPrefs.DeleteKey("flockfive.coins.x");
                    if (hadPay) PlayerPrefs.SetInt("flockfive.daily.pay", keepPay);
                    else PlayerPrefs.DeleteKey("flockfive.daily.pay");
                    if (hadPayX) PlayerPrefs.SetInt("flockfive.daily.pay.x", keepPayX);
                    else PlayerPrefs.DeleteKey("flockfive.daily.pay.x");
                    PlayerPrefs.Save();
                    Purse.ReloadCoins();
                }
                DailyBonus.TestLand(500, false);
                var go = new GameObject("b88dismiss");
                try
                {
                    var app = go.AddComponent<FlockFiveApp>();
                    var t = typeof(FlockFiveApp);
                    void Set(string f, object v) => t.GetField(f, F).SetValue(app, v);
                    object Get(string f) => t.GetField(f, F).GetValue(app);
                    var phase = t.GetField("_dailyPhase", F);
                    Set("_dailyOpen", true);
                    phase.SetValue(app, System.Enum.ToObject(phase.FieldType, 2));
                    t.GetMethod("ResolveDailyGrant", F).Invoke(app, new object[] { false });
                    bool open = (bool)Get("_dailyOpen");
                    bool hold = (bool)Get("_dailyPayHold");
                    Check("grant-keeps-popup", open && hold, "open " + open + " hold " + hold + " coins " + DailyBonus.SpinCoins);

                    t.GetMethod("FinishRewardPay", F).Invoke(app, null);
                    Sfx.ClearPayout();
                    t.GetMethod("TickDailyDismiss", F).Invoke(app, null);
                    open = (bool)Get("_dailyOpen");
                    hold = (bool)Get("_dailyPayHold");
                    float closeAt = (float)Get("_dailyCloseAt");
                    Check("grant-starts-exit-after-pay",
                        open && !hold && closeAt >= 0f,
                        "open " + open + " hold " + hold + " at " + closeAt.ToString("0.00"));

                    // A young editor clock must still be able to finish the 0.48s beat.
                    var pauseT = typeof(AdPause);
                    var totalF = pauseT.GetField("_total", BindingFlags.NonPublic | BindingFlags.Static);
                    var onF = pauseT.GetField("_on", BindingFlags.NonPublic | BindingFlags.Static);
                    float savedTotal = (float)totalF.GetValue(null);
                    bool savedOn = (bool)onF.GetValue(null);
                    onF.SetValue(null, false);
                    totalF.SetValue(null, Time.unscaledTime - (closeAt + FlockFiveApp.DailyCloseDur + 0.05f));
                    try
                    {
                        t.GetMethod("TickDailyDismiss", F).Invoke(app, null);
                        open = (bool)Get("_dailyOpen");
                        Check("grant-closes-after-exit",
                            !open,
                            "open " + open + " now " + AdPause.Unscaled.ToString("0.00") + " at " + closeAt.ToString("0.00"));
                    }
                    finally
                    {
                        totalF.SetValue(null, savedTotal);
                        onF.SetValue(null, savedOn);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(go);
                    RestorePurse();
                }
            }
            catch (System.Exception ex)
            {
                var ie = ex;
                while (ie.InnerException != null) ie = ie.InnerException;
                Check("dismiss-threw", false, ie.GetType().Name + " " + ie.Message);
            }
            finally
            {
                DailyBonus.TestRelease();
                sb.AppendLine("TOTAL pass " + pass + " fail " + fail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build88dismiss] TOTAL pass " + pass + " fail " + fail);
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89 follow-up: every payout on the open pay table uses ONE font size, the largest
    // at which the widest real payout string fits the cash column (same for the multiplier
    // column), per device and per bet. Real strings: BirdPoker multipliers x bets through
    // Money.Format, up to the longest ($250K, $62.5K, $25M, $250M).
    [InitializeOnLoad]
    static class Build89PayoutTests
    {
        const string Cmd = "/tmp/flock-five-b89-payout-tests";
        const string Out = "/tmp/flock-five-b89-payout-tests.txt";

        static Build89PayoutTests() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (!File.Exists(Cmd) || EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); } catch { return; }
            Run();
        }

        static int FitOne(GUIStyle st, string text, float maxW, int lo, int hi)
        {
            int best = lo;
            for (int px = lo; px <= hi; px++)
            {
                st.fontSize = px;
                if (st.CalcSize(new GUIContent(text)).x <= maxW) best = px; else break;
            }
            return best;
        }

        [MenuItem("Flock Five/Build 89 Payout Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string info = "")
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + (info.Length > 0 ? "  (" + info + ")" : ""));
            }

            var st = FlockFiveApp.CardText.MeasureStyle();
            st.wordWrap = false;
            st.fontStyle = FontStyle.Bold;
            var rows = BirdPoker.PayTableRows;
            Check("pay table fits the 8 cached row slots", rows.Length <= 8, "rows " + rows.Length);

            var devs = new[]
            {
                new { tag = "iphone", w = 1179, h = 2556 },
                new { tag = "ipad", w = 1536, h = 2048 },
                new { tag = "android", w = 1080, h = 2400 },
            };
            int[] bets = { 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000, 10000, 25000, 100000, 1000000 };
            foreach (var d in devs)
            {
                float s = DeviceFit.Ui(d.w, d.h);
                float paperW = d.w * 0.94f - 14f * s;
                var cols = PayHeadLayout.Columns(paperW - 16f * s);
                foreach (int bet in bets)
                {
                    var cash = new string[rows.Length];
                    var mult = new string[rows.Length];
                    for (int i = 0; i < rows.Length; i++)
                    {
                        int m = BirdPoker.Multiplier(rows[i]);
                        mult[i] = m + "\u00d7";
                        cash[i] = Money.Format((long)m * bet);
                    }
                    foreach (float rowH in new[] { 100f * s, 140f * s })
                    {
                        int cap = Mathf.RoundToInt(Mathf.Clamp(rowH * 0.46f, 22f, 40f));
                        float cashMax = cols.CashW - PayHeadLayout.CashMargin;
                        float multMax = cols.MultW - PayHeadLayout.MultMargin;
                        int common = PayHeadLayout.CommonPx(cash, cash.Length, t => FitOne(st, t, cashMax, 11, cap));
                        int mcommon = PayHeadLayout.CommonPx(mult, mult.Length, t => FitOne(st, t, multMax, 11, cap));
                        string k = d.tag + " bet " + bet + " cap " + cap;
                        // Every row at the ONE size fits; the size is the most any row allows.
                        st.fontSize = common;
                        float widest = 0f; string widestTx = "";
                        foreach (var t in cash) { float w = st.CalcSize(new GUIContent(t)).x; if (w > widest) { widest = w; widestTx = t; } }
                        Check(k + " all payouts share one size and fit", common >= 11 && (widest <= cashMax || common == 11),
                            "px " + common + " widest " + widestTx + " " + widest.ToString("0.0") + "/" + cashMax.ToString("0.0"));
                        if (common < cap)
                        {
                            st.fontSize = common + 1;
                            float over = 0f;
                            foreach (var t in cash) over = Mathf.Max(over, st.CalcSize(new GUIContent(t)).x);
                            Check(k + " payout size is the largest that fits", over > cashMax, "px+1 widest " + over.ToString("0.0"));
                        }
                        st.fontSize = mcommon;
                        float mw = 0f;
                        foreach (var t in mult) mw = Mathf.Max(mw, st.CalcSize(new GUIContent(t)).x);
                        Check(k + " multipliers share one size and fit", mcommon >= 11 && (mw <= multMax || mcommon == 11),
                            "px " + mcommon + " widest " + mw.ToString("0.0") + "/" + multMax.ToString("0.0"));
                        // Per-row sizes (the old behaviour) differ only when rows squeeze apart.
                        int lo = int.MaxValue, hi = 0;
                        foreach (var t in cash) { int f = FitOne(st, t, cashMax, 11, cap); lo = Mathf.Min(lo, f); hi = Mathf.Max(hi, f); }
                        Check(k + " common size equals the narrowest row's fit", common == lo, "rows " + lo + ".." + hi);
                    }
                }
                Check(d.tag + " columns add up", Mathf.Abs(cols.NameW + cols.MultW + cols.CashW - cols.InnerW) < 0.01f);
            }

            sb.Insert(0, "Build89PayoutTests pass " + pass + " fail " + fail + "\n");
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[b89-payout-tests] pass " + pass + " fail " + fail);
        }
    }
}
#endif

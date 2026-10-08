#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Every drip on a comb has its own length and its own width. The gap stays
    // above 15% for many seeds, and the same seed repeats.
    static class HoneyDripTests
    {
        [MenuItem("Flock Five/Honey Drip Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[honey-drip] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var buf = new HoneyArt.DripSize[8];
            var again = new HoneyArt.DripSize[8];
            var mid = new HoneyArt.DripSize[8];
            const float gap = 0.15f;
            const int seeds = 48;
            int bad = 0;
            int pairs = 0;
            float worst = 1f;
            string first = "";
            int countBad = 0;
            for (int s = 0; s < seeds; s++)
            {
                int seed = s == seeds - 1 ? int.MaxValue : s * 17 - 3;
                int n = HoneyArt.CombDripSizes(seed, HoneyArt.DripCombCells, buf);
                HoneyArt.CombDripSizes(seed + 10007, HoneyArt.DripCombCells, mid);
                int n2 = HoneyArt.CombDripSizes(seed, HoneyArt.DripCombCells, again);
                if (n != 6 || n2 != n)
                {
                    countBad++;
                    if (first.Length == 0) first = "seed " + seed + " count " + n + "/" + n2;
                    continue;
                }
                for (int i = 0; i < n; i++)
                {
                    if (buf[i].Length != again[i].Length || buf[i].Width != again[i].Width)
                    {
                        bad++;
                        if (first.Length == 0) first = "seed " + seed + " unstable";
                    }
                    if (buf[i].Length <= 0f || buf[i].Width <= 0f)
                    {
                        bad++;
                        if (first.Length == 0) first = "seed " + seed + " non-positive";
                    }
                }
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        float ld = Rel(buf[i].Length, buf[j].Length);
                        float wd = Rel(buf[i].Width, buf[j].Width);
                        if (ld < worst) worst = ld;
                        if (wd < worst) worst = wd;
                        pairs++;
                        if (ld >= gap && wd >= gap) continue;
                        bad++;
                        if (first.Length == 0)
                            first = "seed " + seed + " " + i + " vs " + j
                                + " L " + buf[i].Length.ToString("0.000") + "/" + buf[j].Length.ToString("0.000")
                                + " W " + buf[i].Width.ToString("0.000") + "/" + buf[j].Width.ToString("0.000");
                    }
                }

                int n2cell = HoneyArt.CombDripSizes(seed, 2, mid);
                if (n2cell != 3)
                {
                    countBad++;
                    if (first.Length == 0) first = "seed " + seed + " two-cell count " + n2cell;
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (mid[i].Length == buf[i].Length && mid[i].Width == buf[i].Width) continue;
                        bad++;
                        if (first.Length == 0) first = "seed " + seed + " two-cell drifted";
                    }
                }
            }

            Check("comb sizes unique", bad == 0 && countBad == 0 && pairs > 100,
                "seeds=" + seeds + " pairs=" + pairs + " bad=" + bad + " countBad=" + countBad
                + " worst=" + worst.ToString("0.000") + (first.Length == 0 ? "" : " " + first));

            bool laid = LaidMatches(out string layDetail);
            Check("layout matches comb", laid, layDetail);

            Debug.Log("[honey-drip] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }

        static bool LaidMatches(out string detail)
        {
            var expect = new HoneyArt.DripSize[8];
            int n = HoneyArt.CombDripSizes(HoneyArt.DripGroupSeed, HoneyArt.DripCombCells, expect);
            float cellH = 220f;
            float cellW = cellH * (96f / 111f) * 0.78f;
            float bodyH = cellH * 0.78f;
            var stamps = new HoneyArt.Stamp[8];
            int di = 0;
            float worst = 0f;
            for (int i = 0; i < HoneyArt.DripCombCells; i++)
            {
                var hex = new Rect(i * (cellW + 8f), 0f, cellW, cellH);
                int sn = HoneyArt.LayoutCell(hex, Color.white, 0f, 1f, i, true, false, true, stamps);
                for (int s = 0; s < sn; s++)
                {
                    var tex = stamps[s].Tex;
                    if (tex == null || string.IsNullOrEmpty(tex.name) || !tex.name.StartsWith("HoneyDrip")) continue;
                    if (di >= n)
                    {
                        detail = "extra drip";
                        return false;
                    }
                    float len = stamps[s].Rect.height / bodyH;
                    float wid = stamps[s].Rect.width / bodyH;
                    float dl = Mathf.Abs(len - expect[di].Length);
                    float dw = Mathf.Abs(wid - expect[di].Width);
                    if (dl > worst) worst = dl;
                    if (dw > worst) worst = dw;
                    if (dl > 0.004f || dw > 0.004f)
                    {
                        detail = "cell " + i + " drip " + di
                            + " got " + len.ToString("0.000") + "x" + wid.ToString("0.000")
                            + " want " + expect[di].Length.ToString("0.000") + "x" + expect[di].Width.ToString("0.000");
                        return false;
                    }
                    di++;
                }
            }
            detail = "drips=" + di + " worst=" + worst.ToString("0.0000");
            return di == n && n == 6;
        }

        static float Rel(float a, float b)
        {
            float m = Mathf.Max(a, b);
            return m <= 0f ? 0f : Mathf.Abs(a - b) / m;
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b105: the garden restart/hive buttons never overlap the bottom ad branch or its arrow sign, and the poker
    // Pay Table tab stays smaller than before and clear of the logo halo, at iPhone sizes and ~0.56-aspect
    // (iPhone-compat window on an iPad) sizes. Base layout, not an iPad path.
    public static class Build105Tests
    {
        const string Out = "/tmp/flock-five-b105-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0, pass = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build105] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // w, h, safe top, safe bottom
            var devs = new[]
            {
                new Vector4(1179f, 2556f, 177f, 102f), new Vector4(1290f, 2796f, 204f, 102f), new Vector4(750f, 1334f, 40f, 0f),
                new Vector4(1080f, 2400f, 88f, 48f), new Vector4(1000f, 1786f, 24f, 20f), new Vector4(1180f, 2107f, 24f, 20f),
                new Vector4(1125f, 2000f, 24f, 20f), new Vector4(1536f, 2048f, 48f, 40f), new Vector4(2048f, 2732f, 48f, 40f)
            };
            foreach (var dv in devs)
            {
                float w = dv.x, h = dv.y, top = dv.z, bot = dv.w;
                string tag = w.ToString("0") + "x" + h.ToString("0") + " asp " + (w / h).ToString("0.00");
                DeviceFit.TestWindow = w / h;
                float s = DeviceFit.Ui(w, h);
                var safe = new Rect(0f, bot, w, h - bot - top);
                CornerAnchor.Garden(w, h, safe, out _, out _, out _, out var rs, out var hv);
                CornerAnchor.AdRowBand(w, h, out var sl, out var sr, out var bl, out var br, out _, out _);
                bool clear = !rs.Overlaps(sl) && !rs.Overlaps(sr) && !rs.Overlaps(bl) && !rs.Overlaps(br)
                    && !hv.Overlaps(sl) && !hv.Overlaps(sr) && !hv.Overlaps(bl) && !hv.Overlaps(br);
                bool inside = rs.yMin >= 0f && rs.yMax <= h - bot + 0.01f && hv.yMax <= h - bot + 0.01f
                    && rs.xMin >= 0f && hv.xMax <= w + 0.01f && Mathf.Approximately(rs.y, hv.y) && rs.width >= 40f;
                Check("restart/hive clear of ad branch " + tag, clear && inside,
                    "restart " + rs.xMin.ToString("0") + "," + rs.yMin.ToString("0") + " " + rs.width.ToString("0") + "px; sign L y "
                    + sl.yMin.ToString("0") + ".." + sl.yMax.ToString("0") + " branch y " + bl.yMin.ToString("0") + ".." + bl.yMax.ToString("0"));

                var chrome = FlockFiveApp.LayoutPokerChrome(w, h, safe, s);
                var tab = FlockFiveApp.PokerPayTabBox(w, safe, chrome.Below, s);
                bool tabOk = !tab.Overlaps(chrome.Title) && !tab.Overlaps(chrome.Halo) && tab.yMin >= chrome.Halo.yMax
                    && tab.xMax <= w && tab.xMin >= 0f && tab.width <= w * 0.24f;
                Check("pay tab clear of logo " + tag, tabOk,
                    "tab " + tab.xMin.ToString("0") + "," + tab.yMin.ToString("0") + " " + tab.width.ToString("0") + "x" + tab.height.ToString("0")
                    + " halo bottom " + chrome.Halo.yMax.ToString("0") + " w% " + (tab.width / w * 100f).ToString("0.0"));
            }
            DeviceFit.TestWindow = null;
            Debug.Log("[build105] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif

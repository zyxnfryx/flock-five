#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b131 (Brandon): the garden Restart button lives in the top bar. Its hit rect never intersects any
    // branch / bird / perch-pad hit reach (CornerAnchor.PlayfieldTopGui), never the bottom ad row, stays inside
    // the safe area and keeps a 44 pt tap target, on tall and short iPhones and the iPad phone-aspect area.
    // Batch: -executeMethod FlockFive.Editor.Build131ResetTests.Run
    public static class Build131ResetTests
    {
        const string Out = "/tmp/flock-five-b131-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0, pass = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build131] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // w, h, safe top, safe bottom. Tall iPhones, short (SE / 9:16), Android tall, and the iPad
            // phone-aspect (9:16) area at 11" and 13" heights.
            var devs = new[]
            {
                new Vector4(1179f, 2556f, 177f, 102f), new Vector4(1290f, 2796f, 204f, 102f), new Vector4(1170f, 2532f, 141f, 102f),
                new Vector4(750f, 1334f, 40f, 0f), new Vector4(640f, 1136f, 40f, 0f), new Vector4(1080f, 2400f, 88f, 48f),
                new Vector4(1328f, 2360f, 48f, 40f), new Vector4(1536f, 2732f, 48f, 40f), new Vector4(1159f, 2064f, 48f, 40f)
            };
            foreach (var dv in devs)
            {
                float w = dv.x, h = dv.y, top = dv.z, bot = dv.w;
                string tag = w.ToString("0") + "x" + h.ToString("0") + " asp " + (w / h).ToString("0.00");
                DeviceFit.TestWindow = w / h;
                var safe = new Rect(0f, bot, w, h - bot - top);
                CornerAnchor.Garden(w, h, safe, out _, out float hudTop, out _, out var rs, out var hv);
                float field = CornerAnchor.PlayfieldTopGui(w, h);
                CornerAnchor.AdRowBand(w, h, out var sl, out var sr, out var bl, out var br, out _, out _);
                bool offField = rs.yMax <= field && rs.yMax <= hudTop;
                bool offAd = !rs.Overlaps(sl) && !rs.Overlaps(sr) && !rs.Overlaps(bl) && !rs.Overlaps(br);
                bool inSafe = rs.yMin >= h - safe.yMax - 0.01f && rs.xMin >= safe.xMin && rs.xMax <= safe.xMax + 0.01f;
                bool bigEnough = rs.width >= CornerAnchor.MinTapPx(0f) && rs.height >= CornerAnchor.MinTapPx(0f);
                bool offHive = !rs.Overlaps(hv);
                Check("restart off playfield " + tag, offField && offAd && inSafe && bigEnough && offHive,
                    "restart " + rs.xMin.ToString("0") + "," + rs.yMin.ToString("0") + " " + rs.width.ToString("0") + "px yMax " + rs.yMax.ToString("0")
                    + " fieldTop " + field.ToString("0") + " hudTop " + hudTop.ToString("0"));
            }
            DeviceFit.TestWindow = null;
            Debug.Log("[build131] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif

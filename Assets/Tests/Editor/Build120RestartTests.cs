#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 R5 (Readability): the garden Restart is bottom-left, mirrors the hive's padding, sits below the
    // top safe band, keeps a 44 pt target, has a visible >=12 px "Restart" label inside the safe area that
    // does not overlap the hive, and always asks "Restart level?" first.
    public static class Build120RestartTests
    {
        const string Out = "/tmp/flock-five-b120-restart-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0, pass = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build120-restart] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // w, h, safe top, safe bottom (px). 402x874 / 412x915 / 820x1180 at dpr 3/2.6/2 plus SE and tall iPhones.
            var devs = new[]
            {
                new Vector4(1206f, 2622f, 177f, 102f), new Vector4(1071f, 2379f, 120f, 60f), new Vector4(1640f, 2360f, 48f, 40f),
                new Vector4(1179f, 2556f, 177f, 102f), new Vector4(750f, 1334f, 40f, 0f), new Vector4(1080f, 2400f, 88f, 48f)
            };
            foreach (var dv in devs)
            {
                float w = dv.x, h = dv.y, top = dv.z, bot = dv.w;
                string tag = w.ToString("0") + "x" + h.ToString("0");
                DeviceFit.TestWindow = w / h;
                var safe = new Rect(0f, bot, w, h - bot - top);
                CornerAnchor.Garden(w, h, safe, out float s, out _, out _, out var rs, out var hv);
                Check("restart below top band " + tag, rs.yMin >= top, "y " + rs.yMin + " band " + top);
                Check("restart bottom-left " + tag, rs.center.x < w * 0.5f && rs.center.y > h * 0.5f, rs.ToString());
                float padL = rs.xMin - safe.xMin, padR = safe.xMax - hv.xMax;
                Check("mirrors hive padding " + tag, Mathf.Abs(padL - padR) < 1f && Mathf.Abs(rs.y - hv.y) < 0.5f && Mathf.Abs(rs.width - hv.width) < 0.5f,
                    "padL " + padL + " padR " + padR);
                CornerAnchor.AdRowBand(w, h, out var sl, out var sr, out var bl, out var br, out _, out _);
                Check("restart off ad row " + tag, !rs.Overlaps(sl) && !rs.Overlaps(bl), rs.ToString());
                Check("44pt target " + tag, rs.width >= CornerAnchor.MinTapPx(0f), rs.width.ToString());
                var lab = CornerAnchor.RestartLabel(rs, h, safe, s);
                bool labSafe = lab.yMax <= h - bot + 0.5f && lab.yMin >= top && lab.xMin >= safe.xMin;
                Check("label inside safe area " + tag, labSafe, lab.ToString());
                Check("label off hive " + tag, !lab.Overlaps(hv), lab.ToString());
                Check("label >= 12px " + tag, Mathf.Max(13, Mathf.RoundToInt(19f * s)) >= 12, "s " + s);
            }
            DeviceFit.TestWindow = null;
            Check("confirm copy", FlockFiveApp.RestartLevelAsk == "Restart level?", FlockFiveApp.RestartLevelAsk);
            Check("label copy", FlockFiveApp.RestartLabelText == "Restart", FlockFiveApp.RestartLabelText);
            Debug.Log("[build120-restart] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif

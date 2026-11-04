#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 R8 (Readability): the garden lesson captions ("Tap a branch...", "Tap a matching or empty branch.")
    // end above the home-indicator band, including where the platform reports no bottom inset.
    public static class Build120CaptionInsetTests
    {
        const string Out = "/tmp/flock-five-b120-caption-inset-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0, pass = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build120-inset] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // w, h (px), reported safe top/bottom (px). 402/412/820 at dpr 2 with 0 reported (WebGL) and real insets.
            var devs = new[]
            {
                new Vector4(804f, 1748f, 0f, 0f), new Vector4(824f, 1830f, 0f, 0f), new Vector4(1640f, 2360f, 0f, 0f),
                new Vector4(1206f, 2622f, 177f, 102f), new Vector4(1640f, 2360f, 48f, 40f), new Vector4(1080f, 2400f, 88f, 48f)
            };
            foreach (var dv in devs)
            {
                float w = dv.x, h = dv.y;
                DeviceFit.TestWindow = w / h;
                var safe = new Rect(0f, dv.w, w, h - dv.w - dv.z);
                float s = DeviceFit.Ui(w, h);
                string tag = w.ToString("0") + "x" + h.ToString("0") + " safeBot " + dv.w;
                // Home band in this window: 34 CSS px at the screen's own scale (inspector bands), or the reported inset.
                float band = Mathf.Max(dv.w, CaptionLayout.HomeBandFloor(w, h));
                Check("inset >= reported " + tag, CaptionLayout.BottomInset(w, h, safe) >= dv.w, CaptionLayout.BottomInset(w, h, safe).ToString());
                Check("inset >= 34pt band " + tag, CaptionLayout.BottomInset(w, h, safe) >= h * 34f / 1180f - 0.5f, band.ToString());
                float refH = FlockFiveApp.GardenRefBandH(w, h, safe, s, 0f);
                float y = FlockFiveApp.GardenFixedSeatY(w, h, safe, s, refH);
                Check("garden caption above home band " + tag, y + refH <= h - band + 0.5f, "bottom " + (y + refH) + " limit " + (h - band));
            }
            DeviceFit.TestWindow = null;
            Debug.Log("[build120-inset] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif

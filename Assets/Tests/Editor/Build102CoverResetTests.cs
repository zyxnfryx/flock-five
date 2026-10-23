#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b102 (Brandon): the lifted bottom lesson banner must FULLY cover the garden restart button on every device,
    // measured against the real CornerAnchor restart rect, and still stay below the glove-target rows.
    public static class Build102CoverResetTests
    {
        const string Out = "/tmp/flock-five-b102-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build102] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            var devs = new[] { new Vector2(1179f, 2556f), new Vector2(1290f, 2796f), new Vector2(1536f, 2048f), new Vector2(2048f, 2732f), new Vector2(750f, 1334f), new Vector2(1080f, 2400f) };
            string[] tags = { "iphone", "tall-iphone", "ipad4:3", "ipad-pro", "se", "android" };
            for (int d = 0; d < devs.Length; d++)
            {
                float w = devs[d].x, h = devs[d].y;
                DeviceFit.TestWindow = w / h;
                float s = DeviceFit.Ui(w, h);
                // iPhones carry a bottom home-indicator inset, iPads a smaller one; SE none.
                float bot = d == 4 ? 0f : (w > h * 0.7f ? h * 0.02f : h * 0.04f);
                var safe = new Rect(0f, bot, w, h - bot - h * 0.04f);
                CornerAnchor.Garden(w, h, safe, out _, out _, out _, out var rs, out var hv);
                rs = CornerAnchor.GardenBottomSeat(w, h, safe); // b131: banner seat (Restart itself moved to the top bar)
                float refH = FlockFiveApp.GardenRefBandH(w, h, safe, s, 0f);
                float y = FlockFiveApp.GardenFixedSeatY(w, h, safe, s, 0f);
                var band = new Rect(0f, y, w, refH);
                float pad = 4f * s;
                bool covers = band.yMin <= rs.yMin - pad && band.yMax >= rs.yMax + pad;
                // Lowest branch row of the Level 1 garden target rows: step-2 destination (branch 5) bird bottom.
                float destBottom = h * 0.39f + h * 0.026f;
                Check("banner covers restart " + tags[d], covers && band.yMin > destBottom,
                    "lift " + CaptionLayout.GardenBannerLiftFrac.ToString("0.0000") + " band " + band.yMin.ToString("0") + ".." + band.yMax.ToString("0")
                    + " restart " + rs.yMin.ToString("0") + ".." + rs.yMax.ToString("0") + " destBirdBottom " + destBottom.ToString("0") + " h " + h.ToString("0"));
            }
        }
    }
}
#endif

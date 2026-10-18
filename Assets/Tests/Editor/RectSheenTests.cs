#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // b110: the button gloss is clipped to the rounded rect, and a pulsing button's label scales with it.
    public static class RectSheenTests
    {
        const string Out = "/tmp/flock-five-rect-sheen-tests.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }
            const float w = 300f, h = 110f, c = 28f;
            float corner = PushButton.RectSheenAt(1f, 1f, w, h, c, 0.04f);
            float outsideOval = PushButton.RectSheenAt(w * 0.5f, h * 0.5f, w, h, c, 0.5f);
            Check("corner-is-clipped", corner == 0f, "corner " + corner);
            Check("band-lights-the-middle", outsideOval > 0.5f, "mid " + outsideOval);
            // Along the whole top-left strip of the plate, never lit past the rounded corner.
            bool spill = false;
            for (float u = 0f; u <= 1f; u += 0.05f)
                for (float x = 0f; x < 8f; x += 1f)
                    for (float y = h - 8f; y <= h; y += 1f)
                        if (PushButton.RoundRectSdf(x, y, w, h, c) > 0f && PushButton.RectSheenAt(x, y, w, h, c, u) > 0f) spill = true;
            Check("no-sheen-outside-rounded-rect", !spill, "");
            // Fills the straight edge of the plate (the old oval stopped well short of the sides).
            float edge = 0f;
            for (float u = 0f; u <= 1f; u += 0.02f) edge = Mathf.Max(edge, PushButton.RectSheenAt(w * 0.5f, h - 4f, w, h, c, u));
            Check("reaches-the-straight-edge", edge > 0.3f, "edge " + edge);
            // Pulse: plate rect and label rect share one k.
            float k = PushButton.PulseK(0.4f, 0.03f, 1.6f);
            var plate = new Rect(10f, 20f, 300f, 110f);
            var big = PushButton.Scaled(plate, k);
            var back = PushButton.ScaledAbout(big, big.center, 1f / k);
            Check("pulse-in-band", k > 0.97f && k < 1.03f, "k " + k);
            Check("label-unscale-roundtrip", Mathf.Abs(back.width - plate.width) < 0.01f && Mathf.Abs(back.center.x - plate.center.x) < 0.01f, "");
            sb.AppendLine(fail == 0 ? "ALL PASS" : fail + " FAIL");
            File.WriteAllText(Out, sb.ToString());
            UnityEngine.Debug.Log("[rect-sheen] " + (fail == 0 ? "ALL PASS" : fail + " FAIL"));
        }
    }
}
#endif

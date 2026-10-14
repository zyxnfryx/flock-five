#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b89 (Brandon): the Go VIP disc sheen hugs the rim and starts on it. Shared oval
    // sheen in PushButton.DrawSheen / OvalSheenAt.
    public static class Build89GoVipSheenTests
    {
        const string Out = "/tmp/flock-five-b89-govip-sheen-tests.txt";

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
                Debug.Log("[build89govip] " + line);
            }
            try
            {
                const int N = 256;
                // 1. Rim gap: half coverage sits within 2% of the face width (old inset ellipse ~5.4%).
                float gap = PushButton.OvalEdgeGap;
                Check("rim-gap-tight", gap > 0f && gap <= 0.02f, "gap=" + gap.ToString("0.0000"));

                // 2. Nothing ever lands outside the face ellipse.
                bool outside = true;
                foreach (float u in new[] { 0f, 0.1f, 0.5f, 0.9f, 1f })
                    for (int y = 0; y < N; y++)
                        for (int x = 0; x < N; x++)
                        {
                            float t = (x + 0.5f) / N, v = (y + 0.5f) / N;
                            float dx = t - 0.5f, dy = v - 0.5f;
                            if (dx * dx + dy * dy > 0.25f && PushButton.OvalSheenAt(t, v, u, true) > 0f) outside = false;
                        }
                Check("clip-inside-face", outside, "");

                // 3. The trace starts on the left rim: at u = 0 the band centre is the
                // leftmost point of the clip, and the brightest pixel there is near the edge.
                float best = 0f, bt = 1f, bv = 0.5f;
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float t = (x + 0.5f) / N, v = (y + 0.5f) / N;
                        float c = PushButton.OvalSheenAt(t, v, 0f, false);
                        if (c > best) { best = c; bt = t; bv = v; }
                    }
                float rimDist = 0.5f - Mathf.Sqrt((bt - 0.5f) * (bt - 0.5f) + (bv - 0.5f) * (bv - 0.5f));
                Check("start-on-rim", best > 0.5f && rimDist < 0.04f,
                    "best=" + best.ToString("0.00") + " at t=" + bt.ToString("0.000") + " v=" + bv.ToString("0.000") + " rimDist=" + rimDist.ToString("0.000"));

                // 4. Start frame is visible: the idle envelope is already up early in the sweep.
                float early = PushButton.OvalFade(0.07f);
                Check("start-visible", early >= 0.5f && PushButton.OvalFade(0.5f) > 0.99f && PushButton.OvalFade(0f) < 0.001f, "fade(0.07)=" + early.ToString("0.00"));

                // 5. Mirror end: u = 1 sits on the right rim, and the travel is symmetric.
                Check("sweep-symmetric", Mathf.Abs(PushButton.OvalTravel(0f) + PushButton.OvalTravel(1f) - 1f) < 1e-4f
                    && PushButton.OvalTravel(0f) < 0f, "start=" + PushButton.OvalTravel(0f).ToString("0.000"));

                // 6. Mid-sweep is bright across the centre.
                Check("mid-bright", PushButton.OvalSheenAt(0.5f, 0.5f, 0.5f, false) > 0.9f && PushButton.OvalFade(0.5f) > 0.99f, "");
            }
            catch (System.Exception e)
            {
                Check("exception", false, e.Message);
            }
            try
            {
                // Pass 2: the face mask is the sprite's top-face silhouette.
                // Gold top face centre is in, the thickness band and the petals are out.
                Check("face-centre-in", PushButton.FaceCover(0.50f, 0.25f, PushButton.FaceInset) > 0.99f, "");
                Check("face-side-band-out", PushButton.FaceCover(0.50f, 0.50f, 1f, 0f) == 0f
                    && PushButton.FaceCover(0.50f, 0.60f, 1f, 0f) == 0f, "");
                Check("face-above-out", PushButton.FaceCover(0.50f, 0.01f, 1f, 0f) == 0f, "");
                Check("face-left-petals-out", PushButton.FaceCover(0.17f, 0.25f, 1f, 0f) == 0f
                    && PushButton.FaceCover(0.86f, 0.25f, 1f, 0f) == 0f, "");

                // Zero sheen pixels outside the face across the sweep, at several draw sizes.
                int spill = 0;
                foreach (var sz in new[] { new Vector2(878f, 695f), new Vector2(1300f, 1030f), new Vector2(800f, 634f) })
                    foreach (float u in new[] { 0f, 0.05f, 0.25f, 0.5f, 0.75f, 0.95f, 1f })
                        spill += PushButton.FaceSpillPixels(sz.x, sz.y, u);
                Check("face-spill-zero", spill == 0, "spill=" + spill);

                // Trace starts on the left rim: u = 0 band sits at the leftmost covered point.
                float best = 0f, bx = 1f;
                for (int y = 0; y < 200; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        float c = PushButton.FaceSheenAt((x + 0.5f) / 256f, (y + 0.5f) / 200f, 0.02f);
                        if (c > best) { best = c; bx = (x + 0.5f) / 256f; }
                    }
                Check("face-start-left-rim", best > 0.5f && bx < 0.30f, "best=" + best.ToString("0.00") + " su=" + bx.ToString("0.000"));
                Check("face-mid-bright", PushButton.FaceSheenAt(0.5f, 0.25f, 0.5f) > 0.5f, "");
            }
            catch (System.Exception e)
            {
                Check("exception-face", false, e.Message);
            }
            sb.AppendLine(fail == 0 ? "BUILD89GOVIP_OK " + pass : "BUILD89GOVIP_FAIL " + fail + " passed " + pass);
            File.WriteAllText(Out, sb.ToString());
        }
    }
}
#endif

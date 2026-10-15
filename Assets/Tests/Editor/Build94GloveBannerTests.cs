#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b94 (Brandon QA build 93): the glove comes in from the lower right, beneath the banner.
    // For every garden lesson caption, on iPhone, iPad and a short iPhone SE, a fingertip at or
    // below the banner never has its hand (anywhere on the approach, or at the dip) on the banner.
    public static class Build94GloveBannerTests
    {
        const string Out = "/tmp/flock-five-b94-glove-banner-tests.txt";

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
                Debug.Log("[build94glove] " + line);
            }
            float? keep = DeviceFit.TestWindow;
            try
            {
                var band = new Rect(0f, 400f, 1000f, 200f);
                Check("clear helper overlap", !CaptionLayout.GloveClearsBanner(new Rect(100f, 500f, 100f, 100f), band), "");
                Check("clear helper below", CaptionLayout.GloveClearsBanner(new Rect(100f, 600f, 100f, 100f), band), "");
                Check("aim lowered under banner", Mathf.Approximately(CaptionLayout.AimBelowBanner(new Vector2(50f, 590f), band, 80f, 4f, 500f).y, 684f), "");
                Check("aim under banner untouched", CaptionLayout.AimBelowBanner(new Vector2(50f, 900f), band, 80f, 4f, 500f).y == 900f, "");
                Check("banner below aim untouched", CaptionLayout.AimBelowBanner(new Vector2(50f, 300f), band, 80f, 4f, 500f).y == 300f, "");
                Check("max two banner lines", CaptionLayout.BannerMaxLines == 2, CaptionLayout.BannerMaxLines.ToString());

                string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
                Check("garden glove is the only garden call", src.IndexOf("GardenGlove(_cueAimGui, dt, s, FnCueTap())") > 0
                    && src.IndexOf("GardenGlove(gui, dt, handS, FnPestTap())") > 0
                    && src.IndexOf("GardenGlove(_cueAimGui, PlayClock.Delta") > 0, "");

                Check("ad.hand is a banner", CaptionStyles.IsBanner("ad.hand") && !CaptionStyles.IsSnug("ad.hand"), "");
                int adAt = src.IndexOf("bool adHand = spot == \"ad.hand\"");
                Check("ad.hand banner rides below Watch flower + glove", adAt > 0 && src.IndexOf("band.y = adFloor + gap", adAt) > adAt
                    && src.IndexOf("BannerSkip = \"watch button\"", adAt) > adAt, "");
                Check("no-room banner rides at HUD floor", src.IndexOf("band.y = minTop; moved = true;") > 0, "");

                string[] lines =
                {
                    CoachTap.Caption(CoachStep.CoachPickup), CaptionLayout.CoachPlaceLine, CaptionLayout.CoachNowhereLine,
                    CaptionLayout.LeafLine, CaptionLayout.SparrowLine, CaptionLayout.HawkLine, CaptionLayout.BeePestLine,
                    CaptionLayout.OstrichLine, CaptionLayout.HiveGardenLine,
                };
                var devices = new[]
                {
                    new Vector3(1179f, 2556f, 0f), new Vector3(1536f, 2048f, 0f), new Vector3(750f, 1334f, 0f),
                };
                string[] tags = { "iphone", "ipad", "se" };
                for (int d = 0; d < devices.Length; d++)
                {
                    float w = devices[d].x, h = devices[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    float dh = FlockFiveApp.GloveAuditDh(s);
                    float above = FlockFiveApp.GloveAuditAbove(s);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    var seat = new Rect(w * 0.2f, h * 0.42f, w * 0.6f, 120f);
                    var style = FlockFiveApp.CoachCaptionStyle();
                    int bad = 0, probes = 0, tallest = 0;
                    foreach (var line in lines)
                    {
                        var rect = FlockFiveApp.BannerRect("coach.line", line, w, h, safe, seat, 0f, false, s, out int px);
                        int rows = CaptionLayout.FitBanner(style, line, CaptionLayout.BannerMeasureWidth(w, style, line, px), px).Lines;
                        if (rows > tallest) tallest = rows;
                        if (rows > 2) bad++;
                        // Fingertips from the banner's bottom edge down the screen, across the width.
                        for (float y = rect.yMax; y < h - 8f; y += dh * 0.25f)
                            for (int xi = 0; xi < 6; xi++)
                            {
                                float x = w * (0.08f + 0.84f * xi / 5f);
                                var aim = CaptionLayout.AimBelowBanner(new Vector2(x, y), rect, above, CaptionLayout.GloveBannerGap * s, dh * 1.0f);
                                // Approach: the cuff sits below-right of the tip and slides to it.
                                var perch = aim + new Vector2(0.7071f, 0.7071f) * (dh * 0.9f);
                                for (int k = 0; k <= 8; k++)
                                {
                                    var pos = Vector2.Lerp(perch, aim, k / 8f);
                                    float dip = k >= 7 ? 1f : 0f;
                                    var box = FlockFiveApp.GloveAuditBox(pos, FlockFiveApp.GloveAuditBelowDeg, dh, dip);
                                    probes++;
                                    if (!CaptionLayout.GloveClearsBanner(box, rect)) bad++;
                                }
                            }
                    }
                    Check("glove clear of banner " + tags[d], bad == 0, "probes " + probes + " bad " + bad + " rows<=" + tallest);
                }
            }
            catch (System.Exception ex)
            {
                Check("b94 glove banner", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                DeviceFit.TestWindow = keep;
            }
            string end = fail == 0 ? "BUILD94GLOVE_OK " + pass : "BUILD94GLOVE_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif

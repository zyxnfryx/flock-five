#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 133. Reverts the b132 whole-button raise and centres the LEVEL + joke ink stack on the
    // disc's top face instead. Edit-mode only, arithmetic on explicit screen sizes (no Screen, no GUI).
    // Font sizes are the ones FitFont / EaseSubtextPx land on with the default font (offline
    // estimate); the centring itself does not depend on them, LevelStackLift measures whatever it gets.
    static class Build133LevelLabelCenterTests
    {
        struct Dev
        {
            public string Name;
            public float W, H, SafeBottom, SafeTop;
            public float OldPad; // pre-b132 bottom gap under the LEVEL square, px
            public int Lv2, Lv3, Lv4, Joke; // LEVEL px for 1-2 / 3 / 4 digits with a joke, joke px
        }

        const float PadTop = 1f, PadBottom = 2f; // default label padding (assumed)
        const float CentreTolFrac = 0.005f;      // |ink centre - face centre| <= 0.5% of the disc

        [MenuItem("Flock Five/Build 133 Level Label Center Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build133] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var devs = new[]
            {
                new Dev { Name = "se 750x1334", W = 750f, H = 1334f, OldPad = 4f, Lv2 = 58, Lv3 = 58, Lv4 = 58, Joke = 33 },
                new Dev { Name = "iphone 1179x2556", W = 1179f, H = 2556f, SafeBottom = 102f, SafeTop = 177f, OldPad = 104f, Lv2 = 84, Lv3 = 64, Lv4 = 72, Joke = 64 },
                new Dev { Name = "ipad 4:3 1536x2048", W = 1536f, H = 2048f, SafeBottom = 40f, SafeTop = 48f, OldPad = 42f, Lv2 = 54, Lv3 = 54, Lv4 = 54, Joke = 41 },
                new Dev { Name = "phone window 0.56 1080x1920", W = 1080f, H = 1920f, OldPad = 4f, Lv2 = 84, Lv3 = 64, Lv4 = 72, Joke = 48 },
            };

            var prev = DeviceFit.TestWindow;
            try
            {
                for (int i = 0; i < devs.Length; i++)
                {
                    var d = devs[i];
                    DeviceFit.TestWindow = d.W / d.H;
                    float s = DeviceFit.Ui(d.W, d.H);
                    var safe = new Rect(0f, d.SafeBottom, d.W, d.H - d.SafeBottom - d.SafeTop);
                    var hit = SplashPress.LayoutLevel(d.W, d.H, safe);
                    var rect = hit.Rect;

                    // (1) The button is back on the pre-b132 square: no extra bottom gap.
                    Check("button rect reverted " + d.Name, Mathf.Abs((d.H - rect.yMax) - d.OldPad) < 0.01f,
                        "gap " + (d.H - rect.yMax).ToString("0.0") + " want " + d.OldPad.ToString("0.0"));
                    var press = SplashPress.PressHit(SplashPress.Id.Level);
                    var launch = SplashPress.LaunchHit(SplashPress.Id.Level);
                    Check("hit is the square " + d.Name,
                        ReferenceEquals(hit, press) && ReferenceEquals(press, launch) && press.Contains(rect.center),
                        "rect " + rect);

                    var disc = FlockFiveApp.LevelDiscAudit(d.W, d.H, safe);
                    float face = disc.y + disc.height * FlockFiveApp.FaceMidFrac;
                    float plateGap = FlockFiveApp.HomePlateGapAudit(rect, d.H, s, out float plateBottom);

                    int[] lvPx = { d.Lv2, d.Lv2, d.Lv3, d.Lv4 };
                    string[] lvName = { "LEVEL 7", "LEVEL 38", "LEVEL 388", "LEVEL 1000" };
                    for (int k = 0; k < lvPx.Length; k++)
                    {
                        // Lines 1 = "Easy" / "Super Easy" / "Super Duper Easy" on one line; 2 = a wrapped long joke.
                        for (int lines = 1; lines <= 2; lines++)
                        {
                            string tag = d.Name + " " + lvName[k] + (lines == 1 ? " + Super Duper Easy" : " + 2-line joke");
                            // Same rects as DrawFlowerCaption.
                            float stackH = disc.height * 0.56f;
                            float stackY = disc.y + (disc.height - stackH) * 0.68f;
                            float lvH = stackH * 0.52f;
                            float lvBottom = stackY + lvH;
                            float jokeTop = lvBottom + disc.height * 0.005f;
                            float lvLine = lvPx[k] * FlockFiveApp.InkLineFrac;
                            float jokeLine = d.Joke * FlockFiveApp.InkLineFrac;

                            FlockFiveApp.LevelStackInk(lvBottom, PadBottom, lvLine, lvPx[k], jokeTop, PadTop, jokeLine, d.Joke, lines,
                                out float top0, out float bot0);
                            float lift = FlockFiveApp.LevelStackLift(disc, lvBottom, PadBottom, lvLine, lvPx[k], jokeTop, PadTop, jokeLine, d.Joke, lines);
                            float top1 = top0 - lift, bot1 = bot0 - lift;
                            float before = (top0 + bot0) * 0.5f - face;
                            float after = (top1 + bot1) * 0.5f - face;

                            // (2) Stack ink centre sits on the face centre.
                            Check("stack centred " + tag, Mathf.Abs(after) <= disc.height * CentreTolFrac,
                                "before " + before.ToString("+0.0;-0.0") + " px (" + (before / disc.height * 100f).ToString("0.00")
                                + "% disc) after " + after.ToString("+0.0;-0.0") + " px, lift " + lift.ToString("0.0"));
                            Check("lift in band " + tag,
                                lift <= disc.height * FlockFiveApp.StackLiftMaxFrac + 0.01f && lift >= -disc.height * FlockFiveApp.StackDropMaxFrac - 0.01f,
                                "lift " + lift.ToString("0.0") + " disc " + disc.height.ToString("0.0"));
                            Check("ink inside face " + tag, top1 > disc.y + disc.height * 0.10f && bot1 < disc.yMax - 2f,
                                "top " + top1.ToString("0.0") + " bottom " + bot1.ToString("0.0") + " disc " + disc);

                            // (3) Avatar name plate (e.g. "Goldie"): the perch is seated off FlowerLevelTop (rect
                            // top), which b133 does not move. The raised ink must stay below that line, so the
                            // plate-to-ink gap is the audited gap plus the headroom, and never crowded.
                            float levelTop = plateBottom + plateGap;
                            float inkGap = top1 - plateBottom;
                            Check("plate clear " + tag,
                                top1 >= levelTop + 4f * s && inkGap >= plateGap && inkGap >= 8f * s,
                                "plate→ink " + inkGap.ToString("0.0") + " px (audited plate→LEVEL rect " + plateGap.ToString("0.0")
                                + ", headroom " + (top1 - levelTop).ToString("0.0") + ")");
                        }
                    }
                }
            }
            finally
            {
                DeviceFit.TestWindow = prev;
            }

            Debug.Log("[build133] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif

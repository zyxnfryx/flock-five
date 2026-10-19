#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 133. Reverts the b132 whole-button raise and centres the LEVEL + joke ink stack on the
    // disc's top face instead. Edit-mode only, arithmetic on explicit screen sizes (no Screen, no GUI).
    // Font sizes are the ones FitFont / EaseSubtextPx land on with the default font (offline
    // estimate); the centring itself does not depend on them, LevelStackLift measures whatever it gets.
    // b133.1: the stack centres only within the room under the name plate (8 x s gap). Shipping jokes
    // (Easy / Super Easy / Super Duper Easy) are one line on every size here; the 2-line case is a
    // robustness check for a wrapped joke and is allowed to stop at the plate gap.
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
                    // The plate as the caption sees it: spanning the disc (worst case), bottom at the audit.
                    var plate = new Rect(disc.x, plateBottom - 20f * s, disc.width, 20f * s);
                    float ceiling = FlockFiveApp.LevelInkCeiling(plate, disc, s);
                    Check("ceiling from plate " + d.Name, Mathf.Abs(ceiling - (plateBottom + FlockFiveApp.PlateInkClearPt * s)) < 0.01f,
                        "ceiling " + ceiling.ToString("0.0") + " plateBottom " + plateBottom.ToString("0.0"));

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
                            float lift = FlockFiveApp.LevelStackLift(disc, lvBottom, PadBottom, lvLine, lvPx[k], jokeTop, PadTop, jokeLine, d.Joke, lines, ceiling);
                            float top1 = top0 - lift, bot1 = bot0 - lift;
                            float before = (top0 + bot0) * 0.5f - face;
                            float after = (top1 + bot1) * 0.5f - face;
                            // Room to rise before the ink meets the plate gap (negative: already crowding).
                            float room = top0 - ceiling;
                            float upCap = disc.height * FlockFiveApp.StackLiftMaxFrac;
                            float downCap = disc.height * FlockFiveApp.StackDropMaxFrac;
                            float want = Mathf.Clamp(before, -downCap, upCap);
                            bool plateBound = room < want;
                            float expect = plateBound ? room : want;

                            // (2) Stack ink centre sits on the face centre, unless the name plate leaves less
                            // room than that; then the ink top sits exactly on the plate gap (centred within
                            // the available space) and the leftover offset is logged.
                            Check("stack centred " + tag,
                                Mathf.Abs(lift - expect) < 0.05f
                                && (plateBound ? Mathf.Abs(top1 - ceiling) < 0.05f : Mathf.Abs(after) <= disc.height * CentreTolFrac),
                                "before " + before.ToString("+0.0;-0.0") + " px (" + (before / disc.height * 100f).ToString("0.00")
                                + "% disc) after " + after.ToString("+0.0;-0.0") + " px (" + (after / disc.height * 100f).ToString("0.00")
                                + "%), lift " + lift.ToString("0.0") + (plateBound ? " [limited by name plate, room " + room.ToString("0.0") + "]" : ""));
                            Check("lift in band " + tag,
                                lift <= upCap + 0.01f && (lift >= -downCap - 0.01f || plateBound),
                                "lift " + lift.ToString("0.0") + " disc " + disc.height.ToString("0.0"));
                            // Never raised past the plate gap; never pushed down unless the plate forces it.
                            Check("lift never crowds " + tag, top1 >= ceiling - 0.05f && (lift >= -0.05f || plateBound || before < 0f),
                                "inkTop " + top1.ToString("0.0") + " ceiling " + ceiling.ToString("0.0"));
                            Check("ink inside face " + tag, top1 > disc.y + disc.height * 0.10f && bot1 < disc.yMax - 2f,
                                "top " + top1.ToString("0.0") + " bottom " + bot1.ToString("0.0") + " disc " + disc);

                            // (3) Avatar name plate (e.g. "Goldie"). The audited plate already hangs below the LEVEL
                            // rect top (negative audited gap), so the gap that matters is plate bottom to ink top.
                            // It must be at least PlateInkClearPt x s (8 x s) on every size, two-line jokes included.
                            float levelTop = plateBottom + plateGap;
                            float inkGap = top1 - plateBottom;
                            Check("plate clear " + tag,
                                top1 >= levelTop + 4f * s && inkGap >= plateGap && inkGap >= FlockFiveApp.PlateInkClearPt * s - 0.05f,
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

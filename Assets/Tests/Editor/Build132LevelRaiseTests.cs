#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 132. The home LEVEL button sits a little higher off the bottom, and the bird's name
    // plate still clears it. Edit-mode only, arithmetic on explicit screen sizes (no Screen).
    static class Build132LevelRaiseTests
    {
        struct Dev
        {
            public string Name;
            public float W, H, SafeBottom, SafeTop;
            // The bottom gap before b132 (the old pad), in px.
            public float OldPad;
        }

        [MenuItem("Flock Five/Build 132 Level Raise Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build132] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var devs = new[]
            {
                new Dev { Name = "se 750x1334", W = 750f, H = 1334f, SafeBottom = 0f, SafeTop = 0f, OldPad = 4f },
                new Dev { Name = "iphone 1179x2556", W = 1179f, H = 2556f, SafeBottom = 102f, SafeTop = 177f, OldPad = 104f },
                new Dev { Name = "ipad 4:3 1536x2048", W = 1536f, H = 2048f, SafeBottom = 40f, SafeTop = 48f, OldPad = 42f },
                new Dev { Name = "phone window 0.56 1080x1920", W = 1080f, H = 1920f, SafeBottom = 0f, SafeTop = 0f, OldPad = 4f },
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
                    float raise = SplashPress.LevelRaise(d.H);
                    float gap = d.H - rect.yMax;

                    // Raised bottom gap: the old pad plus the shared lift. The lift is 1.2% of the height,
                    // capped at SplashPress.LevelRaiseMaxPx (18 px), so it stays a tiny bit on every device.
                    float wantRaise = Mathf.Min(d.H * 0.012f, 18f);
                    Check("bottom gap raised " + d.Name,
                        Mathf.Abs(gap - (d.OldPad + raise)) < 0.01f && Mathf.Abs(raise - wantRaise) < 0.01f
                        && raise >= 15.9f && raise <= 20f,
                        "gap " + gap.ToString("0.0") + " old " + d.OldPad.ToString("0.0") + " raise " + raise.ToString("0.0")
                        + " (" + (raise / d.H * 100f).ToString("0.00") + "% of h)");

                    // Size and x are untouched; only y moved, so the ring and the sink read the same square.
                    var before = new Rect(rect.x, rect.y + raise, rect.width, rect.height);
                    Check("only y moved " + d.Name,
                        Mathf.Abs((d.H - before.yMax) - d.OldPad) < 0.01f && rect.width > 1f && rect.yMin > 0f,
                        "top " + rect.yMin.ToString("0.0") + " size " + rect.width.ToString("0.0"));

                    // Press, launch and the drawn square are one hit on the raised rect.
                    var press = SplashPress.PressHit(SplashPress.Id.Level);
                    var launch = SplashPress.LaunchHit(SplashPress.Id.Level);
                    Check("hit follows raised rect " + d.Name,
                        ReferenceEquals(hit, press) && ReferenceEquals(press, launch)
                        && press.Contains(rect.center) && !press.Contains(new Vector2(rect.center.x, rect.yMax + 1f)),
                        "rect " + rect);

                    // Name plate clearance: the stack hangs off the square, so raising it must leave the
                    // plate-to-LEVEL gap exactly as it was, and the plate stays well above the button centre.
                    float gapNow = FlockFiveApp.HomePlateGapAudit(rect, d.H, s, out float plateBottom);
                    float gapOld = FlockFiveApp.HomePlateGapAudit(before, d.H, s, out _);
                    Check("plate clearance kept " + d.Name,
                        Mathf.Abs(gapNow - gapOld) < 0.01f,
                        "plate gap now " + gapNow.ToString("0.0") + " before " + gapOld.ToString("0.0") + " px");
                    Check("plate above button centre " + d.Name,
                        plateBottom < rect.center.y,
                        "plateBottom " + plateBottom.ToString("0") + " centre " + rect.center.y.ToString("0"));
                }
            }
            finally
            {
                DeviceFit.TestWindow = prev;
            }

            Debug.Log("[build132] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif

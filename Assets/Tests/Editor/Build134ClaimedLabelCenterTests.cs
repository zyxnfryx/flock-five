#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 134. The Daily claim push-button in its CLAIMED state shows "Claimed" over
    // "Ready in HH:MM:SS". Both lines sit as one stack centred on the top disc face, and the
    // timer fits on one line (shrink-to-fit, no wrap) on iPhone and iPad portrait.
    // Edit-mode only, arithmetic on explicit screen sizes (no Screen).
    static class Build134ClaimedLabelCenterTests
    {
        struct Dev
        {
            public string Name;
            public float W, H, SafeBottom, SafeTop;
        }

        [MenuItem("Flock Five/Build 134 Claimed Label Center Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build134] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var devs = new[]
            {
                new Dev { Name = "se 750x1334", W = 750f, H = 1334f, SafeBottom = 0f, SafeTop = 0f },
                new Dev { Name = "iphone 1179x2556", W = 1179f, H = 2556f, SafeBottom = 102f, SafeTop = 177f },
                new Dev { Name = "ipad 4:3 1536x2048", W = 1536f, H = 2048f, SafeBottom = 40f, SafeTop = 48f },
                new Dev { Name = "ipad pro 2048x2732", W = 2048f, H = 2732f, SafeBottom = 40f, SafeTop = 48f },
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
                    var disc = FlockFiveApp.LevelDiscAudit(d.W, d.H, safe);
                    FlockFiveApp.DailyClaimLabelRects(disc, out var verbR, out var countR);

                    float face = disc.y + disc.height * FlockFiveApp.ClaimFaceMidFrac;
                    float stackMid = (verbR.yMin + countR.yMax) * 0.5f;
                    Check("stack centred on face " + d.Name, Mathf.Abs(stackMid - face) < 0.5f,
                        "stack mid " + stackMid.ToString("0.0") + " face " + face.ToString("0.0"));
                    Check("verb centred x " + d.Name, Mathf.Abs(verbR.center.x - disc.center.x) < 0.5f, "verb " + verbR);
                    Check("timer centred x " + d.Name, Mathf.Abs(countR.center.x - disc.center.x) < 0.5f, "timer " + countR);
                    Check("lines inside disc " + d.Name,
                        disc.Contains(verbR.min) && disc.Contains(countR.max - Vector2.one * 0.01f) && verbR.yMax <= countR.yMin,
                        "disc " + disc + " verb " + verbR + " timer " + countR);

                    var verbSt = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                    var countSt = FlockFiveApp.DailyCountStyle(new GUIStyle());
                    FlockFiveApp.DailyClaimFit(verbSt, countSt, disc, s, out int verbPx, out int countPx);
                    countSt.fontSize = countPx;
                    var one = countSt.CalcSize(new GUIContent(FlockFiveApp.ClaimCountSample));
                    Check("timer one line " + d.Name, !countSt.wordWrap && one.x <= countR.width + 1f && one.y <= countR.height * 1.6f,
                        "px " + countPx + " size " + one + " box " + countR.size);
                    Check("verb bigger than timer " + d.Name, verbPx >= countPx, "verb " + verbPx + " timer " + countPx);
                }
            }
            finally
            {
                DeviceFit.TestWindow = prev;
            }

            Debug.Log("[build134] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
        }
    }
}
#endif

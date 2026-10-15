#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89. QA rule B: a hidden (shrouded) bird stays blacked out through restart fly-off and
    // fly-in, wearing exactly the seated look (one tint, no kit/face, far wing follows).
    public static class Build89ShadowBirdTests
    {
        const string Out = "/tmp/flock-five-b89-shadow-tests.txt";

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
                Debug.Log("[build89shadow] " + line);
            }

            try
            {
                Color hidden = BirdIdle.HiddenTint;
                Check("rule-hidden-is-silhouette-in-every-pose",
                    BirdDress.Silhouette(true) && !BirdDress.Silhouette(false), "");
                Check("rule-one-tint", BirdIdle.BodyTint(true) == hidden && BirdIdle.BodyTint(false) == Color.white, "");

                // Seated hidden bird vs the same bird in restart flight: identical tint on body
                // and far wing, no kit, no face, opaque. A visible bird stays full art.
                for (int pose = 0; pose < 2; pose++)
                {
                    bool hid = pose == 0;
                    var g = new GameObject("b89-bird-" + pose);
                    try
                    {
                        var sr = g.AddComponent<SpriteRenderer>();
                        var idle = g.AddComponent<BirdIdle>();
                        idle.Bind(new Bird(BirdColor.Ruby, BirdKit.Bow), Vector3.zero);
                        idle.Shrouded = hid;
                        sr.color = BirdIdle.BodyTint(hid); // what BranchView paints for a seated bird
                        var seated = sr.color;
                        idle.Frozen = true;
                        idle.Flapping = true;
                        idle.Lift = 1f;
                        sr.color = Color.white;             // whatever a prior flight frame left
                        idle.ShowFlightLook();
                        var c = sr.color;
                        string tag = hid ? "hidden" : "visible";
                        Check("flight-silhouette-matches-seated-" + tag,
                            c.a > 0.99f && Mathf.Approximately(c.r, seated.r) && Mathf.Approximately(c.g, seated.g)
                            && Mathf.Approximately(c.b, seated.b),
                            "seated " + seated + " flight " + c);
                        Check("flight-no-half-art-" + tag,
                            !hid || (c.r < 0.1f && c.g < 0.1f && c.b < 0.1f),
                            "body " + c);
                        Check("flight-kit-face-follow-silhouette-" + tag,
                            BirdDress.KitOn(true, BirdDress.Silhouette(hid), false) == !hid
                            && BirdDress.FaceOn(true, BirdDress.Silhouette(hid)) == !hid, "");
                    }
                    finally { Object.DestroyImmediate(g); }
                }

                // Flight code paths take the tint from the shared look, never their own colour.
                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                int a = app.IndexOf("void ArmFlight");
                int b = app.IndexOf("bool LiveBird", a);
                string body = a >= 0 && b > a ? app.Substring(a, b - a) : "";
                Check("flight-path-uses-shared-look",
                    body.Length > 0 && body.IndexOf("ShowFlightLook") >= 0 && body.IndexOf("0.04f") < 0
                    && body.IndexOf("Color.white") < 0 && app.IndexOf("FlightSilhouette") < 0,
                    "len " + body.Length);
            }
            catch (System.Exception e) { Check("no-exception", false, e.Message); }

            Debug.Log(fail == 0
                ? "build89shadow tests DONE " + pass + "/" + (pass + fail)
                : "build89shadow tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 89. A restart flight (fly-off and fly-in) never draws a bird as a flat black
    // silhouette. Hidden (shrouded) birds stay black only while seated.
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

            GameObject go = null;
            try
            {
                Check("rule-seated-hidden-is-silhouette", BirdDress.Silhouette(true, false, false), "");
                Check("rule-flying-hidden-has-art", !BirdDress.Silhouette(true, true, true), "");
                Check("rule-visible-never-silhouette",
                    !BirdDress.Silhouette(false, false, false) && !BirdDress.Silhouette(false, true, true), "");

                go = new GameObject("b89-bird");
                var sr = go.AddComponent<SpriteRenderer>();
                var idle = go.AddComponent<BirdIdle>();
                idle.Bind(new Bird(BirdColor.Ruby, BirdKit.Bow), Vector3.zero);
                idle.Shrouded = true;
                sr.color = new Color(0.04f, 0.03f, 0.05f, 1f); // what a perched hidden bird left behind
                idle.Frozen = true;
                idle.Flapping = true;
                idle.ShowFlightArt();
                var c = sr.color;
                Check("flight-art-not-black", c.r > 0.9f && c.g > 0.9f && c.b > 0.9f && c.a > 0.99f,
                    "color " + c);

                // Every bird tint in the restart flight path must come from the shared art path.
                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                int a = app.IndexOf("void ArmFlight");
                int b = app.IndexOf("bool LiveBird", a);
                string body = a >= 0 && b > a ? app.Substring(a, b - a) : "";
                Check("flight-path-no-dark-tint",
                    body.Length > 0 && body.IndexOf("Silhouette") < 0 && body.IndexOf("0.04f") < 0
                    && body.IndexOf("ShowFlightArt") >= 0 && app.IndexOf("FlightSilhouette") < 0,
                    "len " + body.Length);
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            Debug.Log(fail == 0
                ? "build89shadow tests DONE " + pass + "/" + (pass + fail)
                : "build89shadow tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b125: tutorial glove start is at most 0.67 of the previous start-to-target distance (target 0.5), side rule kept.
    public static class GloveTravelTests
    {
        const string Out = "/tmp/flock-five-b125-glove-travel-tests.txt";

        [MenuItem("Flock Five/Glove Travel Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[glove-travel] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }

            Check("tunable-in-range", FlockFiveApp.GloveTravelScale > 0f && FlockFiveApp.GloveTravelScale <= 0.67f, "scale " + FlockFiveApp.GloveTravelScale);

            var devices = new[] { new Vector2(1170, 2532), new Vector2(2048, 2732), new Vector2(1080, 2400) };
            string[] names = { "iphone", "ipad", "android" };
            for (int d = 0; d < devices.Length; d++)
            {
                float w = devices[d].x, h = devices[d].y;
                float s = DeviceFit.Ui(w, h);
                float dh = FlockFiveApp.GloveAuditDh(s);
                var safe = new Rect(0f, 60f * s, w, h - 120f * s);
                bool ratioOk = true, sideOk = true, clearOk = true, neverLonger = true;
                string detail = "";
                // Aims across the screen; glove enters from the right on the left half, from the left on the right half.
                foreach (float fx in new[] { 0.15f, 0.3f, 0.45f, 0.55f, 0.7f, 0.85f })
                {
                    var aim = new Vector2(w * fx, h * 0.5f);
                    float dir = fx < 0.5f ? 1f : -1f; // +1: glove sits right of the target (points left)
                    // (a) angled entry path
                    var away = new Vector2(dir, 0f);
                    var rest = aim + away * (48f * s);
                    var oldStart = aim + (rest + away * (dh * 0.9f) - aim) * FlockFiveApp.GloveEntryScale;
                    var ns = FlockFiveApp.GloveEntryPoint(rest, aim, away, dh);
                    float r1 = (ns - aim).magnitude / (oldStart - aim).magnitude;
                    // (b) side/perch start: a far rest, as the shove loop can produce
                    var farRest = aim + away * (dh * 4f);
                    var oldNear = aim + away * (1.6f * dh);
                    var nn = FlockFiveApp.GloveNearStartPoint(farRest, aim, s, new Rect(0f, 0f, w * 4f, h * 4f));
                    float r2 = (nn - aim).magnitude / (oldNear - aim).magnitude;
                    ratioOk &= r1 <= 0.67f + 0.001f && r2 <= 0.67f + 0.001f;
                    sideOk &= Mathf.Sign(ns.x - aim.x) == dir && Mathf.Sign(nn.x - aim.x) == dir
                        && Mathf.Abs(ns.y - aim.y) < 0.01f && Mathf.Abs(nn.y - aim.y) < 0.01f;
                    clearOk &= (ns - aim).magnitude >= FlockFiveApp.GloveMinStartHands * dh - 0.01f
                        && (nn - aim).magnitude >= FlockFiveApp.GloveMinStartHands * dh - 0.01f;
                    // a start already near the target is never pushed outward
                    var close = aim + away * (0.2f * dh);
                    neverLonger &= (FlockFiveApp.GloveScaleTravel(close, aim, dh) - aim).magnitude <= (close - aim).magnitude + 0.01f;
                    detail += " fx" + fx + ":" + r1.ToString("0.00") + "/" + r2.ToString("0.00");
                }
                Check("travel-ratio-le-0.67-" + names[d], ratioOk, detail);
                Check("side-rule-kept-" + names[d], sideOk, "");
                Check("start-clear-of-target-" + names[d], clearOk, "");
                Check("never-lengthens-" + names[d], neverLonger, "");
            }

            string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("shared-by-entry-and-near-start",
                src.Contains("return GloveScaleTravel(aim + (far - aim) * GloveEntryScale, aim, dh);")
                && src.Contains("p = GloveScaleTravel(p, aim, dh);"), "");
        }
    }
}
#endif

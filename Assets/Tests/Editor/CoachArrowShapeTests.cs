#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Brandon (2026-10-18): the tutorial pointer (CoachArrowAt) is a solid flat-top, pointed-bottom marker that hovers.
    public static class CoachArrowShapeTests
    {
        const string Out = "/tmp/flock-five-coach-arrow-shape-tests.txt";

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
                Debug.Log("[coacharrow] " + line);
            }
            foreach (float k in new[] { 1f, 1.5f, 2f })
            {
                float h = FlockFiveApp.CoachArrowHeight * k, w = FlockFiveApp.CoachArrowWidth * k;
                Check("flat top is full width k=" + k, Mathf.Approximately(FlockFiveApp.CoachArrowHalfWidth(0f, k) * 2f, w), "");
                Check("point at the bottom k=" + k, FlockFiveApp.CoachArrowHalfWidth(h, k) < 0.01f, "");
                float prev = float.MaxValue; bool mono = true;
                for (float d = 0f; d <= h; d += 0.5f)
                {
                    float hw = FlockFiveApp.CoachArrowHalfWidth(d, k);
                    if (hw > prev + 0.0001f) mono = false;
                    prev = hw;
                }
                Check("width never grows going down (solid, no barbs/stem) k=" + k, mono, "");
                Check("narrower near the tip than the top k=" + k, FlockFiveApp.CoachArrowHalfWidth(h * 0.9f, k) < FlockFiveApp.CoachArrowHalfWidth(h * 0.5f, k) + 0.001f
                    && FlockFiveApp.CoachArrowHalfWidth(h * 0.9f, k) < w * 0.25f, "");
                var poly = FlockFiveApp.CoachArrowPolygon(new Vector2(100f, 200f), k);
                Check("polygon: 5 corners, flat top, tip is lowest k=" + k, poly.Length == 5 && Mathf.Approximately(poly[0].y, poly[1].y)
                    && Mathf.Approximately(poly[3].y, 200f) && poly[3].y > poly[0].y && Mathf.Approximately(poly[3].x, 100f), "");
                Check("polygon scales with UI size k=" + k, Mathf.Approximately(poly[1].x - poly[0].x, w) && Mathf.Approximately(poly[3].y - poly[0].y, h), "");
            }
            // Hover: gentle, eased, visible, 1-1.5 s period.
            float p = FlockFiveApp.CoachArrowBobPeriod;
            Check("bob period 1-1.5 s", p >= 1f && p <= 1.5f, "" + p);
            Check("bob amplitude clearly visible but gentle (4-8 px)", FlockFiveApp.CoachArrowBobAmp >= 4f && FlockFiveApp.CoachArrowBobAmp <= 8f, "");
            float max = 0f, min = 0f;
            for (float t = 0f; t < p; t += p / 200f)
            {
                float o = FlockFiveApp.CoachArrowBobOffset(t, 1f);
                max = Mathf.Max(max, o); min = Mathf.Min(min, o);
            }
            Check("bob swings both ways by the amplitude", Mathf.Abs(max - FlockFiveApp.CoachArrowBobAmp) < 0.05f && Mathf.Abs(min + FlockFiveApp.CoachArrowBobAmp) < 0.05f, max + " " + min);
            Check("bob repeats every period", Mathf.Abs(FlockFiveApp.CoachArrowBobOffset(0.3f, 1f) - FlockFiveApp.CoachArrowBobOffset(0.3f + p, 1f)) < 0.001f, "");
            // Hit/gate behavior untouched: the arrow is paint-only.
            string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            int i = src.IndexOf("void CoachArrowAt(Rect target, float s)");
            int j = src.IndexOf("static void CoachArrowFill(", i);
            string body = i < 0 || j < 0 ? "" : src.Substring(i, j - i);
            Check("CoachArrowAt stays paint-only (no input/gate)", body.Length > 0 && body.Contains("GuiPaint()") && !body.Contains("Event.current.Use")
                && !body.Contains("GUI.Button") && !body.Contains("gate."), "");
            string end = fail == 0 ? "COACHARROW_OK " + pass : "COACHARROW_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif

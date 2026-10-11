#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 82 pass b82f. Shared perch: no snap, no idle gap, eased settle.
    static class Build82TestsRedistribute
    {
        [MenuItem("Flock Five/Build 82 Redistribute Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build82f-redist] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            const float dt = 1f / 60f;
            int[] index = { 0, 4, 9 };
            var starts = new[]
            {
                new Vector3(-1.2f, 3.4f, 0f),
                new Vector3(0.4f, 4.1f, 0f),
                new Vector3(2.2f, 2.8f, 0f),
            };
            var dests = new[]
            {
                new Vector3(-2.4f, 0.6f, 0f),
                new Vector3(0.2f, 1.1f, 0f),
                new Vector3(1.6f, 0.4f, 0f),
            };
            bool smooth = true;
            bool moving = true;
            bool seated = true;
            string detail = "continuous";
            float fleet = 0f;
            for (int i = 0; i < index.Length; i++)
            {
                fleet = Mathf.Max(fleet, FlockRedistribute.Duration(index[i]));
                float dist = Vector3.Distance(starts[i], dests[i]);
                float limit = Mathf.Max(1.15f, dist * 0.35f);
                var prev = FlockRedistribute.Sample(starts[i], dests[i], index[i], 0f);
                if ((prev - starts[i]).sqrMagnitude > 1e-8f)
                {
                    smooth = false;
                    detail = "index " + index[i] + " left early";
                    break;
                }
                float moved = 0f;
                for (float t = dt; t <= FlockRedistribute.Duration(index[i]) + dt; t += dt)
                {
                    var p = FlockRedistribute.Sample(starts[i], dests[i], index[i], t);
                    float step = Vector3.Distance(prev, p);
                    if (step > limit)
                    {
                        smooth = false;
                        detail = "index " + index[i] + " step " + step.ToString("0.00") + " limit " + limit.ToString("0.00");
                        break;
                    }
                    if (t <= dt * 2f) moved += step;
                    prev = p;
                }
                if (!smooth) break;
                if (moved < 0.01f)
                {
                    moving = false;
                    detail = "index " + index[i] + " idle at the start";
                    break;
                }
                var landed = FlockRedistribute.Sample(starts[i], dests[i], index[i], FlockRedistribute.Duration(index[i]));
                if (Vector3.Distance(landed, dests[i]) > 0.001f)
                {
                    seated = false;
                    detail = "index " + index[i] + " ended " + landed + " want " + dests[i];
                    break;
                }
            }

            float t0 = FlockRedistribute.Duration(0);
            var firstDone = FlockRedistribute.Sample(starts[0], dests[0], 0, t0);
            var lastAtFirst = FlockRedistribute.Sample(starts[2], dests[2], 9, t0);
            var lastStart = FlockRedistribute.Sample(starts[2], dests[2], 9, dt);
            bool overlap = Vector3.Distance(firstDone, dests[0]) <= 0.001f
                && Vector3.Distance(lastAtFirst, dests[2]) > 0.05f
                && Vector3.Distance(lastStart, starts[2]) > 0.01f
                && FlockRedistribute.Duration(9) > FlockRedistribute.Duration(0);

            float peak = 0f;
            for (int i = 0; i <= 40; i++)
            {
                float e = FlockRedistribute.Ease(i / 40f);
                if (e > peak) peak = e;
            }
            bool ease = peak > 1.001f
                && Mathf.Abs(FlockRedistribute.Ease(0f)) < 1e-5f
                && Mathf.Abs(FlockRedistribute.Ease(1f) - 1f) < 1e-5f;
            float dur = FlockRedistribute.Duration(4);
            bool sway = FlockRedistribute.Sway(4, 0f) == 0f
                && FlockRedistribute.Sway(4, dur) == 0f
                && FlockRedistribute.Sway(4, dur * 0.9f) > 0.2f
                && FlockRedistribute.Sway(4, dur * 0.5f) == 0f;

            Check("motion", smooth && moving && seated && overlap && ease && sway, detail
                + " peak=" + peak.ToString("0.000") + " fleet=" + fleet.ToString("0.00"));

            string app = File.ReadAllText(Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "Assets/_Project/Scripts/App/FlockFiveApp.cs"));
            int at = app.IndexOf("IEnumerator PerchOnRemain", System.StringComparison.Ordinal);
            int end = at >= 0 ? app.IndexOf("static IEnumerator PerchOne", at, System.StringComparison.Ordinal) : -1;
            string body = at >= 0 && end > at ? app.Substring(at, end - at) : "";
            int one = app.IndexOf("static IEnumerator PerchOne", System.StringComparison.Ordinal);
            int oneEnd = one >= 0 ? app.IndexOf("Vector3[] ScatterDests", one, System.StringComparison.Ordinal) : -1;
            string perch = one >= 0 && oneEnd > one ? app.Substring(one, oneEnd - one) : "";
            bool shared = body.Contains("FlockRedistribute.Duration")
                && !body.Contains("0.08f")
                && !body.Contains("0.35f")
                && perch.Contains("FlockRedistribute.Sample")
                && perch.Contains("LandSway");
            Check("shared-helper", shared, shared ? "perch uses the sampler" : "delay or snap still in PerchOnRemain");

            Debug.Log("[build82f-redist] " + (fail == 0 ? "BUILD82F_REDIST_OK " + pass : "BUILD82F_REDIST_FAIL " + fail));
            if (fail > 0) throw new System.Exception("BUILD82F_REDIST_FAIL " + fail);
        }
    }
}
#endif

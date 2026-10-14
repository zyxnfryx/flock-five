#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FlockFive.Editor
{
    // The sky is the level index on one day-cycle. Consecutive stages walk forward
    // (night rolls into dawn). Cold load and the finale stay on that same path.
    public static class SkyCycleTests
    {
        const string Out = "/tmp/flock-five-sky-tests.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[sky] " + line);
            }

            GameObject go = null;
            float keepScale = Time.timeScale;
            try
            {
                GamePause.Reset();
                AdPause.ResetForTest();
                Time.timeScale = 1f;
                SkyCycle.ResetForTest();

                const float beat = 1f / SkyCycle.Beats;
                bool beats = true;
                string beatDetail = "";
                for (int i = 0; i < 48 && beats; i++)
                {
                    float d = SkyCycle.ForwardDelta(SkyCycle.PhaseForLevel(i), SkyCycle.PhaseForLevel(i + 1));
                    float rev = SkyCycle.ForwardDelta(SkyCycle.PhaseForLevel(i + 1), SkyCycle.PhaseForLevel(i));
                    if (Mathf.Abs(d - beat) > 1e-5f || d > rev)
                    {
                        beats = false;
                        beatDetail = i + " forward " + d.ToString("0.0000") + " rev " + rev.ToString("0.0000");
                    }
                }
                Check("consecutive-beats-forward", beats && SkyCycle.PhaseForLevel(0) == 0f
                    && Mathf.Abs(SkyCycle.PhaseForLevel(5) - SkyCycle.NightPhase) < 1e-5f
                    && SkyCycle.PhaseForLevel(6) == 0f
                    && SkyCycle.PhaseForLevel(-3) == 0f,
                    beatDetail.Length == 0 ? "step " + beat.ToString("0.0000") : beatDetail);

                bool path = true;
                string pathDetail = "";
                float maxUStep = 0f;
                for (int i = 0; i < 18 && path; i++)
                {
                    float from = SkyCycle.PhaseForLevel(i);
                    float to = SkyCycle.PhaseForLevel(i + 1);
                    float prev = SkyCycle.ForwardAt(from, to, 0f);
                    if (Mathf.Abs(SkyCycle.ForwardDelta(prev, from)) > 1e-4f
                        && Mathf.Abs(SkyCycle.ForwardDelta(from, prev)) > 1e-4f)
                    {
                        path = false;
                        pathDetail = "start " + i;
                        break;
                    }
                    for (int s = 1; s <= 40; s++)
                    {
                        float u = s / 40f;
                        float p = SkyCycle.ForwardAt(from, to, u);
                        float fwd = SkyCycle.ForwardDelta(prev, p);
                        float rev = SkyCycle.ForwardDelta(p, prev);
                        float dd = Mathf.Abs(SkyCycle.Darkness(p) - SkyCycle.Darkness(prev));
                        if (fwd > maxUStep) maxUStep = fwd;
                        if (fwd > rev + 1e-4f || fwd > 0.02f || dd > 0.08f)
                        {
                            path = false;
                            pathDetail = i + " u " + u.ToString("0.00") + " fwd " + fwd.ToString("0.000") + " dark " + dd.ToString("0.000");
                            break;
                        }
                        prev = p;
                    }
                    if (!path) break;
                    float endErr = Mathf.Min(
                        SkyCycle.ForwardDelta(prev, to),
                        SkyCycle.ForwardDelta(to, prev));
                    if (endErr > 1e-3f)
                    {
                        path = false;
                        pathDetail = "end " + i + " err " + endErr.ToString("0.000");
                    }
                }
                Check("shared-path-no-backward-no-cut", path, pathDetail.Length == 0 ? "max step " + maxUStep.ToString("0.000") : pathDetail);

                bool curve = true;
                string curveDetail = "";
                float prevD = SkyCycle.Darkness(0f);
                for (int i = 1; i <= 360; i++)
                {
                    float p = i / 360f;
                    if (p >= 1f) p = 0f;
                    float d = SkyCycle.Darkness(p);
                    float dd = Mathf.Abs(d - prevD);
                    if (dd > 0.035f)
                    {
                        curve = false;
                        curveDetail = "p " + p.ToString("0.000") + " dd " + dd.ToString("0.000");
                        break;
                    }
                    prevD = d;
                }
                float meet = Mathf.Abs(SkyCycle.Darkness(0f) - SkyCycle.Darkness(0.999f));
                Check("darkness-meets-around-the-wrap", curve && meet < 0.02f,
                    curveDetail.Length == 0 ? "dawn-to-late " + meet.ToString("0.000") : curveDetail);

                go = NewSky();
                var cam = AttachCam(go);
                SkyCycle.Present(5);
                Color clear = cam.backgroundColor;
                // The garden camera is born brown. A night load must already be the night clear.
                bool clearNight = clear.b > clear.r + 0.03f && clear.r < 0.12f;
                float night = SkyCycle.Phase;
                bool held = Mathf.Abs(SkyCycle.ForwardDelta(night, SkyCycle.PhaseForLevel(5))) < 1e-4f
                    && Mathf.Abs(SkyCycle.ForwardDelta(SkyCycle.PhaseForLevel(5), night)) < 1e-4f;
                for (int i = 0; i < 8 && held; i++)
                {
                    PlayClock.StepForTest(1f / 60f);
                    float p = SkyCycle.Phase;
                    if (SkyCycle.ForwardDelta(night, p) > 0.002f) held = false;
                }
                Check("cold-load-parks-no-pop", held && night > 0.8f && clearNight,
                    "phase " + night.ToString("0.000") + " dusk " + SkyCycle.Dusk.ToString("0.000")
                    + " clear " + clear.r.ToString("0.00") + "," + clear.g.ToString("0.00") + "," + clear.b.ToString("0.00"));

                int popped = 0;
                for (int level = 0; level < SkyCycle.Beats; level++)
                {
                    Drop(ref go);
                    SkyCycle.ResetForTest();
                    go = NewSky();
                    SkyCycle.Present(level);
                    float want = SkyCycle.PhaseForLevel(level);
                    float got = SkyCycle.Phase;
                    if (Mathf.Min(SkyCycle.ForwardDelta(got, want), SkyCycle.ForwardDelta(want, got)) > 1e-4f)
                        popped++;
                }
                Check("cold-load-every-beat", popped == 0, "off " + popped);

                Drop(ref go);
                SkyCycle.ResetForTest();
                string stageDetail = "";
                bool stages = WalkStages(ref go, 0, out stageDetail)
                    && WalkStages(ref go, 4, out stageDetail)
                    && WalkStages(ref go, 5, out stageDetail)
                    && WalkStages(ref go, 11, out stageDetail);
                Check("stage-glide-forward-no-cut", stages, stageDetail);

                Drop(ref go);
                SkyCycle.ResetForTest();
                go = NewSky();
                var finCam = AttachCam(go);
                SkyCycle.Present(2);
                float noon = SkyCycle.Phase;
                Color noonBg = finCam.backgroundColor;
                SkyCycle.SnapNight();
                float atSnap = SkyCycle.Phase;
                Color snapBg = finCam.backgroundColor;
                bool finalePop = Mathf.Min(SkyCycle.ForwardDelta(noon, atSnap), SkyCycle.ForwardDelta(atSnap, noon)) > 1e-3f;
                bool camPop = Mathf.Abs(noonBg.r - snapBg.r) > 0.04f
                    || Mathf.Abs(noonBg.g - snapBg.g) > 0.04f
                    || Mathf.Abs(noonBg.b - snapBg.b) > 0.04f;
                finalePop = finalePop || camPop;
                bool finale = !finalePop && WalkUntil(SkyCycle.NightPhase, 8f, out stageDetail);
                float atNight = SkyCycle.Phase;
                SkyCycle.EaseToSaved(1.1f);
                float atEase = SkyCycle.Phase;
                bool easePop = Mathf.Min(SkyCycle.ForwardDelta(atNight, atEase), SkyCycle.ForwardDelta(atEase, atNight)) > 1e-3f;
                bool back = !easePop && WalkUntil(SkyCycle.PhaseForLevel(2), 8f, out stageDetail);
                Check("finale-no-pop-forward", finale && back && !finalePop && !easePop, stageDetail);

                string root = Application.dataPath;
                string wb = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/WorldBuilder.cs"));
                string fin = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FinaleShow.cs"));
                int presentAt = wb.IndexOf("SkyCycle.Present(LevelData.Index)");
                int applyAt = wb.IndexOf("fit.Apply()");
                Check("present-before-first-tint", presentAt >= 0 && applyAt > presentAt
                    && fin.Contains("SkyCycle.SnapNight()") && fin.Contains("SkyCycle.EaseToSaved("),
                    "present " + presentAt + " apply " + applyAt);
            }
            catch (System.Exception ex)
            {
                Check("sky-threw", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
                SkyCycle.ResetForTest();
                AdPause.ResetForTest();
                GamePause.Reset();
                Time.timeScale = keepScale;
            }

            string end = fail == 0 ? "SKY_OK " + pass : "SKY_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[sky] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static Camera AttachCam(GameObject go)
        {
            var camGo = new GameObject("sky-cam");
            camGo.transform.SetParent(go.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.22f, 0.18f, 0.10f);
            var sky = go.GetComponent<SkyCycle>();
            typeof(SkyCycle).GetField("_cam", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sky, cam);
            return cam;
        }

        static GameObject NewSky()
        {
            var go = new GameObject("sky-test");
            var sky = go.AddComponent<SkyCycle>();
            // Edit-mode AddComponent does not always run OnEnable, so Instance
            // would stay null and Present would only queue.
            typeof(SkyCycle).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).SetValue(null, sky);
            return go;
        }

        static void Drop(ref GameObject go)
        {
            if (go == null) return;
            SkyCycle.Remember();
            Object.DestroyImmediate(go);
            go = null;
            if (SkyCycle.Instance != null)
                typeof(SkyCycle).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).SetValue(null, null);
        }

        // Destroy carries the phase. The next sky must open on it and glide forward.
        static bool WalkStages(ref GameObject go, int index, out string detail)
        {
            Drop(ref go);
            SkyCycle.ResetForTest();
            go = NewSky();
            SkyCycle.Present(index);
            float from = SkyCycle.Phase;
            Drop(ref go);
            go = NewSky();
            SkyCycle.Present(index + 1);
            float opened = SkyCycle.Phase;
            float openErr = Mathf.Min(SkyCycle.ForwardDelta(opened, from), SkyCycle.ForwardDelta(from, opened));
            if (openErr > 1e-3f)
            {
                detail = index + " opened " + opened.ToString("0.000") + " wanted " + from.ToString("0.000");
                return false;
            }
            bool ok = WalkUntil(SkyCycle.PhaseForLevel(index + 1), 4f, out detail);
            if (!ok) detail = index + " " + detail;
            else detail = index + " -> " + (index + 1);
            return ok;
        }

        static bool WalkUntil(float target, float seconds, out string detail)
        {
            float maxStep = SkyCycle.PhasePerSecond * 1.5f / 60f * 1.25f;
            int n = Mathf.CeilToInt(seconds * 60f);
            float prev = SkyCycle.Phase;
            detail = "";
            for (int i = 0; i < n; i++)
            {
                PlayClock.StepForTest(1f / 60f);
                float p = SkyCycle.Phase;
                float fwd = SkyCycle.ForwardDelta(prev, p);
                float back = SkyCycle.ForwardDelta(p, prev);
                float dd = Mathf.Abs(SkyCycle.Darkness(p) - SkyCycle.Darkness(prev));
                if (fwd > back + 1e-4f || fwd > maxStep || dd > 0.05f)
                {
                    detail = "step " + i + " fwd " + fwd.ToString("0.0000") + " back " + back.ToString("0.0000")
                        + " dark " + dd.ToString("0.000") + " p " + p.ToString("0.000");
                    return false;
                }
                prev = p;
            }
            float err = Mathf.Min(SkyCycle.ForwardDelta(prev, target), SkyCycle.ForwardDelta(target, prev));
            if (err > 0.01f)
            {
                detail = "ended " + prev.ToString("0.000") + " wanted " + target.ToString("0.000");
                return false;
            }
            detail = "at " + prev.ToString("0.000");
            return true;
        }
    }
}
#endif

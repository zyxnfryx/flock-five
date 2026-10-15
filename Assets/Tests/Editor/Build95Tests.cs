#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b95: Daily popup seated once (no recenter after the first spin), Go VIP sheen rests between
    // sweeps, LEVEL idle rim glimmers gone (press burst kept), plane idle 15s / 40s.
    public static class Build95Tests
    {
        const string Out = "/tmp/flock-five-b95-tests.txt";

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
                Debug.Log("[build95] " + line);
            }
            float? keep = DeviceFit.TestWindow;
            try
            {
                // Daily popup: every chrome (claim flower, wheel only, collect, collect + watch) seats the
                // card at the same x/y/width, so nothing recenters after the first spin.
                var faces = new[] { new Vector2(1179f, 2556f), new Vector2(1536f, 2048f) };
                string[] tags = { "iphone", "ipad" };
                for (int d = 0; d < faces.Length; d++)
                {
                    DeviceFit.TestWindow = faces[d].x / faces[d].y;
                    float s = DeviceFit.Ui(faces[d].x, faces[d].y);
                    var c0 = FlockFiveApp.DailyAuditCard(s, 0);
                    bool same = true;
                    string det = "";
                    for (int c = 1; c <= 3; c++)
                    {
                        var r = FlockFiveApp.DailyAuditCard(s, c);
                        if (Mathf.Abs(r.x - c0.x) > 0.01f || Mathf.Abs(r.y - c0.y) > 0.01f || Mathf.Abs(r.width - c0.width) > 0.01f) same = false;
                        det += " c" + c + "=" + r.y.ToString("0.0");
                    }
                    Check("daily card fixed across chromes " + tags[d], same, "c0=" + c0.y.ToString("0.0") + det);
                }

                // Go VIP / shared sheen: a full sweep, then a rest of 1.5..2.5 s of u 0 (invisible).
                float period = PushButton.SheenPeriod, pause = PushButton.SheenPauseSeconds;
                Check("sheen pause in range", pause >= 1.5f && pause <= 2.5f, pause.ToString());
                Check("sheen sweeps then rests", Mathf.Abs(PushButton.SheenU(period * 0.5f) - 0.5f) < 0.01f
                    && PushButton.SheenU(period + pause * 0.5f) == 0f
                    && PushButton.SheenU(period + pause - 0.01f) == 0f, "");
                Check("sheen repeats", Mathf.Abs(PushButton.SheenU(period + pause + period * 0.5f) - 0.5f) < 0.01f, "");
                Check("rest is invisible", PushButton.OvalFade(PushButton.SheenU(period + 0.1f)) == 0f, "");

                // LEVEL idle rim glimmers removed; press burst code intact.
                string wow = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.PressWow.cs");
                int idle = wow.IndexOf("static void DrawLevelIdleLife(");
                int idleEnd = wow.IndexOf("float _levelPressAt", idle);
                string idleBody = idle >= 0 && idleEnd > idle ? wow.Substring(idle, idleEnd - idle) : "";
                Check("no idle comets/twinkles/catch-light", idleBody.IndexOf("OnFlatRing") < 0 && idleBody.IndexOf("DrawAt(") < 0
                    && wow.IndexOf("LevelOrbitSpeed") < 0 && wow.IndexOf("LevelRimAng") < 0, "");
                Check("press burst intact", wow.IndexOf("static void DrawLevelPressOver(") > 0 && wow.IndexOf("DrawLevelPressUnder(") > 0
                    && File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs").IndexOf("SparkleFx.DrawAround(rest, 0.95f, true, 0.12f)") > 0, "");
                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
                Check("level shimmer has no glints", app.IndexOf("DrawFlowerShimmer(visual, 0f, 1f, false)") > 0, "");

                // Plane: 15 s first, 40 s later.
                Check("plane idle 15/40", FlockFiveApp.PlaneIdleFor(false) == 15f && FlockFiveApp.PlaneIdleFor(true) == 40f, "");
            }
            catch (System.Exception ex)
            {
                Check("b95", false, ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                DeviceFit.TestWindow = keep;
            }
            string end = fail == 0 ? "BUILD95_OK " + pass : "BUILD95_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif

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

                // Garden banner at the bottom: when it seats, it is under the lowest row and above the
                // restart / hive controls, on iPhone, iPad and SE; when it does not fit it returns false.
                var sizes = new[] { new Vector2(1179f, 2556f), new Vector2(1536f, 2048f), new Vector2(750f, 1334f) };
                string[] stags = { "iphone", "ipad", "se" };
                for (int d = 0; d < sizes.Length; d++)
                {
                    float w = sizes[d].x, h = sizes[d].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, h * 0.04f, w, h * 0.92f);
                    CornerAnchor.Garden(w, h, safe, out _, out _, out _, out var restart, out var hive);
                    int fit = 0, noFit = 0, bad = 0;
                    for (float rowFrac = 0.55f; rowFrac <= 0.90f; rowFrac += 0.05f)
                        for (float bandH = 90f * s; bandH <= 260f * s; bandH += 40f * s)
                        {
                            float rowBottom = h * rowFrac;
                            bool ok = FlockFiveApp.BottomBannerSeat(bandH, rowBottom, restart, hive, h, safe.yMin, 8f * s, out float y);
                            if (!ok) { noFit++; continue; }
                            fit++;
                            var band = new Rect(0f, y, w, bandH);
                            if (band.yMin < rowBottom || band.Overlaps(restart) || band.Overlaps(hive) || band.yMax > h) bad++;
                        }
                    Check("bottom banner never touches row/restart/hive " + stags[d], bad == 0, "fit " + fit + " nofit " + noFit + " bad " + bad);
                }

                // Press burst: glints start on the upper half of the button ring (inverted U), centred.
                {
                    float W = 1179f, H = 2556f;
                    var src = new Rect(W * 0.14f, H * 0.62f, W * 0.72f, W * 0.72f);
                    var buf = new LevelCurtain.Glint[LevelCurtain.Count * 2];
                    int n = LevelCurtain.Fill(0.03f, W, H, src, buf);
                    float ringCy = src.y + src.height * SparkleFx.FlatRingCy;
                    float minX = 1e9f, maxX = -1e9f, maxY = -1e9f;
                    for (int i = 0; i < n; i++) { minX = Mathf.Min(minX, buf[i].X); maxX = Mathf.Max(maxX, buf[i].X); maxY = Mathf.Max(maxY, buf[i].Y); }
                    Check("burst starts on the top curve", n > 0 && maxY < ringCy && minX < src.center.x - src.width * 0.2f && maxX > src.center.x + src.width * 0.2f,
                        "n " + n + " maxY " + maxY.ToString("0") + " ringCy " + ringCy.ToString("0") + " x " + minX.ToString("0") + ".." + maxX.ToString("0"));
                }

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

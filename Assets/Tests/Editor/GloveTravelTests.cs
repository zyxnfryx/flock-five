#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // First-garden finger: seated on the control, press only, no travel.
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

            // First-garden cues no longer travel. The finger is seated and only presses.
            var go = new GameObject("glove-travel-stationary");
            var winOn = typeof(PhoneArea).GetField("EditorWindowOn", BindingFlags.NonPublic | BindingFlags.Static);
            var winW = typeof(PhoneArea).GetField("EditorWindowW", BindingFlags.NonPublic | BindingFlags.Static);
            var winH = typeof(PhoneArea).GetField("EditorWindowH", BindingFlags.NonPublic | BindingFlags.Static);
            var safeTop = typeof(PhoneArea).GetField("EditorSafeTop", BindingFlags.NonPublic | BindingFlags.Static);
            var safeBot = typeof(PhoneArea).GetField("EditorSafeBottom", BindingFlags.NonPublic | BindingFlags.Static);
            bool keepOn = (bool)winOn.GetValue(null);
            int keepW = (int)winW.GetValue(null);
            int keepH = (int)winH.GetValue(null);
            float keepTop = (float)safeTop.GetValue(null);
            float keepBot = (float)safeBot.GetValue(null);
            try
            {
                var app = go.AddComponent<FlockFiveApp>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                var tap = typeof(FlockFiveApp).GetMethod("CoachTapAt", flags);
                var sizes = new[] { new Vector2(820f, 1180f), new Vector2(402f, 874f), new Vector2(412f, 915f), new Vector2(360f, 800f) };
                foreach (var size in sizes)
                {
                    winOn.SetValue(null, true);
                    winW.SetValue(null, (int)size.x);
                    winH.SetValue(null, (int)size.y);
                    safeTop.SetValue(null, 59f);
                    safeBot.SetValue(null, 34f);
                    var target = new Rect(80f, 120f, 120f, 72f);
                    var label = FlockFiveApp.CoachTapLabel(target);
                    typeof(FlockFiveApp).GetField("_coachTapOn", flags).SetValue(app, false);
                    typeof(FlockFiveApp).GetField("_glovePhase", flags).SetValue(app, 0f);
                    typeof(FlockFiveApp).GetField("_coachFade", flags).SetValue(app, 1f);
                    Vector2 start = default;
                    var path = new System.Text.StringBuilder();
                    Vector2 end = default;
                    float bestDip = -1f;
                    float span = 0f;
                    bool vis = false;
                    for (int i = 0; i < 24; i++)
                    {
                        tap.Invoke(app, new object[] { target, label, 0.06f, null });
                        vis = (bool)typeof(FlockFiveApp).GetField("_gloveVis", flags).GetValue(app);
                        var tip = (Vector2)typeof(FlockFiveApp).GetField("_gloveShown", flags).GetValue(app);
                        float dip = (float)typeof(FlockFiveApp).GetField("_gloveDip", flags).GetValue(app);
                        if (i == 0) start = tip;
                        path.Append(tip.x.ToString("0")).Append(',').Append(tip.y.ToString("0")).Append(' ');
                        span = Mathf.Max(span, (tip - start).magnitude);
                        if (dip >= bestDip) { bestDip = dip; end = tip; }
                    }
                    var pose = FlockFiveApp.CoachTapSide(target, label, PhoneArea.W, PhoneArea.H, PhoneArea.Safe, default, default, default);
                    bool touch = pose.FromLeft
                        ? Mathf.Abs(pose.Glove.xMax - target.xMin) <= 1.2f
                        : Mathf.Abs(pose.Glove.xMin - target.xMax) <= 1.2f;
                    bool onEdge = touch && Mathf.Abs(end.x - pose.End.x) < 1.5f && !label.Contains(pose.End);
                    bool poke = span <= FlockFiveApp.CoachTapPressPx + 0.6f && Mathf.Abs(start.y - end.y) < 1.5f;
                    Debug.Log("[coach-tap] travel " + (int)size.x
                        + " start " + start + " path " + path + " end " + end + " dip " + bestDip.ToString("0.00"));
                    Check("stationary-tap-" + (int)size.x, vis && onEdge && poke && bestDip > 0.9f,
                        "span " + span.ToString("0.0") + " end " + end);
                }
            }
            finally
            {
                winOn.SetValue(null, keepOn);
                winW.SetValue(null, keepW);
                winH.SetValue(null, keepH);
                safeTop.SetValue(null, keepTop);
                safeBot.SetValue(null, keepBot);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
#endif

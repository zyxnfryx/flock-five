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
                    typeof(FlockFiveApp).GetField("_coachFade", flags).SetValue(app, 1f);
                    Vector2 first = default;
                    float span = 0f;
                    bool ok = true;
                    for (int i = 0; i < 8; i++)
                    {
                        tap.Invoke(app, new object[] { target, label, 0.18f, null });
                        bool vis = (bool)typeof(FlockFiveApp).GetField("_gloveVis", flags).GetValue(app);
                        var tip = (Vector2)typeof(FlockFiveApp).GetField("_gloveShown", flags).GetValue(app);
                        if (i == 0) first = tip;
                        span = Mathf.Max(span, (tip - first).magnitude);
                        ok &= vis && target.Contains(tip) && !label.Contains(tip);
                    }
                    ok &= span <= FlockFiveApp.CoachTapPressPx + 0.6f;
                    Check("stationary-tap-" + (int)size.x, ok, "span " + span.ToString("0.0"));
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

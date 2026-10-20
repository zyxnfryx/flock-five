#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 134 feel: kit read, splash breath, refused drop, lightning bolt.
    // Edit-mode. Registered in EnsurePlayable.RunEditorTests.
    public static class Build134FeelTests
    {
        const string Out = "/tmp/flock-five-b134-feel-tests.txt";

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
                Debug.Log("[build134-feel] " + line);
            }

            float row = WorldBuilder.RowGap;
            Check("select-lift-under-row",
                BirdIdle.SelectLift > 0.15f && BirdIdle.SelectLift < row * 0.45f
                && BirdIdle.HoverPerch > BirdIdle.SelectLift && BirdIdle.HoverPerch < 1.05f,
                "lift " + BirdIdle.SelectLift.ToString("0.00") + " hover " + BirdIdle.HoverPerch.ToString("0.00")
                + " row " + row.ToString("0.00"));

            float quarter = 0.25f / BirdIdle.SplashBreathHz;
            BirdIdle.SplashIdle(quarter, 0f, out float sx, out float sy, out float nod, out bool shimmerAtPeak);
            Check("splash-breath-peaks",
                sx > 1f && sx < 1.04f && sy < 1f && Mathf.Abs(nod) < 0.05f,
                "sx " + sx.ToString("0.000") + " sy " + sy.ToString("0.000") + " nod " + nod.ToString("0.000"));

            BirdIdle.SplashIdle(0f, 0f, out sx, out sy, out nod, out bool shimmerAt0);
            Check("splash-nod-quadrature",
                Mathf.Abs(sx - 1f) < 0.001f && Mathf.Abs(nod - BirdIdle.SplashNodDeg) < 0.001f,
                "sx " + sx.ToString("0.000") + " nod " + nod.ToString("0.000"));

            BirdIdle.SplashIdle(1f, 0f, out _, out _, out _, out bool shimmerAt1);
            Check("splash-shimmer-window",
                shimmerAt0 && !shimmerAt1 && BirdIdle.SplashShimmerDur < 0.35f
                && BirdIdle.SplashBreathHz > 1.15f && BirdIdle.SplashBreathHz < 1.65f,
                "at0 " + shimmerAt0 + " at1 " + shimmerAt1 + " hz " + BirdIdle.SplashBreathHz.ToString("0.00"));
            _ = shimmerAtPeak;

            string idle = File.ReadAllText("Assets/_Project/Scripts/View/BirdIdle.cs");
            int late = idle.IndexOf("void LateUpdate()");
            int after = idle.IndexOf("void SetReaction(", late);
            string lateBody = late >= 0 && after > late ? idle.Substring(late, after - late) : "";
            Check("splash-not-on-garden-birds",
                lateBody.Length > 40 && lateBody.IndexOf("SplashIdle") < 0,
                "late " + late + " after " + after);

            float crown = BirdIdle.ShareOf(BirdKit.Crown);
            float bow = BirdIdle.ShareOf(BirdKit.Bow);
            Check("kit-share-bolder",
                crown >= 1.65f && crown <= 1.85f && bow >= 1.85f && bow <= 2.05f && bow > crown
                && BirdIdle.KitInkMul > 1.10f && BirdIdle.KitInkMul < 1.28f
                && BirdIdle.KitRimMul > 1.02f && BirdIdle.KitRimMul < BirdIdle.KitInkMul,
                "crown " + crown.ToString("0.00") + " bow " + bow.ToString("0.00"));
            Check("kit-rim-seated",
                idle.Contains("PlaceKitRim") && idle.Contains("KitInkMul") && idle.Contains("SeatOutline"),
                "rim helpers");

            string branch = File.ReadAllText("Assets/_Project/Scripts/View/BranchView.cs");
            Check("select-uses-soft-lift",
                branch.Contains("BirdIdle.SelectLift") && branch.IndexOf("tip ? 1.15f") < 0
                && branch.Contains("NopeFlock") && branch.Contains("BranchGlow"),
                "lift and glow");
            Check("locked-wood-tint",
                branch.Contains("0.55f, 0.62f, 0.52f"),
                "locked bark");

            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            int handle = app.IndexOf("void HandleTap(Vector2 world)");
            int refuse = app.IndexOf("void RefuseDrop(int hit)", handle);
            string tap = handle >= 0 && refuse > handle ? app.Substring(handle, refuse - handle) : "";
            int lesson = tap.IndexOf("LessonTapBlocked()");
            int coach = tap.IndexOf("CoachReject(");
            int nope = tap.IndexOf("NopeFlock()");
            Check("refuse-stays-behind-tutorial-gate",
                lesson >= 0 && coach > lesson && nope > coach && tap.Contains("RefuseDrop(hit)"),
                "lesson " + lesson + " coach " + coach + " nope " + nope);

            string storm = File.ReadAllText("Assets/_Project/Scripts/App/GardenStorm.cs");
            string bolt = File.ReadAllText("Assets/_Project/Art/Resources/Shaders/Bolt.shader");
            Check("bolt-is-its-own-pass",
                storm.Contains("FlockFive/Bolt") && storm.Contains("if (a > 0.42f) a = 0.42f;")
                && storm.Contains("0.36f * u") && storm.Contains("CamShake.Bolt")
                && storm.Contains("StrikeAt") && storm.Contains("_strikeDark")
                && bolt.Contains("Blend One One") && bolt.Contains("UniversalForward")
                && bolt.IndexOf("_MainTex") < 0,
                "shader and caps");

            Debug.Log("[build134-feel] " + pass + " pass, " + fail + " fail");
        }
    }
}
#endif

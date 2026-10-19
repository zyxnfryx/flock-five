#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 115. Ostrich is ~15% bigger and stands on a ledge with a contact shadow, so he no longer
    // floats. Edit-mode only (no Unity launch needed to author it); registered in EnsurePlayable.RunEditorTests.
    public static class Build115OstrichGroundTests
    {
        const string Out = "/tmp/flock-five-b115-ground-tests.txt";

        [MenuItem("Flock Five/Build 115 Ostrich Ground Tests")]
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
                Debug.Log("[build115-ground] " + line);
            }

            var camGo = new GameObject("b115g-cam");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = WorldBuilder.CamOrtho;
            cam.aspect = 9f / 19.5f; // narrowest phone: the worst case for "stays on screen"
            try
            {
                // --- size: modest, named tunable ---
                float g = OstrichMark.GrowFactor;
                Check("grow-modest", g >= 1.10f && g <= 1.20f, "grow " + g);
                Check("body-scaled", Mathf.Approximately(OstrichMark.BodyScale, OstrichMark.BaseBodyScale * g)
                    && OstrichMark.BodyScale >= 1.5f, "body " + OstrichMark.BodyScale);
                Check("head-scaled", Mathf.Approximately(OstrichMark.RigScale, OstrichMark.BaseRigScale * g), "rig " + OstrichMark.RigScale);

                // --- feet do not move: ground height equals the pre-grow toe line ---
                float fy = 8.12f;
                float oldFoot = fy - 0.05f + OstrichMark.PortY - OstrichMark.BaseBodyDrop - OstrichMark.FootBelowPivot * OstrichMark.BaseBodyScale;
                Check("feet-unchanged-by-grow", Mathf.Abs(OstrichMark.GroundY(fy) - oldFoot) < 0.01f,
                    "now " + OstrichMark.GroundY(fy) + " was " + oldFoot);

                // --- stays on screen, legs and tail included ---
                float half = OstrichMark.ViewHalfWidth();
                Check("view-half-is-phone", Mathf.Abs(half - WorldBuilder.CamOrtho * 9f / 19.5f) < 0.01f, "half " + half);
                float span = OstrichMark.BodyBack + OstrichMark.BodyFront;
                Check("body-fits-width", span + 0.24f <= half * 2f, "span " + span + " width " + half * 2f);
                bool onScreen = true;
                var root0 = new GameObject("b115g-screen");
                try
                {
                    var m = OstrichMark.Attach(root0.transform, 3);
                    for (int s = 0; s < 3; s++)
                    {
                        float x = FeederRow.X(s, 3);
                        m.Preview(OstrichAct.Idle, 0, x, WorldBuilder.FeederY - 0.05f, false);
                        var body = root0.transform.Find("Ostrich/Body");
                        var bodySr = body != null ? body.GetComponent<SpriteRenderer>() : null;
                        var bb = bodySr != null ? bodySr.bounds : new Bounds();
                        onScreen &= bodySr != null && bb.min.x >= -half - 0.2f && bb.max.x <= half + 0.2f;
                        float beak = m.Head.transform.position.x + (x < -0.3f ? -1f : 1f) * OstrichMark.BeakReach;
                        onScreen &= Mathf.Abs(beak) <= half;
                    }
                }
                finally { Object.DestroyImmediate(root0); }
                Check("slots-on-screen", onScreen, "body bounds and beak inside +/-" + half);

                // --- ground sits clear of the bottom HUD zone ---
                float bottom = WorldBuilder.CamRestY - WorldBuilder.CamOrtho;
                Check("ground-above-hud", OstrichMark.GroundY(fy) - OstrichGround.LedgeDepth > bottom + 4f,
                    "ledge bottom " + (OstrichMark.GroundY(fy) - OstrichGround.LedgeDepth) + " screen bottom " + bottom);

                // --- draw order: above backdrop/life sheets, below body and legs ---
                Check("ground-order", OstrichGround.LedgeOrder > -12 && OstrichGround.LedgeOrder < OstrichGround.ShadowOrder
                    && OstrichGround.ShadowOrder < OstrichMark.BodyOrder, "ledge " + OstrichGround.LedgeOrder + " shadow " + OstrichGround.ShadowOrder);

                // --- textures: soft grass lip on top, fades to clear below; shadow soft-edged ---
                var top = OstrichGround.LedgePixel(10, 0.05f);
                var seam = OstrichGround.LedgePixel(10, 0.125f);
                var bot = OstrichGround.LedgePixel(10, 0.99f);
                Check("ledge-top-opaque-green", top.a > 250 && top.g > top.r, "a " + top.a + " g " + top.g + " r " + top.r);
                Check("ledge-seam-dark", seam.a > 250 && seam.g < top.g, "seam g " + seam.g);
                Check("ledge-fades-out", bot.a < 8, "bottom a " + bot.a);
                var st = OstrichGround.ShadowTexture();
                Check("shadow-soft", st.GetPixel(st.width / 2, st.height / 2).a > 0.9f && st.GetPixel(1, 1).a < 0.05f,
                    "center " + st.GetPixel(st.width / 2, st.height / 2).a);
                Object.DestroyImmediate(st);

                // --- the ground follows the feet and shows/hides with the ostrich (tutorial cue gate) ---
                var root = new GameObject("b115g-ground");
                try
                {
                    var mark = OstrichMark.Attach(root.transform, 3);
                    var gr = mark.Ground;
                    Check("ground-attached", gr != null && gr.Ledge != null && gr.Shadow != null, "");
                    if (gr != null)
                    {
                        Check("ground-shown-with-ostrich", gr.Shown && mark.OnStage, "");
                        float foot = OstrichMark.GroundY(WorldBuilder.FeederY);
                        Check("ledge-top-at-feet", Mathf.Abs(gr.Ledge.transform.position.y - OstrichGround.LedgeTop(foot)) < 0.001f,
                            "ledge y " + gr.Ledge.transform.position.y + " foot " + foot);
                        Check("shadow-at-feet-y", Mathf.Abs(gr.Shadow.transform.position.y - (foot - OstrichGround.ShadowDrop)) < 0.001f, "");
                        // Legs end at the toe line (leg sprite bottom, world) within a hair.
                        var leg = root.transform.Find("Ostrich/Body/LegFront");
                        var legSr = leg != null ? leg.GetComponent<SpriteRenderer>() : null;
                        Check("feet-land-on-ledge", legSr != null && Mathf.Abs(legSr.bounds.min.y - foot) < 0.12f,
                            "leg bottom " + (legSr != null ? legSr.bounds.min.y : float.NaN) + " foot " + foot);
                        // Shadow tracks the feet when the body swings to another feeder.
                        mark.Preview(OstrichAct.Idle, 0, FeederRow.X(0, 3), WorldBuilder.FeederY - 0.05f, false);
                        float sx0 = gr.Shadow.transform.position.x;
                        mark.Preview(OstrichAct.Idle, 0, FeederRow.X(2, 3), WorldBuilder.FeederY - 0.05f, false);
                        float sx2 = gr.Shadow.transform.position.x;
                        Check("shadow-follows-feet", Mathf.Abs(sx0 - sx2) > 0.5f && Mathf.Abs(sx0) <= half && Mathf.Abs(sx2) <= half,
                            "x0 " + sx0 + " x2 " + sx2);
                        // No colliders or raycast targets: the tutorial gate and taps never see the ground.
                        Check("ground-no-colliders", gr.GetComponentsInChildren<Collider2D>(true).Length == 0
                            && gr.GetComponentsInChildren<Collider>(true).Length == 0, "");
                        mark.Pose(0f, 0f, 0, false);
                        Check("ground-hides-with-ostrich", !gr.Shown && !gr.Shadow.enabled, "");
                    }
                }
                finally { Object.DestroyImmediate(root); }

                // --- every ostrich garden gets the ground: it is built inside OstrichMark.Attach ---
                bool only = true; int found = 0; var nums = "";
                for (int i = 0; i < LevelData.Count; i++)
                {
                    var lv = LevelData.Peek(i);
                    if (lv == null || !lv.Ostrich) continue;
                    found++; nums += lv.Number + " ";
                    only &= lv.Number == 27 || lv.Number == 38 || lv.Number == 43;
                }
                Check("ostrich-gardens-covered", found >= 2 && only, "ostrich gardens: " + nums);

                // --- b116: planted feet. Body y and the toe line stay fixed through every act and frame ---
                var rootA = new GameObject("b116-anim");
                try
                {
                    var m = OstrichMark.Attach(rootA.transform, 3);
                    float aimY = WorldBuilder.FeederY - 0.05f;
                    float wantFoot = OstrichMark.PlantedFootY(aimY);
                    float worstBody = 0f, worstLeg = 0f, worstRot = 0f, worstShadow = 0f;
                    string worstAt = "";
                    var bodyT = rootA.transform.Find("Ostrich/Body");
                    var legSrA = rootA.transform.Find("Ostrich/Body/LegFront").GetComponent<SpriteRenderer>();
                    for (int a = 0; a <= (int)OstrichAct.Shake; a++)
                    {
                        var act = (OstrichAct)a;
                        for (int f = 0; f < OstrichAnim.Count(act); f++)
                        {
                            m.Preview(act, f, FeederRow.X(1, 3), aimY, false);
                            float footNow = bodyT.position.y - OstrichMark.FootBelowPivot * OstrichMark.BodyScale;
                            float db = Mathf.Abs(footNow - wantFoot);
                            if (db > worstBody) { worstBody = db; worstAt = act + "#" + f; }
                            worstRot = Mathf.Max(worstRot, Mathf.Abs(Mathf.DeltaAngle(0f, bodyT.eulerAngles.z)));
                            worstShadow = Mathf.Max(worstShadow, Mathf.Abs(m.Ground.Shadow.transform.position.y - (OstrichMark.GroundY(WorldBuilder.FeederY) - OstrichGround.ShadowDrop)));
                            // Standing acts: the leg sprite itself stays on the toe line too (walk acts swing the legs).
                            if (act != OstrichAct.Flee && act != OstrichAct.Strut && act != OstrichAct.Daze)
                                worstLeg = Mathf.Max(worstLeg, Mathf.Abs(legSrA.bounds.min.y - OstrichMark.GroundY(WorldBuilder.FeederY)));
                        }
                    }
                    Check("feet-y-fixed-all-acts", worstBody < 0.02f, "max drift " + worstBody + " at " + worstAt);
                    Check("body-never-rocks", worstRot < 0.01f, "max rotation " + worstRot);
                    Check("standing-legs-stay-on-ledge", worstLeg < 0.12f, "max leg-bottom drift " + worstLeg);
                    Check("shadow-steady-y", worstShadow < 0.001f, "max " + worstShadow);
                    // Idle breathing at a tiny amplitude.
                    float lo = float.MaxValue, hi = float.MinValue;
                    for (int f = 0; f < OstrichAnim.IdleFrames; f++)
                    {
                        m.Preview(OstrichAct.Idle, f, FeederRow.X(1, 3), aimY, false);
                        lo = Mathf.Min(lo, bodyT.position.y); hi = Mathf.Max(hi, bodyT.position.y);
                    }
                    Check("idle-breath-tiny", hi - lo < 0.02f, "idle body travel " + (hi - lo));
                    // Head still moves naturally (neck/head not frozen).
                    m.Preview(OstrichAct.Peck, 4, FeederRow.X(1, 3), aimY, false);
                    float hy0 = m.Head.transform.position.y;
                    m.Preview(OstrichAct.Peck, 7, FeederRow.X(1, 3), aimY, false);
                    Check("head-still-moves", Mathf.Abs(m.Head.transform.position.y - hy0) > 0.05f, "head dy " + (m.Head.transform.position.y - hy0));
                }
                finally { Object.DestroyImmediate(rootA); }

                // --- b116: ledge + shadow + contact in every season/scene, tinted per season ---
                foreach (GardenScene sc in System.Enum.GetValues(typeof(GardenScene)))
                {
                    var rs = new GameObject("b116-scene");
                    try
                    {
                        var m = OstrichMark.Attach(rs.transform, 3);
                        m.SetScene(sc);
                        m.Preview(OstrichAct.Idle, 0, FeederRow.X(1, 3), WorldBuilder.FeederY - 0.05f, false);
                        var gr = m.Ground;
                        float foot = OstrichMark.GroundY(WorldBuilder.FeederY);
                        var want = OstrichGround.SceneTint(sc);
                        var got = gr.Ledge.color;
                        bool tinted = Mathf.Abs(got.r - want.r) + Mathf.Abs(got.g - want.g) + Mathf.Abs(got.b - want.b) < 0.001f;
                        bool differs = sc == GardenScene.Summer || Mathf.Abs(want.r - 1f) + Mathf.Abs(want.g - 1f) + Mathf.Abs(want.b - 1f) > 0.1f;
                        Check("season-" + sc + "-ledge", gr.Shown && gr.Shadow.enabled && tinted && differs
                            && Mathf.Abs(gr.Ledge.transform.position.y - OstrichGround.LedgeTop(foot)) < 0.001f
                            && Mathf.Abs(gr.Shadow.transform.position.y - (foot - OstrichGround.ShadowDrop)) < 0.001f,
                            "tint " + got);
                    }
                    finally { Object.DestroyImmediate(rs); }
                }
            }
            finally { Object.DestroyImmediate(camGo); }
            Debug.Log(fail == 0 ? "build115-ground tests DONE " + pass + "/" + (pass + fail) : "build115-ground tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif

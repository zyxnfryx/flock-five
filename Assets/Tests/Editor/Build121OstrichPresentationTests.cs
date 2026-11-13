#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 121. Ostrich entrance gate, claw-to-mound contact, and the fifth-bird POW.
    // Edit-mode only. Registered in EnsurePlayable.RunEditorTests.
    public static class Build121OstrichPresentationTests
    {
        const string Out = "/tmp/flock-five-b121-ostrich-tests.txt";

        [MenuItem("Flock Five/Build 121 Ostrich Presentation Tests")]
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
                Debug.Log("[build121-ostrich] " + line);
            }

            var camGo = new GameObject("b121-cam");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = WorldBuilder.CamOrtho;
            cam.aspect = 9f / 16f;
            try
            {
                // --- intro timeline: offstage, then in play, taps gated the whole way ---
                Check("intro-gates-before-play",
                    OstrichEntrance.GatesTaps(0f)
                    && OstrichEntrance.GatesTaps(OstrichEntrance.Total - 0.02f)
                    && !OstrichEntrance.GatesTaps(OstrichEntrance.Total)
                    && !OstrichEntrance.InPlay(OstrichEntrance.StrideStart)
                    && OstrichEntrance.InPlay(OstrichEntrance.Total)
                    && !OstrichEntrance.BirdVisible(0f)
                    && OstrichEntrance.BirdVisible(OstrichEntrance.StrideStart),
                    "total " + OstrichEntrance.Total);
                Check("wipe-covers-until-garden-is-clear",
                    OstrichEntrance.WipeCovers(true, false)
                    && OstrichEntrance.WipeCovers(false, true)
                    && OstrichEntrance.WipeCovers(true, true)
                    && !OstrichEntrance.WipeCovers(false, false), "");
                Check("wipe-wait-does-not-shorten-entrance",
                    Mathf.Abs(OstrichEntrance.Total - (OstrichEntrance.StrideStart + OstrichEntrance.StrideSec + OstrichEntrance.PlantSec)) < 1e-4f
                    && !OstrichEntrance.BirdVisible(0f)
                    && !OstrichEntrance.InPlay(0f), "");

                var none = new OstrichEntrance.StartGate();
                var noRainbow = OstrichEntrance.TickStart(none, false, 0.5f);
                Check("no-rainbow-does-not-hang", noRainbow.Beat && noRainbow.EmptyShown && !noRainbow.SawWipe, "");

                var held = OstrichEntrance.TickStart(none, true, 5f);
                held = OstrichEntrance.TickStart(held, true, 5f);
                Check("wipe-holds-the-gate", held.SawWipe && !held.Beat && held.Clear == 0f && !held.EmptyShown, "");
                var spiked = OstrichEntrance.TickStart(held, false, 5f);
                Check("thinning-does-not-start-the-beat", !spiked.Beat && !spiked.EmptyShown
                    && spiked.Clear > 0f && spiked.Clear <= OstrichEntrance.StepMax + 1e-4f
                    && spiked.Clear < OstrichEntrance.ClearHold, "clear " + spiked.Clear);

                var open = spiked;
                int guard = 0;
                while (!open.EmptyShown && guard < 80)
                {
                    open = OstrichEntrance.TickStart(open, false, OstrichEntrance.StepMax);
                    guard++;
                }
                Check("empty-garden-frame-before-beat", open.EmptyShown && !open.Beat
                    && open.Clear >= OstrichEntrance.ClearHold, "clear " + open.Clear + " steps " + guard);
                var started = OstrichEntrance.TickStart(open, false, OstrichEntrance.StepMax);
                Check("beat-starts-after-empty-frame", started.Beat && started.EmptyShown, "");
                var recovered = OstrichEntrance.TickStart(spiked, true, 0.016f);
                Check("wipe-returning-resets-the-hold", recovered.SawWipe && !recovered.Beat && recovered.Clear == 0f
                    && !recovered.EmptyShown, "");

                float clock = 0f;
                bool sawCall = false, sawStride = false, sawPlant = false, skipped = false;
                for (int i = 0; i < 400 && clock < OstrichEntrance.Total; i++)
                {
                    float prev = clock;
                    clock = OstrichEntrance.Advance(clock, 5f);
                    if (clock - prev > OstrichEntrance.StepMax + 1e-4f) skipped = true;
                    if (clock >= OstrichEntrance.HonkAt && clock < OstrichEntrance.StrideStart) sawCall = true;
                    if (OstrichEntrance.BirdVisible(clock) && OstrichEntrance.PlantU(clock) == 0f) sawStride = true;
                    if (OstrichEntrance.PlantU(clock) > 0f && clock < OstrichEntrance.Total) sawPlant = true;
                }
                Check("spike-cannot-skip-honk-stride-plant", !skipped && sawCall && sawStride && sawPlant
                    && !OstrichEntrance.BirdVisible(OstrichEntrance.Advance(0f, 5f)),
                    "t " + clock);

                void CheckSpan(string label, float half)
                {
                    float plant = OstrichMark.RestBodyX(OstrichMark.PortX, 1f, half);
                    float start = OstrichEntrance.BodyX(0f, plant, half, OstrichMark.BodyFront);
                    float end = OstrichEntrance.BodyX(OstrichEntrance.Total, plant, half, OstrichMark.BodyFront);
                    float midT = OstrichEntrance.StrideStart + OstrichEntrance.StrideSec * 0.5f;
                    float mid = OstrichEntrance.BodyX(midT, plant, half, OstrichMark.BodyFront);
                    Check(label + "-starts-offstage", start < -half - 0.2f, "start " + start + " half " + half);
                    Check(label + "-plants", Mathf.Abs(end - plant) < 0.001f && mid > start && mid < end,
                        "end " + end + " plant " + plant);
                    Check(label + "-stride-feet-level",
                        Mathf.Abs(OstrichEntrance.Stomp(midT)) < 0.001f
                        && Mathf.Abs(OstrichEntrance.Stomp(OstrichEntrance.Total)) < 0.001f
                        && OstrichEntrance.Stomp(OstrichEntrance.StrideStart + OstrichEntrance.StrideSec + OstrichEntrance.PlantSec * 0.5f) < -0.04f,
                        "");
                }
                CheckSpan("phone-9-16", WorldBuilder.CamOrtho * 9f / 16f);
                CheckSpan("ipad-3-4", WorldBuilder.CamOrtho * 3f / 4f);
                // 820-wide iPad and a 402-wide phone, portrait. The stride uses the live half width.
                CheckSpan("ipad-820", WorldBuilder.CamOrtho * (820f / 1180f));
                CheckSpan("phone-402", WorldBuilder.CamOrtho * (402f / 874f));

                var root = new GameObject("b121-intro");
                try
                {
                    var mark = OstrichMark.Attach(root.transform, 3);
                    mark.BeginEntrance();
                    Check("intro-not-in-play", mark.Entering && !mark.InPlay && !mark.OnStage, "");
                    // The play coroutine holds this pose until WipeCovers is false. No PresentEntrance yet.
                    Check("held-offstage-through-wipe", mark.Entering && !mark.InPlay && !mark.OnStage
                        && (mark.Ground == null || mark.Ground.Weight < 0.02f), "");
                    mark.PresentEntrance(0.05f);
                    Check("intro-hidden-at-call", mark.Entering && !mark.OnStage && mark.Ground != null
                        && mark.Ground.Shown && mark.Ground.Weight < 0.25f,
                        "weight " + (mark.Ground != null ? mark.Ground.Weight : -1f));
                    float midT = OstrichEntrance.StrideStart + OstrichEntrance.StrideSec * 0.45f;
                    mark.PresentEntrance(midT);
                    var body = root.transform.Find("Ostrich/Body");
                    float half = OstrichMark.ViewHalfWidth();
                    float plant = OstrichMark.RestBodyX(OstrichMark.PortX, 1f, half);
                    Check("intro-walking-before-play", mark.Entering && !mark.InPlay && mark.OnStage && body != null
                        && body.position.x < plant - 0.15f && body.position.x > -half - OstrichMark.BodyBack,
                        "body " + (body != null ? body.position.x : float.NaN) + " plant " + plant);
                    float aimY = WorldBuilder.FeederY - 0.05f;
                    float toe = body != null ? body.position.y - OstrichMark.FootBelowPivot * OstrichMark.BodyScale : 0f;
                    Check("intro-feet-on-toe-line", body != null
                        && Mathf.Abs(toe - OstrichMark.PlantedFootY(aimY)) < 0.04f,
                        "toe " + toe + " planted " + OstrichMark.PlantedFootY(aimY));
                    mark.PresentEntrance(OstrichEntrance.Total - 0.02f);
                    Check("intro-still-gated-at-plant", mark.Entering && !mark.InPlay, "");
                    mark.EndEntrance();
                    Check("intro-ends-in-play", !mark.Entering && mark.InPlay && mark.OnStage && mark.Settled
                        && mark.Ground != null && mark.Ground.Weight > 0.99f, "");
                }
                finally { Object.DestroyImmediate(root); }

                string app = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),
                    "Assets/_Project/Scripts/App/FlockFiveApp.cs"));
                int gate = app.IndexOf("if (OstrichIntroBlocking()) return;", System.StringComparison.Ordinal);
                int handle = app.IndexOf("void HandleTap(", System.StringComparison.Ordinal);
                int stage = app.IndexOf("bool StageClearingCollect(", System.StringComparison.Ordinal);
                int stageEnd = stage >= 0 ? app.IndexOf("static bool PestWillLeave(", stage, System.StringComparison.Ordinal) : -1;
                string stageBody = stage >= 0 && stageEnd > stage ? app.Substring(stage, stageEnd - stage) : "";
                Check("intro-gate-on-taps", gate > 0 && handle > gate && app.IndexOf("OstrichIntroBlocking()", handle, System.StringComparison.Ordinal) > handle, "");
                Check("entrance-armed-on-load-and-restart",
                    app.Split(new[] { "ArmOstrichEntrance();" }, System.StringSplitOptions.None).Length - 1 >= 2, "");
                Check("intro-gate-skips-stage-clear", stageBody.Length > 0 && !stageBody.Contains("OstrichIntro")
                    && !stageBody.Contains("Watch"), "len " + stageBody.Length);

                // --- claws sit on the mound; shadow under the feet; bank clears the HUD ---
                cam.aspect = 9f / 19.5f;
                var stand = new GameObject("b121-stand");
                try
                {
                    var mark = OstrichMark.Attach(stand.transform, 3);
                    mark.Preview(OstrichAct.Idle, 0, FeederRow.X(1, 3), WorldBuilder.FeederY - 0.05f, false);
                    var gr = mark.Ground;
                    float foot = OstrichMark.GroundY(WorldBuilder.FeederY);
                    var front = stand.transform.Find("Ostrich/Body/LegFront").GetComponent<SpriteRenderer>();
                    var back = stand.transform.Find("Ostrich/Body/LegBack").GetComponent<SpriteRenderer>();
                    float mesh = Mathf.Min(front.bounds.min.y, back.bounds.min.y);
                    float painted = mesh + OstrichMark.ClawLift;
                    Check("pad-attached", gr != null && gr.Pad != null && gr.Pad.enabled && gr.Shadow.enabled && gr.Shown, "");
                    Check("claws-flush-on-pad", gr.Pad != null
                        && Mathf.Abs(gr.Pad.transform.position.y - OstrichGround.PadTop(foot)) < 0.001f
                        && Mathf.Abs(painted - gr.Pad.transform.position.y) < 0.06f
                        && painted <= gr.Pad.transform.position.y + 0.02f,
                        "painted " + painted + " pad " + (gr.Pad != null ? gr.Pad.transform.position.y : float.NaN) + " mesh " + mesh);
                    Check("shadow-under-feet", Mathf.Abs(gr.Shadow.transform.position.y - (foot - OstrichGround.ShadowDrop)) < 0.001f
                        && Mathf.Abs(gr.Shadow.transform.position.x - gr.Pad.transform.position.x) < 0.001f
                        && gr.Shadow.color.a > 0.45f, "a " + gr.Shadow.color.a);
                    float bottom = WorldBuilder.CamRestY - WorldBuilder.CamOrtho;
                    Check("bank-clears-hud", foot - OstrichGround.LedgeDepth > bottom + 4f,
                        "bank " + (foot - OstrichGround.LedgeDepth) + " hud " + (bottom + 4f));
                    Check("stand-no-colliders", gr.GetComponentsInChildren<Collider>(true).Length == 0
                        && gr.GetComponentsInChildren<Collider2D>(true).Length == 0, "");
                    var mid = OstrichGround.PadPixel(OstrichGround.PadTexW / 2, OstrichGround.PadTexH - 4);
                    var edge = OstrichGround.PadPixel(0, OstrichGround.PadTexH / 2);
                    Check("pad-reads-as-ground", mid.a > 200 && mid.g > mid.r && edge.a < mid.a, "mid " + mid.a + " edge " + edge.a);
                }
                finally { Object.DestroyImmediate(stand); }

                int found = 0;
                var nums = "";
                bool only = true;
                for (int i = 0; i < LevelData.Count; i++)
                {
                    var lv = LevelData.Peek(i);
                    if (lv == null || !lv.Ostrich) continue;
                    found++;
                    nums += lv.Number + " ";
                    only &= lv.Number == 27 || lv.Number == 38 || lv.Number == 43;
                }
                Check("ostrich-gardens-only", found == 3 && only, nums);

                // --- left attacks face the bird at a full silhouette; neck join is soft ---
                cam.aspect = 9f / 16f;
                var faceRoot = new GameObject("b121-face");
                try
                {
                    var mark = OstrichMark.Attach(faceRoot.transform, 3);
                    float aimY = WorldBuilder.FeederY - 0.05f;
                    mark.Preview(OstrichAct.Idle, 0, 0f, aimY, false);
                    var bodySr = faceRoot.transform.Find("Ostrich/Body").GetComponent<SpriteRenderer>();
                    mark.BeginStrike(new Vector3(-3.4f, 1.2f, 0f));
                    mark.Tick(0.05f);
                    Check("left-attack-faces-left", bodySr.flipX && mark.Head.flipX
                        && OstrichMark.AttackFace(-3.4f) < 0f, "");
                    Check("left-attack-silhouette", bodySr.transform.localScale.x >= OstrichMark.BodyScale * OstrichMark.TurnFloor - 0.01f
                        && bodySr.transform.localScale.x <= OstrichMark.BodyScale + 0.05f,
                        "sx " + bodySr.transform.localScale.x + " body " + OstrichMark.BodyScale);
                    mark.BeginStrike(new Vector3(2.8f, 1.2f, 0f));
                    mark.Tick(0.05f);
                    Check("right-attack-faces-right", !bodySr.flipX && !mark.Head.flipX, "");

                    var line = faceRoot.GetComponentInChildren<LineRenderer>();
                    var collar = faceRoot.transform.Find("Ostrich/Body/Collar");
                    bool neck = line != null;
                    if (neck)
                    {
                        var g = line.colorGradient;
                        neck = g.Evaluate(0f).a < 0.08f && g.Evaluate(0.5f).a > 0.9f && g.Evaluate(1f).a < 0.55f;
                    }
                    Check("neck-join-fades", neck && OstrichMark.NeckRootMul < 2.2f && OstrichMark.NeckRootMul > 1.2f, "mul " + OstrichMark.NeckRootMul);
                    Check("collar-laps-join", collar != null && collar.localScale.x >= OstrichMark.CollarOverlap - 0.001f,
                        "scale " + (collar != null ? collar.localScale.x : 0f));
                }
                finally { Object.DestroyImmediate(faceRoot); }

                // --- fifth bird is the POW; the squash fires on that finish and then lets go ---
                Check("whack-is-the-fifth", OstrichHitTimeline.IsWhackBird(4, 5)
                    && !OstrichHitTimeline.IsWhackBird(0, 5)
                    && !OstrichHitTimeline.IsWhackBird(3, 5)
                    && !OstrichHitTimeline.IsWhackBird(4, 4)
                    && OstrichHitTimeline.IsWhackBird(3, 4), "");
                Check("whack-after-four", Mathf.Abs(OstrichHitTimeline.WhackAt(5) - OstrichHitTimeline.ArriveAt(4)) < 1e-4f
                    && OstrichHitTimeline.WhackAt(5) > OstrichHitTimeline.ArriveAt(3) + 0.1f, "");
                float peakAt = 0.22f * OstrichHitTimeline.WhackSec;
                Check("whack-squash-peaks-then-releases",
                    OstrichHitTimeline.WhackSquash(0f) == 0f
                    && OstrichHitTimeline.WhackSquash(OstrichHitTimeline.WhackSec) == 0f
                    && OstrichHitTimeline.WhackSquash(peakAt) > 0.95f
                    && OstrichPow.Word == "POW"
                    && OstrichPow.Alpha(0f) == 0f
                    && OstrichPow.Alpha(OstrichPow.Life * 0.2f) > 0.5f
                    && OstrichPow.Alpha(OstrichPow.Life) == 0f,
                    "peak " + OstrichHitTimeline.WhackSquash(peakAt));

                var hitRoot = new GameObject("b121-whack");
                try
                {
                    var mark = OstrichMark.Attach(hitRoot.transform, 3);
                    mark.Preview(OstrichAct.Idle, 0, 0f, WorldBuilder.FeederY - 0.05f, false);
                    var body = hitRoot.transform.Find("Ostrich/Body");
                    float before = body.localScale.y;
                    mark.BeginWhack();
                    mark.ShowHit(1);
                    mark.Tick(peakAt);
                    float squashed = body.localScale.y;
                    float toeSquash = body.position.y - OstrichMark.FootBelowPivot * body.localScale.y;
                    float toeLine = OstrichMark.PlantedFootY(WorldBuilder.FeederY - 0.05f);
                    mark.Tick(OstrichHitTimeline.WhackSec);
                    Check("whack-squashes-on-finish", mark.Act == OstrichAct.Hit1 && squashed < before * 0.88f
                        && body.localScale.y > before * 0.95f, "before " + before + " peak " + squashed + " after " + body.localScale.y);
                    Check("whack-feet-stay-planted", Mathf.Abs(toeSquash - toeLine) < 0.08f,
                        "toe " + toeSquash + " line " + toeLine);
                }
                finally { Object.DestroyImmediate(hitRoot); }

                var pow = OstrichPow.Spawn(new Vector3(0.2f, 1f, 0f), null);
                try
                {
                    var word = pow.GetComponentInChildren<TextMesh>();
                    Check("pow-mark-readable", word != null && word.text == "POW"
                        && word.color.r > 0.9f && word.color.g > 0.7f && word.color.b < 0.6f, "");
                }
                finally { if (pow != null) Object.DestroyImmediate(pow); }

                string fight = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),
                    "Assets/_Project/Scripts/App/FlockFiveApp.OstrichFight.cs"));
                int hold = fight.IndexOf("if (!mark.Entering) mark.BeginEntrance();", System.StringComparison.Ordinal);
                int firstYield = fight.IndexOf("yield return null", System.StringComparison.Ordinal);
                int tick = fight.IndexOf("OstrichEntrance.TickStart(", System.StringComparison.Ordinal);
                int wipe = fight.IndexOf("OstrichEntrance.WipeCovers(PressRainbow.Live(), PressRainbow.StageHeld())", System.StringComparison.Ordinal);
                int advance = fight.IndexOf("OstrichEntrance.Advance(", System.StringComparison.Ordinal);
                int present = fight.IndexOf("mark.PresentEntrance(t)", System.StringComparison.Ordinal);
                Check("entrance-waits-until-star-wipe-clears", hold > 0 && firstYield > hold && tick > firstYield
                    && wipe > tick && advance > wipe && present > advance
                    && fight.IndexOf("SfxLibrary.Honk()", System.StringComparison.Ordinal) > wipe, "");
                int whack = fight.IndexOf("OstrichHitTimeline.IsWhackBird(i, n)", System.StringComparison.Ordinal);
                int show = fight.IndexOf("mark.ShowHit(hitNo)", System.StringComparison.Ordinal);
                int begin = fight.IndexOf("mark.BeginWhack()", System.StringComparison.Ordinal);
                Check("whack-wired-on-the-finish", whack > 0 && show > whack && begin > whack
                    && fight.Contains("SfxLibrary.SharpHit()")
                    && fight.Contains("OstrichPow.Spawn(")
                    && fight.Contains("Impact(tr.position, col)"), "");
            }
            finally { Object.DestroyImmediate(camGo); }

            Debug.Log(fail == 0
                ? "build121-ostrich tests DONE " + pass + "/" + (pass + fail)
                : "build121-ostrich tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif

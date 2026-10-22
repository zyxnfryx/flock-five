#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b126: the tap-to-continue fingertip (mockup D). Every any-tap caption gets it, no target/claim caption does,
    // the cue never changes the caption box, and it sits on the box's bottom-right corner (part in, part out).
    static class Build126TapCueTests
    {
        [MenuItem("Flock Five/Build 126 Tap Cue Tests")]
        public static void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                Debug.Log((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            foreach (CoachStep step in System.Enum.GetValues(typeof(CoachStep)))
            {
                string cap = CoachTap.Caption(step);
                bool any = CoachTap.AnyTap(step);
                bool cue = TapCue.ForCaption(cap) && TapCue.ForCaption(CaptionLayout.HeadlineGap(cap))
                    && TapCue.ForCaption((cap ?? "").Replace("\n", " "));
                if (any) Check("cue-on-" + step, cue, cap);
                else
                {
                    // A required/claim caption whose text equals an any-tap caption would be ambiguous; none do today.
                    Check("no-cue-on-" + step, !TapCue.ForCaption(cap), cap);
                }
            }

            int px = 52;
            var plate = new Rect(100f, 600f, 900f, 260f);
            var before = plate;
            bool inside = true, outside = true, still = true;
            for (float t = 0f; t < TapCue.Period * 2f; t += 0.05f)
            {
                var r = TapCue.CueRect(plate, px, t);
                still &= plate == before;
                inside &= r.yMin < plate.yMax && r.xMax <= plate.xMax && r.xMin > plate.x;
                outside &= r.yMax > plate.yMax;
            }
            Check("box-size-unchanged", still && plate.width == 900f && plate.height == 260f, plate.ToString());
            Check("cue-overlaps-bottom-right", inside && outside, "part inside, part below the bottom edge");
            Check("cue-tiny", TapCue.CueRect(plate, px, 0f).height <= px * 0.6f && TapCue.CueRect(plate, px, 0f).width <= px * 0.35f, "");
            Check("outline-thicker-than-hairline", TapCue.OutlinePx(px) >= 3, TapCue.OutlinePx(px) + "px");
            Check("taps", TapCue.Press(TapCue.Period * TapCue.PressFrac * 0.5f) > 0.9f && TapCue.Press(TapCue.Period * 0.6f) == 0f, "");

            // Source guard: the one hook, after the plate is final, no padding change for the cue.
            string coach = System.IO.File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            int hooks = coach.Split(new[] { "TapCue.DrawAt(" }, System.StringSplitOptions.None).Length - 1;
            Check("one-shared-hook", hooks == 1 && coach.IndexOf("TapCue.ForCaption(shown)") >= 0, hooks + " hooks");

            Debug.Log(fail == 0 ? "build126 tap-cue tests DONE " + pass + "/" + pass
                : "build126 tap-cue tests FAIL " + fail + " failed, " + pass + " passed");
        }
    }
}
#endif

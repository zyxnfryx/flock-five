#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 82 pass b82f. Forced lessons use the shared veil's cut-out.
    static class Build82TestsLessonGate
    {
        [MenuItem("Flock Five/Build 82 Lesson Gate Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build82f-lesson] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var screen = new Rect(0f, 0f, 1179f, 2556f);
            var hole = new Rect(140f, 1680f, 220f, 180f);
            var off = new Vector2(40f, 40f);
            var on = hole.center;
            var forced = new StringBuilder();
            int forcedN = 0;
            int infoN = 0;
            bool gates = true;
            string detail = "cut-out";
            var steps = CoachTap.All;
            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                bool isForced = CoachTap.Forced(step);
                var kind = CoachTap.Kind(step);
                bool kindOk = isForced == (kind == CoachTapKind.Required || kind == CoachTapKind.ClaimOnly);
                if (!kindOk)
                {
                    gates = false;
                    detail = step + " forced flag drifted";
                    break;
                }
                if (!isForced)
                {
                    infoN++;
                    var any = ModalBlock.ResolveLesson(false, screen, hole, off, false);
                    if (any.Deliver && !any.Advance) continue;
                    gates = false;
                    detail = step + " informational deliver=" + any.Deliver;
                    break;
                }
                forcedN++;
                forced.Append(step).Append(' ');
                var miss = ModalBlock.ResolveLesson(true, screen, hole, off, false);
                var hit = ModalBlock.ResolveLesson(true, screen, hole, on, false);
                var world = ModalBlock.ResolveLesson(true, screen, default, off, true);
                var shut = ModalBlock.ResolveLesson(true, screen, default, on, false);
                if (!miss.Deliver && !miss.Advance && hit.Deliver && hit.Advance
                    && world.Deliver && world.Advance && !shut.Deliver && !shut.Advance)
                    continue;
                gates = false;
                detail = step + " off=" + miss.Deliver + " on=" + hit.Deliver
                    + " world=" + world.Deliver + " shut=" + shut.Deliver;
                break;
            }
            Debug.Log("[build82f-lesson] forced (" + forcedN + ") " + forced);
            Check("each-forced", gates && forcedN >= 20 && infoN >= 8,
                detail + " forced=" + forcedN + " info=" + infoN);

            var controls = new[]
            {
                HomeControl.Poker, HomeControl.Daily, HomeControl.Vip, HomeControl.Hive, HomeControl.Gear,
            };
            bool glove = true;
            string gloveDetail = "vip hole";
            for (int i = 0; i < controls.Length; i++)
            {
                var press = GloveGate.Resolve(true, HomeControl.Vip, controls[i]);
                bool allow = controls[i] == HomeControl.Vip;
                if (press.AllowPress == allow && press.Navigate == allow && press.Advance == allow) continue;
                glove = false;
                gloveDetail = controls[i].ToString();
                break;
            }
            var idle = GloveGate.Resolve(false, HomeControl.Poker, HomeControl.Daily);
            var none = GloveGate.Resolve(true, HomeControl.None, HomeControl.Hive);
            Check("glove-gate", glove && idle.AllowPress && idle.Navigate && !idle.Advance && !none.AllowPress,
                gloveDetail);

            string root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets/_Project/Scripts/App");
            string modal = File.ReadAllText(Path.Combine(root, "FlockFiveApp.Modal.cs"));
            string app = File.ReadAllText(Path.Combine(root, "FlockFiveApp.cs"));
            bool wired = modal.Contains("CoachStep.HiveGarden")
                && modal.Contains("RaiseLesson")
                && modal.Contains("VeilDrops")
                && app.Contains("VeilDrops(")
                && File.ReadAllText(Path.Combine(root, "CoachTap.cs")).Contains("ModalBlock.ResolveLesson");
            Check("shared-gate", wired, wired ? "hive garden uses the veil" : "missing HiveGarden or ResolveLesson");

            Debug.Log("[build82f-lesson] " + (fail == 0 ? "BUILD82F_LESSON_OK " + pass : "BUILD82F_LESSON_FAIL " + fail));
            if (fail > 0) throw new System.Exception("BUILD82F_LESSON_FAIL " + fail);
        }
    }
}
#endif

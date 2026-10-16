#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b123: the shared tutorial table names, for every step, what advances it, what only toggles, and which pointer shows.
    public static class Build123BadgerTutorialTests
    {
        const string Out = "/tmp/flock-five-b123-badger-tutorial-tests.txt";

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
                Debug.Log("[build123] " + line);
            }

            // Every step has a rule, at least one Advance zone and a pointer, consistent with its kind.
            int bad = 0;
            foreach (var step in CoachTap.All)
            {
                var r = CoachTap.Rule(step);
                bool any = CoachTap.AnyTap(step);
                if (r.Advance == TapZone.None || r.Pointer == CoachPointer.None) bad++;
                if (any != r.Advances(TapZone.Blank)) bad++;
                if (!any && r.Advances(TapZone.Blank)) bad++;
                if (CoachTap.Caption(step) == null) bad++;
            }
            Check("every-step-has-advance-and-pointer", bad == 0, "bad " + bad);

            // Lessons 1-3: arrow; blank or the glowed area continues; back and combs ignored.
            bool lessons = true;
            for (int i = 0; i < 3; i++)
            {
                var r = CoachTap.Rule(CoachTap.BadgerLesson(i));
                lessons &= r.Pointer == CoachPointer.Arrow && r.Advances(TapZone.Blank) && r.Advances(TapZone.Focus)
                    && r.Ignores(TapZone.Back) && r.Ignores(TapZone.Comb);
            }
            Check("lessons-arrow-blank-or-focus-advance", lessons, "");
            Check("lesson2-power-buttons-toggle-never-advance", CoachTap.Rule(CoachStep.BadgerPowers).Toggles(TapZone.PowerButton)
                && !CoachTap.Rule(CoachStep.BadgerPowers).Advances(TapZone.PowerButton), "");

            // Moves 1-3: glove; only the glowed comb advances; everything else ignored.
            bool moves = true;
            for (int m = 1; m <= 3; m++)
            {
                var r = CoachTap.Rule(CoachTap.BadgerGuide(m));
                moves &= r.Pointer == CoachPointer.Glove && r.Advances(TapZone.Target) && !r.Advances(TapZone.Blank)
                    && r.Ignores(TapZone.Back) && r.Ignores(TapZone.Comb) && r.Ignores(TapZone.PowerButton);
            }
            Check("moves-glove-target-only", moves, "");
            var unlock = CoachTap.Rule(CoachStep.BadgerPowerIntro);
            var armed = CoachTap.Rule(CoachStep.BadgerPowerArmed);
            Check("round4-x2-button-then-comb", unlock.Pointer == CoachPointer.Glove && unlock.Advances(TapZone.Target) && unlock.Ignores(TapZone.Comb) && unlock.Ignores(TapZone.Back)
                && armed.Pointer == CoachPointer.Glove && armed.Advances(TapZone.Target) && armed.Toggles(TapZone.PowerButton) && armed.Ignores(TapZone.Back), "");
            Check("result-glove-on-continue", CoachTap.Rule(CoachStep.BadgerResult).Pointer == CoachPointer.Glove && CoachTap.Rule(CoachStep.BadgerResult).Advances(TapZone.Target), "");

            // Copy: lessons name the continue tap; nothing says the old "bird followed you home" for the badger.
            bool copy = BadgerCopy.LessonAt(0).Contains("Tap to continue") && BadgerCopy.LessonAt(1).Contains("x2") && BadgerCopy.LessonAt(1).Contains("Locked for 3 moves")
                && BadgerCopy.LessonAt(2).StartsWith("Higher honey scores the difference") && BadgerCopy.LessonAt(2).Contains("First full column wins");
            Check("lesson-copy-names-continue-and-rules", copy, BadgerCopy.LessonAt(1));
            Check("round4-lines", BadgerCopy.PowersOpen.Contains("Tap x2") && BadgerCopy.X2Armed.Contains("doubles your next tile") && BadgerCopy.GuideLine(3).Contains("halve"), "");

            // Source wiring.
            string badger = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("coach-stamp-after-move-3-not-after-lesson",
                badger.Contains("BadgerCopy.MarkCoach();") && !badger.Substring(badger.IndexOf("void AdvanceBadgerLesson()"), 700).Contains("MarkCoach()")
                && badger.Substring(badger.IndexOf("void NextBadgerRound()"), 1400).Contains("MarkCoach()"), "");
            Check("back-held-through-locked-power-stretch", badger.Contains("BadgerCoachHoldsBack()") && badger.Contains("_bgFight.PowersLocked || _bgPowerGuideOn"), "");
            Check("guided-gate-in-shared-step-gate", coach.Contains("gate.Step = GateBadgerGuide;") && coach.Contains("void CoachArrowAt(Rect target, float s)")
                && coach.Contains("GateBadgerLesson && BadgerLessonIgnores"), "");

            string end = fail == 0 ? "BUILD123_OK " + pass : "BUILD123_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif

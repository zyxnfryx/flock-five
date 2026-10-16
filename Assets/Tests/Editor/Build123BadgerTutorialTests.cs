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

            // Runtime gate: a press in an ignore zone is eaten; blank space continues (lessons 1-3).
            var back = new Rect(10, 10, 60, 60); var coin = new Rect(700, 10, 60, 60);
            var boss = new Rect(100, 90, 500, 200); var power = new Rect(10, 520, 740, 70); var grid = new Rect(100, 320, 500, 190);
            bool gate = true;
            for (int st = 0; st < 3; st++)
            {
                gate &= FlockFiveApp.BadgerLessonPressIgnored(st, back, coin, boss, power, grid, new Vector2(30, 30));     // back
                gate &= FlockFiveApp.BadgerLessonPressIgnored(st, back, coin, boss, power, grid, new Vector2(720, 30));    // coin
                gate &= FlockFiveApp.BadgerLessonPressIgnored(st, back, coin, boss, power, grid, new Vector2(300, 150));   // boss grid
                gate &= !FlockFiveApp.BadgerLessonPressIgnored(st, back, coin, boss, power, grid, new Vector2(40, 300));   // blank
            }
            gate &= !FlockFiveApp.BadgerLessonPressIgnored(0, back, coin, boss, power, grid, new Vector2(300, 400));        // L1 grid continues
            gate &= FlockFiveApp.BadgerLessonPressIgnored(1, back, coin, boss, power, grid, new Vector2(300, 400));         // L2 combs eaten
            gate &= FlockFiveApp.BadgerLessonPressIgnored(0, back, coin, boss, power, grid, new Vector2(300, 550));         // L1 power row eaten
            gate &= FlockFiveApp.BadgerLessonPressIgnored(1, back, coin, boss, power, grid, new Vector2(60, 550));          // L2 power button eaten
            Check("runtime-lesson-gate-ignore-zones", gate, "");

            // Caption fit: every new coach line fits its band at SE-class and iPad sizes (incl. the column step's locked break).
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < BadgerCopy.LessonCount; i++) lines.Add(BadgerCopy.LessonAt(i));
            for (int m = 1; m <= 3; m++) lines.Add(BadgerCopy.GuideLine(m));
            foreach (var pw in new[] { BadgerPower.X2, BadgerPower.X3, BadgerPower.HotSauce, BadgerPower.FreezeSpray })
            { lines.Add(BadgerCopy.PowerIntroLine(pw, true)); lines.Add(BadgerCopy.PowerArmedLine(pw)); }
            var sizes = new[] { new Vector2(320, 568), new Vector2(375, 667), new Vector2(390, 844), new Vector2(768, 1024), new Vector2(1024, 1366) };
            int clipped = 0; string firstBad = "";
            foreach (var sz in sizes)
                foreach (var ln in lines)
                    if (!FlockFiveApp.BadgerCaptionFits(ln, sz.x, sz.y, new Rect(0, 0, sz.x, sz.y), out string d))
                    { clipped++; if (firstBad == "") firstBad = ln.Replace("\n", "/") + " @ " + d; }
            Check("coach-captions-fit-band-se-to-ipad", clipped == 0, "clipped " + clipped + " " + firstBad);

            // Powers: intro/armed copy for every power, one-per-step; fair first grid.
            bool pcopy = true;
            foreach (var pw in new[] { BadgerPower.X2, BadgerPower.X3, BadgerPower.HotSauce, BadgerPower.FreezeSpray })
                pcopy &= BadgerCopy.PowerIntroLine(pw, false).Length > 0 && BadgerCopy.PowerArmedLine(pw).Length > 0;
            Check("per-power-intro-and-armed-copy", pcopy && BadgerCopy.Armed == "ARMED", "");
            var fair = BadgerLoadout.Preload((k, f) => 0, 0, 16);
            fair.FairStart(1);
            int ones = 0, sum = 0; foreach (var h in fair.Honeys()) { if (h <= 1) ones++; sum += h; }
            var late = BadgerLoadout.Preload((k, f) => 0, 0, 16); late.FairStart(2);
            Check("fair-first-grid-not-all-ones", ones <= 3 && sum >= 40 && late.Honeys()[0] == 1, "ones " + ones + " sum " + sum);

            // Source wiring.
            string badger = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("coach-stamp-after-move-3-not-after-lesson",
                badger.Contains("BadgerCopy.MarkCoach();") && !badger.Substring(badger.IndexOf("void AdvanceBadgerLesson()"), 700).Contains("MarkCoach()")
                && badger.Substring(badger.IndexOf("void NextBadgerRound()"), 1400).Contains("MarkCoach()"), "");
            Check("back-held-through-locked-power-stretch", badger.Contains("BadgerCoachHoldsBack()") && badger.Contains("_bgFight.PowersLocked || _bgPowerGuideOn"), "");
            Check("result-gated-and-back-inert-at-over", coach.Contains("gate.Step = GateBadgerResult;") && badger.Contains("BadgerCoachHoldsBack() || BadgerResultLive()")
                && badger.Contains("BadgerContinueRect(L); // one rect"), "");
            Check("back-held-in-round4-bosswait", badger.Contains("bool BadgerGuidePending()") && badger.Contains("|| BadgerGuidePending())"), "");
            Check("badger-tap-presses-level", File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs").Contains("the badger on the pedestal is part of the pedestal"), "");
            Check("no-bird-greet-over-badger", File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Avatar.cs").Contains("BadgerSitter.Visible(BadgerOwed()"), "");
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

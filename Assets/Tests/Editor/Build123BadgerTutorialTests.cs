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
            { lines.Add(BadgerCopy.PowerIntroLine(pw, true)); if (BadgerCopy.PowerArmedLine(pw).Length > 0) lines.Add(BadgerCopy.PowerArmedLine(pw)); }
            lines.Add(BadgerCopy.BlockWorkedLine(BadgerPower.HotSauce)); lines.Add(BadgerCopy.BlockWorkedLine(BadgerPower.FreezeSpray));
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
                pcopy &= BadgerCopy.PowerIntroLine(pw, false).Length > 0 && (BadgerFight.IsBlock(pw) || BadgerCopy.PowerArmedLine(pw).Length > 0);
            Check("per-power-intro-and-armed-copy", pcopy && BadgerCopy.Armed == "ARMED", "");
            // Real honey only (2 / 3 / 5 by finish): yard slots are 2 on every visit, and the badger's mix never has 1s or 4s.
            var yardGrid = BadgerLoadout.Preload((k, f) => 0, 0, 16);
            bool real = true; foreach (var h in yardGrid.Honeys()) real &= h == Hive.HoneyOfFinish(BeeFinish.Normal) && h == 2;
            bool bossReal = true;
            for (int visit = 1; visit <= 9; visit++)
                foreach (var h in BadgerSchedule.BossTileMix(visit, 40 + visit)) bossReal &= h == 2 || h == 3 || h == 5;
            Check("player-and-badger-honey-real-values-only", real && bossReal && BadgerSchedule.YardHoney == 2 && Hive.HoneyOfFinish(BeeFinish.Holo) == 3
                && Hive.HoneyOfFinish(BeeFinish.InverseRainbow) == 5, "");

            // Forced power round: x2/x3 on the player's highest comb vs the badger's lowest; blocks vs the badger's highest.
            var pl = new[] { 2, 3, 5, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
            var bo = new[] { 2, 2, 2, 3, 3, 3, 3, 3, 5, 5, 5, 5, 5, 2, 3, 5 };
            var f1 = new BadgerFight(2, 7, pl, bo, false);
            int lowIx = f1.BossPickFor(BadgerPower.X3);
            int hiTile = f1.PowerStepPlayerTile(BadgerPower.X3);
            Check("power-step-x3-highest-comb-vs-low-badger", bo[lowIx] == 2 && pl[hiTile] == 5 && f1.StepPower == BadgerPower.X3, "boss " + bo[lowIx] + " you " + pl[hiTile]);
            var f2 = new BadgerFight(2, 7, pl, bo, false);
            int bigIx = f2.BossPickFor(BadgerPower.HotSauce);
            int saveTile = f2.PowerStepPlayerTile(BadgerPower.HotSauce);
            // Blocks: a normal badger pick (not scripted big), item lands on the weakest open tile, ties by first open.
            Check("power-step-block-weakest-tile-first-open", pl[saveTile] == 2 && saveTile == 0 && f2.StepPower == BadgerPower.HotSauce && bigIx >= 0, "boss " + bo[bigIx] + " tile " + saveTile);
            f2.PlayerPick(saveTile);
            bool noSlam = !f2.TryDontCare(n => 0, out var sl) && !sl.Hit;
            Check("power-step-round-never-slammed", noSlam, "");

            // Hot Sauce / Freeze: (a) caption explains replace + block, (b) glove on the button then the comb (shared table),
            // (c) the icon flies to the weakest tile and swaps the number, (d) a result beat says the block worked.
            bool blocks = true;
            foreach (var pw in new[] { BadgerPower.HotSauce, BadgerPower.FreezeSpray })
            {
                string intro = BadgerCopy.PowerIntroLine(pw, false), armedLn = BadgerCopy.PowerArmedLine(pw), done = BadgerCopy.BlockWorkedLine(pw);
                blocks &= intro.Contains("replaces") && intro.Contains("blocks") && intro.Contains("weakest") && armedLn.Length == 0
                    && done.Contains("badger scores nothing") && BadgerFight.IsBlock(pw);
            }
            Check("block-powers-caption-explains-replace-and-block", blocks, BadgerCopy.PowerIntroLine(BadgerPower.HotSauce, false));
            var introR = CoachTap.Rule(CoachStep.BadgerPowerIntro); var armR = CoachTap.Rule(CoachStep.BadgerPowerArmed);
            Check("block-powers-glove-on-button-then-comb-table", introR.Pointer == CoachPointer.Glove && introR.Advances(TapZone.Target)
                && armR.Pointer == CoachPointer.Glove && armR.Advances(TapZone.Target) && armR.Ignores(TapZone.Back), "");
            float p0 = BadgerPowerFly.Progress(0f), pm = BadgerPowerFly.Progress(0.6f), p1 = BadgerPowerFly.Progress(2f);
            Check("block-power-fly-beat-timing", p0 == 0f && p1 == 1f && pm > 0f && pm < 1f && BadgerPowerFly.Progress(0.8f) > pm
                && BadgerPowerFly.RevealExtra(BadgerPower.HotSauce) > 0.9f && BadgerPowerFly.RevealExtra(BadgerPower.FreezeSpray) > 0.9f
                && BadgerPowerFly.RevealExtra(BadgerPower.X2) == 0f && BadgerPowerFly.SwapFrom < 1f, "");
            // Result: with the block the badger's final honey is 0 and the player scores their tile; without it the badger scores.
            BadgerFight.ApplyRound(2, 5, BadgerPower.HotSauce, out int pf, out int bf, out int pg, out int bg);
            BadgerFight.ApplyRound(2, 5, BadgerPower.None, out int pf2, out int bf2, out int pg2, out int bg2);
            Check("block-result-badger-does-not-score", bf == 0 && bg == 0 && pf == 0 && pg == 0 && bg2 == 5, "with block you +" + pg + " badger +" + bg + "; without badger +" + bg2);

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
            Check("block-auto-assign-no-tile-tap", badger.Contains("assign themselves to the weakest open tile") && badger.Contains("ClickBadgerTile(weakest)")
                && badger.Contains("picture ? \"\" : BadgerPowerName(p)") && badger.Contains("PLACEHOLDER art"), "");
            Check("block-beat-wired", badger.Contains("void DrawBadgerBlockBeat(") && badger.Contains("DrawBadgerPowerIcon(") && badger.Contains("BadgerCopy.BlockWorkedLine(_bgGuideRoundPower)")
                && badger.Contains("BadgerPowerFly.RevealExtra(_bgBlockBeat)"), "");
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

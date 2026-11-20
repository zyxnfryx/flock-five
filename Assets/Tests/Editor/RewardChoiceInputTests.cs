#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // After a clear, the REWARD card's Watch Ad 2X and Collect must take the first tap
    // as soon as they are up. An ostrich clear can still be holding a fight lock, a pest
    // cue, a rule-eat or a lesson guard; a plain clear usually is not. Neither may block.
    // Run: Unity -batchmode -executeMethod FlockFive.Editor.RewardChoiceInputTests.Run
    public static class RewardChoiceInputTests
    {
        const string Out = "/tmp/flock-five-reward-choice-input-tests.txt";

        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + (string.IsNullOrEmpty(detail) ? "" : "  " + detail);
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[reward-choice-input] " + line);
            }

            var ostrich = new RewardChoiceInput.Leftovers
            {
                FightHold = true,
                PestHold = true,
                RuleEat = true,
                LessonGuard = true,
            };
            var dropped = RewardChoiceInput.Drop(true, ostrich);
            Check("ostrich-clear-drops-leftovers",
                !dropped.FightHold && !dropped.PestHold && !dropped.RuleEat && !dropped.LessonGuard,
                "fight/pest/rule/guard");
            Check("ostrich-clear-card-up-live", RewardChoiceInput.Live(true, false, false), "buttons accept input");

            var plain = new RewardChoiceInput.Leftovers();
            var plainKept = RewardChoiceInput.Drop(true, plain);
            Check("normal-clear-card-up-live",
                RewardChoiceInput.Live(true, false, false)
                && !plainKept.FightHold && !plainKept.PestHold && !plainKept.RuleEat && !plainKept.LessonGuard,
                "no leftover, still live");

            var held = RewardChoiceInput.Drop(false, ostrich);
            Check("intro-keeps-leftovers",
                held.FightHold && held.PestHold && held.RuleEat && held.LessonGuard, "card not up yet");
            Check("intro-not-live", !RewardChoiceInput.Live(false, false, false), "animation still playing");
            Check("resume-hold-blocks", !RewardChoiceInput.Live(true, true, false), "resume swallow still holds");
            Check("startup-cover-blocks", !RewardChoiceInput.Live(true, false, true), "cover still holds");

            // iPad first, then the phone widths. A point in the middle of each laid-out button
            // is the first tap, including when the LEVEL flower underneath covers that point.
            CheckCard("ipad-820", 820f, 1180f, Check);
            CheckCard("phone-402", 402f, 874f, Check);
            CheckCard("phone-412", 412f, 892f, Check);

            string choice = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.RewardChoice.cs"));
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            Check("choice-uses-shared-press", choice.Contains("RewardChoiceInput.Resolve("), "DrawRewardChoice");
            Check("fade-does-not-discard-tap", choice.IndexOf("a < 0.5f", System.StringComparison.Ordinal) < 0, "no half-fade return");
            int release = app.IndexOf("ReleaseRewardChoiceGates()", System.StringComparison.Ordinal);
            int swallow = app.IndexOf("SwallowRuleTipPointer()", System.StringComparison.Ordinal);
            Check("gates-drop-before-swallows", release >= 0 && swallow > release, "release " + release + " swallow " + swallow);

            Debug.Log("[reward-choice-input] done pass=" + pass + " fail=" + fail);
            if (fail > 0) throw new System.Exception("reward choice input tests failed: " + fail);
        }

        static void CheckCard(string tag, float w, float h, System.Action<string, bool, string> check)
        {
            var face = new Rect(w * 0.12f, h * 0.22f, w * 0.76f, h * 0.22f);
            var lay = FlockFiveApp.LayoutRewardChoice(face, w, h, true);
            var flower = new Rect(0f, lay.Primary.y - 8f, w, h - (lay.Primary.y - 8f));
            bool covers = flower.Contains(lay.Primary.center) && flower.Contains(lay.Secondary.center);
            check(tag + "-flower-covers-buttons", covers, lay.Primary.center + " " + lay.Secondary.center);

            var ostrich = new RewardChoiceInput.Leftovers
            {
                FightHold = true, PestHold = true, RuleEat = true, LessonGuard = true,
            };
            check(tag + "-ostrich-watch-first-tap",
                FirstTap(lay.Primary.center, flower, true, true, lay, ostrich) == RewardChoiceInput.Tap.Watch,
                "Watch Ad");
            check(tag + "-ostrich-collect-first-tap",
                FirstTap(lay.Secondary.center, flower, true, true, lay, ostrich) == RewardChoiceInput.Tap.Collect,
                "Collect");

            var plain = new RewardChoiceInput.Leftovers();
            check(tag + "-normal-watch-first-tap",
                FirstTap(lay.Primary.center, flower, true, true, lay, plain) == RewardChoiceInput.Tap.Watch,
                "Watch Ad");
            check(tag + "-normal-collect-first-tap",
                FirstTap(lay.Secondary.center, flower, true, true, lay, plain) == RewardChoiceInput.Tap.Collect,
                "Collect");

            check(tag + "-outside-tap",
                FirstTap(new Vector2(4f, 4f), flower, true, true, lay, ostrich) == RewardChoiceInput.Tap.None,
                "miss");
            check(tag + "-intro-tap-ignored",
                FirstTap(lay.Primary.center, flower, false, true, lay, ostrich) == RewardChoiceInput.Tap.None,
                "card not up");

            var alone = FlockFiveApp.LayoutRewardChoice(face, w, h, false);
            check(tag + "-lone-collect",
                FirstTap(alone.Primary.center, flower, true, false, alone, plain) == RewardChoiceInput.Tap.Collect,
                "no ad");

            // Device path: finger down, then up, with the flower rejecting the same touch first.
            check(tag + "-ostrich-press-release",
                PressRelease(lay.Secondary.center, flower, lay, true) == RewardChoiceInput.Tap.Collect,
                "Began then Ended");
        }

        static RewardChoiceInput.Tap FirstTap(
            Vector2 at, Rect flower, bool cardUp, bool hasAd,
            FlockFiveApp.OfferButtonsLayout lay, RewardChoiceInput.Leftovers leftovers)
        {
            SplashPress.ResetBoard();
            int frame = 4100 + (int)at.x + (int)at.y + (cardUp ? 17 : 0) + (hasAd ? 3 : 9);
            var samples = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 4, Pos = at, Phase = SplashPress.Phase.Tap },
            };
            // LEVEL is under the buttons and blocked while the card is up. It must not claim the tap.
            var level = new SplashPress.Gesture();
            level.Apply(new SplashPress.Hit { Rect = flower }, true, samples, true, true, frame);
            var after = RewardChoiceInput.Drop(cardUp, leftovers);
            bool live = RewardChoiceInput.Live(cardUp, false, false);
            if (cardUp && (after.FightHold || after.PestHold || after.RuleEat || after.LessonGuard))
                return RewardChoiceInput.Tap.None;
            var press = RewardChoiceInput.Resolve(
                cardUp && live, hasAd, false, false,
                lay.Primary, lay.Secondary,
                new SplashPress.Gesture(), new SplashPress.Gesture(),
                samples, true, true, frame);
            return press.Action;
        }

        static RewardChoiceInput.Tap PressRelease(Vector2 at, Rect flower, FlockFiveApp.OfferButtonsLayout lay, bool hasAd)
        {
            SplashPress.ResetBoard();
            var watch = new SplashPress.Gesture();
            var collect = new SplashPress.Gesture();
            var level = new SplashPress.Gesture();
            int down = 8200 + (int)at.y;
            var began = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 11, Pos = at, Phase = SplashPress.Phase.Began },
            };
            level.Apply(new SplashPress.Hit { Rect = flower }, true, began, true, true, down);
            var held = RewardChoiceInput.Resolve(true, hasAd, false, false, lay.Primary, lay.Secondary, watch, collect, began, true, true, down);
            if (held.Action != RewardChoiceInput.Tap.None) return RewardChoiceInput.Tap.None;
            int up = down + 1;
            var ended = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 11, Pos = at, Phase = SplashPress.Phase.Ended },
            };
            level.Apply(new SplashPress.Hit { Rect = flower }, true, ended, false, true, up);
            var press = RewardChoiceInput.Resolve(true, hasAd, false, false, lay.Primary, lay.Secondary, watch, collect, ended, false, true, up);
            return press.Action;
        }
    }
}
#endif

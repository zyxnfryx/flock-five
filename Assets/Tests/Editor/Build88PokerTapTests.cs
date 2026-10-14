#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 88. One touch on the home poker button while the coach glove is up
    // opens poker and dismisses the coach. The hand does not come back.
    public static class Build88PokerTapTests
    {
        const string Out = "/tmp/flock-five-b88-poker-tap-tests.txt";

        [UnityEditor.MenuItem("Flock Five/Build 88 Poker Tap Tests")]
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
                if (ok) Debug.Log("[build88pokertap] " + line);
                else Debug.LogError("[build88pokertap] " + line);
            }

            SplashPress.Phase moved;
            bool movedOk = SplashPress.ClassifyContact(false, false, false, true, true, out moved);
            Check("android-moved-is-began", movedOk && moved == SplashPress.Phase.Began, moved.ToString());

            SplashPress.Phase held;
            bool heldOk = SplashPress.ClassifyContact(false, false, false, true, false, out held);
            Check("held-stays-held", heldOk && held == SplashPress.Phase.Held, held.ToString());

            SplashPress.Phase tap;
            bool tapOk = SplashPress.ClassifyContact(false, true, false, false, true, out tap);
            Check("same-frame-tap", tapOk && tap == SplashPress.Phase.Tap, tap.ToString());

            SplashPress.Phase none;
            bool noneOk = SplashPress.ClassifyContact(false, false, false, false, false, out none);
            Check("no-contact", !noneOk, none.ToString());

            Check("stale-touch-hides-mouse",
                !SplashPress.MouseStandsIn(false, 1, false, false),
                "held only");
            Check("stale-touch-yields-to-click",
                SplashPress.MouseStandsIn(false, 1, true, false)
                && SplashPress.MouseStandsIn(false, 1, false, true),
                "mouse or imgui");
            Check("fresh-finger-owns-the-sample",
                !SplashPress.MouseStandsIn(true, 1, true, true),
                "began");

            var eating = new SplashPress.Gates
            {
                CoachAte = true,
                LessonGuard = true,
                StepGate = true,
                PopupGate = true,
                ResumeSwallow = true,
                SoftModal = true,
                Consent = true,
                RailLesson = true,
            };
            var hit = CoachPress.Resolve(true, HomeControl.Poker, HomeControl.Poker, eating, true);
            Check("one-tap-opens-and-dismisses",
                hit.Open && hit.Dismiss && !hit.Rearm && !hit.Blocked,
                "open=" + hit.Open + " dismiss=" + hit.Dismiss + " rearm=" + hit.Rearm + " blocked=" + hit.Blocked);

            var missed = CoachPress.Resolve(true, HomeControl.Poker, HomeControl.Poker, eating, false);
            Check("miss-does-not-open",
                !missed.Open && !missed.Dismiss && missed.Rearm,
                "open=" + missed.Open + " rearm=" + missed.Rearm);

            var other = CoachPress.Resolve(true, HomeControl.Poker, HomeControl.Hive, eating, true);
            Check("other-button-stays-shut",
                !other.Open && !other.Dismiss && other.Rearm && other.Blocked,
                "open=" + other.Open + " blocked=" + other.Blocked);

            var hard = eating;
            hard.HardModal = true;
            var covered = CoachPress.Resolve(true, HomeControl.Poker, HomeControl.Poker, hard, true);
            Check("real-card-still-blocks",
                !covered.Open && covered.Rearm && covered.Blocked,
                "open=" + covered.Open + " blocked=" + covered.Blocked);

            var after = CoachPress.Resolve(false, HomeControl.None, HomeControl.Poker, default, true);
            Check("dismissed-coach-does-not-rearm",
                after.Open && !after.Dismiss && !after.Rearm && !after.Blocked,
                "open=" + after.Open + " dismiss=" + after.Dismiss + " rearm=" + after.Rearm);

            const int pokerStep = (int)CoachStep.PokerIntro;
            Check("fade-does-not-rearm-after-dismiss",
                CoachPress.ReturnStep(pokerStep, pokerStep, true) < 0
                && CoachPress.ReturnStep(pokerStep, pokerStep, false) == pokerStep
                && CoachPress.ReturnStep(pokerStep, -1, false) < 0,
                "same=" + CoachPress.ReturnStep(pokerStep, pokerStep, true)
                + " miss=" + CoachPress.ReturnStep(pokerStep, pokerStep, false));

            var poker = new Rect(860f, 420f, 150f, 150f);
            SplashPress.ResetBoard();
            SplashPress.Layout(SplashPress.Id.Poker, poker);
            var button = SplashPress.PressHit(SplashPress.Id.Poker);
            var finger = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 7, Pos = poker.center, Phase = SplashPress.Phase.Began },
            };
            bool blocked = SplashPress.Blocks(new SplashPress.Gates
            {
                CoachAte = true,
                LessonGuard = true,
                StepGate = true,
                PopupGate = true,
                ResumeSwallow = true,
                SoftModal = true,
                Consent = true,
                RailLesson = true,
                LessonOwn = true,
            });
            var gesture = new SplashPress.Gesture();
            var first = gesture.Apply(button, blocked, finger, true, true, 88, true);
            var again = gesture.Apply(button, blocked, finger, true, true, 88, true);
            var release = gesture.Apply(button, blocked,
                new List<SplashPress.Sample>
                {
                    new SplashPress.Sample { TouchId = 7, Pos = poker.center + new Vector2(40f, 30f), Phase = SplashPress.Phase.Ended },
                },
                false, true, 89, true);
            Check("single-touch-launches-once",
                !blocked && first.Launch && !again.Launch && !release.Launch && !gesture.Pressed,
                "blocked=" + blocked + " first=" + first.Launch + " again=" + again.Launch + " release=" + release.Launch);

            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            string press = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.SplashPress.cs"));
            string coach = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Coach.cs"));
            int pokerAt = app.IndexOf("SplashPress.Id.Poker");
            string pokerBody = pokerAt >= 0 ? app.Substring(pokerAt, Mathf.Min(700, app.Length - pokerAt)) : "";
            bool wired = press.Contains("CoachPress.OwnsTarget")
                && press.Contains("LessonOwn = own")
                && press.Contains("ownTarget")
                && app.Contains("RailTapRect(RailPoker)")
                && pokerBody.Contains("pokerGates.LessonOwn")
                && coach.Contains("RailTapRect(RailPoker)")
                && coach.Contains("_gloveHidStep = -1")
                && coach.Contains("_gloveFadeDismissed");
            Check("shared-path-wired", wired, "poker=" + (pokerAt >= 0));

            sb.AppendLine("pass " + pass + " fail " + fail);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build88pokertap] " + (fail == 0 ? "ALL OK " + pass : "FAILED " + fail + " passed " + pass));
            if (fail > 0) throw new System.Exception("poker tap tests failed " + fail);
        }
    }
}
#endif

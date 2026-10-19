#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b131 (Brandon): background the app during first-run onboarding (Level 2 splash, "Tap Daily for
    // your bonus!"), come back: the bird was UNNAMED and the tutorial was gone. Rules under test:
    //   - adopted but unnamed on a fresh process -> back to the look step, name picker required
    //   - never adopted -> whole scene; adopted and named -> nothing owed
    //   - a heal (pause/focus/resume) never plans a dismissal for a healthy splash lesson
    //   - a heal reset does not stamp a lesson done (source wiring)
    //   - only the glove's target takes a tap while the lesson is up (global tutorial gate)
    //   - Daily caption stays inside the safe text width on narrow iPhones, two lines at most
    public static class PauseResumeTutorialTests
    {
        const string Out = "/tmp/flock-five-b131-tests.txt";

        [MenuItem("Flock Five/Pause Resume Tutorial Tests")]
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
                Debug.Log("[b131] " + line);
            }

            NameGate(Check);
            AdoptBootPlan(Check);
            HealKeepsLesson(Check);
            GlovePointsOnly(Check);
            CaptionWidth(Check);
            Wiring(Check);

            string end = fail == 0 ? "B131_OK " + pass : "B131_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[b131] " + end);
            if (fail > 0) throw new Exception(end);
        }

        static void NameGate(Action<string, bool, string> Check)
        {
            Check("needs-name-flag-0", AdoptResume.NeedsName(0, true), "suggested name only");
            Check("needs-name-flag-1", !AdoptResume.NeedsName(1, false), "named flag wins");
            Check("needs-name-missing-flag-with-name", !AdoptResume.NeedsName(-1, true), "older save keeps its name");
            Check("needs-name-missing-flag-no-name", AdoptResume.NeedsName(-1, false), "");
        }

        static void AdoptBootPlan(Action<string, bool, string> Check)
        {
            Check("boot-level-1-nothing", AdoptResume.Plan(0, false, -1, false) == AdoptBoot.None
                && AdoptResume.Plan(0, true, 0, true) == AdoptBoot.None, "");
            Check("boot-never-adopted-begins", AdoptResume.Plan(1, false, -1, false) == AdoptBoot.Begin, "");
            // The bug: adopted=1, named=0 after the process was reclaimed mid-adoption.
            Check("boot-adopted-unnamed-resumes-look", AdoptResume.Plan(1, true, 0, true) == AdoptBoot.ResumeLook,
                "was skipped: bird stayed unnamed");
            Check("boot-adopted-named-nothing", AdoptResume.Plan(2, true, 1, true) == AdoptBoot.None, "");
            Check("boot-legacy-named-nothing", AdoptResume.Plan(2, true, -1, true) == AdoptBoot.None, "");
            Check("boot-legacy-no-name-resumes", AdoptResume.Plan(2, true, -1, false) == AdoptBoot.ResumeLook, "");
        }

        // A suspend must not hand the heal anything to dismiss on a healthy Level 2 Daily lesson.
        static void HealKeepsLesson(Action<string, bool, string> Check)
        {
            var daily = new TutorSnapshot { Splash = true, HomeLessonLive = true, StepGateBlind = 0f, ResumeGateLeft = 0.35f };
            Check("heal-daily-lesson-on-splash-untouched", TutorialHeal.Plan(daily) == TutorFix.None, TutorialHeal.Plan(daily).ToString());
            var away = daily;
            away.StepGateBlind = 1.2f;
            Check("heal-short-blind-not-finished", (TutorialHeal.Plan(away) & TutorFix.FinishGatedStep) == 0, "");
            var longAway = daily;
            longAway.StepGateBlind = 600f;
            Check("heal-clock-jump-would-finish-step (why blind resets on suspend)",
                (TutorialHeal.Plan(longAway) & TutorFix.FinishGatedStep) != 0, "guarded in NoteAppBackground/Resume");
            var adopt = new TutorSnapshot { Splash = true, PauseHeld = false };
            Check("heal-no-fix-for-adopt-look", TutorialHeal.Plan(adopt) == TutorFix.None, "");
        }

        // Global tutorial gate: while the glove is posed only its target takes the tap.
        static void GlovePointsOnly(Action<string, bool, string> Check)
        {
            Check("gate-off-target-down-eaten", !TutorialHeal.GateLets(GatePointer.Down, false, false, false), "");
            Check("gate-off-target-up-eaten", !TutorialHeal.GateLets(GatePointer.Up, false, false, false), "");
            Check("gate-on-target-passes", TutorialHeal.GateLets(GatePointer.Down, true, false, false), "");
            Check("glove-focus-blocks-other-control", !FlockFiveApp.GloveFocusAllows(true, true, false, false), "");
            Check("glove-focus-allows-target", FlockFiveApp.GloveFocusAllows(true, true, true, false), "");
            Check("glove-focus-off-when-no-lesson", FlockFiveApp.GloveFocusAllows(false, true, false, false), "");
        }

        static void CaptionWidth(Action<string, bool, string> Check)
        {
            // Pixel sizes of narrow to wide portrait iPhones (SE 750x1334 up to Pro Max 1290x2796).
            int[][] screens = { new[] { 750, 1334 }, new[] { 828, 1792 }, new[] { 1080, 2340 }, new[] { 1170, 2532 }, new[] { 1290, 2796 } };
            bool inside = true, greedy = true;
            string why = "";
            foreach (var sc in screens)
            {
                float safe = CaptionLayout.SafeTextWidth(sc[0], sc[1]);
                // A band as wide as the whole screen (rails collapsed) must still clamp.
                float w = AdoptResume.DailyCaptionMaxWidth(sc[0], sc[0], sc[1]);
                if (w > safe + 0.01f) { inside = false; why += sc[0] + "x" + sc[1] + " w=" + w + " safe=" + safe + " "; }
                // A normal narrow band is left alone.
                float band = sc[0] * 0.58f;
                if (Mathf.Abs(AdoptResume.DailyCaptionMaxWidth(band, sc[0], sc[1]) - Mathf.Min(band, safe)) > 0.01f) greedy = false;
            }
            Check("daily-caption-inside-safe-width", inside, why);
            Check("daily-caption-band-kept-when-narrower", greedy, "");
            Check("daily-caption-no-band-fallback-inside-safe",
                AdoptResume.DailyCaptionMaxWidth(0f, 750f, 1334f) <= CaptionLayout.SafeTextWidth(750f, 1334f) + 0.01f, "");
        }

        static void Wiring(Action<string, bool, string> Check)
        {
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            string av = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Avatar.cs");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");

            string arm = Slice(av, "void ArmAdopt()", "bool AdoptTurn()");
            Check("wiring-armadopt-plans-boot-and-resumes-look",
                arm.Contains("AdoptResume.Plan(") && arm.Contains("ResumeAdoptLook();"), "");
            string resume = Slice(av, "void ResumeAdoptLook()", "bool AdoptTurn()");
            Check("wiring-resume-look-keeps-name-required",
                resume.Contains("_adoptStep = AdoptStep.Look;") && resume.Contains("_adoptOwed = true;")
                && !resume.Contains("AvatarNamedPref") && !resume.Contains("SetInt(AvatarAdoptedPref"), "never writes named=1");
            string tick = Slice(av, "void TickAdopt()", "void BeginAdopt()");
            Check("wiring-tickadopt-holds-step-across-pause", tick.Contains("!HoldLiveLesson()"), "");

            string heal = Slice(coach, "void HealInterruptedTutorials()", "void FreshTutorGlove()");
            Check("wiring-heal-drop-does-not-stamp-done",
                heal.Contains("NoteDailyIntroLeft(false);") && heal.Contains("NotePokerIntroLeft(false);")
                && !heal.Contains("NoteDailyIntroLeft();") && !heal.Contains("NotePokerIntroLeft();"), "");

            string bg = Slice(app, "public static void NoteAppBackground()", "public static void NoteAppResume()");
            string rs = Slice(app, "public static void NoteAppResume()", "void Start()");
            Check("wiring-suspend-is-not-blind-time",
                bg.Contains("_gateBlindSince = -1f;") && rs.Contains("_gateBlindSince = -1f;"), "");
            Check("wiring-pause-and-focus-both-note",
                Slice(app, "void OnApplicationPause(bool paused)", "void OnApplicationQuit()").Contains("NoteAppBackground();")
                && Slice(app, "void OnApplicationFocus(bool focus)", "void Update()").Contains("NoteAppResume();"), "");

            string box = Slice(coach, "Rect DailyIntroBox(float s)", "Vector2 MeasureDailyIntro(");
            Check("wiring-daily-caption-width-clamped", box.Contains("AdoptResume.DailyCaptionMaxWidth("), "");
            // Brandon: captions are at most two lines. The authored line has one break.
            int nl = 0;
            string line = "Tap Daily for\nyour bonus!";
            foreach (char c in line) if (c == '\n') nl++;
            Check("daily-line-two-lines", nl == 1 && coach.Contains("const string DailyIntroLine = \"Tap Daily for\\nyour bonus!\";"), "");
        }

        static string Slice(string src, string from, string to)
        {
            int a = src.IndexOf(from, StringComparison.Ordinal);
            if (a < 0) return "";
            int b = src.IndexOf(to, a + from.Length, StringComparison.Ordinal);
            return b < 0 ? src.Substring(a) : src.Substring(a, b - a);
        }
    }
}
#endif

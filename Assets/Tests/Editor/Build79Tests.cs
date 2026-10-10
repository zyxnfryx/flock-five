#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 79. A Level 1 tap uses SplashPress.CanBegin, Blocks, and Gesture.Apply,
    // the same three calls StepHomePress makes on device. Fresh-install consent,
    // a closed Settings sheet, and a Settings sheet that opens and closes all
    // have to launch. ATT waits until the home screen after the level 2 win.
    static class Build79Tests
    {
        const float W = 1179f;
        const float H = 2556f;

        [MenuItem("Flock Five/Build 79 Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build79] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            bool hadChoice = PlayerPrefs.HasKey(AdConsent.ChoiceKey);
            int choice = PlayerPrefs.GetInt(AdConsent.ChoiceKey, 0);
            bool hadAsk = PlayerPrefs.HasKey(AdConsent.AttAskKey);
            int askKeep = PlayerPrefs.GetInt(AdConsent.AttAskKey, 0);
            bool hadSnooze = PlayerPrefs.HasKey(AdConsent.AttSnoozeKey);
            int snoozeKeep = PlayerPrefs.GetInt(AdConsent.AttSnoozeKey, 0);
            try
            {
                AdConsent.ResetTransient();
                var safe = new Rect(0f, 102f, W, H - 102f - 177f);
                var level = SplashPress.LevelRect(W, H, safe);
                var legacy = LegacyNotNow(W, H);
                Check("legacy-not-now-covered-level",
                    legacy.Contains(level.center),
                    "notNow=" + legacy + " level=" + level.center);

                Fresh(Check, "us", AdConsent.RegionClass.UnitedStates, false, level, safe);
                Fresh(Check, "eea", AdConsent.RegionClass.EeaUk, true, level, safe);
                Fresh(Check, "unknown", AdConsent.RegionClass.Unknown, true, level, safe);

                AdConsent.ResetTransient();
                Check("settings-closed-launches",
                    Tap(level, false, false, 400),
                    "closed");
                Check("settings-open-blocks",
                    !Tap(level, false, true, 420),
                    "open");
                Check("settings-closed-again-launches",
                    Tap(level, false, false, 440),
                    "closed again");

                var began = new List<SplashPress.Sample>
                {
                    new SplashPress.Sample { TouchId = 3, Pos = level.center, Phase = SplashPress.Phase.Began }
                };
                Check("used-event-still-begins",
                    SplashPress.CanBegin(EventType.Used, 0, began)
                    && SplashPress.CanBegin(EventType.MouseDown, 0, null)
                    && !SplashPress.CanBegin(EventType.Used, 0, null),
                    "touch began arms");

                Check("covered-control-does-not-launch",
                    !Tap(level, true, false, 460),
                    "consent gate");

                var gear = FlockFiveApp.SettingsGearRect(W, H, safe);
                float logoW = W * 0.72f * 0.91f;
                float logoLeft = (W - logoW) * 0.5f;
                Check("gear-clears-logo",
                    gear.width >= 44f && gear.height >= 44f
                    && gear.xMax < logoLeft - 4f
                    && gear.y >= H - safe.yMax
                    && gear.xMax < W * 0.5f,
                    "gear=" + gear + " logoLeft=" + logoLeft);

                AdConsent.ResetTransient();
                AdConsent.EditorHoldAtt = true;
                var attCard = AdConsent.LayoutHome(W, H, safe, true, false, 0);
                Check("att-card-misses-level-and-gear",
                    attCard.Kind == AdConsent.Sheet.Att
                    && attCard.Card.yMax < level.yMin - 1f
                    && !attCard.Card.Overlaps(level)
                    && !attCard.Card.Overlaps(gear)
                    && attCard.Accept.height >= 44f
                    && attCard.Decline.height >= 44f
                    && !AdConsent.BlocksControl(level)
                    && AdConsent.BlocksPoint(attCard.Card.center),
                    "card=" + attCard.Card + " level=" + level);

                CheckAtt(Check);
                CheckBoot(Check);
                CheckSmoke(Check);
                CheckSheet(Check);
            }
            finally
            {
                AdConsent.ResetTransient();
                if (hadChoice) PlayerPrefs.SetInt(AdConsent.ChoiceKey, choice);
                else PlayerPrefs.DeleteKey(AdConsent.ChoiceKey);
                if (hadAsk) PlayerPrefs.SetInt(AdConsent.AttAskKey, askKeep);
                else PlayerPrefs.DeleteKey(AdConsent.AttAskKey);
                if (hadSnooze) PlayerPrefs.SetInt(AdConsent.AttSnoozeKey, snoozeKeep);
                else PlayerPrefs.DeleteKey(AdConsent.AttSnoozeKey);
                PlayerPrefs.Save();
            }

            Debug.Log("[build79] " + (fail == 0 ? "BUILD79_OK " + pass : "BUILD79_FAIL " + fail + " passed " + pass));
            if (fail > 0)
                throw new System.Exception("BUILD79_FAIL " + fail + " passed " + pass);
        }

        static void Fresh(
            System.Action<string, bool, string> Check,
            string name,
            AdConsent.RegionClass region,
            bool wantsCard,
            Rect level,
            Rect safe)
        {
            AdConsent.ResetTransient();
            AdConsent.ArmLaunch(region);
            var home = AdConsent.LayoutHome(W, H, safe, true, false, 0);
            bool cardOk = wantsCard
                ? home.Kind == AdConsent.Sheet.Gdpr && home.Card.yMax < level.yMin - 1f && !home.Card.Overlaps(level)
                : home.Kind == AdConsent.Sheet.None;
            bool blocked = AdConsent.BlocksControl(level) || AdConsent.BlocksPoint(level.center);
            bool launched = Tap(level, AdConsent.BlocksControl(level), false, name.Length * 20);
            Check("fresh-" + name + "-level-launches",
                cardOk && !blocked && launched,
                "kind=" + home.Kind + " blocked=" + blocked + " launch=" + launched + " card=" + home.Card);
        }

        static void CheckAtt(System.Action<string, bool, string> Check)
        {
            PlayerPrefs.DeleteKey(AdConsent.AttAskKey);
            PlayerPrefs.DeleteKey(AdConsent.AttSnoozeKey);
            AdConsent.ResetTransient();

            bool before = AdConsent.Consider(0, true, false, 0, 0) == AdConsent.AttMoment.None
                && AdConsent.Consider(1, true, false, 0, 0) == AdConsent.AttMoment.None
                && AdConsent.Consider(2, false, false, 0, 0) == AdConsent.AttMoment.None
                && AdConsent.Consider(2, true, true, 0, 0) == AdConsent.AttMoment.None
                && AdConsent.Consider(2, true, false, 0, 0) == AdConsent.AttMoment.PrePrompt;
            Check("no-att-before-level-2-home", before, "consider");

            bool preFirst = AdConsent.Consider(2, true, false, 0, 0) == AdConsent.AttMoment.PrePrompt
                && !AdConsent.ShouldRequestSystem(false, true, false, false);
            AdConsent.ContinueAtt();
            bool system = AdConsent.SystemPending
                && AdConsent.ShouldRequestSystem(true, true, false, false)
                && !AdConsent.ShouldRequestSystem(true, false, false, false)
                && !AdConsent.ShouldRequestSystem(true, true, true, false)
                && !AdConsent.ShouldRequestSystem(true, true, false, true)
                && !AdConsent.PollSystem(false, false, false)
                && AdConsent.SystemPending
                && !AdConsent.PollSystem(true, true, false)
                && !AdConsent.PollSystem(true, false, true)
                && AdConsent.PollSystem(true, false, false)
                && !AdConsent.SystemPending;
            Check("preprompt-then-system-when-active",
                preFirst && system,
                "pending=" + AdConsent.SystemPending);

            PlayerPrefs.DeleteKey(AdConsent.AttAskKey);
            PlayerPrefs.DeleteKey(AdConsent.AttSnoozeKey);
            AdConsent.ResetTransient();
            AdConsent.NotNow(2);
            int ask = PlayerPrefs.GetInt(AdConsent.AttAskKey, -1);
            int snooze = PlayerPrefs.GetInt(AdConsent.AttSnoozeKey, -1);
            bool held = ask == 1 && snooze == 2
                && AdConsent.Consider(2, true, false, ask, snooze) == AdConsent.AttMoment.None
                && AdConsent.Consider(3, true, false, ask, snooze) == AdConsent.AttMoment.PrePrompt;
            AdConsent.NotNow(3);
            ask = PlayerPrefs.GetInt(AdConsent.AttAskKey, -1);
            bool done = ask == 2
                && AdConsent.Consider(4, true, false, ask, PlayerPrefs.GetInt(AdConsent.AttSnoozeKey, 0)) == AdConsent.AttMoment.None
                && AdConsent.Consider(9, true, false, ask, 0) == AdConsent.AttMoment.None;
            Check("not-now-one-reask", held && done, "ask=" + ask + " snooze=" + snooze);
        }

        static void CheckBoot(System.Action<string, bool, string> Check)
        {
            var gate = default(AdConsent.Gate);
            string consent = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/AdConsent.cs"));
            int run = consent.IndexOf("static IEnumerator Run");
            int wait = run < 0 ? -1 : consent.IndexOf("WaitForAds", run);
            Check("ads-boot-without-att",
                AdConsent.MayBoot(true, true, gate)
                && !AdConsent.MayBoot(false, true, gate)
                && wait < 0
                && consent.IndexOf("AttTimeoutSeconds") < 0
                && consent.IndexOf("GUI.Button") < 0
                && consent.IndexOf(AdConsent.AttBody) >= 0,
                "wait=" + wait);
        }

        static void CheckSmoke(System.Action<string, bool, string> Check)
        {
            bool had = PlayerPrefs.HasKey(AdConsent.ChoiceKey);
            int raw = PlayerPrefs.GetInt(AdConsent.ChoiceKey, 0);
            AdConsent.ResetTransient();
            AdConsent.ArmLaunch(AdConsent.RegionClass.EeaUk);
            var safe = new Rect(0f, 102f, W, H - 102f - 177f);
            var home = AdConsent.LayoutHome(W, H, safe, true, false, 0);
            bool painted = AdConsent.BlocksPoint(home.Card.center);
            AdConsent.DismissTransient();
            bool released = !AdConsent.BlocksPoint(home.Card.center) && !AdConsent.BlocksControl(home.Card);
            bool choiceSame = PlayerPrefs.HasKey(AdConsent.ChoiceKey) == had
                && PlayerPrefs.GetInt(AdConsent.ChoiceKey, 0) == raw;

            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            string consent = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/AdConsent.cs"));
            bool wired = app.IndexOf("FF_LEVEL_STARTED") >= 0
                && app.IndexOf("PressLevelButton()") >= 0
                && app.IndexOf("Tracking.AskOnce") < 0
                && consent.IndexOf("GetCommandLineArgs") >= 0
                && consent.IndexOf("\"-ffSmokeAutoPlay\"") >= 0
                && !AdConsent.SmokeAutoPlay();
            Check("smoke-dismiss-does-not-persist",
                painted && released && choiceSame && wired,
                "painted=" + painted + " released=" + released + " choice=" + choiceSame + " wired=" + wired);
        }

        static void CheckSheet(System.Action<string, bool, string> Check)
        {
            var rows = SettingsSheet.Place(new Rect(24f, 80f, 360f, 400f), 1f);
            bool rowsOk = rows.PrivacyLink.height == rows.Label.height
                && rows.PrivacyLink.xMin == rows.Label.xMin
                && rows.Toggle.width >= 44f;
            string ui = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Settings.cs"));
            string press = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.SplashPress.cs"));
            bool drawn = ui.IndexOf("DrawCheckbox(rows.Toggle") >= 0
                && ui.IndexOf("DrawWoodPanel") >= 0
                && ui.IndexOf("DrawDailyFrame") < 0
                && ui.IndexOf("Blanket") < 0
                && ui.IndexOf("GuiSlot.SettingsChevron") >= 0;
            bool hard = press.IndexOf("_settingsOpen") >= 0;
            Check("settings-checkbox-and-panel",
                rowsOk && drawn && hard,
                "rows=" + rowsOk + " drawn=" + drawn);
        }

        // The build 78 consent form, reconstructed. GUI.matrix scaled by width/400,
        // and the Not now button was the second control.
        static Rect LegacyNotNow(float w, float h)
        {
            float s = w / 400f;
            if (s < 0.5f) s = 0.5f;
            float gh = h / Mathf.Max(s, 0.01f);
            return new Rect(24f * s, (gh * 0.62f + 64f) * s, (400f - 48f) * s, 52f * s);
        }

        // StepHomePress: CanBegin, then Blocks, then Gesture.Apply. A Used mouse
        // event is what the old consent GUI.Button left behind.
        static bool Tap(Rect level, bool consent, bool hard, int frame)
        {
            SplashPress.ResetBoard();
            SplashPress.Layout(SplashPress.Id.Level, level);
            var hit = SplashPress.PressHit(SplashPress.Id.Level);
            var began = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 11, Pos = level.center, Phase = SplashPress.Phase.Began }
            };
            bool can = SplashPress.CanBegin(EventType.Used, 0, began);
            var gates = new SplashPress.Gates { Consent = consent, HardModal = hard };
            bool blocked = SplashPress.Blocks(gates);
            var g = new SplashPress.Gesture();
            g.Apply(hit, blocked, began, can, true, frame);
            var ended = new List<SplashPress.Sample>
            {
                new SplashPress.Sample { TouchId = 11, Pos = level.center, Phase = SplashPress.Phase.Ended }
            };
            var up = g.Apply(hit, blocked, ended, SplashPress.CanBegin(EventType.Used, 0, ended), true, frame + 1);
            return can && up.Launch;
        }
    }
}
#endif

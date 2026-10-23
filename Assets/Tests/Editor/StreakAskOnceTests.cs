#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Brandon: the streak card ("Want a reminder so you never miss your streak?")
    // came back on day two after a daily claim. One ask, ever. Yes and Not now both
    // stick, and so does the card being shown. A notification permission ask already
    // made this session (the card, or the OS sheet) keeps it off. The daily-wheel
    // landing clink waits, the way coin fly does, while coin audio is held.
    public static class StreakAskOnceTests
    {
        const string Out = "/tmp/flock-five-streak-ask-tests.txt";

        [MenuItem("Flock Five/Streak Ask Once Tests")]
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
                Debug.Log("[streak-ask] " + line);
            }

            var saved = SnapAsk();
            try
            {
                OnceEver(Check);
                SameSession(Check);
                WheelLandClink(Check);
                Wiring(Check);
            }
            finally
            {
                DailyReminder.ResetSessionForTest();
                ForegroundGate.ResetForTest();
                RestoreAsk(saved);
            }

            string end = fail == 0 ? "STREAK_ASK_OK " + pass : "STREAK_ASK_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[streak-ask] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        struct AskSnap
        {
            public bool Asked, Yes, Asks, Owed;
            public int AskedV, YesV, AsksV, OwedV;
        }

        static AskSnap SnapAsk()
        {
            return new AskSnap
            {
                Asked = PlayerPrefs.HasKey(DailyReminder.AskedKey),
                Yes = PlayerPrefs.HasKey(DailyReminder.YesKey),
                Asks = PlayerPrefs.HasKey(DailyReminder.AsksKey),
                Owed = PlayerPrefs.HasKey(DailyReminder.OsOwedKey),
                AskedV = PlayerPrefs.GetInt(DailyReminder.AskedKey, 0),
                YesV = PlayerPrefs.GetInt(DailyReminder.YesKey, 0),
                AsksV = PlayerPrefs.GetInt(DailyReminder.AsksKey, 0),
                OwedV = PlayerPrefs.GetInt(DailyReminder.OsOwedKey, 0)
            };
        }

        static void RestoreAsk(AskSnap s)
        {
            Put(DailyReminder.AskedKey, s.Asked, s.AskedV);
            Put(DailyReminder.YesKey, s.Yes, s.YesV);
            Put(DailyReminder.AsksKey, s.Asks, s.AsksV);
            Put(DailyReminder.OsOwedKey, s.Owed, s.OwedV);
            PlayerPrefs.Save();
        }

        static void Put(string key, bool had, int value)
        {
            if (!had) PlayerPrefs.DeleteKey(key);
            else PlayerPrefs.SetInt(key, value);
        }

        static void Fresh()
        {
            DailyReminder.ResetSessionForTest();
            PlayerPrefs.DeleteKey(DailyReminder.AskedKey);
            PlayerPrefs.DeleteKey(DailyReminder.YesKey);
            PlayerPrefs.DeleteKey(DailyReminder.AsksKey);
            PlayerPrefs.DeleteKey(DailyReminder.OsOwedKey);
            PlayerPrefs.Save();
        }

        // A new launch keeps PlayerPrefs and clears the session latch.
        static void NextDay() => DailyReminder.ResetSessionForTest();

        static void OnceEver(System.Action<string, bool, string> Check)
        {
            Fresh();
            Check("ask-due-until-shown", DailyReminder.PromptDue() && !DailyReminder.PermissionAskedThisSession, "");

            DailyReminder.MarkShown();
            bool shown = !DailyReminder.PromptDue()
                && PlayerPrefs.GetInt(DailyReminder.AskedKey, 0) == 1
                && PlayerPrefs.GetInt(DailyReminder.AsksKey, 0) >= 1
                && PlayerPrefs.GetInt(DailyReminder.YesKey, 0) == 0;
            NextDay();
            Check("shown-card-never-returns", shown && !DailyReminder.PromptDue(), "asked " + PlayerPrefs.GetInt(DailyReminder.AskedKey, 0));

            Fresh();
            DailyReminder.Decline();
            bool notNow = !DailyReminder.PromptDue()
                && PlayerPrefs.GetInt(DailyReminder.AskedKey, 0) == 1
                && PlayerPrefs.GetInt(DailyReminder.YesKey, 0) == 0
                && !DailyReminder.OsRequestOwed;
            NextDay();
            Check("not-now-never-returns", notNow && !DailyReminder.PromptDue(), "");

            Fresh();
            DailyReminder.Accept();
            bool yes = !DailyReminder.PromptDue()
                && PlayerPrefs.GetInt(DailyReminder.YesKey, 0) == 1
                && DailyReminder.OsRequestOwed;
            NextDay();
            Check("yes-never-reasks", yes && !DailyReminder.PromptDue() && DailyReminder.OsRequestOwed, "");

            Fresh();
            PlayerPrefs.SetInt(DailyReminder.AsksKey, 1);
            PlayerPrefs.SetInt(DailyReminder.AskedKey, 0);
            PlayerPrefs.SetInt(DailyReminder.YesKey, 0);
            PlayerPrefs.DeleteKey(DailyReminder.OsOwedKey);
            PlayerPrefs.Save();
            NextDay();
            Check("one-prior-not-now-stays-quiet", !DailyReminder.PromptDue(), "asks " + PlayerPrefs.GetInt(DailyReminder.AsksKey, 0));
        }

        static void SameSession(System.Action<string, bool, string> Check)
        {
            Fresh();
            DailyReminder.NotePermissionAsked();
            bool blocked = !DailyReminder.PromptDue() && DailyReminder.PermissionAskedThisSession
                && !PlayerPrefs.HasKey(DailyReminder.AskedKey);
            NextDay();
            Check("other-notification-ask-blocks-this-session", blocked, "");
            Check("new-session-can-ask-if-never-answered", DailyReminder.PromptDue() && !DailyReminder.PermissionAskedThisSession, "");

            Fresh();
            DailyReminder.Accept();
            NextDay();
            DailyReminder.RequestOsNow();
            PlayerPrefs.DeleteKey(DailyReminder.AskedKey);
            PlayerPrefs.DeleteKey(DailyReminder.YesKey);
            PlayerPrefs.DeleteKey(DailyReminder.AsksKey);
            PlayerPrefs.DeleteKey(DailyReminder.OsOwedKey);
            PlayerPrefs.Save();
            bool afterSheet = !DailyReminder.OsRequestOwed && DailyReminder.PermissionAskedThisSession && !DailyReminder.PromptDue();
            NextDay();
            Check("os-sheet-counts-as-this-sessions-ask", afterSheet, "");
            Check("os-sheet-does-not-stick-without-an-answer", DailyReminder.PromptDue(), "");
        }

        static void WheelLandClink(System.Action<string, bool, string> Check)
        {
            bool landed = false;
            int clinks = 0;
            void Step(bool allows)
            {
                if (landed) return;
                if (FlockFiveApp.HoldDailyWheelClink(allows)) return;
                landed = true;
                clinks++;
            }

            Step(false);
            Step(false);
            Check("wheel-land-waits-while-coin-audio-held", !landed && clinks == 0
                && FlockFiveApp.HoldDailyWheelClink(false)
                && WelcomeAward.HoldCoinPlay(false, false), "");
            Step(true);
            Step(true);
            Check("wheel-land-clinks-once-when-audio-returns", landed && clinks == 1
                && !FlockFiveApp.HoldDailyWheelClink(true), "clinks " + clinks);

            ForegroundGate.ResetForTest();
            ForegroundGate.NotePause(true);
            bool paused = FlockFiveApp.HoldDailyWheelClink(ForegroundGate.AllowsCoinAudio);
            ForegroundGate.NotePause(false);
            ForegroundGate.NoteFocus(false);
            bool blurred = FlockFiveApp.HoldDailyWheelClink(ForegroundGate.AllowsCoinAudio);
            ForegroundGate.ResetForTest();
            bool home = !FlockFiveApp.HoldDailyWheelClink(ForegroundGate.AllowsCoinAudio) && ForegroundGate.AllowsCoinAudio;
            Check("wheel-land-follows-the-foreground-gate", paused && blurred && home, "");
        }

        static void Wiring(System.Action<string, bool, string> Check)
        {
            string daily = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.DailyBonus.cs");
            string rem = File.ReadAllText("Assets/_Project/Scripts/App/DailyReminder.cs");

            string land = Slice(daily, "void LandDailyWheel()", "IEnumerator ResolveDailySoon");
            int hold = land.IndexOf("HoldDailyWheelClink(ForegroundGate.AllowsCoinAudio)", System.StringComparison.Ordinal);
            int ret = hold < 0 ? -1 : land.IndexOf("return;", hold, System.StringComparison.Ordinal);
            int phase = land.IndexOf("_dailyPhase = DailyWheelPhase.Landed", System.StringComparison.Ordinal);
            int clink = land.IndexOf("Sfx.Clink()", System.StringComparison.Ordinal);
            Check("wheel-land-returns-before-the-clink", hold >= 0 && ret > hold && phase > ret && clink > phase, "");

            string finish = Slice(daily, "IEnumerator FinishDailyClaim(bool firstEver)", "void NoteWelcomeClaim");
            int open = finish.IndexOf("_dailyAskOpen = true", System.StringComparison.Ordinal);
            int mark = finish.IndexOf("DailyReminder.MarkShown()", System.StringComparison.Ordinal);
            Check("claim-persists-the-ask-when-the-card-opens", open >= 0 && mark > open, "");

            string prompt = Slice(rem, "bool PromptDue()", "public static void MarkShown()");
            string decline = Slice(rem, "void Decline()", "static void StickAsked()");
            Check("prompt-is-once-and-session-deduped",
                prompt.Contains("_permissionAskedThisSession")
                && prompt.Contains("AsksKey, 0) < 1")
                && !prompt.Contains("< 2")
                && decline.Contains("StickAsked()"), "");

            string req = Slice(rem, "void RequestOsNow()", "public static void Decline()");
            int note = req.IndexOf("NotePermissionAsked()", System.StringComparison.Ordinal);
            int auth = req.IndexOf("new AuthorizationRequest", System.StringComparison.Ordinal);
            int perm = req.IndexOf("new PermissionRequest", System.StringComparison.Ordinal);
            Check("os-sheet-notes-the-session-before-it-asks", note >= 0 && auth > note && perm > note, "");

            var extra = new List<string>();
            foreach (var file in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                if (path.EndsWith("/DailyReminder.cs") || path.Contains("/Tests/")) continue;
                string text = File.ReadAllText(file);
                if (text.Contains("new AuthorizationRequest") || text.Contains("new PermissionRequest"))
                    extra.Add(path);
            }
            Check("no-other-notification-permission-ask", extra.Count == 0, extra.Count == 0 ? "" : string.Join(", ", extra.ToArray()));
        }

        static string Slice(string src, string from, string to)
        {
            int a = src.IndexOf(from, System.StringComparison.Ordinal);
            if (a < 0) return "";
            int b = src.IndexOf(to, a + from.Length, System.StringComparison.Ordinal);
            return b < 0 ? src.Substring(a) : src.Substring(a, b - a);
        }
    }
}
#endif

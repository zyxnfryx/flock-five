#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b119 (Brandon): only the first-hand tutorial flag hides the poker bet controls / holds Back.
    public static class Build119PokerLessonTests
    {
        const string Out = "/tmp/flock-five-b119-poker-lesson-tests.txt";

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
                Debug.Log("[build119] " + line);
            }
            // pageArmed, dealtKeySet, lessonHandThisVisit, backDone
            Check("tutorial: first visit before the first hand is the lesson", FlockFiveApp.PokerLessonActive(true, false, false, false), "");
            Check("tutorial: after the first hand, same visit, controls stay hidden until Back", FlockFiveApp.PokerLessonActive(true, true, true, false), "");
            Check("not tutorial: Back done, every later hand shows controls", !FlockFiveApp.PokerLessonActive(true, true, true, true) && !FlockFiveApp.PokerLessonActive(true, true, false, true), "");
            Check("not tutorial: a later visit (Back never tapped) plays normally", !FlockFiveApp.PokerLessonActive(true, true, false, false), "");
            Check("not tutorial: lesson never armed (skipped / older save)", !FlockFiveApp.PokerLessonActive(false, false, false, false) && !FlockFiveApp.PokerLessonActive(false, true, false, false), "");
            Check("lesson hold needs the flag", FlockFiveApp.PokerLessonHolds(true, true, false, false, false, true)
                && !FlockFiveApp.PokerLessonHolds(true, true, false, false, false, false), "");
            string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("result gate and settle honor the flag", src.Contains("PokerBackDone() || !PokerLessonLive()) return false;")
                && src.Contains("_pokerLessonHand = false; // each visit starts clean")
                && src.Contains("_pokerLessonHand = true;"), "");
            string end = fail == 0 ? "BUILD119_OK " + pass : "BUILD119_FAIL " + fail + " passed " + pass;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            if (fail > 0) throw new System.Exception(end);
        }
    }
}
#endif

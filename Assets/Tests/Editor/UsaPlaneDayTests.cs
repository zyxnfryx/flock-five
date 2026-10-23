#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Home-screen USA plane flies on the first eligible idle of a local calendar day,
    // then not again until that date changes. Today is injected; the helper does not
    // read the clock. The stored day changes only when a flight starts.
    public static class UsaPlaneDayTests
    {
        const string Out = "/tmp/flock-five-usa-plane-day-tests.txt";

        [MenuItem("Flock Five/USA Plane Day Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++; else fail++;
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[usa-plane-day] " + line);
            }

            var morning = new DateTime(2026, 10, 22, 0, 5, 0);
            var night = new DateTime(2026, 10, 22, 23, 59, 0);
            var nextMorning = new DateTime(2026, 10, 23, 0, 1, 0);
            string today = UsaPlaneDay.Stamp(morning);
            string later = UsaPlaneDay.Stamp(night);
            string tomorrow = UsaPlaneDay.Stamp(nextMorning);

            Check("stamp-is-local-calendar-day", today == "2026-10-22" && later == today && tomorrow == "2026-10-23",
                today + " / " + tomorrow);
            Check("leap-day-and-new-year",
                UsaPlaneDay.Stamp(new DateTime(2024, 2, 29, 12, 0, 0)) == "2024-02-29"
                && UsaPlaneDay.Stamp(new DateTime(2027, 1, 1, 0, 0, 0)) == "2027-01-01",
                "");

            // Injected today wins over the machine clock. A stored day that matches the
            // real clock must not block a flight whose caller passed a different day.
            string clock = UsaPlaneDay.Stamp(DateTime.Now);
            string other = clock == "1999-01-01" ? "1999-01-02" : "1999-01-01";
            Check("injected-today-ignores-clock",
                UsaPlaneDay.MayStart(clock, other, false)
                && !UsaPlaneDay.MayStart(clock, clock, false)
                && !UsaPlaneDay.AlreadyFlown(clock, other),
                "clock " + clock + " other " + other);

            Check("never-recorded-may-start",
                UsaPlaneDay.MayStart("", today, false) && UsaPlaneDay.MayStart(null, today, false),
                "");
            bool refused = UsaPlaneDay.TryBegin(null, "", false, out string kept);
            Check("empty-today-does-not-start",
                !UsaPlaneDay.MayStart("", "", false) && !UsaPlaneDay.MayStart(today, "", true)
                && !refused && kept == "",
                kept ?? "null");

            string stored = "";
            bool first = UsaPlaneDay.TryBegin(stored, today, false, out stored);
            Check("first-idle-records-the-day-it-starts", first && stored == today, stored);

            bool second = UsaPlaneDay.TryBegin(stored, later, false, out string afterSecond);
            Check("later-visit-same-day-does-not-fly",
                !second && !UsaPlaneDay.MayStart(stored, later, false) && afterSecond == stored,
                afterSecond);
            Check("restart-same-day-does-not-fly", !UsaPlaneDay.MayStart(today, today, false), "");

            bool forced = UsaPlaneDay.TryBegin(stored, later, true, out string forcedDay);
            Check("debug-force-still-flies-and-records",
                forced && forcedDay == later && UsaPlaneDay.MayStart(stored, later, true),
                forcedDay);

            bool next = UsaPlaneDay.TryBegin(stored, tomorrow, false, out string afterNext);
            Check("next-local-day-flies-again",
                next && afterNext == tomorrow && UsaPlaneDay.MayStart("2026-12-31", "2027-01-01", false),
                afterNext);

            // The flight that still happens keeps the first-appearance wait. The 40s
            // figure remains the legacy later-visit wait and is not a second flight.
            Check("daily-flight-keeps-15s-first-wait",
                FlockFiveApp.PlaneIdleFor(false) == 15f
                && FlockFiveApp.PlaneIdleFor(true) == 40f
                && FlockFiveApp.PlaneIdleFirst == 15f
                && FlockFiveApp.PlaneIdleLater == 40f,
                "");

            string plane = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Plane.cs");
            int writes = Count(plane, "SetString(UsaPlaneDayKey");
            int begin = plane.IndexOf("void BeginUsaPlane()", StringComparison.Ordinal);
            int end = plane.IndexOf("void EndUsaPlane()", StringComparison.Ordinal);
            int writeAt = plane.IndexOf("SetString(UsaPlaneDayKey", StringComparison.Ordinal);
            bool recordOnStart = writes == 1 && begin >= 0 && writeAt > begin && (end < 0 || writeAt < end)
                && plane.IndexOf("TryBegin(", StringComparison.Ordinal) >= 0
                && plane.IndexOf("PlaneIdleFor(UsaFlownToday())", StringComparison.Ordinal) < 0;
            Check("date-written-only-when-flight-starts", recordOnStart,
                "writes " + writes + " at " + writeAt + " begin " + begin);

            string endLine = fail == 0 ? "USA_PLANE_DAY_OK " + pass : "USA_PLANE_DAY_FAIL " + fail + " passed " + pass;
            sb.AppendLine(endLine);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[usa-plane-day] " + endLine);
            if (fail > 0) throw new Exception(endLine);
        }

        static int Count(string hay, string needle)
        {
            int n = 0, i = 0;
            while (i >= 0 && i < hay.Length)
            {
                i = hay.IndexOf(needle, i, StringComparison.Ordinal);
                if (i < 0) break;
                n++;
                i += needle.Length;
            }
            return n;
        }
    }
}
#endif

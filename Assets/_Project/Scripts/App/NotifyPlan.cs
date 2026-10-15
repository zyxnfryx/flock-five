using System;
using System.Collections.Generic;
using System.Globalization;

namespace FlockFive
{
    // Pure schedule logic for "come back" local notifications. No Unity or native calls,
    // so it is editor-testable. Rule: these exist only while the app is closed or
    // backgrounded. DailyReminder cancels everything on launch/foreground and calls
    // Compute on background/quit. Anything not safely in the future is dropped.
    public static class NotifyPlan
    {
        public const int ReadyHour = 18;
        public const int KeepHour = 20;
        // A reminder closer than this to "now" is never scheduled (it would fire almost at once).
        public const double MinLeadSeconds = 60.0;

        public enum Kind { Ready, Keep }

        public readonly struct Entry
        {
            public readonly Kind Kind;
            public readonly DateTime When;
            public readonly string Body;
            public Entry(Kind kind, DateTime when, string body) { Kind = kind; When = when; Body = body; }
        }

        public const string ReadyBody = "Your daily coins are ready.";

        public static string KeepBody(int streak)
        {
            return "Keep your " + streak.ToString(CultureInfo.InvariantCulture) + " day streak!";
        }

        // Entries to schedule when the app leaves the foreground at `now`.
        // After today's claim both alerts are tomorrow (6pm coins, 8pm streak).
        // Unclaimed: next 6pm that has not passed; the streak line only if that evening is today.
        public static List<Entry> Compute(DateTime now, int streak, bool claimedToday, bool atRisk)
        {
            if (streak < 0) streak = 0;
            bool risk = atRisk && streak >= 2;
            DateTime coins, keep;
            bool doKeep;
            if (claimedToday)
            {
                coins = DayAt(now, 1, ReadyHour);
                keep = DayAt(now, 1, KeepHour);
                doKeep = risk;
            }
            else
            {
                coins = NextFuture(now, ReadyHour);
                keep = NextFuture(now, KeepHour);
                doKeep = risk && keep.Date == now.Date;
            }
            var list = new List<Entry> { new Entry(Kind.Ready, coins, ReadyBody) };
            if (doKeep) list.Add(new Entry(Kind.Keep, keep, KeepBody(streak)));
            return Filter(list, now);
        }

        // Drops anything in the past or inside the minimum lead.
        public static List<Entry> Filter(List<Entry> entries, DateTime now)
        {
            var kept = new List<Entry>();
            for (int i = 0; i < entries.Count; i++)
                if ((entries[i].When - now).TotalSeconds >= MinLeadSeconds) kept.Add(entries[i]);
            return kept;
        }

        static DateTime DayAt(DateTime now, int daysAhead, int hour)
        {
            return new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, now.Kind).AddDays(daysAhead);
        }

        static DateTime NextFuture(DateTime now, int hour)
        {
            var slot = DayAt(now, 0, hour);
            if ((slot - now).TotalSeconds < MinLeadSeconds) slot = slot.AddDays(1);
            return slot;
        }

        // Lifecycle state machine shared by iOS and Android: true when pending/delivered
        // reminders must be cleared (foreground/launch) and when a schedule may be written.
        public static bool MayScheduleOn(bool appActive) { return !appActive; }
    }
}

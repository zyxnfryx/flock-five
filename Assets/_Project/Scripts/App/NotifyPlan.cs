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
        // iOS (and Android) give no reliable "user closed the app" callback, so a schedule written
        // on BACKGROUND is a guess that the app may be closed. To keep an ordinary app switch from
        // ever producing a reminder, a background schedule must be at least this far out; anything
        // closer rolls to the next day. A real QUIT signal only needs the small lead.
        public const double BackgroundGraceSeconds = 2 * 3600.0;

        // Why a schedule is being written. Quit = the OS told us the app is terminating (reliable
        // when it arrives). Background = app left the foreground, may or may not be closed later.
        public enum Signal { Background, Quit }

        public static double LeadFor(Signal signal)
        {
            return signal == Signal.Quit ? MinLeadSeconds : BackgroundGraceSeconds;
        }

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
        public static List<Entry> Compute(DateTime now, int streak, bool claimedToday, bool atRisk, Signal signal = Signal.Background)
        {
            double lead = LeadFor(signal);
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
                coins = NextFuture(now, ReadyHour, lead);
                keep = NextFuture(now, KeepHour, lead);
                doKeep = risk && keep.Date == now.Date;
            }
            var list = new List<Entry> { new Entry(Kind.Ready, coins, ReadyBody) };
            if (doKeep) list.Add(new Entry(Kind.Keep, keep, KeepBody(streak)));
            return Filter(list, now, lead);
        }

        // b129: the Daily is a 24h window from the last claim (DailyBonus). Hours before the
        // grace deadline (lastClaim + 48h) that the streak-at-risk reminder fires. Same 4h the
        // old 8pm reminder had before midnight.
        public const double KeepLeadSeconds = 4 * 3600.0;

        // Entries when the app leaves the foreground at `now` (local) and the last claim was at
        // lastClaimUnix (UTC seconds, 0 = never claimed).
        //   Ready:  "Your daily coins are ready." at lastClaim + 24h (not a fixed 18:00).
        //           If that moment is already past (or inside the lead) the Daily is open now, so
        //           the old nudge is used: the next 18:00 that clears the lead.
        //   Keep:   "Keep your N day streak!" KeepLeadSeconds before the grace deadline
        //           (lastClaim + 48h), only while the streak is at risk.
        // Absolute-time math (unix seconds), so a DST change never shifts either reminder.
        public static List<Entry> ComputeWindow(DateTime now, long lastClaimUnix, int streak, bool atRisk, Signal signal = Signal.Background)
        {
            if (lastClaimUnix <= 0) return Compute(now, streak, false, atRisk, signal);
            double lead = LeadFor(signal);
            if (streak < 0) streak = 0;
            bool risk = atRisk && streak >= 2;
            DateTime readyAt = DateTimeOffset.FromUnixTimeSeconds(lastClaimUnix + DailyBonus.WindowSeconds).LocalDateTime;
            DateTime deadline = DateTimeOffset.FromUnixTimeSeconds(lastClaimUnix + DailyBonus.GraceSeconds).LocalDateTime;
            DateTime keep = deadline.AddSeconds(-KeepLeadSeconds);
            DateTime coins = (readyAt - now).TotalSeconds >= lead ? readyAt : NextFuture(now, ReadyHour, lead);
            var list = new List<Entry> { new Entry(Kind.Ready, coins, ReadyBody) };
            if (risk) list.Add(new Entry(Kind.Keep, keep, KeepBody(streak)));
            return Filter(list, now, lead);
        }

        // Drops anything in the past or inside the minimum lead.
        public static List<Entry> Filter(List<Entry> entries, DateTime now, double leadSeconds = MinLeadSeconds)
        {
            var kept = new List<Entry>();
            for (int i = 0; i < entries.Count; i++)
                if ((entries[i].When - now).TotalSeconds >= leadSeconds) kept.Add(entries[i]);
            return kept;
        }

        static DateTime DayAt(DateTime now, int daysAhead, int hour)
        {
            return new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, now.Kind).AddDays(daysAhead);
        }

        static DateTime NextFuture(DateTime now, int hour, double lead)
        {
            var slot = DayAt(now, 0, hour);
            if ((slot - now).TotalSeconds < lead) slot = slot.AddDays(1);
            return slot;
        }

        // Shared by iOS and Android: nothing may be pending while the app is open/foreground.
        public static bool MayScheduleOn(bool appActive) { return !appActive; }
    }
}

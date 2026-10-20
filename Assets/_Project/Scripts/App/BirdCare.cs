using System;
using System.Collections.Generic;

namespace FlockFive
{
    // Bird Care v1 (MAGI 3-0 with conditions). Pure rules, no Unity calls, so the time math is
    // editor-testable (BirdCareTests).
    //   - One owned bird (the adopted home bird), one stat: Hunger 0..100.
    //   - No death and nothing taken away. The low state is a cute nap, never sick art.
    //   - Full to empty in about 48 hours of wall-clock time.
    //   - 3+ days away = vacation: the bird napped the whole time and greets you happy.
    //   - Feeding is free: nectar earned from play, and the very first feed is on the house.
    //     A rewarded ad can never feed or heal; it may only add a bonus on top of a feed that
    //     already happened for free (AdBonusOpen).
    // Time is unix seconds (UTC), so DST and timezone moves never shift the decay.
    public static class BirdCare
    {
        public const double Max = 100.0;
        // Casper #8: full to empty in about 48 hours.
        public const double FullToEmptySeconds = 48.0 * 3600.0;
        public const double DecayPerSecond = Max / FullToEmptySeconds;
        // One feed. Two feeds take an empty bird to full.
        public const double FeedAmount = 50.0;
        public const int FeedCost = 1;
        // Under this the bird asks for a snack (the "feed me" bubble). Not a warning colour.
        public const double HungryAt = 25.0;
        // Under this the bird naps (cute pose, closed eyes, little z's).
        public const double NapAt = 8.0;
        // Balthasar #4: 3+ days away freezes hunger and the bird greets you happy.
        public const double VacationSeconds = 3.0 * 24.0 * 3600.0;
        // The welcome-back level: comfortably fed, not hungry, no bubble.
        public const double WelcomeBackHunger = 75.0;
        // A gap longer than this is clamped (bad clock, years-old save). Vacation covers it anyway.
        public const double MaxElapsedSeconds = 60.0 * 24.0 * 3600.0;
        // The ad bonus is offered only right after a free feed.
        public const double AdBonusWindowSeconds = 120.0;
        // Nectar from one level clear. Capped so it never feels like a hoard to manage.
        public const int NectarPerClear = 1;
        public const int NectarCap = 9;

        public enum Mood { Fed, Hungry, Nap }

        public enum FeedSource { Nectar, FirstFree }

        public enum Clock { Normal, Backward, Vacation, Fresh }

        [Serializable]
        public struct State
        {
            public double Hunger;
            // High-water mark of evaluated wall time. Never moves backward.
            public long LastUnix;
            public bool FirstFeedUsed;
            // Unix time of the last free feed, for the optional ad bonus window.
            public long LastFeedUnix;
            public bool AdBonusTaken;

            public static State NewBird(long nowUnix)
            {
                return new State { Hunger = Max, LastUnix = nowUnix, FirstFeedUsed = false, LastFeedUnix = 0, AdBonusTaken = true };
            }
        }

        public static double Clamp(double h)
        {
            if (double.IsNaN(h)) return Max;
            return h < 0.0 ? 0.0 : (h > Max ? Max : h);
        }

        // Seconds to count since the last evaluation. A clock set backward counts as zero
        // (nothing decays, and the high-water mark is kept so time is never counted twice).
        public static double OfflineElapsed(long lastUnix, long nowUnix, out Clock clock)
        {
            if (lastUnix <= 0) { clock = Clock.Fresh; return 0.0; }
            long d = nowUnix - lastUnix;
            if (d < 0) { clock = Clock.Backward; return 0.0; }
            double e = d;
            if (e > MaxElapsedSeconds) e = MaxElapsedSeconds;
            clock = e >= VacationSeconds ? Clock.Vacation : Clock.Normal;
            return e;
        }

        public static double Decayed(double hunger, double elapsedSeconds)
        {
            if (elapsedSeconds <= 0.0) return Clamp(hunger);
            return Clamp(hunger - elapsedSeconds * DecayPerSecond);
        }

        // Brings the state up to now. Returns how the clock looked.
        public static Clock Advance(ref State s, long nowUnix)
        {
            s.Hunger = Clamp(s.Hunger);
            double e = OfflineElapsed(s.LastUnix, nowUnix, out var clock);
            switch (clock)
            {
                case Clock.Fresh:
                    s.LastUnix = nowUnix;
                    break;
                case Clock.Backward:
                    // Keep LastUnix: once the clock catches up, decay resumes from where it was.
                    break;
                case Clock.Vacation:
                    // Napped the whole time. Never return to a sad bird.
                    s.Hunger = Math.Max(s.Hunger, WelcomeBackHunger);
                    s.LastUnix = nowUnix;
                    break;
                default:
                    s.Hunger = Decayed(s.Hunger, e);
                    s.LastUnix = nowUnix;
                    break;
            }
            return clock;
        }

        public static Mood MoodOf(double hunger)
        {
            hunger = Clamp(hunger);
            if (hunger < NapAt) return Mood.Nap;
            if (hunger < HungryAt) return Mood.Hungry;
            return Mood.Fed;
        }

        public static bool CanFeed(State s, int nectar)
        {
            if (Clamp(s.Hunger) >= Max - 0.5) return false;
            return !s.FirstFeedUsed || nectar >= FeedCost;
        }

        public struct FeedResult
        {
            public bool Fed;
            public FeedSource Source;
            public int NectarSpent;
            public double Before;
            public double After;
        }

        // A feed: free first, then 1 nectar. Never coins, ads or real money.
        public static FeedResult Feed(ref State s, long nowUnix, int nectar)
        {
            Advance(ref s, nowUnix);
            var r = new FeedResult { Before = s.Hunger, After = s.Hunger };
            if (!CanFeed(s, nectar)) return r;
            r.Fed = true;
            if (!s.FirstFeedUsed) { r.Source = FeedSource.FirstFree; s.FirstFeedUsed = true; }
            else { r.Source = FeedSource.Nectar; r.NectarSpent = FeedCost; }
            s.Hunger = Clamp(s.Hunger + FeedAmount);
            s.LastFeedUnix = nowUnix;
            s.AdBonusTaken = false;
            r.After = s.Hunger;
            return r;
        }

        // Optional bonus after a feed that already happened for free. Never a feed itself.
        public static bool AdBonusOpen(State s, long nowUnix)
        {
            if (s.AdBonusTaken || s.LastFeedUnix <= 0) return false;
            long d = nowUnix - s.LastFeedUnix;
            return d >= 0 && d <= AdBonusWindowSeconds;
        }

        public static bool TakeAdBonus(ref State s, long nowUnix)
        {
            if (!AdBonusOpen(s, nowUnix)) return false;
            s.AdBonusTaken = true;
            return true;
        }

        public static int EarnNectar(int nectar, int clears = 1)
        {
            long n = (long)Math.Max(0, nectar) + (long)Math.Max(0, clears) * NectarPerClear;
            if (n < 0) n = 0;
            return (int)Math.Min(NectarCap, n);
        }

        // Unix time when hunger reaches `level`, or nowUnix if it is already there.
        public static long WhenHungerReaches(State s, long nowUnix, double level)
        {
            var copy = s;
            Advance(ref copy, nowUnix);
            if (copy.Hunger <= level) return nowUnix;
            double secs = (copy.Hunger - level) / DecayPerSecond;
            return nowUnix + (long)Math.Ceiling(secs);
        }
    }

    // Local "feed me" notification plan. Pure, like NotifyPlan.
    //   - Opt-in only, asked after a feed (a moment of value), never at launch.
    //   - Off by default for a child-directed audience (Balthasar #6).
    //   - Daytime only; never in the evening family hours (Casper #9).
    //   - At most one per day; cadence is an A/B variant; backs off after ignores; stops
    //     after a week of not playing.
    public static class BirdCareNotify
    {
        public enum Cadence
        {
            // A: one gentle nudge per hungry spell.
            Once = 0,
            // B: a nudge, then at most one more two days later if still unfed.
            TwoDayFollowUp = 1,
        }

        public struct Settings
        {
            public bool FeatureOn;        // the remote flag
            public bool OptedIn;          // player said yes after a feed
            public bool ChildDirected;    // store/age signal; default true = safest
            public bool ParentAllowed;    // explicit grown-up OK for a child-directed audience
            public Cadence Cadence;
            public int Ignored;           // reminders delivered with no app open since
            public int WindowStartHour;   // local, inclusive
            public int WindowEndHour;     // local, exclusive (clamped to EveningHour)
        }

        public const int DefaultStartHour = 10;
        public const int DefaultEndHour = 17;
        // Evening family time starts here. No reminder at or after this hour, ever.
        public const int EveningHour = 18;
        public const int EarliestHour = 9;
        public const double MinLeadSeconds = 2 * 3600.0;
        public const double BackoffSeconds = 3 * 24 * 3600.0;
        public const double StopAfterInactiveSeconds = 7 * 24 * 3600.0;
        public const int IgnoredBeforeBackoff = 2;

        public static Settings Defaults(bool childDirected)
        {
            return new Settings
            {
                FeatureOn = false,
                OptedIn = false,
                ChildDirected = childDirected,
                ParentAllowed = false,
                Cadence = Cadence.Once,
                Ignored = 0,
                WindowStartHour = DefaultStartHour,
                WindowEndHour = DefaultEndHour,
            };
        }

        // True when a reminder may be scheduled at all.
        public static bool Allowed(Settings st)
        {
            if (!st.FeatureOn || !st.OptedIn) return false;
            if (st.ChildDirected && !st.ParentAllowed) return false;
            return true;
        }

        // Ask for opt-in only after a feed, only once, never for a child-directed audience
        // without a grown-up OK.
        public static bool ShouldAsk(bool featureOn, bool justFed, bool askedBefore, bool childDirected, bool parentAllowed)
        {
            if (!featureOn || !justFed || askedBefore) return false;
            return !childDirected || parentAllowed;
        }

        public static string Line(string name, int variant)
        {
            if (string.IsNullOrEmpty(name)) name = "Your bird";
            // Invitations only. No "misses you", no guilt words.
            return (variant & 1) == 0
                ? name + "'s tummy is rumbling \U0001F338 A sip of nectar?"
                : name + " saved you a seat at the feeder.";
        }

        // Whole words (so a name like "Goldie" is fine). Prefixes catch "missed", "starving".
        static readonly string[] BannedPrefix = { "miss", "dying", "starv", "abandon", "lonel", "hurry", "sick", "sad" };
        static readonly string[] BannedWord = { "die", "dies", "died", "dead" };

        public static bool Warm(string line)
        {
            if (string.IsNullOrEmpty(line)) return false;
            var words = System.Text.RegularExpressions.Regex.Split(line.ToLowerInvariant(), "[^a-z]+");
            foreach (var w in words)
            {
                if (w.Length == 0) continue;
                for (int i = 0; i < BannedPrefix.Length; i++) if (w.StartsWith(BannedPrefix[i], StringComparison.Ordinal)) return false;
                for (int i = 0; i < BannedWord.Length; i++) if (w == BannedWord[i]) return false;
            }
            return true;
        }

        // Moves a local time into the next daytime window slot at or after `t`.
        public static DateTime IntoWindow(DateTime t, int startHour, int endHour)
        {
            if (startHour < EarliestHour) startHour = EarliestHour;
            if (endHour > EveningHour) endHour = EveningHour;
            if (endHour <= startHour) endHour = Math.Min(EveningHour, startHour + 1);
            var dayStart = new DateTime(t.Year, t.Month, t.Day, startHour, 0, 0, t.Kind);
            var dayEnd = new DateTime(t.Year, t.Month, t.Day, endHour, 0, 0, t.Kind);
            if (t < dayStart) return dayStart;
            if (t >= dayEnd) return dayStart.AddDays(1);
            return t;
        }

        // Entries (local times) to schedule when the app leaves the foreground.
        // lastPlayUnix: last time the app was open. Nothing if the player has been away a week.
        public static List<DateTime> Plan(BirdCare.State s, long nowUnix, long lastPlayUnix, Settings st, Func<long, DateTime> toLocal)
        {
            var list = new List<DateTime>();
            if (!Allowed(st)) return list;
            if (lastPlayUnix > 0 && nowUnix - lastPlayUnix >= StopAfterInactiveSeconds) return list;
            long hungry = BirdCare.WhenHungerReaches(s, nowUnix, BirdCare.HungryAt);
            long earliest = nowUnix + (long)MinLeadSeconds;
            if (st.Ignored >= IgnoredBeforeBackoff) earliest = Math.Max(earliest, nowUnix + (long)BackoffSeconds);
            long first = Math.Max(hungry, earliest);
            // A vacation-length gap means the bird is napping happily: no reminder past that point.
            if (first - nowUnix >= BirdCare.VacationSeconds) return list;
            var a = IntoWindow(toLocal(first), st.WindowStartHour, st.WindowEndHour);
            list.Add(a);
            if (st.Cadence == Cadence.TwoDayFollowUp && st.Ignored < IgnoredBeforeBackoff)
            {
                var b = IntoWindow(a.AddDays(2), st.WindowStartHour, st.WindowEndHour);
                // Never inside the vacation span (the bird would be napping happily by then).
                if ((b - toLocal(nowUnix)).TotalSeconds < BirdCare.VacationSeconds) list.Add(b);
            }
            return list;
        }
    }

    // Whole feature behind a remote flag, default OFF. A remote-config provider calls
    // SetRemote; until one exists nothing turns it on outside editor shots and tests.
    public static class BirdCareFlag
    {
        static bool _remote;
        static bool _remoteNotify;
        static int _remoteCadence;
        static bool _childDirected = true;

        public static bool Enabled => _remote || EditorForce;
        public static bool NotifyFeatureOn => Enabled && _remoteNotify;
        public static BirdCareNotify.Cadence Cadence => _remoteCadence == 1 ? BirdCareNotify.Cadence.TwoDayFollowUp : BirdCareNotify.Cadence.Once;
        // Safest default: treat the audience as child-directed until the store/age signal says otherwise.
        public static bool ChildDirected => _childDirected;

        public static bool EditorForce;

        public static void SetRemote(bool enabled, bool notify = false, int cadenceVariant = 0, bool childDirected = true)
        {
            _remote = enabled;
            _remoteNotify = notify;
            _remoteCadence = cadenceVariant;
            _childDirected = childDirected;
        }

        public static void ResetForTests()
        {
            _remote = false; _remoteNotify = false; _remoteCadence = 0; _childDirected = true; EditorForce = false;
        }
    }

    // Analytics hooks. No SDK is linked yet: a provider subscribes to OnEvent.
    // Balthasar #7: watch feeds by nectar vs ad bonus, so care never turns into ad pressure.
    public static class BirdCareAnalytics
    {
        public const string Feed = "bird_care_feed";
        public const string AdBonus = "bird_care_ad_bonus";
        public const string Vacation = "bird_care_welcome_back";
        public const string NotifyOptIn = "bird_care_notify_opt_in";
        public const string NotifyScheduled = "bird_care_notify_scheduled";
        public const string TutorialDone = "bird_care_tutorial_done";
        public const string Share = "bird_care_share";

        public static event Action<string, Dictionary<string, string>> OnEvent;
        public static int FeedsNectar;
        public static int FeedsFirstFree;
        public static int AdBonuses;

        public static string SourceName(BirdCare.FeedSource s) => s == BirdCare.FeedSource.FirstFree ? "free_first" : "nectar";

        public static void LogFeed(BirdCare.FeedResult r)
        {
            if (!r.Fed) return;
            if (r.Source == BirdCare.FeedSource.Nectar) FeedsNectar++; else FeedsFirstFree++;
            Emit(Feed, new Dictionary<string, string>
            {
                { "source", SourceName(r.Source) },
                { "via_ad", "false" },
                { "before", ((int)r.Before).ToString() },
                { "after", ((int)r.After).ToString() },
            });
        }

        public static void LogAdBonus()
        {
            AdBonuses++;
            Emit(AdBonus, new Dictionary<string, string> { { "source", "ad" }, { "via_ad", "true" } });
        }

        public static void Emit(string name, Dictionary<string, string> props = null)
        {
            var h = OnEvent;
            if (h != null) { try { h(name, props ?? new Dictionary<string, string>()); } catch (Exception) { } }
        }

        public static void ResetForTests() { FeedsNectar = 0; FeedsFirstFree = 0; AdBonuses = 0; OnEvent = null; }
    }
}

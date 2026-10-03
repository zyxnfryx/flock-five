using System;
using System.Globalization;
using UnityEngine;

namespace FlockFive
{
    // Splash daily medal. Ready is the red badge. A streak is the flame only
    // after a claim. Painters take one of these and never stack the two.
    public enum DailyBonusIconState
    {
        Plain,
        Badge,
        Flame
    }

    // Daily login coins. Local calendar day, not the stage-clear streak in Purse.
    // A missed day (gap of 2+) offers day 1 again. The saved streak still counts
    // past 5 ("12 day streak"); the row is only the 5-day cycle.
    // Week bonus is +5 per full cycle already finished before today's claim, cap +25.
    // Day 5 pays 100 with no week bonus. Day 6 pays 10+5. Clock behind the last
    // claim cannot claim again. A saved streak of 6 or 7 still wraps into the row.
    public static class DailyBonus
    {
        const string PrefLast = "flockfive.daily.last";
        const string PrefStreak = "flockfive.daily.streak";
        const int StreakCap = 100000;
        public const int CycleDays = 5;

        // Day 1..5. Every value is a multiple of 5. Day 5 is the crown.
        static readonly int[] DayPay = { 10, 15, 20, 25, 100 };
        static readonly string[] TileLabels = new string[CycleDays];
        static bool _tiles;

        static bool _booted;
        static int _streak;
        static int _last;
        static int _seen;
        static bool _continuing;
        static int _offerStreak = 1;
        static int _cycle;
        static int _bonus;
        static int _payout = 10;
        static bool _atRisk;
        static int _lineN = -1;
        static int _payN = -1;
        static int _bonusN = -1;
        static string _digits = "1";
        static string _streakLine = "1 day streak";
        static string _payLine = null;
        static string _bonusLine = "";

        public enum Gate
        {
            Ready,
            Claimed,
            Held
        }

        static Gate _gate = Gate.Ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _booted = false;
            _tiles = false;
            _streak = 0;
            _last = 0;
            _seen = 0;
            _continuing = false;
            _offerStreak = 1;
            _cycle = 0;
            _bonus = 0;
            _payout = 10;
            _atRisk = false;
            _lineN = -1;
            _payN = -1;
            _bonusN = -1;
            _digits = "1";
            _streakLine = "1 day streak";
            _payLine = PayFallback();
            _bonusLine = "";
            _gate = Gate.Ready;
            for (int i = 0; i < TileLabels.Length; i++) TileLabels[i] = null;
        }

        public static bool OfferReady => _booted && _gate == Gate.Ready;
        public static int Streak => _streak;
        public static int OfferStreak => _offerStreak;
        public static int Cycle => _cycle;
        public static int Payout => _payout;
        public static int Bonus => _bonus;
        public static bool AtRisk => _atRisk;
        public static bool ClaimedToday => _booted && _gate == Gate.Claimed;

        // Ready wins, so a collectable day never keeps yesterday's flame.
        // Flame is claimed-today or clock-held behind that claim, with a streak.
        public static DailyBonusIconState IconState
        {
            get
            {
                if (OfferReady) return DailyBonusIconState.Badge;
                if (_booted && _last > 0 && _streak >= 1) return DailyBonusIconState.Flame;
                return DailyBonusIconState.Plain;
            }
        }

        // Same answer as IconState. Ready hides the flame.
        public static bool FlameLit => IconState == DailyBonusIconState.Flame;
        public static string StreakDigits => _digits;
        public static string StreakLine => _streakLine;
        public static string PayLine => _payLine ?? PayFallback();
        public static string BonusLine => _bonusLine;

        public static string TileLabel(int day)
        {
            if (TileLabels.Length == 0) return PayFallback();
            if ((uint)day >= (uint)TileLabels.Length) return TileLabels[0] ?? PayFallback();
            return TileLabels[day] ?? PayFallback();
        }

        static string PayFallback() => Money.Format(10);

        public static void Boot()
        {
            if (_booted) return;
            _booted = true;
            EnsureTiles();
            _streak = PrefGuard.GetInt(PrefStreak, 0);
            if (_streak < 0) _streak = 0;
            if (_streak > StreakCap) _streak = StreakCap;
            _last = ReadStamp(PrefGuard.GetString(PrefLast, ""));
            _seen = TodayStamp();
            Rebuild();
        }

        // True when the local calendar day changed since the last look.
        public static bool RefreshDay()
        {
            if (!_booted) Boot();
            int today = TodayStamp();
            if (today == _seen) return false;
            _seen = today;
            Rebuild();
            return true;
        }

        public static bool TryClaim(out int coins, out bool firstEver)
        {
            coins = 0;
            firstEver = false;
            if (!_booted) Boot();
            RefreshDay();
            if (_gate != Gate.Ready) return false;
            int today = TodayStamp();
            if (today != _seen)
            {
                _seen = today;
                Rebuild();
                if (_gate != Gate.Ready) return false;
            }
            // Last claim is today, or the clock moved behind that day.
            if (_last >= today) return false;
            firstEver = _last <= 0;
            coins = _payout;
            if (coins < 5 || (coins % 5) != 0) return false;
            _streak = _offerStreak;
            _last = today;
            PrefGuard.SetInt(PrefStreak, _streak);
            PrefGuard.SetString(PrefLast, WriteStamp(_last));
            PlayerPrefs.Save();
            Purse.Credit(coins);
            Rebuild();
            return true;
        }

        static void Rebuild()
        {
            int today = _seen;
            _continuing = false;
            if (_last <= 0)
            {
                _gate = Gate.Ready;
                _offerStreak = 1;
            }
            else if (_last > today)
            {
                _gate = Gate.Held;
                _offerStreak = _streak < 1 ? 1 : _streak;
            }
            else if (_last == today)
            {
                _gate = Gate.Claimed;
                _offerStreak = _streak < 1 ? 1 : _streak;
            }
            else
            {
                int gap = DaysFrom(_last, today);
                if (gap <= 0)
                {
                    _gate = Gate.Held;
                    _offerStreak = _streak < 1 ? 1 : _streak;
                }
                else if (gap == 1)
                {
                    _gate = Gate.Ready;
                    _continuing = true;
                    _offerStreak = _streak + 1;
                    if (_offerStreak < 1) _offerStreak = 1;
                    if (_offerStreak > StreakCap) _offerStreak = StreakCap;
                }
                else
                {
                    _gate = Gate.Ready;
                    _offerStreak = 1;
                }
            }

            int days = DayPay.Length;
            _cycle = days > 0 ? (_offerStreak - 1) % days : 0;
            if (_cycle < 0 || _cycle >= days) _cycle = 0;
            _bonus = WeekBonus(_offerStreak);
            _payout = (days > 0 ? DayPay[_cycle] : 10) + _bonus;
            bool alive = _gate == Gate.Claimed || (_gate == Gate.Ready && _continuing);
            _atRisk = alive && _streak >= 2;
            CacheLines(_gate == Gate.Ready ? _offerStreak : (_streak < 1 ? _offerStreak : _streak));
        }

        // Full cycles completed before this claim. Day 5 is the last day of a cycle
        // and does not add the bonus; the next claim does.
        static int WeekBonus(int streakDay)
        {
            int done = streakDay - 1;
            if (done < CycleDays) return 0;
            int bonus = (done / CycleDays) * 5;
            return bonus > 25 ? 25 : bonus;
        }

        static void CacheLines(int shown)
        {
            if (shown < 1) shown = 1;
            if (shown != _lineN)
            {
                _lineN = shown;
                _digits = shown.ToString(CultureInfo.InvariantCulture);
                _streakLine = _digits + " day streak";
            }
            if (_payout != _payN)
            {
                _payN = _payout;
                _payLine = Money.Format(_payout);
            }
            if (_bonus != _bonusN)
            {
                _bonusN = _bonus;
                _bonusLine = _bonus > 0 ? "streak +" + Money.Format(_bonus) : "";
            }
        }

        static void EnsureTiles()
        {
            if (_tiles) return;
            for (int i = 0; i < DayPay.Length; i++)
                TileLabels[i] = Money.Format(DayPay[i]);
            _tiles = true;
        }

        static int TodayStamp()
        {
            var n = DateTime.Now;
            return n.Year * 10000 + n.Month * 100 + n.Day;
        }

        static int ReadStamp(string s)
        {
            if (s == null || s.Length != 8) return 0;
            int n = 0;
            for (int i = 0; i < 8; i++)
            {
                char c = s[i];
                if (c < '0' || c > '9') return 0;
                n = n * 10 + (c - '0');
            }
            int y = n / 10000;
            int m = (n / 100) % 100;
            int d = n % 100;
            if (y < 2000 || y > 9999 || m < 1 || m > 12 || d < 1 || d > 31) return 0;
            try
            {
                var dt = new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Unspecified);
                return dt.Year * 10000 + dt.Month * 100 + dt.Day;
            }
            catch (ArgumentOutOfRangeException)
            {
                return 0;
            }
        }

        static string WriteStamp(int stamp) =>
            stamp.ToString("00000000", CultureInfo.InvariantCulture);

        static int DaysFrom(int from, int to)
        {
            var a = new DateTime(from / 10000, (from / 100) % 100, from % 100, 0, 0, 0, DateTimeKind.Unspecified);
            var b = new DateTime(to / 10000, (to / 100) % 100, to % 100, 0, 0, 0, DateTimeKind.Unspecified);
            return (int)(b - a).TotalDays;
        }
    }
}

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
    // past 5. The coin amount does not use that streak.
    //
    // The prize is one spin of an 8-wedge wheel. The base is
    // 100 + 100 * floor(highestLevel / 10), capped at BaseCap. Highest level is
    // the 1-based level the player is on (levels cleared + 1). Every wedge pays
    // multiplier × base, so every prize is a multiple of 100 and at least 100.
    // Weights below are the only odds. The spin is chosen with UnityEngine.Random
    // before the wheel moves, then the wheel lands on that wedge.
    // After it lands, a rewarded ad may double that spin once. A skip or a failed
    // show pays the spin once. The once-per-day gate is the same day key as the
    // claim (flockfive.daily.last). The tutorial spin does not offer the double.
    public static class DailyBonus
    {
        const string PrefLast = "flockfive.daily.last";
        const string PrefStreak = "flockfive.daily.streak";
        const string PrefPay = "flockfive.daily.pay";
        const int StreakCap = 100000;
        public const int CycleDays = 5;
        public const int BaseCap = 1000;
        public const int WedgeCount = 8;
        public const float WedgeArc = 45f;

        // Around the wheel from the top, clockwise. Two low wedges, then the climb.
        public static readonly int[] WedgeMul = { 1, 1, 2, 2, 3, 4, 5, 10 };

        // Stated weights. Sum 100. 1x is 56, 2x is 28, 3x is 8, 4x is 4, 5x is 3, 10x is 1.
        public static readonly int[] WedgeWeight = { 30, 26, 16, 12, 8, 4, 3, 1 };

        static bool _booted;
        static int _streak;
        static int _last;
        static int _seen;
        static bool _continuing;
        static int _offerStreak = 1;
        static int _cycle;
        static int _payout = 100;
        static bool _atRisk;
        static int _lineN = -1;
        static int _payN = -1;
        static string _digits = "1";
        static string _streakLine = "1 day streak";
        static string _payLine = null;
        static int _testToday;
        static int _spinWedge = -1;
        static int _spinCoins;
        static bool _landed;
        static bool _resolved;
        static bool _tutorialSpin;
        static bool _pose;

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
            _streak = 0;
            _last = 0;
            _seen = 0;
            _continuing = false;
            _offerStreak = 1;
            _cycle = 0;
            _payout = 100;
            _atRisk = false;
            _lineN = -1;
            _payN = -1;
            _digits = "1";
            _streakLine = "1 day streak";
            _payLine = PayFallback();
            _gate = Gate.Ready;
            _testToday = 0;
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
            _pose = false;
        }

        public static bool OfferReady => _booted && _gate == Gate.Ready;
        public static int Streak => _streak;
        public static int OfferStreak => _offerStreak;
        public static int Cycle => _cycle;
        public static int Payout => _payout;
        public static int Bonus => 0;
        public static bool AtRisk => _atRisk;
        public static bool ClaimedToday => _booted && _gate == Gate.Claimed;
        public static int SpinWedge => _spinWedge;
        public static int SpinCoins => _spinCoins;
        public static bool Landed => _landed;
        public static bool Resolved => _resolved;
        public static bool TutorialSpin => _tutorialSpin;

        // Next level index is how many levels are already cleared.
        public static int LevelsCompleted
        {
            get
            {
                int n = LevelData.NextPlay;
                return n < 0 ? 0 : n;
            }
        }

        // 1-based level the player is on. Clearing level 1 stores NextPlay 1,
        // so the level on screen is levels cleared + 1.
        public static int HighestLevel
        {
            get
            {
                int n = LevelsCompleted + 1;
                return n < 1 ? 1 : n;
            }
        }

        public static int WeightSum
        {
            get
            {
                int n = 0;
                int len = WedgeWeight.Length < WedgeCount ? WedgeWeight.Length : WedgeCount;
                for (int i = 0; i < len; i++) n += WedgeWeight[i];
                return n;
            }
        }

        // 100, then +100 per 10 of the 1-based level, never above BaseCap.
        public static int BaseFor(int highestLevel)
        {
            if (highestLevel < 1) highestLevel = 1;
            int b = 100 + 100 * (highestLevel / 10);
            if (b > BaseCap) b = BaseCap;
            if (b < 100) b = 100;
            return b;
        }

        public static int Prize(int highestLevel, int wedge)
        {
            int mul = 1;
            if ((uint)wedge < (uint)WedgeCount && wedge < WedgeMul.Length)
                mul = WedgeMul[wedge];
            if (mul < 1) mul = 1;
            int p = mul * BaseFor(highestLevel);
            if (p < 100) p = 100;
            return p;
        }

        // roll is 0 .. WeightSum-1. The only mapping from a roll to a wedge.
        public static int WedgeForRoll(int roll)
        {
            int sum = WeightSum;
            if (sum < 1) return 0;
            if (roll < 0) roll = 0;
            int acc = 0;
            int len = WedgeWeight.Length < WedgeCount ? WedgeWeight.Length : WedgeCount;
            for (int i = 0; i < len; i++)
            {
                acc += WedgeWeight[i];
                if (roll < acc) return i;
            }
            return len > 0 ? len - 1 : 0;
        }

        public static int RollWedge()
        {
            int sum = WeightSum;
            if (sum < 1) return 0;
            return WedgeForRoll(UnityEngine.Random.Range(0, sum));
        }

        // From the wheel center to the flapper tip, in screen space (+x right, +y down).
        // The tip sits on the top rim and points down into the wedge, so the
        // winning wedge's center lies straight up the screen, along this vector.
        public static Vector2 PointerTipDir => new Vector2(0f, -1f);

        // Clockwise degrees that put wedge i's center under the pointer tip.
        // Positive rotation turns the wheel clockwise, so the wedge that was
        // counter-clockwise of the pointer arrives at the top.
        public static float PointerAngle(int wedge)
        {
            if (WedgeCount < 1) return 0f;
            int w = wedge % WedgeCount;
            if (w < 0) w += WedgeCount;
            if (w == 0) return 0f;
            return (WedgeCount - w) * WedgeArc;
        }

        public static int WedgeUnderPointer(float clockwiseDegrees)
        {
            if (WedgeCount < 1) return 0;
            float a = clockwiseDegrees % 360f;
            if (a < 0f) a += 360f;
            int steps = Mathf.FloorToInt((a + WedgeArc * 0.5f) / WedgeArc);
            int wrapped = steps % WedgeCount;
            if (wrapped < 0) wrapped += WedgeCount;
            int w = (WedgeCount - wrapped) % WedgeCount;
            return w < 0 ? 0 : w;
        }

        // Screen offset from the wheel center. +x right, +y down. Wedge centers
        // sit on this after the wheel has turned clockwise by clockwiseDegrees.
        public static Vector2 WedgeOffset(int wedge, float clockwiseDegrees, float radius)
        {
            float deg = wedge * WedgeArc + clockwiseDegrees;
            float rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad)) * radius;
        }

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

        public static bool FlameLit => IconState == DailyBonusIconState.Flame;
        public static string StreakDigits => _digits;
        public static string StreakLine => _streakLine;
        public static string PayLine => _payLine ?? PayFallback();
        public static string BonusLine => "";

        static string PayFallback() => Money.Format(100);

        public static void Boot()
        {
            if (_booted) return;
            _booted = true;
            _streak = PrefGuard.GetInt(PrefStreak, 0);
            if (_streak < 0) _streak = 0;
            if (_streak > StreakCap) _streak = StreakCap;
            _last = ReadStamp(PrefGuard.GetString(PrefLast, ""));
            _seen = TodayStamp();
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
            Rebuild();
        }

        public static bool RefreshDay()
        {
            if (!_booted) Boot();
            int today = TodayStamp();
            if (today == _seen) return false;
            _seen = today;
            if (!_pose)
            {
                _spinWedge = -1;
                _spinCoins = 0;
                _landed = false;
                _resolved = false;
                _tutorialSpin = false;
            }
            Rebuild();
            return true;
        }

        // Locks today and picks the wedge now. Does not pay. The wheel then
        // animates to this wedge. A second call the same day fails.
        public static bool TryBeginSpin(bool tutorial, out int wedge, out int coins, out bool firstEver)
        {
            wedge = 0;
            coins = 0;
            firstEver = false;
            if (_pose) return false;
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
            if (_last >= today) return false;
            wedge = RollWedge();
            coins = Prize(HighestLevel, wedge);
            if (coins < 100 || (coins % 100) != 0) return false;
            firstEver = _last <= 0;
            _spinWedge = wedge;
            _spinCoins = coins;
            _landed = false;
            _resolved = false;
            _tutorialSpin = tutorial;
            _payout = coins;
            _streak = _offerStreak;
            _last = today;
            PrefGuard.SetInt(PrefStreak, _streak);
            PrefGuard.SetString(PrefLast, WriteStamp(_last));
            PrefGuard.SetInt(PrefPay, coins);
            PlayerPrefs.Save();
            Rebuild();
            _payout = coins;
            CacheLines(_streak < 1 ? 1 : _streak);
            return true;
        }

        public static void MarkLanded()
        {
            if (_spinCoins < 100) return;
            _landed = true;
        }

        // After today's spin has landed, once, when an ad is ready and this spin
        // is not the tutorial. The day key is the claim itself.
        public static bool ShowDouble(bool adReady, bool tutorial)
        {
            if (tutorial || _tutorialSpin || !adReady) return false;
            if (!_booted || !_landed || _resolved) return false;
            if (_gate != Gate.Claimed) return false;
            return _spinCoins >= 100 && (_spinCoins % 100) == 0;
        }

        // One payout for the spin. doubled pays 2× only while ShowDouble would
        // be allowed for a finished ad. Otherwise pays 1×. A second call fails.
        public static bool TryResolve(bool doubled, out int coins)
        {
            coins = 0;
            if (_pose) return false;
            if (_resolved || _spinCoins < 100) return false;
            if (!_booted) Boot();
            if (!_landed) _landed = true;
            if (_gate != Gate.Claimed) return false;
            if (doubled && !ShowDouble(true, false)) doubled = false;
            coins = doubled ? _spinCoins * 2 : _spinCoins;
            if (coins < 100 || (coins % 100) != 0)
            {
                coins = 0;
                return false;
            }
            _resolved = true;
            _payout = coins;
            PrefGuard.SetInt(PrefPay, coins);
            PlayerPrefs.Save();
            Purse.Credit(coins);
            CacheLines(_streak < 1 ? 1 : _streak);
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

            int days = CycleDays > 0 ? CycleDays : 1;
            _cycle = (_offerStreak - 1) % days;
            if (_cycle < 0) _cycle = 0;
            bool alive = _gate == Gate.Claimed || (_gate == Gate.Ready && _continuing);
            _atRisk = alive && _streak >= 2;
            if (_spinCoins >= 100 && _gate == Gate.Claimed)
                _payout = _resolved ? _payout : _spinCoins;
            else if (_gate == Gate.Claimed)
            {
                int saved = PrefGuard.GetInt(PrefPay, 0);
                _payout = saved >= 100 ? saved : BaseFor(HighestLevel);
            }
            else
                _payout = BaseFor(HighestLevel);
            CacheLines(_gate == Gate.Ready ? _offerStreak : (_streak < 1 ? _offerStreak : _streak));
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
        }

        static int TodayStamp()
        {
            if (_testToday > 0) return _testToday;
            var n = DateTime.Now;
            return n.Year * 10000 + n.Month * 100 + n.Day;
        }

        public static int TestToday
        {
            get => _testToday;
            set => _testToday = value;
        }

        public static void TestBegin(int today, int lastStamp, int streak)
        {
            _testToday = today;
            _pose = false;
            _booted = false;
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
            if (lastStamp > 0)
                PrefGuard.SetString(PrefLast, WriteStamp(lastStamp));
            else
            {
                PlayerPrefs.DeleteKey(PrefLast);
                PlayerPrefs.DeleteKey(PrefLast + ".x");
            }
            if (streak < 0) streak = 0;
            if (streak > StreakCap) streak = StreakCap;
            PrefGuard.SetInt(PrefStreak, streak);
            PlayerPrefs.DeleteKey(PrefPay);
            PlayerPrefs.DeleteKey(PrefPay + ".x");
            PlayerPrefs.DeleteKey("flockfive.daily.reclaim");
            PlayerPrefs.DeleteKey("flockfive.daily.reclaim.x");
            PlayerPrefs.Save();
            Boot();
        }

        public static void TestRelease()
        {
            _testToday = 0;
            _pose = false;
            _booted = false;
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
        }

#if UNITY_EDITOR
        // Stills only. Does not write the daily prefs. mode 1 is mid-spin, 2 is the landed result.
        public static void PoseWheelShot(int mode, int wedge)
        {
            if (wedge < 0) wedge = 0;
            if (wedge >= WedgeCount) wedge = WedgeCount - 1;
            _pose = true;
            _testToday = 20261010;
            _booted = true;
            _tutorialSpin = false;
            _resolved = false;
            _spinWedge = wedge;
            _spinCoins = Prize(HighestLevel, wedge);
            _payout = _spinCoins;
            _payN = -1;
            _streak = 3;
            _offerStreak = 3;
            _last = _testToday;
            _seen = _testToday;
            _continuing = false;
            _cycle = 2;
            _atRisk = true;
            _gate = Gate.Claimed;
            _landed = mode == 2;
            CacheLines(3);
        }

        public static void ClearWheelShot()
        {
            _pose = false;
            _testToday = 0;
            _booted = false;
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
            _gate = Gate.Ready;
        }
#endif

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

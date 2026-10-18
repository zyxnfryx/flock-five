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

    // Daily login coins. A 24-hour window from the last claim (b129, Brandon 2026-10-18),
    // not the local calendar day and not the stage-clear streak in Purse.
    //   Claimed: now is under 24h after the last claim. The card shows "Ready in H:MM:SS".
    //   Ready:   now >= lastClaim + 24h. A claim 24h..48h after the last one (inclusive)
    //            continues the streak (+1). Later than 48h offers day 1 again. This is the old
    //            gap 1 vs gap 2+ rule with the window sliding with the claim.
    //   Held:    now < lastClaim (the clock was set backwards). Nothing can be claimed.
    // The last claim is saved as unix seconds (UTC) in flockfive.daily.last24 through PrefGuard
    // (same checksum and tamper fallback as every other pref here), so time zone and DST
    // changes never move the window. The old yyyymmdd pref (flockfive.daily.last) is migrated
    // once, see MigratedClaim, and removed. The saved streak still counts past 5. The coin
    // amount does not use that streak. Forward clock changes cannot be detected without a
    // server clock; the window trusts the device clock exactly as the calendar rule did.
    //
    // The prize is one spin of an 8-wedge wheel. The base is
    // 100 + 100 * floor(highestLevel / 10), capped at BaseCap. Highest level is
    // the 1-based level the player is on (levels cleared + 1). Every wedge pays
    // multiplier × base, so every prize is a multiple of 100 and at least 100.
    // Weights below are the only odds. The spin is chosen with UnityEngine.Random
    // before the wheel moves, then the wheel lands on that wedge.
    // After it lands, a rewarded ad may double that spin once. A prize that is
    // not a multiple of 100 still offers the double; the doubled result rounds
    // up to a multiple of 100. A skip or a failed show pays the spin once.
    // The once-per-window gate is the same timestamp as the claim (flockfive.daily.last24).
    // The tutorial spin does not offer the double.
    public static class DailyBonus
    {
        // Legacy yyyymmdd local day (read once for migration, then deleted).
        const string PrefLast = "flockfive.daily.last";
        // Unix seconds (UTC) of the last claim.
        const string PrefLast24 = "flockfive.daily.last24";
        public const long WindowSeconds = 24L * 3600L;
        // Latest claim gap (from the last claim) that still continues the streak.
        public const long GraceSeconds = 48L * 3600L;
        const string PrefStreak = "flockfive.daily.streak";
        const string PrefPay = "flockfive.daily.pay";
        const int StreakCap = 100000;
        public const int CycleDays = 5;
        public const int BaseCap = 1000;
        public const int WedgeCount = 8;
        public const float WedgeArc = 45f;
        // Last wedge, clockwise from the top. 10× the base ($1,000 at base 100).
        public const int JackpotWedge = 7;

        // Degrees off the jackpot centerline. The radial amount owns the center;
        // the star sits beside it, still inside the ruby wedge.
        public const float JackpotStarLean = 16f;
        public const float PointerFlickSeconds = 0.16f;
        public const float PointerFlickPeak = 14f;

        // Around the wheel from the top, clockwise. Two low wedges, then the climb.
        public static readonly int[] WedgeMul = { 1, 1, 2, 2, 3, 4, 5, 10 };

        // Stated weights. Sum 100. 1x is 56, 2x is 28, 3x is 8, 4x is 4, 5x is 3, 10x is 1.
        public static readonly int[] WedgeWeight = { 30, 26, 16, 12, 8, 4, 3, 1 };

        static bool _booted;
        static int _streak;
        static long _last;
        static long _seen;
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
        static long _testNow;
        static readonly char[] _cdBuf = new char[24];
        static int _cdLineKey = -1;
        static int _cdClockKey = -1;
        static string _cdLine = "";
        static string _cdClock = "";
        static int _spinWedge = -1;
        static int _spinCoins;
        static bool _landed;
        static bool _resolved;
        static bool _tutorialSpin;
        static bool _pose;
        static int _oddCoins;

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
            _testNow = 0;
            _cdLineKey = -1;
            _cdClockKey = -1;
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
            _pose = false;
            _oddCoins = 0;
        }

        public static bool OfferReady => _booted && _gate == Gate.Ready;
        public static int Streak => _streak;
        public static int OfferStreak => _offerStreak;
        public static int Cycle => _cycle;
        public static int Payout => _payout;
        public static int Bonus => 0;
        public static bool AtRisk => _atRisk;
        // True while the 24h window from the last claim is open (name kept from the calendar rule).
        public static bool ClaimedToday => _booted && _gate == Gate.Claimed;
        public static bool Held => _booted && _gate == Gate.Held;
        // Unix seconds (UTC) of the last claim, 0 when none. DailyReminder plans from this.
        public static long LastClaimUnix => _booted ? _last : 0L;
        // Whole seconds until Ready while Claimed (as of the last RefreshDay), else 0.
        public static int RemainingSeconds
        {
            get
            {
                if (!_booted || _gate != Gate.Claimed || _last <= 0) return 0;
                long r = _last + WindowSeconds - _seen;
                if (r < 0) return 0;
                return r > WindowSeconds ? (int)WindowSeconds : (int)r;
            }
        }
        public static bool CountdownActive => RemainingSeconds > 0;
        // "Ready in H:MM:SS". Rebuilt only when the second changes; same string object otherwise.
        public static string CountdownLine
        {
            get
            {
                int r = RemainingSeconds;
                if (r == _cdLineKey) return _cdLine;
                _cdLineKey = r;
                const string pre = "Ready in ";
                for (int i = 0; i < pre.Length; i++) _cdBuf[i] = pre[i];
                int n = WriteClock(_cdBuf, pre.Length, r);
                _cdLine = new string(_cdBuf, 0, n);
                return _cdLine;
            }
        }
        // "H:MM:SS" alone, for the small splash medal.
        public static string CountdownClock
        {
            get
            {
                int r = RemainingSeconds;
                if (r == _cdClockKey) return _cdClock;
                _cdClockKey = r;
                int n = WriteClock(_cdBuf, 0, r);
                _cdClock = new string(_cdBuf, 0, n);
                return _cdClock;
            }
        }
        // Shared clock text. 3725 is "1:02:05", 86400 is "24:00:00". Negative is "0:00:00".
        public static string FormatClock(long seconds)
        {
            int n = WriteClock(_cdBuf, 0, seconds);
            return new string(_cdBuf, 0, n);
        }
        static int WriteClock(char[] b, int at, long sec)
        {
            if (sec < 0) sec = 0;
            long h = sec / 3600;
            if (h > 99) h = 99;
            int m = (int)(sec / 60 % 60);
            int s = (int)(sec % 60);
            if (h >= 10) b[at++] = (char)('0' + (int)(h / 10));
            b[at++] = (char)('0' + (int)(h % 10));
            b[at++] = ':';
            b[at++] = (char)('0' + m / 10);
            b[at++] = (char)('0' + m % 10);
            b[at++] = ':';
            b[at++] = (char)('0' + s / 10);
            b[at++] = (char)('0' + s % 10);
            return at;
        }
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
#if UNITY_EDITOR
            if (_pose && _oddCoins > 0 && wedge == _spinWedge) return _oddCoins;
#endif
            return p;
        }

        // Doubled wheel coins. 260 becomes 300. A multiple of 100 stays put.
        public static int RoundUp100(int amount)
        {
            if (amount <= 0) return 0;
            int rem = amount % 100;
            if (rem == 0) return amount;
            return amount + (100 - rem);
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

        // Boundaries lie halfway between wedge centers (22.5°, 67.5°, …).
        // Counts each line the spin crosses from `fromDeg` to `toDeg`, including
        // a boundary the spin lands on and excluding one it starts on.
        // The total for a spin that begins and ends on wedge centers equals
        // |delta| / WedgeArc.
        public static int BoundaryCrossings(float fromDeg, float toDeg)
        {
            return BoundaryCrossings(fromDeg, toDeg, WedgeArc * 0.5f, WedgeArc);
        }

        public static int BoundaryCrossings(float fromDeg, float toDeg, float origin, float step)
        {
            if (step <= 0.0001f) return 0;
            float ka = (fromDeg - origin) / step;
            float kb = (toDeg - origin) / step;
            const float eps = 0.0001f;
            int n;
            if (kb >= ka)
            {
                int k0 = Mathf.FloorToInt(ka + eps) + 1;
                int k1 = Mathf.FloorToInt(kb + eps);
                n = k1 - k0 + 1;
            }
            else
            {
                int hi = Mathf.FloorToInt(ka - eps);
                int lo = Mathf.CeilToInt(kb - eps);
                n = hi - lo + 1;
            }
            return n > 0 ? n : 0;
        }

        // Degrees for GUIUtility.RotateAroundPivot. Positive is clockwise on
        // screen. Hub-to-rim (local +x along the wedge center, toward the rim),
        // then flipped 180° when that would read upside down. The result stays
        // in (-90, 90] of upright at the wedge's resting angle.
        public static float WedgeLabelDegrees(int wedge, float clockwiseDegrees)
        {
            if (WedgeCount < 1) return 0f;
            int w = wedge % WedgeCount;
            if (w < 0) w += WedgeCount;
            float deg = w * WedgeArc + clockwiseDegrees;
            float theta = SignedDegrees(deg - 90f);
            if (theta <= -90f || theta > 90f)
                theta = SignedDegrees(theta + 180f);
            if (theta <= -90f) theta += 180f;
            if (theta > 90f) theta -= 180f;
            return theta;
        }

        // Signed degrees in (-180, 180].
        public static float SignedDegrees(float deg)
        {
            deg %= 360f;
            if (deg > 180f) deg -= 360f;
            if (deg <= -180f) deg += 360f;
            return deg;
        }

        // Clockwise rotation in GUI space (+x right, +y down). Same sense as
        // RotateAroundPivot: (1, 0) at +90° lands on (0, 1), down the screen.
        public static Vector2 RotateClockwise(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // Tip flicks against the spin, then springs back to rest. againstSign
        // is -1 while the wheel turns clockwise (angle increasing).
        public static float PointerFlick(float age, float againstSign)
        {
            if (age < 0f || age >= PointerFlickSeconds) return 0f;
            float u = age / PointerFlickSeconds;
            float env = Mathf.Sin(u * Mathf.PI) * (1f - 0.35f * u);
            float sign = againstSign < 0f ? -1f : 1f;
            return sign * PointerFlickPeak * env;
        }

        // Ruby jackpot, warm gold and brown for the rest. dist is 0 at the hub.
        public static Color WedgeFace(int wedge, float dist)
        {
            if (dist < 0f) dist = 0f;
            if (wedge == JackpotWedge)
            {
                var ruby = new Color(0.74f, 0.07f, 0.13f, 1f);
                var deep = new Color(0.40f, 0.02f, 0.06f, 1f);
                return Color.Lerp(ruby, deep, dist * 0.28f);
            }
            bool gold = (wedge & 1) == 1;
            Color face = gold
                ? new Color(0.80f, 0.58f, 0.20f, 1f)
                : new Color(0.48f, 0.27f, 0.11f, 1f);
            return Color.Lerp(face, new Color(0.24f, 0.12f, 0.04f, 1f), dist * 0.16f);
        }

        public static readonly Color JackpotTrim = new Color(1f, 0.86f, 0.42f, 1f);
        public static readonly Color JackpotStar = new Color(1f, 0.95f, 0.62f, 1f);

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
            long now = NowSec();
            _last = ReadClaim(PrefGuard.GetString(PrefLast24, ""));
            if (_last <= 0 && PlayerPrefs.HasKey(PrefLast))
            {
                int old = ReadStamp(PrefGuard.GetString(PrefLast, ""));
                if (old > 0)
                {
                    _last = MigratedClaim(old, now);
                    PrefGuard.SetString(PrefLast24, WriteClaim(_last));
                }
            }
            if (PlayerPrefs.HasKey(PrefLast))
            {
                PlayerPrefs.DeleteKey(PrefLast);
                PlayerPrefs.DeleteKey(PrefLast + ".x");
                PlayerPrefs.Save();
            }
            _seen = now;
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
            Rebuild();
        }

        // Re-reads the clock. Cheap: it does nothing until the wall-clock second changes. True
        // only when the Daily STATE changed (Claimed to Ready, the grace lapsing so the offer
        // drops to day 1, Held to anything, or back); the per-second countdown alone returns
        // false and is read from RemainingSeconds. A spin that was started but never resolved
        // keeps the gate Claimed until TryResolve pays it (a claim is never lost to the flip).
        public static bool RefreshDay()
        {
            if (!_booted) Boot();
            long now = NowSec();
            if (now == _seen) return false;
            _seen = now;
            Eval(now, out var gate, out bool cont, out int offer);
            if (gate == _gate && cont == _continuing && offer == _offerStreak) return false;
            if (!_pose && _gate == Gate.Claimed && gate != Gate.Claimed && _spinCoins >= 100 && !_resolved)
                return false;
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

        // Locks the window and picks the wedge now. Does not pay. The wheel then
        // animates to this wedge. A second call inside the window fails.
        public static bool TryBeginSpin(bool tutorial, out int wedge, out int coins, out bool firstEver)
        {
            wedge = 0;
            coins = 0;
            firstEver = false;
            if (_pose) return false;
            if (!_booted) Boot();
            RefreshDay();
            if (_gate != Gate.Ready) return false;
            long now = NowSec();
            if (_last > 0 && now < _last + WindowSeconds) return false;
            wedge = RollWedge();
            coins = Prize(HighestLevel, wedge);
            if (coins <= 0) return false;
            firstEver = _last <= 0;
            _spinWedge = wedge;
            _spinCoins = coins;
            _landed = false;
            _resolved = false;
            _tutorialSpin = tutorial;
            _payout = coins;
            _streak = _offerStreak;
            _last = now;
            _seen = now;
            PrefGuard.SetInt(PrefStreak, _streak);
            PrefGuard.SetString(PrefLast24, WriteClaim(_last));
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
        // is not the tutorial. The day key is the claim itself. A spin that is
        // not a multiple of 100 still offers; TryResolve rounds the double up.
        public static bool ShowDouble(bool adReady, bool tutorial)
        {
            if (tutorial || _tutorialSpin || !adReady) return false;
            if (!_booted || !_landed || _resolved) return false;
            if (_gate != Gate.Claimed) return false;
            return _spinCoins > 0;
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
            coins = doubled ? RoundUp100(_spinCoins * 2) : _spinCoins;
            if (coins <= 0)
            {
                coins = 0;
                return false;
            }
            if (doubled && (coins < 100 || (coins % 100) != 0))
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

        // The one rule table. now and _last are unix seconds.
        static void Eval(long now, out Gate gate, out bool cont, out int offer)
        {
            cont = false;
            int kept = _streak < 1 ? 1 : _streak;
            if (_last <= 0)
            {
                gate = Gate.Ready;
                offer = 1;
                return;
            }
            if (now < _last)
            {
                gate = Gate.Held;
                offer = kept;
                return;
            }
            long elapsed = now - _last;
            if (elapsed < WindowSeconds)
            {
                gate = Gate.Claimed;
                offer = kept;
                return;
            }
            gate = Gate.Ready;
            if (elapsed <= GraceSeconds)
            {
                cont = true;
                offer = _streak + 1;
                if (offer < 1) offer = 1;
                if (offer > StreakCap) offer = StreakCap;
            }
            else
                offer = 1;
        }

        static void Rebuild()
        {
            Eval(_seen, out _gate, out _continuing, out _offerStreak);
            _cdLineKey = -1;
            _cdClockKey = -1;

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

        static long NowSec()
        {
            if (_testNow > 0) return _testNow;
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // Doubled spin from a rewarded ad, including one that was saved while the
        // app was paused or killed. A 1x resolve is topped up. Already at 2x is a
        // no-op. False only when today's claim cannot take the pay yet.
        public static bool PayStoredDouble(int baseCoins)
        {
            if (baseCoins <= 0) return false;
            int doubled = RoundUp100(baseCoins * 2);
            if (doubled < 100 || (doubled % 100) != 0) return false;
            if (!_booted) Boot();
            if (_resolved && _payout >= doubled) return true;
            if (_resolved && _payout > 0 && _payout < doubled)
            {
                Purse.Credit(doubled - _payout);
                _payout = doubled;
                _payN = -1;
                PrefGuard.SetInt(PrefPay, _payout);
                PlayerPrefs.Save();
                CacheLines(_streak < 1 ? 1 : _streak);
                return true;
            }
            if (_resolved) return true;
            _spinCoins = baseCoins;
            _landed = true;
            _tutorialSpin = false;
            if (_gate != Gate.Claimed) return false;
            return TryResolve(true, out _);
        }

        // Drop the in-memory spin so the next Boot reads the saved day again.
        public static void TestDropMemory()
        {
            _booted = false;
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
        }

        // A landed spin whose coins are not a multiple of 100. The x2 offer
        // still shows, and the doubled pay rounds up.
        public static void TestLand(int coins, bool tutorial)
        {
            if (!_booted) Boot();
            _pose = false;
            _oddCoins = 0;
            _spinCoins = coins;
            _payout = coins;
            _payN = -1;
            _landed = true;
            _resolved = false;
            _tutorialSpin = tutorial;
            _gate = Gate.Claimed;
        }

        // yyyymmdd view of the test clock (local noon of that day). 0 returns to the real clock.
        public static int TestToday
        {
            get => 0;
            set => _testNow = value > 0 ? NoonOf(value) : 0L;
        }

        // Unix seconds (UTC) of the test clock. 0 is the real clock.
        public static long TestNow
        {
            get => _testNow;
            set => _testNow = value > 0 ? value : 0L;
        }

        // Day-stamp form kept for the older suites: the clock is local noon of `today` and a
        // nonzero lastStamp is a claim at local noon of that day.
        public static void TestBegin(int today, int lastStamp, int streak)
        {
            TestBeginAt(NoonOf(today), lastStamp > 0 ? NoonOf(lastStamp) : 0L, streak);
        }

        // Clock at nowUnix, last claim at lastUnix (0 = never), saved streak.
        public static void TestBeginAt(long nowUnix, long lastUnix, int streak)
        {
            _testNow = nowUnix;
            _pose = false;
            _booted = false;
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
            PlayerPrefs.DeleteKey(PrefLast);
            PlayerPrefs.DeleteKey(PrefLast + ".x");
            if (lastUnix > 0)
                PrefGuard.SetString(PrefLast24, WriteClaim(lastUnix));
            else
            {
                PlayerPrefs.DeleteKey(PrefLast24);
                PlayerPrefs.DeleteKey(PrefLast24 + ".x");
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

        // Boots from whatever prefs are saved (including a legacy yyyymmdd pref) at nowUnix.
        public static void TestBootAt(long nowUnix)
        {
            _testNow = nowUnix;
            _pose = false;
            _booted = false;
            _spinWedge = -1;
            _spinCoins = 0;
            _landed = false;
            _resolved = false;
            _tutorialSpin = false;
            Boot();
        }

        // The saved last-claim text, for migration checks.
        public static string TestSavedClaim => PlayerPrefs.GetString(PrefLast24, "");

        public static void TestRelease()
        {
            _testNow = 0;
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
            _testNow = NoonOf(20261010);
            _booted = true;
            _tutorialSpin = false;
            _resolved = false;
            _oddCoins = 0;
            _spinWedge = wedge;
            _spinCoins = Prize(HighestLevel, wedge);
            _payout = _spinCoins;
            _payN = -1;
            _streak = 3;
            _offerStreak = 3;
            _last = _testNow - 3L * 3600L;
            _seen = _testNow;
            _continuing = false;
            _cycle = 2;
            _atRisk = true;
            _gate = Gate.Claimed;
            _landed = mode == 2;
            CacheLines(3);
        }

        public static void PoseOddCoins(int coins)
        {
            if (coins < 1) coins = 1;
            _oddCoins = coins;
            _spinCoins = coins;
            _payout = coins;
            _payN = -1;
            _landed = true;
            _resolved = false;
            _tutorialSpin = false;
            _gate = Gate.Claimed;
            CacheLines(_streak < 1 ? 1 : _streak);
        }

        public static void ClearWheelShot()
        {
            _pose = false;
            _oddCoins = 0;
            _testNow = 0;
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

        static long ReadClaim(string v)
        {
            if (string.IsNullOrEmpty(v) || v.Length > 12) return 0;
            if (!long.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out long n)) return 0;
            // 2000-01-01 .. 9999-12-31 in unix seconds. Anything else is a bad pref: no claim.
            if (n < 946684800L || n > 253402300799L) return 0;
            return n;
        }

        static string WriteClaim(long unix) => unix.ToString(CultureInfo.InvariantCulture);

        // Migration of the old yyyymmdd local-day claim. The old pref cannot say what time of
        // day it was claimed, so the claim is anchored at the END of that local day (the next
        // local midnight), capped at now so a claim made today is not dated in the future.
        //   Never early: the real claim was at or before that instant, so Ready (anchor + 24h)
        //   is never earlier than the 24h after the real claim. Cost: someone who claimed in
        //   the morning waits up to a day longer ONCE, on the day they update.
        //   Streak never lost: the continue window (anchor+24h .. anchor+48h) starts the local
        //   midnight after the next day and ends the midnight after that, so it covers the whole
        //   old "next calendar day" and part of the day after (more generous than the old rule).
        // A stamp dated after now (clock was moved back) becomes now: claimed now, streak kept.
        public static long MigratedClaim(int stamp, long nowSec)
        {
            int y = stamp / 10000;
            int m = (stamp / 100) % 100;
            int d = stamp % 100;
            long end;
            try
            {
                var next = new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Local).AddDays(1);
                end = new DateTimeOffset(next).ToUnixTimeSeconds();
            }
            catch (ArgumentOutOfRangeException)
            {
                return nowSec;
            }
            return end < nowSec ? end : nowSec;
        }

        // Local noon of a yyyymmdd day as unix seconds. Test support only.
        public static long NoonOf(int stamp)
        {
            var dt = new DateTime(stamp / 10000, (stamp / 100) % 100, stamp % 100, 12, 0, 0, DateTimeKind.Local);
            return new DateTimeOffset(dt).ToUnixTimeSeconds();
        }
    }
}

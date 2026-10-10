#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 80. Gear silhouette, settings insets, release ads, the prize wheel,
    // the home gear staying up, daily prizes in multiples of 100, and the
    // home airplane flyby (idle +5s, sky show about twice as long).
    static class Build80Tests
    {
        const float W = 1179f;
        const float H = 2556f;

        [MenuItem("Flock Five/Build 80 Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build80] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            CheckGear(Check);
            CheckPointer(Check);
            CheckBadge(Check);
            CheckPlane(Check);
            CheckSheet(Check);
            CheckMock(Check);
            CheckPrizes(Check);
            CheckGearVis(Check);
            var snap = PrefSnap.Take();
            try
            {
                Purse.Boot();
                CheckDouble(Check);
            }
            finally
            {
                DailyBonus.TestRelease();
                PrefSnap.Restore(snap);
                Purse.ReloadCoins();
                DailyBonus.Boot();
            }

            Debug.Log("[build80] " + (fail == 0 ? "BUILD80_OK " + pass : "BUILD80_FAIL " + fail + " passed " + pass));
            if (fail > 0)
                throw new System.Exception("BUILD80_FAIL " + fail + " passed " + pass);
        }

        static void CheckGear(System.Action<string, bool, string> Check)
        {
            var px = GearIcon.Pixels();
            int n = GearIcon.Resolution;
            int teeth = GearIcon.CountTeeth(px, n);
            float ring = GearIcon.RingRatio(px, n);
            float hole = GearIcon.HoleRatio(px, n);
            float tipDuty = GearIcon.TipDuty(px, n);
            float tooth = GearIcon.MinToothWidth(px, n);
            Check("gear-eight-teeth",
                GearIcon.Teeth == 8 && teeth == 8 && GearIcon.HoleClear(px, n) && GearIcon.FlatTops(px, n),
                "teeth=" + teeth + " hole=" + GearIcon.HoleClear(px, n) + " flat=" + GearIcon.FlatTops(px, n));
            // Solid ring about 0.75 of the tip, hole about 0.30. Tip duty is the
            // angular fill at the tooth tip (not the root), and the flat tooth
            // is at least 0.18 of the diameter so it cannot read as a ray.
            Check("gear-proportions",
                ring >= 0.66f && ring <= 0.78f
                && hole >= 0.22f && hole <= 0.34f
                && tipDuty >= 0.45f
                && tooth >= 0.18f,
                "ring=" + ring.ToString("0.00") + " hole=" + hole.ToString("0.00")
                + " tipDuty=" + tipDuty.ToString("0.00") + " tooth=" + tooth.ToString("0.00"));

            var safe = new Rect(0f, 102f, W, H - 102f - 177f);
            float s = H / 720f;
            float old = Mathf.Max(44f, FlockFiveApp.SettingsGearRef * s);
            var gear = FlockFiveApp.SettingsGearRect(W, H, safe);
            float ratio = old > 1f ? gear.width / old : 0f;
            float logoW = W * 0.72f * 0.91f;
            float logoLeft = (W - logoW) * 0.5f;
            Check("gear-size-and-hit",
                gear.width >= 44f && gear.height >= 44f
                && gear.width == gear.height
                && ratio >= 1.25f && ratio <= 1.35f
                && Mathf.Abs(ratio - FlockFiveApp.SettingsGearGrow) < 0.02f
                && gear.xMax < logoLeft - 4f
                && gear.y >= H - safe.yMax
                && gear.xMax < W * 0.5f,
                "gear=" + gear + " ratio=" + ratio.ToString("0.00") + " logoLeft=" + logoLeft.ToString("0.0"));
        }

        static void CheckPointer(System.Action<string, bool, string> Check)
        {
            var tip = DailyBonus.PointerTipDir;
            bool land = Mathf.Abs(tip.magnitude - 1f) < 0.001f && tip.y < -0.9f && Mathf.Abs(tip.x) < 0.05f;
            float worst = 0f;
            for (int w = 0; w < DailyBonus.WedgeCount; w++)
            {
                float ang = DailyBonus.PointerAngle(w);
                float spun = ang + 360f * 5f;
                if (DailyBonus.WedgeUnderPointer(ang) != w) land = false;
                if (DailyBonus.WedgeUnderPointer(spun) != w) land = false;
                var off = DailyBonus.WedgeOffset(w, spun, 1f);
                float along = off.x * tip.x + off.y * tip.y;
                float side = off.x * (-tip.y) + off.y * tip.x;
                float err = Mathf.Abs(side) + Mathf.Abs(1f - along);
                if (err > worst) worst = err;
                if (along < 0.985f || Mathf.Abs(side) > 0.02f) land = false;
            }
            var px = FlockFiveApp.WheelPointerPixels(out int pw, out int ph);
            bool down = FlockFiveApp.PointerPointsDown(px, pw, ph);
            Check("wheel-pointer-lands", land && down,
                "worst=" + worst.ToString("0.000") + " down=" + down + " px=" + pw + "x" + ph);
        }

        static void CheckBadge(System.Action<string, bool, string> Check)
        {
            string root = Application.dataPath;
            string daily = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.DailyBonus.cs"));
            string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
            string badge = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/StreakBadge.cs"));
            int stamp = app.IndexOf("void DrawRewardStamp(");
            int wax = app.IndexOf("static void DrawWaxSplat(");
            string stampBody = stamp >= 0 && wax > stamp ? app.Substring(stamp, wax - stamp) : "";
            int garden = app.IndexOf("void DrawGardenStamp(");
            int restart = app.IndexOf("void DrawRestartAsk(");
            string gardenBody = garden >= 0 && restart > garden ? app.Substring(garden, restart - garden) : "";
            int helper = daily.IndexOf("void DrawWheelX2(");
            int claim = daily.IndexOf("void DrawDailyClaim(");
            string helperBody = helper >= 0 && claim > helper ? daily.Substring(helper, claim - helper) : "";
            int uses = 0;
            int from = 0;
            while (from >= 0)
            {
                int i = daily.IndexOf("DrawWheelX2(", from);
                if (i < 0) break;
                uses++;
                from = i + 12;
            }
            var disc = StreakBadge.SharedDisc();
            bool sprite = disc != null && disc.name == StreakBadge.DiscName
                && badge.IndexOf("DiscName") >= 0
                && badge.IndexOf("SharedDisc") >= 0;
            bool shared = helperBody.IndexOf("StreakBadge.Draw") >= 0
                && helperBody.IndexOf("StreakBadge.Colors(2)") >= 0
                && stampBody.IndexOf("StreakBadge.Draw") >= 0
                && gardenBody.IndexOf("DrawRewardStamp") >= 0
                && uses >= 3
                && daily.IndexOf("BakeWheelBadge") < 0
                && daily.IndexOf("DailyX2") < 0;
            Check("wheel-x2-shared-badge", sprite && shared,
                "uses=" + uses + " sprite=" + (disc != null ? disc.name : "null")
                + " helper=" + (helperBody.Length > 0) + " stamp=" + (stampBody.IndexOf("StreakBadge.Draw") >= 0));
        }

        static void CheckPlane(System.Action<string, bool, string> Check)
        {
            const float oldIdle = 10f;
            const float oldShow = 2.70f + 0.50f + 0.18f + 0.60f + 0.85f;
            float show3 = FlockFiveApp.PlaneSkyShowEnd(3);
            float show6 = FlockFiveApp.PlaneSkyShowEnd(FlockFiveApp.PlaneSkyBursts);
            bool times = Mathf.Abs(FlockFiveApp.PlaneSkyWhen(0) - 0.95f) < 0.001f
                && Mathf.Abs(FlockFiveApp.PlaneSkyWhen(1) - 1.80f) < 0.001f
                && Mathf.Abs(FlockFiveApp.PlaneSkyWhen(2) - 2.70f) < 0.001f;
            bool shells = FlockFiveApp.PlaneSkyHeartPoints == 48
                && FlockFiveApp.PlaneSkyRoundPoints == 24
                && Mathf.Abs(FlockFiveApp.PlaneSkyTail(0) - (0.50f + 1.12f)) < 0.001f
                && Mathf.Abs(FlockFiveApp.PlaneSkyTail(2) - (0.50f + 0.18f + 0.60f + 0.85f)) < 0.001f
                && Mathf.Abs(FlockFiveApp.PlaneSkyTail(5) - FlockFiveApp.PlaneSkyTail(2)) < 0.001f;
            Check("plane-idle-plus-5",
                Mathf.Abs(FlockFiveApp.PlaneIdleDelay - (oldIdle + 5f)) < 0.001f
                && FlockFiveApp.PlaneCrossSeconds == 6.5f,
                "idle=" + FlockFiveApp.PlaneIdleDelay.ToString("0.00")
                + " was=" + oldIdle.ToString("0.00")
                + " cross=" + FlockFiveApp.PlaneCrossSeconds.ToString("0.00"));
            Check("plane-sky-longer",
                times && shells
                && FlockFiveApp.PlaneSkyBursts == 6
                && Mathf.Abs(show3 - oldShow) < 0.02f
                && Mathf.Abs(show6 - oldShow * 2f) < 0.05f
                && show6 > show3 * 1.9f,
                "bursts=" + FlockFiveApp.PlaneSkyBursts
                + " show3=" + show3.ToString("0.00")
                + " show6=" + show6.ToString("0.00")
                + " old=" + oldShow.ToString("0.00"));
            Check("other-flybys-unchanged",
                Mathf.Abs(HawkPass.Lead - 0.6f) < 0.001f
                && Mathf.Abs(HawkPass.Dur - 0.48f) < 0.001f
                && Mathf.Abs(HawkPass.Feather - 0.72f) < 0.001f
                && Mathf.Abs(HawkView.ArriveSeconds - 1.05f) < 0.001f
                && Mathf.Abs(SparrowView.ArriveSeconds - 0.95f) < 0.001f
                && Mathf.Abs(FlockFiveApp.AvatarCrossSeconds - 1.15f) < 0.001f,
                "hawk=" + HawkPass.Lead.ToString("0.00") + "/" + HawkPass.Dur.ToString("0.00")
                + "/" + HawkPass.Feather.ToString("0.00")
                + " arrive=" + HawkView.ArriveSeconds.ToString("0.00")
                + " sparrow=" + SparrowView.ArriveSeconds.ToString("0.00")
                + " avatar=" + FlockFiveApp.AvatarCrossSeconds.ToString("0.00"));
        }

        static void CheckSheet(System.Action<string, bool, string> Check)
        {
            var safe = new Rect(0f, 102f, W, H - 102f - 177f);
            var frame = SettingsSheet.Layout(W, H, safe, 220f);
            float left = frame.Content.x - frame.Card.x;
            float right = frame.Card.xMax - frame.Content.xMax;
            float need = frame.Card.width * 0.06f;
            var body = frame.Body;
            bool rows = body.PrivacyLink.yMax <= body.Label.y + 0.5f
                && body.Label.xMax <= body.Toggle.xMin + 0.5f
                && body.Toggle.xMax <= frame.Content.xMax + 0.5f
                && body.Label.xMin >= frame.Content.x - 0.5f
                && !body.PrivacyLink.Overlaps(body.Label)
                && !body.Label.Overlaps(body.Toggle);
            bool xClear = frame.Close.width >= 44f
                && frame.Close.yMax <= frame.Title.y + 0.5f
                && !frame.Close.Overlaps(frame.Title);
            Check("settings-padding",
                left >= need && right >= need && rows && xClear
                && frame.Content.y >= frame.Card.y + need * 0.5f
                && frame.Card.yMax - frame.Content.yMax >= need * 0.5f,
                "left=" + left.ToString("0.0") + " right=" + right.ToString("0.0")
                + " need=" + need.ToString("0.0")
                + " close=" + frame.Close + " title=" + frame.Title);
        }

        static void CheckMock(System.Action<string, bool, string> Check)
        {
            string ads = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/Ads.cs"));
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            string leakAds = DevOnlyLeaks(ads);
            string leakApp = DevOnlyLeaks(app);
            Check("release-no-mock",
                !Ads.MockRewarded && !Ads.TestSuite && leakAds == null && leakApp == null,
                "mock=" + Ads.MockRewarded + " ads=" + (leakAds ?? "clean") + " app=" + (leakApp ?? "clean"));
        }

        static string DevOnlyLeaks(string source)
        {
            string[] markers =
            {
                "Playing…",
                "DrawGiftMovie(",
                "Ads.Simulate(",
                "yield return Simulate("
            };
            var stack = new List<bool>();
            bool parent = false;
            bool inDev = false;
            var lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (t.StartsWith("#if"))
                {
                    stack.Add(parent);
                    parent = inDev;
                    inDev = inDev || t.IndexOf("FF_ADS_DEV") >= 0;
                    continue;
                }
                if (t.StartsWith("#else") && stack.Count > 0)
                {
                    inDev = parent;
                    continue;
                }
                if (t.StartsWith("#endif") && stack.Count > 0)
                {
                    inDev = parent;
                    parent = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }
                if (inDev) continue;
                for (int m = 0; m < markers.Length; m++)
                {
                    if (t.IndexOf(markers[m]) >= 0)
                        return "line " + (i + 1) + " " + markers[m];
                }
            }
            return null;
        }

        static void CheckPrizes(System.Action<string, bool, string> Check)
        {
            int[] mul = { 1, 1, 2, 2, 3, 4, 5, 10 };
            int[] wgt = { 30, 26, 16, 12, 8, 4, 3, 1 };
            bool table = DailyBonus.WedgeCount == 8
                && DailyBonus.WedgeMul.Length == 8
                && DailyBonus.WedgeWeight.Length == 8;
            int sum = 0;
            for (int i = 0; i < 8; i++)
            {
                sum += wgt[i];
                if (DailyBonus.WedgeMul[i] != mul[i] || DailyBonus.WedgeWeight[i] != wgt[i])
                    table = false;
            }
            bool bounds = table && DailyBonus.WeightSum == 100 && sum == 100;
            int acc = 0;
            for (int i = 0; i < 8 && bounds; i++)
            {
                if (DailyBonus.WedgeForRoll(acc) != i) bounds = false;
                if (DailyBonus.WedgeForRoll(acc + wgt[i] - 1) != i) bounds = false;
                acc += wgt[i];
            }
            if (bounds && DailyBonus.WedgeForRoll(99) != 7) bounds = false;

            string bad = null;
            int[] levels = { 1, 9, 10, 25, 50, 89, 90, 100, 200 };
            int[] expectB = { 100, 100, 200, 300, 600, 900, 1000, 1000, 1000 };
            for (int i = 0; i < levels.Length && bad == null; i++)
            {
                int got = DailyBonus.BaseFor(levels[i]);
                if (got != expectB[i]) bad = "base L" + levels[i] + "=" + got;
            }
            for (int level = 1; level <= 200 && bad == null; level++)
            {
                int raw = 100 + 100 * (level / 10);
                int cap = raw > DailyBonus.BaseCap ? DailyBonus.BaseCap : raw;
                int b = DailyBonus.BaseFor(level);
                if (b != cap || b < 100 || (b % 100) != 0)
                {
                    bad = "growth L" + level + "=" + b;
                    break;
                }
                for (int w = 0; w < 8; w++)
                {
                    int p = DailyBonus.Prize(level, w);
                    if (p < 100 || (p % 100) != 0 || p != mul[w] * b)
                    {
                        bad = "L" + level + " w" + w + "=" + p;
                        break;
                    }
                }
            }

            bool aim = true;
            for (int w = 0; w < 8; w++)
            {
                float ang = DailyBonus.PointerAngle(w);
                if (DailyBonus.WedgeUnderPointer(ang) != w) aim = false;
                if (DailyBonus.WedgeUnderPointer(ang + 360f * 5f) != w) aim = false;
                var off = DailyBonus.WedgeOffset(w, ang, 100f);
                if ((off - new Vector2(0f, -100f)).sqrMagnitude > 0.01f) aim = false;
            }

            var state = UnityEngine.Random.state;
            const int n = 20000;
            var hits = new int[8];
            bool dist = true;
            string distDetail = "";
            try
            {
                UnityEngine.Random.InitState(80080);
                for (int i = 0; i < n; i++)
                {
                    int w = DailyBonus.RollWedge();
                    if ((uint)w < 8u) hits[w]++;
                    else dist = false;
                }
                for (int i = 0; i < 8; i++)
                {
                    double p = wgt[i] / 100.0;
                    double sd = System.Math.Sqrt(n * p * (1.0 - p));
                    double allow = 4.0 * sd + 1.0;
                    if (System.Math.Abs(hits[i] - n * p) > allow) dist = false;
                    distDetail += i + ":" + hits[i] + " ";
                }
            }
            finally
            {
                UnityEngine.Random.state = state;
            }

            Check("daily-multiples-of-100",
                bad == null && table && bounds && aim && dist
                && DailyBonus.BaseFor(1) == 100
                && DailyBonus.Prize(1, 0) == 100
                && DailyBonus.Prize(1, 7) == 1000
                && DailyBonus.Prize(100, 7) == 10000
                && DailyBonus.BaseCap == 1000,
                (bad ?? "ok") + " hits " + distDetail);
        }

        static void CheckDouble(System.Action<string, bool, string> Check)
        {
            int level = DailyBonus.HighestLevel;

            DailyBonus.TestBegin(20261010, 0, 0);
            bool tutorSpin = DailyBonus.TryBeginSpin(true, out int tw, out int tcoins, out _);
            bool tutorEarly = DailyBonus.ShowDouble(true, false);
            DailyBonus.MarkLanded();
            bool tutorHidden = !DailyBonus.ShowDouble(true, false) && !DailyBonus.ShowDouble(true, true);
            int beforeT = Purse.Coins;
            bool tutorPay = DailyBonus.TryResolve(true, out int tutorGot);
            int tutorDelta = Purse.Coins - beforeT;
            Check("daily-tutorial-no-double",
                tutorSpin && !tutorEarly && tutorHidden && tutorPay
                && tutorGot == tcoins && tutorDelta == tcoins
                && tcoins == DailyBonus.Prize(level, tw)
                && tcoins >= 100 && (tcoins % 100) == 0,
                "spin=" + tcoins + " paid=" + tutorGot + " delta=" + tutorDelta + " wedge=" + tw);

            DailyBonus.TestBegin(20261010, 0, 0);
            bool spin = DailyBonus.TryBeginSpin(false, out int w, out int coins, out _);
            bool beforeLand = DailyBonus.ShowDouble(true, false);
            DailyBonus.MarkLanded();
            bool noAd = !DailyBonus.ShowDouble(false, false);
            bool ad = DailyBonus.ShowDouble(true, false);
            int before = Purse.Coins;
            bool doubled = DailyBonus.TryResolve(true, out int paid);
            int delta = Purse.Coins - before;
            bool second = DailyBonus.TryResolve(true, out int again);
            bool sameDay = DailyBonus.TryBeginSpin(false, out _, out _, out _);
            Check("daily-double-complete",
                spin && !beforeLand && noAd && ad && doubled
                && paid == coins * 2 && delta == paid
                && !second && again == 0 && !sameDay
                && coins == DailyBonus.Prize(level, w)
                && paid >= 200 && (paid % 100) == 0,
                "spin=" + coins + " paid=" + paid + " delta=" + delta + " wedge=" + w);

            DailyBonus.TestBegin(20261010, 0, 0);
            bool spin2 = DailyBonus.TryBeginSpin(false, out int w2, out int coins2, out _);
            DailyBonus.MarkLanded();
            int before2 = Purse.Coins;
            bool skip = DailyBonus.TryResolve(false, out int paid2);
            int delta2 = Purse.Coins - before2;
            bool after = DailyBonus.ShowDouble(true, false);
            bool same = DailyBonus.TryBeginSpin(false, out _, out _, out _);
            DailyBonus.TestToday = 20261011;
            bool rolled = DailyBonus.RefreshDay();
            bool next = DailyBonus.TryBeginSpin(false, out int w3, out int coins3, out _);
            Check("daily-double-once",
                spin2 && skip && paid2 == coins2 && delta2 == coins2
                && !after && !same && rolled && next
                && coins3 == DailyBonus.Prize(level, w3)
                && coins2 >= 100 && (coins2 % 100) == 0,
                "skip=" + paid2 + " next=" + coins3 + " wedge=" + w2 + "/" + w3);

            string root = Application.dataPath;
            string bonus = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/DailyBonus.cs"));
            string ui = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.DailyBonus.cs"));
            string ads = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/Ads.cs"));
            string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
            string all = bonus + "\n" + ui + "\n" + ads + "\n" + app;
            bool gone = all.IndexOf("ShowReclaim") < 0
                && all.IndexOf("TryGrantReclaim") < 0
                && all.IndexOf("Watch ad to claim again") < 0
                && all.IndexOf("PlacementDailyReclaim") < 0
                && all.IndexOf("ShotDailyReclaim") < 0
                && all.IndexOf("_dailyReclaimBusy") < 0
                && all.IndexOf("PoseReclaimShot") < 0;
            bool wired = ads.IndexOf("daily_double") >= 0
                && ui.IndexOf("Watch Ad: x2") >= 0
                && ui.IndexOf("ShowDouble") >= 0
                && ui.IndexOf("PlacementDailyDouble") >= 0
                && ui.IndexOf("Ads.LastEarned") >= 0
                && ui.IndexOf("BeginRewardPay") >= 0
                && ui.IndexOf("SfxLibrary.Play(\"tick\"") >= 0;
            Check("daily-reclaim-removed", gone && wired, "gone=" + gone + " wired=" + wired);
        }

        static void CheckGearVis(System.Action<string, bool, string> Check)
        {
            bool lessons = HomeGear.Lessons != null && HomeGear.Lessons.Length >= 8;
            if (lessons)
            {
                for (int i = 0; i < HomeGear.Lessons.Length; i++)
                {
                    var up = new HomeGearQuery { Home = true, Cover = HomeGear.Lessons[i], ForcedTap = false };
                    var forced = up;
                    forced.ForcedTap = true;
                    if (!HomeGear.Shown(up) || !HomeGear.AboveDimmer(up) || !HomeGear.AcceptsTap(up)
                        || !HomeGear.Shown(forced) || !HomeGear.AboveDimmer(forced) || HomeGear.AcceptsTap(forced))
                    {
                        lessons = false;
                        break;
                    }
                }
            }
            var garden = new HomeGearQuery { Home = false, Cover = HomeCover.KeepMultiplier, ForcedTap = false };
            string app = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.cs"));
            string settings = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scripts/App/FlockFiveApp.Settings.cs"));
            int splash = app.IndexOf("void DrawSplash()");
            int flower = app.IndexOf("bool DrawFlowerPlay(");
            string body = splash >= 0 && flower > splash ? app.Substring(splash, flower - splash) : "";
            int poll = body.IndexOf("PollHomeGear(");
            int streak = body.IndexOf("DrawStreakSignLayer(");
            int tutor = body.LastIndexOf("DrawTutorOverlay(");
            int daily = body.IndexOf("DrawDailyBonus(");
            int hive = body.IndexOf("DrawHiveIntro(");
            int paint = body.IndexOf("PaintHomeGear(");
            int sheet = body.IndexOf("DrawSettingsSheet(");
            bool order = poll >= 0 && streak > poll && daily > streak && hive > streak
                && tutor > hive && paint > tutor && sheet > paint
                && body.IndexOf("!modal && !tutorUp") < 0;
            bool wired = settings.IndexOf("HomeGear.Shown") >= 0
                && settings.IndexOf("HomeGear.AboveDimmer") >= 0
                && settings.IndexOf("HomeGear.AcceptsTap") >= 0
                && settings.IndexOf("HomeStepRail()") >= 0
                && settings.IndexOf("AdoptStep.Look") >= 0
                && settings.IndexOf("_dailyIntroLive && _dailyOpen") >= 0
                && settings.IndexOf("HomeCover.KeepMultiplier") >= 0
                && settings.IndexOf("HomeCover.Streak") >= 0;
            Check("gear-stays-up",
                lessons && !HomeGear.Shown(garden) && !HomeGear.AboveDimmer(garden) && order && wired,
                "lessons=" + lessons + " order=" + order + " wired=" + wired
                + " poll=" + poll + " paint=" + paint + " sheet=" + sheet);
        }

        struct PrefSnap
        {
            public string Key;
            public bool Had;
            public bool Text;
            public string S;
            public int I;

            public static PrefSnap[] Take()
            {
                string[] text =
                {
                    "flockfive.login.day",
                    "flockfive.daily.last",
                    "flockfive.daily.reclaim"
                };
                string[] ints =
                {
                    "flockfive.coins", "flockfive.coins.x",
                    "flockfive.streak", "flockfive.streak.x",
                    "flockfive.login.n", "flockfive.login.n.x",
                    "flockfive.login.day.x",
                    "flockfive.owed", "flockfive.owed.x",
                    "flockfive.instage",
                    "flockfive.daily.streak", "flockfive.daily.streak.x",
                    "flockfive.daily.pay", "flockfive.daily.pay.x",
                    "flockfive.daily.last.x", "flockfive.daily.reclaim.x"
                };
                var list = new List<PrefSnap>();
                for (int i = 0; i < text.Length; i++) list.Add(Read(text[i], true));
                for (int i = 0; i < ints.Length; i++) list.Add(Read(ints[i], false));
                return list.ToArray();
            }

            static PrefSnap Read(string key, bool text)
            {
                bool had = PlayerPrefs.HasKey(key);
                return new PrefSnap
                {
                    Key = key,
                    Had = had,
                    Text = text,
                    S = had && text ? PlayerPrefs.GetString(key, "") : "",
                    I = had && !text ? PlayerPrefs.GetInt(key, 0) : 0
                };
            }

            public static void Restore(PrefSnap[] slots)
            {
                if (slots == null) return;
                for (int i = 0; i < slots.Length; i++)
                {
                    var s = slots[i];
                    if (!s.Had) PlayerPrefs.DeleteKey(s.Key);
                    else if (s.Text) PlayerPrefs.SetString(s.Key, s.S);
                    else PlayerPrefs.SetInt(s.Key, s.I);
                }
                PlayerPrefs.Save();
            }
        }
    }
}
#endif

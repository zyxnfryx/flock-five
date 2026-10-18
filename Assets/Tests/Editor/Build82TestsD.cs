#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 82 pass b82d. Stage-clear 2x, new-player grace, 75s cooldown,
    // wheel x2 round-up, and the soft Go VIP ask. VIP silences interstitials only.
    static class Build82TestsD
    {
        const float W = 1179f;
        const float H = 2556f;

        [MenuItem("Flock Five/Build 82d Tests")]
        public static void Run()
        {
            int pass = 0;
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (ok) pass++;
                else fail++;
                Debug.Log("[build82d] " + (ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
            }

            var snap = PrefSnap.Take();
            double lastKeep = Ads.LastAnyAd;
            try
            {
                DrainSoft();
                Ads.TakeClearSkip();
                Ads.SetClockForTest(10000);
                Ads.SetSoftDayForTest(20261011);
                CheckClear(Check);
                CheckGrace(Check);
                CheckCooldown(Check);
                CheckWheel(Check);
                CheckVip(Check);
                CheckWiring(Check);
            }
            finally
            {
                DrainSoft();
                Ads.TakeClearSkip();
                Ads.ClearClockForTest();
                Ads.SetLastAnyForTest(lastKeep);
                Ads.SetSoftDayForTest(0);
                Ads.SetReadyOverride(-1);
                if (FlockFiveApp.VipOffer.SoftUp) FlockFiveApp.VipOffer.DismissSoft(false);
                FlockFiveApp.VipOffer.TakeFullRequest();
                DailyBonus.TestRelease();
                DailyBonus.ClearWheelShot();
                PrefSnap.Restore(snap);
                Purse.ReloadCoins();
                bool want = PlayerPrefs.GetInt("flockfive.clear.double", 0) == 1;
                if (want && !Purse.ClearDoubleArmed) Purse.ArmClearDouble();
                else if (!want && Purse.ClearDoubleArmed) Purse.ApplyClearDouble(1);
            }

            if (fail > 0) throw new System.Exception("BUILD82D_FAIL " + fail + " passed " + pass);
            Debug.Log("BUILD82D_OK " + pass);
        }

        static void DrainSoft()
        {
            Ads.DeliverInterstitialClosed();
            if (Ads.VipSoftPending) Ads.TryConsumeVipSoft();
        }

        static void CheckClear(System.Action<string, bool, string> Check)
        {
            bool offered = Ads.ClearRewardOffered(true, true);
            bool noAd = !Ads.ClearRewardOffered(true, false);
            bool off = !Ads.ClearRewardOffered(false, true);
            Check("clear-offer-when-ready", offered && noAd && off,
                "ready=" + offered + " noAd=" + noAd + " off=" + off);

            Ads.MarkClearRewardWatched();
            bool once = Ads.TakeClearSkip();
            bool twice = Ads.TakeClearSkip();
            bool skipped = !Ads.StageInterstitialDue(8, false, true, true, 10000, 0);
            bool open = Ads.StageInterstitialDue(8, false, true, false, 10000, 0);
            Check("clear-watch-skips-interstitial", once && !twice && skipped && open,
                "once=" + once + " twice=" + twice + " skipped=" + skipped + " open=" + open);

            if (Purse.ClearDoubleArmed) Purse.ApplyClearDouble(1);
            Purse.ArmClearDouble();
            int doubled = Purse.ApplyClearDouble(150);
            int plain = Purse.ApplyClearDouble(150);
            Check("clear-double-pays-once", doubled == 300 && plain == 150 && !Purse.ClearDoubleArmed,
                "doubled=" + doubled + " plain=" + plain + " armed=" + Purse.ClearDoubleArmed);

            var lay = FlockFiveApp.LayoutClearOffer(W, H);
            var stamp = FlockFiveApp.RewardStampRect(lay.Watch, 2);
            bool same = Near(lay.Badge, stamp);
            bool topRight = lay.Badge.center.x > lay.Watch.center.x && lay.Badge.center.y < lay.Watch.center.y;
            float safe = CaptionLayout.SafePlateWidth(W, H);
            bool seat = lay.Caption.width <= safe + 0.5f
                && lay.Caption.yMin >= H * 0.18f
                && lay.Skip.yMax < H * 0.90f
                && lay.Watch.yMin > lay.Caption.yMax
                && lay.Skip.yMin > lay.Watch.yMax;
            var gold = FlockFiveApp.GoldCaption(FlockFiveApp.ClearDoubleLine, W, H);
            bool copy = FlockFiveApp.ClearDoubleWatch == "Watch Ad: 2x coins"
                && !gold.Overflow && gold.Lines >= 1 && gold.Lines <= 2;
            Check("clear-badge-and-caption", same && topRight && seat && copy,
                "same=" + same + " topRight=" + topRight + " seat=" + seat
                + " lines=" + gold.Lines + " overflow=" + gold.Overflow
                + " capY=" + lay.Caption.yMin.ToString("0") + " badge=" + lay.Badge);
        }

        static void CheckGrace(System.Action<string, bool, string> Check)
        {
            bool early = true;
            string detail = "";
            for (int n = 1; n <= 5; n++)
            {
                bool due = Ads.StageInterstitialDue(n, false, true, false, 10000, 0);
                if (due) { early = false; detail = "clear " + n + " due"; }
            }
            bool hit2 = Ads.CadenceHit(2);
            bool hit5 = Ads.CadenceHit(5);
            bool hit6 = Ads.CadenceHit(6);
            bool due6 = Ads.StageInterstitialDue(6, false, true, false, 10000, 0);
            bool due8 = Ads.StageInterstitialDue(8, false, true, false, 10000, 0);
            bool vip8 = Ads.StageInterstitialDue(8, true, true, false, 10000, 0);
            bool grace = Ads.GraceBlocks(5) && !Ads.GraceBlocks(6) && Ads.InterstitialGraceClears == 6;
            Check("grace-before-clear-6",
                early && hit2 && hit5 && !hit6 && !due6 && due8 && !vip8 && grace,
                (detail.Length == 0 ? "clears 1-5 blocked" : detail)
                + " hit2=" + hit2 + " hit5=" + hit5 + " hit6=" + hit6
                + " due6=" + due6 + " due8=" + due8 + " vip8=" + vip8);
        }

        static void CheckCooldown(System.Action<string, bool, string> Check)
        {
            bool inside = Ads.Cooling(1000, 1000 - 74.9);
            bool edge = !Ads.Cooling(1000, 1000 - 75);
            bool none = !Ads.Cooling(1000, 0);
            bool sec = Ads.InterstitialCooldownSec == 75;
            Ads.SetClockForTest(5000);
            Ads.NoteAnyAd();
            bool stamped = System.Math.Abs(Ads.LastAnyAd - 5000) < 0.001;
            bool blocked = !Ads.StageInterstitialDue(8, false, true, false, 5000, Ads.LastAnyAd);
            bool just = !Ads.StageInterstitialDue(8, false, true, false, 5074.9, Ads.LastAnyAd);
            bool later = Ads.StageInterstitialDue(8, false, true, false, 5075, Ads.LastAnyAd);
            bool pokerGrace = !Ads.PokerInterstitialDue(5, 10, false, true, 10000, 0);
            bool pokerHands = !Ads.PokerInterstitialDue(8, 9, false, true, 10000, 0);
            bool pokerCool = !Ads.PokerInterstitialDue(8, 10, false, true, 1000, 950);
            bool pokerVip = !Ads.PokerInterstitialDue(8, 10, true, true, 10000, 0);
            bool pokerOff = !Ads.PokerInterstitialDue(8, 10, false, false, 10000, 0);
            bool pokerOk = Ads.PokerInterstitialDue(8, 10, false, true, 10000, 0);
            Check("cooldown-75s-any-ad",
                inside && edge && none && sec && stamped && blocked && just && later
                && pokerGrace && pokerHands && pokerCool && pokerVip && pokerOff && pokerOk,
                "inside=" + inside + " edge=" + edge + " none=" + none
                + " stamped=" + stamped + " blocked=" + blocked + " just=" + just + " later=" + later
                + " poker=" + pokerGrace + "/" + pokerHands + "/" + pokerCool + "/" + pokerVip + "/" + pokerOk);
        }

        static void CheckWheel(System.Action<string, bool, string> Check)
        {
            bool round = DailyBonus.RoundUp100(260) == 300
                && DailyBonus.RoundUp100(500) == 500
                && DailyBonus.RoundUp100(200) == 200
                && DailyBonus.RoundUp100(2) == 100
                && DailyBonus.RoundUp100(0) == 0
                && DailyBonus.RoundUp100(-40) == 0;
            Check("wheel-round-up-100", round, "260=" + DailyBonus.RoundUp100(260));

            DailyBonus.TestBegin(20261010, 0, 0);
            DailyBonus.TestLand(130, false);
            bool offer = DailyBonus.ShowDouble(true, false);
            bool noAd = !DailyBonus.ShowDouble(false, false);
            bool tutorArg = !DailyBonus.ShowDouble(true, true);
            int before = Purse.Coins;
            bool paid = DailyBonus.TryResolve(true, out int got);
            int delta = Purse.Coins - before;
            bool second = DailyBonus.TryResolve(true, out int again);
            Check("wheel-odd-double-rounds-up",
                offer && noAd && tutorArg && paid && got == 300 && delta == 300 && !second && again == 0,
                "offer=" + offer + " noAd=" + noAd + " tutorArg=" + tutorArg
                + " got=" + got + " delta=" + delta + " second=" + second);

            DailyBonus.TestBegin(20261010, 0, 0);
            DailyBonus.TestLand(130, false);
            int before1 = Purse.Coins;
            bool one = DailyBonus.TryResolve(false, out int face);
            int delta1 = Purse.Coins - before1;
            bool top = DailyBonus.PayStoredDouble(130);
            int topped = Purse.Coins - before1;
            int beforeTop = Purse.Coins;
            bool againTop = DailyBonus.PayStoredDouble(130);
            Check("wheel-odd-face-then-top-up",
                one && face == 130 && delta1 == 130 && top && topped == 300
                && againTop && Purse.Coins == beforeTop,
                "face=" + face + " delta=" + delta1 + " topped=" + topped + " again=" + againTop);

            DailyBonus.TestBegin(20261010, 0, 0);
            DailyBonus.TestLand(130, true);
            bool hidden = !DailyBonus.ShowDouble(true, false) && !DailyBonus.ShowDouble(true, true);
            int beforeT = Purse.Coins;
            bool tutorPay = DailyBonus.TryResolve(true, out int tutorGot);
            int tutorDelta = Purse.Coins - beforeT;
            Check("wheel-tutorial-no-double",
                hidden && tutorPay && tutorGot == 130 && tutorDelta == 130,
                "hidden=" + hidden + " got=" + tutorGot + " delta=" + tutorDelta);

            DailyBonus.TestBegin(20261010, 0, 0);
            bool spin = DailyBonus.TryBeginSpin(false, out int wedge, out int coins, out _);
            bool beforeLand = DailyBonus.ShowDouble(true, false);
            DailyBonus.MarkLanded();
            bool ad = DailyBonus.ShowDouble(true, false);
            int beforeN = Purse.Coins;
            bool doubled = DailyBonus.TryResolve(true, out int paidN);
            int deltaN = Purse.Coins - beforeN;
            bool replay = DailyBonus.TryResolve(true, out _);
            bool sameDay = DailyBonus.TryBeginSpin(false, out _, out _, out _);
            Check("wheel-multiple-still-exact",
                spin && !beforeLand && ad && doubled
                && paidN == coins * 2 && deltaN == paidN
                && !replay && !sameDay
                && coins >= 100 && (coins % 100) == 0
                && coins == DailyBonus.Prize(DailyBonus.HighestLevel, wedge),
                "spin=" + coins + " paid=" + paidN + " delta=" + deltaN + " wedge=" + wedge);
        }

        static void CheckVip(System.Action<string, bool, string> Check)
        {
            bool owned = !Ads.VipSoftDue(true, 20261011, 0);
            bool same = !Ads.VipSoftDue(false, 20261011, 20261011);
            bool fresh = Ads.VipSoftDue(false, 20261011, 0);
            bool next = Ads.VipSoftDue(false, 20261011, 20261010);
            bool blank = !Ads.VipSoftDue(false, 0, 0);
            Check("vip-soft-once-a-day", owned && same && fresh && next && blank,
                "owned=" + owned + " same=" + same + " fresh=" + fresh + " next=" + next);

            Ads.SetClockForTest(8000);
            Ads.SetSoftDayForTest(20261011);
            PlayerPrefs.DeleteKey(Ads.PrefVipSoft);
            Ads.DeliverInterstitialClosed();
            bool failQuiet = !Ads.VipSoftPending;
            Ads.NoteInterstitialDisplayed();
            Ads.DeliverInterstitialClosed();
            bool armed = Ads.VipSoftPending;
            bool ate = Ads.TryConsumeVipSoft();
            bool spent = !Ads.VipSoftPending;
            int saved = PlayerPrefs.GetInt(Ads.PrefVipSoft, 0);
            Ads.DeliverInterstitialClosed();
            bool noRearm = !Ads.VipSoftPending;
            bool closeOk = NoAds.Owned
                ? failQuiet && !armed && !ate && spent && noRearm
                : failQuiet && armed && ate && saved == 20261011 && spent && noRearm;
            Check("vip-soft-after-real-close", closeOk,
                "failQuiet=" + failQuiet + " armed=" + armed + " ate=" + ate
                + " saved=" + saved + " spent=" + spent + " owned=" + NoAds.Owned);

            var lay = FlockFiveApp.LayoutVipSoft(W, H);
            float safe = CaptionLayout.SafePlateWidth(W, H);
            bool seat = lay.Caption.width <= safe + 0.5f
                && lay.Caption.yMin >= H * 0.18f
                && lay.Go.yMin > lay.Caption.yMax
                && lay.Skip.yMin > lay.Go.yMax // b83 10c: smaller secondary centered under the primary
                && lay.Skip.width < lay.Go.width
                && lay.Skip.yMax < H * 0.90f
                && lay.Go.xMin >= lay.Caption.xMin - 1f
                && lay.Skip.xMax <= lay.Caption.xMax + 1f;
            var gold = FlockFiveApp.GoldCaption(FlockFiveApp.VipSoftLine, W, H);
            bool copy = !gold.Overflow && gold.Lines >= 1 && gold.Lines <= 2
                && FlockFiveApp.VipSoftGo == "Go VIP"
                && FlockFiveApp.VipSoftSkip == "Not now";
            bool live;
            if (NoAds.Owned)
            {
                FlockFiveApp.VipOffer.OpenSoft();
                live = !FlockFiveApp.VipOffer.SoftUp;
                FlockFiveApp.VipOffer.DismissSoft(true);
                live = live && !FlockFiveApp.VipOffer.TakeFullRequest();
            }
            else
            {
                FlockFiveApp.VipOffer.OpenSoft();
                bool up = FlockFiveApp.VipOffer.SoftUp;
                FlockFiveApp.VipOffer.DismissSoft(false);
                bool down = !FlockFiveApp.VipOffer.SoftUp && !FlockFiveApp.VipOffer.FullRequested;
                FlockFiveApp.VipOffer.OpenSoft();
                FlockFiveApp.VipOffer.DismissSoft(true);
                bool go = !FlockFiveApp.VipOffer.SoftUp && FlockFiveApp.VipOffer.TakeFullRequest();
                live = up && down && go;
            }
            Check("vip-soft-layout-dismiss", seat && copy && live,
                "seat=" + seat + " lines=" + gold.Lines + " overflow=" + gold.Overflow + " live=" + live);
        }

        static void CheckWiring(System.Action<string, bool, string> Check)
        {
            string root = Application.dataPath;
            string ads = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/Ads.cs"));
            string bonus = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/DailyBonus.cs"));
            string app = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.cs"));
            string clear = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.ClearAds.cs"));
            string vip = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.VipOffer.cs"));
            string rewards = File.ReadAllText(Path.Combine(root, "_Project/Scripts/App/FlockFiveApp.Rewards.cs"));
            string stamp = Slice(app, "public static Rect RewardStampRect(", "static Texture2D RewardDiscTex");

            string rewarded = Slice(ads, "IEnumerator Rewarded(string placement, int payload, bool tutorialGoal)", "public static RewardedGrant.Place PlaceFor");
            bool rewardFree = rewarded.IndexOf("NoAds.Owned") < 0
                && rewarded.IndexOf("Cooling(") < 0
                && rewarded.IndexOf("InterstitialCooldown") < 0
                && rewarded.Length > 40;

            string show = Slice(bonus, "public static bool ShowDouble(", "public static bool TryResolve");
            bool showOpen = show.IndexOf("% 100") < 0 && show.IndexOf("%100") < 0 && show.Length > 40;
            string resolve = Slice(bonus, "public static bool TryResolve(", "static void Rebuild");
            string stored = Slice(bonus, "public static bool PayStoredDouble(", "public static void TestDropMemory");
            bool rounds = resolve.IndexOf("RoundUp100") >= 0 && stored.IndexOf("RoundUp100") >= 0;

            // b83 10d: the mid-clear offer is gone (2X lives in the home REWARD popup).
            int offerAt = app.IndexOf("yield return OfferClearDouble();");
            int adAt = app.IndexOf("yield return Ads.Interstitial();");
            int softAt = app.IndexOf("Ads.TryConsumeVipSoft()", adAt < 0 ? 0 : adAt);
            bool order = offerAt < 0 && adAt >= 0 && softAt > adAt;

            string closed = Slice(ads, "public static void DeliverInterstitialClosed()", "public static void DeliverDisplayedCallback");
            bool shownGate = closed.IndexOf("_intDisplayed") >= 0
                && closed.IndexOf("NoAds.Owned") >= 0
                && closed.IndexOf("VipSoftPending = true") > closed.IndexOf("_intDisplayed");
            string loadFail = Slice(ads, "void OnIntLoadFail(", "void OnIntDisplayFail");
            string displayFail = Slice(ads, "void OnIntDisplayFail(", "void OnRvShown");
            bool failsClose = loadFail.IndexOf("DeliverInterstitialClosed") >= 0
                && loadFail.IndexOf("NoteInterstitialDisplayed") < 0
                && displayFail.IndexOf("DeliverInterstitialClosed") >= 0
                && displayFail.IndexOf("NoteInterstitialDisplayed") < 0;
            bool shownNotes = Slice(ads, "void OnIntShown(", "void OnIntClosed").IndexOf("NoteInterstitialDisplayed") >= 0;
            bool displayNotes = Slice(ads, "public static void DeliverDisplayedCallback()", "public static void DeliverFailedCallback").IndexOf("NoteAnyAd") >= 0;

            bool ui = clear.IndexOf("PaintCoachCaption") >= 0
                && clear.IndexOf("RewardStampRect") >= 0
                && clear.IndexOf("Watch Ad: 2x coins") >= 0
                && clear.IndexOf("skipHit || outside") >= 0
                && stamp.IndexOf("StreakBadge.TopRight") >= 0;
            int tick = vip.IndexOf("public static void Tick()");
            int ownedReturn = tick < 0 ? -1 : vip.IndexOf("if (!NoAds.Owned)", tick);
            int flag = tick < 0 ? -1 : vip.IndexOf("FullRequested", tick);
            bool softUi = vip.IndexOf("PaintOfferCaption") >= 0
                && vip.IndexOf("skipHit || outside") >= 0
                && vip.IndexOf("DismissSoft(true)") >= 0
                && vip.IndexOf("HomeFace.Splash") >= 0
                && flag >= 0 && ownedReturn > flag;
            bool grant = rewards.IndexOf("Place.Clear") >= 0
                && Ads.PlaceFor(Ads.PlacementClearDouble) == RewardedGrant.Place.Clear
                && Ads.PlaceFor(Ads.PlacementBonus) == RewardedGrant.Place.Branch;

            // A watched clear still offers the 2x. VIP is not an input to that gate.
            bool vipKeepsReward = Ads.ClearRewardOffered(true, true);

            Check("wiring-shared-helpers",
                rewardFree && showOpen && rounds && order && shownGate && failsClose
                && shownNotes && displayNotes && ui && softUi && grant && vipKeepsReward,
                "rewardFree=" + rewardFree + " showOpen=" + showOpen + " rounds=" + rounds
                + " order=" + order + " shownGate=" + shownGate + " fails=" + failsClose
                + " notes=" + shownNotes + "/" + displayNotes
                + " ui=" + ui + " soft=" + softUi + " grant=" + grant);
        }

        static string Slice(string src, string start, string end)
        {
            int a = src.IndexOf(start);
            if (a < 0) return "";
            int b = src.IndexOf(end, a + start.Length);
            if (b < 0) b = src.Length;
            return src.Substring(a, b - a);
        }

        static bool Near(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 0.5f
                && Mathf.Abs(a.y - b.y) < 0.5f
                && Mathf.Abs(a.width - b.width) < 0.5f
                && Mathf.Abs(a.height - b.height) < 0.5f;
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
                    "flockfive.daily.last24",
                    "flockfive.ads.lastany"
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
                    "flockfive.daily.last.x",
                    "flockfive.daily.last24.x",
                    "flockfive.clear.double",
                    "flockfive.vip.soft",
                    "flockfive.session_clears"
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

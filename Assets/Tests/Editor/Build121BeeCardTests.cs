#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Build 121 bee cards: stage-clear board names a card count and doubles coins only,
    // the Upgrade glove travels onto the button without covering the word, and each
    // upgrade adds one honeycomb on the card back (rarity still 2 / 3 / 5).
    public static class Build121BeeCardTests
    {
        const string Out = "/tmp/flock-five-b121-bee-cards.txt";

        [MenuItem("Flock Five/Build 121 Bee Card Tests")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            int fail = 0;
            void Check(string name, bool ok, string detail)
            {
                if (!ok) fail++;
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build121] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }

            CheckCopy(Check);
            CheckGlove(Check);
            CheckCombs(Check);

            string end = fail == 0 ? "BUILD121_BEE_CARDS_OK" : "BUILD121_BEE_CARDS_FAIL " + fail;
            sb.AppendLine(end);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build121] " + end);
            if (fail > 0) throw new System.Exception(end);
        }

        static void CheckCopy(System.Action<string, bool, string> Check)
        {
            Check("no-cards-line", FlockFiveApp.CardsCollectedLine(0) == null, "");
            Check("one-card-line", FlockFiveApp.CardsCollectedLine(1) == "1 card collected",
                FlockFiveApp.CardsCollectedLine(1));
            Check("many-cards-line", FlockFiveApp.CardsCollectedLine(3) == "3 cards collected",
                FlockFiveApp.CardsCollectedLine(3));
            var hero = new Rect(40f, 80f, 420f, 140f);
            var none = FlockFiveApp.LayoutRewardCards(hero, 0, 1f);
            var some = FlockFiveApp.LayoutRewardCards(hero, 3, 1.2f);
            Check("zero-cards-keeps-the-amount",
                !none.Shown && Mathf.Abs(none.Amount.height - hero.height) < 0.01f, none.Amount.ToString());
            Check("cards-line-under-the-coins",
                some.Shown
                && some.Cards.y >= some.Amount.yMax - 0.5f
                && some.Amount.height > hero.height * 0.5f
                && some.Cards.xMax <= hero.xMax + 0.5f
                && some.Amount.yMin >= hero.yMin - 0.5f,
                "amt " + some.Amount + " cards " + some.Cards);

            Check("daily-word-stays-2x", FlockFiveApp.RewardDoubleWord == "2X", FlockFiveApp.RewardDoubleWord);
            Check("stage-2x-is-coins-only",
                FlockFiveApp.DoublesCoinsOnly(FlockFiveApp.RewardCoinsDoubleWord)
                && !FlockFiveApp.DoublesCoinsOnly(FlockFiveApp.RewardDoubleWord)
                && FlockFiveApp.RewardCoinsDoubleWord.IndexOf("CARD") < 0,
                FlockFiveApp.RewardCoinsDoubleWord);

            string choice = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.RewardChoice.cs");
            int draw = choice.IndexOf("void DrawRewardChoice(");
            int offer = draw >= 0 ? choice.IndexOf("void DrawOfferChoice(", draw) : -1;
            string rewardBody = draw >= 0 && offer > draw ? choice.Substring(draw, offer - draw) : "";
            Check("stage-popup-passes-coins-word", rewardBody.Contains("RewardCoinsDoubleWord"), "");
            string daily = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.DailyBonus.cs");
            Check("daily-popup-does-not-take-the-coins-word",
                daily.Contains("DrawOfferChoice(") && daily.IndexOf("RewardCoinsDoubleWord") < 0, "");
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("reward-board-paints-the-count",
                app.Contains("LayoutRewardCards(heroFull, _rewardCards, s)")
                && app.Contains("CardsCollectedLine(_rewardCards)"), "");
        }

        static void CheckGlove(System.Action<string, bool, string> Check)
        {
            float? keep = DeviceFit.TestWindow;
            try
            {
                var screens = new[]
                {
                    new Vector2(820f, 1180f),
                    new Vector2(402f, 874f),
                    new Vector2(412f, 915f)
                };
                string[] names = { "ipad", "402", "412" };
                for (int i = 0; i < screens.Length; i++)
                {
                    float w = screens[i].x, h = screens[i].y;
                    DeviceFit.TestWindow = w / h;
                    float s = DeviceFit.Ui(w, h);
                    float btnH = Mathf.Max(84f, 92f * Mathf.Min(s, 1.35f));
                    float wideW = Mathf.Min(w * 0.80f, 460f * s);
                    float btnW = wideW * 0.60f;
                    float y = h - 36f * s - btnH;
                    var btn = new Rect((w - btnW) * 0.5f, y, btnW, btnH);
                    var label = new Rect(btn.x + btn.width * 0.06f, btn.y + btn.height * 0.10f,
                        btn.width * 0.88f, btn.height * 0.80f);
                    var aim = FlockFiveApp.ButtonLabelAim(btn, label, true);
                    float dh = FlockFiveApp.GloveDh(s);
                    var safe = new Rect(0f, 0f, w, h);
                    var near = aim + new Vector2(-12f, -6f);
                    var start = FlockFiveApp.ClaimGloveStart(near, aim, dh, safe);
                    float arch = dh * 0.55f;
                    var p0 = FlockFiveApp.ClaimGloveArc(start, aim, 0f, arch);
                    var mid = FlockFiveApp.ClaimGloveArc(start, aim, 0.45f, arch);
                    var land = FlockFiveApp.ClaimGloveArc(start, aim, 1f, arch);
                    var chord = Vector2.Lerp(start, aim, 0.45f);
                    var again = FlockFiveApp.ClaimGloveArc(start, aim, 0f, arch);
                    float apart = (start - aim).magnitude;
                    float nearApart = (near - aim).magnitude;
                    bool offWord = btn.Contains(aim) && !label.Contains(aim) && aim.x < label.center.x;
                    // The safe edge can shorten a phone start. It still leaves the button.
                    bool away = !btn.Contains(start) && apart > 60f && apart > nearApart * 3f;
                    bool bow = (p0 - start).magnitude < 0.5f
                        && (land - aim).magnitude < 0.5f
                        && (mid - start).magnitude > 24f
                        && (mid - aim).magnitude > 24f
                        && mid.y < chord.y - 8f
                        && (again - start).magnitude < 0.5f;
                    var toWord = label.center - aim;
                    var toStart = start - aim;
                    bool palmOff = Vector2.Dot(toWord, toStart) < 0f;
                    Check("upgrade-glove-" + names[i],
                        offWord && away && bow && palmOff
                        && !FlockFiveApp.ClaimGloveTravelDone(0f)
                        && FlockFiveApp.ClaimGloveTravelDone(0.82f),
                        "aim " + aim + " start " + start + " apart " + apart.ToString("0")
                        + " off " + offWord + " away " + away + " bow " + bow + " palm " + palmOff);
                }
            }
            finally
            {
                DeviceFit.TestWindow = keep;
            }

            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            int tick = app.IndexOf("bool TickUpgradeTutor(");
            int tickEnd = tick >= 0 ? app.IndexOf("void DrawAlbumTutor(", tick) : -1;
            string tickBody = tick >= 0 && tickEnd > tick ? app.Substring(tick, tickEnd - tick) : "";
            Check("upgrade-aim-uses-shared-helper",
                tickBody.Contains("ButtonLabelAim(_upgradeBtnR, _upgradeLabelR, true)")
                && tickBody.Contains("float.NaN, true, float.NaN, _upgradeBtnR"), "");
            string coach = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Coach.cs");
            Check("upgrade-start-is-the-shared-far-perch",
                coach.Contains("string.Equals(_cueLine, DailyClaimLine) || string.Equals(_cueLine, AlbumUpgradeLine)")
                && coach.Contains("_gloveTip = ClaimGloveStart(_gloveTip, aimGui, faceDh, CoachSafeGui(8f * s));"), "");
            Check("upgrade-bow-loops-claim-pins",
                coach.Contains("bool buttonArc = claimTravel || string.Equals(_gloveStepLine, AlbumUpgradeLine)")
                && coach.Contains("if (buttonArc)")
                && coach.Contains("arcPos = ClaimGloveArc(_gloveTip, aim, travel, GloveDh(s) * 0.55f);"), "");
            int pin = coach.IndexOf("if (claimTravel && _gloveAct == TutorGloveAct.Live && ClaimGloveTravelDone");
            int pinEnd = pin >= 0 ? coach.IndexOf("GloveArmTap(tapRect, tapHit);", pin) : -1;
            string pinBody = pin >= 0 && pinEnd > pin ? coach.Substring(pin, pinEnd - pin) : "";
            Check("upgrade-does-not-pin-on-the-button",
                pin >= 0 && pinBody.IndexOf("AlbumUpgrade") < 0, "");
            Check("upgrade-tap-is-only-the-button",
                CoachTap.Kind(CoachStep.AlbumUpgrade) == CoachTapKind.Required
                && !CoachTap.AnyTap(CoachStep.AlbumUpgrade)
                && app.Contains("gate.Target = _upgradeBtnR"), "");
        }

        static void CheckCombs(System.Action<string, bool, string> Check)
        {
            bool floor = Hive.HoneyOfFinish(BeeFinish.Normal) == 2
                && Hive.HoneyOfFinish(BeeFinish.Holo) == 3
                && Hive.HoneyOfFinish(BeeFinish.InverseRainbow) == 5;
            bool caps = Hive.MaxUpgrades(BeeFinish.Normal) == 1
                && Hive.MaxUpgrades(BeeFinish.Holo) == 2
                && Hive.MaxUpgrades(BeeFinish.InverseRainbow) == 3;
            bool maxed = Hive.HoneyAfterUpgrades(BeeFinish.Normal, 0) == 2
                && Hive.HoneyAfterUpgrades(BeeFinish.Normal, 1) == 3
                && Hive.HoneyAfterUpgrades(BeeFinish.Normal, 4) == 3
                && Hive.HoneyAfterUpgrades(BeeFinish.Holo, 0) == 3
                && Hive.HoneyAfterUpgrades(BeeFinish.Holo, 2) == 5
                && Hive.HoneyAfterUpgrades(BeeFinish.InverseRainbow, 0) == 5
                && Hive.HoneyAfterUpgrades(BeeFinish.InverseRainbow, 1) == 6
                && Hive.HoneyAfterUpgrades(BeeFinish.InverseRainbow, 2) == 7
                && Hive.HoneyAfterUpgrades(BeeFinish.InverseRainbow, 3) == 8
                && Hive.HoneyAfterUpgrades(BeeFinish.InverseRainbow, 9) == 8;
            Check("rarity-floor-2-3-5", floor, "");
            Check("upgrade-caps-1-2-3", caps, "");
            Check("each-upgrade-adds-one-comb", maxed, "");

            Check("rows-2-3-4-5-stay-one-line",
                RowIs(2, 2, 0) && RowIs(3, 3, 0) && RowIs(4, 4, 0) && RowIs(5, 5, 0), "");
            Check("rows-6-7-8-are-balanced",
                RowIs(6, 3, 3) && RowIs(7, 3, 4) && RowIs(8, 4, 4), "");

            var screens = new[]
            {
                new Vector2(820f, 1180f),
                new Vector2(402f, 874f),
                new Vector2(412f, 915f)
            };
            string[] names = { "ipad", "402", "412" };
            for (int d = 0; d < screens.Length; d++)
            {
                var card = FlockFiveApp.AlbumInspectRect(screens[d].x, screens[d].y);
                var face = FlockFiveApp.CardText.FaceOf(card);
                string detail = "";
                bool ok = CombFamily(face, out detail);
                Check("back-comb-family-" + names[d], ok, detail);
            }

            string cardText = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.CardText.cs");
            string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            Check("draw-no-longer-stops-at-five",
                cardText.IndexOf("honey > 5") < 0 && app.IndexOf("if (honey > 5) honey = 5") < 0, "");
            Check("back-still-asks-the-slot",
                app.Contains("Hive.HoneyOfSlot(") && app.Contains("CardText.DrawBack("), "");
        }

        static bool RowIs(int honey, int top, int bottom)
        {
            FlockFiveApp.CardText.HoneyRows(honey, out int a, out int b);
            return a == top && b == bottom;
        }

        static bool CombFamily(Rect face, out string detail)
        {
            detail = "";
            var cells = new Rect[HoneyPoke.Cap];
            var five = new Rect[HoneyPoke.Cap];
            int n5 = FlockFiveApp.CardText.HoneyBand(face, 5, true, five);
            if (n5 != 5) { detail = "five " + n5; return false; }
            float fiveH = five[0].height;
            for (int honey = 2; honey <= 8; honey++)
            {
                int n = FlockFiveApp.CardText.HoneyBand(face, honey, true, cells);
                if (n != honey) { detail = "n " + n + " want " + honey; return false; }
                FlockFiveApp.CardText.FaceBands(face, true, true, true, out var title, out var flavor, honey);
                FlockFiveApp.CardText.HoneyRows(honey, out int top, out int bot);
                int rows = bot > 0 ? 2 : 1;
                float y0 = cells[0].y;
                int countedTop = 0;
                int countedBot = 0;
                float topX0 = 0f, topX1 = 0f, botX0 = 0f, botX1 = 0f;
                bool topSet = false, botSet = false;
                for (int i = 0; i < n; i++)
                {
                    var c = cells[i];
                    if (c.width < 8f || c.height < 8f) { detail = "tiny " + honey + " " + c; return false; }
                    if (c.yMin < face.y || c.yMax > face.yMax + 0.5f || c.xMin < face.x || c.xMax > face.xMax)
                    { detail = "off face " + honey + " " + c; return false; }
                    if (c.Overlaps(title) || c.Overlaps(flavor)) { detail = "copy " + honey; return false; }
                    if (c.yMin < title.yMax + 6f) { detail = "title gap " + honey; return false; }
                    for (int j = 0; j < i; j++)
                        if (c.Overlaps(cells[j])) { detail = "overlap " + honey; return false; }
                    bool lower = bot > 0 && c.y > y0 + c.height * 0.4f;
                    if (!lower)
                    {
                        countedTop++;
                        if (!topSet) { topX0 = c.xMin; topX1 = c.xMax; topSet = true; }
                        else { if (c.xMin < topX0) topX0 = c.xMin; if (c.xMax > topX1) topX1 = c.xMax; }
                        if (Mathf.Abs(c.y - y0) > 0.6f) { detail = "top y " + honey; return false; }
                    }
                    else
                    {
                        countedBot++;
                        if (!botSet) { botX0 = c.xMin; botX1 = c.xMax; botSet = true; }
                        else { if (c.xMin < botX0) botX0 = c.xMin; if (c.xMax > botX1) botX1 = c.xMax; }
                    }
                    if (rows == 1 && Mathf.Abs(c.y - y0) > 0.6f) { detail = "split " + honey; return false; }
                    if (honey >= 6 && c.height < fiveH * 0.85f) { detail = "cramped " + honey + " " + c.height + " vs " + fiveH; return false; }
                }
                if (countedTop != top || countedBot != bot) { detail = "rows " + honey + " " + countedTop + "+" + countedBot; return false; }
                float cx = face.center.x;
                if (Mathf.Abs((topX0 + topX1) * 0.5f - cx) > 1.5f) { detail = "top center " + honey; return false; }
                if (bot > 0 && Mathf.Abs((botX0 + botX1) * 0.5f - cx) > 1.5f) { detail = "bot center " + honey; return false; }
                if (!EvenGaps(cells, n, y0, true)) { detail = "gap " + honey; return false; }
                if (bot > 0 && !EvenGaps(cells, n, y0, false)) { detail = "lower gap " + honey; return false; }
            }
            float w3 = RowWidth(face, 3);
            float w5 = RowWidth(face, 5);
            if (!(w3 + 8f < w5)) { detail = "five should be the wide row " + w3 + " " + w5; return false; }
            return true;
        }

        static float RowWidth(Rect face, int honey)
        {
            var cells = new Rect[HoneyPoke.Cap];
            int n = FlockFiveApp.CardText.HoneyBand(face, honey, true, cells);
            float x0 = cells[0].xMin, x1 = cells[0].xMax;
            for (int i = 1; i < n; i++)
            {
                if (cells[i].xMin < x0) x0 = cells[i].xMin;
                if (cells[i].xMax > x1) x1 = cells[i].xMax;
            }
            return x1 - x0;
        }

        static bool EvenGaps(Rect[] cells, int n, float y0, bool top)
        {
            float gap = -1f;
            float prev = -1f;
            for (int i = 0; i < n; i++)
            {
                bool onTop = Mathf.Abs(cells[i].y - y0) <= 0.6f;
                if (onTop != top) continue;
                if (prev >= 0f)
                {
                    float g = cells[i].xMin - prev;
                    if (gap >= 0f && Mathf.Abs(g - gap) > 0.6f) return false;
                    gap = g;
                }
                prev = cells[i].xMax;
            }
            return true;
        }
    }
}
#endif

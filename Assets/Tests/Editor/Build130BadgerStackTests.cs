#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // b130 badger stack (Brandon a-j): rect / overlap checks at 375x667, 390x844, 430x932 and iPad, from the same pure
    // layout the fight page draws with: grids, HUD (back, coin pill), YOU / BADGER LEFT plates, the pick combs, the
    // two-lane columns, both fighters, the coach arrows, the power row gap, the caption seat and the win banner.
    public static class Build130BadgerStackTests
    {
        const string Out = "/tmp/ff-badger-stack-b130-tests.txt";

        struct Dev { public string Name; public float W, H, Top, Bottom, Pt; }

        static bool Hit(Rect a, Rect b) => a.xMin < b.xMax - 0.5f && a.xMax > b.xMin + 0.5f && a.yMin < b.yMax - 0.5f && a.yMax > b.yMin + 0.5f;
        static bool Inside(Rect o, Rect i) => i.xMin >= o.xMin - 0.5f && i.xMax <= o.xMax + 0.5f && i.yMin >= o.yMin - 0.5f && i.yMax <= o.yMax + 0.5f;
        static string R(Rect r) => string.Format(CultureInfo.InvariantCulture, "({0:0},{1:0},{2:0},{3:0})", r.x, r.y, r.width, r.height);

        [MenuItem("Flock Five/Build 130 Badger Stack Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            var ci = CultureInfo.InvariantCulture;
            void Check(string name, bool ok, string detail)
            {
                string line = (ok ? "PASS  " : "FAIL  ") + name + "  " + detail;
                if (ok) pass++; else fail++;
                sb.AppendLine(line);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build130] " + line);
            }

            var devs = new[]
            {
                new Dev { Name = "375x667", W = 750f, H = 1334f, Top = 40f, Bottom = 0f, Pt = 375f / 750f },
                new Dev { Name = "390x844", W = 1170f, H = 2532f, Top = 141f, Bottom = 102f, Pt = 390f / 1170f },
                new Dev { Name = "430x932", W = 1290f, H = 2796f, Top = 177f, Bottom = 102f, Pt = 430f / 1290f },
                new Dev { Name = "ipad1024x1366", W = 2048f, H = 2732f, Top = 48f, Bottom = 40f, Pt = 1024f / 2048f },
            };
            var idle = SpriteCatalog.BadgerArt("badger_idle_b");
            float aw = idle != null ? idle.rect.width : 480f, ah = idle != null ? idle.rect.height : 640f;
            int youT = BadgerSchedule.PlayerTarget(2), bossT = BadgerSchedule.BadgerTarget(2);
            float? keepWin = DeviceFit.TestWindow;
            try
            {
                foreach (var d in devs)
                {
                    DeviceFit.TestWindow = d.W / d.H;
                    float s = DeviceFit.Ui(d.W, d.H);
                    var safe = new Rect(0f, d.Bottom, d.W, d.H - d.Top - d.Bottom);
                    float top = d.Top + 8f * s;
                    var back = new Rect(Mathf.Max(16f, safe.xMin + 10f), top, 101.2f * Mathf.Clamp(s, 1f, 1.35f), 101.2f * Mathf.Clamp(s, 1f, 1.35f));
                    var L = FlockFiveApp.BadgerLayoutFor(d.W, d.H, safe, s, top, back);
                    string n = d.Name;
                    var screenSafe = new Rect(0f, d.Top, d.W, d.H - d.Top - d.Bottom);

                    // grids vs HUD and columns
                    Rect[] hud = { L.Back, L.Coin, L.Title };
                    foreach (var g in new[] { L.BossGrid, L.PlayerGrid })
                    {
                        bool hh = false; foreach (var h in hud) hh |= Hit(g, h);
                        Check("grid-vs-hud." + n, !hh, "grid " + R(g) + " back " + R(L.Back) + " coin " + R(L.Coin));
                        Check("grid-vs-columns." + n, !Hit(g, L.ColL) && !Hit(g, L.ColR), "grid " + R(g) + " colL " + R(L.ColL) + " colR " + R(L.ColR));
                    }
                    Check("columns-vs-hud." + n, !Hit(L.ColL, L.Back) && !Hit(L.ColR, L.Coin) && !Hit(L.ColL, L.Coin) && !Hit(L.ColR, L.Back),
                        "colL " + R(L.ColL) + " colR " + R(L.ColR) + " back " + R(L.Back) + " coin " + R(L.Coin));
                    Check("columns-in-safe." + n, Inside(screenSafe, L.ColL) && Inside(screenSafe, L.ColR), "safe " + R(screenSafe));

                    // (h) two lanes
                    float unit = BadgerMeter.Unit(L.ColL, BadgerSchedule.BadgerTargetFixed);
                    var c0 = BadgerMeter.Comb2(L.ColL, 0, unit); var c1 = BadgerMeter.Comb2(L.ColL, 1, unit); var c2 = BadgerMeter.Comb2(L.ColL, 2, unit);
                    Check("two-lanes." + n, Mathf.Abs(c0.y - c1.y) < 0.01f && c1.x >= c0.xMax && c2.yMax <= c0.y + 0.5f && Inside(L.ColL, c1)
                        && BadgerMeter.Rows(18) == 9, "c0 " + R(c0) + " c1 " + R(c1) + " c2 " + R(c2));

                    // YOU / BADGER LEFT plates vs coin pill and grids
                    var you = BadgerMeter.EndPlate2(L.ColL, youT, unit);
                    var bl = BadgerMeter.EndPlate2(L.ColR, bossT, unit);
                    // coin pill: icon plus the amount to its left (generous 4 icons wide), on the coin row
                    var pill = new Rect(L.Coin.x - L.Coin.width * 4f - 8f * s, L.Coin.y, L.Coin.width * 5f + 8f * s, L.Coin.height);
                    Check("badger-left-vs-coin-pill." + n, !Hit(bl, pill) && !Hit(L.ColR, pill), "plate " + R(bl) + " pill " + R(pill));
                    Check("you-counter-clear." + n, !Hit(you, L.Back) && !Hit(you, L.PlayerGrid) && !Hit(you, L.BossGrid) && Inside(L.ColL, you), "you " + R(you));
                    Check("badger-left-clear." + n, !Hit(bl, L.PlayerGrid) && !Hit(bl, L.BossGrid) && Inside(L.ColR, bl), "plate " + R(bl));
                    float pillL = L.Coin.x - 8f * s - FlockFiveApp.BadgerCoinPillTextW(L.Coin.width);
                    Check("title-vs-coin-pill." + n, L.Title.xMax <= pillL + 0.5f, "title " + R(L.Title) + " pill.x " + pillL.ToString("0", ci));
                    Check("coin-size-shared." + n, Mathf.Abs(L.Coin.width - FlockFiveApp.CoinBalanceIcon(d.W, s)) < 0.01f, "coin " + R(L.Coin));

                    // fighters: level, symmetric, bigger bird, clear of columns and pick combs
                    var box = BadgerArenaRects.Split(L.Arena);
                    var m = BadgerFighterPlace.Rest(box, aw, ah, L.ColR.x - 2f);
                    float oldIcon = Mathf.Min(box.YouFighter.width * 0.92f, box.YouFighter.height * 0.90f);
                    Check("bird-1.25x." + n, m.Icon >= oldIcon * 1.24f || m.Icon >= box.YouFighter.height - 0.5f, "icon " + m.Icon.ToString("0", ci) + " old " + oldIcon.ToString("0", ci));
                    Check("bird-on-baseline." + n, Mathf.Abs(m.BirdBox.yMax - m.Foot.y) < 0.5f, "bird.yMax " + m.BirdBox.yMax.ToString("0", ci) + " foot.y " + m.Foot.y.ToString("0", ci));
                    float axis = box.Middle.center.x;
                    float inL = axis - m.BirdBox.center.x, inR = m.Foot.x - axis;
                    Check("fighters-mirror-axis." + n, Mathf.Abs(inL - inR) < 1f, "in " + inL.ToString("0.0", ci) + " / " + inR.ToString("0.0", ci));
                    Check("avatars-vs-columns." + n, !Hit(m.BirdBox, L.ColL) && !Hit(m.BadgerBox, L.ColR) && !Hit(m.BirdBox, L.ColR) && !Hit(m.BadgerBox, L.ColL),
                        "bird " + R(m.BirdBox) + " badger " + R(m.BadgerBox));
                    var g2 = FlockFiveApp.BadgerPickComb(L.Arena, L.ColR, s);
                    var youComb = g2.You;
                    Check("pick-combs-clear." + n, !Hit(youComb, m.BirdBox) && !Hit(g2.Comb, g2.Fighter) && !Hit(youComb, g2.Comb)
                        && !Hit(youComb, L.ColL) && !Hit(g2.Comb, L.ColR) && youComb.width > 8f && Mathf.Abs(youComb.width - g2.Comb.width) < 0.5f, "you " + R(youComb) + " boss " + R(g2.Comb) + " bird " + R(m.BirdBox));

                    // (a/b/g) arrows
                    var tips = new Vector2[4];
                    for (int step = 0; step < 3; step++)
                    {
                        int k = FlockFiveApp.BadgerCoachArrowTips(L, step, youT, bossT, tips);
                        bool ok = k == (step == 1 ? 1 : 2);
                        string det = "";
                        for (int i = 0; i < k; i++)
                        {
                            var ar = FlockFiveApp.CoachArrowRect(tips[i], s, FlockFiveApp.BadgerCoachArrowMul(step));
                            det += " " + R(ar);
                            ok &= !Hit(ar, m.BirdBox) && !Hit(ar, m.BadgerBox) && !Hit(ar, L.Back) && !Hit(ar, L.Coin);
                            // the pick combs are not painted during the lesson (no tiles picked); the arrows clear the fighters
                            if (step == 0) ok &= ar.yMax <= L.PlayerGrid.y + 0.5f && ar.y >= L.Arena.y;
                            if (step == 1) ok &= ar.yMax <= L.Power.y + 0.5f && Mathf.Abs(tips[i].x - FlockFiveApp.BadgerPowerPairCenterX(L)) < 0.5f;
                            if (step == 2) ok &= Inside(i == 0 ? L.ColL : L.ColR, ar) && !Hit(ar, i == 0 ? you : bl);
                        }
                        Check("arrows.step" + step + "." + n, ok, "bird " + R(m.BirdBox) + " badger " + R(m.BadgerBox) + det);
                    }

                    // (c) power row gap, (f) caption seat inside the safe area
                    float rowGap = L.Power.y - L.Pick.yMax;
                    Check("power-row-gap." + n, Mathf.Abs(rowGap - FlockFiveApp.BadgerPowerRowGap(s)) < 0.5f && !Hit(L.Power, L.PlayerGrid),
                        "gap " + rowGap.ToString("0.0", ci) + "px (" + (rowGap * d.Pt).ToString("0.0", ci) + "pt)");
                    var seat = FlockFiveApp.BadgerCaptionSeat(L.Caption, s);
                    float safeBottom = d.H - d.Bottom;
                    Check("caption-in-safe." + n, Inside(L.Caption, seat) && L.Caption.yMax <= safeBottom - 4f * s && L.Caption.y >= L.Power.yMax && !Hit(seat, L.Power),
                        "caption " + R(L.Caption) + " seat " + R(seat) + " safeBottom " + safeBottom.ToString("0", ci) + " power " + R(L.Power));
                    for (int i = 0; i < BadgerCopy.LessonCount; i++)
                    {
                        bool paints = FlockFiveApp.BadgerCaptionPaints(BadgerCopy.LessonAt(i), d.W, d.H, safe, out string det);
                        Check("lesson-caption-fits." + i + "." + n, paints, det);
                        bool room = FlockFiveApp.BadgerLessonCaptionRoom(BadgerCopy.LessonAt(i), d.W, d.H, safe, out _, out _, out string det2);
                        Check("lesson-caption-room." + i + "." + n, room, det2);
                    }

                    // (j) win banner clear of Continue and inside the screen
                    float bw = Mathf.Min(L.Power.width * 0.72f, 420f * L.S);
                    var cont = new Rect(L.Power.center.x - bw * 0.5f, L.Power.y, bw, L.Power.height);
                    var panel = FlockFiveApp.BadgerWinPanelRect(L, cont);
                    Check("win-banner." + n, !Hit(panel, cont) && Inside(screenSafe, panel) && panel.yMax <= cont.y - 8f * s, "panel " + R(panel) + " continue " + R(cont));
                }
                DeviceFit.TestWindow = 1170f / 2532f;
                float s390 = DeviceFit.Ui(1170f, 2532f);
                Check("power-row-gap-14pt-at-390x844", Mathf.Abs(FlockFiveApp.BadgerPowerRowGap(s390) * 390f / 1170f - 14f) < 0.1f,
                    "pt " + (FlockFiveApp.BadgerPowerRowGap(s390) * 390f / 1170f).ToString("0.00", ci));
            }
            finally { DeviceFit.TestWindow = keepWin; }
            sb.AppendLine("pass " + pass + " fail " + fail);
            File.WriteAllText(Out, sb.ToString());
            Debug.Log("[build130] pass " + pass + " fail " + fail);
        }
    }
}
#endif

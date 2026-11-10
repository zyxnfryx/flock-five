#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace FlockFive.Editor
{
    // The yellow celebration amount ("WIN $100") keeps its fill and gains a
    // black disc thicker than StampLight's 2px keyline. The disc is even
    // (same reach on every side, diagonals filled, no hole). On iPad 820,
    // iPhone 402, and iPhone 412 the stroked word stays inside the screen,
    // under the top chrome, and above the bet row.
    public class PokerWinBurstInkTests
    {
        // Hero point sizes for "WIN $100" at the portrait targets. Width-fit
        // against safeW * 0.90 / 1.13 / 1.06, Liberation Sans Bold ~4.45 em.
        // iPad 820×1180 plays in the 9:16 column (664). Phones are the full width.
        const int IpadHero = 112;
        const int Phone402Hero = 68;
        const int Phone412Hero = 70;
        const int Native1206Hero = 204;

        [Test]
        public void BurstOutlineIsThickerThanTheChromeHairline()
        {
            Assert.That(PokerWinBurstInk.ChromePx, Is.EqualTo(2));
            Assert.That(PokerWinBurstInk.OutlineEm, Is.EqualTo(0.06f).Within(0.0001f));

            // iPad first: several times the 2px hairline, and the same weight on the phones.
            Assert.That(PokerWinBurstInk.OutlinePx(IpadHero), Is.EqualTo(7));
            Assert.That(PokerWinBurstInk.OutlinePx(IpadHero), Is.GreaterThanOrEqualTo(PokerWinBurstInk.ChromePx * 3));
            Assert.That(PokerWinBurstInk.OutlinePx(Phone402Hero), Is.EqualTo(4));
            Assert.That(PokerWinBurstInk.OutlinePx(Phone412Hero), Is.EqualTo(4));
            Assert.That(PokerWinBurstInk.OutlinePx(Native1206Hero), Is.EqualTo(12));

            foreach (int hero in new[] { IpadHero, Phone402Hero, Phone412Hero, Native1206Hero })
            {
                int px = PokerWinBurstInk.OutlinePx(hero);
                Assert.That(px, Is.GreaterThan(PokerWinBurstInk.ChromePx), "hero " + hero);
                Assert.That(px, Is.GreaterThanOrEqualTo(PokerWinBurstInk.MinPx), "hero " + hero);
                AssertEven(px);
            }
        }

        [Test]
        public void BurstDiscIsEvenOnAllSides()
        {
            for (int hero = 40; hero <= 320; hero += 17)
                AssertEven(PokerWinBurstInk.OutlinePx(hero));
        }

        [Test]
        public void StrokedHeroStaysOffTheChrome()
        {
            // iPad 820×1180 pillar column, then the two phones. Portrait only.
            Clears(Mathf.RoundToInt(1180 * 9f / 16f), 1180, "ipad-820");
            Clears(402, 874, "iphone-402");
            Clears(412, 915, "iphone-412");
        }

        [Test]
        public void FanfareAmountUsesTheBurstDiscAndKeepsTheGoldFill()
        {
            string src = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.cs");
            int amount = src.IndexOf("PokerWinBurstInk.OutlinePx(_pokerWinHeroFont)");
            int paint = src.IndexOf("StampWinBurst(r, text, st, tint, burstPx)");
            int rank = src.IndexOf("DrawPokerWinWord(title, st, c, size, k, fill, alpha, true)");
            int rest = src.IndexOf("StampLight(r, text, st, PokerWinRed)");
            Assert.That(amount, Is.GreaterThan(0), "hero stroke");
            Assert.That(paint, Is.GreaterThan(0), "burst stamp");
            Assert.That(rank, Is.GreaterThan(0), "rank title stays on the chrome stroke");
            Assert.That(rest, Is.GreaterThan(0), "resting banner stays on the chrome stroke");

            // One black disc, then the fill. No white keyline on this word.
            int stamp = src.IndexOf("static void StampWinBurst(");
            int next = src.IndexOf("static void DrawPokerWinRays(");
            Assert.That(stamp, Is.GreaterThan(0));
            Assert.That(next, Is.GreaterThan(stamp));
            string body = src.Substring(stamp, next - stamp);
            Assert.That(body, Does.Contain("new Color(0f, 0f, 0f, a)"));
            Assert.That(body, Does.Not.Contain("new Color(1f, 1f, 1f"));
            Assert.That(body, Does.Contain("Paint(st, fill)"));

            // Yellow fill is the same gold, lerped toward red only as the word flies.
            Assert.That(src, Does.Contain("static readonly Color PokerWinGold = new Color(1f, 0.86f, 0.22f, 1f)"));
            Assert.That(src, Does.Contain("Color.Lerp(PokerWinGold, PokerWinRed, whoosh)"));
            // The word does not move. Center stays mid-screen.
            Assert.That(src, Does.Contain("g.HeroC = new Vector2(PokerSafeGui().center.x, PhoneArea.H * 0.47f)"));
        }

        // Upper bound: the fit box at the punch peak, plus the thicker disc.
        // The real word is this size or smaller. Top band is the purse / PAY TABLE.
        // Bottom band is the bet row.
        static void Clears(int w, int h, string name)
        {
            float s = Mathf.Max(h / 720f, 1f);
            float heroH = Mathf.Min(w * 0.26f, 190f * s);
            float maxW = w * 0.90f / 1.13f / 1.06f;
            int peakDrawn = Mathf.Max(1, Mathf.RoundToInt(heroH * 1.13f));
            int side = PokerWinBurstInk.OutlinePx(peakDrawn);
            float halfW = maxW * 1.13f * 0.5f + side;
            float halfH = heroH * 1.13f * 0.5f + side;
            float cx = w * 0.5f;
            float cy = h * 0.47f;
            Assert.That(cx - halfW, Is.GreaterThan(8f), name + " left");
            Assert.That(cx + halfW, Is.LessThan(w - 8f), name + " right");
            Assert.That(cy - halfH, Is.GreaterThan(h * 0.18f), name + " clears purse and PAY TABLE");
            Assert.That(cy + halfH, Is.LessThan(h * 0.78f), name + " clears the bet row");
            // Shortest payout ("WIN $1", ~3.33 em) still fits the existing 6% outline pad.
            float frac = (2f * side) / (3.33f * peakDrawn);
            Assert.That(frac, Is.LessThan(0.06f), name + " pad " + frac.ToString("0.000"));
        }

        static void AssertEven(int radius)
        {
            Assert.That(radius, Is.GreaterThanOrEqualTo(PokerWinBurstInk.MinPx));
            int n = LabelRim.Count(radius);
            int reachX = 0, reachY = 0;
            var seen = new bool[radius * 2 + 5, radius * 2 + 5];
            int origin = radius + 2;
            for (int i = 0; i < n; i++)
            {
                LabelRim.At(radius, i, out int x, out int y);
                Assert.That(x != 0 || y != 0, Is.True, "fill pixel is not part of the disc");
                reachX = Mathf.Max(reachX, Mathf.Abs(x));
                reachY = Mathf.Max(reachY, Mathf.Abs(y));
                Assert.That(seen[x + origin, y + origin], Is.False, "duplicate " + x + "," + y);
                seen[x + origin, y + origin] = true;
            }
            // Same reach on the horizontal and the vertical, and that far on both.
            Assert.That(reachX, Is.EqualTo(radius), "horizontal reach");
            Assert.That(reachY, Is.EqualTo(radius), "vertical reach");
            Assert.That(Has(radius, radius, 0) && Has(radius, -radius, 0), Is.True, "left and right");
            Assert.That(Has(radius, 0, radius) && Has(radius, 0, -radius), Is.True, "top and bottom");

            int lim = (2 * radius + 1) * (2 * radius + 1);
            int expect = 0;
            for (int y = -radius - 1; y <= radius + 1; y++)
            {
                for (int x = -radius - 1; x <= radius + 1; x++)
                {
                    if (x == 0 && y == 0) continue;
                    if (((x * x + y * y) << 2) > lim) continue;
                    expect++;
                    Assert.That(seen[x + origin, y + origin], Is.True, "hole at " + x + "," + y + " r=" + radius);
                }
            }
            Assert.That(n, Is.EqualTo(expect));
            // Furthest filled diagonal. A plus has none. A diamond stops at radius/2.
            // A disc reaches about 0.7 of the radius, so this has to pass the halfway mark.
            int diag = 0;
            for (int d = radius; d >= 1; d--)
            {
                if (((d * d + d * d) << 2) <= lim)
                {
                    diag = d;
                    break;
                }
            }
            Assert.That(diag, Is.GreaterThan(radius / 2), "diagonal reach r=" + radius);
            Assert.That(Has(radius, diag, diag) && Has(radius, -diag, diag)
                && Has(radius, diag, -diag) && Has(radius, -diag, -diag), Is.True, "diagonals r=" + radius);
        }

        static bool Has(int radius, int x, int y)
        {
            int n = LabelRim.Count(radius);
            for (int i = 0; i < n; i++)
            {
                LabelRim.At(radius, i, out int ax, out int ay);
                if (ax == x && ay == y) return true;
            }
            return false;
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 R6 (Readability): poker text contrast >= 4.5:1, text >= 12 pt, targets >= 44 pt,
    // a glyph the font has for minus, and Back/balance below the 59 pt top band.
    public static class Build120PokerFloorsTests
    {
        const string Out = "/tmp/flock-five-b120-poker-floors-tests.txt";
        static float L(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
        static float Lum(Color c) => 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        static float Ratio(Color a, Color b) { float x = Lum(a), y = Lum(b); return (Mathf.Max(x, y) + 0.05f) / (Mathf.Min(x, y) + 0.05f); }
        static Color Over(Color top, Color under) => new Color(Mathf.Lerp(under.r, top.r, top.a), Mathf.Lerp(under.g, top.g, top.a), Mathf.Lerp(under.b, top.b, top.a), 1f);
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build120-pokerfloors] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // BET: gold label on the dark plate, plate composited over the brightest wash (white).
            var gold = new Color(1f, 0.94f, 0.72f, 1f);
            var plate = Over(FlockFiveApp.PokerBetPlate, Color.white);
            float bet = Ratio(gold, plate);
            Check("BET >= 4.5:1", bet >= 4.5f, bet.ToString("0.0"));
            // DEAL: ink on the ochre disc. Inspector measured 3.2:1 with the old ink -> disc luminance ~0.25.
            var oldInk = new Color(0.36f, 0.18f, 0.07f);
            float discLum = 3.2f * (Lum(oldInk) + 0.05f) - 0.05f;
            float deal = (discLum + 0.05f) / (Lum(FlockFiveApp.PokerDiscInk) + 0.05f);
            Check("DEAL >= 4.5:1", deal >= 4.5f, deal.ToString("0.0") + " disc " + discLum.ToString("0.00"));
            // Minus is plain ASCII (in every bundled font).
            Check("minus glyph ASCII", FlockFiveApp.PokerMinusGlyph == "-", "");
            var devs = new[] { new Vector2(804f, 1748f), new Vector2(824f, 1830f), new Vector2(1640f, 2360f), new Vector2(1179f, 2556f) };
            foreach (var dv in devs)
            {
                float w = dv.x, h = dv.y;
                DeviceFit.TestWindow = w / h;
                float s = DeviceFit.Ui(w, h);
                var safe = new Rect(0f, 0f, w, h);
                string tag = w + "x" + h;
                float pt = FlockFiveApp.PokerPtPx(s);
                var chrome = FlockFiveApp.LayoutPokerChrome(w, h, safe, s);
                Check("Back below 59pt band " + tag, chrome.Back.yMin >= 59f * pt - 0.5f, chrome.Back.ToString());
                Check("balance below 59pt band " + tag, chrome.Coins.yMin >= 59f * pt - 0.5f, chrome.Coins.ToString());
                Check("Back >= 44pt " + tag, chrome.Back.width >= 44f * pt - 0.5f && chrome.Back.height >= 44f * pt - 0.5f, chrome.Back.ToString());
                Check("PAY TABLE >= 44pt tall " + tag, chrome.PayTab.height >= 44f * pt - 0.5f, chrome.PayTab.ToString());
                var fives = FlockFiveApp.PokerFivesIconBox(chrome.PayTab, s, w);
                Check("trophy >= 44pt " + tag, fives.width >= 44f * pt - 0.5f, fives.ToString());
                FlockFiveApp.FivesIconParts(fives, out _, out var pill);
                int floorPx = FlockFiveApp.PokerTextFloorPx(s);
                Check("text floor >= 12pt " + tag, floorPx >= 12f * pt - 0.01f, floorPx.ToString());
                Check("badge pill holds 12pt " + tag, pill.height * 0.70f >= floorPx * 0.95f, pill.height.ToString("0"));
                Check("PAY TABLE clear of logo " + tag, chrome.PayTab.yMin >= chrome.Below, (chrome.PayTab.yMin - chrome.Below).ToString("0"));
                Check("Back clear of logo " + tag, !chrome.Back.Overlaps(chrome.Title), "");
            }
            DeviceFit.TestWindow = null;
        }
    }
}
#endif

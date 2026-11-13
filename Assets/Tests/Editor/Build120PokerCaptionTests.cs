#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace FlockFive.Editor
{
    // b120 R4 (Readability): the "Tap poker" line seats below the Poker + VIP icon row and off the glove.
    public static class Build120PokerCaptionTests
    {
        const string Out = "/tmp/flock-five-b120-poker-caption-tests.txt";
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            void Check(string name, bool ok, string detail)
            {
                sb.AppendLine((ok ? "PASS  " : "FAIL  ") + name + "  " + detail);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[build120-pokercap] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
            }
            // 820x1180 @2 shape from the inspector stills: poker on the right rail, VIP medal + ribbon on the left.
            var poker = new Rect(1420f, 520f, 170f, 170f);
            var vip = new Rect(50f, 560f, 170f, 170f);
            var ribbon = new Rect(57f, 700f, 156f, 50f);
            var hand = FlockFiveApp.PokerPointHand(new Vector2(poker.x + poker.width * 0.56f, poker.y + poker.height * 0.8f), 2f);
            var icons = new[] { poker, default(Rect), default(Rect), vip, ribbon };
            float gap = 16f;
            var band = new Rect(0f, poker.yMax + gap, 1640f, 210f);
            float y = FlockFiveApp.PokerCoinsSeat(band, icons, hand, 200f, gap);
            var seat = new Rect(band.x, y, band.width, band.height);
            Check("below poker", seat.yMin >= poker.yMax, y.ToString());
            Check("below VIP medal", seat.yMin >= vip.yMax, y.ToString());
            Check("below No Ads ribbon", seat.yMin >= ribbon.yMax, y.ToString());
            Check("off landed glove", !seat.Overlaps(hand), hand + " " + seat);
            foreach (var r in icons) if (r.width > 2f) Check("no icon overlap " + r, !seat.Overlaps(r), seat.ToString());
            // Clear band stays put.
            var clear = new Rect(0f, 900f, 1640f, 200f);
            Check("clear band unchanged", Mathf.Abs(FlockFiveApp.PokerCoinsSeat(clear, icons, default, 0f, gap) - 900f) < 0.01f, "");
            // minTop floor wins.
            Check("minTop floor", FlockFiveApp.PokerCoinsSeat(new Rect(0f, 10f, 100f, 10f), null, default, 50f, gap) >= 50f, "");
        }
    }
}
#endif

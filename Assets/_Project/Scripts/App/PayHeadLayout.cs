using UnityEngine;

namespace FlockFive
{
    // Build 89: the open pay table's header ("PAY TABLE" over "BET $25"), sized from
    // the panel's own header band, not from screen size. Pure rect math so it is the
    // same on iPhone, iPad and Android, and testable outside OnGUI.
    //   Title band: top TitleShare of the header, text fills TitleFill of it.
    //   Bet band:   right under the title, BetShare of the header, above the rule.
    // The header band itself is the one DrawPokerPayTable already reserves, so the
    // rows keep their height and the panel keeps its seat under the PAY TABLE tab.
    public static class PayHeadLayout
    {
        public const float TitleShare = 0.60f;
        public const float BetShare = 0.34f;
        public const float TitleFill = 0.94f;
        public const float BetFill = 0.96f;
        public const float TitleWidth = 0.86f;
        public const float BetWidth = 0.70f;
        // The bet line stays visibly second: never above this share of the title px.
        public const float BetToTitle = 0.82f;
        public const int TitleMinPx = 24;
        public const int TitleMaxPx = 160;
        public const int BetMinPx = 16;

        public struct Bands
        {
            public Rect Title;
            public Rect Bet;
            public float RuleY;
            public float TitleMaxW, TitleMaxH, BetMaxW, BetMaxH;
        }

        // paper: the drawn card-paper rect; headH: header band height; rule: rule thickness.
        public static Bands Layout(Rect paper, float headH, float rule)
        {
            var b = new Bands();
            float ruleY = paper.y + headH - rule;
            float top = paper.y + rule; // polish 2: title takes the room, only a hairline above
            float room = Mathf.Max(1f, ruleY - top);
            float titleH = room * TitleShare / (TitleShare + BetShare);
            float betH = room - titleH;
            b.Title = new Rect(paper.x, top, paper.width, titleH);
            b.Bet = new Rect(paper.x, top + titleH, paper.width, betH);
            b.RuleY = ruleY;
            b.TitleMaxW = paper.width * TitleWidth;
            b.TitleMaxH = titleH * TitleFill;
            b.BetMaxW = paper.width * BetWidth;
            b.BetMaxH = betH * BetFill;
            return b;
        }

        public static int BetCap(int titlePx) =>
            Mathf.Max(BetMinPx, Mathf.FloorToInt(titlePx * BetToTitle));
    }
}

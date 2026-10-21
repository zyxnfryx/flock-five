using UnityEngine;

namespace FlockFive
{
    // b130 badger-stack layout pass (Brandon): one CoachArrow helper, the shared PowerRowGap, one caption-band helper,
    // two-lane score columns, the shared reward banner for the win. Pure rect maths are static so the rect tests share them.
    public partial class FlockFiveApp
    {
        // ---- (h) double-thick columns ----
        public const float BadgerColumnWide = 1.25f;

        // ---- (c) power row gap: ~14 px at 390x844, scaled with the layout like the comb gap ----
        public const float PowerRowGapRef = 14f;
        public static float BadgerPowerRowGap(float s) => PowerRowGapRef * s / BadgerFighterPlace.CombGapRefUi;

        // ---- (f) caption band ----
        // Bottom margin under the caption band: the safe-area bottom inset plus breathing room (never less than 12 px at Ui 1).
        public static float BadgerCaptionSafeMargin(float screenH, Rect safe, float s)
        {
            bool have = safe.width >= 2f && safe.height >= 2f;
            float inset = have ? Mathf.Max(0f, safe.yMin) : 0f;
            return Mathf.Max(12f * s, inset + 8f * s);
        }

        // The one caption seat inside a caption band: even side/top/bottom padding so 'Tap to continue.' never touches
        // the box edge, the power row above or the screen bottom below.
        public static Rect BadgerCaptionSeat(Rect band, float s)
        {
            float padX = 10f * s, padY = 6f * s;
            float w = Mathf.Max(8f, band.width - padX * 2f);
            float h = Mathf.Max(8f, band.height - padY * 2f);
            return new Rect(band.x + padX, band.y + padY, w, h);
        }

        // Room kept for the coin amount left of the icon ($99,999 at the shared CoinBalanceType size).
        public static float BadgerCoinPillTextW(float icon) => icon * CoinBalanceType * 3.6f;

        // b130 (f): the biggest caption font (<= hi) at which every authored row fits the text rect on one line and all rows
        // fit its height, so the box never grows past the caption band and 'Tap to continue.' is never pushed off-screen.
        public static int BadgerCaptionHi(string line, float maxW, float roomH, float scale, int hi)
        {
            if (string.IsNullOrEmpty(line)) return hi;
            CaptionTextRect(new Rect(0f, 0f, maxW, roomH), scale, out var textR);
            var st = CoachLineStyle();
            int keep = st.fontSize; bool wrap = st.wordWrap;
            var content = new GUIContent(line);
            int lo = Mathf.Max(12, CaptionPx(12));
            int f = hi;
            try
            {
                st.wordWrap = false;
                for (; f > lo; f--)
                {
                    st.fontSize = f;
                    var size = st.CalcSize(content);
                    if (size.x <= textR.width && size.y <= textR.height) break;
                }
            }
            finally { st.fontSize = keep; st.wordWrap = wrap; }
            return f;
        }

        // b130 (f) test hook: the lesson caption as PaintCoachCaption really sizes it (lesson floor font, wraps at the gold
        // width, paragraph gap kept): the box height it needs vs the room the caption seat gives it.
        public static bool BadgerLessonCaptionRoom(string line, float screenW, float screenH, Rect safe, out float need, out float have, out string detail)
        {
            BadgerCaptionLimits(screenW, screenH, safe, out float maxW, out float roomH, out float scale, out int hi);
            CaptionTextRect(new Rect(0f, 0f, maxW, roomH), scale, out var textR);
            float padH = roomH - textR.height;
            var fit = FitLessonText(line, textR.width, 99999f, 12, hi, screenW, screenH);
            need = fit.TextH + padH;
            have = roomH;
            detail = screenW + "x" + screenH + " need " + need.ToString("0") + " have " + have.ToString("0") + " font " + fit.Font + " [" + (fit.Shown ?? "").Replace("\n", "/") + "]";
            return need <= have + 0.5f;
        }

        // ---- (a/b/g) CoachArrow ----
        public const float CoachArrowSmall = 0.6f;
        public static float CoachArrowK(float s) => Mathf.Clamp(s, 1f, 2f) * CoachArrowSmall;

        // Everything the arrow can paint over one bob cycle (outline included).
        public static Rect CoachArrowRect(Vector2 tip, float s)
        {
            float k = CoachArrowK(s);
            float w = CoachArrowWidth * k + 5f * k, h = CoachArrowHeight * k, amp = CoachArrowBobAmp * k, rim = 2.5f * k;
            return new Rect(tip.x - w * 0.5f, tip.y - h - amp - rim, w, h + amp * 2f + rim * 2f);
        }

        // Small down-arrow with the gentle shared bob; tip is the rest position of the point.
        static void DrawCoachArrow(Vector2 tip, float s)
        {
            if (!GuiPaint()) return;
            float k = CoachArrowK(s);
            tip.y += CoachArrowBobOffset(AdPause.Unscaled, k);
            var prevC = GUI.color;
            GUI.color = new Color(0.10f, 0.06f, 0.02f, 0.92f);
            CoachArrowFill(tip, k, 2.5f * k);
            GUI.color = new Color(1f, 0.86f, 0.30f, 1f);
            CoachArrowFill(tip, k, 0f);
            GUI.color = prevC;
        }

        readonly Vector2[] _bgArrowTips = new Vector2[4];

        // Lesson arrow tips. 0: two above the player grid. 1: one centred over the power row. 2: one over each score
        // column's end plate, both at once.
        public static int BadgerCoachArrowTips(BadgerRects L, int step, int youTarget, int bossTarget, Vector2[] into)
        {
            float s = L.S > 0.01f ? L.S : 1f;
            float k = CoachArrowK(s);
            float amp = CoachArrowBobAmp * k + 2.5f * k;
            float h = CoachArrowHeight * k;
            if (step <= 0)
            {
                float y = L.PlayerGrid.y - amp - 2f * s;
                into[0] = new Vector2(L.PlayerGrid.x + L.PlayerGrid.width * 0.36f, y);
                into[1] = new Vector2(L.PlayerGrid.x + L.PlayerGrid.width * 0.64f, y);
                return 2;
            }
            if (step == 1)
            {
                into[0] = new Vector2(L.Power.center.x, L.Power.y - amp - 1f * s);
                return 1;
            }
            int max = BadgerSchedule.BadgerTargetFixed;
            float unit = BadgerMeter.Unit(L.ColL, max);
            into[0] = ColumnTip(L.ColL, youTarget, unit, s, h, amp);
            into[1] = ColumnTip(L.ColR, bossTarget, unit, s, h, amp);
            return 2;
        }

        static Vector2 ColumnTip(Rect col, int target, float unit, float s, float h, float amp)
        {
            var plate = BadgerMeter.EndPlate2(col, target, unit);
            float y = plate.y - amp - 2f * s;
            float minY = col.y + h + amp;
            if (y < minY) y = minY;
            return new Vector2(col.center.x, y);
        }

        // ---- (j) win banner ----
        int _bgWinCoins;
        float _bgWinAt;

        bool BadgerWinBannerLive()
            => _bgStage == BadgerStage.Over && _bgFight != null && _bgFight.Result == BadgerResult.PlayerWon;

        // Panel above Continue, centred on the board; never touches Continue.
        public static Rect BadgerWinPanelRect(BadgerRects L, Rect cont)
        {
            float s = L.S > 0.01f ? L.S : 1f;
            float w = Mathf.Min(L.Power.width * 0.92f, 440f * s);
            float h = Mathf.Clamp(w * 0.46f, 120f * s, 200f * s);
            float bottom = cont.y - 18f * s;
            float cy = (L.BossGrid.y + bottom) * 0.5f;
            float y = cy - h * 0.5f;
            if (y + h > bottom) y = bottom - h;
            return new Rect(L.Power.center.x - w * 0.5f, y, w, h);
        }

        void DrawBadgerWinBanner(BadgerRects L)
        {
            float s = L.S;
            var cont = BadgerContinueRect(L);
            float age = AdPause.Unscaled - _bgWinAt;
            float fade = Mathf.Clamp01(age / 0.25f);
            // Dim the board, leaving Continue bright.
            var c = new Color(0f, 0f, 0f, 0.55f * fade);
            float W = Screen.width, H = Screen.height;
            DrawRewardDim(new Rect(0f, 0f, W, cont.y), c);
            DrawRewardDim(new Rect(0f, cont.yMax, W, H - cont.yMax), c);
            DrawRewardDim(new Rect(0f, cont.y, cont.x, cont.height), c);
            DrawRewardDim(new Rect(cont.xMax, cont.y, W - cont.xMax, cont.height), c);
            var panel = BadgerWinPanelRect(L, cont);
            float pop = 0.85f + 0.15f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.30f));
            panel = new Rect(panel.center.x - panel.width * pop * 0.5f, panel.center.y - panel.height * pop * 0.5f, panel.width * pop, panel.height * pop);
            int coins = Mathf.RoundToInt(_bgWinCoins * Mathf.Clamp01((age - 0.25f) / 0.9f));
            DrawRewardBanner(panel, BadgerCopy.Win, _bgWinCoins > 0 ? coins : -1, age, s, fade);
        }

        static void DrawRewardDim(Rect r, Color c)
        {
            if (r.width < 1f || r.height < 1f) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        }

        // Shared reward banner: solid wood panel with a gold rim, bright cream title with a dark outline, then the coin
        // reward (+N and the coin icon) in gold. coins < 0 hides the reward row. Corner sparkles breathe.
        static void DrawRewardBanner(Rect panel, string title, int coins, float age, float s, float alpha)
        {
            if (!GuiPaint() || alpha < 0.02f) return;
            var white = Texture2D.whiteTexture;
            float rad = Mathf.Min(panel.width, panel.height) * 0.16f;
            float rim = Mathf.Max(3f, 4f * s);
            var prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(panel.x, panel.y + rim * 1.6f, panel.width, panel.height), white, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.40f * alpha), 0f, rad);
            GUI.DrawTexture(panel, white, ScaleMode.StretchToFill, true, 0f, new Color(0.98f, 0.78f, 0.28f, alpha), 0f, rad);
            var inner = new Rect(panel.x + rim, panel.y + rim, panel.width - rim * 2f, panel.height - rim * 2f);
            float ir = Mathf.Max(1f, rad - rim);
            GUI.DrawTexture(inner, white, ScaleMode.StretchToFill, true, 0f, new Color(0.34f, 0.17f, 0.06f, alpha), 0f, ir);
            GUI.DrawTexture(new Rect(inner.x, inner.y, inner.width, inner.height * 0.5f), white, ScaleMode.StretchToFill, true, 0f,
                new Color(0.48f, 0.26f, 0.10f, alpha), Vector4.zero, new Vector4(ir, ir, 0f, 0f));
            GUI.color = prev;

            var st = BadgerStyle();
            bool reward = coins >= 0;
            var tr = new Rect(inner.x + inner.width * 0.06f, inner.y + inner.height * (reward ? 0.10f : 0.25f), inner.width * 0.88f, inner.height * (reward ? 0.40f : 0.50f));
            st.fontSize = FitFont(st, title, tr.width, tr.height, 12, 64);
            StampOutlined(tr, title, st, new Color(1f, 0.95f, 0.80f, alpha), 0, Mathf.Max(2, st.fontSize / 9));
            if (reward)
            {
                string amt = "+" + coins;
                var row = new Rect(inner.x, inner.y + inner.height * 0.52f, inner.width, inner.height * 0.38f);
                st.fontSize = FitFont(st, "+" + Mathf.Max(coins, 999), row.width * 0.5f, row.height, 12, 72);
                var size = st.CalcSize(new GUIContent(amt));
                float icon = row.height * 0.92f;
                float total = size.x + 8f * s + icon;
                var ar = new Rect(row.center.x - total * 0.5f, row.y, size.x + 4f, row.height);
                StampOutlined(ar, amt, st, new Color(1f, 0.86f, 0.30f, alpha), 0, Mathf.Max(2, st.fontSize / 9));
                var coin = SpriteCatalog.Coin;
                var ic = new Rect(ar.x + size.x + 8f * s, row.center.y - icon * 0.5f, icon, icon);
                if (coin != null && coin.texture != null)
                {
                    GUI.color = new Color(1f, 1f, 1f, alpha);
                    var tc = coin.textureRect;
                    var tex = coin.texture;
                    GUI.DrawTextureWithTexCoords(ic, tex, new Rect(tc.x / tex.width, tc.y / tex.height, tc.width / tex.width, tc.height / tex.height), true);
                    GUI.color = prev;
                }
            }
            float tw = 0.5f + 0.5f * Mathf.Sin(age * 5f);
            float gs = Mathf.Max(10f, panel.height * 0.20f);
            DrawBadgerGlint(new Vector2(panel.x + rad * 0.6f, panel.y + rad * 0.5f), gs, alpha * (0.35f + 0.65f * tw));
            DrawBadgerGlint(new Vector2(panel.xMax - rad * 0.6f, panel.y + rad * 0.5f), gs * 0.8f, alpha * (1f - 0.65f * tw));
            DrawBadgerGlint(new Vector2(panel.xMax - rad * 0.5f, panel.yMax - rad * 0.5f), gs * 0.7f, alpha * (0.35f + 0.65f * tw));
        }
    }
}

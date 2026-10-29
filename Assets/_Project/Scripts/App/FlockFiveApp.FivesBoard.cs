using UnityEngine;

namespace FlockFive
{
    // b119 (Brandon): "Five of a Kind" trophy board. A ★ FIVES n/16 chip left of the PAY TABLE
    // tab opens a board of every five-of-a-kind slot: 5 colors × 3 looks + FIVE WILDS.
    // Unhit = dark silhouette + "?"; hit = full card, ×count · first date, ★ if ever natural.
    // Record lives in BirdPoker (flockfive.poker.fives.v1). FULLCARD never clears punch.v1,
    // so the board is lifetime: after a FULLCARD every slot stays lit and counts keep climbing.
    public partial class FlockFiveApp
    {
        bool _fivesOpen;
        float _fivesAnim;
        bool _fivesChipHeld;
        GUIStyle _fivesChipSt, _fivesTitleSt, _fivesSubSt, _fivesCapSt, _fivesQSt;

        public const string FivesChipPrefix = "★ FIVES ";

        public static string FivesChipLabel(int found) => FivesChipPrefix + found + "/" + BirdPoker.PunchKinds;

        // Same size as the PAY TABLE tab, sitting just left of it.
        public static Rect PokerFivesChipBox(Rect payTab, float s)
        {
            float w = payTab.width;
            return new Rect(payTab.x - 8f * s - w, payTab.y, w, payTab.height);
        }

        // No hand in play: idle (fresh or showing a settled result), nothing animating, no overlay.
        public static bool FivesChipTappable(BirdPoker.Phase phase, bool motionBusy, bool stamp, bool payOpen)
            => phase == BirdPoker.Phase.Idle && !motionBusy && !stamp && !payOpen;

        bool FivesChipLive() =>
            FivesChipTappable(BirdPoker.PhaseNow, PokerMotionBusy(), _pokerStamp, _pokerPayOpen || _pokerPayAnim > 0.05f)
            && !PokerWinFlying();

        // Hit-test before cards / DEAL so the open board swallows taps.
        void TickPokerFives(Rect payTab, float s)
        {
            if (GuiPaint())
                _fivesAnim = Mathf.MoveTowards(_fivesAnim, _fivesOpen ? 1f : 0f, AdPause.UnscaledDelta * 7f);
            if (_fivesOpen)
            {
                _fivesChipHeld = false;
                if (_fivesAnim > 0.55f && HitPad(new Rect(0f, 0f, PhoneArea.W, PhoneArea.H), out _))
                {
                    _fivesOpen = false;
                    Sfx.Chirp(BirdColor.Gold);
                }
                return;
            }
            var chip = PokerFivesChipBox(payTab, s);
            if (!FivesChipLive()) { _fivesChipHeld = false; return; }
            if (HitPad(chip, out bool held))
            {
                _fivesOpen = true;
                Sfx.Chirp(BirdColor.Gold);
            }
            _fivesChipHeld = held;
        }

        void DrawPokerFivesChip(Rect payTab, float s)
        {
            if (!GuiPaint()) return;
            var tab = PokerFivesChipBox(payTab, s);
            bool live = FivesChipLive() || _fivesOpen;
            float sink = _fivesChipHeld ? 2f : 0f;
            var chip = new Rect(tab.x, tab.y + sink, tab.width, tab.height - sink);
            float a = live ? 1f : 0.55f;
            GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.45f * a);
            GUI.DrawTexture(new Rect(chip.x + 2f, chip.y + 3f, chip.width, chip.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.58f, 0.18f, 0.92f * a);
            GUI.DrawTexture(chip, Texture2D.whiteTexture);
            GUI.color = new Color(0.62f, 0.12f, 0.16f, 0.96f * a);
            GUI.DrawTexture(new Rect(chip.x + 3f * s, chip.y + 3f * s, chip.width - 6f * s, chip.height - 6f * s), Texture2D.whiteTexture);
            GUI.color = new Color(0.88f, 0.28f, 0.32f, 0.55f * a);
            GUI.DrawTexture(new Rect(chip.x + 5f * s, chip.y + 4f * s, chip.width - 10f * s, chip.height * 0.38f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (_fivesChipSt == null)
                _fivesChipSt = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            string lab = FivesChipLabel(BirdPoker.FivesFound());
            _fivesChipSt.fontSize = FitFont(_fivesChipSt, lab, chip.width * 0.90f, chip.height * 0.72f, 10, 26);
            StampLight(chip, lab, _fivesChipSt, new Color(1f, 0.94f, 0.72f, a));
        }

        // Board geometry, shared by the draw and the editor stills log.
        public struct FivesBoardGeo
        {
            public Rect Panel;
            public float Card, CardH, ColW, Gap, GridTop, RowH;
        }

        public static FivesBoardGeo LayoutFivesBoard(float w, float h, Rect safe, float s)
        {
            float pw = Mathf.Min(w * 0.92f, 560f * s);
            float top = Mathf.Max(safe.yMin + 16f * s, h * 0.07f);
            float bot = Mathf.Min(safe.yMax - 16f * s, h * 0.94f);
            var panel = new Rect((w - pw) * 0.5f, top, pw, bot - top);
            float gap = 10f * s;
            float colW = (pw - 32f * s - gap * 2f) / 3f;
            float gridTop = panel.y + 92f * s;
            float avail = panel.yMax - 34f * s - gridTop;
            float capH = 16f * s;
            float cardH = avail / 6f - capH - 4f * s;
            float aspect = 1704f / 1200f;
            float cardW = Mathf.Min(cardH / aspect, colW * 0.92f);
            cardH = cardW * aspect;
            return new FivesBoardGeo { Panel = panel, Card = cardW, CardH = cardH, ColW = colW, Gap = gap, GridTop = gridTop, RowH = cardH + capH + 4f * s };
        }

        static readonly string[] FivesLookNames = { "PLAIN", "BOW", "CROWN" };

        public static string FivesCaption(BirdPoker.FiveRecord r)
            => "×" + r.Count + " · " + BirdPoker.DayLabel(r.FirstDay) + (r.Natural ? " ★" : "");

        void DrawPokerFivesBoard(float s)
        {
            if (!GuiPaint()) return;
            if (_fivesAnim <= 0.01f && !_fivesOpen) return;
            float u = _fivesAnim;
            ModalBlock.Paint(ModalBlock.Id.PokerFives, new Color(0.04f, 0.03f, 0.02f, 0.62f * u));
            var g = LayoutFivesBoard(PhoneArea.W, PhoneArea.H, PhoneArea.Safe, s);
            var prev = GUI.matrix;
            float k = Mathf.Lerp(0.9f, 1f, u);
            GUIUtility.ScaleAroundPivot(new Vector2(k, k), g.Panel.center);
            var p = g.Panel;
            GUI.color = new Color(0.77f, 0.55f, 0.16f, u);
            GUI.DrawTexture(p, Texture2D.whiteTexture);
            GUI.color = new Color(0.98f, 0.94f, 0.84f, u);
            GUI.DrawTexture(new Rect(p.x + 5f * s, p.y + 5f * s, p.width - 10f * s, p.height - 10f * s), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (_fivesTitleSt == null)
            {
                _fivesTitleSt = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
                _fivesSubSt = new GUIStyle(_fivesTitleSt);
                _fivesCapSt = new GUIStyle(_fivesTitleSt);
                _fivesQSt = new GUIStyle(_fivesTitleSt);
            }
            const string title = "FIVE OF A KIND";
            _fivesTitleSt.fontSize = FitFont(_fivesTitleSt, title, p.width * 0.78f, 40f * s, 14, 64);
            StampOutlined(new Rect(p.x, p.y + 10f * s, p.width, 42f * s), title, _fivesTitleSt, new Color(1f, 0.84f, 0.27f, u), 0, 2);
            int found = BirdPoker.FivesFound();
            string sub = found + " / " + BirdPoker.PunchKinds + " found   ·   lifetime hits " + BirdPoker.FivesHits();
            _fivesSubSt.fontSize = FitFont(_fivesSubSt, sub, p.width * 0.86f, 20f * s, 9, 30);
            _fivesSubSt.normal.textColor = new Color(0.43f, 0.27f, 0.12f, u);
            GUI.Label(new Rect(p.x, p.y + 52f * s, p.width, 20f * s), sub, _fivesSubSt);

            float gx = p.x + 16f * s;
            _fivesCapSt.fontSize = Mathf.Max(8, Mathf.RoundToInt(12f * s));
            for (int c = 0; c < 3; c++)
            {
                _fivesCapSt.normal.textColor = new Color(0.55f, 0.35f, 0.16f, u);
                GUI.Label(new Rect(gx + c * (g.ColW + g.Gap), g.GridTop - 18f * s, g.ColW, 16f * s), FivesLookNames[c], _fivesCapSt);
            }
            for (int color = 0; color < Palette.Max; color++)
                for (int look = 0; look < 3; look++)
                {
                    int kind = BirdPoker.KindId((BirdColor)color, (BirdSex)look);
                    float cx = gx + look * (g.ColW + g.Gap) + g.ColW * 0.5f;
                    DrawFivesSlot(kind, cx, g.GridTop + color * g.RowH, g, s, u);
                }
            DrawFivesSlot(BirdPoker.WildKind, p.center.x, g.GridTop + Palette.Max * g.RowH, g, s, u);

            string foot = BirdPoker.FullcardCount > 0
                ? "FULLCARDS won " + BirdPoker.FullcardCount + "  ·  every hit still counts"
                : "All 16 = FULLCARD " + Money.Format(BirdPoker.FullcardPrize);
            _fivesSubSt.fontSize = FitFont(_fivesSubSt, foot, p.width * 0.86f, 18f * s, 9, 26);
            GUI.Label(new Rect(p.x, p.yMax - 30f * s, p.width, 20f * s), foot, _fivesSubSt);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        void DrawFivesSlot(int kind, float cx, float y, FivesBoardGeo g, float s, float u)
        {
            var rec = BirdPoker.Five(kind);
            bool on = rec.Count > 0;
            Sprite spr;
            if (BirdPoker.IsWildKind(kind)) spr = SpriteCatalog.PokerFace(BirdPoker.Card.MakeWild());
            else
            {
                BirdPoker.KindParts(kind, out BirdColor c, out BirdSex sex);
                spr = SpriteCatalog.PokerFace(BirdPoker.Card.Of(c, sex));
            }
            var r = new Rect(cx - g.Card * 0.5f, y, g.Card, g.CardH);
            if (spr != null && spr.texture != null)
            {
                GUI.color = on ? new Color(1f, 1f, 1f, u) : new Color(0.30f, 0.28f, 0.26f, u);
                GUI.DrawTexture(r, spr.texture, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
            if (!on)
            {
                _fivesQSt.fontSize = Mathf.Max(10, Mathf.RoundToInt(g.CardH * 0.40f));
                StampOutlined(r, "?", _fivesQSt, new Color(0.95f, 0.91f, 0.82f, u), 0, 1);
            }
            string cap;
            if (on) cap = FivesCaption(rec);
            else if (BirdPoker.IsWildKind(kind)) cap = "FIVE WILDS · 250×";
            else
            {
                BirdPoker.KindParts(kind, out BirdColor c, out BirdSex sex);
                cap = c.ToString().ToUpperInvariant() + " " + FivesLookNames[(int)sex];
            }
            _fivesCapSt.fontSize = FitFont(_fivesCapSt, cap, g.ColW + g.Gap * 0.5f, 15f * s, 7, 22);
            _fivesCapSt.normal.textColor = on ? new Color(0.22f, 0.44f, 0.16f, u) : new Color(0.58f, 0.53f, 0.45f, u);
            GUI.Label(new Rect(cx - g.ColW * 0.5f - g.Gap * 0.25f, r.yMax + 2f * s, g.ColW + g.Gap * 0.5f, 15f * s), cap, _fivesCapSt);
        }
    }
}

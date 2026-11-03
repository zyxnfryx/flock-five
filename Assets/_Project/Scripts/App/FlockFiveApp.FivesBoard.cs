using UnityEngine;

namespace FlockFive
{
    // b119 (Brandon): "Five of a Kind" trophy board. A trophy medallion (found/16 pill) under the PAY TABLE
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

        // b119 Option A (Brandon): a round trophy medallion with a found/16 pill, directly under PAY TABLE,
        // right edges aligned. 44pt tap target, no button chrome.
        public const float FivesIconPt = 44f;

        public static string FivesBadgeLabel(int found) => found + "/" + BirdPoker.PunchKinds;

        // Medallion centred at the top of the 44pt box; found/16 pill centred under it, overlapping the bottom rim.
        public static void FivesIconParts(Rect box, out Rect disc, out Rect pill)
        {
            float md = box.width * 0.80f;
            disc = new Rect(box.center.x - md * 0.5f, box.y, md, md);
            float ph = box.height * 0.36f, pw = box.width * 0.70f;
            pill = new Rect(box.center.x - pw * 0.5f, Mathf.Min(disc.yMax - ph * 0.40f, box.yMax - ph), pw, ph);
        }

        // Uniform padding (Brandon): same right inset as PAY TABLE, and the gap PAY TABLE -> icon equals
        // PAY TABLE's own gap from the top of the safe area.
        // b119 Keep (Brandon): trophy "nudged down a smidge away from the pay table button" (~14-16px on device).
        public const float FivesIconExtraGap = 2.5f;

        public static Rect PokerFivesIconBox(Rect payTab, float s, float screenW)
        {
            float d = FivesIconPt * s;
            // Snug, uniform padding: PAY->icon gap equals PAY TABLE's right inset.
            float gap = Mathf.Max(0f, screenW - payTab.xMax) + FivesIconExtraGap * s;
            return new Rect(payTab.xMax - d, payTab.yMax + gap, d, d);
        }

        public static Rect PokerFivesIconBox(Rect payTab, float s) => PokerFivesIconBox(payTab, s, PhoneArea.W);

        // No hand in play: idle (fresh or showing a settled result), nothing animating, no overlay.
        public static bool FivesChipTappable(BirdPoker.Phase phase, bool motionBusy, bool stamp, bool payOpen)
            => phase == BirdPoker.Phase.Idle && !motionBusy && !stamp && !payOpen;

        bool FivesChipLive() =>
            FivesChipTappable(BirdPoker.PhaseNow, PokerMotionBusy(), _pokerStamp, _pokerPayOpen || _pokerPayAnim > 0.05f)
            && !PokerWinFlying();

        // b119 hardening: the board belongs to the poker page. Any screen away from it
        // (Splash, Hive, Badger, or a level) closes it so a stale PokerFives modal can't linger.
        public static bool FivesMayStayOpen(bool splash, bool onPoker) => splash && onPoker;

        // Called once per frame from UpdateBody: the one place every screen change passes through.
        void ClearFivesOffPoker()
        {
            if (!_fivesOpen && _fivesAnim <= 0f) return;
            if (FivesMayStayOpen(_splash, _home == HomeFace.Poker)) return;
            _fivesOpen = false; _fivesAnim = 0f; _fivesChipHeld = false;
        }

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
            var chip = PokerFivesIconBox(payTab, s);
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
            var box = PokerFivesIconBox(payTab, s);
            _fivesIconLast = box; _fivesPayLast = payTab;
            bool live = FivesChipLive() || _fivesOpen;
            float a = live ? 1f : 0.55f;
            float sink = _fivesChipHeld ? 2f * s : 0f;
            FivesIconParts(box, out var discR, out var pillR);
            float md = discR.width;
            var disc = new Rect(discR.x, discR.y + sink, md, md);
            GUI.color = new Color(0.05f, 0.03f, 0.01f, 0.45f * a);
            GUI.DrawTexture(new Rect(disc.x + 1.5f * s, disc.y + 2.5f * s, md, md), FivesMedalTex(), ScaleMode.StretchToFill, true);
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(disc, FivesMedalTex(), ScaleMode.StretchToFill, true);
            float ph = pillR.height, pw = pillR.width;
            var pill = new Rect(pillR.x, pillR.y + sink, pw, ph);
            GUI.DrawTexture(pill, FivesPillTex(), ScaleMode.StretchToFill, true);
            if (_fivesChipSt == null)
                _fivesChipSt = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            string lab = FivesBadgeLabel(BirdPoker.FivesFound());
            _fivesChipSt.fontSize = FitFont(_fivesChipSt, lab, pw * 0.78f, ph * 0.70f, 6, 40);
            _fivesChipSt.normal.textColor = new Color(1f, 0.95f, 0.80f, a);
            GUI.Label(pill, lab, _fivesChipSt);
            GUI.color = Color.white;
        }

        static Texture2D _fivesMedalTex, _fivesPillTex;
        Rect _fivesIconLast, _fivesPayLast, _pokerRowLast;

        static float FivesAA(float d, float px) => Mathf.Clamp01(0.5f - d / px);

        static Color FivesOver(Color dst, Color src, float k)
        {
            k *= src.a;
            return new Color(Mathf.Lerp(dst.r, src.r, k), Mathf.Lerp(dst.g, src.g, k), Mathf.Lerp(dst.b, src.b, k), dst.a + (1f - dst.a) * k);
        }

        // Dark-wood medallion, gold rim, gold cup, cream star. CPU painted once.
        static Texture2D FivesMedalTex()
        {
            if (_fivesMedalTex != null) return _fivesMedalTex;
            const int N = 192;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            float pix = 2f / N;
            var gold = new Color(0.93f, 0.72f, 0.24f, 1f);
            var goldDk = new Color(0.62f, 0.42f, 0.10f, 1f);
            var goldHi = new Color(1f, 0.90f, 0.55f, 1f);
            var cream = new Color(1f, 0.96f, 0.84f, 1f);
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                var c = new Color(0, 0, 0, 0);
                // gold rim
                float rimK = FivesAA(r - 0.98f, pix);
                float lit = Mathf.Clamp01(0.5f + 0.5f * (v * 0.7f - u * 0.3f));
                c = FivesOver(c, Color.Lerp(goldDk, goldHi, lit), rimK);
                // wood face
                float faceK = FivesAA(r - 0.82f, pix);
                float grain = 0.5f + 0.5f * Mathf.Sin((v + 0.08f * Mathf.Sin(u * 7f)) * 46f);
                var wood = Color.Lerp(new Color(0.20f, 0.11f, 0.05f, 1f), new Color(0.33f, 0.19f, 0.09f, 1f), grain * 0.6f + (1f - r) * 0.4f);
                c = FivesOver(c, wood, faceK);
                // inner gold hairline
                float lineK = FivesAA(Mathf.Abs(r - 0.82f) - 0.018f, pix);
                c = FivesOver(c, gold, lineK * 0.9f);
                // cup bowl: v 0.00..0.52, half-width 0.40 at top tapering round to bottom
                float bowlTop = 0.52f, bowlBot = -0.06f;
                float tv = Mathf.Clamp01((v - bowlBot) / (bowlTop - bowlBot));
                float hw = 0.40f * Mathf.Sqrt(Mathf.Clamp01(1f - (1f - tv) * (1f - tv)));
                float bowl = Mathf.Max(Mathf.Abs(u) - hw, Mathf.Max(v - bowlTop, bowlBot - v));
                // handles: rings either side
                float hr = Mathf.Abs(Mathf.Sqrt((Mathf.Abs(u) - 0.40f) * (Mathf.Abs(u) - 0.40f) + (v - 0.30f) * (v - 0.30f)) - 0.13f) - 0.035f;
                float handle = Mathf.Abs(u) > 0.36f ? hr : 9f;
                float stem = Mathf.Max(Mathf.Abs(u) - 0.06f, Mathf.Max(v - -0.04f, -0.30f - v));
                float plinth = Mathf.Max(Mathf.Abs(u) - 0.17f, Mathf.Max(v - -0.30f, -0.38f - v));
                float foot = Mathf.Max(Mathf.Abs(u) - 0.27f, Mathf.Max(v - -0.40f, -0.50f - v));
                float cup = Mathf.Min(Mathf.Min(bowl, handle), Mathf.Min(stem, Mathf.Min(plinth, foot)));
                float cupK = FivesAA(cup, pix);
                float shade = Mathf.Clamp01(0.55f - u * 0.9f + v * 0.25f);
                c = FivesOver(c, Color.Lerp(goldDk, goldHi, shade), cupK);
                // star on the bowl
                float sx = u, sy = v - 0.25f;
                float ang = Mathf.Atan2(sy, sx) - Mathf.PI * 0.5f;
                float seg = Mathf.PI * 2f / 5f;
                float m = Mathf.Repeat(ang + seg * 0.5f, seg) - seg * 0.5f;
                float ro = 0.20f, ri = 0.085f;
                float lim = Mathf.Lerp(ro, ri, Mathf.Abs(m) / (seg * 0.5f));
                float sr = Mathf.Sqrt(sx * sx + sy * sy);
                c = FivesOver(c, cream, FivesAA(sr - lim, pix) * cupK);
                px[y * N + x] = c;
            }
            t.SetPixels(px);
            t.Apply(false, true);
            t.hideFlags = HideFlags.HideAndDontSave;
            return _fivesMedalTex = t;
        }

        // Red pill, gold border.
        static Texture2D FivesPillTex()
        {
            if (_fivesPillTex != null) return _fivesPillTex;
            const int W = 160, H = 82;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[W * H];
            float rad = H * 0.5f - 1f;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, rad + 1f, W - rad - 1f);
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - H * 0.5f) * (y + 0.5f - H * 0.5f)) - rad;
                var c = new Color(0, 0, 0, 0);
                c = FivesOver(c, new Color(0.95f, 0.75f, 0.28f, 1f), FivesAA(d, 1.5f));
                float inner = d + 7f;
                var red = Color.Lerp(new Color(0.55f, 0.08f, 0.10f, 1f), new Color(0.80f, 0.16f, 0.18f, 1f), (float)y / H);
                c = FivesOver(c, red, FivesAA(inner, 1.5f));
                px[y * W + x] = c;
            }
            t.SetPixels(px);
            t.Apply(false, true);
            t.hideFlags = HideFlags.HideAndDontSave;
            return _fivesPillTex = t;
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

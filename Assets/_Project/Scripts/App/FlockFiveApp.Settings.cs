using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        bool _settingsOpen;
        bool _smokePlayed;
        bool _smokeNoted;
        AdConsent.HomeCard _homePrompt;

        static Texture2D _gearTex;

        // Square gear in the top-left safe band. 44pt minimum. Clears the wordmark.
        public static Rect SettingsGearRect(float w, float h, Rect safe)
        {
            float s = h / 720f;
            if (s < 0.5f) s = 0.5f;
            float size = Mathf.Max(44f, 40f * s);
            float x = Mathf.Max(8f, safe.xMin + 8f);
            float y = TopHudBox(h, safe, 0f) + 2f * s;
            return new Rect(x, y, size, size);
        }

        bool DrawSettingsEntry(float s)
        {
            var r = SettingsGearRect(Screen.width, Screen.height, Screen.safeArea);
            bool fire = HitPad(r, out bool held);
            var tex = GearTex();
            if (tex != null)
            {
                GUI.color = new Color(0.12f, 0.07f, 0.03f, held ? 0.20f : 0.32f);
                GUI.DrawTexture(new Rect(r.x + 2f, r.y + 3f, r.width, r.height), tex, ScaleMode.ScaleToFit, true);
                GUI.color = held ? new Color(0.78f, 0.78f, 0.78f, 1f) : Color.white;
                GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }
            return fire;
        }

        // Privacy link, then the Do Not Sell checkbox on the next row. The whole
        // checkbox row is the hit. The card is a flat wood panel (no oval vignette).
        void DrawSettingsSheet(float s)
        {
            if (s < 0.5f) s = 0.5f;
            if (Texture2D.whiteTexture == null) return;
            GUI.color = new Color(0.05f, 0.04f, 0.03f, 0.42f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = StandardPopupWidth(s);
            float titleH = Mathf.Max(40f, 44f * s);
            float pad = 18f * s;
            float innerW = Mathf.Max(8f, w - pad * 2f);
            var probe = SettingsSheet.Place(new Rect(0f, 0f, innerW, 8f), s);
            float body = probe.Label.yMax - probe.PrivacyLink.yMin;
            float h = titleH + body + pad * 2.4f;
            var card = PlacePopup(s, w, h, 0.34f);
            DrawWoodPanel(card, s);

            float edge = Mathf.Max(10f, 14f * s);
            var inner = new Rect(
                card.x + edge,
                card.y + edge,
                Mathf.Max(8f, card.width - edge * 2f),
                Mathf.Max(8f, card.height - edge * 2f));
            var titleR = new Rect(inner.x, inner.y + 4f * s, inner.width, titleH);
            var rows = SettingsSheet.Place(new Rect(inner.x, titleR.yMax + 6f * s, inner.width, inner.yMax - titleR.yMax), s);

            var title = GuiPool.Label(GuiSlot.SettingsTitle);
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.wordWrap = false;
            title.fontSize = FitFont(title, "Settings", titleR.width * 0.9f, titleR.height * 0.8f, 16, Mathf.Max(22, Mathf.RoundToInt(32f * s)));
            StampOutlined(titleR, "Settings", title, new Color(1f, 0.97f, 0.90f, 1f), 0, Mathf.Max(3, Mathf.RoundToInt(title.fontSize * 0.12f)));

            bool link = HitPad(rows.PrivacyLink, out bool linkHeld);
            var linkSt = GuiPool.Label(GuiSlot.SettingsLink);
            linkSt.fontStyle = FontStyle.Bold;
            linkSt.alignment = TextAnchor.MiddleLeft;
            linkSt.wordWrap = false;
            float textW = Mathf.Max(8f, rows.Toggle.xMin - rows.PrivacyLink.x - 8f * s);
            linkSt.fontSize = FitFont(
                linkSt, SettingsSheet.PrivacyLabel,
                textW, rows.PrivacyLink.height * 0.62f,
                14, Mathf.Max(16, Mathf.RoundToInt(22f * s)));
            var linkCol = linkHeld ? new Color(1f, 1f, 0.94f, 1f) : new Color(1f, 0.97f, 0.90f, 1f);
            StampOutlined(rows.PrivacyLink, SettingsSheet.PrivacyLabel, linkSt, linkCol, 0, Mathf.Max(2, Mathf.RoundToInt(linkSt.fontSize * 0.16f)));

            float sw = rows.Toggle.width;
            var chev = new Rect(
                rows.Toggle.x,
                rows.PrivacyLink.y + (rows.PrivacyLink.height - sw) * 0.5f,
                sw, sw);
            var chevSt = GuiPool.Label(GuiSlot.SettingsChevron);
            chevSt.fontStyle = FontStyle.Bold;
            chevSt.alignment = TextAnchor.MiddleCenter;
            chevSt.wordWrap = false;
            chevSt.fontSize = FitFont(chevSt, "\u203A", chev.width * 0.8f, chev.height * 0.8f, 16, Mathf.Max(20, Mathf.RoundToInt(28f * s)));
            StampOutlined(chev, "\u203A", chevSt, linkCol, 0, Mathf.Max(2, Mathf.RoundToInt(chevSt.fontSize * 0.12f)));

            var rowHit = new Rect(rows.Label.x, rows.Label.y, rows.Toggle.xMax - rows.Label.x, rows.Label.height);
            bool flip = HitPad(rowHit, out bool rowHeld);
            var rowSt = GuiPool.Label(GuiSlot.SettingsRow);
            rowSt.fontStyle = FontStyle.Bold;
            rowSt.alignment = TextAnchor.MiddleLeft;
            rowSt.wordWrap = true;
            rowSt.fontSize = FitFontWrapped(
                rowSt, SettingsSheet.DoNotSellLabel,
                rows.Label.width * 0.96f, rows.Label.height * 0.88f,
                13, Mathf.Max(16, Mathf.RoundToInt(20f * s)));
            StampOutlined(
                rows.Label, SettingsSheet.DoNotSellLabel, rowSt,
                new Color(1f, 0.97f, 0.90f, 1f), 0, Mathf.Max(2, Mathf.RoundToInt(rowSt.fontSize * 0.14f)));

            bool on = AdConsent.DoNotSell;
            DrawCheckbox(rows.Toggle, on, rowHeld);
            if (on) DrawCheckMark(Inset(rows.Toggle, 0.22f), new Color(0.20f, 0.42f, 0.16f, 1f));

            float xSz = Mathf.Max(40f, 42f * s);
            var xBtn = new Rect(card.xMax - xSz - 6f, card.y + 6f, xSz, xSz);
            bool close = HitPad(CloseTapRect(xBtn), out bool xHeld);
            DrawGiftCloseX(xBtn, xHeld, s);

            if (link)
            {
                Sfx.CardTap();
                if (!string.IsNullOrEmpty(SettingsSheet.PrivacyUrl))
                    Application.OpenURL(SettingsSheet.PrivacyUrl);
                return;
            }
            if (flip)
            {
                AdConsent.DoNotSell = !on;
                Sfx.CardTap();
                return;
            }
            if (close)
            {
                _settingsOpen = false;
                Sfx.CardTap();
            }
        }

        // Square box. Empty when off. The check is drawn by the caller when on.
        static void DrawCheckbox(Rect box, bool on, bool held)
        {
            if (box.width < 2f || box.height < 2f || Texture2D.whiteTexture == null) return;
            var tex = Texture2D.whiteTexture;
            GUI.color = held ? new Color(0.95f, 0.78f, 0.32f, 1f) : new Color(0.62f, 0.42f, 0.14f, 1f);
            GUI.DrawTexture(box, tex);
            float inset = Mathf.Max(3f, box.width * 0.14f);
            var inner = new Rect(box.x + inset, box.y + inset, box.width - inset * 2f, box.height - inset * 2f);
            GUI.color = on ? new Color(0.99f, 0.96f, 0.88f, 1f) : new Color(0.93f, 0.88f, 0.76f, 1f);
            GUI.DrawTexture(inner, tex);
            GUI.color = Color.white;
        }

        // Flat wood slab and a gold rim. White texture only, so the blanket oval stays out.
        static void DrawWoodPanel(Rect card, float s)
        {
            var tex = Texture2D.whiteTexture;
            if (tex == null || card.width < 4f || card.height < 4f) return;
            float edge = Mathf.Max(8f, 11f * s);
            GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.28f);
            GUI.DrawTexture(new Rect(card.x + 3f * s, card.y + 5f * s, card.width, card.height), tex);
            GUI.color = new Color(0.34f, 0.18f, 0.07f, 1f);
            GUI.DrawTexture(card, tex);
            GUI.color = new Color(0.86f, 0.68f, 0.30f, 1f);
            GUI.DrawTexture(new Rect(card.x + 3f, card.y + 3f, card.width - 6f, card.height - 6f), tex);
            GUI.color = new Color(0.55f, 0.36f, 0.12f, 1f);
            float lip = Mathf.Max(2f, 3f * s);
            GUI.DrawTexture(new Rect(card.x + edge, card.y + edge, card.width - edge * 2f, card.height - edge * 2f), tex);
            GUI.color = new Color(0.48f, 0.29f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(card.x + edge + lip, card.y + edge + lip, card.width - (edge + lip) * 2f, card.height - (edge + lip) * 2f), tex);
            GUI.color = new Color(1f, 0.90f, 0.55f, 0.55f);
            GUI.DrawTexture(new Rect(card.x + edge, card.y + edge, card.width - edge * 2f, Mathf.Max(2f, 3f * s)), tex);
            GUI.color = Color.white;
        }

        // Same path as the LEVEL flower: badger leap, otherwise the gate sting and Load.
        bool PressLevelButton()
        {
            if (TakeBadgerFlower()) return true;
            Sfx.GateGo();
            Load(LevelData.NextPlay);
            return true;
        }

        // Simulator hook. Absent the argument and the env var, this returns before it touches consent or the level.
        void MaybeSmokeLaunch()
        {
            if (_smokePlayed || !AdConsent.SmokeAutoPlay()) return;
            if (!AdConsent.FirstFrameReady) return;
            if (!_smokeNoted)
            {
                _smokeNoted = true;
                Debug.Log("FF_SMOKE state=played=" + (_smokePlayed ? 1 : 0)
                    + " first=" + (AdConsent.FirstFrameReady ? 1 : 0)
                    + " argc=" + AdConsent.SmokeArgc
                    + " argv=" + (AdConsent.SmokeArgv ? 1 : 0)
                    + " native=" + (AdConsent.SmokeNative ? 1 : 0)
                    + " env=" + (AdConsent.SmokeEnv ? 1 : 0)
                    + " splash=" + (_splash ? 1 : 0)
                    + " home=" + _home
                    + " badger=" + BadgerOwed());
            }
            if (!_splash || _home != HomeFace.Splash) return;
            _smokePlayed = true;
            AdConsent.DismissTransient();
            PressLevelButton();
        }

        void PollHomeAtt()
        {
            bool mid = !_splash;
            bool coach;
            if (_splash)
            {
                HomeModals(out bool hard, out bool soft);
                coach = hard || soft || HomeTutorLive() || TutorialGuideLive();
            }
            else
                coach = TutorialGuideLive();
            AdConsent.PollSystem(Application.isFocused, mid, coach);
        }

        void PrepareSoftPrompt(bool coachOrModal)
        {
            bool onHome = _splash && _home == HomeFace.Splash && !_settingsOpen;
            _homePrompt = AdConsent.LayoutHome(
                Screen.width, Screen.height, Screen.safeArea,
                onHome, coachOrModal || !onHome, LevelData.NextPlay);
        }

        void DrawSoftPrompt(float s)
        {
            var card = _homePrompt;
            if (card.Kind == AdConsent.Sheet.None) return;
            if (Texture2D.whiteTexture == null) return;
            if (s < 0.5f) s = 0.5f;
            GUI.color = new Color(0.05f, 0.04f, 0.03f, 0.28f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawWoodPanel(card.Card, s);

            string body = card.Kind == AdConsent.Sheet.Att ? AdConsent.AttBody : AdConsent.GdprBody;
            var st = GuiPool.Label(GuiSlot.PromptBody);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = true;
            st.fontSize = FitFontWrapped(
                st, body, card.Body.width * 0.92f, card.Body.height * 0.92f,
                15, Mathf.Max(18, Mathf.RoundToInt(22f * s)));
            StampOutlined(card.Body, body, st, new Color(1f, 0.97f, 0.90f, 1f), 0, Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.14f)));

            string yes = card.Kind == AdConsent.Sheet.Att ? AdConsent.ContinueLabel : AdConsent.AllowLabel;
            if (DrawSheetButton(card.Accept, yes, true, s))
            {
                if (card.Kind == AdConsent.Sheet.Att) AdConsent.ContinueAtt();
                else AdConsent.AcceptGdpr();
                Sfx.CardTap();
                return;
            }
            if (DrawSheetButton(card.Decline, AdConsent.NotNowLabel, false, s))
            {
                if (card.Kind == AdConsent.Sheet.Att) AdConsent.NotNow(LevelData.NextPlay);
                else AdConsent.DeclineGdpr();
                Sfx.CardTap();
            }
        }

        bool DrawSheetButton(Rect rect, string label, bool gold, float s)
        {
            bool fire = HitPad(rect, out bool held);
            var face = DrawPopupButton(rect, held, gold ? PopupTint.Gold : PopupTint.Wood);
            var st = GuiPool.Label(GuiSlot.PromptButton);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleCenter;
            st.wordWrap = false;
            st.fontSize = FitFont(st, label, face.width * 0.9f, face.height * 0.62f, 13, Mathf.Max(16, Mathf.RoundToInt(20f * s)));
            StampOutlined(face, label, st, new Color(1f, 0.98f, 0.92f, 1f), 0, Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.16f)));
            return fire;
        }

        // Gold gear, transparent outside the disc. Procedural so it matches the wood buttons.
        static Texture2D GearTex()
        {
            if (_gearTex != null) return _gearTex;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "SettingsGear"
            };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            float hole = n * 0.07f;
            float body = n * 0.32f;
            float toothIn = n * 0.28f;
            float outer = n * 0.46f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c;
                    float dy = y + 0.5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > outer + 1.1f || d < hole - 0.8f) continue;
                    float ang = Mathf.Atan2(dy, dx);
                    // Short teeth on a wood disc. The hole in the middle keeps it a cog.
                    float phase = Mathf.Repeat(ang * 8f / (Mathf.PI * 2f) + 0.5f, 1f);
                    bool tooth = phase < 0.48f && d <= outer + 0.7f && d >= toothIn;
                    bool disc = d <= body && d >= hole;
                    if (!tooth && !disc) continue;
                    float edge = tooth ? outer + 0.7f - d : body + 0.7f - d;
                    edge = Mathf.Min(edge, d - hole + 0.7f);
                    float a = Mathf.Clamp01(edge);
                    float light = Mathf.Clamp01(0.32f + (c - y) / (n * 0.85f));
                    var wood = Color.Lerp(new Color(0.40f, 0.22f, 0.07f), new Color(0.74f, 0.48f, 0.16f), light);
                    var gold = Color.Lerp(new Color(0.62f, 0.40f, 0.10f), new Color(1f, 0.88f, 0.42f), light);
                    var col = tooth ? gold : wood;
                    if (d < hole + n * 0.03f) col = Color.Lerp(col, new Color(0.28f, 0.16f, 0.05f), 0.55f);
                    px[y * n + x] = new Color(col.r, col.g, col.b, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _gearTex = tex;
            return tex;
        }
    }
}

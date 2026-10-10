using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        bool _settingsOpen;

        // Top-left of the home splash, under the notch. Everyone can open it.
        static Rect SettingsEntryRect(float s)
        {
            if (s < 0.5f) s = 0.5f;
            float h = Mathf.Max(44f, 36f * s);
            float w = Mathf.Clamp(156f * s, 112f, 200f);
            var safe = Screen.safeArea;
            float x = Mathf.Max(8f, safe.xMin + 8f);
            float y = TopHud() + 2f * s;
            return new Rect(x, y, w, h);
        }

        bool DrawSettingsEntry(float s)
        {
            var r = SettingsEntryRect(s);
            bool fire = HitPad(r, out bool held);
            var st = GuiPool.Label(GuiSlot.SettingsEntry);
            st.fontStyle = FontStyle.Bold;
            st.alignment = TextAnchor.MiddleLeft;
            st.wordWrap = false;
            const string lab = "Settings";
            st.fontSize = FitFont(st, lab, r.width * 0.92f, r.height * 0.72f, 13, Mathf.Max(16, Mathf.RoundToInt(18f * s)));
            var col = held ? new Color(1f, 1f, 0.94f, 1f) : new Color(1f, 0.97f, 0.90f, 0.92f);
            StampOutlined(r, lab, st, col, 0, Mathf.Max(2, Mathf.RoundToInt(st.fontSize * 0.16f)));
            return fire;
        }

        // Privacy link, then the Do Not Sell switch on the next row. Same card
        // frame as the other home sheets. The switch writes AdConsent.DoNotSell.
        void DrawSettingsSheet(float s)
        {
            if (s < 0.5f) s = 0.5f;
            if (Texture2D.whiteTexture == null) return;
            GUI.color = new Color(0.04f, 0.03f, 0.02f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = StandardPopupWidth(s);
            float titleH = Mathf.Max(40f, 44f * s);
            float pad = 18f * s;
            float innerW = Mathf.Max(8f, w - pad * 2f);
            var probe = SettingsSheet.Place(new Rect(0f, 0f, innerW, 8f), s);
            float body = probe.Label.yMax - probe.PrivacyLink.yMin;
            float h = titleH + body + pad * 2.2f;
            var card = PlacePopup(s, w, h, 0.38f);
            float band = Mathf.Clamp(w * 0.045f, 10f * s, 18f * s);
            var board = new Rect(
                card.x + band,
                card.y + band,
                Mathf.Max(8f, card.width - band * 2f),
                Mathf.Max(8f, card.height - band * 2f));
            DrawDailyFrame(card, board, band, s, 0.5f, GlowTex());
            var inner = new Rect(
                board.x + 10f * s,
                board.y + 10f * s,
                Mathf.Max(8f, board.width - 20f * s),
                Mathf.Max(8f, board.height - 20f * s));
            var titleR = new Rect(inner.x, inner.y + 8f * s, inner.width, titleH);
            var rows = SettingsSheet.Place(new Rect(inner.x, titleR.yMax, inner.width, inner.yMax - titleR.yMax), s);

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
            linkSt.fontSize = FitFont(
                linkSt, SettingsSheet.PrivacyLabel,
                rows.PrivacyLink.width * 0.92f, rows.PrivacyLink.height * 0.7f,
                13, Mathf.Max(16, Mathf.RoundToInt(20f * s)));
            var linkCol = linkHeld ? new Color(1f, 1f, 0.94f, 1f) : new Color(1f, 0.97f, 0.90f, 1f);
            StampOutlined(rows.PrivacyLink, SettingsSheet.PrivacyLabel, linkSt, linkCol, 0, Mathf.Max(2, Mathf.RoundToInt(linkSt.fontSize * 0.16f)));
            GUI.color = linkCol;
            float ruleW = Mathf.Min(rows.PrivacyLink.width * 0.46f, 168f * s);
            GUI.DrawTexture(
                new Rect(rows.PrivacyLink.x, rows.PrivacyLink.yMax - 10f * s, ruleW, Mathf.Max(1.5f, 2f * s)),
                Texture2D.whiteTexture);
            GUI.color = Color.white;

            var rowHit = new Rect(rows.Label.x, rows.Label.y, rows.Toggle.xMax - rows.Label.x, rows.Label.height);
            bool flip = HitPad(rowHit, out bool rowHeld);
            var rowSt = GuiPool.Label(GuiSlot.SettingsRow);
            rowSt.fontStyle = FontStyle.Bold;
            rowSt.alignment = TextAnchor.MiddleLeft;
            rowSt.wordWrap = true;
            rowSt.fontSize = FitFontWrapped(
                rowSt, SettingsSheet.DoNotSellLabel,
                rows.Label.width * 0.96f, rows.Label.height * 0.9f,
                12, Mathf.Max(15, Mathf.RoundToInt(18f * s)));
            StampOutlined(
                rows.Label, SettingsSheet.DoNotSellLabel, rowSt,
                new Color(1f, 0.97f, 0.90f, 1f), 0, Mathf.Max(2, Mathf.RoundToInt(rowSt.fontSize * 0.14f)));

            bool on = AdConsent.DoNotSell;
            DrawPopupButton(rows.Toggle, rowHeld, on ? PopupTint.Gold : PopupTint.Wood);
            if (on) DrawCheckMark(Inset(rows.Toggle, 0.22f));

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
    }
}

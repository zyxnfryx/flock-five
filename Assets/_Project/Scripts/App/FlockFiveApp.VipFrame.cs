using UnityEngine;

namespace FlockFive
{
    // Shared VIP pieces, one helper per behavior:
    //   VipCardLayout   - the bigger VIP card (+13%, clamped so nothing crops on small phones).
    //   AdCardPlate     - the dark panel seated evenly inside fx_ad_card's gold rail.
    //   DrawAdCardFrame - aura, shadow, card art, corner wash, panel and its gold inset.
    //   DrawVipTwinkles - soft gold twinkles (VIP glint) for the badge, frame and welcome.
    //   DrawVipTagAccent- small gold crown on the player's name tag while VIP.
    // The medallion itself is DrawVipMedal (FlockFiveApp.cs, next to its baked textures).
    public sealed partial class FlockFiveApp
    {
        // Measured on fx_ad_card.png (780 x 501): centre line of the gold rail that rings the
        // wood opening. The art's alpha box includes the orchids above the slab, so the
        // opening sits ~20 px BELOW the middle of the texture. The old panel used fractions
        // that were symmetric about the texture middle (10/18/80/64%), so it sat 9 px under
        // the top rail but 49 px over the bottom rail (and 5 px vs 14 px on the sides):
        // the "overbite". The panel is now inset the same distance from all four rails.
        const float AdCardArtW = 780f;
        const float AdCardArtH = 501f;
        const float AdCardRailL = 73f;
        const float AdCardRailR = 716f;
        const float AdCardRailT = 81f;
        const float AdCardRailB = 460f;
        // Same gap on every side, in art pixels (scales with the card, uniform x/y).
        const float AdCardPlateGap = 16f;
        // Wood slab spans 45..746 of 780; orchid tips 8..771.
        const float AdCardSlabFrac = (746f - 45f) / AdCardArtW;

        // Build 58: the VIP card is ~13% larger than the shared standard card.
        const float VipCardGrow = 1.13f;
        // Buy flower drawn 8% over the shared CTA size (unchanged from build 51).
        const float VipBuyScale = 1.08f;

        // Bottom rail from the card's bottom edge, so a card taller than the art (the build 63
        // offer card, see DrawAdCardArt) keeps the same even gap on all four rails.
        static Rect AdCardPlate(Rect card)
        {
            float px = card.width / AdCardArtW;
            float g = AdCardPlateGap * px;
            float x0 = card.x + AdCardRailL * px + g;
            float x1 = card.x + AdCardRailR * px - g;
            float y0 = card.y + AdCardRailT * px + g;
            float y1 = card.yMax - (AdCardArtH - AdCardRailB) * px - g;
            return new Rect(x0, y0, Mathf.Max(8f, x1 - x0), Mathf.Max(8f, y1 - y0));
        }

        // Art height at the card's width (the card's natural height).
        static float AdCardArtHeight(Rect card) => card.width * (AdCardArtH / AdCardArtW);

        // Build 63: the offer card can be taller than fx_ad_card's 780 x 501. The art is cut in
        // three across its plain side rails (rows AdCardStretchTop..AdCardStretchBot sit under
        // the orchids and above the scroll ornaments and petals): the top (orchids) and bottom
        // (scrolls) keep their aspect, and only that band of wood rail stretches. The dark
        // plate covers the middle, so the stretch shows only on the two side rails. At the art's
        // own height this is the single draw it always was.
        const float AdCardStretchTop = 272f;
        const float AdCardStretchBot = 322f;

        static void DrawAdCardArt(Rect card, Texture2D tex)
        {
            float px = card.width / AdCardArtW;
            float natH = AdCardArtH * px;
            if (card.height <= natH + 0.5f)
            {
                GUI.DrawTexture(card, tex, ScaleMode.ScaleToFit, true);
                return;
            }
            float topH = AdCardStretchTop * px;
            float botH = (AdCardArtH - AdCardStretchBot) * px;
            float midH = Mathf.Max(0f, card.height - topH - botH);
            // UVs are bottom-up: art row r (top-down) is v = 1 - r / H.
            float vTop = 1f - AdCardStretchTop / AdCardArtH;
            float vBot = 1f - AdCardStretchBot / AdCardArtH;
            GUI.DrawTextureWithTexCoords(new Rect(card.x, card.y, card.width, topH), tex, new Rect(0f, vTop, 1f, 1f - vTop), true);
            GUI.DrawTextureWithTexCoords(new Rect(card.x, card.y + topH, card.width, midH), tex, new Rect(0f, vBot, 1f, vTop - vBot), true);
            GUI.DrawTextureWithTexCoords(new Rect(card.x, card.y + topH + midH, card.width, botH), tex, new Rect(0f, 0f, 1f, vBot), true);
        }

        // Standard width x VipCardGrow, but the wood slab stays inside the standard
        // side insets and the orchid tips stay on screen, so a narrow phone gets as
        // much of the 13% as fits instead of a cropped frame. Height too: the stack
        // (card + Buy flower, or crest + card) only grows while it still fits between
        // the logo and the Restore line. Phones get the full 13%; a short, wide screen
        // (iPad) keeps the standard size it had, never less.
        static float VipCardWidth(float s, bool withButton)
        {
            float baseW = StandardPopupWidth(s);
            float insetL = Mathf.Max(12f * s, Screen.safeArea.xMin + 8f);
            float insetR = Mathf.Max(12f * s, Screen.width - Screen.safeArea.xMax + 8f);
            float span = Mathf.Max(8f, Screen.width - insetL - insetR);
            float w = baseW * VipCardGrow;
            float slabFit = span / AdCardSlabFrac;
            float edgeFit = Screen.width - 8f;
            if (w > slabFit) w = slabFit;
            if (w > edgeFit) w = edgeFit;
            if (w < baseW) w = baseW;
            float room = VipStackRoom(s);
            for (int i = 0; i < 14 && w > baseW; i++)
            {
                if (VipStackHeight(s, w, withButton) <= room) break;
                w = Mathf.Max(baseW, w - baseW * 0.01f);
            }
            return w;
        }

        // Restore link at the bottom of the safe area, the status note just above it.
        static void VipBottomRects(float s, out Rect link, out Rect status)
        {
            float statusH = Mathf.Max(28f, 22f * s);
            float linkH = Mathf.Max(48f, 40f * s);
            float linkW = StandardPopupWidth(s);
            float bottom = Screen.height - Mathf.Max(6f, Screen.safeArea.yMin);
            link = new Rect((Screen.width - linkW) * 0.5f, bottom - linkH - 4f * s, linkW, linkH);
            status = new Rect(link.x, link.y - 4f * s - statusH, link.width, statusH);
        }

        // Vertical band the VIP stack may use: under the logo, over the status note.
        static float VipStackRoom(float s)
        {
            VipBottomRects(s, out _, out var status);
            float ceiling = status.y - 12f * s;
            float floor = SplashTitleHalo().yMax + 8f * s;
            return ceiling - floor;
        }

        static float VipStackHeight(float s, float cardW, bool withButton)
        {
            float cardH = cardW * (AdCardArtH / AdCardArtW);
            if (!withButton) return VipCrestOverhang(cardH) + cardH;
            float fs = PopupButtonSize(s, cardW);
            // Flower rises PopupCtaOverlap (15%) into the card; scaled by VipBuyScale about its centre.
            return cardH - PopupCtaOverlap(fs, 0f) + fs * (0.5f + 0.5f * VipBuyScale);
        }

        // Card plus the square Buy flower under it (withButton), or the card alone with
        // room above it for the member crest. Centred on the safe span (PlacePopup would
        // clamp the wider card to the span and push it off the right edge).
        static void VipCardLayout(float s, bool withButton, out Rect card, out Rect flower)
        {
            if (withButton)
            {
                VipOfferLayout(s, out card, out flower);
                return;
            }
            float cardW = VipCardWidth(s, withButton);
            float cardH = cardW * (AdCardArtH / AdCardArtW);
            float insetL = Mathf.Max(12f * s, Screen.safeArea.xMin + 8f);
            float insetR = Mathf.Max(12f * s, Screen.width - Screen.safeArea.xMax + 8f);
            float cx = insetL + Mathf.Max(8f, Screen.width - insetL - insetR) * 0.5f;
            float flowerSz = withButton ? PopupButtonSize(s, cardW) : 0f;
            float overlap = withButton ? PopupCtaOverlap(flowerSz, 0f) : 0f;
            float crestUp = withButton ? 0f : VipCrestOverhang(cardH);
            float stackH = crestUp + cardH + flowerSz - overlap;
            var placed = PlacePopup(s, Mathf.Min(cardW, Screen.width), stackH, withButton ? 0.32f : 0.36f);
            card = new Rect(cx - cardW * 0.5f, placed.y + crestUp, cardW, cardH);
            flower = withButton
                ? new Rect(cx - flowerSz * 0.5f, card.yMax - overlap, flowerSz, flowerSz)
                : new Rect(cx, card.yMax, 0f, 0f);
            float bot = Screen.height - Mathf.Max(8f, Screen.safeArea.yMin + 6f);
            float low = Mathf.Max(card.yMax, flower.yMax);
            if (low > bot)
            {
                float shift = low - bot;
                card.y -= shift;
                flower.y -= shift;
            }
            float hud = TopHud() + 8f * s + crestUp;
            if (card.y < hud)
            {
                float push = hud - card.y;
                float room = bot - Mathf.Max(card.yMax, flower.yMax);
                if (push > room) push = room > 0f ? room : 0f;
                card.y += push;
                flower.y += push;
            }
        }

        // Build 63 (Brandon: the list sells it, make it big). The offer card is the hero:
        // its wood frame VipOfferWidth of the screen wide (capped so the frame stays inside the
        // safe area and the orchid tips on screen, the same caps VipCardWidth uses) and taller
        // than the art (DrawAdCardArt), up to
        // VipOfferTallMax art heights. The stack (card + Buy flower) sits between the close X
        // and the status note above the Restore link, centred in that band. The logo and the
        // LEVEL flower under it stay where they are, under the dim. Short wide screens (iPad)
        // narrow the card until an art-height card and the flower fit.
        const float VipOfferWidth = 0.92f;
        const float VipOfferTallMax = 2.0f;
        // Same share PopupButtonSize uses, so the Go VIP flower keeps its full size.
        const float VipOfferFlowerMax = 0.62f;

        static void VipOfferLayout(float s, out Rect card, out Rect flower)
        {
            VipBottomRects(s, out _, out var status);
            var x = CornerCloseRect(s, TopHud());
            float top = x.yMax + 4f * s;
            float ceiling = status.y - 12f * s;
            float room = Mathf.Max(80f, ceiling - top);
            float insetL = Mathf.Max(12f * s, Screen.safeArea.xMin + 8f);
            float insetR = Mathf.Max(12f * s, Screen.width - Screen.safeArea.xMax + 8f);
            float span = Mathf.Max(8f, Screen.width - insetL - insetR);
            // The card rect includes the orchid overhang; the visible frame is AdCardSlabFrac.
            float cardW = Mathf.Min(Screen.width * VipOfferWidth, span) / AdCardSlabFrac;
            cardW = Mathf.Min(cardW, Screen.width - 8f);
            float natH = 0f, fs = 0f, below = 0f;
            for (int i = 0; i < 40; i++)
            {
                natH = cardW * (AdCardArtH / AdCardArtW);
                fs = Mathf.Min(PopupButtonSize(s, cardW), cardW * VipOfferFlowerMax);
                // Flower rises PopupCtaOverlap into the card; drawn VipBuyScale about its centre.
                below = fs * (0.5f + 0.5f * VipBuyScale) - PopupCtaOverlap(fs, 0f);
                if (natH + below <= room || cardW <= 160f) break;
                cardW *= 0.97f;
            }
            float cardH = Mathf.Clamp(room - below, natH, natH * VipOfferTallMax);
            float y = top + Mathf.Max(0f, room - (cardH + below)) * 0.5f;
            float cx = Mathf.Clamp(insetL + span * 0.5f, cardW * 0.5f + 4f, Screen.width - cardW * 0.5f - 4f);
            card = new Rect(cx - cardW * 0.5f, y, cardW, cardH);
            flower = new Rect(cx - fs * 0.5f, card.yMax - PopupCtaOverlap(fs, 0f), fs, fs);
        }

        // Member crest: the VIP medallion crowning the card's top edge, between the
        // orchids, clear of the panel (its bottom stops above the panel top).
        static Rect VipCrestRect(Rect card)
        {
            float d = card.height * 0.27f;
            float cy = card.y + card.height * 0.04f;
            return new Rect(card.center.x - d * 0.5f, cy - d * 0.5f, d, d);
        }

        static float VipCrestOverhang(float cardH) => cardH * (0.27f * 0.5f - 0.04f) + 4f;

        // The VIP frame. alpha fades every layer (open / close). petals adds the two
        // small petals and the corner wash over the baked orchids. Returns the panel.
        static Rect DrawAdCardFrame(Rect card, float s, float breathe, float alpha, bool petals)
        {
            var plate = AdCardPlate(card);
            if (alpha < 0.02f) return plate;
            var glow = GlowTex();
            float aura = card.width * (0.10f + 0.04f * breathe);
            GUI.color = new Color(1f, 0.78f, 0.22f, (0.30f + 0.22f * breathe) * alpha);
            bool tall = card.height > AdCardArtHeight(card) + 0.5f;
            GUI.DrawTexture(new Rect(card.x - aura, card.y - aura * 0.6f, card.width + aura * 2f, card.height + aura * 1.2f), glow,
                tall ? ScaleMode.StretchToFill : ScaleMode.ScaleToFit, true);
            var tex = SpriteCatalog.AdCard != null ? SpriteCatalog.AdCard.texture : null;
            if (tex != null)
            {
                GUI.color = new Color(0.10f, 0.06f, 0.03f, 0.40f * alpha);
                DrawAdCardArt(new Rect(card.x + 6f, card.y + 14f, card.width, card.height), tex);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                DrawAdCardArt(card, tex);
            }
            if (petals) DrawAdCardCorners(card, alpha);

            GUI.color = new Color(0.04f, 0.02f, 0.01f, 0.92f * alpha);
            GUI.DrawTexture(plate, Texture2D.whiteTexture);
            // Gold inset: same 2*s margin and 3*s weight on all four sides.
            float m = 2f * s;
            float w = 3f * s;
            GUI.color = new Color(1f, 0.78f, 0.22f, (0.42f + 0.28f * breathe) * alpha);
            GUI.DrawTexture(new Rect(plate.x + m, plate.y + m, plate.width - m * 2f, w), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.x + m, plate.yMax - m - w, plate.width - m * 2f, w), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.x + m, plate.y + m, w, plate.height - m * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(plate.xMax - m - w, plate.y + m, w, plate.height - m * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            return plate;
        }

        // Baked orchids stay in the card texture. Wash the corners, then two small
        // petals at opposite top corners so the frame is not a cluster.
        static void DrawAdCardCorners(Rect card, float alpha)
        {
            var glow = GlowTex();
            // Art height, not the rect's: a taller offer card keeps the same corner wash.
            float natH = AdCardArtHeight(card);
            float w = card.width * 0.20f;
            float h = natH * 0.24f;
            GUI.color = new Color(0.05f, 0.03f, 0.025f, 0.62f * alpha);
            GUI.DrawTexture(new Rect(card.x - w * 0.04f, card.y - h * 0.02f, w, h), glow, ScaleMode.ScaleToFit, true);
            GUI.DrawTexture(new Rect(card.xMax - w * 0.96f, card.y - h * 0.02f, w, h), glow, ScaleMode.ScaleToFit, true);
            float bw = card.width * 0.18f;
            float bh = natH * 0.20f;
            GUI.DrawTexture(new Rect(card.x, card.yMax - bh, bw, bh), glow, ScaleMode.ScaleToFit, true);
            GUI.DrawTexture(new Rect(card.xMax - bw, card.yMax - bh, bw, bh), glow, ScaleMode.ScaleToFit, true);
            float sz = card.width * 0.10f;
            var pink = SpriteCatalog.PetalPink;
            var peach = SpriteCatalog.PetalPeach;
            if (pink != null && pink.texture != null)
            {
                GUI.color = new Color(1f, 0.74f, 0.82f, 0.88f * alpha);
                GUI.DrawTexture(new Rect(card.x + card.width * 0.03f, card.y + natH * 0.03f, sz, sz), pink.texture, ScaleMode.ScaleToFit, true);
            }
            if (peach != null && peach.texture != null)
            {
                GUI.color = new Color(1f, 0.80f, 0.58f, 0.88f * alpha);
                float pz = sz * 0.86f;
                GUI.DrawTexture(new Rect(card.xMax - pz - card.width * 0.04f, card.y + natH * 0.045f, pz, pz), peach.texture, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        // Soft gold twinkles around a rect. Shared SparkleFx (same feel as streak).
        // Medal / welcome crown only: its seats also fall inside r.
        static void DrawVipTwinkles(Rect r, float alpha, float sizeFrac)
        {
            SparkleFx.DrawAround(r, alpha, gold: true, sizeFrac: sizeFrac, wide: true);
        }

        // VIP card: twinkles on the wood frame's outer edge only (shared SparkleFx border
        // mode), never over the dark plate or its copy. Frame = card edge to the plate.
        static void DrawVipFrameTwinkles(Rect card, Rect plate, float alpha, float sizeFrac)
        {
            float frame = Mathf.Min(Mathf.Min(plate.x - card.x, card.xMax - plate.xMax),
                Mathf.Min(plate.y - card.y, card.yMax - plate.yMax));
            SparkleFx.DrawBorder(card, frame, alpha, gold: true, sizeFrac: sizeFrac);
        }

        // VIP accent on the name tag: a small gold crown tipped on the tag's top-left
        // corner plus one slow glint. Draw-only; the tag's rect and hit do not change.
        static void DrawVipTagAccent(Rect tag, float s)
        {
            if (!NoAds.Owned || tag.width < 8f || tag.height < 8f) return;
            var crown = VipCrownTex();
            if (crown == null) return;
            float w = tag.height * 0.78f;
            float h = w * (VipCrownH / (float)VipCrownW);
            var c = new Vector2(tag.x + tag.height * 0.26f, tag.y + h * 0.05f);
            var r = new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h);
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(-18f, c);
            GUI.color = new Color(0.10f, 0.05f, 0.02f, 0.40f);
            GUI.DrawTexture(new Rect(r.x + 1f * s, r.y + 1.5f * s, r.width, r.height), crown, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            GUI.DrawTexture(r, crown, ScaleMode.ScaleToFit, true);
            GUI.matrix = prev;
            float tw = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.7f);
            tw *= tw;
            var glint = VipGlintTex();
            if (glint != null && tw > 0.15f)
            {
                float g = w * 0.42f;
                GUI.color = new Color(1f, 0.97f, 0.84f, 0.85f * tw);
                GUI.DrawTexture(new Rect(r.xMax - g * 0.6f, r.y - g * 0.3f, g, g), glint, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }
    }
}

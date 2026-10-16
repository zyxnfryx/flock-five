using UnityEngine;

namespace FlockFive
{
    // One seat for the garden's two bottom corners. Restart is the left button,
    // hive is the right. Both rects are at rest. Draw them inside the HUD shake
    // matrix as-is. A world pin or a hit test applies Shake once, with the same
    // offset the matrix uses, so the hive moves with the restart.
    public static class CornerAnchor
    {
        public const float SizeRef = 72f;
        public const float InsetRef = 40f;
        // Air inside the safe area, above the home-indicator inset. Scaled by screen.
        public const float BottomMarginRef = 72f;
        // b103 (Brandon): on tablets the buttons sit on a fixed small buffer above the bottom safe edge, not
        // 72 ref px, so they never ride up into the lowest (ad) branch row. Phones are exactly as before.
        public const float TabletBottomMarginRef = 18f;
        public static float BottomMargin(float scale, float window)
            => Mathf.Lerp(BottomMarginRef, TabletBottomMarginRef, DeviceFit.Wide(window)) * scale;

        public static void Garden(float screenW, float screenH, Rect safe,
            out float scale, out float top, out float bot, out Rect restart, out Rect hive)
        {
            scale = DeviceFit.Ui(screenW, screenH);
            if (safe.width < 2f || safe.height < 2f)
                safe = new Rect(0f, 0f, screenW, screenH);
            float safeTop = screenH - safe.yMax;
            float safeBot = safe.yMin;
            if (safeTop < 0f) safeTop = 0f;
            if (safeBot < 0f) safeBot = 0f;
            top = 72f * scale + safeTop;
            float size = SizeRef * scale;
            float inset = InsetRef * scale;
            float margin = BottomMargin(scale, DeviceFit.TestWindow ?? (Application.isBatchMode ? DeviceFit.PhoneAspect : DeviceFit.WindowAspect(screenW, screenH)));
            // Whole icon inside the safe glass: bottom inset, then this margin.
            float y = screenH - safeBot - margin - size;
            if (y < safeTop) y = safeTop;
            if (y < 0f) y = 0f;
            bot = screenH - y;
            float left = safe.xMin + inset;
            float right = safe.xMax - inset - size;
            if (left < 0f) left = 0f;
            if (right + size > screenW) right = screenW - size;
            if (right < 0f) right = 0f;
            // b105 (Brandon): the corner buttons must never sit on the bottom ad (gift) branch or its
            // arrow sign. This is the base layout for every window, not a tablet path: phones and the
            // ~0.56-aspect iPad compat window put the 72-ref-px margin right on the ad row.
            float room = screenH - safeBot;
            float adTop, adBot;
            AdRowBand(screenW, screenH, out var signL, out var signR, out var branchL, out var branchR, out adTop, out adBot);
            float pad = 4f * scale;
            bool hits = Hits(restart0(left, y, size, pad), signL, signR, branchL, branchR)
                || Hits(restart0(right, y, size, pad), signL, signR, branchL, branchR);
            if (hits)
            {
                float below = adBot + 6f * scale;
                float avail = room - below;
                if (avail >= size * 0.6f)
                {
                    y = below;
                    size = Mathf.Min(size, avail);
                }
                else
                {
                    y = Mathf.Max(0f, adTop - 6f * scale - size);
                }
                bot = screenH - y;
                right = safe.xMax - inset - size;
                if (right + size > screenW) right = screenW - size;
                if (right < 0f) right = 0f;
            }
            restart = new Rect(left, y, size, size);
            hive = new Rect(right, y, size, size);
        }

        static Rect restart0(float x, float y, float size, float pad)
            => new Rect(x - pad, y - pad, size + pad * 2f, size + pad * 2f);

        static bool Hits(Rect r, Rect a, Rect b, Rect c, Rect d)
            => r.Overlaps(a) || r.Overlaps(b) || r.Overlaps(c) || r.Overlaps(d);

        // Where the play camera puts world (wx, wy) in GUI space (y down), for a window of this size.
        // Mirrors WorldBuilder.MakeCamera + PortraitLock.Apply: the 9:16 play rect is letter/pillar-boxed.
        public static Vector2 WorldToGui(float screenW, float screenH, float wx, float wy)
        {
            float win = screenW / Mathf.Max(1f, screenH);
            float want = DeviceFit.PlayAspect(win);
            float px = 0f, py = 0f, pw = screenW, ph = screenH;
            if (Mathf.Abs(win - want) >= 0.03f)
            {
                if (win > want) { pw = screenW * want / win; px = (screenW - pw) * 0.5f; }
                else { ph = screenH * win / want; py = (screenH - ph) * 0.5f; }
            }
            float sx = px + pw * 0.5f + wx / (WorldBuilder.CamOrtho * want) * (pw * 0.5f);
            float sy = py + ph * 0.5f - (wy - WorldBuilder.CamRestY) / WorldBuilder.CamOrtho * (ph * 0.5f);
            return new Vector2(sx, sy);
        }

        static Rect GuiBox(float w, float h, float x0, float y0, float x1, float y1)
        {
            var a = WorldToGui(w, h, x0, y0);
            var b = WorldToGui(w, h, x1, y1);
            float xa = Mathf.Min(a.x, b.x), xb = Mathf.Max(a.x, b.x);
            float ya = Mathf.Min(a.y, b.y), yb = Mathf.Max(a.y, b.y);
            return new Rect(xa, ya, xb - xa, yb - ya);
        }

        // The bottom ad (gift) row in GUI space: both arrow signs and both branch bodies, conservative
        // (sign 1126x530 @200 ppu x 0.26 at local (+-1.08, 0.48); wood 1280 @140 ppu x WoodScaleX).
        public static void AdRowBand(float w, float h, out Rect signL, out Rect signR, out Rect branchL, out Rect branchR,
            out float top, out float bottom)
        {
            float win = w / Mathf.Max(1f, h);
            float want = DeviceFit.PlayAspect(win);
            float woodHalf = WorldBuilder.WoodHalfOf(WorldBuilder.WoodScaleX);
            float edge = WorldBuilder.CamOrtho * want - woodHalf + 0.18f;
            float gy = WorldBuilder.GiftY;
            const float signHalfW = 0.85f, signHalfH = 0.45f, signUp = 0.48f;
            // Left limb: dir -1, sign sits on the inner side (+1.08). Right limb mirrors.
            signL = GuiBox(w, h, -edge + 1.08f - signHalfW, gy + signUp + signHalfH, -edge + 1.08f + signHalfW, gy + signUp - signHalfH);
            signR = GuiBox(w, h, edge - 1.08f - signHalfW, gy + signUp + signHalfH, edge - 1.08f + signHalfW, gy + signUp - signHalfH);
            branchL = GuiBox(w, h, -edge - woodHalf, gy + 0.87f, -edge + woodHalf, gy - 0.95f);
            branchR = GuiBox(w, h, edge - woodHalf, gy + 0.87f, edge + woodHalf, gy - 0.95f);
            top = Mathf.Min(Mathf.Min(signL.yMin, signR.yMin), Mathf.Min(branchL.yMin, branchR.yMin));
            bottom = Mathf.Max(Mathf.Max(signL.yMax, signR.yMax), Mathf.Max(branchL.yMax, branchR.yMax));
        }

        // Same T*R the garden OnGUI matrix applies: rotate around the screen
        // center, then translate by the shake offset.
        public static Vector2 ShakePoint(Vector2 p, Vector2 offset, float twistDeg, Vector2 pivot)
        {
            var v = p - pivot;
            float rad = twistDeg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            return new Vector2(
                pivot.x + v.x * c - v.y * s,
                pivot.y + v.x * s + v.y * c) + offset;
        }

        public static Rect Shake(Rect r, Vector2 offset)
        {
            return new Rect(r.x + offset.x, r.y + offset.y, r.width, r.height);
        }

        public static Rect Shake(Rect r, Vector2 offset, float twistDeg, Vector2 pivot)
        {
            if (Mathf.Abs(twistDeg) < 0.001f) return Shake(r, offset);
            Vector2 a = ShakePoint(new Vector2(r.xMin, r.yMin), offset, twistDeg, pivot);
            Vector2 b = ShakePoint(new Vector2(r.xMax, r.yMin), offset, twistDeg, pivot);
            Vector2 c = ShakePoint(new Vector2(r.xMin, r.yMax), offset, twistDeg, pivot);
            Vector2 d = ShakePoint(new Vector2(r.xMax, r.yMax), offset, twistDeg, pivot);
            float x0 = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x));
            float y0 = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
            float x1 = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x));
            float y1 = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));
            return new Rect(x0, y0, Mathf.Max(1f, x1 - x0), Mathf.Max(1f, y1 - y0));
        }
    }
}

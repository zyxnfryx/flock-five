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
            float margin = BottomMarginRef * scale;
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
            restart = new Rect(left, y, size, size);
            hive = new Rect(right, y, size, size);
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

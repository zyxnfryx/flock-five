using UnityEngine;

namespace FlockFive
{
    // Where a lesson plate and glove may sit relative to the thing they teach.
    // Captions and gloves miss the focused object's text. Order is below the
    // object, then above, then the side with more room.
    public static class CoachSeat
    {
        public struct FocusText
        {
            public Rect Title;
            public Rect Body;
            public Rect Flavor;

            public bool Hits(Rect r) => Over(r, Title) || Over(r, Body) || Over(r, Flavor);

            public static bool Over(Rect a, Rect b)
            {
                if (a.width < 2f || a.height < 2f || b.width < 2f || b.height < 2f) return false;
                return a.Overlaps(b);
            }
        }

        public static Rect PlaceCaption(float screenW, float screenH, Rect focus, float capW, float capH, FocusText text, float gap)
        {
            if (capW < 8f) capW = 8f;
            if (capH < 8f) capH = 8f;
            float x = focus.center.x - capW * 0.5f;
            if (x < 8f) x = 8f;
            if (x + capW > screenW - 8f) x = Mathf.Max(8f, screenW - 8f - capW);
            var below = new Rect(x, focus.yMax + gap, capW, capH);
            if (!text.Hits(below)) return below;
            var above = new Rect(x, focus.yMin - gap - capH, capW, capH);
            if (above.y < 8f) above.y = 8f;
            if (!text.Hits(above)) return above;
            float leftRoom = focus.xMin;
            float rightRoom = screenW - focus.xMax;
            float sx = leftRoom >= rightRoom ? focus.xMin - gap - capW : focus.xMax + gap;
            if (sx < 8f) sx = 8f;
            if (sx + capW > screenW - 8f) sx = Mathf.Max(8f, screenW - 8f - capW);
            float sy = focus.center.y - capH * 0.5f;
            if (sy < 8f) sy = 8f;
            if (sy + capH > screenH - 8f) sy = Mathf.Max(8f, screenH - 8f - capH);
            var side = new Rect(sx, sy, capW, capH);
            if (!text.Hits(side)) return side;
            return below;
        }
    }
}

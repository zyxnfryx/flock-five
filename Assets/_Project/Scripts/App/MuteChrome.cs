using UnityEngine;

namespace FlockFive
{
    // One mute control for the home screen, a garden level, and the poker table.
    // Every face uses the same paint pass: after the modal washes, above PopupSort.
    // A popup does not get its own mute redraw, and opening one does not hide it.
    public static class MuteChrome
    {
        public const int AbovePopup = ModalBlock.PopupSort + 1;
        // Poker draws smaller than the home speaker so it fits under the coin.
        public const float PokerScale = 0.55f;
        // Share of the corner gap kept between the coin icon and the poker speaker.
        public const float PokerTuck = 0.35f;

        public enum Face
        {
            Off = 0,
            Home = 1,
            Garden = 2,
            Poker = 3
        }

        public struct GardenSeat
        {
            public Rect Mute;
            public Rect Stamp;
            public Rect Restart;
            public Rect Hive;
        }

        public struct PokerSeat
        {
            public Rect Mute;
            public Rect Back;
            public Rect Coins;
            public Rect Title;
            public Rect Halo;
            public Rect PayTab;
        }

        public static bool AboveOverlay(Face face) => face != Face.Off;

        public static int Sort(Face face) => AboveOverlay(face) ? AbovePopup : 0;

        public static float Gap(float h)
        {
            float g = 4f * FlockFiveApp.SettingsGearPxPerPt(h);
            if (g < 8f) g = 8f;
            return g;
        }

        // Drawn speaker edge. Home and garden use the gear's drawn size, sitting
        // in the top-right of a possibly larger hit. Poker fills its smaller hit.
        public static float DrawSize(Face face, Rect hit, float h)
        {
            if (face == Face.Poker) return hit.width;
            float drawn = FlockFiveApp.SettingsGearDrawnSize(h);
            if (drawn > hit.width) drawn = hit.width;
            if (drawn < 2f) drawn = hit.width;
            return drawn;
        }

        static Rect Full(float w, float h, Rect safe)
        {
            if (safe.width < 2f || safe.height < 2f)
                return new Rect(0f, 0f, w, h);
            return safe;
        }

        // Gear mirrored into the top-right. hudBottom drops it under a pill that
        // already owns that corner. Empty safe area is the screen itself.
        public static Rect Home(float w, float h, Rect safe, float hudBottom = 0f)
        {
            safe = Full(w, h, safe);
            var g = FlockFiveApp.SettingsGearRect(w, h, safe);
            float gx = g.x - safe.xMin;
            var r = new Rect(safe.xMax - gx - g.width, g.y, g.width, g.height);
            if (hudBottom > r.y) r.y = hudBottom + gx * 0.5f;
            return r;
        }

        public static bool Clear(Rect a, Rect b, float gap)
        {
            if (a.width < 1f || a.height < 1f) return true;
            if (b.width < 2f || b.height < 2f) return true;
            var grow = new Rect(b.x - gap, b.y - gap, b.width + gap * 2f, b.height + gap * 2f);
            return !a.Overlaps(grow);
        }

        static bool OnGlass(Rect r, float w, float h, Rect safe)
        {
            if (r.width < 2f || r.height < 2f) return false;
            if (r.xMin < -0.5f || r.yMin < -0.5f) return false;
            if (r.xMax > w + 0.5f || r.yMax > h + 0.5f) return false;
            safe = Full(w, h, safe);
            float top = Mathf.Max(0f, h - safe.yMax);
            float bot = h - Mathf.Max(0f, safe.yMin);
            if (r.yMin < top - 0.5f) return false;
            if (r.yMax > bot + 0.5f) return false;
            if (r.xMin < safe.xMin - 0.5f) return false;
            if (r.xMax > safe.xMax + 0.5f) return false;
            return true;
        }

        static bool Clears(Rect r, float w, float h, Rect safe, float gap, Rect stamp, Rect restart, Rect hive)
        {
            return OnGlass(r, w, h, safe)
                && Clear(r, stamp, gap)
                && Clear(r, restart, gap)
                && Clear(r, hive, gap);
        }

        // Top-right first. If that corner meets the stamp, the restart, or the
        // hive, step down and then left until the button is clear.
        public static Rect Seat(float w, float h, Rect safe, Rect want, Rect stamp, Rect restart, Rect hive)
        {
            float gap = Gap(h);
            if (Clears(want, w, h, safe, gap, stamp, restart, hive)) return want;
            float step = gap;
            float size = want.width;
            for (int dy = 0; dy <= 48; dy++)
            {
                for (int dx = 0; dx <= 48; dx++)
                {
                    var r = new Rect(want.x - dx * step, want.y + dy * step, size, size);
                    if (Clears(r, w, h, safe, gap, stamp, restart, hive)) return r;
                }
            }
            for (float k = 0.92f; k >= 0.42f; k -= 0.08f)
            {
                float sz = size * k;
                var r = new Rect(want.xMax - sz, want.y, sz, sz);
                if (Clears(r, w, h, safe, gap, stamp, restart, hive)) return r;
            }
            return want;
        }

        public static GardenSeat Garden(float w, float h, Rect safe)
        {
            float s = DeviceFit.Ui(w, h);
            CornerAnchor.Garden(w, h, safe, out _, out _, out _, out var restart, out var hive);
            var stamp = FlockFiveApp.GardenStampBox(w, h, safe, s);
            var want = Home(w, h, safe);
            return new GardenSeat
            {
                Mute = Seat(w, h, safe, want, stamp, restart, hive),
                Stamp = stamp,
                Restart = restart,
                Hive = hive
            };
        }

        public static PokerSeat Poker(float w, float h, Rect safe)
        {
            float s = DeviceFit.Ui(w, h);
            var chrome = FlockFiveApp.LayoutPokerChrome(w, h, safe, s);
            float gap = Gap(h);
            float home = FlockFiveApp.SettingsGearDrawnSize(h);
            float size = home * PokerScale;
            float right = chrome.Coins.xMax;
            float leftLimit = chrome.Title.xMax;
            if (chrome.Halo.xMax > leftLimit) leftLimit = chrome.Halo.xMax;
            leftLimit += gap;
            float room = right - leftLimit;
            if (size > room) size = room;
            float min = 22f * FlockFiveApp.SettingsGearPxPerPt(h);
            if (size < min && room >= min) size = min;
            if (size < 8f) size = 8f;
            float tuck = gap * PokerTuck;
            if (tuck < 4f) tuck = 4f;
            float y = chrome.Coins.yMax + tuck;
            float bottom = chrome.PayTab.y - gap;
            if (y + size > bottom)
            {
                float fit = bottom - y;
                if (fit < 8f) fit = 8f;
                if (size > fit) size = fit;
            }
            // Centered under the coin icon, then pulled back if the title or the
            // screen edge would take that seat.
            float x = chrome.Coins.center.x - size * 0.5f;
            if (x < leftLimit) x = leftLimit;
            if (x + size > right) x = right - size;
            var mute = new Rect(x, y, size, size);
            return new PokerSeat
            {
                Mute = mute,
                Back = chrome.Back,
                Coins = chrome.Coins,
                Title = chrome.Title,
                Halo = chrome.Halo,
                PayTab = chrome.PayTab
            };
        }
    }
}

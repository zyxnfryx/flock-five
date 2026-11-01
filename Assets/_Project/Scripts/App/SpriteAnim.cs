using UnityEngine;

namespace FlockFive
{
    // Shared sprite swap. Badger idle, hops, and the leap all come through here.
    // A dissolve that draws both layers below 1 opens a hole (peak a quarter of the
    // background at the midpoint) and reads as a blink. Cover keeps the louder layer
    // at 1. Feet are planted on the opaque foot, not the texture center: idle_a and
    // idle_b (and the leap frames) do not share a canvas, so a center anchor pops.
    public static class SpriteAnim
    {
        public static readonly string[] Frames =
        {
            "badger_idle_a", "badger_idle_b", "badger_crouch", "badger_hop_a", "badger_hop_b",
            "badger_leap_0", "badger_leap_1", "badger_leap_3", "badger_shrug",
            "hive_idle", "hive_swiped", "honey_splat",
            "bee_1", "bee_2", "bee_3", "bee_4", "bee_5"
        };

        // Opaque foot, measured once. Cx is the foot center as a fraction of width.
        // Bottom is the first opaque row from the top, as a fraction of height,
        // so the pad under the feet does not lift the next frame.
        struct Foot
        {
            public float Cx;
            public float Bottom;
        }

        static readonly System.Collections.Generic.Dictionary<string, Foot> _feet =
            new System.Collections.Generic.Dictionary<string, Foot>
        {
            { "badger_idle_a", new Foot { Cx = 221f / 363f, Bottom = 628f / 634f } },
            { "badger_idle_b", new Foot { Cx = 352.5f / 438f, Bottom = 634f / 640f } },
            { "badger_crouch", new Foot { Cx = 345f / 803f, Bottom = 598f / 604f } },
            { "badger_hop_a", new Foot { Cx = 600f / 645f, Bottom = 593f / 599f } },
            { "badger_hop_b", new Foot { Cx = 433f / 608f, Bottom = 593f / 600f } },
            { "badger_leap_0", new Foot { Cx = 615f / 668f, Bottom = 596f / 602f } },
            { "badger_leap_1", new Foot { Cx = 249.5f / 736f, Bottom = 608f / 614f } },
            { "badger_leap_3", new Foot { Cx = 495.5f / 977f, Bottom = 662f / 669f } },
            { "badger_shrug", new Foot { Cx = 399f / 618f, Bottom = 648f / 654f } },
            { "hive_idle", new Foot { Cx = 418f / 900f, Bottom = 675f / 681f } },
            { "hive_swiped", new Foot { Cx = 438.5f / 899f, Bottom = 659f / 665f } },
            { "honey_splat", new Foot { Cx = 373.5f / 775f, Bottom = 668f / 675f } },
            { "bee_1", new Foot { Cx = 144f / 295f, Bottom = 294f / 300f } },
            { "bee_2", new Foot { Cx = 137f / 325f, Bottom = 298f / 304f } },
            { "bee_3", new Foot { Cx = 164f / 292f, Bottom = 273f / 280f } },
            { "bee_4", new Foot { Cx = 126.5f / 284f, Bottom = 294f / 300f } },
            { "bee_5", new Foot { Cx = 117f / 263f, Bottom = 284f / 290f } },
        };

        public static bool HasFoot(string name) => !string.IsNullOrEmpty(name) && _feet.ContainsKey(name);

        // Opaque-foot centre as a fraction of width. Unknown frames stay at 0.5.
        public static float FootCx(string name)
        {
            if (!string.IsNullOrEmpty(name) && _feet.TryGetValue(name, out var foot))
                return foot.Cx;
            return 0.5f;
        }

        // Louder layer stays at full cover. A missing sprite holds the one that loaded
        // at 1 so the swap never shows a blank frame.
        public static void Cover(ref float frameAlpha, ref float underAlpha, bool frameReady, bool underReady)
        {
            if (!frameReady && !underReady)
            {
                frameAlpha = 0f;
                underAlpha = 0f;
                return;
            }
            if (!frameReady)
            {
                frameAlpha = 0f;
                underAlpha = 1f;
                return;
            }
            if (!underReady)
            {
                underAlpha = 0f;
                frameAlpha = 1f;
                return;
            }
            if (frameAlpha >= underAlpha) frameAlpha = 1f;
            else underAlpha = 1f;
        }

        // `anchor` is the planted foot. Known frames use the opaque foot; anything
        // else stays bottom-center of its texture, which is the old drawer.
        public static Rect Dest(string name, float texW, float texH, Vector2 anchor, float unit, float sx, float sy)
        {
            if (texW < 1f) texW = 1f;
            if (texH < 1f) texH = 1f;
            if (sx < 0.05f) sx = 1f;
            if (sy < 0.05f) sy = 1f;
            float w = texW * unit * sx;
            float h = texH * unit * sy;
            float cx = 0.5f;
            float bottom = 1f;
            if (!string.IsNullOrEmpty(name) && _feet.TryGetValue(name, out var foot))
            {
                cx = foot.Cx;
                bottom = foot.Bottom;
            }
            return new Rect(anchor.x - cx * w, anchor.y - bottom * h, w, h);
        }

        // Where the opaque foot lands. Known frames return `anchor`.
        public static Vector2 Planted(string name, float texW, float texH, Vector2 anchor, float unit)
        {
            var dest = Dest(name, texW, texH, anchor, unit, 1f, 1f);
            float cx = 0.5f;
            float bottom = 1f;
            if (!string.IsNullOrEmpty(name) && _feet.TryGetValue(name, out var foot))
            {
                cx = foot.Cx;
                bottom = foot.Bottom;
            }
            return new Vector2(dest.x + cx * dest.width, dest.y + bottom * dest.height);
        }
    }
}

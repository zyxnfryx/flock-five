using UnityEngine;

namespace FlockFive
{
    // Shared push-button motion. Level, Claim, Go VIP, and every popup face use it.
    // Idle scale stays 1 (the old Go VIP sine was a quiver). A press sinks and
    // squashes; release springs past rest and settles. The hit rect is the layout
    // rect the whole time. The label rides the snapped visual top by a fixed pixel
    // offset, the same snap as the fill.
    public static class PushButton
    {
        public const int Level = 0;
        public const int Claim = 1;
        public const int Vip = 2;
        public const float ReleaseSeconds = 0.32f;
        public const float SheenPeriod = 1.40f; // b86: sweep about 2x faster
        public const float SinkFrac = 0.055f;

        public struct Pose
        {
            public float Sink;
            public float SquashX;
            public float SquashY;
        }

        public struct Motion
        {
            public Pose Pose;
            public bool PressedEdge;
            public bool ReleasedEdge;
        }

        struct Track
        {
            public bool Held;
            public bool Seen;
            public float ReleasedAt;
            public int PressFrame;
            public int ReleaseFrame;
            public int ThunkFrame;
            public bool Releasing;
        }

        static Track[] _tracks = new Track[24];

        // Shot hook. When set, At() returns this pose and idle scale stays 1.
        public static bool Force;
        public static Pose Forced;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _tracks = new Track[24];
            Force = false;
            Forced = default;
            _band = null;
            _bandPx = null;
        }

        public static float IdleScale(float time) => 1f;

        public static float SheenU(float time)
        {
            float period = SheenPeriod;
            if (period < 0.05f) period = 2.75f;
            return Mathf.Repeat(time < 0f ? 0f : time / period, 1f);
        }

        public static Pose Rest() => new Pose { Sink = 0f, SquashX = 1f, SquashY = 1f };

        // held sinks. releaseAge < 0 or past the spring is rest. Mid-spring overshoots up.
        public static Pose At(bool held, float releaseAge)
        {
            if (Force) return Forced.SquashX < 0.05f ? Rest() : Forced;
            if (held)
                return new Pose { Sink = SinkFrac, SquashX = 1.06f, SquashY = 0.88f };
            if (releaseAge < 0f || releaseAge >= ReleaseSeconds) return Rest();
            float u = releaseAge / ReleaseSeconds;
            const float peak = 0.42f;
            if (u < peak)
            {
                float e = Smooth(u / peak);
                return new Pose
                {
                    Sink = Mathf.Lerp(SinkFrac, -0.020f, e),
                    SquashX = Mathf.Lerp(1.06f, 0.96f, e),
                    SquashY = Mathf.Lerp(0.88f, 1.07f, e)
                };
            }
            float f = Smooth((u - peak) / (1f - peak));
            return new Pose
            {
                Sink = Mathf.Lerp(-0.020f, 0f, f),
                SquashX = Mathf.Lerp(0.96f, 1f, f),
                SquashY = Mathf.Lerp(1.07f, 1f, f)
            };
        }

        public static Rect HitRect(Rect layout) => layout;

        public static Rect Visual(Rect layout, Pose pose)
        {
            float sx = pose.SquashX < 0.05f ? 1f : pose.SquashX;
            float sy = pose.SquashY < 0.05f ? 1f : pose.SquashY;
            float w = layout.width * sx;
            float h = layout.height * sy;
            float x = layout.center.x - w * 0.5f;
            float y = layout.y + layout.height * pose.Sink + (layout.height - h) * 0.5f;
            return new Rect(x, y, w, h);
        }

        // Raised disc on the clay flower. Square LEVEL / Watch / Claim faces use it.
        public static Rect FlowerCap(Rect face)
        {
            return new Rect(
                face.x + face.width * 0.205f,
                face.y + face.height * 0.10f,
                face.width * 0.59f,
                face.height * 0.38f);
        }

        // The label keeps its center fraction on the cap. A squash widens the
        // plate around its center, so a top-left delta walks the word off the disc.
        // Width and height stay the rest size so the glyphs do not reflow.
        public static Rect Follow(Rect restLabel, Rect rest, Rect visual, float pixelsPerPoint)
        {
            float nx = rest.width > 1f ? (restLabel.center.x - rest.x) / rest.width : 0.5f;
            float ny = rest.height > 1f ? (restLabel.center.y - rest.y) / rest.height : 0.5f;
            float cx = visual.x + visual.width * nx;
            float cy = visual.y + visual.height * ny;
            return new Rect(
                LabelRim.Snap(cx - restLabel.width * 0.5f, pixelsPerPoint),
                LabelRim.Snap(cy - restLabel.height * 0.5f, pixelsPerPoint),
                restLabel.width,
                restLabel.height);
        }

        // A tutorial glove is a cue. Only a real pointer squashes the cap.
        public static bool Finger(bool pointerHeld, bool glovePosing)
        {
            if (glovePosing && !pointerHeld) return false;
            return pointerHeld;
        }

        public static int Slot(Rect r)
        {
            int h = (Mathf.RoundToInt(r.x) * 73856093) ^ (Mathf.RoundToInt(r.y) * 19349663)
                ^ (Mathf.RoundToInt(r.width) * 83492791);
            if (h < 0) h = -h;
            return 3 + (h % 20);
        }

        // effects plays the wooden thunk and the light haptic once on the press edge.
        public static Motion Sample(int slot, bool held, float now, bool effects)
        {
            if (slot < 0 || slot >= _tracks.Length) slot = 0;
            var tr = _tracks[slot];
            int frame = Time.frameCount;
            if (!tr.Seen || tr.Held != held)
            {
                if (held && tr.Seen && !tr.Held) tr.PressFrame = frame;
                if (!held && tr.Seen && tr.Held)
                {
                    tr.ReleaseFrame = frame;
                    tr.ReleasedAt = now;
                    tr.Releasing = true;
                }
                tr.Held = held;
                tr.Seen = true;
            }
            var motion = new Motion
            {
                PressedEdge = tr.PressFrame == frame,
                ReleasedEdge = tr.ReleaseFrame == frame
            };
            if (effects && motion.PressedEdge && tr.ThunkFrame != frame)
            {
                tr.ThunkFrame = frame;
                Sfx.ButtonThunk();
                Haptics.Play(Haptics.Tier.Light);
            }
            float age = -1f;
            if (!held && tr.Releasing) age = now - tr.ReleasedAt;
            motion.Pose = At(held, age);
            _tracks[slot] = tr;
            return motion;
        }

        static Texture2D _band;
        static Color32[] _bandPx;

        // Gloss band clipped to the button. u is 0..1 across one sweep. flash is the release pop.
        // Baked into one bilinear texture so the sweep is smooth, and zeroed outside an inset
        // ellipse so it cannot walk onto the card the way a rotated GUI quad does.
        public static void DrawSheen(Rect r, float u, float alpha, bool flash)
        {
            if (alpha < 0.02f || r.width < 8f || r.height < 8f) return;
            var tex = SheenTex(u, alpha, flash);
            if (tex == null) return;
            var prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        static Texture2D SheenTex(float u, float alpha, bool flash)
        {
            const int n = 128;
            if (_band == null)
            {
                _band = new Texture2D(n, n, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                    name = "PushButtonSheen"
                };
                _bandPx = new Color32[n * n];
            }
            float travel = Mathf.Lerp(-0.20f, 1.15f, Mathf.Clamp01(u));
            float width = flash ? 0.22f : 0.12f;
            float aScale = flash ? alpha : alpha * Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                float ey = (v - 0.50f) / 0.47f;
                for (int x = 0; x < n; x++)
                {
                    float t = (x + 0.5f) / n;
                    float ex = (t - 0.50f) / 0.47f;
                    float e2 = ex * ex + ey * ey;
                    int i = y * n + x;
                    if (e2 > 1f)
                    {
                        _bandPx[i] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float along = t + (v - 0.5f) * 0.48f;
                    float d = (along - travel) / width;
                    float band = Mathf.Exp(-d * d * 3.4f);
                    float rim = Mathf.Clamp01((1f - e2) * 5f);
                    byte a = (byte)Mathf.Clamp(band * rim * aScale * 230f, 0f, 255f);
                    _bandPx[i] = new Color32(255, 250, 228, a);
                }
            }
            _band.SetPixels32(_bandPx);
            _band.Apply(false);
            return _band;
        }

        static float Smooth(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }
    }
}

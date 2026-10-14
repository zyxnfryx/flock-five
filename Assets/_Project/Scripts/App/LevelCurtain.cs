using UnityEngine;

namespace FlockFive
{
    // Sparkles build out of LEVEL, wash the glass, then pull apart from the center
    // like a tear-away curtain. The garden loads behind that sheet. No frame is a
    // full-screen painted rect, and the reveal is not a left-to-right wipe.
    // The clock starts when LEVEL is pressed. Load begins on that same press.
    // Until the load is ready the clock holds in the wash, so a slow load waits
    // on the closed sheet instead of adding time after the tear.
    public static class LevelCurtain
    {
        public const float Duration = SparkleFx.CurtainDuration;
        public const float BuildEnd = SparkleFx.CurtainBuildEnd;
        public const float WashEnd = SparkleFx.CurtainWashEnd;
        // Mid-wash. VisualAge parks here until MarkReady, while the sheet is full.
        public const float Flood = (BuildEnd + WashEnd) * 0.5f;
        public const int Cols = 20;
        public const int Rows = 28;
        public const int Count = Cols * Rows;

        public static bool Armed { get; private set; }
        public static bool LoadStarted { get; private set; }
        public static bool Ready { get; private set; }
        public static float StartedAt { get; private set; }

        // Button the sheet leaves. Empty until Aim.
        public static Rect Source;

        // Shot clock. Negative uses the time the caller passes.
        public static float PreviewNow = -1f;

        public struct Glint
        {
            public float X, Y, Size, Alpha;
            public byte Hot;
        }

        static Glint[] _buf;
        // One step per rendered frame while a capture is locked to captureDeltaTime.
        // Wall time jumps when a screenshot encodes, and that was skipping the build.
        static int _capFrame = -1;
        static float _capClock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();

        public static void Reset()
        {
            Armed = false;
            LoadStarted = false;
            Ready = false;
            StartedAt = 0f;
            Source = default;
            PreviewNow = -1f;
            _capFrame = -1;
            _capClock = 0f;
        }

        public static void Clear() => Reset();

        // Pointer-down. Starts the curtain clock. Does not mark the load.
        public static void Arm(float now)
        {
            if (Armed) return;
            Armed = true;
            StartedAt = now;
        }

        // Where the sparkles come out of. The LEVEL flower passes its rest rect.
        public static void Aim(Rect button)
        {
            if (button.width > 2f && button.height > 2f) Source = button;
        }

        // Committed press, immediately before Load. The test reads this flag.
        public static void BeginLoad()
        {
            LoadStarted = true;
        }

        public static void MarkReady() => Ready = true;

        // PreviewNow wins. A locked capture then advances one captureDeltaTime per
        // frame, so a 1s tear is 60 frames even when encoding a shot is slow.
        public static float Clock(float now)
        {
            if (PreviewNow >= 0f) return PreviewNow;
            float step = Time.captureDeltaTime;
            if (step > 0f)
            {
                int frame = Time.frameCount;
                if (frame != _capFrame)
                {
                    _capClock = _capFrame < 0 ? now : _capClock + step;
                    _capFrame = frame;
                }
                return _capClock;
            }
            return now;
        }

        public static float VisualAge(float now)
        {
            if (!Armed) return 0f;
            float age = Clock(now) - StartedAt;
            if (age < 0f) age = 0f;
            if (!Ready && age > Flood) return Flood;
            if (age > Duration) return Duration;
            return age;
        }

        public static bool Finished(float now)
        {
            if (!Armed || !Ready) return false;
            return Clock(now) - StartedAt >= Duration;
        }

        // build, wash, part. Wash sits on Flood so a held load freezes on the full sheet.
        public static string BeatName(float age) => SparkleFx.CurtainPhase(age);

        public static void Draw(float now)
        {
            if (!Armed) return;
            float w = Screen.width;
            float h = Screen.height;
            if (w < 2f || h < 2f) return;
            if (_buf == null || _buf.Length < Count) _buf = new Glint[Count];
            int n = Fill(VisualAge(now), w, h, Source, _buf);
            if (n < 1) return;
            var prev = GUI.color;
            for (int i = 0; i < n; i++)
            {
                SparkleFx.DrawCurtainGlint(_buf[i].X, _buf[i].Y, _buf[i].Size, _buf[i].Alpha, _buf[i].Hot == 1);
            }
            GUI.color = prev;
        }

        // Individual glints in GUI pixels (y grows down). Nothing here is a screen fill.
        public static int Fill(float age, float screenW, float screenH, Rect sourcePx, Glint[] dst)
        {
            if (dst == null || dst.Length < 1 || screenW < 2f || screenH < 2f) return 0;
            if (age < 0f) age = 0f;
            float ox = 0.50f;
            float oy = 0.74f;
            if (sourcePx.width > 2f && sourcePx.height > 2f)
            {
                ox = sourcePx.center.x / screenW;
                oy = sourcePx.center.y / screenH;
            }
            if (ox < 0.04f) ox = 0.04f;
            else if (ox > 0.96f) ox = 0.96f;
            if (oy < 0.08f) oy = 0.08f;
            else if (oy > 0.94f) oy = 0.94f;
            float minSide = screenW < screenH ? screenW : screenH;
            int n = 0;
            int cap = dst.Length;
            for (int i = 0; i < Count && n < cap; i++)
            {
                if (!Place(i, age, ox, oy, out float nx, out float ny, out float alpha, out float sizeN, out bool hot))
                    continue;
                dst[n++] = new Glint
                {
                    X = nx * screenW,
                    Y = ny * screenH,
                    Size = sizeN * minSide,
                    Alpha = alpha,
                    Hot = hot ? (byte)1 : (byte)0
                };
            }
            return n;
        }

        // Normalized GUI space. y = 0 is the top. Seats are a sheet; the tear
        // moves that sheet with SparkleFx.CurtainShiftX.
        static bool Place(int i, float age, float ox, float oy,
            out float nx, out float ny, out float alpha, out float sizeN, out bool hot)
        {
            nx = ny = alpha = sizeN = 0f;
            hot = false;
            int col = i % Cols;
            int row = i / Cols;
            float h1 = Hash(i, 1);
            float h2 = Hash(i, 2);
            float h4 = Hash(i, 4);
            float seatX = (col + 0.5f) / Cols + (h1 - 0.5f) * (0.28f / Cols);
            float seatY = (row + 0.5f) / Rows + (h2 - 0.5f) * (0.28f / Rows);
            float dx = seatX - ox;
            float dy = seatY - oy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            // Near seats arrive first. The far corners land as build ends, so the
            // wash is the first full sheet.
            float arrive = Mathf.Lerp(0.05f, BuildEnd, Mathf.Clamp01(dist / 1.05f));
            float born = arrive * 0.22f;
            if (age < born) return false;
            float travel = arrive - born;
            if (travel < 0.045f) travel = 0.045f;
            float u = (age - born) / travel;
            if (u < 0f) u = 0f;
            else if (u > 1f) u = 1f;
            float e = u * u * (3f - 2f * u);
            float x = ox + (seatX - ox) * e;
            float y = oy + (seatY - oy) * e;
            // Vertical only. A sideways drift would turn the tear into a wipe.
            y += Mathf.Sin(age * 8.5f + h4 * 6.2831853f) * 0.0045f;
            float open = SparkleFx.CurtainOpen(age);
            x += SparkleFx.CurtainShiftX(seatX, open);
            if (x < -0.04f || x > 1.04f || y < -0.04f || y > 1.04f) return false;
            nx = x;
            ny = y;
            hot = h2 > 0.78f;
            sizeN = (hot ? 0.092f : 0.118f) + 0.012f * h4;
            sizeN *= 0.55f + 0.45f * e;
            float tw = 0.90f + 0.10f * Mathf.Sin(age * 11f + h4 * 6.283f);
            if (tw < 0.85f) tw = 0.85f;
            float fadeIn = (age - born) / 0.04f;
            if (fadeIn > 1f) fadeIn = 1f;
            else if (fadeIn < 0f) fadeIn = 0f;
            float fadeOut = 1f;
            if (open > 0.92f) fadeOut = (1f - open) / 0.08f;
            if (fadeOut < 0f) fadeOut = 0f;
            else if (fadeOut > 1f) fadeOut = 1f;
            alpha = tw * fadeIn * fadeOut * (0.72f + 0.28f * e);
            if (alpha < 0.04f) return false;
            return true;
        }

        static float Hash(int i, int salt)
        {
            uint x = (uint)(i * 374761393 + salt * 668265263 + 17);
            x = (x ^ (x >> 13)) * 1274126177u;
            x ^= x >> 16;
            return (x & 65535) / 65535f;
        }
    }
}

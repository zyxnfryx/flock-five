using UnityEngine;

namespace FlockFive
{
    // Gold sparkle flood that tears off to show the garden. The clock starts when
    // LEVEL is pressed. Load begins on that same press. Until the load is ready the
    // tear holds at full cover, so a fast load adds no extra wait after the flood.
    public static class LevelCurtain
    {
        public const float Duration = 1.0f;
        public const float Flood = 0.40f;

        public static bool Armed { get; private set; }
        public static bool LoadStarted { get; private set; }
        public static bool Ready { get; private set; }
        public static float StartedAt { get; private set; }

        // Shot clock. Negative uses the time the caller passes.
        public static float PreviewNow = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();

        public static void Reset()
        {
            Armed = false;
            LoadStarted = false;
            Ready = false;
            StartedAt = 0f;
            PreviewNow = -1f;
        }

        public static void Clear() => Reset();

        // Pointer-down. Starts the flood clock. Does not mark the load.
        public static void Arm(float now)
        {
            if (Armed) return;
            Armed = true;
            StartedAt = now;
        }

        // Committed press, immediately before Load. The test reads this flag.
        public static void BeginLoad()
        {
            LoadStarted = true;
        }

        public static void MarkReady() => Ready = true;

        public static float Clock(float now) => PreviewNow >= 0f ? PreviewNow : now;

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

        public static void Draw(float now)
        {
            if (!Armed) return;
            float age = VisualAge(now);
            float dur = Duration < 0.05f ? 1f : Duration;
            float u = Mathf.Clamp01(age / dur);
            float floodU = Mathf.Clamp(Flood / dur, 0.05f, 0.9f);
            float cover;
            float tear = 0f;
            if (u < floodU)
                cover = Mathf.SmoothStep(0f, 1f, u / floodU);
            else
            {
                cover = 1f;
                tear = (u - floodU) / (1f - floodU);
            }
            if (cover < 0.02f) return;
            float y = tear * Screen.height;
            float h = Mathf.Max(0f, Screen.height - y);
            if (h < 1f) return;
            var tex = Texture2D.whiteTexture;
            if (tex == null) return;
            GUI.color = new Color(1f, 0.78f, 0.22f, 0.94f * cover);
            GUI.DrawTexture(new Rect(0f, y, Screen.width, h), tex);
            GUI.color = new Color(1f, 0.94f, 0.62f, 0.35f * cover * (1f - tear));
            float band = Mathf.Max(8f, h * 0.08f);
            GUI.DrawTexture(new Rect(0f, y, Screen.width, band), tex);
            GUI.color = Color.white;
            if (tear < 0.92f)
            {
                var spark = new Rect(0f, y, Screen.width, Mathf.Max(h, Screen.height * 0.35f));
                SparkleFx.DrawAround(spark, cover * (1f - tear * 0.5f), true, 0.05f, true);
            }
        }
    }
}

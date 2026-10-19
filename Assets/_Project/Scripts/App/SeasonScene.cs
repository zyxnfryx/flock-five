using UnityEngine;

namespace FlockFive
{
    // Short visual season change (replaces the old "Welcome to the <season>" text banner).
    // Plays once, the first time a new season's garden loads. Pure rules and timeline live here
    // so the editor test can cover them without a scene. SeasonCrossfade drives it at runtime.
    //
    // Timeline (seconds), all tunables below:
    //   0.0-0.8  sky palette wipe (direction and tint per season)
    //   0.4-1.6  season particles enter (GardenLife burst sheet)
    //   1.0      bird reaction starts + short sting (Sfx.SeasonSting hook)
    //   1.6-2.5  particles settle into the ambient look; small season label fades in and out
    // Gates: ostrich gardens never get a scene; no scene during an active tutorial (an
    // already-playing scene is cut short if a tutorial starts); a tap skips; a badger action
    // finishes the scene first (SeasonCrossfade.FinishLive).
    public static class SeasonScene
    {
        // ---- tunables ----
        public const float Duration = 2.5f;
        public const float WipeEnd = 0.8f;
        public const float ParticlesInAt = 0.4f;
        public const float ParticlesFullAt = 1.6f;
        public const float BirdAt = 1.0f;
        public const float StingAt = 1.0f;
        public const float SettleStart = 1.6f;
        public const float LabelFadeIn = 0.35f;   // 1.6 -> 1.95
        public const float LabelFadeOut = 0.35f;  // 2.15 -> 2.5
        public const float WipeFeather = 0.13f;
        public const float VeilStrength = 0.20f;  // peak tint alpha over the wipe
        // Burst particle counts (extra quads on top of the ambient look), full / low-power phones.
        public const int BurstCount = 14;
        public const int BurstCountLow = 7;
        // Max bird phase delay between neighbours, seconds (keeps the whole flock inside Duration).
        public const float BirdStagger = 0.05f;

        public enum Skip
        {
            None = 0,
            NotSeason = 1,
            Ostrich = 2,
            Tutorial = 3,
        }

        // Wipe modes understood by FoliageSway.SetWipe / the shader.
        public const int WipeFromTop = 0;
        public const int WipeFromSide = 1;
        public const int WipeFromBottom = 2;
        public const int WipeFromHorizon = 3;

        // Set while a scene is running (SeasonCrossfade). Read by the badger gate.
        public static bool Live;
        // App writes this each frame: any tutorial step / glove is up.
        public static bool TutorialLive;

        public static bool HasScene(GardenScene s) =>
            s == GardenScene.Spring || s == GardenScene.Summer || s == GardenScene.Fall || s == GardenScene.Winter;

        // The one gate. Ostrich first (never, no matter what), then tutorial.
        public static Skip Gate(GardenScene scene, bool ostrichGarden, bool tutorialLive)
        {
            if (!HasScene(scene)) return Skip.NotSeason;
            if (ostrichGarden) return Skip.Ostrich;
            if (tutorialLive) return Skip.Tutorial;
            return Skip.None;
        }

        // Badger actions wait for a finished scene (or none).
        public static bool BadgerMayStart(SeasonClock clock) => clock == null || clock.Done;

        public static string Label(GardenScene s)
        {
            switch (s)
            {
                case GardenScene.Spring: return "Spring";
                case GardenScene.Summer: return "Summer";
                case GardenScene.Fall: return "Fall";
                case GardenScene.Winter: return "Winter";
                default: return "";
            }
        }

        public static int WipeMode(GardenScene s)
        {
            switch (s)
            {
                case GardenScene.Spring: return WipeFromBottom;
                case GardenScene.Summer: return WipeFromHorizon;
                case GardenScene.Fall: return WipeFromSide;
                default: return WipeFromTop;
            }
        }

        // Palette tint laid over the wipe (alpha is set per frame).
        public static Color Palette(GardenScene s)
        {
            switch (s)
            {
                case GardenScene.Spring: return new Color(0.86f, 0.96f, 0.84f, 1f); // pale pink-green
                case GardenScene.Summer: return new Color(1.00f, 0.86f, 0.46f, 1f); // warm gold
                case GardenScene.Fall: return new Color(1.00f, 0.62f, 0.26f, 1f);   // amber-orange
                default: return new Color(0.82f, 0.92f, 1.00f, 1f);                 // cool blue-white
            }
        }

        // Spring wipe is pink-green: the tint blends from pink to green with the edge.
        public static Color PaletteB(GardenScene s) =>
            s == GardenScene.Spring ? new Color(1.00f, 0.82f, 0.88f, 1f) : Palette(s);

        // ---- timeline curves, all 0..1 ----
        public static float Wipe(float t) => Mathf.Clamp01(t / WipeEnd);

        public static float ParticleIn(float t) =>
            Smooth(Mathf.Clamp01((t - ParticlesInAt) / (ParticlesFullAt - ParticlesInAt)));

        public static float Settle(float t) =>
            Smooth(Mathf.Clamp01((t - SettleStart) / (Duration - SettleStart)));

        // Burst sheet alpha: in over 0.4-1.6, out over 1.6-2.5 (hands over to the ambient sheets).
        public static float Burst(float t) => ParticleIn(t) * (1f - Settle(t));

        // Speed multiplier on ambient drift: energetic on entry, calm once settled.
        public static float Energy(float t) => Mathf.Lerp(1.9f, 1f, Settle(t)) * Mathf.Lerp(0.6f, 1f, ParticleIn(t));

        public static float LabelAlpha(float t)
        {
            float a = Mathf.Clamp01((t - SettleStart) / LabelFadeIn);
            float o = Mathf.Clamp01((Duration - t) / LabelFadeOut);
            return Mathf.Min(a, o);
        }

        // 0..1 progress through the bird reaction, or -1 before it starts.
        public static float BirdPhase(float t) =>
            t < BirdAt ? -1f : Mathf.Clamp01((t - BirdAt) / (Duration - BirdAt));

        static float Smooth(float x) => x * x * (3f - 2f * x);

        // ---- bird reaction ----
        public struct Pose
        {
            public float Dx, Dy, Rot, Sx, Sy;
            public static Pose Rest => new Pose { Sx = 1f, Sy = 1f };
        }

        // u = 0..1 reaction progress (already staggered per bird). Tapers to rest at u = 1.
        public static Pose BirdPose(GardenScene s, float u)
        {
            var p = Pose.Rest;
            if (u < 0f) return p;
            u = Mathf.Clamp01(u);
            float taper = 1f - u * u * u;
            switch (s)
            {
                case GardenScene.Spring:
                    // Happy hops: three bounces.
                    p.Dy = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 3f)) * 0.14f * taper;
                    p.Sy = 1f + 0.05f * Mathf.Sin(u * Mathf.PI * 6f) * taper;
                    break;
                case GardenScene.Summer:
                {
                    // Stretch up, then puff out.
                    float stretch = Mathf.Sin(Mathf.Clamp01(u / 0.5f) * Mathf.PI);
                    float puff = Mathf.Sin(Mathf.Clamp01((u - 0.4f) / 0.6f) * Mathf.PI);
                    p.Sy = 1f + 0.12f * stretch + 0.08f * puff;
                    p.Sx = 1f - 0.04f * stretch + 0.12f * puff;
                    p.Dy = 0.03f * stretch;
                    break;
                }
                case GardenScene.Fall:
                {
                    // Look around (slow head turns), then a quick shake to drop a leaf.
                    p.Rot = 10f * Mathf.Sin(u * Mathf.PI * 2.5f) * (1f - u);
                    float shake = Mathf.Clamp01((u - 0.55f) / 0.25f) * Mathf.Clamp01((0.85f - u) / 0.05f);
                    p.Rot += 9f * Mathf.Sin(u * 70f) * shake;
                    p.Dx = 0.012f * Mathf.Sin(u * 70f) * shake;
                    break;
                }
                default:
                {
                    // Shiver, then fluff.
                    const float split = 0.5f;
                    if (u < split)
                    {
                        p.Dx = 0.014f * Mathf.Sin(u * 110f);
                        p.Sy = 1f - 0.03f * Mathf.Abs(Mathf.Sin(u * 55f));
                    }
                    else
                    {
                        float f = Mathf.Sin(Mathf.Clamp01((u - split) / (1f - split)) * Mathf.PI);
                        p.Sx = 1f + 0.14f * f;
                        p.Sy = 1f + 0.10f * f;
                    }
                    break;
                }
            }
            return p;
        }
    }

    // Pure clock for one scene. No Unity objects, so the editor test can step it.
    public sealed class SeasonClock
    {
        public float T { get; private set; }
        public bool Done { get; private set; }
        public bool Skipped { get; private set; }
        bool _stingTaken;
        bool _birdsTaken;

        // A tutorial that starts mid-scene ends it at once (the new painting stays).
        public void Tick(float dt, bool tutorialLive)
        {
            if (Done) return;
            if (tutorialLive) { Skip(); return; }
            T += dt;
            if (T >= SeasonScene.Duration)
            {
                T = SeasonScene.Duration;
                Done = true;
            }
        }

        // Tap, tutorial, or a badger action: finish immediately.
        public void Skip()
        {
            if (Done) return;
            T = SeasonScene.Duration;
            Done = true;
            Skipped = true;
        }

        // True exactly once, on the first tick at or past 1.0 s, never after a skip.
        public bool TakeSting()
        {
            if (_stingTaken || Skipped || T < SeasonScene.StingAt) return false;
            _stingTaken = true;
            return true;
        }

        public bool TakeBirds()
        {
            if (_birdsTaken || Skipped || T < SeasonScene.BirdAt) return false;
            _birdsTaken = true;
            return true;
        }
    }
}

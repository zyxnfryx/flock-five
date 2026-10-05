using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Honey badger contest, Phase 4: the timelines for the leap, the hive swipe and bee
    // fill, and the splash sitter rule. Pure timing and rules (plus the one coroutine that
    // walks the leap clock), so editor tests can check them. FlockFiveApp.BadgerShow.cs
    // draws them. Mild and cartoonish only: hops, a honey splat, startled bees.
    // Every clock here is PlayClock, so a pause or a backgrounded app holds the show
    // instead of skipping it, and the resume frame cannot jump ahead.

    public enum BadgerLeapBeat { Hop = 0, Crouch = 1, Leap = 2, Wash = 3, Done = 4 }

    // State for the one leap. The app owns a single instance; the coroutine fills it in
    // and the overlay reads it. Live stays true after the clock ends (wash held at full)
    // until the caller releases it, so the contest page comes up under a full wash with
    // no flash of the garden or splash in between.
    public sealed class BadgerLeapRun
    {
        public bool Live;
        public bool Done;
        public bool Stung;
        public float T;
        public float StartedAt;

        public void Release()
        {
            Live = false;
            Done = false;
            Stung = false;
            T = 0f;
        }
    }

    public static class BadgerLeap
    {
        // Beat lengths. Hop 0.0-0.8, crouch 0.8-1.4, leap 1.4-2.6, wash 2.6-3.0.
        public const float HopSeconds = 0.8f;
        public const float CrouchSeconds = 0.6f;
        public const float LeapSeconds = 1.2f;
        public const float WashSeconds = 0.4f;
        public static readonly float[] Beats = { HopSeconds, CrouchSeconds, LeapSeconds, WashSeconds };

        // Two hops in the hop beat. Each hop: airborne frame, then the landing frame.
        public const float HopStride = 0.4f;
        public const float HopAirShare = 0.6f;

        // Leap frames by share of the leap beat: leap_0 (takeoff), leap_1, leap_3.
        public static readonly string[] LeapFrames = { "badger_leap_0", "badger_leap_1", "badger_leap_3" };
        public static readonly float[] LeapFrameEnds = { 0.28f, 0.62f, 1f };

        // The overlay fades the wash out over this once the contest page is up.
        public const float WashOutSeconds = 0.30f;

        public static float Duration
        {
            get
            {
                float t = 0f;
                for (int i = 0; i < Beats.Length; i++) t += Beats[i];
                return t;
            }
        }

        public static float BeatStart(BadgerLeapBeat beat)
        {
            float t = 0f;
            int n = (int)beat;
            if (n > Beats.Length) n = Beats.Length;
            for (int i = 0; i < n; i++) t += Beats[i];
            return t;
        }

        // Takeoff: the "badger" sting fires here.
        public static float TakeoffAt => BeatStart(BadgerLeapBeat.Leap);

        public static BadgerLeapBeat BeatAt(float t)
        {
            if (t < 0f) t = 0f;
            float edge = 0f;
            for (int i = 0; i < Beats.Length; i++)
            {
                edge += Beats[i];
                if (t < edge) return (BadgerLeapBeat)i;
            }
            return BadgerLeapBeat.Done;
        }

        // 0..1 inside the current beat (1 once Done).
        public static float Local(float t)
        {
            var beat = BeatAt(t);
            if (beat == BadgerLeapBeat.Done) return 1f;
            float len = Beats[(int)beat];
            if (len <= 0f) return 1f;
            return Mathf.Clamp01((t - BeatStart(beat)) / len);
        }

        // Sprite name for this moment. Every badger frame faces left.
        public static string FrameAt(float t)
        {
            var beat = BeatAt(t);
            if (beat == BadgerLeapBeat.Hop)
            {
                float phase = Mathf.Repeat(t, HopStride) / HopStride;
                return phase < HopAirShare ? "badger_hop_a" : "badger_hop_b";
            }
            if (beat == BadgerLeapBeat.Crouch) return "badger_crouch";
            if (beat == BadgerLeapBeat.Leap)
            {
                float u = Local(t);
                for (int i = 0; i < LeapFrameEnds.Length; i++)
                    if (u < LeapFrameEnds[i]) return LeapFrames[i];
            }
            return LeapFrames[LeapFrames.Length - 1];
        }

        // 0..1 travel from the start anchor to the landing spot (eased, done by the crouch).
        public static float HopTravel(float t)
        {
            float u = Mathf.Clamp01(t / HopSeconds);
            return 1f - (1f - u) * (1f - u);
        }

        // 0..1 height of the current hop arc (0 on the ground).
        public static float HopLift(float t)
        {
            if (BeatAt(t) != BadgerLeapBeat.Hop) return 0f;
            float phase = Mathf.Repeat(t, HopStride) / HopStride;
            if (phase >= HopAirShare) return 0f;
            return Mathf.Sin(phase / HopAirShare * Mathf.PI);
        }

        // 0 at takeoff, 1 when the toward-camera frame fills the screen. Eases in so the
        // badger seems to speed up as it comes at the player. Stays 1 through the wash.
        public static float Grow(float t)
        {
            var beat = BeatAt(t);
            if (beat < BadgerLeapBeat.Leap) return 0f;
            if (beat > BadgerLeapBeat.Leap) return 1f;
            float u = Local(t);
            float e = u * u * (3f - 2f * u);
            return e * u;
        }

        // Crouch squash: 0..1, deepest right at takeoff.
        public static float CrouchDip(float t)
        {
            if (BeatAt(t) != BadgerLeapBeat.Crouch) return 0f;
            return Mathf.Sin(Local(t) * Mathf.PI * 0.5f);
        }

        // Honey-gold wash over the whole screen: 0 before the wash, 1 at the end.
        public static float WashAlpha(float t)
        {
            var beat = BeatAt(t);
            if (beat < BadgerLeapBeat.Wash) return 0f;
            if (beat == BadgerLeapBeat.Done) return 1f;
            float u = Local(t);
            return u * u * (3f - 2f * u);
        }

        // True on the one step that crosses takeoff.
        public static bool CrossesTakeoff(float before, float after)
        {
            return before < TakeoffAt && after >= TakeoffAt;
        }

        // Interruption rule (same idea as TutorialHeal): a leap still marked live well past
        // its end has lost its coroutine (killed mid-show). The app drops it and frees taps.
        public static bool Stuck(bool live, float elapsed)
        {
            return live && elapsed > Duration + TutorialHeal.MaxGateSeconds;
        }

        // THE leap. Both entries (a fresh clear, still in the garden, and the flower tap on
        // the splash) yield on this. Not skippable. The caller holds _busy and releases
        // the run once the contest page is up. Does nothing to the flag.
        public static IEnumerator Play(BadgerLeapRun run)
        {
            if (run == null) yield break;
            run.Live = true;
            run.Done = false;
            run.Stung = false;
            run.T = 0f;
            run.StartedAt = PlayClock.Now;
            float end = Duration;
            while (run.Live && run.T < end)
            {
                yield return null;
                if (!run.Live) yield break;
                float before = run.T;
                run.T = Mathf.Min(end, run.T + PlayClock.Delta);
                if (!run.Stung && CrossesTakeoff(before, run.T))
                {
                    run.Stung = true;
                    SfxLibrary.Badger();
                }
            }
            run.T = end;
            run.Done = true;
        }
    }

    // The contest opening: the badger swipes the hive, bees fly out and land one per
    // player tile in grid order. Runs every time the contest screen opens. Not skippable.
    public static class BadgerOpening
    {
        // 0.0-0.5 the swipe (hit at 0.25: hive swaps to hive_swiped, splat, wobble).
        public const float SwipeSeconds = 0.50f;
        public const float SwipeHitAt = 0.25f;
        public const float SplatSeconds = 0.35f;
        public const float WobbleSeconds = 0.60f;
        public const float WobbleDegrees = 7f;
        // One bee leaves every Stagger; each flies for FlightSeconds.
        public const float Stagger = 0.10f;
        public const float FlightSeconds = 0.35f;
        // Flip of the landed tile, inside the last share of the flight.
        public const float FlipSeconds = 0.18f;
        // After the last bee lands: hive and badger fade. PostSeconds then holds the line
        // before the badger picks first (no swap / FIGHT gate).
        public const float SettleSeconds = 0.15f;
        // Short beat after the bees land, before the badger picks first.
        public const float PostSeconds = 0.6f;
        public const int BeeArt = 5;
        // Padding (yard honey) tiles still get a bee, drawn smaller.
        public const float YardBeeScale = 0.72f;

        public static int Landings => BadgerSchedule.Tiles;

        // Grid order: tile 0 first (top-left of the 4x4), row by row.
        public static int TileAt(int order) => order;

        // Inverse of TileAt: which landing fills this tile.
        public static int OrderOf(int tile) => tile;

        public static int[] Order()
        {
            var o = new int[Landings];
            for (int i = 0; i < o.Length; i++) o[i] = TileAt(i);
            return o;
        }

        public static float LaunchAt(int order) => SwipeSeconds + order * Stagger;

        public static float LandAt(int order) => LaunchAt(order) + FlightSeconds;

        public static float Duration => LandAt(Landings - 1) + SettleSeconds;

        // bee_1..bee_5, cycled by landing order. Names are built once: this runs for
        // every bee on every OnGUI pass of the opening.
        static string[] _beeNames;

        public static string BeeFrame(int order)
        {
            int k = order % BeeArt;
            if (k < 0) k += BeeArt;
            if (_beeNames == null)
            {
                var names = new string[BeeArt];
                for (int i = 0; i < names.Length; i++) names[i] = "bee_" + (i + 1);
                _beeNames = names;
            }
            return _beeNames[k];
        }

        public static float BeeScale(bool yard) => yard ? YardBeeScale : 1f;

        // 0 at the hive, 1 on the tile. -1 before launch; 2 once landed (bee gone).
        public static float Flight(int order, float t)
        {
            float a = LaunchAt(order);
            if (t < a) return -1f;
            if (t >= LandAt(order)) return 2f;
            return Mathf.Clamp01((t - a) / FlightSeconds);
        }

        // Tile state for the shared plate: 0 = empty cell, 0..1 = flipping, 1 = final face.
        public static float TileLand(int order, float t)
        {
            float land = LandAt(order);
            float from = land - FlipSeconds;
            if (t <= from) return 0f;
            if (t >= land) return 1f;
            return Mathf.Clamp01((t - from) / FlipSeconds);
        }

        public static int LandedCount(float t)
        {
            int n = 0;
            for (int i = 0; i < Landings; i++)
                if (t >= LandAt(i)) n++;
            return n;
        }

        public static bool HiveSwiped(float t) => t >= SwipeHitAt;

        public static float SplatAlpha(float t)
        {
            float u = (t - SwipeHitAt) / SplatSeconds;
            if (u < 0f || u > 1f) return 0f;
            return 1f - u * u;
        }

        // Damped wobble after the hit, in degrees.
        public static float Wobble(float t)
        {
            float u = (t - SwipeHitAt) / WobbleSeconds;
            if (u < 0f || u > 1f) return 0f;
            return Mathf.Sin(u * Mathf.PI * 5f) * WobbleDegrees * (1f - u);
        }

        // Badger paw swing toward the hive: 0..1..0 across the swipe.
        public static float Swing(float t)
        {
            if (t < 0f || t > SwipeSeconds) return 0f;
            return Mathf.Sin(t / SwipeSeconds * Mathf.PI);
        }

        // Hive and badger fade over the settle beat once the last bee has landed.
        public static float CastAlpha(float t)
        {
            float from = LandAt(Landings - 1);
            if (t <= from) return 1f;
            return Mathf.Clamp01(1f - (t - from) / SettleSeconds);
        }
    }

    public enum BadgerSlamBeat { Words = 0, Speed = 1, ZoomIn = 2, Nasty = 3, ZoomOut = 4, Done = 5 }

    // "Honey Badger Don't Care" critical-hit beat (spec 2b), about 2.2 s:
    // 1 word slam Honey -> Badger -> Don't -> Care on a dark wash, 2 speed lines, 3 hard zoom
    // into the badger's eye with a sparkle glint, 4 the nasty move (honey_splat + shrug),
    // 5 zoom back out, then the round resolves. Pure timing and shapes; BadgerFight.TryDontCare
    // decides the hit and the effect, FlockFiveApp.PlayDontCareSlam draws it.
    // Zoom(t) is exactly 1 outside the zoom beats and once Done, and the app derives the GUI
    // scale from it every frame (nothing is stored), so the contest always returns to 1:1.
    public static class BadgerSlam
    {
        public const float WordSeconds = 0.22f;
        public const float SpeedSeconds = 0.30f;
        public const float ZoomInSeconds = 0.45f;
        public const float NastySeconds = 0.22f;
        public const float ZoomOutSeconds = 0.35f;

        // Each word lands big and settles to 1 over this share of its slot.
        public const float PopShare = 0.45f;
        public const float PopFrom = 1.60f;
        public const float WashMax = 0.62f;
        public const float WashInSeconds = 0.08f;
        public const float FaceInSeconds = 0.10f;
        public const float ZoomMax = 3.2f;
        public const int SpeedLineCount = 22;

        // Close-up frame for the eye zoom (toward-camera leap frame; no new art) and the
        // pupil of its left eye as a share of the frame (0,0 = top-left).
        public const string FaceFrame = "badger_leap_3";
        public const float EyeU = 0.442f;
        public const float EyeV = 0.266f;

        public static int Words => BadgerCopy.DontCareWordCount;
        public static float WordsEnd => Words * WordSeconds;
        public static float SpeedEnd => WordsEnd + SpeedSeconds;
        public static float ZoomInEnd => SpeedEnd + ZoomInSeconds;
        // The nasty move lands here (power-up destroyed or the halve shown).
        public static float NastyAt => ZoomInEnd;
        public static float NastyEnd => NastyAt + NastySeconds;
        public static float Duration => NastyEnd + ZoomOutSeconds;

        public static bool Done(float t) => t >= Duration;

        public static BadgerSlamBeat BeatAt(float t)
        {
            if (t < WordsEnd) return BadgerSlamBeat.Words;
            if (t < SpeedEnd) return BadgerSlamBeat.Speed;
            if (t < ZoomInEnd) return BadgerSlamBeat.ZoomIn;
            if (t < NastyEnd) return BadgerSlamBeat.Nasty;
            if (t < Duration) return BadgerSlamBeat.ZoomOut;
            return BadgerSlamBeat.Done;
        }

        static float Ease(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }

        // True on the one step that crosses `at`.
        public static bool Crosses(float before, float after, float at)
        {
            return before < at && after >= at;
        }

        // -1 before the first word and after the word beat.
        public static int WordIndexAt(float t)
        {
            if (t < 0f || t >= WordsEnd) return -1;
            int i = Mathf.FloorToInt(t / WordSeconds);
            int last = Words - 1;
            return i > last ? last : i;
        }

        public static string WordAt(float t)
        {
            return BadgerCopy.DontCareWordAt(WordIndexAt(t));
        }

        // Scale of the current word: PopFrom on its first frame, 1 once settled.
        public static float Pop(float t)
        {
            int i = WordIndexAt(t);
            if (i < 0) return 1f;
            float u = Mathf.Clamp01((t - i * WordSeconds) / (WordSeconds * PopShare));
            float e = 1f - (1f - u) * (1f - u);
            return PopFrom + (1f - PopFrom) * e;
        }

        // Dark wash: quick in, held to the zoom-out, then eased to 0 by the end.
        public static float WashAlpha(float t)
        {
            if (t <= 0f || t >= Duration) return 0f;
            float a = Mathf.Clamp01(t / WashInSeconds);
            if (t > NastyEnd) a = Mathf.Min(a, 1f - Ease((t - NastyEnd) / ZoomOutSeconds));
            return a * WashMax;
        }

        // Close-up badger face: pops in with the speed lines, fades out with the zoom-out.
        public static float FaceAlpha(float t)
        {
            if (t < WordsEnd || t >= Duration) return 0f;
            float a = Mathf.Clamp01((t - WordsEnd) / FaceInSeconds);
            if (t > NastyEnd) a = Mathf.Min(a, 1f - Ease((t - NastyEnd) / ZoomOutSeconds));
            return a;
        }

        // Camera scale around the eye. 1 until the zoom-in, a hard ease-in to ZoomMax,
        // held through the nasty move, smoothly back to exactly 1.
        public static float Zoom(float t)
        {
            var beat = BeatAt(t);
            if (beat == BadgerSlamBeat.ZoomIn)
            {
                float u = Mathf.Clamp01((t - SpeedEnd) / ZoomInSeconds);
                return 1f + (ZoomMax - 1f) * u * u * u;
            }
            if (beat == BadgerSlamBeat.Nasty) return ZoomMax;
            if (beat == BadgerSlamBeat.ZoomOut)
                return ZoomMax + (1f - ZoomMax) * Ease((t - NastyEnd) / ZoomOutSeconds);
            return 1f;
        }

        // Speed lines: full in the speed beat, fading over the first half of the zoom-in.
        public static float SpeedLines(float t)
        {
            if (t < WordsEnd || t >= ZoomInEnd) return 0f;
            if (t < SpeedEnd) return 1f;
            return 1f - Mathf.Clamp01((t - SpeedEnd) / (ZoomInSeconds * 0.5f));
        }

        // One speed line: angle (degrees), inner and outer radius as shares of the screen
        // diagonal. Deterministic per index; the lines rush inward over time.
        public static void SpeedLine(int i, float t, out float degrees, out float inner, out float outer)
        {
            int n = SpeedLineCount < 1 ? 1 : SpeedLineCount;
            float jitter = ((i * 7919) % 13) / 13f;
            degrees = i * 360f / n + jitter * 9f;
            float rush = Mathf.Repeat((t - WordsEnd) * 3.4f + jitter, 1f);
            inner = 0.16f + 0.10f * (1f - rush) + 0.05f * jitter;
            outer = 0.62f + 0.10f * jitter;
        }

        // White glint on the pupil: rises over the second half of the zoom-in, holds through
        // the nasty move, gone as the zoom-out starts.
        public static float Glint(float t)
        {
            float from = SpeedEnd + ZoomInSeconds * 0.5f;
            if (t < from || t >= NastyEnd) return 0f;
            return Mathf.Clamp01((t - from) / (ZoomInSeconds * 0.5f));
        }

        // The nasty move's honey_splat + shrug, from NastyAt to the end of the beat.
        public static bool ShowNasty(float t) => t >= NastyAt && t < Duration;

        // Same interruption rule as BadgerLeap.Stuck: a slam still live well past its end
        // lost its driver; the app finishes it and the scale is 1 again.
        public static bool Stuck(bool live, float elapsed)
        {
            return live && elapsed > Duration + TutorialHeal.MaxGateSeconds;
        }
    }

    // When the badger sits on the splash play flower. Pure, so a test covers the switch.
    public static class BadgerSitter
    {
        // Idle A and idle B, ~0.6 s each, both bottom-aligned.
        public const float BreatheSeconds = 0.6f;

        public static bool Visible(int pending, bool leapLive, bool shotHidden)
        {
            return pending > 0 && !leapLive && !shotHidden;
        }

        // Kill-switch aware: reads the flag through BadgerSave, so Enabled false hides it.
        public static bool VisibleFor(bool enabled, bool leapLive, bool shotHidden)
        {
            return Visible(BadgerSave.PendingFor(enabled), leapLive, shotHidden);
        }

        public static string FrameAt(float now)
        {
            int k = Mathf.FloorToInt(now / BreatheSeconds);
            return (k & 1) == 0 ? "badger_idle_a" : "badger_idle_b";
        }
    }
}

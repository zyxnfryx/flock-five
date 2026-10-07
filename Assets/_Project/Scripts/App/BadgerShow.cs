using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Fight-screen stages. One list so the show and the editor test agree. Both fighters
    // draw in every stage; nothing here fades the badger out with the hive.
    public enum BadgerStage
    {
        PostOpen, BossWait, YourPick, Reveal, Verdict, Outro, Over, Opening, Lesson, DontCare
    }

    // Honey badger contest, Phase 4: the timelines for the leap, the hive swipe and bee
    // fill, and the splash sitter rule. Pure timing and rules (plus the one coroutine that
    // walks the leap clock), so editor tests can check them. FlockFiveApp.BadgerShow.cs
    // draws them. Mild and cartoonish only: a claw swipe, a honey splat, startled bees.
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
        // 0.0-0.5 the swipe. The hit is BadgerSwipe.HitAt (hive swaps to hive_swiped,
        // splat, wobble). BadgerSwipe owns the pose; this clock only gates the bees.
        public const float SwipeSeconds = 0.50f;
        public static float SwipeHitAt => BadgerSwipe.HitAt;
        public const float SplatSeconds = 0.35f;
        public const float WobbleSeconds = 0.60f;
        public const float WobbleDegrees = 7f;
        // One bee leaves every Stagger; each flies for FlightSeconds.
        public const float Stagger = 0.10f;
        public const float FlightSeconds = 0.35f;
        // Flip of the landed tile, inside the last share of the flight.
        public const float FlipSeconds = 0.18f;
        // After the last bee lands: the hive fades. The badger stays (the duel draws him).
        // PostSeconds then holds the line before the badger picks first (no swap / FIGHT gate).
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

        // Hive fade over the settle beat once the last bee has landed. The badger does
        // not use this: he stays on the duel mark through the rest of the fight.
        public static float CastAlpha(float t)
        {
            float from = LandAt(Landings - 1);
            if (t <= from) return 1f;
            return Mathf.Clamp01(1f - (t - from) / SettleSeconds);
        }
    }

    // THE hive swipe. Every badger attack on the hive or the honey reads this clock:
    // a short crouch, a fast lunge with a claw slash, then a return to the fight spot.
    // Pure, so the editor test can check the beats. The app draws the frames and the
    // streaks; it does not keep a second swipe.
    public static class BadgerSwipe
    {
        public enum Phase { None = 0, Windup = 1, Strike = 2, Recover = 3 }

        public const float WindupSeconds = 0.12f;
        public const float StrikeSeconds = 0.16f;
        // The claw sweeps the hive, then fades. Sweep stays inside 0.12-0.18 s.
        public const float SlashSeconds = 0.15f;
        public const float SlashFadeSeconds = 0.10f;
        public const float RecoverSeconds = 0.22f;
        public const float Duration = 0.50f;
        public const float HitFrac = 0.62f;
        public const float FlashSeconds = 0.09f;
        public const float LeanBack = 11f;
        public const float Snap = -24f;
        public const int Streaks = 3;
        public const int Segs = 6;

        public static float HitAt => WindupSeconds + SlashSeconds * HitFrac;

        public static Phase PhaseAt(float t)
        {
            if (t < 0f || t >= Duration) return Phase.None;
            if (t < WindupSeconds) return Phase.Windup;
            if (t < WindupSeconds + StrikeSeconds) return Phase.Strike;
            return Phase.Recover;
        }

        public static bool Active(float t) => PhaseAt(t) != Phase.None;

        public static string FrameAt(float t, float now)
        {
            var p = PhaseAt(t);
            if (p == Phase.Windup) return "badger_crouch";
            if (p == Phase.Strike)
            {
                float u = StrikeSeconds <= 0f ? 1f : (t - WindupSeconds) / StrikeSeconds;
                return u < 0.45f ? "badger_leap_1" : "badger_leap_3";
            }
            if (p == Phase.Recover)
            {
                float u = RecoverSeconds <= 0f ? 1f : (t - WindupSeconds - StrikeSeconds) / RecoverSeconds;
                if (u < 0.28f) return "badger_crouch";
                return BadgerSitter.FrameAt(now);
            }
            return BadgerSitter.FrameAt(now);
        }

        // Positive tips the head away from the hive. The strike snaps the other way.
        public static float Lean(float t)
        {
            var p = PhaseAt(t);
            if (p == Phase.Windup)
            {
                float u = WindupSeconds <= 0f ? 1f : t / WindupSeconds;
                return LeanBack * Smooth(u);
            }
            if (p == Phase.Strike)
            {
                float u = StrikeSeconds <= 0f ? 1f : (t - WindupSeconds) / StrikeSeconds;
                float e = Mathf.Clamp01(u / 0.32f);
                return Mathf.Lerp(LeanBack, Snap, e * e);
            }
            if (p == Phase.Recover)
            {
                float u = RecoverSeconds <= 0f ? 1f : (t - WindupSeconds - StrikeSeconds) / RecoverSeconds;
                return Mathf.Lerp(Snap, 0f, Smooth(u));
            }
            return 0f;
        }

        // 0 at the fight spot, 1 lunged at the hive, a small negative on the wind-up.
        public static float Lunge(float t)
        {
            var p = PhaseAt(t);
            if (p == Phase.Windup)
            {
                float u = WindupSeconds <= 0f ? 1f : t / WindupSeconds;
                return -0.10f * Smooth(u);
            }
            if (p == Phase.Strike)
            {
                float u = StrikeSeconds <= 0f ? 1f : (t - WindupSeconds) / StrikeSeconds;
                float e = 1f - (1f - Mathf.Clamp01(u)) * (1f - Mathf.Clamp01(u));
                return Mathf.Lerp(-0.10f, 1f, e);
            }
            if (p == Phase.Recover)
            {
                float u = RecoverSeconds <= 0f ? 1f : (t - WindupSeconds - StrikeSeconds) / RecoverSeconds;
                return Mathf.Lerp(1f, 0f, Smooth(u));
            }
            return 0f;
        }

        // Positive lifts the feet off the arena floor. Negative is the crouch.
        public static float Lift(float t)
        {
            var p = PhaseAt(t);
            if (p == Phase.Windup)
            {
                float u = WindupSeconds <= 0f ? 1f : t / WindupSeconds;
                return -0.40f * Smooth(u);
            }
            if (p == Phase.Strike)
            {
                float u = StrikeSeconds <= 0f ? 1f : (t - WindupSeconds) / StrikeSeconds;
                return Mathf.Lerp(-0.40f, 0.70f, Mathf.Clamp01(u) * Mathf.Clamp01(u));
            }
            if (p == Phase.Recover)
            {
                float u = RecoverSeconds <= 0f ? 1f : (t - WindupSeconds - StrikeSeconds) / RecoverSeconds;
                return Mathf.Lerp(0.70f, 0f, Smooth(u));
            }
            return 0f;
        }

        // 0 before the slash, 1 once it has crossed the hive.
        public static float Sweep(float t)
        {
            if (SlashSeconds <= 0f) return t >= WindupSeconds ? 1f : 0f;
            return Mathf.Clamp01((t - WindupSeconds) / SlashSeconds);
        }

        public static float SlashAlpha(float t)
        {
            float end = WindupSeconds + SlashSeconds;
            float fadeEnd = end + SlashFadeSeconds;
            if (t < WindupSeconds || t >= fadeEnd) return 0f;
            if (t <= end) return 1f;
            float u = SlashFadeSeconds <= 0f ? 1f : (t - end) / SlashFadeSeconds;
            return 1f - u * u;
        }

        public static float FlashAlpha(float t)
        {
            if (FlashSeconds <= 0f) return 0f;
            float u = (t - HitAt) / FlashSeconds;
            if (u < 0f || u > 1f) return 0f;
            return 1f - u * u;
        }

        // One piece of one claw. `sweep` is 0..1 across the hive. Width tapers toward
        // the tip. False when that piece is still ahead of or behind the sweep.
        public static bool Segment(int streak, int seg, float sweep, Rect hive,
            out Vector2 a, out Vector2 b, out float width, out float cover)
        {
            a = b = default;
            width = 0f;
            cover = 0f;
            if ((uint)streak >= (uint)Streaks || (uint)seg >= (uint)Segs) return false;
            if (hive.width < 2f || hive.height < 2f) return false;
            const float tail = 0.62f;
            float head = Mathf.Clamp01(sweep);
            float back = head - tail;
            float u0 = seg / (float)Segs;
            float u1 = (seg + 1) / (float)Segs;
            float vis0 = u0 > back ? u0 : back;
            float vis1 = u1 < head ? u1 : head;
            if (vis1 - vis0 < 0.012f) return false;
            a = Point(streak, vis0, hive);
            b = Point(streak, vis1, hive);
            float mid = (vis0 + vis1) * 0.5f;
            float span = head - back;
            float along = span <= 0.001f ? 0f : Mathf.Clamp01((mid - back) / span);
            float body = 0.035f + 0.07f * Mathf.Sin(along * Mathf.PI);
            float taper = 0.45f + 0.55f * (1f - along * 0.85f);
            width = hive.height * body * taper;
            cover = 0.55f + 0.45f * Mathf.Sin(Mathf.Clamp01(along) * Mathf.PI);
            return width > 0.35f;
        }

        static Vector2 Point(int streak, float along, Rect hive)
        {
            float lane = (streak - 1) * hive.height * 0.16f;
            float y0 = hive.y + hive.height * (0.28f + 0.07f * streak) + lane * 0.2f;
            float y1 = hive.y + hive.height * (0.64f - 0.05f * streak) + lane * 0.2f;
            float x0 = hive.xMax - hive.width * 0.05f;
            float x1 = hive.x + hive.width * 0.06f;
            float x = Mathf.Lerp(x0, x1, along);
            float y = Mathf.Lerp(y0, y1, along);
            y -= Mathf.Sin(along * Mathf.PI) * hive.height * 0.18f;
            return new Vector2(x, y);
        }

        static float Smooth(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
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
        // Word beat stays dark so the board cannot compete. The eye zoom eases lighter.
        public const float WashWord = 0.88f;
        public const float WashEye = 0.48f;
        public const float WashInSeconds = 0.08f;
        public const float FaceInSeconds = 0.10f;
        public const float ZoomMax = 3.2f;
        public const int SpeedLineCount = 22;

        // Close-up frame for the eye zoom (toward-camera leap frame; no new art).
        // Eye shares are of that frame, top-left origin, on the left pupil.
        // ShrugEye is the half-closed eye of badger_shrug, so the glint follows the pose.
        public const string FaceFrame = "badger_leap_3";
        public const float EyeU = 0.405f;
        public const float EyeV = 0.356f;
        public const float ShrugEyeU = 0.436f;
        public const float ShrugEyeV = 0.264f;

        public static void EyeUv(string frame, out float u, out float v)
        {
            if (frame == "badger_shrug")
            {
                u = ShrugEyeU;
                v = ShrugEyeV;
                return;
            }
            u = EyeU;
            v = EyeV;
        }

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

        // Full-screen wash. About 0.88 through the words, lighter once the eye zooms,
        // gone by the end. The words themselves draw after this.
        public static float WashAlpha(float t)
        {
            if (t <= 0f || t >= Duration) return 0f;
            float inn = Mathf.Clamp01(t / WashInSeconds);
            if (t < WordsEnd) return inn * WashWord;
            if (t < ZoomInEnd)
            {
                float u = Ease((t - WordsEnd) / (ZoomInEnd - WordsEnd));
                return Mathf.Lerp(WashWord, WashEye, u);
            }
            if (t < NastyEnd) return WashEye;
            return WashEye * (1f - Ease((t - NastyEnd) / ZoomOutSeconds));
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

    public enum BadgerDuelBeat { Idle = 0, Swipe = 1, Lunge = 2, Recoil = 3, Leap = 4, Shrug = 5 }

    // Fight poses for the one badger actor. Frames are the existing badger art.
    // Travel and the jolt use PopupMotion.Beat so the lunge paces the same at any fps.
    // The app draws one sprite from Frame; it does not stack a second badger on top.
    public static class BadgerDuel
    {
        public const float LungeSeconds = 0.42f;
        // Kept for the old actor-only cap. The slam camera is BadgerSlam.Zoom on the page.
        public const float SlamScaleMax = 1.28f;

        // 0 at rest, 1 at the closest point, 0 again.
        public static float Travel(float age)
        {
            float u = PopupMotion.Beat(age, 0f, LungeSeconds);
            if (u <= 0f || u >= 1f) return 0f;
            return Mathf.Sin(u * Mathf.PI);
        }

        // Defender shake, 0 at both ends. Same falloff shape as the contest deny shake.
        public static float Jolt(float age)
        {
            float u = PopupMotion.Beat(age, 0f, LungeSeconds);
            if (u <= 0f || u >= 1f) return 0f;
            return Mathf.Sin(u * 18f) * (1f - u);
        }

        // Maps BadgerSlam.Zoom (1..ZoomMax) onto 1..SlamScaleMax. 1 when the slam is not zooming.
        public static float SlamScale(float zoom)
        {
            float span = BadgerSlam.ZoomMax - 1f;
            float u = span <= 0.001f ? 0f : Mathf.Clamp01((zoom - 1f) / span);
            return Mathf.Lerp(1f, SlamScaleMax, PopupMotion.Smooth(u));
        }

        public static string Frame(BadgerDuelBeat beat, float age, float now)
        {
            switch (beat)
            {
                case BadgerDuelBeat.Swipe:
                    return BadgerSwipe.FrameAt(age, now);
                case BadgerDuelBeat.Lunge:
                {
                    float u = PopupMotion.Beat(age, 0f, LungeSeconds);
                    if (u <= 0.08f || u >= 0.92f) return BadgerSitter.FrameAt(now);
                    int k = Mathf.FloorToInt(age / 0.10f);
                    return (k & 1) == 0 ? "badger_hop_a" : "badger_hop_b";
                }
                case BadgerDuelBeat.Recoil:
                    return "badger_crouch";
                case BadgerDuelBeat.Leap:
                {
                    var frames = BadgerLeap.LeapFrames;
                    int n = frames.Length;
                    if (n < 1) return "badger_leap_3";
                    int i = Mathf.FloorToInt(Mathf.Clamp01(age) * n);
                    if (i >= n) i = n - 1;
                    if (i < 0) i = 0;
                    return frames[i];
                }
                case BadgerDuelBeat.Shrug:
                    return "badger_shrug";
                default:
                    return BadgerSitter.FrameAt(now);
            }
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

    // One badger actor. Idle reuses the sitter breathe. The swipe is BadgerSwipe.
    // Shrug is a pose of this actor, not a second sprite. Alpha stays 1 in every stage.
    public static class BadgerFighter
    {
        public static string FrameAt(BadgerDuelBeat beat, float age, float now)
        {
            switch (beat)
            {
                case BadgerDuelBeat.Swipe:
                    return BadgerSwipe.FrameAt(age, now);
                case BadgerDuelBeat.Shrug:
                    return "badger_shrug";
                case BadgerDuelBeat.Idle:
                    return BadgerSitter.FrameAt(now);
                default:
                    return BadgerDuel.Frame(beat, age, now);
            }
        }

        public static bool BothVisible(BadgerStage stage)
        {
            switch (stage)
            {
                case BadgerStage.PostOpen:
                case BadgerStage.BossWait:
                case BadgerStage.YourPick:
                case BadgerStage.Reveal:
                case BadgerStage.Verdict:
                case BadgerStage.Outro:
                case BadgerStage.Over:
                case BadgerStage.Opening:
                case BadgerStage.Lesson:
                case BadgerStage.DontCare:
                    return true;
                default:
                    return false;
            }
        }

        public static float Alpha(BadgerStage stage) => BothVisible(stage) ? 1f : 0f;
    }

    // Five boxes across the arena: you, your slot, the VS/TIE gap, his slot, him.
    // Shares are about 0.20 / 0.22 / 0.10 / 0.22 / 0.20, with a small gap between.
    public static class BadgerArenaRects
    {
        public static readonly float[] Shares = { 0.20f, 0.22f, 0.10f, 0.22f, 0.20f };
        public const float GapFrac = 0.012f;

        public struct Boxes
        {
            public Rect YouFighter, YouSlot, Middle, BossSlot, BossFighter;
        }

        public static Boxes Split(Rect arena)
        {
            var b = new Boxes();
            float sum = 0f;
            int n = Shares.Length;
            for (int i = 0; i < n; i++) sum += Shares[i];
            if (sum < 0.01f) sum = 1f;
            float gap = arena.width * GapFrac;
            float inner = arena.width - gap * 4f;
            if (inner < arena.width * 0.5f)
            {
                inner = arena.width;
                gap = 0f;
            }
            float x = arena.x;
            for (int i = 0; i < 5; i++)
            {
                float share = i < n ? Shares[i] : 0.2f;
                float w = inner * (share / sum);
                if (i == 4) w = Mathf.Max(1f, arena.xMax - x);
                var r = new Rect(x, arena.y, w, arena.height);
                if (i == 0) b.YouFighter = r;
                else if (i == 1) b.YouSlot = r;
                else if (i == 2) b.Middle = r;
                else if (i == 3) b.BossSlot = r;
                else b.BossFighter = r;
                x += w + gap;
            }
            return b;
        }
    }

    // "Don't Care" word band. The rotated, popped word stays inside the device safe
    // area. It does not have to clear the arena, the grids, or the fighters: the words
    // draw last and covering the board is allowed.
    public static class DontCareSlamRects
    {
        public const float TiltDegrees = -15f;
        static readonly Vector2[] Fit = new Vector2[4];

        // Unity safeArea is bottom-left. GUI space is top-left.
        public static Rect SafeGui(float screenW, float screenH, Rect safeBottomLeft)
        {
            if (screenW < 2f) screenW = 2f;
            if (screenH < 2f) screenH = 2f;
            if (safeBottomLeft.width < 2f || safeBottomLeft.height < 2f)
                return new Rect(0f, 0f, screenW, screenH);
            float x = Mathf.Clamp(safeBottomLeft.x, 0f, screenW - 2f);
            float yMax = Mathf.Clamp(safeBottomLeft.yMax, 2f, screenH);
            float yMin = Mathf.Clamp(safeBottomLeft.y, 0f, yMax);
            float top = screenH - yMax;
            float h = (screenH - yMin) - top;
            float w = Mathf.Min(safeBottomLeft.width, screenW - x);
            if (w < 2f || h < 2f) return new Rect(0f, 0f, screenW, screenH);
            return new Rect(x, top, w, h);
        }

        // Resting word rect. PopFrom scale and TiltDegrees still land inside `safe`.
        public static Rect WordBand(Rect safe)
        {
            float margin = Mathf.Max(8f, Mathf.Min(safe.width, safe.height) * 0.035f);
            var inner = new Rect(
                safe.x + margin,
                safe.y + margin,
                Mathf.Max(8f, safe.width - margin * 2f),
                Mathf.Max(8f, safe.height - margin * 2f));
            float pop = BadgerSlam.PopFrom < 1f ? 1f : BadgerSlam.PopFrom;
            float bw = inner.width * 0.78f / pop;
            float bh = inner.height * 0.18f / pop;
            var band = Center(inner, bw, bh);
            for (int n = 0; n < 12; n++)
            {
                RotatedCorners(band, pop, TiltDegrees, Fit);
                if (Inside(inner, Fit, 1.5f)) return band;
                bw *= 0.90f;
                bh *= 0.90f;
                band = Center(inner, bw, bh);
            }
            return band;
        }

        public static void RotatedCorners(Rect band, float pop, float degrees, Vector2[] into)
        {
            if (into == null || into.Length < 4) return;
            float rad = degrees * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            float hw = band.width * pop * 0.5f;
            float hh = band.height * pop * 0.5f;
            var c = band.center;
            Corner(into, 0, c, -hw, -hh, cs, sn);
            Corner(into, 1, c, hw, -hh, cs, sn);
            Corner(into, 2, c, hw, hh, cs, sn);
            Corner(into, 3, c, -hw, hh, cs, sn);
        }

        static void Corner(Vector2[] into, int i, Vector2 c, float x, float y, float cs, float sn)
        {
            into[i] = new Vector2(c.x + x * cs - y * sn, c.y + x * sn + y * cs);
        }

        static Rect Center(Rect inner, float w, float h)
        {
            if (w > inner.width) w = inner.width;
            if (h > inner.height) h = inner.height;
            if (w < 4f) w = 4f;
            if (h < 4f) h = 4f;
            return new Rect(inner.center.x - w * 0.5f, inner.center.y - h * 0.5f, w, h);
        }

        static bool Inside(Rect r, Vector2[] corners, float pad)
        {
            for (int i = 0; i < 4; i++)
            {
                var p = corners[i];
                if (p.x < r.x + pad || p.x > r.xMax - pad) return false;
                if (p.y < r.y + pad || p.y > r.yMax - pad) return false;
            }
            return true;
        }
    }

    // Resting marks in the arena band. The hummingbird is the left box, facing right
    // (art faces right, so FaceLeft stays false). The badger stands in the right box.
    // DrawBadgerFighters is the only draw; this is the geometry it and the test share.
    public static class BadgerFighterPlace
    {
        public struct Mark
        {
            public Vector2 Bird;
            public float Icon;
            public bool FaceLeft;
            public Vector2 Foot;
            public float Unit;
            public Rect BirdBox;
            public Rect BadgerBox;
        }

        public static Mark Rest(BadgerArenaRects.Boxes box, float artW, float artH)
        {
            var m = new Mark();
            var you = box.YouFighter;
            var boss = box.BossFighter;
            float icon = Mathf.Min(you.width * 0.92f, you.height * 0.90f);
            if (icon < 1f) icon = 1f;
            m.Icon = icon;
            m.FaceLeft = false;
            m.Bird = you.center;
            m.BirdBox = new Rect(m.Bird.x - icon * 0.5f, m.Bird.y - icon * 0.5f, icon, icon);

            if (artW < 1f) artW = 480f;
            if (artH < 1f) artH = 640f;
            float fit = 0.96f;
            float unitH = boss.height * fit / artH;
            float unitW = boss.width * fit / artW;
            m.Unit = unitH < unitW ? unitH : unitW;
            if (m.Unit < 0.001f) m.Unit = 0.001f;
            float w = artW * m.Unit;
            float h = artH * m.Unit;
            m.Foot = new Vector2(boss.center.x, boss.yMax);
            m.BadgerBox = new Rect(m.Foot.x - w * 0.5f, m.Foot.y - h, w, h);
            return m;
        }

        // Both bodies sit in the arena band, bird on the left facing the badger.
        public static bool Readable(Mark m, Rect arena)
        {
            if (m.FaceLeft) return false;
            if (m.Bird.x >= m.Foot.x - 4f) return false;
            if (m.BirdBox.width < arena.height * 0.35f) return false;
            if (m.BadgerBox.height < arena.height * 0.55f) return false;
            return Contains(arena, m.BirdBox) && Contains(arena, m.BadgerBox);
        }

        static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin - 0.5f
                && inner.xMax <= outer.xMax + 0.5f
                && inner.yMin >= outer.yMin - 0.5f
                && inner.yMax <= outer.yMax + 0.5f;
        }
    }

    // One honeycomb per point still needed. Both meters share Unit, so the shorter
    // target is a shorter stack. Index 0 is the bottom comb. The number band is the
    // top of the column; the stack never climbs into it.
    public static class BadgerMeter
    {
        public static float NumberBand(Rect col)
        {
            float h = col.width * 0.95f;
            float cap = col.height * 0.18f;
            if (h > cap) h = cap;
            if (h < 4f) h = 4f;
            return h;
        }

        public static float Unit(Rect col, int maxTarget)
        {
            int n = maxTarget < 1 ? 1 : maxTarget;
            float stack = col.height - NumberBand(col);
            if (stack < 4f) stack = 4f;
            return stack / n;
        }

        public static Rect Number(Rect col)
        {
            return new Rect(col.x, col.y, col.width, NumberBand(col));
        }

        public static Rect Comb(Rect col, int index, float unit)
        {
            float gap = Mathf.Max(1f, unit * 0.12f);
            float h = Mathf.Max(2f, unit - gap);
            float y = col.yMax - (index + 1) * unit + gap * 0.5f;
            float inset = col.width * 0.06f;
            return new Rect(col.x + inset, y, Mathf.Max(2f, col.width - inset * 2f), h);
        }
    }

    // The round's hit. A badger score is the shared hive swipe turned onto the bird.
    // A player score is a dart. A tie does neither. Pulse is the winner's meter bump.
    public static class BadgerRoundAct
    {
        public enum Kind { None = 0, Swipe = 1, Peck = 2 }

        public const float PeckSeconds = 0.56f;
        public const float PeckHit = 0.22f;
        public const float RecoilSeconds = 0.36f;
        public const float SplatSeconds = 0.35f;
        public const float PulseSeconds = 0.42f;

        public static Kind Of(int playerGained, int bossGained)
        {
            if (playerGained > 0 && bossGained <= 0) return Kind.Peck;
            if (bossGained > 0 && playerGained <= 0) return Kind.Swipe;
            return Kind.None;
        }

        // 0 before the claw lands, 1 at the shove, 0 again.
        public static float Recoil(float age)
        {
            float u = PopupMotion.Beat(age, BadgerSwipe.HitAt, RecoilSeconds);
            if (u <= 0f || u >= 1f) return 0f;
            return Mathf.Sin(u * Mathf.PI);
        }

        public static float SplatAlpha(float age)
        {
            float u = (age - BadgerSwipe.HitAt) / SplatSeconds;
            if (u < 0f || u > 1f) return 0f;
            return 1f - u * u;
        }

        // 0 on the bird's mark, 1 at the badger, 0 again.
        public static float Peck(float age)
        {
            float u = PopupMotion.Beat(age, 0f, PeckSeconds);
            if (u <= 0f || u >= 1f) return 0f;
            return Mathf.Sin(u * Mathf.PI);
        }

        public static bool PeckHits(float before, float after)
        {
            return before < PeckHit && after >= PeckHit;
        }

        // 1 at rest. Bigger while the new point lands.
        public static float Pulse(float age)
        {
            float u = PopupMotion.Beat(age, 0f, PulseSeconds);
            if (u <= 0f || u >= 1f) return 1f;
            return 1f + 0.14f * Mathf.Sin(u * Mathf.PI);
        }
    }

    // End beat before the result line. About 1.25 s. Badger win: he taunts, the bird
    // droops. Player win: shrug, step back, the bird lifts. PopUp is the line after.
    public static class BadgerOutro
    {
        public const float Duration = 1.25f;
        public const float DroopDegrees = 16f;

        public static string Frame(bool playerWon, float age, float now)
        {
            if (playerWon) return "badger_shrug";
            float u = PopupMotion.Beat(age, 0f, 0.70f);
            if (u < 0.45f) return "badger_leap_1";
            if (u < 1f) return "badger_leap_3";
            return BadgerSitter.FrameAt(now);
        }

        // 0 on the mark, 1 stepped away from the bird (to the right).
        public static float BackOff(bool playerWon, float age)
        {
            if (!playerWon) return 0f;
            if (age >= Duration) return 1f;
            return PopupMotion.Smooth(PopupMotion.Beat(age, 0.12f, 0.85f));
        }

        // Clockwise degrees. GUI +Y is down, so this drops a right-facing bird's head.
        public static float Droop(bool playerWon, float age)
        {
            if (playerWon) return 0f;
            if (age >= Duration) return DroopDegrees;
            return DroopDegrees * PopupMotion.Smooth(PopupMotion.Beat(age, 0.05f, 0.45f));
        }

        // Upward shares of the bird icon. Holds the lift once the beat ends.
        public static float Proud(bool playerWon, float age)
        {
            if (!playerWon) return 0f;
            if (age >= Duration) return 0.10f;
            float rise = 0.10f * PopupMotion.Smooth(PopupMotion.Beat(age, 0f, 0.40f));
            return rise + 0.025f * Mathf.Sin(age * 5.2f);
        }

        public static bool PopUp(float age) => age >= Duration;
    }
}

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

        // Dominant sprite. BadgerAnim adds the crossfade, squash, and the leap_2 blend.
        public static string FrameAt(float t) => BadgerAnim.Entrance(t).Frame;

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
        // The swipe is the shared special-attack clock. Bees leave once it ends,
        // so the hive hit stays on BadgerSwipe.HitAt.
        public const float SwipeSeconds = BadgerSwipe.Duration;
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

    // THE hive swipe, and the special attack. Every badger attack on the hive or the
    // honey reads this clock. About 2x the old 0.50 s snap, with room to read each phase:
    // anticipation (crouch + wind-up, ~0.4 s), the leap/swipe, an impact hold (~0.15 s),
    // then recovery back to idle. BadgerAnim owns the in-betweens. Pure, so the editor
    // test can check the beats. The app draws one posed sprite; it does not keep a second swipe.
    public static class BadgerSwipe
    {
        public enum Phase { None = 0, Windup = 1, Strike = 2, Recover = 3, Hold = 4 }

        public const float WindupSeconds = 0.40f;
        public const float StrikeSeconds = 0.24f;
        public const float HoldSeconds = 0.15f;
        public const float RecoverSeconds = 0.24f;
        // The claw sweeps during the strike, then fades.
        public const float SlashSeconds = 0.20f;
        public const float SlashFadeSeconds = 0.12f;
        public const float Duration = WindupSeconds + StrikeSeconds + HoldSeconds + RecoverSeconds;
        public const float HitFrac = 0.62f;
        public const float FlashSeconds = 0.09f;
        public const float LeanBack = 11f;
        public const float Snap = -24f;
        public const int Streaks = 3;
        public const int Segs = 6;

        public static float HitAt => WindupSeconds + SlashSeconds * HitFrac;
        public static float HoldStart => WindupSeconds + StrikeSeconds;
        public static float RecoverStart => HoldStart + HoldSeconds;

        public static Phase PhaseAt(float t)
        {
            if (t < 0f || t >= Duration) return Phase.None;
            if (t < WindupSeconds) return Phase.Windup;
            if (t < HoldStart) return Phase.Strike;
            if (t < RecoverStart) return Phase.Hold;
            return Phase.Recover;
        }

        public static bool Active(float t) => PhaseAt(t) != Phase.None;

        // Dominant sprite. BadgerAnim owns the blend, squash, and the in-between leap_2.
        public static string FrameAt(float t, float now) => BadgerAnim.Swipe(t, now).Frame;

        public static float Lean(float t) => BadgerAnim.Swipe(t, 0f).Degrees;

        public static float Lunge(float t) => BadgerAnim.Swipe(t, 0f).Shift;

        public static float Lift(float t) => BadgerAnim.Swipe(t, 0f).Lift;

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

    }

    // One posed badger for every fight action: entrance, idle, hops, claw swipe,
    // shrug, the special attack, and the win/lose beat. Nine drawings. The missing
    // leap_2 is leap_1 blended into leap_3. Scale and rotation ease between keys,
    // with squash on takeoff and landing and a short crossfade (~3 frames at 60fps).
    // Pure. No per-frame allocs. Drawers paint Under then Frame.
    public struct BadgerSample
    {
        public string Frame;
        public string Under;
        public float FrameAlpha;
        public float UnderAlpha;
        public float ScaleX;
        public float ScaleY;
        public float Degrees;
        public float Lift;
        public float Shift;
    }

    public static class BadgerAnim
    {
        public const float BlendSeconds = 0.050f;
        // Share of the leap beat where the missing leap_2 lives (leap_1 into leap_3).
        public const float Leap2From = 0.62f;
        public const float Leap2To = 0.82f;

        public static BadgerSample Idle(float now)
        {
            float step = BadgerSitter.BreatheSeconds;
            if (step < 0.05f) step = 0.6f;
            float t = now < 0f ? 0f : now;
            int k = Mathf.FloorToInt(t / step);
            bool a = (k & 1) == 0;
            string cur = a ? "badger_idle_a" : "badger_idle_b";
            string prev = a ? "badger_idle_b" : "badger_idle_a";
            var s = Fresh(cur);
            Cross(ref s, t - k * step, prev, cur);
            Breathe(t, ref s, 0.030f);
            return s;
        }

        public static BadgerSample Entrance(float t)
        {
            var beat = BadgerLeap.BeatAt(t);
            if (beat == BadgerLeapBeat.Hop) return Hop(t);
            if (beat == BadgerLeapBeat.Crouch)
            {
                float u = BadgerLeap.Local(t);
                float age = t - BadgerLeap.BeatStart(BadgerLeapBeat.Crouch);
                var s = Fresh("badger_crouch");
                Cross(ref s, age, "badger_hop_b", "badger_crouch");
                Squash(ref s, Mathf.Sin(u * Mathf.PI * 0.5f), 0f);
                return s;
            }
            return LeapShot(t);
        }

        public static BadgerSample Swipe(float t, float now)
        {
            var p = BadgerSwipe.PhaseAt(t);
            if (p == BadgerSwipe.Phase.None) return Idle(now);
            if (p == BadgerSwipe.Phase.Windup)
            {
                float u = BadgerSwipe.WindupSeconds <= 0f ? 1f : t / BadgerSwipe.WindupSeconds;
                float e = Smooth(u);
                var s = Fresh("badger_crouch");
                Cross(ref s, t, "badger_idle_a", "badger_crouch");
                s.Degrees = BadgerSwipe.LeanBack * e;
                s.Shift = -0.10f * e;
                s.Lift = -0.40f * e;
                Squash(ref s, e, 0f);
                return s;
            }
            if (p == BadgerSwipe.Phase.Strike)
            {
                float u = BadgerSwipe.StrikeSeconds <= 0f ? 1f : (t - BadgerSwipe.WindupSeconds) / BadgerSwipe.StrikeSeconds;
                u = Mathf.Clamp01(u);
                var s = Fresh("badger_leap_1");
                float snap = Mathf.Clamp01(u / 0.32f);
                s.Degrees = Mathf.Lerp(BadgerSwipe.LeanBack, BadgerSwipe.Snap, snap * snap);
                float rush = 1f - (1f - u) * (1f - u);
                s.Shift = Mathf.Lerp(-0.10f, 1f, rush);
                s.Lift = Mathf.Lerp(-0.40f, 0.70f, u * u);
                if (u < 0.40f)
                {
                    Cross(ref s, u * BadgerSwipe.StrikeSeconds, "badger_crouch", "badger_leap_1");
                    s.Frame = "badger_leap_1";
                    Squash(ref s, 0f, Mathf.Clamp01(u / 0.40f));
                }
                else if (u < 0.72f)
                {
                    Pair(ref s, (u - 0.40f) / 0.32f, "badger_leap_1", "badger_leap_3");
                    Squash(ref s, 0f, Mathf.Lerp(1f, 0.35f, (u - 0.40f) / 0.32f));
                }
                else
                {
                    Cross(ref s, (u - 0.72f) * BadgerSwipe.StrikeSeconds, "badger_leap_1", "badger_leap_3");
                    s.Frame = "badger_leap_3";
                    Squash(ref s, 0.25f, 0.40f);
                }
                return s;
            }
            if (p == BadgerSwipe.Phase.Hold)
            {
                float u = BadgerSwipe.HoldSeconds <= 0f ? 1f : (t - BadgerSwipe.HoldStart) / BadgerSwipe.HoldSeconds;
                var s = Fresh("badger_leap_3");
                s.Degrees = BadgerSwipe.Snap;
                s.Shift = 1f;
                s.Lift = 0.70f;
                Squash(ref s, (1f - Smooth(u)) * 0.95f, 0f);
                return s;
            }
            float ru = BadgerSwipe.RecoverSeconds <= 0f ? 1f : (t - BadgerSwipe.RecoverStart) / BadgerSwipe.RecoverSeconds;
            ru = Mathf.Clamp01(ru);
            var back = Fresh("badger_crouch");
            back.Degrees = Mathf.Lerp(BadgerSwipe.Snap, 0f, Smooth(ru));
            back.Shift = Mathf.Lerp(1f, 0f, Smooth(ru));
            back.Lift = Mathf.Lerp(0.70f, 0f, Smooth(ru));
            if (ru < 0.28f)
            {
                Cross(ref back, ru * BadgerSwipe.RecoverSeconds, "badger_leap_3", "badger_crouch");
                back.Frame = "badger_crouch";
                Squash(ref back, 1f - ru / 0.28f, 0f);
                return back;
            }
            var idle = Idle(now);
            Cross(ref back, (ru - 0.28f) * BadgerSwipe.RecoverSeconds, "badger_crouch", idle.Frame);
            back.Frame = idle.Frame;
            back.ScaleX = idle.ScaleX;
            back.ScaleY = idle.ScaleY;
            return back;
        }

        public static BadgerSample Shrug(float age, float now)
        {
            var s = Fresh("badger_shrug");
            float pop = PopupMotion.SlamScale(Mathf.Clamp01(age / 0.22f), 1.12f);
            s.ScaleX = pop;
            s.ScaleY = pop;
            Breathe(now, ref s, 0.018f);
            return s;
        }

        public static BadgerSample Outro(bool playerWon, float age, float now)
        {
            if (playerWon) return Shrug(age, now);
            float u = PopupMotion.Beat(age, 0f, 0.70f);
            if (u >= 1f) return Idle(now);
            if (u < 0.45f)
            {
                var s = Fresh("badger_leap_1");
                Cross(ref s, age, "badger_idle_a", "badger_leap_1");
                s.Frame = "badger_leap_1";
                Squash(ref s, 0f, Mathf.Clamp01(u / 0.45f));
                return s;
            }
            float blend = (u - 0.45f) / 0.55f;
            var taunt = Fresh("badger_leap_3");
            if (blend < 0.72f) Pair(ref taunt, blend / 0.72f, "badger_leap_1", "badger_leap_3");
            else
            {
                taunt.Frame = "badger_leap_3";
                Squash(ref taunt, blend * 0.35f, (1f - blend) * 0.35f);
            }
            return taunt;
        }

        public static BadgerSample Duel(BadgerDuelBeat beat, float age, float now)
        {
            switch (beat)
            {
                case BadgerDuelBeat.Swipe: return Swipe(age, now);
                case BadgerDuelBeat.Shrug: return Shrug(age, now);
                case BadgerDuelBeat.Lunge: return LungeHop(age, now);
                case BadgerDuelBeat.Recoil:
                    var c = Fresh("badger_crouch");
                    Squash(ref c, 0.70f, 0f);
                    return c;
                case BadgerDuelBeat.Leap:
                    return LeapShot(BadgerLeap.BeatStart(BadgerLeapBeat.Leap) + Mathf.Clamp01(age) * BadgerLeap.LeapSeconds);
                default:
                    return Idle(now);
            }
        }

        static BadgerSample Hop(float t)
        {
            float stride = BadgerLeap.HopStride;
            if (stride < 0.05f) stride = 0.4f;
            float phase = Mathf.Repeat(t < 0f ? 0f : t, stride) / stride;
            bool air = phase < BadgerLeap.HopAirShare;
            string cur = air ? "badger_hop_a" : "badger_hop_b";
            string prev = air ? "badger_hop_b" : "badger_hop_a";
            float age = air ? phase * stride : (phase - BadgerLeap.HopAirShare) * stride;
            var s = Fresh(cur);
            Cross(ref s, age, prev, cur);
            if (air) Squash(ref s, 0f, Mathf.Sin(phase / Mathf.Max(0.05f, BadgerLeap.HopAirShare) * Mathf.PI));
            else
            {
                float land = (phase - BadgerLeap.HopAirShare) / Mathf.Max(0.05f, 1f - BadgerLeap.HopAirShare);
                Squash(ref s, (1f - Smooth(land)) * 0.85f, 0f);
            }
            return s;
        }

        // Leap keys, then the synthetic leap_2 (leap_1 dissolved into leap_3).
        static BadgerSample LeapShot(float t)
        {
            var beat = BadgerLeap.BeatAt(t);
            float u = beat == BadgerLeapBeat.Leap ? BadgerLeap.Local(t) : 1f;
            if (u < BadgerLeap.LeapFrameEnds[0])
            {
                var s = Fresh("badger_leap_0");
                Cross(ref s, u * BadgerLeap.LeapSeconds, "badger_crouch", "badger_leap_0");
                s.Frame = "badger_leap_0";
                Squash(ref s, 0f, Mathf.Clamp01(u / 0.28f));
                return s;
            }
            if (u < Leap2From)
            {
                var s = Fresh("badger_leap_1");
                Cross(ref s, (u - BadgerLeap.LeapFrameEnds[0]) * BadgerLeap.LeapSeconds, "badger_leap_0", "badger_leap_1");
                s.Frame = "badger_leap_1";
                Squash(ref s, 0f, 0.65f);
                return s;
            }
            if (u < Leap2To)
            {
                float span = Leap2To - Leap2From;
                float blend = span <= 0.001f ? 1f : (u - Leap2From) / span;
                var s = Fresh("badger_leap_3");
                Pair(ref s, blend, "badger_leap_1", "badger_leap_3");
                Squash(ref s, 0f, Mathf.Lerp(0.75f, 0.20f, blend));
                return s;
            }
            var end = Fresh("badger_leap_3");
            Cross(ref end, (u - Leap2To) * BadgerLeap.LeapSeconds, "badger_leap_1", "badger_leap_3");
            end.Frame = "badger_leap_3";
            float land = Mathf.Clamp01((u - Leap2To) / Mathf.Max(0.05f, 1f - Leap2To));
            Squash(ref end, land * 0.40f, (1f - land) * 0.45f);
            return end;
        }

        static BadgerSample LungeHop(float age, float now)
        {
            float u = PopupMotion.Beat(age, 0f, BadgerDuel.LungeSeconds);
            if (u <= 0.08f || u >= 0.92f) return Idle(now);
            const float stride = 0.28f;
            float phase = Mathf.Repeat(age < 0f ? 0f : age, stride) / stride;
            bool air = phase < 0.55f;
            string cur = air ? "badger_hop_a" : "badger_hop_b";
            string prev = air ? "badger_hop_b" : "badger_hop_a";
            var s = Fresh(cur);
            Cross(ref s, (air ? phase : phase - 0.55f) * stride, prev, cur);
            if (air) Squash(ref s, 0f, Mathf.Sin(phase / 0.55f * Mathf.PI));
            else Squash(ref s, 0.70f * (1f - (phase - 0.55f) / 0.45f), 0f);
            return s;
        }

        static BadgerSample Fresh(string frame)
        {
            var s = new BadgerSample();
            s.Frame = frame;
            s.FrameAlpha = 1f;
            s.ScaleX = 1f;
            s.ScaleY = 1f;
            return s;
        }

        static void Breathe(float now, ref BadgerSample s, float amp)
        {
            float w = Mathf.Sin(now * 2.6f);
            s.ScaleY *= 1f + amp * w;
            s.ScaleX *= 1f - amp * 0.65f * w;
        }

        static void Squash(ref BadgerSample s, float crouch, float air)
        {
            float k = air > 0.001f ? air : -Mathf.Clamp01(crouch);
            s.ScaleY = 1f + 0.16f * k;
            s.ScaleX = 1f - 0.12f * k;
            if (s.ScaleY < 0.78f) s.ScaleY = 0.78f;
            if (s.ScaleX < 0.78f) s.ScaleX = 0.78f;
        }

        static void Cross(ref BadgerSample s, float age, string prev, string cur)
        {
            s.Frame = cur;
            s.FrameAlpha = 1f;
            s.Under = null;
            s.UnderAlpha = 0f;
            if (string.IsNullOrEmpty(prev) || prev == cur || age < 0f || age >= BlendSeconds) return;
            float u = age / BlendSeconds;
            s.Under = prev;
            s.UnderAlpha = 1f - u;
            s.FrameAlpha = u;
        }

        // u 0 is all `a`, u 1 is all `b`. The louder sprite is Frame so a single draw still reads.
        static void Pair(ref BadgerSample s, float u, string a, string b)
        {
            u = Mathf.Clamp01(u);
            float keep = 1f - u;
            if (keep >= u)
            {
                s.Frame = a;
                s.FrameAlpha = keep;
                s.Under = u > 0.04f ? b : null;
                s.UnderAlpha = u > 0.04f ? u : 0f;
            }
            else
            {
                s.Frame = b;
                s.FrameAlpha = u;
                s.Under = keep > 0.04f ? a : null;
                s.UnderAlpha = keep > 0.04f ? keep : 0f;
            }
        }

        static float Smooth(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }
    }

    // "+N" for a scored round. Player N is the margin. Boss N is the full loss honey.
    // The strings are built once. Rise and alpha are pure so the meter total can match.
    public static class BadgerGainFloat
    {
        public const float Seconds = 0.85f;
        const int Cap = 48;
        static string[] _plus;

        public static string Text(int gain)
        {
            if (gain <= 0) return "";
            if (gain > Cap) gain = Cap;
            if (_plus == null)
            {
                _plus = new string[Cap + 1];
                for (int i = 1; i <= Cap; i++) _plus[i] = "+" + i.ToString();
            }
            return _plus[gain];
        }

        public static float Rise(float age)
        {
            return PopupMotion.Smooth(PopupMotion.Beat(age, 0.04f, Seconds));
        }

        public static float Alpha(float age)
        {
            float u = PopupMotion.Beat(age, 0f, Seconds);
            if (u <= 0f || u >= 1f) return 0f;
            if (u > 0.70f) return 1f - PopupMotion.Smooth((u - 0.70f) / 0.30f);
            return 1f;
        }
    }

    public enum BadgerSlamBeat { Words = 0, Speed = 1, ZoomIn = 2, Nasty = 3, ZoomOut = 4, Done = 5 }

    // "Honey Badger Don't Care" critical-hit beat (spec 2b), about 2.9 s:
    // the four words slam in one at a time and stay up together, then speed lines, a hard zoom
    // into the badger's eye, the nasty move (honey_splat + shrug), zoom back out, then the round
    // resolves. Pure timing and shapes; BadgerFight.TryDontCare decides the hit and the effect.
    // Zoom(t) is exactly 1 outside the zoom beats and once Done, and the app derives the GUI
    // scale from it every frame (nothing is stored), so the contest always returns to 1:1.
    public static class BadgerSlam
    {
        public const float WordSeconds = 0.40f;
        public const float SpeedSeconds = 0.30f;
        public const float ZoomInSeconds = 0.45f;
        public const float NastySeconds = 0.22f;
        public const float ZoomOutSeconds = 0.35f;

        // Each word slams from PopFrom down to 1 over this share of its slot (shared stamp slam).
        public const float PopShare = 0.45f;
        public const float PopFrom = 1.72f;
        // Small screen shake on each word. Decays before the next word.
        public const float ShakeSeconds = 0.14f;
        public const float ShakePx = 7f;
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
        public const float EyeU = 0.440f;
        public const float EyeV = 0.270f;
        public const float ShrugEyeU = 0.510f;
        public const float ShrugEyeV = 0.185f;

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

        // -1 before the first word and after the word beat. The word that is slamming now.
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

        // How many words are on screen. All four stay until the beat ends.
        public static int ShownCount(float t)
        {
            if (t < 0f || t >= Duration) return 0;
            if (t >= WordsEnd) return Words;
            int n = Mathf.FloorToInt(t / WordSeconds) + 1;
            if (n > Words) n = Words;
            return n < 0 ? 0 : n;
        }

        public static bool WordOn(float t, int i)
        {
            if (i < 0 || i >= Words) return false;
            return t >= i * WordSeconds && t < Duration;
        }

        // Scale of word i: PopFrom on its first instant, 1 once the slam lands.
        public static float WordPop(float t, int i)
        {
            if (!WordOn(t, i)) return 1f;
            float u = (t - i * WordSeconds) / (WordSeconds * PopShare);
            return PopupMotion.SlamScale(u, PopFrom);
        }

        // Scale of the word that is slamming now.
        public static float Pop(float t)
        {
            int i = WordIndexAt(t);
            if (i < 0) return 1f;
            return WordPop(t, i);
        }

        // GUI-space shake for the newest word. Zero once that punch has decayed.
        public static void Shake(float t, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            int i = WordIndexAt(t);
            if (i < 0 || ShakeSeconds <= 0f) return;
            float age = t - i * WordSeconds;
            if (age < 0f || age >= ShakeSeconds) return;
            float d = 1f - age / ShakeSeconds;
            d *= d;
            float wob = age * 78f;
            x = Mathf.Sin(wob) * ShakePx * d;
            y = Mathf.Cos(wob * 1.31f) * ShakePx * 0.55f * d;
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
        public const float LungeSeconds = 0.72f;
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
            return BadgerAnim.Duel(beat, age, now).Frame;
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

        public static string FrameAt(float now) => BadgerAnim.Idle(now).Frame;
    }

    // One badger actor. Idle reuses the sitter breathe. The swipe is BadgerSwipe.
    // Shrug is a pose of this actor, not a second sprite. Alpha stays 1 in every stage.
    public static class BadgerFighter
    {
        public static string FrameAt(BadgerDuelBeat beat, float age, float now)
        {
            return BadgerDuel.Frame(beat, age, now);
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

        // Resting rect of word 0. PopFrom scale and TiltDegrees still land inside `safe`.
        public static Rect WordBand(Rect safe) => WordSlot(safe, 0);

        // Four stacked slots. Popped, tilted boxes stay inside `safe` and clear each other.
        public static Rect WordSlot(Rect safe, int index)
        {
            int n = BadgerSlam.Words;
            if (n < 1) n = 1;
            if (index < 0) index = 0;
            if (index >= n) index = n - 1;
            float margin = Mathf.Max(8f, Mathf.Min(safe.width, safe.height) * 0.035f);
            var inner = new Rect(
                safe.x + margin,
                safe.y + margin,
                Mathf.Max(8f, safe.width - margin * 2f),
                Mathf.Max(8f, safe.height - margin * 2f));
            float pop = BadgerSlam.PopFrom < 1f ? 1f : BadgerSlam.PopFrom;
            float pad = Mathf.Max(6f, inner.height * 0.012f);
            float bw = inner.width * 0.86f / pop;
            float bh = inner.height * 0.20f / pop;
            float rad = TiltDegrees * Mathf.Deg2Rad;
            float sn = Mathf.Abs(Mathf.Sin(rad));
            float cs = Mathf.Abs(Mathf.Cos(rad));
            for (int k = 0; k < 16; k++)
            {
                float hw = bw * pop * 0.5f;
                float hh = bh * pop * 0.5f;
                float vExt = hw * sn + hh * cs;
                float hExt = hw * cs + hh * sn;
                float stack = (vExt * 2f + pad) * n - pad;
                if (stack <= inner.height + 0.5f && hExt * 2f <= inner.width + 0.5f) break;
                bw *= 0.90f;
                bh *= 0.90f;
            }
            if (bw < 4f) bw = 4f;
            if (bh < 4f) bh = 4f;
            float hw2 = bw * pop * 0.5f;
            float hh2 = bh * pop * 0.5f;
            float vExt2 = hw2 * sn + hh2 * cs;
            float slot = vExt2 * 2f + pad;
            float stackH = slot * n - pad;
            if (stackH > inner.height) stackH = inner.height;
            float top = inner.center.y - stackH * 0.5f;
            float cy = top + vExt2 + index * slot;
            return Center(inner, bw, bh, cy);
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

        static Rect Center(Rect inner, float w, float h, float cy)
        {
            if (w > inner.width) w = inner.width;
            if (h > inner.height) h = inner.height;
            if (w < 4f) w = 4f;
            if (h < 4f) h = 4f;
            float y = cy - h * 0.5f;
            if (y < inner.y) y = inner.y;
            if (y + h > inner.yMax) y = inner.yMax - h;
            return new Rect(inner.center.x - w * 0.5f, y, w, h);
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

        // Keep every beat's drawn sprite left of ColR (the meter column). Shared by
        // rest, lunge, swipe, strike and outro so SE wide frames never cover Continue.
        public static float ClampFootX(float footX, float artW, float rightLimit)
        {
            float half = artW * 0.5f;
            if (half < 1f) half = 1f;
            float right = footX + half;
            if (right > rightLimit) footX -= right - rightLimit;
            return footX;
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

        public const float PeckSeconds = 0.98f;
        public const float PeckHit = 0.38f;
        public const float RecoilSeconds = 0.55f;
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
            return BadgerAnim.Outro(playerWon, age, now).Frame;
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

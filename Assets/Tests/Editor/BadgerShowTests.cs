#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Honey badger Phase 4: leap timeline, opening landing order, sitter switch, art names.
    // Run from the menu, or drop a file on /tmp/flock-five-badger-show-tests.
    [InitializeOnLoad]
    static class BadgerShowTests
    {
        const string Cmd = "/tmp/flock-five-badger-show-tests";
        const string Out = "/tmp/flock-five-badger-show-tests.txt";
        const float Eps = 0.0005f;

        static BadgerShowTests()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!File.Exists(Cmd) || EditorApplication.isCompiling) return;
            try { File.Delete(Cmd); }
            catch { return; }
            Run();
        }

        [MenuItem("Flock Five/Badger Show Tests")]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0;
            int fail = 0;
            void Line(string t)
            {
                sb.AppendLine(t);
                File.WriteAllText(Out, sb.ToString());
                Debug.Log("[badger-show] " + t);
            }
            void Check(string name, bool ok, string detail)
            {
                if (ok) { pass++; Line("PASS  " + name + "  " + detail); }
                else { fail++; Line("FAIL  " + name + "  " + detail); }
            }

            CheckLeapTimeline(Check);
            CheckLeapFrames(Check);
            CheckLeapWashAndSting(Check);
            CheckOpeningOrder(Check);
            CheckOpeningTiming(Check);
            CheckSitterSwitch(Check);
            CheckArt(Check);
            CheckPhase5Copy(Check);
            CheckFighters(Check);
            CheckArenaBoxes(Check);
            CheckFighterPlace(Check);
            CheckMeter(Check);
            CheckRoundAct(Check);
            CheckOutro(Check);
            CheckSwipe(Check);
            CheckSlamWords(Check);

            Line(fail == 0 ? "ALL OK  " + pass : "FAILED  " + fail + "  passed " + pass);
        }

        static bool Near(float a, float b) => Mathf.Abs(a - b) < Eps;

        static void CheckLeapTimeline(System.Action<string, bool, string> Check)
        {
            float sum = 0f;
            foreach (var b in BadgerLeap.Beats) sum += b;
            bool total = Near(sum, 3.0f) && Near(BadgerLeap.Duration, 3.0f) && BadgerLeap.Beats.Length == 4;
            bool starts = Near(BadgerLeap.BeatStart(BadgerLeapBeat.Hop), 0f)
                && Near(BadgerLeap.BeatStart(BadgerLeapBeat.Crouch), 0.8f)
                && Near(BadgerLeap.BeatStart(BadgerLeapBeat.Leap), 1.4f)
                && Near(BadgerLeap.BeatStart(BadgerLeapBeat.Wash), 2.6f)
                && Near(BadgerLeap.BeatStart(BadgerLeapBeat.Done), 3.0f);
            bool beats = BadgerLeap.BeatAt(0.1f) == BadgerLeapBeat.Hop
                && BadgerLeap.BeatAt(0.9f) == BadgerLeapBeat.Crouch
                && BadgerLeap.BeatAt(2.0f) == BadgerLeapBeat.Leap
                && BadgerLeap.BeatAt(2.8f) == BadgerLeapBeat.Wash
                && BadgerLeap.BeatAt(3.01f) == BadgerLeapBeat.Done;
            Check("leap timeline", total && starts && beats,
                "hop 0.8 + crouch 0.6 + leap 1.2 + wash 0.4 = " + sum.ToString("0.000") + " s");
        }

        static void CheckLeapFrames(System.Action<string, bool, string> Check)
        {
            bool hop = BadgerLeap.FrameAt(0.10f) == "badger_hop_a" && BadgerLeap.FrameAt(0.30f) == "badger_hop_b"
                && BadgerLeap.FrameAt(0.50f) == "badger_hop_a" && BadgerLeap.FrameAt(0.75f) == "badger_hop_b";
            bool crouch = BadgerLeap.FrameAt(1.0f) == "badger_crouch";
            bool leap = BadgerLeap.FrameAt(1.45f) == "badger_leap_0" && BadgerLeap.FrameAt(2.0f) == "badger_leap_1"
                && BadgerLeap.FrameAt(2.5f) == "badger_leap_3" && BadgerLeap.FrameAt(2.9f) == "badger_leap_3";
            var mid = BadgerAnim.Entrance(2.24f);
            bool blend = mid.Frame == "badger_leap_1" && mid.Under == "badger_leap_3"
                && mid.UnderAlpha > 0.2f && mid.FrameAlpha > 0.4f;
            var squat = BadgerAnim.Entrance(1.0f);
            bool squash = squat.Frame == "badger_crouch" && squat.ScaleY < 0.98f && squat.ScaleX > 1.01f;
            var fade = BadgerAnim.Idle(0.6f);
            bool cross = fade.Frame == "badger_idle_b" && fade.Under == "badger_idle_a" && fade.FrameAlpha < 0.05f;
            bool breathe = BadgerAnim.Idle(0.604f).ScaleY > 1.02f;
            bool grow = Near(BadgerLeap.Grow(1.0f), 0f) && Near(BadgerLeap.Grow(2.6f), 1f)
                && BadgerLeap.Grow(2.0f) > 0f && BadgerLeap.Grow(2.0f) < 1f && Near(BadgerLeap.Grow(2.9f), 1f);
            bool travel = Near(BadgerLeap.HopTravel(0f), 0f) && Near(BadgerLeap.HopTravel(0.8f), 1f)
                && Near(BadgerLeap.HopTravel(1.2f), 1f) && Near(BadgerLeap.HopLift(1.0f), 0f);
            Check("leap frames", hop && crouch && leap && blend && squash && cross && breathe && grow && travel,
                "hop_a/hop_b alternate, crouch, leap_0 -> leap_1 -> leap_3, leap_2 is a blend, scale grows to full by 2.6 s");
        }

        static void CheckLeapWashAndSting(System.Action<string, bool, string> Check)
        {
            bool wash = Near(BadgerLeap.WashAlpha(2.5f), 0f) && Near(BadgerLeap.WashAlpha(2.6f), 0f)
                && BadgerLeap.WashAlpha(2.8f) > 0f && Near(BadgerLeap.WashAlpha(3.0f), 1f);
            bool sting = Near(BadgerLeap.TakeoffAt, 1.4f) && BadgerLeap.CrossesTakeoff(1.39f, 1.41f)
                && !BadgerLeap.CrossesTakeoff(1.41f, 1.5f) && !BadgerLeap.CrossesTakeoff(1.2f, 1.39f);
            bool stuck = !BadgerLeap.Stuck(true, 3.0f) && !BadgerLeap.Stuck(false, 99f)
                && BadgerLeap.Stuck(true, 3.0f + TutorialHeal.MaxGateSeconds + 0.1f);
            Check("leap wash + sting", wash && sting && stuck,
                "wash 0 -> 1 over 2.6-3.0 s; sting crosses once at 1.4 s; a dead leap heals after 3.0 + gate");
        }

        static void CheckOpeningOrder(System.Action<string, bool, string> Check)
        {
            var order = BadgerOpening.Order();
            bool count = BadgerOpening.Landings == 16 && order.Length == 16;
            bool grid = true;
            for (int k = 0; k < order.Length; k++)
            {
                if (order[k] != k) grid = false;
                if (BadgerOpening.OrderOf(BadgerOpening.TileAt(k)) != k) grid = false;
                if (k > 0 && !(BadgerOpening.LandAt(k) > BadgerOpening.LandAt(k - 1))) grid = false;
            }
            bool bees = BadgerOpening.BeeFrame(0) == "bee_1" && BadgerOpening.BeeFrame(4) == "bee_5"
                && BadgerOpening.BeeFrame(5) == "bee_1" && BadgerOpening.BeeFrame(15) == "bee_1";
            bool yard = BadgerOpening.BeeScale(true) < BadgerOpening.BeeScale(false);
            Check("opening order", count && grid && bees && yard,
                "16 landings, tile 0..15 in grid order, each later than the last; bee_1..bee_5 cycle; yard bee smaller");
        }

        static void CheckOpeningTiming(System.Action<string, bool, string> Check)
        {
            float d = BadgerOpening.Duration;
            bool about = d > 2.85f && d < 3.30f;
            bool stagger = Near(BadgerOpening.LandAt(1) - BadgerOpening.LandAt(0), 0.1f);
            bool counts = BadgerOpening.LandedCount(0f) == 0
                && BadgerOpening.LandedCount(BadgerOpening.LandAt(0)) == 1
                && BadgerOpening.LandedCount(d) == 16;
            bool flip = Near(BadgerOpening.TileLand(3, 0f), 0f) && Near(BadgerOpening.TileLand(3, BadgerOpening.LandAt(3)), 1f)
                && Near(BadgerOpening.TileLand(15, d), 1f);
            float hit = BadgerOpening.SwipeHitAt;
            bool swipe = !BadgerOpening.HiveSwiped(hit - 0.05f) && BadgerOpening.HiveSwiped(hit + 0.02f)
                && BadgerOpening.SplatAlpha(hit + 0.05f) > 0f
                && Near(BadgerOpening.SplatAlpha(hit + BadgerOpening.SplatSeconds + 0.05f), 0f);
            bool fly = BadgerOpening.Flight(0, 0f) < 0f && BadgerOpening.Flight(0, BadgerOpening.LandAt(0)) > 1f;
            bool post = Near(BadgerOpening.PostSeconds, 0.6f);
            Check("opening timing", about && stagger && counts && flip && swipe && fly && post,
                "about " + d.ToString("0.00") + " s, 0.1 s stagger, landed frame is final, PostSeconds 0.6");
        }

        static void CheckSitterSwitch(System.Action<string, bool, string> Check)
        {
            bool keepOn = BadgerSchedule.Enabled;
            int keepFlag = BadgerSave.PendingFor(true);
            try
            {
                BadgerSave.Clear();
                BadgerSave.Set(15, true);
                BadgerSchedule.Enabled = false;
                bool off = !BadgerSitter.VisibleFor(false, false, false)
                    && !BadgerSitter.Visible(BadgerSave.Pending, false, false);
                BadgerSchedule.Enabled = true;
                bool on = BadgerSitter.VisibleFor(true, false, false)
                    && BadgerSitter.Visible(BadgerSave.Pending, false, false);
                bool leap = !BadgerSitter.VisibleFor(true, true, false);
                bool shot = !BadgerSitter.VisibleFor(true, false, true);
                BadgerSave.Clear();
                bool none = !BadgerSitter.VisibleFor(true, false, false);
                bool breathe = BadgerSitter.FrameAt(0.1f) == "badger_idle_a" && BadgerSitter.FrameAt(0.7f) == "badger_idle_b"
                    && BadgerSitter.FrameAt(1.3f) == "badger_idle_a";
                Check("sitter switch", off && on && leap && shot && none && breathe,
                    "hidden with Enabled false, with no flag, during the leap, and in editor shots; idle A/B at 0.6 s");
            }
            finally
            {
                BadgerSchedule.Enabled = keepOn;
                if (keepFlag > 0) BadgerSave.Set(keepFlag, true);
                else BadgerSave.Clear();
            }
        }

        static void CheckArt(System.Action<string, bool, string> Check)
        {
            string[] names =
            {
                "badger_hop_a", "badger_hop_b", "badger_crouch", "badger_leap_0", "badger_leap_1", "badger_leap_3",
                "badger_idle_a", "badger_idle_b", "badger_shrug", "hive_idle", "hive_swiped", "honey_splat",
                "bee_1", "bee_2", "bee_3", "bee_4", "bee_5"
            };
            var missing = new StringBuilder();
            foreach (var n in names)
                if (SpriteCatalog.BadgerArt(n) == null) missing.Append(n).Append(' ');
            Check("art", missing.Length == 0,
                missing.Length == 0 ? "all 17 frames load from Sprites/Badger/" : "missing: " + missing);
        }

        static void CheckPhase5Copy(System.Action<string, bool, string> Check)
        {
            bool ends = BadgerCopy.Win == "Fine. Take the yard."
                && BadgerCopy.Lose == "Yard's still mine."
                && BadgerCopy.OpenLater.StartsWith("Hive's mine");
            bool verdict = BadgerCopy.Tie == "That didn't prove much."
                && BadgerCopy.DontCare == "Honey badger don't care."
                && BadgerCopy.MineNow == "Mine now."
                && BadgerCopy.CoinsShort == "Coins short.";
            var winRound = new BadgerRound { PlayerGained = 3, BossGained = 0 };
            var theft = new BadgerRound { PlayerGained = 0, BossGained = 4 };
            bool beats = BadgerCopy.ShowShrug(winRound) && BadgerCopy.ShowTheftSplat(theft)
                && !BadgerCopy.ShowTheftSplat(winRound);
            Check("phase5 copy", ends && verdict && beats,
                "win/lose/opening/verdict lines; shrug + theft splat rules");
        }

        static void CheckFighters(System.Action<string, bool, string> Check)
        {
            var stages = (BadgerStage[])System.Enum.GetValues(typeof(BadgerStage));
            string[] need =
            {
                "PostOpen", "BossWait", "YourPick", "Reveal", "Verdict", "Outro", "Over", "Opening", "Lesson", "DontCare"
            };
            bool named = stages.Length >= need.Length;
            for (int i = 0; i < need.Length; i++)
            {
                bool found = false;
                for (int s = 0; s < stages.Length; s++)
                    if (stages[s].ToString() == need[i]) found = true;
                if (!found) named = false;
            }
            bool shown = true;
            for (int s = 0; s < stages.Length; s++)
            {
                if (!BadgerFighter.BothVisible(stages[s]) || BadgerFighter.Alpha(stages[s]) < 0.99f)
                    shown = false;
            }
            bool fade = BadgerOpening.CastAlpha(BadgerOpening.Duration) < 0.05f
                && BadgerFighter.Alpha(BadgerStage.Opening) > 0.99f
                && BadgerFighter.Alpha(BadgerStage.PostOpen) > 0.99f
                && BadgerFighter.Alpha(BadgerStage.DontCare) > 0.99f;
            bool frames = BadgerFighter.FrameAt(BadgerDuelBeat.Idle, 0f, 0.1f) == BadgerSitter.FrameAt(0.1f)
                && BadgerFighter.FrameAt(BadgerDuelBeat.Shrug, 0f, 0f) == "badger_shrug"
                && BadgerFighter.FrameAt(BadgerDuelBeat.Swipe, 0.05f, 0f) == BadgerSwipe.FrameAt(0.05f, 0f)
                && BadgerDuel.Frame(BadgerDuelBeat.Swipe, 0.05f, 0f) == BadgerSwipe.FrameAt(0.05f, 0f);
            Check("fighters", named && shown && fade && frames,
                "both fighters stay at full alpha in every stage; hive cast fade does not hide them; one swipe frame");
        }

        static void CheckArenaBoxes(System.Action<string, bool, string> Check)
        {
            var arena = new Rect(12f, 40f, 360f, 96f);
            var b = BadgerArenaRects.Split(arena);
            Rect[] box = { b.YouFighter, b.YouSlot, b.Middle, b.BossSlot, b.BossFighter };
            float[] want = { 0.20f, 0.22f, 0.10f, 0.22f, 0.20f };
            bool inside = true;
            bool order = true;
            bool gap = true;
            bool ratio = true;
            for (int i = 0; i < box.Length; i++)
            {
                var r = box[i];
                if (r.width < 4f || r.x < arena.x - 0.2f || r.xMax > arena.xMax + 0.2f) inside = false;
                if (Mathf.Abs(r.y - arena.y) > 0.2f || Mathf.Abs(r.height - arena.height) > 0.2f) inside = false;
                float frac = r.width / arena.width;
                if (frac < want[i] - 0.03f || frac > want[i] + 0.02f) ratio = false;
                if (i == 0) continue;
                if (r.x < box[i - 1].xMax - 0.05f) order = false;
                float g = r.x - box[i - 1].xMax;
                if (g < arena.width * 0.005f || g > arena.width * 0.03f) gap = false;
            }
            bool sides = b.YouFighter.center.x < b.Middle.center.x && b.BossFighter.center.x > b.Middle.center.x;
            Check("arena boxes", inside && order && gap && ratio && sides,
                "five non-overlapping boxes, about 0.20/0.22/0.10/0.22/0.20, fighters on the outside");
        }

        static void CheckFighterPlace(System.Action<string, bool, string> Check)
        {
            var arena = new Rect(40f, 280f, 670f, 220f);
            var box = BadgerArenaRects.Split(arena);
            var mark = BadgerFighterPlace.Rest(box, 480f, 640f);
            bool read = BadgerFighterPlace.Readable(mark, arena);
            bool birdInYou = mark.BirdBox.xMin >= box.YouFighter.xMin - 0.5f
                && mark.BirdBox.xMax <= box.YouFighter.xMax + 0.5f;
            bool badgerInBoss = mark.BadgerBox.xMin >= box.BossFighter.xMin - 0.5f
                && mark.BadgerBox.xMax <= box.BossFighter.xMax + 0.5f
                && mark.BadgerBox.height > box.BossFighter.height * 0.5f;
            bool face = !mark.FaceLeft && mark.Bird.x < box.Middle.center.x && mark.Foot.x > box.Middle.center.x;

            // SE 750x1334: wide crouch / leap_1 / leap_3 / shrug must stay left of ColR.
            float seW = 750f, seH = 1334f;
            float colW = 64f;
            float gx = 8f + colW + 10f;
            float gw = seW - colW - 10f - gx - colW;
            var seArena = new Rect(gx, 400f, gw, seH * 0.16f);
            var seBox = BadgerArenaRects.Split(seArena);
            float colR = seW - 8f - colW; // left edge of ColR
            var idle = BadgerFighterPlace.Rest(seBox, 480f, 640f);
            string[] wide = { "badger_crouch", "badger_leap_1", "badger_leap_3", "badger_shrug" };
            // Approximate aspect from the idle-fitted unit: scale width by typical frame ratios.
            float[] wideW = { 560f, 620f, 640f, 600f };
            float[] wideH = { 640f, 640f, 640f, 640f };
            bool seClear = true;
            for (int i = 0; i < wide.Length; i++)
            {
                float unit = idle.Unit;
                float w = wideW[i] * unit;
                float footX = BadgerFighterPlace.ClampFootX(idle.Foot.x, w, colR - 2f);
                float right = footX + w * 0.5f;
                if (right > colR - 1.5f) seClear = false;
            }

            Check("fighter place", read && birdInYou && badgerInBoss && face && seClear,
                "hummingbird in the left box facing right, badger in the right box; SE wide frames stay left of ColR");
        }

        static void CheckMeter(System.Action<string, bool, string> Check)
        {
            var col = new Rect(8f, 120f, 78f, 900f);
            float unit = BadgerMeter.Unit(col, 18);
            var n0 = BadgerMeter.Comb(col, 0, unit);
            var n1 = BadgerMeter.Comb(col, 1, unit);
            var top = BadgerMeter.Comb(col, 17, unit);
            var num = BadgerMeter.Number(col);
            bool order = n0.y > n1.y && n1.y > top.y && top.y >= num.yMax - 1f;
            bool gap = n0.yMin >= n1.yMax - 0.2f;
            float you = 10f * unit;
            float boss = 18f * unit;
            bool shorter = you < boss - 4f && boss <= col.height - num.height + 1f;
            bool pulse = Near(BadgerRoundAct.Pulse(0f), 1f)
                && Near(BadgerRoundAct.Pulse(BadgerRoundAct.PulseSeconds), 1f)
                && BadgerRoundAct.Pulse(BadgerRoundAct.PulseSeconds * 0.5f) > 1.05f;
            Check("meter", order && gap && shorter && pulse && unit > 4f,
                "combs stack from the bottom under a number band, 10 shorter than 18, pulse returns to 1");
        }

        static void CheckRoundAct(System.Action<string, bool, string> Check)
        {
            bool kind = BadgerRoundAct.Of(0, 4) == BadgerRoundAct.Kind.Swipe
                && BadgerRoundAct.Of(3, 0) == BadgerRoundAct.Kind.Peck
                && BadgerRoundAct.Of(0, 0) == BadgerRoundAct.Kind.None;
            float hit = BadgerSwipe.HitAt;
            bool recoil = Near(BadgerRoundAct.Recoil(hit - 0.02f), 0f)
                && BadgerRoundAct.Recoil(hit + 0.08f) > 0.4f
                && Near(BadgerRoundAct.Recoil(hit + BadgerRoundAct.RecoilSeconds + 0.05f), 0f);
            bool swipe = BadgerFighter.FrameAt(BadgerDuelBeat.Swipe, hit, 0f) == BadgerSwipe.FrameAt(hit, 0f);
            bool peck = Near(BadgerRoundAct.Peck(0f), 0f)
                && BadgerRoundAct.Peck(BadgerRoundAct.PeckSeconds * 0.5f) > 0.9f
                && Near(BadgerRoundAct.Peck(BadgerRoundAct.PeckSeconds), 0f)
                && BadgerRoundAct.PeckHits(0.30f, 0.48f)
                && !BadgerRoundAct.PeckHits(0.48f, 0.60f);
            bool splat = BadgerRoundAct.SplatAlpha(hit + 0.02f) > 0.8f
                && Near(BadgerRoundAct.SplatAlpha(0f), 0f);
            Check("round act", kind && recoil && swipe && peck && splat,
                "badger score swipes, player score pecks, tie is quiet, splat follows the claw");
        }

        static void CheckOutro(System.Action<string, bool, string> Check)
        {
            bool len = BadgerOutro.Duration >= 1f && BadgerOutro.Duration <= 1.5f;
            bool taunt = BadgerOutro.Frame(false, 0.1f, 0.1f) == "badger_leap_1"
                && BadgerOutro.Frame(false, 0.5f, 0.1f) == "badger_leap_3"
                && BadgerOutro.Frame(false, 1.1f, 0.1f) == BadgerSitter.FrameAt(0.1f);
            bool shrug = BadgerOutro.Frame(true, 0.2f, 0f) == "badger_shrug"
                && BadgerOutro.Frame(true, BadgerOutro.Duration, 0f) == "badger_shrug";
            bool back = Near(BadgerOutro.BackOff(true, 0f), 0f)
                && Near(BadgerOutro.BackOff(true, BadgerOutro.Duration), 1f)
                && Near(BadgerOutro.BackOff(false, 1f), 0f);
            bool droop = Near(BadgerOutro.Droop(false, 0f), 0f)
                && Near(BadgerOutro.Droop(false, BadgerOutro.Duration), BadgerOutro.DroopDegrees)
                && Near(BadgerOutro.Droop(true, 1f), 0f);
            bool proud = BadgerOutro.Proud(true, BadgerOutro.Duration) > 0.05f
                && Near(BadgerOutro.Proud(false, 1f), 0f);
            bool pop = !BadgerOutro.PopUp(BadgerOutro.Duration - 0.05f) && BadgerOutro.PopUp(BadgerOutro.Duration);
            Check("outro", len && taunt && shrug && back && droop && proud && pop,
                "1.25 s, taunt + droop or shrug + back-off, line after the beat");
        }

        static void CheckSwipe(System.Action<string, bool, string> Check)
        {
            float sum = BadgerSwipe.WindupSeconds + BadgerSwipe.StrikeSeconds
                + BadgerSwipe.HoldSeconds + BadgerSwipe.RecoverSeconds;
            bool clock = Near(sum, BadgerSwipe.Duration) && Near(BadgerSwipe.Duration, BadgerOpening.SwipeSeconds)
                && BadgerSwipe.SlashSeconds >= 0.18f && BadgerSwipe.SlashSeconds <= 0.30f
                && BadgerSwipe.WindupSeconds >= 0.38f && BadgerSwipe.WindupSeconds <= 0.42f
                && BadgerSwipe.HoldSeconds >= 0.14f && BadgerSwipe.HoldSeconds <= 0.16f
                && BadgerSwipe.Streaks == 3;
            bool hit = BadgerSwipe.HitAt > BadgerSwipe.WindupSeconds
                && BadgerSwipe.HitAt < BadgerSwipe.WindupSeconds + BadgerSwipe.SlashSeconds
                && !BadgerOpening.HiveSwiped(BadgerSwipe.HitAt - 0.05f)
                && BadgerOpening.HiveSwiped(BadgerSwipe.HitAt + 0.02f);
            bool wind = BadgerSwipe.FrameAt(0.06f, 0f) == "badger_crouch"
                && BadgerSwipe.Lean(0.32f) > 4f && BadgerSwipe.Lunge(0.32f) < 0f;
            bool hold = BadgerSwipe.PhaseAt(BadgerSwipe.HoldStart + 0.02f) == BadgerSwipe.Phase.Hold
                && BadgerSwipe.FrameAt(BadgerSwipe.HoldStart + 0.02f, 0f) == "badger_leap_3";
            float early = BadgerSwipe.WindupSeconds + BadgerSwipe.StrikeSeconds * 0.20f;
            float late = BadgerSwipe.WindupSeconds + BadgerSwipe.StrikeSeconds * 0.80f;
            bool strike = BadgerSwipe.FrameAt(early, 0f) == "badger_leap_1"
                && BadgerSwipe.FrameAt(late, 0f) == "badger_leap_3"
                && BadgerSwipe.Lean(late) < -8f && BadgerSwipe.Lunge(late) > 0.5f
                && BadgerSwipe.SlashAlpha(late) > 0.9f;
            float backT = BadgerSwipe.Duration - 0.02f;
            bool back = BadgerSwipe.FrameAt(backT, 0.1f) == BadgerSitter.FrameAt(0.1f)
                && Near(BadgerSwipe.Lunge(BadgerSwipe.Duration), 0f)
                && Near(BadgerSwipe.Lean(BadgerSwipe.Duration), 0f)
                && Near(BadgerSwipe.SlashAlpha(0f), 0f)
                && BadgerSwipe.FlashAlpha(BadgerSwipe.HitAt) > 0.9f;
            var hive = new Rect(100f, 80f, 140f, 120f);
            int n = 0;
            float minX = 9999f;
            float maxX = -9999f;
            float tipW = 0f;
            float midW = 0f;
            bool each = true;
            for (int s = 0; s < BadgerSwipe.Streaks; s++)
            {
                int got = 0;
                for (int i = 0; i < BadgerSwipe.Segs; i++)
                {
                    Vector2 a;
                    Vector2 b;
                    float w;
                    float cover;
                    if (!BadgerSwipe.Segment(s, i, 1f, hive, out a, out b, out w, out cover)) continue;
                    got++;
                    n++;
                    if (a.x < minX) minX = a.x;
                    if (b.x < minX) minX = b.x;
                    if (a.x > maxX) maxX = a.x;
                    if (b.x > maxX) maxX = b.x;
                    if (i == 3) midW = w;
                    if (i >= BadgerSwipe.Segs - 1) tipW = w;
                }
                if (got < 1) each = false;
            }
            Vector2 za;
            Vector2 zb;
            float zw;
            float zc;
            bool quiet = !BadgerSwipe.Segment(0, 0, 0f, hive, out za, out zb, out zw, out zc);
            bool cross = each && n >= 3 && minX < hive.center.x && maxX > hive.center.x && tipW > 0f && midW > tipW;
            Check("swipe", clock && hit && wind && hold && strike && back && quiet && cross,
                "0.40 s wind-up, leap_1/leap_3 snap, 0.15 s hold, hit inside the slash, back to idle");
        }

        static bool SlamPhone(float w, float h, Rect safeBl)
        {
            var safe = DontCareSlamRects.SafeGui(w, h, safeBl);
            var band = DontCareSlamRects.WordBand(safe);
            var c = new Vector2[4];
            DontCareSlamRects.RotatedCorners(band, BadgerSlam.PopFrom, DontCareSlamRects.TiltDegrees, c);
            for (int i = 0; i < 4; i++)
            {
                if (c[i].x < safe.x + 1f || c[i].x > safe.xMax - 1f) return false;
                if (c[i].y < safe.y + 1f || c[i].y > safe.yMax - 1f) return false;
            }
            float leftY = (c[0].y + c[3].y) * 0.5f;
            float rightY = (c[1].y + c[2].y) * 0.5f;
            return rightY < leftY - 1f && band.width > 8f && band.height > 8f;
        }

        static bool SlamStack(float w, float h, Rect safeBl)
        {
            var safe = DontCareSlamRects.SafeGui(w, h, safeBl);
            float prevMax = -100000f;
            var c = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                var band = DontCareSlamRects.WordSlot(safe, i);
                DontCareSlamRects.RotatedCorners(band, BadgerSlam.PopFrom, DontCareSlamRects.TiltDegrees, c);
                float minX = c[0].x;
                float maxX = c[0].x;
                float minY = c[0].y;
                float maxY = c[0].y;
                for (int k = 0; k < 4; k++)
                {
                    if (c[k].x < minX) minX = c[k].x;
                    if (c[k].x > maxX) maxX = c[k].x;
                    if (c[k].y < minY) minY = c[k].y;
                    if (c[k].y > maxY) maxY = c[k].y;
                }
                if (minX < safe.x + 1f || maxX > safe.xMax - 1f) return false;
                if (minY < safe.y + 1f || maxY > safe.yMax - 1f) return false;
                if (minY < prevMax + 2f) return false;
                prevMax = maxY;
            }
            return true;
        }

        static void CheckSlamWords(System.Action<string, bool, string> Check)
        {
            float early = BadgerSlam.WashAlpha(0.10f);
            float eye = BadgerSlam.WashAlpha(BadgerSlam.ZoomInEnd - 0.01f);
            bool wash = early > 0.80f && early < 0.92f && eye > 0.30f && eye < 0.60f
                && Near(BadgerSlam.WashAlpha(BadgerSlam.Duration), 0f);
            bool tilt = Near(DontCareSlamRects.TiltDegrees, -15f);
            // Pixels, portrait, safeArea bottom-left. SE status bar 40px. 15 and Pro Max
            // are 3x of 59pt top / 34pt bottom.
            bool phones = SlamPhone(750f, 1334f, new Rect(0f, 0f, 750f, 1294f))
                && SlamPhone(1179f, 2556f, new Rect(0f, 102f, 1179f, 2277f))
                && SlamPhone(1290f, 2796f, new Rect(0f, 102f, 1290f, 2517f));
            bool pace = BadgerSlam.WordSeconds >= 0.35f && BadgerSlam.WordSeconds <= 0.45f;
            float late = BadgerSlam.Duration - 0.05f;
            bool stay = BadgerSlam.ShownCount(BadgerSlam.WordSeconds * 3f + 0.05f) == 4
                && BadgerSlam.ShownCount(late) == 4
                && BadgerSlam.ShownCount(BadgerSlam.Duration) == 0
                && BadgerSlam.WordOn(late, 0) && BadgerSlam.WordOn(late, 3);
            bool pop = Near(BadgerSlam.WordPop(0f, 0), BadgerSlam.PopFrom)
                && Near(BadgerSlam.WordPop(BadgerSlam.WordSeconds * 0.5f, 0), 1f);
            BadgerSlam.Shake(0.02f, out float sx, out float sy);
            BadgerSlam.Shake(0.30f, out float zx, out float zy);
            bool shake = sx * sx + sy * sy > 1f && zx * zx + zy * zy < 0.01f;
            bool stack = SlamStack(750f, 1334f, new Rect(0f, 0f, 750f, 1294f))
                && SlamStack(1179f, 2556f, new Rect(0f, 102f, 1179f, 2277f))
                && SlamStack(1290f, 2796f, new Rect(0f, 102f, 1290f, 2517f));
            bool plus = BadgerGainFloat.Text(2) == "+2" && BadgerGainFloat.Text(0) == "" && BadgerGainFloat.Text(5) == "+5";
            Check("slam words", wash && tilt && phones && pace && stay && pop && shake && stack && plus,
                "four words 0.40 s apart, slam from big, stay until the beat ends, inside the safe area");
        }
    }
}
#endif

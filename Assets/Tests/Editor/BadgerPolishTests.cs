#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace FlockFive.Editor
{
    // EditMode checks for the honey-badger polish: the shared tell/flash clocks,
    // the arc, the one twist per visit, the desperate slam bump, and the sharp hit.
    public class BadgerPolishTests
    {
        [SetUp]
        public void Mute()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

        [Test]
        public void TellIsTheSameCurveEveryTime()
        {
            Assert.That(BossBeat.TellSeconds, Is.EqualTo(0.50f).Within(0.0001f));
            Assert.That(BossBeat.Glint(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BossBeat.Glint(BossBeat.TellSeconds), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BossBeat.Glint(-0.1f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BossBeat.Glint(0.25f), Is.GreaterThan(0.99f));
            Assert.That(BossBeat.TellDone(0.50f), Is.True);
            Assert.That(BossBeat.TellDone(0.49f), Is.False);
            float a = BossBeat.Glint(0.10f);
            float b = BossBeat.Glint(0.10f);
            Assert.That(a, Is.EqualTo(b).Within(0.00001f));
            Assert.That(BossBeat.PawTwitch(0.25f), Is.EqualTo(-7f * BossBeat.Glint(0.25f)).Within(0.0001f));
            Assert.That(BossBeat.PawTwitch(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BossBeat.BeatSeconds, Is.EqualTo(0.40f).Within(0.0001f));
            Assert.That(BossBeat.SilenceSeconds, Is.EqualTo(BossBeat.BeatSeconds).Within(0.0001f));
            Assert.That(BossBeat.PitchCap, Is.EqualTo(1.04f).Within(0.0001f));
            Assert.That(BossBeat.ClampPitch(0.5f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BossBeat.ClampPitch(2f), Is.EqualTo(BossBeat.PitchCap).Within(0.0001f));
        }

        [Test]
        public void FinalBlowIsFlashThenOneBeatThenFanfare()
        {
            float flash = BossBeat.FlashSeconds;
            float silence = BossBeat.SilenceSeconds;
            Assert.That(BadgerPolish.BlowAt(-0.01f), Is.EqualTo(BadgerBlowPhase.None));
            Assert.That(BadgerPolish.BlowAt(0f), Is.EqualTo(BadgerBlowPhase.Flash));
            Assert.That(BadgerPolish.BlowAt(flash - 0.001f), Is.EqualTo(BadgerBlowPhase.Flash));
            Assert.That(BadgerPolish.BlowAt(flash), Is.EqualTo(BadgerBlowPhase.Silence));
            Assert.That(BadgerPolish.BlowAt(flash + silence - 0.001f), Is.EqualTo(BadgerBlowPhase.Silence));
            Assert.That(BadgerPolish.BlowAt(flash + silence), Is.EqualTo(BadgerBlowPhase.Fanfare));
            Assert.That(BadgerPolish.FanfareDue(flash + silence - 0.01f, flash + silence), Is.True);
            Assert.That(BadgerPolish.FanfareDue(0.01f, 0.02f), Is.False);
            Assert.That(BadgerPolish.FanfareDue(flash + silence, flash + silence + 0.1f), Is.False);
            Assert.That(BadgerPolish.BlowFlash(0f), Is.GreaterThan(0.99f));
            Assert.That(BadgerPolish.BlowFlash(flash + 0.01f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BadgerPolish.BlowDuration, Is.EqualTo(flash + silence).Within(0.0001f));
        }

        [Test]
        public void ArcAndWearClimbWithTheScore()
        {
            const int target = 10;
            Assert.That(BadgerPolish.Mood(0, target, false), Is.EqualTo(BadgerMood.Smug));
            for (int s = 1; s <= 6; s++)
                Assert.That(BadgerPolish.Mood(s, target, false), Is.EqualTo(BadgerMood.Rattled), "score " + s);
            Assert.That(BadgerPolish.Mood(7, target, false), Is.EqualTo(BadgerMood.Desperate));
            Assert.That(BadgerPolish.Mood(10, target, true), Is.EqualTo(BadgerMood.Surrender));

            Assert.That(BadgerPolish.WearOf(0, target), Is.EqualTo(BadgerWear.Fresh));
            for (int s = 1; s <= 3; s++)
                Assert.That(BadgerPolish.WearOf(s, target), Is.EqualTo(BadgerWear.Scuffed), "score " + s);
            for (int s = 4; s <= 6; s++)
                Assert.That(BadgerPolish.WearOf(s, target), Is.EqualTo(BadgerWear.Bruised), "score " + s);
            Assert.That(BadgerPolish.WearOf(7, target), Is.EqualTo(BadgerWear.Wrecked));

            Assert.That(BadgerPolish.BruiseCount(BadgerWear.Fresh), Is.EqualTo(0));
            Assert.That(BadgerPolish.BruiseCount(BadgerWear.Scuffed), Is.EqualTo(1));
            Assert.That(BadgerPolish.BruiseCount(BadgerWear.Bruised), Is.EqualTo(2));
            Assert.That(BadgerPolish.BruiseCount(BadgerWear.Wrecked), Is.EqualTo(3));

            Assert.That(BadgerPolish.EarsBack(BadgerMood.Smug), Is.EqualTo(0f).Within(0.01f));
            float rattled = BadgerPolish.EarsBack(BadgerMood.Rattled);
            float desperate = BadgerPolish.EarsBack(BadgerMood.Desperate);
            float give = BadgerPolish.EarsBack(BadgerMood.Surrender);
            Assert.That(rattled, Is.GreaterThan(0f));
            Assert.That(desperate, Is.GreaterThan(rattled));
            Assert.That(give, Is.GreaterThan(desperate));

            Assert.That(BadgerPolish.Sweat(BadgerMood.Rattled), Is.True);
            Assert.That(BadgerPolish.Sweat(BadgerMood.Desperate), Is.True);
            Assert.That(BadgerPolish.Sweat(BadgerMood.Smug), Is.False);
            Assert.That(BadgerPolish.Sweat(BadgerMood.Surrender), Is.False);

            BadgerPolish.WearScale(BadgerWear.Fresh, out float sx, out float sy);
            Assert.That(sx, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(sy, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BadgerPolish.HitLean(BadgerWear.Wrecked, 1f), Is.GreaterThan(BadgerPolish.HitLean(BadgerWear.Fresh, 1f)));
        }

        [Test]
        public void WearStagesMapOntoTheNinePoses()
        {
            Assert.That(BadgerWearArt.Poses.Length, Is.EqualTo(9));
            Assert.That(BadgerWearArt.Tokens, Is.EqualTo(new[] { "fresh", "scuffed", "bruised", "wrecked" }));
            Assert.That(BadgerWearArt.Token(BadgerWear.Fresh), Is.EqualTo("fresh"));
            Assert.That(BadgerWearArt.Token(BadgerWear.Scuffed), Is.EqualTo("scuffed"));
            Assert.That(BadgerWearArt.Token(BadgerWear.Bruised), Is.EqualTo("bruised"));
            Assert.That(BadgerWearArt.Token(BadgerWear.Wrecked), Is.EqualTo("wrecked"));

            const int target = 10;
            Assert.That(BadgerWearArt.StageKey("badger_idle_b", BadgerPolish.WearOf(0, target)), Is.EqualTo("badger_idle_b_wear_fresh"));
            Assert.That(BadgerWearArt.StageKey("badger_idle_b", BadgerPolish.WearOf(2, target)), Is.EqualTo("badger_idle_b_wear_scuffed"));
            Assert.That(BadgerWearArt.StageKey("badger_idle_b", BadgerPolish.WearOf(5, target)), Is.EqualTo("badger_idle_b_wear_bruised"));
            Assert.That(BadgerWearArt.StageKey("badger_crouch", BadgerPolish.WearOf(8, target)), Is.EqualTo("badger_crouch_wear_wrecked"));
            Assert.That(BadgerWearArt.StageKey("hive_idle", BadgerWear.Wrecked), Is.EqualTo(""));

            const string art = "Assets/_Project/Art/Resources/Sprites/Badger";
            var wears = new[] { BadgerWear.Fresh, BadgerWear.Scuffed, BadgerWear.Bruised, BadgerWear.Wrecked };
            int missing = 0;
            var miss = new System.Text.StringBuilder();
            for (int p = 0; p < BadgerWearArt.Poses.Length; p++)
            {
                for (int w = 0; w < wears.Length; w++)
                {
                    string key = BadgerWearArt.StageKey(BadgerWearArt.Poses[p], wears[w]);
                    string path = art + "/" + key + ".png";
                    if (File.Exists(path)) continue;
                    missing++;
                    miss.Append(key).Append(' ');
                }
            }
            Assert.That(missing, Is.EqualTo(0), miss.ToString());

            string duel = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            string stills = File.ReadAllText("Assets/_Project/Scripts/Editor/BadgerPolishStills.cs");
            Assert.That(duel.Contains("BadgerWearArt.PaintWear"), Is.True);
            Assert.That(stills.Contains("BadgerWearArt.PaintWear"), Is.True);
            Assert.That(duel.Contains("static bool PaintWear("), Is.True);
        }

        [Test]
        public void PaddedDestKeepsTheUnpaddedBodyPlanted()
        {
            Assert.That(BadgerWearArt.PadX, Is.EqualTo(70));
            Assert.That(BadgerWearArt.PadTop, Is.EqualTo(110));
            var sizes = new[]
            {
                new Vector2(363f, 634f),
                new Vector2(438f, 640f),
                new Vector2(803f, 604f),
                new Vector2(645f, 599f),
                new Vector2(608f, 600f),
                new Vector2(668f, 602f),
                new Vector2(736f, 614f),
                new Vector2(977f, 669f),
                new Vector2(618f, 654f),
            };
            var foot = new Vector2(400f, 900f);
            for (int i = 0; i < BadgerWearArt.Poses.Length; i++)
            {
                string pose = BadgerWearArt.Poses[i];
                float srcW = sizes[i].x;
                float srcH = sizes[i].y;
                var unpadded = SpriteAnim.Dest(pose, srcW, srcH, foot, 1f, 1f, 1f);
                var planted = SpriteAnim.Planted(pose, srcW, srcH, foot, 1f);
                Assert.That(Vector2.Distance(planted, foot), Is.LessThan(0.6f), pose);

                var padded = BadgerWearArt.PaddedDest(unpadded, srcW, srcH);
                Assert.That(padded.width, Is.EqualTo(srcW + BadgerWearArt.PadW).Within(0.01f), pose);
                Assert.That(padded.height, Is.EqualTo(srcH + BadgerWearArt.PadTop).Within(0.01f), pose);
                var body = BadgerWearArt.BodyOfPadded(padded, srcW, srcH);
                Assert.That(body.x, Is.EqualTo(unpadded.x).Within(0.01f), pose);
                Assert.That(body.y, Is.EqualTo(unpadded.y).Within(0.01f), pose);
                Assert.That(body.width, Is.EqualTo(unpadded.width).Within(0.01f), pose);
                Assert.That(body.height, Is.EqualTo(unpadded.height).Within(0.01f), pose);

                var blit = BadgerWearArt.PaintWear(pose, unpadded, srcW, srcH, BadgerWear.Scuffed);
                Assert.That(blit.Sprite, Is.EqualTo(pose + "_wear_scuffed"));
                Assert.That(blit.Dest.x, Is.EqualTo(padded.x).Within(0.01f), pose);
                Assert.That(blit.Dest.y, Is.EqualTo(padded.y).Within(0.01f), pose);
                Assert.That(blit.Dest.width, Is.EqualTo(padded.width).Within(0.01f), pose);
                Assert.That(blit.Dest.height, Is.EqualTo(padded.height).Within(0.01f), pose);
                Assert.That(blit.StagePng, Is.True);
            }
        }

        [Test]
        public void WearDrawOrderIsStagePngWithoutASecondHalo()
        {
            Assert.That(BadgerWearArt.UseStagePngs, Is.True);
            Assert.That(BadgerWearArt.CompositeOrder, Is.EqualTo(new[] { "halo_back", "body", "halo_front" }));
            var layers = new System.Collections.Generic.List<string>();
            var wears = new[] { BadgerWear.Fresh, BadgerWear.Scuffed, BadgerWear.Bruised, BadgerWear.Wrecked };
            for (int p = 0; p < BadgerWearArt.Poses.Length; p++)
            {
                for (int w = 0; w < wears.Length; w++)
                {
                    BadgerWearArt.FillDrawList(BadgerWearArt.Poses[p], wears[w], layers);
                    Assert.That(BadgerWearArt.DrawListIsSafe(layers), Is.True, BadgerWearArt.Poses[p] + " " + wears[w]);
                    Assert.That(layers.Count, Is.EqualTo(1));
                    Assert.That(layers[0], Is.EqualTo(BadgerWearArt.StageKey(BadgerWearArt.Poses[p], wears[w])));
                    Assert.That(layers[0].Contains("halo_"), Is.False);
                }
            }
            layers.Clear();
            layers.Add("halo_back_idle_a_scuffed");
            layers.Add("badger_idle_a_wear_scuffed");
            layers.Add("halo_front_idle_a_scuffed");
            Assert.That(BadgerWearArt.DrawListIsSafe(layers), Is.False, "stage PNG plus halo files is a double halo");
        }

        [Test]
        public void MusicTightensWithoutPassingThePitchCap()
        {
            Assert.That(BadgerPolish.DesperatePitch, Is.EqualTo(1.02f).Within(0.0001f));
            Assert.That(BadgerPolish.DesperatePitch, Is.LessThan(BossBeat.PitchCap));
            Assert.That(BadgerPolish.LoopPitch(BadgerMood.Smug, BadgerTwist.StrongerTiles), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BadgerPolish.LoopPitch(BadgerMood.Desperate, BadgerTwist.NewTaunt), Is.EqualTo(BadgerPolish.DesperatePitch).Within(0.0001f));
            Assert.That(BadgerPolish.LoopPitch(BadgerMood.Desperate, BadgerTwist.NewTaunt), Is.LessThan(BossBeat.PitchCap));
            Assert.That(BadgerPolish.LoopPitch(BadgerMood.Smug, BadgerTwist.FasterRiff), Is.EqualTo(1.04f).Within(0.0001f));
            float both = BadgerPolish.LoopPitch(BadgerMood.Desperate, BadgerTwist.FasterRiff);
            Assert.That(both, Is.EqualTo(BossBeat.PitchCap).Within(0.0001f));
            Assert.That(both, Is.LessThanOrEqualTo(BossBeat.PitchCap));
        }

        [Test]
        public void LoopPitchSlewsAfterTheCueAndTheCutRamps()
        {
            Assert.That(MixDesk.BadgerPitchSlewSeconds, Is.GreaterThanOrEqualTo(0.30f).And.LessThanOrEqualTo(0.50f));
            Assert.That(MixDesk.BadgerCutRampSeconds, Is.GreaterThanOrEqualTo(0.005f).And.LessThanOrEqualTo(0.010f));
            float riff = BadgerPolish.LoopPitch(BadgerMood.Smug, BadgerTwist.FasterRiff);
            // Entrance cue still speaking: the loop stays at 1. No +68 cent step onto the riff.
            Assert.That(MixDesk.PitchTarget(riff, true, true), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(MixDesk.SlewPitch(1f, riff, 0f), Is.EqualTo(1f).Within(0.0001f));
            float mid = MixDesk.SlewPitch(1f, riff, 0.5f);
            Assert.That(mid, Is.GreaterThan(1f));
            Assert.That(mid, Is.LessThan(riff));
            Assert.That(MixDesk.PitchTarget(riff, false, true), Is.EqualTo(riff).Within(0.0001f));
            Assert.That(MixDesk.SlewPitch(1f, riff, 1f), Is.EqualTo(riff).Within(0.0001f));
            Assert.That(MixDesk.CutGain(0f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(MixDesk.CutGain(MixDesk.BadgerCutRampSeconds * 0.5f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(MixDesk.CutGain(MixDesk.BadgerCutRampSeconds), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void CutRampsAcrossTicksThenStops()
        {
            bool paused = AudioListener.pause;
            float listen = AudioListener.volume;
            AudioListener.pause = false;
            AudioListener.volume = 0f;
            var go = new GameObject("badger-cut-ramp");
            var desk = go.AddComponent<MixDesk>();
            desk.AttachForTests();
            AudioClip clip = null;
            try
            {
                desk.PlantBadgerVoicesForTests(out var loop, out var cue, out var sting);
                clip = loop.clip;
                Assert.That(loop.isPlaying && cue.isPlaying && sting.isPlaying, Is.True, "probe voices did not start");
                float full = loop.volume;

                float t0 = Time.realtimeSinceStartup;
                desk.BadgerCut();
                float waited = Time.realtimeSinceStartup - t0;
                Assert.That(waited, Is.LessThan(MixDesk.BadgerCutRampSeconds * 0.5f), "cut blocked the frame");
                Assert.That(desk.BadgerOnForTests, Is.True);
                Assert.That(loop.isPlaying && cue.isPlaying && sting.isPlaying, Is.True, "cut stopped the voices on the same call");
                Assert.That(loop.volume, Is.EqualTo(full).Within(0.0001f));
                Assert.That(cue.volume, Is.EqualTo(full).Within(0.0001f));
                Assert.That(sting.volume, Is.EqualTo(full).Within(0.0001f));

                // A tick with no clock must not finish the ramp or spin.
                float held = desk.TickBadgerForTests(0f);
                Assert.That(held, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(desk.BadgerOnForTests, Is.True);
                Assert.That(loop.isPlaying && cue.isPlaying && sting.isPlaying, Is.True);

                float step = MixDesk.BadgerCutRampSeconds * 0.5f;
                float g = MixDesk.CutGain(step);
                float mid = desk.TickBadgerForTests(step);
                Assert.That(g, Is.GreaterThan(0f));
                Assert.That(mid, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(desk.BadgerOnForTests, Is.True);
                Assert.That(loop.isPlaying && cue.isPlaying && sting.isPlaying, Is.True);
                Assert.That(loop.volume, Is.EqualTo(full * g).Within(0.0001f));
                Assert.That(cue.volume, Is.EqualTo(full * g).Within(0.0001f));
                Assert.That(sting.volume, Is.EqualTo(full * g).Within(0.0001f));

                desk.TickBadgerForTests(MixDesk.BadgerCutRampSeconds);
                Assert.That(desk.BadgerOnForTests, Is.True);
                Assert.That(loop.isPlaying, Is.False);
                Assert.That(cue.isPlaying, Is.False);
                Assert.That(sting.isPlaying, Is.False);
                Assert.That(loop.volume, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(cue.volume, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(sting.volume, Is.EqualTo(0f).Within(0.0001f));

                // Sharp hit stays on the cut frame, immediately after the cut returns.
                string app = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
                int at = app.IndexOf("MixDesk.Live.BadgerCut()", StringComparison.Ordinal);
                int hit = at < 0 ? -1 : app.IndexOf("SfxLibrary.SharpHit()", at, StringComparison.Ordinal);
                Assert.That(at, Is.GreaterThan(0));
                Assert.That(hit, Is.GreaterThan(at));
                Assert.That(hit - at, Is.LessThan(120));
            }
            finally
            {
                MixDesk.DetachForTests(desk);
                UnityEngine.Object.DestroyImmediate(go);
                if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
                AudioListener.pause = paused;
                AudioListener.volume = listen;
            }
        }

        [Test]
        public void OneTwistPerAppearanceAndItStaysThisMinigame()
        {
            Assert.That(Enum.GetValues(typeof(BadgerTwist)).Length, Is.EqualTo(3));
            Assert.That(BadgerSchedule.Appearance(15), Is.EqualTo(1));
            Assert.That(BadgerSchedule.Appearance(20), Is.EqualTo(2));
            Assert.That(BadgerSchedule.Appearance(25), Is.EqualTo(3));
            Assert.That(BadgerPolish.TwistOf(1), Is.EqualTo(BadgerTwist.StrongerTiles));
            Assert.That(BadgerPolish.TwistOf(2), Is.EqualTo(BadgerTwist.NewTaunt));
            Assert.That(BadgerPolish.TwistOf(3), Is.EqualTo(BadgerTwist.FasterRiff));
            Assert.That(BadgerPolish.TwistOf(4), Is.EqualTo(BadgerTwist.StrongerTiles));

            var mix = BadgerSchedule.BossTileMix(1, 9);
            var hotter = BadgerPolish.BossTiles(1, 9);
            Assert.That(Sum(hotter), Is.EqualTo(Sum(mix) + 1));
            Assert.That(OnlyHoney(hotter), Is.True);
            int changed = 0;
            for (int i = 0; i < mix.Length; i++)
                if (mix[i] != hotter[i]) changed++;
            Assert.That(changed, Is.EqualTo(1));

            var same = BadgerSchedule.BossTileMix(2, 9);
            var kept = BadgerPolish.BossTiles(2, 9);
            Assert.That(kept, Is.EqualTo(same));
            Assert.That(BadgerPolish.BossTiles(3, 9), Is.EqualTo(BadgerSchedule.BossTileMix(3, 9)));

            var owned = (int[])mix.Clone();
            var you = Grid(2);
            var explicitFight = new BadgerFight(1, 9, you, owned);
            for (int i = 0; i < owned.Length; i++)
            {
                Assert.That(owned[i], Is.EqualTo(mix[i]));
                Assert.That(explicitFight.BossHoney(i), Is.EqualTo(mix[i]));
            }

            var visit = BadgerFight.Start(15, 9);
            Assert.That(SumBoss(visit), Is.EqualTo(Sum(BadgerSchedule.BossTileMix(1, 9)) + 1));
            var later = BadgerFight.Start(20, 9);
            Assert.That(SumBoss(later), Is.EqualTo(Sum(BadgerSchedule.BossTileMix(2, 9))));

            var fives = Grid(5);
            var before = (int[])fives.Clone();
            BadgerPolish.Strengthen(fives);
            Assert.That(fives, Is.EqualTo(before));
            var threes = Grid(3);
            BadgerPolish.Strengthen(threes);
            Assert.That(threes[0], Is.EqualTo(5));
            Assert.That(threes[1], Is.EqualTo(3));
        }

        [Test]
        public void DesperateSlamAddsToTheRollAndResetsToBase()
        {
            var f = new BadgerFight(1, 1, Grid(3), Grid(2), false);
            Assert.That(f.PlayerTarget, Is.EqualTo(10));
            Play(f, _ => 0, out var first);
            Assert.That(first.Eligible, Is.False);
            Assert.That(first.Hit, Is.False);
            Assert.That(f.DontCareChance, Is.EqualTo(BadgerFight.DontCareBaseChance));
            Assert.That(f.PlayerScore, Is.EqualTo(1));

            int[] afterMiss = { 15, 20, 25, 30, 35, 40 };
            for (int i = 0; i < afterMiss.Length; i++)
            {
                int stored = f.DontCareChance;
                Assert.That(BadgerPolish.Mood(f.PlayerScore, f.PlayerTarget, false), Is.Not.EqualTo(BadgerMood.Desperate));
                Play(f, _ => stored, out var miss);
                Assert.That(miss.Eligible, Is.True);
                Assert.That(miss.Hit, Is.False);
                Assert.That(miss.ChanceBefore, Is.EqualTo(stored));
                Assert.That(f.DontCareChance, Is.EqualTo(afterMiss[i]));
            }
            Assert.That(f.PlayerScore, Is.EqualTo(7));
            Assert.That(BadgerPolish.Mood(f.PlayerScore, f.PlayerTarget, false), Is.EqualTo(BadgerMood.Desperate));

            int faceStored = f.DontCareChance;
            Play(f, _ => faceStored, out var hit);
            Assert.That(hit.Hit, Is.True);
            Assert.That(hit.ChanceBefore, Is.EqualTo(faceStored + BadgerPolish.DesperateChanceAdd));
            Assert.That(hit.ChanceAfter, Is.EqualTo(BadgerFight.DontCareBaseChance));
            Assert.That(f.DontCareChance, Is.EqualTo(BadgerFight.DontCareBaseChance));
        }

        [Test]
        public void PowerStepRoundNeverSlams()
        {
            var f = new BadgerFight(2, 4, Grid(3), Grid(2), false);
            Play(f, _ => 99, out _);
            int stored = f.DontCareChance;
            Assert.That(f.BossPickFor(BadgerPower.X3), Is.GreaterThanOrEqualTo(0));
            int tile = f.PowerStepPlayerTile(BadgerPower.X3);
            Assert.That(f.PlayerPick(tile), Is.True);
            bool slammed = f.TryDontCare(_ => 0, out var slam);
            Assert.That(slammed, Is.False);
            Assert.That(slam.Hit, Is.False);
            Assert.That(f.DontCareChance, Is.EqualTo(stored));
            Assert.That(f.StepPower, Is.EqualTo(BadgerPower.X3));
        }

        [Test]
        public void ShortTauntsAndGiveUpUseTheSharedLines()
        {
            Assert.That(BadgerCopy.GiveUp, Is.EqualTo("I give!"));
            Assert.That(BadgerCopy.Spoken(BadgerCopy.GoesFirst, BadgerMood.Smug), Is.EqualTo(BadgerCopy.GoesFirst));
            string shortFirst = BadgerCopy.Spoken(BadgerCopy.GoesFirst, BadgerMood.Rattled);
            Assert.That(shortFirst.Length, Is.LessThan(BadgerCopy.GoesFirst.Length));
            Assert.That(BadgerCopy.Spoken(BadgerCopy.GoesFirst, BadgerMood.Desperate), Is.EqualTo(shortFirst));
            Assert.That(BadgerCopy.Spoken(BadgerCopy.GiveUp, BadgerMood.Surrender), Is.EqualTo(BadgerCopy.GiveUp));
            Assert.That(BadgerCopy.OpeningForVisit(1, true), Is.EqualTo(""));
            Assert.That(BadgerCopy.OpeningForVisit(2, false), Is.EqualTo(BadgerCopy.TwistTaunt));
            Assert.That(BadgerCopy.OpeningForVisit(3, false), Is.EqualTo(BadgerCopy.OpenLater));
            Assert.That(BadgerCopy.RoundLine(0, BadgerMood.Smug, false), Is.EqualTo(BadgerCopy.GoesFirst));
            Assert.That(BadgerCopy.RoundLine(3, BadgerMood.Smug, true), Is.EqualTo(BadgerCopy.TwistTaunt));
            Assert.That(BadgerCopy.RoundLine(3, BadgerMood.Rattled, true), Is.EqualTo(BadgerCopy.ShortTaunt(BadgerCopy.TwistTaunt)));
            Assert.That(BadgerCopy.OpeningLine(2, false), Is.EqualTo(BadgerCopy.OpenLater));
            Assert.That(BadgerCopy.RoundStartLine(0), Is.EqualTo(BadgerCopy.GoesFirst));
        }

        [Test]
        public void NewLinesFitTheSharedCaption()
        {
            var sizes = new[]
            {
                new Vector2(402f, 874f),
                new Vector2(820f, 1180f),
                new Vector2(412f, 915f),
            };
            string[] lines =
            {
                BadgerCopy.GiveUp,
                BadgerCopy.TwistTaunt,
                BadgerCopy.ShortTaunt(BadgerCopy.GoesFirst),
                BadgerCopy.ShortTaunt(BadgerCopy.HowMany),
                BadgerCopy.ShortTaunt(BadgerCopy.DontCare),
                BadgerCopy.ShortTaunt(BadgerCopy.TwistTaunt),
            };
            for (int i = 0; i < sizes.Length; i++)
            {
                var safe = new Rect(0f, 0f, sizes[i].x, sizes[i].y);
                for (int k = 0; k < lines.Length; k++)
                {
                    bool ok = FlockFiveApp.BadgerCaptionFits(lines[k], sizes[i].x, sizes[i].y, safe, out string detail);
                    Assert.That(ok, Is.True, detail + " [" + lines[k] + "]");
                }
            }
        }

        [Test]
        public void SharpHitIsAShortWoodThock()
        {
            var clip = SfxLibrary.SharpHitClip();
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.length, Is.GreaterThan(0.05f).And.LessThan(0.20f));
            Assert.That(clip.name, Is.EqualTo("sharp-hit"));
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            Assert.That(data.Length, Is.GreaterThan(8));
            float peak = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float a = Mathf.Abs(data[i]);
                if (a > peak) peak = a;
            }
            Assert.That(peak, Is.GreaterThan(0.2f));
            // 4 ms linear tail. The last sample sits on zero.
            int tail = Mathf.RoundToInt(0.004f * clip.frequency);
            Assert.That(tail / (float)clip.frequency, Is.GreaterThanOrEqualTo(0.003f).And.LessThanOrEqualTo(0.005f));
            Assert.That(Mathf.Abs(data[data.Length - 1]), Is.LessThan(0.0001f));
            Assert.That(Mathf.Abs(data[data.Length - 1]), Is.LessThan(peak * 0.01f));
        }

        [Test]
        public void WearPlacementIsCenteredOnOneScaleWithMargin()
        {
            var wears = new[] { BadgerWear.Fresh, BadgerWear.Scuffed, BadgerWear.Bruised, BadgerWear.Wrecked };
            for (int i = 0; i < wears.Length; i++)
            {
                BadgerPolish.WearScale(wears[i], out float sx, out float sy);
                Assert.That(sx, Is.EqualTo(1f).Within(0.0001f), wears[i].ToString());
                Assert.That(sy, Is.EqualTo(1f).Within(0.0001f), wears[i].ToString());
            }
            Assert.That(BadgerWearArt.FeatureScale("badger_crouch"), Is.EqualTo(BadgerWearArt.CrouchFeature).Within(0.0001f));
            Assert.That(BadgerWearArt.FeatureScale("badger_idle_b"), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BadgerWearArt.SideMarginPx, Is.GreaterThanOrEqualTo(20f));

            var sizes = new[]
            {
                new Vector2(402f, 874f),
                new Vector2(820f, 1180f),
                new Vector2(412f, 915f),
            };
            for (int s = 0; s < sizes.Length; s++)
            {
                float w = sizes[s].x;
                float h = sizes[s].y;
                float device = BadgerWearArt.DeviceScale(w, h);
                var stand = BadgerWearArt.Layout("badger_idle_b", w, h);
                var crouch = BadgerWearArt.Layout("badger_crouch", w, h);
                Assert.That(stand.Ok, Is.True);
                Assert.That(crouch.Ok, Is.True);
                Assert.That(stand.DeviceScale, Is.EqualTo(device).Within(0.0001f));
                Assert.That(crouch.DeviceScale, Is.EqualTo(device).Within(0.0001f), "one scale at " + w);
                Assert.That(stand.Scale, Is.EqualTo(device).Within(0.0001f));
                Assert.That(crouch.Scale * BadgerWearArt.CrouchFeature, Is.EqualTo(device).Within(0.001f));
                Assert.That(stand.BodyCenterX, Is.EqualTo(w * 0.5f).Within(1.5f), "idle centre " + w);
                Assert.That(crouch.BodyCenterX, Is.EqualTo(w * 0.5f).Within(1.5f), "crouch centre " + w);
                Assert.That(crouch.FeetY, Is.EqualTo(stand.FeetY).Within(1.5f), "feet " + w);

                Rect placed = default;
                float bodyW = -1f;
                for (int i = 0; i < wears.Length; i++)
                {
                    var blit = BadgerWearArt.PaintWear("badger_idle_b", stand.Unpadded, stand.SrcW, stand.SrcH, wears[i]);
                    Assert.That(blit.Sprite, Is.EqualTo("badger_idle_b_wear_" + BadgerWearArt.Token(wears[i])));
                    if (i == 0)
                    {
                        placed = blit.Dest;
                        bodyW = stand.BodyWidth;
                    }
                    else
                    {
                        Assert.That(blit.Dest.x, Is.EqualTo(placed.x).Within(0.01f), wears[i].ToString());
                        Assert.That(blit.Dest.y, Is.EqualTo(placed.y).Within(0.01f), wears[i].ToString());
                        Assert.That(blit.Dest.width, Is.EqualTo(placed.width).Within(0.01f), wears[i].ToString());
                        Assert.That(blit.Dest.height, Is.EqualTo(placed.height).Within(0.01f), wears[i].ToString());
                        Assert.That(stand.BodyWidth, Is.EqualTo(bodyW).Within(0.01f));
                        Assert.That(stand.BodyCenterX, Is.EqualTo(BadgerWearArt.Layout("badger_idle_b", w, h).BodyCenterX).Within(0.01f));
                        Assert.That(stand.FeetY, Is.EqualTo(BadgerWearArt.Layout("badger_idle_b", w, h).FeetY).Within(0.01f));
                    }
                    var again = BadgerWearArt.Layout("badger_crouch", w, h);
                    Assert.That(again.Scale, Is.EqualTo(crouch.Scale).Within(0.0001f), "crouch scale " + wears[i]);
                    Assert.That(again.BodyCenterX, Is.EqualTo(crouch.BodyCenterX).Within(0.01f));
                    Assert.That(again.FeetY, Is.EqualTo(crouch.FeetY).Within(0.01f));
                }

                if (Mathf.Abs(w - 402f) > 0.5f) continue;
                Assert.That(stand.ContentLeft, Is.GreaterThanOrEqualTo(18f), "idle left");
                Assert.That(w - stand.ContentRight, Is.GreaterThanOrEqualTo(18f), "idle right " + stand.ContentRight);
                Assert.That(crouch.ContentLeft, Is.GreaterThanOrEqualTo(18f), "crouch left");
                Assert.That(w - crouch.ContentRight, Is.GreaterThanOrEqualTo(18f), "crouch right " + crouch.ContentRight);
            }

            string duel = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            string stills = File.ReadAllText("Assets/_Project/Scripts/Editor/BadgerPolishStills.cs");
            Assert.That(duel.Contains("BadgerWearArt.FeatureScale"), Is.True);
            Assert.That(duel.Contains("BadgerWearArt.PaintWear"), Is.True);
            Assert.That(stills.Contains("BadgerWearArt.Layout"), Is.True);
            Assert.That(stills.Contains("BadgerWearArt.PaintWear"), Is.True);
            Assert.That(stills.Contains("WearScale"), Is.False);
            Assert.That(stills.Contains("0.56f"), Is.False);
            string art = File.ReadAllText("Assets/_Project/Scripts/App/BadgerWearArt.cs");
            Assert.That(art.Contains("ScreenMidX(screenW)"), Is.True, "stills Layout and the duel share ScreenMidX");
            Assert.That(duel.Contains("DuelRestPlant"), Is.True);
        }

        [Test]
        public void DuelBodyCentreIsScreenMidAndFootStaysOnBossBox()
        {
            Assert.That(BadgerWearArt.ScreenMidX(402f), Is.EqualTo(201f).Within(0.001f));
            Assert.That(BadgerWearArt.ScreenMidX(820f), Is.EqualTo(410f).Within(0.001f));
            Assert.That(BadgerWearArt.ScreenMidX(412f), Is.EqualTo(206f).Within(0.001f));

            var idleSpr = SpriteCatalog.BadgerArt("badger_idle_b");
            float idleW = idleSpr != null ? idleSpr.rect.width : 438f;
            float idleH = idleSpr != null ? idleSpr.rect.height : 640f;
            var sizes = new[]
            {
                new Vector2(402f, 874f),
                new Vector2(820f, 1180f),
                new Vector2(412f, 915f),
            };
            var frames = new[] { "badger_idle_a", "badger_idle_b", "badger_crouch" };
            float? keep = DeviceFit.TestWindow;
            try
            {
                for (int s = 0; s < sizes.Length; s++)
                {
                    float w = sizes[s].x;
                    float h = sizes[s].y;
                    DeviceFit.TestWindow = w / h;
                    float ui = DeviceFit.Ui(w, h);
                    var safe = new Rect(0f, 0f, w, h);
                    float top = FlockFiveApp.TopHudBox(h, safe, 0f);
                    float medal = 101.2f * Mathf.Clamp(ui, 1f, 1.35f) * DeviceFit.ControlK();
                    float ptPx = Mathf.Max(1f, ui);
                    medal = Mathf.Min(medal, 56f * ptPx);
                    medal = Mathf.Max(medal, 44f);
                    var back = new Rect(Mathf.Max(16f, safe.xMin + 10f), top, medal, medal);
                    var L = FlockFiveApp.BadgerLayoutFor(w, h, safe, ui, top, back);
                    var box = BadgerArenaRects.Split(L.Arena);
                    var mark = BadgerFighterPlace.Rest(box, idleW, idleH, L.ColR.x - 2f);
                    Assert.That(mark.Foot.y, Is.EqualTo(box.BossFighter.yMax).Within(0.01f), "boss foot " + w);

                    var stand = BadgerWearArt.Layout("badger_idle_b", w, h);
                    Assert.That(stand.BodyCenterX, Is.EqualTo(BadgerWearArt.ScreenMidX(w)).Within(0.51f), "still aim " + w);

                    for (int f = 0; f < frames.Length; f++)
                    {
                        string frame = frames[f];
                        var spr = SpriteCatalog.BadgerArt(frame);
                        float artW = spr != null ? spr.rect.width : (frame == "badger_crouch" ? 803f : idleW);
                        float sx = 1f / BadgerWearArt.FeatureScale(frame);
                        FlockFiveApp.DuelRestPlant(frame, w, mark.Unit, sx, artW, mark.Foot.y, L.ColR.x - 2f,
                            out float footX, out float footY, out float bodyX);
                        Assert.That(bodyX, Is.EqualTo(w * 0.5f).Within(0.05f), frame + " centre " + w + " got " + bodyX);
                        Assert.That(footY, Is.EqualTo(mark.Foot.y).Within(0.001f), frame + " foot y " + w);
                        Assert.That(footY, Is.EqualTo(box.BossFighter.yMax).Within(0.01f), frame + " boss foot " + w);
                        float frac = BadgerWearArt.BodyFrac(frame);
                        float cx = SpriteAnim.FootCx(frame);
                        float drawnW = artW * mark.Unit * sx;
                        Assert.That(BadgerWearArt.BodyCenterAt(footX, frac, cx, drawnW), Is.EqualTo(bodyX).Within(0.001f));
                    }
                }
            }
            finally
            {
                DeviceFit.TestWindow = keep;
            }
        }

        static int[] Grid(int honey)
        {
            var tiles = new int[BadgerSchedule.Tiles];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = honey;
            return tiles;
        }

        static int Sum(int[] tiles)
        {
            int n = 0;
            for (int i = 0; i < tiles.Length; i++) n += tiles[i];
            return n;
        }

        static int SumBoss(BadgerFight fight)
        {
            int n = 0;
            for (int i = 0; i < fight.BossCount; i++) n += fight.BossHoney(i);
            return n;
        }

        static bool OnlyHoney(int[] tiles)
        {
            for (int i = 0; i < tiles.Length; i++)
                if (tiles[i] != 2 && tiles[i] != 3 && tiles[i] != 5) return false;
            return true;
        }

        static void Play(BadgerFight fight, Func<int, int> roll, out BadgerDontCare slam)
        {
            Assert.That(fight.BossPick(), Is.GreaterThanOrEqualTo(0));
            int tile = -1;
            for (int i = 0; i < fight.PlayerCount; i++)
            {
                if (!fight.PlayerOpen(i)) continue;
                tile = i;
                break;
            }
            Assert.That(fight.PlayerPick(tile), Is.True);
            fight.TryDontCare(roll, out slam);
            Assert.That(fight.Resolve(out _), Is.True);
        }
    }
}
#endif

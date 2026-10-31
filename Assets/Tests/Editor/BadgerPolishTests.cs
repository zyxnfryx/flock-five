#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
                Assert.That(blit.HaloBack, Is.EqualTo(BadgerWearArt.HaloBack(pose, BadgerWear.Scuffed)));
                Assert.That(blit.HaloFront, Is.EqualTo(BadgerWearArt.HaloFront(pose, BadgerWear.Scuffed)));
                Assert.That(blit.Dest.x, Is.EqualTo(padded.x).Within(0.01f), pose);
                Assert.That(blit.Dest.y, Is.EqualTo(padded.y).Within(0.01f), pose);
                Assert.That(blit.Dest.width, Is.EqualTo(padded.width).Within(0.01f), pose);
                Assert.That(blit.Dest.height, Is.EqualTo(padded.height).Within(0.01f), pose);
                Assert.That(blit.Layered, Is.True);
                Assert.That(blit.HaloAlpha, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(blit.BodyTint.r, Is.EqualTo(BadgerWearArt.BodyTint(BadgerWear.Scuffed).r).Within(0.0001f));
            }
        }

        [Test]
        public void WearDrawOrderIsBodyBetweenHaloLayers()
        {
            Assert.That(BadgerWearArt.UseStagePngs, Is.False);
            Assert.That(BadgerWearArt.CompositeOrder, Is.EqualTo(new[] { "halo_back", "body", "halo_front" }));
            var layers = new System.Collections.Generic.List<string>();
            var wears = new[] { BadgerWear.Fresh, BadgerWear.Scuffed, BadgerWear.Bruised, BadgerWear.Wrecked };
            for (int p = 0; p < BadgerWearArt.Poses.Length; p++)
            {
                for (int w = 0; w < wears.Length; w++)
                {
                    string pose = BadgerWearArt.Poses[p];
                    BadgerWearArt.FillDrawList(pose, wears[w], layers);
                    Assert.That(BadgerWearArt.DrawListIsSafe(layers), Is.True, pose + " " + wears[w]);
                    Assert.That(layers.Count, Is.EqualTo(3));
                    Assert.That(layers[0], Is.EqualTo(BadgerWearArt.HaloBack(pose, wears[w])));
                    Assert.That(layers[1], Is.EqualTo(BadgerWearArt.StageKey(pose, wears[w])));
                    Assert.That(layers[2], Is.EqualTo(BadgerWearArt.HaloFront(pose, wears[w])));
                }
            }
            layers.Clear();
            layers.Add("halo_front_idle_a_scuffed");
            layers.Add("badger_idle_a_wear_scuffed");
            layers.Add("halo_back_idle_a_scuffed");
            Assert.That(BadgerWearArt.DrawListIsSafe(layers), Is.False, "reversed halo would paint the back on top");
            layers.Clear();
            layers.Add("halo_back_idle_a_scuffed");
            layers.Add("badger_idle_a_wear_scuffed");
            layers.Add("badger_idle_a_wear_scuffed");
            Assert.That(BadgerWearArt.DrawListIsSafe(layers), Is.False, "two bodies is a double draw");
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
            int page = duel.IndexOf("void DrawBadgerPage()", StringComparison.Ordinal);
            int bodyCall = duel.IndexOf("DrawBadgerPageBody();", page, StringComparison.Ordinal);
            int flashCall = duel.IndexOf("DrawBadgerBlowFlash();", bodyCall, StringComparison.Ordinal);
            int tokenCall = duel.IndexOf("DrawBadgerPickToken();", flashCall, StringComparison.Ordinal);
            Assert.That(bodyCall, Is.GreaterThan(page));
            Assert.That(flashCall, Is.GreaterThan(bodyCall), "flash paints after the badger page");
            Assert.That(tokenCall, Is.GreaterThan(flashCall), "pick token paints after the flash");
            int bodyFn = duel.IndexOf("void DrawBadgerPageBody()", StringComparison.Ordinal);
            int bodyEnd = duel.IndexOf("void DrawBadgerGain", bodyFn, StringComparison.Ordinal);
            string bodySrc = duel.Substring(bodyFn, bodyEnd - bodyFn);
            Assert.That(bodySrc.Contains("DrawBadgerFighters(L);"), Is.True, "badger paints in the page body");
            Assert.That(bodySrc.Contains("DrawBadgerPickToken();"), Is.False, "token is not under the flash");
        }

        [Test]
        public void WearTintHaloPunchAndLeanEase()
        {
            Assert.That(BadgerWearArt.TintAmount(BadgerWear.Fresh), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BadgerWearArt.TintAmount(BadgerWear.Scuffed), Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(BadgerWearArt.TintAmount(BadgerWear.Bruised), Is.EqualTo(0.24f).Within(0.0001f));
            Assert.That(BadgerWearArt.TintAmount(BadgerWear.Wrecked), Is.EqualTo(0.36f).Within(0.0001f));

            var fresh = BadgerWearArt.BodyTint(BadgerWear.Fresh);
            Assert.That(fresh.r, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(fresh.g, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(fresh.b, Is.EqualTo(1f).Within(0.0001f));

            var toward = BadgerWearArt.TintToward;
            Assert.That(toward.r * 255f, Is.EqualTo(14f).Within(0.01f));
            Assert.That(toward.g * 255f, Is.EqualTo(10f).Within(0.01f));
            Assert.That(toward.b * 255f, Is.EqualTo(8f).Within(0.01f));
            var wears = new[] { BadgerWear.Scuffed, BadgerWear.Bruised, BadgerWear.Wrecked };
            var amounts = new[] { 0.12f, 0.24f, 0.36f };
            for (int i = 0; i < wears.Length; i++)
            {
                var tint = BadgerWearArt.BodyTint(wears[i]);
                var expect = Color.Lerp(Color.white, toward, amounts[i]);
                Assert.That(tint.r, Is.EqualTo(expect.r).Within(0.0001f), wears[i].ToString());
                Assert.That(tint.g, Is.EqualTo(expect.g).Within(0.0001f), wears[i].ToString());
                Assert.That(tint.b, Is.EqualTo(expect.b).Within(0.0001f), wears[i].ToString());
                Assert.That(tint.r - tint.g, Is.LessThan(0.05f), "tint stays brown");
                Assert.That(tint.g, Is.GreaterThan(tint.b));
            }

            Assert.That(BadgerWearArt.HaloAlpha(BadgerWear.Fresh), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BadgerWearArt.HaloAlpha(BadgerWear.Scuffed), Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(BadgerWearArt.HaloAlpha(BadgerWear.Bruised), Is.EqualTo(0.85f).Within(0.0001f));
            Assert.That(BadgerWearArt.HaloAlpha(BadgerWear.Wrecked), Is.EqualTo(1f).Within(0.0001f));

            Assert.That(BadgerWearArt.PunchSeconds, Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(BadgerWearArt.PunchPeak, Is.EqualTo(1.08f).Within(0.0001f));
            Assert.That(BadgerWearArt.PunchScale(0f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BadgerWearArt.PunchScale(0.03f), Is.EqualTo(1.04f).Within(0.0001f));
            Assert.That(BadgerWearArt.PunchScale(0.06f), Is.EqualTo(1.08f).Within(0.0001f));
            Assert.That(BadgerWearArt.PunchScale(0.09f), Is.EqualTo(1.04f).Within(0.0001f));
            Assert.That(BadgerWearArt.PunchScale(0.12f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BadgerWearArt.PunchScale(0.2f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BadgerWearArt.PunchScale(-0.1f), Is.EqualTo(1f).Within(0.0001f));

            var planted = new Rect(80f, 40f, 40f, 60f);
            float pivotX = planted.center.x;
            float footY = planted.yMax;
            var grown = BadgerWearArt.ScaleAbout(planted, pivotX, footY, BadgerWearArt.PunchPeak);
            Assert.That(grown.center.x, Is.EqualTo(pivotX).Within(0.001f));
            Assert.That(grown.yMax, Is.EqualTo(footY).Within(0.001f));
            Assert.That(grown.width, Is.EqualTo(planted.width * 1.08f).Within(0.001f));
            var rest = BadgerWearArt.ScaleAbout(planted, pivotX, footY, 1f);
            Assert.That(rest.x, Is.EqualTo(planted.x).Within(0.001f));
            Assert.That(rest.yMax, Is.EqualTo(footY).Within(0.001f));

            Assert.That(BadgerPolish.LeanEaseSeconds, Is.EqualTo(0.18f).Within(0.0001f));
            Assert.That(BadgerPolish.EaseOut01(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BadgerPolish.EaseOut01(1f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BadgerPolish.EaseOut01(0.5f), Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(BadgerPolish.EaseLean(0f, 6f, 0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BadgerPolish.EaseLean(0f, 6f, 0.09f), Is.EqualTo(4.5f).Within(0.0001f));
            Assert.That(BadgerPolish.EaseLean(0f, 6f, 0.18f), Is.EqualTo(6f).Within(0.0001f));
            Assert.That(BadgerPolish.EaseLean(6f, 11f, 0.18f), Is.EqualTo(11f).Within(0.0001f));
            Assert.That(BadgerPolish.EaseLean(2f, 11f, 0.3f), Is.EqualTo(11f).Within(0.0001f));

            const string art = "Assets/_Project/Art/Resources/Sprites/Badger";
            int missing = 0;
            for (int p = 0; p < BadgerWearArt.Poses.Length; p++)
            {
                for (int w = 0; w < wears.Length; w++)
                {
                    string back = art + "/" + BadgerWearArt.HaloBack(BadgerWearArt.Poses[p], wears[w]) + ".png";
                    string front = art + "/" + BadgerWearArt.HaloFront(BadgerWearArt.Poses[p], wears[w]) + ".png";
                    if (!File.Exists(back) || !File.Exists(front)) missing++;
                }
            }
            Assert.That(missing, Is.EqualTo(0), "halo layers for scuffed, bruised, wrecked");

            string duel = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            Assert.That(duel.Contains("BadgerWearArt.BodyTint"), Is.False);
            Assert.That(duel.Contains("blit.BodyTint"), Is.True);
            Assert.That(duel.Contains("blit.HaloAlpha"), Is.True);
            Assert.That(duel.Contains("BadgerWearArt.PunchPivot"), Is.True);
            Assert.That(duel.Contains("MoodLean("), Is.True);
            Assert.That(duel.Contains("BadgerPolish.EaseLean"), Is.True);
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

        [Test]
        public void WearFlashIsOneFrameOfTheBlowFlash()
        {
            float full = BadgerPolish.BlowFlash(0f);
            Assert.That(full, Is.EqualTo(BossBeat.FlashAlpha(0f)).Within(0.0001f));
            Assert.That(full, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BadgerPolish.WearFlashScale, Is.EqualTo(0.60f).Within(0.0001f));
            Assert.That(BadgerPolish.WearFlashAlpha(7, 7), Is.EqualTo(full * 0.60f).Within(0.0001f));
            Assert.That(BadgerPolish.WearFlashAlpha(8, 7), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BadgerPolish.WearFlashAlpha(7, -1), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BadgerPolish.WearFlashAlpha(0, 0), Is.EqualTo(0.60f).Within(0.0001f));

            string polish = File.ReadAllText("Assets/_Project/Scripts/App/BadgerPolish.cs");
            int at = polish.IndexOf("float WearFlashAlpha", StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThan(0));
            int blow = polish.IndexOf("BlowFlash(0f)", at, StringComparison.Ordinal);
            Assert.That(blow, Is.GreaterThan(at));

            string duel = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            Assert.That(duel.Contains("BadgerPolish.WearFlashAlpha(Time.frameCount, _bgWearFlashFrame)"), Is.True);
            Assert.That(duel.Contains("new Color(1f, 1f, 1f, a)"), Is.True);
            Assert.That(duel.Contains("new Color(1f, 1f, 1f, blit.HaloAlpha * alpha)"), Is.True);
            string stills = File.ReadAllText("Assets/_Project/Scripts/Editor/BadgerPolishStills.cs");
            Assert.That(stills.Contains("new Color(1f, 1f, 1f, blit.HaloAlpha)"), Is.True);
            Assert.That(BadgerPolish.YellowStreak(1f, 1f, 1f), Is.False, "white flash and white halo are not a yellow streak");
        }

        [Test]
        public void RestPoseMatchesAcrossScreenSizes()
        {
            var sizes = new[]
            {
                new Vector2(402f, 874f),
                new Vector2(820f, 1180f),
                new Vector2(412f, 915f),
            };
            var wears = new[] { BadgerWear.Fresh, BadgerWear.Scuffed, BadgerWear.Bruised, BadgerWear.Wrecked };
            float[] times = { 0.1f, 0.7f, 1.3f, 1.9f };
            for (int t = 0; t < times.Length; t++)
            {
                for (int w = 0; w < wears.Length; w++)
                {
                    string a = BadgerWearArt.RestPose(times[t], wears[w], sizes[0].x, sizes[0].y);
                    for (int s = 1; s < sizes.Length; s++)
                    {
                        string b = BadgerWearArt.RestPose(times[t], wears[w], sizes[s].x, sizes[s].y);
                        Assert.That(b, Is.EqualTo(a), wears[w] + " t=" + times[t] + " size " + sizes[s].x);
                    }
                    if (wears[w] == BadgerWear.Wrecked)
                        Assert.That(a, Is.EqualTo("badger_crouch"));
                    else if (Mathf.Abs(times[t] - 0.7f) < 0.001f || Mathf.Abs(times[t] - 1.9f) < 0.001f)
                        Assert.That(a, Is.EqualTo("badger_idle_b"), "t=" + times[t]);
                    else
                        Assert.That(a, Is.EqualTo("badger_idle_a"), "t=" + times[t]);
                }
            }

            Assert.That(BadgerPoseClock.Hitch, Is.LessThan(0.5f));
            Assert.That(BadgerPoseClock.Hitch, Is.EqualTo(0.10f).Within(0.0001f));
            Assert.That(BadgerPoseClock.StepOf(0.40f), Is.EqualTo(BadgerPoseClock.Hitch).Within(0.000001f));
            Assert.That(BadgerPoseClock.StepOf(0.80f), Is.EqualTo(BadgerPoseClock.StepOf(0.40f)).Within(0.000001f));
            Assert.That(BadgerPoseClock.StepOf(0.016f), Is.EqualTo(0.016f).Within(0.000001f));
            Assert.That(BadgerPoseClock.StepOf(0.05f), Is.EqualTo(0.05f).Within(0.000001f));
            Assert.That(BadgerPoseClock.StepOf(-0.2f), Is.EqualTo(0f).Within(0.000001f));

            string duel = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            Assert.That(duel.Contains("BadgerPoseClock.Now"), Is.True);
            Assert.That(duel.Contains("BadgerPoseClock.Reset(0.75f)"), Is.True);
            Assert.That(duel.Contains("BadgerWearArt.RestPose"), Is.True);
            Assert.That(duel.Contains("TickBadger(AdPause.UnscaledDelta)"), Is.True);
            Assert.That(duel.Contains("_bgTickFrame != Time.frameCount"), Is.True);
            Assert.That(duel.Contains("BadgerWearArt.BodyTint"), Is.False);
            string art = File.ReadAllText("Assets/_Project/Scripts/App/BadgerWearArt.cs");
            int rest = art.IndexOf("string RestPose(", StringComparison.Ordinal);
            int next = art.IndexOf("public static float PunchScale", rest, StringComparison.Ordinal);
            Assert.That(rest, Is.GreaterThan(0));
            Assert.That(next, Is.GreaterThan(rest));
            string body = art.Substring(rest, next - rest);
            Assert.That(body.Contains("Screen.width"), Is.False);
        }

        [Test]
        public void GreyBodyAndYellowStreakRules()
        {
            Assert.That(BadgerPolish.GreySatMax, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(BadgerPolish.GreyValueMin, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(BadgerPolish.GreyBody(0.5f, 0.5f, 0.5f), Is.True);
            Assert.That(BadgerPolish.GreyBody(0.2f, 0.2f, 0.2f), Is.False, "value at 0.20 is not above 0.25");
            Assert.That(BadgerPolish.GreyBody(1f, 0f, 0f), Is.False);
            Assert.That(BadgerPolish.GreyBody(0.25f, 0.25f, 0.25f), Is.False, "value must be above 0.25");
            Assert.That(BadgerPolish.YellowStreak(1f, 1f, 1f), Is.False);
            Assert.That(BadgerPolish.YellowStreak(1f, 0.92f, 0.05f), Is.True);
            Assert.That(BadgerPolish.YellowStreak(0.9f, 0.12f, 0.1f), Is.False, "red is not a honey mark");
            var honey = BadgerWearArt.HoneyInk;
            Assert.That(BadgerPolish.YellowStreak(honey.r / 255f, honey.g / 255f, honey.b / 255f), Is.True, "honey marks count as yellow");
            Assert.That(BadgerWearArt.HoneyMarkPixel(honey), Is.True);
            Assert.That(BadgerWearArt.HoneyMarkPixel(new Color32(250, 189, 46, 255)), Is.False, "token gold is not a honey streak");
            Assert.That(BadgerPolish.YellowHueMin * 360f, Is.EqualTo(45f).Within(0.01f));
            Assert.That(BadgerPolish.YellowHueMax * 360f, Is.EqualTo(70f).Within(0.01f));
        }

        [Test]
        public void HaloRingIsSolidThickAndBrighter()
        {
            Assert.That(BadgerWearArt.HaloRgbMul, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(BadgerWearArt.HaloMinScreenPx, Is.EqualTo(2));
            Assert.That(BadgerWearArt.HaloSolid, Is.True);
            Assert.That(BadgerWearArt.HaloStrokePx(0.5f), Is.GreaterThanOrEqualTo(BadgerWearArt.HaloMinScreenPx));
            Assert.That(BadgerWearArt.HaloStrokePx(0.08f), Is.GreaterThanOrEqualTo(36), "small duel fighter still gets a 2 px ring");
            var mid = BadgerWearArt.Brighten(new Color32(137, 119, 67, 255));
            Assert.That(mid.r, Is.EqualTo((byte)171));
            Assert.That(mid.g, Is.EqualTo((byte)149));
            Assert.That(mid.b, Is.EqualTo((byte)84));
            Assert.That(BadgerWearArt.Brighten(new Color32(240, 230, 220, 255)).r, Is.EqualTo((byte)255));

            var dash = new Color32[80 * 40];
            void Blob(int cx, int cy)
            {
                for (int y = cy - 2; y <= cy + 2; y++)
                    for (int x = cx - 4; x <= cx + 4; x++)
                        if ((uint)x < 80u && (uint)y < 40u)
                            dash[y * 80 + x] = new Color32(140, 120, 60, 255);
            }
            Blob(10, 20);
            Blob(28, 20);
            Blob(46, 20);
            Blob(64, 20);
            var joined = BadgerWearArt.SolidHalo(dash, 80, 40, 0.5f);
            var joinedM = BadgerWearArt.MeasureScreen(joined, 80, 40, 0.5f);
            Assert.That(joinedM.GapCount, Is.EqualTo(0), "gaps " + joinedM.GapCount + " comps " + joinedM.Components);
            Assert.That(joinedM.MinThickness, Is.GreaterThanOrEqualTo(BadgerWearArt.HaloMinScreenPx), "thick " + joinedM.MinThickness);
            bool brighter = false;
            for (int i = 0; i < joined.Length; i++)
                if (joined[i].a > 0 && joined[i].r > 150) brighter = true;
            Assert.That(brighter, Is.True, "stroke is brighter than the source dashes");

            Assert.That(BadgerWearArt.TryArt("halo_back_idle_b_scuffed", out var back, out int bw, out int bh), Is.True);
            Assert.That(BadgerWearArt.TryArt("halo_front_idle_b_scuffed", out var front, out int fw, out int fh), Is.True);
            var scales = new[] { 0.5263f, 0.8481f, 0.5408f };
            for (int s = 0; s < scales.Length; s++)
            {
                var sb = BadgerWearArt.SolidHalo(back, bw, bh, scales[s]);
                var sf = BadgerWearArt.SolidHalo(front, fw, fh, scales[s]);
                var mb = BadgerWearArt.MeasureScreen(sb, bw, bh, scales[s]);
                var mf = BadgerWearArt.MeasureScreen(sf, fw, fh, scales[s]);
                Assert.That(mb.GapCount, Is.EqualTo(0), "back gaps at " + scales[s] + " comps " + mb.Components);
                Assert.That(mf.GapCount, Is.EqualTo(0), "front gaps at " + scales[s] + " comps " + mf.Components);
                Assert.That(mb.MinThickness, Is.GreaterThanOrEqualTo(2), "back thick " + mb.MinThickness + " p10 " + mb.P10Thickness);
                Assert.That(mf.MinThickness, Is.GreaterThanOrEqualTo(2), "front thick " + mf.MinThickness + " p10 " + mf.P10Thickness);
            }

            float crouch = BadgerWearArt.DeviceScale(820f, 1180f) / BadgerWearArt.CrouchFeature;
            Assert.That(BadgerWearArt.TryArt("halo_back_crouch_scuffed", out var cb, out int cw, out int ch), Is.True);
            Assert.That(BadgerWearArt.TryArt("halo_front_crouch_scuffed", out var cf, out int cfw, out int cfh), Is.True);
            var crouchBack = BadgerWearArt.MeasureScreen(BadgerWearArt.SolidHalo(cb, cw, ch, crouch), cw, ch, crouch);
            var crouchFront = BadgerWearArt.MeasureScreen(BadgerWearArt.SolidHalo(cf, cfw, cfh, crouch), cfw, cfh, crouch);
            Assert.That(crouchBack.GapCount, Is.EqualTo(0), "crouch back comps " + crouchBack.Components);
            Assert.That(crouchFront.GapCount, Is.EqualTo(0), "crouch front comps " + crouchFront.Components);
            Assert.That(crouchBack.MinThickness, Is.GreaterThanOrEqualTo(2), "crouch back thick " + crouchBack.MinThickness + " p10 " + crouchBack.P10Thickness);
            Assert.That(crouchFront.MinThickness, Is.GreaterThanOrEqualTo(2), "crouch front thick " + crouchFront.MinThickness + " p10 " + crouchFront.P10Thickness);

            string duel = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            string stills = File.ReadAllText("Assets/_Project/Scripts/Editor/BadgerPolishStills.cs");
            Assert.That(duel.Contains("BadgerWearArt.SolidHaloTexture"), Is.True);
            Assert.That(duel.Contains("BadgerWearArt.HaloSolid"), Is.True);
            Assert.That(stills.Contains("BadgerWearArt.SolidHalo"), Is.True);
            Assert.That(duel.Contains("new Color(1f, 1f, 1f, blit.HaloAlpha * alpha)"), Is.True, "halo alpha stays a white multiply; brightness is baked");
        }

        [Test]
        public void BandageCrossOnBruisedAndWreckedOnly()
        {
            Assert.That(BadgerWearArt.BandageOn(BadgerWear.Fresh), Is.False);
            Assert.That(BadgerWearArt.BandageOn(BadgerWear.Scuffed), Is.False);
            Assert.That(BadgerWearArt.BandageOn(BadgerWear.Bruised), Is.True);
            Assert.That(BadgerWearArt.BandageOn(BadgerWear.Wrecked), Is.True);
            foreach (var c in new[] { BadgerWearArt.BandageGauze, BadgerWearArt.BandageEdge, BadgerWearArt.BandagePad })
            {
                Assert.That(c.r - c.b, Is.LessThan(80), "no red");
                Assert.That(BadgerWearArt.HoneyMarkPixel(c), Is.False, "bandage is not a honey streak");
            }
            foreach (var pose in new[] { "badger_idle_b", "badger_crouch" })
            {
                var uv = BadgerWearArt.HeadUv(pose);
                Assert.That(uv.y, Is.LessThan(0.35f), pose + " head near top");
                Assert.That(BadgerWearArt.FreshCovers(pose, uv.x, uv.y), Is.True, pose + " head on body");
                var layout = BadgerWearArt.Layout(pose, 820f, 1180f);
                int scuffed = BadgerWearArt.StampBandage(pose, layout.Unpadded, layout.BodyCenterX, layout.FeetY, 0f,
                    BadgerWear.Scuffed, (x, y, c) => { }, (x, y) => true);
                int bruised = BadgerWearArt.StampBandage(pose, layout.Unpadded, layout.BodyCenterX, layout.FeetY, 0f,
                    BadgerWear.Bruised, (x, y, c) => { }, (x, y) => true);
                Assert.That(scuffed, Is.EqualTo(0));
                Assert.That(bruised, Is.GreaterThan(40));
            }
        }

        [Test]
        public void Standing820MiddleStreakShiftsTwelveLeft()
        {
            Assert.That(BadgerWearArt.MidStreakShiftPx("badger_idle_b", 820f), Is.EqualTo(-12f));
            Assert.That(BadgerWearArt.MidStreakShiftPx("badger_idle_a", 820f), Is.EqualTo(-12f));
            Assert.That(BadgerWearArt.MidStreakShiftPx("badger_crouch", 820f), Is.EqualTo(0f));
            Assert.That(BadgerWearArt.MidStreakShiftPx("badger_idle_b", 402f), Is.EqualTo(0f));
            Assert.That(BadgerWearArt.MidStreakShiftPx("badger_idle_b", 412f), Is.EqualTo(0f));
            var layout = BadgerWearArt.Layout("badger_idle_b", 820f, 1180f);
            float a = MidX(layout, 0f), b = MidX(layout, -12f);
            Assert.That(b - a, Is.EqualTo(-12f).Within(1.01f));
        }

        static float MidX(BadgerWearArt.WearLayout layout, float shift)
        {
            // Mean x of the middle streak: pixels not in a stamp whose middle streak is parked far right.
            double sum = 0; int n = 0;
            var same = new HashSet<long>();
            BadgerWearArt.StampHoney(layout.Unpadded, layout.BodyCenterX, layout.FeetY, 0f, BadgerWear.Scuffed,
                (x, y) => same.Add(((long)x << 32) | (uint)y), (x, y) => true, 60f);
            BadgerWearArt.StampHoney(layout.Unpadded, layout.BodyCenterX, layout.FeetY, 0f, BadgerWear.Scuffed,
                (x, y) => { if (!same.Contains(((long)x << 32) | (uint)y)) { sum += x; n++; } }, (x, y) => true, shift);
            return n > 0 ? (float)(sum / n) : float.NaN;
        }

        [Test]
        public void HoneyStreaksSitInsideFreshOnScuffedAndAbove()
        {
            Assert.That(BadgerWearArt.HoneyStreakCount, Is.EqualTo(3));
            Assert.That(BadgerWearArt.HoneyMarksOn(BadgerWear.Fresh), Is.False);
            Assert.That(BadgerWearArt.HoneyMarksOn(BadgerWear.Scuffed), Is.True);
            Assert.That(BadgerWearArt.HoneyMarksOn(BadgerWear.Bruised), Is.True);
            Assert.That(BadgerWearArt.HoneyMarksOn(BadgerWear.Wrecked), Is.True);
            Assert.That(BadgerWearArt.HoneyStreaksFor(BadgerWear.Fresh), Is.EqualTo(0));
            Assert.That(BadgerWearArt.HoneyStreaksFor(BadgerWear.Scuffed), Is.EqualTo(3));
            Assert.That(BadgerWearArt.HoneyStreaksFor(BadgerWear.Bruised), Is.EqualTo(5));
            Assert.That(BadgerWearArt.HoneyStreaksFor(BadgerWear.Wrecked), Is.EqualTo(5));
            var widths = new[] { 2, 3, 2, 4, 3 };
            for (int i = 0; i < 5; i++)
            {
                var mark = BadgerWearArt.HoneyMarkAt(i);
                Assert.That(mark.WidthPx, Is.EqualTo(widths[i]), "streak " + i);
                if (i >= 3) Assert.That(mark.WidthPx, Is.GreaterThanOrEqualTo(3).And.LessThanOrEqualTo(4));
                Assert.That(BadgerWearArt.FreshCovers("badger_idle_b", mark.U, mark.V), Is.True, "idle anchor " + i);
                Assert.That(BadgerWearArt.FreshCovers("badger_crouch", mark.U, mark.V), Is.True, "crouch anchor " + i);
            }
            var ink = BadgerWearArt.HoneyInk;
            Assert.That(ink.r, Is.GreaterThan(ink.b));
            Assert.That(ink.g, Is.GreaterThan((byte)140));
            Assert.That(ink.b, Is.LessThan((byte)40));
            Assert.That(BadgerPolish.YellowStreak(ink.r / 255f, ink.g / 255f, ink.b / 255f), Is.True);

            var sizes = new[] { new Vector2(402f, 874f), new Vector2(820f, 1180f), new Vector2(412f, 915f) };
            var poses = new[] { "badger_idle_b", "badger_crouch" };
            var wears = new[] { BadgerWear.Scuffed, BadgerWear.Bruised, BadgerWear.Wrecked };
            for (int s = 0; s < sizes.Length; s++)
            {
                for (int p = 0; p < poses.Length; p++)
                {
                    var layout = BadgerWearArt.Layout(poses[p], sizes[s].x, sizes[s].y);
                    int sw = (int)sizes[s].x;
                    int sh = (int)sizes[s].y;
                    int freshN = BadgerWearArt.StampHoney(layout.Unpadded, layout.BodyCenterX, layout.FeetY, 0f, BadgerWear.Fresh,
                        (x, y) => { }, (x, y) => true);
                    Assert.That(freshN, Is.EqualTo(0), poses[p] + " fresh");
                    int first = -1;
                    int firstBruised = -1;
                    for (int w = 0; w < wears.Length; w++)
                    {
                        var buf = new Color32[sw * sh];
                        int n = BadgerWearArt.StampHoney(layout.Unpadded, layout.BodyCenterX, layout.FeetY, 0f, wears[w],
                            (x, y) =>
                            {
                                if ((uint)x >= (uint)sw || (uint)y >= (uint)sh) return;
                                buf[y * sw + x] = BadgerWearArt.HoneyInk;
                            },
                            (x, y) =>
                            {
                                float u = (x + 0.5f - layout.Unpadded.x) / layout.Unpadded.width;
                                float v = (y + 0.5f - layout.Unpadded.y) / layout.Unpadded.height;
                                return BadgerWearArt.FreshCovers(poses[p], u, v);
                            });
                        Assert.That(n, Is.GreaterThan(20), poses[p] + " " + wears[w] + " " + sizes[s].x);
                        if (wears[w] == BadgerWear.Scuffed) first = n;
                        else if (firstBruised < 0) { firstBruised = n; Assert.That(n, Is.GreaterThan(first), "bruised adds marks"); }
                        else Assert.That(n, Is.EqualTo(firstBruised), "wrecked keeps bruised marks");
                        var measured = BadgerWearArt.MeasureStreaks(buf, sw, sh);
                        int want = BadgerWearArt.HoneyStreaksFor(wears[w]);
                        Assert.That(measured.Count, Is.EqualTo(want), poses[p] + " " + sizes[s].x + " " + wears[w] + " count " + measured.Count);
                        for (int i = 0; i < measured.Widths.Length; i++)
                            Assert.That(measured.Widths[i], Is.GreaterThanOrEqualTo(2).And.LessThanOrEqualTo(4),
                                poses[p] + " width " + measured.Widths[i]);
                    }
                }
            }

            string duel = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            string stills = File.ReadAllText("Assets/_Project/Scripts/Editor/BadgerPolishStills.cs");
            Assert.That(duel.Contains("BadgerWearArt.HoneyTexture"), Is.True);
            Assert.That(duel.Contains("DrawHoneyMarks"), Is.True);
            Assert.That(stills.Contains("BadgerWearArt.StampHoney"), Is.True);
        }

        [Test]
        public void PickTokenDrawsAfterTheWearFlash()
        {
            string duel = File.ReadAllText("Assets/_Project/Scripts/App/FlockFiveApp.Badger.cs");
            int page = duel.IndexOf("void DrawBadgerPage()", StringComparison.Ordinal);
            int bodyCall = duel.IndexOf("DrawBadgerPageBody();", page, StringComparison.Ordinal);
            int flashCall = duel.IndexOf("DrawBadgerBlowFlash();", bodyCall, StringComparison.Ordinal);
            int tokenCall = duel.IndexOf("DrawBadgerPickToken();", flashCall, StringComparison.Ordinal);
            Assert.That(bodyCall, Is.GreaterThan(0));
            Assert.That(flashCall, Is.GreaterThan(bodyCall));
            Assert.That(tokenCall, Is.GreaterThan(flashCall));
            int flashFn = duel.IndexOf("void DrawBadgerBlowFlash()", StringComparison.Ordinal);
            int next = duel.IndexOf("bool DrawBadgerIceBlock", flashFn, StringComparison.Ordinal);
            string flashBody = duel.Substring(flashFn, next - flashFn);
            Assert.That(flashBody.Contains("FightFlashSerial = ++FightPaintSerial"), Is.True);
            int tokenFn = duel.IndexOf("void DrawBadgerPickToken()", StringComparison.Ordinal);
            Assert.That(duel.IndexOf("FightTokenSerial = ++FightPaintSerial", tokenFn, StringComparison.Ordinal), Is.GreaterThan(tokenFn));
            Assert.That(flashFn, Is.LessThan(tokenFn).Or.GreaterThan(0));
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

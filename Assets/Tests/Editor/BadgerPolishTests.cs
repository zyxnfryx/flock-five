#if UNITY_EDITOR
using System;
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
        public void MusicTightensWithoutPassingThePitchCap()
        {
            Assert.That(BadgerPolish.LoopPitch(BadgerMood.Smug, BadgerTwist.StrongerTiles), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BadgerPolish.LoopPitch(BadgerMood.Desperate, BadgerTwist.NewTaunt), Is.EqualTo(BadgerPolish.DesperatePitch).Within(0.0001f));
            Assert.That(BadgerPolish.LoopPitch(BadgerMood.Smug, BadgerTwist.FasterRiff), Is.EqualTo(1.04f).Within(0.0001f));
            float both = BadgerPolish.LoopPitch(BadgerMood.Desperate, BadgerTwist.FasterRiff);
            Assert.That(both, Is.EqualTo(BossBeat.PitchCap).Within(0.0001f));
            Assert.That(both, Is.LessThanOrEqualTo(BossBeat.PitchCap));
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

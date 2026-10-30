namespace FlockFive
{
    public enum BadgerMood
    {
        Smug = 0,
        Rattled = 1,
        Desperate = 2,
        Surrender = 3,
    }

    // One new twist per appearance. Cycles these three. Never a fourth, never a new minigame.
    public enum BadgerTwist
    {
        StrongerTiles = 0,
        NewTaunt = 1,
        FasterRiff = 2,
    }

    // Cartoon wear. No blood. Fresh is untouched; each step adds a bruise.
    public enum BadgerWear
    {
        Fresh = 0,
        Scuffed = 1,
        Bruised = 2,
        Wrecked = 3,
    }

    public enum BadgerBlowPhase
    {
        None = 0,
        Flash = 1,
        Silence = 2,
        Fanfare = 3,
    }

    // Honey-badger polish (spec items 1-4 plus beat-up hits). Pure. The fight and the
    // page read it; they do not keep a second copy of the numbers.
    public static class BadgerPolish
    {
        public const int DesperateChanceAdd = 20;
        public const float DesperatePitch = 1.02f;
        public const float RiffPitch = BossBeat.PitchCap;

        // Open pupil on badger_idle_b, top-left shares of that frame. The tell glint
        // uses this every time so it lands on the same eye.
        public const float TellEyeU = 0.43f;
        public const float TellEyeV = 0.21f;

        public static BadgerMood Mood(int playerScore, int playerTarget, bool surrendered)
        {
            if (surrendered) return BadgerMood.Surrender;
            if (playerScore <= 0) return BadgerMood.Smug;
            int target = playerTarget < 1 ? 1 : playerTarget;
            if (playerScore * 3 >= target * 2) return BadgerMood.Desperate;
            return BadgerMood.Rattled;
        }

        public static BadgerWear WearOf(int playerScore, int playerTarget)
        {
            if (playerScore <= 0) return BadgerWear.Fresh;
            int target = playerTarget < 1 ? 1 : playerTarget;
            if (playerScore * 3 < target) return BadgerWear.Scuffed;
            if (playerScore * 3 < target * 2) return BadgerWear.Bruised;
            return BadgerWear.Wrecked;
        }

        public static int BruiseCount(BadgerWear wear)
        {
            if (wear == BadgerWear.Scuffed) return 1;
            if (wear == BadgerWear.Bruised) return 2;
            if (wear == BadgerWear.Wrecked) return 3;
            return 0;
        }

        // Cheek, shoulder, brow. Shares of the sprite, top-left origin. Dusty, not red.
        public static void BruiseUv(int i, out float u, out float v, out float rad)
        {
            if (i <= 0) { u = 0.34f; v = 0.36f; rad = 0.045f; return; }
            if (i == 1) { u = 0.56f; v = 0.50f; rad = 0.055f; return; }
            u = 0.38f; v = 0.26f; rad = 0.034f;
        }

        public static void SweatUv(int i, out float u, out float v)
        {
            if (i <= 0) { u = 0.50f; v = 0.22f; return; }
            u = 0.58f; v = 0.20f;
        }

        public static void EarUv(int i, out float u, out float v)
        {
            if (i <= 0) { u = 0.22f; v = 0.14f; return; }
            u = 0.70f; v = 0.12f;
        }

        public static bool Sweat(BadgerMood mood)
            => mood == BadgerMood.Rattled || mood == BadgerMood.Desperate;

        // Degrees of lean. Smug stands square. Later moods pin the ears by leaning back.
        public static float EarsBack(BadgerMood mood)
        {
            if (mood == BadgerMood.Rattled) return 6f;
            if (mood == BadgerMood.Desperate) return 11f;
            if (mood == BadgerMood.Surrender) return 14f;
            return 0f;
        }

        // Wider and shorter as the hits add up. 1,1 when fresh.
        public static void WearScale(BadgerWear wear, out float sx, out float sy)
        {
            float k = 0f;
            if (wear == BadgerWear.Scuffed) k = 0.03f;
            else if (wear == BadgerWear.Bruised) k = 0.06f;
            else if (wear == BadgerWear.Wrecked) k = 0.10f;
            sx = 1f + k;
            sy = 1f - k;
        }

        // Extra lean on the hit itself, growing with the wear already on him.
        public static float HitLean(BadgerWear wear, float peck)
        {
            if (peck < 0f) peck = 0f;
            if (peck > 1f) peck = 1f;
            float amp = 3f + 3.5f * (int)wear;
            return amp * peck;
        }

        // Stored chance plus the desperate bump, capped at 100. Other moods pass the stored chance through.
        public static int SlamChance(int stored, BadgerMood mood)
        {
            if (stored < 0) stored = 0;
            if (mood != BadgerMood.Desperate) return stored > 100 ? 100 : stored;
            int c = stored + DesperateChanceAdd;
            if (c > 100) c = 100;
            return c;
        }

        public static BadgerTwist TwistOf(int appearance)
        {
            int n = appearance < 1 ? 1 : appearance;
            int k = (n - 1) % 3;
            if (k == 0) return BadgerTwist.StrongerTiles;
            if (k == 1) return BadgerTwist.NewTaunt;
            return BadgerTwist.FasterRiff;
        }

        public static bool HasTwist(int appearance, BadgerTwist kind) => TwistOf(appearance) == kind;

        // Desperate lifts the loop to DesperatePitch. A faster-riff visit sits at the pitch cap.
        // The two never stack past PitchCap, and smug visits without that twist stay at 1.
        // This is the target. MixDesk slews the heard pitch after the entrance cue.
        public static float LoopPitch(BadgerMood mood, BadgerTwist twist)
        {
            float p = 1f;
            if (mood == BadgerMood.Desperate) p = DesperatePitch;
            if (twist == BadgerTwist.FasterRiff && RiffPitch > p) p = RiffPitch;
            return BossBeat.ClampPitch(p);
        }

        // Promote the coolest tile one step (2 to 3, or 3 to 5). Same order every call.
        // Values stay on the real honey faces. A grid of only 5s is left alone.
        public static void Strengthen(int[] tiles)
        {
            if (tiles == null) return;
            int ix = -1;
            int cool = 99;
            for (int i = 0; i < tiles.Length; i++)
            {
                int h = tiles[i];
                if (h >= 5) continue;
                if (h < cool) { cool = h; ix = i; }
            }
            if (ix < 0) return;
            tiles[ix] = cool <= 2 ? 3 : 5;
        }

        // The boss grid for a visit. Stronger-tiles visits step one tile up.
        // Every other twist keeps BadgerSchedule.BossTileMix exactly.
        public static int[] BossTiles(int appearance, int seed)
        {
            var tiles = BadgerSchedule.BossTileMix(appearance, seed);
            if (HasTwist(appearance, BadgerTwist.StrongerTiles)) Strengthen(tiles);
            return tiles;
        }

        // t = 0 is the winning hit. Flash, then one beat of silence, then the fanfare is due.
        public static BadgerBlowPhase BlowAt(float t)
        {
            if (t < 0f) return BadgerBlowPhase.None;
            if (t < BossBeat.FlashSeconds) return BadgerBlowPhase.Flash;
            if (t < BossBeat.FlashSeconds + BossBeat.SilenceSeconds) return BadgerBlowPhase.Silence;
            return BadgerBlowPhase.Fanfare;
        }

        public static float BlowFlash(float t)
        {
            if (BlowAt(t) != BadgerBlowPhase.Flash) return 0f;
            return BossBeat.FlashAlpha(t);
        }

        public static bool FanfareDue(float before, float after)
            => BlowAt(before) != BadgerBlowPhase.Fanfare && BlowAt(after) == BadgerBlowPhase.Fanfare;

        public static float BlowDuration => BossBeat.FlashSeconds + BossBeat.SilenceSeconds;
    }
}

using UnityEngine;

namespace FlockFive
{
    // b129: badger polish (frozen look, battle size, garden-to-fight leap). Pure helpers so tests can pin them.
    // The Legacy* switches exist only so the evidence harness can paint the old look beside the new one in one
    // session; nothing in the game sets them.
    public static class Badger129
    {
        public static bool LegacyFrozen;
        public static bool LegacyLeap;

        // The one battle-screen badger scale. Battle draw path only (BadgerActorNow); the garden pest, the intros and
        // the album never read it.
        public const float BattleScaleDefault = 1f;
        public static float BattleScale = BattleScaleDefault;
        // v3 option: per-device ceiling on the knob. 0 = off (default, nothing changes). TabletAspect: w/h above this is
        // treated as a tablet (iPad 0.75, iPhone 0.46).
        public static float BattleScaleMaxTablet = 0f;
        public static float BattleScaleMaxPhone = 0f;
        public const float TabletAspect = 0.6f;
        public static float BattleScaleFor(float screenW, float screenH)
        {
            float cap = screenH > 0f && screenW / screenH > TabletAspect ? BattleScaleMaxTablet : BattleScaleMaxPhone;
            return cap > 0f ? Mathf.Min(BattleScale, cap) : BattleScale;
        }

        // ---- the leap (garden to fight) ----
        // Same beats as BadgerLeap (hop 0.8, crouch 0.6, leap 1.2, wash 0.4 = 3.0 s). The leap beat now carries the
        // badger from the garden crouch to his fight mark (ease-out), the wash beat is the landing: a 3-frame squash,
        // then he holds his fight pose while the honey wash crossfades in behind him. The fight page draws him on the
        // same mark over its wash-out, so nothing pops at the cut.
        public const float LandSquashSeconds = 3f / 60f;
        public const float LandSquashAmount = 0.18f;

        // 0..1 travel across the leap beat, ease-out (fast takeoff, soft arrival).
        public static float LeapTravel(float t)
        {
            var beat = BadgerLeap.BeatAt(t);
            if (beat < BadgerLeapBeat.Leap) return 0f;
            if (beat > BadgerLeapBeat.Leap) return 1f;
            float u = BadgerLeap.Local(t);
            float k = 1f - u;
            return 1f - k * k; // ease-out (quadratic: soft arrival without a long dead tail)
        }

        // Arc height share (0 at both ends) and the toward-camera swell (peaks mid-air).
        public static float LeapArc(float t) { float e = LeapTravel(t); return Mathf.Sin(e * Mathf.PI); }
        public static float LeapSwell(float t) { return 0.30f * LeapArc(t); }

        // Landing squash: 1 on the touchdown frame, gone after LandSquashSeconds.
        public static float LandSquash(float t)
        {
            float land = BadgerLeap.BeatStart(BadgerLeapBeat.Wash);
            if (t < land) return 0f;
            float age = t - land;
            if (age >= LandSquashSeconds) return 0f;
            return 1f - age / LandSquashSeconds;
        }

        // v2: one shared scale per frame. Every leap/landing frame is drawn so its trimmed art is the same on-screen
        // height as his idle pose at that unit, so a frame swap changes only the pose, never his size.
        public static readonly Color ShatterIce = new Color(0.78f, 0.92f, 1f, 1f); // icy blue-white chunks
        public const string LeapScaleRef = "badger_idle_b";
        public static float LeapUnit(float unit, float refArtH, float frameArtH)
            => frameArtH < 1f ? unit : unit * refArtH / frameArtH;

        public static string LeapFrame(float t)
        {
            float e = LeapTravel(t);
            if (e < 0.18f) return "badger_leap_0";
            if (e < 0.50f) return "badger_leap_1";
            if (e < 0.82f) return "badger_leap_3";
            return "badger_crouch";
        }

        // ---- frozen ----
        public const string FrozenBlockArt = "badger_frozen_block";
        // Block height as a share of the standing idle height (taller: the block clears his raised paws).
        public const float FrozenBlockHeight = 1.45f; // v2 art: badger body ~0.70 of the block, so he reads at fighter height
        // Frost rim inset from the hex outline, in screen pixels (spec 1-2 px).
        public const float FrostRimInsetPx = 1.5f;
        public const float FrostRimWidthPx = 1.6f;
    
    }

    // b129: the freeze and thaw beats on the fighter. Clocks on PlayClock (a capture step is honored).
    public static class BadgerIceAnim
    {
        // v3: the no-ice hands-up art, placed inside the block art (block-sprite px), measured from the fur bounds of
        // both PNGs so he lines up with himself when the ice finishes.
        public const string HandsUpArt = "badger_handsup_noice";
        public const float HandsUpScale = 0.805f;
        public const float HandsUpX = 57.0f;
        public const float HandsUpY = 44.7f;
        // Hands-up badger's feet inside the block art, in block-art pixels above its bottom edge (icicles hang below).

        public const float SpraySeconds = 0.10f;
        public const float GrowFrom = 0.08f, GrowSeconds = 0.40f;
        public const float FreezeSeconds = 0.50f;
        public const float ShiverFrom = 0.40f;
        public const float GlintAt = 0.42f;

        public const float WobbleSeconds = 0.50f;   // two wobbles
        public const float CrackFrom = 0.12f;
        public const float ShatterSeconds = 0.55f;  // chunks fall and fade
        public const float HopSeconds = 0.40f;
        public static float ThawSeconds => WobbleSeconds + ShatterSeconds;

        public static float FreezeAt = -1f, ThawAt = -1f;
        public static bool CrackPlayed, ShatterPlayed;

        public static void Freeze(float now) { FreezeAt = now; ThawAt = -1f; }
        public static void Thaw(float now) { ThawAt = now; CrackPlayed = false; ShatterPlayed = false; }
        public static void Reset() { FreezeAt = -1f; ThawAt = -1f; }

        public static float FreezeAge(float now) => FreezeAt < 0f ? 99f : now - FreezeAt;
        public static float ThawAge(float now) => ThawAt < 0f ? -1f : now - ThawAt;
        public static bool Freezing(float now) => FreezeAge(now) < FreezeSeconds;
        public static bool Thawing(float now) { float b = ThawAge(now); return b >= 0f && b < ThawSeconds; }

        // 0..1 ice height, feet to head.
        public static float Grow(float age) => PopupMotion.Smooth(Mathf.Clamp01((age - GrowFrom) / GrowSeconds));
        public static float SprayAlpha(float age) => age < 0f || age > SpraySeconds * 1.6f ? 0f : 1f - Mathf.Clamp01(age / (SpraySeconds * 1.6f));
        // Two wobbles that grow then settle, degrees.
        public static float Wobble(float b) => b < 0f || b >= WobbleSeconds ? 0f : 6f * Mathf.Sin(b / WobbleSeconds * Mathf.PI * 4f) * Mathf.Sin(b / WobbleSeconds * Mathf.PI);
        public static float Crack(float b) => Mathf.Clamp01((b - CrackFrom) / (WobbleSeconds - CrackFrom));
        public static float ShatterT(float b) => b < WobbleSeconds ? -1f : (b - WobbleSeconds) / ShatterSeconds;
        // Hop after the shatter: lift share 0..1 and a decaying shake-off (degrees).
        public static float Hop(float b) { float u = (b - WobbleSeconds - 0.05f) / HopSeconds; return u <= 0f || u >= 1f ? 0f : Mathf.Sin(u * Mathf.PI); }
        public static float ShakeOff(float b) { float u = (b - WobbleSeconds) / HopSeconds; return u <= 0f || u >= 1f ? 0f : 5f * Mathf.Sin(u * Mathf.PI * 5f) * (1f - u); }
    }
}

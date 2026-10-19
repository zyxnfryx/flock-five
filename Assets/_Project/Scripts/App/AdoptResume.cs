using System;

namespace FlockFive
{
    // b131: what the adoption scene does on a fresh process.
    // BeginAdopt writes "adopted" at once (color + suggested name, named flag 0) but the scene itself
    // (AdoptStep, the glove, the name picker) is memory-only. If the app is backgrounded and iOS
    // reclaims it, the next launch saw "adopted", skipped the scene, and nothing ever asked for the
    // name: the bird stayed unnamed and the Daily lesson started on top of it. Pure, so an editor
    // test can drive it.
    public enum AdoptBoot
    {
        // Nothing owed: not past level 1, or adopted and named.
        None = 0,
        // Never adopted: run the whole scene (fly, greet, look, name).
        Begin = 1,
        // Adopted but not named: back to the look step (glove on the bird), name picker required.
        ResumeLook = 2
    }

    public static class AdoptResume
    {
        // namedFlag: the saved named pref, -1 when the key is missing (older save).
        // hasName: the saved name is non-empty after clipping.
        // Same rule as FlockFiveApp.AvatarNeedsName: flag 1 named, flag 0 not, missing flag follows the name.
        public static bool NeedsName(int namedFlag, bool hasName)
        {
            if (namedFlag == 1) return false;
            if (namedFlag == 0) return true;
            return !hasName;
        }

        public static AdoptBoot Plan(int nextPlay, bool adopted, int namedFlag, bool hasName)
        {
            if (nextPlay < 1) return AdoptBoot.None;
            if (!adopted) return AdoptBoot.Begin;
            return NeedsName(namedFlag, hasName) ? AdoptBoot.ResumeLook : AdoptBoot.None;
        }

        // Daily intro caption width: the band the rails leave, never wider than the safe text width
        // (so a narrow iPhone cannot get a plate that touches the glass edge).
        public static float DailyCaptionMaxWidth(float bandW, float screenW, float screenH)
        {
            float safe = CaptionLayout.SafeTextWidth(screenW, screenH);
            float w = bandW > 48f ? bandW : Math.Min(screenW * 0.72f, 420f);
            return w > safe ? safe : w;
        }
    }
}

using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // b132: gap in px between the bottom of the home bird's name plate and the top of the
        // LEVEL lettering, for a named bird and an unclamped stack. It walks the same statics
        // PlaceHomeBranch walks (limb, seat, PerchAnchor, AvatarPlateRect), but takes the LEVEL
        // square and the screen height as arguments so a test needs no Screen. The limb is
        // seated off FlowerLevelTop, so sliding the square slides the plate with it: the gap
        // must not change when LevelRect is raised. Not a layout clamp, only an audit.
        public static float HomePlateGapAudit(Rect level, float h, float s, out float plateBottom)
        {
            float levelTop = FlowerLevelTop(level);
            WoodWorld(out float worldW, out float worldH);
            float ppu = h / (WorldBuilder.CamOrtho * 2f);
            if (ppu < 1f) ppu = 1f;
            float branchPx = ppu * AvatarBranchMul;
            HomeTwigRect(Vector2.zero, branchPx * worldW, branchPx * worldH, out var probe);
            float visH = probe.height > 8f ? probe.height : 8f;
            float y = levelTop - visH - AvatarPlateDrop(s) - 8f * s;
            var limbCenter = new Vector2(0f, y - probe.y);
            float icon = HomeAvatarBirdPx(h);
            float world = AvatarBirdWorld();
            float birdPx = world > 0.001f ? icon / world : 0.01f;
            var seated = LimbBirdSeated(limbCenter, branchPx, birdPx, true, HomeBranchAnchor());
            var anchor = PerchAnchor(seated, icon, true, 0f, 0f);
            plateBottom = AvatarPlateRect(anchor, icon, s).yMax;
            return levelTop - plateBottom;
        }
    }
}

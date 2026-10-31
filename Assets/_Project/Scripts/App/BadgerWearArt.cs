using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // v4_1 wear sprites. One helper for the duel drawer and the stills painter.
    // Stage PNGs already composite halo_back, body, halo_front. Never blit the
    // halo files on top of those (double halo). Padding is +70 left/right, +110 top.
    // Placement: one device scale for every wear stage, crouch art divided by its
    // 1.15 feature scale, body-bbox centre on the screen mid, feet locked.
    public static class BadgerWearArt
    {
        public const int PadX = 70;
        public const int PadTop = 110;
        public const int PadW = PadX * 2;

        // Mock composites used a 16 px margin and the wide wrecked crouch sat on it.
        // 20 px is the fit target so both sides stay at least 20 after the blit.
        public const float SideMarginPx = 20f;
        public const float FeetFrac = 0.70f;
        public const float RefHeightFrac = 0.46f;
        public const float RefSrcH = 640f;
        public const float CrouchFeature = 1.15f;

        // Fresh-stage body box (alpha > 8) and the widest content box across stages,
        // in padded-canvas pixels. Right and bottom are exclusive.
        struct ArtBox
        {
            public int W, H;
            public int BodyL, BodyR, BodyB;
            public int ContentL, ContentR;
        }

        // Parallel to Poses. Content L/R is the union of every wear stage.
        static readonly ArtBox[] Boxes =
        {
            new ArtBox { W = 503, H = 744, BodyL = 76, BodyR = 427, BodyB = 738, ContentL = 30, ContentR = 454 },
            new ArtBox { W = 578, H = 750, BodyL = 76, BodyR = 502, BodyB = 744, ContentL = 76, ContentR = 502 },
            new ArtBox { W = 943, H = 714, BodyL = 76, BodyR = 867, BodyB = 708, ContentL = 76, ContentR = 867 },
            new ArtBox { W = 785, H = 709, BodyL = 76, BodyR = 709, BodyB = 703, ContentL = 15, ContentR = 709 },
            new ArtBox { W = 748, H = 710, BodyL = 76, BodyR = 672, BodyB = 704, ContentL = 51, ContentR = 672 },
            new ArtBox { W = 808, H = 712, BodyL = 76, BodyR = 732, BodyB = 706, ContentL = 76, ContentR = 732 },
            new ArtBox { W = 876, H = 724, BodyL = 76, BodyR = 800, BodyB = 718, ContentL = 76, ContentR = 800 },
            new ArtBox { W = 1117, H = 779, BodyL = 76, BodyR = 1041, BodyB = 773, ContentL = 68, ContentR = 1088 },
            new ArtBox { W = 758, H = 764, BodyL = 76, BodyR = 682, BodyB = 758, ContentL = 76, ContentR = 682 },
        };

        // The wear stills are idle_b, and wrecked swaps to crouch. Those two set the scale.
        static readonly string[] FitPoses = { "badger_idle_b", "badger_crouch" };

        public static readonly string[] Poses =
        {
            "badger_idle_a", "badger_idle_b", "badger_crouch",
            "badger_hop_a", "badger_hop_b",
            "badger_leap_0", "badger_leap_1", "badger_leap_3",
            "badger_shrug"
        };

        public static readonly string[] Tokens = { "fresh", "scuffed", "bruised", "wrecked" };

        // Stage PNGs already include the halo. Layers stay off.
        public const bool UseStagePngs = true;

        public static readonly string[] CompositeOrder = { "halo_back", "body", "halo_front" };

        public readonly struct Blit
        {
            public readonly string Sprite;
            public readonly Rect Dest;
            public readonly bool StagePng;

            public Blit(string sprite, Rect dest, bool stagePng)
            {
                Sprite = sprite;
                Dest = dest;
                StagePng = stagePng;
            }
        }

        public static string Token(BadgerWear wear)
        {
            int i = (int)wear;
            if (i < 0) i = 0;
            if (i >= Tokens.Length) i = Tokens.Length - 1;
            return Tokens[i];
        }

        public static string PoseToken(string frame)
        {
            if (string.IsNullOrEmpty(frame)) return "";
            if (frame.StartsWith("badger_")) return frame.Substring(7);
            return frame;
        }

        public static bool IsPose(string frame)
        {
            if (string.IsNullOrEmpty(frame)) return false;
            for (int i = 0; i < Poses.Length; i++)
                if (Poses[i] == frame) return true;
            return false;
        }

        public static string StageKey(string frame, BadgerWear wear)
        {
            if (!IsPose(frame)) return "";
            return frame + "_wear_" + Token(wear);
        }

        public static string HaloBack(string frame, BadgerWear wear)
            => HaloName("back", frame, wear);

        public static string HaloFront(string frame, BadgerWear wear)
            => HaloName("front", frame, wear);

        static string HaloName(string side, string frame, BadgerWear wear)
        {
            if (!IsPose(frame)) return "";
            return "halo_" + side + "_" + PoseToken(frame) + "_" + Token(wear);
        }

        // Unpadded body dest -> dest of the padded stage PNG. The inner srcW x srcH
        // region (offset 70,110) lands on `unpadded`, so the body does not jump.
        public static Rect PaddedDest(Rect unpadded, float srcW, float srcH)
        {
            if (srcW < 1f) srcW = 1f;
            if (srcH < 1f) srcH = 1f;
            float ux = unpadded.width / srcW;
            float uy = unpadded.height / srcH;
            return new Rect(
                unpadded.x - PadX * ux,
                unpadded.y - PadTop * uy,
                unpadded.width + PadW * ux,
                unpadded.height + PadTop * uy);
        }

        public static Rect BodyOfPadded(Rect padded, float srcW, float srcH)
        {
            if (srcW < 1f) srcW = 1f;
            if (srcH < 1f) srcH = 1f;
            float padW = srcW + PadW;
            float padH = srcH + PadTop;
            float ux = padded.width / padW;
            float uy = padded.height / padH;
            return new Rect(
                padded.x + PadX * ux,
                padded.y + PadTop * uy,
                srcW * ux,
                srcH * uy);
        }

        // Crouch source art is ~1.15x larger per feature than idle_b. Other poses stay at 1.
        public static float FeatureScale(string frame)
        {
            if (frame == "badger_crouch") return CrouchFeature;
            return 1f;
        }

        // One scale for the screen. Wear stage is not an input: fresh, scuffed,
        // bruised and wrecked share it. Crouch is normalised later by FeatureScale.
        public static float DeviceScale(float screenW, float screenH)
        {
            if (screenW < 2f) screenW = 2f;
            if (screenH < 2f) screenH = 2f;
            float k = screenH * RefHeightFrac / RefSrcH;
            float room = screenW * 0.5f - SideMarginPx;
            if (room < 1f) room = 1f;
            for (int i = 0; i < FitPoses.Length; i++)
            {
                if (!TryBox(FitPoses[i], out var box)) continue;
                float cx = (box.BodyL + box.BodyR) * 0.5f;
                float half = Mathf.Max(cx - box.ContentL, box.ContentR - cx);
                float feat = FeatureScale(FitPoses[i]);
                if (feat > 0.01f) half /= feat;
                if (half < 1f) continue;
                float fit = room / half;
                if (fit < k) k = fit;
            }
            return k;
        }

        public readonly struct WearLayout
        {
            public readonly bool Ok;
            public readonly float DeviceScale;
            public readonly float Scale;
            public readonly float BodyCenterX;
            public readonly float FeetY;
            public readonly float BodyWidth;
            public readonly float ContentLeft;
            public readonly float ContentRight;
            public readonly Rect Dest;
            public readonly Rect Unpadded;
            public readonly float SrcW;
            public readonly float SrcH;

            public WearLayout(bool ok, float deviceScale, float scale, float bodyCenterX, float feetY,
                float bodyWidth, float contentLeft, float contentRight, Rect dest, Rect unpadded, float srcW, float srcH)
            {
                Ok = ok;
                DeviceScale = deviceScale;
                Scale = scale;
                BodyCenterX = bodyCenterX;
                FeetY = feetY;
                BodyWidth = bodyWidth;
                ContentLeft = contentLeft;
                ContentRight = contentRight;
                Dest = dest;
                Unpadded = unpadded;
                SrcW = srcW;
                SrcH = srcH;
            }
        }

        // Full-screen placement. Body-bbox centre x is the screen mid. Feet y is
        // H * 0.70. The same call for every wear stage: stage does not move or scale him.
        public static WearLayout Layout(string frame, float screenW, float screenH)
        {
            if (!TryBox(frame, out var box))
                return new WearLayout(false, 0f, 0f, 0f, 0f, 0f, 0f, 0f, new Rect(), new Rect(), 0f, 0f);
            if (screenW < 2f) screenW = 2f;
            if (screenH < 2f) screenH = 2f;
            float device = DeviceScale(screenW, screenH);
            float feat = FeatureScale(frame);
            if (feat < 0.01f) feat = 1f;
            float kk = device / feat;
            float srcW = box.W - PadW;
            float srcH = box.H - PadTop;
            if (srcW < 1f) srcW = 1f;
            if (srcH < 1f) srcH = 1f;
            float bx = (box.BodyL + box.BodyR) * 0.5f;
            float x0 = Mathf.Round(screenW * 0.5f - bx * kk);
            float y0 = Mathf.Round(screenH * FeetFrac - box.BodyB * kk);
            var dest = new Rect(x0, y0, box.W * kk, box.H * kk);
            var unpadded = new Rect(x0 + PadX * kk, y0 + PadTop * kk, srcW * kk, srcH * kk);
            return new WearLayout(
                true,
                device,
                kk,
                x0 + bx * kk,
                y0 + box.BodyB * kk,
                (box.BodyR - box.BodyL) * kk,
                Mathf.Round(x0 + box.ContentL * kk),
                Mathf.Round(x0 + box.ContentR * kk),
                dest,
                unpadded,
                srcW,
                srcH);
        }

        static bool TryBox(string frame, out ArtBox box)
        {
            box = default;
            if (string.IsNullOrEmpty(frame)) return false;
            for (int i = 0; i < Poses.Length && i < Boxes.Length; i++)
            {
                if (Poses[i] != frame) continue;
                box = Boxes[i];
                return true;
            }
            return false;
        }

        // Shared wear blit. DrawBadgerWear and the stills PaintWear both call this.
        public static Blit PaintWear(string frame, Rect unpaddedBody, float srcW, float srcH, BadgerWear wear)
        {
            string key = StageKey(frame, wear);
            if (string.IsNullOrEmpty(key) || unpaddedBody.width < 1f || unpaddedBody.height < 1f)
                return new Blit("", unpaddedBody, UseStagePngs);
            return new Blit(key, PaddedDest(unpaddedBody, srcW, srcH), UseStagePngs);
        }

        public static void FillDrawList(string frame, BadgerWear wear, List<string> dst)
        {
            if (dst == null) return;
            dst.Clear();
            if (!IsPose(frame)) return;
            if (UseStagePngs)
            {
                dst.Add(StageKey(frame, wear));
                return;
            }
            dst.Add(HaloBack(frame, wear));
            dst.Add(StageKey(frame, wear));
            dst.Add(HaloFront(frame, wear));
        }

        public static bool DrawListIsSafe(List<string> layers)
        {
            if (layers == null || layers.Count == 0) return false;
            bool stage = false;
            bool halo = false;
            for (int i = 0; i < layers.Count; i++)
            {
                string n = layers[i];
                if (string.IsNullOrEmpty(n)) continue;
                if (n.Contains("_wear_")) stage = true;
                if (n.StartsWith("halo_")) halo = true;
            }
            if (stage && halo) return false;
            if (UseStagePngs) return stage && !halo && layers.Count == 1;
            if (!halo || layers.Count < 3) return false;
            return layers[0].StartsWith("halo_back_")
                && layers[layers.Count - 1].StartsWith("halo_front_");
        }
    }
}

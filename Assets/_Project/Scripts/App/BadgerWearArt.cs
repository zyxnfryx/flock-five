using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // v4_1 wear sprites. One helper for the duel drawer and the stills painter.
    // The stage files used to bake halo_back + body + halo_front. Those PNGs are
    // body-only now. Halo files draw as their own layers (back, body, front) so
    // the body can darken without tinting the halo, and the halo has its own alpha.
    // Never blit a baked composite and the halo files together (double halo).
    // Padding is +70 left/right, +110 top.
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

        // Wear PNGs are the body. Halo layers are separate files. Keep this false:
        // drawing a baked stage PNG plus the halo files would double the halo.
        public const bool UseStagePngs = false;

        public static readonly string[] CompositeOrder = { "halo_back", "body", "halo_front" };

        // Body tint walks toward this near-black brown. No red. Body sprite only.
        public static readonly Color TintToward = new Color(14f / 255f, 10f / 255f, 8f / 255f, 1f);

        public const float PunchSeconds = 0.12f;
        public const float PunchPeak = 1.08f;

        public readonly struct Blit
        {
            public readonly string Sprite;
            public readonly string HaloBack;
            public readonly string HaloFront;
            public readonly Rect Dest;
            public readonly Color BodyTint;
            public readonly float HaloAlpha;
            public readonly bool Layered;

            public Blit(string sprite, string haloBack, string haloFront, Rect dest, Color bodyTint, float haloAlpha, bool layered)
            {
                Sprite = sprite;
                HaloBack = haloBack;
                HaloFront = haloFront;
                Dest = dest;
                BodyTint = bodyTint;
                HaloAlpha = haloAlpha;
                Layered = layered;
            }
        }

        // 0, 12, 24, 36 percent. Fresh is untouched.
        public static float TintAmount(BadgerWear wear)
        {
            int i = (int)wear;
            if (i <= 0) return 0f;
            if (i == 1) return 0.12f;
            if (i == 2) return 0.24f;
            return 0.36f;
        }

        // Multiply colour for the badger body only. Halo and UI stay out of this.
        public static Color BodyTint(BadgerWear wear)
        {
            var c = Color.Lerp(Color.white, TintToward, TintAmount(wear));
            c.a = 1f;
            return c;
        }

        public static float HaloAlpha(BadgerWear wear)
        {
            int i = (int)wear;
            if (i <= 0) return 0f;
            if (i == 1) return 0.50f;
            if (i == 2) return 0.70f;
            return 0.90f;
        }

        // 1 -> peak -> 1 over PunchSeconds. Age 0 and anything past the window sit at 1.
        public static float PunchScale(float age)
        {
            if (age < 0f || age >= PunchSeconds) return 1f;
            float u = age / PunchSeconds;
            float tri = u <= 0.5f ? u * 2f : (1f - u) * 2f;
            return Mathf.Lerp(1f, PunchPeak, tri);
        }

        // Scale pivot: body-bbox centre X, planted foot Y. Centre X stays put.
        // Foot Y stays put. At rest the scale is 1, so the plant does not move.
        public static Vector2 PunchPivot(float bodyCenterX, float footY)
            => new Vector2(bodyCenterX, footY);

        public static Rect ScaleAbout(Rect r, float pivotX, float pivotY, float scale)
        {
            if (scale == 1f) return r;
            return new Rect(
                pivotX + (r.x - pivotX) * scale,
                pivotY + (r.y - pivotY) * scale,
                r.width * scale,
                r.height * scale);
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

        // Screen X of the body-bbox centre. The duel drawer and Layout both call this.
        // 402 -> 201, 820 -> 410, 412 -> 206.
        public static float ScreenMidX(float screenW)
        {
            if (screenW < 2f) screenW = 2f;
            return screenW * 0.5f;
        }

        // Body-bbox centre as a fraction of the unpadded sprite width.
        public static float BodyFrac(string frame)
        {
            if (!TryBox(frame, out var box)) return 0.5f;
            float srcW = box.W - PadW;
            if (srcW < 1f) return 0.5f;
            return ((box.BodyL + box.BodyR) * 0.5f - PadX) / srcW;
        }

        // Opaque-foot anchor X so the body-bbox centre lands on ScreenMidX.
        // drawnW is the unpadded sprite width in screen px.
        public static float FootXForBodyMid(float screenW, float bodyFrac, float footCx, float drawnW)
        {
            return ScreenMidX(screenW) - (bodyFrac - footCx) * drawnW;
        }

        public static float BodyCenterAt(float footX, float bodyFrac, float footCx, float drawnW)
        {
            return footX + (bodyFrac - footCx) * drawnW;
        }

        // Content and body edges of a placed unpadded sprite, in the same space as `unpadded`.
        public static void PlacedEdges(string frame, Rect unpadded, float srcW, float srcH,
            out float bodyL, out float bodyR, out float bodyCx, out float contentL, out float contentR, out float bodyBottom)
        {
            bodyL = unpadded.x;
            bodyR = unpadded.xMax;
            bodyCx = unpadded.center.x;
            contentL = bodyL;
            contentR = bodyR;
            bodyBottom = unpadded.yMax;
            if (!TryBox(frame, out var box)) return;
            if (srcW < 1f) srcW = 1f;
            if (srcH < 1f) srcH = 1f;
            float ux = unpadded.width / srcW;
            float uy = unpadded.height / srcH;
            float x0 = unpadded.x - PadX * ux;
            float y0 = unpadded.y - PadTop * uy;
            bodyL = x0 + box.BodyL * ux;
            bodyR = x0 + box.BodyR * ux;
            bodyCx = (bodyL + bodyR) * 0.5f;
            contentL = x0 + box.ContentL * ux;
            contentR = x0 + box.ContentR * ux;
            bodyBottom = y0 + box.BodyB * uy;
        }

        // Full-screen placement. Body-bbox centre x is ScreenMidX. Feet y is
        // H * 0.70. The same call for every wear stage: stage does not move or scale him.
        // x0 stays pixel-rounded, so the 1px content asymmetry at 402 is unchanged.
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
            float x0 = Mathf.Round(ScreenMidX(screenW) - bx * kk);
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
        // One body sprite plus the two halo layers. Same dest for every layer.
        public static Blit PaintWear(string frame, Rect unpaddedBody, float srcW, float srcH, BadgerWear wear)
        {
            var tint = BodyTint(wear);
            float haloA = HaloAlpha(wear);
            string key = StageKey(frame, wear);
            if (string.IsNullOrEmpty(key) || unpaddedBody.width < 1f || unpaddedBody.height < 1f)
                return new Blit("", "", "", unpaddedBody, tint, haloA, false);
            return new Blit(
                key,
                HaloBack(frame, wear),
                HaloFront(frame, wear),
                PaddedDest(unpaddedBody, srcW, srcH),
                tint,
                haloA,
                true);
        }

        public static void FillDrawList(string frame, BadgerWear wear, List<string> dst)
        {
            if (dst == null) return;
            dst.Clear();
            if (!IsPose(frame)) return;
            // Body file no longer contains the halo, so the three layers are not a double.
            dst.Add(HaloBack(frame, wear));
            dst.Add(StageKey(frame, wear));
            dst.Add(HaloFront(frame, wear));
        }

        // Safe means one body with halo_back before it and halo_front after it.
        // A second body, a reversed halo, or a short list would hide a layer or stack two.
        public static bool DrawListIsSafe(List<string> layers)
        {
            if (layers == null || layers.Count != 3) return false;
            if (string.IsNullOrEmpty(layers[0]) || string.IsNullOrEmpty(layers[1]) || string.IsNullOrEmpty(layers[2]))
                return false;
            if (!layers[0].StartsWith("halo_back_")) return false;
            if (!layers[2].StartsWith("halo_front_")) return false;
            if (!layers[1].Contains("_wear_") || layers[1].StartsWith("halo_")) return false;
            if (layers[0] == layers[2]) return false;
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
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

        // Fresh is invisible. Later stages step 0.75, 0.85, then solid.
        public static float HaloAlpha(BadgerWear wear)
        {
            int i = (int)wear;
            if (i <= 0) return 0f;
            if (i == 1) return 0.75f;
            if (i == 2) return 0.85f;
            return 1f;
        }

        // Idle breathe, or the wrecked crouch. screenW and screenH are on the
        // signature so a layout size cannot hide inside the frame index.
        // They are not read. Odd steps of BadgerSitter.BreatheSeconds are idle_b.
        public static string RestPose(float time, BadgerWear wear, float screenW, float screenH)
        {
            _ = screenW;
            _ = screenH;
            if (wear == BadgerWear.Wrecked) return "badger_crouch";
            return BadgerAnim.Idle(time).Frame;
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

        // Scuffed and above. The halo art is a dashed arc. SolidHalo closes those
        // dashes and thickens the stroke so it stays at least HaloMinScreenPx on
        // screen, then Brighten multiplies RGB by HaloRgbMul (clamped). One path
        // for the duel drawer and the stills painter.
        public const float HaloRgbMul = 1.25f;
        public const int HaloMinScreenPx = 2;
        public const bool HaloSolid = true;
        public const float HaloLinkPx = 24f;

        public static int HaloStrokePx(float screenScale)
        {
            if (screenScale < 0.05f) screenScale = 0.05f;
            // One pixel over the minimum so nearest sampling still measures >= 2.
            int px = Mathf.CeilToInt((HaloMinScreenPx + 1f) / screenScale);
            if (px < 3) px = 3;
            // The in-fight fighter is much smaller than the 1x still. The cap has
            // to stay above (min+1)/scale or the ring drops under 2 px on screen.
            if (px > 64) px = 64;
            return px;
        }

        public static Color32 Brighten(Color32 p)
        {
            p.r = MulChannel(p.r);
            p.g = MulChannel(p.g);
            p.b = MulChannel(p.b);
            return p;
        }

        static byte MulChannel(byte c)
        {
            int v = Mathf.RoundToInt(c * HaloRgbMul);
            if (v < 0) v = 0;
            if (v > 255) v = 255;
            return (byte)v;
        }

        public struct RingMeasure
        {
            public int Components;
            public int GapCount;
            public int MinThickness;
            public int P10Thickness;
            public int Ink;
        }

        // Closes dashes whose edges are within HaloLinkPx, strokes the gap at
        // HaloStrokePx(screenScale), then brightens. Same buffer order as the input.
        public static Color32[] SolidHalo(Color32[] src, int w, int h, float screenScale)
        {
            if (src == null || w < 2 || h < 2 || src.Length < w * h) return src;
            int n = w * h;
            var ink = new bool[n];
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                if (src[i].a >= 24)
                {
                    ink[i] = true;
                    count++;
                }
            }
            var dst = new Color32[n];
            if (count < 8)
            {
                for (int i = 0; i < n; i++)
                    if (ink[i]) dst[i] = Brighten(WithAlpha(src[i], 255));
                return dst;
            }

            var compOf = new int[n];
            for (int i = 0; i < n; i++) compOf[i] = -1;
            var comps = new List<List<int>>();
            var stack = new int[count];
            for (int i = 0; i < n; i++)
            {
                if (!ink[i] || compOf[i] >= 0) continue;
                int id = comps.Count;
                var pts = new List<int>();
                int sp = 0;
                stack[sp++] = i;
                compOf[i] = id;
                while (sp > 0)
                {
                    int p = stack[--sp];
                    pts.Add(p);
                    int x = p % w;
                    int y = p / w;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = y + dy;
                        if ((uint)yy >= (uint)h) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int xx = x + dx;
                            if ((uint)xx >= (uint)w) continue;
                            int j = yy * w + xx;
                            if (!ink[j] || compOf[j] >= 0) continue;
                            compOf[j] = id;
                            stack[sp++] = j;
                        }
                    }
                }
                comps.Add(pts);
            }

            for (int i = 0; i < n; i++)
                if (ink[i]) dst[i] = WithAlpha(src[i], 255);

            int stroke = HaloStrokePx(screenScale);
            float link = HaloLinkPx;
            var cents = new Vector2[comps.Count];
            var avg = new Color32[comps.Count];
            for (int c = 0; c < comps.Count; c++)
            {
                long sx = 0, sy = 0, sr = 0, sg = 0, sb = 0;
                var pts = comps[c];
                for (int k = 0; k < pts.Count; k++)
                {
                    int p = pts[k];
                    sx += p % w;
                    sy += p / w;
                    sr += src[p].r;
                    sg += src[p].g;
                    sb += src[p].b;
                }
                float inv = 1f / Mathf.Max(1, pts.Count);
                cents[c] = new Vector2(sx * inv, sy * inv);
                avg[c] = new Color32((byte)(sr * inv), (byte)(sg * inv), (byte)(sb * inv), 255);
            }

            for (int a = 0; a < comps.Count; a++)
            {
                for (int b = a + 1; b < comps.Count; b++)
                {
                    float cd = Vector2.Distance(cents[a], cents[b]);
                    if (cd > 160f) continue;
                    if (SurfaceDistance(comps[a], comps[b], w, link) > link) continue;
                    var col = new Color32(
                        (byte)((avg[a].r + avg[b].r) >> 1),
                        (byte)((avg[a].g + avg[b].g) >> 1),
                        (byte)((avg[a].b + avg[b].b) >> 1),
                        255);
                    Stroke(dst, w, h, cents[a], cents[b], stroke, col);
                }
            }

            // One pixel of growth so a 1px joint between back and front survives scale.
            Dilate1(dst, w, h);
            for (int i = 0; i < n; i++)
                if (dst[i].a > 0) dst[i] = Brighten(dst[i]);
            return dst;
        }

        static Color32 WithAlpha(Color32 p, byte a)
        {
            p.a = a;
            return p;
        }

        static float SurfaceDistance(List<int> a, List<int> b, int w, float limit)
        {
            var small = a.Count <= b.Count ? a : b;
            var large = small == a ? b : a;
            const int cell = 8;
            var buckets = new Dictionary<long, List<int>>(large.Count / 4 + 1);
            for (int i = 0; i < large.Count; i++)
            {
                int p = large[i];
                long key = ((long)((p % w) / cell) << 32) | (uint)((p / w) / cell);
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<int>(4);
                    buckets[key] = list;
                }
                list.Add(p);
            }
            float best2 = (limit + 8f) * (limit + 8f);
            int reach = Mathf.CeilToInt(limit / cell) + 1;
            float limit2 = limit * limit;
            for (int i = 0; i < small.Count; i++)
            {
                int p = small[i];
                int x = p % w;
                int y = p / w;
                int cx = x / cell;
                int cy = y / cell;
                for (int oy = -reach; oy <= reach; oy++)
                {
                    for (int ox = -reach; ox <= reach; ox++)
                    {
                        long key = ((long)(cx + ox) << 32) | (uint)(cy + oy);
                        if (!buckets.TryGetValue(key, out var list)) continue;
                        for (int k = 0; k < list.Count; k++)
                        {
                            int q = list[k];
                            int dx = x - q % w;
                            int dy = y - q / w;
                            float d2 = dx * dx + dy * dy;
                            if (d2 < best2) best2 = d2;
                        }
                    }
                }
                if (best2 <= limit2) return Mathf.Sqrt(best2);
            }
            return Mathf.Sqrt(best2);
        }

        static void Stroke(Color32[] dst, int w, int h, Vector2 a, Vector2 b, int width, Color32 col)
        {
            float rad = Mathf.Max(1f, width * 0.5f);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x) - rad - 1f), 0, w - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x) + rad + 1f), 0, w - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y) - rad - 1f), 0, h - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y) + rad + 1f), 0, h - 1);
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            float len2 = dx * dx + dy * dy;
            if (len2 < 0.01f) len2 = 0.01f;
            float r2 = rad * rad;
            for (int y = y0; y <= y1; y++)
            {
                int row = y * w;
                for (int x = x0; x <= x1; x++)
                {
                    if (dst[row + x].a > 0) continue;
                    float t = ((x - a.x) * dx + (y - a.y) * dy) / len2;
                    if (t < 0f) t = 0f;
                    else if (t > 1f) t = 1f;
                    float px = a.x + t * dx;
                    float py = a.y + t * dy;
                    float ex = x - px;
                    float ey = y - py;
                    if (ex * ex + ey * ey <= r2) dst[row + x] = col;
                }
            }
        }

        static void Dilate1(Color32[] dst, int w, int h)
        {
            var grow = new List<int>();
            var cols = new List<Color32>();
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (dst[row + x].a > 0) continue;
                    Color32 hit = default;
                    bool found = false;
                    for (int dy = -1; dy <= 1 && !found; dy++)
                    {
                        int yy = y + dy;
                        if ((uint)yy >= (uint)h) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int xx = x + dx;
                            if ((uint)xx >= (uint)w) continue;
                            var p = dst[yy * w + xx];
                            if (p.a == 0) continue;
                            hit = p;
                            found = true;
                            break;
                        }
                    }
                    if (!found) continue;
                    grow.Add(row + x);
                    cols.Add(hit);
                }
            }
            for (int i = 0; i < grow.Count; i++) dst[grow[i]] = cols[i];
        }

        // Nearest downsample, then ring thickness along the ellipse normal.
        // GapCount is extra connected components (0 means one solid piece).
        public static RingMeasure MeasureScreen(Color32[] px, int w, int h, float scale)
        {
            var m = new RingMeasure();
            if (px == null || w < 2 || h < 2 || scale < 0.02f) return m;
            int sw = Mathf.Max(1, Mathf.RoundToInt(w * scale));
            int sh = Mathf.Max(1, Mathf.RoundToInt(h * scale));
            var sm = new bool[sw * sh];
            int ink = 0;
            for (int y = 0; y < sh; y++)
            {
                int sy = Mathf.Clamp(Mathf.RoundToInt(y / scale), 0, h - 1);
                int row = y * sw;
                for (int x = 0; x < sw; x++)
                {
                    int sx = Mathf.Clamp(Mathf.RoundToInt(x / scale), 0, w - 1);
                    if (px[sy * w + sx].a < 24) continue;
                    sm[row + x] = true;
                    ink++;
                }
            }
            m.Ink = ink;
            if (ink < 4) return m;
            m.Components = CountComponents(sm, sw, sh);
            m.GapCount = Mathf.Max(0, m.Components - 1);
            int minX = sw, minY = sh, maxX = 0, maxY = 0;
            for (int y = 0; y < sh; y++)
            {
                int row = y * sw;
                for (int x = 0; x < sw; x++)
                {
                    if (!sm[row + x]) continue;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;
            float rx = Mathf.Max(1f, (maxX - minX) * 0.5f);
            float ry = Mathf.Max(1f, (maxY - minY) * 0.5f);
            // Stars pull the box out past the ring. The ray has to reach the stroke,
            // or a tip nick inside a short window is counted as the thickness.
            int reach = Mathf.CeilToInt(Mathf.Max(rx, ry)) + 4;
            var thick = new List<int>(180);
            for (int deg = 0; deg < 360; deg += 2)
            {
                float t = deg * Mathf.Deg2Rad;
                float ct = Mathf.Cos(t);
                float st = Mathf.Sin(t);
                float ex = cx + rx * ct;
                float ey = cy + ry * st;
                float nx = ry * ct;
                float ny = rx * st;
                float ln = Mathf.Sqrt(nx * nx + ny * ny);
                if (ln < 0.001f) continue;
                nx /= ln;
                ny /= ln;
                int run = 0, best = 0;
                for (int s = -reach; s <= reach; s++)
                {
                    int x = Mathf.RoundToInt(ex + nx * s);
                    int y = Mathf.RoundToInt(ey + ny * s);
                    bool hit = (uint)x < (uint)sw && (uint)y < (uint)sh && sm[y * sw + x];
                    if (hit)
                    {
                        run++;
                        if (run > best) best = run;
                    }
                    else run = 0;
                }
                if (best >= 1) thick.Add(best);
            }
            if (thick.Count == 0) return m;
            // One angle can nick a star on the box and read 1 even though the
            // ring on either side is thick. The ring thickness is the lowest
            // 3-angle median, so a single tip does not count as the stroke.
            int robust = int.MaxValue;
            for (int i = 0; i < thick.Count; i++)
            {
                int a = thick[(i + thick.Count - 1) % thick.Count];
                int b = thick[i];
                int c = thick[(i + 1) % thick.Count];
                int hi = a > b ? a : b;
                if (c > hi) hi = c;
                int lo = a < b ? a : b;
                if (c < lo) lo = c;
                int med = a + b + c - hi - lo;
                if (med < robust) robust = med;
            }
            thick.Sort();
            m.MinThickness = robust;
            m.P10Thickness = thick[Mathf.Clamp(thick.Count / 10, 0, thick.Count - 1)];
            return m;
        }

        static int CountComponents(bool[] sm, int sw, int sh)
        {
            var seen = new bool[sm.Length];
            int n = 0;
            var stack = new int[sm.Length];
            for (int i = 0; i < sm.Length; i++)
            {
                if (!sm[i] || seen[i]) continue;
                n++;
                int sp = 0;
                stack[sp++] = i;
                seen[i] = true;
                while (sp > 0)
                {
                    int p = stack[--sp];
                    int x = p % sw;
                    int y = p / sw;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = y + dy;
                        if ((uint)yy >= (uint)sh) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int xx = x + dx;
                            if ((uint)xx >= (uint)sw) continue;
                            int j = yy * sw + xx;
                            if (!sm[j] || seen[j]) continue;
                            seen[j] = true;
                            stack[sp++] = j;
                        }
                    }
                }
            }
            return n;
        }

        // Three honey smears. Same anchors on scuffed, bruised and wrecked, so they
        // follow the pose. Width is screen pixels. Fresh draws none. No red.
        public const int HoneyStreakCount = 3;
        public static readonly Color32 HoneyInk = new Color32(236, 186, 24, 255);

        public readonly struct HoneyMark
        {
            public readonly float U;
            public readonly float V;
            public readonly float AngleDeg;
            public readonly float LengthPx;
            public readonly int WidthPx;

            public HoneyMark(float u, float v, float angleDeg, float lengthPx, int widthPx)
            {
                U = u;
                V = v;
                AngleDeg = angleDeg;
                LengthPx = lengthPx;
                WidthPx = widthPx;
            }
        }

        static readonly HoneyMark[] HoneyMarks =
        {
            new HoneyMark(0.42f, 0.38f, -34f, 22f, 2),
            new HoneyMark(0.60f, 0.50f, 22f, 26f, 3),
            new HoneyMark(0.46f, 0.62f, -8f, 18f, 2),
        };

        public static bool HoneyMarksOn(BadgerWear wear) => (int)wear >= (int)BadgerWear.Scuffed;

        public static HoneyMark HoneyMarkAt(int index)
        {
            if (index < 0) index = 0;
            if (index >= HoneyMarks.Length) index = HoneyMarks.Length - 1;
            return HoneyMarks[index];
        }

        public static bool HoneyMarkPixel(Color32 p)
        {
            if (p.a < 200) return false;
            if (p.b > 40) return false;
            if (p.r < 210 || p.g < 150) return false;
            if (p.r > p.g + 70) return false;
            return BadgerPolish.YellowStreak(p.r / 255f, p.g / 255f, p.b / 255f);
        }

        public struct StreakMeasure
        {
            public int Count;
            public int[] Widths;
        }

        // y grows down for the plot (the stills board and GUI). allow drops pixels
        // outside the fresh silhouette. Returns pixels accepted.
        public static int StampHoney(Rect unpadded, float pivotX, float pivotY, float deg, BadgerWear wear,
            Action<int, int> plot, Func<int, int, bool> allow)
        {
            if (!HoneyMarksOn(wear) || plot == null || unpadded.width < 2f || unpadded.height < 2f) return 0;
            float rad = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            int n = 0;
            for (int i = 0; i < HoneyMarks.Length; i++)
            {
                var mark = HoneyMarks[i];
                float lx = unpadded.x + mark.U * unpadded.width;
                float ly = unpadded.y + mark.V * unpadded.height;
                float dx = lx - pivotX;
                float dy = ly - pivotY;
                float cx = pivotX + dx * c - dy * s;
                float cy = pivotY + dx * s + dy * c;
                float ang = (mark.AngleDeg + deg) * Mathf.Deg2Rad;
                float dirX = Mathf.Cos(ang);
                float dirY = Mathf.Sin(ang);
                // Step the long axis by one pixel and stamp the width on the short
                // axis, so every station is exactly 2 or 3 px. A diagonal floor()
                // stair leaves some stations 1 px wide.
                bool horiz = Mathf.Abs(dirX) >= Mathf.Abs(dirY);
                int stepX = dirX >= 0f ? 1 : -1;
                int stepY = dirY >= 0f ? 1 : -1;
                float slope = horiz
                    ? (Mathf.Abs(dirX) < 0.001f ? 0f : Mathf.Abs(dirY / dirX))
                    : (Mathf.Abs(dirY) < 0.001f ? 0f : Mathf.Abs(dirX / dirY));
                int width = mark.WidthPx;
                if (width < 2) width = 2;
                if (width > 3) width = 3;
                // Length is tuned for the 1x body (~230 px). On the small duel
                // fighter the same pixel length would overlap the three marks.
                float lenPx = mark.LengthPx;
                if (unpadded.width < 160f)
                    lenPx *= Mathf.Max(0.22f, unpadded.width / 230f);
                int len = Mathf.Max(width + 1, Mathf.RoundToInt(lenPx));
                int mid = (width - 1) / 2;
                int gx = 0, gy = 0;
                float acc = 0f;
                int sumX = 0, sumY = 0;
                for (int t = 0; t < len; t++)
                {
                    sumX += gx;
                    sumY += gy;
                    if (horiz)
                    {
                        gx += stepX;
                        acc += slope;
                        if (acc >= 0.5f) { gy += stepY; acc -= 1f; }
                    }
                    else
                    {
                        gy += stepY;
                        acc += slope;
                        if (acc >= 0.5f) { gx += stepX; acc -= 1f; }
                    }
                }
                int shiftX = Mathf.RoundToInt(cx - sumX / (float)len);
                int shiftY = Mathf.RoundToInt(cy - sumY / (float)len);
                gx = 0;
                gy = 0;
                acc = 0f;
                for (int t = 0; t < len; t++)
                {
                    int ix = gx + shiftX;
                    int iy = gy + shiftY;
                    for (int k = 0; k < width; k++)
                    {
                        int off = k - mid;
                        int x = horiz ? ix : ix + off;
                        int y = horiz ? iy + off : iy;
                        if (allow != null && !allow(x, y)) continue;
                        plot(x, y);
                        n++;
                    }
                    if (horiz)
                    {
                        gx += stepX;
                        acc += slope;
                        if (acc >= 0.5f) { gy += stepY; acc -= 1f; }
                    }
                    else
                    {
                        gy += stepY;
                        acc += slope;
                        if (acc >= 0.5f) { gx += stepX; acc -= 1f; }
                    }
                }
            }
            return n;
        }

        public static StreakMeasure MeasureStreaks(Color32[] px, int w, int h)
        {
            var m = new StreakMeasure { Widths = new int[0] };
            if (px == null || w < 2 || h < 2 || px.Length < w * h) return m;
            var ink = new bool[w * h];
            for (int i = 0; i < ink.Length; i++) ink[i] = HoneyMarkPixel(px[i]);
            var widths = new List<int>();
            var seen = new bool[ink.Length];
            var stack = new int[ink.Length];
            for (int i = 0; i < ink.Length; i++)
            {
                if (!ink[i] || seen[i]) continue;
                int sp = 0;
                stack[sp++] = i;
                seen[i] = true;
                var pts = new List<int>();
                while (sp > 0)
                {
                    int p = stack[--sp];
                    pts.Add(p);
                    int x = p % w;
                    int y = p / w;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = y + dy;
                        if ((uint)yy >= (uint)h) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int xx = x + dx;
                            if ((uint)xx >= (uint)w) continue;
                            int j = yy * w + xx;
                            if (!ink[j] || seen[j]) continue;
                            seen[j] = true;
                            stack[sp++] = j;
                        }
                    }
                }
                if (pts.Count < 4) continue;
                widths.Add(StreakWidth(pts, w));
            }
            m.Count = widths.Count;
            m.Widths = widths.ToArray();
            return m;
        }

        // Median station along the long screen axis. The mark is stamped 2 or 3
        // pixels on the short axis, so that station width is the on-screen width.
        // A rotated minor-axis bin splits those pixels and reports 1.
        static int StreakWidth(List<int> pts, int w)
        {
            float mx = 0f, my = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                mx += pts[i] % w;
                my += pts[i] / w;
            }
            mx /= pts.Count;
            my /= pts.Count;
            float cxx = 0f, cyy = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                float dx = pts[i] % w - mx;
                float dy = pts[i] / w - my;
                cxx += dx * dx;
                cyy += dy * dy;
            }
            bool alongX = cxx >= cyy;
            var lo = new Dictionary<int, int>();
            var hi = new Dictionary<int, int>();
            for (int i = 0; i < pts.Count; i++)
            {
                int x = pts[i] % w;
                int y = pts[i] / w;
                int key = alongX ? x : y;
                int val = alongX ? y : x;
                if (!lo.TryGetValue(key, out int a))
                {
                    lo[key] = val;
                    hi[key] = val;
                }
                else
                {
                    if (val < a) lo[key] = val;
                    if (val > hi[key]) hi[key] = val;
                }
            }
            var widths = new List<int>(lo.Count);
            foreach (var kv in lo)
                widths.Add(hi[kv.Key] - kv.Value + 1);
            if (widths.Count == 0) return 1;
            widths.Sort();
            return Mathf.Max(1, widths[widths.Count / 2]);
        }

        // True when the unpadded UV sits on the fresh pose's opaque body.
        public static bool FreshCovers(string frame, float u, float v)
        {
            if (!TryArt(StageKey(frame, BadgerWear.Fresh), out var px, out int w, out int h)) return true;
            if (u < 0f || v < 0f || u > 1f || v > 1f) return false;
            int srcW = w - PadW;
            int srcH = h - PadTop;
            if (srcW < 2 || srcH < 2) return false;
            int x = Mathf.RoundToInt(PadX + u * srcW);
            int yTop = Mathf.RoundToInt(PadTop + v * srcH);
            if ((uint)x >= (uint)w || (uint)yTop >= (uint)h) return false;
            return px[(h - 1 - yTop) * w + x].a > 200;
        }

        public static bool TryArt(string name, out Color32[] px, out int w, out int h)
        {
            px = null;
            w = 0;
            h = 0;
            if (string.IsNullOrEmpty(name)) return false;
            if (_art.TryGetValue(name, out var img) && img.Px != null)
            {
                px = img.Px;
                w = img.W;
                h = img.H;
                return true;
            }
            string path = Path.Combine(Application.dataPath, "_Project/Art/Resources/Sprites/Badger", name + ".png");
            if (!File.Exists(path)) return false;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!tex.LoadImage(File.ReadAllBytes(path))) return false;
                img = new ArtImg { Px = tex.GetPixels32(), W = tex.width, H = tex.height };
            }
            finally
            {
                if (tex != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(tex);
                    else UnityEngine.Object.DestroyImmediate(tex);
                }
            }
            _art[name] = img;
            px = img.Px;
            w = img.W;
            h = img.H;
            return px != null;
        }

        public static Texture2D SolidHaloTexture(string name, float destW, float destH)
        {
            if (string.IsNullOrEmpty(name) || destW < 2f || destH < 2f) return null;
            if (!TryArt(name, out var px, out int w, out int h)) return null;
            float scale = destW / Mathf.Max(1, w);
            int stroke = HaloStrokePx(scale);
            string key = name + "@" + stroke;
            if (_haloTex.TryGetValue(key, out var cached) && cached != null) return cached;
            var solid = SolidHalo(px, w, h, scale);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(solid);
            tex.Apply(false, false);
            _haloTex[key] = tex;
            return tex;
        }

        // Screen-space honey overlay for the unpadded body. Cached per pose and size.
        // Scuffed, bruised and wrecked share it. y in the texture matches GUI (top-down plot).
        public static Texture2D HoneyTexture(string frame, int tw, int th)
        {
            if (tw < 2 || th < 2 || string.IsNullOrEmpty(frame)) return null;
            string key = frame + "@" + tw + "x" + th;
            if (_honeyTex.TryGetValue(key, out var cached) && cached != null) return cached;
            var px = new Color32[tw * th];
            var rect = new Rect(0f, 0f, tw, th);
            StampHoney(rect, tw * 0.5f, th * 0.5f, 0f, BadgerWear.Scuffed,
                (x, y) =>
                {
                    if ((uint)x >= (uint)tw || (uint)y >= (uint)th) return;
                    px[(th - 1 - y) * tw + x] = HoneyInk;
                },
                (x, y) =>
                {
                    if ((uint)x >= (uint)tw || (uint)y >= (uint)th) return false;
                    return FreshCovers(frame, (x + 0.5f) / tw, (y + 0.5f) / th);
                });
            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _honeyTex[key] = tex;
            return tex;
        }

        sealed class ArtImg
        {
            public Color32[] Px;
            public int W, H;
        }

        static readonly Dictionary<string, ArtImg> _art = new Dictionary<string, ArtImg>();
        static readonly Dictionary<string, Texture2D> _haloTex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> _honeyTex = new Dictionary<string, Texture2D>();
    }
}

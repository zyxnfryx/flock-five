using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // v4_final wear sprites. One helper for the duel drawer and the stills painter.
    // Stage PNGs already composite halo_back, body, halo_front. Never blit the
    // halo files on top of those (double halo). Padding is +70 left/right, +110 top.
    public static class BadgerWearArt
    {
        public const int PadX = 70;
        public const int PadTop = 110;
        public const int PadW = PadX * 2;

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

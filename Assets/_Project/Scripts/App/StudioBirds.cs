using UnityEngine;

namespace FlockFive
{
    // Birds v5 (Brandon approved 10:01 PM PT, Docs/birds-sync.md): five flock birds on a 3D halo
    // ring around the ZFX logo, then a fan-out break-out at 1.70-1.86s ahead of the 2.000s pop.
    // Port of the producer's birds_anim.py. All coordinates are the 1080x1920 mockup; the mockup
    // logo sits at (LogoOx, LogoOy) size LogoW x LogoH, so a point maps into the card through the
    // real logo rect (StudioLogoRect). Driven by the card clock (the jingle's audio time).
    public static class StudioBirds
    {
        public const float MockLogoW = 960f;
        public const float MockLogoH = 461f;          // 960 * 1200/2500
        public const float LogoOx = 60f;              // (1080-960)/2
        public const float LogoOy = 609f;             // (1920-461)/2 - 120
        public const float RingCx = 540f, RingCy = 812f, RingA = 470f, RingB = 225f, RingRot = -15f;
        public const float Omega = 3.30f, TScatter = 1.70f, Lag = 0.04f, FadeIn = 0.12f, TGone = 3.0f;
        public const float FlapRate = 1.25f, ScatterGrow = 1.30f;
        public static readonly int[] Flap6 = { 3, 1, 4, 2, 4, 1 };
        public static readonly string[] Prefix = { "bird_ruby", "bird_gold", "bird_teal", "bird_violet", "bird_peach_f" };
        static readonly float[] BaseScale = { 0.080f, 0.075f, 0.077f, 0.073f, 0.075f };
        static readonly float[] Phase = { 0.00f, 0.37f, 0.74f, 1.11f, 1.48f };
        static readonly float[] ExitT = { 1.20f, 1.20f, 1.18f, 1.15f, 1.12f };
        static readonly Vector2[,] Ctrl =
        {
            // Brandon 10:42 PM: longer burst, every bird flies fully off screen (tall phones too:
            // a 1179x2556 card reaches mock y~2330 at the bottom, x~-160 / ~1240 at the sides).
            { new Vector2(80, 1150), new Vector2(40, 1500), new Vector2(-380, 1760) },
            { new Vector2(260, 1260), new Vector2(330, 1750), new Vector2(320, 2620) },
            { new Vector2(520, 1320), new Vector2(820, 1800), new Vector2(940, 2600) },
            { new Vector2(700, 1250), new Vector2(1120, 1350), new Vector2(1500, 1520) },
            { new Vector2(740, 1160), new Vector2(1040, 1130), new Vector2(1520, 1150) },
        };
        public const int Count = 5;
        const float TH1 = 2.5f * Mathf.PI;

        public struct State
        {
            public bool On;
            public Vector2 Pos;   // mockup px
            public int Pose;      // 1..4
            public float Face, Tilt, Scale, Depth, Alpha;
            public bool Behind => Depth < 0f;
        }

        public static float BreakOut(int b) => TScatter + b * Lag;

        static Vector2 Ring(float th, out float depth)
        {
            float u = RingA * Mathf.Cos(th), v = RingB * Mathf.Sin(th);
            float rr = -RingRot * Mathf.Deg2Rad, cr = Mathf.Cos(rr), sr = Mathf.Sin(rr);
            depth = Mathf.Sin(th);
            return new Vector2(RingCx + u * cr + v * sr, RingCy - u * sr + v * cr);
        }

        static Vector2 Orbit(int b, float t, out Vector2 vel, out float depth)
        {
            float th = TH1 - Omega * (BreakOut(b) - t);
            var p = Ring(th, out depth);
            var p2 = Ring(th + 1e-3f, out _);
            vel = (p2 - p) / 1e-3f * Omega;
            return p;
        }

        static Vector2 Ctl(int b, int i, out Vector2 d)
        {
            // P0, P1 = P0 + v*0.12, then the three listed controls.
            float tb = BreakOut(b);
            var p0 = Orbit(b, tb, out var v, out _);
            d = Vector2.zero;
            if (i == 0) return p0;
            if (i == 1) return p0 + v * 0.12f;
            return Ctrl[b, i - 2];
        }

        static readonly int[] Binom4 = { 1, 4, 6, 4, 1 };
        static readonly int[] Binom3 = { 1, 3, 3, 1 };

        public static State At(int b, float t)
        {
            var s = new State();
            if (t < 0f || t > TGone) return s;
            Vector2 p, vel;
            float d, k = 1f;
            float tb = BreakOut(b);
            if (t < tb)
            {
                p = Orbit(b, t, out vel, out d);
            }
            else
            {
                float T = ExitT[b];
                float u = (t - tb) / T;
                if (u >= 1f) return s;
                var P = new Vector2[5];
                for (int i = 0; i < 5; i++) P[i] = Ctl(b, i, out _);
                p = Vector2.zero; var dv = Vector2.zero;
                for (int i = 0; i <= 4; i++) p += Binom4[i] * Mathf.Pow(1f - u, 4 - i) * Mathf.Pow(u, i) * P[i];
                for (int i = 0; i <= 3; i++) dv += Binom3[i] * Mathf.Pow(1f - u, 3 - i) * Mathf.Pow(u, i) * (P[i + 1] - P[i]);
                dv *= 4f;
                vel = dv / T; d = 1f;
                k = 1f + (ScatterGrow - 1f) * Mathf.Sin(Mathf.PI / 2f * Mathf.Min(u * 1.6f, 1f));
            }
            p.y += 4f * Mathf.Sin(2f * Mathf.PI * 1.6f * t + Phase[b] * 3f);
            int step = Mathf.FloorToInt((t * FlapRate + Phase[b]) * 24f);
            s.Pose = Flap6[((step % 6) + 6) % 6];
            s.Face = vel.x >= 0f ? 1f : -1f;
            s.Tilt = Mathf.Clamp(Mathf.Atan2(-vel.y, Mathf.Abs(vel.x) + 1e-6f) * Mathf.Rad2Deg, -60f, 60f);
            float ds = Mathf.Lerp(0.75f, 1.15f, (d + 1f) * 0.5f);
            s.Scale = BaseScale[b] * ds * k;
            s.Depth = d;
            // No card-fade dimming: they leave by flying off the edges, not by fading.
            s.Alpha = Mathf.Clamp01((t - b * Lag) / FadeIn);
            s.Pos = p;
            s.On = true;
            return s;
        }

        // Mockup px -> card px through the real logo rect.
        public static Vector2 ToCard(Vector2 mock, Rect logo) =>
            new Vector2(logo.x + (mock.x - LogoOx) / MockLogoW * logo.width,
                        logo.y + (mock.y - LogoOy) / MockLogoH * logo.height);

        public static float SourcePx(int pose) => pose == 2 ? 1536f : 1504f;

        // Shared garden art path: the same SpriteCatalog wingbeat sheets the garden birds fly.
        public static readonly BirdColor[] Colors = { BirdColor.Ruby, BirdColor.Gold, BirdColor.Teal, BirdColor.Violet, BirdColor.Peach };
        public static Sprite Sprite(int b, int pose) => SpriteCatalog.BirdPose(Colors[b], pose);

        public static void Warm()
        {
            for (int b = 0; b < Count; b++)
                for (int p = 1; p <= 4; p++) Sprite(b, p);
        }

        // GUI draw for one layer (back = under the logo). Back-arc birds are paler: on the white
        // card, drawing at (1 - 0.30*|d|) alpha is the mockup's 30%*|d| blend toward white.
        public static void Draw(Rect logo, float t, bool back, float cardA)
        {
            var order = new int[Count];
            var st = new State[Count];
            int n = 0;
            for (int b = 0; b < Count; b++)
            {
                st[b] = At(b, t);
                if (!st[b].On || st[b].Behind != back) continue;
                order[n++] = b;
            }
            System.Array.Sort(order, 0, n, System.Collections.Generic.Comparer<int>.Create((x, y) => st[x].Depth.CompareTo(st[y].Depth)));
            var keepM = GUI.matrix;
            var keepC = GUI.color;
            float k = logo.width / MockLogoW;
            for (int i = 0; i < n; i++)
            {
                int b = order[i];
                var s = st[b];
                var spr = Sprite(b, s.Pose);
                if (spr == null || spr.texture == null) continue;
                float size = SourcePx(s.Pose) * s.Scale * k;
                var c = ToCard(s.Pos, logo);
                float pale = s.Behind ? 1f - 0.30f * -s.Depth : 1f;
                float al = s.Alpha * cardA * pale;
                if (al < 0.01f) continue;
                GUI.matrix = keepM;
                GUIUtility.RotateAroundPivot(-s.Tilt * s.Face, c);
                var tex = spr.texture;
                var tr = spr.textureRect;
                var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                if (s.Face < 0f) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
                GUI.color = new Color(1f, 1f, 1f, al);
                GUI.DrawTextureWithTexCoords(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), tex, uv, true);
            }
            GUI.matrix = keepM;
            GUI.color = keepC;
        }
    }
}

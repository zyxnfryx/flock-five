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
        public const float Omega = 3.30f, TScatter = 1.62f, Lag = Wow.ComboStagger, FadeIn = 0.12f, TGone = 3.0f;
        public const float FlapRate = 1.25f, ScatterGrow = 1.30f;
        public static readonly int[] Flap6 = { 3, 1, 4, 2, 4, 1 };
        // Brandon 12:20 AM: no Christmas read. No ruby, so no red+green neighbours; varied order.
        public static readonly string[] Prefix = { "bird_violet", "bird_teal", "bird_gold", "bird_violet", "bird_peach_f" };
        static readonly float[] BaseScale = { 0.077f, 0.077f, 0.077f, 0.077f, 0.077f }; // one size: an ordered flock
        static readonly float[] Phase = { 0.00f, 0.37f, 0.74f, 1.11f, 1.48f };
        // Combo-style cascade: break-outs every ComboStagger (1.62-1.99s, all before the 2.000s pop)
        // and one burst length, so exits land on the same spacing (about 2.55-2.92s).
        static readonly float[] ExitT = { 0.93f, 0.93f, 0.93f, 0.93f, 0.93f };
        // Ordered fan: five evenly spaced headings from down-left to right, same curve shape,
        // ending well past the edges on tall phones.
        static readonly Vector2[,] Ctrl =
        {
            { new Vector2(120, 1180), new Vector2(-40, 1500), new Vector2(-420, 1900) },
            { new Vector2(330, 1260), new Vector2(300, 1800), new Vector2(200, 2650) },
            { new Vector2(560, 1300), new Vector2(760, 1850), new Vector2(980, 2650) },
            { new Vector2(760, 1250), new Vector2(1130, 1500), new Vector2(1550, 1900) },
            { new Vector2(820, 1150), new Vector2(1150, 1150), new Vector2(1560, 1180) },
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
        public static readonly BirdColor[] Colors = { BirdColor.Violet, BirdColor.Teal, BirdColor.Gold, BirdColor.Violet, BirdColor.Peach };
        public static Sprite Sprite(int b, int pose) => SpriteCatalog.BirdPose(Colors[b], pose);

        // ROOT CAUSE of the flat "paper" birds (b83 v5 card): the 1504px bird sheets have no
        // mipmaps (SpriteImport policy) and the card drew them straight into ~110-180px with
        // bilinear sampling, a 9-14x minify that skips most texels, so the feather shading
        // aliased into flat facets. The mocks were Lanczos-downsampled. Fix: once, on the dark
        // preload screen, each pose is box-filtered on the GPU (2:1 bilinear halvings) into a
        // 256px trilinear mipmapped copy, and the card draws that. Same art, same proportions.
        public const int CardTexPx = 384; // ~2x the largest on-card bird (~180px)
        static Texture2D[,] _col, _white;

        // Colour copy (and a white silhouette of it) per pose, 256px, mipmapped, trilinear.
        public static Texture CardTex(int b, int pose, out Rect uv) { uv = new Rect(0f, 0f, 1f, 1f); Build(b, pose); return _col[b, pose]; }
        public static Texture CardWhite(int b, int pose) { Build(b, pose); return _white[b, pose]; }

        static void Build(int b, int pose)
        {
            if (_col == null) { _col = new Texture2D[Count, 5]; _white = new Texture2D[Count, 5]; }
            if (_col[b, pose] != null) return;
            var spr = Sprite(b, pose);
            if (spr == null || spr.texture == null) return;
            var src = spr.texture;
            var prevActive = RenderTexture.active;
            Texture cur = src;
            int w = src.width, h = src.height;
            RenderTexture tmp = null;
            while (w / 2 >= CardTexPx * 1.4f)
            {
                w /= 2; h /= 2;
                var nx = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                nx.filterMode = FilterMode.Bilinear;
                Graphics.Blit(cur, nx);
                if (tmp != null) RenderTexture.ReleaseTemporary(tmp);
                tmp = nx; cur = nx;
            }
            var fin = RenderTexture.GetTemporary(CardTexPx, CardTexPx, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            fin.filterMode = FilterMode.Bilinear;
            Graphics.Blit(cur, fin);
            if (tmp != null) RenderTexture.ReleaseTemporary(tmp);
            RenderTexture.active = fin;
            var col = new Texture2D(CardTexPx, CardTexPx, TextureFormat.RGBA32, true, false)
            { name = "StudioBird" + b + "_" + pose, filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, mipMapBias = -0.6f, anisoLevel = 2 };
            col.ReadPixels(new Rect(0, 0, CardTexPx, CardTexPx), 0, 0, false);
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(fin);
            var px = col.GetPixels32();
            col.Apply(true, false);
            for (int i = 0; i < px.Length; i++) { px[i].r = 255; px[i].g = 255; px[i].b = 255; }
            var wt = new Texture2D(CardTexPx, CardTexPx, TextureFormat.RGBA32, true, false)
            { name = "StudioBirdW" + b + "_" + pose, filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, mipMapBias = -0.6f };
            wt.SetPixels32(px);
            wt.Apply(true, true);
            col.Apply(true, true);
            _col[b, pose] = col;
            _white[b, pose] = wt;
        }

        // One pose per call (20 total), run on the dark preload screen before the card.
        static int _warmI;
        public static bool WarmStep()
        {
            if (_warmI >= Count * 4) return true;
            int b = _warmI / 4, p = _warmI % 4 + 1;
            CardTex(b, p, out _);
            _warmI++;
            return _warmI >= Count * 4;
        }

        public static void Warm()
        {
            while (!WarmStep()) { }
        }

        // GUI draw for one layer (back = under the logo). Back-arc birds are paler like the mock:
        // the bird stays OPAQUE and its colour is blended 30%*|d| toward white by a white
        // silhouette laid over it. (v5 faded alpha instead, so overlapping birds and the logo
        // showed through each other: the see-through "paper/tissue" look Brandon flagged.)
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
                var tex = CardTex(b, s.Pose, out var uv);
                if (tex == null) continue;
                float size = SourcePx(s.Pose) * s.Scale * k;
                var c = ToCard(s.Pos, logo);
                float pale = s.Behind ? 0.30f * -s.Depth : 0f;
                float al = s.Alpha * cardA;
                if (al < 0.01f) continue;
                GUI.matrix = keepM;
                GUIUtility.RotateAroundPivot(-s.Tilt * s.Face, c);
                if (s.Face < 0f) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
                var dst = new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size);
                GUI.color = new Color(1f, 1f, 1f, al);
                GUI.DrawTextureWithTexCoords(dst, tex, uv, true);
                if (pale > 0.005f)
                {
                    var wt = CardWhite(b, s.Pose);
                    if (wt != null)
                    {
                        GUI.color = new Color(1f, 1f, 1f, al * pale);
                        GUI.DrawTextureWithTexCoords(dst, wt, uv, true);
                    }
                }
            }
            GUI.matrix = keepM;
            GUI.color = keepC;
        }
    }
}

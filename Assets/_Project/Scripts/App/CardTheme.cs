using UnityEngine;

namespace FlockFive
{
    // Face theme: the parametric art behind a card's words. BeeKind.Theme names one;
    // Auto picks a bright family theme from the kind's tint (or a hand assignment below),
    // so no card is ever the flat cream plate again. The painter lives in CardArt.
    public enum FaceTheme
    {
        Auto = 0,
        Starburst = 1,   // night-to-gold, radial rays, halo ring, sparkles
        Comet = 2,       // star field, diagonal comet streak, lens-flare ghosts
        Supernova = 3,   // hot core, prismatic shockwave rings, prismatic foil edge
        HoneyGlow = 4,   // family themes (bright, tint-mixed)
        Garden = 5,
        Sky = 6,
        Twilight = 7,
        Ember = 8,
        Moonlit = 9,
    }

    public enum FaceMotif { Glow = 0, Rays = 1, Comet = 2, Ring = 3 }

    public struct FaceStyle
    {
        public FaceMotif Motif;
        // Four gradient stops, top to bottom (Ring: centre to edge).
        public Color S0, S1, S2, S3;
        public Color Glow;          // bee halo + comb glow
        public Color Ink;           // title / flavor fill
        public Color InkOuter;      // outer rim of the lettering
        public Color InkInner;      // inner rim of the lettering
        public Color CombRim;       // brass-honey rim for the comb row
        public int Sparks;
        public int Rays;
        public bool Night;          // dark ground: lettering is light-on-dark and a scrim is laid under it
    }

    public static class FaceThemes
    {
        static readonly Color Cream = new Color(1f, 0.97f, 0.86f, 1f);

        public static FaceStyle Of(FaceTheme t, Color tint)
        {
            switch (t)
            {
                case FaceTheme.Starburst:
                    return Night(FaceMotif.Rays,
                        new Color(0.03f, 0.04f, 0.20f), new Color(0.30f, 0.10f, 0.45f), new Color(0.80f, 0.30f, 0.12f), new Color(0.95f, 0.62f, 0.20f),
                        new Color(1f, 0.78f, 0.25f), new Color(0.72f, 0.46f, 0.13f), new Color(0.13f, 0.05f, 0.17f), 46, 18);
                case FaceTheme.Comet:
                    return Night(FaceMotif.Comet,
                        new Color(0.02f, 0.03f, 0.14f), new Color(0.18f, 0.07f, 0.34f), new Color(0.62f, 0.22f, 0.36f), new Color(0.98f, 0.55f, 0.26f),
                        new Color(1f, 0.62f, 0.35f), new Color(0.62f, 0.33f, 0.22f), new Color(0.10f, 0.04f, 0.16f), 70, 0);
                case FaceTheme.Supernova:
                    return Night(FaceMotif.Ring,
                        new Color(1f, 0.97f, 0.82f), new Color(1f, 0.66f, 0.20f), new Color(0.72f, 0.14f, 0.42f), new Color(0.07f, 0.02f, 0.18f),
                        new Color(1f, 0.55f, 0.40f), new Color(0.66f, 0.36f, 0.24f), new Color(0.12f, 0.03f, 0.16f), 40, 0);
                case FaceTheme.Garden:
                    return Bright(tint, new Color(0.92f, 1f, 0.86f), new Color(0.62f, 0.90f, 0.55f), new Color(0.30f, 0.66f, 0.38f), new Color(1f, 0.95f, 0.55f));
                case FaceTheme.Sky:
                    return Bright(tint, new Color(0.88f, 0.97f, 1f), new Color(0.60f, 0.82f, 0.98f), new Color(0.38f, 0.56f, 0.90f), new Color(1f, 0.92f, 0.70f));
                case FaceTheme.Twilight:
                    return Bright(tint, new Color(0.97f, 0.90f, 1f), new Color(0.78f, 0.62f, 0.95f), new Color(0.52f, 0.34f, 0.80f), new Color(1f, 0.80f, 0.90f));
                case FaceTheme.Ember:
                    return Bright(tint, new Color(1f, 0.93f, 0.78f), new Color(1f, 0.72f, 0.42f), new Color(0.90f, 0.38f, 0.22f), new Color(1f, 0.86f, 0.45f));
                case FaceTheme.Moonlit:
                    return Bright(tint, new Color(0.96f, 0.96f, 1f), new Color(0.80f, 0.82f, 0.96f), new Color(0.58f, 0.62f, 0.84f), new Color(1f, 0.97f, 0.80f));
                default:   // HoneyGlow (and the fallback)
                    return Bright(tint, new Color(1f, 0.98f, 0.80f), new Color(1f, 0.88f, 0.48f), new Color(0.96f, 0.68f, 0.22f), new Color(1f, 0.96f, 0.70f));
            }
        }

        static FaceStyle Night(FaceMotif m, Color s0, Color s1, Color s2, Color s3, Color glow, Color comb, Color line, int sparks, int rays)
        {
            return new FaceStyle
            {
                Motif = m, S0 = s0, S1 = s1, S2 = s2, S3 = s3, Glow = glow, Ink = Cream,
                InkOuter = line, InkInner = line, CombRim = comb, Sparks = sparks, Rays = rays, Night = true,
            };
        }

        // Light, warm, tint-mixed grounds. Dark ink with the old white-and-black rim.
        static FaceStyle Bright(Color tint, Color a, Color b, Color c, Color glow)
        {
            const float k = 0.30f;
            Color Mix(Color x) => Color.Lerp(x, new Color(tint.r, tint.g, tint.b, 1f), k);
            return new FaceStyle
            {
                Motif = FaceMotif.Glow, S0 = Mix(a), S1 = Mix(b), S2 = Mix(c), S3 = Mix(Color.Lerp(b, a, 0.4f)),
                Glow = glow, Ink = new Color(0.16f, 0.07f, 0.02f, 1f),
                InkOuter = new Color(0.02f, 0.02f, 0.02f, 1f), InkInner = Color.white,
                CombRim = new Color(0.84f, 0.54f, 0.14f, 1f), Sparks = 14, Rays = 0, Night = false,
            };
        }

        // Hand-assigned showpieces; everything else follows its colour family.
        public static FaceTheme Resolve(BeeKind kind)
        {
            if (kind.Theme != FaceTheme.Auto) return kind.Theme;
            switch (kind.Id)
            {
                case "nova": case "lunar": return FaceTheme.Supernova;
                case "wow": case "sunflower": case "harvest": case "topaz": return FaceTheme.Starburst;
                case "comet": case "firefly": case "thunder": case "orbit": return FaceTheme.Comet;
            }
            return Family(kind.Tint);
        }

        public static FaceTheme Family(Color tint)
        {
            Color.RGBToHSV(tint, out float h, out float s, out float v);
            if (s < 0.16f) return FaceTheme.Moonlit;
            if (h < 0.045f || h > 0.94f) return FaceTheme.Ember;
            if (h < 0.115f) return FaceTheme.Ember;
            if (h < 0.19f) return FaceTheme.HoneyGlow;
            if (h < 0.46f) return FaceTheme.Garden;
            if (h < 0.70f) return FaceTheme.Sky;
            return FaceTheme.Twilight;
        }
    }
}

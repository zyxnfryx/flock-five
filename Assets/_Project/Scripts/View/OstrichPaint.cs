using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // One timeline for the painterly ostrich. The view plays these frames.
    // Calm play keeps the bob and the blink and skips the ruffle flourish.
    public enum OstrichAct
    {
        Idle = 0,
        Peck = 1,
        Hit1 = 2,
        Hit2 = 3,
        Daze = 4,
        Flee = 5,
        Strut = 6,
        Honk = 7,
        Shake = 8,
    }

    public struct OstrichSample
    {
        public float Bob;
        public float Tilt;
        public float Lean;
        public float Shake;
        public float Wind;
        public float Strike;
        public float Blink;
        public float Ruffle;
        public float Beak;
        public float Step;
        public float Spread;
        public int Damage;
        public float Flee;
        public float Dip;
    }

    public static class OstrichAnim
    {
        public const int IdleFrames = 16;
        public const int PeckFrames = 12;
        public const int Hit1Frames = 8;
        public const int Hit2Frames = 8;
        public const int DazeFrames = 10;
        public const int FleeFrames = 14;
        public const int StrutFrames = 10;
        public const int HonkFrames = 8;
        public const int ShakeFrames = 10;

        public static int Count(OstrichAct act)
        {
            switch (act)
            {
                case OstrichAct.Peck: return PeckFrames;
                case OstrichAct.Hit1: return Hit1Frames;
                case OstrichAct.Hit2: return Hit2Frames;
                case OstrichAct.Daze: return DazeFrames;
                case OstrichAct.Flee: return FleeFrames;
                case OstrichAct.Strut: return StrutFrames;
                case OstrichAct.Honk: return HonkFrames;
                case OstrichAct.Shake: return ShakeFrames;
                default: return IdleFrames;
            }
        }

        public static int TotalFrames()
        {
            int n = 0;
            for (int i = 0; i <= (int)OstrichAct.Shake; i++)
                n += Count((OstrichAct)i);
            return n;
        }

        public static bool BlinkFrame(int frame) => frame == 7 || frame == 8;

        public static bool RuffleFrame(int frame) => frame >= 10 && frame <= 15;

        public static bool WindFrame(int frame) => frame <= 4;

        public static bool StrikeFrame(int frame) => frame >= 5 && frame <= 7;

        public static OstrichSample At(OstrichAct act, int frame, bool calm)
        {
            int n = Count(act);
            if (n < 1) n = 1;
            if (frame < 0) frame = 0;
            if (frame >= n) frame = n - 1;
            float u = frame / (float)(n - 1 > 0 ? n - 1 : 1);
            float wave = Mathf.Sin(u * Mathf.PI * 2f);
            var s = new OstrichSample();
            switch (act)
            {
                case OstrichAct.Peck:
                    s.Wind = frame <= 4 ? frame / 4f : Mathf.Max(0f, 1f - (frame - 4) / 4f);
                    s.Strike = frame >= 5 && frame <= 7 ? (frame - 4) / 3f : (frame > 7 ? Mathf.Max(0f, 1f - (frame - 7) / 4f) : 0f);
                    s.Beak = s.Strike;
                    s.Dip = s.Strike * 0.34f - s.Wind * 0.06f;
                    s.Tilt = -16f * s.Wind + 22f * s.Strike;
                    s.Lean = -8f * s.Wind + 6f * s.Strike;
                    break;
                case OstrichAct.Hit1:
                    s.Damage = 1;
                    s.Ruffle = 0.85f;
                    s.Lean = Mathf.Sin(u * Mathf.PI) * -14f;
                    s.Tilt = Mathf.Sin(u * Mathf.PI) * 12f;
                    s.Shake = Mathf.Sin(u * Mathf.PI * 3f) * 8f;
                    break;
                case OstrichAct.Hit2:
                    s.Damage = 2;
                    s.Ruffle = 1f;
                    s.Lean = Mathf.Sin(u * Mathf.PI) * -22f;
                    s.Tilt = Mathf.Sin(u * Mathf.PI) * 16f;
                    s.Shake = Mathf.Sin(u * Mathf.PI * 4f) * 11f;
                    s.Bob = Mathf.Sin(u * Mathf.PI) * -0.06f;
                    break;
                case OstrichAct.Daze:
                    s.Damage = 3;
                    s.Ruffle = 0.4f;
                    s.Lean = Mathf.Sin(u * Mathf.PI * 3f) * 16f;
                    s.Tilt = Mathf.Sin(u * Mathf.PI * 2f) * 14f;
                    s.Bob = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 3f)) * -0.05f;
                    s.Step = u;
                    break;
                case OstrichAct.Flee:
                    s.Damage = 3;
                    s.Spread = Mathf.Clamp01(u * 1.4f);
                    s.Flee = u;
                    s.Step = u * 3f;
                    s.Lean = 18f + 10f * u;
                    s.Tilt = -8f;
                    s.Bob = Mathf.Sin(u * Mathf.PI * 6f) * 0.06f;
                    break;
                case OstrichAct.Strut:
                    s.Step = u;
                    s.Bob = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f)) * 0.12f;
                    s.Tilt = -18f;
                    s.Lean = -10f;
                    s.Beak = 0.15f;
                    break;
                case OstrichAct.Honk:
                    s.Beak = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
                    s.Bob = s.Beak * 0.06f;
                    s.Tilt = -6f * s.Beak;
                    s.Ruffle = 0.25f * s.Beak;
                    break;
                case OstrichAct.Shake:
                    s.Shake = Mathf.Sin(u * Mathf.PI * 4f) * 28f;
                    s.Tilt = s.Shake * 0.35f;
                    break;
                default:
                    s.Bob = wave * 0.07f;
                    s.Tilt = Mathf.Sin(u * Mathf.PI * 2f + 0.8f) * 7f;
                    s.Lean = s.Tilt * 0.35f;
                    s.Blink = BlinkFrame(frame) ? 1f : (frame == 6 || frame == 9 ? 0.4f : 0f);
                    s.Ruffle = RuffleFrame(frame) ? Mathf.Sin((frame - 10) / 5f * Mathf.PI) : 0.04f;
                    s.Step = u * 0.15f;
                    break;
            }
            if (calm)
            {
                s.Bob *= 0.25f;
                s.Tilt *= 0.25f;
                s.Lean *= 0.25f;
                s.Shake *= 0.2f;
                s.Ruffle *= 0.15f;
            }
            return s;
        }
    }

    // Painted ostrich, cut from the approved master (ostrich-master.jpg) into rig parts:
    // body (wing and tail), neck strip, head (+ blink), two legs. Every pose is the same art
    // moved by the rig in OstrichMark; nothing is drawn in code. Pixel geometry comes from
    // the cut (rig.json): 140 px per unit, the head 20% bigger.
    public static class OstrichPaint
    {
        public const float BodyPpu = 140f;
        public const float HeadPpu = BodyPpu / 1.2f; // review: head ~20% bigger, same chin pivot
        // Pivots (sprite-normalized, y up). Body: the neck root. Head: the neck top under the jaw.
        public static readonly Vector2 BodyPivot = new Vector2(527.5f / 566f, 1f - 102f / 323f);
        public static readonly Vector2 HeadPivot = new Vector2(42f / 160f, 1f - 114f / 124f);
        static readonly Vector2 BackLegPivot = new Vector2(47.25f / 88f, 1f - 22f / 279f);
        static readonly Vector2 FrontLegPivot = new Vector2(49.5f / 155f, 1f - 30f / 278f);
        // Hips relative to the body pivot, in units (facing right, before lean).
        public static readonly Vector2 BackHip = new Vector2((546.25f - 871.5f) / BodyPpu, (272f - 452f) / BodyPpu);
        public static readonly Vector2 FrontHip = new Vector2((632.5f - 871.5f) / BodyPpu, (272f - 460f) / BodyPpu);
        // The real neck is 46 px across.
        public const float NeckWidth = 46f / BodyPpu;

        public const string BodyPath = "Sprites/ostrich_body";
        public const string BodyHurtPath = "Sprites/ostrich_body_hurt";
        public const string HeadPath = "Sprites/ostrich_head";
        public const string BlinkPath = "Sprites/ostrich_head_blink";
        public const string NeckPath = "Sprites/ostrich_neck";
        public const string BackLegPath = "Sprites/ostrich_leg_back";
        public const string FrontLegPath = "Sprites/ostrich_leg_front";
        // Feathered collar from the body's own plumage: laps over the neck root, soft alpha.
        public const string CollarPath = "Sprites/ostrich_collar";
        static readonly Vector2 CollarPivot = new Vector2(135.5f / 168f, 1f - 48f / 164f);
        public static Sprite Collar() => Load(CollarPath, BodyPpu, CollarPivot);

        static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        static Sprite _neck;
        static Texture2D _neckTex;

        static Sprite Load(string path, float ppu, Vector2 pivot)
        {
            Sprite s;
            if (_sprites.TryGetValue(path, out s) && s != null) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            tex.wrapMode = TextureWrapMode.Clamp;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect);
            s.name = tex.name;
            s.hideFlags = HideFlags.HideAndDontSave;
            _sprites[path] = s;
            return s;
        }

        // Damage 1+ shows the plucked plume tips.
        public static Sprite Body(OstrichAct act, int frame)
        {
            var smp = OstrichAnim.At(act, frame, false);
            return smp.Damage >= 1 ? Load(BodyHurtPath, BodyPpu, BodyPivot) : Load(BodyPath, BodyPpu, BodyPivot);
        }

        // Pivot is the neck top, where the neck meets the head.
        public static Sprite Head(OstrichAct act, int frame)
        {
            var smp = OstrichAnim.At(act, frame, false);
            return smp.Blink > 0.5f || act == OstrichAct.Daze && (frame % 4 == 1)
                ? Load(BlinkPath, HeadPpu, HeadPivot) : Load(HeadPath, HeadPpu, HeadPivot);
        }

        public static Sprite BackLeg() => Load(BackLegPath, BodyPpu, BackLegPivot);
        public static Sprite FrontLeg() => Load(FrontLegPath, BodyPpu, FrontLegPivot);

        // The neck strip cut from the master, straightened: U runs body -> head, V across.
        public static Texture2D NeckTexture()
        {
            if (_neckTex != null) return _neckTex;
            _neckTex = Resources.Load<Texture2D>(NeckPath);
            if (_neckTex != null) _neckTex.wrapMode = TextureWrapMode.Clamp;
            return _neckTex;
        }

        public static Sprite Neck()
        {
            if (_neck != null) return _neck;
            var tex = NeckTexture();
            if (tex == null) return null;
            _neck = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), BodyPpu, 0, SpriteMeshType.FullRect);
            _neck.name = "ostrich-neck";
            _neck.hideFlags = HideFlags.HideAndDontSave;
            return _neck;
        }

        // Boot-gap preload (studio card): load every part and upload it now.
        public static int WarmCount => 8;
        public static void Warm(int i)
        {
            switch (i)
            {
                case 0: Load(BodyPath, BodyPpu, BodyPivot); break;
                case 1: Load(BodyHurtPath, BodyPpu, BodyPivot); break;
                case 2: Load(HeadPath, HeadPpu, HeadPivot); break;
                case 3: Load(BlinkPath, HeadPpu, HeadPivot); break;
                case 4: NeckTexture(); break;
                case 5: BackLeg(); break;
                case 6: FrontLeg(); break;
                case 7: Collar(); break;
            }
        }

        public static int OpaqueCount(Sprite sprite) => Count(sprite, false);

        public static int DarkCount(Sprite sprite) => Count(sprite, true);

        // Compressed textures are not CPU-readable; the editor counts from the source PNG.
        static int Count(Sprite sprite, bool dark)
        {
            if (sprite == null || sprite.texture == null) return 0;
            Color32[] px = null;
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(sprite.texture);
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                var t = new Texture2D(2, 2);
                t.LoadImage(System.IO.File.ReadAllBytes(path));
                px = t.GetPixels32();
                Object.DestroyImmediate(t);
            }
#endif
            if (px == null)
            {
                if (!sprite.texture.isReadable) return 0;
                px = sprite.texture.GetPixels32();
            }
            int n = 0;
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a < 48) continue;
                if (!dark) { n++; continue; }
                if (c.r < 55 && c.g < 55 && c.b < 55) n++;
            }
            return n;
        }
    }
}

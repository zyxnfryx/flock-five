using System.Collections.Generic;
using UnityEngine;

namespace FlockFive
{
    // Themed accessories that give look-alike bees their own read on a card
    // (Brandon: Sphinx Sting and Noodle Loop were twins). One sprite per type under
    // Resources/Sprites/Accessories, painted to match fx_bee. Every type is anchored
    // to a point on the painted bee (head top, eye, brow, neck, mouth) in the bee
    // sprite's own 0..1 space (x right, y down), so it scales and flips with the bee.
    public enum BeeAccessory
    {
        None = 0,
        Nemes = 1,
        NoodleBowl = 2,
        TopHat = 3,
        Beanie = 4,
        Cap = 5,
        SailorCap = 6,
        ChefHat = 7,
        HardHat = 8,
        Headphones = 9,
        Shades = 10,
        PixelShades = 11,
        StarCrown = 12,
        Flower = 13,
        Scarf = 14,
        Headband = 15,
        Bolt = 16,
        ScarfSky = 17,
        BowTie = 18,
    }

    public static class BeeAccessoryArt
    {
        // Points on fx_bee.png (1024 sq, bee faces right). y grows downward like GUI.
        public static class Anchors
        {
            public static readonly Vector2 Head = new Vector2(0.755f, 0.350f);
            public static readonly Vector2 Eye = new Vector2(0.745f, 0.450f);
            public static readonly Vector2 Neck = new Vector2(0.700f, 0.520f);
            public static readonly Vector2 Mouth = new Vector2(0.805f, 0.560f);
            public static readonly Vector2 Brow = new Vector2(0.760f, 0.395f);
        }

        public readonly struct Spec
        {
            public readonly string Path;
            public readonly Vector2 Anchor;   // bee space
            public readonly float Size;       // sprite square edge as a fraction of the bee rect
            public readonly Vector2 Pivot;    // sprite space point that lands on Anchor
            public readonly float Rot;        // degrees, clockwise on screen (head tilt)
            public Spec(string path, Vector2 anchor, float size, Vector2 pivot, float rot)
            { Path = "Sprites/Accessories/" + path; Anchor = anchor; Size = size; Pivot = pivot; Rot = rot; }
        }

        // Index = (int)BeeAccessory - 1. Mirrors tools/accessories.py SPEC.
        static readonly Spec[] Specs =
        {
            new Spec("acc_nemes", Anchors.Head, 0.38f, new Vector2(0.52f, 0.34f), 10f),   // Nemes
            new Spec("acc_noodles", Anchors.Mouth, 0.44f, new Vector2(0.30f, 0.30f), 0f),   // NoodleBowl
            new Spec("acc_tophat", Anchors.Head, 0.38f, new Vector2(0.50f, 0.86f), 14f),   // TopHat
            new Spec("acc_beanie", Anchors.Head, 0.35f, new Vector2(0.50f, 0.80f), 14f),   // Beanie
            new Spec("acc_cap", Anchors.Head, 0.38f, new Vector2(0.42f, 0.80f), 12f),   // Cap
            new Spec("acc_sailor", Anchors.Head, 0.35f, new Vector2(0.50f, 0.80f), 14f),   // SailorCap
            new Spec("acc_chef", Anchors.Head, 0.38f, new Vector2(0.50f, 0.90f), 12f),   // ChefHat
            new Spec("acc_hardhat", Anchors.Head, 0.35f, new Vector2(0.50f, 0.80f), 14f),   // HardHat
            new Spec("acc_headphones", Anchors.Eye, 0.43f, new Vector2(0.50f, 0.55f), 12f),   // Headphones
            new Spec("acc_shades", Anchors.Eye, 0.38f, new Vector2(0.56f, 0.50f), 10f),   // Shades
            new Spec("acc_pixelshades", Anchors.Eye, 0.38f, new Vector2(0.56f, 0.48f), 8f),   // PixelShades
            new Spec("acc_crown", Anchors.Head, 0.40f, new Vector2(0.50f, 0.88f), 14f),   // StarCrown
            new Spec("acc_flower", Anchors.Head, 0.30f, new Vector2(0.50f, 0.55f), 0f),   // Flower
            new Spec("acc_scarf", Anchors.Neck, 0.34f, new Vector2(0.50f, 0.40f), 10f),   // Scarf
            new Spec("acc_headband", Anchors.Brow, 0.33f, new Vector2(0.50f, 0.50f), 14f),   // Headband
            new Spec("acc_bolt", Anchors.Head, 0.34f, new Vector2(0.40f, 0.95f), 18f),   // Bolt
            new Spec("acc_scarfsky", Anchors.Neck, 0.34f, new Vector2(0.50f, 0.40f), 10f),   // ScarfSky
            new Spec("acc_bowtie", Anchors.Neck, 0.26f, new Vector2(0.50f, 0.50f), 8f),   // BowTie
        };

        static readonly Dictionary<int, Texture2D> _tex = new Dictionary<int, Texture2D>();
        static readonly Dictionary<int, Sprite> _spr = new Dictionary<int, Sprite>();

        public static bool Has(BeeAccessory a) => a != BeeAccessory.None && (int)a - 1 < Specs.Length;

        public static Texture2D Texture(BeeAccessory a)
        {
            if (!Has(a)) return null;
            int k = (int)a;
            if (_tex.TryGetValue(k, out var t)) return t;
            t = Resources.Load<Texture2D>(Specs[k - 1].Path);
            _tex[k] = t;
            return t;
        }

        // ---------------------------------------------------------------- OnGUI
        // `bee` is the rect the bee texture was drawn into (ScaleToFit of a square
        // sprite, so the square centred in it). Draw after the bee. A caller that
        // mirrors the bee with GUI.matrix gets a mirrored accessory for free.
        public static void Draw(Rect bee, BeeAccessory a, Color tint, float alpha)
        {
            if (alpha < 0.04f || bee.width < 4f || bee.height < 4f) return;
            var tex = Texture(a);
            if (tex == null) return;
            var sp = Specs[(int)a - 1];
            float side = Mathf.Min(bee.width, bee.height);
            var sq = new Rect(bee.center.x - side * 0.5f, bee.center.y - side * 0.5f, side, side);
            var at = new Vector2(sq.x + sp.Anchor.x * side, sq.y + sp.Anchor.y * side);
            float w = sp.Size * side;
            var r = new Rect(at.x - sp.Pivot.x * w, at.y - sp.Pivot.y * w, w, w);
            var prevM = GUI.matrix;
            var prevC = GUI.color;
            if (sp.Rot != 0f) GUIUtility.RotateAroundPivot(sp.Rot, at);
            if (tint.a <= 0f) tint = Color.white;
            GUI.color = new Color(tint.r, tint.g, tint.b, alpha);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
            GUI.matrix = prevM;
            GUI.color = prevC;
        }

        public static void Draw(Rect bee, BeeKind kind, float alpha) =>
            Draw(bee, kind.Accessory, kind.AccessoryTint, alpha);

        // Card art (CardCritter) only has the kind id.
        public static void DrawFor(Rect bee, string id, float alpha)
        {
            var a = Hive.AccessoryOf(id, out var tint);
            if (a != BeeAccessory.None) Draw(bee, a, tint, alpha);
        }

        // ---------------------------------------------------------------- World
        // Child SpriteRenderer on a world bee (HiveView visitor). Same anchors, so it
        // follows the bee's scale, flipX and sorting. Returns null for None.
        public static SpriteRenderer Attach(SpriteRenderer bee, BeeKind kind)
        {
            if (bee == null || bee.sprite == null || !Has(kind.Accessory)) return null;
            var tex = Texture(kind.Accessory);
            if (tex == null) return null;
            int k = (int)kind.Accessory;
            var sp = Specs[k - 1];
            var b = bee.sprite.bounds.size;   // local units of the bee sprite
            float ppu = tex.width / Mathf.Max(0.0001f, sp.Size * b.x);
            if (!_spr.TryGetValue(k, out var s) || s == null)
            {
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(sp.Pivot.x, 1f - sp.Pivot.y), ppu);
                _spr[k] = s;
            }
            var go = new GameObject("BeeAccessory");
            go.transform.SetParent(bee.transform, false);
            // The cached sprite was cut for one bee size; rescale if this bee differs.
            float want = sp.Size * b.x, have = s.bounds.size.x;
            go.transform.localScale = Vector3.one * (have > 0f ? want / have : 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingLayerID = bee.sortingLayerID;
            sr.sortingOrder = bee.sortingOrder + 1;
            var follow = go.AddComponent<BeeAccessoryFollow>();
            follow.Init(bee, sr, new Vector2((sp.Anchor.x - 0.5f) * b.x, (0.5f - sp.Anchor.y) * b.y), sp.Rot,
                kind.AccessoryTint.a > 0f ? kind.AccessoryTint : Color.white);
            return sr;
        }
    }

    // Keeps a world accessory on its bee when the bee flips (flipX) or fades.
    public sealed class BeeAccessoryFollow : MonoBehaviour
    {
        SpriteRenderer _bee, _sr;
        Vector2 _at;
        float _rot;
        Color _tint;
        bool _flip;

        public void Init(SpriteRenderer bee, SpriteRenderer sr, Vector2 at, float rot, Color tint)
        {
            _bee = bee; _sr = sr; _at = at; _rot = rot; _tint = tint;
            _flip = !bee.flipX;   // force first Apply
            Apply();
        }

        void LateUpdate() => Apply();

        void Apply()
        {
            if (_bee == null || _sr == null) return;
            bool f = _bee.flipX;
            if (f != _flip)
            {
                _flip = f;
                float sx = f ? -1f : 1f;
                transform.localPosition = new Vector3(_at.x * sx, _at.y, 0f);
                transform.localRotation = Quaternion.Euler(0f, 0f, -_rot * sx);
                _sr.flipX = f;
            }
            var c = _tint;
            c.a = _bee.color.a * (_bee.enabled ? 1f : 0f);
            _sr.color = c;
        }
    }
}

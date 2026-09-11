using UnityEngine;

namespace FlockFive
{
    public static class SpriteCatalog
    {
        static Sprite _bg, _branch, _branchGift, _leaf, _vine, _petalPink, _petalPeach, _firefly, _glow, _rain, _smoke, _blanket, _zee, _sparkle, _moon, _logo, _bee, _beeFlap, _feather, _bow, _bowtie, _crown, _hive, _playFlower, _adSign, _adBulb, _adCard, _iceA, _iceB, _iceShard, _restart, _piggy, _coin, _poker, _cardBack;
        static Sprite[] _letters;
        static Sprite[] _digits;
        static Sprite[] _birds;
        static Sprite[] _flap1;
        static Sprite[] _flap2;
        static Sprite[] _kitRest;
        static Sprite[] _kitUp;
        static Sprite[] _kitMid;
        static Sprite[] _feeders;

        public static Sprite GardenBg => Load(ref _bg, "Sprites/bg_garden", 96f);
        public static Sprite Branch => Load(ref _branch, "Sprites/branch", 140f);
        public static Sprite BranchGift => Load(ref _branchGift, "Sprites/branch_gift", 140f);
        public static Sprite AdSign => Load(ref _adSign, "Sprites/fx_ad_sign", 200f);
        public static Sprite AdBulb => Load(ref _adBulb, "Sprites/fx_ad_bulb", 200f);
        public static Sprite AdCard => Load(ref _adCard, "Sprites/fx_ad_card", 200f);
        public static Sprite IceA => Load(ref _iceA, "Sprites/fx_ice_a", 96f);
        public static Sprite IceB => Load(ref _iceB, "Sprites/fx_ice_b", 96f);
        public static Sprite IceShard => Load(ref _iceShard, "Sprites/fx_ice_shard", 200f);
        public static Sprite Restart => Load(ref _restart, "Sprites/fx_restart", 200f);
        public static Sprite Piggy => Load(ref _piggy, "Sprites/fx_piggy", 200f);
        public static Sprite Coin => Load(ref _coin, "Sprites/fx_coin", 200f);
        public static Sprite Leaf => Load(ref _leaf, "Sprites/fx_leaf", 200f);
        public static Sprite Vine => Load(ref _vine, "Sprites/fx_vine", 200f);
        public static Sprite PetalPink => Load(ref _petalPink, "Sprites/fx_petal_pink", 200f);
        public static Sprite PetalPeach => Load(ref _petalPeach, "Sprites/fx_petal_peach", 200f);
        public static Sprite Feather => Load(ref _feather, "Sprites/fx_feather", 200f);
        public static Sprite Bow => Load(ref _bow, "Sprites/fx_bow", 200f);
        public static Sprite Bowtie => Load(ref _bowtie, "Sprites/fx_bowtie", 200f);
        public static Sprite Crown => Load(ref _crown, "Sprites/fx_crown", 200f);
        public static Sprite Hive => Load(ref _hive, "Sprites/fx_hive", 200f);
        public static Sprite Poker => Load(ref _poker, "Sprites/fx_poker", 200f);
        public static Sprite CardBack => Load(ref _cardBack, "Sprites/fx_card_back", 200f);
        public static Sprite PlayFlower => Load(ref _playFlower, "Sprites/fx_play_flower", 200f);
        public static Sprite Firefly => Load(ref _firefly, "Sprites/fx_firefly", 200f);
        public static Sprite Zee => Load(ref _zee, "Sprites/fx_z", 200f);
        public static Sprite Sparkle => Load(ref _sparkle, "Sprites/fx_sparkle", 200f);
        public static Sprite Moon => Load(ref _moon, "Sprites/fx_moon", 240f);
        public static Sprite Logo => Load(ref _logo, "Sprites/fx_logo", 180f);
        public static Sprite Letter(char c)
        {
            c = char.ToUpperInvariant(c);
            if (c < 'A' || c > 'Z') return Glow;
            if (_letters == null) _letters = new Sprite[26];
            int i = c - 'A';
            if (_letters[i] == null)
                _letters[i] = LoadNew("Sprites/fx_let_" + c, 150f);
            return _letters[i];
        }

        public static Sprite Digit(int d)
        {
            d = Mathf.Clamp(d, 0, 9);
            if (_digits == null) _digits = new Sprite[10];
            if (_digits[d] == null)
                _digits[d] = LoadNew("Sprites/fx_let_" + d, 150f);
            return _digits[d];
        }

        public static Sprite Glyph(char c)
        {
            if (c >= '0' && c <= '9') return Digit(c - '0');
            return Letter(c);
        }
        public static Sprite Bee => Load(ref _bee, "Sprites/fx_bee", 520f);

        public static Sprite BeeFrame(float t)
        {
            var a = Bee;
            var b = Load(ref _beeFlap, "Sprites/fx_bee_1", 520f);
            return (Mathf.FloorToInt(Mathf.Abs(t)) % 2 == 0) ? a : b;
        }

        public static Sprite Glow
        {
            get
            {
                if (_glow != null) return _glow;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                float m = (n - 1) * 0.5f;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - m) / m;
                    float dy = (y - m) / m;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    tex.SetPixel(x, y, new Color(1f, 0.95f, 0.72f, a));
                }
                tex.Apply();
                _glow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 64f);
                return _glow;
            }
        }

        public static Sprite RainStreak
        {
            get
            {
                if (_rain != null) return _rain;
                const int w = 12, h = 96;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                float mx = (w - 1) * 0.5f;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs((x - mx) / mx);
                    float along = y / (float)(h - 1);
                    float a = Mathf.Clamp01(1f - dx);
                    a *= a;
                    a *= Mathf.Sin(along * Mathf.PI);
                    tex.SetPixel(x, y, new Color(0.78f, 0.86f, 0.95f, a));
                }
                tex.Apply();
                _rain = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 96f);
                return _rain;
            }
        }

        public static Sprite Smoke
        {
            get
            {
                if (_smoke != null) return _smoke;
                const int w = 256, h = 128;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                float cx = (w - 1) * 0.5f;
                float cy = (h - 1) * 0.5f;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a;
                    if (d < 0.86f) a = 1f;
                    else if (d < 1.02f) a = Mathf.SmoothStep(1f, 0f, (d - 0.86f) / 0.16f);
                    else a = 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                _smoke = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
                return _smoke;
            }
        }

        public static Sprite Blanket
        {
            get
            {
                if (_blanket != null) return _blanket;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                float m = (n - 1) * 0.5f;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - m) / m;
                    float dy = (y - m) / m;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a;
                    if (d < 0.84f) a = 1f;
                    else if (d < 1f) a = Mathf.SmoothStep(1f, 0f, (d - 0.84f) / 0.16f);
                    else a = 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                _blanket = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 128f);
                return _blanket;
            }
        }

        public static void ForgetBirds()
        {
            _birds = null;
            _flap1 = null;
            _flap2 = null;
            _kitRest = null;
            _kitUp = null;
            _kitMid = null;
        }

        public static Sprite Bird(BirdColor c) => Slot(ref _birds, (int)c, "Sprites/bird_" + Name(c), 280f);
        public static Sprite Bird(BirdColor c, BirdSex sex)
        {
            if (sex == BirdSex.Neutral) return Bird(c);
            string tag = sex == BirdSex.Female ? "_f" : "_m";
            var got = SlotWide(ref _kitRest, KitIx(c, sex), "Sprites/bird_" + Name(c) + tag, 280f);
            return got != null ? got : Bird(c);
        }
        public static Sprite Feeder(BirdColor c) => Slot(ref _feeders, (int)c, "Sprites/feeder_" + Name(c), 180f);

        public static Sprite BirdFrame(BirdColor c, float t, bool flap) =>
            BirdFrame(c, t, flap, BirdSex.Neutral);

        public static Sprite BirdFrame(BirdColor c, float t, bool flap, BirdSex sex)
        {
            var rest = Bird(c, sex);
            if (!flap) return rest;
            string tag = sex == BirdSex.Female ? "_f" : sex == BirdSex.Male ? "_m" : "";
            int ix = KitIx(c, sex);
            var up = tag.Length == 0
                ? Slot(ref _flap1, (int)c, "Sprites/bird_" + Name(c) + "_1", 280f)
                : SlotWide(ref _kitUp, ix, "Sprites/bird_" + Name(c) + tag + "_1", 280f);
            var mid = tag.Length == 0
                ? Slot(ref _flap2, (int)c, "Sprites/bird_" + Name(c) + "_2", 280f)
                : SlotWide(ref _kitMid, ix, "Sprites/bird_" + Name(c) + tag + "_2", 280f);
            int k = Mathf.FloorToInt(Mathf.Abs(t) * 16f) % 4;
            if (k == 0) return rest;
            if (k == 2) return mid != null ? mid : up;
            return up != null ? up : rest;
        }

        public static Sprite BirdFrame(BirdColor c, float t) => BirdFrame(c, t, false);

        static int KitIx(BirdColor c, BirdSex s) => (int)s * Palette.Max + (int)c;

        static string Name(BirdColor c)
        {
            switch (c)
            {
                case BirdColor.Ruby: return "ruby";
                case BirdColor.Gold: return "gold";
                case BirdColor.Teal: return "teal";
                case BirdColor.Peach: return "peach";
                default: return "violet";
            }
        }

        static Sprite Slot(ref Sprite[] arr, int i, string path, float ppu)
        {
            if (arr == null) arr = new Sprite[Palette.Max];
            if (arr[i] == null)
            {
                arr[i] = TryLoad(path, ppu);
                if (arr[i] == null && i == (int)BirdColor.Peach)
                {
                    string goldPath = path.Replace("peach", "gold");
                    var gold = Slot(ref arr, (int)BirdColor.Gold, goldPath, ppu);
                    arr[i] = Recolor(gold, new Color(1.18f, 0.58f, 0.52f, 1f));
                }
                if (arr[i] == null) arr[i] = Fallback(ppu);
            }
            return arr[i];
        }

        static Sprite SlotWide(ref Sprite[] arr, int i, string path, float ppu)
        {
            int n = Palette.Max * 3;
            if (arr == null) arr = new Sprite[n];
            if (i < 0 || i >= arr.Length) return null;
            if (arr[i] == null)
                arr[i] = TryLoad(path, ppu);
            return arr[i];
        }

        static Sprite Load(ref Sprite cache, string path, float ppu)
        {
            if (cache == null) cache = LoadNew(path, ppu);
            return cache;
        }

        static Sprite LoadNew(string path, float ppu)
        {
            var got = TryLoad(path, ppu);
            if (got != null) return got;
            Debug.LogWarning("Missing sprite " + path);
            return Fallback(ppu);
        }

        static Sprite TryLoad(string path, float ppu)
        {
            var ready = Resources.Load<Sprite>(path);
            if (ready != null) return ready;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pivot = new Vector2(0.5f, 0.5f);
            if (path.Contains("fx_vine")) pivot = new Vector2(0.5f, 0.94f);
            else if (path.Contains("fx_leaf")) pivot = new Vector2(0.5f, 0.08f);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect);
        }

        static Sprite Recolor(Sprite src, Color mul)
        {
            if (src == null) return Fallback(100f);
            var rect = src.rect;
            int w = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            int h = Mathf.Max(1, Mathf.RoundToInt(rect.height));
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            try
            {
                var pix = src.texture.GetPixels(
                    Mathf.RoundToInt(rect.x),
                    Mathf.RoundToInt(rect.y),
                    w, h);
                for (int i = 0; i < pix.Length; i++)
                {
                    var p = pix[i];
                    pix[i] = new Color(
                        Mathf.Clamp01(p.r * mul.r),
                        Mathf.Clamp01(p.g * mul.g),
                        Mathf.Clamp01(p.b * mul.b),
                        p.a);
                }
                tex.SetPixels(pix);
            }
            catch (System.Exception)
            {
                return src;
            }
            tex.Apply();
            var pivot = new Vector2(
                src.pivot.x / Mathf.Max(1f, rect.width),
                src.pivot.y / Mathf.Max(1f, rect.height));
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, src.pixelsPerUnit);
        }

        static Sprite Fallback(float ppu)
        {
            var tex = new Texture2D(8, 8);
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, Color.magenta);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}

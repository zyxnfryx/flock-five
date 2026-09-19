using UnityEngine;

namespace FlockFive
{
    public static class SpriteCatalog
    {
        static Sprite _bg, _branch, _branchGift, _leaf, _vine, _petalPink, _petalPeach, _firefly, _glow, _rain, _smoke, _blanket, _zee, _sparkle, _moon, _logo, _bee, _beeFlap, _feather, _bow, _bowtie, _crown, _hive, _playFlower, _adSign, _adBulb, _adCard, _iceA, _iceB, _iceShard, _restart, _piggy, _coin, _poker, _cardBack, _cardPaper, _handFan, _handFanFront, _handPluck, _wildBanner, _dash, _chain, _padlock, _stampRing, _stampTool, _joker, _clipboard, _sparrow, _sparrowFlap1, _sparrowFlap2, _hawk;
        static Sprite[] _flames;
        static bool _sparrowPlaceholder;
        static bool _hawkPlaceholder;
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
        public static bool SparrowIsPlaceholder => _sparrowPlaceholder;
        public static Sprite Sparrow
        {
            get
            {
                if (_sparrow != null) return _sparrow;
                _sparrow = TryLoad("Sprites/fx_sparrow", 200f);
                if (_sparrow != null)
                {
                    _sparrowPlaceholder = false;
                    return _sparrow;
                }
                // No sparrow art yet — brown/grey-tint a hummingbird as placeholder.
                _sparrowPlaceholder = true;
                _sparrow = Recolor(Bird(BirdColor.Violet), new Color(0.62f, 0.55f, 0.48f, 1f));
                return _sparrow;
            }
        }

        public static Sprite SparrowFrame(float t)
        {
            var rest = Sparrow;
            if (_sparrowPlaceholder) return BirdFrame(BirdColor.Violet, t, true);
            var up = Load(ref _sparrowFlap1, "Sprites/fx_sparrow_1", 200f);
            var mid = Load(ref _sparrowFlap2, "Sprites/fx_sparrow_2", 200f);
            int k = Mathf.FloorToInt(Mathf.Abs(t) * 16f) % 4;
            if (k == 1) return up != null ? up : rest;
            if (k == 2) return mid != null ? mid : rest;
            if (k == 3) return up != null ? up : rest;
            return rest;
        }
        public static bool HawkIsPlaceholder => _hawkPlaceholder;
        public static Sprite Hawk
        {
            get
            {
                if (_hawk != null) return _hawk;
                _hawk = TryLoad("Sprites/fx_hawk", 200f);
                if (_hawk != null)
                {
                    _hawkPlaceholder = false;
                    return _hawk;
                }
                // No hawk art yet — dark rust-tint a hummingbird as placeholder.
                _hawkPlaceholder = true;
                _hawk = Recolor(Bird(BirdColor.Ruby), new Color(0.55f, 0.38f, 0.28f, 1f));
                return _hawk;
            }
        }

        public static Sprite HawkFrame(float t)
        {
            var rest = Hawk;
            if (_hawkPlaceholder) return BirdFrame(BirdColor.Ruby, t, true);
            // Single-frame art for now (no flap sheet yet).
            return rest;
        }
        public static Sprite Bow => Load(ref _bow, "Sprites/fx_bow", 200f);
        public static Sprite Bowtie => Load(ref _bowtie, "Sprites/fx_bowtie", 200f);
        public static Sprite Crown => Load(ref _crown, "Sprites/fx_crown", 200f);
        public static Sprite Hive => Load(ref _hive, "Sprites/fx_hive", 200f);
        public static Sprite Poker => Load(ref _poker, "Sprites/fx_poker", 200f);
        public static Sprite CardBack => Load(ref _cardBack, "Sprites/fx_card_back", 200f);
        public static void DropPokerArt()
        {
            _cardBack = null;
            _handFan = null;
            _handFanFront = null;
            _handPluck = null;
            _stampTool = null;
            _wildBanner = null;
            _padlock = null;
            _chain = null;
        }
        public static Sprite CardPaper
        {
            get
            {
                if (_cardPaper == null) _cardPaper = TryLoad("Sprites/fx_card_paper", 200f);
                return _cardPaper;
            }
        }
        public static Sprite HandFan
        {
            get
            {
                if (!HandSolid(_handFan, "HandFanSolid5"))
                    _handFan = NameHand(HardenHand(TryLoad("Sprites/fx_hand_fan", 200f), true), "HandFanSolid5");
                return _handFan;
            }
        }
        public static Sprite HandFanFront
        {
            get
            {
                if (!HandSolid(_handFanFront, "HandFanFrontSolid5"))
                    _handFanFront = NameHand(HardenHand(TryLoad("Sprites/fx_hand_fan_front", 200f), true), "HandFanFrontSolid5");
                return _handFanFront;
            }
        }
        public static Sprite HandPluck
        {
            get
            {
                if (!HandSolid(_handPluck, "HandPluckSolid5"))
                    _handPluck = NameHand(HardenHand(TryLoad("Sprites/fx_hand_pluck", 200f), true), "HandPluckSolid5");
                return _handPluck;
            }
        }

        static bool HandSolid(Sprite spr, string name) =>
            spr != null && spr.texture != null && spr.name == name;

        static Sprite NameHand(Sprite spr, string name)
        {
            if (spr != null) spr.name = name;
            return spr;
        }
        public static Sprite WildBanner
        {
            get
            {
                if (_wildBanner == null) _wildBanner = TryLoad("Sprites/fx_wild_banner", 200f);
                return _wildBanner;
            }
        }
        public static Sprite Dash
        {
            get
            {
                if (_dash == null) _dash = TryLoad("Sprites/fx_dash", 200f);
                return _dash;
            }
        }
        public static Sprite Chain
        {
            get
            {
                if (_chain == null) _chain = TryLoad("Sprites/fx_chain", 200f);
                return _chain;
            }
        }
        public static Sprite Padlock
        {
            get
            {
                if (_padlock == null) _padlock = TryLoad("Sprites/fx_padlock", 200f);
                return _padlock;
            }
        }
        public static Sprite Flame(int i)
        {
            if (_flames == null) _flames = new Sprite[6];
            int k = ((i % 6) + 6) % 6;
            if (_flames[k] == null) _flames[k] = TryLoad("Sprites/fx_flame_" + k, 200f);
            return _flames[k];
        }
        public static Sprite Joker
        {
            get
            {
                if (_joker == null) _joker = TryLoad("Sprites/fx_joker", 200f);
                return _joker;
            }
        }
        public static Sprite Clipboard
        {
            get
            {
                if (_clipboard == null) _clipboard = TryLoad("Sprites/fx_clipboard", 200f);
                return _clipboard;
            }
        }
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

        public static Sprite StampTool
        {
            get
            {
                if (_stampTool == null) _stampTool = TryLoad("Sprites/fx_stamp_tool", 200f);
                return _stampTool;
            }
        }
        public static Sprite StampRing
        {
            get
            {
                if (_stampRing != null) return _stampRing;
                const int n = 160;
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
                    float a = 0f;
                    if (d > 0.72f && d < 1.02f)
                    {
                        float inner = Mathf.InverseLerp(0.72f, 0.80f, d);
                        float outer = 1f - Mathf.InverseLerp(0.94f, 1.02f, d);
                        a = Mathf.Clamp01(inner) * Mathf.Clamp01(outer);
                    }
                    else if (d > 0.50f && d < 0.62f)
                    {
                        float inner = Mathf.InverseLerp(0.50f, 0.54f, d);
                        float outer = 1f - Mathf.InverseLerp(0.58f, 0.62f, d);
                        a = Mathf.Clamp01(inner) * Mathf.Clamp01(outer) * 0.92f;
                    }
                    tex.SetPixel(x, y, new Color(0.82f, 0.08f, 0.10f, a));
                }
                tex.Apply();
                _stampRing = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 128f);
                return _stampRing;
            }
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
            _sparrow = null;
            _sparrowFlap1 = null;
            _sparrowFlap2 = null;
            _sparrowPlaceholder = false;
            _hawk = null;
            _hawkPlaceholder = false;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("Missing sprite " + path);
#endif
            return Fallback(ppu);
        }

        // Solid silhouette: keep faint sleeve paint, drop white finger-edge
        // fringe, close holes in palm/forearm, then force alpha = 1.
        static Sprite HardenHand(Sprite src, bool fillHoles)
        {
            if (src == null || src.texture == null) return src;
            var srcTex = src.texture;
            int w = srcTex.width;
            int h = srcTex.height;
            Color[] pix;
            try { pix = srcTex.GetPixels(); }
            catch { return src; }
            int n = pix.Length;
            var mask = new byte[n];
            for (int i = 0; i < n; i++)
            {
                if (pix[i].a < 0.12f)
                {
                    pix[i] = new Color(0f, 0f, 0f, 0f);
                    mask[i] = 0;
                }
                else
                {
                    var p = pix[i];
                    p.a = 1f;
                    pix[i] = p;
                    mask[i] = 1;
                }
            }
            if (fillHoles)
            {
                DropSmall(mask, w, h, 400);
                MorphOpen(mask, w, h, 1);
                DropSmall(mask, w, h, 400);
                int ones = 0;
                for (int i = 0; i < n; i++)
                    if (mask[i] != 0) ones++;
                if (ones > 80000)
                {
                    MorphClose(mask, w, h, 3);
                    ScanlineFill(mask, pix, w, h);
                }
                else
                    FillClosedHoles(mask, pix, w, h);
                RecolorWhiteEdge(mask, pix, w, h);
                for (int i = 0; i < n; i++)
                {
                    if (mask[i] == 0) pix[i] = new Color(0f, 0f, 0f, 0f);
                    else
                    {
                        var p = pix[i];
                        p.a = 1f;
                        pix[i] = p;
                    }
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(pix);
            tex.Apply(false, false);
#if UNITY_EDITOR
            try
            {
                var png = tex.EncodeToPNG();
                if (png != null && png.Length > 0)
                    System.IO.File.WriteAllBytes("/tmp/paradice/hard-" + src.name + ".png", png);
                Debug.Log("Flock Five: HardenHand " + src.name + " " + w + "x" + h);
            }
            catch { }
#endif
            return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f),
                src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        static void DropSmall(byte[] mask, int w, int h, int min)
        {
            int n = w * h;
            var seen = new bool[n];
            var q = new int[n];
            var comp = new int[n];
            for (int s = 0; s < n; s++)
            {
                if (mask[s] == 0 || seen[s]) continue;
                int qh = 0, qt = 0;
                q[qt++] = s;
                seen[s] = true;
                int count = 0;
                while (qh < qt)
                {
                    int i = q[qh++];
                    comp[count++] = i;
                    int x = i % w;
                    int y = i / w;
                    if (x > 0) EnqMask(mask, seen, q, ref qt, i - 1);
                    if (x + 1 < w) EnqMask(mask, seen, q, ref qt, i + 1);
                    if (y > 0) EnqMask(mask, seen, q, ref qt, i - w);
                    if (y + 1 < h) EnqMask(mask, seen, q, ref qt, i + w);
                }
                if (count >= min) continue;
                for (int k = 0; k < count; k++) mask[comp[k]] = 0;
            }
        }

        static void EnqMask(byte[] mask, bool[] seen, int[] q, ref int qt, int i)
        {
            if (mask[i] == 0 || seen[i]) return;
            seen[i] = true;
            q[qt++] = i;
        }

        static void MorphOpen(byte[] mask, int w, int h, int r)
        {
            int n = w * h;
            var tmp = new byte[n];
            MorphMin(mask, tmp, w, h, r);
            MorphMax(tmp, mask, w, h, r);
        }

        static void MorphClose(byte[] mask, int w, int h, int r)
        {
            int n = w * h;
            var tmp = new byte[n];
            MorphMax(mask, tmp, w, h, r);
            MorphMin(tmp, mask, w, h, r);
        }

        static bool WhiteFringe(Color p)
        {
            return p.r > 0.86f && p.g > 0.86f && p.b > 0.86f;
        }

        static void FillClosedHoles(byte[] mask, Color[] pix, int w, int h)
        {
            int n = w * h;
            var outside = new bool[n];
            var q = new int[n];
            int qh = 0, qt = 0;
            void Enq(int i)
            {
                if ((uint)i >= (uint)n || outside[i] || mask[i] != 0) return;
                outside[i] = true;
                q[qt++] = i;
            }
            for (int x = 0; x < w; x++) { Enq(x); Enq((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { Enq(y * w); Enq(y * w + w - 1); }
            while (qh < qt)
            {
                int i = q[qh++];
                int x = i % w;
                if (x > 0) Enq(i - 1);
                if (x + 1 < w) Enq(i + 1);
                if (i >= w) Enq(i - w);
                if (i + w < n) Enq(i + w);
            }
            var skin = new Color(0.78f, 0.56f, 0.44f, 1f);
            for (int i = 0; i < n; i++)
            {
                if (outside[i] || mask[i] != 0) continue;
                Color fill = skin;
                int x = i % w;
                if (x > 0 && mask[i - 1] != 0) fill = pix[i - 1];
                fill.a = 1f;
                pix[i] = fill;
                mask[i] = 1;
            }
        }

        static void RecolorWhiteEdge(byte[] mask, Color[] pix, int w, int h)
        {
            var skin = new Color(0.78f, 0.56f, 0.44f, 1f);
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int i = row + x;
                    if (mask[i] == 0 || !WhiteFringe(pix[i])) continue;
                    Color fill = skin;
                    if (x > 0 && mask[i - 1] != 0 && !WhiteFringe(pix[i - 1])) fill = pix[i - 1];
                    else if (y > 0 && mask[i - w] != 0 && !WhiteFringe(pix[i - w])) fill = pix[i - w];
                    else if (x + 1 < w && mask[i + 1] != 0 && !WhiteFringe(pix[i + 1])) fill = pix[i + 1];
                    fill.a = 1f;
                    pix[i] = fill;
                }
            }
        }

        static void MorphMin(byte[] src, byte[] dst, int w, int h, int r)
        {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte v = 1;
                int y0 = y - r, y1 = y + r, x0 = x - r, x1 = x + r;
                if (y0 < 0) y0 = 0;
                if (y1 >= h) y1 = h - 1;
                if (x0 < 0) x0 = 0;
                if (x1 >= w) x1 = w - 1;
                for (int yy = y0; yy <= y1 && v != 0; yy++)
                {
                    int row = yy * w;
                    for (int xx = x0; xx <= x1; xx++)
                        if (src[row + xx] == 0) { v = 0; break; }
                }
                dst[y * w + x] = v;
            }
        }

        static void MorphMax(byte[] src, byte[] dst, int w, int h, int r)
        {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                byte v = 0;
                int y0 = y - r, y1 = y + r, x0 = x - r, x1 = x + r;
                if (y0 < 0) y0 = 0;
                if (y1 >= h) y1 = h - 1;
                if (x0 < 0) x0 = 0;
                if (x1 >= w) x1 = w - 1;
                for (int yy = y0; yy <= y1 && v == 0; yy++)
                {
                    int row = yy * w;
                    for (int xx = x0; xx <= x1; xx++)
                        if (src[row + xx] != 0) { v = 1; break; }
                }
                dst[y * w + x] = v;
            }
        }

        static void ScanlineFill(byte[] mask, Color[] pix, int w, int h)
        {
            var skin = new Color(0.78f, 0.56f, 0.44f, 1f);
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                int lo = -1, hi = -1;
                for (int x = 0; x < w; x++)
                {
                    if (mask[row + x] == 0) continue;
                    if (lo < 0) lo = x;
                    hi = x;
                }
                if (lo < 0) continue;
                for (int x = lo; x <= hi; x++)
                {
                    int i = row + x;
                    if (mask[i] != 0)
                    {
                        var p = pix[i];
                        p.a = 1f;
                        pix[i] = p;
                        continue;
                    }
                    Color fill = skin;
                    if (x > lo && mask[i - 1] != 0) fill = pix[i - 1];
                    else if (y > 0 && mask[i - w] != 0) fill = pix[i - w];
                    fill.a = 1f;
                    pix[i] = fill;
                    mask[i] = 1;
                }
            }
            for (int x = 0; x < w; x++)
            {
                int lo = -1, hi = -1;
                for (int y = 0; y < h; y++)
                {
                    if (mask[y * w + x] == 0) continue;
                    if (lo < 0) lo = y;
                    hi = y;
                }
                if (lo < 0) continue;
                for (int y = lo; y <= hi; y++)
                {
                    int i = y * w + x;
                    if (mask[i] != 0)
                    {
                        var p = pix[i];
                        p.a = 1f;
                        pix[i] = p;
                        continue;
                    }
                    Color fill = skin;
                    if (y > lo && mask[i - w] != 0) fill = pix[i - w];
                    fill.a = 1f;
                    pix[i] = fill;
                    mask[i] = 1;
                }
            }
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

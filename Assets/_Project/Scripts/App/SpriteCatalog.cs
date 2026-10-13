using UnityEngine;

namespace FlockFive
{
    public static class SpriteCatalog
    {
        static Sprite _bg, _branch, _branchGift, _leaf, _vine, _petalPink, _petalPeach, _firefly, _glow, _rain, _smoke, _blanket, _zee, _sparkle, _moon, _logo, _bee, _beeFlap, _feather, _bow, _crown, _hive, _playFlower, _adSign, _adBulb, _adCard, _iceA, _iceB, _iceShard, _restart, _piggy, _coin, _poker, _cardBack, _cardPaper, _handFan, _handFanFront, _handPluck, _handPalm, _handPinky, _handRing, _handMiddle, _handIndex, _handThumb, _handThumbShadow, _dash, _chain, _padlock, _stampRing, _waxSeal, _joker, _clipboard, _sparrow, _sparrowFlap1, _sparrowFlap2, _hawk, _pokerFaceWild;
        static Sprite[] _pokerFaces;
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
        static Sprite[] _kitFlap3;
        static Sprite[] _kitFlap4;
        static Sprite[] _kitFlap5;
        static Sprite[] _flap3;
        static Sprite[] _flap4;
        static Sprite[] _flap5;
        static Sprite[] _feeders;

        public static Sprite GardenBg => Load(ref _bg, "Sprites/bg_garden", 96f);

        // Garden backdrops. bg_garden (summer) stays cached for good: the home wash and
        // every summer garden use it. The other paintings (desert, fall, winter, spring)
        // share ONE slot, so at most one of them is in memory at a time.
        static Sprite _bgScene;
        static string _bgScenePath;
        static bool _bgSceneOwned;

        static string BgPath(GardenScene scene)
        {
            switch (scene)
            {
                case GardenScene.Desert: return "Sprites/bg_oasis";
                case GardenScene.Fall: return "Sprites/bg_fall";
                case GardenScene.Winter: return "Sprites/bg_winter";
                case GardenScene.Spring: return "Sprites/bg_spring";
                default: return null;
            }
        }

        // The only garden-backdrop picker. GardenSeason.ForLevel owns the schedule.
        public static Sprite GardenBgFor(int gardenNumber) => GardenBgFor(GardenSeason.ForLevel(gardenNumber));

        public static Sprite GardenBgFor(GardenScene scene)
        {
            string path = BgPath(scene);
            if (path == null)
            {
                // Summer: the previous season painting is no longer in use.
                ReleaseSceneBg(null);
                return GardenBg;
            }
            if (_bgScene != null && _bgScenePath == path) return _bgScene;
            var next = TryLoad(path, 96f);
            ReleaseSceneBg(next);
            if (next == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("Missing sprite " + path + " (using bg_garden)");
#endif
                return GardenBg;
            }
            _bgScene = next;
            _bgScenePath = path;
            // TryLoad builds a runtime Sprite when only a Texture2D imports at the path.
            _bgSceneOwned = next != Resources.Load<Sprite>(path);
            return next;
        }

        // Drop the season slot and unload its texture, unless something the catalog
        // still holds (bg_garden, or the painting replacing it) uses the same texture.
        // The old garden's Bg renderer was Destroy()ed just before WorldBuilder.Build
        // asked for the new backdrop; that destroy lands before the frame renders, and
        // Unity reloads an unloaded asset if anything does touch it again.
        static void ReleaseSceneBg(Sprite keep)
        {
            var old = _bgScene;
            bool owned = _bgSceneOwned;
            _bgScene = null;
            _bgScenePath = null;
            _bgSceneOwned = false;
            if (old == null || old == keep) return;
            var tex = old.texture;
            bool shared = tex == null
                || (_bg != null && _bg.texture == tex)
                || (keep != null && keep.texture == tex);
            if (owned) Object.Destroy(old);
            if (!shared) Resources.UnloadAsset(tex);
        }

        // Next season, held beside the live slot so warming it does not unload
        // the garden that is still on screen. Summer is GardenBg and stays put.
        // TakeHeldScene detaches the outgoing painting so GardenBgFor can load the
        // next one without unloading that pin mid-crossfade.
        static Sprite _nextBgHold;
        static string _nextBgPath;

        public static bool WarmNextScene(GardenScene scene)
        {
            string path = BgPath(scene);
            if (path == null)
            {
                var summer = GardenBg;
                return summer != null;
            }
            if (_bgScene != null && _bgScenePath == path) return true;
            if (_nextBgHold != null && _nextBgPath == path) return true;
            var spr = TryLoad(path, 96f);
            if (spr == null) return false;
            if (spr == _bgScene) return true;
            _nextBgHold = spr;
            _nextBgPath = path;
            return true;
        }

        // Card back, then one face per color the next deal can draw. Not every
        // sex sheet, foil, or hand. Caller steps once per frame.
        public const int DealColorSteps = 1 + Palette.Max;

        public static Sprite WarmDealColor(int step)
        {
            if (step <= 0) return CardBack;
            int color = step - 1;
            if (color < 0 || color >= Palette.Max) return null;
            return PokerFace(BirdPoker.Card.Of((BirdColor)color, BirdSex.Neutral));
        }

        static Sprite _bgHeld;
        static bool _bgHeldOwned;

        public static Sprite TakeHeldScene(GardenScene prev)
        {
            string path = BgPath(prev);
            if (path == null || _bgScene == null || _bgScenePath != path) return null;
            var spr = _bgScene;
            bool owned = _bgSceneOwned;
            _bgScene = null;
            _bgScenePath = null;
            _bgSceneOwned = false;
            if (_bgHeld != null && _bgHeld != spr) ReleaseHeld();
            _bgHeld = spr;
            _bgHeldOwned = owned;
            return spr;
        }

        public static void ReleaseHeld()
        {
            var old = _bgHeld;
            bool owned = _bgHeldOwned;
            _bgHeld = null;
            _bgHeldOwned = false;
            if (old == null) return;
            var tex = old.texture;
            bool shared = tex == null
                || (_bg != null && _bg.texture == tex)
                || (_bgScene != null && _bgScene.texture == tex);
            if (owned) Object.Destroy(old);
            if (!shared) Resources.UnloadAsset(tex);
        }

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
            if (_sparrowPlaceholder) return BirdFrame(BirdColor.Violet, t * 0.25f, true);
            var up = Load(ref _sparrowFlap1, "Sprites/fx_sparrow_1", 200f);
            var mid = Load(ref _sparrowFlap2, "Sprites/fx_sparrow_2", 200f);
            // t counts wingbeats (one rest-up-mid-up cycle per unit).
            int k = Mathf.FloorToInt(Mathf.Abs(t) * 4f) % 4;
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
            if (_hawkPlaceholder) return BirdFrame(BirdColor.Ruby, t * 0.25f, true);
            // Single-frame art for now (no flap sheet yet).
            return rest;
        }
        public static Sprite Bow => Load(ref _bow, "Sprites/fx_bow", 200f);
        // Legacy constant. Worn bows take their size from BirdIdle.KitFit,
        // which follows HeadSpan of the body instead of this fixed scale.
        public const float BowScale = 0.42f * 1.12f;
        public static Sprite Crown => Load(ref _crown, "Sprites/fx_crown", 200f);
        static readonly Sprite[] _crownByColor = new Sprite[5];
        static readonly string[] CrownNames = { "ruby", "gold", "teal", "violet", "peach" };
        /// <summary>Crown whose jewels match the wearer's plumage; falls back to fx_crown.</summary>
        public static Sprite CrownFor(BirdColor c)
        {
            int i = (int)c;
            if (i < 0 || i >= _crownByColor.Length) return Crown;
            if (_crownByColor[i] == null) _crownByColor[i] = TryLoad("Sprites/fx_crown_" + CrownNames[i], 200f);
            return _crownByColor[i] != null ? _crownByColor[i] : Crown;
        }

        // Worn kit. Crown and bow are the only accessories. Plain draws nothing.
        public static Sprite KitSprite(BirdColor color, BirdKit worn)
        {
            switch (worn)
            {
                case BirdKit.Crown: return CrownFor(color);
                case BirdKit.Bow: return Bow;
                default: return null;
            }
        }

        // Rest plus flap frames of the shared hummingbird sheet. Frame 5 has no art
        // and uses the rest pose. Every color but peach aliases this silhouette.
        static Sprite[] _rubyPose;

        public static Sprite BodyPose(int frame)
        {
            if (frame < 0 || frame > 5) frame = 0;
            if (frame == 0 || frame == 5) return Bird(BirdColor.Ruby);
            if (_rubyPose == null) _rubyPose = new Sprite[5];
            int i = frame;
            if (_rubyPose[i] == null)
                _rubyPose[i] = TryLoad("Sprites/bird_ruby_" + frame, 220f);
            return _rubyPose[i] != null ? _rubyPose[i] : Bird(BirdColor.Ruby);
        }

        // Build 85: the flap frame whose skull seats the kit for every flap frame of that
        // bird (bird_<color>[_f]_2..4 -> _1). Rest and unknown sprites come back unchanged.
        static readonly System.Collections.Generic.Dictionary<int, Sprite> _seatBody = new System.Collections.Generic.Dictionary<int, Sprite>();
        public static Sprite FlapSeatBody(Sprite s)
        {
            if (s == null) return null;
            int id = s.GetInstanceID();
            if (_seatBody.TryGetValue(id, out var hit)) return hit;
            var res = s;
            string n = s.texture != null ? s.texture.name : s.name;
            if (n.StartsWith("bird_") && n.Length > 2 && n[n.Length - 2] == '_')
            {
                char k = n[n.Length - 1];
                if (k >= '2' && k <= '4')
                {
                    var one = TryLoad("Sprites/" + n.Substring(0, n.Length - 1) + "1", s.pixelsPerUnit);
                    if (one != null) res = one;
                }
            }
            _seatBody[id] = res;
            return res;
        }

        // Highest opaque pixel of the head column, between the eye and the back of
        // the skull, ignoring the beak and raised wings. Bird-local units. Cached
        // per sprite so the kit can ride every flap frame without scanning again.
        struct DomeHit
        {
            public int Id;
            public Vector2 Point;
        }

        static DomeHit[] _domes;
        static int _domeN;

        public static Vector2 HeadDome(Sprite sprite)
        {
            if (sprite == null) return new Vector2(0.22f, 1.20f);
            int id = sprite.GetInstanceID();
            if (_domes != null)
            {
                for (int i = 0; i < _domeN; i++)
                    if (_domes[i].Id == id) return _domes[i].Point;
            }
            var point = ScanHeadDome(sprite);
            if (_domes == null) _domes = new DomeHit[8];
            if (_domeN >= _domes.Length)
            {
                var grow = new DomeHit[_domes.Length * 2];
                for (int i = 0; i < _domes.Length; i++) grow[i] = _domes[i];
                _domes = grow;
            }
            _domes[_domeN++] = new DomeHit { Id = id, Point = point };
            return point;
        }

        // Skull width in the sprite's local units. The same dome walk as
        // HeadDome, then the columns whose tops still sit on that crown.
        // A raised wing is too high and the beak is too low, so neither
        // stretches the fit. Cached per sprite.
        struct SpanHit
        {
            public int Id;
            public float Span;
        }

        static SpanHit[] _spans;
        static int _spanN;

        public static float HeadSpan(Sprite sprite)
        {
            if (sprite == null) return 0.90f;
            int id = sprite.GetInstanceID();
            if (_spans != null)
            {
                for (int i = 0; i < _spanN; i++)
                    if (_spans[i].Id == id) return _spans[i].Span;
            }
            float span = ScanHeadSpan(sprite);
            if (_spans == null) _spans = new SpanHit[8];
            if (_spanN >= _spans.Length)
            {
                var grow = new SpanHit[_spans.Length * 2];
                for (int i = 0; i < _spans.Length; i++) grow[i] = _spans[i];
                _spans = grow;
            }
            _spans[_spanN++] = new SpanHit { Id = id, Span = span };
            return span;
        }

        static float ScanHeadSpan(Sprite sprite)
        {
            var dome = HeadDome(sprite);
            float ppu = sprite.pixelsPerUnit > 1f ? sprite.pixelsPerUnit : 220f;
            float fallback = sprite.rect.height > 8f ? sprite.rect.height / ppu * 0.18f : 0.90f;
            var tex = sprite.texture;
            if (tex == null || !tex.isReadable) return fallback;
            var rect = sprite.rect;
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            if (w < 8 || h < 8) return fallback;
            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (UnityException) { return fallback; }
            if (px == null || px.Length < tex.width * tex.height) return fallback;
            float pivX = sprite.pivot.x;
            float pivY = sprite.pivot.y;
            int ox = Mathf.RoundToInt(rect.x);
            int oy = Mathf.RoundToInt(rect.y);
            int tw = tex.width;
            int x0 = Mathf.RoundToInt((dome.x - 0.95f) * ppu + pivX - 0.5f);
            int x1 = Mathf.RoundToInt((dome.x + 0.95f) * ppu + pivX - 0.5f);
            if (x0 < 0) x0 = 0;
            if (x1 >= w) x1 = w - 1;
            float lo = 999f;
            float hi = -999f;
            int n = 0;
            for (int x = x0; x <= x1; x++)
            {
                float top = ColumnTop(px, tw, ox, oy, x, h, pivY, ppu);
                if (top < dome.y - 0.28f || top > dome.y + 0.06f) continue;
                if (ColumnThick(px, tw, ox, oy, x, h, ppu) < 0.20f) continue;
                float lx = (x + 0.5f - pivX) / ppu;
                if (lx < lo) lo = lx;
                if (lx > hi) hi = lx;
                n++;
            }
            if (n < 4 || hi <= lo) return fallback;
            return hi - lo;
        }

        static Vector2 ScanHeadDome(Sprite sprite)
        {
            var fallback = new Vector2(0.22f, 1.20f);
            var tex = sprite.texture;
            if (tex == null || !tex.isReadable) return fallback;
            var rect = sprite.rect;
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            if (w < 8 || h < 8) return fallback;
            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (UnityException) { return fallback; }
            if (px == null || px.Length < tex.width * tex.height) return fallback;
            float ppu = sprite.pixelsPerUnit > 1f ? sprite.pixelsPerUnit : 220f;
            float pivX = sprite.pivot.x;
            float pivY = sprite.pivot.y;
            int ox = Mathf.RoundToInt(rect.x);
            int oy = Mathf.RoundToInt(rect.y);
            int tw = tex.width;
            int beak = -1;
            // Rightmost thick column is the beak root. The beak itself is thinner.
            for (int x = w - 1; x >= 0; x--)
            {
                if (ColumnThick(px, tw, ox, oy, x, h, ppu) > 0.40f)
                {
                    beak = x;
                    break;
                }
            }
            if (beak < 0) return fallback;
            float peak = -999f;
            float bestX = 0f;
            float bestY = 0f;
            bool any = false;
            int sincePeak = 0;
            float beakX = (beak + 0.5f - pivX) / ppu;
            for (int x = beak; x >= 0; x--)
            {
                float lx = (x + 0.5f - pivX) / ppu;
                if (beakX - lx > 0.85f) break;
                float thick = ColumnThick(px, tw, ox, oy, x, h, ppu);
                if (thick < 0.35f) { sincePeak++; continue; }
                float top = ColumnTop(px, tw, ox, oy, x, h, pivY, ppu);
                if (any && top > peak + 0.42f) break;
                if (any && sincePeak > 8 && top < peak - 0.30f) break;
                if (!any || top >= peak)
                {
                    peak = top;
                    bestX = lx;
                    bestY = top;
                    sincePeak = 0;
                    any = true;
                }
                else sincePeak++;
            }
            if (!any) return fallback;
            // Center of the crown plateau. bestX is only the back edge of the max.
            float minX = 999f;
            float maxX = -999f;
            int band = 0;
            for (int x = beak; x >= 0; x--)
            {
                float lx = (x + 0.5f - pivX) / ppu;
                if (beakX - lx > 0.85f) break;
                if (ColumnThick(px, tw, ox, oy, x, h, ppu) < 0.35f) continue;
                float top = ColumnTop(px, tw, ox, oy, x, h, pivY, ppu);
                if (top > peak + 0.18f) break;
                if (top < peak - 0.04f) continue;
                if (lx < minX) minX = lx;
                if (lx > maxX) maxX = lx;
                band++;
            }
            if (band >= 3) return new Vector2((minX + maxX) * 0.5f, peak);
            return new Vector2(bestX, bestY);
        }

        // Degrees the skull leans toward +X (the beak on right-facing art).
        // Wings above the crown are ignored. 0 when the pose has no readable skull.
        struct TiltHit
        {
            public int Id;
            public float Deg;
        }

        static TiltHit[] _tilts;
        static int _tiltN;

        public static float HeadTilt(Sprite sprite)
        {
            if (sprite == null) return 0f;
            int id = sprite.GetInstanceID();
            if (_tilts != null)
            {
                for (int i = 0; i < _tiltN; i++)
                    if (_tilts[i].Id == id) return _tilts[i].Deg;
            }
            float deg = ScanHeadTilt(sprite);
            if (_tilts == null) _tilts = new TiltHit[8];
            if (_tiltN >= _tilts.Length)
            {
                var grow = new TiltHit[_tilts.Length * 2];
                for (int i = 0; i < _tilts.Length; i++) grow[i] = _tilts[i];
                _tilts = grow;
            }
            _tilts[_tiltN++] = new TiltHit { Id = id, Deg = deg };
            return deg;
        }

        static float ScanHeadTilt(Sprite sprite)
        {
            var dome = HeadDome(sprite);
            var tex = sprite.texture;
            if (tex == null || !tex.isReadable) return 0f;
            var rect = sprite.rect;
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            if (w < 8 || h < 8) return 0f;
            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (UnityException) { return 0f; }
            if (px == null || px.Length < tex.width * tex.height) return 0f;
            float ppu = sprite.pixelsPerUnit > 1f ? sprite.pixelsPerUnit : 220f;
            float pivX = sprite.pivot.x;
            float pivY = sprite.pivot.y;
            int ox = Mathf.RoundToInt(rect.x);
            int oy = Mathf.RoundToInt(rect.y);
            int tw = tex.width;
            float sx = 0f;
            float sy = 0f;
            int n = 0;
            for (int x = 0; x < w; x++)
            {
                float lx = (x + 0.5f - pivX) / ppu;
                if (lx < dome.x - 0.42f || lx > dome.x + 0.42f) continue;
                if (ColumnThick(px, tw, ox, oy, x, h, ppu) < 0.28f) continue;
                float top = ColumnTop(px, tw, ox, oy, x, h, pivY, ppu);
                if (top > dome.y + 0.06f || top < dome.y - 0.32f) continue;
                sx += lx;
                sy += top - 0.16f;
                n++;
            }
            if (n < 4) return 0f;
            float ux = dome.x - sx / n;
            float uy = dome.y - sy / n;
            if (uy < 0.02f) return 0f;
            float ang = Mathf.Atan2(ux, uy) * Mathf.Rad2Deg;
            if (ang > 26f) ang = 26f;
            if (ang < -6f) ang = -6f;
            return ang;
        }

        // Point on the kit, in the kit's local units, that sits on the skull.
        // Crown: center of the base. Bow: the knot (center of the loop band),
        // not the tail-weighted opaque box.
        struct KnotHit
        {
            public int Id;
            public int Worn;
            public Vector2 Point;
        }

        static KnotHit[] _knots;
        static int _knotN;

        public static Vector2 KitSeatPoint(Sprite kit, BirdKit worn)
        {
            if (kit == null) return Vector2.zero;
            int id = kit.GetInstanceID();
            int kind = (int)worn;
            if (_knots != null)
            {
                for (int i = 0; i < _knotN; i++)
                    if (_knots[i].Id == id && _knots[i].Worn == kind) return _knots[i].Point;
            }
            var point = ScanKitSeat(kit, worn);
            if (_knots == null) _knots = new KnotHit[8];
            if (_knotN >= _knots.Length)
            {
                var grow = new KnotHit[_knots.Length * 2];
                for (int i = 0; i < _knots.Length; i++) grow[i] = _knots[i];
                _knots = grow;
            }
            _knots[_knotN++] = new KnotHit { Id = id, Worn = kind, Point = point };
            return point;
        }

        static Vector2 ScanKitSeat(Sprite kit, BirdKit worn)
        {
            var box = MeasureOpaque(kit);
            if (!box.Ok) return Vector2.zero;
            float midX = (box.Left + box.Right) * 0.5f;
            if (worn != BirdKit.Bow) return new Vector2(midX, box.Bottom);
            var tex = kit.texture;
            if (tex == null || !tex.isReadable) return new Vector2(midX, (box.Bottom + box.Top) * 0.5f);
            var rect = kit.rect;
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            if (w < 4 || h < 4) return new Vector2(midX, (box.Bottom + box.Top) * 0.5f);
            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (UnityException) { return new Vector2(midX, (box.Bottom + box.Top) * 0.5f); }
            if (px == null) return new Vector2(midX, (box.Bottom + box.Top) * 0.5f);
            float ppu = kit.pixelsPerUnit > 1f ? kit.pixelsPerUnit : 200f;
            float pivX = kit.pivot.x;
            float pivY = kit.pivot.y;
            int ox = Mathf.RoundToInt(rect.x);
            int oy = Mathf.RoundToInt(rect.y);
            int tw = tex.width;
            int max = 0;
            var rows = new int[h];
            for (int y = 0; y < h; y++)
            {
                int row = (oy + y) * tw;
                int count = 0;
                for (int x = 0; x < w; x++)
                {
                    int i = row + ox + x;
                    if ((uint)i < (uint)px.Length && px[i].a > 28) count++;
                }
                rows[y] = count;
                if (count > max) max = count;
            }
            if (max < 4) return new Vector2(midX, (box.Bottom + box.Top) * 0.5f);
            int thresh = (int)(max * 0.80f);
            int y0 = -1;
            int y1 = -1;
            for (int y = 0; y < h; y++)
            {
                if (rows[y] < thresh) continue;
                if (y0 < 0) y0 = y;
                y1 = y;
            }
            if (y0 < 0) return new Vector2(midX, (box.Bottom + box.Top) * 0.5f);
            double sumX = 0.0;
            int n = 0;
            for (int y = y0; y <= y1; y++)
            {
                int row = (oy + y) * tw;
                for (int x = 0; x < w; x++)
                {
                    int i = row + ox + x;
                    if ((uint)i >= (uint)px.Length || px[i].a <= 28) continue;
                    sumX += x;
                    n++;
                }
            }
            if (n < 4) return new Vector2(midX, (box.Bottom + box.Top) * 0.5f);
            float knotX = ((float)(sumX / n) + 0.5f - pivX) / ppu;
            float knotY = ((y0 + y1) * 0.5f + 0.5f - pivY) / ppu;
            return new Vector2(knotX, knotY);
        }

        static float ColumnThick(Color32[] px, int tw, int ox, int oy, int x, int h, float ppu)
        {
            int n = 0;
            int tx = ox + x;
            for (int y = 0; y < h; y++)
            {
                int i = (oy + y) * tw + tx;
                if ((uint)i < (uint)px.Length && px[i].a > 28) n++;
            }
            return n / ppu;
        }

        static float ColumnTop(Color32[] px, int tw, int ox, int oy, int x, int h, float pivY, float ppu)
        {
            int tx = ox + x;
            for (int y = h - 1; y >= 0; y--)
            {
                int i = (oy + y) * tw + tx;
                if ((uint)i < (uint)px.Length && px[i].a > 28)
                    return (y + 0.5f - pivY) / ppu;
            }
            return -999f;
        }

        // Opaque box in the sprite's local units. Crown base and bow loops seat from this.
        public struct OpaqueBox
        {
            public float Left, Right, Bottom, Top;
            public bool Ok;
            public float Width => Right - Left;
            public float Height => Top - Bottom;
        }

        struct BoxHit
        {
            public int Id;
            public OpaqueBox Box;
        }

        static BoxHit[] _boxes;
        static int _boxN;

        public static OpaqueBox MeasureOpaque(Sprite sprite)
        {
            if (sprite == null) return default;
            int id = sprite.GetInstanceID();
            if (_boxes != null)
            {
                for (int i = 0; i < _boxN; i++)
                    if (_boxes[i].Id == id) return _boxes[i].Box;
            }
            var box = ScanOpaque(sprite);
            if (_boxes == null) _boxes = new BoxHit[8];
            if (_boxN >= _boxes.Length)
            {
                var grow = new BoxHit[_boxes.Length * 2];
                for (int i = 0; i < _boxes.Length; i++) grow[i] = _boxes[i];
                _boxes = grow;
            }
            _boxes[_boxN++] = new BoxHit { Id = id, Box = box };
            return box;
        }

        static OpaqueBox ScanOpaque(Sprite sprite)
        {
            var tex = sprite.texture;
            if (tex == null || !tex.isReadable) return default;
            var rect = sprite.rect;
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            if (w < 2 || h < 2) return default;
            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (UnityException) { return default; }
            if (px == null) return default;
            float ppu = sprite.pixelsPerUnit > 1f ? sprite.pixelsPerUnit : 200f;
            float pivX = sprite.pivot.x;
            float pivY = sprite.pivot.y;
            int ox = Mathf.RoundToInt(rect.x);
            int oy = Mathf.RoundToInt(rect.y);
            int tw = tex.width;
            int minX = w, maxX = -1, minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                int row = (oy + y) * tw;
                for (int x = 0; x < w; x++)
                {
                    int i = row + ox + x;
                    if ((uint)i >= (uint)px.Length || px[i].a <= 28) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (maxX < 0) return default;
            return new OpaqueBox
            {
                Ok = true,
                Left = (minX + 0.5f - pivX) / ppu,
                Right = (maxX + 0.5f - pivX) / ppu,
                Bottom = (minY + 0.5f - pivY) / ppu,
                Top = (maxY + 0.5f - pivY) / ppu
            };
        }
        public static Sprite Hive => Load(ref _hive, "Sprites/fx_hive", 200f);
        public static Sprite Poker => Load(ref _poker, "Sprites/fx_poker", 200f);
        public static Sprite CardBack => Load(ref _cardBack, "Sprites/fx_card_back", 200f);
        public static void DropPokerArt()
        {
            _cardBack = null;
            _handFan = null;
            _handFanFront = null;
            _handPluck = null;
            _handPalm = null;
            _handPinky = null;
            _handRing = null;
            _handMiddle = null;
            _handIndex = null;
            _handThumb = null;
            _handThumbShadow = null;
            _waxSeal = null;
            _pokerWildFoil = null;
            _padlock = null;
            _chain = null;
            _pokerFaceWild = null;
            _pokerFaces = null;
        }

        public static Sprite PokerFace(BirdPoker.Card card)
        {
            if (card.Wild)
            {
                if (_pokerFaceWild == null) _pokerFaceWild = TryLoad("Sprites/fx_poker_face_wild", 200f);
                return _pokerFaceWild;
            }
            int n = Palette.Max * 3;
            if (_pokerFaces == null) _pokerFaces = new Sprite[n];
            int ix = (int)card.Sex * Palette.Max + (int)card.Color;
            if (ix < 0 || ix >= n) return null;
            if (_pokerFaces[ix] == null)
            {
                string tag = card.Sex == BirdSex.Female ? "_f" : card.Sex == BirdSex.Male ? "_m" : "";
                _pokerFaces[ix] = TryLoad("Sprites/fx_poker_face_" + Name(card.Color) + tag, 200f);
            }
            return _pokerFaces[ix];
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
                if (_handFan == null) _handFan = TryLoad("Sprites/fx_hand_fan", 200f);
                return _handFan;
            }
        }
        public static Sprite HandFanFront
        {
            get
            {
                if (_handFanFront == null) _handFanFront = TryLoad("Sprites/fx_hand_fan_front", 200f);
                return _handFanFront;
            }
        }
        public static Sprite HandPluck
        {
            get
            {
                if (_handPluck == null) _handPluck = TryLoad("Sprites/fx_hand_pluck", 200f);
                return _handPluck;
            }
        }
        public static Sprite HandPalm
        {
            get
            {
                if (_handPalm == null) _handPalm = TryLoad("Sprites/fx_hand_palm", 200f);
                return _handPalm;
            }
        }
        public static Sprite HandPinky
        {
            get
            {
                if (_handPinky == null) _handPinky = TryLoad("Sprites/fx_hand_pinky", 200f);
                return _handPinky;
            }
        }
        public static Sprite HandRing
        {
            get
            {
                if (_handRing == null) _handRing = TryLoad("Sprites/fx_hand_ring", 200f);
                return _handRing;
            }
        }
        public static Sprite HandMiddle
        {
            get
            {
                if (_handMiddle == null) _handMiddle = TryLoad("Sprites/fx_hand_middle", 200f);
                return _handMiddle;
            }
        }
        static Sprite _studioLogo;
        // Boot studio card (ZFX Gaming), shown on white, unaltered.
        public static Sprite StudioLogo
        {
            get
            {
                if (_studioLogo == null) _studioLogo = TryLoad("Sprites/studio_zfx_logo", 100f);
                return _studioLogo;
            }
        }
        public static Sprite HandIndex
        {
            get
            {
                if (_handIndex == null) _handIndex = TryLoad("Sprites/fx_hand_index", 200f);
                return _handIndex;
            }
        }
        public static Sprite HandThumb
        {
            get
            {
                if (_handThumb == null) _handThumb = TryLoad("Sprites/fx_hand_thumb", 200f);
                return _handThumb;
            }
        }
        // Soft contact/occlusion shadow baked from the thumb alpha (same canvas as the thumb).
        public static Sprite HandThumbShadow
        {
            get
            {
                if (_handThumbShadow == null) _handThumbShadow = TryLoad("Sprites/fx_hand_thumb_shadow", 200f);
                return _handThumbShadow;
            }
        }
        static Sprite[] _pokerWildFoil;
        // Holo foil overlays for the wild face (hue-shifted thirds, crossfaded at runtime).
        public static Sprite PokerWildFoil(int i)
        {
            if (i < 0 || i > 2) return null;
            if (_pokerWildFoil == null) _pokerWildFoil = new Sprite[3];
            if (_pokerWildFoil[i] == null) _pokerWildFoil[i] = TryLoad("Sprites/fx_poker_face_wild_foil" + i, 100f);
            return _pokerWildFoil[i];
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

        // Card back, paper, palm, thumb, chain, padlock, sparkle, glow, wild, 3 foils, 15 faces.
        const int PokerArtSteps = 8 + 1 + 3 + Palette.Max * 3;
        static int _pokerArtStep;
        public static bool PokerArtReady => _pokerArtStep >= PokerArtSteps;

        // One Resources.Load per call. Caller draws the texture offscreen so the GPU upload
        // is not the first card flip.
        public static Sprite PokerArtWarmStep()
        {
            if (_pokerArtStep >= PokerArtSteps) return null;
            int i = _pokerArtStep++;
            if (i == 0) return CardBack;
            if (i == 1) return CardPaper;
            if (i == 2) return HandPalm;
            if (i == 3) return HandThumb;
            if (i == 4) return Chain;
            if (i == 5) return Padlock;
            if (i == 6) return Sparkle;
            if (i == 7) return Glow;
            if (i == 8) return PokerFace(BirdPoker.Card.MakeWild());
            if (i < 12) return PokerWildFoil(i - 9);
            int k = i - 12;
            return PokerFace(BirdPoker.Card.Of((BirdColor)(k % Palette.Max), (BirdSex)(k / Palette.Max)));
        }

        public static void PokerArtWarmFinish()
        {
            while (!PokerArtReady) PokerArtWarmStep();
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
        static Sprite _usaPlane, _usaBanner;
        static bool _usaPlaneTried, _usaBannerTried;

        // Missing art draws nothing. No fallback blob over the home sky.
        public static Sprite UsaPlane => OptionalSprite(ref _usaPlane, ref _usaPlaneTried, "Sprites/fx_usa_plane");
        public static Sprite UsaBanner => OptionalSprite(ref _usaBanner, ref _usaBannerTried, "Sprites/fx_usa_banner");

        static Sprite OptionalSprite(ref Sprite cache, ref bool tried, string path)
        {
            if (tried) return cache;
            tried = true;
            cache = TryLoad(path, 200f);
            return cache;
        }

        public static Sprite Bee => Load(ref _bee, "Sprites/fx_bee", 520f);

        public static Sprite BeeFrame(float t)
        {
            var a = Bee;
            var b = Load(ref _beeFlap, "Sprites/fx_bee_1", 520f);
            return (Mathf.FloorToInt(Mathf.Abs(t)) % 2 == 0) ? a : b;
        }

        // Punch-card mark: painted red wax seal with an embossed hummingbird (square, round art).
        // fx_stamp_tool.png (the old rubber-stamp art) stays in Resources but is no longer loaded.
        public static Sprite WaxSeal
        {
            get
            {
                if (_waxSeal == null) _waxSeal = TryLoad("Sprites/fx_wax_seal", 200f);
                return _waxSeal;
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
            _kitFlap3 = null;
            _kitFlap4 = null;
            _kitFlap5 = null;
            _flap3 = null;
            _flap4 = null;
            _flap5 = null;
            _sparrow = null;
            _sparrowFlap1 = null;
            _sparrowFlap2 = null;
            _sparrowPlaceholder = false;
            _hawk = null;
            _hawkPlaceholder = false;
        }

        // Path strings are built once. BirdFrame / Bird run for every bird every frame.
        // Sex copies of these poses are the same PNG. Lookups keep the old names;
        // TryLoad falls back to the one file that is still in Resources.
        static readonly System.Collections.Generic.Dictionary<string, string> _spriteAlias = BuildSpriteAlias();

        static System.Collections.Generic.Dictionary<string, string> BuildSpriteAlias()
        {
            var map = new System.Collections.Generic.Dictionary<string, string>();
            string[] plain = { "gold", "ruby", "teal", "violet" };
            string[] poses = { "", "_1", "_2", "_3", "_4" };
            for (int c = 0; c < plain.Length; c++)
            {
                for (int p = 0; p < poses.Length; p++)
                {
                    string keep = "Sprites/bird_" + plain[c] + poses[p];
                    map["Sprites/bird_" + plain[c] + "_f" + poses[p]] = keep;
                    map["Sprites/bird_" + plain[c] + "_m" + poses[p]] = keep;
                }
            }
            for (int p = 0; p < poses.Length; p++)
                map["Sprites/bird_peach_m" + poses[p]] = "Sprites/bird_peach_f" + poses[p];
            return map;
        }

        static readonly string[] _birdPath = new string[Palette.Max * 3];
        static readonly string[] _flapPath = new string[Palette.Max * 3 * 4];
        static readonly string[] _feederPath = new string[Palette.Max];

        static string BirdPath(BirdColor c, BirdSex sex)
        {
            int i = (int)sex * Palette.Max + (int)c;
            if ((uint)i >= (uint)_birdPath.Length) i = 0;
            var p = _birdPath[i];
            if (p != null) return p;
            if (sex == BirdSex.Female) p = "Sprites/bird_" + Name(c) + "_f";
            else if (sex == BirdSex.Male) p = "Sprites/bird_" + Name(c) + "_m";
            else p = "Sprites/bird_" + Name(c);
            _birdPath[i] = p;
            return p;
        }

        static string FlapPath(BirdColor c, BirdSex sex, int n)
        {
            int i = ((int)sex * Palette.Max + (int)c) * 4 + (n - 1);
            if ((uint)i >= (uint)_flapPath.Length) return BirdPath(c, sex);
            var p = _flapPath[i];
            if (p != null) return p;
            string tag = sex == BirdSex.Female ? "_f" : sex == BirdSex.Male ? "_m" : "";
            p = "Sprites/bird_" + Name(c) + tag + "_" + n;
            _flapPath[i] = p;
            return p;
        }

        static string FeederPath(BirdColor c)
        {
            int i = (int)c;
            if ((uint)i >= (uint)_feederPath.Length) i = 0;
            var p = _feederPath[i];
            if (p != null) return p;
            p = "Sprites/feeder_" + Name(c);
            _feederPath[i] = p;
            return p;
        }

        public static Sprite Bird(BirdColor c) => Slot(ref _birds, (int)c, BirdPath(c, BirdSex.Neutral), 280f);
        // Peach only ships female/male art. A plain (neutral) request for a color with
        // no plain sheet borrows the female sheet so it gets the full 6-pose wingbeat.
        static readonly int[] _plainArt = new int[8];
        static BirdSex PlainOr(BirdColor c, BirdSex sex)
        {
            if (sex != BirdSex.Neutral) return sex;
            int i = (int)c;
            if (i < 0 || i >= _plainArt.Length) return sex;
            if (_plainArt[i] == 0)
                _plainArt[i] = TryLoad("Sprites/bird_" + Name(c) + "_1", 280f) != null ? 1 : 2;
            return _plainArt[i] == 1 ? sex : BirdSex.Female;
        }

        public static Sprite Bird(BirdColor c, BirdSex sex)
        {
            sex = PlainOr(c, sex);
            if (sex == BirdSex.Neutral) return Bird(c);
            var got = SlotWide(ref _kitRest, KitIx(c, sex), BirdPath(c, sex), 280f);
            return got != null ? got : Bird(c);
        }
        public static Sprite Feeder(BirdColor c) => Slot(ref _feeders, (int)c, FeederPath(c), 180f);

        public static Sprite BirdFrame(BirdColor c, float t, bool flap) =>
            BirdFrame(c, t, flap, BirdSex.Neutral);

        static readonly System.Collections.Generic.HashSet<string> _missingFlap = new System.Collections.Generic.HashSet<string>();

        // pose 0 rest, 1 wings up, 2 wings mid. Uses the same loaders as the garden
        // (plain sheet, or the female sheet when a colour has no plain art).
        public static Sprite FlapPose(BirdColor c, int pose) => FlapPose(c, pose, BirdSex.Neutral);

        public static Sprite FlapPose(BirdColor c, int pose, BirdSex sex)
        {
            if (pose <= 0) return Bird(c, sex);
            sex = PlainOr(c, sex);
            LoadFlaps(c, sex, out var f1, out var f2, out _, out _);
            if (pose == 2)
                return f2 != null ? f2 : (f1 != null ? f1 : Bird(c, sex));
            return f1 != null ? f1 : Bird(c, sex);
        }

        public static Sprite BirdFrame(BirdColor c, float t, bool flap, BirdSex sex)
        {
            FlapPair(c, sex, t, flap, out var body, out _, out _);
            return body;
        }

        // Current pose, the next pose in the same cycle, and the 0..1 phase
        // inside this step. BirdIdle.KitFollow lerps the crown and bow across
        // that phase so the accessory does not snap when the sheet steps.
        // Not flapping: both sprites are the rest pose and phase is 0.
        public static void FlapPair(BirdColor c, BirdSex sex, float t, bool flap,
            out Sprite body, out Sprite next, out float phase)
        {
            sex = PlainOr(c, sex);
            var rest = Bird(c, sex);
            phase = 0f;
            if (!flap)
            {
                body = next = rest;
                return;
            }
            LoadFlaps(c, sex, out var f1, out var f2, out var f3, out var f4);
            // 6-pose wingbeat when the in-betweens exist: _3 lowered, _4 half-raised.
            // Same beat as the 2-pose cycle, order 3,1,4,2,4,1. No rest in flight.
            bool six = f3 != null && f4 != null && f1 != null && f2 != null;
            if (six)
            {
                FlapStep(t, true, out int pose, out int nxt, out phase);
                body = FlapPick(pose, f1, f2, f3, f4) ?? rest;
                next = FlapPick(nxt, f1, f2, f3, f4) ?? body;
                return;
            }
            if (f1 == null)
            {
                body = next = rest;
                return;
            }
            if (f2 == null)
            {
                body = next = f1;
                return;
            }
            FlapStep(t, false, out int pose2, out int nxt2, out phase);
            body = pose2 == 2 ? f2 : f1;
            next = nxt2 == 2 ? f2 : f1;
        }

        static void LoadFlaps(BirdColor c, BirdSex sex, out Sprite f1, out Sprite f2, out Sprite f3, out Sprite f4)
        {
            int ci = (int)c;
            if (ci < 0 || ci >= Palette.Max) ci = 0;
            if (sex == BirdSex.Neutral)
            {
                f1 = Slot(ref _flap1, ci, FlapPath(c, sex, 1), 280f);
                f2 = Slot(ref _flap2, ci, FlapPath(c, sex, 2), 280f);
                f3 = OptPlain(ref _flap3, ci, FlapPath(c, sex, 3));
                f4 = OptPlain(ref _flap4, ci, FlapPath(c, sex, 4));
                return;
            }
            int ix = KitIx(c, sex);
            f1 = SlotWide(ref _kitUp, ix, FlapPath(c, sex, 1), 280f);
            f2 = SlotWide(ref _kitMid, ix, FlapPath(c, sex, 2), 280f);
            f3 = SlotWide(ref _kitFlap3, ix, FlapPath(c, sex, 3), 280f);
            f4 = SlotWide(ref _kitFlap4, ix, FlapPath(c, sex, 4), 280f);
        }

        // One pose (1..4) of a bird's wingbeat through the same loaders the garden uses
        // (plain sheet, or the female sheet for colours without one, e.g. peach).
        public static Sprite BirdPose(BirdColor c, int pose)
        {
            var sex = PlainOr(c, BirdSex.Neutral);
            LoadFlaps(c, sex, out var f1, out var f2, out var f3, out var f4);
            return FlapPick(pose, f1, f2, f3, f4) ?? Bird(c, sex);
        }

        static Sprite FlapPick(int pose, Sprite f1, Sprite f2, Sprite f3, Sprite f4)
        {
            switch (pose)
            {
                case 1: return f1;
                case 2: return f2;
                case 3: return f3;
                case 4: return f4;
                default: return null;
            }
        }

        // step is floor(|t| * rate) mod the cycle. phase is the fraction of this step.
        static void FlapStep(float t, bool six, out int pose, out int next, out float phase)
        {
            float rate = six ? 24f : 8f;
            int n = six ? 6 : 2;
            float u = Mathf.Abs(t) * rate;
            int step = Mathf.FloorToInt(u);
            phase = u - step;
            if (phase < 0f) phase = 0f;
            int i = step % n;
            if (i < 0) i += n;
            if (!six)
            {
                pose = i == 0 ? 1 : 2;
                next = i == 0 ? 2 : 1;
                return;
            }
            pose = Flap6(i);
            next = Flap6((i + 1) % 6);
        }

        static int Flap6(int i)
        {
            switch (i)
            {
                case 0: return 3;
                case 1: return 1;
                case 2: return 4;
                case 3: return 2;
                case 4: return 4;
                default: return 1;
            }
        }

        // Optional in-betweens must not fall back to a placeholder sprite.
        static Sprite OptPlain(ref Sprite[] arr, int i, string path)
        {
            if (arr == null) arr = new Sprite[Palette.Max];
            if ((uint)i >= (uint)arr.Length) return null;
            if (arr[i] == null && !_missingFlap.Contains(path))
            {
                arr[i] = TryLoad(path, 280f);
                if (arr[i] == null) _missingFlap.Add(path);
            }
            return arr[i];
        }

        // Which baked pose this sprite is (0 rest, 1–4 flaps). Reference compare, no name string.
        public static int PoseIndex(Sprite spr, BirdColor c, BirdSex sex)
        {
            if (spr == null) return 0;
            sex = PlainOr(c, sex);
            int ci = (int)c;
            if (ci < 0 || ci >= Palette.Max) return 0;
            if (sex == BirdSex.Neutral)
            {
                if (Hit(_flap1, ci, spr)) return 1;
                if (Hit(_flap2, ci, spr)) return 2;
                if (Hit(_flap3, ci, spr)) return 3;
                if (Hit(_flap4, ci, spr)) return 4;
                if (Hit(_flap5, ci, spr)) return 5;
                return 0;
            }
            int ix = KitIx(c, sex);
            if (Hit(_kitUp, ix, spr)) return 1;
            if (Hit(_kitMid, ix, spr)) return 2;
            if (Hit(_kitFlap3, ix, spr)) return 3;
            if (Hit(_kitFlap4, ix, spr)) return 4;
            if (Hit(_kitFlap5, ix, spr)) return 5;
            return 0;
        }

        static bool Hit(Sprite[] arr, int i, Sprite spr) =>
            arr != null && (uint)i < (uint)arr.Length && arr[i] == spr;

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
            if (arr[i] == null && !_missingFlap.Contains(path))
            {
                arr[i] = TryLoad(path, ppu);
                if (arr[i] == null) _missingFlap.Add(path);
            }
            return arr[i];
        }

        // Honey badger art: Sprites/Badger/<name> (badger_*, hive_*, honey_splat, bee_1..5).
        // File names are stable so polished art drops in. Missing art returns null and the
        // caller skips the draw (no placeholder over the contest).
        static readonly System.Collections.Generic.Dictionary<string, Sprite> _badgerArt =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        public static Sprite BadgerArt(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_badgerArt.TryGetValue(name, out var got) && got != null) return got;
            got = TryLoad("Sprites/Badger/" + name, 200f);
            if (got == null)
            {
                _badgerArt.Remove(name);
                return null;
            }
            _badgerArt[name] = got;
            return got;
        }

        // Every fight and idle frame, before the first swap. A miss is not cached.
        public static int PreloadBadgerArt()
        {
            int n = 0;
            var names = SpriteAnim.Frames;
            for (int i = 0; i < names.Length; i++)
                if (BadgerArt(names[i]) != null) n++;
            return n;
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
            var trace = StartupTrace.BeginSprite(path);
            try
            {
            var ready = Resources.Load<Sprite>(path);
            Texture2D tex = null;
            if (ready == null)
            {
                tex = Resources.Load<Texture2D>(path);
                if (tex == null && path != null && _spriteAlias.TryGetValue(path, out var alias))
                {
                    path = alias;
                    ready = Resources.Load<Sprite>(path);
                    if (ready == null) tex = Resources.Load<Texture2D>(path);
                }
            }
            if (ready != null) return ready;
            if (tex == null) return null;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pivot = new Vector2(0.5f, 0.5f);
            if (path.Contains("fx_vine")) pivot = new Vector2(0.5f, 0.94f);
            else if (path.Contains("fx_leaf")) pivot = new Vector2(0.5f, 0.08f);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu, 0, SpriteMeshType.FullRect);
            }
            finally
            {
                trace.Dispose();
            }
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
                // Straight alpha with a black or white matte fringes when the bust
                // zooms the bird. Un-premultiply only pixels that are actually stored
                // premultiplied, then dilate the opaque colour into the matte.
                UnpremultiplyFringe(pix);
                SealFringe(pix, w, h);
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
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, src.pixelsPerUnit, 1, SpriteMeshType.Tight);
        }

        // Premultiplied fringe is dark RGB that never exceeds alpha. Straight-alpha
        // art (rgb > a) is left alone so we don't blow the edge out to white.
        static void UnpremultiplyFringe(Color[] pix)
        {
            for (int i = 0; i < pix.Length; i++)
            {
                var p = pix[i];
                if (p.a < 0.04f || p.a > 0.65f) continue;
                if (p.r > p.a + 0.02f || p.g > p.a + 0.02f || p.b > p.a + 0.02f) continue;
                if (p.maxColorComponent > 0.4f) continue;
                float a = Mathf.Max(0.04f, p.a);
                pix[i] = new Color(
                    Mathf.Clamp01(p.r / a),
                    Mathf.Clamp01(p.g / a),
                    Mathf.Clamp01(p.b / a),
                    p.a);
            }
        }

        // Copy opaque RGB into empty texels and into near-white partial alpha
        // (the white matte that reads as specks once the bird is huge).
        static void SealFringe(Color[] pix, int w, int h)
        {
            var src = new Color[pix.Length];
            for (int pass = 0; pass < 4; pass++)
            {
                System.Array.Copy(pix, src, pix.Length);
                for (int y = 0; y < h; y++)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int i = row + x;
                        var p = src[i];
                        bool empty = p.a < 0.08f;
                        bool whiteMatte = p.a < 0.55f && p.r > 0.78f && p.g > 0.78f && p.b > 0.78f;
                        if (!empty && !whiteMatte) continue;
                        float bestA = -1f;
                        Color best = p;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int yy = y + dy;
                            if ((uint)yy >= (uint)h) continue;
                            int nrow = yy * w;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int xx = x + dx;
                                if ((uint)xx >= (uint)w) continue;
                                var n = src[nrow + xx];
                                if (n.a <= bestA) continue;
                                bestA = n.a;
                                best = n;
                            }
                        }
                        if (bestA > p.a)
                            pix[i] = new Color(best.r, best.g, best.b, p.a);
                    }
                }
            }
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

        // Far wing of a right-facing sheet: the head-side wing, which is the
        // bird's left wing. flipX turns that into the right wing. Null when this
        // pose has no separate raised far wing (rest, and flaps that do not
        // clear the skull). Cached per sprite.
        struct FarHit
        {
            public int Id;
            public byte[] Mask;
            public int W, H;
            public bool Ready;
        }

        static FarHit[] _farHits;
        static int _farN;
        struct FarPair
        {
            public int Id;
            public Sprite Far, Body;
            public bool Ready;
        }

        static FarPair[] _farPairs;
        static int _farPairN;

        public static bool TryFarWingMask(Sprite sprite, out byte[] mask, out int width, out int height)
        {
            mask = null;
            width = 0;
            height = 0;
            if (sprite == null) return false;
            int id = sprite.GetInstanceID();
            if (_farHits != null)
            {
                for (int i = 0; i < _farN; i++)
                {
                    if (_farHits[i].Id != id) continue;
                    mask = _farHits[i].Mask;
                    width = _farHits[i].W;
                    height = _farHits[i].H;
                    return mask != null;
                }
            }
            var built = BuildFarWingMask(sprite, out int bw, out int bh);
            if (_farHits == null) _farHits = new FarHit[8];
            if (_farN >= _farHits.Length)
            {
                var grow = new FarHit[_farHits.Length * 2];
                for (int i = 0; i < _farHits.Length; i++) grow[i] = _farHits[i];
                _farHits = grow;
            }
            _farHits[_farN++] = new FarHit { Id = id, Mask = built, W = bw, H = bh, Ready = true };
            mask = built;
            width = bw;
            height = bh;
            return built != null;
        }

        // Punched body (near wing stays on it) and the far wing alone. Same
        // rect, pivot and pixels-per-unit, so the two quads stack.
        public static bool TrySplitFarWing(Sprite sprite, out Sprite far, out Sprite body)
        {
            far = null;
            body = null;
            if (sprite == null) return false;
            int id = sprite.GetInstanceID();
            if (_farPairs != null)
            {
                for (int i = 0; i < _farPairN; i++)
                {
                    if (_farPairs[i].Id != id) continue;
                    far = _farPairs[i].Far;
                    body = _farPairs[i].Body;
                    return far != null;
                }
            }
            Sprite farS = null;
            Sprite bodyS = null;
            if (TryFarWingMask(sprite, out var mask, out int w, out int h) && mask != null)
                SliceFarWing(sprite, mask, w, h, out farS, out bodyS);
            if (_farPairs == null) _farPairs = new FarPair[8];
            if (_farPairN >= _farPairs.Length)
            {
                var grow = new FarPair[_farPairs.Length * 2];
                for (int i = 0; i < _farPairs.Length; i++) grow[i] = _farPairs[i];
                _farPairs = grow;
            }
            _farPairs[_farPairN++] = new FarPair { Id = id, Far = farS, Body = bodyS, Ready = true };
            far = farS;
            body = bodyS;
            return farS != null;
        }

        static byte[] BuildFarWingMask(Sprite sprite, out int width, out int height)
        {
            width = 0;
            height = 0;
            var tex = sprite.texture;
            if (tex == null || !tex.isReadable) return null;
            var rect = sprite.rect;
            int ox = Mathf.RoundToInt(rect.x);
            int oy = Mathf.RoundToInt(rect.y);
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            if (w < 16 || h < 16) return null;
            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (UnityException) { return null; }
            int tw = tex.width;
            if (px == null || ox < 0 || oy < 0 || ox + w > tw || oy + h > tex.height) return null;
            var op = new bool[w * h];
            for (int y = 0; y < h; y++)
            {
                int src = (oy + y) * tw + ox;
                int dst = y * w;
                for (int x = 0; x < w; x++)
                    op[dst + x] = px[src + x].a > 28;
            }
            var dome = HeadDome(sprite);
            float ppu = sprite.pixelsPerUnit > 1f ? sprite.pixelsPerUnit : 220f;
            int domeX = Mathf.RoundToInt(dome.x * ppu + sprite.pivot.x - 0.5f);
            int domeY = Mathf.RoundToInt(dome.y * ppu + sprite.pivot.y - 0.5f);
            int above = Mathf.RoundToInt(0.55f * ppu);
            var runs = new Run8[8];
            int start = -1;
            int rn = 0;
            for (int y = h - 1; y >= 0; y--)
            {
                rn = BigRuns(op, w, y, runs);
                if (rn >= 2) { start = y; break; }
            }
            if (start < 0 || start < domeY + above) return null;
            int nearI = 0;
            int farI = rn - 1;
            if (runs[farI].A > domeX + (int)(0.15f * ppu)) return null;
            var lab = new byte[w * h];
            int far0 = runs[farI].A, far1 = runs[farI].B;
            int near0 = runs[nearI].A, near1 = runs[nearI].B;
            int seedTop = start;
            int seedBot = start;
            int farMaxX = far1;
            for (int y = start; y >= 0; y--)
            {
                rn = BigRuns(op, w, y, runs);
                if (rn < 2) break;
                int fi = BestRun(runs, rn, far0, far1);
                int ni = BestRun(runs, rn, near0, near1);
                if (fi < 0 || ni < 0 || fi == ni) break;
                if (runs[fi].A < runs[ni].A) break;
                PaintRun(lab, op, w, y, runs[fi], 1);
                PaintRun(lab, op, w, y, runs[ni], 2);
                far0 = runs[fi].A;
                far1 = runs[fi].B;
                near0 = runs[ni].A;
                near1 = runs[ni].B;
                if (far1 > farMaxX) farMaxX = far1;
                seedBot = y;
            }
            if (seedTop - seedBot < above) return null;
            int yCut = (int)(h * 0.38f);
            int xCut = farMaxX + 8;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                bool low = y < yCut;
                for (int x = 0; x < w; x++)
                {
                    int i = row + x;
                    if (!op[i] || lab[i] != 0) continue;
                    if (low || x > xCut) lab[i] = 2;
                }
            }
            var q = new int[w * h];
            int qt = 0;
            for (int i = 0; i < lab.Length; i++)
                if (lab[i] == 1 || lab[i] == 2) q[qt++] = i;
            int qh = 0;
            while (qh < qt)
            {
                int i = q[qh++];
                byte v = lab[i];
                int x = i % w;
                int y = i / w;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int ny = y + dy;
                    if ((uint)ny >= (uint)h) continue;
                    int nrow = ny * w;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        if ((uint)nx >= (uint)w) continue;
                        int j = nrow + nx;
                        if (lab[j] != 0 || !op[j]) continue;
                        lab[j] = v;
                        q[qt++] = j;
                    }
                }
            }
            int farCount = 0;
            int opaque = 0;
            int minX = w, maxX = 0, maxY = 0;
            long sumX = 0;
            for (int i = 0; i < lab.Length; i++)
            {
                if (!op[i]) { lab[i] = 0; continue; }
                opaque++;
                if (lab[i] != 1) { lab[i] = 0; continue; }
                farCount++;
                int x = i % w;
                int y = i / w;
                sumX += x;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
            if (farCount < 8000 || farCount * 2 > opaque) return null;
            if (maxX > domeX + 0.5f * ppu) return null;
            if (maxY < domeY + above * 0.5f) return null;
            if (sumX > (long)domeX * farCount) return null;
            width = w;
            height = h;
            return lab;
        }

        struct Run8
        {
            public int A, B;
        }

        static int BigRuns(bool[] op, int w, int y, Run8[] into)
        {
            int n = 0;
            int row = y * w;
            int x = 0;
            while (x < w)
            {
                while (x < w && !op[row + x]) x++;
                if (x >= w) break;
                int a = x;
                int b = x;
                while (x < w)
                {
                    if (op[row + x]) { b = x; x++; continue; }
                    int g = x;
                    while (g < w && !op[row + g] && g - x < 12) g++;
                    if (g < w && op[row + g] && g - x < 12) { x = g; continue; }
                    break;
                }
                x = b + 1;
                if (b - a + 1 < 36) continue;
                if (n < into.Length) into[n++] = new Run8 { A = a, B = b };
                else into[into.Length - 1] = new Run8 { A = a, B = b };
            }
            return n;
        }

        static int BestRun(Run8[] runs, int n, int a, int b)
        {
            int best = -1;
            int ov = 0;
            for (int i = 0; i < n; i++)
            {
                int o = Mathf.Min(runs[i].B, b) - Mathf.Max(runs[i].A, a);
                if (o > ov) { ov = o; best = i; }
            }
            return ov > 15 ? best : -1;
        }

        static void PaintRun(byte[] lab, bool[] op, int w, int y, Run8 run, byte v)
        {
            int row = y * w;
            for (int x = run.A; x <= run.B; x++)
                if (op[row + x]) lab[row + x] = v;
        }

        static void SliceFarWing(Sprite src, byte[] mask, int w, int h, out Sprite far, out Sprite body)
        {
            far = null;
            body = null;
            var tex = src.texture;
            if (tex == null) return;
            var rect = src.rect;
            int ox = Mathf.RoundToInt(rect.x);
            int oy = Mathf.RoundToInt(rect.y);
            int tw = tex.width;
            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (UnityException) { return; }
            var farPx = new Color32[w * h];
            var bodyPx = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int srcRow = (oy + y) * tw + ox;
                int dst = y * w;
                for (int x = 0; x < w; x++)
                {
                    var c = px[srcRow + x];
                    if (mask[dst + x] != 0) farPx[dst + x] = c;
                    else bodyPx[dst + x] = c;
                }
            }
            var pivot = new Vector2(
                src.pivot.x / Mathf.Max(1f, rect.width),
                src.pivot.y / Mathf.Max(1f, rect.height));
            // One pixel of the cut lives on both slices, so bilinear filtering
            // does not open a sky crack between the far wing and the body.
            OverlapCut(farPx, bodyPx, mask, w, h);
            BleedColor(farPx, w, h);
            BleedColor(bodyPx, w, h);
            far = MakeSlice(src.name + "Far", farPx, w, h, pivot, src.pixelsPerUnit);
            body = MakeSlice(src.name + "Body", bodyPx, w, h, pivot, src.pixelsPerUnit);
        }

        static void OverlapCut(Color32[] farPx, Color32[] bodyPx, byte[] mask, int w, int h)
        {
            int n = w * h;
            var ring = new byte[n];
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int i = row + x;
                    bool wing = mask[i] != 0;
                    bool touch = false;
                    for (int dy = -1; dy <= 1 && !touch; dy++)
                    {
                        int ny = y + dy;
                        if ((uint)ny >= (uint)h) continue;
                        int nrow = ny * w;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx;
                            if ((uint)nx >= (uint)w) continue;
                            if ((mask[nrow + nx] != 0) != wing)
                            {
                                touch = true;
                                break;
                            }
                        }
                    }
                    if (!touch) continue;
                    if (wing) ring[i] = 2;
                    else if (bodyPx[i].a > 28) ring[i] = 1;
                }
            }
            for (int i = 0; i < n; i++)
            {
                if (ring[i] == 2) bodyPx[i] = farPx[i];
                else if (ring[i] == 1) farPx[i] = bodyPx[i];
            }
        }

        // Straight-alpha bleed. Transparent texels keep alpha 0 and borrow the
        // neighbor color so a filtered edge does not turn black.
        static void BleedColor(Color32[] px, int w, int h)
        {
            int n = w * h;
            var src = new Color32[n];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < n; i++) src[i] = px[i];
                for (int y = 0; y < h; y++)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int i = row + x;
                        if (src[i].a != 0) continue;
                        int br = 0, bg = 0, bb = 0, bn = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int ny = y + dy;
                            if ((uint)ny >= (uint)h) continue;
                            int nrow = ny * w;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = x + dx;
                                if ((uint)nx >= (uint)w) continue;
                                var c = src[nrow + nx];
                                if (c.a == 0) continue;
                                br += c.r;
                                bg += c.g;
                                bb += c.b;
                                bn++;
                            }
                        }
                        if (bn == 0) continue;
                        px[i] = new Color32((byte)(br / bn), (byte)(bg / bn), (byte)(bb / bn), 0);
                    }
                }
            }
        }

        static Sprite MakeSlice(string name, Color32[] px, int w, int h, Vector2 pivot, float ppu)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixels32(px);
            // Tight, not a full-rect quad. The quad's diagonal cracks the sky
            // between the raised wing and the head. Readable so the outline builds.
            tex.Apply(false, false);
            var spr = Sprite.Create(tex, new Rect(0f, 0f, w, h), pivot, ppu, 0, SpriteMeshType.Tight);
            spr.name = name;
            spr.hideFlags = HideFlags.HideAndDontSave;
            return spr;
        }
    }
}

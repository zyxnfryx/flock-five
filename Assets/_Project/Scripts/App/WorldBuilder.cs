using UnityEngine;

namespace FlockFive
{
    public static class WorldBuilder
    {
        public const float PortraitAspect = 9f / 16f;
        public const int Rows = 7;
        public const int Cols = 2;
        public const float LimbX = 2.38f;
        // Wood sprite 1280px @ 140 PPU. X scale is long enough that Cap (5) birds sit on the limb.
        // Bonus limbs use that same scale (keep GiftWoodScaleX equal to WoodScaleX). A shorter X
        // packed the five perches and drew the gift row short.
        public const float WoodScaleX = 0.64f;
        public const float GiftWoodScaleX = 0.64f;
        public const float WoodHalf = 1280f / 140f * WoodScaleX * 0.5f;

        // Exact perch spots (V24.3). Birds sit on bare-wood pads painted into branch.png /
        // branch_gift.png, and the wood slopes down toward the tip, so each spot has its own
        // height. X is in branch-sprite pixels (1280 wide, left-branch orientation, right
        // branches mirror). Seat Y is local, fitted so the reference toe row (sprite row 831)
        // touches the bark under both feet. Checked by Playtest/perch-contact/check_perch_contact.py:
        // re-run it after any branch art, bird foot or BirdScale change.
        public const float SeatToeRowPx = 831f;
        // Even steps on bare bark. The outer seat stays in from the tip
        // (1280px wood) so the last bird is not crammed into the one before it.
        public static readonly float[] SeatXPx = { 220f, 458f, 695f, 933f, 1148f };
        public static readonly float[] SeatYMain = { 0.3364f, 0.3757f, 0.2669f, 0.0776f, 0.0847f };
        public static readonly float[] SeatYGift = { 0.3382f, 0.3791f, 0.2659f, 0.0690f, 0.0847f };

        public static Vector2 SeatLocal(int s, bool gift, bool fromRight)
        {
            // Same pixel steps and the same wood scale, so a bonus limb spaces
            // its Cap perches like a regular one. Y stays fitted to each art.
            float x = (SeatXPx[s] - 640f) * WoodScaleX / 140f;
            return new Vector2(fromRight ? -x : x, (gift ? SeatYGift : SeatYMain)[s]);
        }

        public static float WoodHalfOf(float woodScaleX) => 1280f / 140f * woodScaleX * 0.5f;

        // Resting 9:16 playfield. Not the live orthographic size: a combo or
        // sparrow punch shrinks that for a few frames, and a refit during the
        // punch would park both columns closer to center after the camera settles.
        public static float PlayHalfWidth() => CamOrtho * PortraitAspect;

        // Sprite center for a unit-scale limb whose outer bark kisses the bezel.
        // Screen width and the constant wood length only — never the remaining
        // branch count, the vertical pitch, or a pack scale.
        public static float ColumnX() => PlayHalfWidth() - WoodHalf + 0.18f;

        public static float EdgeX(Camera cam, float packScale) => EdgeX(cam, packScale, WoodScaleX);

        public static float EdgeX(Camera cam, float packScale, float woodScaleX)
        {
            // packScale and cam are ignored. A short stack passed a larger
            // packScale, and a break refit read the punched orthographicSize.
            // Either one walked the root inward. Length stays constant; only
            // a different wood width (still not the row count) changes the inset.
            return PlayHalfWidth() - WoodHalfOf(woodScaleX) + 0.18f;
        }
        public const float CamRestY = -0.45f;
        public const float CamOrtho = 10.6f;
        // Tall phones: lift the rows so the flock sits mid-screen instead of low under a gap.
        public static float RowY0 => 3.42f + 5.0f * (PortraitLock.TallFactor() - 1f);
        // Ring of the feeder art above the pivot (1024px @ 180 PPU × FeederView.Scale).
        // The painted rope above the ring may enter the island; the bulb must not.
        public const float FeederCrown = 1.34f;
        public const float FeederSafeMargin = 0.48f;
        // Same drop from the glass on a phone with no inset. Notch / island
        // pushes the bulb below Screen.safeArea, re-read every call.
        public static float FeederY
        {
            get
            {
                float legacy = 8.12f + CamOrtho * (PortraitLock.TallFactor() - 1f);
                float safe = ScreenTop() - SafeTopWorld() - FeederSafeMargin - FeederCrown;
                return Mathf.Min(legacy, safe);
            }
        }

        // Highest a perched bird's crown may sit so it stays clear of the feeder tray.
        public static float FeederShelf() => FeederY - 1.08f - 0.40f;

        public static float ScreenTop() => CamRestY + CamOrtho * PortraitLock.TallFactor();

        public static float SafeTopWorld()
        {
            float h = Mathf.Max(1f, Screen.height);
            var safe = Screen.safeArea;
            // Before the OS reports insets, safeArea can be empty or the full glass.
            if (safe.width < 2f || safe.height < 2f) return 0f;
            float inset = Mathf.Max(0f, h - safe.yMax) / h;
            if (inset > 0.40f) return 0f;
            return inset * (CamOrtho * PortraitLock.TallFactor() * 2f);
        }
        public const float RowGap = 1.50f;
        public const int GiftCount = 2;
        public const int GiftIndex = Rows * Cols;
        public const float GiftY = -7.52f;

        public struct Garden
        {
            public Transform Root;
            public BranchView[] Branches;
            public FeederView[] Feeders;
            public HiveView Hive;
            public Camera Cam;
            public GardenIce Ice;
        }

        public static Garden Build(Transform parent)
        {
            var root = new GameObject("Garden").transform;
            root.SetParent(parent, false);

            var cam = MakeCamera(parent);
            float limbX = EdgeX(cam, 1f);

            var bg = Sprite("Bg", SpriteCatalog.GardenBgFor(LevelData.DisplayNumber), new Vector3(0f, -0.15f, 8f), 1f, -20, root);
            var fit = bg.AddComponent<BackgroundFitter>();
            fit.Cam = cam;
            fit.FollowCamera = false;
            fit.WorldCenter = new Vector3(0f, -0.15f, 8f);
            fit.WorldSize = new Vector2(13.6f, 24.0f);
            fit.Apply();
            // Tall phones: PortraitLock letterboxes the 9:16 play area; a bleed camera
            // paints this layer into the bands so the garden art reaches the bezel.
            bg.layer = PortraitLock.BleedLayer;
            SkyCycle.Attach(root, cam);
            GardenLife.Attach(root);
            GardenStorm.Attach(root);

            var branches = new BranchView[Rows * Cols + GiftCount];
            for (int row = 0; row < Rows; row++)
            {
                float y = RowY0 - row * RowGap;
                branches[row * 2] = MakeBranch(row * 2, new Vector2(-limbX, y), false, root);
                branches[row * 2 + 1] = MakeBranch(row * 2 + 1, new Vector2(limbX, y), true, root);
            }
            for (int g = 0; g < GiftCount; g++)
                branches[GiftIndex + g] = MakeGift(GiftIndex + g, g == 1, root);

            var feeders = new FeederView[2];
            feeders[0] = MakeFeeder(0, new Vector3(-1.22f, FeederY, 0f), root);
            feeders[1] = MakeFeeder(1, new Vector3(1.22f, FeederY, 0f), root);
            var hive = HiveView.Attach(root);
            var ice = GardenIce.Attach(root, cam);

            return new Garden
            {
                Root = root,
                Branches = branches,
                Feeders = feeders,
                Hive = hive,
                Cam = cam,
                Ice = ice
            };
        }

        public static BranchView MakePlain(int index, bool fromRight, Transform parent)
        {
            float x = EdgeX(null, 1f);
            return MakeBranch(index, new Vector2(fromRight ? x : -x, RowY0), fromRight, parent, WoodScaleX);
        }

        public static BranchView MakeSpare(int index, Vector2 pos, Transform parent)
        {
            var view = MakeGift(index, pos.x >= 0f, parent);
            view.transform.position = new Vector3(pos.x, pos.y, 0f);
            return view;
        }

        public static BranchView MakeGift(int index, bool fromRight, Transform parent)
        {
            float x = EdgeX(null, 1f, WoodScaleX);
            var view = MakeBranch(index, new Vector2(fromRight ? x : -x, GiftY), fromRight, parent, WoodScaleX, true);
            view.IsGift = true;
            if (view.Wood != null)
            {
                view.Wood.sprite = SpriteCatalog.BranchGift;
                view.Wood.transform.localScale = new Vector3(WoodScaleX, 0.52f, 1f);
                view.Wood.sortingOrder = 3;
                view.RetakeWood();
            }

            var glowGo = Sprite("GiftGlow", SpriteCatalog.Glow, view.transform.position, 1f, 1, view.transform);
            glowGo.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            glowGo.transform.localScale = new Vector3(0.72f, 0.55f, 1f);
            var glow = glowGo.GetComponent<SpriteRenderer>();
            glow.color = new Color(1f, 0.86f, 0.42f, 0.28f);

            // Plank sits on the inner bark. Negative X scale mirrors the arrow
            // onto a left-hand limb, bulbs included.
            float dir = fromRight ? 1f : -1f;
            var signGo = Sprite("GiftSign", SpriteCatalog.AdSign, view.transform.position, 1f, 11, view.transform);
            signGo.transform.localPosition = new Vector3(-1.08f * dir, 0.48f, 0f);
            signGo.transform.localScale = new Vector3(0.26f * dir, 0.26f, 1f);
            signGo.transform.localRotation = Quaternion.Euler(0f, 0f, -6f * dir);
            view.Sign = signGo.transform;
            var signCol = signGo.AddComponent<BoxCollider2D>();
            var signSr = signGo.GetComponent<SpriteRenderer>();
            if (signSr != null && signSr.sprite != null)
                signCol.size = signSr.sprite.bounds.size * 0.72f;
            else
                signCol.size = new Vector2(4.2f, 1.8f);
            signCol.offset = Vector2.zero;

            var bulbs = PinBulbs(signGo.transform);

            // Soft wash behind the plank so the Watch sign reads lit from the back.
            var backGo = Sprite("SignBacklight", SpriteCatalog.Glow, signGo.transform.position, 1f, 10, signGo.transform);
            backGo.transform.localPosition = new Vector3(-0.25f, 0.05f, 0f);
            backGo.transform.localScale = new Vector3(4.2f, 2.2f, 1f);
            var backlight = backGo.GetComponent<SpriteRenderer>();
            backlight.color = new Color(1f, 0.80f, 0.32f, 0.36f);

            var want = view.gameObject.AddComponent<GiftWant>();
            want.Glow = glow;
            want.Backlight = backlight;
            want.Sign = view.Sign;
            want.Bulbs = bulbs;
            return view;
        }

        // Mirrored marquee. hSegs is how many equal steps span the top and the bottom;
        // vSegs is the same for the two sides. Corners are the shared endpoints, so
        // they are drawn once. n = 2 * (hSegs + vSegs). minPitch > 0 drops a candidate
        // whose step would crowd the glass; 0 leaves sizing to the caller.
        public static void FitMarquee(float width, float height, int want, float minPitch, out int hSegs, out int vSegs)
        {
            width = Mathf.Max(1e-3f, width);
            height = Mathf.Max(1e-3f, height);
            want = Mathf.Clamp(want, 8, 16);
            int bestH = 1, bestV = 1;
            float best = float.MaxValue;
            for (int h = 1; h <= 7; h++)
            {
                for (int v = 1; v <= 7; v++)
                {
                    int n = 2 * (h + v);
                    if (n < 8 || n > 16) continue;
                    float hp = width / h;
                    float vp = height / v;
                    if (minPitch > 0f && (hp < minPitch || vp < minPitch)) continue;
                    float mismatch = Mathf.Abs(hp - vp) / Mathf.Max(hp, vp);
                    float density = Mathf.Abs(n - want) / (float)want;
                    float score = mismatch * 5f + density;
                    if (score < best)
                    {
                        best = score;
                        bestH = h;
                        bestV = v;
                    }
                }
            }
            hSegs = bestH;
            vSegs = bestV;
        }

        public static int MarqueeCount(int hSegs, int vSegs) => 2 * (hSegs + vSegs);

        // Clockwise from (x0, y0). y0 and y1 are one opposite pair (GUI: y0 is the top).
        // Top and bottom use the same x fractions; left and right use the same y fractions.
        public static void MarqueeSpot(float x0, float y0, float x1, float y1, int hSegs, int vSegs, int i, out Vector2 pos, out float ang)
        {
            int topN = hSegs + 1;
            int sideN = vSegs > 1 ? vSegs - 1 : 0;
            int botN = hSegs + 1;
            if (i < topN)
            {
                float t = i / (float)hSegs;
                pos = new Vector2(Mathf.Lerp(x0, x1, t), y0);
                ang = 180f;
                return;
            }
            i -= topN;
            if (i < sideN)
            {
                float t = (i + 1) / (float)vSegs;
                pos = new Vector2(x1, Mathf.Lerp(y0, y1, t));
                ang = -90f;
                return;
            }
            i -= sideN;
            if (i < botN)
            {
                float t = i / (float)hSegs;
                pos = new Vector2(Mathf.Lerp(x1, x0, t), y1);
                ang = 0f;
                return;
            }
            i -= botN;
            float s = (i + 1) / (float)vSegs;
            pos = new Vector2(x0, Mathf.Lerp(y1, y0, s));
            ang = 90f;
        }

        // Glass points out of the box. MarqueeSpot stores the screw-in angle:
        // 180 top, -90 right, 0 bottom, 90 left. GUI y grows downward.
        public static Vector2 MarqueeOutward(float angDeg)
        {
            float a = angDeg;
            if (a > 180f) a -= 360f;
            if (a < -180f) a += 360f;
            if (a >= 135f || a < -135f) return new Vector2(0f, -1f);
            if (a < -45f) return new Vector2(1f, 0f);
            if (a < 45f) return new Vector2(0f, 1f);
            return new Vector2(-1f, 0f);
        }

        // Rim point stays on the outer edge. The sprite center moves out by `half`
        // (half the glass) so the screw base touches that edge and the body stays outside.
        // yDown is GUI. World sprites flip Y and negate the spin.
        public static void SeatBulb(Vector2 rim, float angDeg, float half, bool yDown, out Vector2 center, out float spin)
        {
            var g = MarqueeOutward(angDeg);
            if (half < 0f) half = 0f;
            if (yDown)
            {
                center = rim + g * half;
                spin = angDeg;
                return;
            }
            center = new Vector2(rim.x + g.x * half, rim.y - g.y * half);
            spin = -angDeg;
        }

        // Circle: index 0 is the top, then clockwise in GUI space. Same seat rule as SeatBulb.
        public static void RadialSeat(Vector2 origin, float radius, int i, int n, float half, out Vector2 rim, out Vector2 center, out Vector2 outward, out float spin)
        {
            float turns = n > 0 ? i / (float)n : 0f;
            float rad = -Mathf.PI * 0.5f + turns * Mathf.PI * 2f;
            outward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            if (radius < 0f) radius = 0f;
            rim = origin + outward * radius;
            if (half < 0f) half = 0f;
            center = rim + outward * half;
            var inward = new Vector2(-outward.x, -outward.y);
            spin = Mathf.Atan2(inward.x, -inward.y) * Mathf.Rad2Deg;
        }

        // fx_ad_sign's board is the rectangle on the left of the texture. The arrow
        // head is the right third, so a full-bounds ring would hang in the notch.
        static Rect GiftSignRect(Sprite sprite)
        {
            const float u0 = 12f / 1126f;
            const float u1 = 768f / 1126f;
            const float vTop = 100f / 530f;
            const float vBot = 431f / 530f;
            if (sprite == null)
                return new Rect(-2.755f, -0.830f, 3.780f, 1.655f);
            var b = sprite.bounds;
            float x0 = Mathf.Lerp(b.min.x, b.max.x, u0);
            float x1 = Mathf.Lerp(b.min.x, b.max.x, u1);
            float yTop = Mathf.Lerp(b.max.y, b.min.y, vTop);
            float yBot = Mathf.Lerp(b.max.y, b.min.y, vBot);
            return new Rect(x0, yBot, x1 - x0, yTop - yBot);
        }

        static SpriteRenderer[] PinBulbs(Transform sign)
        {
            // Same ring as the GUI marquees, on the board rect. Chase still walks the index order.
            var signSr = sign.GetComponent<SpriteRenderer>();
            var shaft = GiftSignRect(signSr != null ? signSr.sprite : null);
            const float glass = 0.20f;
            var bulbSpr = SpriteCatalog.AdBulb;
            float minPitch = bulbSpr != null ? bulbSpr.bounds.size.x * glass / 0.62f : 0.9f;
            FitMarquee(shaft.width, shaft.height, 12, minPitch, out int hSegs, out int vSegs);
            int n = MarqueeCount(hSegs, vSegs);
            var bulbs = new SpriteRenderer[n];
            float half = bulbSpr != null ? bulbSpr.bounds.size.y * glass * 0.5f : glass * 1.6f;
            for (int i = 0; i < n; i++)
            {
                MarqueeSpot(shaft.xMin, shaft.yMax, shaft.xMax, shaft.yMin, hSegs, vSegs, i, out var p, out float ang);
                SeatBulb(p, ang, half, false, out var seat, out float spin);
                var go = Sprite("GiftBulb" + i, SpriteCatalog.AdBulb, sign.position, 1f, 13, sign);
                go.transform.localPosition = new Vector3(seat.x, seat.y, 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, spin);
                go.transform.localScale = new Vector3(glass, glass, 1f);
                var sr = go.GetComponent<SpriteRenderer>();
                bulbs[i] = sr;
                var halo = Sprite("Halo", SpriteCatalog.Glow, go.transform.position, 1f, 12, go.transform);
                halo.transform.localPosition = Vector3.zero;
                halo.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
                var hr = halo.GetComponent<SpriteRenderer>();
                hr.color = new Color(1f, 0.82f, 0.32f, 0.55f);
            }
            return bulbs;
        }

        static BranchView MakeBranch(int index, Vector2 pos, bool fromRight, Transform parent, float woodScaleX = WoodScaleX, bool gift = false)
        {
            var go = new GameObject("Branch" + index);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            var woodGo = Sprite("Wood", SpriteCatalog.Branch, go.transform.position, 1f, 2, go.transform);
            woodGo.transform.localPosition = Vector3.zero;
            woodGo.transform.localScale = new Vector3(woodScaleX, 0.50f, 1f);
            var wood = woodGo.GetComponent<SpriteRenderer>();
            wood.flipX = fromRight;

            var view = go.AddComponent<BranchView>();
            view.Index = index;
            view.FromRight = fromRight;
            view.Wood = wood;

            float half = WoodHalfOf(woodScaleX);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(half * 2.6f, 2.7f);
            col.offset = new Vector2(fromRight ? 0.08f : -0.08f, 0.62f);

            // Outer bird sits at the bark / screen edge without the sprite spilling.
            // Inner uses the rest of the limb so five pads have air; a 4-stack
            // still leaves the inner tip. Bees may fly off-screen.
            // Exact per-spot seats: see SeatXPx / SeatYMain / SeatYGift.
            // Kind, not scale: bonus wood is WoodScaleX too, so a scale compare would
            // put regular limbs on the gift heights.
            for (int s = 0; s < BranchState.Cap; s++)
            {
                var p = SeatLocal(s, gift, fromRight);
                var seat = new GameObject("Seat" + s).transform;
                seat.SetParent(go.transform, false);
                seat.localPosition = new Vector3(p.x, p.y, 0f);
                view.Seats[s] = seat;

                var bird = Sprite("Bird" + s, SpriteCatalog.Bird(BirdColor.Ruby), go.transform.position, 1f, FlockSort.Perch, go.transform);
                bird.transform.localPosition = new Vector3(p.x, p.y + BranchView.RestLift, 0f);
                bird.transform.localScale = BranchView.BirdScale;
                var idle = bird.AddComponent<BirdIdle>();
                idle.RestScale = BranchView.BirdScale;
                idle.RestLocal = bird.transform.localPosition;
                idle.FaceLeft = fromRight;
                view.Birds[s] = bird.GetComponent<SpriteRenderer>();
                view.Birds[s].flipX = fromRight;
                view.Birds[s].enabled = false;
            }
            view.RememberWood();
            return view;
        }

        static FeederView MakeFeeder(int slot, Vector3 pos, Transform parent)
        {
            var go = Sprite("Feeder" + slot, SpriteCatalog.Feeder(BirdColor.Ruby), pos, FeederView.Scale, 8, parent);
            var view = go.AddComponent<FeederView>();
            view.Slot = slot;
            view.Art = go.GetComponent<SpriteRenderer>();
            return view;
        }

        public static GameObject Sprite(string name, Sprite spr, Vector3 pos, float scale, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            FlockSort.Apply(sr, order);
            return go;
        }

        public static Camera MakeCamera(Transform parent)
        {
            foreach (var existing in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                existing.enabled = false;
                var old = existing.GetComponent<AudioListener>();
                if (old != null) old.enabled = false;
            }

            var go = new GameObject("Cam");
            go.transform.SetParent(parent, false);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CamOrtho;
            cam.aspect = PortraitAspect;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.22f, 0.18f, 0.10f);
            cam.nearClipPlane = -10f;
            cam.farClipPlane = 50f;
            cam.transform.position = new Vector3(0f, CamRestY, -10f);
            cam.transform.rotation = Quaternion.identity;
            go.AddComponent<AudioListener>();
            go.AddComponent<MasterLoudness>();
            go.AddComponent<PortraitLock>();
            go.AddComponent<CamShake>();
            go.tag = "MainCamera";
            return cam;
        }
    }

    public sealed class PortraitLock : MonoBehaviour
    {
        public const int BleedLayer = 30;
        Camera _cam;
        Camera _bleed;
        bool _orientOnce;

        void Awake() => _cam = GetComponent<Camera>();

        void OnEnable() => Apply();

        void LateUpdate() => Apply();

        void Apply()
        {
            // Setting orientation every frame stalls iOS. Lock the flags once,
            // and only write orientation when the device has left portrait.
            if (!_orientOnce)
            {
                _orientOnce = true;
                Screen.autorotateToPortrait = true;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
                Screen.orientation = ScreenOrientation.Portrait;
            }
            else if (Screen.orientation != ScreenOrientation.Portrait)
                Screen.orientation = ScreenOrientation.Portrait;

            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam == null) return;

            const float want = WorldBuilder.PortraitAspect;
            _cam.aspect = want;
            float window = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (Mathf.Abs(window - want) < 0.03f)
            {
                _cam.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }
            if (window > want)
            {
                float w = want / window;
                _cam.rect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else
            {
                float h = window / want;
                _cam.rect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
                Bleed(want / window);
                return;
            }
            if (_bleed != null) _bleed.enabled = false;
        }

        // Full-screen camera behind the play camera that draws only the background
        // layer, same world scale, so the top/bottom bands show garden art, not clear color.
        void Bleed(float tall)
        {
            if (_bleed == null)
            {
                var go = new GameObject("BleedCam");
                go.transform.SetParent(transform, false);
                _bleed = go.AddComponent<Camera>();
                _bleed.orthographic = true;
                _bleed.clearFlags = CameraClearFlags.SolidColor;
                // Everything but UI: any world effect (rain, ice, veils, birds flying off)
                // reaches the bezel automatically. The play camera redraws its 9:16 rect on top.
                _bleed.cullingMask = ~(1 << 5);
                _bleed.nearClipPlane = _cam.nearClipPlane;
                _bleed.farClipPlane = _cam.farClipPlane;
                _bleed.rect = new Rect(0f, 0f, 1f, 1f);
            }
            _bleed.enabled = _cam.enabled;
            _bleed.backgroundColor = _cam.backgroundColor;
            _bleed.depth = _cam.depth - 1f;
            _bleed.orthographicSize = _cam.orthographicSize * tall;
            _bleed.transform.localPosition = Vector3.zero;
            _bleed.transform.localRotation = Quaternion.identity;
        }

        public static float TallFactor()
        {
            float window = (float)Screen.width / Mathf.Max(1, Screen.height);
            return Mathf.Max(1f, WorldBuilder.PortraitAspect / Mathf.Max(0.05f, window));
        }
    }
}

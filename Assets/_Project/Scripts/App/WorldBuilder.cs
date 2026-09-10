using UnityEngine;

namespace FlockFive
{
    public static class WorldBuilder
    {
        public const float PortraitAspect = 9f / 16f;
        public const int Rows = 7;
        public const int Cols = 2;
        public const float LimbX = 2.38f;
        public const float RowY0 = 3.42f;
        public const float RowGap = 1.42f;
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

            var bg = Sprite("Bg", SpriteCatalog.GardenBg, new Vector3(0f, -0.15f, 8f), 1f, -20, root);
            var fit = bg.AddComponent<BackgroundFitter>();
            fit.Cam = cam;
            fit.FollowCamera = false;
            fit.WorldCenter = new Vector3(0f, -0.15f, 8f);
            fit.WorldSize = new Vector2(10.2f, 22.4f);
            fit.Apply();
            SkyCycle.Attach(root, cam);
            GardenLife.Attach(root);
            GardenStorm.Attach(root);

            var branches = new BranchView[Rows * Cols + 1];
            for (int row = 0; row < Rows; row++)
            {
                float y = RowY0 - row * RowGap;
                branches[row * 2] = MakeBranch(row * 2, new Vector2(-LimbX, y), false, root);
                branches[row * 2 + 1] = MakeBranch(row * 2 + 1, new Vector2(LimbX, y), true, root);
            }
            branches[GiftIndex] = MakeGift(root);

            var feeders = new FeederView[2];
            feeders[0] = MakeFeeder(0, new Vector3(-1.22f, 8.12f, 0f), root);
            feeders[1] = MakeFeeder(1, new Vector3(1.22f, 8.12f, 0f), root);
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

        public static BranchView MakeSpare(int index, Vector2 pos, Transform parent)
        {
            var view = MakeBranch(index, pos, false, parent);
            view.IsGift = true;
            if (view.Wood != null)
            {
                view.Wood.sprite = SpriteCatalog.BranchGift;
                view.Wood.transform.localScale = new Vector3(0.46f, 0.52f, 1f);
                view.Wood.sortingOrder = 3;
            }
            return view;
        }

        static BranchView MakeGift(Transform parent)
        {
            var view = MakeBranch(GiftIndex, new Vector2(0f, GiftY), false, parent);
            view.IsGift = true;
            if (view.Wood != null)
            {
                view.Wood.sprite = SpriteCatalog.BranchGift;
                view.Wood.transform.localScale = new Vector3(0.46f, 0.52f, 1f);
                view.Wood.sortingOrder = 3;
            }

            var glowGo = Sprite("GiftGlow", SpriteCatalog.Glow, view.transform.position, 1f, 1, view.transform);
            glowGo.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            glowGo.transform.localScale = new Vector3(0.72f, 0.55f, 1f);
            var glow = glowGo.GetComponent<SpriteRenderer>();
            glow.color = new Color(1f, 0.86f, 0.42f, 0.28f);

            // Sit farther off the spare perch, arrow still pointing at the limb.
            var signGo = Sprite("GiftSign", SpriteCatalog.AdSign, view.transform.position, 1f, 11, view.transform);
            signGo.transform.localPosition = new Vector3(-3.95f, 0.92f, 0f);
            signGo.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
            view.Sign = signGo.transform;
            var signCol = signGo.AddComponent<BoxCollider2D>();
            signCol.size = new Vector2(5.6f, 2.5f);
            signCol.offset = new Vector2(-0.35f, 0f);

            var bulbs = PinBulbs(signGo.transform);

            var want = view.gameObject.AddComponent<GiftWant>();
            want.Glow = glow;
            want.Sign = view.Sign;
            want.Bulbs = bulbs;
            return view;
        }

        static SpriteRenderer[] PinBulbs(Transform sign)
        {
            // Chase around the plank and arrowhead only — no trail onto the spare.
            var spots = new Vector2[]
            {
                new Vector2(-2.35f, 1.02f),
                new Vector2(-0.55f, 1.08f),
                new Vector2(1.05f, 1.00f),
                new Vector2(2.05f, 0.72f),
                new Vector2(2.58f, 0.08f),
                new Vector2(2.05f, -0.72f),
                new Vector2(1.05f, -1.00f),
                new Vector2(-0.55f, -1.08f),
                new Vector2(-2.35f, -1.02f)
            };
            var bulbs = new SpriteRenderer[spots.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                var go = Sprite("GiftBulb" + i, SpriteCatalog.AdBulb, sign.position, 1f, 13, sign);
                go.transform.localPosition = new Vector3(spots[i].x, spots[i].y, 0f);
                go.transform.localScale = new Vector3(0.20f, 0.20f, 1f);
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

        static BranchView MakeBranch(int index, Vector2 pos, bool fromRight, Transform parent)
        {
            var go = new GameObject("Branch" + index);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            var woodGo = Sprite("Wood", SpriteCatalog.Branch, go.transform.position, 1f, 2, go.transform);
            woodGo.transform.localPosition = Vector3.zero;
            woodGo.transform.localScale = new Vector3(0.42f, 0.50f, 1f);
            var wood = woodGo.GetComponent<SpriteRenderer>();
            wood.flipX = fromRight;

            var view = go.AddComponent<BranchView>();
            view.Index = index;
            view.FromRight = fromRight;
            view.Wood = wood;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(5.1f, 2.7f);
            col.offset = new Vector2(fromRight ? 0.08f : -0.08f, 0.62f);

            float outer = fromRight ? 1.95f : -1.95f;
            float inner = fromRight ? -1.72f : 1.72f;
            for (int s = 0; s < BranchState.Cap; s++)
            {
                float u = BranchState.Cap <= 1 ? 0.5f : s / (float)(BranchState.Cap - 1);
                float x = Mathf.Lerp(outer, inner, u);
                var seat = new GameObject("Seat" + s).transform;
                seat.SetParent(go.transform, false);
                seat.localPosition = new Vector3(x, 0.38f, 0f);
                view.Seats[s] = seat;

                var bird = Sprite("Bird" + s, SpriteCatalog.Bird(BirdColor.Ruby), go.transform.position, 1f, 6, go.transform);
                bird.transform.localPosition = new Vector3(x, 0.38f + BranchView.RestLift, 0f);
                bird.transform.localScale = BranchView.BirdScale;
                var idle = bird.AddComponent<BirdIdle>();
                idle.RestScale = BranchView.BirdScale;
                idle.RestLocal = bird.transform.localPosition;
                idle.FaceLeft = fromRight;
                view.Birds[s] = bird.GetComponent<SpriteRenderer>();
                view.Birds[s].flipX = fromRight;
                view.Birds[s].enabled = false;
            }
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
            sr.sortingOrder = order;
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
            cam.orthographicSize = 10.6f;
            cam.aspect = PortraitAspect;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.12f, 0.08f);
            cam.nearClipPlane = -10f;
            cam.farClipPlane = 50f;
            cam.transform.position = new Vector3(0f, -0.45f, -10f);
            cam.transform.rotation = Quaternion.identity;
            go.AddComponent<AudioListener>();
            go.AddComponent<PortraitLock>();
            go.AddComponent<CamShake>();
            go.tag = "MainCamera";
            return cam;
        }
    }

    public sealed class PortraitLock : MonoBehaviour
    {
        Camera _cam;

        void Awake() => _cam = GetComponent<Camera>();

        void OnEnable() => Apply();

        void LateUpdate() => Apply();

        void Apply()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;

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
            }
        }
    }
}

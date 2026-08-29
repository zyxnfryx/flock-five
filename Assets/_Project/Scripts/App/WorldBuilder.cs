using UnityEngine;

namespace FlockFive
{
    public static class WorldBuilder
    {
        public const float PortraitAspect = 9f / 16f;
        const int Rows = 4;
        const int Cols = 2;

        public struct Garden
        {
            public Transform Root;
            public BranchView[] Branches;
            public FeederView[] Feeders;
            public Camera Cam;
        }

        public static Garden Build(Transform parent)
        {
            var root = new GameObject("Garden").transform;
            root.SetParent(parent, false);

            var cam = MakeCamera(parent);

            var bg = Sprite("Bg", SpriteCatalog.GardenBg, new Vector3(0f, 0.25f, 8f), 1f, -20, root);
            var fit = bg.AddComponent<BackgroundFitter>();
            fit.Cam = cam;
            fit.FollowCamera = false;
            fit.WorldCenter = new Vector3(0f, 0.35f, 8f);
            fit.WorldSize = new Vector2(10.2f, 18.2f);
            fit.Apply();
            SkyCycle.Attach(root, cam);
            GardenLife.Attach(root);

            // One limb from the off-screen left tree and one from the off-screen
            // right tree at each row. Index = row * 2 + (right ? 1 : 0).
            var branches = new BranchView[Rows * Cols];
            float[] ys = { 2.95f, 0.45f, -2.05f, -4.55f };
            for (int row = 0; row < Rows; row++)
            {
                branches[row * 2] = MakeBranch(row * 2, new Vector2(-2.38f, ys[row]), false, root);
                branches[row * 2 + 1] = MakeBranch(row * 2 + 1, new Vector2(2.38f, ys[row]), true, root);
            }

            var feeders = new FeederView[2];
            feeders[0] = MakeFeeder(0, new Vector3(-1.22f, 6.88f, 0f), root);
            feeders[1] = MakeFeeder(1, new Vector3(1.22f, 6.88f, 0f), root);

            return new Garden
            {
                Root = root,
                Branches = branches,
                Feeders = feeders,
                Cam = cam
            };
        }

        static BranchView MakeBranch(int index, Vector2 pos, bool fromRight, Transform parent)
        {
            var go = new GameObject("Branch" + index);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            var woodGo = Sprite("Wood", SpriteCatalog.Branch, go.transform.position, 1f, 2, go.transform);
            woodGo.transform.localPosition = Vector3.zero;
            woodGo.transform.localScale = new Vector3(0.48f, 0.58f, 1f);
            var wood = woodGo.GetComponent<SpriteRenderer>();
            wood.flipX = fromRight;

            var view = go.AddComponent<BranchView>();
            view.Index = index;
            view.FromRight = fromRight;
            view.Wood = wood;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4.4f, 2.3f);
            col.offset = new Vector2(fromRight ? 0.15f : -0.15f, 0.55f);

            // Seat 0 is the trunk (off-screen tree); last seat is the tip at the aisle.
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
            cam.orthographicSize = 8.2f;
            cam.aspect = PortraitAspect;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.12f, 0.08f);
            cam.nearClipPlane = -10f;
            cam.farClipPlane = 50f;
            cam.transform.position = new Vector3(0f, 0.35f, -10f);
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

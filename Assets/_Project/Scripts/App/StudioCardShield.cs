using UnityEngine;
using UnityEngine.UI;

namespace FlockFive
{
    // Full-screen raycast plate above the home for the whole studio card
    // (StudioMinSec + the end hold + the fade). Invisible. Graphic raycasts hit
    // this and stop. Turn it off when the card is gone so the rest of the game is clear.
    [DefaultExecutionOrder(-500)]
    public sealed class StudioCardShield : MonoBehaviour
    {
        public const int SortOrder = 32000;

        Canvas _canvas;
        GraphicRaycaster _caster;
        Image _plate;
        CanvasGroup _group;
        static Sprite _pixel;

        public Image Plate => _plate;
        public Canvas Canvas => _canvas;

        public bool BlocksRaycasts =>
            isActiveAndEnabled
            && _plate != null && _plate.enabled && _plate.raycastTarget
            && _group != null && _group.enabled && _group.blocksRaycasts
            && _canvas != null && _canvas.enabled && _canvas.sortingOrder >= SortOrder
            && _caster != null && _caster.enabled;

        public bool CoversScreen
        {
            get
            {
                if (_plate == null) return false;
                var rt = _plate.rectTransform;
                return rt.anchorMin == Vector2.zero
                    && rt.anchorMax == Vector2.one
                    && rt.offsetMin == Vector2.zero
                    && rt.offsetMax == Vector2.zero;
            }
        }

        public static StudioCardShield Ensure(Transform host)
        {
            var found = host.GetComponentInChildren<StudioCardShield>(true);
            if (found != null) return found;
            var go = new GameObject("StudioCardShield");
            go.transform.SetParent(host, false);
            return go.AddComponent<StudioCardShield>();
        }

        void Awake() => Build();

        public void SetBlocking(bool up)
        {
            if (_plate == null) Build();
            if (!up)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            _plate.raycastTarget = true;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _canvas.enabled = true;
            _caster.enabled = true;
            KeepRaycastable();
        }

        // A clear plate is still a hit. The canvas will cull a 0-alpha graphic; put the hit back
        // before the event system asks, or taps fall through onto the home.
        void Update() => KeepRaycastable();

        void KeepRaycastable()
        {
            if (_plate == null || _plate.canvasRenderer == null) return;
            _plate.canvasRenderer.cull = false;
        }

        // Tests place the plate on a camera so a raycast has a stable screen.
        // The player uses an overlay, which sits above every other canvas.
        public void AdoptCamera(Camera cam)
        {
            if (_canvas == null) Build();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = cam;
            _canvas.planeDistance = 5f;
        }

        void Build()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = SortOrder;

            _caster = GetComponent<GraphicRaycaster>();
            if (_caster == null) _caster = gameObject.AddComponent<GraphicRaycaster>();
            _caster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;

            var plate = transform.Find("StudioCardPlate");
            if (plate == null)
            {
                var child = new GameObject("StudioCardPlate");
                child.transform.SetParent(transform, false);
                plate = child.transform;
            }
            _plate = plate.GetComponent<Image>();
            if (_plate == null) _plate = plate.gameObject.AddComponent<Image>();
            _plate.sprite = Pixel();
            _plate.color = new Color(0f, 0f, 0f, 0f);
            _plate.raycastTarget = true;
            var rt = _plate.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static Sprite Pixel()
        {
            if (_pixel != null) return _pixel;
            var tex = Texture2D.whiteTexture;
            if (tex == null) return null;
            _pixel = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), Mathf.Max(1f, tex.width));
            _pixel.hideFlags = HideFlags.HideAndDontSave;
            return _pixel;
        }
    }
}

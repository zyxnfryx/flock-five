using UnityEngine;

namespace FlockFive
{
    public sealed class BackgroundFitter : MonoBehaviour
    {
        public Camera Cam;
        public bool FollowCamera;
        public Vector2 WorldSize = new Vector2(24f, 13.5f);
        public Vector3 WorldCenter = new Vector3(0f, 0.4f, 8f);
        SpriteRenderer _sr;

        void Awake() => _sr = GetComponent<SpriteRenderer>();
        void LateUpdate() => Apply();

        public void Apply()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null || _sr.sprite == null) return;
            var size = _sr.sprite.bounds.size;
            if (size.x < 0.01f || size.y < 0.01f) return;

            if (FollowCamera && Cam != null)
            {
                float h = Cam.orthographicSize * 2.16f;
                float w = h * Cam.aspect;
                transform.position = new Vector3(Cam.transform.position.x, Cam.transform.position.y, 8f);
                transform.localScale = new Vector3(w / size.x, h / size.y, 1f);
                return;
            }

            transform.position = WorldCenter;
            transform.localScale = new Vector3(WorldSize.x / size.x, WorldSize.y / size.y, 1f);
            float dusk = SkyCycle.Dusk;
            var wash = new Color(0.78f, 0.82f, 0.80f, 1f);
            var sunset = Color.Lerp(wash, new Color(0.86f, 0.84f, 0.78f, 1f), 0.35f);
            var night = new Color(0.52f, 0.54f, 0.68f, 1f);
            _sr.color = Color.Lerp(sunset, night, dusk);
        }
    }
}

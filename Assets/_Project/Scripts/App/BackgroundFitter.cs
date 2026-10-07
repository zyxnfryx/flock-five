using UnityEngine;

namespace FlockFive
{
    public sealed class BackgroundFitter : MonoBehaviour
    {
        public Camera Cam;
        public bool FollowCamera;
        public Vector2 WorldSize = new Vector2(24f, 13.5f);
        public Vector3 WorldCenter = new Vector3(0f, 0.4f, 8f);
        // SeasonCrossfade multiplies this. 1 is the normal painting.
        public float Alpha = 1f;
        // Home wash keeps the raw painting. The dark overlay stays in DrawHomeWash.
        public bool Untinted;
        SpriteRenderer _sr;
        Color _tint;
        bool _tintSet;

        void Awake() => _sr = GetComponent<SpriteRenderer>();
        void LateUpdate() => Apply();

        public void Apply()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null || _sr.sprite == null) return;
            var size = _sr.sprite.bounds.size;
            if (size.x < 0.01f || size.y < 0.01f) return;

            Vector3 pos;
            Vector3 scale;
            if (Cam != null)
            {
                // Bleed past the frustum so the garden painting, not the clear color, hits the bezel.
                float h = Cam.orthographicSize * 2.24f * PortraitLock.TallFactor();
                float w = h * Mathf.Max(0.05f, Cam.aspect);
                float x = FollowCamera ? Cam.transform.position.x : WorldCenter.x;
                float y = FollowCamera ? Cam.transform.position.y : WorldCenter.y;
                pos = new Vector3(x, y, WorldCenter.z);
                scale = new Vector3(w / size.x, h / size.y, 1f);
            }
            else
            {
                pos = WorldCenter;
                scale = new Vector3(WorldSize.x / size.x, WorldSize.y / size.y, 1f);
            }
            if (transform.position != pos) transform.position = pos;
            if (transform.localScale != scale) transform.localScale = scale;
            Color tint;
            float a = Mathf.Clamp01(Alpha);
            if (Untinted)
                tint = new Color(1f, 1f, 1f, a);
            else
            {
                float dusk = SkyCycle.Dusk;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 0.28f);
                var sunset = Color.Lerp(new Color(0.96f, 0.93f, 0.86f, 1f), new Color(1f, 0.86f, 0.68f, 1f), pulse * 0.28f);
                var night = new Color(0.64f, 0.58f, 0.82f, 1f);
                var sky = Color.Lerp(sunset, night, dusk);
                tint = Color.Lerp(sky, new Color(0.52f, 0.58f, 0.64f, 1f), GardenStorm.Wet * 0.32f);
                float bolt = GardenStorm.SkyFlash;
                if (bolt > 0.004f)
                    tint = Color.Lerp(tint, new Color(0.88f, 0.92f, 0.98f, 1f), bolt * 0.7f);
                tint.a = a;
            }
            if (!_tintSet || !SameByte(_tint, tint))
            {
                _tintSet = true;
                _tint = tint;
                _sr.color = tint;
            }
        }

        static bool SameByte(Color a, Color b)
        {
            return Byte(a.r) == Byte(b.r) && Byte(a.g) == Byte(b.g)
                && Byte(a.b) == Byte(b.b) && Byte(a.a) == Byte(b.a);
        }

        static int Byte(float u)
        {
            int v = (int)(Mathf.Clamp01(u) * 255f + 0.5f);
            return v > 255 ? 255 : v;
        }
    }
}


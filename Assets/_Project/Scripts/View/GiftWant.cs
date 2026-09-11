using UnityEngine;

namespace FlockFive
{
    // The spare perch should feel like a gift, not a HUD button.
    public sealed class GiftWant : MonoBehaviour
    {
        public SpriteRenderer Glow;
        public SpriteRenderer Backlight;
        public Transform Sign;
        public SpriteRenderer[] Bulbs;
        public bool On = true;
        Vector3 _signRest;
        Vector3[] _bulbRest;
        bool _rested;

        void LateUpdate()
        {
            if (Sign != null && !_rested)
            {
                _signRest = Sign.localPosition;
                if (Bulbs != null)
                {
                    _bulbRest = new Vector3[Bulbs.Length];
                    for (int i = 0; i < Bulbs.Length; i++)
                        _bulbRest[i] = Bulbs[i] != null ? Bulbs[i].transform.localScale : Vector3.one * 0.2f;
                }
                _rested = true;
            }
            if (!On)
            {
                if (Glow != null) Glow.enabled = false;
                if (Backlight != null) Backlight.enabled = false;
                if (Sign != null) Sign.gameObject.SetActive(false);
                enabled = false;
                return;
            }
            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.05f);
            if (Glow != null)
            {
                Glow.enabled = true;
                Glow.color = new Color(1f, 0.88f, 0.45f, 0.30f + 0.20f * breathe);
                float s = 2.85f + 0.28f * breathe;
                Glow.transform.localScale = new Vector3(s, s * 0.52f, 1f);
            }
            if (Backlight != null)
            {
                Backlight.enabled = true;
                // Warm back-fill — brighter toward the center, breathing with the marquee.
                Backlight.color = new Color(1f, 0.82f, 0.34f, 0.40f + 0.26f * breathe);
                float sx = 5.7f + 0.45f * breathe;
                float sy = 3.05f + 0.28f * breathe;
                Backlight.transform.localScale = new Vector3(sx, sy, 1f);
            }
            if (Sign != null)
            {
                Sign.gameObject.SetActive(true);
                Sign.localPosition = _signRest + new Vector3(0f, Mathf.Sin(t * 1.35f) * 0.028f, 0f);
                Sign.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.12f) * 3.4f);
            }
            Blink(t);
        }

        void Blink(float t)
        {
            if (Bulbs == null || Bulbs.Length == 0) return;
            int n = Bulbs.Length;
            // Marquee chase toward the perch, with a periodic all-flash.
            int phase = Mathf.FloorToInt(t * 8.2f);
            bool strobe = (Mathf.FloorToInt(t * 1.85f) % 6) == 0;
            bool strobeOn = ((int)(t * 16f) & 1) == 0;
            for (int i = 0; i < n; i++)
            {
                var sr = Bulbs[i];
                if (sr == null) continue;
                int k = phase - i;
                k %= 4;
                if (k < 0) k += 4;
                bool on = strobe ? strobeOn : (k <= 1);
                sr.color = on ? Color.white : new Color(0.38f, 0.22f, 0.08f, 0.62f);
                if (_bulbRest != null && i < _bulbRest.Length)
                    sr.transform.localScale = _bulbRest[i] * (on ? 1.12f : 0.78f);
                if (sr.transform.childCount > 0)
                {
                    var halo = sr.transform.GetChild(0).GetComponent<SpriteRenderer>();
                    if (halo != null)
                    {
                        halo.enabled = true;
                        halo.color = on
                            ? new Color(1f, 0.88f, 0.40f, 0.82f)
                            : new Color(1f, 0.72f, 0.24f, 0.22f);
                        float hs = on ? 2.8f : 2.2f;
                        halo.transform.localScale = new Vector3(hs, hs, 1f);
                    }
                }
            }
        }
    }
}

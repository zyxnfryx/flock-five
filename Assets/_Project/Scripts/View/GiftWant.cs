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
        // Shared stage-clear state (FlockFiveApp sets it from _won). While it is up, every live
        // bonus sign fades out over SignFadeDur and stays hidden; the limb, its glow and its
        // birds are untouched. A fresh stage (or a restart) brings the sign straight back.
        public static bool StageClear;
        public const float SignFadeDur = 0.4f;
        float _signFade = 1f;
        SpriteRenderer _signSr;
        Vector3 _signRest;
        Quaternion _signRestRot = Quaternion.identity;
        Vector3[] _bulbRest;
        SpriteRenderer[] _halo;
        bool _rested;

        void LateUpdate()
        {
            if (Sign != null && !_rested)
            {
                _signRest = Sign.localPosition;
                _signRestRot = Sign.localRotation;
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
            if (GamePause.Paused) return;
            float fade = SignFade();
            float t = Time.unscaledTime;
            float breathe = 0.5f + 0.5f * Mathf.Sin(t * 2.05f);
            if (Glow != null)
            {
                Glow.enabled = true;
                Glow.color = new Color(1f, 0.88f, 0.45f, 0.30f + 0.20f * breathe);
                float s = 2.85f + 0.28f * breathe;
                Glow.transform.localScale = new Vector3(s, s * 0.52f, 1f);
            }
            if (fade <= 0f)
            {
                // Faded out for the stage clear: the sign (with its backlight and bulbs) is gone.
                if (Sign != null && Sign.gameObject.activeSelf) Sign.gameObject.SetActive(false);
                return;
            }
            if (Backlight != null)
            {
                Backlight.enabled = true;
                // Warm back-fill — brighter toward the center, breathing with the marquee.
                Backlight.color = new Color(1f, 0.82f, 0.34f, (0.40f + 0.26f * breathe) * fade);
                float sx = 4.1f + 0.28f * breathe;
                float sy = 2.15f + 0.18f * breathe;
                Backlight.transform.localScale = new Vector3(sx, sy, 1f);
            }
            if (Sign != null)
            {
                Sign.gameObject.SetActive(true);
                Sign.localPosition = _signRest + new Vector3(0f, Mathf.Sin(t * 1.35f) * 0.028f, 0f);
                Sign.localRotation = _signRestRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.12f) * 3.4f);
                if (_signSr == null) _signSr = Sign.GetComponent<SpriteRenderer>();
                if (_signSr != null)
                {
                    var c = _signSr.color;
                    c.a = fade;
                    _signSr.color = c;
                }
            }
            Blink(t, fade);
        }

        // Sign alpha from the shared stage-clear state: eases out over SignFadeDur once the
        // stage is complete, snaps back to 1 when it is not.
        float SignFade()
        {
            if (!StageClear)
            {
                _signFade = 1f;
                return 1f;
            }
            _signFade = Mathf.Max(0f, _signFade - Time.unscaledDeltaTime / SignFadeDur);
            return Mathf.SmoothStep(0f, 1f, _signFade);
        }

        void Blink(float t, float fade)
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
                sr.color = on ? new Color(1f, 1f, 1f, fade) : new Color(0.38f, 0.22f, 0.08f, 0.62f * fade);
                if (_bulbRest != null && i < _bulbRest.Length)
                    sr.transform.localScale = _bulbRest[i] * (on ? 1.12f : 0.78f);
                if (_halo == null || _halo.Length != n) _halo = new SpriteRenderer[n];
                var halo = _halo[i];
                if (halo == null && sr.transform.childCount > 0)
                {
                    halo = sr.transform.GetChild(0).GetComponent<SpriteRenderer>();
                    _halo[i] = halo;
                }
                if (halo != null)
                {
                    halo.enabled = true;
                    halo.color = on
                        ? new Color(1f, 0.88f, 0.40f, 0.82f * fade)
                        : new Color(1f, 0.72f, 0.24f, 0.22f * fade);
                    float hs = on ? 2.8f : 2.2f;
                    halo.transform.localScale = new Vector3(hs, hs, 1f);
                }
            }
        }
    }
}


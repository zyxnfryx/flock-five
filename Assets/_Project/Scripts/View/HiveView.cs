using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed class HiveView : MonoBehaviour
    {
        public Transform Home;
        public static float GuiPulse;
        // ~27% slower than the old visitor / resident orbits.
        const float Pace = 0.73f;

        SpriteRenderer[] _residents;
        SpriteRenderer[] _glints;
        float[] _glintPh;
        float[] _phase;
        float[] _speedMul;
        float[] _liss;
        float[] _radiusK;
        float[] _wobAmp;
        float[] _wobHz;
        bool _pulse;
        float _pulseT;
        float _baseScale = 0.72f;

        public static HiveView Attach(Transform parent)
        {
            var go = new GameObject("Hive");
            go.transform.SetParent(parent, false);
            // Park under the bottom-right stage hive HUD (FlockFiveApp tracks exact rect).
            go.transform.position = new Vector3(2.75f, -8.35f, 0f);
            go.transform.localScale = Vector3.one * 0.72f;
            var view = go.AddComponent<HiveView>();
            view._baseScale = 0.72f;
            view.Build();
            return view;
        }

        // Snap the mouth under the on-screen hive button so visitors never head for the feeders.
        public void TrackHud(Camera cam, Rect hiveGui)
        {
            if (cam == null) return;
            float sx = hiveGui.center.x;
            float sy = Screen.height - hiveGui.center.y;
            // Ortho cams need distance-from-camera as z, not world z=0 (that parks on the lens).
            float depth = Mathf.Abs(cam.transform.position.z);
            if (depth < 0.01f) depth = 10f;
            var w = cam.ScreenToWorldPoint(new Vector3(sx, sy, depth));
            w.z = 0f;
            transform.position = w;
        }

        public Vector3 Mouth => transform.position + Vector3.up * 0.12f;

        void Build()
        {
            Home = transform;
            // Soft glow only — the wood hive art is the OnGUI button; this is the fly-to target.
            // Round discs on the hive center. A stretched scale read as a rectangle
            // that drifted off the skep toward the middle of the garden.
            var comb = WorldBuilder.Sprite("Comb", SpriteCatalog.Glow, transform.position, 1f, 9, transform);
            comb.transform.localPosition = Vector3.zero;
            comb.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            comb.GetComponent<SpriteRenderer>().color = new Color(0.92f, 0.62f, 0.18f, 0.22f);

            var core = WorldBuilder.Sprite("CombCore", SpriteCatalog.Glow, transform.position, 1f, 10, transform);
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = new Vector3(0.58f, 0.58f, 1f);
            core.GetComponent<SpriteRenderer>().color = new Color(1f, 0.82f, 0.32f, 0.30f);

            _glints = new SpriteRenderer[4];
            _glintPh = new float[4];
            var spark = SpriteCatalog.Sparkle;
            for (int g = 0; g < _glints.Length; g++)
            {
                var go = WorldBuilder.Sprite("CombGlint" + g, spark != null ? spark : SpriteCatalog.Glow, transform.position, 0.22f, 12, transform);
                float ang = g * (Mathf.PI * 0.5f) + 0.4f;
                go.transform.localPosition = new Vector3(Mathf.Cos(ang) * 0.34f, Mathf.Sin(ang) * 0.28f, 0f);
                go.transform.localScale = Vector3.one * (0.16f + (g & 1) * 0.05f);
                _glints[g] = go.GetComponent<SpriteRenderer>();
                _glints[g].enabled = false;
                _glintPh[g] = g * 1.37f;
            }

            _residents = new SpriteRenderer[6];
            _phase = new float[6];
            _speedMul = new float[6];
            _liss = new float[6];
            _radiusK = new float[6];
            _wobAmp = new float[6];
            _wobHz = new float[6];
            for (int i = 0; i < 6; i++)
            {
                var go = WorldBuilder.Sprite("HiveBee" + i, SpriteCatalog.Bee, transform.position, 0.16f, 11, transform);
                _residents[i] = go.GetComponent<SpriteRenderer>();
                _residents[i].enabled = false;
                _phase[i] = Random.Range(0f, Mathf.PI * 2f);
                _speedMul[i] = Random.Range(0.8f, 1.2f);
                _liss[i] = Random.Range(1.3f, 1.7f);
                _radiusK[i] = Random.Range(0.85f, 1.15f);
                _wobAmp[i] = Random.Range(0.08f, 0.18f);
                _wobHz[i] = Random.Range(1.2f, 2.4f);
            }
            RefreshResidents();
        }

        // Restart drops an in-flight visitor. Album copies already saved stay saved.
        public void CancelVisitors()
        {
            StopAllCoroutines();
            _pulse = false;
            _pulseT = 0f;
            GuiPulse = 0f;
            transform.localScale = Vector3.one * _baseScale;
            var parent = transform.parent;
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch == null || ch.name != "Visitor") continue;
                var sr = ch.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
                Object.Destroy(ch.gameObject);
            }
        }

        public IEnumerator Welcome(BeeVisit visit, Vector3 from)
        {
            RefreshResidents();
            _pulse = true;
            _pulseT = 0f;
            var go = WorldBuilder.Sprite("Visitor", SpriteCatalog.Bee, from, 0.22f, 48, transform.parent);
            var sr = go.GetComponent<SpriteRenderer>();
            // Keep Kind.Tint; bump brightness a touch for foil finishes.
            var tint = visit.Kind.Tint;
            if (visit.Finish != BeeFinish.Normal)
                tint = Color.Lerp(tint, Color.white, visit.Finish == BeeFinish.Holo ? 0.18f : 0.28f);
            sr.color = tint;
            float speedMul = Random.Range(0.8f, 1.2f);
            float phase = Random.Range(0f, Mathf.PI * 2f);
            float wobAmp = Random.Range(0.08f, 0.18f);
            float wobHz = Random.Range(1.2f, 2.4f);
            float t = 0f;
            float dur = 0.85f / (Pace * speedMul);
            Sfx.BeeHum();
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                // Live Mouth — LateUpdate keeps snapping the hive under the HUD button.
                var dest = Mouth;
                var p = Vector3.Lerp(from, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.55f;
                // Sideways sine, gone by the time the bee reaches the mouth.
                Vector3 dir = dest - from;
                dir.z = 0f;
                float dmag = dir.magnitude;
                if (dmag > 0.001f)
                {
                    var perp = new Vector3(-dir.y, dir.x, 0f) / dmag;
                    float fade = 1f - u;
                    float wob = Mathf.Sin(phase + t * wobHz * Mathf.PI * 2f) * wobAmp * fade;
                    p += perp * wob;
                }
                go.transform.position = p;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.26f, 0.12f, u);
                sr.sprite = SpriteCatalog.BeeFrame(Time.time * 18f);
                sr.flipX = dest.x < from.x;
                yield return null;
            }
            Object.Destroy(go);
            RefreshResidents();
        }

        void RefreshResidents()
        {
            // World residents sit under the OnGUI skep, so the visible halo
            // is drawn in FlockFiveApp. Keep these off so they cannot stack.
            if (_residents == null) return;
            for (int i = 0; i < _residents.Length; i++)
            {
                if (_residents[i] == null) continue;
                _residents[i].enabled = false;
            }
        }

        void LateUpdate()
        {
            if (_residents == null) return;
            float t = Time.time;
            if (_pulse)
            {
                _pulseT += Time.deltaTime;
                float u = Mathf.Clamp01(_pulseT / 0.4f);
                GuiPulse = Mathf.Sin(u * Mathf.PI);
                float k = _baseScale * (1f + 0.08f * GuiPulse);
                transform.localScale = Vector3.one * k;
                if (u >= 1f)
                {
                    _pulse = false;
                    GuiPulse = 0f;
                    transform.localScale = Vector3.one * _baseScale;
                }
            }
            else GuiPulse = 0f;
            if (_glints != null)
            {
                for (int g = 0; g < _glints.Length; g++)
                {
                    var gl = _glints[g];
                    if (gl == null || !gl.enabled) continue;
                    float tw = 0.5f + 0.5f * Mathf.Sin(t * 2.4f + _glintPh[g]);
                    tw = tw * tw;
                    gl.color = new Color(1f, 0.94f, 0.62f, 0.15f + 0.75f * tw);
                }
            }
            for (int i = 0; i < _residents.Length; i++)
            {
                var sr = _residents[i];
                if (sr == null || !sr.enabled) continue;
                if (_speedMul == null || i >= _speedMul.Length) continue;
                float rate = (1.55f + i * 0.22f) * Pace * _speedMul[i];
                float ax = t * rate + _phase[i];
                float orbit = (0.42f + (i % 3) * 0.12f) * _radiusK[i];
                float x = Mathf.Cos(ax) * orbit;
                float y = 0.28f + Mathf.Sin(ax * _liss[i]) * (orbit * 0.62f);
                float tx = -Mathf.Sin(ax);
                float ty = Mathf.Cos(ax * _liss[i]) * _liss[i];
                float wob = Mathf.Sin(t * _wobHz[i] * Mathf.PI * 2f + _phase[i]) * _wobAmp[i];
                Vector3 wobL = WorldWobble(transform, tx, ty, wob);
                Vector3 bobL = transform.InverseTransformVector(
                    new Vector3(0f, Mathf.Sin(t * 1.3f + _phase[i]) * 0.05f, 0f));
                sr.transform.localPosition = new Vector3(x + wobL.x, y + wobL.y + bobL.y, 0f);
                sr.sprite = SpriteCatalog.BeeFrame(t * 16f + _phase[i]);
                sr.flipX = Mathf.Cos(ax) < 0f;
            }
        }

        static Vector3 WorldWobble(Transform host, float tx, float ty, float amp)
        {
            Vector3 tangentW = host.TransformVector(new Vector3(tx, ty, 0f));
            tangentW.z = 0f;
            Vector3 normalW = new Vector3(-tangentW.y, tangentW.x, 0f);
            if (normalW.sqrMagnitude < 1e-8f) normalW = Vector3.up;
            else normalW.Normalize();
            return host.InverseTransformVector(normalW * amp);
        }
    }
}


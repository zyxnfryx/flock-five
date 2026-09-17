using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed class HiveView : MonoBehaviour
    {
        public Transform Home;
        public static float GuiPulse;
        SpriteRenderer[] _residents;
        float[] _phase;
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
            var comb = WorldBuilder.Sprite("Comb", SpriteCatalog.Glow, transform.position, 1f, 9, transform);
            comb.transform.localPosition = Vector3.zero;
            comb.transform.localScale = new Vector3(1.35f, 1.05f, 1f);
            comb.GetComponent<SpriteRenderer>().color = new Color(0.92f, 0.62f, 0.18f, 0.20f);

            var core = WorldBuilder.Sprite("CombCore", SpriteCatalog.Glow, transform.position, 1f, 10, transform);
            core.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            core.transform.localScale = new Vector3(0.72f, 0.58f, 1f);
            core.GetComponent<SpriteRenderer>().color = new Color(1f, 0.82f, 0.32f, 0.28f);

            _residents = new SpriteRenderer[6];
            _phase = new float[6];
            for (int i = 0; i < 6; i++)
            {
                var go = WorldBuilder.Sprite("HiveBee" + i, SpriteCatalog.Bee, transform.position, 0.16f, 11, transform);
                _residents[i] = go.GetComponent<SpriteRenderer>();
                _residents[i].enabled = false;
                _phase[i] = Random.Range(0f, 20f);
            }
            RefreshResidents();
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
            float t = 0f;
            const float dur = 0.85f;
            Sfx.BeeHum();
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                // Live Mouth — LateUpdate keeps snapping the hive under the HUD button.
                var dest = Mouth;
                var p = Vector3.Lerp(from, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.55f;
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
            for (int i = 0; i < _residents.Length; i++)
            {
                var sr = _residents[i];
                if (sr == null || !sr.enabled) continue;
                float a = t * (1.55f + i * 0.22f) + _phase[i];
                float orbit = 0.42f + (i % 3) * 0.12f;
                float x = Mathf.Cos(a) * orbit;
                float y = 0.28f + Mathf.Sin(a * 1.15f) * (orbit * 0.62f);
                sr.transform.localPosition = new Vector3(x, y, 0f);
                sr.sprite = SpriteCatalog.BeeFrame(t * 16f + _phase[i]);
                sr.flipX = Mathf.Cos(a) < 0f;
            }
        }
    }
}


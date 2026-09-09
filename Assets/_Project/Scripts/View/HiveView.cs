using System.Collections;
using UnityEngine;

namespace FlockFive
{
    public sealed class HiveView : MonoBehaviour
    {
        public Transform Home;
        SpriteRenderer[] _residents;
        float[] _phase;
        bool _pulse;
        float _pulseT;

        public static HiveView Attach(Transform parent)
        {
            var go = new GameObject("Hive");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-2.85f, 7.35f, 0f);
            var view = go.AddComponent<HiveView>();
            view.Build();
            return view;
        }

        public Vector3 Mouth => transform.position + Vector3.up * 0.12f;

        void Build()
        {
            Home = transform;
            var comb = WorldBuilder.Sprite("Comb", SpriteCatalog.Glow, transform.position, 1f, 9, transform);
            comb.transform.localPosition = Vector3.zero;
            comb.transform.localScale = new Vector3(1.35f, 1.05f, 1f);
            comb.GetComponent<SpriteRenderer>().color = new Color(0.92f, 0.62f, 0.18f, 0.55f);

            var core = WorldBuilder.Sprite("CombCore", SpriteCatalog.Glow, transform.position, 1f, 10, transform);
            core.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            core.transform.localScale = new Vector3(0.72f, 0.58f, 1f);
            core.GetComponent<SpriteRenderer>().color = new Color(1f, 0.82f, 0.32f, 0.7f);

            _residents = new SpriteRenderer[3];
            _phase = new float[3];
            for (int i = 0; i < 3; i++)
            {
                var go = WorldBuilder.Sprite("HiveBee" + i, SpriteCatalog.Bee, transform.position, 0.13f, 11, transform);
                _residents[i] = go.GetComponent<SpriteRenderer>();
                _phase[i] = Random.Range(0f, 20f);
            }
            RefreshResidents();
        }

        public IEnumerator Welcome(BeeVisit visit, Vector3 from)
        {
            RefreshResidents();
            _pulse = true;
            _pulseT = 0f;
            var go = WorldBuilder.Sprite("Visitor", SpriteCatalog.Bee, from, 0.18f, 17, transform.parent);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = visit.Kind.Tint;
            var dest = Mouth;
            float t = 0f;
            const float dur = 0.72f;
            Sfx.BeeHum();
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                var p = Vector3.Lerp(from, dest, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 1.35f;
                go.transform.position = p;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.20f, 0.12f, u);
                sr.sprite = SpriteCatalog.BeeFrame(Time.time * 18f);
                sr.flipX = dest.x < from.x;
                yield return null;
            }
            Object.Destroy(go);
            RefreshResidents();
        }

        void RefreshResidents()
        {
            if (_residents == null) return;
            int shown = Mathf.Min(_residents.Length, Mathf.Max(1, Hive.Found));
            int slot = 0;
            for (int k = 0; k < Hive.Kinds && slot < shown; k++)
            {
                if (Hive.CountOf(k) <= 0) continue;
                if (_residents[slot] == null) { slot++; continue; }
                _residents[slot].enabled = true;
                _residents[slot].color = Hive.Roster[k].Tint;
                slot++;
            }
            for (int i = slot; i < _residents.Length; i++)
            {
                if (_residents[i] == null) continue;
                bool empty = Hive.Found == 0 && i == 0;
                _residents[i].enabled = empty;
                if (empty) _residents[i].color = new Color(1f, 0.78f, 0.22f, 0.85f);
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
                float k = 1f + 0.08f * Mathf.Sin(u * Mathf.PI);
                transform.localScale = Vector3.one * k;
                if (u >= 1f)
                {
                    _pulse = false;
                    transform.localScale = Vector3.one;
                }
            }
            for (int i = 0; i < _residents.Length; i++)
            {
                var sr = _residents[i];
                if (sr == null || !sr.enabled) continue;
                float a = t * (3.6f + i * 0.4f) + _phase[i];
                float x = (i - 1) * 0.22f + Mathf.Cos(a) * 0.10f;
                float y = 0.08f + Mathf.Sin(a * 1.3f) * 0.10f;
                sr.transform.localPosition = new Vector3(x, y, 0f);
                sr.sprite = SpriteCatalog.BeeFrame(t * 16f + _phase[i]);
                sr.flipX = Mathf.Cos(a) < 0f;
            }
        }
    }
}

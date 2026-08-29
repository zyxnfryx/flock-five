using UnityEngine;

namespace FlockFive
{
    public sealed class BirdIdle : MonoBehaviour
    {
        public Vector3 RestLocal;
        public Vector3 RestScale = new Vector3(0.42f, 0.42f, 1f);
        public float Lift;
        public bool Frozen;
        public bool Flapping;
        public bool Sleeping;
        public bool Shrouded;
        public bool FaceLeft;
        public BirdColor Color;
        SpriteRenderer _sr;
        float _phase;
        float _liftShown;
        float _nextWing;
        float _nextRuffle;
        float _ruffle;
        float _flutterUntil;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _phase = Random.Range(0f, 40f);
            _nextRuffle = Time.time + Random.Range(0.4f, 3.2f);
            _nextWing = Time.time + Random.Range(0f, 0.08f);
        }

        public void Bind(BirdColor color, Vector3 restLocal)
        {
            Color = color;
            RestLocal = restLocal;
            Frozen = false;
            Flapping = false;
            _flutterUntil = 0f;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _sr.flipX = FaceLeft;
        }

        public void Flutter(float seconds)
        {
            Flapping = true;
            _flutterUntil = Time.time + Mathf.Max(0.12f, seconds);
            _nextWing = 0f;
        }

        void LateUpdate()
        {
            if (!isActiveAndEnabled) return;
            if (_flutterUntil > 0f && Time.time >= _flutterUntil)
            {
                _flutterUntil = 0f;
                if (Lift < 0.05f) Flapping = false;
            }
            if (_ruffle > 0f) _ruffle -= Time.deltaTime;
            bool show = _sr != null && _sr.enabled;
            bool fly = !Sleeping && !Shrouded && show && (Flapping || Lift > 0.05f || _ruffle > 0f);
            if (show)
            {
                _sr.flipX = FaceLeft;
                _sr.sprite = SpriteCatalog.BirdFrame(Color, Time.time * (fly ? 16f : 0.9f) + _phase, fly);
                _sr.color = Shrouded ? new Color(0.04f, 0.03f, 0.05f, 1f) : UnityEngine.Color.white;
                _sr.sortingOrder = Shrouded ? 7 : 12;
            }
            if (fly) BeatWings();
            else if (show && !Sleeping && !Shrouded) MaybeRuffle();

            if (Frozen) return;
            if (Shrouded)
            {
                transform.localPosition = RestLocal;
                transform.localRotation = Quaternion.identity;
                transform.localScale = RestScale;
                return;
            }
            float wantLift = Sleeping ? -0.10f : Lift;
            _liftShown = Mathf.MoveTowards(_liftShown, wantLift, 4.2f * Time.deltaTime);
            if (Sleeping)
            {
                float snore = Mathf.Sin(Time.time * 1.7f + _phase);
                transform.localPosition = new Vector3(RestLocal.x, RestLocal.y + snore * 0.025f + _liftShown, RestLocal.z);
                transform.localRotation = Quaternion.Euler(0f, 0f, (FaceLeft ? 8f : -8f) + snore * 2.5f);
                float breathe = 1f + snore * 0.03f;
                transform.localScale = new Vector3(RestScale.x * breathe, RestScale.y * (2f - breathe), 1f);
                return;
            }
            float bob = Mathf.Sin(Time.time * 6.6f + _phase) * (fly ? 0.10f : 0.018f);
            float beat = Mathf.Sin(Time.time * 21f + _phase);
            transform.localPosition = new Vector3(RestLocal.x, RestLocal.y + bob + _liftShown, RestLocal.z);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 5.1f + _phase) * (fly ? 8f : 5f));
            transform.localScale = new Vector3(
                RestScale.x * (1f + beat * 0.05f),
                RestScale.y * (1f - beat * 0.04f),
                1f);
        }

        void BeatWings()
        {
            if (Time.time < _nextWing) return;
            _nextWing = Time.time + Random.Range(0.07f, 0.12f);
            if (_ruffle > 0f && !Flapping && Lift < 0.05f) Sfx.FlapSoft();
            else Sfx.Flap();
        }

        void MaybeRuffle()
        {
            if (Time.time < _nextRuffle) return;
            if (!Sfx.QuietMid)
            {
                _nextRuffle = Time.time + Random.Range(1.2f, 2.4f);
                return;
            }
            _nextRuffle = Time.time + Random.Range(5.5f, 10f);
            _ruffle = 0.22f;
            _nextWing = 0f;
            Sfx.FlapSoft();
        }
    }
}

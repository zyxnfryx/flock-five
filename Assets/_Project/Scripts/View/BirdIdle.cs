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
        SpriteRenderer _face;
        float _phase;
        float _liftShown;
        float _nextWing;
        float _nextRuffle;
        float _ruffle;
        float _flutterUntil;
        float _blinkUntil;
        float _nextBlink;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _phase = Random.Range(0f, 40f);
            _nextRuffle = Time.time + Random.Range(0.4f, 3.2f);
            _nextWing = Time.time + Random.Range(0f, 0.08f);
            _nextBlink = Time.time + Random.Range(0.6f, 2.4f);
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
            EnsureFace();
        }

        public void Flutter(float seconds)
        {
            Flapping = true;
            _flutterUntil = Time.time + Mathf.Max(0.12f, seconds);
            _nextWing = 0f;
        }

        void EnsureFace()
        {
            if (_face != null)
            {
                _face.sprite = BirdMood.Face(Color);
                return;
            }
            var mood = BirdMood.Of(Color);
            var go = WorldBuilder.Sprite("Mood", BirdMood.Face(Color), transform.position, mood.FaceScale, 13, transform);
            go.transform.localRotation = Quaternion.identity;
            _face = go.GetComponent<SpriteRenderer>();
            _face.sortingOrder = 13;
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
            var mood = BirdMood.Of(Color);
            if (show)
            {
                _sr.flipX = FaceLeft;
                _sr.sprite = SpriteCatalog.BirdFrame(Color, Time.time * (fly ? 16f : 0.9f) + _phase, fly);
                _sr.color = Shrouded ? new Color(0.04f, 0.03f, 0.05f, 1f) : UnityEngine.Color.white;
                _sr.sortingOrder = Shrouded ? 7 : 12;
            }
            PlaceFace(mood, show && !Shrouded);
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
            float scale = mood.Scale;
            if (Sleeping)
            {
                float snore = Mathf.Sin(Time.time * (Color == BirdColor.Violet ? 1.35f : 1.7f) + _phase);
                float droop = Color == BirdColor.Violet ? 0.04f : 0.025f;
                transform.localPosition = new Vector3(RestLocal.x, RestLocal.y + snore * droop + _liftShown, RestLocal.z);
                float z = (FaceLeft ? 8f : -8f) + snore * 2.5f + mood.Lean * 0.35f;
                transform.localRotation = Quaternion.Euler(0f, 0f, z);
                float breathe = 1f + snore * 0.03f;
                transform.localScale = new Vector3(RestScale.x * scale * breathe, RestScale.y * scale * (2f - breathe), 1f);
                return;
            }
            float look = Color == BirdColor.Teal ? Mathf.Sin(Time.time * 1.15f + _phase) * mood.Tilt : Mathf.Sin(Time.time * 5.1f + _phase) * mood.Tilt;
            float bob = Mathf.Sin(Time.time * mood.BobHz + _phase) * (fly ? mood.BobAmp * 3.2f : mood.BobAmp);
            float beat = Mathf.Sin(Time.time * 21f + _phase);
            transform.localPosition = new Vector3(RestLocal.x, RestLocal.y + bob + _liftShown, RestLocal.z);
            transform.localRotation = Quaternion.Euler(0f, 0f, look + mood.Lean);
            float squash = fly ? mood.Squash : mood.Squash * 0.7f;
            transform.localScale = new Vector3(
                RestScale.x * scale * (1f + beat * squash),
                RestScale.y * scale * (1f - beat * squash * 0.8f),
                1f);
        }

        void PlaceFace(BirdMood.Pose mood, bool on)
        {
            if (_face == null) return;
            _face.enabled = on;
            if (!on) return;
            if (Time.time >= _nextBlink)
            {
                _blinkUntil = Time.time + 0.08f;
                _nextBlink = Time.time + mood.BlinkEvery + Random.Range(-0.4f, 0.8f);
            }
            bool blink = Time.time < _blinkUntil || Sleeping;
            float x = FaceLeft ? -mood.HeadX : mood.HeadX;
            float y = mood.HeadY + (Sleeping ? -0.02f : 0f);
            _face.transform.localPosition = new Vector3(x, y, 0f);
            _face.transform.localRotation = Quaternion.identity;
            _face.flipX = FaceLeft;
            _face.sortingOrder = 13;
            float fs = mood.FaceScale * (blink ? 1f : 1f);
            _face.transform.localScale = new Vector3(fs, blink ? fs * 0.18f : fs, 1f);
            _face.color = UnityEngine.Color.white;
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

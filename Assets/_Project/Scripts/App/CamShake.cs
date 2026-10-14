using UnityEngine;

namespace FlockFive
{
    public sealed class CamShake : MonoBehaviour
    {
        public static CamShake Live;
        // Screen-space mirror of the shake (GUI pixels, degrees) so IMGUI garden
        // controls (hive, restart) shake with the scene.
        public static Vector2 HudOffset;
        public static float HudTwist;
        public static event System.Action AfterShake;
        static readonly System.Collections.Generic.List<Transform> HudWorld = new System.Collections.Generic.List<Transform>(4);
        Vector3 _rest;
        Vector3 _restWorld;
        bool _boltWorld;
        float _restSize;
        Camera _cam;
        float _t;
        float _dur;
        float _amp;
        float _twist;
        float _kick;
        float _seed;
        float _bolt;
        float _boltDur;
        float _boltAmp;
        float _boltKick;
        float _boltTwist;
        float _boltSeed;
        Vector2 _boltDir;

        void Awake()
        {
            Live = this;
            _cam = GetComponent<Camera>();
            _rest = transform.localPosition;
            _restWorld = transform.position;
            _restSize = _cam != null ? _cam.orthographicSize : 8.2f;
            if (GetComponent<HudPin>() == null) gameObject.AddComponent<HudPin>();
        }

        // Nectar bottles and other world HUD. A world-only bolt pins these so
        // the camera can shake the garden without dragging them.
        public static void WatchHud(Transform t)
        {
            if (t == null || HudWorld.Contains(t)) return;
            HudWorld.Add(t);
        }

        public static void ForgetHud(Transform t)
        {
            if (t == null) return;
            HudWorld.Remove(t);
        }

        void OnEnable() => Live = this;

        void OnDisable()
        {
            if (Live == this) Live = null;
            Settle();
        }

        public static void Combo(int size)
        {
            if (Live == null) return;
            size = Mathf.Clamp(size, 1, Palette.ComboMax);
            float n = Mathf.Min(size - 1, 10);
            Live.Punch(0.16f + 0.07f * n, 0.09f + 0.07f * n, 1.5f + 1.2f * n, 0.10f + 0.05f * n);
        }

        // b87 (Brandon, iPad): sparrow / hawk hits made the whole field "breathe" — each
        // peck zoomed the camera (kick 3-9%) and twisted it 1-2.6 deg. Pest hits now use
        // one tiny crisp shake: no zoom, no twist, short and small, so branches stay steady.
        public const float PestTapDur = 0.07f;
        public const float PestTapAmp = 0.022f;
        public static bool LegacyPestPunch; // shot harness only: replays the old pest punch for a before clip
        public static void PestTap()
        {
            if (Live == null) return;
            if (LegacyPestPunch) { Live.Punch(0.10f, 0.07f, 2.4f, 0.08f); return; }
            Live.Punch(PestTapDur, PestTapAmp, 0f, 0f);
        }

        public void Punch(float dur, float amp, float twist, float kick)
        {
            _dur = Mathf.Max(0.08f, dur);
            _amp = amp;
            _twist = twist;
            _kick = kick;
            _t = _dur;
            _seed = Random.Range(0f, 80f);
        }

        public static void Bolt(float power) => Bolt(power, false);

        // worldOnly shakes the camera and leaves IMGUI (undo, hive, top bar) put.
        public static void Bolt(float power, bool worldOnly)
        {
            if (Live == null) return;
            power = Mathf.Clamp01(power);
            Live._boltDur = Mathf.Lerp(0.46f, 0.78f, power);
            Live._boltAmp = Mathf.Lerp(0.07f, 0.16f, power);
            Live._boltKick = Mathf.Lerp(0.022f, 0.052f, power);
            Live._boltTwist = Mathf.Lerp(0.85f, 2.1f, power);
            Live._bolt = Live._boltDur;
            Live._boltWorld = worldOnly;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Live._boltDir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.52f);
            Live._boltSeed = Random.Range(0f, 40f);
            Live._t = 0f;
            if (worldOnly)
            {
                HudOffset = Vector2.zero;
                HudTwist = 0f;
            }
        }

        void LateUpdate()
        {
            if (GamePause.Paused) return;
            if (_bolt > 0f)
            {
                ApplyBolt();
                return;
            }
            if (_t <= 0f) return;
            _t -= ShakeStep();
            if (_t <= 0f)
            {
                Settle();
                return;
            }
            float u = Mathf.Clamp01(_t / _dur);
            float decay = u * u;
            float w = AdPause.Unscaled * 62f + _seed;
            transform.localPosition = _rest + new Vector3(
                Mathf.Sin(w) * _amp * decay,
                Mathf.Cos(w * 1.17f) * _amp * 0.72f * decay,
                0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(w * 1.3f) * _twist * decay);
            if (_cam != null)
                _cam.orthographicSize = _restSize * (1f - _kick * Mathf.Sin((1f - u) * Mathf.PI) * decay);
            Mirror();
        }

        void ApplyBolt()
        {
            _bolt -= ShakeStep();
            if (_bolt <= 0f)
            {
                Settle();
                return;
            }
            float t = _boltDur - _bolt;
            float s1 = Mathf.Exp(-(t - 0.018f) * (t - 0.018f) / 0.00048f);
            float s2 = 0.58f * Mathf.Exp(-(t - 0.096f) * (t - 0.096f) / 0.00105f);
            float roll = Mathf.Exp(-t * 3.1f) * Mathf.Sin(t * 19.5f + _boltSeed);
            float env = s1 + s2 + 0.24f * roll;
            transform.localPosition = _rest + new Vector3(_boltDir.x, _boltDir.y, 0f) * (_boltAmp * env);
            transform.localRotation = Quaternion.Euler(0f, 0f, (s1 * 0.75f + s2 * 0.4f + roll * 0.28f) * _boltTwist);
            if (_cam != null)
                _cam.orthographicSize = _restSize * (1f - _boltKick * (s1 + 0.32f * s2));
            if (_boltWorld)
            {
                HudOffset = Vector2.zero;
                HudTwist = 0f;
            }
            else Mirror();
        }

        // After feeders have written their rest pose. Keeps world HUD screen-fixed
        // while the camera shakes the garden.
        public void PinHudLate()
        {
            if (!_boltWorld || _bolt <= 0f) return;
            float zoom = _cam != null && _restSize > 0.01f ? _cam.orthographicSize / _restSize : 1f;
            var rot = transform.rotation;
            var cam = transform.position;
            for (int i = HudWorld.Count - 1; i >= 0; i--)
            {
                var t = HudWorld[i];
                if (t == null) { HudWorld.RemoveAt(i); continue; }
                var restPos = t.position;
                var restRot = t.rotation;
                var restScale = t.localScale;
                var rel = new Vector3(restPos.x - _restWorld.x, restPos.y - _restWorld.y, 0f) * zoom;
                var spun = rot * rel;
                t.position = new Vector3(cam.x + spun.x, cam.y + spun.y, restPos.z);
                t.rotation = rot * restRot;
                t.localScale = restScale * zoom;
            }
            var after = AfterShake;
            if (after != null) after();
        }

        // A capture frame's wall step is the encode hitch. Follow the capture
        // clock so the shake lasts the right number of recorded frames.
        static float ShakeStep()
        {
            if (Time.captureDeltaTime > 0f) return Time.captureDeltaTime;
            return AdPause.UnscaledDelta;
        }

        void Mirror()
        {
            float size = _restSize > 0.01f ? _restSize : 8.2f;
            float ppu = Screen.height / (2f * size);
            var d = transform.localPosition - _rest;
            HudOffset = new Vector2(-d.x * ppu, d.y * ppu);
            HudTwist = transform.localEulerAngles.z;
            if (HudTwist > 180f) HudTwist -= 360f;
        }

        void Settle()
        {
            HudOffset = Vector2.zero;
            HudTwist = 0f;
            _t = 0f;
            transform.localPosition = _rest;
            transform.localRotation = Quaternion.identity;
            if (_cam != null) _cam.orthographicSize = _restSize;
            _boltWorld = false;
        }
    }

    // Runs after feeder LateUpdate so a world-only bolt can pin HUD sprites.
    [DefaultExecutionOrder(10000)]
    public sealed class HudPin : MonoBehaviour
    {
        CamShake _shake;

        void LateUpdate()
        {
            if (_shake == null) _shake = GetComponent<CamShake>();
            if (_shake != null) _shake.PinHudLate();
        }
    }
}

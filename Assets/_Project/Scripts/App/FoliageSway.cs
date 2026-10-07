using UnityEngine;

namespace FlockFive
{
    // One sway for every garden painting, including the home wash.
    // A mask around each crown rotates that paint in place. Tips travel further
    // than the trunk because the angle is scaled by distance, and the mask edge
    // is pinned so the still paint underneath does not show a seam.
    // Built once. LateUpdate only writes a gust into the existing property block.
    public sealed class FoliageSway : MonoBehaviour
    {
        static Material _mat;
        static bool _tried;
        static readonly int P0 = Shader.PropertyToID("_P0");
        static readonly int P1 = Shader.PropertyToID("_P1");
        static readonly int P2 = Shader.PropertyToID("_P2");
        static readonly int P3 = Shader.PropertyToID("_P3");
        static readonly int A0 = Shader.PropertyToID("_A0");
        static readonly int A1 = Shader.PropertyToID("_A1");
        static readonly int A2 = Shader.PropertyToID("_A2");
        static readonly int A3 = Shader.PropertyToID("_A3");
        static readonly int WindId = Shader.PropertyToID("_Wind");
        static readonly int WipeId = Shader.PropertyToID("_Wipe");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        static bool _lowReady;
        static bool _low;

        // iPhone SE (all three) and other 3 GB-class phones. Cached once.
        public static bool LowPower
        {
            get
            {
                if (_lowReady) return _low;
                _lowReady = true;
                _low = DetectLow();
                return _low;
            }
        }

        public static bool ShaderReady => EnsureMat();

        SpriteRenderer _sr;
        MaterialPropertyBlock _block;
        Vector4 _p0, _p1, _p2, _p3;
        Vector4 _a0, _a1, _a2, _a3;
        Vector4 _wipe;
        float _phase;
        bool _on;

        public static void Apply(SpriteRenderer sr, GardenScene scene)
        {
            if (sr == null) return;
            var sway = sr.GetComponent<FoliageSway>();
            if (sway == null) sway = sr.gameObject.AddComponent<FoliageSway>();
            sway.Boot(scene);
        }

        public void Boot(GardenScene scene)
        {
            _sr = GetComponent<SpriteRenderer>();
            _phase = scene == GardenScene.Desert ? 0.4f : 1.7f + (int)scene * 0.6f;
            _wipe = new Vector4(-0.2f, 0.14f, 0f, 0f);
            Fill(scene, LowPower);
            if (_block == null) _block = new MaterialPropertyBlock();
            _on = EnsureMat() && _sr != null;
            if (!_on) return;
            _sr.sharedMaterial = _mat;
            Push();
        }

        // edge runs from -feather (fully shown) to 1+feather (gone).
        // Horizontal reveals from the left. Vertical reveals from the top.
        public void SetWipe(float edge, float feather, bool horizontal)
        {
            _wipe.x = edge;
            _wipe.y = feather;
            _wipe.z = horizontal ? 1f : 0f;
            _wipe.w = 1f;
            if (_on) Push();
        }

        void LateUpdate()
        {
            if (!_on || _sr == null) return;
            Push();
        }

        void Push()
        {
            if (_block == null || _sr == null) return;
            var tex = _sr.sprite != null ? _sr.sprite.texture : null;
            if (tex != null) _block.SetTexture(MainTexId, tex);
            float t = Time.time + _phase;
            float gust = 0.12f + 0.10f * Mathf.Sin(t * 0.21f);
            float u = Mathf.Repeat(t, 8.4f);
            if (u > 6.2f)
                gust += Mathf.Sin((u - 6.2f) / 1.5f * Mathf.PI) * 0.9f;
            if (gust < 0f) gust = 0f;
            else if (gust > 1f) gust = 1f;
            _block.SetVector(P0, _p0);
            _block.SetVector(P1, _p1);
            _block.SetVector(P2, _p2);
            _block.SetVector(P3, _p3);
            _block.SetVector(A0, _a0);
            _block.SetVector(A1, _a1);
            _block.SetVector(A2, _a2);
            _block.SetVector(A3, _a3);
            _block.SetVector(WindId, new Vector4(gust, 0f, 0f, 0f));
            _block.SetVector(WipeId, _wipe);
            _sr.SetPropertyBlock(_block);
        }

        void Fill(GardenScene scene, bool low)
        {
            _p0 = _p1 = _p2 = _p3 = Vector4.zero;
            _a0 = _a1 = _a2 = _a3 = new Vector4(0f, 1f, 1f, 0f);
            if (scene == GardenScene.Desert)
            {
                // Palm enters from the top-left. Two short masks: the long fronds along
                // the top, then the droop under the crown. The cactus and mesas sit outside.
                Put(0, 0.00f, 1.00f, 0.046f, 0.86f, 0.98f, -0.16f, 0.58f, 0.35f);
                Put(1, 0.00f, 0.88f, 0.038f, 0.40f, 0.55f, -0.84f, 0.48f, 1.7f);
                return;
            }
            if (scene == GardenScene.Winter)
            {
                // Foreground boughs only. The moon and the far pines stay still.
                Put(0, 0.02f, 0.00f, 0.030f, 0.30f, 0.25f, 0.97f, 0.62f, 0.2f);
                if (!low) Put(1, 0.98f, 0.00f, 0.028f, 0.28f, -0.30f, 0.95f, 0.58f, 1.4f);
                return;
            }
            if (scene == GardenScene.Fall)
            {
                // Corner leaves. The tree line is too broad to sway cleanly.
                Put(0, 0.00f, 1.00f, 0.046f, 0.28f, 0.62f, -0.78f, 0.85f, 0.6f);
                return;
            }
            if (scene == GardenScene.Spring)
            {
                Put(0, 0.00f, 0.98f, 0.040f, 0.52f, 0.78f, -0.42f, 0.70f, 0.3f);
                Put(1, 1.00f, 0.80f, 0.032f, 0.22f, -0.70f, -0.30f, 0.70f, 1.1f);
                if (!low) Put(2, 0.02f, 0.02f, 0.026f, 0.28f, 0.40f, 0.92f, 0.60f, 2.0f);
                if (!low) Put(3, 0.98f, 0.04f, 0.026f, 0.26f, -0.45f, 0.90f, 0.60f, 2.6f);
                return;
            }
            // Summer jungle, also the home painting. Right palm and the left crown.
            // The hibiscus and the center banana stay still so the perches do not swim.
            Put(0, 0.99f, 0.78f, 0.042f, 0.48f, -0.88f, 0.20f, 0.75f, 0.5f);
            Put(1, 0.02f, 0.98f, 0.032f, 0.42f, 0.25f, -0.97f, 0.65f, 1.3f);
        }

        void Put(int i, float px, float py, float angle, float radius, float ax, float ay, float half, float phase)
        {
            float m = Mathf.Sqrt(ax * ax + ay * ay);
            if (m < 1e-4f) m = 1f;
            var p = new Vector4(px, py, angle, radius);
            var a = new Vector4(ax / m, ay / m, Mathf.Cos(half), phase);
            if (i == 0) { _p0 = p; _a0 = a; }
            else if (i == 1) { _p1 = p; _a1 = a; }
            else if (i == 2) { _p2 = p; _a2 = a; }
            else { _p3 = p; _a3 = a; }
        }

        static bool EnsureMat()
        {
            if (_mat != null) return true;
            if (_tried) return false;
            _tried = true;
            var sh = Resources.Load<Shader>("Shaders/FoliageSway");
            if (sh == null) sh = Shader.Find("FlockFive/FoliageSway");
            if (sh == null) return false;
            _mat = new Material(sh) { name = "FoliageSway" };
            return true;
        }

        static bool DetectLow()
        {
            var model = SystemInfo.deviceModel;
            if (!string.IsNullOrEmpty(model))
            {
                if (model.IndexOf("iPhone8,4", System.StringComparison.Ordinal) >= 0) return true;
                if (model.IndexOf("iPhone12,8", System.StringComparison.Ordinal) >= 0) return true;
                if (model.IndexOf("iPhone14,6", System.StringComparison.Ordinal) >= 0) return true;
                if (model.IndexOf("iPhone SE", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            int mem = SystemInfo.systemMemorySize;
            return mem > 0 && mem <= 3072;
        }
    }
}

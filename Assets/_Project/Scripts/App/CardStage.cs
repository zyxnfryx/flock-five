using UnityEngine;

namespace FlockFive
{
    // Tier Full only: the real thick-card flip. One slab mesh (front face, back face, brass wall) rendered by a private
    // camera into a RenderTexture, drawn with a single GUI.DrawTexture. Lives only while an inspect is open; no Canvas, no URP.
    // The faces are pixel grabs of the flat card as OnGUI drew it (CardFaceBake text and all), so nothing re-wraps in motion.
    // Any failure (no shader, RT, grab) demotes to Lite and the OnGUI slab flip takes over.
    public static class CardStage
    {
        const int Layer = 31;
        const float W = 1f, H = 1.4f, Margin = 1.30f, Fov = 22f;
        static GameObject _root;
        static Camera _cam;
        static Transform _slab;
        static Material _front, _back, _wall;
        static Mesh _mesh;
        static RenderTexture _rt;
        static Texture2D _texA, _texB;       // A = front face of the card, B = back face
        static Shader _shader;
        static bool _failed;

        public static bool Available => !_failed && CardQuality.Tier == CardTier.Full;

        // Grab the card rect from the frame buffer into face A (front) or B (back). Call right after drawing that face flat and neutral.
        public static bool Grab(bool back, Rect card)
        {
            if (!Available) return false;
            try
            {
                int w = Mathf.RoundToInt(card.width), h = Mathf.RoundToInt(card.height);
                if (w < 16 || h < 16 || w > 2048 || h > 2900) return false;
                float x = Mathf.Clamp(card.x, 0f, PhoneArea.W - w), y = Mathf.Clamp(PhoneArea.H - card.yMax, 0f, PhoneArea.H - h);
                ref var tex = ref (back ? ref _texB : ref _texA);
                if (tex == null || tex.width != w || tex.height != h)
                {
                    if (tex != null) Object.Destroy(tex);
                    tex = new Texture2D(w, h, TextureFormat.RGB24, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave,
                        name = back ? "CardGrabBack" : "CardGrabFront"
                    };
                }
                tex.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
                tex.Apply(false, false);
                return true;
            }
            catch (System.Exception)
            {
                Fail();
                return false;
            }
        }

        // angle 0..PI, fromBack = the face showing when the flip starts. Returns false if the stage cannot draw (caller draws the Lite flip).
        public static bool DrawFlip(Rect card, float angle, bool fromBack, Vector2 tilt, BeeFinish finish, float lift)
        {
            if (!Available || _texA == null || _texB == null) return false;
            try
            {
                if (!Ensure(card)) { Fail(); return false; }
                var start = fromBack ? _texB : _texA;
                var end = fromBack ? _texA : _texB;
                _front.mainTexture = start;
                _back.mainTexture = end;
                float foil = finish == BeeFinish.Holo ? 1f : finish == BeeFinish.InverseRainbow ? 2f : 0f;
                // The glare also sweeps with the turn so the face catches light as it comes round.
                var t = new Vector4(Mathf.Clamp(tilt.x + Mathf.Sin(angle) * 0.55f * (fromBack ? -1f : 1f), -1.2f, 1.2f), tilt.y, 0f, 0f);
                Set(_front, t, foil);
                Set(_back, t, foil);
                Set(_wall, t, foil);
                float bob = Mathf.Sin(angle);
                _slab.localRotation = Quaternion.Euler(-tilt.y * 6f + 6f * bob, angle * Mathf.Rad2Deg + tilt.x * 6f * (1f - bob), 0f);
                _slab.localPosition = new Vector3(0f, 0f, -lift * 0.35f * bob);
                var prev = RenderTexture.active;
                _cam.Render();
                RenderTexture.active = prev;
                var r = new Rect(card.center.x - card.width * Margin * 0.5f, card.center.y - card.height * Margin * 0.5f, card.width * Margin, card.height * Margin);
                GUI.color = Color.white;
                GUI.DrawTexture(r, _rt, ScaleMode.StretchToFill, true);
                return true;
            }
            catch (System.Exception)
            {
                Fail();
                return false;
            }
        }

        static void Set(Material m, Vector4 tilt, float foil)
        {
            m.SetVector("_Tilt", tilt);
            m.SetFloat("_Foil", foil);
            m.SetFloat("_Glare", 0.6f);
        }

        static bool Ensure(Rect card)
        {
            if (_shader == null)
            {
                _shader = Resources.Load<Shader>("Shaders/CardFoil");
                if (_shader == null) _shader = Shader.Find("FlockFive/CardFoil");
                if (_shader == null || !_shader.isSupported) return false;
            }
            int rw = Mathf.RoundToInt(card.width * Margin), rh = Mathf.RoundToInt(card.height * Margin);
            float cap = Mathf.Min(1f, 1024f / Mathf.Max(rw, rh));
            rw = Mathf.Max(64, Mathf.RoundToInt(rw * cap)); rh = Mathf.Max(64, Mathf.RoundToInt(rh * cap));
            if (_rt == null || _rt.width != rw || _rt.height != rh)
            {
                if (_rt != null) { _rt.Release(); Object.Destroy(_rt); }
                _rt = new RenderTexture(rw, rh, 16, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                    name = "CardStageRT"
                };
                if (!_rt.Create()) return false;
                if (_cam != null) _cam.targetTexture = _rt;
            }
            if (_root == null)
            {
                _root = new GameObject("CardStage") { hideFlags = HideFlags.HideAndDontSave };
                _root.transform.position = new Vector3(0f, -4000f, 0f);
                var camGo = new GameObject("CardStageCam") { hideFlags = HideFlags.HideAndDontSave };
                camGo.transform.SetParent(_root.transform, false);
                _cam = camGo.AddComponent<Camera>();
                _cam.enabled = false;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                // Straight-alpha edge: bleed the frame wood colour into the transparent area so GUI blending leaves no dark fringe.
                _cam.backgroundColor = new Color(CardArt.FrameWood.r, CardArt.FrameWood.g, CardArt.FrameWood.b, 0f);
                _cam.cullingMask = 1 << Layer;
                _cam.fieldOfView = Fov;
                _cam.nearClipPlane = 0.1f;
                _cam.farClipPlane = 20f;
                _cam.allowHDR = false;
                _cam.allowMSAA = true;
                _cam.useOcclusionCulling = false;
                float d = (H * Margin * 0.5f) / Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
                camGo.transform.localPosition = new Vector3(0f, 0f, -d);
                camGo.transform.localRotation = Quaternion.identity;
                _cam.aspect = card.width / card.height;
                _cam.targetTexture = _rt;
                var slabGo = new GameObject("CardSlab") { hideFlags = HideFlags.HideAndDontSave, layer = Layer };
                slabGo.transform.SetParent(_root.transform, false);
                _slab = slabGo.transform;
                _mesh = BuildMesh();
                slabGo.AddComponent<MeshFilter>().sharedMesh = _mesh;
                _front = new Material(_shader) { name = "CardFront", hideFlags = HideFlags.HideAndDontSave };
                _back = new Material(_shader) { name = "CardBack", hideFlags = HideFlags.HideAndDontSave };
                _wall = new Material(_shader) { name = "CardWall", hideFlags = HideFlags.HideAndDontSave };
                _wall.SetFloat("_Mode", 1f);
                var j = CardArt.JewelRect(new Rect(0f, 0f, 1f, 1f), 1.25f);
                // uv origin is bottom-left, GUI rect origin is top-left.
                var jv = new Vector4(j.x, 1f - j.yMax, j.xMax, 1f - j.y);
                _front.SetVector("_Jewel", jv);
                _back.SetVector("_Jewel", new Vector4(-1f, -1f, -1f, -1f));
                _wall.SetVector("_Jewel", jv);
                var mr = slabGo.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { _front, _back, _wall };
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            _cam.aspect = card.width / card.height;
            return _cam != null && _slab != null;
        }

        // 3 submeshes: 0 front face (z +T/2), 1 back face (z -T/2, u mirrored so it reads correctly from behind), 2 wall ring.
        static Mesh BuildMesh()
        {
            float hw = W * 0.5f, hh = H * 0.5f, hz = CardSlab.Thick * W * 0.5f;
            var v = new System.Collections.Generic.List<Vector3>();
            var n = new System.Collections.Generic.List<Vector3>();
            var uv = new System.Collections.Generic.List<Vector2>();
            var front = new System.Collections.Generic.List<int>();
            var back = new System.Collections.Generic.List<int>();
            var wall = new System.Collections.Generic.List<int>();
            void Quad(System.Collections.Generic.List<int> tri, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nrm, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                n.Add(nrm); n.Add(nrm); n.Add(nrm); n.Add(nrm);
                uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
                tri.Add(i); tri.Add(i + 1); tri.Add(i + 2); tri.Add(i); tri.Add(i + 2); tri.Add(i + 3);
            }
            // Front faces -z (toward the camera at -z): winding clockwise seen from -z.
            Quad(front, new Vector3(-hw, -hh, -hz), new Vector3(-hw, hh, -hz), new Vector3(hw, hh, -hz), new Vector3(hw, -hh, -hz), Vector3.back,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
            Quad(back, new Vector3(hw, -hh, hz), new Vector3(hw, hh, hz), new Vector3(-hw, hh, hz), new Vector3(-hw, -hh, hz), Vector3.forward,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
            // Wall: left, right, top, bottom. u runs along the edge, v across the thickness.
            Quad(wall, new Vector3(-hw, -hh, hz), new Vector3(-hw, hh, hz), new Vector3(-hw, hh, -hz), new Vector3(-hw, -hh, -hz), Vector3.left,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            Quad(wall, new Vector3(hw, -hh, -hz), new Vector3(hw, hh, -hz), new Vector3(hw, hh, hz), new Vector3(hw, -hh, hz), Vector3.right,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            Quad(wall, new Vector3(-hw, hh, -hz), new Vector3(-hw, hh, hz), new Vector3(hw, hh, hz), new Vector3(hw, hh, -hz), Vector3.up,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
            Quad(wall, new Vector3(-hw, -hh, hz), new Vector3(-hw, -hh, -hz), new Vector3(hw, -hh, -hz), new Vector3(hw, -hh, hz), Vector3.down,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
            var m = new Mesh { name = "CardSlabMesh", hideFlags = HideFlags.HideAndDontSave, subMeshCount = 3 };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(front, 0);
            m.SetTriangles(back, 1);
            m.SetTriangles(wall, 2);
            m.RecalculateBounds();
            return m;
        }

        public static void Fail()
        {
            _failed = true;
            Release();
            CardQuality.Demote();
        }

        // Inspect closed: free the camera, mesh, RT, and the two grabs.
        public static void Release()
        {
            if (_rt != null) { _rt.Release(); Object.Destroy(_rt); _rt = null; }
            if (_texA != null) { Object.Destroy(_texA); _texA = null; }
            if (_texB != null) { Object.Destroy(_texB); _texB = null; }
            if (_front != null) Object.Destroy(_front);
            if (_back != null) Object.Destroy(_back);
            if (_wall != null) Object.Destroy(_wall);
            if (_mesh != null) Object.Destroy(_mesh);
            if (_root != null) Object.Destroy(_root);
            _front = _back = _wall = null; _mesh = null; _root = null; _cam = null; _slab = null;
        }

        public static bool HasFaces => _texA != null && _texB != null;
    }
}

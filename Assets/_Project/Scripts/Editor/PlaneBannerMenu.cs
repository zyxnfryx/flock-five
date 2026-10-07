#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FlockFive.Editor
{
    [InitializeOnLoad]
    static class PlaneBannerMenu
    {
        static PlaneBannerMenu()
        {
            EditorApplication.playModeStateChanged += OnPlay;
        }

        static void OnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            if (!SessionState.GetBool("FlockFive.ForcePlane", false)) return;
            SessionState.SetBool("FlockFive.ForcePlane", false);
            FlockFiveApp.ForceUsaPlane();
        }

        [MenuItem("Flock Five/Debug/Force USA Plane")]
        public static void Force()
        {
            SessionState.SetBool("FlockFive.ForcePlane", true);
            FlockFiveApp.ForceUsaPlane();
            Debug.Log("[plane] forced. It flies on the home screen once the sky is clear.");
        }

        // Batch camera render. Unity -executeMethod FlockFive.Editor.PlaneBannerMenu.SaveStills
        // Needs a graphics device (no -nographics). Writes new b69-final stills and leaves
        // the earlier feature stills alone.
        public static void SaveStills()
        {
            string dir = "/Users/zfxgames/wkspaces/birdshot/gb";
            Directory.CreateDirectory(dir);
            int n = 0;
            n += Shot(dir + "/b69-final-plane.png", 750, 1334, SkyPlane);
            n += Shot(dir + "/b69-final-honey.png", 750, 1334, HoneyRow);
            Debug.Log("B69_STILLS_DONE " + n);
        }

        static int Shot(string path, int w, int h, System.Action build)
        {
            var camGo = new GameObject("B69StillCam");
            var root = new GameObject("B69StillRoot");
            try
            {
                var cam = camGo.AddComponent<Camera>();
                // A bare Camera does not draw sprites under URP. The extra data
                // puts this shot on the same renderer the garden camera uses.
                var extra = cam.GetUniversalAdditionalCameraData();
                extra.renderType = CameraRenderType.Base;
                cam.orthographic = true;
                cam.orthographicSize = 5.6f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.53f, 0.76f, 0.93f, 1f);
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 30f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
                cam.aspect = w / (float)h;
                build();
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                var bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);
                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);
                if (bytes == null || bytes.Length < 1000) return 0;
                File.WriteAllBytes(path, bytes);
                Debug.Log("[still] " + path + " " + bytes.Length);
                return 1;
            }
            finally
            {
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(root);
            }
        }

        static void SkyPlane()
        {
            var bg = SpriteCatalog.GardenBg;
            if (bg != null) Put(bg, new Vector3(0f, -0.2f, 2f), 12f);
            var plane = SpriteCatalog.UsaPlane;
            var banner = SpriteCatalog.UsaBanner;
            if (plane != null) Put(plane, new Vector3(-0.35f, 3.15f, 0f), 2.05f);
            if (banner != null) Put(banner, new Vector3(0f, 0.85f, 0f), 1.15f);
        }

        static Transform StillRoot()
        {
            var root = GameObject.Find("B69StillRoot");
            return root != null ? root.transform : null;
        }

        static void HoneyRow()
        {
            HoneyArt.Warm();
            var fill = HoneyArt.Fill();
            if (fill == null) return;
            // Unlit/Texture drops the hex alpha and draws a white block.
            var sh = Shader.Find("Unlit/Transparent");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) return;
            for (int i = 0; i < 3; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "Honey" + i;
                var root = StillRoot();
                if (root != null) go.transform.SetParent(root, true);
                Object.DestroyImmediate(go.GetComponent<Collider>());
                var r = go.GetComponent<MeshRenderer>();
                var mat = new Material(sh);
                mat.mainTexture = fill;
                r.sharedMaterial = mat;
                go.transform.position = new Vector3(-2.2f + i * 2.2f, 0.4f, 0f);
                go.transform.localScale = new Vector3(1.7f, 2.0f, 1f);
            }
        }

        static void Put(Sprite spr, Vector3 pos, float height)
        {
            var go = new GameObject(spr.name);
            var root = StillRoot();
            if (root != null) go.transform.SetParent(root, true);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            // The default sprite material is shared. The garden draw left its
            // texture on that material, so the plane and banner meshes showed
            // garden scraps. An instance keeps each sprite's own texture.
            if (sr.sharedMaterial != null)
            {
                var mat = new Material(sr.sharedMaterial);
                mat.mainTexture = spr.texture;
                sr.material = mat;
            }
            float ph = spr.bounds.size.y;
            float sc = ph > 0.01f ? height / ph : 1f;
            // Ortho size 5.6 at 750x1334. The backdrop is taller than the view
            // and must stay full-bleed; only the flyby is pulled back on screen.
            if (height < 6f)
            {
                float halfW = 5.6f * (750f / 1334f) - 0.12f;
                float worldW = spr.bounds.size.x * sc;
                if (worldW > halfW * 2f)
                {
                    sc *= (halfW * 2f) / worldW;
                    worldW = spr.bounds.size.x * sc;
                }
                float minX = -halfW + worldW * 0.5f;
                float maxX = halfW - worldW * 0.5f;
                if (pos.x < minX) pos.x = minX;
                if (pos.x > maxX) pos.x = maxX;
            }
            go.transform.position = pos;
            go.transform.localScale = new Vector3(sc, sc, 1f);
        }
    }
}
#endif

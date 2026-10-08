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

        // Card back, three honey (holo). Unity -executeMethod FlockFive.Editor.PlaneBannerMenu.SaveDripStill
        public static void SaveDripStill()
        {
            _stillHoney = 3;
            string dir = "/Users/zfxgames/wkspaces/birdshot/gb";
            Directory.CreateDirectory(dir);
            int n = Shot(dir + "/b70-drips.png", 780, 1100, CardBackHoney);
            Debug.Log("B70_DRIPS_DONE " + n);
        }

        // Five honey so the card row shows every drip size. Unity -executeMethod FlockFive.Editor.PlaneBannerMenu.SaveDripSizeStill
        public static void SaveDripSizeStill()
        {
            _stillHoney = 5;
            string dir = "/Users/zfxgames/wkspaces/birdshot/gb";
            Directory.CreateDirectory(dir);
            int n = Shot(dir + "/b70-dripsize.png", 780, 1100, CardBackHoney);
            Debug.Log("B70_DRIPSIZE_DONE " + n);
        }

        static int _stillHoney = 3;

        static int _shotW, _shotH;

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
                _shotW = w;
                _shotH = h;
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

        // One holo card back: Static Wing, three honey, drips from HoneyArt.LayoutCell.
        static void CardBackHoney()
        {
            int w = _shotW;
            int h = _shotH;
            var tint = new Color(0.88f, 0.90f, 0.55f, 1f);
            var wood = Color.Lerp(new Color(0.28f, 0.16f, 0.07f, 1f), tint, 0.35f);
            var face = Color.Lerp(new Color(0.98f, 0.92f, 0.72f, 1f), tint, 0.18f);
            var rim = new Color(0.15f, 0.48f, 0.92f, 1f);
            var ink = new Color(0.16f, 0.07f, 0.02f, 1f);
            var card = RoundedCard(w, h, wood, face, 22f, 46f);
            PixQuad(new Rect(0f, 0f, w, h), card, Color.white, 1f);

            float innerX = w * 0.10f;
            float innerW = w * 0.80f;
            PutLine("Static Wing", w * 0.5f, h * 0.10f, 64, ink, 0.6f);
            PutLine("Shockingly social.", w * 0.5f, h * 0.22f, 36, ink, 0.6f);
            PutLine("Hair stands up; so", w * 0.5f, h * 0.27f, 36, ink, 0.6f);
            PutLine("does the hive.", w * 0.5f, h * 0.32f, 36, ink, 0.6f);

            // Same band the album back reserves: bottom quarter of the face.
            float faceTop = h * 0.06f;
            float faceH = h * 0.88f;
            float honeyH = faceH * 0.28f;
            var zone = new Rect(innerX, faceTop + faceH - honeyH, innerW, honeyH);
            int honey = _stillHoney;
            float gap = Mathf.Max(3f, zone.width * 0.02f);
            float cellH = zone.height * 0.92f;
            float cellW = cellH * (96f / 111f) * 0.78f;
            float rowW = honey * cellW + (honey - 1) * gap;
            if (rowW > zone.width)
            {
                float k = zone.width / rowW;
                cellW *= k;
                cellH *= k;
                gap *= k;
                rowW = honey * cellW + (honey - 1) * gap;
            }
            float x0 = zone.center.x - rowW * 0.5f;
            float y0 = zone.center.y - cellH * 0.5f;
            var stamps = new HoneyArt.Stamp[8];
            float z = 0.25f;
            for (int i = 0; i < honey; i++)
            {
                var hex = new Rect(x0 + i * (cellW + gap), y0, cellW, cellH);
                int n = HoneyArt.LayoutCell(hex, rim, 0f, 1f, i, true, false, true, stamps);
                for (int s = 0; s < n; s++)
                {
                    PixQuad(stamps[s].Rect, stamps[s].Tex, stamps[s].Color, z);
                    z -= 0.01f;
                }
            }
        }

        static Texture2D RoundedCard(int w, int h, Color wood, Color face, float border, float radius)
        {
            var px = new Color32[w * h];
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float rad = radius;
            for (int iy = 0; iy < h; iy++)
            {
                for (int ix = 0; ix < w; ix++)
                {
                    // iy 0 is the bottom of the texture. The card is symmetric.
                    float dx = Mathf.Abs(ix + 0.5f - cx) - (w * 0.5f - border - rad);
                    float dy = Mathf.Abs(iy + 0.5f - cy) - (h * 0.5f - border - rad);
                    float ax = Mathf.Max(dx, 0f);
                    float ay = Mathf.Max(dy, 0f);
                    float dist = Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(dx, dy), 0f) - rad;
                    float al = Mathf.Clamp01(0.5f - dist);
                    var c = Color.Lerp(wood, face, al);
                    px[iy * w + ix] = (Color32)c;
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        static void PixQuad(Rect px, Texture tex, Color tint, float z)
        {
            if (tex == null || px.width < 1f || px.height < 1f) return;
            float ppu = (5.6f * 2f) / _shotH;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Pix";
            var root = StillRoot();
            if (root != null) go.transform.SetParent(root, true);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var sh = Shader.Find("Unlit/Transparent");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) return;
            var mat = new Material(sh);
            mat.mainTexture = tex;
            mat.color = tint;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            float x = (px.x + px.width * 0.5f - _shotW * 0.5f) * ppu;
            float y = (_shotH * 0.5f - (px.y + px.height * 0.5f)) * ppu;
            go.transform.position = new Vector3(x, y, z);
            go.transform.localScale = new Vector3(px.width * ppu, px.height * ppu, 1f);
        }

        static void PutLine(string text, float cxPx, float topPx, int size, Color color, float z)
        {
            if (string.IsNullOrEmpty(text)) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;
            font.RequestCharactersInTexture(text, size, FontStyle.Bold);
            var atlas = font.material != null ? font.material.mainTexture : null;
            if (atlas == null) return;
            float width = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                if (!font.GetCharacterInfo(text[i], out var info, size, FontStyle.Bold)) continue;
                width += info.advance;
            }
            float ppu = (5.6f * 2f) / _shotH;
            float baseline = (_shotH * 0.5f - (topPx + size * 0.82f)) * ppu;
            float pen = (cxPx - width * 0.5f - _shotW * 0.5f) * ppu;
            var verts = new System.Collections.Generic.List<Vector3>(text.Length * 4);
            var uvs = new System.Collections.Generic.List<Vector2>(text.Length * 4);
            var tris = new System.Collections.Generic.List<int>(text.Length * 6);
            for (int i = 0; i < text.Length; i++)
            {
                if (!font.GetCharacterInfo(text[i], out var info, size, FontStyle.Bold)) continue;
                float x0 = pen + info.minX * ppu;
                float x1 = pen + info.maxX * ppu;
                float y0 = baseline + info.minY * ppu;
                float y1 = baseline + info.maxY * ppu;
                pen += info.advance * ppu;
                if (info.maxX <= info.minX || info.maxY <= info.minY) continue;
                int v = verts.Count;
                verts.Add(new Vector3(x0, y0, z));
                verts.Add(new Vector3(x1, y0, z));
                verts.Add(new Vector3(x0, y1, z));
                verts.Add(new Vector3(x1, y1, z));
                uvs.Add(info.uvBottomLeft);
                uvs.Add(info.uvBottomRight);
                uvs.Add(info.uvTopLeft);
                uvs.Add(info.uvTopRight);
                tris.Add(v);
                tris.Add(v + 2);
                tris.Add(v + 1);
                tris.Add(v + 2);
                tris.Add(v + 3);
                tris.Add(v + 1);
            }
            if (verts.Count == 0) return;
            var mesh = new Mesh { name = "CardLine" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            var go = new GameObject("CardLine");
            var root = StillRoot();
            if (root != null) go.transform.SetParent(root, true);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var sh = Shader.Find("Unlit/Transparent");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) return;
            var mat = new Material(sh);
            mat.mainTexture = atlas;
            mat.color = color;
            mr.sharedMaterial = mat;
        }
    }
}
#endif

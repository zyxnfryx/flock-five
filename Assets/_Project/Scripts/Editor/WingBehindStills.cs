#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FlockFive.Editor
{
    // iPhone portrait close-ups of a kit bird mid-flap.
    // Unity -executeMethod FlockFive.Editor.WingBehindStills.Capture
    public static class WingBehindStills
    {
        public static void Capture()
        {
            AudioListener.volume = 0f;
            string dir = "/Users/zfxgames/wkspaces/wingout";
            Directory.CreateDirectory(dir);
            int n = 0;
            n += Shot(dir + "/b88-wing-behind-left.png", true, BirdKit.Bow);
            n += Shot(dir + "/b88-wing-behind-right.png", false, BirdKit.Crown);
            Debug.Log("WING_STILLS_DONE " + n);
        }

        static int Shot(string path, bool faceLeft, BirdKit worn)
        {
            var camGo = new GameObject("WingStillCam");
            var root = new GameObject("WingStillRoot");
            try
            {
                var cam = camGo.AddComponent<Camera>();
                var extra = cam.GetUniversalAdditionalCameraData();
                extra.renderType = CameraRenderType.Base;
                cam.orthographic = true;
                cam.orthographicSize = 2.05f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f, 1f);
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 30f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
                const int w = 1179;
                const int h = 2556;
                cam.aspect = w / (float)h;

                var pose = SpriteCatalog.BirdPose(BirdColor.Ruby, 2);
                if (pose == null || !SpriteCatalog.TrySplitFarWing(pose, out var far, out var body))
                {
                    Debug.LogError("[wing-still] no far wing");
                    return 0;
                }
                var kit = SpriteCatalog.KitSprite(BirdColor.Ruby, worn);
                var rest = SpriteCatalog.Bird(BirdColor.Ruby);
                BirdIdle.KitAnchor(worn, 0, faceLeft, pose, rest, kit, out float kx, out float ky, out float ks, out float tilt, out bool flip);
                int bodyOrder = 10;
                BirdIdle.KitDrawOrder(worn, bodyOrder, out int farOrder, out int topOrder);
                Put(root.transform, "Far", far, Vector3.zero, 1f, farOrder, faceLeft, 0f);
                Put(root.transform, "Body", body, Vector3.zero, 1f, bodyOrder, faceLeft, 0f);
                Put(root.transform, "Kit", kit, new Vector3(kx, ky, 0f), ks, topOrder, flip, tilt);
                // Head-side wing rises beside the topper. Shift toward the tail so both sit in frame.
                float tail = faceLeft ? 0.42f : -0.42f;
                cam.transform.position = new Vector3(kx + tail, ky + 0.62f, -10f);

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
                Debug.Log("[wing-still] " + path + " " + bytes.Length);
                return 1;
            }
            finally
            {
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(root);
            }
        }

        static void Put(Transform parent, string name, Sprite spr, Vector3 pos, float scale, int order, bool flipX, float tilt)
        {
            if (spr == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.flipX = flipX;
            FlockSort.Apply(sr, order);
        }
    }
}
#endif

using UnityEngine;

namespace FlockFive
{
    // iPad pillarbox (Brandon, iPad approval). On a screen wider than the phone
    // reference (DeviceFit.PhoneAspect, 9:16) the whole game runs in a centered
    // phone-aspect area: the play camera renders into a W x H RenderTexture that is
    // drawn in the middle, the immediate-mode GUI is translated into the same area,
    // and pointer positions are shifted into it. The side bars show the garden
    // painting (bg_garden), aspect-filled, never stretched.
    // Game code reads PhoneArea.W / PhoneArea.H / PhoneArea.Safe instead of Screen.*; on phones (and in
    // batch mode / editor tests) these equal Screen.width / height / safeArea exactly.
    public static class PhoneArea
    {
        // Same tolerance PlayAspect uses before it treats a window as wider than a phone.
        const float Slack = 0.03f;

        public static bool Pillar
        {
            get
            {
                if (Application.isBatchMode) return false;
                int sw = Screen.width, sh = Screen.height;
                return sh > 0 && (float)sw / sh > DeviceFit.PhoneAspect + Slack;
            }
        }

        public static int W => Pillar ? Mathf.Min(Screen.width, Mathf.RoundToInt(Screen.height * DeviceFit.PhoneAspect)) : Screen.width;
        public static int H => Screen.height;
        // Left edge of the area in screen pixels.
        public static int OffX => Pillar ? (Screen.width - W) / 2 : 0;

        // Screen.safeArea in area coordinates (bottom-up, like Screen.safeArea).
        public static Rect Safe
        {
            get
            {
                var s = Screen.safeArea;
                if (!Pillar) return s;
                int off = OffX, w = W;
                float x0 = Mathf.Clamp(s.xMin - off, 0f, w), x1 = Mathf.Clamp(s.xMax - off, 0f, w);
                return Rect.MinMaxRect(x0, s.yMin, x1, s.yMax);
            }
        }

        // Raw device pointer (Input System, screen pixels) to area pixels.
        public static Vector2 In(Vector2 screen) => new Vector2(screen.x - OffX, screen.y);

        // Base GUI matrix: identity on phones, a pure x translation in pillar mode.
        public static Matrix4x4 GuiBase => Pillar ? Matrix4x4.Translate(new Vector3(OffX, 0f, 0f)) : Matrix4x4.identity;

        // GUIUtility.RotateAroundPivot / ScaleAroundPivot pre-multiply a pivot given in
        // raw screen pixels, ignoring GuiBase. In pillar mode (iPad) that pivots every
        // rotation OffX px to the left of the intended point, so a turning wheel and its
        // labels orbit off-center. These post-multiply in the caller's GUI space instead,
        // so the pivot is the point the caller means on every device.
        public static Matrix4x4 RotateAboutLocal(Matrix4x4 m, float degrees, Vector2 pivot)
        {
            var p = new Vector3(pivot.x, pivot.y, 0f);
            return m * Matrix4x4.Translate(p) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, degrees)) * Matrix4x4.Translate(-p);
        }

        public static Matrix4x4 ScaleAboutLocal(Matrix4x4 m, Vector2 scale, Vector2 pivot)
        {
            var p = new Vector3(pivot.x, pivot.y, 0f);
            return m * Matrix4x4.Translate(p) * Matrix4x4.Scale(new Vector3(scale.x, scale.y, 1f)) * Matrix4x4.Translate(-p);
        }

        public static void RotateAround(float degrees, Vector2 pivot) => GUI.matrix = RotateAboutLocal(GUI.matrix, degrees, pivot);
        public static void ScaleAround(Vector2 scale, Vector2 pivot) => GUI.matrix = ScaleAboutLocal(GUI.matrix, scale, pivot);

        static RenderTexture _rt;
        public static RenderTexture Target
        {
            get
            {
                int w = W, h = H;
                if (_rt != null && (_rt.width != w || _rt.height != h)) { _rt.Release(); Object.Destroy(_rt); _rt = null; }
                if (_rt == null)
                {
                    _rt = new RenderTexture(w, h, 24) { name = "PillarView", antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing) };
                    _rt.Create();
                }
                return _rt;
            }
        }
        public static bool IsTarget(RenderTexture t) => t != null && t == _rt;

        // First thing in the main OnGUI. Repaint: paint the garden bars and the play
        // camera's image with an identity matrix, then hand the GUI the area matrix.
        public static void BeginGui()
        {
            if (!Pillar) return;
            GUI.matrix = Matrix4x4.identity;
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                float sw = Screen.width, sh = Screen.height;
                var bg = SpriteCatalog.GardenBg;
                if (bg != null && bg.texture != null)
                {
                    var tr = bg.textureRect;
                    float tw = bg.texture.width, th = bg.texture.height;
                    // Aspect-fill (crop) the painting to the whole screen.
                    float imgA = tr.width / Mathf.Max(1f, tr.height), scrA = sw / Mathf.Max(1f, sh);
                    var uv = new Rect(tr.x / tw, tr.y / th, tr.width / tw, tr.height / th);
                    if (imgA > scrA) { float k = scrA / imgA; uv.x += uv.width * (1f - k) * 0.5f; uv.width *= k; }
                    else { float k = imgA / scrA; uv.y += uv.height * (1f - k) * 0.5f; uv.height *= k; }
                    var keep = GUI.color;
                    GUI.color = new Color(0.62f, 0.62f, 0.62f, 1f); // dimmed so the play area reads as the game
                    GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, sw, sh), bg.texture, uv);
                    GUI.color = keep;
                }
                if (_rt != null)
                    GUI.DrawTexture(new Rect(OffX, 0f, W, H), _rt, ScaleMode.StretchToFill, false);
            }
            GUI.matrix = GuiBase;
        }

        // Play camera hook (PortraitLock): render into the area texture in pillar mode.
        public static void BindCamera(Camera cam)
        {
            if (cam == null) return;
            if (Pillar)
            {
                var rt = Target;
                if (cam.targetTexture != rt) cam.targetTexture = rt;
                cam.rect = new Rect(0f, 0f, 1f, 1f);
            }
            else if (IsTarget(cam.targetTexture)) cam.targetTexture = null;
        }
    }
}

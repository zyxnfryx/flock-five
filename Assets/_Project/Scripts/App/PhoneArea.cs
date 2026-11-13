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

        // Play column for a window. Wider than a phone (iPad 820x1180) pillars to 9:16.
        // Phones keep the full window. Same rule as W/H, with the window passed in.
        public static void PlayPixels(int screenW, int screenH, out int w, out int h)
        {
            bool pillar = screenH > 0 && (float)screenW / screenH > DeviceFit.PhoneAspect + Slack;
            w = pillar ? Mathf.Min(screenW, Mathf.RoundToInt(screenH * DeviceFit.PhoneAspect)) : screenW;
            h = screenH;
        }

        public static int W => Pillar ? Mathf.Min(Screen.width, Mathf.RoundToInt(Screen.height * DeviceFit.PhoneAspect)) : Screen.width;
        public static int H => Screen.height;
        // Left edge of the area in screen pixels.
        public static int OffX => Pillar ? (Screen.width - W) / 2 : 0;

        // Screen.safeArea in area coordinates (bottom-up, like Screen.safeArea).
        // Every top-anchored control reads Safe, then GuiTopInset, and nothing else.
        public static Rect Safe
        {
            get
            {
                var s = Screen.safeArea;
#if UNITY_WEBGL && DEVELOPMENT_BUILD
                // DEV-ONLY inspect hook. Not a shipping-path proof. WebGL reports
                // the full window as safe, so Inspector cannot see the notch.
                // ?forceSafeTop=59 and optional ?forceSafeBottom=34 override it.
                // Release players and device builds never compile this.
                s = ApplyInspectSafe(s);
#endif
                if (!Pillar) return s;
                int off = OffX, w = W;
                float x0 = Mathf.Clamp(s.xMin - off, 0f, w), x1 = Mathf.Clamp(s.xMax - off, 0f, w);
                return Rect.MinMaxRect(x0, s.yMin, x1, s.yMax);
            }
        }

#if UNITY_WEBGL && DEVELOPMENT_BUILD
        static Rect ApplyInspectSafe(Rect safe)
        {
            float top = QueryPx("forceSafeTop");
            float bot = QueryPx("forceSafeBottom");
            if (top <= 0f && bot <= 0f) return safe;
            float h = H > 1 ? H : Screen.height;
            float y0 = Mathf.Max(0f, bot);
            float y1 = h - Mathf.Max(0f, top);
            if (y1 < y0) y1 = y0;
            float x0 = safe.width > 1f ? safe.xMin : 0f;
            float x1 = safe.width > 1f ? safe.xMax : (W > 1 ? W : Screen.width);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        static float QueryPx(string key)
        {
            string url = Application.absoluteURL;
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key)) return 0f;
            int q = url.IndexOf('?');
            if (q < 0) return 0f;
            string query = url.Substring(q + 1);
            int from = 0;
            while (from < query.Length)
            {
                int amp = query.IndexOf('&', from);
                if (amp < 0) amp = query.Length;
                string pair = query.Substring(from, amp - from);
                int eq = pair.IndexOf('=');
                string name = eq < 0 ? pair : pair.Substring(0, eq);
                if (name == key)
                {
                    string val = eq < 0 ? "" : pair.Substring(eq + 1);
                    float n;
                    if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out n) && n > 0f)
                        return n;
                    return 0f;
                }
                from = amp + 1;
            }
            return 0f;
        }
#endif

        // GUI pixels from the top of the screen down to the safe glass. 0 when the
        // inset is missing (iPad, or a safe rect that already fills the screen).
        public static float GuiTopInset(float screenH, Rect safe)
        {
            if (screenH < 2f || safe.width < 2f || safe.height < 2f) return 0f;
            return Mathf.Max(0f, screenH - safe.yMax);
        }

        // Raw device pointer (Input System, screen pixels) to area pixels.
        public static Vector2 In(Vector2 screen) => new Vector2(screen.x - OffX, screen.y);

        // Base GUI matrix: identity on phones, a pure x translation in pillar mode.
        public static Matrix4x4 GuiBase => Pillar ? Matrix4x4.Translate(new Vector3(OffX, 0f, 0f)) : Matrix4x4.identity;

        // GUIUtility.RotateAroundPivot / ScaleAroundPivot build a pivot in raw screen
        // pixels and pre-multiply it, ignoring GuiBase. On an iPad pillar that pivot
        // sits OffX px left of the point the caller named, so a turning wheel and its
        // labels orbit off the panel. These post-multiply in the caller's GUI space:
        // the pivot is the GUI point, then the matrix already in use (GuiBase, or a
        // pop scale) is applied outside it.
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

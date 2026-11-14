using UnityEngine;

namespace FlockFive
{
    // b126 (Brandon, mockup D): the tap-to-continue cue. A tiny fingertip that taps the caption box's
    // bottom-right corner, no words. One shared helper: FlockFiveApp.KeepCaptionOnTop (the hook every
    // caption paint path calls with its final plate) asks ForCaption and draws via DrawAt.
    // The cue never changes the box: CueRect only reads the plate, and the plate is measured before it.
    // Shown only on tap-to-continue captions: CoachTap.AnyTap steps (any tap anywhere advances).
    // Required steps (glove on a target) and the Daily claim are excluded.
    public static class TapCue
    {
        // Sizes are fractions of the caption font px.
        public const float WidthEm = 0.30f;     // fingertip width
        public const float HeightEm = 0.50f;    // fingertip height (tip + short finger stub)
        public const float OutlineEm = 0.07f;   // dark outline, slightly thicker than the D sketch
        public const int OutlineMinPx = 3;
        // Tip anchor: inside the right edge, hanging past the bottom edge (part in, part out).
        public const float InsetRightEm = 0.45f;
        public const float HangBelowEm = 0.18f;
        // Tap: a short press every Period seconds. Dips DipEm, a faint touch mark shows while down.
        public const float Period = 1.4f;
        public const float PressFrac = 0.22f;
        public const float DipEm = 0.08f;
        public const float MarkAlpha = 0.28f;
        public const float MarkEm = 0.22f;

        // Caption plates do not draw the fingertip. Any tap still continues.
        public static bool ForCaption(string text)
        {
            // The fingertip's dark outline vanishes into a caption plate and the
            // white core (250,250,252) reads as a tick at the bottom-right.
            // Lightning, bees, and the rule tips all did this. Any tap still continues.
            return false;
        }

        public static int OutlinePx(int px) => Mathf.Max(OutlineMinPx, Mathf.RoundToInt(px * OutlineEm));

        // 0..1 press depth at time t.
        public static float Press(float t)
        {
            float u = Mathf.Repeat(t, Period) / Period;
            if (u >= PressFrac) return 0f;
            return Mathf.Sin(u / PressFrac * Mathf.PI);
        }

        // GUI rect (y down) of the fingertip for a plate. Reads the plate only.
        public static Rect CueRect(Rect plate, int px, float t)
        {
            float w = Mathf.Max(6f, px * WidthEm), h = Mathf.Max(10f, px * HeightEm);
            float tipX = plate.xMax - px * InsetRightEm;
            float tipY = plate.yMax + px * HangBelowEm + Press(t) * px * DipEm;
            return new Rect(tipX - w * 0.5f, tipY - h, w, h);
        }

        public static Rect MarkRect(Rect cue, int px)
        {
            float m = px * MarkEm;
            return new Rect(cue.center.x - m * 0.5f, cue.yMax - m * 0.25f, m, m * 0.4f);
        }

        static Texture2D _tex, _dot;
        static int _texW, _texH, _texO;

        // Fingertip: rounded tip at the bottom, straight stub up, white fill, dark outline.
        public static Texture2D Tex(int w, int h, int outline)
        {
            if (_tex != null && _texW == w && _texH == h && _texO == outline) return _tex;
            if (_tex != null) Object.DestroyImmediate(_tex);
            _tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave, name = "TapCueTip" };
            var px = new Color32[w * h];
            float r = w * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // texture y=0 is the bottom (the tip)
                    float cx = x + 0.5f - r, cy = y + 0.5f;
                    float d = cy < r ? Mathf.Sqrt(cx * cx + (cy - r) * (cy - r)) : Mathf.Abs(cx);
                    float edge = r - d;
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (edge > outline) c = new Color32(250, 250, 252, 255);
                    else if (edge > 0f) c = new Color32(28, 28, 36, 255);
                    px[y * w + x] = c;
                }
            _tex.SetPixels32(px);
            _tex.Apply(false, false);
            _texW = w; _texH = h; _texO = outline;
            return _tex;
        }

        static Texture2D Dot()
        {
            if (_dot != null) return _dot;
            const int n = 32;
            _dot = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, name = "TapCueMark" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            _dot.SetPixels32(px); _dot.Apply(false, false);
            return _dot;
        }

        public static void DrawAt(Rect plate, int px, float fade, float t)
        {
            if (fade < 0.04f || px < 1) return;
            var r = CueRect(plate, px, t);
            var prev = GUI.color;
            float press = Press(t);
            if (press > 0.01f)
            {
                GUI.color = new Color(1f, 1f, 1f, MarkAlpha * press * fade);
                GUI.DrawTexture(MarkRect(r, px), Dot());
            }
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.DrawTexture(r, Tex(Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height), OutlinePx(px)));
            GUI.color = prev;
        }
    }

    // Brandon: tap-wait captions (any tap, or the plate's own timer) get the
    // pointing glove, small, looping, under the words and clear of the step glove.
    // The white tick stays off. This cue does not change when a tap advances.
    public static class TapWaitGlove
    {
        public const float LoopSeconds = 1.6f;

        public static bool Shows(string shown)
        {
            if (string.IsNullOrEmpty(shown)) return false;
            foreach (CoachStep step in System.Enum.GetValues(typeof(CoachStep)))
            {
                if (!CoachTap.AnyTap(step)) continue;
                string cap = CoachTap.Caption(step);
                if (string.IsNullOrEmpty(cap)) continue;
                if (shown == cap || shown == cap.Replace("\n", " ")) return true;
                if (shown == CaptionLayout.HeadlineGap(cap)) return true;
            }
            return false;
        }

        // The keep-out for the whole loop. The drawn frame bobs inside it.
        public static Rect CueSlot(Rect text, Rect plate, Rect stepGlove, float screenW, float screenH)
        {
            if (text.width < 8f || text.height < 8f || screenW < 8f || screenH < 8f) return default;
            float s = DeviceFit.Ui(screenW, screenH);
            float dh = Mathf.Clamp(20f * s, 16f, 34f);
            float dw = dh * (220f / 300f);
            float bob = 4f;
            float gap = Mathf.Max(6f, 4f * s);
            var seat = new Rect(text.xMax - dw * 0.2f, text.yMax + gap, dw, dh + bob * 2f);
            seat = Fit(seat, text, stepGlove, screenW, screenH, dw, gap);
            if (seat.width < 2f) return default;
            if (seat.Overlaps(text)) return default;
            if (stepGlove.width > 2f && seat.Overlaps(stepGlove)) return default;
            return seat;
        }

        static Rect Fit(Rect seat, Rect text, Rect stepGlove, float screenW, float screenH, float dw, float gap)
        {
            if (Blocked(seat, text, stepGlove))
                seat.x = text.xMin - dw - gap;
            if (Blocked(seat, text, stepGlove))
                seat = new Rect(text.xMin, text.yMin - seat.height - gap, seat.width, seat.height);
            if (seat.x < 4f) seat.x = 4f;
            if (seat.y < 4f) seat.y = 4f;
            if (seat.xMax > screenW - 4f) seat.x = screenW - 4f - seat.width;
            if (seat.yMax > screenH - 4f) seat.y = screenH - 4f - seat.height;
            if (Blocked(seat, text, stepGlove)) return default;
            return seat;
        }

        static bool Blocked(Rect seat, Rect text, Rect stepGlove)
        {
            if (seat.Overlaps(text)) return true;
            return stepGlove.width > 2f && seat.Overlaps(stepGlove);
        }

        // One gentle bob inside the slot. The fingertip never leaves the keep-out.
        public static Rect Frame(Rect slot, float time)
        {
            if (slot.width < 2f) return default;
            float bob = 3f;
            float u = Mathf.Sin(time / LoopSeconds * Mathf.PI * 2f);
            float y = slot.y + bob + u * bob;
            return new Rect(slot.x, y, slot.width, Mathf.Max(8f, slot.height - bob * 2f));
        }

        public static float Dip(float time)
        {
            float u = Mathf.Repeat(time, LoopSeconds) / LoopSeconds;
            return 0.12f + 0.2f * Mathf.Sin(u * Mathf.PI);
        }
    }
}

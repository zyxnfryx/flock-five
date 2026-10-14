using UnityEngine;

namespace FlockFive
{
    // Shared push-button motion. Level, Claim, Go VIP, and every popup face use it.
    // Idle scale stays 1 (the old Go VIP sine was a quiver). A press sinks and
    // squashes; release springs past rest and settles. The hit rect is the layout
    // rect the whole time. The label rides the snapped visual top by a fixed pixel
    // offset, the same snap as the fill.
    public static class PushButton
    {
        public const int Level = 0;
        public const int Claim = 1;
        public const int Vip = 2;
        public const float ReleaseSeconds = 0.32f;
        public const float SheenPeriod = 1.40f; // b86: sweep about 2x faster
        public const float SinkFrac = 0.055f;

        public struct Pose
        {
            public float Sink;
            public float SquashX;
            public float SquashY;
        }

        public struct Motion
        {
            public Pose Pose;
            public bool PressedEdge;
            public bool ReleasedEdge;
        }

        struct Track
        {
            public bool Held;
            public bool Seen;
            public float ReleasedAt;
            public int PressFrame;
            public int ReleaseFrame;
            public int ThunkFrame;
            public bool Releasing;
        }

        static Track[] _tracks = new Track[24];

        // Shot hook. When set, At() returns this pose and idle scale stays 1.
        public static bool Force;
        public static Pose Forced;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _tracks = new Track[24];
            Force = false;
            Forced = default;
            _band = null;
            _bandPx = null;
            _faceBand = null;
            _facePx = null;
            _faceMask = null;
        }

        public static float IdleScale(float time) => 1f;

        public static float SheenU(float time)
        {
            float period = SheenPeriod;
            if (period < 0.05f) period = 2.75f;
            return Mathf.Repeat(time < 0f ? 0f : time / period, 1f);
        }

        public static Pose Rest() => new Pose { Sink = 0f, SquashX = 1f, SquashY = 1f };

        // held sinks. releaseAge < 0 or past the spring is rest. Mid-spring overshoots up.
        public static Pose At(bool held, float releaseAge)
        {
            if (Force) return Forced.SquashX < 0.05f ? Rest() : Forced;
            if (held)
                return new Pose { Sink = SinkFrac, SquashX = 1.06f, SquashY = 0.88f };
            if (releaseAge < 0f || releaseAge >= ReleaseSeconds) return Rest();
            float u = releaseAge / ReleaseSeconds;
            const float peak = 0.42f;
            if (u < peak)
            {
                float e = Smooth(u / peak);
                return new Pose
                {
                    Sink = Mathf.Lerp(SinkFrac, -0.020f, e),
                    SquashX = Mathf.Lerp(1.06f, 0.96f, e),
                    SquashY = Mathf.Lerp(0.88f, 1.07f, e)
                };
            }
            float f = Smooth((u - peak) / (1f - peak));
            return new Pose
            {
                Sink = Mathf.Lerp(-0.020f, 0f, f),
                SquashX = Mathf.Lerp(0.96f, 1f, f),
                SquashY = Mathf.Lerp(1.07f, 1f, f)
            };
        }

        public static Rect HitRect(Rect layout) => layout;

        public static Rect Visual(Rect layout, Pose pose)
        {
            float sx = pose.SquashX < 0.05f ? 1f : pose.SquashX;
            float sy = pose.SquashY < 0.05f ? 1f : pose.SquashY;
            float w = layout.width * sx;
            float h = layout.height * sy;
            float x = layout.center.x - w * 0.5f;
            float y = layout.y + layout.height * pose.Sink + (layout.height - h) * 0.5f;
            return new Rect(x, y, w, h);
        }

        // Raised disc on the clay flower. Square LEVEL / Watch / Claim faces use it.
        public static Rect FlowerCap(Rect face)
        {
            return new Rect(
                face.x + face.width * 0.205f,
                face.y + face.height * 0.10f,
                face.width * 0.59f,
                face.height * 0.38f);
        }

        // The label keeps its center fraction on the cap. A squash widens the
        // plate around its center, so a top-left delta walks the word off the disc.
        // Width and height stay the rest size so the glyphs do not reflow.
        public static Rect Follow(Rect restLabel, Rect rest, Rect visual, float pixelsPerPoint)
        {
            float nx = rest.width > 1f ? (restLabel.center.x - rest.x) / rest.width : 0.5f;
            float ny = rest.height > 1f ? (restLabel.center.y - rest.y) / rest.height : 0.5f;
            float cx = visual.x + visual.width * nx;
            float cy = visual.y + visual.height * ny;
            return new Rect(
                LabelRim.Snap(cx - restLabel.width * 0.5f, pixelsPerPoint),
                LabelRim.Snap(cy - restLabel.height * 0.5f, pixelsPerPoint),
                restLabel.width,
                restLabel.height);
        }

        // A tutorial glove is a cue. Only a real pointer squashes the cap.
        public static bool Finger(bool pointerHeld, bool glovePosing)
        {
            if (glovePosing && !pointerHeld) return false;
            return pointerHeld;
        }

        public static int Slot(Rect r)
        {
            int h = (Mathf.RoundToInt(r.x) * 73856093) ^ (Mathf.RoundToInt(r.y) * 19349663)
                ^ (Mathf.RoundToInt(r.width) * 83492791);
            if (h < 0) h = -h;
            return 3 + (h % 20);
        }

        // effects plays the wooden thunk and the light haptic once on the press edge.
        public static Motion Sample(int slot, bool held, float now, bool effects)
        {
            if (slot < 0 || slot >= _tracks.Length) slot = 0;
            var tr = _tracks[slot];
            int frame = Time.frameCount;
            if (!tr.Seen || tr.Held != held)
            {
                if (held && tr.Seen && !tr.Held) tr.PressFrame = frame;
                if (!held && tr.Seen && tr.Held)
                {
                    tr.ReleaseFrame = frame;
                    tr.ReleasedAt = now;
                    tr.Releasing = true;
                }
                tr.Held = held;
                tr.Seen = true;
            }
            var motion = new Motion
            {
                PressedEdge = tr.PressFrame == frame,
                ReleasedEdge = tr.ReleaseFrame == frame
            };
            if (effects && motion.PressedEdge && tr.ThunkFrame != frame)
            {
                tr.ThunkFrame = frame;
                Sfx.ButtonThunk();
                Haptics.Play(Haptics.Tier.Light);
            }
            float age = -1f;
            if (!held && tr.Releasing) age = now - tr.ReleasedAt;
            motion.Pose = At(held, age);
            _tracks[slot] = tr;
            return motion;
        }

        static Texture2D _band;
        static Color32[] _bandPx;

        // Gloss band clipped to the button. u is 0..1 across one sweep. flash is the release pop.
        // Baked into one bilinear texture so the sweep is smooth, and zeroed outside an ellipse
        // on the face rim so it cannot walk onto the card the way a rotated GUI quad does.
        public static void DrawSheen(Rect r, float u, float alpha, bool flash)
        {
            if (alpha < 0.02f || r.width < 8f || r.height < 8f) return;
            var tex = SheenTex(u, alpha, flash);
            if (tex == null) return;
            var prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        // b89 (Brandon): the sheen hugs the disc rim. It used to live in a 0.47 inset ellipse
        // with a soft (1 - e^2) * 5 falloff, so its edge read about 5% of the face width inside
        // the rim, and the sweep started at -0.20, well left of the face, so the first visible
        // light showed up already inside it. Now the clip sits on the rim with a thin antialias
        // and the band centre starts on the rim's leftmost point.
        public const float OvalRim = 0.495f;      // clip radius, fraction of the face (was 0.47)
        public const float OvalFeather = 0.035f;  // rim antialias, fraction of the radius
        public const float OvalSlant = 0.48f;     // along = t + (v - 0.5) * slant: bottom leads
        public const float OvalBand = 0.12f;
        public const float OvalFlashBand = 0.22f;

        // Leftmost and rightmost along-values on the clip ellipse (the slant tilts them).
        public static float OvalStart => 0.5f - OvalRim * Mathf.Sqrt(1f + OvalSlant * OvalSlant);
        public static float OvalEnd => 0.5f + OvalRim * Mathf.Sqrt(1f + OvalSlant * OvalSlant);

        // Band centre at sweep position u. u = 0 is on the left rim, u = 1 on the right rim.
        public static float OvalTravel(float u) => Mathf.Lerp(OvalStart, OvalEnd, Mathf.Clamp01(u));

        // Rim clip 0..1 for a point in face units (t, v in 0..1).
        public static float OvalClip(float t, float v)
        {
            float ex = (t - 0.5f) / OvalRim, ey = (v - 0.5f) / OvalRim;
            float e = Mathf.Sqrt(ex * ex + ey * ey);
            return Mathf.Clamp01((1f - e) / OvalFeather);
        }

        // Gap from the face edge to half coverage, as a fraction of the face width.
        public static float OvalEdgeGap => 0.5f - OvalRim * (1f - OvalFeather * 0.5f);

        // Sheen coverage 0..1 at a point in face units, before the alpha envelope.
        public static float OvalSheenAt(float t, float v, float u, bool flash)
        {
            float clip = OvalClip(t, v);
            if (clip <= 0f) return 0f;
            float along = t + (v - 0.5f) * OvalSlant;
            float d = (along - OvalTravel(u)) / (flash ? OvalFlashBand : OvalBand);
            return Mathf.Exp(-d * d * 3.4f) * clip;
        }

        // Idle alpha across one sweep. A half-sine was ~0.2 at the first visible frame, so the
        // trace seemed to start somewhere inside the face. Ramp in and out on the rims instead.
        public const float OvalRamp = 0.06f;
        public static float OvalFade(float u)
        {
            u = Mathf.Clamp01(u);
            float k = Mathf.Clamp01(Mathf.Min(u, 1f - u) / OvalRamp);
            return k * k * (3f - 2f * k);
        }

        // ---- b89 pass 2: the sheen takes the real top-face silhouette of the flower pedestal ----
        // The pedestal is a sprite (fx_play_flower, 1024x811). Its gold top face is the oval on top;
        // the thickness band and the petals around it are NOT face. The oval below is traced from
        // the sprite itself: an ellipse fit, then the per-angle edge radius found by the strongest
        // luminance edge along each ray (64 rays). FaceRadii is that radius as a ratio of the fit.
        public const float FaceRefW = 1024f, FaceRefH = 811f;
        const float FaceCx = 515f, FaceCy = 203f, FaceRx = 307f, FaceRy = 178f, FaceRotDeg = -3f;
        public const float FaceInset = 0.99f;      // keep the sheen a hair inside the traced edge
        const float FaceFeather = 0.012f;          // edge antialias, ratio of the fit radius
        const float FaceX0 = 0.20f, FaceX1 = 0.81f, FaceY0 = 0.035f, FaceY1 = 0.47f; // face bbox, sprite UV
        static readonly float[] FaceRadii = { 1.06f, 1.05f, 1.05f, 1.04f, 1.03f, 1.03f, 1.03f, 1.03f, 1.03f, 1.02f, 1.02f, 1.02f, 1.01f, 1.01f, 1.00f, 1.00f, 0.99f, 0.98f, 0.98f, 0.97f, 0.97f, 0.97f, 0.97f, 0.97f, 0.97f, 0.97f, 0.97f, 0.97f, 0.98f, 0.98f, 0.99f, 0.99f, 1.00f, 1.00f, 1.00f, 1.01f, 1.01f, 1.01f, 1.02f, 1.02f, 1.02f, 1.02f, 1.01f, 1.01f, 1.00f, 0.99f, 0.99f, 0.98f, 0.97f, 0.97f, 0.96f, 0.96f, 0.96f, 0.96f, 0.97f, 0.98f, 0.99f, 1.00f, 1.02f, 1.03f, 1.05f, 1.06f, 1.07f, 1.06f };

        public static float FaceRadiusAt(float ang)
        {
            int n = FaceRadii.Length;
            float k = Mathf.Repeat(ang, Mathf.PI * 2f) / (Mathf.PI * 2f) * n;
            int i0 = Mathf.FloorToInt(k) % n, i1 = (i0 + 1) % n;
            return Mathf.Lerp(FaceRadii[i0], FaceRadii[i1], k - Mathf.Floor(k));
        }

        // Face coverage 0..1 at sprite UV (su, sv from the top-left). inset scales the traced edge
        // (1 = the edge itself); feather 0 gives a hard reference mask.
        public static float FaceCover(float su, float sv, float inset, float feather = FaceFeather)
        {
            float x = su * FaceRefW - FaceCx, y = sv * FaceRefH - FaceCy;
            float r = FaceRotDeg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            float ex = (x * c + y * s) / FaceRx, ey = (-x * s + y * c) / FaceRy;
            float e = Mathf.Sqrt(ex * ex + ey * ey);
            float lim = FaceRadiusAt(Mathf.Atan2(ey, ex)) * inset;
            if (feather <= 0f) return e <= lim ? 1f : 0f;
            return Mathf.Clamp01((lim - e) / feather);
        }

        static float FaceAlong(float su, float sv)
        {
            float t = (su - FaceX0) / (FaceX1 - FaceX0), v = (sv - FaceY0) / (FaceY1 - FaceY0);
            return t + (v - 0.5f) * OvalSlant;
        }

        static float _faceMin = float.NaN, _faceMax;
        static void FaceAlongRange()
        {
            if (!float.IsNaN(_faceMin)) return;
            float lo = 9f, hi = -9f;
            for (int y = 0; y < 160; y++)
                for (int x = 0; x < 200; x++)
                {
                    float su = (x + 0.5f) / 200f, sv = (y + 0.5f) / 160f * (FaceRefH / FaceRefH);
                    if (FaceCover(su, sv, FaceInset) < 0.5f) continue;
                    float a = FaceAlong(su, sv);
                    if (a < lo) lo = a;
                    if (a > hi) hi = a;
                }
            _faceMin = lo; _faceMax = hi;
        }

        // Band centre at sweep u: u = 0 on the face's left rim, u = 1 on its right rim.
        public static float FaceTravel(float u)
        {
            FaceAlongRange();
            return Mathf.Lerp(_faceMin, _faceMax, Mathf.Clamp01(u));
        }

        // Sheen coverage at sprite UV, before the alpha envelope.
        public static float FaceSheenAt(float su, float sv, float u)
        {
            float clip = FaceCover(su, sv, FaceInset);
            if (clip <= 0f) return 0f;
            float d = (FaceAlong(su, sv) - FaceTravel(u)) / OvalBand;
            return Mathf.Exp(-d * d * 3.4f) * clip;
        }

        // Where a sprite of texW x texH lands when drawn ScaleToFit into r.
        public static Rect FitRect(Rect r, float texW, float texH)
        {
            if (texW < 1f || texH < 1f) return r;
            float k = Mathf.Min(r.width / texW, r.height / texH);
            float w = texW * k, h = texH * k;
            return new Rect(r.x + (r.width - w) * 0.5f, r.y + (r.height - h) * 0.5f, w, h);
        }

        static Texture2D _faceBand;
        static Color32[] _facePx;
        static float[] _faceMask;
        static int _faceW, _faceH;

        static int FaceTexW(float fitW) => Mathf.Clamp(Mathf.RoundToInt(fitW * 0.75f), 96, 640);

        // Bake the sheen over the fitted sprite rect. The mask is cached per size.
        public static Texture2D FaceSheenTex(float fitW, float fitH, float u, float alpha)
        {
            int tw = FaceTexW(fitW);
            int th = Mathf.Max(8, Mathf.RoundToInt(tw * fitH / fitW));
            if (_faceBand == null || _faceW != tw || _faceH != th)
            {
                if (_faceBand != null) Object.DestroyImmediate(_faceBand);
                _faceBand = new Texture2D(tw, th, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                    name = "PushButtonFaceSheen"
                };
                _facePx = new Color32[tw * th];
                _faceMask = new float[tw * th];
                _faceW = tw; _faceH = th;
                for (int y = 0; y < th; y++)
                    for (int x = 0; x < tw; x++)
                        _faceMask[y * tw + x] = FaceCover((x + 0.5f) / tw, (th - 1 - y + 0.5f) / th, FaceInset);
                // Bilinear reads blend a covered texel toward its neighbour, so a texel beside an
                // uncovered one would bleed past the edge. Erode one texel: every nonzero texel is
                // interior, and the blend can only reach another interior texel centre.
                var raw = (float[])_faceMask.Clone();
                for (int y = 0; y < th; y++)
                    for (int x = 0; x < tw; x++)
                    {
                        int i = y * tw + x;
                        bool edge = x == 0 || y == 0 || x == tw - 1 || y == th - 1
                            || raw[i - 1] <= 0f || raw[i + 1] <= 0f || raw[i - tw] <= 0f || raw[i + tw] <= 0f
                            || raw[i - tw - 1] <= 0f || raw[i - tw + 1] <= 0f || raw[i + tw - 1] <= 0f || raw[i + tw + 1] <= 0f;
                        if (edge) _faceMask[i] = 0f;
                    }
            }
            float aScale = alpha * OvalFade(u);
            float travel = FaceTravel(u);
            for (int y = 0; y < th; y++)
            {
                float sv = (th - 1 - y + 0.5f) / th; // texture rows run bottom-up
                for (int x = 0; x < tw; x++)
                {
                    int i = y * tw + x;
                    float m = _faceMask[i];
                    if (m <= 0f) { _facePx[i] = new Color32(0, 0, 0, 0); continue; }
                    float su = (x + 0.5f) / tw;
                    float d = (FaceAlong(su, sv) - travel) / OvalBand;
                    byte a = (byte)Mathf.Clamp(Mathf.Exp(-d * d * 3.4f) * m * aScale * 230f, 0f, 255f);
                    _facePx[i] = new Color32(255, 250, 228, a);
                }
            }
            _faceBand.SetPixels32(_facePx);
            _faceBand.Apply(false);
            return _faceBand;
        }

        // Idle sheen for the flower pedestal: clipped to the sprite's gold top face.
        public static void DrawFaceSheen(Rect drawn, Texture tex, float u, float alpha)
        {
            if (alpha < 0.02f || tex == null || drawn.width < 8f) return;
            var fit = FitRect(drawn, tex.width, tex.height);
            var baked = FaceSheenTex(fit.width, fit.height, u, alpha);
            var prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(fit, baked, ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        // Sheen texels that land outside the hard traced face, sampled at screen pixels of a fit
        // rect (bilinear, the way the GPU reads it). Zero means no spill.
        public static int FaceSpillPixels(float fitW, float fitH, float u)
        {
            var tex = FaceSheenTex(fitW, fitH, u, 1f);
            int w = Mathf.RoundToInt(fitW), h = Mathf.RoundToInt(fitH), bad = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float su = (x + 0.5f) / w, sv = (y + 0.5f) / h;
                    if (FaceCover(su, sv, 1f, 0f) > 0f) continue;
                    if (tex.GetPixelBilinear(su, 1f - sv).a > 0.004f) bad++;
                }
            return bad;
        }

        static Texture2D SheenTex(float u, float alpha, bool flash)
        {
            const int n = 128;
            if (_band == null)
            {
                _band = new Texture2D(n, n, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                    name = "PushButtonSheen"
                };
                _bandPx = new Color32[n * n];
            }
            float aScale = flash ? alpha : alpha * OvalFade(u);
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float c = OvalSheenAt((x + 0.5f) / n, v, u, flash);
                    byte a = (byte)Mathf.Clamp(c * aScale * 230f, 0f, 255f);
                    _bandPx[y * n + x] = new Color32(255, 250, 228, a);
                }
            }
            _band.SetPixels32(_bandPx);
            _band.Apply(false);
            return _band;
        }

        static float Smooth(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Home-screen flyby. Once per calendar day, after 10 quiet seconds.
        // The rail and the title paint after this, so the plane stays behind them.
        const string UsaPlaneDayKey = "flockfive.plane.day";
        const float UsaPlaneWait = 10f;
        const float UsaPlaneDur = 6.5f;

        static bool _usaForce;
        static string _usaToday;
        static int _usaTodayStamp = -1;
        static int _usaDoneStamp = -2;
        float _usaIdle;
        float _usaU = -1f;
        float _fwClock = -1f;
        float _fwDuck;
        int _fwPop;

        public static void ForceUsaPlane() => _usaForce = true;

        static string UsaToday()
        {
            var n = System.DateTime.Now;
            int stamp = n.Year * 400 + n.DayOfYear;
            if (stamp != _usaTodayStamp)
            {
                _usaTodayStamp = stamp;
                _usaToday = n.Year.ToString("0000") + "-" + n.Month.ToString("00") + "-" + n.Day.ToString("00");
            }
            return _usaToday;
        }

        bool UsaFlownToday()
        {
            UsaToday();
            if (_usaDoneStamp == _usaTodayStamp) return true;
            if (PlayerPrefs.GetString(UsaPlaneDayKey, "") != UsaToday()) return false;
            _usaDoneStamp = _usaTodayStamp;
            return true;
        }

        bool UsaPlaneBlocked()
        {
            if (!_splash || _home != HomeFace.Splash) return true;
            if (HomeLessonUp() || _adoptLive) return true;
            if (VipOffer.IsOpen || _dailyOpen || _dailyAskOpen || _welcomeOpen) return true;
            if (_streakSlide >= 0f || RewardPayBusy()) return true;
            if (_restartAsk != RestartAsk.None) return true;
            if (!string.IsNullOrEmpty(_cueLine)) return true;
            if (Ads.IsShowing || Ads.IsBusy) return true;
            return false;
        }

        static bool PointerHeld()
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.isPressed;
        }

        void TickUsaPlane(float dt)
        {
            if (dt > 0.1f) dt = 0.1f;
            if (dt < 0f) dt = 0f;
            if (PointerHeld()) _usaIdle = 0f;
            if (_usaU >= 0f)
            {
                if (UsaPlaneBlocked())
                {
                    EndUsaPlane();
                    return;
                }
                _usaU += dt / UsaPlaneDur;
                _fwClock += dt;
                _fwDuck -= dt * 1.7f;
                if (_fwDuck < 0f) _fwDuck = 0f;
                TickSkyBooms();
                float u = _usaU < 0f ? 0f : (_usaU > 1f ? 1f : _usaU);
                float env = Mathf.Sin(Mathf.PI * u);
                float duck = 1f - 0.48f * _fwDuck;
                Sfx.PlanePass(Mathf.Lerp(1f, -1f, u), env * duck);
                if (_usaU >= 1f) EndUsaPlane();
                return;
            }
            if (UsaPlaneBlocked())
            {
                _usaIdle = 0f;
                return;
            }
            if (_usaForce)
            {
                _usaForce = false;
                BeginUsaPlane();
                return;
            }
            _usaIdle += dt;
            if (_usaIdle < UsaPlaneWait) return;
            _usaIdle = 0f;
            if (UsaFlownToday()) return;
            BeginUsaPlane();
        }

        void BeginUsaPlane()
        {
            _usaU = 0f;
            _usaIdle = 0f;
            _fwClock = 0f;
            _fwDuck = 0f;
            _fwPop = 0;
            string day = UsaToday();
            PlayerPrefs.SetString(UsaPlaneDayKey, day);
            _usaDoneStamp = _usaTodayStamp;
            PlayerPrefs.Save();
            Sfx.PlaneBegin();
        }

        void EndUsaPlane()
        {
            _usaU = -1f;
            _fwClock = -1f;
            _fwDuck = 0f;
            _fwPop = 0;
            Sfx.PlaneStop();
        }

        // Pops are 0.85s then 0.90s apart. The rise is shared, so the booms match.
        static float FwWhen(int i)
        {
            if (i <= 0) return 0.95f;
            if (i == 1) return 1.80f;
            return 2.70f;
        }

        void TickSkyBooms()
        {
            for (int i = 0; i < 3; i++)
            {
                int bit = 1 << i;
                if ((_fwPop & bit) != 0) continue;
                if (_fwClock < FwWhen(i) + FwRise) continue;
                _fwPop |= bit;
                _fwDuck = 1f;
                Sfx.SkyBoom(i);
            }
        }

        // Behind the title and the rails, in front of the sky wash. No hit target.
        // Fireworks paint first so the plane and the banner cross in front of them.
        void DrawUsaPlane(float s)
        {
            if (_usaU < 0f) return;
            DrawSkyFireworks(s);
            var plane = SpriteCatalog.UsaPlane;
            var banner = SpriteCatalog.UsaBanner;
            if (plane == null && banner == null) return;
            float u = _usaU < 0f ? 0f : (_usaU > 1f ? 1f : _usaU);
            float bob = Mathf.Sin(u * Mathf.PI * 3f) * 7f * s;
            float planeH = 132f * s;
            if (planeH < 96f) planeH = 96f;
            if (planeH > 210f) planeH = 210f;
            float pAspect = SpriteAspect(plane, 1.7f);
            float planeW = planeH * pAspect;
            float banH = planeH * 0.58f;
            float banW = banH * SpriteAspect(banner, 3.4f);
            float gap = 8f * s;
            float total = planeW + gap + banW;
            float left = Mathf.Lerp(Screen.width + 12f, -total - 12f, u);
            float y = TopHud() + 6f * s + bob;
            if (plane != null)
                DrawSpriteTex(plane, new Rect(left, y + planeH * 0.06f, planeW, planeH));
            float banX = left + planeW + gap;
            float banY = y + planeH * 0.28f;
            float time = Time.unscaledTime;
            float lead = BannerDy(0.04f, time, banH);
            DrawPlaneRope(new Vector2(left + planeW * 0.92f, y + planeH * 0.48f), new Vector2(banX, banY + banH * 0.45f + lead), s);
            if (banner != null)
                DrawBannerRipple(banner, new Rect(banX, banY, banW, banH), time);
        }

        static float SpriteAspect(Sprite spr, float fallback)
        {
            if (spr == null || spr.rect.height < 2f) return fallback;
            return spr.rect.width / spr.rect.height;
        }

        static void DrawSpriteTex(Sprite spr, Rect r)
        {
            if (spr == null || spr.texture == null || r.width < 2f) return;
            var tr = spr.textureRect;
            var tex = spr.texture;
            float tw = tex.width;
            float th = tex.height;
            if (tw < 2f || th < 2f) return;
            var uv = new Rect(tr.x / tw, tr.y / th, tr.width / tw, tr.height / th);
            GUI.DrawTextureWithTexCoords(r, tex, uv, true);
        }

        static void DrawPlaneRope(Vector2 a, Vector2 b, float s)
        {
            const int Segs = 6;
            float thick = Mathf.Max(2f, 2.4f * s);
            GUI.color = new Color(0.55f, 0.42f, 0.24f, 0.9f);
            float time = Time.unscaledTime;
            for (int i = 0; i < Segs; i++)
            {
                float u0 = i / (float)Segs;
                float u1 = (i + 1) / (float)Segs;
                float sag0 = Mathf.Sin(u0 * Mathf.PI) * 5f * s + Mathf.Sin(time * 2.2f + i) * 1.5f * s;
                float sag1 = Mathf.Sin(u1 * Mathf.PI) * 5f * s + Mathf.Sin(time * 2.2f + i + 1f) * 1.5f * s;
                var p0 = Vector2.Lerp(a, b, u0) + new Vector2(0f, sag0);
                var p1 = Vector2.Lerp(a, b, u1) + new Vector2(0f, sag1);
                var mid = (p0 + p1) * 0.5f;
                float len = Vector2.Distance(p0, p1) + 1f;
                GUI.DrawTexture(new Rect(mid.x - len * 0.5f, mid.y - thick * 0.5f, len, thick), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        // Travelling sheet. along 0 is the leading edge on the plane, 1 is the tail.
        // The tail's second sine is faster. Amplitude grows with along so the words stay put.
        const int BannerSlices = 22;

        static float BannerCrest(float along, float time)
        {
            if (along < 0f) along = 0f;
            if (along > 1f) along = 1f;
            float env = along * along;
            // One phase for every strip, so neighbours stay sewn together.
            // The fast sine is quiet at the lead and owns the tail.
            float travel = Mathf.Sin(time * 2.5f - along * 4.4f);
            float billow = Mathf.Sin(time * 5.6f - along * 5.5f);
            float tip = Mathf.Sin(time * 16.5f - along * 2.2f);
            float wave = travel * 0.85f + billow * Mathf.Lerp(0.30f, 0.9f, along) + tip * env * env * 0.95f;
            float n = wave * 0.50f;
            if (n > 1f) n = 1f;
            if (n < -1f) n = -1f;
            return n;
        }

        static float BannerDy(float along, float time, float height)
        {
            float env = along * along;
            float amp = height * (0.06f + 0.22f * env);
            return -BannerCrest(along, time) * amp;
        }

        static void DrawBannerRipple(Sprite spr, Rect r, float time)
        {
            if (spr == null || spr.texture == null) return;
            var tr = spr.textureRect;
            var tex = spr.texture;
            float tw = tex.width;
            float th = tex.height;
            if (tw < 2f || th < 2f || r.width < 8f) return;
            float baseU = tr.x / tw;
            float baseV = tr.y / th;
            float uvW = tr.width / tw;
            float uvH = tr.height / th;
            float sliceW = r.width / BannerSlices;
            float prevDy = BannerDy(0f, time, r.height);
            var prevM = GUI.matrix;
            for (int i = 0; i < BannerSlices; i++)
            {
                float a = i / (float)BannerSlices;
                float b = (i + 1) / (float)BannerSlices;
                float dy = BannerDy(b, time, r.height);
                float mid = (a + b) * 0.5f;
                float crest = BannerCrest(mid, time);
                float k = crest * 0.5f + 0.5f;
                float shade = 0.82f + 0.18f * k;
                GUI.color = new Color(shade, shade * (0.96f + 0.04f * k), shade * (0.92f + 0.08f * k), 1f);
                float midDy = (prevDy + dy) * 0.5f;
                var slice = new Rect(r.x + r.width * a, r.y + midDy, sliceW + 2f, r.height);
                float ang = Mathf.Atan2(dy - prevDy, sliceW) * Mathf.Rad2Deg;
                if (ang > 12f) ang = 12f;
                if (ang < -12f) ang = -12f;
                if (ang > 0.35f || ang < -0.35f)
                    GUIUtility.RotateAroundPivot(ang, slice.center);
                var uv = new Rect(baseU + uvW * a, baseV, uvW * (b - a), uvH);
                GUI.DrawTextureWithTexCoords(slice, tex, uv, true);
                GUI.matrix = prevM;
                prevDy = dy;
            }
            GUI.color = Color.white;
        }

        // Sky shells while the plane crosses. Drawn before the plane.
        // 0 white round, 1 blue round, 2 red heart (holds, then droops).
        const float FwRise = 0.50f;
        const int HeartN = 48;
        const int RoundN = 24;
        const float HeartForm = 0.18f;
        const float HeartHold = 0.60f;
        const float HeartFade = 0.85f;
        const float RoundLife = 1.12f;
        static float[] _heartX;
        static float[] _heartY;
        static float[] _roundAng;
        static float[] _roundSpd;
        static float[] _roundShell;

        static void EnsureFwTables()
        {
            if (_heartX == null)
            {
                _heartX = new float[HeartN];
                _heartY = new float[HeartN];
                for (int i = 0; i < HeartN; i++)
                {
                    float t = (i + 0.5f) * (Mathf.PI * 2f / HeartN);
                    float sn = Mathf.Sin(t);
                    _heartX[i] = 16f * sn * sn * sn;
                    float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                    _heartY[i] = -y;
                }
            }
            if (_roundAng != null) return;
            _roundAng = new float[RoundN];
            _roundSpd = new float[RoundN];
            _roundShell = new float[RoundN];
            for (int i = 0; i < RoundN; i++)
            {
                _roundAng[i] = (i + 0.5f) / RoundN * Mathf.PI * 2f + ((i * 37 % 11) - 5) * 0.04f;
                _roundSpd[i] = 0.58f + (i * 17 % 10) * 0.046f;
                _roundShell[i] = (i & 1) == 0 ? 1f : 0.64f;
            }
        }

        void DrawSkyFireworks(float s)
        {
            if (_fwClock < 0f) return;
            float w = Screen.width;
            float h = Screen.height;
            if (w < 8f || h < 8f) return;
            EnsureFwTables();
            float unit = Mathf.Min(w, h) * 0.0112f;
            if (unit < 5.5f) unit = 5.5f;
            if (unit > 14f) unit = 14f;
            float flowerTop = h - Mathf.Min(w * 0.94f, h * 0.50f);
            // Whole heart sits in the open sky: below the banner, above the play flower.
            float bannerClear = TopHud() + 200f + 36f * s;
            float heartY = bannerClear + 14f * unit;
            float bottom = heartY + 17f * unit;
            float limit = flowerTop - 16f * s;
            if (bottom > limit) heartY -= bottom - limit;
            if (heartY < bannerClear) heartY = bannerClear;
            float sky = TopHud() + 28f * s;
            DrawFwBurst(0, w * 0.68f, Mathf.Lerp(sky, heartY, 0.42f), s, unit);
            DrawFwBurst(1, w * 0.30f, Mathf.Lerp(sky, heartY, 0.62f), s, unit);
            DrawFwBurst(2, w * 0.50f, heartY, s, unit);
            GUI.color = Color.white;
        }

        static Color FwTint(int i)
        {
            if (i <= 0) return new Color(1f, 0.94f, 0.78f);
            if (i == 1) return new Color(0.42f, 0.70f, 1f);
            return new Color(1f, 0.18f, 0.28f);
        }

        void DrawFwBurst(int i, float x, float y, float s, float unit)
        {
            float age = _fwClock - FwWhen(i);
            if (age < 0f) return;
            var tint = FwTint(i);
            float risePx = 108f * s;
            if (risePx < 72f) risePx = 72f;
            if (risePx > 220f) risePx = 220f;
            float u = age / FwRise;
            if (u < 0f) u = 0f;
            float e = u > 1f ? 1f : 1f - (1f - u) * (1f - u);
            float weave = Mathf.Sin(age * 16f) * 5f * s * (u < 1f ? 1f - u : 0f);
            var from = new Vector2(x + weave, y + risePx);
            var head = new Vector2(x, y);
            var tip = Vector2.Lerp(from, head, e);
            if (age < FwRise + 0.16f)
                DrawFwTrail(age, from, tip, s, tint);
            if (age < FwRise) return;
            float t = age - FwRise;
            if (i == 2) DrawFwHeart(t, x, y, unit);
            else DrawFwRound(t, x, y, s, tint);
        }

        static void DrawFwTrail(float age, Vector2 from, Vector2 tip, float s, Color tint)
        {
            float u = age / FwRise;
            if (u < 0f) return;
            float fade = 1f;
            if (u > 1f) fade = 1f - (u - 1f) / 0.16f;
            if (fade <= 0f) return;
            const int n = 7;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)(n - 1);
                var p = Vector2.Lerp(tip, from, k * 0.92f);
                float a = fade * (1f - k) * (0.4f + 0.6f * (u > 1f ? 1f : u));
                float sz = (8f + 14f * (1f - k)) * s;
                if (sz < 8f) sz = 8f;
                if (sz > 34f) sz = 34f;
                var hot = Color.Lerp(tint, Color.white, 0.35f + 0.45f * (1f - k));
                SparkleFx.DrawGlow(p, sz * 1.15f, new Color(hot.r, hot.g, hot.b, a * 0.55f));
                SparkleFx.DrawAt(p, sz * (i == 0 ? 1.15f : 0.72f), a, hot);
            }
        }

        static void DrawFwFlash(float t, float dur, Vector2 at, float size, Color tint, float gain)
        {
            if (t < 0f || t > dur) return;
            float f = 1f - t / dur;
            f = f * f;
            SparkleFx.DrawGlow(at, size, new Color(1f, 0.96f, 0.92f, f * 0.50f * gain));
            SparkleFx.DrawGlow(at, size * 1.55f, new Color(tint.r, tint.g, tint.b, f * 0.28f * gain));
        }

        static void DrawFwRound(float t, float x, float y, float s, Color tint)
        {
            if (t < 0f || t > RoundLife) return;
            float life = 1f - t / RoundLife;
            float fade = life * life;
            var at = new Vector2(x, y);
            float rad = Screen.height * 0.072f;
            if (rad < 64f) rad = 64f;
            if (rad > 180f) rad = 180f;
            DrawFwFlash(t, 0.26f, at, rad * 1.15f, tint, 1f);
            float sc = s;
            if (sc > 3f) sc = 3f;
            float hang = t - 0.10f;
            if (hang < 0f) hang = 0f;
            float grav = hang * hang * Screen.height * 0.085f;
            const float expand = 0.24f;
            // SE keeps the round shape and drops every other spark plus the extra glint.
            int step = FoliageSway.LowPower ? 2 : 1;
            bool low = step > 1;
            for (int i = 0; i < RoundN; i += step)
            {
                float local = t - (i % 5) * 0.012f;
                if (local < 0f) continue;
                float eu = local / expand;
                if (eu > 1f) eu = 1f;
                float ease = 1f - (1f - eu) * (1f - eu);
                float spd = _roundSpd[i];
                float shell = _roundShell[i];
                float dist = (ease * spd * shell + hang * spd * 0.22f) * rad;
                float ang = _roundAng[i];
                float gy = grav * (shell > 0.8f ? 0.85f : 1.25f);
                float px = x + Mathf.Cos(ang) * dist;
                float py = y + Mathf.Sin(ang) * dist + gy;
                float lean = hang * 1.6f;
                if (lean > 1.3f) lean = 1.3f;
                float dir = Mathf.Atan2(Mathf.Sin(ang) + lean, Mathf.Cos(ang));
                float thick = (7f + 8f * shell) * sc * (0.55f + 0.45f * fade);
                if (thick < 5f) thick = 5f;
                float len = thick * Mathf.Lerp(2.4f, 0.85f, hang > 0.6f ? 1f : hang / 0.6f);
                var c = Color.Lerp(Color.white, tint, 0.35f + 0.55f * (1f - life));
                c.a = fade * (0.75f + 0.25f * shell);
                DrawFwStreak(new Vector2(px, py), dir, len, thick, c);
                if (!low && (i % 3) != 2)
                    SparkleFx.DrawAt(new Vector2(px, py), thick * 1.35f, fade, c);
            }
        }

        static void DrawFwStreak(Vector2 at, float ang, float len, float thick, Color c)
        {
            if (c.a < 0.02f || len < 1f) return;
            var glow = SpriteCatalog.Glow;
            if (glow == null || glow.texture == null) return;
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang * Mathf.Rad2Deg, at);
            GUI.color = c;
            GUI.DrawTexture(new Rect(at.x - len, at.y - thick * 0.5f, len, thick), glow.texture, ScaleMode.StretchToFill, true);
            GUI.matrix = prev;
            GUI.color = Color.white;
        }

        // x = 16 sin^3 t, y = 13 cos t - 5 cos 2t - 2 cos 3t - cos 4t.
        // Grows onto the curve, holds, then the point sags and the shell fades.
        static void DrawFwHeart(float t, float x, float y, float unit)
        {
            float end = HeartForm + HeartHold + HeartFade;
            if (t < 0f || t > end) return;
            float form = t / HeartForm;
            if (form > 1f) form = 1f;
            form = 1f - (1f - form) * (1f - form);
            float scale = 0.22f + 0.78f * form;
            float droopT = t - (HeartForm + HeartHold);
            if (droopT < 0f) droopT = 0f;
            float droopU = droopT / HeartFade;
            if (droopU > 1f) droopU = 1f;
            float a = form;
            if (droopU > 0f) a *= (1f - droopU) * (1f - droopU);
            if (a < 0.02f) return;
            var tint = FwTint(2);
            var at = new Vector2(x, y);
            DrawFwFlash(t, 0.34f, at, unit * 34f, tint, 1.35f);
            SparkleFx.DrawGlow(at, unit * 30f * scale, new Color(tint.r, tint.g * 0.55f, tint.b * 0.55f, a * 0.40f));
            float sag = droopU * droopU * unit * 16f;
            float spread = 1f + droopU * 0.10f;
            // Full heart is two rings. SE draws the outer ring at half density.
            bool low = FoliageSway.LowPower;
            int rings = low ? 1 : 2;
            int step = low ? 2 : 1;
            for (int ring = 0; ring < rings; ring++)
            {
                float ringSc = ring == 0 ? 1f : 0.78f;
                float szMul = ring == 0 ? 1f : 0.62f;
                for (int i = 0; i < HeartN; i += step)
                {
                    if (ring == 1 && (i & 1) != 0) continue;
                    float bias = (_heartY[i] + 6f) / 23f;
                    if (bias < 0f) bias = 0f;
                    if (bias > 1f) bias = 1f;
                    float drop = sag * (0.40f + 0.90f * bias);
                    float px = x + _heartX[i] * unit * scale * spread * ringSc;
                    float py = y + _heartY[i] * unit * scale * spread * ringSc + drop;
                    float pulse = 1f + 0.05f * Mathf.Sin(t * 9f + i * 0.55f + ring);
                    float sz = unit * 3.1f * pulse * szMul;
                    float hot = droopU > 0f ? 1f - droopU : 1f;
                    var c = Color.Lerp(tint, new Color(1f, 0.86f, 0.82f), 0.35f * hot);
                    var p = new Vector2(px, py);
                    if (!low)
                        SparkleFx.DrawGlow(p, sz * 1.65f, new Color(c.r, c.g, c.b, a * 0.55f));
                    SparkleFx.DrawGlow(p, sz, new Color(1f, 0.55f, 0.48f, a * 0.92f));
                    if (!low && (i & 1) == 0)
                        SparkleFx.DrawAt(p, sz * 0.95f, a, Color.Lerp(c, Color.white, 0.62f * hot));
                }
            }
        }
    }
}

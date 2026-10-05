using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Honey badger contest, Phase 4-5: the leap toward camera, the hive swipe and bee fill,
    // the takeoff sting, the splash sitter, and the Phase 5 opening lines. Timing and rules
    // live in BadgerShow.cs (BadgerLeap, BadgerOpening, BadgerSitter); lines in BadgerCopy.
    // Everything here is behind BadgerSchedule.Enabled: with the switch off the flag reads
    // 0, so no leap starts, no sitter draws, and the flower always loads the next garden.
    //
    // Shared pieces: BadgerLeap.Play (the one leap coroutine, both entries), BadgerOpening
    // (the one opening clock), BadgerTilePlate (the one player-grid tile path: the opening's
    // landing frame and the live grid), DrawBadgerArt (every badger, hive, bee, and splat
    // frame, through DrawSprite), SfxLibrary.Badger ("badger" clip + MixDesk lead mark),
    // Sfx.CardBump and the "tick" clip, PlayClock, BadgerSave, BadgerCopy, TutorialHeal.
    // Don't Care slam: BadgerSlam (clock, zoom, lines, glint), BadgerFight.TryDontCare (roll +
    // effect), PushBadgerZoom / DrawBadgerSpeedLines / DrawBadgerGlint (small shared helpers),
    // DrawBadgerShrug / DrawBadgerColumnSplat (shared with the round beat), StampOutlined,
    // SpriteCatalog.Sparkle, SfxLibrary.Badger + Sfx.CardBump, HealBadgerShow (interruption).
    public sealed partial class FlockFiveApp
    {
        readonly BadgerLeapRun _bgLeap = new BadgerLeapRun();
        Vector2 _bgLeapFrom;
        Vector2 _bgLeapTo;
        float _bgLeapUnit;
        float _bgWashOut;
        float _bgOpenT;
        int _bgOpenLanded;
        int _bgPendFrame = -1;
        BadgerDontCare _bgSlam;
        float _bgSlamT;
        int _bgSlamWord = -1;
        float _bgSlamStartedAt;
        int _bgPendValue;
#if UNITY_EDITOR
        // Editor shots hide the sitter unless this is set (debug override).
        public static bool BadgerShotOverride;
#endif

        static readonly Color BadgerWashTint = new Color(1f, 0.80f, 0.32f, 1f);

        // Read the flag once per frame (PrefGuard reads PlayerPrefs). Kill-switch aware.
        int BadgerOwed()
        {
            int f = Time.frameCount;
            if (f != _bgPendFrame)
            {
                _bgPendFrame = f;
                _bgPendValue = BadgerSave.Pending;
            }
            return _bgPendValue;
        }

        // ---- shared art ----

        // Every badger, hive, bee, and splat frame. Size is art px times `unit`, so frames
        // keep their true relative size; anchored bottom-center on `foot` so the feet stay
        // put between frames (no pop). Missing art draws nothing.
        static Rect DrawBadgerArt(string name, Vector2 foot, float unit, float alpha, float degrees = 0f)
        {
            var spr = SpriteCatalog.BadgerArt(name);
            if (spr == null || unit <= 0f) return default;
            var r = spr.rect;
            float w = r.width * unit;
            float h = r.height * unit;
            var dest = new Rect(foot.x - w * 0.5f, foot.y - h, w, h);
            if (!GuiPaint() || alpha < 0.01f) return dest;
            var keep = GUI.matrix;
            if (Mathf.Abs(degrees) > 0.01f)
                GUIUtility.RotateAroundPivot(degrees, new Vector2(foot.x, foot.y - h * 0.5f));
            GUI.color = new Color(1f, 1f, 1f, alpha);
            DrawSprite(dest, spr, false);
            GUI.color = Color.white;
            GUI.matrix = keep;
            return dest;
        }

        static float BadgerArtHeight(string name, float unit)
        {
            var spr = SpriteCatalog.BadgerArt(name);
            return spr == null ? 620f * unit : spr.rect.height * unit;
        }

        static void DrawBadgerWash(float a)
        {
            if (!GuiPaint() || a < 0.01f) return;
            var c = BadgerWashTint;
            c.a = Mathf.Clamp01(a);
            GUI.color = c;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // ---- input ----

        bool BadgerShowHoldsTaps()
        {
            if (_bgLeap.Live) return true;
            // Lesson is not held here: the shared glove must receive its valid tap.
            return _splash && _home == HomeFace.Badger
                && (_bgStage == BadgerStage.Opening || _bgStage == BadgerStage.PostOpen);
        }

        // Same pattern as SwallowResumePointer: the leap and the opening are not skippable.
        void SwallowBadgerShowPointer()
        {
            if (!BadgerShowHoldsTaps()) return;
            var e = Event.current;
            if (e == null) return;
            var t = e.type;
            if (t != EventType.MouseDown && t != EventType.MouseUp && t != EventType.MouseDrag) return;
            if (t == EventType.MouseUp) GUIUtility.hotControl = 0;
            e.Use();
        }

        // ---- the leap ----

        // Fresh clear: hops in from the right and lands in the garden.
        void AimBadgerLeapGarden()
        {
            float h = Screen.height;
            _bgLeapUnit = h * 0.24f / 620f;
            float y = h * 0.80f;
            _bgLeapTo = new Vector2(Screen.width * 0.60f, y);
            _bgLeapFrom = new Vector2(Screen.width + 420f * _bgLeapUnit, y);
        }

        // Flower tap: hops down off the bloom, in front of the flower.
        void AimBadgerLeapSplash()
        {
            BadgerSitterSpot(out var foot, out var unit);
            _bgLeapUnit = unit;
            _bgLeapFrom = foot;
            _bgLeapTo = new Vector2(Screen.width * 0.5f, foot.y + FlowerPlayRect().height * 0.34f);
        }

        // Drawn last, over the garden or the splash, while the leap is live.
        void DrawBadgerLeap()
        {
            if (!_bgLeap.Live || !GuiPaint()) return;
            float t = _bgLeap.T;
            var beat = BadgerLeap.BeatAt(t);
            string frame = BadgerLeap.FrameAt(t);
            float unit = _bgLeapUnit;
            if (beat <= BadgerLeapBeat.Crouch)
            {
                var foot = Vector2.Lerp(_bgLeapFrom, _bgLeapTo, BadgerLeap.HopTravel(t));
                foot.y -= BadgerLeap.HopLift(t) * Screen.height * 0.06f;
                DrawBadgerArt(frame, foot, unit, 1f);
            }
            else
            {
                // Grow from the crouch spot toward the middle until the toward-camera frame
                // fills the frame. One fill unit for all leap frames (sized on badger_leap_3).
                float g = BadgerLeap.Grow(t);
                var last = SpriteCatalog.BadgerArt(BadgerLeap.LeapFrames[BadgerLeap.LeapFrames.Length - 1]);
                float lw = last != null ? last.rect.width : 977f;
                float lh = last != null ? last.rect.height : 669f;
                float fill = Mathf.Max(Screen.width * 1.30f / lw, Screen.height * 0.55f / lh);
                float u = Mathf.Lerp(unit, Mathf.Max(unit, fill), g);
                float h0 = BadgerArtHeight(frame, unit);
                var c0 = new Vector2(_bgLeapTo.x, _bgLeapTo.y - h0 * 0.5f);
                var c1 = new Vector2(Screen.width * 0.5f, Screen.height * 0.48f);
                var c = Vector2.Lerp(c0, c1, g);
                c.y -= Mathf.Sin(g * Mathf.PI) * Screen.height * 0.08f;
                float h = BadgerArtHeight(frame, u);
                DrawBadgerArt(frame, new Vector2(c.x, c.y + h * 0.5f), u, 1f);
            }
            DrawBadgerWash(BadgerLeap.WashAlpha(t));
        }

        // Flower tap with the flag set: the leap, then the contest. Never Load.
        bool TakeBadgerFlower()
        {
            int owed = BadgerOwed();
            if (owed <= 0) return false;
            if (!_bgLeap.Live) StartCoroutine(BadgerFlowerLeap(owed));
            return true;
        }

        IEnumerator BadgerFlowerLeap(int owed)
        {
            AimBadgerLeapSplash();
            _busy = true;
            // Entrance cue: scored to the leap + opening clocks, rolls into the battle bed.
            if (MixDesk.Live != null) MixDesk.Live.BadgerEntrance();
            yield return BadgerLeap.Play(_bgLeap);
            _busy = false;
            if (!_bgLeap.Live)
            {
                if (MixDesk.Live != null) MixDesk.Live.SetBadger(false);
                yield break;
            }
            if (!_splash || _home != HomeFace.Splash || !OpenBadgerFight(owed))
            {
                _bgLeap.Release();
                if (MixDesk.Live != null) MixDesk.Live.SetBadger(false);
            }
        }

        // From SettleIfIdle, after the flag, RememberClear, and AwardClear, before ShowSplash,
        // with the garden still alive. The caller already holds _busy.
        IEnumerator BadgerClearLeap()
        {
            AimBadgerLeapGarden();
            if (MixDesk.Live != null) MixDesk.Live.BadgerEntrance();
            yield return BadgerLeap.Play(_bgLeap);
        }

        // A leap that lost its coroutine (killed mid-show) is dropped and taps freed.
        // Called from HealInterruptedTutorials, so it runs on the same start, splash,
        // pause, resume, focus, and every-few-frames schedule as every tutorial check.
        void HealBadgerShow()
        {
            if (_bgLessonLive && !(_splash && _home == HomeFace.Badger))
            {
                AdLog.Add("badger heal: lesson dropped");
                EndBadgerLesson();
                if (_bgFight == null && _bgStage == BadgerStage.Lesson)
                    BeginBadgerFight();
            }
            // A slam left behind (page gone) or stuck past its end finishes now; the zoom is
            // derived from the stage, so the contest is back at 1:1 the same frame.
            if (DontCareSlamLive && (!(_splash && _home == HomeFace.Badger)
                || BadgerSlam.Stuck(true, PlayClock.Now - _bgSlamStartedAt)))
            {
                AdLog.Add("badger heal: slam finished");
                FinishDontCareSlam();
            }
            if (!BadgerLeap.Stuck(_bgLeap.Live, PlayClock.Now - _bgLeap.StartedAt)) return;
            AdLog.Add("badger heal: leap dropped");
            _bgLeap.Release();
            if (MixDesk.Live != null) MixDesk.Live.SetBadger(false);
            if (_splash) _busy = false;
            else if (!_restarting) ShowSplash();
        }

        // ---- the opening ----

        void BeginBadgerOpening()
        {
            _bgStage = BadgerStage.Opening;
            _bgOpenT = 0f;
            _bgOpenLanded = 0;
            _bgLine = "";
            if (_bgLeap.Live)
            {
                _bgLeap.Release();
                _bgWashOut = BadgerLeap.WashOutSeconds;
            }
            else _bgWashOut = 0f;
        }

        void StepBadgerOpening(float dt)
        {
            if (_bgWashOut > 0f)
            {
                _bgWashOut = Mathf.Max(0f, _bgWashOut - dt);
                return;
            }
            float before = _bgOpenT;
            _bgOpenT += dt;
            if (!BadgerOpening.HiveSwiped(before) && BadgerOpening.HiveSwiped(_bgOpenT))
                Sfx.CardBump();
            int landed = BadgerOpening.LandedCount(_bgOpenT);
            if (landed > _bgOpenLanded)
            {
                _bgOpenLanded = landed;
                SfxLibrary.Play("tick", 0.24f);
            }
            if (_bgOpenT < BadgerOpening.Duration) return;
            _bgT = 0f;
            _bgStage = BadgerStage.PostOpen;
            // Appearance 1 with the lesson pending: lesson owns the box (no opening line).
            _bgLine = BadgerCopy.OpeningLine(_bgAppearance, BadgerCopy.NeedsLesson(_bgAppearance));
        }

        // 0 = empty cell, 0..1 flipping, 1 = final face. Outside the opening every tile is final.
        float BadgerTileLand(int tile)
        {
            if (_bgStage != BadgerStage.Opening) return 1f;
            if (_bgWashOut > 0f) return 0f;
            return BadgerOpening.TileLand(BadgerOpening.OrderOf(tile), _bgOpenT);
        }

        // THE player-grid tile. The opening's landing frame and the live grid both draw here,
        // so the landed tile is exactly the final tile. The flip squashes the plate across its
        // middle and lerps the album tint from wax to the bee's own, like the album plate.
        void BadgerTilePlate(Rect hex, BadgerTile t, BadgerLook look, float land)
        {
            var tint = BadgerTint(t);
            if (land >= 1f)
            {
                DrawBadgerTile(hex, look, t.Honey, tint, t.Finish, 1f, null);
                return;
            }
            DrawBadgerTile(hex, BadgerLook.Spent, 0, tint, BeeFinish.Normal, 1f, null);
            if (land <= 0f) return;
            float w = Mathf.Abs(Mathf.Cos(land * Mathf.PI));
            var r = new Rect(hex.center.x - hex.width * w * 0.5f, hex.y, hex.width * w, hex.height);
            if (land < 0.5f)
                DrawBadgerTile(r, BadgerLook.Down, 0, BadgerWax, BeeFinish.Normal, 1f, null);
            else
                DrawBadgerTile(r, look, t.Honey, Color.Lerp(BadgerWax, tint, (land - 0.5f) * 2f), t.Finish, 1f, null);
        }

        // Hive, badger swipe, splat, and the bees in flight. Drawn over the grids.
        void DrawBadgerOpeningCast(BadgerRects L, BadgerHex youHex)
        {
            if (_bgStage != BadgerStage.Opening || !GuiPaint()) return;
            float t = _bgWashOut > 0f ? 0f : _bgOpenT;
            float cast = BadgerOpening.CastAlpha(t);
            var a = L.Arena;
            float hiveUnit = a.height * 1.25f / 681f;
            var hiveFoot = new Vector2(a.center.x - a.width * 0.10f, a.yMax);
            string hive = BadgerOpening.HiveSwiped(t) ? "hive_swiped" : "hive_idle";
            var hr = DrawBadgerArt(hive, hiveFoot, hiveUnit, cast, BadgerOpening.Wobble(t));
            if (hr.width < 1f) hr = new Rect(hiveFoot.x - a.height * 0.6f, a.y, a.height * 1.2f, a.height);

            float swing = BadgerOpening.Swing(t);
            var badgerFoot = new Vector2(a.center.x + a.width * 0.30f - swing * a.width * 0.10f, a.yMax);
            DrawBadgerArt("badger_idle_b", badgerFoot, a.height * 1.35f / 640f, cast);

            float splat = BadgerOpening.SplatAlpha(t);
            if (splat > 0f)
            {
                float su = hiveUnit * 0.75f;
                float sh = BadgerArtHeight("honey_splat", su);
                DrawBadgerArt("honey_splat", new Vector2(hr.center.x, hr.center.y + sh * 0.5f), su, splat * cast);
            }

            var from = hr.center;
            float beeH = youHex.Cw * 0.62f;
            for (int k = 0; k < BadgerOpening.Landings; k++)
            {
                float u = BadgerOpening.Flight(k, t);
                if (u < 0f || u > 1f) continue;
                int tile = BadgerOpening.TileAt(k);
                var to = youHex.Center(tile);
                var p = Vector2.Lerp(from, to, u * u * (3f - 2f * u));
                p.y -= Mathf.Sin(u * Mathf.PI) * a.height * 0.55f;
                p.x += Mathf.Sin(u * Mathf.PI * 3f + k) * youHex.Cw * 0.10f;
                bool yard = _bgLoadout != null && _bgLoadout[tile].Yard;
                float shrink = u < 0.7f ? 1f : Mathf.Lerp(1f, 0.40f, (u - 0.7f) / 0.3f);
                string bee = BadgerOpening.BeeFrame(k);
                var spr = SpriteCatalog.BadgerArt(bee);
                float artH = spr != null ? spr.rect.height : 300f;
                float unit = beeH * BadgerOpening.BeeScale(yard) * shrink / artH;
                float h = artH * unit;
                DrawBadgerArt(bee, new Vector2(p.x, p.y + h * 0.5f), unit, 1f);
            }
        }

        // ---- "Honey Badger Don't Care" slam (spec 2b) ----

        // THE slam beat. Starts the clock after a TryDontCare hit; TickBadger steps it and
        // resolves the round once BadgerSlam.Done. The fight already holds the effect (power-up
        // stripped, or this round's honey halved); the show reveals it at BadgerSlam.NastyAt.
        void PlayDontCareSlam(BadgerDontCare slam)
        {
            _bgSlam = slam;
            _bgSlamT = 0f;
            _bgSlamWord = -1;
            _bgSlamStartedAt = PlayClock.Now;
            _bgStage = BadgerStage.DontCare;
            _bgLine = "";
        }

        // Shared SFX only: the "badger" sting (MixDesk lead mark) on the first word,
        // Sfx.CardBump on each later word and on the nasty move.
        void StepDontCareSlam(float dt)
        {
            float before = _bgSlamT;
            _bgSlamT += dt;
            int w = BadgerSlam.WordIndexAt(_bgSlamT);
            if (w > _bgSlamWord)
            {
                _bgSlamWord = w;
                if (w == 0) SfxLibrary.Badger();
                else Sfx.CardBump();
            }
            if (BadgerSlam.Crosses(before, _bgSlamT, BadgerSlam.NastyAt)) ApplyDontCareNasty();
        }

        // The visible half of the nasty move. A destroyed power-up loses its badge and lit
        // button here (coins stay spent; BadgerFight already dropped it from the compare).
        void ApplyDontCareNasty()
        {
            if (_bgSlam.Destroyed != BadgerPower.None) _bgArmed = BadgerPower.None;
        }

        // End the beat now (normal end, or the heal). Scale is derived from the stage, so
        // leaving DontCare is what puts the contest back at 1:1.
        void FinishDontCareSlam()
        {
            if (_bgStage != BadgerStage.DontCare) return;
            _bgSlamT = BadgerSlam.Duration;
            ApplyDontCareNasty();
            if (_bgFight != null) ResolveBadgerRound();
            else _bgStage = BadgerStage.Opening;
        }

        bool DontCareSlamLive => _bgStage == BadgerStage.DontCare;

        // Zoom for this frame: 1 unless the slam is live. Read fresh every frame, never stored.
        float DontCareZoom() => DontCareSlamLive ? BadgerSlam.Zoom(_bgSlamT) : 1f;

        // Where the pupil sits on screen; the zoom pivots here so the eye holds still and grows.
        static Vector2 DontCareEyeAt() => new Vector2(Screen.width * 0.5f, Screen.height * 0.42f);

        // Shared: scale the GUI around a pivot. Returns the matrix to restore; callers restore
        // it in a finally so an exception mid-draw cannot leave the screen zoomed.
        static Matrix4x4 PushBadgerZoom(float k, Vector2 pivot)
        {
            var keep = GUI.matrix;
            if (Mathf.Abs(k - 1f) > 0.0001f) GUIUtility.ScaleAroundPivot(new Vector2(k, k), pivot);
            return keep;
        }

        // Shared: radial speed lines rushing toward `center`. Thin cream strokes, no art.
        static void DrawBadgerSpeedLines(Vector2 center, float strength, float t)
        {
            if (!GuiPaint() || strength < 0.02f) return;
            float diag = Mathf.Sqrt(Screen.width * (float)Screen.width + Screen.height * (float)Screen.height);
            float thick = Mathf.Max(2f, diag * 0.004f);
            var keep = GUI.matrix;
            try
            {
                for (int i = 0; i < BadgerSlam.SpeedLineCount; i++)
                {
                    BadgerSlam.SpeedLine(i, t, out float deg, out float inner, out float outer);
                    GUI.matrix = keep;
                    GUIUtility.RotateAroundPivot(deg, center);
                    float x0 = center.x + inner * diag;
                    float len = Mathf.Max(1f, (outer - inner) * diag);
                    GUI.color = new Color(BadgerCream.r, BadgerCream.g, BadgerCream.b, 0.70f * strength);
                    GUI.DrawTexture(new Rect(x0, center.y - thick * 0.5f, len, thick), Texture2D.whiteTexture);
                }
            }
            finally
            {
                GUI.matrix = keep;
                GUI.color = Color.white;
            }
        }

        // Shared: a white glint. SpriteCatalog.Sparkle (the existing celebration glint) over a
        // small white core rect; the core alone still reads if the sparkle sprite is missing.
        static void DrawBadgerGlint(Vector2 at, float size, float alpha)
        {
            if (!GuiPaint() || alpha < 0.02f || size < 1f) return;
            float core = size * 0.22f;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(at.x - core * 0.5f, at.y - core * 0.5f, core, core), Texture2D.whiteTexture);
            var spr = SpriteCatalog.Sparkle;
            if (spr != null && spr.texture != null)
                GUI.DrawTexture(new Rect(at.x - size * 0.5f, at.y - size * 0.5f, size, size), spr.texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        // Close-up face for the eye zoom: placed so its pupil sits on DontCareEyeAt.
        static void DrawDontCareFace(float alpha)
        {
            var spr = SpriteCatalog.BadgerArt(BadgerSlam.FaceFrame);
            float artW = spr != null ? spr.rect.width : 977f;
            float artH = spr != null ? spr.rect.height : 669f;
            float unit = Mathf.Min(Screen.width * 0.86f / artW, Screen.height * 0.50f / artH);
            var eye = DontCareEyeAt();
            float w = artW * unit;
            float h = artH * unit;
            float left = eye.x - BadgerSlam.EyeU * w;
            float top = eye.y - BadgerSlam.EyeV * h;
            DrawBadgerArt(BadgerSlam.FaceFrame, new Vector2(left + w * 0.5f, top + h), unit, alpha);
        }

        // Overlay, drawn after the page with the page matrix restored. Wash and words in screen
        // space; the face under the same zoom as the page; lines, glint, and the nasty move's
        // honey_splat + shrug on top. Mild and cartoonish only.
        void DrawDontCareSlam(BadgerRects L)
        {
            if (!DontCareSlamLive || !GuiPaint()) return;
            float t = _bgSlamT;
            var eye = DontCareEyeAt();
            float k = BadgerSlam.Zoom(t);

            float wash = BadgerSlam.WashAlpha(t);
            GUI.color = new Color(0.05f, 0.04f, 0.03f, wash);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float face = BadgerSlam.FaceAlpha(t);
            if (face > 0.01f)
            {
                var keep = PushBadgerZoom(k, eye);
                try { DrawDontCareFace(face); }
                finally { GUI.matrix = keep; }
            }

            DrawBadgerSpeedLines(eye, BadgerSlam.SpeedLines(t), t);
            float g = BadgerSlam.Glint(t);
            if (g > 0f)
            {
                float tw = 0.85f + 0.15f * Mathf.Sin(t * 40f);
                DrawBadgerGlint(new Vector2(eye.x - Screen.height * 0.012f * k, eye.y - Screen.height * 0.012f * k),
                    Screen.height * 0.035f * k * tw, g);
            }

            if (BadgerSlam.ShowNasty(t))
            {
                float a = 1f - Mathf.Clamp01((t - BadgerSlam.NastyEnd) / BadgerSlam.ZoomOutSeconds) * 0.4f;
                DrawBadgerShrug(L, a);
                DrawBadgerColumnSplat(L, 0.92f * a);
            }

            string word = BadgerSlam.WordAt(t);
            if (string.IsNullOrEmpty(word)) return;
            float pop = BadgerSlam.Pop(t);
            float w = Screen.width * 0.80f;
            float h = Screen.height * 0.14f;
            var box = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.40f - h * 0.5f, w, h);
            var st = BadgerStyle();
            int size = FitFont(st, word, box.width, box.height, 18, 160);
            st.fontSize = Mathf.Max(18, Mathf.RoundToInt(size * pop));
            var big = new Rect(box.center.x - box.width * pop * 0.5f, box.center.y - box.height * pop * 0.5f,
                box.width * pop, box.height * pop);
            float wa = wash > 0f ? Mathf.Clamp01(wash / BadgerSlam.WashMax) : 0f;
            StampOutlined(big, word, st, new Color(BadgerCream.r, BadgerCream.g, BadgerCream.b, wa), 0, 3);
        }

        // ---- splash sitter ----

        // Feet on the bloom, body above the LEVEL line, centered on the play flower.
        void BadgerSitterSpot(out Vector2 foot, out float unit)
        {
            var rest = FlowerPlayRect();
            var disc = FlowerDisc(rest, 0f);
            float y = Mathf.Min(disc.y + disc.height * 0.30f, FlowerLevelTop());
            foot = new Vector2(rest.center.x, y);
            unit = rest.height * 0.40f / 640f;
        }

        bool BadgerShotHidden()
        {
#if UNITY_EDITOR
            if (BadgerShotOverride) return false;
            return _shotLevelNumber > 0 || _shotEase != null || _finalePreview;
#else
            return false;
#endif
        }

        // After the bloom and before DrawFlowerCaption, so LEVEL n and the joke stay on top.
        // Hit target, caption, and disc are untouched.
        void DrawBadgerSitter(float sink)
        {
            if (!GuiPaint()) return;
            if (!BadgerSitter.Visible(BadgerOwed(), _bgLeap.Live, BadgerShotHidden())) return;
            BadgerSitterSpot(out var foot, out var unit);
            foot.y += sink;
            DrawBadgerArt(BadgerSitter.FrameAt(PlayClock.Now), foot, unit, 1f);
        }
    }
}

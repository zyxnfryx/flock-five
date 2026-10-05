using System.Collections;
using UnityEngine;

namespace FlockFive
{
    // Honey badger contest, Phase 4: the leap toward camera, the hive swipe and bee fill,
    // the takeoff sting, and the splash sitter. Timing and rules live in BadgerShow.cs
    // (BadgerLeap, BadgerOpening, BadgerSitter); this file draws them and hooks them in.
    // Everything here is behind BadgerSchedule.Enabled: with the switch off the flag reads
    // 0, so no leap starts, no sitter draws, and the flower always loads the next garden.
    //
    // Shared pieces: BadgerLeap.Play (the one leap coroutine, both entries), BadgerOpening
    // (the one opening clock), BadgerTilePlate (the one player-grid tile path: the opening's
    // landing frame and the live grid), DrawBadgerArt (every badger, hive, bee, and splat
    // frame, through DrawSprite), SfxLibrary.Badger ("badger" clip + MixDesk lead mark),
    // Sfx.CardBump and the "tick" clip, PlayClock, BadgerSave, and the TutorialHeal gate length.
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
            yield return BadgerLeap.Play(_bgLeap);
            _busy = false;
            if (!_bgLeap.Live) yield break;
            if (!_splash || _home != HomeFace.Splash || !OpenBadgerFight(owed))
                _bgLeap.Release();
        }

        // From SettleIfIdle, after the flag, RememberClear, and AwardClear, before ShowSplash,
        // with the garden still alive. The caller already holds _busy.
        IEnumerator BadgerClearLeap()
        {
            AimBadgerLeapGarden();
            yield return BadgerLeap.Play(_bgLeap);
        }

        // A leap that lost its coroutine (killed mid-show) is dropped and taps freed.
        // Called from HealInterruptedTutorials, so it runs on the same start, splash,
        // pause, resume, focus, and every-few-frames schedule as every tutorial check.
        void HealBadgerShow()
        {
            if (!BadgerLeap.Stuck(_bgLeap.Live, PlayClock.Now - _bgLeap.StartedAt)) return;
            AdLog.Add("badger heal: leap dropped");
            _bgLeap.Release();
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

        const string BadgerOpenLine = "The badger swatted the hive. Your bees took their tiles!";

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
            _bgLine = BadgerOpenLine;
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

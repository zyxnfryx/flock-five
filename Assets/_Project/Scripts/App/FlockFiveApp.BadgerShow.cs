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
    // effect), PushBadgerZoom on the whole page around the boss eye, then a full-screen wash,
    // DrawBadgerSpeedLines / DrawBadgerGlint, and the tilted words last. DontCareSlamRects keeps
    // that word band inside the safe area. BadgerSwipe is the one hive swipe. SparkleFx + DrawWaxSplat
    // are the honey-drop burst. Sfx.CardBump is the hit (no separate swipe clip). HealBadgerShow.
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
        static Rect DrawBadgerArt(string name, Vector2 foot, float unit, float alpha, float degrees = 0f, float sx = 1f, float sy = 1f)
        {
            var spr = SpriteCatalog.BadgerArt(name);
            if (spr == null || unit <= 0f) return default;
            if (sx < 0.05f) sx = 1f;
            if (sy < 0.05f) sy = 1f;
            var r = spr.rect;
            float w = r.width * unit * sx;
            float h = r.height * unit * sy;
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

        // Under then Frame, feet planted. Scale is the sample's squash / breathe.
        static void DrawBadgerPose(BadgerSample pose, Vector2 foot, float unit, float alpha)
        {
            if (alpha < 0.01f || unit <= 0f) return;
            if (!string.IsNullOrEmpty(pose.Under) && pose.UnderAlpha > 0.02f && pose.Under != pose.Frame)
                DrawBadgerArt(pose.Under, foot, unit, alpha * pose.UnderAlpha, pose.Degrees, pose.ScaleX, pose.ScaleY);
            if (!string.IsNullOrEmpty(pose.Frame) && pose.FrameAlpha > 0.02f)
                DrawBadgerArt(pose.Frame, foot, unit, alpha * pose.FrameAlpha, pose.Degrees, pose.ScaleX, pose.ScaleY);
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
            if (!_bgLeap.Live) return;
            SparkleFx.Tick(Time.unscaledDeltaTime);
            StepBadgerLeapFx();
            if (!GuiPaint()) return;
            float t = _bgLeap.T;
            var beat = BadgerLeap.BeatAt(t);
            var pose = BadgerAnim.Entrance(t);
            float unit = _bgLeapUnit;
            if (beat <= BadgerLeapBeat.Crouch)
            {
                var foot = Vector2.Lerp(_bgLeapFrom, _bgLeapTo, BadgerLeap.HopTravel(t));
                foot.y -= BadgerLeap.HopLift(t) * Screen.height * 0.06f;
                _bgFxFoot = foot;
                DrawBadgerPose(pose, foot, unit, 1f);
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
                float h0 = BadgerArtHeight(pose.Frame, unit);
                var c0 = new Vector2(_bgLeapTo.x, _bgLeapTo.y - h0 * 0.5f);
                var c1 = new Vector2(Screen.width * 0.5f, Screen.height * 0.48f);
                var c = Vector2.Lerp(c0, c1, g);
                c.y -= Mathf.Sin(g * Mathf.PI) * Screen.height * 0.08f;
                float h = BadgerArtHeight(pose.Frame, u);
                var foot = new Vector2(c.x, c.y + h * 0.5f);
                _bgFxFoot = foot;
                DrawBadgerPose(pose, foot, u, 1f);
            }
            DrawBadgerWash(BadgerLeap.WashAlpha(t));
            SparkleFx.DrawBits();
        }

        // Dust when a hop lands and when the crouch catches the leap. Once per crossing.
        void StepBadgerLeapFx()
        {
            float t = _bgLeap.T;
            float before = _bgLeapEmitT;
            if (before > t) before = -1f;
            _bgLeapEmitT = t;
            if (before < 0f || t <= before) return;
            var foot = Vector2.Lerp(_bgLeapFrom, _bgLeapTo, BadgerLeap.HopTravel(t));
            foot.y -= BadgerLeap.HopLift(t) * Screen.height * 0.06f;
            var was = BadgerLeap.BeatAt(before);
            var now = BadgerLeap.BeatAt(t);
            if (was == BadgerLeapBeat.Hop && now != BadgerLeapBeat.Hop)
            {
                SparkleFx.DustPuff(foot, 8);
                return;
            }
            if (now != BadgerLeapBeat.Hop) return;
            float stride = BadgerLeap.HopStride;
            if (stride < 0.05f) return;
            float p0 = Mathf.Repeat(before, stride) / stride;
            float p1 = Mathf.Repeat(t, stride) / stride;
            if (p1 < p0) p0 = 0f;
            if (p0 < BadgerLeap.HopAirShare && p1 >= BadgerLeap.HopAirShare)
                SparkleFx.DustPuff(foot, 6);
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
            {
                // No swipe clip. CardBump is the hit, and the hive uses the shared shake.
                Sfx.CardBump();
                BadgerShakeStart(BadgerHiveKey);
                SparkleFx.HoneySplash(_bgFxHive, 14);
                SparkleFx.DustPuff(_bgFxFoot, 6);
            }
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

        // Hive, the comic splat, a hit flash, and a honey-drop burst. The badger is the
        // duel actor, not a second sprite here. Returns the hive rect the claws cross.
        Rect DrawBadgerOpeningHive(BadgerRects L)
        {
            var a = L.Arena;
            var hiveFoot = new Vector2(a.center.x, a.yMax);
            var fallback = new Rect(hiveFoot.x - a.height * 0.6f, a.y, a.height * 1.2f, a.height);
            if (_bgStage != BadgerStage.Opening || !GuiPaint()) return fallback;
            float t = _bgWashOut > 0f ? 0f : _bgOpenT;
            hiveFoot.x += BadgerShakeX(BadgerHiveKey, L.S);
            float cast = BadgerOpening.CastAlpha(t);
            float hiveUnit = a.height * 1.25f / 681f;
            string hive = BadgerOpening.HiveSwiped(t) ? "hive_swiped" : "hive_idle";
            var hr = DrawBadgerArt(hive, hiveFoot, hiveUnit, cast, BadgerOpening.Wobble(t));
            if (hr.width < 1f) hr = fallback;
            _bgFxHive = hr.center;

            float flash = BadgerSwipe.FlashAlpha(t);
            if (flash > 0.02f)
            {
                float pad = hr.width * 0.08f;
                GUI.color = new Color(1f, 0.96f, 0.78f, 0.72f * flash * cast);
                GUI.DrawTexture(new Rect(hr.x - pad, hr.y - pad, hr.width + pad * 2f, hr.height + pad * 2f),
                    GlowTex(), ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
            }

            float splat = BadgerOpening.SplatAlpha(t);
            if (splat > 0f)
            {
                float su = hiveUnit * 0.75f;
                float sh = BadgerArtHeight("honey_splat", su);
                DrawBadgerArt("honey_splat", new Vector2(hr.center.x, hr.center.y + sh * 0.5f), su, splat * cast);
            }

            float age = t - BadgerSwipe.HitAt;
            if (age >= 0f && age < 0.42f && hr.width > 2f)
            {
                float drop = cast * (1f - age / 0.42f);
                DrawWaxSplat(hr, BadgerGold, new Color(0.72f, 0.38f, 0.06f, 1f), age, drop);
                SparkleFx.DrawAround(hr, drop, true, 0.16f);
            }
            return hr;
        }

        // Three curved claw streaks across the hive. White core, warm edge, drawn with the
        // same texture quads as the speed lines. BadgerSwipe owns the shape.
        static void DrawBadgerClaws(Rect hive, float t)
        {
            if (!GuiPaint() || hive.width < 4f || hive.height < 4f) return;
            if (BadgerSwipe.SlashAlpha(t) < 0.02f) return;
            var keep = GUI.matrix;
            try
            {
                for (int s = 0; s < BadgerSwipe.Streaks; s++)
                {
                    float lag = s * 0.02f;
                    float sweep = BadgerSwipe.Sweep(t - lag);
                    float a = BadgerSwipe.SlashAlpha(t - lag);
                    if (a < 0.02f || sweep <= 0f) continue;
                    for (int i = 0; i < BadgerSwipe.Segs; i++)
                    {
                        if (!BadgerSwipe.Segment(s, i, sweep, hive, out var p0, out var p1, out float width, out float cover))
                            continue;
                        var dir = p1 - p0;
                        float len = dir.magnitude;
                        if (len < 0.4f || width < 0.3f) continue;
                        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        GUI.matrix = keep;
                        GUIUtility.RotateAroundPivot(ang, p0);
                        float fade = a * cover;
                        float extra = width * 0.65f;
                        GUI.color = new Color(1f, 0.72f, 0.22f, 0.82f * fade);
                        GUI.DrawTexture(new Rect(p0.x - extra * 0.25f, p0.y - width, len + extra, width * 2f), Texture2D.whiteTexture);
                        float core = width * 0.38f;
                        GUI.color = new Color(1f, 1f, 1f, 0.95f * fade);
                        GUI.DrawTexture(new Rect(p0.x, p0.y - core, len, core * 2f), Texture2D.whiteTexture);
                    }
                }
            }
            finally
            {
                GUI.matrix = keep;
                GUI.color = Color.white;
            }
        }

        // Bees in flight, drawn in front of the fighters so a landing reads over the arena.
        void DrawBadgerOpeningBees(BadgerRects L, BadgerHex youHex, Vector2 from)
        {
            if (_bgStage != BadgerStage.Opening || !GuiPaint()) return;
            float t = _bgWashOut > 0f ? 0f : _bgOpenT;
            var a = L.Arena;
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

        // Each word is a CardBump thud (Lead, shorter than the 0.40 s gap). The badger
        // sting waits for the nasty beat so it does not sit on a word.
        void StepDontCareSlam(float dt)
        {
            float before = _bgSlamT;
            _bgSlamT += dt;
            int w = BadgerSlam.WordIndexAt(_bgSlamT);
            if (w > _bgSlamWord)
            {
                _bgSlamWord = w;
                Sfx.CardBump();
                var safe = DontCareSlamRects.SafeGui(Screen.width, Screen.height, Screen.safeArea);
                var band = DontCareSlamRects.WordSlot(safe, w);
                SparkleFx.SlamBurst(band.center);
            }
            if (BadgerSlam.Crosses(before, _bgSlamT, BadgerSlam.NastyAt))
            {
                ApplyDontCareNasty();
                SfxLibrary.Badger();
            }
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

        // Shared: scale the GUI around a pivot. Returns the matrix to restore; callers restore
        // it in a finally so an exception mid-draw cannot leave the screen zoomed.
        // The Don't Care slam scales the whole page around the boss eye, and only while
        // painting, so hit rects stay in layout space.
        static Matrix4x4 PushBadgerZoom(float k, Vector2 pivot)
        {
            var keep = GUI.matrix;
            if (Mathf.Abs(k - 1f) > 0.0001f) GUIUtility.ScaleAroundPivot(new Vector2(k, k), pivot);
            return keep;
        }

        // Shared: radial speed lines rushing toward `center`. Thin cream strokes, no art.
        // maxRadius > 0 keeps every stroke inside that circle so the lines cannot cross the caption.
        static void DrawBadgerSpeedLines(Vector2 center, float strength, float t, float maxRadius)
        {
            if (!GuiPaint() || strength < 0.02f) return;
            float diag = Mathf.Sqrt(Screen.width * (float)Screen.width + Screen.height * (float)Screen.height);
            float reach = maxRadius > 1f ? maxRadius : diag;
            float thick = Mathf.Max(2f, reach * 0.012f);
            var keep = GUI.matrix;
            try
            {
                for (int i = 0; i < BadgerSlam.SpeedLineCount; i++)
                {
                    BadgerSlam.SpeedLine(i, t, out float deg, out float inner, out float outer);
                    GUI.matrix = keep;
                    GUIUtility.RotateAroundPivot(deg, center);
                    float x0 = center.x + inner * reach;
                    float len = Mathf.Max(1f, (outer - inner) * reach);
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

        // Shared sparkle helper. White core + SpriteCatalog.Sparkle via SparkleFx.DrawAt.
        static void DrawBadgerGlint(Vector2 at, float size, float alpha)
        {
            if (!GuiPaint() || alpha < 0.02f || size < 1f) return;
            SparkleFx.DrawAt(at, size, alpha);
        }

        // After the zoomed page. Full-screen wash, then speed lines, then the eye glint,
        // then the tilted words. Nothing in this method draws after the words.
        void DrawDontCareSlam(Vector2 pivot, float zoom)
        {
            if (!DontCareSlamLive || !GuiPaint()) return;
            float t = _bgSlamT;

            float wash = BadgerSlam.WashAlpha(t);
            if (wash > 0.01f)
            {
                GUI.color = new Color(0.05f, 0.04f, 0.03f, wash);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            Vector2 local = _bgDuelEyeOn ? _bgDuelEye : pivot;
            var eye = new Vector2(
                pivot.x + (local.x - pivot.x) * zoom,
                pivot.y + (local.y - pivot.y) * zoom);
            DrawBadgerSpeedLines(eye, BadgerSlam.SpeedLines(t), t, 0f);
            float g = BadgerSlam.Glint(t);
            if (g > 0f)
            {
                float tw = 0.85f + 0.15f * Mathf.Sin(t * 40f);
                DrawBadgerGlint(eye, Screen.height * 0.035f * tw, g);
            }

            int n = BadgerSlam.ShownCount(t);
            if (n < 1) return;
            var safe = DontCareSlamRects.SafeGui(Screen.width, Screen.height, Screen.safeArea);
            for (int i = 0; i < n; i++)
            {
                if (!BadgerSlam.WordOn(t, i)) continue;
                string word = BadgerCopy.DontCareWordAt(i);
                if (string.IsNullOrEmpty(word)) continue;
                DrawDontCareWord(DontCareSlamRects.WordSlot(safe, i), word, BadgerSlam.WordPop(t, i));
            }
        }

        // One slammed word, tilted so the right side rises. Drawn last.
        static void DrawDontCareWord(Rect band, string word, float pop)
        {
            if (!GuiPaint() || band.width < 4f || band.height < 4f || string.IsNullOrEmpty(word)) return;
            var st = BadgerStyle();
            st.fontSize = FitFont(st, word, band.width * 0.92f, band.height * 0.88f, 14, 140);
            var keep = GUI.matrix;
            try
            {
                var c = band.center;
                GUIUtility.RotateAroundPivot(DontCareSlamRects.TiltDegrees, c);
                if (Mathf.Abs(pop - 1f) > 0.001f)
                    GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), c);
                StampOutlined(band, word, st, BadgerCream, 1, Mathf.Max(2, st.fontSize / 10));
            }
            finally
            {
                GUI.matrix = keep;
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
            DrawBadgerPose(BadgerAnim.Idle(PlayClock.Now), foot, unit, 1f);
        }
    }
}

using UnityEngine;

namespace FlockFive
{
    public sealed partial class FlockFiveApp
    {
        // Old coach floor was an unscaled 18, then CaptionFontBoost. It did not
        // grow with the phone, so a long lesson dropped to about 21px.
        public const int CaptionFloorRef = 18;

        // Lesson type on a 2556-tall phone. Scales with screen height. 60–70px there.
        public const float CaptionFloorPhonePx = 64f;
        public const float CaptionFloorPhoneH = 2556f;

        public static int CaptionPxOf(int px) => CaptionPx(px);

        // Seat width shared by the garden lesson and the measured caption box.
        // Wide enough that the bee sentence wraps in a few lines, and the plate
        // (BoxPad on each side) still sits on the glass.
        public static float LessonColumnWidth(float screenW, float screenH)
        {
            float s = Mathf.Max(screenH / 720f, 1f);
            float plate = CaptionLayout.BoxPad(screenH).x;
            float maxSeat = screenW - 16f - plate * 2f;
            float want = Mathf.Min(screenW * 0.92f, 720f * s);
            if (want > maxSeat) want = maxSeat;
            if (want < 160f) want = Mathf.Min(Mathf.Max(48f, maxSeat), 160f);
            if (want < 48f) want = 48f;
            return want;
        }

        // Inner text size of the standard lesson box on this phone.
        public static void LessonTextBox(float screenW, float screenH, out float textW, out float textH)
        {
            float s = Mathf.Max(screenH / 720f, 1f);
            float boxW = LessonColumnWidth(screenW, screenH);
            float boxH = Mathf.Max(128f * s, 108f);
            float padX = Mathf.Clamp(boxW * 0.055f, 10f * s, 18f * s);
            float padY = Mathf.Clamp(boxH * 0.10f, 6f * s, 14f * s);
            textW = Mathf.Max(48f, boxW - padX * 2f);
            textH = Mathf.Max(24f, boxH - padY * 2f);
        }

        // Shared lesson size. About 64px on a 2556-tall phone, scaled by height.
        // Never below the old boosted 18.
        public static int LessonCaptionFloor(float screenW, float screenH)
        {
            float h = screenH > 2f ? screenH : CaptionFloorPhoneH;
            int floor = Mathf.RoundToInt(CaptionFloorPhonePx * (h / CaptionFloorPhoneH));
            if (floor < 1) floor = 1;
            int old = CaptionPx(CaptionFloorRef);
            if (floor < old) floor = old;
            return floor;
        }

        // Wrapped line count at the lesson floor, in the shared column.
        public static int LessonCaptionLines(string text, float screenW, float screenH)
        {
            float s = Mathf.Max(screenH / 720f, 1f);
            float seatW = LessonColumnWidth(screenW, screenH);
            float padX = Mathf.Clamp(seatW * 0.055f, 10f * s, 18f * s);
            float textW = Mathf.Max(48f, seatW - padX * 2f);
            var fit = FitLessonText(text, textW, 4096f, 12, Mathf.RoundToInt(32f * s), screenW, screenH);
            int lines = 1;
            string shown = fit.Shown ?? "";
            for (int i = 0; i < shown.Length; i++)
                if (shown[i] == '\n') lines++;
            return lines;
        }

        public static string LessonCaptionShown(string text)
        {
            return CaptionLayout.LessonShown(text, CoachLineStyle().font);
        }

        public static int LessonCaptionFont(string text, float screenW, float screenH)
        {
            LessonTextBox(screenW, screenH, out float textW, out float textH);
            float s = Mathf.Max(screenH / 720f, 1f);
            var fit = FitLessonText(text, textW, textH, 12, Mathf.RoundToInt(32f * s), screenW, screenH);
            return fit.Font;
        }

        // Every sentence a lesson plate can paint, including the multiline forms.
        public static string[] PaintedLessonLines()
        {
            var steps = CoachTap.All;
            var extra = new[]
            {
                CaptionLayout.HiveGardenLine,
                CaptionLayout.AlbumHoneyBlock,
                "Tap the hive to see your bee collection.",
                "You earned coins!\nTap poker to bet them.",
                "Tap Daily for\nyour bonus!",
                "Tap to claim!",
                "Leaves hide these birds.\nCollect at a feeder\nto blow them away.",
                "A sparrow blocks a feeder! Match five birds there to chase it off.",
                "A hawk blocks a feeder! Match five there twice to drive it off.",
                "Bees cover birds on a branch! Move the birds off a bee to free it and win a bee card.",
                "Tap a card you have\nto enlarge it.",
                "Tap again to flip.",
                "Upgrade adds honey for 1000 coins.\nMax 1 · 2 · 3 by rarity.",
                "Swipe to turn the page!\nOr tap a page number.",
                "Tap a card to take\na closer look!",
                "Tap to unlock your bonus!",
                "Stuck? Tap the gift branch for a bonus spot.",
                "Look, a bird followed you home!",
                "Tap your bird for a closer look.",
                "Welcome, VIP!",
                "Ready to remove ads and support the game? Tap the VIP button anytime.",
                "Are you sure you want to restart?",
                "Keep multiplier (Watch Ad / No thanks).",
                "TAP A CARD TO KEEP",
                "Pick your bet, then tap Deal.",
                "Tap a card to hold it, then tap Draw.",
                "Pick your bet, then tap Deal. (later visit)",
                "Tap the back button to return home.",
                "Tap a branch to pick up its top birds.",
                "Now tap a branch with the same color on top, or an empty one.",
                "Second guided hop (glove on the next branch).",
                "Those birds have nowhere to go yet. Try another branch.",
                BadgerCopy.LessonAt(0),
                BadgerCopy.LessonAt(1),
                BadgerCopy.LessonAt(2),
                BadgerCopy.GuideLine(1),
                BadgerCopy.GuideLine(2),
                BadgerCopy.GuideLine(3),
            };
            var all = new string[steps.Length + extra.Length];
            for (int i = 0; i < steps.Length; i++)
                all[i] = CoachTap.Caption(steps[i]);
            for (int i = 0; i < extra.Length; i++)
                all[steps.Length + i] = extra[i];
            return all;
        }

        static CaptionLayout.CaptionFit FitLessonText(string text, float textW, float textH, int lo, int hi, float screenW, float screenH)
        {
            text = CaptionLayout.LessonShown(text, CoachLineStyle().font);
            lo = CaptionPx(lo);
            hi = CaptionPx(hi);
            int floor = LessonCaptionFloor(screenW, screenH);
            if (lo < floor) lo = floor;
            // The floor is the lesson size. A tall seat must not grow the type.
            if (hi > floor) hi = floor;
            if (hi < lo) hi = lo;
            return CaptionLayout.FitFloor(CoachLineStyle(), text, textW, textH, lo, hi);
        }

        public struct LessonPose
        {
            public Rect Seat;
            public Rect Plate;
            public Rect Glove;
            public Rect Title;
            public Rect Body;
            public Rect Flavor;
            public Rect Honey;
            public Vector2 Aim;
            public float Approach;
            public bool Clear;
        }

        // Album lessons that point at a card. Step 1 is a grid sleeve, 2 is the
        // inspect front, 3 is the inspect back (honey icons).
        public static LessonPose PoseLesson(int step, Rect focus, float screenW, float screenH, string line)
        {
            bool inspect = step >= 2;
            bool honeyOn = step == 3;
            CardText.LessonBands(focus, inspect, honeyOn, honeyOn, out var title, out var body, out var flavor, out var icons);
            var text = new CoachSeat.FocusText { Title = title, Body = body, Flavor = flavor };
            float s = Mathf.Max(screenH / 720f, 1f);
            var hang = CaptionLayout.BoxPad(screenH);
            // Seat is the box PaintCoachCaption insets. Fit the inner width once
            // so the paint does not wrap again and grow off the bottom.
            float seatW = LessonSeatWidth(screenW, focus.width);
            float seatH = LessonSeatHeight(line, screenW, screenH, seatW);
            float gap = hang.y + 10f;
            var seat = CoachSeat.PlaceCaption(screenW, screenH, focus, seatW, seatH, text, gap);
            var plate = new Rect(seat.x - hang.x, seat.y - hang.y, seat.width + hang.x * 2f, seat.height + hang.y * 2f);
            ResolveLessonGlove(focus, icons, plate, text, s, screenW, screenH, out var aim, out float approach, out var glove);
            bool clear = !text.Hits(plate) && !text.Hits(glove);
            return new LessonPose
            {
                Seat = seat,
                Plate = plate,
                Glove = glove,
                Title = title,
                Body = body,
                Flavor = flavor,
                Honey = icons,
                Aim = aim,
                Approach = approach,
                Clear = clear,
            };
        }

        public static float LessonSeatWidth(float screenW, float focusW)
        {
            float seatW = Mathf.Min(screenW - 16f, Mathf.Max(focusW, screenW * 0.92f));
            if (seatW < 120f) seatW = Mathf.Min(screenW - 16f, 120f);
            return seatW;
        }

        // Outer seat whose CaptionTextRect is exactly the width and height the
        // floor fit used. PaintCoachCaption insets this same way.
        public static float LessonSeatHeight(string line, float screenW, float screenH, float seatW)
        {
            float s = Mathf.Max(screenH / 720f, 1f);
            float padX = Mathf.Clamp(seatW * 0.055f, 10f * s, 18f * s);
            float textW = Mathf.Max(48f, seatW - padX * 2f);
            var fit = FitLessonText(line, textW, 4096f, 12, Mathf.RoundToInt(32f * s), screenW, screenH);
            float padY = 14f * s;
            float seatH = fit.TextH + padY * 2f;
            padY = Mathf.Clamp(seatH * 0.10f, 6f * s, 14f * s);
            return fit.TextH + padY * 2f;
        }

        // How far the painted plate hangs below the card's bottom.
        public static float LessonPlateDrop(float screenH, float seatH)
        {
            float hangY = CaptionLayout.BoxPad(screenH).y;
            return hangY + 10f + seatH + hangY + 8f;
        }

        public static Rect AlbumInspectRect(float screenW, float screenH)
        {
            var big = CardText.InspectCard(screenW, screenH);
            return new Rect((screenW - big.x) * 0.5f, (screenH - big.y) * 0.42f, big.x, big.y);
        }

        public static Rect AlbumGridRect(float screenW, float screenH, Rect safe, int slot)
        {
            float shell = CardText.ShellBottom(screenW, screenH, safe);
            var grid = CardText.GridOf(screenW, screenH, safe, shell);
            int col = slot % 3;
            int row = slot / 3;
            float cx = grid.Gx0 + col * (grid.CellW + grid.Gap) + grid.CellW * 0.5f;
            float cy = grid.Gy0 + row * (grid.CellH + grid.Gap) + grid.CellH * 0.5f;
            return new Rect(cx - grid.CardW * 0.5f, cy - grid.CardH * 0.5f, grid.CardW, grid.CardH);
        }

        // Landed glove: fingertip on the aim, same rotation DrawGloveAt uses.
        public static Rect GloveAabb(Vector2 pivot, float ang, float s, bool mirror)
        {
            float dh = GlovePx * s;
            GloveSpan(pivot, ang, dh, 0f, mirror, out float x0, out float y0, out float x1, out float y1);
            return new Rect(x0, y0, Mathf.Max(1f, x1 - x0), Mathf.Max(1f, y1 - y0));
        }

        // Below the focus (finger up), then above (finger down), then the side.
        static void ResolveLessonGlove(Rect focus, Rect honey, Rect plate, CoachSeat.FocusText text, float s,
            float screenW, float screenH, out Vector2 aim, out float approach, out Rect glove)
        {
            bool haveHoney = honey.width > 2f && honey.height > 2f;
            var band = haveHoney ? honey : focus;
            float[] angs = { 0f, 180f, -90f, 90f };
            Vector2[] aims = haveHoney
                ? new[]
                {
                    new Vector2(band.center.x, band.center.y),
                    new Vector2(band.center.x + band.width * 0.22f, band.center.y),
                    new Vector2(band.xMax - 12f, band.center.y),
                    new Vector2(band.center.x, band.yMax - 8f),
                    new Vector2(band.center.x, band.y + 8f),
                }
                : new[]
                {
                    new Vector2(focus.center.x, focus.yMax - 8f),
                    new Vector2(focus.center.x, focus.y + 8f),
                    new Vector2(focus.xMax - 8f, focus.y + focus.height * 0.16f),
                    new Vector2(focus.x + 8f, focus.y + focus.height * 0.16f),
                    focus.center,
                };
            aim = aims[0];
            approach = angs[0];
            glove = GloveAabb(aim, approach, s, false);
            bool found = false;
            for (int a = 0; a < angs.Length && !found; a++)
            {
                for (int i = 0; i < aims.Length; i++)
                {
                    var g = GloveAabb(aims[i], angs[a], s, false);
                    if (text.Hits(g) || CoachSeat.FocusText.Over(g, plate)) continue;
                    if (g.xMax < 4f || g.yMax < 4f || g.xMin > screenW - 4f || g.yMin > screenH - 4f) continue;
                    aim = aims[i];
                    approach = angs[a];
                    glove = g;
                    found = true;
                    break;
                }
            }
            if (found) return;
            // Last push: walk the side pose until the text and the plate are clear.
            approach = -90f;
            aim = new Vector2(band.xMax - 8f, band.center.y);
            for (int n = 0; n < 16; n++)
            {
                glove = GloveAabb(aim, approach, s, false);
                if (!text.Hits(glove) && !CoachSeat.FocusText.Over(glove, plate)) return;
                float dx = 16f * s;
                float dy = 0f;
                if (CoachSeat.FocusText.Over(glove, text.Flavor) || CoachSeat.FocusText.Over(glove, text.Title))
                    dy += 12f * s;
                if (CoachSeat.FocusText.Over(glove, text.Body))
                    dy += 12f * s;
                if (CoachSeat.FocusText.Over(glove, plate))
                    dy -= 12f * s;
                aim += new Vector2(dx, dy);
            }
            glove = GloveAabb(aim, approach, s, false);
        }

        public static void GardenCorners(float screenW, float screenH, Rect safe, out Rect restart, out Rect hive)
        {
            CornerAnchor.Garden(screenW, screenH, safe, out _, out _, out _, out restart, out hive);
        }

        public static void GardenCornersShaken(float screenW, float screenH, Rect safe, Vector2 offset, float twist,
            out Rect restart, out Rect hive)
        {
            GardenCorners(screenW, screenH, safe, out restart, out hive);
            var pivot = new Vector2(screenW * 0.5f, screenH * 0.5f);
            restart = CornerAnchor.Shake(restart, offset, twist, pivot);
            hive = CornerAnchor.Shake(hive, offset, twist, pivot);
        }

        bool _shotBeeLesson;
        bool _shotCardLesson;
        bool _shotGlovePin;
        int _shotCardSlot = -1;

#if UNITY_EDITOR
        void ClearShotModals()
        {
            _tutorPause = 0;
            _coach = false;
            _leafIntro = false;
            _cueGift = false;
            _adHand = false;
            _pestCue = 0;
            _adoptLive = false;
            _welcomeGlove = false;
            _welcomeOpen = false;
            _welcomeQueued = false;
            _pokerPageOn = false;
            _pokerDealHint = false;
            _bgLessonLive = false;
            _dailyIntroLive = false;
            _dailyOpen = false;
            _dailyAskOpen = false;
            _hiveIntroLive = false;
            _pokerIntroLive = false;
            _streakSlide = -1f;
            _rewardGap = false;
            _gift = GiftFace.None;
            _resumeInputUntil = -1f;
            VipOffer.Close();
        }

        void ApplyShotLesson()
        {
            if (_shotBeeLesson)
            {
                ClearShotModals();
                _splash = false;
                _home = HomeFace.Splash;
                _hiveLevelCue = true;
                _hiveLevelLive = true;
                _cueHand = true;
                _albumTutorOn = false;
                _upgradeTutorOn = false;
                _hiveTutorOn = false;
                if (_cueLine != HiveIntroLine) CueLine(HiveIntroLine);
                _coachFade = 1f;
                _shotGlovePin = true;
                return;
            }
            if (!_shotCardLesson) return;
            _shotBeeLesson = false;
            ClearShotModals();
            _hiveLevelLive = false;
            _hiveLevelCue = false;
            _cueHand = false;
            _hiveTutorOn = false;
            _upgradeTutorOn = false;
            _splash = true;
            _home = HomeFace.Hive;
            _hivePageTurn = 99f;
            _albumOpenFrames = 8;
            _albumOpenAt = PlayClock.Now - 1f;
            if (_shotCardSlot < 0)
            {
                _shotCardSlot = 0;
                for (int k = 0; k < Hive.AlbumSlots; k++)
                {
                    if (Hive.CountOfSlot(k) <= 0) continue;
                    _shotCardSlot = k;
                    break;
                }
            }
            int ix = _shotCardSlot;
            if (_hiveInspect != ix)
            {
                _hiveInspect = ix;
                _hiveInspectFrom = AlbumInspectRect(Screen.width > 2f ? Screen.width : 1179f, Screen.height > 2f ? Screen.height : 2556f);
            }
            _hiveInspectT = 1f;
            _hiveInspectClosing = false;
            _hiveFlip = -1;
            if ((uint)ix < (uint)_hiveFaceBack.Length) _hiveFaceBack[ix] = true;
            _albumTutorOn = true;
            _albumTutorStep = 3;
            _albumTutorFlipped = true;
            _coachFade = 1f;
            _shotGlovePin = true;
        }
#endif
    }
}

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
            float s = DeviceFit.UiH(screenH);
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
            float s = DeviceFit.UiH(screenH);
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
            // b87: wide screens (iPad) ease the caption down with the shared device scale.
            float tab = screenW > 2f && screenH > 2f ? DeviceFit.Ui(screenW, screenH) / Mathf.Max(screenH / 720f, 1f) : 1f;
            int floor = Mathf.RoundToInt(CaptionFloorPhonePx * (h / CaptionFloorPhoneH) * tab);
            if (floor < 1) floor = 1;
            int old = CaptionPx(CaptionFloorRef);
            if (floor < old) floor = old;
            return floor;
        }

        // Wrapped line count at the lesson floor, in the shared column.
        public static int LessonCaptionLines(string text, float screenW, float screenH)
        {
            float s = DeviceFit.UiH(screenH);
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
            float s = DeviceFit.UiH(screenH);
            var fit = FitLessonText(text, textW, textH, 12, Mathf.RoundToInt(32f * s), screenW, screenH);
            return fit.Font;
        }

        // Painted plate of a standard caption: the same wrap and font the coach
        // fits, then CaptionLayout.MeasureSnug so the box hugs the lines.
        public static CaptionLayout.SnugPlate LessonCaptionFit(string text, float screenW, float screenH)
        {
            float s = DeviceFit.UiH(screenH);
            float seatW = LessonColumnWidth(screenW, screenH);
            float padX = Mathf.Clamp(seatW * 0.055f, 10f * s, 18f * s);
            float textW = Mathf.Max(48f, seatW - padX * 2f);
            var fit = FitLessonText(text, textW, 4096f, 12, Mathf.RoundToInt(32f * s), screenW, screenH);
            var hang = CaptionLayout.BoxPad(screenH);
            return CaptionLayout.MeasureSnug(CoachCaptionStyle(), fit.Shown, fit.Font, hang.x, hang.y);
        }

        // Same floor and style PaintCoachCaption uses, measured inside the safe width.
        public static CaptionLayout.GoldFit GoldCaption(string text, float screenW, float screenH)
        {
            int floor = LessonCaptionFloor(screenW, screenH);
            return CaptionLayout.FitGold(CoachLineStyle(), text, screenW, screenH, floor, floor);
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
                CaptionLayout.LeafLine,
                CaptionLayout.SparrowLine,
                CaptionLayout.HawkLine,
                CaptionLayout.BeePestLine,
                "Tap a card you have\nto enlarge it.",
                "Tap again to flip.",
                CaptionLayout.AlbumUpgradeLine,
                "Swipe to turn the page!\nOr tap a page number.",
                "Tap a card to take\na closer look!",
                "Tap to unlock your bonus!",
                "Stuck? Tap the gift branch for a bonus spot.",
                CaptionLayout.AdoptGreetLine,
                "Tap your bird for a closer look.",
                "Welcome, VIP!",
                CaptionLayout.VipPitchLine,
                ClearDoubleLine,
                VipSoftLine,
                "Are you sure you want to restart?",
                "Keep multiplier (Watch Ad / No thanks).",
                "TAP A CARD TO KEEP",
                "Pick your bet, then tap Deal.",
                "Tap a card to hold it, then tap Draw.",
                "Pick your bet, then tap Deal. (later visit)",
                "Tap the back button to return home.",
                "Tap a branch to pick up its top birds.",
                CaptionLayout.CoachPlaceLine,
                "Second guided hop (glove on the next branch).",
                CaptionLayout.CoachNowhereLine,
                BadgerCopy.LessonAt(0),
                BadgerCopy.LessonAt(1),
                BadgerCopy.LessonAt(2),
                BadgerCopy.GuideLine(1),
                BadgerCopy.GuideLine(2),
                BadgerCopy.GuideLine(3),
                BadgerCopy.OpenLater,
                BadgerCopy.GoesFirst,
                BadgerCopy.HowMany,
                BadgerCopy.YourPick,
                BadgerCopy.PowersOpen,
                BadgerCopy.DontCareWasted,
                BadgerCopy.DontCareHalved,
                BadgerCopy.Tie,
                BadgerCopy.MineNow,
                BadgerCopy.CoinsShort,
                BadgerCopy.Win,
                BadgerCopy.Lose,
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
            float goldW = CaptionLayout.GoldTextWidth(CoachLineStyle(), screenW, screenH, hi);
            if (textW > goldW) textW = goldW;
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
        public static LessonPose PoseLesson(int step, Rect focus, float screenW, float screenH, string line, int honey = -1)
        {
            if (honey < 0) honey = step == 3 ? 5 : 0;
            bool inspect = step >= 2;
            bool honeyOn = step == 3;
            CardText.LessonBands(focus, inspect, honeyOn, honeyOn, out var title, out var body, out var flavor, out var icons);
            var text = new CoachSeat.FocusText { Title = title, Body = body, Flavor = flavor };
            float s = DeviceFit.UiH(screenH);
            var hang = CaptionLayout.BoxPad(screenH);
            // Seat is the box PaintCoachCaption insets. Fit the inner width once
            // so the paint does not wrap again and grow off the bottom.
            float seatW = LessonSeatWidth(screenW, focus.width);
            float seatH = LessonSeatHeight(line, screenW, screenH, seatW);
            float gap = hang.y + 10f;
            var seat = CoachSeat.PlaceCaption(screenW, screenH, focus, seatW, seatH, text, gap);
            var plate = new Rect(seat.x - hang.x, seat.y - hang.y, seat.width + hang.x * 2f, seat.height + hang.y * 2f);
            Rect[] cells = null;
            int cellN = 0;
            if (honeyOn && honey > 0 && icons.width > 2f)
            {
                cells = new Rect[5];
                cellN = CardText.HoneyCells(icons, honey, inspect, cells);
            }
            ResolveLessonGlove(focus, icons, plate, text, s, screenW, screenH, cells, cellN, out var aim, out float approach, out var glove);
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
            float s = DeviceFit.UiH(screenH);
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

        // Opaque hand. The texture's transparent lead past the fingertip is clipped
        // off, so a pivot on a comb edge does not count as covering the comb.
        public static Rect GloveBody(Vector2 pivot, float ang, float s, bool mirror)
        {
            float dh = GlovePx * s;
            var tex = CoachGloveTex();
            float dw = tex == null ? dh * (220f / 300f) : dh * (tex.width / (float)Mathf.Max(1, tex.height));
            float left = pivot.x - _gloveTipU * dw;
            float top = pivot.y - (1f - _gloveTipV) * dh;
            float widen = 1f;
            float squash = 1f;
            float sx = mirror ? -widen : widen;
            float rad = ang * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            var finger = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
            float minX = 0f, minY = 0f, maxX = 0f, maxY = 0f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? left : left + dw;
                float y = (i & 2) == 0 ? top : top + dh;
                float dx = (x - pivot.x) * sx;
                float dy = (y - pivot.y) * squash;
                float rx = pivot.x + dx * c - dy * sn;
                float ry = pivot.y + dx * sn + dy * c;
                float along = (rx - pivot.x) * finger.x + (ry - pivot.y) * finger.y;
                if (along > 0f)
                {
                    rx -= finger.x * along;
                    ry -= finger.y * along;
                }
                if (i == 0)
                {
                    minX = maxX = rx;
                    minY = maxY = ry;
                }
                else
                {
                    if (rx < minX) minX = rx;
                    if (rx > maxX) maxX = rx;
                    if (ry < minY) minY = ry;
                    if (ry > maxY) maxY = ry;
                }
            }
            return new Rect(minX, minY, Mathf.Max(1f, maxX - minX), Mathf.Max(1f, maxY - minY));
        }

        // Full plate (icon included) just above the hive, in the lower garden.
        public static Rect BeeCaptionSeat(float screenW, float screenH, Rect hive, float plateW, float plateH, float iconW)
        {
            float fullW = plateW + (iconW > 0f ? iconW : 0f);
            float fullH = plateH > 8f ? plateH : 8f;
            if (fullW < 8f) fullW = 8f;
            float margin = CaptionLayout.SafeMargin(screenW, screenH);
            float gap = screenH * 0.008f;
            if (gap < 10f) gap = 10f;
            float x = hive.xMin - gap - fullW;
            float y = hive.yMin - gap - fullH;
            if (x < margin) x = margin;
            if (y < margin) y = margin;
            if (x + fullW > screenW - margin) x = screenW - margin - fullW;
            if (y + fullH > screenH - margin) y = screenH - margin - fullH;
            if (x < margin) x = margin;
            if (y < margin) y = margin;
            var seat = new Rect(x, y, fullW, fullH);
            if (hive.width > 2f && seat.Overlaps(hive))
            {
                seat.x = hive.xMin - gap - fullW;
                seat.y = hive.yMin - gap - fullH;
                if (seat.x < margin) seat.x = margin;
                if (seat.y < margin) seat.y = margin;
            }
            return seat;
        }

        // Outer seat whose CaptionTextRect is `text`. PaintCoachCaption insets this way.
        static Rect SeatAroundText(Rect text, float s)
        {
            if (s < 1f) s = 1f;
            float padX = 10f * s;
            float padY = 6f * s;
            for (int i = 0; i < 3; i++)
            {
                float w = text.width + padX * 2f;
                float h = text.height + padY * 2f;
                padX = Mathf.Clamp(w * 0.055f, 10f * s, 18f * s);
                padY = Mathf.Clamp(h * 0.10f, 6f * s, 14f * s);
            }
            float seatW = text.width + padX * 2f;
            float seatH = text.height + padY * 2f;
            return new Rect(text.center.x - seatW * 0.5f, text.center.y - seatH * 0.5f, seatW, seatH);
        }

        // Honey lesson: fingertip on a comb edge, hand from the side or from below.
        // Other lessons: below the focus, then above, then the side.
        static void ResolveLessonGlove(Rect focus, Rect honey, Rect plate, CoachSeat.FocusText text, float s,
            float screenW, float screenH, Rect[] cells, int cellN, out Vector2 aim, out float approach, out Rect glove)
        {
            if (cellN > 0 && cells != null && TryHoneyGlove(cells, cellN, plate, text, s, screenW, screenH, out aim, out approach, out glove))
                return;
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

        // Fingertip on the comb edge. The body stays on the hand side of that edge,
        // so the combs and their drips stay clear. Side first, then below.
        static bool TryHoneyGlove(Rect[] cells, int n, Rect plate, CoachSeat.FocusText text, float s,
            float screenW, float screenH, out Vector2 aim, out float approach, out Rect glove)
        {
            aim = cells[n - 1].center;
            approach = -90f;
            glove = GloveBody(aim, approach, s, false);
            var right = cells[n - 1];
            var left = cells[0];
            var low = cells[0];
            for (int i = 1; i < n; i++)
                if (cells[i].yMax > low.yMax) low = cells[i];
            Vector2[] aims =
            {
                new Vector2(right.xMax + 1f, right.center.y),
                new Vector2(right.xMax + 1f, right.yMax - 2f),
                new Vector2(left.xMin - 1f, left.center.y),
                new Vector2(left.xMin - 1f, left.yMax - 2f),
                new Vector2(low.center.x, low.yMax + 1f),
            };
            float[] angs = { -90f, -90f, 90f, 90f, 0f };
            for (int i = 0; i < aims.Length; i++)
            {
                var g = GloveBody(aims[i], angs[i], s, false);
                if (CombHit(g, cells, n)) continue;
                if (text.Hits(g) || CoachSeat.FocusText.Over(g, plate)) continue;
                if (g.xMax < 4f || g.yMax < 4f || g.xMin > screenW - 4f || g.yMin > screenH - 4f) continue;
                aim = aims[i];
                approach = angs[i];
                glove = g;
                return true;
            }
            // Stay on the right edge and walk down until the card text is clear.
            aim = new Vector2(right.xMax + 1f, right.center.y);
            approach = -90f;
            for (int k = 0; k < 14; k++)
            {
                glove = GloveBody(aim, approach, s, false);
                if (!CombHit(glove, cells, n) && !text.Hits(glove)) return true;
                aim.y += 16f * s;
            }
            glove = GloveBody(aim, approach, s, false);
            return !CombHit(glove, cells, n);
        }

        static bool CombHit(Rect glove, Rect[] cells, int n)
        {
            for (int i = 0; i < n; i++)
                if (glove.Overlaps(cells[i])) return true;
            return false;
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
        bool _shotBirdHome;
        bool _shotDailyCaption;
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
            if (_shotCardLesson)
            {
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
                return;
            }
            if (_shotBirdHome)
            {
                ClearShotModals();
                _shotBirdHome = true;
                _splash = true;
                _home = HomeFace.Splash;
                if (_adoptStep != AdoptStep.Fly)
                    BeginAdopt();
                _adoptLive = true;
                _adoptGreetDone = false;
                _coachFade = 1f;
                return;
            }
            if (!_shotDailyCaption) return;
            ClearShotModals();
            _shotDailyCaption = true;
            _splash = true;
            _home = HomeFace.Splash;
            _dailyIntro = true;
            _dailyIntroLive = true;
            _dailyOpen = false;
            _dailyAskOpen = false;
            _coachFade = 1f;
        }
#endif
    }
}

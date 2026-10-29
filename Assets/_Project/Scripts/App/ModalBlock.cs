using UnityEngine;

namespace FlockFive
{
    // One input veil for every modal and every forced-glove lesson.
    // The wash paints at BlockerSort, one under the popup (PopupSort), and the
    // controls behind it ask BlocksPoint. Popup buttons do not ask: they are
    // drawn after the wash and keep the tap. A forced lesson is the same veil
    // with a hole on the glove's target. There is no per-popup blocker.
    public static class ModalBlock
    {
        public const int PopupSort = 100;
        public const int BlockerSort = PopupSort - 1;

        public enum Id
        {
            None = 0,
            Att,
            Consent,
            Settings,
            Daily,
            DailyAsk,
            Welcome,
            Vip,
            VipSoft,
            ClearDouble,
            Streak,
            AvatarRename,
            Gift,
            Restart,
            ClearReward,
            Adopt,
            Inspect,
            PokerPay,
            Lesson,
            PokerFives, // b119 Five of a Kind board
        }

        public struct Veil
        {
            public Id Owner;
            public bool Open;
            public Rect Screen;
            public Rect Cutout;
            public int Sort;
        }

        public struct LessonHit
        {
            public bool Deliver;
            public bool Advance;
        }

        public static Id[] Popups()
        {
            var all = (Id[])System.Enum.GetValues(typeof(Id));
            int n = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != Id.None && all[i] != Id.Lesson) n++;
            var pop = new Id[n];
            int w = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == Id.None || all[i] == Id.Lesson) continue;
                pop[w++] = all[i];
            }
            return pop;
        }

        public static bool HasCutout(Rect cut)
        {
            return cut.width >= 2f && cut.height >= 2f;
        }

        public static Veil Raise(Id id, float w, float h)
        {
            return Raise(id, w, h, default);
        }

        public static Veil Raise(Id id, float w, float h, Rect cutout)
        {
            if (w < 2f) w = 2f;
            if (h < 2f) h = 2f;
            return new Veil
            {
                Owner = id,
                Open = id != Id.None,
                Screen = new Rect(0f, 0f, w, h),
                Cutout = HasCutout(cutout) ? cutout : default,
                Sort = BlockerSort,
            };
        }

        // Forced lesson. A missing target is a solid veil (fail closed) until the glove arms.
        public static Veil RaiseLesson(float w, float h, Rect target)
        {
            return Raise(Id.Lesson, w, h, target);
        }

        public static Veil Dismiss(Veil veil)
        {
            veil.Open = false;
            veil.Owner = Id.None;
            veil.Cutout = default;
            veil.Sort = 0;
            return veil;
        }

        public static bool BlocksPoint(Veil veil, Vector2 p)
        {
            if (!veil.Open) return false;
            if (veil.Screen.width >= 2f && veil.Screen.height >= 2f && !veil.Screen.Contains(p))
                return false;
            if (HasCutout(veil.Cutout) && veil.Cutout.Contains(p)) return false;
            return true;
        }

        // A missing or tiny control is not a button, so it is not blocked.
        public static bool BlocksControl(Veil veil, Rect control)
        {
            if (control.width < 2f || control.height < 2f) return false;
            return BlocksPoint(veil, control.center);
        }

        public static bool Tap(Veil veil, Rect control)
        {
            if (control.width < 2f || control.height < 2f) return false;
            return !BlocksControl(veil, control);
        }

        // Wash under the card. Lesson veils do not dim the garden.
        public static void DrawUnder(Veil veil, Color tint)
        {
            if (!veil.Open || veil.Owner == Id.Lesson) return;
            if (tint.a < 0.004f || Texture2D.whiteTexture == null) return;
            var screen = veil.Screen;
            if (screen.width < 2f || screen.height < 2f)
                screen = new Rect(0f, 0f, PhoneArea.W, PhoneArea.H);
            var prev = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        public static void Paint(Id id, Color tint)
        {
            float w = PhoneArea.W > 2f ? PhoneArea.W : 2f;
            float h = PhoneArea.H > 2f ? PhoneArea.H : 2f;
            DrawUnder(Raise(id, w, h), tint);
            GUI.color = Color.white;
        }

        // After the popup's own buttons. An event they already used is left alone.
        public static void SwallowRest(Veil veil)
        {
            var e = Event.current;
            if (e == null || !veil.Open) return;
            var t = e.type;
            if (t != EventType.MouseDown && t != EventType.MouseUp && t != EventType.MouseDrag)
                return;
            if (!BlocksPoint(veil, e.mousePosition)) return;
            if (t == EventType.MouseUp) GUIUtility.hotControl = 0;
            e.Use();
        }

        // Forced: deliver and advance only on the hole (or a world hit the caller already matched).
        // Informational: the tap is delivered and the lesson does not require the hole.
        public static LessonHit ResolveLesson(bool forced, Rect screen, Rect target, Vector2 point, bool cutoutHit)
        {
            if (!forced)
                return new LessonHit { Deliver = true, Advance = false };
            bool on = cutoutHit || (HasCutout(target) && target.Contains(point));
            if (!on) return default;
            if (screen.width >= 2f && screen.height >= 2f && !cutoutHit && !screen.Contains(point))
                return default;
            return new LessonHit { Deliver = true, Advance = true };
        }
    }
}

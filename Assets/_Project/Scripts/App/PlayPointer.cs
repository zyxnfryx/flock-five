using UnityEngine;

namespace FlockFive
{
    // The portrait play canvas. iPhone is this canvas. On iPad the same canvas
    // sits in the middle and the garden painting overfills the side bars.
    // Those bars are not buttons.
    //
    // Every raw pointer (mouse, touch, IMGUI event, editor inject) is converted
    // here, once, into play-canvas pixels: x in 0..PhoneArea.W, y down.
    // Hit tests read At. They do not add or subtract the pillar.
    public static class PlayPointer
    {
        // The mouse is not a touch. Touch ids are >= 0. -1 is only a missing id.
        public const int MouseId = -3;

        public static bool Live;
        public static bool Down;
        public static bool Up;
        public static int Id;
        public static Vector2 At;

        // Bottom-origin play pixels for the garden camera. Y only. The pillar
        // was already removed in Enter.
        public static Vector2 PlayBottom => new Vector2(At.x, PhoneArea.H - At.y);

        public static bool Inside(Vector2 play)
        {
            return play.x >= 0f && play.y >= 0f && play.x <= PhoneArea.W && play.y <= PhoneArea.H;
        }

        // The only pillar shift. Window GUI is top-origin and includes the bars.
        public static Vector2 Enter(Vector2 windowTop)
        {
            return new Vector2(windowTop.x - PhoneArea.OffX, windowTop.y);
        }

        public static void CaptureWindow(Vector2 windowTop, bool down, bool up, int id)
        {
            At = Enter(windowTop);
            Live = true;
            Down = down;
            Up = up;
            Id = id;
        }

        // The only Event.mousePosition read.
        // Consumers call this instead of reading Event.mousePosition.
        public static Vector2 Gui(Event e)
        {
            CaptureEvent(e);
            return At;
        }

        public static void CaptureEvent(Event e)
        {
            if (e == null)
            {
                Live = false;
                Down = false;
                Up = false;
                return;
            }
            bool down = e.type == EventType.MouseDown && e.button == 0;
            bool up = e.type == EventType.MouseUp && e.button == 0;
            bool drag = e.type == EventType.MouseDrag;
            if (!down && !up && !drag && !e.isMouse)
            {
                Live = false;
                return;
            }
            CaptureWindow(e.mousePosition, down, up, 0);
        }

        public static void CaptureScreenBottom(Vector2 screenBottom, bool down, bool up, int id)
        {
            CaptureWindow(new Vector2(screenBottom.x, PhoneArea.H - screenBottom.y), down, up, id);
        }

#if UNITY_EDITOR
        public static void CaptureInject()
        {
            var p = FlockFiveApp.EditorPointerInject;
            if (!p.On)
            {
                Live = false;
                return;
            }
            CaptureWindow(p.Window, p.Down, p.Up, p.Id);
        }
#endif
    }
}

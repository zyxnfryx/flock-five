using UnityEngine;

namespace FlockFive
{
    // Finish name on the cream plate and on the binder column.
    // Inverse Rainbow used to be a light magenta that sat on the foil wash.
    public static class FinishInk
    {
        public static string Label(BeeFinish finish)
        {
            if (finish == BeeFinish.Holo) return "Holo";
            if (finish == BeeFinish.InverseRainbow) return "Inverse Rainbow";
            return "";
        }

        public static Color Tag(BeeFinish finish)
        {
            if (finish == BeeFinish.InverseRainbow)
                return new Color(0.28f, 0.04f, 0.14f, 1f);
            if (finish == BeeFinish.Holo)
                return new Color(0.08f, 0.32f, 0.72f, 1f);
            return new Color(0.16f, 0.07f, 0.02f, 1f);
        }
    }
}

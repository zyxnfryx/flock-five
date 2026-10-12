using UnityEngine;

namespace FlockFive
{
    // LEVEL flower polish. Paint only: nothing here reads input, gates the press or
    // waits on a clock, so the sink and Load still fire on the frame the tap lands.
    public sealed partial class FlockFiveApp
    {
        // Idle heartbeat period. Slow enough to read as breathing, not a blink.
        const float LevelBeat = 3.4f;

        // Glints that orbit the clay petals (outside the disc, so the caption stays clean).
        static readonly float[] LevelOrbitSpeed = { 0.52f, -0.41f, 0.67f };
        static readonly float[] LevelOrbitPhase = { 0.3f, 2.4f, 4.4f };
        static readonly Color[] LevelOrbitTint =
        {
            new Color(1f, 0.93f, 0.70f, 1f),
            new Color(1f, 0.82f, 0.46f, 1f),
            new Color(1f, 0.80f, 0.70f, 1f),
        };

        // Idle life for the LEVEL flower: a soft double-beat swell in the halo, three
        // comet glints orbiting the petals, and a rim catch-light once per beat.
        // Layered on top of the shared halo/shimmer; scale stays 1 (no quiver).
        static void DrawLevelIdleLife(Rect visual)
        {
            if (!GuiPaint() || visual.width < 16f || visual.height < 16f) return;
            float t = Time.unscaledTime;
            float w = visual.width;
            float h = visual.height;
            var c = new Vector2(visual.center.x, visual.y + h * 0.47f);

            // Heartbeat: lub-dub then a long rest. Gaussian bumps ease in and out.
            float u = Mathf.Repeat(t, LevelBeat) / LevelBeat;
            float d1 = (u - 0.08f) / 0.035f;
            float d2 = (u - 0.19f) / 0.045f;
            float beat = Mathf.Exp(-d1 * d1) + 0.6f * Mathf.Exp(-d2 * d2);
            if (beat > 0.02f)
            {
                float pad = w * (0.10f + 0.06f * beat);
                SparkleFx.DrawGlow(c, w + pad * 2f, new Color(1f, 0.80f, 0.42f, 0.20f * beat));
            }

            // Orbiting comets: head plus three fading echoes along the path.
            float rx = w * 0.47f;
            float ry = h * 0.43f;
            for (int k = 0; k < LevelOrbitSpeed.Length; k++)
            {
                float tw = 0.55f + 0.45f * Mathf.Sin(t * 3.1f + k * 1.7f);
                var tint = LevelOrbitTint[k];
                float dir = LevelOrbitSpeed[k] < 0f ? -1f : 1f;
                float head = t * LevelOrbitSpeed[k] + LevelOrbitPhase[k];
                for (int j = 3; j >= 0; j--)
                {
                    float ang = head - dir * j * 0.11f;
                    var p = new Vector2(c.x + Mathf.Cos(ang) * rx, c.y + Mathf.Sin(ang) * ry);
                    float fall = 1f - j * 0.26f;
                    float sz = w * (0.050f + 0.030f * tw) * fall;
                    float a = (0.42f + 0.40f * tw) * fall * fall;
                    if (j == 0)
                        SparkleFx.DrawGlow(p, sz * 2.4f, new Color(tint.r, tint.g, tint.b, 0.26f * a));
                    SparkleFx.DrawAt(p, sz, a, tint);
                }
            }

            // Catch-light: a star blooms on the upper-right rim mid-beat and fades.
            float ping = (u - 0.52f) * LevelBeat;
            if (ping > 0f && ping < 0.42f)
            {
                float g = ping < 0.10f ? ping / 0.10f : 1f - (ping - 0.10f) / 0.32f;
                g = g * g * (3f - 2f * g);
                var at = new Vector2(visual.x + w * 0.80f, visual.y + h * 0.17f);
                SparkleFx.DrawGlow(at, w * 0.30f * g, new Color(1f, 0.92f, 0.66f, 0.35f * g));
                SparkleFx.DrawAt(at, w * (0.10f + 0.12f * g), 0.95f * g, new Color(1f, 0.97f, 0.86f, 1f));
            }
            GUI.color = Color.white;
        }
    }
}

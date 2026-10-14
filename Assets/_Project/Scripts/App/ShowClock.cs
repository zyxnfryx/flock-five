using UnityEngine;

namespace FlockFive
{
    // captureDeltaTime steps Time.deltaTime and leaves Time.unscaledDeltaTime on the wall clock.
    // Hit-stop and the wing flap read this so a 60fps capture holds the same durations as play.
    public static class ShowClock
    {
        public static float Delta
        {
            get
            {
                float cap = Time.captureDeltaTime;
                // Capture steps the bust at 60. Otherwise stay on PlayClock so a
                // resume spike cannot rush the sky or the hit-stop.
                return cap > 0.0001f ? cap : PlayClock.Delta;
            }
        }
    }
}

using UnityEngine;

namespace FlockFive
{
    // How much of the collectible-card motion this device gets. Picked once, stored, with a debug override.
    //   Full   : OnGUI tilt (parallax + glare) AND the real thick-card flip (CardStage: camera -> RenderTexture slab mesh).
    //   Lite   : OnGUI tilt (parallax + glare) and an OnGUI slab flip (squash + brass edge strip). No camera, no RT, no shader.
    //   Static : flat baked card, no tilt, no sheen motion (old GL, reduce motion).
    public enum CardTier { Full = 0, Lite = 1, Static = 2 }

    public static class CardQuality
    {
        const string TierKey = "flockfive.card.tier";          // -1 / missing = auto
        const string ReduceKey = "flockfive.card.reducemotion"; // 1 = user turned motion off
        static CardTier _tier;
        static bool _resolved;
        static bool _demoted;
        static int _slowFrames;

        public static CardTier Tier
        {
            get
            {
                if (!_resolved) Resolve();
                return _tier;
            }
        }

        public static bool ReduceMotion
        {
            get => PlayerPrefs.GetInt(ReduceKey, 0) == 1;
            set { PlayerPrefs.SetInt(ReduceKey, value ? 1 : 0); _resolved = false; }
        }

        // Debug toggle: -1 auto, 0 Full, 1 Lite, 2 Static.
        public static void Override(int tier)
        {
            PlayerPrefs.SetInt(TierKey, tier);
            _demoted = false;
            _resolved = false;
        }

        static void Resolve()
        {
            _resolved = true;
            _tier = ReduceMotion ? CardTier.Static : Pick(
                PlayerPrefs.GetInt(TierKey, -1),
                SystemInfo.supportsRenderTextures,
                SystemInfo.graphicsDeviceType,
                SystemInfo.systemMemorySize,
                SystemInfo.processorCount,
                Application.platform);
            if (_demoted && _tier == CardTier.Full) _tier = CardTier.Lite;
        }

        // Pure so a test can drive it with fake devices.
        public static CardTier Pick(int forced, bool rtOk, UnityEngine.Rendering.GraphicsDeviceType gfx, int ramMb, int cores, RuntimePlatform plat)
        {
            if (forced >= 0 && forced <= 2) return (CardTier)forced;
            if (gfx == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2 || gfx == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return CardTier.Static;
            bool android = plat == RuntimePlatform.Android;
            // Low-end Android: under ~3 GB or a small core count. Keeps tilt and glare (cheap GUI draws), drops the RT stage.
            if (android && (ramMb < 3000 || cores <= 4)) return CardTier.Lite;
            if (!rtOk) return CardTier.Lite;
            return CardTier.Full;
        }

        // The stage asks to drop to Lite when it cannot build (no shader, RT failed, capture blank) or the frame time stays bad.
        public static void Demote()
        {
            _demoted = true;
            _resolved = false;
        }

        // Called once per inspect frame while the stage is up. 40 frames over 1/22 s in a row = too slow, fall back.
        public static void Watchdog(float unscaledDt)
        {
            if (unscaledDt > 1f / 22f) _slowFrames++;
            else _slowFrames = 0;
            if (_slowFrames >= 40) { _slowFrames = 0; Demote(); }
        }
    }
}

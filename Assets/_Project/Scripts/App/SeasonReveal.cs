using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FlockFive
{
    // One path for every backdrop change (desert, winter, spring, and the 40+ rotation).
    // Pure decisions live here. SeasonCrossfade plays them. A change is shown once,
    // the first time that garden is entered; a restart or a later visit does not replay it.
    public static class SeasonReveal
    {
        public const float FadeSeconds = 1.8f;
        public const float DesertFadeSeconds = 2.5f;
        public const float BannerSeconds = 2.05f;
        public const float DesertBannerSeconds = 2.35f;
        const string PrefSeen = "flockfive.season.seen";

        static readonly List<int> _seen = new List<int>(8);
        static bool _seenLoaded;
        static readonly StringBuilder _seenSb = new StringBuilder(64);

        // App writes this each frame from the live lesson. The banner waits on it
        // so a tutorial caption and the welcome line never share the screen.
        public static bool TutorUp;

        // Desert only, while its wipe and banner are still running. Garden lessons
        // and pest intros wait. Level 16 never sets this, so the badger intro does not.
        public static bool HoldsPlay { get; private set; }

        public static bool BannerLive { get; private set; }
        public static float BannerAlpha { get; private set; }
        public static string BannerLine { get; private set; }

        static SeasonCrossfade _owner;

        public static bool Due(int level) => Due(level, Seen(level));

        public static bool Due(int level, bool seen) =>
            GardenSeason.IsChangeLevel(level) && !seen;

        public static float FadeOf(GardenScene scene) =>
            scene == GardenScene.Desert ? DesertFadeSeconds : FadeSeconds;

        public static float BannerOf(GardenScene scene) =>
            scene == GardenScene.Desert ? DesertBannerSeconds : BannerSeconds;

        // Desert holds garden intros only while its own reveal is still playing.
        public static bool BlocksIntros(GardenScene scene, bool playing) =>
            playing && scene == GardenScene.Desert;

        public static string Banner(GardenScene scene)
        {
            switch (scene)
            {
                case GardenScene.Desert: return "Welcome to the Desert";
                case GardenScene.Winter: return "Welcome to the Winter";
                case GardenScene.Spring: return "Welcome to the Spring";
                case GardenScene.Fall: return "Welcome to the Fall";
                default: return "Welcome to the Summer";
            }
        }

        public static bool Seen(int level)
        {
            LoadSeen();
            for (int i = 0; i < _seen.Count; i++)
                if (_seen[i] == level) return true;
            return false;
        }

        public static void Mark(int level)
        {
            LoadSeen();
            for (int i = 0; i < _seen.Count; i++)
                if (_seen[i] == level) return;
            _seen.Add(level);
            _seenSb.Length = 0;
            for (int i = 0; i < _seen.Count; i++)
            {
                if (i > 0) _seenSb.Append(',');
                _seenSb.Append(_seen[i]);
            }
            PlayerPrefs.SetString(PrefSeen, _seenSb.ToString());
            PlayerPrefs.Save();
        }

        internal static void Bind(SeasonCrossfade who, GardenScene scene, bool hold)
        {
            _owner = who;
            BannerLine = Banner(scene);
            BannerAlpha = 0f;
            BannerLive = true;
            HoldsPlay = hold;
        }

        internal static void SetBanner(float alpha)
        {
            BannerAlpha = alpha;
            BannerLive = alpha > 0.02f;
        }

        internal static void ClearIf(SeasonCrossfade who)
        {
            if (_owner != who) return;
            _owner = null;
            HoldsPlay = false;
            BannerLive = false;
            BannerAlpha = 0f;
        }

        static void LoadSeen()
        {
            if (_seenLoaded) return;
            _seenLoaded = true;
            string raw = PlayerPrefs.GetString(PrefSeen, "");
            if (string.IsNullOrEmpty(raw)) return;
            int n = 0;
            bool any = false;
            for (int i = 0; i <= raw.Length; i++)
            {
                bool end = i == raw.Length || raw[i] == ',';
                if (!end)
                {
                    char c = raw[i];
                    if (c >= '0' && c <= '9')
                    {
                        n = n * 10 + (c - '0');
                        any = true;
                    }
                    continue;
                }
                if (any) _seen.Add(n);
                n = 0;
                any = false;
            }
        }
    }

    // Crossfade, flourish, and the one caption. Birds and branches stay where they are.
    // Desert holds play until the banner is gone so a level-15 lesson cannot start early.
    // Any other season keeps play live and queues the banner behind a tutorial caption.
    public sealed class SeasonCrossfade : MonoBehaviour
    {
        static SeasonCrossfade _live;

        SpriteRenderer _old;
        SpriteRenderer _veil;
        BackgroundFitter _oldFit;
        FoliageSway _oldSway;
        float _t;
        float _bannerT = -1f;
        float _fadeDur;
        float _bannerDur;
        float _bannerAt;
        bool _desert;
        bool _horizontal;
        bool _shader;
        bool _retired;
        bool _holdLive;

        public static void RetireLive()
        {
            if (_live == null) return;
            var live = _live;
            _live = null;
            live.Retire();
        }

        public static void Begin(Transform root, SpriteRenderer next, Sprite oldSprite, Camera cam, int level, GardenScene scene, GardenScene prev)
        {
            if (root == null || !SeasonReveal.Due(level))
            {
                // WorldBuilder pins the outgoing painting before this. Do not keep it
                // if the reveal is not going to run.
                if (prev != GardenScene.Summer && oldSprite != null)
                    SpriteCatalog.ReleaseHeld();
                return;
            }
            SeasonReveal.Mark(level);
            // Do not RetireLive here. The caller already did, before TakeHeldScene.
            // Retiring now would unload the painting this wipe is about to draw.
            var go = new GameObject("SeasonReveal");
            go.transform.SetParent(root, false);
            var fade = go.AddComponent<SeasonCrossfade>();
            _live = fade;
            fade.StartReveal(next, oldSprite, cam, scene, prev);
        }

        void StartReveal(SpriteRenderer next, Sprite oldSprite, Camera cam, GardenScene scene, GardenScene prev)
        {
            _desert = scene == GardenScene.Desert;
            _fadeDur = SeasonReveal.FadeOf(scene);
            _bannerDur = SeasonReveal.BannerOf(scene);
            _bannerAt = _fadeDur * (_desert ? 0.38f : 0.28f);
            _horizontal = _desert;
            _shader = FoliageSway.ShaderReady;
            _holdLive = oldSprite != null && prev != GardenScene.Summer;
            bool differ = oldSprite != null && (next == null || oldSprite != next.sprite);
            if (differ)
            {
                var go = WorldBuilder.Sprite("BgPrev", oldSprite, next != null ? next.transform.position : new Vector3(0f, -0.15f, 8f), 1f, -19, transform);
                go.layer = PortraitLock.BleedLayer;
                _old = go.GetComponent<SpriteRenderer>();
                _oldFit = go.AddComponent<BackgroundFitter>();
                _oldFit.Cam = cam;
                _oldFit.FollowCamera = false;
                _oldFit.WorldCenter = new Vector3(0f, -0.15f, 8f);
                _oldFit.WorldSize = new Vector2(13.6f, 24.0f);
                _oldFit.Apply();
                _oldSway = go.AddComponent<FoliageSway>();
                _oldSway.Boot(prev);

                var veilGo = WorldBuilder.Sprite("SeasonVeil", SpriteCatalog.Glow, new Vector3(0f, WorldBuilder.CamRestY, 7.6f), 1f, -17, transform);
                veilGo.layer = PortraitLock.BleedLayer;
                float tall = PortraitLock.TallFactor();
                veilGo.transform.localScale = new Vector3(26f, 32f * tall, 1f);
                _veil = veilGo.GetComponent<SpriteRenderer>();
                _veil.color = new Color(1f, 1f, 1f, 0f);
            }
            Sfx.SeasonArrive(_desert);
            SeasonReveal.Bind(this, scene, _desert);
        }

        void LateUpdate()
        {
            if (_retired) return;
            float dt = PlayClock.Delta;
            _t += dt;
            TickWipe();
            TickBanner(dt);
            bool fadeDone = _t >= _fadeDur;
            bool bannerDone = _bannerT >= _bannerDur;
            if (fadeDone && _old != null)
            {
                _old.enabled = false;
                if (_veil != null) _veil.enabled = false;
            }
            if (fadeDone && bannerDone)
            {
                SeasonReveal.ClearIf(this);
                if (_live == this) _live = null;
                ReleaseHold();
                enabled = false;
            }
        }

        void TickWipe()
        {
            float u = _fadeDur <= 0.01f ? 1f : Mathf.Clamp01(_t / _fadeDur);
            float feather = _desert ? 0.18f : 0.13f;
            if (_old != null)
            {
                if (_shader && _oldSway != null)
                {
                    // Start just below 0 so the first frame is still the old painting.
                    float edge = Mathf.Lerp(-feather, 1f + feather, u);
                    _oldSway.SetWipe(edge, feather, _horizontal);
                    if (_oldFit != null) _oldFit.Alpha = 1f;
                }
                else if (_oldFit != null)
                    _oldFit.Alpha = 1f - u;
            }
            if (_veil != null)
            {
                float flourish = Mathf.Sin(u * Mathf.PI);
                if (_desert)
                    _veil.color = new Color(0.96f, 0.70f, 0.40f, flourish * 0.16f);
                else
                    _veil.color = new Color(1f, 0.97f, 0.92f, flourish * 0.07f);
            }
        }

        void TickBanner(float dt)
        {
            bool wait = !_desert && SeasonReveal.TutorUp;
            if (_bannerT < 0f)
            {
                if (_t < _bannerAt || wait) return;
                _bannerT = 0f;
            }
            else if (!wait)
                _bannerT += dt;
            float a = BannerAlpha(_bannerT, _bannerDur);
            SeasonReveal.SetBanner(a);
        }

        // Fade in, hold, fade out. The whole window is about two seconds.
        static float BannerAlpha(float t, float dur)
        {
            const float inn = 0.32f;
            const float outT = 0.42f;
            if (t <= 0f || dur <= 0.05f) return 0f;
            if (t < inn) return t / inn;
            float tail = dur - outT;
            if (t < tail) return 1f;
            if (t >= dur) return 0f;
            return 1f - (t - tail) / outT;
        }

        void Retire()
        {
            if (_retired) return;
            _retired = true;
            enabled = false;
            if (_old != null) _old.enabled = false;
            if (_veil != null) _veil.enabled = false;
            SeasonReveal.ClearIf(this);
            ReleaseHold();
        }

        void ReleaseHold()
        {
            if (!_holdLive) return;
            _holdLive = false;
            SpriteCatalog.ReleaseHeld();
        }

        void OnDestroy()
        {
            if (_live == this) _live = null;
            ReleaseHold();
            SeasonReveal.ClearIf(this);
        }
    }
}

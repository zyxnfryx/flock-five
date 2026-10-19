using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FlockFive
{
    // One path for every backdrop change (desert, winter, spring, and the 40+ rotation).
    // Pure decisions live here. SeasonCrossfade plays them. A change is shown once,
    // the first time that garden is entered; a restart or a later visit does not replay it.
    // The four seasons play the short SeasonScene (wipe, particles, birds, sting, small label).
    // Desert keeps its own wipe and welcome line. The "Welcome to the <season>" banner is gone.
    public static class SeasonReveal
    {
        public const float FadeSeconds = SeasonScene.WipeEnd;
        public const float DesertFadeSeconds = 2.5f;
        // Season label window (1.6-2.5 s of the scene). Desert keeps its banner time below.
        public const float BannerSeconds = SeasonScene.Duration - SeasonScene.SettleStart;
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
        // True for the small season label, false for the desert welcome plate.
        public static bool BannerSmall { get; private set; }
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
                default: return SeasonScene.Label(scene);
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
            BannerSmall = scene != GardenScene.Desert;
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

    // Crossfade, flourish, and the season scene. Branches stay where they are.
    // Desert holds play until the banner is gone so a level-15 lesson cannot start early.
    // The four seasons run SeasonScene's 2.5 s timeline and never hold play: a tap skips it,
    // a tutorial cuts it, a badger action finishes it (FinishLive).
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
        int _mode;
        GardenScene _scene;
        SeasonClock _clock;
        GardenLife _life;
        BirdIdle[] _birds;
        bool _ended;
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
            // Ostrich gardens never get a scene; none while a tutorial is up. The new painting
            // simply shows (the reveal is spent). Desert is not a season scene and is unaffected.
            bool ostrich = LevelData.Current != null && LevelData.Current.Ostrich;
            var gate = SeasonScene.Gate(scene, ostrich, SeasonScene.TutorialLive);
            if (gate == SeasonScene.Skip.Ostrich || gate == SeasonScene.Skip.Tutorial)
            {
                if (prev != GardenScene.Summer && oldSprite != null)
                    SpriteCatalog.ReleaseHeld();
                return;
            }
            // Do not RetireLive here. The caller already did, before TakeHeldScene.
            // Retiring now would unload the painting this wipe is about to draw.
            var go = new GameObject("SeasonReveal");
            go.transform.SetParent(root, false);
            var fade = go.AddComponent<SeasonCrossfade>();
            _live = fade;
            fade.StartReveal(next, oldSprite, cam, scene, prev);
        }

        // Tap skip: the scene ends now, painting and particles land in their final state.
        public static void SkipLive()
        {
            if (_live != null && _live._clock != null) _live._clock.Skip();
        }

        // Badger actions call this first: the scene finishes (skips to its end state) before
        // the leap or the contest opens, so the two never overlap.
        public static void FinishLive()
        {
            if (_live == null || _live._clock == null) return;
            _live._clock.Skip();
            _live.TickSeason();
        }

        void StartReveal(SpriteRenderer next, Sprite oldSprite, Camera cam, GardenScene scene, GardenScene prev)
        {
            _desert = scene == GardenScene.Desert;
            _scene = scene;
            _fadeDur = SeasonReveal.FadeOf(scene);
            _bannerDur = SeasonReveal.BannerOf(scene);
            _bannerAt = _fadeDur * (_desert ? 0.38f : 0.28f);
            _mode = _desert ? SeasonScene.WipeFromSide : SeasonScene.WipeMode(scene);
            if (!_desert)
            {
                _clock = new SeasonClock();
                _life = transform.parent != null ? transform.parent.GetComponentInChildren<GardenLife>() : null;
                if (_life != null) _life.BeginBurst(scene);
                SeasonScene.Live = true;
            }
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
            if (_desert) Sfx.SeasonArrive(true);
            SeasonReveal.Bind(this, scene, _desert);
        }

        void LateUpdate()
        {
            if (_retired) return;
            if (_clock != null)
            {
                _clock.Tick(PlayClock.Delta, SeasonScene.TutorialLive);
                TickSeason();
                return;
            }
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

        // One frame of the season scene, driven by the clock (also called by FinishLive).
        void TickSeason()
        {
            if (_ended) return;
            _t = _clock.T;
            TickWipe();
            if (_clock.TakeSting())
            {
                // Clean hook: Sfx.SeasonStingHook lets the Musical Producer's stings replace the whoosh.
                Sfx.SeasonSting(_scene);
                if (_scene == GardenScene.Spring) Sfx.SpringChirpDelayed();
            }
            if (_clock.TakeBirds()) CollectBirds();
            TickBirds();
            if (_life != null) _life.SetBurst(SeasonScene.Burst(_t), SeasonScene.Energy(_t));
            SeasonReveal.SetBanner(SeasonScene.LabelAlpha(_t));
            if (_t >= _fadeDur && _old != null)
            {
                _old.enabled = false;
                if (_veil != null) _veil.enabled = false;
            }
            if (_clock.Done) EndScene();
        }

        void CollectBirds()
        {
            var root = transform.parent;
            if (root == null) return;
            _birds = root.GetComponentsInChildren<BirdIdle>(false);
        }

        void TickBirds()
        {
            if (_birds == null) return;
            float phase = SeasonScene.BirdPhase(_t);
            for (int i = 0; i < _birds.Length; i++)
            {
                var b = _birds[i];
                if (b == null) continue;
                if (!b.CanReact || phase < 0f)
                {
                    b.ClearReaction();
                    continue;
                }
                // Neighbours start a hair apart; the last bird still lands at rest by 2.5 s.
                float stagger = Mathf.Min(i * SeasonScene.BirdStagger, 0.3f);
                float u = Mathf.Clamp01((phase - stagger) / (1f - stagger));
                var pose = SeasonScene.BirdPose(_scene, u);
                b.SetReaction(pose.Dx, pose.Dy, pose.Rot, pose.Sx, pose.Sy);
            }
        }

        // Cleanup for every exit (finished, skipped, retired, destroyed). Idempotent.
        void EndScene()
        {
            if (_ended) return;
            _ended = true;
            if (_birds != null)
            {
                for (int i = 0; i < _birds.Length; i++)
                    if (_birds[i] != null) _birds[i].ClearReaction();
                _birds = null;
            }
            if (_life != null) _life.EndBurst();
            if (_old != null) _old.enabled = false;
            if (_veil != null) _veil.enabled = false;
            if (_clock != null)
            {
                SeasonScene.Live = false;
                SeasonReveal.ClearIf(this);
                if (_live == this) _live = null;
                ReleaseHold();
                enabled = false;
            }
        }

        void TickWipe()
        {
            float u = _fadeDur <= 0.01f ? 1f : Mathf.Clamp01(_t / _fadeDur);
            float feather = _desert ? 0.18f : SeasonScene.WipeFeather;
            if (_old != null)
            {
                if (_shader && _oldSway != null)
                {
                    // Start just below 0 so the first frame is still the old painting.
                    float edge = Mathf.Lerp(-feather, 1f + feather, u);
                    _oldSway.SetWipe(edge, feather, _mode);
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
                {
                    // Season palette tint riding the wipe (spring blends pink to green).
                    var tint = Color.Lerp(SeasonScene.PaletteB(_scene), SeasonScene.Palette(_scene), u);
                    tint.a = flourish * SeasonScene.VeilStrength;
                    _veil.color = tint;
                }
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
            EndScene();
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
            EndScene();
            if (_clock != null) SeasonScene.Live = false;
            ReleaseHold();
            SeasonReveal.ClearIf(this);
        }
    }
}

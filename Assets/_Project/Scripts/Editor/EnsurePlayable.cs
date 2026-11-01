#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FlockFive.Editor
{
    [InitializeOnLoad]
    static class EnsurePlayable
    {
        const string PlayCmd = "/tmp/flock-five-play";

        // One batch entry for the edit-mode suites. Unity -executeMethod FlockFive.Editor.EnsurePlayable.RunEditorTests
        public static void RunEditorTests()
        {
            // Suites write PlayerPrefs; keep the real prefs untouched (snapshot + restore).
            using (TestPrefsSandbox.Begin())
                RunEditorSuites();
        }

        static void RunEditorSuites()
        {
            Suite("board", BoardLayoutTests.Run);
            Suite("badger", BadgerFightTests.Run);
            Suite("blackbird", BlackbirdFightTests.Run);
            Suite("badger-show", BadgerShowTests.Run);
            Suite("garden-season", GardenSeasonTests.Run);
            Suite("card-fit", CardTextFitTests.Run);
            Suite("poker-deal", PokerDealGateTests.Run);
            Suite("poker-flush", PokerFlushTests.Run);
            Suite("heal", TutorialHealTests.Run);
            Suite("reset-guard", CoachResetGuardTests.Run);
            Suite("poker-bet", PokerBetTests.Run);
            Suite("discard-anim", PokerDiscardAnimTests.Run);
            Suite("pest", PestScheduleTests.Run);
            Suite("honey-drip", HoneyDripTests.Run);
            Suite("build71", Build71Tests.Run);
            Suite("build73", Build73Tests.Run);
            Suite("stamp", GardenStampVisTests.Run);
            Suite("build75", Build75Tests.Run);
            Suite("build76", Build76Tests.Run);
            Suite("build76-rules", Build76RuleTests.Run);
            Suite("build77-ads", Build77AdsTests.Run);
            Suite("build78", Build78Tests.Run);
            Suite("build79", Build79Tests.Run);
            Suite("build80", Build80Tests.Run);
            Suite("build81", Build81Tests.Run);
            Suite("build81-reward", Build81RewardTests.Run);
            Suite("build81-audio", Build81AudioTests.Run);
            Suite("build82-levels", Build82LevelTests.Run);
            Suite("build82", Build82Tests.Run);
            Suite("build82c", Build82TestsC.Run);
            Suite("build82d", Build82TestsD.Run);
            Suite("build82e", Build82TestsE.Run);
            Suite("build82f-modal", Build82TestsModal.Run);
            Suite("build82f-caption", Build82TestsCaption.Run);
            Suite("build82f-lesson", Build82TestsLessonGate.Run);
            Suite("build82f-redistribute", Build82TestsRedistribute.Run);
            Suite("build83", Build83Tests.Run);
            Suite("build83b", Build83TestsB.Run);
            Suite("build83c1", Build83TestsC1.Run);
            Suite("build83c2", Build83TestsC2.Run);
            Suite("build83c3", Build83TestsC3.Run);
            Suite("build83d", Build83TestsD.Run);
            Suite("build83p2", Build83TestsP2.Run);
            Suite("build84-feeder", Build84TestsFeeder.Run);
            Suite("build85-adpause", Build85TestsAdPause.Run);
            Suite("build85-look", Build85TestsLook.Run);
            Suite("build85-daily", Build85TestsDaily.Run);
            Suite("build87-sparkle", Build87SparkleTests.Run);
            Suite("build88-wing", Build88WingTests.Run);
            Suite("build88-mute", Build88MuteTests.Run);
            Suite("build88-adaudio", Build88AdAudioTests.Run);
            Suite("build88-chirp", Build88ChirpTests.Run);
            Suite("build88-banner", Build88BannerTests.Run);
            Suite("build88-caption", Build88CaptionTests.Run);
            Suite("build88-glove", Build88GloveTests.Run);
            Suite("build88-streak", Build88StreakTests.Run);
            Suite("build94-ad-stage", Build94AdStageTests.Run);
            Suite("build94-glove-banner", Build94GloveBannerTests.Run);
            Suite("build89-glove-caption", Build89GloveCaptionTests.Run);
            Suite("build88-pest", Build88PestTests.Run);
            Suite("build110-spring-ostrich", Build110SpringOstrichTests.Run);
            Suite("build110-ostrich-peck", Build110OstrichPeckTests.Run);
            Suite("build88-curtain", Build88CurtainTests.Run);
            Suite("build88-poker-tap", Build88PokerTapTests.Run);
            Suite("build88-dismiss", Build88DismissTests.Run);
            Suite("build88-poker-glove", Build88PokerGloveTests.Run);
            Suite("build89-vippending", Build89VipPendingTests.Run);
            Suite("build88-adtest", Build88AdTestTests.Run);
            Suite("build89-adhold", Build89AdHoldTests.Run);
            Suite("build89-shadow", Build89ShadowBirdTests.Run);
            Suite("storm-glass", StormGlassTests.Run);
            Suite("sky", SkyCycleTests.Run);
            Suite("bee-cards", BeeCardQueueTests.Run);
            Suite("build110-lightning-fire", Build110LightningFireTests.Run);
            Suite("build126-tap-cue", Build126TapCueTests.Run);
            Suite("build126-clear-garden-1", Build126ClearGardenOneTests.Run);
            Suite("build112-money", Build112MoneyFormatTests.Run);
            Suite("build89-payhead", Build89PayHeadTests.Run);
            Suite("poker-back-caption-timing", PokerBackCaptionTimingTests.Run);
            Suite("vip-name-gate", VipNameGateTests.Run);
            Suite("name-card-layout", NameCardLayoutTests.Run);
            Suite("pause-resume-tutorial", PauseResumeTutorialTests.Run);
            Suite("coach-arrow-shape", CoachArrowShapeTests.Run);
            Suite("build115-ostrich-neck", Build115OstrichNeckTests.Run);
            Suite("build115-ostrich-ground", Build115OstrichGroundTests.Run);
            Suite("build116-ostrich-pace", Build116OstrichPaceTests.Run);
            Suite("build119-collection-banner", Build119CollectionBannerTests.Run);
            Suite("season-scene", SeasonSceneTests.Run);
            Suite("glove-travel", GloveTravelTests.Run);
            Suite("build128-badger-power", Build128BadgerPowerTests.Run);
            Suite("build129-badger-gap", Build129BadgerGapTests.Run);
            Suite("build130-badger-stack", Build130BadgerStackTests.Run);
            Suite("build131-reset", Build131ResetTests.Run);
            Suite("usa-plane-day", UsaPlaneDayTests.Run);
            Suite("build119-fives", Build119FivesBoardTests.Run);
            Suite("build119-playtest", Build119PlaytestTests.Run);
            Debug.Log("EDITOR_SUITES_DONE");
        }

        // Badger editor suites for this branch. Skips the ostrich suites in RunEditorTests.
        public static void RunBadgerPolishGate()
        {
            Suite("badger", BadgerFightTests.Run);
            Suite("badger-show", BadgerShowTests.Run);
            Suite("build75", Build75Tests.Run);
            Suite("build110", Build110BadgerDialogTests.Run);
            Suite("build110ff", Build110FreezeFrozenTests.Run);
            Suite("build123", Build123BadgerTutorialTests.Run);
            Suite("build127", Build127Tests.Run);
            Suite("build128", Build128BadgerPowerTests.Run);
            Suite("build129", Build129BadgerGapTests.Run);
            Suite("build130", Build130BadgerStackTests.Run);
            Debug.Log("BADGER_POLISH_GATE_DONE");
        }

        // One suite throwing used to skip every suite after it.
        static void Suite(string name, System.Action run)
        {
            try { run(); }
            catch (System.Exception ex)
            {
                Debug.LogError("[suite] " + name + " THREW " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        static EnsurePlayable()
        {
            EditorApplication.delayCall += Pin;
            EditorApplication.delayCall += EnsureUrp;
            EditorApplication.delayCall += PinIosPackaging;
            EditorApplication.delayCall += MaybePlayCmd;
            EditorApplication.update += TickPlayCmd;
            EditorApplication.playModeStateChanged += ClearEnterQueued;
        }

        static void ClearEnterQueued(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode ||
                change == PlayModeStateChange.EnteredEditMode)
                _enterQueued = false;
            if (change == PlayModeStateChange.EnteredPlayMode)
                EditorApplication.delayCall += EnsureAppInPlay;
        }

        static void EnsureAppInPlay()
        {
            if (!EditorApplication.isPlaying) return;
            EditorApplication.isPaused = false;
            if (Object.FindAnyObjectByType<FlockFive.FlockFiveApp>() != null) return;
            var go = new GameObject("FlockFiveApp");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<FlockFive.FlockFiveApp>();
        }

        [MenuItem("Flock Five/Ensure Project Setup")]
        public static void Boot()
        {
            Pin();
            EnsureUrp();
            PinIosPackaging();
        }

        static bool Busy() =>
            EditorApplication.isCompiling
            || EditorApplication.isPlaying
            || EditorApplication.isPlayingOrWillChangePlaymode
            || EditorApplication.isUpdating;

        static void EnsureUrp()
        {
            if (Busy()) return;
            const string dir = "Assets/Settings";
            const string pipePath = dir + "/ParadiceURP.asset";
            const string rendPath = dir + "/ParadiceRenderer.asset";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            bool created = false;
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendPath);
                created = true;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipePath);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(asset, pipePath);
                created = true;
            }

            bool dirty = created;
            if (GraphicsSettings.defaultRenderPipeline != asset)
            {
                GraphicsSettings.defaultRenderPipeline = asset;
                dirty = true;
            }
            if (QualitySettings.renderPipeline != asset)
            {
                QualitySettings.renderPipeline = asset;
                dirty = true;
            }
            if (dirty) AssetDatabase.SaveAssets();
        }

        static void Pin()
        {
            if (Busy()) return;
            if (PlayerSettings.companyName != "zfxgames")
                PlayerSettings.companyName = "zfxgames";
            if (PlayerSettings.productName != "Flock Five")
                PlayerSettings.productName = "Flock Five";
            const string id = "com.zfxgames.flockfive";
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) != id)
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, id);
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            if (!PlayerSettings.allowedAutorotateToPortrait)
                PlayerSettings.allowedAutorotateToPortrait = true;
            if (PlayerSettings.allowedAutorotateToPortraitUpsideDown)
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            if (PlayerSettings.allowedAutorotateToLandscapeLeft)
                PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            if (PlayerSettings.allowedAutorotateToLandscapeRight)
                PlayerSettings.allowedAutorotateToLandscapeRight = false;
            if (PlayerSettings.defaultScreenWidth != 1080)
                PlayerSettings.defaultScreenWidth = 1080;
            if (PlayerSettings.defaultScreenHeight != 1920)
                PlayerSettings.defaultScreenHeight = 1920;
            if (!PlayerSettings.runInBackground)
                PlayerSettings.runInBackground = true;
            UseInputSystemOnly();
        }

        static void UseInputSystemOnly()
        {
            var objs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (objs == null || objs.Length == 0) return;
            var so = new SerializedObject(objs[0]);
            var p = so.FindProperty("activeInputHandler");
            if (p == null || p.intValue == 1) return;
            p.intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("Flock Five: Active Input Handling set to Input System package.");
        }

        static void PinIosPackaging()
        {
            if (Busy()) return;

            const string boot = "Assets/_Recovery/0.unity";
            var scenes = EditorBuildSettings.scenes;
            bool have = false;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == boot && scenes[i].enabled) { have = true; break; }
            }
            if (!have)
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(boot, true) };
                Debug.Log("Flock Five: boot scene added to build settings.");
            }

            if (PlayerSettings.insecureHttpOption != InsecureHttpOption.NotAllowed)
                PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
            if (!PlayerSettings.iOS.appleEnableAutomaticSigning)
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            const string team = "23K8T866FT";
            if (PlayerSettings.iOS.appleDeveloperTeamID != team)
                PlayerSettings.iOS.appleDeveloperTeamID = team;
            const string att = "Ads help keep Flock Five free.";
            var attProp = typeof(PlayerSettings.iOS).GetProperty("userTrackingUsageDescription");
            if (attProp != null && attProp.CanWrite)
            {
                var cur = attProp.GetValue(null) as string;
                if (cur != att) attProp.SetValue(null, att);
            }

            const string dest = "Assets/_Project/Art/Icons/app-icon-1024.png";
            var src = Path.GetFullPath(Path.Combine(Application.dataPath, "../Store/AppStore/app-icon-1024.png"));
            if (!File.Exists(src))
            {
                Debug.LogWarning("Flock Five: Store/AppStore/app-icon-1024.png missing.");
                return;
            }
            var destDir = Path.GetDirectoryName(dest);
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
            if (!File.Exists(dest) || File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(dest))
            {
                File.Copy(src, dest, true);
                AssetDatabase.ImportAsset(dest);
            }
            var imp = AssetImporter.GetAtPath(dest) as TextureImporter;
            if (imp != null)
            {
                bool dirty = false;
                if (imp.textureType != TextureImporterType.Default) { imp.textureType = TextureImporterType.Default; dirty = true; }
                if (imp.mipmapEnabled) { imp.mipmapEnabled = false; dirty = true; }
                if (imp.npotScale != TextureImporterNPOTScale.None) { imp.npotScale = TextureImporterNPOTScale.None; dirty = true; }
                if (imp.maxTextureSize < 1024) { imp.maxTextureSize = 1024; dirty = true; }
                if (imp.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }
                if (dirty) imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dest);
            if (tex == null) return;

            var kinds = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS);
            bool iconsDirty = false;
            for (int k = 0; k < kinds.Length; k++)
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kinds[k]);
                bool kindDirty = false;
                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i].GetTexture(0) == tex) continue;
                    var layers = new Texture2D[Mathf.Max(1, icons[i].maxLayerCount)];
                    layers[0] = tex;
                    icons[i].SetTextures(layers);
                    kindDirty = true;
                }
                if (!kindDirty) continue;
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kinds[k], icons);
                iconsDirty = true;
            }
            if (iconsDirty) AssetDatabase.SaveAssets();
        }

        [MenuItem("Flock Five/Preview Finale")]
        public static void PreviewFinale()
        {
            System.IO.File.WriteAllText("/tmp/flock-five-finale", "1");
            EnterPlay();
        }

        // Batch play-mode entry for the night-to-dawn clip. The flag stays until the
        // app exists, so BeforeSceneLoad can mute before the shot deletes it.
        public static void RunSkyShot()
        {
            File.WriteAllText("/tmp/flock-five-b88-sky", "1");
            EnterPlay();
        }

        // Play-mode curtain clip. Launch without -batchmode and without -nographics.
        // Do not enter play from -executeMethod: that nests inside "Finish Loading Project".
        public static void ArmCurtainEditor()
        {
            Debug.Log("CURTAIN_EDITOR_READY");
            File.WriteAllText("/tmp/curtain-editor-ready", "1");
        }

        public static void RunCurtainShot()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            File.WriteAllText("/tmp/flock-five-b88-curtain", "1");
            EnterPlay();
        }

        public static void RunStormDropShot()
        {
            File.WriteAllText("/tmp/flock-five-storm-drops", "1");
            EnterPlay();
        }

        static bool _enterQueued;

        [MenuItem("Flock Five/Play Slice")]
        public static void EnterPlay()
        {
            if (File.Exists("/tmp/flock-five-peach-flutter")) ForceFlutterFrame();
            else ForcePortraitGameView();
            if (EditorApplication.isPlaying)
            {
                if (_enterQueued) return;
                EditorApplication.playModeStateChanged += RestartPlay;
                EditorApplication.isPlaying = false;
                return;
            }
            _enterQueued = true;
            if (EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += WaitThenPlay;
                return;
            }
            EditorApplication.isPlaying = true;
        }

        static void RestartPlay(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= RestartPlay;
            EditorApplication.delayCall += WaitThenPlay;
        }

        static void WaitThenPlay()
        {
            if (EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += WaitThenPlay;
                return;
            }
            if (File.Exists("/tmp/flock-five-peach-flutter")) ForceFlutterFrame();
            else ForcePortraitGameView();
            _enterQueued = false;
            EditorApplication.isPaused = false;
            if (!EditorApplication.isPlaying)
                EditorApplication.isPlaying = true;
        }

        static readonly string[] LiveShotCmds =
        {
            "/tmp/flock-five-poker-faces",
            "/tmp/flock-five-streak-shot",
            "/tmp/flock-five-birds-shot",
            "/tmp/flock-five-leaves-shot",
            "/tmp/flock-five-oasis-shot",
            "/tmp/flock-five-gift-shot",
            "/tmp/flock-five-consumer-tour",
            "/tmp/flock-five-shot",
            "/tmp/flock-five-hand-qa",
            "/tmp/flock-five-hive-inspect",
            "/tmp/flock-five-poker-feel",
            "/tmp/flock-five-b81-hud",
            "/tmp/flock-five-b82-stills",
            "/tmp/flock-five-b82-gardens",
            "/tmp/flock-five-b82c-stills",
            "/tmp/flock-five-b82d-stills",
            "/tmp/flock-five-b82e-stills",
            "/tmp/flock-five-b82f-stills",
            "/tmp/flock-five-b82g-stills",
            "/tmp/flock-five-b83-gardens",
            "/tmp/flock-five-b83c-stills",
            "/tmp/flock-five-b85-reward",
            "/tmp/flock-five-b85-daily",
            "/tmp/flock-five-b87",
            "/tmp/flock-five-b87v",
            "/tmp/flock-five-b87x",
            "/tmp/flock-five-b88-mute",
            "/tmp/flock-five-b88-sky",
            "/tmp/flock-five-b88-banner",
            "/tmp/flock-five-b88-caption",
            "/tmp/flock-five-b88-streak",
            "/tmp/flock-five-wt-b88-pest",
            "/tmp/flock-five-b88-curtain",
            "/tmp/flock-five-b88-poker-tap",
            "/tmp/flock-five-peach-flutter",
            "/tmp/flock-five-b88-dismiss",
            "/tmp/flock-five-b88-poker-glove",
            "/tmp/flock-five-b89-glove",
            "/tmp/flock-five-b89-settings",
            "/tmp/flock-five-b89-deadend",
            "/tmp/flock-five-b89-popup",
            "/tmp/flock-five-b89-payhead",
            "/tmp/flock-five-storm-drops",
            "/tmp/flock-five-badger-power",
            "/tmp/flock-five-b129-badger",
            "/tmp/ff-badger-stack-shot",
            "/tmp/ff-badger-fight-shot",
            "/tmp/flock-five-pr221"
        };

        static bool AnyShotCmd()
        {
            for (int i = 0; i < LiveShotCmds.Length; i++)
                if (File.Exists(LiveShotCmds[i])) return true;
            return false;
        }

        static void TryStartShot(FlockFive.FlockFiveApp app, string cmd, string method)
        {
            if (!File.Exists(cmd)) return;
            try { File.Delete(cmd); } catch { return; }
            app.StartCoroutine(method);
        }

        static double _flutterT0 = -1;

        static void WatchFlutter()
        {
            const string done = "/tmp/flock-peach-flutter-done";
            bool armed = File.Exists("/tmp/flock-five-peach-flutter") || FlockFive.FlockFiveApp.EditorShotLive;
            // A leftover done marker must not quit an ordinary compile.
            if (File.Exists(done) && armed)
            {
                Debug.Log("Flock Five: peach flutter capture done");
                EditorApplication.Exit(0);
                return;
            }
            if (!armed) return;
            if (_flutterT0 < 0) _flutterT0 = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - _flutterT0 > 720)
            {
                Debug.LogError("Flock Five: peach flutter capture timed out");
                EditorApplication.Exit(1);
            }
        }

        static void TickPlayCmd()
        {
            WatchFlutter();
            if (File.Exists("/tmp/flock-five-unpause")
                || AnyShotCmd()
                || FlockFive.FlockFiveApp.EditorShotLive
                || FlockFive.FlockFiveApp.ShotKeepAwake)
            {
                EditorApplication.isPaused = false;
                EditorApplication.QueuePlayerLoopUpdate();
                try { if (File.Exists("/tmp/flock-five-unpause")) File.Delete("/tmp/flock-five-unpause"); } catch { }
            }
            if (EditorApplication.isPlaying && FlockFive.FlockFiveApp.EditorShotLive)
                EditorApplication.QueuePlayerLoopUpdate();
            if (AnyShotCmd())
            {
                if (File.Exists("/tmp/flock-five-b88-curtain") && !File.Exists("/tmp/curtain-flag-seen"))
                    File.WriteAllText("/tmp/curtain-flag-seen",
                        EditorApplication.isPlaying ? "playing" : "enter");
                EditorApplication.isPaused = false;
                if (!EditorApplication.isPlaying)
                {
                    if (!EditorApplication.isCompiling) EnterPlay();
                }
                else
                {
                    EnsureAppInPlay();
                    var app = Object.FindAnyObjectByType<FlockFive.FlockFiveApp>();
                    if (app != null)
                    {
                        TryStartShot(app, "/tmp/flock-five-poker-faces", "ShotPokerFaces");
                        TryStartShot(app, "/tmp/flock-five-streak-shot", "ShotStreak");
                        TryStartShot(app, "/tmp/flock-five-birds-shot", "ShotBirds");
                        TryStartShot(app, "/tmp/flock-five-leaves-shot", "ShotLeaves");
                        TryStartShot(app, "/tmp/flock-five-oasis-shot", "ShotOasis");
                        TryStartShot(app, "/tmp/flock-five-gift-shot", "ShotGift");
                        TryStartShot(app, "/tmp/flock-five-consumer-tour", "ShotConsumerTour");
                        TryStartShot(app, "/tmp/flock-five-shot", "ShotHome");
                        TryStartShot(app, "/tmp/flock-five-hand-qa", "ShotHandQa");
                        TryStartShot(app, "/tmp/flock-five-hive-inspect", "ShotHiveInspect");
                        TryStartShot(app, "/tmp/flock-five-b81-hud", "ShotBuild81PassH");
                        TryStartShot(app, "/tmp/flock-five-b82-stills", "ShotBuild82");
                        TryStartShot(app, "/tmp/flock-five-b82-gardens", "ShotBuild82Gardens");
                        TryStartShot(app, "/tmp/flock-five-b82c-stills", "ShotBuild82c");
                        TryStartShot(app, "/tmp/flock-five-b82d-stills", "ShotBuild82d");
                        TryStartShot(app, "/tmp/flock-five-b82e-stills", "ShotBuild82e");
                        TryStartShot(app, "/tmp/flock-five-b82f-stills", "ShotBuild82f");
                        TryStartShot(app, "/tmp/flock-five-b82g-stills", "ShotBuild82g");
                        TryStartShot(app, "/tmp/flock-five-b83-gardens", "ShotBuild83Gardens");
                        TryStartShot(app, "/tmp/flock-five-b83c-stills", "ShotBuild83c");
                        TryStartShot(app, "/tmp/flock-five-b85-reward", "ShotBuild85Reward");
                        TryStartShot(app, "/tmp/flock-five-b85-daily", "ShotBuild85Daily");
                        TryStartShot(app, "/tmp/flock-five-b87", "ShotBuild87");
                        TryStartShot(app, "/tmp/flock-five-b87v", "ShotBuild87Video");
                        TryStartShot(app, "/tmp/flock-five-b87x", "ShotBuild87Extra");
                        TryStartShot(app, "/tmp/flock-five-b88-mute", "ShotBuild88Mute");
                        TryStartShot(app, "/tmp/flock-five-b88-sky", "ShotSkyTransition");
                        TryStartShot(app, "/tmp/flock-five-b88-banner", "ShotBuild88Banner");
                        TryStartShot(app, "/tmp/flock-five-b88-caption", "ShotBuild88Caption");
                        TryStartShot(app, "/tmp/flock-five-b88-streak", "ShotKeepStreak");
                        TryStartShot(app, "/tmp/flock-five-wt-b88-pest", "ShotPestReset");
                        TryStartShot(app, "/tmp/flock-five-b88-curtain", "ShotLevelCurtain");
                        TryStartShot(app, "/tmp/flock-five-b88-poker-tap", "ShotPokerTap");
                        TryStartShot(app, "/tmp/flock-five-b88-dismiss", "ShotDailyDismiss");
                        TryStartShot(app, "/tmp/flock-five-b89-glove", "ShotGloveCaption");
                        TryStartShot(app, "/tmp/flock-five-b88-poker-glove", "ShotPokerGlove");
                        TryStartShot(app, "/tmp/flock-five-b89-settings", "ShotBuild89Settings");
                        TryStartShot(app, "/tmp/flock-five-b89-deadend", "ShotDeadEndNudge");
                        TryStartShot(app, "/tmp/flock-five-storm-drops", "ShotStormDrops");
                        TryStartShot(app, "/tmp/flock-five-poker-feel", "ShotPokerFeel");
                        TryStartShot(app, "/tmp/flock-five-b89-popup", "ShotPopupExclusive");
                        TryStartShot(app, "/tmp/flock-five-b89-payhead", "ShotPayHead");
                        TryStartShot(app, "/tmp/flock-five-badger-power", "ShotBadgerPower");
                        TryStartShot(app, "/tmp/flock-five-b129-badger", "ShotB129");
                        TryStartShot(app, "/tmp/ff-badger-stack-shot", "ShotBadgerStack");
                        TryStartShot(app, "/tmp/ff-badger-fight-shot", "ShotBadgerFight");
                        TryStartShot(app, "/tmp/flock-five-pr221", "ShotPr221");
                    }
                }
            }
            if (!File.Exists(PlayCmd)) return;
            MaybePlayCmd();
        }

        static void MaybePlayCmd()
        {
            if (!File.Exists(PlayCmd)) return;
            if (EditorApplication.isCompiling) return;
            try { File.Delete(PlayCmd); }
            catch { return; }
            EnterPlay();
        }

        static void ForceFlutterFrame() => ForceGameView(1170, 2532, "Flock Five Flutter");

        static void ForcePortraitGameView() => ForceGameView(1080, 1920, "Flock Five iPhone");

        static void ForceGameView(int width, int height, string label)
        {
            var asm = typeof(EditorWindow).Assembly;
            var gvType = asm.GetType("UnityEditor.GameView");
            if (gvType == null) return;

            EditorWindow gv = null;
            var all = Resources.FindObjectsOfTypeAll(gvType);
            if (all != null && all.Length > 0) gv = all[0] as EditorWindow;
            if (gv == null)
            {
                try { gv = EditorWindow.GetWindow(gvType, false, "Game", true); }
                catch { return; }
            }
            if (gv == null) return;
            gv.Focus();

            try
            {
                int found = FindOrAddPortraitSize(asm, width, height, label);
                if (found < 0)
                {
                    Debug.LogWarning("Flock Five: no portrait Game view size.");
                    return;
                }

                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                var callback = gvType.GetMethod("SizeSelectionCallback", flags);
                if (callback != null)
                    callback.Invoke(gv, new object[] { found, null });
                else
                {
                    var prop = gvType.GetProperty("selectedSizeIndex", flags);
                    if (prop != null && prop.CanWrite) prop.SetValue(gv, found, null);
                }

                var maxField = gvType.GetField("m_MaximizeOnPlay", flags);
                if (maxField != null) maxField.SetValue(gv, false);

                Debug.Log("Flock Five Game view portrait index=" + found);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Could not switch Game view to portrait: " + e.Message);
            }
        }

        static int FindOrAddPortraitSize(Assembly asm, int width, int height, string label)
        {
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");
            if (sizesType == null || sizeType == null) return -1;

            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleton.GetProperty("instance").GetValue(null, null);
            var group = sizesType.GetProperty("currentGroup").GetValue(instance, null);
            var groupType = group.GetType();
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = groupType.GetMethod("GetGameViewSize");

            int found = -1;
            int fallback = -1;
            for (int i = 0; i < count; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                var st = size.GetType();
                int w = (int)st.GetProperty("width").GetValue(size, null);
                int h = (int)st.GetProperty("height").GetValue(size, null);
                if (w == width && h == height) return i;
                if (h > w && w >= 320)
                {
                    fallback = i;
                    if (Mathf.Abs(h / (float)Mathf.Max(1, w) - 16f / 9f) < 0.04f)
                        found = i;
                }
            }
            if (found >= 0) return found;

            var add = groupType.GetMethod("AddCustomSize");
            var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            if (add != null && ctor != null)
            {
                object fixedRes = 1;
                if (sizeTypeEnum != null)
                    fixedRes = System.Enum.ToObject(sizeTypeEnum, 1);
                var custom = ctor.Invoke(new[] { fixedRes, width, height, label });
                add.Invoke(group, new[] { custom });
                var save = sizesType.GetMethod("SaveToHDD", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (save != null) save.Invoke(instance, null);
                return (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }

            return fallback;
        }
    }
}
#endif









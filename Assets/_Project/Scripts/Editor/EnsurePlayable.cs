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

        static EnsurePlayable()
        {
            EditorApplication.delayCall += Pin;
            EditorApplication.delayCall += EnsureUrp;
            EditorApplication.delayCall += MaybePlayCmd;
            EditorApplication.update += TickPlayCmd;
        }

        [MenuItem("Flock Five/Ensure Project Setup")]
        public static void Boot()
        {
            Pin();
            EnsureUrp();
        }

        static void EnsureUrp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string dir = "Assets/Settings";
            const string pipePath = dir + "/ParadiceURP.asset";
            const string rendPath = dir + "/ParadiceRenderer.asset";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendPath);
            }

            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipePath);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(asset, pipePath);
            }

            GraphicsSettings.defaultRenderPipeline = asset;
            QualitySettings.renderPipeline = asset;
            AssetDatabase.SaveAssets();
        }

        static void Pin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (PlayerSettings.companyName != "zfxgames")
                PlayerSettings.companyName = "zfxgames";
            if (PlayerSettings.productName != "Flock Five")
                PlayerSettings.productName = "Flock Five";
            const string id = "com.zfxgames.flockfive";
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) != id)
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, id);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
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

        [MenuItem("Flock Five/Preview Finale")]
        public static void PreviewFinale()
        {
            System.IO.File.WriteAllText("/tmp/flock-five-finale", "1");
            EnterPlay();
        }

        [MenuItem("Flock Five/Play Slice")]
        public static void EnterPlay()
        {
            ForcePortraitGameView();
            if (EditorApplication.isPlaying)
            {
                EditorApplication.playModeStateChanged += RestartPlay;
                EditorApplication.isPlaying = false;
                return;
            }
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
            ForcePortraitGameView();
            if (!EditorApplication.isPlaying)
                EditorApplication.isPlaying = true;
        }

        static void TickPlayCmd()
        {
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

        static void ForcePortraitGameView()
        {
            var asm = typeof(EditorWindow).Assembly;
            var gvType = asm.GetType("UnityEditor.GameView");
            if (gvType == null) return;

            EditorWindow gv = null;
            var all = Resources.FindObjectsOfTypeAll(gvType);
            if (all != null && all.Length > 0) gv = all[0] as EditorWindow;
            if (gv == null) gv = EditorWindow.GetWindow(gvType, false, "Game", true);
            if (gv == null) return;
            gv.Focus();

            try
            {
                int found = FindOrAddPortraitSize(asm);
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

        static int FindOrAddPortraitSize(Assembly asm)
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
                if (w == 1080 && h == 1920) return i;
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
                var custom = ctor.Invoke(new[] { fixedRes, 1080, 1920, "Flock Five iPhone" });
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

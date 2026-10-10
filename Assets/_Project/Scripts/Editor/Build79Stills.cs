#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlockFive.Editor
{
    // Play-mode captures of the home gear, Settings, and the ATT pre-prompt.
    // Launch without -quit and without -nographics. The shot coroutine exits the editor.
    public static class Build79Stills
    {
        public static void Capture()
        {
            SetGameView(1179, 2556);
            File.WriteAllText("/tmp/flock-five-b79-stills", "1");
            EditorApplication.isPlaying = true;
        }

        public static void Capture80()
        {
            SetGameView(1179, 2556);
            File.WriteAllText("/tmp/flock-five-b80-stills", "1");
            EditorApplication.isPlaying = true;
        }

        // Home airplane with the heart shell up. Pose is capture-only.
        public static void CapturePlane()
        {
            SetGameView(1179, 2556);
            File.WriteAllText("/tmp/flock-five-b80-plane", "1");
            EditorApplication.isPlaying = true;
        }

        static void SetGameView(int width, int height)
        {
            var asm = typeof(EditorWindow).Assembly;
            var gvType = asm.GetType("UnityEditor.GameView");
            if (gvType == null) return;
            EditorWindow gv = null;
            var all = Resources.FindObjectsOfTypeAll(gvType);
            if (all != null && all.Length > 0) gv = all[0] as EditorWindow;
            if (gv == null) gv = EditorWindow.GetWindow(gvType, false, "Game", true);
            if (gv == null) return;
            int index = FindOrAdd(asm, width, height);
            if (index < 0) return;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var callback = gvType.GetMethod("SizeSelectionCallback", flags);
            if (callback != null) callback.Invoke(gv, new object[] { index, null });
        }

        static int FindOrAdd(Assembly asm, int width, int height)
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
            for (int i = 0; i < count; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                var st = size.GetType();
                int w = (int)st.GetProperty("width").GetValue(size, null);
                int h = (int)st.GetProperty("height").GetValue(size, null);
                if (w == width && h == height) return i;
            }
            var add = groupType.GetMethod("AddCustomSize");
            var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            if (add == null || ctor == null) return -1;
            object fixedRes = System.Enum.ToObject(sizeTypeEnum, 1);
            var custom = ctor.Invoke(new[] { fixedRes, width, height, "Flock Five b79" });
            add.Invoke(group, new[] { custom });
            var save = sizesType.GetMethod("SaveToHDD", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (save != null) save.Invoke(instance, null);
            return (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
        }
    }
}
#endif

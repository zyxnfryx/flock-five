#if UNITY_EDITOR
using UnityEditor;

namespace FlockFive.Editor
{
    // b87: lets play-mode still harnesses pick an exact Game view size (iPad 3:4).
    [InitializeOnLoad]
    static class GameSizeHook87
    {
        static GameSizeHook87()
        {
            FlockFive.FlockFiveApp.EditorGameSize = (w, h) => Build79Stills.SetGameView(w, h);
        }
    }
}
#endif

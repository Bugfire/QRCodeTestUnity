using UnityEditor;

namespace MainEditorMenu
{

    public static class EditorMenu
    {
        [MenuItem("Tools/UpdateCanvasScale")]
        static void Start()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            foreach (var g in scene.GetRootGameObjects())
            {
                var t = g.GetComponentsInChildren<MainComponents.MainComponent>();
                if (t != null)
                {
                    foreach (var o in t)
                    {
                        o.AdjustCanvas(true);
                    }
                }
            }
        }
    }
}
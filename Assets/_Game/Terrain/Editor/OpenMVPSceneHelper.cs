using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TankGame.Terrain.Editor
{
    [InitializeOnLoad]
    internal static class OpenMVPSceneHelper
    {
        private const string MvpScenePath = "Assets/_Game/Scenes/MVPIntegrated.unity";

        static OpenMVPSceneHelper()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                if (EditorSceneManager.GetActiveScene().path != MvpScenePath)
                {
                    EditorSceneManager.OpenScene(MvpScenePath, OpenSceneMode.Single);
                    Debug.Log("[Scene Switcher] Switched to main graphical scene: " + MvpScenePath);
                }
            };
        }

        [MenuItem("Tools/Tank Game/Open Main Graphical Scene (MVP) _F6")]
        public static void OpenMainScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorSceneManager.OpenScene(MvpScenePath, OpenSceneMode.Single);
        }
    }
}

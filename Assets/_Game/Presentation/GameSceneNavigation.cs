using System.Collections;
using TankGame.AI;
using TankGame.Core;
using TankGame.Multiplayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TankGame.Presentation
{
    /// <summary>Scene-scoped Back navigation; waits for the existing service to finish shutdown.</summary>
    public sealed class GameSceneNavigation : MonoBehaviour
    {
        public const string MenuScene = "Assets/_Game/Scenes/MainMenu.unity";
        public const string OfflineScene = "Assets/_Game/Scenes/MVPIntegrated.unity";
        public const string LanScene = "Assets/_Game/Multiplayer/LanGameplayTest.unity";
        public bool IsReturning { get; private set; }
        public string Error { get; private set; } = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != OfflineScene && scene.path != LanScene) return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<GameSceneNavigation>(true) != null) return;
            var navigation = new GameObject("Session navigation");
            SceneManager.MoveGameObjectToScene(navigation, scene);
            navigation.AddComponent<GameSceneNavigation>();
        }

        internal static AsyncOperation Load(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
#endif
        }

        public void ReturnToMenu()
        {
            if (!IsReturning) StartCoroutine(Return());
        }

        private IEnumerator Return()
        {
            IsReturning = true;
            Error = "";
            foreach (var input in FindObjectsByType<IntegratedInput>()) input.enabled = false;
            foreach (var ai in FindObjectsByType<SimpleArtilleryAI>()) ai.enabled = false;
            foreach (var panel in FindObjectsByType<LanGameplayPanel>()) panel.enabled = false;
            foreach (var controls in FindObjectsByType<GameplayTouchControls>()) controls.enabled = false;
            foreach (var flow in FindObjectsByType<MatchFlowController>())
                if (flow.isActiveAndEnabled) flow.EndMatch();
            var services = FindObjectsByType<NgoMultiplayerService>();
            foreach (var service in services) service.Disconnect();
            double deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (System.Array.Exists(services, service => service != null && service.IsRunning))
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                { Error = "Network is still stopping. Tap Main Menu to retry."; IsReturning = false; yield break; }
                yield return null;
            }
            // Let deferred destruction of NGO's session root finish before loading another mode.
            yield return null;
            Load(MenuScene);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ReturnToMenu();
        }

        private void OnGUI()
        {
            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) return;
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax), Quaternion.identity,
                new Vector3(safe.width / 1000f, safe.height / 600f, 1));
            bool enabled = GUI.enabled;
            GUI.enabled = !IsReturning;
            if (GUI.Button(new Rect(400, 80, 200, 52), IsReturning ? "LEAVING..." : "MAIN MENU",
                new GUIStyle(GUI.skin.button) { fontSize = 23 })) ReturnToMenu();
            GUI.enabled = enabled;
            if (!string.IsNullOrEmpty(Error)) GUI.Label(new Rect(380, 135, 350, 65), Error);
            GUI.matrix = previous;
        }
    }
}

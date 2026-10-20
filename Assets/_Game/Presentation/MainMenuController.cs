using TankGame.AI;
using TankGame.Core;
using UnityEngine;

namespace TankGame.Presentation
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public GameMode SelectedMode => GameSessionConfiguration.Mode;
        public AIDifficulty SelectedDifficulty => GameSessionConfiguration.Difficulty;
        public bool IsStarting { get; private set; }
        public string Error { get; private set; } = "";
        public bool DifficultyVisible => SelectedMode == GameMode.VsComputer;

        public void SelectMode(GameMode mode)
        {
            if (IsStarting) return;
            GameSessionConfiguration.SelectMode(mode);
        }

        public void SelectDifficulty(AIDifficulty difficulty)
        {
            if (IsStarting || !DifficultyVisible) return;
            GameSessionConfiguration.SelectDifficulty(difficulty);
        }

        public void StartGame()
        {
            if (IsStarting) return;
            IsStarting = true;
            try { GameSceneNavigation.Load(SelectedMode == GameMode.VsComputer ? GameSceneNavigation.OfflineScene : GameSceneNavigation.LanScene); }
            catch (System.Exception error) { IsStarting = false; Error = error.Message; Debug.LogException(error); }
        }

        private void OnGUI()
        {
            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) return;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax), Quaternion.identity,
                new Vector3(safe.width / 1000f, safe.height / 600f, 1));
            var title = new GUIStyle(GUI.skin.label) { fontSize = 42, alignment = TextAnchor.MiddleCenter };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 25 };
            var label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            bool enabled = GUI.enabled;
            GUI.Box(new Rect(180, 25, 640, 550), GUIContent.none);
            GUI.Label(new Rect(200, 45, 600, 70), "TANK ARTILLERY", title);
            GUI.enabled = !IsStarting;
            if (GUI.Button(new Rect(240, 135, 520, 64), Mark(SelectedMode == GameMode.VsComputer, "VS Computer"), button)) SelectMode(GameMode.VsComputer);
            if (GUI.Button(new Rect(240, 215, 520, 64), Mark(SelectedMode == GameMode.LanMultiplayer, "LAN Multiplayer"), button)) SelectMode(GameMode.LanMultiplayer);
            if (DifficultyVisible)
            {
                GUI.Label(new Rect(240, 295, 520, 32), "Computer difficulty", label);
                for (int i = 0; i < 3; i++)
                {
                    var difficulty = (AIDifficulty)i;
                    if (GUI.Button(new Rect(210 + i * 195, 335, 190, 64), Mark(SelectedDifficulty == difficulty, difficulty.ToString()), button)) SelectDifficulty(difficulty);
                }
            }
            else GUI.Label(new Rect(240, 305, 520, 80), "Create a room, find rooms, or connect by IP on the next screen.", label);
            if (GUI.Button(new Rect(280, 435, 440, 72), IsStarting ? "LOADING..." : "START GAME", button)) StartGame();
            GUI.Label(new Rect(210, 515, 580, 45), Error, label);
            GUI.enabled = enabled;
            GUI.matrix = previous;
        }

        private static string Mark(bool selected, string text) => (selected ? "●  " : "○  ") + text;
    }
}

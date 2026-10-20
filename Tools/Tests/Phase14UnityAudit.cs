using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TankGame.AI;
using TankGame.Core;
using TankGame.Data;
using TankGame.Multiplayer;
using TankGame.Presentation;
using TankGame.Projectiles;
using TankGame.Tank;
using TankGame.Testing;
using Unity.Netcode;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        Application.runInBackground = true;
        var log = new StringBuilder(); int checks = 0, stage = 0, difficultyIndex = 0;
        double deadline = EditorApplication.timeSinceStartup + 120;
        bool sawAIShot = false; float shotSpeed = 0; int owner = -1;
        GameManager subscribedGame = null;
        Action<MatchState, MatchState> onState = null;
        Action<bool, string> check = (ok, name) => { log.AppendLine((ok ? "PASS " : "FAIL ") + name); if (!ok) throw new Exception(name); checks++; };
        var weapon = ScriptableObject.CreateInstance<WeaponData>();
        try
        {
            foreach (AIDifficulty difficulty in Enum.GetValues(typeof(AIDifficulty)))
            {
                var profile = AIDifficultyProfile.For(difficulty);
                float expectedAngle = difficulty == AIDifficulty.Easy ? 10 : difficulty == AIDifficulty.Normal ? 5 : 1;
                float totalError = 0, minimumSpeed = 100, maximumSpeed = 0;
                for (int i = 0; i <= 1000; i++)
                {
                    float sample = i / 500f - 1f;
                    float angle = profile.ApplyAngle(45, sample, 0, 90);
                    float power = profile.GetPower(weapon, sample);
                    if (!float.IsFinite(angle) || !float.IsFinite(power) || power < 0 || power > 1 || Mathf.Abs(angle - 45) > expectedAngle + .001f)
                        throw new Exception("Invalid difficulty sample");
                    totalError += Mathf.Abs(angle - 45);
                    float speed = weapon.GetLaunchSpeed(power); minimumSpeed = Mathf.Min(minimumSpeed, speed); maximumSpeed = Mathf.Max(maximumSpeed, speed);
                }
                check(profile.AngleError == expectedAngle, difficulty + " provisional angle bound");
                check(totalError > 0 && minimumSpeed > 0 && maximumSpeed <= weapon.LaunchSpeed, difficulty + " bounded variable speed and nonzero accuracy variation (1001 samples)");
                check(profile.ApplyAngle(89, 1, 0, 90) <= 90 && profile.ApplyAngle(0, -1, 0, 90) == 0, difficulty + " final aim clamping");
                log.AppendLine("MEASURE " + difficulty + " mean absolute angle error=" + totalError / 1001 + " speed=" + minimumSpeed + ".." + maximumSpeed);
            }
            bool rejected = false;
            try { AIDifficultyProfile.For(AIDifficulty.Normal).GetPower(weapon, float.NaN); } catch (ArgumentException) { rejected = true; }
            check(rejected, "nonfinite difficulty sample rejected");
            check(GameSessionConfiguration.Difficulty == AIDifficulty.Normal, "startup difficulty Normal");
        }
        finally { UnityEngine.Object.Destroy(weapon); }
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(GameSceneNavigation.MenuScene, new LoadSceneParameters(LoadSceneMode.Single));
        EditorApplication.CallbackFunction tick = null;
        Action finish = () =>
        {
            EditorApplication.update -= tick;
            if (subscribedGame != null) subscribedGame.StateChanged -= onState;
            File.WriteAllText("Builds/Phase14/MenuValidation.txt", log + "TOTAL PASS " + checks);
        };
        tick = () =>
        {
            try
            {
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline) throw new Exception("Menu audit timeout or Play stopped at stage " + stage);
                var scene = SceneManager.GetActiveScene();
                var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
                var navigation = UnityEngine.Object.FindAnyObjectByType<GameSceneNavigation>();
                if (stage == 0 && menu != null)
                {
                    check(scene.path == GameSceneNavigation.MenuScene, "initial MainMenu scene load");
                    check(menu.SelectedDifficulty == AIDifficulty.Normal && menu.DifficultyVisible, "Normal selected in VS Computer menu");
                    check(UnityEngine.Object.FindObjectsByType<GameplayActionController>().Length == 0 && UnityEngine.Object.FindObjectsByType<EventSystem>().Length == 0, "menu has no gameplay commands or duplicate EventSystem");
                    stage = 1;
                }
                if (stage == 1 && menu != null)
                {
                    var difficulty = (AIDifficulty)difficultyIndex;
                    menu.SelectMode(GameMode.VsComputer); menu.SelectDifficulty(difficulty);
                    check(menu.SelectedDifficulty == difficulty && menu.DifficultyVisible, difficulty + " exclusive radio selection");
                    menu.SelectMode(GameMode.LanMultiplayer);
                    menu.SelectDifficulty((AIDifficulty)((difficultyIndex + 1) % 3));
                    check(!menu.DifficultyVisible && menu.SelectedDifficulty == difficulty, "LAN hides and cannot change difficulty");
                    menu.SelectMode(GameMode.VsComputer); menu.StartGame(); menu.StartGame();
                    check(menu.IsStarting, "duplicate Start guarded"); stage = 2;
                }
                else if (stage == 2 && scene.path == GameSceneNavigation.OfflineScene)
                {
                    var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                    var ai = UnityEngine.Object.FindAnyObjectByType<SimpleArtilleryAI>();
                    if (game == null || game.CurrentState != MatchState.TurnActive || ai == null) return;
                    check(ai.enabled && ai.Difficulty == (AIDifficulty)difficultyIndex, "offline AI captures selected difficulty before match");
                    check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length == 1 && UnityEngine.Object.FindObjectsByType<NetworkManager>().Length == 0, "offline single EventSystem and no NetworkManager");
                    check(navigation != null && UnityEngine.Object.FindObjectsByType<GameSceneNavigation>().Length == 1, "one scene-scoped Back navigation");
                    GameSessionConfiguration.SelectDifficulty((AIDifficulty)((difficultyIndex + 1) % 3));
                    check(ai.Difficulty == (AIDifficulty)difficultyIndex, "running match difficulty immutable");
                    GameSessionConfiguration.SelectDifficulty((AIDifficulty)difficultyIndex);
                    subscribedGame = game; sawAIShot = false;
                    onState = (previous, current) =>
                    {
                        if (current != MatchState.ProjectileFlying) return;
                        var projectile = UnityEngine.Object.FindAnyObjectByType<Projectile>();
                        if (projectile != null && projectile.OwnerPlayerIndex == 1)
                        { sawAIShot = true; owner = projectile.OwnerPlayerIndex; shotSpeed = projectile.GetComponent<Rigidbody2D>().linearVelocity.magnitude; }
                    };
                    game.StateChanged += onState;
                    var turns = UnityEngine.Object.FindAnyObjectByType<TurnManager>();
                    check(turns.CurrentPlayerIndex == 0, "offline Player0 begins");
                    UnityEngine.Object.FindAnyObjectByType<GameplayActionController>().Fire(0, Vector2.up, 1);
                    stage = 3;
                }
                else if (stage == 3 && sawAIShot)
                {
                    var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                    if (game.CurrentState != MatchState.TurnActive) return;
                    var ai = UnityEngine.Object.FindAnyObjectByType<SimpleArtilleryAI>();
                    var settings = new SerializedObject(ai);
                    var data = (WeaponData)settings.FindProperty("weaponData").objectReferenceValue;
                    var profile = AIDifficultyProfile.For((AIDifficulty)difficultyIndex);
                    check(owner == 1 && shotSpeed >= data.GetLaunchSpeed(profile.GetPower(data, -1)) - .01f && shotSpeed <= data.GetLaunchSpeed(profile.GetPower(data, 1)) + .01f, ai.Difficulty + " real AI shot owner and launch speed bounds");
                    log.AppendLine("MEASURE real AI " + ai.Difficulty + " launch speed=" + shotSpeed);
                    subscribedGame.StateChanged -= onState; subscribedGame = null;
                    var flow = UnityEngine.Object.FindAnyObjectByType<MatchFlowController>(); flow.EndMatch();
                    UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>().RestartTestMatch();
                    check(ai.Difficulty == (AIDifficulty)difficultyIndex && game.CurrentState == MatchState.TurnActive, "restart retains selected difficulty and action flow");
                    check(UnityEngine.Object.FindAnyObjectByType<GameplayActionController>().GetMovementRemaining(0) == 3, "restart restores movement budget");
                    navigation.ReturnToMenu(); navigation.ReturnToMenu(); stage = 4;
                }
                else if (stage == 4 && menu != null)
                {
                    check(UnityEngine.Object.FindObjectsByType<Projectile>().Length == 0 && UnityEngine.Object.FindObjectsByType<SimpleArtilleryAI>().Length == 0, "Back removes prior gameplay and AI");
                    check(menu.SelectedDifficulty == (AIDifficulty)difficultyIndex, "menu preserves explicit difficulty choice");
                    difficultyIndex++;
                    if (difficultyIndex < 3) stage = 1;
                    else { menu.SelectMode(GameMode.LanMultiplayer); menu.StartGame(); stage = 5; }
                }
                else if (stage == 5 && scene.path == GameSceneNavigation.LanScene)
                {
                    var session = UnityEngine.Object.FindAnyObjectByType<LanGameplaySession>();
                    if (session == null || navigation == null) return;
                    check(!UnityEngine.Object.FindAnyObjectByType<SimpleArtilleryAI>(FindObjectsInactive.Include).enabled, "LAN AI disabled");
                    check(UnityEngine.Object.FindAnyObjectByType<LanGameplayPanel>() != null && !session.Service.IsRunning, "menu enters existing room browser without auto-hosting");
                    session.StartHost(7787); stage = 6;
                }
                else if (stage == 6)
                {
                    var session = UnityEngine.Object.FindAnyObjectByType<LanGameplaySession>();
                    if (session == null || !session.Service.IsConnected) return;
                    check(session.Adapter.LocalPlayerIndex == 0 && session.Service.IsHost, "LAN host retains Player0");
                    check(!UnityEngine.Object.FindAnyObjectByType<SimpleArtilleryAI>(FindObjectsInactive.Include).enabled, "AI remains disabled after Host startup");
                    check(session.Service.NetworkManager.NetworkConfig.ProtocolVersion == 2, "protocol v2 preserved");
                    navigation.ReturnToMenu(); stage = 7;
                }
                else if (stage == 7 && menu != null)
                {
                    check(UnityEngine.Object.FindObjectsByType<NetworkManager>().Length == 0 && NetworkManager.Singleton == null, "Back waits for NGO shutdown; no stale manager");
                    check(UnityEngine.Object.FindObjectsByType<NgoMultiplayerService>().Length == 0, "no persistent LAN service leaks into menu");
                    menu.SelectMode(GameMode.LanMultiplayer); menu.StartGame(); stage = 8;
                }
                else if (stage == 8 && scene.path == GameSceneNavigation.LanScene && navigation != null)
                {
                    var session = UnityEngine.Object.FindAnyObjectByType<LanGameplaySession>();
                    if (session == null) return;
                    check(!session.Service.IsRunning && UnityEngine.Object.FindObjectsByType<NetworkManager>().Length == 0, "repeated LAN entry is fresh");
                    session.StartHost(7787); stage = 9;
                }
                else if (stage == 9)
                {
                    var session = UnityEngine.Object.FindAnyObjectByType<LanGameplaySession>();
                    if (session == null || !session.Service.IsConnected) return;
                    check(session.Service.ConnectedClientCount == 1, "fresh host can reuse port after Back"); navigation.ReturnToMenu(); stage = 10;
                }
                else if (stage == 10 && menu != null)
                {
                    check(NetworkManager.Singleton == null && UnityEngine.Object.FindObjectsByType<EventSystem>().Length == 0, "final menu has no stale network or input objects");
                    finish(); Debug.Log("PHASE14 MENU AUDIT PASS " + checks); stage = 11;
                }
            }
            catch (Exception error)
            { log.AppendLine("FAIL " + error); finish(); Debug.LogException(error); }
        };
        EditorApplication.update += tick;
        result.Log("Scheduled Phase14 menu/difficulty/scene lifecycle audit.");
    }
}

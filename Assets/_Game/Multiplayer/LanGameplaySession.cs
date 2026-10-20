using System;
using System.Collections.Generic;
using TankGame.AI;
using TankGame.Core;
using TankGame.Presentation;
using TankGame.Projectiles;
using TankGame.Tank;
using TankGame.Terrain;
using TankGame.Testing;
using UnityEngine;
using UnityEngine.UI;

namespace TankGame.Multiplayer
{
    /// <summary>Opt-in test-scene composition. The offline scene does not use this component.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class LanGameplaySession : MonoBehaviour
    {
        [SerializeField] private NgoMultiplayerService service;
        [SerializeField] private TankGameNetworkAdapter adapter;
        [SerializeField] private TankGameStateStream stream;
        [SerializeField] private GameManager game;
        [SerializeField] private TurnManager turns;
        [SerializeField] private MatchFlowController flow;
        [SerializeField] private GameplayActionController actions;
        [SerializeField] private BackendTestBootstrap bootstrap;
        [SerializeField] private SimpleArtilleryAI ai;
        [SerializeField] private IntegratedInput offlineInput;
        [SerializeField] private IntegratedHud offlineHud;
        [SerializeField] private BackendTestDiagnostics diagnostics;
        [SerializeField] private TankController playerZero, playerOne;
        [SerializeField] private Transform aimPivotZero, aimPivotOne;
        [SerializeField] private DestructibleTerrain terrain;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private Image playerFill, enemyFill;
        [SerializeField] private Text playerLabel, enemyLabel, turnLabel;
        private bool used;
        private bool quitting;
        private GameObject replicaProjectile;
        private readonly List<GameObject> replicaEffects = new List<GameObject>();
        public NgoMultiplayerService Service => service;
        public TankGameNetworkAdapter Adapter => adapter;
        public TankGameStateStream Stream => stream;
        public LanAimStream AimStream { get; private set; }
        public LanAimPresentation AimPresentation { get; private set; }

        private void Awake()
        {
            AimStream = gameObject.AddComponent<LanAimStream>(); AimStream.Initialize(service, adapter, playerZero.GetComponent<TankAim>(), playerOne.GetComponent<TankAim>());
            AimPresentation = gameObject.AddComponent<LanAimPresentation>(); AimPresentation.Initialize(this, AimStream, aimPivotZero, aimPivotOne);
            bootstrap.enabled = false; ai.enabled = false;
            offlineInput.enabled = false; offlineHud.enabled = false;
            diagnostics.enabled = false;
            SetSimulation(false);
            foreach (var button in gameObject.scene.GetRootGameObjects())
                foreach (var control in button.GetComponentsInChildren<Button>(true)) control.interactable = false;
            stream.SnapshotReceived += OnSnapshot;
            stream.PresentationReceived += OnPresentation;
            stream.ObservationReset += ResetObservation;
        }

        private void SetSimulation(bool enabled)
        {
            game.enabled = enabled; turns.enabled = enabled; flow.enabled = enabled; actions.enabled = enabled;
            terrain.GetComponent<PolygonCollider2D>().enabled = enabled;
            foreach (var tank in new[] { playerZero, playerOne })
            {
                tank.GetComponent<Rigidbody2D>().simulated = enabled;
                tank.GetComponent<TankMotor>().enabled = enabled;
                tank.GetComponent<TankAim>().enabled = enabled;
                tank.GetComponent<TankGame.Weapons.WeaponController>().enabled = enabled;
                tank.GetComponent<HealthComponent>().enabled = enabled;
                tank.GetComponent<BackendTestPlayerController>().enabled = enabled;
                foreach (var collider in tank.GetComponentsInChildren<Collider2D>(true)) collider.enabled = enabled;
            }
        }

        public bool StartHost(int port = 7777)
        {
            if (used) throw new InvalidOperationException("Reload this test scene for a fresh match/session.");
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            SetSimulation(true);
            service.ConfigureProtocol(GameplayNetworkCommand.ProtocolVersion);
            var first = playerZero.GetComponent<BackendTestPlayerController>();
            var second = playerOne.GetComponent<BackendTestPlayerController>();
            first.Initialize(0); second.Initialize(1);
            adapter.Initialize(service, game, turns, flow, actions, first, second);
            stream.Initialize(service, adapter, game, turns, flow, playerZero, playerOne, terrain);
            used = true;
            bool started = service.StartHost(port);
            if (started) bootstrap.enabled = true;
            return started;
        }

        public bool Connect(string ip, int port = 7777)
        {
            if (used) throw new InvalidOperationException("Reload this test scene for a fresh match/session.");
            // Validation occurs before initialization, so a typo does not consume the session.
            ip = ip?.Trim();
            if (string.IsNullOrEmpty(ip) || ip.Split('.').Length != 4
                || !System.Net.IPAddress.TryParse(ip, out var address)
                || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork
                || address.ToString() != ip || address.GetAddressBytes()[0] == 0 || address.GetAddressBytes()[0] >= 224)
                throw new ArgumentException("Enter a valid IPv4 unicast address.");
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            service.ConfigureProtocol(GameplayNetworkCommand.ProtocolVersion);
            adapter.Initialize(service);
            stream.Initialize(service, adapter);
            used = true;
            return service.StartClient(ip, port);
        }

        public void Disconnect() => service.Disconnect();

        /// <summary>Only the connected host can explicitly restart a completed, non-aborted match.</summary>
        public bool RestartMatch()
        {
            if (!service.IsHost || !service.IsConnected || adapter.SessionAborted || game.CurrentState != MatchState.MatchEnded)
                return false;
            foreach (var projectile in FindObjectsByType<Projectile>())
                if (projectile.gameObject.scene == gameObject.scene)
                {
                    projectile.gameObject.SetActive(false);
                    Destroy(projectile.gameObject);
                }
            bootstrap.RestartTestMatch();
            return true;
        }

        private void OnPresentation(GameplayPresentationEvent entry)
        {
            if (service.IsHost) return;
            if (entry.Kind == GameplayPresentationKind.TerrainCarved)
            {
                terrain.Carve(entry.Position, entry.Radius);
                return;
            }
            if (entry.IsReplay) return;
            var presentation = playerZero.GetComponent<TankGame.Weapons.WeaponController>()
                .CurrentWeapon.ProjectilePrefab.GetComponent<ProjectilePresentation>();
            if (presentation == null || presentation.ExplosionVisual == null) return;
            var effect = Instantiate(presentation.ExplosionVisual, entry.Position, Quaternion.identity);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(effect, gameObject.scene);
            var animator = effect.GetComponent<Animator>();
            animator.Play("Base Layer.Explode", 0, 0f); animator.Update(0f);
            replicaEffects.RemoveAll(value => value == null);
            replicaEffects.Add(effect);
            Destroy(effect, 0.6f);
        }

        private void ResetObservation()
        {
            if (quitting || !isActiveAndEnabled || service == null || service.IsHost) return;
            if (replicaProjectile != null) Destroy(replicaProjectile);
            foreach (var effect in replicaEffects) if (effect != null) Destroy(effect);
            replicaEffects.Clear();
            if (terrain != null && terrain.GetComponent<SpriteRenderer>() != null
                && terrain.GetComponent<PolygonCollider2D>() != null) terrain.ResetSurface();
            if (turnLabel != null) turnLabel.text = service.IsConnected ? "WAITING FOR HOST" : "DISCONNECTED";
        }

        private void OnSnapshot(GameplayNetworkState state)
        {
            if (!service.IsHost)
            {
                ApplyPose(playerZero.transform, aimPivotZero, state.PlayerZero);
                ApplyPose(playerOne.transform, aimPivotOne, state.PlayerOne);
                if (state.ProjectileActive)
                {
                    if (replicaProjectile == null)
                    {
                        replicaProjectile = new GameObject("Authoritative projectile display");
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(replicaProjectile, gameObject.scene);
                        var sprite = replicaProjectile.AddComponent<SpriteRenderer>();
                        sprite.sprite = projectileSprite; sprite.sortingOrder = 20;
                        replicaProjectile.transform.localScale = Vector3.one * 0.25f;
                    }
                    replicaProjectile.transform.position = state.ProjectilePosition;
                    replicaProjectile.transform.rotation = Quaternion.Euler(0, 0,
                        Mathf.Atan2(state.ProjectileVelocity.y, state.ProjectileVelocity.x) * Mathf.Rad2Deg);
                }
                else if (replicaProjectile != null) Destroy(replicaProjectile);
            }
            playerFill.fillAmount = state.PlayerZero.Health / state.PlayerZero.MaxHealth;
            enemyFill.fillAmount = state.PlayerOne.Health / state.PlayerOne.MaxHealth;
            playerLabel.text = "PLAYER " + Mathf.RoundToInt(playerFill.fillAmount * 100) + "%";
            enemyLabel.text = "ENEMY " + Mathf.RoundToInt(enemyFill.fillAmount * 100) + "%";
            turnLabel.text = state.State == MatchState.MatchEnded
                ? state.Outcome == -2 ? "MATCH ENDED" : state.Outcome == -1 ? "DRAW" : "PLAYER " + state.Outcome + " WINS"
                : state.State == MatchState.TurnActive ? "PLAYER " + state.CurrentPlayer + " TURN" : state.State.ToString();
        }

        private static void ApplyPose(Transform root, Transform aimPivot, NetworkTankState state)
        {
            root.SetPositionAndRotation(state.Position, Quaternion.Euler(0, 0, state.Rotation));
            aimPivot.localRotation = Quaternion.Euler(0, 0, state.Aim);
        }

        private void OnApplicationQuit() => quitting = true;

        private void OnDestroy()
        {
            stream.SnapshotReceived -= OnSnapshot;
            stream.PresentationReceived -= OnPresentation;
            stream.ObservationReset -= ResetObservation;
            if (replicaProjectile != null) Destroy(replicaProjectile);
        }
    }
}

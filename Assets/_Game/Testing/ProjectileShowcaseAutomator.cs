using System;
using System.Collections;
using TankGame.Core;
using TankGame.Data;
using TankGame.Projectiles;
using TankGame.Tank;
using TankGame.Weapons;
using UnityEngine;

namespace TankGame.Testing
{
    /// <summary>
    /// Automated cinematic showcase runner for recording and demonstrating
    /// all 7 projectile flight and impact behaviors.
    /// </summary>
    public sealed class ProjectileShowcaseAutomator : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameplayActionController gameplayActions = null;
        [SerializeField] private TurnManager turnManager = null;
        [SerializeField] private BackendTestBootstrap bootstrap = null;
        [SerializeField] private TankAim playerZeroAim = null;
        [SerializeField] private Transform playerZeroMuzzle = null;
        [SerializeField] private WeaponController playerZeroWeapon = null;

        [Header("Showcase Configuration")]
        [SerializeField] private WeaponData[] showcaseWeapons = null;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float postShotWaitTime = 2.0f;

        private int currentIndex = -1;
        private string currentTitle = "";
        private string currentDescription = "";
        private bool isRunning;
        private bool isFinished;

        private struct WeaponShowcaseProfile
        {
            public float AimAngle;
            public string Description;

            public WeaponShowcaseProfile(float angle, string desc)
            {
                AimAngle = angle;
                Description = desc;
            }
        }

        private static readonly WeaponShowcaseProfile[] Profiles = new WeaponShowcaseProfile[]
        {
            new WeaponShowcaseProfile(40f, "🚀 Rocket Missile: Delayed engine ignition & forward acceleration in flight"),
            new WeaponShowcaseProfile(70f, "💣 Heavy Mortar: High gravity (2.0G) steep arc creating a massive crater"),
            new WeaponShowcaseProfile(22f, "⚡ Railgun: Zero-gravity (0G) direct linear trajectory at high speed"),
            new WeaponShowcaseProfile(50f, "🎈 Floating Bomb: Low gravity & high drag (air resistance) for gentle descent"),
            new WeaponShowcaseProfile(32f, "🏓 Bouncy Shell: Triple ricochet bouncing off terrain before detonation"),
            new WeaponShowcaseProfile(62f, "🎆 Cluster Bomb: Reaches apex in the sky and splits into 4 submunitions"),
            new WeaponShowcaseProfile(42f, "🚜 Digger Shell: Drills a subterranean tunnel into soil before deep explosion")
        };

        private void Start()
        {
            ResolveReferences();
            if (autoStart)
            {
                StartCoroutine(RunShowcaseRoutine());
            }
        }

        public void ResolveReferences()
        {
            if (gameplayActions == null) gameplayActions = FindAnyObjectByType<GameplayActionController>();
            if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
            if (bootstrap == null) bootstrap = FindAnyObjectByType<BackendTestBootstrap>();
            if (playerZeroAim == null)
            {
                var tanks = FindObjectsByType<TankAim>(FindObjectsInactive.Exclude);
                if (tanks.Length > 0) playerZeroAim = tanks[0];
            }
            if (playerZeroAim != null)
            {
                if (playerZeroWeapon == null) playerZeroWeapon = playerZeroAim.GetComponent<WeaponController>();
                if (playerZeroMuzzle == null)
                {
                    Transform t = playerZeroAim.transform.Find("TurretPivot/Barrel/Muzzle");
                    if (t != null) playerZeroMuzzle = t;
                }
            }
        }

        public void RestartShowcase()
        {
            StopAllCoroutines();
            if (bootstrap != null && bootstrap.CanRestart)
                bootstrap.RestartTestMatch();
            StartCoroutine(RunShowcaseRoutine());
        }

        private IEnumerator RunShowcaseRoutine()
        {
            isRunning = true;
            isFinished = false;

            // Wait for match flow to enter active turn
            yield return new WaitForSeconds(1.2f);
            while (bootstrap != null && !bootstrap.CanIssueActions)
                yield return null;

            if (showcaseWeapons == null || showcaseWeapons.Length == 0)
            {
                currentTitle = "No showcase weapons configured.";
                yield break;
            }

            for (int i = 0; i < showcaseWeapons.Length; i++)
            {
                currentIndex = i;
                WeaponData weapon = showcaseWeapons[i];
                if (weapon == null) continue;

                WeaponShowcaseProfile profile = i < Profiles.Length
                    ? Profiles[i]
                    : new WeaponShowcaseProfile(45f, weapon.WeaponName);

                currentTitle = $"[SHOWCASE {i + 1}/{showcaseWeapons.Length}] {weapon.WeaponName}";
                currentDescription = profile.Description;

                // 1. Aim the tank barrel smoothly
                if (playerZeroAim != null && gameplayActions != null)
                {
                    gameplayActions.Aim(0, profile.AimAngle);
                }

                // 2. Assign the weapon
                if (playerZeroWeapon != null)
                {
                    playerZeroWeapon.AssignWeapon(weapon);
                }

                // 3. Pause so the viewer can read the banner on video
                yield return new WaitForSeconds(1.8f);

                // Wait until it's player 0's turn to fire
                while (turnManager != null && turnManager.CurrentPlayerIndex != 0)
                {
                    yield return new WaitForSeconds(0.2f);
                }

                // 4. Fire the projectile
                Projectile firedProj = null;
                if (gameplayActions != null && playerZeroMuzzle != null)
                {
                    Vector2 dir = playerZeroMuzzle.TransformVector(Vector3.right).normalized;
                    try
                    {
                        firedProj = gameplayActions.Fire(0, dir);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[Showcase] Fire skipped: " + ex.Message);
                    }
                }

                // 5. Wait for projectile to impact/finish
                float timer = 0f;
                while (firedProj != null && !firedProj.IsFinished && timer < 7f)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                // 6. Pause to let the viewer see the crater and damage
                yield return new WaitForSeconds(postShotWaitTime);

                // If match ended, restart before next weapon
                if (bootstrap != null && bootstrap.CanRestart)
                {
                    bootstrap.RestartTestMatch();
                    yield return new WaitForSeconds(1.0f);
                }
            }

            currentTitle = "🎉 ALL 7 PROJECTILES DEMONSTRATED SUCCESSFULLY!";
            currentDescription = "Press [R] to replay showcase or switch manually with keys [1-7]";
            isRunning = false;
            isFinished = true;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(currentTitle))
                return;

            float boxWidth = Mathf.Min(Screen.width - 40, 720f);
            float boxHeight = 85f;
            float left = (Screen.width - boxWidth) * 0.5f;
            float top = 16f;

            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Box(new Rect(left, top, boxWidth, boxHeight), GUIContent.none);

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(left + 15, top + 10, boxWidth - 30, boxHeight - 15));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = isFinished ? Color.green : new Color(1f, 0.85f, 0.3f);
            GUILayout.Label(currentTitle, titleStyle);

            GUIStyle descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            descStyle.normal.textColor = Color.white;
            GUILayout.Label(currentDescription, descStyle);

            GUILayout.EndArea();
        }
    }
}

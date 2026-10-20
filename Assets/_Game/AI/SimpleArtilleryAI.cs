using System;
using TankGame.Core;
using TankGame.Data;
using TankGame.Player;
using UnityEngine;

namespace TankGame.AI
{
    public sealed class SimpleArtilleryAI : MonoBehaviour
    {
        [SerializeField] private GameplayActionController gameplayActions = null;
        [SerializeField] private GameManager gameManager = null;
        [SerializeField] private TurnManager turnManager = null;
        [SerializeField] private PlayerControllerBase playerController = null;
        [SerializeField] private Transform muzzle = null;
        [SerializeField] private Transform aimReferenceFrame = null;
        [SerializeField] private Transform target = null;
        [SerializeField] private WeaponData weaponData = null;
        [SerializeField, Min(0f)] private float thinkDelay = 0.5f;
        // Retain legacy scene data; the session profile now supplies the shot error.
        [SerializeField, HideInInspector] private float angleErrorRange = 2f;
        [SerializeField, Range(-180f, 180f)] private float minAimAngle = 10f;
        [SerializeField, Range(-180f, 180f)] private float maxAimAngle = 170f;
        private bool pendingShot;
        private bool firedThisTurn;
        private float fireTime;
        private AIDifficultyProfile difficultyProfile;
        public AIDifficulty Difficulty { get; private set; }

        private void Awake()
        {
            Difficulty = GameSessionConfiguration.Difficulty;
            difficultyProfile = AIDifficultyProfile.For(Difficulty);
        }

        private void Start()
        {
            if (gameplayActions == null || gameManager == null || turnManager == null || playerController == null
                || muzzle == null || aimReferenceFrame == null || target == null || weaponData == null
                || weaponData.ProjectilePrefab == null || weaponData.ProjectilePrefab.GetComponent<Rigidbody2D>() == null)
                throw new InvalidOperationException("Assign all SimpleArtilleryAI references and a physics projectile weapon.");
            ValidateConfiguration();
            ScheduleTurn();
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged += OnStateChanged;
                ScheduleTurn();
            }
        }

        private void OnDisable()
        {
            pendingShot = false;
            if (gameManager != null) gameManager.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(MatchState previous, MatchState current)
        {
            pendingShot = false;
            if (current == MatchState.TurnActive) ScheduleTurn();
        }

        private void ScheduleTurn()
        {
            if (!CanAct()) return;
            firedThisTurn = false;
            pendingShot = true;
            fireTime = Time.time + thinkDelay;
        }

        private bool CanAct()
        {
            return isActiveAndEnabled && gameManager != null && turnManager != null && playerController != null
                && gameManager.CurrentState == MatchState.TurnActive
                && turnManager.CurrentPlayerIndex == playerController.PlayerIndex && playerController.CanAct;
        }

        private void Update()
        {
            if (!pendingShot || firedThisTurn) return;
            if (!CanAct())
            {
                pendingShot = false;
                return;
            }
            if (Time.time < fireTime) return;

            pendingShot = false;
            firedThisTurn = true;
            int index = playerController.PlayerIndex;
            float angle = 0f;
            // Recalculate once after aiming because rotation also moves the muzzle origin.
            for (int pass = 0; pass < 2; pass++)
            {
                angle = CalculateLocalAimAngle();
                gameplayActions.Aim(index, angle);
                if (!CanAct()) return;
            }
            angle = difficultyProfile.ApplyAngle(angle, UnityEngine.Random.Range(-1f, 1f), minAimAngle, maxAimAngle);
            gameplayActions.Aim(index, angle);
            if (!CanAct()) return;
            gameplayActions.Fire(index, (Vector2)muzzle.TransformVector(Vector3.right).normalized,
                difficultyProfile.GetPower(weaponData, UnityEngine.Random.Range(-1f, 1f)));
        }

        private float CalculateLocalAimAngle()
        {
            Vector2 displacement = target.position - muzzle.position;
            float horizontal = Mathf.Abs(displacement.x);
            float gravityScale = weaponData.ProjectilePrefab.GetComponent<Rigidbody2D>().gravityScale;
            double gravity = -Physics2D.gravity.y * gravityScale;
            double speedSquared = (double)weaponData.LaunchSpeed * weaponData.LaunchSpeed;
            float elevation = 45f;
            if (horizontal < 0.01f)
            {
                elevation = 90f;
            }
            else if (gravity > 0.0001 && speedSquared > 0.0001
                && Mathf.Abs(Physics2D.gravity.x * gravityScale) < 0.0001f)
            {
                double discriminant = speedSquared * speedSquared
                    - gravity * (gravity * horizontal * horizontal + 2d * displacement.y * speedSquared);
                if (discriminant >= 0d)
                    elevation = (float)(Math.Atan((speedSquared + Math.Sqrt(discriminant)) / (gravity * horizontal)) * Mathf.Rad2Deg);
            }
            else if (Math.Abs(gravity) < 0.0001 && Mathf.Abs(Physics2D.gravity.x * gravityScale) < 0.0001f)
            {
                elevation = Mathf.Atan2(displacement.y, horizontal) * Mathf.Rad2Deg;
            }

            float radians = elevation * Mathf.Deg2Rad;
            Vector3 worldDirection = new Vector3((displacement.x < 0f ? -1f : 1f) * Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            Vector3 localDirection = aimReferenceFrame.InverseTransformVector(worldDirection);
            return Mathf.Clamp(Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg, minAimAngle, maxAimAngle);
        }

        private void OnValidate() => ValidateConfiguration();

        private void ValidateConfiguration()
        {
            if (float.IsNaN(thinkDelay) || float.IsInfinity(thinkDelay)) thinkDelay = 0.5f;
            if (float.IsNaN(minAimAngle) || float.IsInfinity(minAimAngle)) minAimAngle = 10f;
            if (float.IsNaN(maxAimAngle) || float.IsInfinity(maxAimAngle)) maxAimAngle = 170f;
            thinkDelay = Mathf.Max(0f, thinkDelay);
            angleErrorRange = float.IsFinite(angleErrorRange) ? Mathf.Clamp(angleErrorRange, 0f, 10f) : 2f;
            minAimAngle = Mathf.Clamp(minAimAngle, -180f, 180f);
            maxAimAngle = Mathf.Clamp(maxAimAngle, minAimAngle, 180f);
        }
    }
}

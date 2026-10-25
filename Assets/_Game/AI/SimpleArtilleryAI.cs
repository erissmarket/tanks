using System;
using TankGame.Core;
using TankGame.Data;
using TankGame.Player;
using TankGame.Tank;
using TankGame.Terrain;
using TankGame.Weapons;
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
        [SerializeField, Range(0f, 10f)] private float angleErrorRange = 2f;
        [SerializeField, Range(0f, 90f)] private float minAimAngle = 10f;
        [SerializeField, Range(0f, 90f)] private float maxAimAngle = 90f;
        [SerializeField, Min(0f)] private float maximumRepositionDistance = 1.25f;
        private bool pendingShot;
        private bool firedThisTurn;
        private bool movementPlanned, moving;
        private float fireTime;
        private float destinationX, moveDeadline;
        private TankMotor motor;
        private WeaponController weapons;
        private MatchFlowController match;
        private DestructibleTerrain terrain;
        private LavaHazard lava;
        public bool IsMoving => moving;
        public float DestinationX => destinationX;

        private void Start()
        {
            if (gameplayActions == null || gameManager == null || turnManager == null || playerController == null
                || muzzle == null || aimReferenceFrame == null || target == null || weaponData == null
                || weaponData.ProjectilePrefab == null || weaponData.ProjectilePrefab.GetComponent<Rigidbody2D>() == null)
                throw new InvalidOperationException("Assign all SimpleArtilleryAI references and a physics projectile weapon.");
            ValidateConfiguration();
            CacheMovementReferences();
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
            StopMovement();
            if (gameManager != null) gameManager.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(MatchState previous, MatchState current)
        {
            pendingShot = false;
            StopMovement();
            if (current == MatchState.TurnActive) ScheduleTurn();
        }

        private void ScheduleTurn()
        {
            if (!CanAct()) return;
            firedThisTurn = false;
            movementPlanned = false;
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
            if (gameplayActions != null && gameplayActions.IsPaused) return;
            if (!pendingShot || firedThisTurn) return;
            if (!CanAct())
            {
                pendingShot = false;
                StopMovement();
                return;
            }
            if (Time.time < fireTime) return;

            if (!movementPlanned)
            {
                movementPlanned = true;
                if (PlanMovement()) return;
            }
            if (moving)
            {
                float delta = destinationX - playerController.transform.position.x;
                if (Mathf.Abs(delta) > .08f && motor.RemainingMovement > .05f
                    && Time.time < moveDeadline && match.RemainingTurnSeconds > 1f && CanTraverseTo(destinationX))
                {
                    gameplayActions.Move(playerController.PlayerIndex, Mathf.Sign(delta));
                    return;
                }
                StopMovement(); fireTime = Time.time + .20f;
                return;
            }

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
            angle = Mathf.Clamp(angle + UnityEngine.Random.Range(-angleErrorRange, angleErrorRange), minAimAngle, maxAimAngle);
            gameplayActions.Aim(index, angle);
            if (!CanAct()) return;
            gameplayActions.Fire(index, (Vector2)muzzle.TransformVector(Vector3.right).normalized);
        }

        private void CacheMovementReferences()
        {
            if (playerController == null) return;
            motor = playerController.GetComponent<TankMotor>();
            weapons = playerController.GetComponent<WeaponController>();
            match = FindAnyObjectByType<MatchFlowController>();
            terrain = FindAnyObjectByType<DestructibleTerrain>();
            lava = FindAnyObjectByType<LavaHazard>();
        }

        private bool PlanMovement()
        {
            if (motor == null || match == null || target == null || terrain == null
                || motor.RemainingMovement < .3f || match.RemainingTurnSeconds < 3f) return false;
            float currentX = playerController.transform.position.x;
            float toward = Mathf.Sign(target.position.x - currentX);
            float gap = Mathf.Abs(target.position.x - currentX);
            float preferred = gap > 7f ? toward : gap < 4.5f ? -toward
                : ((match.CompletedTurns / 2) % 2 == 0 ? toward : -toward);
            float distance = Mathf.Min(maximumRepositionDistance, motor.RemainingMovement * .6f);
            for (int side = 0; side < 2; side++)
                for (float step = distance; step >= .30f; step -= .30f)
                {
                    float candidate = currentX + preferred * (side == 0 ? 1 : -1) * step;
                    if (Mathf.Abs(candidate - target.position.x) < 3.5f || !CanTraverseTo(candidate)) continue;
                    destinationX = candidate; moving = true; moveDeadline = Time.time + 1.8f;
                    gameplayActions.Move(playerController.PlayerIndex, Mathf.Sign(candidate - currentX));
                    return true;
                }
            return false;
        }

        // Inspect the whole route and track footprint, rather than only its destination.
        public bool CanTraverseTo(float x)
        {
            if (!float.IsFinite(x) || terrain == null || match == null || playerController == null) return false;
            float start = playerController.transform.position.x;
            var hull = playerController.GetComponent<Collider2D>();
            float halfWidth = hull != null && hull.enabled ? hull.bounds.extents.x : .85f;
            var bounds = terrain.GetComponent<SpriteRenderer>().bounds;
            if (x < match.Rules.minimumX || x > match.Rules.maximumX
                || Mathf.Min(start, x) - halfWidth < bounds.min.x || Mathf.Max(start, x) + halfWidth > bounds.max.x) return false;
            int samples = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(x - start) / .15f));
            float previous = terrain.SurfaceHeightAt(start);
            for (int i = 0; i <= samples; i++)
            {
                float px = Mathf.Lerp(start, x, (float)i / samples);
                float height = terrain.SurfaceHeightAt(px);
                float spacing = Mathf.Max(.02f, Mathf.Abs(x - start) / samples);
                if (i > 0 && Mathf.Abs(height - previous) > spacing * .84f + .025f) return false;
                for (int foot = -1; foot <= 1; foot++)
                {
                    float support = terrain.SurfaceHeightAt(px + foot * halfWidth * .8f);
                    if (Mathf.Abs(support - height) > .42f || (lava != null && support <= lava.SurfaceY + .3f)) return false;
                }
                previous = height;
            }
            return true;
        }

        private void StopMovement()
        {
            moving = false;
            if (motor != null) motor.Stop();
        }

        private float CalculateLocalAimAngle()
        {
            Vector2 displacement = target.position - muzzle.position;
            float horizontal = Mathf.Abs(displacement.x);
            var shotWeapon = weapons != null && weapons.CurrentWeapon != null ? weapons.CurrentWeapon : weaponData;
            float gravityScale = shotWeapon.ProjectilePrefab.GetComponent<Rigidbody2D>().gravityScale;
            double gravity = -Physics2D.gravity.y * gravityScale;
            double speedSquared = (double)shotWeapon.LaunchSpeed * shotWeapon.LaunchSpeed;
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
            if (float.IsNaN(angleErrorRange) || float.IsInfinity(angleErrorRange)) angleErrorRange = 2f;
            if (float.IsNaN(minAimAngle) || float.IsInfinity(minAimAngle)) minAimAngle = 10f;
            if (float.IsNaN(maxAimAngle) || float.IsInfinity(maxAimAngle)) maxAimAngle = 90f;
            thinkDelay = Mathf.Max(0f, thinkDelay);
            angleErrorRange = Mathf.Clamp(angleErrorRange, 0f, 10f);
            minAimAngle = Mathf.Clamp(minAimAngle, 0f, 90f);
            maxAimAngle = Mathf.Clamp(maxAimAngle, minAimAngle, 90f);
            maximumRepositionDistance = float.IsFinite(maximumRepositionDistance) ? Mathf.Clamp(maximumRepositionDistance, 0f, 2.5f) : 1.25f;
        }
    }
}

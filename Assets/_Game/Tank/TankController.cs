using System;
using UnityEngine;

namespace TankGame.Tank
{
    public sealed class TankController : MonoBehaviour
    {
        [SerializeField] private TankMotor motor;
        [SerializeField] private TankAim aim;

        public int PlayerIndex { get; private set; } = -1;
        public TankMotor Motor => motor;
        public TankAim Aim => aim;

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<TankMotor>();
            if (aim == null)
                aim = GetComponent<TankAim>();
        }

        /// <summary>Assigns ownership by index without retaining a controller reference.</summary>
        public void Initialize(int playerIndex)
        {
            if (playerIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));

            PlayerIndex = playerIndex;
        }
    }
}

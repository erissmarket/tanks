using System;
using UnityEngine;

namespace TankGame.Player
{
    public abstract class PlayerControllerBase : MonoBehaviour, IPlayerController
    {
        [SerializeField, Min(0)] private int playerIndex;
        private bool controlEnabled;

        public int PlayerIndex => playerIndex;
        public bool CanAct => controlEnabled && isActiveAndEnabled;

        public void Initialize(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            playerIndex = index;
            DisableControl();
        }

        public void EnableControl()
        {
            controlEnabled = isActiveAndEnabled;
        }

        public void DisableControl()
        {
            controlEnabled = false;
        }

        protected virtual void OnDisable()
        {
            DisableControl();
        }

        protected virtual void OnValidate()
        {
            playerIndex = Mathf.Max(0, playerIndex);
        }
    }
}

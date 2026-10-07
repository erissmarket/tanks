using System;
using UnityEngine;

namespace TankGame.Core
{
    public sealed class TurnManager : MonoBehaviour
    {
        public const int PlayerCount = 2;

        // -1 means turns have not started; players use indices 0 and 1.
        public int CurrentPlayerIndex { get; private set; } = -1;

        public event Action<int> TurnChanged;

        /// <summary>Starts or restarts turn order with player zero.</summary>
        public void StartFirstTurn()
        {
            CurrentPlayerIndex = 0;
            TurnChanged?.Invoke(CurrentPlayerIndex);
        }

        public void AdvanceTurn()
        {
            if (CurrentPlayerIndex < 0)
                throw new InvalidOperationException("Start the first turn before advancing turns.");

            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % PlayerCount;
            TurnChanged?.Invoke(CurrentPlayerIndex);
        }
    }
}

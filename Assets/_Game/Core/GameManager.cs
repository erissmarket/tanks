using System;
using UnityEngine;

namespace TankGame.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public MatchState CurrentState { get; private set; } = MatchState.None;

        // Arguments are the previous state and the new state.
        public event Action<MatchState, MatchState> StateChanged;

        public void ChangeState(MatchState nextState)
        {
            if (!Enum.IsDefined(typeof(MatchState), nextState))
                throw new ArgumentOutOfRangeException(nameof(nextState));

            if (CurrentState == nextState)
                return;

            MatchState previousState = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previousState, nextState);
        }
    }
}

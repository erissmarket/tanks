using System;
using System.Collections.Generic;
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
            Action<MatchState, MatchState> handlers = StateChanged;
            if (handlers == null)
                return;

            // One failing listener must not prevent lifecycle cleanup in other listeners.
            List<Exception> errors = null;
            foreach (Action<MatchState, MatchState> handler in handlers.GetInvocationList())
            {
                try { handler(previousState, nextState); }
                catch (Exception error)
                {
                    if (errors == null) errors = new List<Exception>();
                    errors.Add(error);
                }
            }
            if (errors != null)
                throw new AggregateException("Match state changed, but a state listener failed.", errors);
        }
    }
}

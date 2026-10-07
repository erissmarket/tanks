using System;

namespace TankGame.Core
{
    public sealed class MatchResult
    {
        public int? WinnerPlayerIndex { get; }
        public int? LoserPlayerIndex { get; }
        public bool IsDraw => !WinnerPlayerIndex.HasValue;

        /// <summary>Both indices are null for a draw; otherwise they must be opposing players 0 and 1.</summary>
        public MatchResult(int? winnerPlayerIndex, int? loserPlayerIndex)
        {
            if (winnerPlayerIndex.HasValue != loserPlayerIndex.HasValue
                || (winnerPlayerIndex.HasValue && (winnerPlayerIndex.Value < 0 || winnerPlayerIndex.Value > 1
                    || loserPlayerIndex.Value != 1 - winnerPlayerIndex.Value)))
                throw new ArgumentException("Use opposing indices 0 and 1, or two null indices for a draw.");

            WinnerPlayerIndex = winnerPlayerIndex;
            LoserPlayerIndex = loserPlayerIndex;
        }
    }
}

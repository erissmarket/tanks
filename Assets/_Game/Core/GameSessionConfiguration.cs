using System;
using TankGame.AI;
using UnityEngine;

namespace TankGame.Core
{
    public enum GameMode { VsComputer, LanMultiplayer }

    /// <summary>Menu choices for this application session; no gameplay objects persist across scenes.</summary>
    public static class GameSessionConfiguration
    {
        public static GameMode Mode { get; private set; } = GameMode.VsComputer;
        public static AIDifficulty Difficulty { get; private set; } = AIDifficulty.Normal;

        public static void SelectMode(GameMode mode)
        {
            if (!Enum.IsDefined(typeof(GameMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            Mode = mode;
        }

        public static void SelectDifficulty(AIDifficulty difficulty)
        {
            if (!Enum.IsDefined(typeof(AIDifficulty), difficulty)) throw new ArgumentOutOfRangeException(nameof(difficulty));
            Difficulty = difficulty;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Mode = GameMode.VsComputer;
            Difficulty = AIDifficulty.Normal;
        }
    }
}

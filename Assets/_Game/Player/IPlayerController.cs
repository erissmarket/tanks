namespace TankGame.Player
{
    public interface IPlayerController
    {
        int PlayerIndex { get; }
        bool CanAct { get; }
        void EnableControl();
        void DisableControl();
    }
}

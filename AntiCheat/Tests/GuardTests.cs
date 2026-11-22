using System;
using TankGame.AntiCheat;

internal static class GuardTests
{
    private static int passed;
    private static AuthoritySnapshot state = new AuthoritySnapshot
    {
        Session = 7, Epoch = 1, Mode = BattleMode.TurnBased, MatchActive = true, CanAct = true,
        ActivePlayer = 0, WeaponCount = 10, ItemCount = 3, SelectedWeaponAmmunition = 2,
        FuelRemaining = 4, RequestedSelectionAvailable = true
    };

    private static ActionIntent Intent(ulong sequence = 1, int player = 0, ActionKind kind = ActionKind.Fire,
        float x = 1, float y = 0, float power = 1, ulong session = 7, ulong epoch = 1)
        => new ActionIntent(session, epoch, sequence, player, kind, x, y, power);

    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL " + name);
        passed++;
        Console.WriteLine("PASS " + name);
    }

    private static void Reason(Rejection expected, ActionIntent intent, string name, ulong sender = 20)
    {
        var guard = new ActionGuard(7, 20, 30);
        Check(guard.Process(sender, intent, _ => state, _ => true).Reason == expected, name);
    }

    public static int Main()
    {
        Reason(Rejection.None, Intent(), "legal turn fire");
        Reason(Rejection.UnknownPeer, Intent(), "unknown transport identity", 99);
        Reason(Rejection.WrongOwner, Intent(player: 1), "spoofed player");
        Reason(Rejection.WrongSession, Intent(session: 9), "wrong session");
        Reason(Rejection.WrongSession, Intent(epoch: 2), "stale turn epoch");
        Reason(Rejection.StaleSequence, Intent(sequence: 0), "zero sequence");
        Reason(Rejection.InvalidCommand, Intent(x: float.NaN), "NaN");
        Reason(Rejection.InvalidCommand, Intent(y: float.PositiveInfinity), "infinity");
        Reason(Rejection.InvalidCommand, Intent(x: 0), "zero firing vector");
        Reason(Rejection.InvalidCommand, Intent(x: .5f), "unnormalized vector");
        Reason(Rejection.InvalidCommand, Intent(power: 2), "excess power");
        Reason(Rejection.InvalidCommand, Intent(kind: (ActionKind)255), "unknown action");
        Reason(Rejection.InvalidCommand, Intent(kind: ActionKind.SelectWeapon, x: .5f), "fractional slot");
        Reason(Rejection.UnavailableSelection, Intent(kind: ActionKind.SelectWeapon, x: 10), "out of range slot");
        state.Paused = true;
        Reason(Rejection.InactiveMatch, Intent(), "paused match");
        state.Paused = false;
        state.ActivePlayer = 1;
        Reason(Rejection.WrongTurn, Intent(), "inactive player");
        state.Mode = BattleMode.Simultaneous;
        Reason(Rejection.None, Intent(), "simultaneous player zero");
        Reason(Rejection.None, Intent(player: 1), "simultaneous player one", 30);
        state.ReloadRemaining = 2;
        Reason(Rejection.Reloading, Intent(), "server cooldown");
        state.ReloadRemaining = 0;
        state.SelectedWeaponAmmunition = 0;
        Reason(Rejection.NoAmmunition, Intent(), "empty ammunition");
        state.SelectedWeaponAmmunition = -1;
        Reason(Rejection.None, Intent(), "unlimited ammunition");
        state.FuelRemaining = 0;
        Reason(Rejection.NoFuel, Intent(kind: ActionKind.Move), "empty movement fuel");
        Reason(Rejection.None, Intent(kind: ActionKind.Stop, x: 0), "stop without fuel");
        var guard = new ActionGuard(7, 20, 30);
        int calls = 0;
        Check(guard.Process(20, Intent(), _ => state, _ => { calls++; return true; }).Accepted, "dispatch");
        Check(guard.Process(20, Intent(), _ => state, _ => { calls++; return true; }).Reason == Rejection.StaleSequence
            && calls == 1, "replay dispatches once");
        guard = new ActionGuard(7, 20, 30);
        Check(guard.Process(20, Intent(), _ => state, _ => false).Reason == Rejection.GameplayRejected, "gameplay rejection");
        Check(guard.Process(20, Intent(), _ => state, _ => true).Reason == Rejection.StaleSequence, "rejected dispatch consumed");
        guard = new ActionGuard(7, 20, 30);
        Check(guard.Process(20, Intent(), _ => state, _ =>
            guard.Process(20, Intent(2), p => state, c => true).Reason == Rejection.Busy).Accepted,
            "callback reentrancy blocked");
        state.MatchActive = false;
        Reason(Rejection.InactiveMatch, Intent(), "ended match");
        Console.WriteLine(passed + " checks passed");
        return 0;
    }
}

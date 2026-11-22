using System;
using System.Diagnostics;

namespace TankGame.AntiCheat
{
    /// <summary>One guard per match session. Serialize validation and gameplay dispatch on the host.</summary>
    public sealed class ActionGuard
    {
        private readonly object gate = new object();
        private readonly ulong[] peers;
        private readonly ulong session;
        private readonly ulong[,] sequences = new ulong[2, 2];
        private readonly Func<double> authorityClock;
        private readonly double[] tokens = new double[2];
        private readonly double[] refillTime = new double[2];
        private readonly int burst;
        private readonly double commandsPerSecond;
        private double lastTime;
        private bool dispatching;

        public ActionGuard(ulong session, ulong playerZeroPeer, ulong playerOnePeer,
            Func<double> authorityClock = null, int burst = 48, double commandsPerSecond = 60)
        {
            if (session == 0 || playerZeroPeer == playerOnePeer)
                throw new ArgumentException("Use a nonzero session and two distinct authenticated peers.");
            if (burst < 1 || burst > 10000 || double.IsNaN(commandsPerSecond)
                || double.IsInfinity(commandsPerSecond) || commandsPerSecond <= 0 || commandsPerSecond > 10000)
                throw new ArgumentOutOfRangeException(nameof(burst));
            var watch = Stopwatch.StartNew();
            this.authorityClock = authorityClock ?? (() => watch.Elapsed.TotalSeconds);
            double now = this.authorityClock();
            if (!ValidTime(now)) throw new ArgumentException("Authority clock must return finite nonnegative seconds.");
            lastTime = now;
            this.burst = burst;
            this.commandsPerSecond = commandsPerSecond;
            for (int i = 0; i < 2; i++)
            {
                tokens[i] = burst;
                refillTime[i] = now;
            }
            this.session = session;
            peers = new[] { playerZeroPeer, playerOnePeer };
        }

        /// <summary>
        /// snapshot and execute are trusted host callbacks. Execute must use the existing gameplay
        /// action boundary, which owns cooldown, ammunition, movement and damage transactions.
        /// A dispatched sequence is consumed even if gameplay rejects or throws.
        /// </summary>
        public ActionDecision Process(ulong authenticatedSender, ActionIntent intent,
            Func<int, AuthoritySnapshot> snapshot, Func<ActionIntent, bool> execute)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            lock (gate)
            {
                if (dispatching) return new ActionDecision(Rejection.Busy);
                int slot = Array.IndexOf(peers, authenticatedSender);
                if (slot < 0) return new ActionDecision(Rejection.UnknownPeer);
                double now = authorityClock();
                if (!ValidTime(now) || now < lastTime) return new ActionDecision(Rejection.InvalidHostTime);
                lastTime = now;
                // Charge authenticated traffic before payload validation. Stop retains its safety path.
                tokens[slot] = Math.Min(burst, tokens[slot] + (now - refillTime[slot]) * commandsPerSecond);
                refillTime[slot] = now;
                bool stop = intent.Kind == ActionKind.Stop && intent.Player == slot && Valid(intent)
                    && intent.Session == session;
                if (!stop && tokens[slot] < 1) return new ActionDecision(Rejection.RateLimited);
                if (!stop) tokens[slot]--;
                if (intent.Player != slot) return new ActionDecision(Rejection.WrongOwner);
                if (!Valid(intent)) return new ActionDecision(Rejection.InvalidCommand);
                if (intent.Session != session) return new ActionDecision(Rejection.WrongSession);
                // Unreliable aim samples cannot invalidate ordered reliable actions.
                int lane = intent.Kind == ActionKind.Aim ? 1 : 0;
                if (intent.Sequence == 0 || intent.Sequence <= sequences[slot, lane])
                    return new ActionDecision(Rejection.StaleSequence);

                dispatching = true;
                try
                {
                    var state = snapshot(slot);
                    var reason = ValidateState(intent, state);
                    if (reason != Rejection.None) return new ActionDecision(reason);
                    sequences[slot, lane] = intent.Sequence;
                    return new ActionDecision(execute(intent) ? Rejection.None : Rejection.GameplayRejected);
                }
                finally { dispatching = false; }
            }
        }

        private Rejection ValidateState(ActionIntent intent, AuthoritySnapshot state)
        {
            if (state.Session != session || state.Epoch == 0 || intent.Epoch != state.Epoch)
                return Rejection.WrongSession;
            if (!state.MatchActive || state.Paused) return Rejection.InactiveMatch;
            if (state.Mode != BattleMode.TurnBased && state.Mode != BattleMode.Simultaneous)
                return Rejection.InactiveMatch;
            if (state.Mode == BattleMode.TurnBased && state.ActivePlayer != intent.Player)
                return Rejection.WrongTurn;
            if (!state.CanAct) return Rejection.NoControl;

            switch (intent.Kind)
            {
                case ActionKind.Move:
                    if (intent.X != 0 && (!Finite(state.FuelRemaining) || state.FuelRemaining <= 0))
                        return Rejection.NoFuel;
                    break;
                case ActionKind.Fire:
                    if (!Finite(state.ReloadRemaining) || state.ReloadRemaining < 0)
                        return Rejection.Reloading;
                    if (state.ReloadRemaining > 0) return Rejection.Reloading;
                    if (state.SelectedWeaponAmmunition < -1 || state.SelectedWeaponAmmunition == 0)
                        return Rejection.NoAmmunition;
                    break;
                case ActionKind.SelectWeapon:
                    if (intent.X >= state.WeaponCount || !state.RequestedSelectionAvailable)
                        return Rejection.UnavailableSelection;
                    break;
                case ActionKind.UseItem:
                    if (intent.X >= state.ItemCount || !state.RequestedSelectionAvailable)
                        return Rejection.UnavailableSelection;
                    break;
            }
            return Rejection.None;
        }

        private static bool Valid(ActionIntent intent)
        {
            if (intent.Player < 0 || intent.Player > 1 || !Finite(intent.X) || !Finite(intent.Y)
                || !Finite(intent.Power) || intent.Power < 0 || intent.Power > 1) return false;
            switch (intent.Kind)
            {
                case ActionKind.Move: return intent.X >= -1 && intent.X <= 1 && intent.Y == 0;
                case ActionKind.Stop:
                case ActionKind.Surrender: return intent.X == 0 && intent.Y == 0;
                case ActionKind.Aim:
                case ActionKind.Fire:
                    double magnitude = (double)intent.X * intent.X + (double)intent.Y * intent.Y;
                    return Math.Abs(intent.X) <= 1 && Math.Abs(intent.Y) <= 1
                        && Math.Abs(magnitude - 1) <= .01;
                case ActionKind.SelectWeapon:
                case ActionKind.UseItem: return intent.Y == 0 && intent.X >= 0
                    && (double)intent.X <= int.MaxValue && Math.Floor(intent.X) == intent.X;
                default: return false;
            }
        }

        private static bool ValidTime(double value)
            => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

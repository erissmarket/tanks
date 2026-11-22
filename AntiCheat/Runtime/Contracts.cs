using System;

namespace TankGame.AntiCheat
{
    public enum BattleMode { TurnBased, Simultaneous }
    public enum ActionKind { Move, Stop, Aim, Fire, SelectWeapon, UseItem, Surrender }
    public enum Rejection
    {
        None, UnknownPeer, WrongOwner, InvalidCommand, WrongSession, StaleSequence,
        InactiveMatch, WrongTurn, NoControl, NoFuel, Reloading, NoAmmunition,
        UnavailableSelection, Busy, GameplayRejected, RateLimited, InvalidHostTime
    }

    /// <summary>Intent only. Damage, positions, resources and timestamps come from the authority.</summary>
    public readonly struct ActionIntent
    {
        public readonly ulong Session, Epoch, Sequence;
        public readonly int Player;
        public readonly ActionKind Kind;
        public readonly float X, Y, Power;

        public ActionIntent(ulong session, ulong epoch, ulong sequence, int player,
            ActionKind kind, float x = 0, float y = 0, float power = 1)
        {
            Session = session;
            Epoch = epoch;
            Sequence = sequence;
            Player = player;
            Kind = kind;
            X = x;
            Y = y;
            Power = power;
        }
    }

    /// <summary>Value snapshot supplied afresh by the authoritative gameplay adapter for each intent.</summary>
    public struct AuthoritySnapshot
    {
        public ulong Session, Epoch;
        public BattleMode Mode;
        public bool MatchActive, Paused, CanAct;
        public int ActivePlayer, WeaponCount, ItemCount, SelectedWeaponAmmunition;
        public float FuelRemaining, ReloadRemaining;
        public bool RequestedSelectionAvailable;
    }

    public readonly struct ActionDecision
    {
        public bool Accepted => Reason == Rejection.None;
        public Rejection Reason { get; }
        public ActionDecision(Rejection reason) { Reason = reason; }
    }
}

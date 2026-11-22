# Tank anti-cheat module

AntiCheat is a standalone C# module published in the Tank repository outside Unity Assets. It supports turn-based and simultaneous battles, with no Unity, NGO or Android package dependency. Development originated on codex/anticheat-foundation. Gameplay integration remains a separate change.

## Authority boundary

Create one ActionGuard for each match with a nonzero session token and the two transport-authenticated peer identities. A transport adapter must obtain authenticatedSender from the connection, never from the incoming payload.

Call Process with an ActionIntent, a fresh host-owned AuthoritySnapshot and an execution callback. Populate RequestedSelectionAvailable for this specific command from authoritative weapon/item inventory. The guard supports TurnBased and Simultaneous policies. Refresh Epoch on match restart and turn transitions. Both simultaneous players share the current host epoch. A new match needs a fresh guard with a new session token. Peer ownership is fixed for the guard lifetime.

Aim uses a separate monotonically increasing sequence lane from reliable actions. Map the existing disposable AimPower channel to ActionKind.Aim with a world direction. Convert the legacy numeric Aim angle to a host-constrained normalized direction before making this intent. Do not map a reliable fire to the aim lane. This avoids an unreliable aim sample invalidating a delayed reliable fire.

The enhanced guard uses a host-only monotonic clock, with a default burst of 48 commands and a sustained 60 commands per second per authenticated peer. Malformed authenticated commands also consume quota. Tune these limits using measured traffic. Quota exhaustion leaves a validated Stop path available, subject to session, epoch, ownership, match authorization and replay checks. This exception prevents command-rate pressure from delaying a safety stop.

Supply a host clock for deterministic testing, or omit it to use Stopwatch. Backward or nonfinite clock values reject processing. No client clock participates. Remote payload size and deserialize bounds must be enforced in the transport before Process, and transport connection/request flood limits are a separate boundary.

The execution callback routes through GameplayActionController. The callback owns atomic gameplay changes, including consuming ammunition, starting reload, accounting movement and creating projectiles. It returns true only when the action succeeds. The guard serializes callbacks and consumes every dispatched sequence, including callbacks that reject or throw. Unexpected callback exceptions propagate. Retrying a failed action requires a new sequence.

Damage, health, movement distance, projectile trajectory, terrain and rewards must be calculated on the authority. This module accepts intent only. It does not accept client-reported damage, position or clock values. Existing authoritative movement accounting and timeout leases remain necessary to enforce speed and fuel.

A malicious LAN host controls its own simulation. Host-side validation protects the host from client commands. Protecting online matches from a malicious host requires a trusted server running the same gameplay and validation.

## Verification

Run Tests/Run.ps1 with PowerShell. It uses the C# compiler and Mono bundled with Unity 6000.5.10f1. Supply -UnityEditor to select another Editor installation. Compilation treats warnings as errors. Generated test executables remain ignored in TestOutput.

54 focused checks pass, including concurrent replay, aim/reliable reordering, cooldown and fuel checks, invalid inputs, ownership spoofing, command bursts, Stop during quota exhaustion, callback exceptions and session restart.

The module is tested independently. It has not been connected to the live Tank gameplay or validated on Android/real network clients. Integration requires a transport adapter, current game snapshots and authoritative gameplay dispatch.

## Git isolation

All module files live under AntiCheat. Publication brings the module and project README onto main. The Unity gameplay tree is preserved from the existing main branch. The original backend checkout and anti-cheat worktree retain their independent branches and local work.

## Current Tank adapter mapping

The implementation was informed by the current uncommitted dev/backend transport, including its protocol 12 intents and GameplayActionController boundary. The original anti-cheat worktree was based on 98a271e. The published main branch retains its own gameplay baseline and does not include the later uncommitted backend implementation. The new module compiles in isolation with Unity's bundled C# compiler and Mono.

An eventual adapter must read connection ownership, session/turn epoch, flow state and CanAct from the host. It must read selected weapon ammunition, item availability, remaining reload and fuel from authoritative gameplay. Populate the value snapshot at the time the guarded callback runs. Run the guard and Unity callbacks on Unity's main thread. The lock makes the standalone boundary serialize callers; it does not make Unity APIs thread-safe.

A rejection is a command result, not proof warranting an automatic ban. Resource checks in this layer are preliminary checks. Gameplay must still atomically enforce spending and movement while dispatching the accepted action. Do not run network await operations inside the synchronous callback. A boolean false does not roll back mutations performed by the callback.

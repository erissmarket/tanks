# Tank anti-cheat module

This worktree belongs to the Tank Git repository. Branch codex/anticheat-foundation starts at 98a271e. AntiCheat is a standalone C# module outside Unity Assets, with no Unity, NGO or Android package dependency. The current dev/backend working tree has substantial uncommitted work. This branch intentionally isolates the module until an integration change is reviewed.

## Authority boundary

Create one ActionGuard for each match with a nonzero session token and the two transport-authenticated peer identities. A transport adapter must obtain authenticatedSender from the connection, never from the incoming payload.

Call Process with an ActionIntent, a fresh host-owned AuthoritySnapshot and an execution callback. Populate RequestedSelectionAvailable for this specific command from authoritative weapon/item inventory. The guard supports TurnBased and Simultaneous policies. Refresh Epoch on match restart and turn transitions. Both simultaneous players share the current host epoch.

The execution callback routes through GameplayActionController. The callback owns atomic gameplay changes, including consuming ammunition, starting reload, accounting movement and creating projectiles. It returns true only when the action succeeds. The guard serializes callbacks and consumes every dispatched sequence, including callbacks that reject or throw. Unexpected callback exceptions propagate. Retrying a failed action requires a new sequence.

Damage, health, movement distance, projectile trajectory, terrain and rewards must be calculated on the authority. This module accepts intent only. It does not accept client-reported damage, position or clock values. Existing authoritative movement accounting and timeout leases remain necessary to enforce speed and fuel.

A malicious LAN host controls its own simulation. Host-side validation protects the host from client commands. Protecting online matches from a malicious host requires a trusted server running the same gameplay and validation.

## Verification

Run Tests/Run.ps1 with PowerShell. It uses the C# compiler and Mono bundled with Unity 6000.5.10f1. Supply -UnityEditor to select another Editor installation. Compilation treats warnings as errors. Generated test executables remain ignored in TestOutput.

The module is tested independently. It has not been connected to the live Tank gameplay or validated on Android/real network clients. Integration requires a transport adapter, current game snapshots and authoritative gameplay dispatch.

## Git isolation

All module files are under AntiCheat in the linked worktree. Only these paths are staged and committed on this branch. No merge, cherry-pick or push is performed by this setup.

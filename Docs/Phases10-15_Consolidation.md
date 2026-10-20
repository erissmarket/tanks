# Phases 10–15 local history consolidation

Reviewed on 2026-10-20 against the actual repository and the authoritative Master Context in its existing external location. That document was not copied into the repository.

## Scope and history

Starting branch: `dev/backend`, HEAD `e222fe1dd6024595b7eb62597c72faae6e3f5f15`. The index was empty. Local backend was one existing art/gameplay commit ahead of the cached `origin/dev/backend` at `bee9e3b`. No remote references were refreshed for this consolidation.

The final working files contain interleaved changes across several phases. This consolidation preserves their reviewed final versions rather than guessing intermediate historical source states:

1. **Generic LAN foundation (Phase 10):** NGO manifest/lock additions, `IMultiplayerService`, `NgoMultiplayerService`, `LanTestPanel`, `LanTest`, and the default empty NGO prefab list, including asset metadata. The service includes its later generic manager bridge and stopped-only compatibility configuration; it remains independent from Tank gameplay.
2. **Integrated gameplay, controls, menu and aim responsiveness (Phases 11–15):** host-authoritative commands/snapshots/outcome replay, discovery/room browser, movement lease/budget and aim/power, menu/difficulty/navigation, disposable aim/local visual anticipation/remote interpolation, test sources and required metadata. Shared adapter/session/panel/protocol/input files are committed together. Required integration compatibility changes in `MVPIntegrated`, `BackendTest`, `IntegratedProjectile`, terrain reset and bootstrap accompany this dependency-complete group.

The existing published art commits are retained. Source art prefabs, sprites, textures, animations, materials and art generation scripts are not changed by this consolidation. Scene/prefab differences are the previously documented authoritative-component overrides and aiming/terrain/restart wiring, including existing Unity serialization noise. No new gameplay or balancing implementation was added here.

## Runtime contracts retained

- Host authorizes actual sender, player, session, turn, sequence and values; accepted gameplay actions route through `GameplayActionController`. Clients do not simulate damage, projectiles, turns or winners.
- Gameplay and discovery compatibility are v3. Fire reliably carries final direction/power; obsolete disposable aim cannot consume the reliable action sequence.
- Movement lease defaults to 0.35 seconds, actual commanded travel budget to 3 world units per turn.
- Aim command/observation cap 20 Hz; actual rates depend on frame scheduling. Reliable full state remains 10 Hz, NGO tick 30. Remote visual interpolation delay is 35 ms; local anticipation affects SpriteRenderer-only proxies, not authoritative muzzle/physics pivots.
- Generic `LanTest` retains protocol 0 by default and no Tank dependency. LAN gameplay explicitly configures compatibility 3.

## Validation and acceptance

Fresh consolidation checks, without Play Mode:

- Unity 6000.5.10f1: not compiling, `scriptCompilationFailed=false`, Console error query empty, loaded integrated scene missing scripts zero. Serialized GUID checks passed for `MVPIntegrated`, `BackendTest`, `LanTest`, `LanGameplayTest`, and `MainMenu`.
- Phase 15 pure protocol/coalescing/buffer suite: 32 passes.
- Discovery test project built with warnings as errors: zero warnings/errors. All 61 checks passed outside the restricted sandbox. An initial sandboxed attempt passed 58 checks and failed at broadcast-based room selection; that failed attempt is not counted as a successful suite.
- New scripts/assets have their existing Unity-generated metadata. No GUIDs regenerated. Selected content reviewed for secrets, private device identifiers, unintended large files and generated artifacts.
- Raw Git whitespace checks flag existing Unity YAML blank-value trailing spaces and one pre-existing trailing-whitespace line in `Phase14PowerAudit.ps1`. Those original bytes are deliberately preserved; no source whitespace cleanup or scene resave was performed merely for Git hygiene. C# source whitespace checks pass.

Historical Phase 15 evidence was reviewed, not rerun as a whole: 418 distinct checks, independent Host/Client/observer and power/Fire/lease regressions, plus successful Windows and Android builds. The Android artifact is under ignored `Builds/Phase15/Android/`: ARM64/IL2CPP, Development, min SDK 26 / target 36, valid debug v2 signature, MainMenu entry, 76,643,366 bytes. No fresh player build or physical-device validation is claimed by this consolidation.

`Tools/Tests` preserves phase-specific audit sources. `Phase14UnityAudit.cs` contains its historical protocol-v2 assertion; Phase 15 reran a temporary copy with that expectation changed to v3. Do not run it unchanged against v3 and misclassify that assertion as a networking regression. Play Mode harnesses require their documented scene/setup and must not be invoked in an unsaved scene casually. Use `Phase15UnitAudit.cs` for the current v3 pure checks and `Phase15NetworkAudit.ps1` with a matching development LAN-fixture player for current independent checks.

For a MainMenu player build, supply the explicit scene order: `Assets/_Game/Scenes/MainMenu.unity`, `Assets/_Game/Scenes/MVPIntegrated.unity`, `Assets/_Game/Multiplayer/LanGameplayTest.unity`. Existing global scene lists and Build Profiles are preserved and excluded from these commits; they are not automatically promoted to the menu entry flow.

Physical two-phone hotspot/shared-router responsiveness, manual touch feel and full gameplay acceptance remain pending. Earlier user-reported connectivity is not complete physical acceptance. Follow [Phase15_DeviceAcceptance.md](../Tools/Tests/Phase15_DeviceAcceptance.md). Two prior independent Unity broadcast-discovery attempts timed out with Windows Security visible; this consolidation's .NET discovery pass does not retroactively pass those attempts or establish phone behavior.

## Deliberate exclusions

Preserve unstaged/untracked: HubForceResolve script/meta deletions; URP/global/volume settings; ProjectSettings and EditorBuildSettings differences; AI-assistant package preferences; Android Build Profile and its folder metadata; recovery scene/folder metadata; empty/generated Resources and Prefabs folder metadata; root `FETCH_HEAD`; Burst debug output. Ignore existing builds, APKs, caches, logs and temporary audit reports. Preserve all stashes. No push, fetch, pull, merge, rebase, reset or history rewrite is part of this task.

The existing URP manifest request 17.6.0 versus resolved/locked 17.5.0 discrepancy predates these commits and remains a separate Package Manager review. NGO manifest/lock agree on 2.13.3; Unity resolves bundled Transport 6.5.0. No dependency resolution or SDK version change was performed for consolidation.

The open integrated scene's unsaved soil transform edits remain in memory and are not staged. Its saved disk bytes and original unrelated local work are verified against the consolidation preflight. A deliberately dirty working tree after these commits is expected.

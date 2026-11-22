<div align="center">

# TANK ARTILLERY

### Tactical precision. Real-time pressure. A battlefield that fights back.

An Android-first artillery game where every shot can change the terrain, break a defense, or turn a losing duel into a comeback.

![Android](https://img.shields.io/badge/Android-ARM64-3DDC84?logo=android&logoColor=white)
![Unity](https://img.shields.io/badge/Unity-6000.5.10f1-222222?logo=unity&logoColor=white)
![Language](https://img.shields.io/badge/Language-C%23-684D95)
![Multiplayer](https://img.shields.io/badge/Multiplayer-Host_Authoritative_LAN-1579A8)
![Stage](https://img.shields.io/badge/Stage-Playable_Prototype-E2A83B)

</div>

## The battlefield is part of your arsenal

A tank behind a ridge needs a different answer from a shielded opponent in the open. Lob a mortar over cover, skip a shell across a slope, scatter a cluster on descent, or dig away the ground beneath the enemy's tracks.

Tank Artillery combines two approaches to the duel. Tactical battles give players time to plan an angle, manage fuel, and commit to a shot. Live battles let both sides move, aim, and fire together, with independent reloads keeping the pressure on.

The long-term ambition is a competitive artillery platform with a distinctive tank roster, evolving arenas, reliable online matches, and progression worth returning for.

> **Development snapshot**
> This README describes the latest local development direction. The main branch contains the earlier playable game and the standalone anti-cheat module. Later gameplay, including the ten-weapon catalog and live/LAN modes, remains in the backend development work and has not been merged into main. Public servers and the roadmap features are planned. Anti-cheat gameplay integration is pending.

## Choose your kind of battle

| Mode | Experience | Development status |
| --- | --- | --- |
| Tactical Duel | Turn-based combat against the computer | Implemented in backend development |
| Live Duel | Simultaneous combat against the computer | Implemented in backend development |
| Local Turns | Pass-and-play on one device | Implemented in backend development |
| Live Local | Two players, independent controls, one screen | Implemented in backend development |
| LAN Turns | Host or join a turn-based local-network match | Implemented in backend development |
| Live LAN | Simultaneous local-network combat | Implemented in backend development |
| Online Arena | Internet matchmaking and persistent competition | Planned |

Android is the primary platform, with landscape controls, safe-area layouts, ARM64 builds, and a minimum Android version of 8.0 / API 26. Windows builds support development and independent multiplayer validation.

## Ten weapons. Ten reasons to change your plan.

The weapon catalog separates launch physics, tank damage, blast radius, terrain destruction, shield penetration, ammunition, and reload time. Balancing lives in editable weapon assets.

| Weapon | Role | Opportunity |
| --- | --- | --- |
| Basic Shell | Dependable general-purpose artillery | Save specialist ammunition while keeping pressure on an exposed target |
| Rocket Missile | Powered flight | Reach distant targets through a clear approach |
| Heavy Mortar | Heavy arc and broad impact | Attack over a rise and reshape the landing area |
| Railgun | Fast precision and shield penetration | Punish an exposed tank with a narrow, accurate shot |
| Floating Bomb | Gentle flight and wide splash | Cover a wider landing area beyond a ridge |
| Bouncy Shell | Controlled ricochets | Use the ground to reach awkward angles |
| Cluster Bomb | Descending fragmentation | Spread bomblets close to the surface |
| Digger Shell | Excavation | Cut cover and remove support beneath a tank |
| Tremor Bomb | Deep crater specialist | Turn a defensive ridge into unstable ground |
| Skimmer | Low-gravity, multi-skip flight | Chase an opening across shallow slopes |

Projectiles include explicit ownership, bounded lifetimes, impact guards, and shared shot tracking for fragments. Explosion damage accounts for proximity, cover, direct contact, shields, and armor. Terrain damage has its own multiplier, giving excavation weapons a distinct role.

## Built for the player's thumbs

- Larger aim controls with captured touch ownership and separate movement and firing regions.
- Live angle and power feedback, plus a short dotted guide that follows the selected weapon's launch conditions.
- Independent control sets in same-device live battles.
- Fuel-limited movement, with recovery while stationary in live combat.
- Visible projectiles, impact animation, smoke, sparks, debris, and terrain craters.
- Pause, surrender, match results, restart, and return-to-menu flows.

Mobile feel remains an active development focus, including physical-device touch acceptance, readability, performance, and balance across screen sizes.

## An opponent that understands the shot

The artillery AI evaluates candidate weapons, firing angles, and launch powers using the selected projectile's physics. Planning considers damage, shields, self-risk, ammunition scarcity, and terrain support.

Difficulty changes aiming error, power variation, and live firing cadence. Easy has a longer thinking delay and wider error ranges, while Hard commits more quickly and aims more tightly. Candidate evaluation runs incrementally so planning can share frame time with gameplay.

Future AI work includes terrain-aware repositioning, richer tactical personalities, and difficulty tuning from measured match outcomes.

## Hangar, operations, and progression

The current prototype includes a hangar with a tank preview, a garage with three tank definitions, shield selection, tank upgrades, an arsenal, battle preparation, environment selection, settings, and a field guide.

Operations connect career wins to new fronts and one-time milestone rewards. Local progression records equipment choices, coins, results, and upgrades through a checksum-validated save envelope with backup recovery.

The design direction expands this foundation into recognizable tank identities, encounter-based campaigns, meaningful loadout decisions, and cosmetic expression.

## Gameplay and backend architecture

Gameplay actions enter through <code>GameplayActionController</code>. Input, AI, and network adapters submit intent through that shared boundary. Match flow, tank behavior, weapons, projectiles, damage, terrain, and presentation have separate responsibilities.

~~~mermaid
flowchart LR
    Input[Touch and local controls] --> Actions[GameplayActionController]
    AI[Artillery AI] --> Actions
    LAN[LAN command adapter] --> Actions
    Actions --> Match[Match flow and permissions]
    Actions --> Tank[Tank movement and aiming]
    Actions --> Weapons[Weapons and projectiles]
    Weapons --> Damage[Health, shields and armor]
    Weapons --> Terrain[Destructible terrain]
    Match --> View[HUD and match presentation]
    Damage --> View
    Terrain --> View
~~~

The local-network implementation uses Netcode for GameObjects and Unity Transport. The host applies gameplay commands and publishes state and presentation observations. Ownership, session tokens, command sequencing, rematch consent, and disconnect handling provide the foundation for reliable sessions.

Backend and gameplay engineering cover simulation, AI, data, saves, action validation, and multiplayer. Art and design development cover tanks, environments, UI visuals, animation, lighting, and combat feedback.

## Independent anti-cheat development

The standalone anti-cheat module is published under [AntiCheat/](AntiCheat/). Its development originated on <code>codex/anticheat-foundation</code>. It remains outside Unity Assets, allowing separate review and testing before gameplay integration.

| Milestone | Scope | Commit |
| --- | --- | --- |
| Foundation | Ownership, session and epoch checks, replay rejection, turn/live authorization, input and resource validation | e81b2e9 |
| Hardening | Separate aim/reliable sequence lanes, host-clock rate limits, concurrent dispatch protection, exception and restart checks | dd9d956 |

The standalone C# module has **54 passing checks** and compiles with warnings treated as errors. Its execution boundary is designed to dispatch accepted intent through authoritative gameplay.

Integration with the active Unity gameplay is a separate milestone. Server-owned damage, resources, movement accounting, and reward settlement remain central to the security model. A trusted dedicated server is part of the online roadmap because a player-hosted session grants authority to that host.

Read the [anti-cheat integration guide](AntiCheat/README.md), or run its standalone checks from the repository root:

~~~powershell
./AntiCheat/Tests/Run.ps1
~~~

## From LAN battles to an online arena

The planned online stack extends the existing authoritative model with services for identity, matchmaking, match allocation, progression, and operations.

~~~mermaid
flowchart LR
    Android[Android clients] --> Gateway[Identity and session gateway]
    Gateway --> Queue[Matchmaking and room allocation]
    Queue --> Server[Authoritative match server]
    Server --> Guard[Command validation and anti-cheat]
    Guard --> Simulation[Gameplay simulation]
    Server --> Results[Validated match results]
    Results --> Data[Progression and inventory]
    Server --> Ops[Metrics and operational diagnostics]
~~~

The online roadmap targets private rooms, reconnect support, match recovery, server-owned rewards, regional matchmaking, and ranked play. Replayable match evidence and abuse review can build on validated action streams.

Server capacity, hosting choices, latency targets, and operating costs will be established through profiling and load tests as the online implementation takes shape.

## The next horizon

| Direction | Planned development |
| --- | --- |
| Competitive combat | Deeper weapon interactions, clearer counters, refined movement, and measured balance |
| Tanks and equipment | More hull identities, specialized loadouts, and expressive cosmetic options |
| Campaign | Authored encounters, boss-style challenges, and clearer operation objectives |
| Multiplayer | Trusted server matches, reconnect flows, private rooms, and ranked progression |
| Fair play | Anti-cheat integration, authoritative reward checks, match evidence, and reviewed enforcement |
| Presentation | Stronger impact animation, richer environments, audio feedback, and mobile interface polish |
| Live operations | Seasonal objectives, events, achievement tracks, and operational visibility |
| Android delivery | Device coverage, frame-time and memory profiling, thermal testing, and release optimization |

## Explore the project

| Area | Location |
| --- | --- |
| Match and action services | Assets/_Game/Core/ |
| Player abstraction | Assets/_Game/Player/ |
| Tank systems | Assets/_Game/Tank/ |
| Weapon configuration and behavior | Assets/_Game/Data/ and Assets/_Game/Weapons/ |
| Projectile physics and damage | Assets/_Game/Projectiles/ |
| AI planning | Assets/_Game/AI/ |
| Multiplayer and transport integration | Assets/_Game/Multiplayer/ |
| UI, input, and visual feedback | Assets/_Game/Presentation/ |
| Terrain and environment behavior | Assets/_Game/Terrain/ |
| Artwork and runtime resources | Assets/_Game/Art/ and Assets/_Game/Resources/ |
| Verification tools and development notes | Tools/Tests/ and Docs/ |

### Development setup

1. Open the project with **Unity 6000.5.10f1**.
2. Install the matching Android Build Support, SDK, NDK, and OpenJDK modules through Unity Hub.
3. Let Unity resolve the project's package manifest.
4. On main, open <code>Assets/_Game/Scenes/MVPIntegrated.unity</code>. The later backend development checkout uses <code>Assets/_Game/Scenes/MainMenu.unity</code>.
5. For the backend development build, include MainMenu, MVPIntegrated, and LanGameplayTest in the Android build scene list.
6. The backend Android build configuration uses ARM64, IL2CPP, and target SDK API 36.

Run <code>git lfs pull</code> after cloning to retrieve the artwork managed through Git LFS.

### Project handoff and design notes

- [Project handoff in Persian](PROJECT_HANDOFF.fa.md)
- [Latest teammate art and design changes](LATEST_CHANGES.fa.md)
- [Design notes](Docs/Design/)
- [Verification reports](Docs/Verification/)

Core packages include Universal Render Pipeline, the Input System, Netcode for GameObjects, and Unity Transport.

### Validation status

Previous development runs include Android APK installation and play checks in MuMu, same-device live combat, and independent Windows host/client checks for simultaneous LAN commands and projectile observations. Anti-cheat verification is isolated from those gameplay runs.

Physical Android device acceptance, current-build two-phone LAN testing, public-server validation, and broader performance testing remain delivery milestones.

---

<div align="center">

**A precise shot can win a duel. A clever shot can change the whole battlefield.**

</div>

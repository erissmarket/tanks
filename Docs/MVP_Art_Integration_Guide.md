# MVP Art Integration Guide

Project: `D:/tank/tanks` · Unity: `6000.5.10f1` · Date: 2026-10-09

Documentation only: no scene, prefab, code, settings or runtime behavior changed. Inventory and component references below were inspected in saved project files. Paths beginning `Assets/` are relative to the project root above.

## 1. Existing asset inventory

| Asset | Exact project path |
|---|---|
| Scene | `Assets/Scenes/SampleScene.unity` |
| Tank prefab | `Assets/_Game/Art/Tanks/TankVisual.prefab` |
| Tank sprites | `Assets/_Game/Art/Tanks/TankBody.png`, `Assets/_Game/Art/Tanks/TankTracks.png`, `Assets/_Game/Art/Tanks/TankTurret.png`, `Assets/_Game/Art/Tanks/TankBarrel.png` |
| Terrain | `Assets/_Game/Art/Terrain/RollingSoil.png`, `Assets/_Game/Art/Terrain/RollingGrass.png` |
| Background | `Assets/_Game/Art/Background/SkySquare.png`, `Assets/_Game/Art/Background/MountainsFar.png`, `Assets/_Game/Art/Background/MountainsNear.png`, `Assets/_Game/Art/Background/Clouds.png` |
| Weapon UI icon | `Assets/_Game/Art/Weapons/BasicShell_Icon.png` |
| Projectile sprite/prefab | `Assets/_Game/Art/Projectiles/BasicShell_Projectile.png`, `Assets/_Game/Art/Projectiles/BasicShell_Visual.prefab` |
| Explosion prefab | `Assets/_Game/Art/FX/BasicExplosion/BasicExplosion_Visual.prefab` |
| Explosion animation/controller | `Assets/_Game/Art/FX/BasicExplosion/BasicExplosion.anim`, `Assets/_Game/Art/FX/BasicExplosion/BasicExplosion.controller` |
| Explosion frames | `Assets/_Game/Art/FX/BasicExplosion/Explosion_00.png`, `Explosion_01.png`, `Explosion_02.png`, `Explosion_03.png`, `Explosion_04.png`, `Explosion_05.png` in the same folder |
| HUD circle | `Assets/_Game/Art/HUD/HUD_Circle.png` |
| HUD objects | Stored directly in SampleScene; no separate HUD prefab |

Preserve `.meta` files and GUIDs. Assign asset/component references through the developer's existing serialized fields or initialization interface. Object paths below identify setup targets; runtime name searches are not required. Art generation scripts inside Editor folders are setup tools; do not rerun them for integration.

## 2. Prefab hierarchy and verified component references

`Player Tank` and `Enemy Tank` are instances of TankVisual (GUID `a71f46f1bd7f01b4a9e32827ffefa6c5`):

```text
TankVisual / Player Tank / Enemy Tank
  Body                         SpriteRenderer
  Tracks                       SpriteRenderer
  TurretPivot                  Transform
    Turret                     SpriteRenderer
    BarrelPivot                Transform
      Barrel                   SpriteRenderer

BasicShell_Visual              Transform + SpriteRenderer
BasicExplosion_Visual          Transform + SpriteRenderer + Animator

MVP HUD                        Canvas + CanvasScaler
  Player Health Bar            Image (dark background)
    Fill                       Image (Filled/Horizontal)
    Label                      UnityEngine.UI.Text
  Enemy Health Bar
    Fill
    Label
  Turn Indicator               Image
    Label                      UnityEngine.UI.Text
  Fire Button                  Image + Button
    Disc                       Image
    Label                      UnityEngine.UI.Text
```

| Exact scene path/member | Reference / current configuration |
|---|---|
| `Player Tank` | Transform: position (-5,-1.95,0), scale (1,1,1) |
| `Enemy Tank` | Transform: position (5,-1.95,0), scale (-1,1,1) |
| `<tank>/Body` | SpriteRenderer; local position (0,0.48,0) |
| `<tank>/TurretPivot` | Transform; local position (0,0.79,0) |
| `<tank>/TurretPivot/Turret` | SpriteRenderer |
| `<tank>/TurretPivot/BarrelPivot` | Transform; local position (0.2,0.03,0); initial Z rotation 12 degrees |
| `<tank>/TurretPivot/BarrelPivot/Barrel` | SpriteRenderer; local position (0.45,0,0); sprite size 0.9 × 0.13 units |
| `MVP Battlefield/Soil Ground` | SpriteRenderer + enabled PolygonCollider2D, one 75-point path, offset zero, IsTrigger false |
| `MVP Battlefield/Grass Surface` | SpriteRenderer; soil and grass position (0,-2.75,0), unit scale |
| `MVP HUD/Player Health Bar/Fill` | Image.fillAmount = 0.85 |
| `MVP HUD/Enemy Health Bar/Fill` | Image.fillAmount = 0.70 |
| `MVP HUD/Player Health Bar/Label` | Text: `PLAYER 85%` |
| `MVP HUD/Enemy Health Bar/Label` | Text: `ENEMY 70%` |
| `MVP HUD/Fire Button` | Button.interactable false, Image.raycastTarget false, targetGraphic assigned, onClick empty |
| `MVP HUD/Turn Indicator/Label` | UnityEngine.UI.Text: `PLAYER TURN`; not TextMeshPro |

All three visual prefabs have no gameplay scripts or physics components. No runtime gameplay C# controllers/interfaces were found under Assets; the six C# files present are Editor-only tools. Compatibility with the other developer's unseen code remains unverified.

## 3. Integration contracts

These are behavioral requirements for the gameplay developer, **not implemented custom public methods**. Agree on the actual existing shooting/tank/health/turn interfaces before binding; no new manager architecture is requested.

### Position and move tanks

- Assign the visual root Transform to the existing tank controller, or attach the visual as its child. The art root is at track bottom/ground contact; define any controller-origin/contact offset explicitly.
- Preserve final world scale and enemy mirroring, with one movement/physics authority. Keep green/red SpriteRenderer color overrides.
- Current flat tank pads: x=-6..-4 and x=4..6 at y=-1.95. On slopes, use the gameplay grounding/slope policy; a center-height sample does not guarantee both track ends contact the surface.

### Aim and determine muzzle

- Assign `<tank>/TurretPivot/BarrelPivot` for elevation rotation about local Z at the barrel base. Rotate TurretPivot only if the existing controller intends to rotate the complete turret/barrel assembly.
- Barrel artwork faces local +X. There is no dedicated muzzle Transform. With current geometry, the tip is `Barrel.TransformPoint(new Vector3(0.45f,0,0))`; the base is the corresponding point at x=-0.45. These expressions specify geometry, not added runtime code.
- Derive world firing direction from transformed tip minus base. Enemy negative-X scale mirrors local aim; do not assume world +X or Transform.right automatically matches the visible barrel. Agree on world/local angle conversion for both teams.

### Spawn and orient projectile visuals

- Reference BasicShell_Visual as a visual child of the existing gameplay projectile, or assign its sprite to that projectile's SpriteRenderer. Preserve existing physics/controllers.
- At zero rotation the shell faces +X; orient that axis to the firing/velocity direction in the XY plane. Use the barrel tip reference and controller-defined clearance so the gameplay collider does not start inside the firing tank or terrain.
- PNG: 128 × 64, 100 PPU, centered pivot. Prefab uniform scale 0.25 yields a 0.32 × 0.16 world-unit canvas; visible art is approximately 0.30 × 0.085, fitting the 0.13-unit barrel thickness.
- Preserve this final world size when parenting; avoid double scaling. Align the gameplay collider with visible art, not transparent canvas padding. This visual prefab supplies no collider, movement or damage.

### Trigger and retire explosions

- Reference BasicExplosion_Visual's root Animator. Instantiation alone intentionally shows nothing: default `Base Layer.Idle` has null motion and SpriteRenderer starts null.
- Explicitly restart `Base Layer.Explode` on layer 0 at normalized time 0 for each activation. Six 256 × 256 transparent frames play at 12 fps, non-looping; the sprite clears at 0.5 seconds.
- Serialized clip stop time is **0.5833333 seconds**, not 0.5. Own destruction or pool return after playback completion, accounting for Animator speed and pause/time-scale policy. No lifecycle callbacks, damage or cleanup are supplied.
- If pooling, reset transform/playback on checkout, keep inactive instances hidden, and prevent stale cleanup from returning a newly reused instance. Pooling is optional; destruction after completion is sufficient for MVP.

### Update health and turn

- Assign the two Fill Image references. Set Image.fillAmount to clamped health/maxHealth in [0,1]; handle maxHealth <= 0 according to the gameplay model. Do not update the background Image instead.
- Update sibling Text labels separately; their placeholder percentages do not follow fillAmount automatically.
- Assign `MVP HUD/Turn Indicator/Label` and update Text.text from existing turn state. Gameplay owns all values; HUD provides presentation only.

### Connect FIRE

- Assign the existing Button to the developer's actual shooting/command interface. No existing method named Shoot or Fire has been verified.
- Enable interaction and hit testing, provide compatible input infrastructure, and register the listener once. Remove owned listeners when appropriate; prevent duplicate subscriptions on re-enable/scene reload.
- Existing gameplay must validate turn/ammo/state independently; Button.interactable is presentation feedback, not the authority to permit shooting.

## 4. Minimum gameplay developer and mobile UI requirements

### Essential MVP work

1. **Input infrastructure:** reuse one active EventSystem with a compatible UI input module. Input System 1.20.0 is installed and activeInputHandler is 1. Use InputSystemUIInputModule/actions compatible with the target gameplay scene; do not duplicate an existing EventSystem.
2. **Hit testing:** current MVP HUD Canvas has no GraphicRaycaster. Supply one when enabling interaction; enable Fire Button's target Image raycastTarget and Button.interactable as appropriate. Other decorative graphics can remain non-raycast targets.
3. **Bindings:** connect the existing shooting, tank, projectile, health and turn systems and explosion lifecycle. None currently exists as runtime bindings in this art project.
4. **Safe Area:** no safe-area container/adaptation exists. Use the developer's existing solution or fit a HUD container to Screen.safeArea in the overlay canvas, updating on resolution/orientation/inset changes. Fixed corner offsets are not notch protection; keep terrain/camera independent.
5. **Landscape acceptance:** verify target-build orientation policy, both landscape ratios and notch/navigation-bar insets. Check readable text and physical touch size on a target phone, not just pixel dimensions.

Canvas: Screen Space Overlay, order 100; Scale With Screen Size, reference 1920 × 1080, width/height match 0.5.

| Element | Anchor/pivot | Anchored position | Reference size |
|---|---|---|---|
| Player health | top-left (0,1) | (60,-55) | 340 × 56 |
| Enemy health | top-right (1,1) | (-60,-55) | 340 × 56 |
| Turn | top-center (0.5,1) | (0,-55) | 300 × 56 |
| FIRE | bottom-right (1,0) | (-60,60) | 156 × 156 |

**Earlier visually verified:** 16:9 displays the HUD and both tanks without overlap. **Calculated only:** at 2340 × 1080 (19.5:9), scale is ~1.104, FIRE diameter ~172 px and right/bottom inset ~66 px; no HUD/tank overlap predicted. Direct 19.5:9 rendering and safe-area/device tests remain outstanding.

Camera orthographic size 5 shows 17.778 world units wide at 16:9, slightly cropping the 18-unit terrain ends; 19.5:9 would show 21.667 units, exposing ~1.833 units beyond each side. Background width 36 covers this view. Review composition with the art owner before changing camera or geometry.

### Optional improvements

Dedicated muzzle Transform, HUD prefab, animated health feedback, pooling instead of destruction, texture/atlas optimization and broader aspect-ratio coverage can wait until a concrete need is established.

## 5. Known issues and integration risks

| Severity | Issue | Required response |
|---|---|---|
| **Critical** | FIRE cannot receive input in its current configuration | Enable interaction/raycast, supply GraphicRaycaster and one compatible EventSystem/input module, then bind shooting. |
| **Critical** | Runtime gameplay controllers/bindings absent from the inspected art project | Obtain developer interfaces and connect them; visual assets alone are not playable. |
| **Important** | Enemy mirroring and missing muzzle marker can reverse aim/spawn direction | Use transformed endpoints and verify both teams. |
| **Important** | Projectile parent scale, collider size or spawn overlap can disagree with art | Preserve final size, align collider and test clearance. |
| **Important** | Tank physics origin/grounding may differ from visual root | Define offset/slope policy and a single movement owner. |
| **Important** | Explosion stays invisible without trigger and has no cleanup | Explicitly play state and own completion/reset/destruction; use actual duration. |
| **Important** | Safe Area absent; 19.5:9 not directly rendered | Apply approved safe-area handling and validate devices before mobile acceptance. |
| **Important** | Sorting/material/light changes may hide art | Preserve ordering and Global Light 2D for Sprite-Lit materials; verify after integration. |
| **Important** | Lost metadata or stale/duplicate references | Preserve GUIDs and assign current prefab/components in the target scene. |
| **Optional** | Wider screens expose rectangular terrain side boundaries | Review composition with art owner; no terrain change authorized here. |
| **Optional** | No HUD prefab, muzzle marker or atlas | Add only when integration demonstrates a need. |

Sorting convention: Default layer; background -40/-30/-20/-10, soil 0, grass 1, tracks 11, body 12, barrel 13, turret 14, projectile 20, explosion 30; overlay HUD 100.

Task 08 found no broken asset references. Terrain source comparison measured maximum collider/sprite surface deviation ~0.008594 units (<1 pixel at 100 PPU), and grass/soil top edges matched all columns. These are static/art checks; runtime collision and compatibility with unseen gameplay code are untested.

## 6. Recommended integration order

1. Agree on existing controller interfaces; assign component/prefab references; confirm origins, mirroring and scales.
2. Connect tank grounding/aim; validate muzzle position and direction for both teams.
3. Integrate projectile visuals with existing movement/collision, then explosion trigger/lifecycle with existing impact handling.
4. Enable FIRE input and bind shooting once; connect health and turn presentation.
5. Apply approved Safe Area handling and validate actual 16:9/19.5:9 views and target devices.

## 7. Acceptance checklist for a later authorized implementation pass

- [ ] Existing gameplay controllers/physics remain the sole authorities; no duplicate systems or tanks.
- [ ] Tanks rest on terrain and follow agreed grounding/slope conventions.
- [ ] Both teams aim correctly; muzzle direction accounts for enemy mirroring.
- [ ] Projectile faces travel direction, has correct world size/collider alignment, and spawns clear of tank/terrain.
- [ ] Explosion plays only on explicit trigger, clears, and is destroyed or safely returned after playback; repeated reuse works.
- [ ] Health fill and labels update together; turn text reflects gameplay state.
- [ ] FIRE receives mouse/touch via one compatible EventSystem and sends one permitted shooting request per activation.
- [ ] HUD is readable, touchable and unobscured in actual 16:9 and 19.5:9 renders with target safe areas.
- [ ] Sorting, lighting, terrain collision and existing gameplay interfaces remain correct.
- [ ] No missing references/new compilation errors; device/runtime validation recorded by the gameplay developer.

Unchecked items are future acceptance requirements, not completed behaviors. This documentation task did not enter Play Mode or implement integration.

# Gameplay art changes

The updated art/demo scene is `Assets/Scenes/SampleScene.unity`.

- TankMotor follows ground slopes, aligns the body smoothly, accelerates/brakes, and preserves airborne gravity. TankVisual now has a dynamic Rigidbody2D, a track-shaped CapsuleCollider2D and a traction material.
- TankAim accepts local forward/up angles from 0 to 90 degrees. A mirrored tank uses the same local angle range. GameplayActionController constrains world firing requests through TankAim.
- DestructibleTerrain creates runtime crater bowls and scorch marks, removes grass and updates PolygonCollider2D. ExplosionDamage invokes it for terrain inside the blast radius, including zero-damage blasts. Terrain resets when the scene reloads.
- The basic shell visual prefab now has projectile physics and the Projectile component.

## Integration with backend scenes

The independently developed MVPIntegrated and BackendTest scenes were fetched from main; their scene wiring was not modified by this change. To use craters there, attach DestructibleTerrain to the soil SpriteRenderer/PolygonCollider2D object and assign the matching Grass Surface renderer. Set tank aim limits to 0/90 and check any AI that supplies world-space angles against the new local-angle contract. Mirror the enemy visual as in SampleScene. Projectile impact uses the existing ExplosionDamage path.

## Verified in Unity 6000.5.10f1

- Traversal/braking on flat and +/-20 degree slopes, including mirrored tank; airborne gravity preserved.
- Both tank aim directions clamp to 0..90; backward firing requests constrained.
- 0.9-unit crater depth, collider/surface agreement, dark exposed soil, overlapping impacts.
- A real projectile collision in SampleScene created a crater and completed normally; Console had no errors at the end of the test.

Editor-only preview shortcuts while playing: F6/F7 move, F9 stop, F10 fire. F8 verifies slope physics; F11 verifies craters and aim. F5 applies crater/aim setup to SampleScene in Edit Mode.

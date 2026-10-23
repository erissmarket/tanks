# Terrain themes

Open `Assets/_Game/Scenes/MVPIntegrated.unity` and enter Play Mode. The TERRAIN buttons above RESTART select GRASS, SNOW or ROCK. They support the existing EventSystem and touch/mouse input. Selecting a theme only changes terrain art and background colors; it preserves the ongoing match, tank health, crater geometry, collision and scorch marks. The original grass theme remains the scene default. Terrain physics and weapon parameters are unchanged.

## Assets and components

- `Assets/_Game/Art/Terrain/Themes/`: Grassland, Snowfield and Rocklands theme definitions; separate soil and surface PNGs for snow/rock. Sprites match original dimensions, pivots, pixels per unit and alpha silhouettes.
- `TerrainThemeDefinition`: reusable ScriptableObject palette/sprite references.
- `TerrainThemeController`: references on MVP Battlefield, runtime selection and UI listeners.
- `DestructibleTerrain.ApplyAppearance`: reapplies source colors while preserving the current occupancy mask and accumulated scorch values. No crater or collider rebuild is needed when switching themes.
- The scene's Soil Ground now has DestructibleTerrain wired to its matching Grass Surface renderer, so the existing impact handlers operate on all three themes.

## Editor tooling

Tools > Tank Game > Terrain Themes > Build and Install (F3) regenerates theme art from the original rolling terrain and installs/reconnects the selector in MVPIntegrated. Run in Edit Mode. Theme changes made while playing do not rewrite original assets.

Verify All Themes (F4) runs Play Mode checks of alpha masks, crater depth, collider stability, scorch preservation and health invariance. Preview Rocket Impact (F12) drops the existing rocket prefab onto the selected terrain with zero damage to test its actual collision/explosion path.

Verified in Unity 6000.5.10f1: all three alpha silhouettes match; holes, scorch and exact collider points survive switching through all themes; health is unchanged; real rocket impacts on SNOW and ROCK create craters. Existing weapon code, data and projectile prefabs were not edited.

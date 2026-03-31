# Block and Dagger UI Demo

**Genre**: Hybrid of simulation, puzzle and action

**Subgenres**: Building, Relaxing, Farming, Survival, Tower Defense, Action

## Demo focus
- UI flow 		  - Casual user friendly uGUI (Main menu, level creator and ingame UI)
- Game Design 	  - Interesting mid-core gameplay for masses
- Android support - Soon, but few UIs need refactoring

## Design
- Casual style hybrid of "Base Builder" and "Adventure"
- Game includes two phases 1)[Relaxing and building] 2)[Action and puzzle]
- Target platforms: Mobile and desktop
- uGUI based UI
- Not an art demo

## Relevant Packages and Plugins
- Unity version 6.3
- Addressables
- 2D Tilemap Extras
- Newtonsoft.JSON
- VContainer plugin
- AI Navigation - "Dynamic NavMesh baking"
- Prototype tile assets from:https://kenney.nl/assets/platformer-kit
- Icons: https://www.flaticon.com/free-icon

## Notes:
- Audio works but clips not included
- The current tilemap implementation needs attention if going full 3D with Unity's Grid system.

## Known Bugs
- Texture compression artifacts, fixing at somepoint
- Batched rendering currently not working as expected. Fixes coming once there is enough meat
- Block rotation rotates underlying block (same data reference)
- Available block options(icons) syncing has not been finished
- so on...

Coming soon:
- Blueprint menu
- Save level feature (outdated)
- Generate level example feature (outdated)
- New set of maps
- Android support - Coming soon
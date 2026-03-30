# Block and Dagger

**Genre**: Hybrid of simulation, puzzle and action

**Subgenres**: Building, Relaxing, Farming, Survival, Tower Defense, Action

## Demo focus
- UI flow 		  - Casual user friendly uGUI (Main menu, level creator and ingame UI)
- Game Design 	  - Interesting mid-core gameplay for masses
- Android support - Coming soonish

## Design
- Casual style hybrid gameplay with two phases:
"Relaxing build puzzle" and "Hack and slash action"
- Target platforms: Mobile and desktop
- uGUI based UI

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
- The current tilemap implementation(if going full 3D with Unity's Grid system) needs still a bit work
https://docs.unity3d.com/Manual/Tilemap-ScriptableTiles.html

## Known Bugs
- Batched rendering currently not working as expected. Fixes coming once there is enough meat
- Block rotation rotates underlying block
- Available block options(icons) syncing has not been finished
- so on...

Coming soon:
- Loader bar
- Revive save blueprint
- Blueprint menu
- Revive saving
- Revive generate example
- New set of maps
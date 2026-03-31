
# AGENTS: Quick guide for AI coding agents

## Quick architecture summary
- Multi-assembly, Core and Main. More granular in the future if needed
- Dependency injection is implemented with VContainer. Scene-scoped registration lives in `Assets/Scripts/Setup/GameLifetimeScope.cs` and `Assets/Scripts/Setup/LifeTimeConfigureSettings.cs` (use `Construct` methods for injection targets).
- Addressables are used for scenes, prefabs and grouped content. The Addressables configuration lives under `Assets/AddressableAssetsData/` and has groups such as `MainMenu_Scene`, `LevelSelection_Scene`, `Game_Scene`, `Ingame` and project-specific content groups. Code uses a thin wrapper `Assets/Scripts/Core/Utils/AddressablesManager.cs`.

## Important runtime flows and conventions
- Preloading and prefab instantiation: code calls `AddressablesManager.StartPreloadGroupAssets("blocks_folder")` and later `AddressablesManager.FindFromCacheAndInstantiatePrefab(prefabName, pos, parent, rot)`. The manager keeps a cached list via `GetPreloadedGroup()` and `ReleasePreloadedGroup()`.
- Scene <-> manager conventions: GameManager expects a GameObject named `Managers` in loaded scenes and uses `GameObject.Find("Managers")` to locate scene managers. Persistent level object name: `ActiveLevel`. Canvas HUD path: `Canvas/InGameHUD`.
- DI conventions: register scene components with `builder.RegisterComponentInHierarchy<...>()` inside `LifeTimeConfigureSettings`. Use `Construct`-named methods to receive injected dependencies (VContainer pattern).

## Project-specific patterns and gotchas
- Addressable keys are used directly as string constants across code—modify both the Addressables groups and code references together.
- Level file naming conventions: `JsonFilePersistence` and `BuildUtils` show naming suffixes like `_edited_` and `_predefined_...` and a `level_count.txt` is generated under `Assets/Resources/StoredLevels/`.
- `GameManager.DebugSettings`(DebugSettingsScriptableObject) helps to set features on/off like `noAudio`
- Audio binding: `LifeTimeConfigureSettings.SetAudioManager()` registers either a real `MobileAudioManager` or a `MockAudioManager` depending on `GameManager.DebugSettings.noAudio`. Tests or editor runs may prefer the mock.
- Avoid changing `GameManager` expectations without updating scenes: it relies on specific GameObject names and component locations after Addressables-loaded scenes.

## Code guidelines & styling
- Do not add comments.
- No fallback code.
- There should not be any reason to use try/catch if not mentioned separately.
- Do not add debug logging if not specified separately.
- Private variables use underscore prefix. `[SerializeField] private` variables have have unique naming convention `m_` prefix
- Always use curly brackets in if-clauses.
- Do not use pointless if clause null checks that rely provided or injected dependencies:
### Example: call OpenCustomizeMenu using injected `IMenuFacade` provided by DI configuration
Incorrect way:
```csharp

  private void OnCustomizeClicked()
  {
    if (_menuFacade != null)
    {
        _menuFacade.OpenCustomizeMenu();
    }
  }
```
Correct way:
```csharp

  private void OnCustomizeClicked()
  { 
      _menuFacade.OpenCustomizeMenu();
  }
```

## Testing instructions
- Testing will be added at some point when the codebase has less noise. For now testing is done manually
- DebugFastLoadToGame and DebugSettingsScriptableObject utilities will help to speed up manual testing
# SMAPI API Checklist

Use this file to map common Stardew Valley modding tasks to the safest SMAPI API.

## Core Objects

- `Mod`: base class for C# SMAPI mods.
- `Entry(IModHelper helper)`: called early when the mod is loaded.
- `this.Helper`: helper API aggregate.
- `this.Monitor`: logging API.
- `this.ModManifest`: current mod manifest.

Avoid using game state directly in `Entry` unless the API guarantees it is ready. For other mod APIs, usually wait until `GameLoop.GameLaunched`.

## Task to API Map

- Startup integration with other mods: `Helper.Events.GameLoop.GameLaunched` and `Helper.ModRegistry.GetApi<T>()`.
- Save-specific initialization: `SaveLoaded`, `DayStarted`, or `Context.IsWorldReady` checks.
- Daily logic: `DayStarted`, `DayEnding`, `TimeChanged`.
- Save hooks: `Saving`, `Saved`, `SaveLoaded`, `ReturnedToTitle`.
- Input hotkeys: `Helper.Events.Input.ButtonsChanged` and `KeybindList`.
- UI drawing: `Display.RenderedHud`, `RenderedActiveMenu`, or related display events.
- Game asset edits: `Content.AssetRequested` with `e.LoadFrom`, `e.LoadFromModFile<T>`, or `e.Edit`.
- Load mod files: `Helper.ModContent.Load<T>(...)`.
- Load game assets: `Helper.GameContent.Load<T>(...)`.
- Store config: `Helper.ReadConfig<T>()` and `Helper.WriteConfig<T>()`.
- Store arbitrary JSON files: `Helper.Data.ReadJsonFile<T>()` and `WriteJsonFile<T>()`.
- Store save data: `Helper.Data.ReadSaveData<T>()` and `WriteSaveData<T>()` after a save is loaded.
- Store global data: `Helper.Data.ReadGlobalData<T>()` and `WriteGlobalData<T>()`.
- Translate text: `Helper.Translation.Get(...)` and `i18n/default.json`.
- Check other mods: `Helper.ModRegistry.IsLoaded(...)` and `Get(...)`.
- Integrate with another mod API: `Helper.ModRegistry.GetApi<T>(...)` in `GameLaunched`.
- Multiplayer messages: `Helper.Multiplayer.SendMessage(...)` and `Multiplayer.ModMessageReceived`.
- Split-screen state: `PerScreen<T>`.
- Private members: `Helper.Reflection`, only when public APIs are insufficient.
- Console commands: `Helper.ConsoleCommands.Add(...)`.
- Last-resort code patches: Harmony, only after checking safer options.

## Event Selection

- Prefer semantic events over polling in `UpdateTicked`.
- Avoid expensive work in `UpdateTicked`, `Rendered`, and other high-frequency events.
- Use `ButtonsChanged` for keybind combinations, not `ButtonPressed` alone.
- Use `Content.AssetRequested` for edits/replacements instead of direct file replacement.
- Use `AssetsInvalidated` or `AssetReady` only when you need to react to asset cache changes.
- Avoid `Specialized.UnvalidatedUpdateTicked` unless you have a very specific reason.

## Content API Notes

- Game asset names are not filesystem paths and do not include `.xnb`.
- Compare asset names with `IAssetName.IsEquivalentTo(...)`.
- For string asset names, normalize with `PathUtilities.NormalizeAssetName(...)` when appropriate.
- Avoid `AssetLoadPriority.Exclusive` unless you intentionally prevent other mods from editing that asset.
- Do not repeatedly load assets or invalidate cache every tick.
- Use `IRawTextureData` for bulk image edits when possible.

## Config and Data Notes

- Config classes should be simple public-property models with defaults.
- `ReadConfig<T>()` creates `config.json` if missing and throws for invalid JSON.
- Use `KeybindList` for configurable controls.
- Do not store save-specific state in config.
- Farmhands should not write save data in multiplayer.
- For entity-level persistence, use `modData` keys prefixed by your mod ID.

## Manifest Essentials

Required for a C# mod:

```json
{
  "Name": "Your Mod Name",
  "Author": "YourName",
  "Version": "%ProjectVersion%",
  "Description": "One or two sentences about the mod.",
  "UniqueID": "YourName.YourModName",
  "EntryDll": "YourModName.dll",
  "MinimumApiVersion": "4.0.0",
  "UpdateKeys": []
}
```

Required for a content pack:

```json
{
  "Name": "Your Content Pack Name",
  "Author": "YourName",
  "Version": "1.0.0",
  "Description": "One or two sentences about the pack.",
  "UniqueID": "YourName.YourContentPackName",
  "ContentPackFor": {
    "UniqueID": "Pathoschild.ContentPatcher"
  },
  "UpdateKeys": []
}
```

Rules:

- `EntryDll` and `ContentPackFor` are mutually exclusive.
- Keep `UniqueID` stable after release.
- Use semantic versioning.
- Use `MinimumApiVersion` and `MinimumGameVersion` when the mod depends on newer APIs or game data.
- Declare required and optional dependencies in `Dependencies`.

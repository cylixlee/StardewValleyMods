# Android Compatibility

Android SMAPI support is experimental/community-port support. Treat PC and Android as different target environments even when the same mod package may work on both.

## Known Facts

- Official Android install page: `https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Android`.
- Android requires Stardew Valley Android `1.6.15.1+` at the time of this research.
- Android uses SMAPI Launcher and a SMAPI Android zip, not the normal PC installer flow.
- Android mod folder is commonly `/storage/emulated/0/StardewValley/Mods`.
- Android supports most SMAPI mods and content packs, not all.
- Android SMAPI port versions may lag behind official PC SMAPI versions.
- iOS and consoles do not support normal SMAPI modding.

Useful Android resources:

- Android install page: `https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Android`
- SMAPI Launcher: `https://github.com/NRTnarathip/SMAPILoader/releases`
- SMAPI Android fork: `https://github.com/NRTnarathip/SMAPI-Android-1.6/releases`
- Nexus Android package: `https://www.nexusmods.com/stardewvalley/mods/44436`

## Ask for These Details

For Android tasks, ask for:

- Stardew Valley Android version.
- SMAPI Android version.
- SMAPI Launcher version.
- Device model and whether the device is 64-bit.
- Whether the game is from Play Store or Galaxy Store.
- Android Launcher shared log.
- Full mod list and dependency versions.
- Exact mod folder structure.
- Whether the issue happens in single-player, multiplayer, or PC-Android cross-play.

## Compatibility Risks

Higher-risk features on Android:

- Harmony patches, especially transpilers and annotation-based `PatchAll`.
- Reflection into private game members.
- Native libraries or platform-specific DLLs.
- Windows-only APIs.
- Keyboard-only or mouse-only controls.
- Custom UI that assumes a large desktop screen.
- External processes, shell commands, registry access, or desktop browser launching.
- Direct file I/O outside SMAPI helper APIs.
- Compression, large file writes, and large texture processing.
- Multiplayer and PC-Android cross-play.

## Defaults for Android or Both

- Target `net6.0`.
- Use `Pathoschild.Stardew.ModBuildConfig` for PC build packaging, but do not assume Android auto-deploy.
- Keep `MinimumApiVersion` no higher than the Android SMAPI port actually supports if Android is supported.
- Set `MinimumGameVersion` if using newer game content fields or 1.6+ data.
- Use `Path.Combine`, `Helper.DirectoryPath`, `Helper.ModContent`, `Helper.Data`, and content APIs.
- Use `KeybindList` and provide touch-friendly alternatives.
- Use UI-scale-aware cursor and layout calculations.
- Avoid native libraries unless Android ABI support is confirmed.
- If Harmony is unavoidable, provide a config toggle and clear failure logs.

## Android Release Statement

Do not write "Android compatible" unless tested. Use precise wording:

- `Android: tested with Stardew Valley Android X, SMAPI Android Y, Launcher Z.`
- `Android: not tested.`
- `Android: unsupported due to Harmony/native dependencies.`
- `Android: expected to work but experimental; please provide Launcher Share Log for issues.`

## Android Install Structure

Expected after extraction:

```text
/storage/emulated/0/StardewValley/Mods/YourModName/manifest.json
/storage/emulated/0/StardewValley/Mods/YourModName/YourModName.dll
/storage/emulated/0/StardewValley/Mods/YourModName/assets/...
```

Common mistakes:

- Zip not extracted.
- Extra nested folder like `Mods/YourMod-1.0/YourMod/manifest.json`.
- `manifest.json` or DLL placed directly in `Mods` root.
- Missing dependency framework mod.
- PC-only release file installed on Android.

# Debugging SMAPI Mods

Use this checklist when a user reports build failures, load failures, runtime exceptions, or broken behavior.

## First Request

Ask for:

- PC: a `https://smapi.io/log/` link.
- Android: the SMAPI Launcher shared log link.
- The mod source or relevant files if editing code.
- Steps to reproduce.
- Whether this is single-player, multiplayer, split-screen, or PC-Android cross-play.

Avoid diagnosing from only a screenshot or the last stack trace line when a full log is available.

## Log Reading Order

1. SMAPI version.
2. Stardew Valley version.
3. Platform.
4. Mod list and skipped mods.
5. Update alerts.
6. Missing dependencies.
7. Manifest or JSON errors.
8. Assembly load failures.
9. First exception caused by the target mod.
10. Repeated errors and performance warnings.

## Build Error Checklist

- Project is a class library, not `.NET Framework`.
- Target framework is `net6.0`.
- `Pathoschild.Stardew.ModBuildConfig` is referenced.
- Game path detection works, or `GamePath` is configured.
- `EntryDll` matches the output DLL.
- Source uses correct namespaces: `StardewModdingAPI`, `StardewModdingAPI.Events`, and relevant game namespaces.
- If C# language features fail, consider `.csproj` language version or implicit usings settings.
- Do not target newer .NET versions unless the user has a concrete reason and accepts SMAPI loading risk.

## Load Error Checklist

- `manifest.json` exists in the mod folder.
- JSON is valid; use `https://smapi.io/json` if needed.
- Required fields exist: `Name`, `Author`, `Version`, `Description`, `UniqueID`, plus `EntryDll` or `ContentPackFor`.
- `EntryDll` and `ContentPackFor` are not both set.
- `UniqueID` is unique.
- Dependencies are installed and meet `MinimumVersion`.
- `MinimumApiVersion` is not higher than the installed SMAPI.
- `MinimumGameVersion` is not higher than the installed game.
- The release zip was extracted into its own folder under `Mods`.
- Required bundled DLLs are present and compatible.

## Runtime Error Checklist

- Invalid config JSON can make `ReadConfig<T>()` throw.
- Missing assets may be path or case mismatches.
- Use `Helper.ModContent` for mod assets and `Helper.GameContent` for game assets.
- Asset names should not include `.xnb`.
- Compare asset names with `IAssetName.IsEquivalentTo(...)`.
- For save data, ensure a save is loaded and farmhand restrictions are respected.
- For multiplayer, distinguish host/main player/farmhand behavior.
- For split-screen, store per-screen state in `PerScreen<T>`.
- For reflection, handle missing fields/methods gracefully.
- For Harmony, check whether the target method exists and whether the patch logs its own errors.

## Android Triage

Ask for:

- Stardew Valley Android version.
- SMAPI Android version.
- SMAPI Launcher version.
- Device architecture and Android version.
- Play Store or Galaxy Store source.
- Launcher Share Log.
- Exact folder structure under `/storage/emulated/0/StardewValley/Mods`.
- Whether all dependencies have Android-compatible versions.

Common Android causes:

- Wrong folder structure or zip not extracted.
- Android SMAPI port too old for `MinimumApiVersion`.
- Game version mismatch for `MinimumGameVersion`.
- PC-only dependency DLL or native library.
- Harmony patch failure.
- Keyboard/mouse-only input.
- Storage permission or file manager issue.

## Logging Guidance

- `Trace`: low-level developer details; written to log but normally hidden from console.
- `Debug`: troubleshooting details relevant to players.
- `Info`: normal player-facing information; use sparingly.
- `Warn`: possible problem.
- `Error`: actual error.
- `Alert`: rarely appropriate for mods.
- Use `Monitor.LogOnce(...)` for repeated warnings.
- Avoid logging every tick.

# Release Checklist

Use this before packaging or publishing a SMAPI mod or content pack.

## Version and Manifest

- Use semantic versioning.
- Increase version for every release.
- Prefer `.csproj` `<Version>` plus manifest `%ProjectVersion%` for C# mods.
- Keep `UniqueID` stable after release.
- Confirm `EntryDll` matches the DLL.
- Confirm `MinimumApiVersion` matches the newest SMAPI API actually used.
- Confirm `MinimumGameVersion` matches newest required game content/data.
- Confirm all dependencies and optional dependencies are declared.
- Add `UpdateKeys` before release.

Common update key formats:

```json
"UpdateKeys": [ "Nexus:541" ]
"UpdateKeys": [ "GitHub:Pathoschild/LookupAnything" ]
"UpdateKeys": [ "CurseForge:309243" ]
"UpdateKeys": [ "ModDrop:123338" ]
```

If multiple update keys exist, SMAPI uses the newest version it finds and links to the first matching source when versions tie.

## Build

- Rebuild the project.
- Confirm there are no build errors.
- Inspect the generated zip from `bin/Debug` or `bin/Release`.
- If using `Pathoschild.Stardew.ModBuildConfig`, prefer its generated zip.
- Android releases are not automatically deployed by PC build tooling.

## Zip Structure

Expected C# mod zip layout:

```text
YourModName/
  manifest.json
  YourModName.dll
  assets/
  i18n/
```

Expected content pack layout:

```text
YourContentPackName/
  manifest.json
  content.json
  assets/
```

Do not include:

- `obj/`.
- Source files unless intentionally publishing source separately.
- Secrets, tokens, private notes, or local config files.
- Stardew Valley game DLLs.
- `StardewModdingAPI.dll`.
- `MonoGame.Framework.dll`.
- `0Harmony.dll` unless there is a very unusual, justified reason.
- Windows-only `.exe` files unless the mod is explicitly PC-only and they are required.

## Compatibility Statement

Release descriptions should state:

- Tested Stardew Valley version.
- Tested SMAPI version.
- PC platforms tested: Windows, Linux, macOS.
- Android status: supported, tested, not tested, experimental, or unsupported.
- Multiplayer status: single-player only, host only, all players need it, or cross-play tested.
- Split-screen status if relevant.
- Required framework mods and versions.
- Known incompatibilities.
- How to submit logs.

Example:

```text
Compatibility:
- Stardew Valley: tested on 1.6.15.
- SMAPI: tested on 4.3.2+.
- PC: tested on Windows; Linux/macOS not tested.
- Android: not tested.
- Multiplayer: all players should install this mod.
```

## Android Release Notes

Only claim Android support if tested. If supported, include:

- Stardew Valley Android version.
- SMAPI Android version.
- Launcher version.
- Dependency Android versions.
- Install path: `/storage/emulated/0/StardewValley/Mods`.
- Any touch/UI/input limitations.

## Publishing Sites

- Nexus Mods: ensure file version matches manifest version.
- GitHub Releases: release tag must be semantic version for SMAPI update checks.
- CurseForge: latest file display name should include semantic version.
- ModDrop: official wiki notes that as of 2026-02 it may be unmoderated/unsafe; do not recommend it as the default primary publishing target.

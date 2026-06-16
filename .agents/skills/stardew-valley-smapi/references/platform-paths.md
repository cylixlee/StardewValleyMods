# Platform Paths

Prefer SMAPI APIs over hardcoded paths. Use this file when diagnosing installs or explaining where files are.

## Authoring Rules

- Use `this.Helper.DirectoryPath` for the mod folder.
- Use `Path.Combine(...)` for filesystem paths.
- Use `this.Helper.ModContent.Load<T>(...)` for mod assets.
- Use `this.Helper.GameContent.Load<T>(...)` for game assets.
- Use `this.Helper.Data` for JSON, save data, and global data.
- Use `Constants.TargetPlatform` for platform checks.
- Do not infer Linux from `Environment.OSVersion.Platform == Unix`; Android may also appear Unix-like.

## Common Mods Folders

PC mods are usually under the game folder:

```text
Stardew Valley/Mods
```

Android mods are commonly under:

```text
/storage/emulated/0/StardewValley/Mods
```

## Common PC Game Folders

Windows Steam:

```text
C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley
```

Windows GOG:

```text
C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley
C:\GOG Games\Stardew Valley
```

Windows Xbox App:

```text
C:\XboxGames\Stardew Valley
```

Linux Steam:

```text
~/.local/share/Steam/steamapps/common/Stardew Valley
~/.steam/steam/steamapps/common/Stardew Valley
~/.var/app/com.valvesoftware.Steam/data/Steam/steamapps/common/Stardew Valley
```

Linux GOG:

```text
~/GOGGames/StardewValley/game
```

macOS Steam:

```text
~/Library/Application Support/Steam/SteamApps/common/Stardew Valley/Contents/MacOS
```

macOS GOG:

```text
/Applications/Stardew Valley.app/Contents/MacOS
```

## Relevant SMAPI Constants

- `Constants.TargetPlatform`: target platform as `GamePlatform`.
- `Constants.GamePath`: detected game path.
- `Constants.ContentPath`: game content folder.
- `Constants.DataPath`: Stardew Valley data folder.
- `Constants.LogDir`: SMAPI log folder.
- `Constants.SavesPath`: save folder.
- `Constants.ModsPath`: active mods path.
- `Constants.DefaultModsPath`: default mods path.

## SMAPI Platform Concepts

SMAPI exposes platform and path information through public APIs and constants. This guidance is written for downstream mod developers using the public SMAPI API.

- Use `Constants.TargetPlatform` when platform-specific behavior is truly needed.
- Platform values include Android, Linux, macOS, and Windows.
- Android may behave like a Unix-like platform at the runtime level, so do not infer desktop Linux from generic Unix checks.
- Use `Constants.GamePath`, `Constants.ContentPath`, `Constants.DataPath`, `Constants.LogDir`, `Constants.SavesPath`, and `Constants.ModsPath` only when an absolute Stardew/SMAPI path is necessary.
- Prefer `Helper.DirectoryPath`, `Helper.ModContent`, `Helper.GameContent`, and `Helper.Data` for normal mod code.
- Android and Linux file systems may expose case-sensitivity differences. Keep filenames and references case-correct even when SMAPI can compensate in some APIs.

## Case Sensitivity

SMAPI enables case-insensitive file API behavior by default for Android and Linux in newer versions, but do not rely on it. Ensure filenames match references exactly, especially for assets, i18n files, and `manifest.json`.

---
name: stardew-valley-smapi
description: Build, debug, review, and release Stardew Valley SMAPI mods and content packs, including PC and Android compatibility. Use when the user mentions Stardew Valley modding, SMAPI, ModEntry, manifest.json, Content Patcher packs, SMAPI logs, Harmony patches, Android SMAPI, or Stardew mod release workflows.
---

# Stardew Valley SMAPI

## When to Use This Skill

Use this skill for Stardew Valley modding tasks involving SMAPI or SMAPI-adjacent packaging:

- Creating or modifying a C# SMAPI mod.
- Writing `ModEntry`, `Entry(IModHelper helper)`, event handlers, config, data, input, content, translation, multiplayer, or integration code.
- Creating, validating, or fixing `manifest.json` for a SMAPI mod or content pack.
- Diagnosing SMAPI build errors, load errors, runtime exceptions, missing dependencies, invalid JSON, asset issues, or player logs.
- Reviewing PC and Android compatibility for a mod.
- Preparing a SMAPI mod release package, update keys, Nexus/GitHub release notes, or compatibility statement.
- Deciding whether to use SMAPI APIs, Content Patcher/content packs, Reflection, or Harmony.

Do not use this skill for general Stardew Valley gameplay advice, non-modding C# questions, pixel art, story writing, or pure Content Patcher `content.json` syntax unless the task also involves SMAPI packaging, manifest, compatibility, or mod architecture.

## First Decision

Before implementing, ask or infer the target platform and mod type:

- Platform: `PC only`, `Android only`, or `both PC and Android`.
- Mod type: C# SMAPI mod, content pack, framework mod, or unknown.
- Scope: create, edit, debug, review, migrate, or release.

If the user wants Android support, treat Android SMAPI as experimental/community-port support. Do not claim Android compatibility unless the user has tested it or explicitly accepts that it is untested.

## Core Workflow

1. Identify whether C# is necessary. If the task only changes game data, images, maps, or dialogue, consider a content pack or Content Patcher first.
2. Inspect existing files before changing code: `manifest.json`, `.csproj`, `ModEntry.cs`, config models, assets, and logs.
3. Load the relevant local reference file from `references/` before relying on web pages. Use official web pages only when local references are insufficient, stale, or the task needs latest-version confirmation.
4. Prefer official SMAPI APIs over brittle techniques: events, content API, data API, input API, translations, mod registry, multiplayer API, and helper utilities.
5. Keep changes minimal and cross-platform by default.
6. Run or suggest the most relevant verification: build, SMAPI launch, log review, manifest validation, zip inspection, or platform-specific checklist.

## Creating a C# SMAPI Mod

Default choices:

- Use a class library targeting `net6.0`.
- Reference `Pathoschild.Stardew.ModBuildConfig`.
- Put the mod entry in `ModEntry.cs`.
- Use `manifest.json` with `EntryDll` for C# mods.
- Use `%ProjectVersion%` in manifest when the project has a `<Version>` property.
- Do not add Harmony unless the user needs behavior that cannot reasonably be implemented with SMAPI APIs.

Minimal entry pattern:

```csharp
using StardewModdingAPI;
using StardewModdingAPI.Events;

internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.Monitor.Log("Mod loaded.", LogLevel.Info);
    }
}
```

## Cross-Platform Defaults

For PC and Android compatibility:

- Use `Path.Combine` for filesystem paths.
- Use `this.Helper.DirectoryPath` for the mod folder.
- Use `this.Helper.ModContent`, `this.Helper.GameContent`, and `this.Helper.Data` instead of hardcoded paths.
- Compare asset names with `IAssetName.IsEquivalentTo(...)` or normalize with `PathUtilities.NormalizeAssetName(...)` as appropriate.
- Use `KeybindList` for configurable input.
- Consider UI scale, touch input, and small screens when drawing UI.
- Use `Context.IsMainPlayer`, `Context.IsMultiplayer`, and SMAPI multiplayer APIs for multiplayer logic.
- Use `Constants.TargetPlatform == GamePlatform.Android` only when a real platform branch is needed.

Avoid:

- Hardcoded Windows paths, `\\` separators, or PC game folders.
- Hardcoded Android storage paths inside mod logic.
- Keyboard-only or mouse-wheel-only interactions without alternatives.
- Windows-only APIs such as registry, WinForms, WPF, shell commands, or desktop assumptions.
- Bundling game DLLs, SMAPI DLLs, MonoGame DLLs, or native libraries without a clear reason.

For detailed Android guidance, load [references/android-compatibility.md](references/android-compatibility.md) and [references/platform-paths.md](references/platform-paths.md).

For detailed API selection, event choice, content editing, config/data, and manifest rules, load [references/api-checklist.md](references/api-checklist.md).

## Debugging Workflow

When diagnosing errors:

1. Ask for a SMAPI log link from `https://smapi.io/log/` on PC, or the Android Launcher shared log for Android.
2. Check the log header for game version, SMAPI version, platform, mod list, skipped mods, update alerts, and dependencies.
3. Check the first relevant error, not just the last repeated error.
4. Inspect `manifest.json`, `EntryDll`, dependencies, `MinimumApiVersion`, `MinimumGameVersion`, invalid JSON, missing assets, and assembly load failures.
5. For Android, also ask for Android game version, SMAPI Android version, Launcher version, device architecture, install folder, and dependency Android versions.

Use [references/debugging.md](references/debugging.md) for detailed triage.

## Harmony Policy

Harmony is a last resort. Before using it, check whether events, content/data APIs, input suppression, public game APIs, mod integrations, or SMAPI reflection can solve the task.

If Harmony is necessary:

- Add `<EnableHarmony>true</EnableHarmony>`.
- Use SMAPI bundled Harmony.
- Prefer Harmony code API over annotations and `PatchAll`.
- Prefer postfix over prefix when possible.
- Avoid transpilers unless there is no alternative.
- Patch methods should be `static`.
- Wrap patch logic in `try/catch`, log errors, and fall back to original game logic.
- Warn that Harmony is higher risk for Android and future game updates.

Use [references/harmony.md](references/harmony.md) for patterns and warnings.

## Release Workflow

Before release:

- Update semantic version.
- Add or verify `UpdateKeys`.
- Build/rebuild and inspect the generated zip.
- Confirm the zip has a top-level mod folder containing `manifest.json`, the entry DLL, assets, and i18n files as needed.
- Do not include source, `obj`, temporary files, secrets, game DLLs, or SMAPI DLLs.
- State tested game versions, SMAPI versions, PC platforms, Android status, multiplayer status, dependencies, and known limitations.

Use [references/release.md](references/release.md) for the release checklist.

Use [references/templates.md](references/templates.md) when creating a new mod, manifest, `.csproj`, config class, or minimal `ModEntry`.

## Local References First

This skill includes local references distilled from the SMAPI source and official docs. Prefer these files before fetching web pages:

- [references/api-checklist.md](references/api-checklist.md): API selection, event choice, content API, config/data, and manifest rules.
- [references/android-compatibility.md](references/android-compatibility.md): Android SMAPI support status, risks, diagnostics, and release wording.
- [references/platform-paths.md](references/platform-paths.md): PC/Android paths, SMAPI platform concepts, and path-handling rules.
- [references/debugging.md](references/debugging.md): build/load/runtime triage, logs, Android diagnostics, and log levels.
- [references/release.md](references/release.md): versioning, update keys, zip structure, compatibility statements, and publishing notes.
- [references/harmony.md](references/harmony.md): Harmony last-resort policy, setup, safe patch pattern, and Android risks.
- [references/templates.md](references/templates.md): manifest, `.csproj`, `ModEntry.cs`, and config templates.

Fetch official web pages only when a task needs details not covered locally or when verifying current versions, release pages, update-key behavior, or Android port status.

## Important References

- Official Modding Index: `https://stardewvalleywiki.com/Modding:Index`
- SMAPI APIs: `https://stardewvalleywiki.com/Modding:Modder_Guide/APIs`
- Get Started: `https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started`
- Manifest: `https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest`
- Events: `https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events`
- Content API: `https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Content`
- Android install: `https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Android`
- SMAPI log parser: `https://smapi.io/log/`
- JSON validator: `https://smapi.io/json`
- Mod compatibility list: `https://smapi.io/mods`

# SMAPI Best-Practice Implementation Checklist

Use this checklist when creating, refactoring, or reviewing a SMAPI C# mod.

## Architecture

- Keep `ModEntry` small and lifecycle-aware.
- Put behavior in managers, handlers, services, commands, integrations, and patchers.
- Use `internal` by default.
- Enable nullable and initialize lifecycle fields explicitly.
- Use constants for asset names, message types, custom data keys, and public IDs.
- Use namespaced IDs for custom assets and data.

## Lifecycle

- Use `Entry` only for basic initialization and event subscription.
- Use `GameLaunched` for other-mod APIs and integration registration.
- Use `SaveLoaded` for per-save initialization and migrations.
- Use `DayStarted` for daily refreshes.
- Use `ReturnedToTitle` to clear save state.
- Avoid `UpdateTicked` unless narrower events cannot solve the task.
- Throttle or queue expensive work.

## Context Guards

- Check `Context.IsWorldReady` before save/world access.
- Check `Context.IsPlayerFree` before gameplay input.
- Check `Context.IsMainPlayer` before shared world mutation.
- Check `e.IsLocalPlayer` for player events that should be local only.
- Use `PerScreen<T>` for split-screen UI/input state.

## Config

- Use POCO config classes with defaults.
- Use `KeybindList` for hotkeys.
- Use case-insensitive dictionaries for IDs and names.
- Normalize config after deserialization.
- Handle null collections from old config files.
- Apply config changes immediately after saving from GMCM.

## Persistence

- Prefer vanilla state, `modData`, or `Game1.CustomData` before custom save files.
- Run migrations only as host/main player.
- Track migration version semantically.
- Log migrations clearly.
- Do not silently discard invalid legacy data.
- Delay destructive legacy cleanup until migrated data is safely saved.

## Content

- Use `Helper.ModContent` for internal mod files.
- Use `Helper.GameContent` for game or mod-editable assets.
- Use `AssetRequested` for asset edits.
- Compare asset names with `IsEquivalentTo`.
- Use `AssetEditPriority` intentionally.
- Invalidate only affected caches when settings or state change.

## I18n

- Put user-facing text in `i18n/default.json`.
- Prefer generated `I18n` helpers.
- Use full sentence translations with tokens.
- Avoid concatenating localized fragments.

## Input And Commands

- Use `ButtonsChanged` plus `KeybindList.JustPressed()` for configurable controls.
- Suppress input only after deciding to handle it.
- Validate command context before changing state.
- Make command responses actionable.

## Integrations And APIs

- Isolate optional integrations in small classes.
- Check installed mod ID, minimum version, and API availability.
- Catch exceptions from third-party APIs.
- Keep public API interfaces stable.
- Validate API inputs defensively.

## Multiplayer

- Mutate shared world state only on host/main player.
- Let farmhands request host actions through multiplayer messages.
- Validate incoming messages by mod ID, type, sender, and role.
- Check host mod version before enabling synchronized farmhand features.

## Diagnostics

- Use actionable logs.
- Use `Info` for intentional changes and command results.
- Use `Warn` for recoverable setup/data issues.
- Use `Error` when a feature is disabled or required data is invalid.
- Use `LogOnce` for repeated failures.
- Do not swallow exceptions silently.

## Harmony

- Treat Harmony as a last resort.
- Prefer SMAPI events, content APIs, public APIs, input suppression, integrations, or reflection first.
- Keep patches isolated.
- Prefer postfixes.
- Avoid transpilers unless unavoidable.
- Catch exceptions inside patch logic.

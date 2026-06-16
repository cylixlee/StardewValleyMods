# Recommended SMAPI Patterns

This reference distills implementation patterns observed across mature SMAPI mods, including large framework-style mods, content API-heavy mods, utility mods, and shared libraries.

## Common Repository Layout

Typical C# SMAPI mods use:

```text
ModName/
├── ModEntry.cs
├── ModName.csproj
├── manifest.json
├── Framework/
├── Api/
├── assets/
├── i18n/default.json
└── docs/
```

Large mods commonly add:

- `Framework/Commands/` for console commands.
- `Framework/Integrations/` for optional dependencies.
- `Patches/` for Harmony patches.
- `Api/` for public interfaces and implementations.
- Domain folders like `Handlers/`, `Layers/`, `Components/`, `Menus/`, or `Models/`.

Shared code is factored into common projects instead of duplicated across mods.

## ModEntry Responsibilities

`ModEntry` generally handles SMAPI glue code only:

- Initialize translations.
- Read config.
- Create managers/services.
- Subscribe events.
- Register console commands.
- Register integrations in `GameLaunched`.
- Expose APIs with `GetApi()` when needed.

Business logic is moved into managers, handlers, integrations, command classes, and patchers.

## Event Usage Patterns

Observed event choices:

- `GameLaunched`: other-mod APIs, Generic Mod Config Menu, toolbar icons, tokens, late registrations.
- `SaveLoaded`: per-save state, migrations, host/farmhand version checks.
- `DayStarted`: daily cache resets, world scans, daily rule refresh.
- `ReturnedToTitle`: clear per-save state.
- `UpdateTicked`: only for ongoing behavior that cannot use narrower events; usually guarded and throttled.
- `AssetRequested`: load/edit assets lazily.
- `AssetReady` and cache invalidation: refresh runtime state after content reloads.
- `ButtonsChanged`: configurable input with `KeybindList.JustPressed()`.
- `Warped`: local-player location state, guarded by `e.IsLocalPlayer`.
- `World.*ListChanged`: incremental updates instead of full rescans.

## Guard Patterns

Common guards:

- `Context.IsWorldReady` before world/save access.
- `Context.IsPlayerFree` before normal gameplay input.
- `Context.IsMainPlayer` before shared world mutation.
- `Context.IsMultiplayer` for multiplayer-specific flows.
- `Context.IsSplitScreen` and `PerScreen<T>` for split-screen state.
- `e.IsLocalPlayer` for local player events.

Expensive work is usually controlled with intervals, dirty flags, queued reloads, or incremental event tracking.

## Config Patterns

Config classes are POCOs with defaults:

- `ModConfig` for settings.
- `ModConfigKeys` for keybindings.
- `KeybindList` for configurable controls.
- Case-insensitive dictionaries for IDs and user-entered names.
- `[OnDeserialized]` to repair nulls, normalize collections, remove invalid entries, or map legacy fields.

Generic Mod Config Menu save callbacks usually write config and immediately apply the change by refreshing managers, invalidating assets, or recalculating rules.

## Persistence And Migration Patterns

Preferred storage order:

1. Vanilla state or game data when possible.
2. `modData` for object-specific state.
3. `Game1.CustomData` for lightweight per-save global state.
4. `Helper.Data.ReadSaveData<T>()` only when custom save data is necessary.

Migration patterns:

- Run migrations only as host/main player.
- Store a semantic version for last migration.
- Apply migrations incrementally by version.
- Log intentional migrations at `Info`.
- Warn and skip invalid legacy objects.
- Delay deleting legacy data until after successful save when needed.

## Content And I18n Patterns

Content access:

- `Helper.ModContent` for internal files.
- `Helper.GameContent` for game assets and mod-editable assets.
- `AssetRequested` with `e.NameWithoutLocale.IsEquivalentTo(...)` for edits.
- `AssetEditPriority` when load/edit ordering matters.
- Explicit `InvalidateCache(...)` when config, language, or save state affects content.

Translations:

- `i18n/default.json` is the source of user-facing text.
- Generated `I18n` helpers are preferred.
- Full sentences with tokens are preferred over concatenated fragments.
- Missing translation files are logged as setup warnings when they affect UX.

## Optional Integrations

Optional dependencies are isolated into integration classes that check:

- Installed mod ID.
- Minimum version.
- API availability.
- Runtime exceptions from third-party APIs.

Integration failures should normally degrade one feature, not crash the mod.

## Multiplayer And Split-Screen Patterns

World mutation is host-only. Farmhands either run local UI behavior or send multiplayer messages to host.

Incoming messages should validate:

- Source mod ID.
- Message type.
- Sender/player identity.
- Current save/world context.
- Whether the receiver is allowed to process the action.

Split-screen UI/input state should use `PerScreen<T>` or equivalent per-screen storage.

## Diagnostics Patterns

Logs are user-actionable:

- What failed.
- Which feature is disabled or degraded.
- How to fix it.
- Technical details when useful.

Repeated messages use `LogOnce` or verbose logging. Risky loops and patches catch exceptions and disable only the failing feature when possible.

## Historical Patterns To Avoid

Avoid carrying forward old patterns unless maintaining legacy code:

- Legacy asset editor APIs when `AssetRequested` is available.
- Direct private game-state manipulation when events/content APIs work.
- Unthrottled `UpdateTicked` scans.
- Custom save files when vanilla state, `modData`, or `Game1.CustomData` is enough.
- Harmony patches before checking SMAPI-native alternatives.

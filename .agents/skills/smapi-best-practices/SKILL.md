---
name: smapi-best-practices
description: Apply recommended best practices for Stardew Valley SMAPI mod architecture, lifecycle events, config, content APIs, i18n, multiplayer, diagnostics, migrations, and release-quality code. Use when designing, implementing, reviewing, refactoring, or debugging SMAPI C# mods and when the user asks for robust SMAPI implementation patterns.
---

# SMAPI Best Practices

## When to Use This Skill

Use this skill for Stardew Valley SMAPI C# mod work where maintainability, compatibility, robustness, or proven architecture patterns matter:

- Designing a new SMAPI mod architecture.
- Implementing or reviewing `ModEntry`, events, config, content edits, i18n, commands, APIs, integrations, multiplayer, or migrations.
- Refactoring an existing mod toward cleaner lifecycle handling and service/manager separation.
- Diagnosing recurring runtime errors, noisy logs, asset reload issues, config migration bugs, or multiplayer/split-screen issues.
- Deciding whether behavior belongs in SMAPI APIs, content APIs, services, optional integrations, reflection, or Harmony patches.

This skill complements `stardew-valley-smapi`. Use `stardew-valley-smapi` for general setup, manifest/release workflow, Android compatibility, and broad SMAPI debugging. Use this skill for implementation quality and best-practice review.

Do not use this skill for general Stardew Valley gameplay advice, non-SMAPI C# code, art, writing, or pure Content Patcher content unless SMAPI architecture or integration is involved.

## Core Workflow

1. Inspect the existing mod first: `manifest.json`, `.csproj`, `ModEntry.cs`, config models, `Framework/`, `i18n/`, `assets/`, commands, integrations, and patches.
2. Choose the smallest reliable SMAPI-native solution before adding abstractions, reflection, or Harmony.
3. Keep `ModEntry` focused on SMAPI glue: read config, initialize services, subscribe events, register commands, expose APIs, and forward work.
4. Move real behavior into focused managers, services, handlers, commands, integrations, and patchers when the mod is more than trivial.
5. Treat lifecycle timing, multiplayer host/client roles, split-screen state, content invalidation, config normalization, and migrations as explicit design concerns.
6. Verify with the relevant check: build, SMAPI launch/log, asset reload, config reload, multiplayer host/farmhand test, split-screen test, or pure-logic unit tests.

## Load References

Load detailed references only when needed:

- [references/recommended-patterns.md](references/recommended-patterns.md): rationale and recommended patterns observed across mature SMAPI mods.
- [references/implementation-checklist.md](references/implementation-checklist.md): compact checklist for creating, refactoring, or reviewing a SMAPI C# mod.
- [references/templates.md](references/templates.md): reusable code skeletons for common best-practice patterns.

Reference selection:

- Load `recommended-patterns.md` when the user asks why a practice is recommended, asks for robust SMAPI implementation patterns, or needs architecture guidance.
- Load `implementation-checklist.md` when reviewing code, planning a refactor, or checking whether an implementation is robust.
- Load `templates.md` when writing or rewriting `ModEntry`, config normalization, content edits, migrations, optional integrations, or multiplayer message handling.

## Default Rules

- Prefer `Entry` only for basic initialization and event subscription.
- Prefer `GameLaunched` for other-mod APIs and late integration registration.
- Prefer `SaveLoaded` for per-save state and migrations.
- Prefer `AssetRequested` for content edits; avoid legacy asset editor patterns.
- Prefer `Helper.ModContent` for internal files and `Helper.GameContent` for game/mod-editable assets.
- Prefer POCO config classes with defaults, `KeybindList`, case-insensitive ID dictionaries, and deserialization normalization.
- Prefer `i18n/default.json` and generated `I18n` helpers for user-facing text.
- Prefer host-only shared world mutation with `Context.IsMainPlayer`.
- Prefer `PerScreen<T>` or equivalent per-screen storage for split-screen UI/input state.
- Prefer isolated optional integration classes that validate installed mod, version, and API availability.
- Prefer actionable logs, correct log levels, and `LogOnce` for repeated failures.
- Treat Harmony as a last resort after SMAPI events, content APIs, public APIs, integrations, input suppression, and reflection helpers.

## Review Priorities

When reviewing or refactoring, check these first:

- `ModEntry` is small and lifecycle-aware.
- Event handlers have the right `Context` and event-argument guards.
- Expensive work is not done every tick without throttling, dirty flags, or incremental events.
- Config handles old files, null collections, invalid values, and immediate reload after saving.
- Content edits use precise asset-name checks and invalidate only affected caches.
- Multiplayer logic separates host-only world mutation from farmhand-local behavior.
- Split-screen state is not stored in unsafe static/global fields.
- Optional integrations fail gracefully.
- Logs explain what failed, impact, fix, and technical details when useful.
- Migrations are versioned, host-only, logged, and safe against invalid legacy data.

## Simple Vs Large Mods

For simple mods, a compact `ModEntry`, `ModConfig`, `manifest.json`, and `i18n/default.json` may be enough. Still use lifecycle guards, config defaults, translations, and clear logs.

For medium and large mods, split behavior into managers, handlers, command classes, integrations, and patchers. Design cache invalidation, content reloads, multiplayer sync, migrations, APIs, and tests deliberately.

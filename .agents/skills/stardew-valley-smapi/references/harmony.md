# Harmony Guidance

Harmony patches rewrite game methods. Use them only when safer SMAPI APIs cannot solve the task.

Official page: `https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony`

## Before Using Harmony

Check alternatives first:

- SMAPI events.
- Content API or data edits.
- Input API and input suppression.
- Public game APIs.
- Mod-provided APIs via `ModRegistry`.
- SMAPI Reflection helper.
- Content packs or Content Patcher if the task is data/content only.

## Project Setup

Add to the first `.csproj` `PropertyGroup`:

```xml
<EnableHarmony>true</EnableHarmony>
```

Use SMAPI bundled Harmony. Do not add a separate Harmony package unless there is a concrete, justified reason.

## Preferred Pattern

Use Harmony code API, not annotations or `PatchAll`:

```csharp
using HarmonyLib;
using StardewModdingAPI;

internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        ObjectPatches.Initialize(this.Monitor);

        var harmony = new Harmony(this.ModManifest.UniqueID);
        harmony.Patch(
            original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.canBePlacedHere)),
            postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.After_CanBePlacedHere))
        );
    }
}

internal static class ObjectPatches
{
    private static IMonitor Monitor = null!;

    internal static void Initialize(IMonitor monitor)
    {
        Monitor = monitor;
    }

    internal static void After_CanBePlacedHere(ref bool __result)
    {
        try
        {
            // Patch logic here.
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(After_CanBePlacedHere)}:\n{ex}", LogLevel.Error);
        }
    }
}
```

For prefixes that can skip original logic, return original behavior when the patch fails.

## Rules

- Prefer postfix over prefix when possible.
- Avoid transpilers unless absolutely necessary.
- Avoid `[HarmonyPatch]` annotations and `PatchAll` by default.
- Patch methods must be `static`.
- Validate target methods and signatures.
- Wrap patch logic in `try/catch`.
- Log failures under your own mod.
- Provide config toggles for risky patches when practical.
- Document tested game and SMAPI versions.

## Android and Platform Risk

Harmony is especially risky for Android and future game updates because SMAPI often cannot rewrite Harmony patches for compatibility. Annotation-based patches are more brittle because SMAPI cannot reliably rewrite compiled annotations.

For Android or both-platform mods:

- Avoid Harmony if possible.
- Avoid transpilers.
- Prefer small postfix patches.
- Add runtime checks and graceful fallback.
- Clearly mark Android status in release notes.

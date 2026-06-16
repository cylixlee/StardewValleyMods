using System.Reflection;
using HarmonyLib;
using System.Runtime.CompilerServices;
using StardewModdingAPI;
using StardewValley;

namespace CylixLee.StardewValley.FarmCleaner;

public static class FarmCleanerPatches
{
    internal static bool magnetBoostActive;
    internal static readonly List<Item> capturedItems = [];
    internal static readonly HashSet<Item> overflowedItems = [];
    internal static bool skipIntercept;
    internal static bool blockExperience;

    public static void Apply(string uniqueId, IMonitor monitor)
    {
        var harmony = new Harmony(uniqueId);

        PatchIf(harmony,
            original: AccessTools.Method(typeof(Farmer), "GetAppliedMagneticRadius"),
            prefix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(MagneticRadiusPrefix)),
            monitor: monitor);

        PatchIf(harmony,
            original: AccessTools.Method(typeof(Farmer), "addItemToInventory",
                [typeof(Item)]),
            postfix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(AddItemToInventoryPostfix)),
            monitor: monitor);

        PatchIf(harmony,
            original: AccessTools.Method(typeof(Farmer), "gainExperience",
                [typeof(int), typeof(int)]),
            prefix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(GainExperiencePrefix)),
            monitor: monitor);

        var couldAcceptMethod =
            AccessTools.Method(typeof(Farmer), "couldInventoryAcceptThisItem",
                [typeof(Item)])
            ?? AccessTools.Method(typeof(Farmer), "couldInventoryAcceptThisItem",
                [typeof(Item), typeof(bool)]);

        var couldAcceptPrefix = couldAcceptMethod.GetParameters().Length == 1
            ? new HarmonyMethod(typeof(FarmCleanerPatches), nameof(CouldInventoryAcceptPrefix))
            : new HarmonyMethod(typeof(FarmCleanerPatches), nameof(CouldInventoryAcceptPrefixAndroid));

        PatchIf(harmony,
            original: couldAcceptMethod,
            prefix: couldAcceptPrefix,
            monitor: monitor);

        PatchIf(harmony,
            original: AccessTools.Method(typeof(Farmer), "couldInventoryAcceptThisItem",
                [typeof(string), typeof(int), typeof(int)]),
            prefix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(CouldInventoryAcceptPrefixMinimal)),
            monitor: monitor);
    }

    private static void PatchIf(Harmony harmony, MethodInfo? original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, IMonitor? monitor = null)
    {
        if (original is null)
        {
            monitor?.Log("Failed to apply patch: target method not found. Use ilspycmd to verify method name and parameter types.", LogLevel.Error);
            return;
        }

        try
        {
            harmony.Patch(original, prefix, postfix);
            monitor?.Log($"Patched {original.DeclaringType?.Name}.{original.Name}({string.Join(", ", original.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            monitor?.Log($"Failed to apply patch to {original.DeclaringType?.Name}.{original.Name}: {ex.Message}", LogLevel.Error);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool MagneticRadiusPrefix(Farmer __instance, ref int __result)
    {
        if (magnetBoostActive)
        {
            __result = 500000;
            return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddItemToInventoryPostfix(Farmer __instance, Item item, ref Item __result)
    {
        if (!magnetBoostActive || skipIntercept || __result is null)
            return;

        if (overflowedItems.Contains(item))
            return;

        capturedItems.Add(__result);
        __result = null!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CouldInventoryAcceptPrefix(Farmer __instance, Item item, ref bool __result)
    {
        if (magnetBoostActive)
        {
            __result = true;
            return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CouldInventoryAcceptPrefixAndroid(Farmer __instance, Item item, bool message_if_full, ref bool __result)
    {
        return CouldInventoryAcceptPrefix(__instance, item, ref __result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool GainExperiencePrefix()
    {
        if (blockExperience)
            return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CouldInventoryAcceptPrefixMinimal(Farmer __instance, ref bool __result)
    {
        if (magnetBoostActive)
        {
            __result = true;
            return false;
        }
        return true;
    }
}

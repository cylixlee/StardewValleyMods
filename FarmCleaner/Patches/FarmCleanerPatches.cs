using System.Reflection;
using System.Runtime.CompilerServices;
using CylixLee.StardewValley.FarmCleaner.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CylixLee.StardewValley.FarmCleaner.Patches;

internal static class FarmCleanerPatches
{
    private const int MagneticRadius = 500000;

    private static readonly Stack<Debris> UpdatingDebris = new();

    internal static CleanupSession? ActiveSession { get; set; }
    internal static bool BlockExperience { get; set; }

    public static void Apply(string uniqueId, IMonitor monitor)
    {
        Harmony harmony = new(uniqueId);

        PatchIf(
            harmony,
            AccessTools.Method(typeof(Debris), nameof(Debris.updateChunks)),
            prefix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(UpdateChunksPrefix)),
            postfix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(UpdateChunksPostfix)),
            monitor: monitor);

        PatchIf(
            harmony,
            AccessTools.Method(typeof(Farmer), nameof(Farmer.GetAppliedMagneticRadius)),
            postfix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(GetAppliedMagneticRadiusPostfix)),
            monitor: monitor);

        MethodInfo? couldAcceptItem = AccessTools.Method(typeof(Farmer), nameof(Farmer.couldInventoryAcceptThisItem), [typeof(Item)]);
        if (couldAcceptItem is not null)
        {
            PatchIf(
                harmony,
                couldAcceptItem,
                prefix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(CouldInventoryAcceptItemPrefix)),
                monitor: monitor);
        }
        else
        {
            PatchIf(
                harmony,
                AccessTools.Method(typeof(Farmer), nameof(Farmer.couldInventoryAcceptThisItem), [typeof(Item), typeof(bool)]),
                prefix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(CouldInventoryAcceptItemPrefix)),
                monitor: monitor);
        }

        MethodInfo? couldAcceptId = AccessTools.Method(typeof(Farmer), nameof(Farmer.couldInventoryAcceptThisItem), [typeof(string), typeof(int), typeof(int)])
            ?? AccessTools.Method(typeof(Farmer), nameof(Farmer.couldInventoryAcceptThisItem), [typeof(string), typeof(int)]);
        PatchIf(
            harmony,
            couldAcceptId,
            prefix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(CouldInventoryAcceptIdPrefix)),
            monitor: monitor);

        MethodInfo? addItemToInventoryBool = AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool), [typeof(Item), typeof(bool)])
            ?? AccessTools.Method(typeof(Farmer), nameof(Farmer.addItemToInventoryBool), [typeof(Item)]);
        PatchIf(
            harmony,
            addItemToInventoryBool,
            postfix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(AddItemToInventoryBoolPostfix)),
            monitor: monitor);

        PatchIf(
            harmony,
            AccessTools.Method(typeof(Farmer), nameof(Farmer.gainExperience), [typeof(int), typeof(int)]),
            prefix: new HarmonyMethod(typeof(FarmCleanerPatches), nameof(GainExperiencePrefix)),
            monitor: monitor);
    }

    private static void PatchIf(Harmony harmony, MethodInfo? original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, IMonitor? monitor = null)
    {
        if (original is null)
        {
            monitor?.Log("Failed to apply Farm Cleaner patch: target method not found.", LogLevel.Error);
            return;
        }

        try
        {
            harmony.Patch(original, prefix, postfix);
        }
        catch (Exception ex)
        {
            monitor?.Log($"Failed to patch {original.DeclaringType?.Name}.{original.Name}: {ex}", LogLevel.Error);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UpdateChunksPrefix(Debris __instance)
    {
        if (ActiveSession?.Tracks(__instance) == true)
            UpdatingDebris.Push(__instance);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UpdateChunksPostfix(Debris __instance)
    {
        if (UpdatingDebris.Count > 0 && ReferenceEquals(UpdatingDebris.Peek(), __instance))
            UpdatingDebris.Pop();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GetAppliedMagneticRadiusPostfix(Farmer __instance, ref int __result)
    {
        if (IsTrackedDebrisPickup(__instance))
            __result = Math.Max(__result, MagneticRadius);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CouldInventoryAcceptItemPrefix(Farmer __instance, ref bool __result)
    {
        if (!IsTrackedDebrisPickup(__instance))
            return true;

        __result = true;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CouldInventoryAcceptIdPrefix(Farmer __instance, ref bool __result)
    {
        if (!IsTrackedDebrisPickup(__instance))
            return true;

        __result = true;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddItemToInventoryBoolPostfix(Farmer __instance, Item item, ref bool __result)
    {
        if (__result || !IsTrackedDebrisPickup(__instance) || ActiveSession is null)
            return;

        ActiveSession.CaptureOverflow(item);
        item.Stack = 0;
        __result = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GainExperiencePrefix()
    {
        return !BlockExperience;
    }

    private static bool IsTrackedDebrisPickup(Farmer farmer)
    {
        return UpdatingDebris.Count > 0 && ActiveSession?.Tracks(farmer) == true;
    }
}

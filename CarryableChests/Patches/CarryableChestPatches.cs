using System.Reflection;
using CylixLee.StardewValley.CarryableChests.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace CylixLee.StardewValley.CarryableChests.Patches;

internal static class CarryableChestPatches
{
    private static CarryableChestCoordinator? coordinator;

    public static void Apply(string uniqueId, IMonitor monitor, CarryableChestCoordinator coordinator)
    {
        CarryableChestPatches.coordinator = coordinator;
        Harmony harmony = new(uniqueId);

        PatchIf(harmony,
            AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.addItem)),
            prefix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(ChestAddItemPrefix)),
            monitor: monitor);

        PatchIf(harmony,
            AccessTools.DeclaredMethod(typeof(Item), nameof(Item.canBeDropped)),
            postfix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(ItemCanBeDroppedPostfix)),
            monitor: monitor);

        PatchIf(harmony,
            AccessTools.DeclaredMethod(typeof(Item), nameof(Item.canBeTrashed)),
            postfix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(ItemCanBeTrashedPostfix)),
            monitor: monitor);

        PatchIf(harmony,
            AccessTools.DeclaredMethod(typeof(Item), nameof(Item.canStackWith)),
            postfix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(ItemCanStackWithPostfix)),
            monitor: monitor);

        PatchIf(harmony,
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.maximumStackSize)),
            postfix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(ObjectMaximumStackSizePostfix)),
            monitor: monitor);

        PatchIf(harmony,
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.placementAction)),
            prefix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(ObjectPlacementActionPrefix)),
            postfix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(ObjectPlacementActionPostfix)),
            monitor: monitor);
    }

    private static void PatchIf(Harmony harmony, MethodInfo? original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, IMonitor? monitor = null)
    {
        if (original is null)
        {
            monitor?.Log("Failed to apply Carryable Chests patch: target method not found.", LogLevel.Error);
            return;
        }

        try
        {
            harmony.Patch(original, prefix, postfix);
            monitor?.Log($"Patched {original.DeclaringType?.Name}.{original.Name}.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            monitor?.Log($"Failed to apply Carryable Chests patch to {original.DeclaringType?.Name}.{original.Name}: {ex.Message}", LogLevel.Error);
        }
    }

    private static bool ChestAddItemPrefix(Item item, ref Item __result)
    {
        if (!ChestMetadata.IsCarriedChest(item))
            return true;

        __result = item;
        return false;
    }

    private static void ItemCanBeDroppedPostfix(Item __instance, ref bool __result)
    {
        if (ChestMetadata.IsCarriedChest(__instance))
            __result = false;
    }

    private static void ItemCanBeTrashedPostfix(Item __instance, ref bool __result)
    {
        if (ChestMetadata.IsCarriedChest(__instance))
            __result = false;
    }

    private static void ItemCanStackWithPostfix(Item __instance, ISalable other, ref bool __result)
    {
        if (ChestMetadata.IsCarriedChest(__instance) || ChestMetadata.IsCarriedChest(other as Item))
            __result = false;
    }

    private static void ObjectMaximumStackSizePostfix(SObject __instance, ref int __result)
    {
        if (ChestMetadata.IsCarriedChest(__instance))
            __result = 1;
    }

    private static bool ObjectPlacementActionPrefix(SObject __instance, ref bool __result)
    {
        if (!ChestMetadata.IsCarriedChest(__instance) || !Context.IsMultiplayer || Context.IsMainPlayer)
            return true;

        Game1.showRedMessage(I18n.MultiplayerHostPlaceOnly);
        __result = false;
        return false;
    }

    private static void ObjectPlacementActionPostfix(SObject __instance, GameLocation location, int x, int y, Farmer who, ref bool __result)
    {
        if (!__result || __instance is not Chest chest || !ChestMetadata.IsCarriedChest(chest) || coordinator is null)
            return;

        Vector2 tile = new((int)(x / (float)Game1.tileSize), (int)(y / (float)Game1.tileSize));
        if (!location.Objects.TryGetValue(tile, out SObject? placedObject) || placedObject is not Chest)
            return;

        coordinator.CompletePlacement(chest, location, tile, who);
    }
}

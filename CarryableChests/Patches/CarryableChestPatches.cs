using System.Reflection;
using CylixLee.StardewValley.CarryableChests.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace CylixLee.StardewValley.CarryableChests.Patches;

internal static class CarryableChestPatches
{
    private static CarryableChestCoordinator? coordinator;
    private static Func<ModConfig>? getConfig;
    private static bool placementAllowed;

    public static void Apply(string uniqueId, IMonitor monitor, CarryableChestCoordinator coordinator, Func<ModConfig> getConfig)
    {
        CarryableChestPatches.coordinator = coordinator;
        CarryableChestPatches.getConfig = getConfig;
        Harmony harmony = new(uniqueId);

        PatchIf(harmony,
            AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.addItem)),
            prefix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(ChestAddItemPrefix)),
            monitor: monitor);

        PatchIf(harmony,
            AccessTools.DeclaredMethod(typeof(Game1), nameof(Game1.pressUseToolButton)),
            prefix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(Game1PressUseToolButtonPrefix)),
            postfix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(Game1PressUseToolButtonPostfix)),
            monitor: monitor);

        PatchIf(harmony,
            AccessTools.DeclaredMethod(typeof(Game1), nameof(Game1.pressActionButton),
                [typeof(KeyboardState), typeof(MouseState), typeof(GamePadState)]),
            prefix: new HarmonyMethod(typeof(CarryableChestPatches), nameof(Game1PressActionButtonPrefix)),
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

    public static bool RunWithPlacementAllowed(Func<bool> action)
    {
        try
        {
            placementAllowed = true;
            return action();
        }
        finally
        {
            placementAllowed = false;
        }
    }

    private static bool ChestAddItemPrefix(Item item, ref Item __result)
    {
        if (!ChestMetadata.IsCarriedChest(item))
            return true;

        __result = item;
        return false;
    }

    private static bool Game1PressUseToolButtonPrefix(ref bool __result)
    {
        placementAllowed = false;

        if (coordinator is null || !Context.IsWorldReady)
            return true;

        if (Game1.player.CurrentItem is Chest chest && ChestMetadata.IsCarriedChest(chest))
        {
            if (Context.IsMultiplayer && !Context.IsMainPlayer)
            {
                Game1.showRedMessage(I18n.MultiplayerHostPlaceOnly);
                __result = true;
                return false;
            }

            __result = TryPlaceCarriedChest(chest, coordinator);
            return false;
        }

        if (!Context.IsPlayerFree)
            return true;

        if (Game1.player.CurrentItem is not null)
            return true;

        if (Game1.player.isMoving())
            return true;

        if (Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            if (GetPickupTiles().Any(tile => Game1.currentLocation?.Objects.ContainsKey(tile) == true))
            {
                Game1.showRedMessage(I18n.MultiplayerHostPickupOnly);
                __result = true;
                return false;
            }

            return true;
        }

        foreach (Vector2 tile in GetPickupTiles())
        {
            if (!coordinator.TryPickUp(Game1.currentLocation, tile, Game1.player))
                continue;

            __result = true;
            return false;
        }

        return true;
    }

    private static bool TryPlaceCarriedChest(Chest chest, CarryableChestCoordinator activeCoordinator)
    {
        GameLocation location = Game1.currentLocation;
        bool usePlayerGrabTile = StardewModdingAPI.Constants.TargetPlatform == GamePlatform.Android;
        Vector2 placementGrabTile = usePlayerGrabTile
            ? Game1.player.GetGrabTile()
            : Game1.GetPlacementGrabTile();
        Vector2 placementPosition;

        try
        {
            Game1.isCheckingNonMousePlacement = usePlayerGrabTile || !Game1.IsPerformingMousePlacement();
            placementPosition = Utility.GetNearbyValidPlacementPosition(
                Game1.player,
                location,
                chest,
                (int)placementGrabTile.X * Game1.tileSize,
                (int)placementGrabTile.Y * Game1.tileSize);

            if (!Utility.playerCanPlaceItemHere(location, chest, (int)placementPosition.X, (int)placementPosition.Y, Game1.player, show_error: true))
                return false;
        }
        finally
        {
            Game1.isCheckingNonMousePlacement = false;
        }

        Vector2 tile = new((int)(placementPosition.X / Game1.tileSize), (int)(placementPosition.Y / Game1.tileSize));
        if (location.Objects.ContainsKey(tile))
            return false;

        activeCoordinator.CompletePlacement(chest, location, tile, Game1.player);

        return true;
    }

    private static IEnumerable<Vector2> GetPickupTiles()
    {
        HashSet<Vector2> seen = [];

        Vector2 position = Game1.wasMouseVisibleThisFrame
            ? new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y)
            : Game1.player.GetToolLocation();

        foreach (Vector2 tile in new[]
                 {
                     new Vector2((int)(position.X / Game1.tileSize), (int)(position.Y / Game1.tileSize)),
                     Game1.player.GetToolLocation() / Game1.tileSize
                 })
        {
            Vector2 normalized = new((int)tile.X, (int)tile.Y);
            if (seen.Add(normalized))
                yield return normalized;
        }
    }

    private static bool Game1PressActionButtonPrefix(ref bool __result)
    {
        if (coordinator is null || !Context.IsWorldReady || !Context.IsPlayerFree)
            return true;

        if (!ChestMetadata.IsCarriedChest(Game1.player.CurrentItem))
            return true;

        _ = coordinator.TryOpenHeldChest(Game1.player);
        __result = false;
        return false;
    }

    private static void Game1PressUseToolButtonPostfix()
    {
        placementAllowed = false;
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
        if (!ChestMetadata.IsCarriedChest(__instance))
            return true;

        if (Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            Game1.showRedMessage(I18n.MultiplayerHostPlaceOnly);
            __result = false;
            return false;
        }

        if (placementAllowed)
            return true;

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

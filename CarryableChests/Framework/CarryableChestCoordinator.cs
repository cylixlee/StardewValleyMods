using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal sealed class CarryableChestCoordinator
{
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly ChestBackupStore backups;
    private readonly WorldChestPlacer placer;

    public CarryableChestCoordinator(IMonitor monitor, ModConfig config, ChestBackupStore backups, WorldChestPlacer placer)
    {
        this.monitor = monitor;
        this.config = config;
        this.backups = backups;
        this.placer = placer;
    }

    public bool TryPickUp(GameLocation? location, Vector2 tile, Farmer who)
    {
        if (location is null)
            return false;

        if (Math.Abs(who.Tile.X - tile.X) > config.MaximumReach || Math.Abs(who.Tile.Y - tile.Y) > config.MaximumReach)
            return false;

        if (!location.Objects.TryGetValue(tile, out SObject? obj) || obj is not Chest chest)
            return false;

        if (!CanCarry(chest, out string reason))
        {
            Game1.showRedMessage(reason);
            return true;
        }

        if (!who.couldInventoryAcceptThisItem(chest))
        {
            Game1.showRedMessage(I18n.CannotCarryInventoryFull);
            return true;
        }

        string carryId = ChestMetadata.EnsureCarryId(chest);
        ChestMetadata.MarkCarried(chest, carryId, location, tile, who);
        backups.Ensure(chest, carryId, location, tile, who, allowOverwrite: true);

        if (!location.Objects.Remove(tile))
        {
            backups.Remove(carryId);
            ChestMetadata.Clear(chest);
            Game1.showRedMessage(I18n.CannotCarryRemoveWorldFailed);
            return true;
        }

        if (!who.addItemToInventoryBool(chest, true))
        {
            location.Objects[tile] = chest;
            backups.Remove(carryId);
            ChestMetadata.Clear(chest);
            Game1.showRedMessage(I18n.CannotCarryAddInventoryFailed);
            return true;
        }

        SelectCarriedChest(who, chest);
        Game1.playSound("pickUpItem");
        monitor.Log($"Picked up chest {carryId} from {location.Name} at {tile}.", LogLevel.Trace);
        return true;
    }

    public void CompletePlacement(Chest chest, GameLocation location, Vector2 tile, Farmer who)
    {
        if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
            return;

        backups.Ensure(chest, carryId, location, tile, who, allowOverwrite: false);
        bool verified = backups.Matches(carryId, chest);

        placer.PlaceCarriedChest(chest, carryId, location, tile, who, clearBackup: verified);

        if (!verified)
        {
            monitor.Log($"Placed chest {carryId}, but its backup fingerprint does not match. Keeping backup for recovery.", LogLevel.Warn);
            Game1.showRedMessage(I18n.PlacementBackupKept);
            return;
        }

        monitor.Log($"Placed chest {carryId} at {location.Name} {tile}.", LogLevel.Trace);
    }

    public bool TryOpenHeldChest(Farmer who)
    {
        if (!config.OpenHeldChest || who.CurrentItem is not Chest chest || !ChestMetadata.IsCarriedChest(chest))
            return false;

        chest.ShowMenu();
        return true;
    }

    private static void SelectCarriedChest(Farmer who, Chest chest)
    {
        int index = who.Items.IndexOf(chest);
        if (index >= 0)
            who.CurrentToolIndex = index;
    }

    public void SyncBackupForClosedMenu(Chest chest, Farmer who)
    {
        if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
            return;

        string locationName = chest.modData.TryGetValue(Constants.OriginLocationKey, out string originLocation)
            ? originLocation
            : who.currentLocation?.Name ?? string.Empty;

        Vector2 originTile = ChestMetadata.GetOriginTile(chest) ?? who.Tile;
        backups.Upsert(chest, carryId, Game1.getLocationFromName(locationName) ?? who.currentLocation, originTile, who);
    }

    private static bool CanCarry(Chest chest, out string reason)
    {
        if (!chest.playerChest.Value)
        {
            reason = I18n.CannotCarryNonPlayerChest;
            return false;
        }

        if (chest.GetMutex().IsLocked())
        {
            reason = I18n.CannotCarryOpenChest;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(chest.GlobalInventoryId))
        {
            reason = I18n.CannotCarryGlobalInventoryChest;
            return false;
        }

        reason = string.Empty;
        return true;
    }
}

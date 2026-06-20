using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal sealed class RecoveryService
{
    private readonly IMonitor monitor;
    private readonly ChestBackupStore backups;
    private readonly WorldChestPlacer placer;

    public RecoveryService(IMonitor monitor, ChestBackupStore backups, WorldChestPlacer placer)
    {
        this.monitor = monitor;
        this.backups = backups;
        this.placer = placer;
    }

    public void SyncBackupsFromCarriedChests()
    {
        if (!Context.IsWorldReady)
            return;

        foreach (Chest chest in GetCarriedChests(Game1.player))
        {
            if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
                continue;

            string locationName = chest.modData.TryGetValue(Constants.OriginLocationKey, out string originLocation)
                ? originLocation
                : Game1.player.currentLocation?.Name ?? string.Empty;

            Vector2 originTile = ChestMetadata.GetOriginTile(chest) ?? Game1.player.Tile;
            backups.Ensure(chest, carryId, Game1.getLocationFromName(locationName) ?? Game1.player.currentLocation, originTile, Game1.player, allowOverwrite: false);
        }
    }

    public void ReturnCarriedChestsBeforeSaving()
    {
        if (!Context.IsWorldReady)
            return;

        foreach (Chest chest in GetCarriedChests(Game1.player).ToList())
        {
            if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
                continue;

            backups.Ensure(chest, carryId, Game1.player.currentLocation, Game1.player.Tile, Game1.player, allowOverwrite: false);

            if (!backups.Matches(carryId, chest))
            {
                monitor.Log($"Chest {carryId} changed while carried. Keeping it in inventory and preserving its older backup.", LogLevel.Warn);
                Game1.showRedMessage(I18n.RecoveryCarriedChanged);
                continue;
            }

            if (!TileFinder.TryFindReturnTile(chest, out GameLocation? location, out Vector2 tile) || location is null)
            {
                monitor.Log($"Could not find a safe return tile for carried chest {carryId}; backup remains.", LogLevel.Warn);
                Game1.showRedMessage(I18n.RecoveryNoSafeTile);
                continue;
            }

            placer.PlaceCarriedChest(chest, carryId, location, tile, Game1.player, clearBackup: true);
            monitor.Log($"Returned carried chest {carryId} before saving at {location.Name} {tile}.", LogLevel.Info);
        }
    }

    public void RecoverOrphans()
    {
        if (!Context.IsWorldReady)
            return;

        SyncBackupsFromCarriedChests();

        HashSet<string> activeIds = [];

        foreach (Chest chest in GetCarriedChests(Game1.player))
        {
            if (ChestMetadata.TryGetCarryId(chest, out string carryId))
                activeIds.Add(carryId);
        }

        foreach ((GameLocation location, Vector2 tile, Chest chest) in GetWorldCarryTaggedChests())
        {
            if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
                continue;

            activeIds.Add(carryId);
            if (!backups.Matches(carryId, chest))
            {
                monitor.Log($"World chest {carryId} at {location.Name} {tile} does not match its backup. Keeping both for recovery.", LogLevel.Warn);
                continue;
            }

            backups.Remove(carryId);
            ChestMetadata.Clear(chest);
            monitor.Log($"Cleaned carry metadata from placed chest {carryId} at {location.Name} {tile}.", LogLevel.Trace);
        }

        foreach (Chest backup in backups.All())
        {
            if (!ChestMetadata.TryGetCarryId(backup, out string carryId) || activeIds.Contains(carryId))
                continue;

            if (TryRestoreBackupToOrigin(backup, carryId) || TryRestoreBackupToInventory(backup, carryId))
                continue;

            monitor.Log($"Could not restore orphaned carried chest {carryId}; backup remains in {Constants.BackupInventoryId}.", LogLevel.Warn);
            Game1.showRedMessage(I18n.RecoveryOrphanFailed);
        }
    }

    private bool TryRestoreBackupToOrigin(Chest backup, string carryId)
    {
        if (!backup.modData.TryGetValue(Constants.OriginLocationKey, out string locationName))
            return false;

        Vector2? tile = ChestMetadata.GetOriginTile(backup);
        GameLocation? location = Game1.getLocationFromName(locationName);
        if (location is null || tile is null || !TileFinder.IsTileAvailable(location, tile.Value))
            return false;

        Chest restored = ChestMetadata.CloneChest(backup);
        ChestMetadata.Clear(restored);
        location.Objects[tile.Value] = restored;
        backups.Remove(carryId);
        monitor.Log($"Restored orphaned chest {carryId} to {location.Name} {tile.Value}.", LogLevel.Warn);
        Game1.showGlobalMessage(I18n.RecoveryRestoredOrigin);
        return true;
    }

    private bool TryRestoreBackupToInventory(Chest backup, string carryId)
    {
        Chest restored = ChestMetadata.CloneChest(backup);
        ChestMetadata.MarkCarried(restored, carryId, Game1.player.currentLocation, Game1.player.Tile, Game1.player);

        if (!Game1.player.addItemToInventoryBool(restored, true))
            return false;

        backups.Upsert(restored, carryId, Game1.player.currentLocation, Game1.player.Tile, Game1.player);
        monitor.Log($"Restored orphaned chest {carryId} to the player's inventory.", LogLevel.Warn);
        Game1.showGlobalMessage(I18n.RecoveryRestoredInventory);
        return true;
    }

    private static IEnumerable<Chest> GetCarriedChests(Farmer farmer)
    {
        return farmer.Items.OfType<Chest>().Where(ChestMetadata.IsCarriedChest);
    }

    private static IEnumerable<(GameLocation Location, Vector2 Tile, Chest Chest)> GetWorldCarryTaggedChests()
    {
        foreach (GameLocation location in Game1.locations)
        {
            foreach (var pair in location.Objects.Pairs)
            {
                if (pair.Value is Chest chest && chest.modData.ContainsKey(Constants.CarryIdKey))
                    yield return (location, pair.Key, chest);
            }
        }
    }
}

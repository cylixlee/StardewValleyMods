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
    private readonly CarryStateStore stateStore;
    private CarryableChestNetworkService? network;

    public CarryableChestCoordinator(IMonitor monitor, ModConfig config, ChestBackupStore backups, WorldChestPlacer placer, CarryStateStore stateStore)
    {
        this.monitor = monitor;
        this.config = config;
        this.backups = backups;
        this.placer = placer;
        this.stateStore = stateStore;
    }

    public void SetNetwork(CarryableChestNetworkService networkService)
    {
        network = networkService;
    }

    public bool TryPickUp(GameLocation? location, Vector2 tile, Farmer who)
    {
        if (Context.IsMultiplayer && !Context.IsMainPlayer)
            return TryRequestPickUp(location, tile, who);

        CarryActionResult result = TryPickUpLocal(location, tile, who, showMessages: true);
        return result.Handled;
    }

    internal CarryActionResult TryPickUpLocal(GameLocation? location, Vector2 tile, Farmer who, bool showMessages, bool cloneForInventory = false)
    {
        if (location is null)
            return CarryActionResult.Ignored;

        if (Math.Abs(who.Tile.X - tile.X) > config.MaximumReach || Math.Abs(who.Tile.Y - tile.Y) > config.MaximumReach)
            return CarryActionResult.Ignored;

        if (!location.Objects.TryGetValue(tile, out SObject? obj) || obj is not Chest chest)
            return CarryActionResult.Ignored;

        if (!CanCarry(chest, out string reason))
        {
            if (showMessages)
                Game1.showRedMessage(reason);
            return CarryActionResult.Failed(reason);
        }

        if (!who.couldInventoryAcceptThisItem(chest))
        {
            if (showMessages)
                Game1.showRedMessage(I18n.CannotCarryInventoryFull);
            return CarryActionResult.Failed(I18n.CannotCarryInventoryFull);
        }

        string carryId = ChestMetadata.EnsureCarryId(chest);
        ChestMetadata.MarkCarried(chest, carryId, location, tile, who);
        backups.Upsert(chest, carryId, location, tile, who);
        stateStore.Upsert(chest, carryId, location, tile, who, Constants.StateCarried);
        Chest inventoryChest = cloneForInventory ? ChestMetadata.CloneChest(chest) : chest;
        if (cloneForInventory)
            ChestMetadata.MarkCarried(inventoryChest, carryId, location, tile, who);

        if (!location.Objects.Remove(tile))
        {
            backups.Remove(carryId);
            stateStore.Remove(carryId);
            ChestMetadata.Clear(chest);
            if (showMessages)
                Game1.showRedMessage(I18n.CannotCarryRemoveWorldFailed);
            return CarryActionResult.Failed(I18n.CannotCarryRemoveWorldFailed);
        }

        if (!who.addItemToInventoryBool(inventoryChest, true))
        {
            location.Objects[tile] = chest;
            backups.Remove(carryId);
            stateStore.Remove(carryId);
            ChestMetadata.Clear(chest);
            if (showMessages)
                Game1.showRedMessage(I18n.CannotCarryAddInventoryFailed);
            return CarryActionResult.Failed(I18n.CannotCarryAddInventoryFailed);
        }

        SelectCarriedChest(who, inventoryChest);
        if (who == Game1.player)
            Game1.playSound("pickUpItem");
        monitor.Log($"Picked up chest {carryId} from {location.Name} at {tile}.", LogLevel.Trace);
        return CarryActionResult.Succeeded;
    }

    internal CarryActionResult PrepareRemotePickup(GameLocation? location, Vector2 tile, Farmer who, out string carryId)
    {
        carryId = string.Empty;

        if (location is null)
            return CarryActionResult.Ignored;

        if (Math.Abs(who.Tile.X - tile.X) > config.MaximumReach || Math.Abs(who.Tile.Y - tile.Y) > config.MaximumReach)
            return CarryActionResult.Ignored;

        if (!location.Objects.TryGetValue(tile, out SObject? obj) || obj is not Chest chest)
            return CarryActionResult.Ignored;

        if (!CanCarry(chest, out string reason))
            return CarryActionResult.Failed(reason);

        if (!who.couldInventoryAcceptThisItem(chest))
            return CarryActionResult.Failed(I18n.CannotCarryInventoryFull);

        carryId = ChestMetadata.EnsureCarryId(chest);
        ChestMetadata.MarkCarried(chest, carryId, location, tile, who);
        backups.Upsert(chest, carryId, location, tile, who);
        stateStore.Upsert(chest, carryId, location, tile, who, Constants.StateCarried);
        monitor.Log($"Prepared remote pickup {carryId} for player {who.UniqueMultiplayerID} from {location.NameOrUniqueName} at {tile}.", LogLevel.Info);
        return CarryActionResult.Succeeded;
    }

    internal bool CommitRemotePickup(GameLocation? location, Vector2 tile, string carryId, Farmer who)
    {
        if (location is null)
            return false;

        if (!location.Objects.TryGetValue(tile, out SObject? obj) || obj is not Chest chest)
            return false;

        if (!ChestMetadata.TryGetCarryId(chest, out string worldCarryId) || worldCarryId != carryId)
            return false;

        bool removed = location.Objects.Remove(tile);
        if (removed)
            monitor.Log($"Committed remote pickup {carryId} for player {who.UniqueMultiplayerID}; removed world chest from {location.NameOrUniqueName} at {tile}.", LogLevel.Info);

        return removed;
    }

    internal void CancelRemotePickup(GameLocation? location, Vector2 tile, string carryId)
    {
        if (location?.Objects.TryGetValue(tile, out SObject? obj) == true && obj is Chest chest)
        {
            if (ChestMetadata.TryGetCarryId(chest, out string worldCarryId) && worldCarryId == carryId)
                ChestMetadata.Clear(chest);
        }

        backups.Remove(carryId);
        stateStore.Remove(carryId);
        monitor.Log($"Cancelled remote pickup {carryId}; durable vault entry cleared and world chest left in place.", LogLevel.Warn);
    }

    private bool TryRequestPickUp(GameLocation? location, Vector2 tile, Farmer who)
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

        if (network is null)
        {
            Game1.showRedMessage(I18n.MultiplayerHostPickupOnly);
            return true;
        }

        network.RequestPickup(location, tile);
        return true;
    }

    public void CompletePlacement(Chest chest, GameLocation location, Vector2 tile, Farmer who)
    {
        if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
            return;

        backups.Upsert(chest, carryId, location, tile, who);
        placer.PlaceCarriedChest(chest, carryId, location, tile, who, clearBackup: true);
        stateStore.Remove(carryId);

        monitor.Log($"Placed chest {carryId} at {location.Name} {tile}.", LogLevel.Trace);
    }

    internal void CompleteRemotePlacement(Chest carriedChest, GameLocation location, Vector2 tile, Farmer who)
    {
        if (!ChestMetadata.TryGetCarryId(carriedChest, out string carryId))
            return;

        backups.Upsert(carriedChest, carryId, location, tile, who);
        stateStore.Upsert(carriedChest, carryId, location, tile, who, Constants.StatePendingPlace);

        Chest worldChest = ChestMetadata.CloneChest(carriedChest);
        placer.PlaceCarriedChest(worldChest, carryId, location, tile, who, clearBackup: false);
        monitor.Log($"Committed remote placement {carryId} for player {who.UniqueMultiplayerID} at {location.NameOrUniqueName} {tile}; waiting for client inventory removal.", LogLevel.Info);
    }

    internal void FinalizeRemotePlacement(string carryId)
    {
        backups.Remove(carryId);
        stateStore.Remove(carryId);
        monitor.Log($"Finalized remote placement {carryId}; durable vault entry cleared.", LogLevel.Info);
    }

    public bool TryPlaceOrRequest(Chest chest, GameLocation location, Vector2 tile, Farmer who)
    {
        if (Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            if (network is null)
            {
                Game1.showRedMessage(I18n.MultiplayerHostPlaceOnly);
                return true;
            }

            network.RequestPlace(chest, location, tile);
            return true;
        }

        CompletePlacement(chest, location, tile, who);
        return true;
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

        if (Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            network?.NotifyChestUpdated(carryId);
            return;
        }

        string locationName = chest.modData.TryGetValue(Constants.OriginLocationKey, out string originLocation)
            ? originLocation
            : who.currentLocation?.Name ?? string.Empty;

        Vector2 originTile = ChestMetadata.GetOriginTile(chest) ?? who.Tile;
        GameLocation? location = Game1.getLocationFromName(locationName) ?? who.currentLocation;
        backups.Upsert(chest, carryId, location, originTile, who);
        stateStore.Upsert(chest, carryId, location, originTile, who, Constants.StateCarried);
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

        if (chest.modData.ContainsKey(Constants.CarryIdKey))
        {
            reason = I18n.CannotCarryInTransition;
            return false;
        }

        reason = string.Empty;
        return true;
    }

    internal Chest? FindCarriedChest(Farmer who, string carryId)
    {
        return who.Items.OfType<Chest>().FirstOrDefault(chest =>
            ChestMetadata.IsCarriedChest(chest)
            && ChestMetadata.TryGetCarryId(chest, out string id)
            && id == carryId);
    }

    internal Chest? FindVaultChest(string carryId)
    {
        return backups.Find(carryId);
    }

    internal readonly record struct CarryActionResult(bool Handled, bool Success, string? ErrorMessage)
    {
        public static CarryActionResult Ignored => new(false, false, null);
        public static CarryActionResult Succeeded => new(true, true, null);
        public static CarryActionResult Failed(string errorMessage) => new(true, false, errorMessage);
    }
}

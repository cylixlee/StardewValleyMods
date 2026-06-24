using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal sealed class ChestReconciliationService
{
    private readonly IMonitor monitor;
    private readonly ChestBackupStore vault;
    private readonly CarryStateStore stateStore;

    public ChestReconciliationService(IMonitor monitor, ChestBackupStore vault, CarryStateStore stateStore)
    {
        this.monitor = monitor;
        this.vault = vault;
        this.stateStore = stateStore;
    }

    public void Reconcile(bool repairFromVault = false)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        int carried = SyncVaultFromCarriedChests();
        int placed = CleanPlacedWorldMetadata();
        int rehydrated = RehydrateMissingCarriedChests(repairFromVault);
        stateStore.SaveFromVault(vault);

        monitor.Log($"Reconciled carry state. Synced {carried} carried chest(s), cleaned {placed} placed chest(s), rehydrated {rehydrated} carried chest(s).", LogLevel.Info);
    }

    public int SyncVaultFromCarriedChests()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return 0;

        int synced = 0;
        foreach (Farmer farmer in GetKnownFarmers())
        {
            foreach (Chest chest in GetCarriedChests(farmer))
            {
                if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
                    continue;

                GameLocation? originLocation = GetOriginLocation(chest) ?? farmer.currentLocation;
                Vector2 originTile = ChestMetadata.GetOriginTile(chest) ?? farmer.Tile;
                vault.Upsert(chest, carryId, originLocation, originTile, farmer);
                stateStore.Upsert(chest, carryId, originLocation, originTile, farmer, Constants.StateCarried);
                synced++;
            }
        }

        return synced;
    }

    public void SyncBackupForClosedMenu(Chest chest, Farmer who)
    {
        if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
            return;

        GameLocation? originLocation = GetOriginLocation(chest) ?? who.currentLocation;
        Vector2 originTile = ChestMetadata.GetOriginTile(chest) ?? who.Tile;
        vault.Upsert(chest, carryId, originLocation, originTile, who);
        stateStore.Upsert(chest, carryId, originLocation, originTile, who, Constants.StateCarried);
    }

    private int CleanPlacedWorldMetadata()
    {
        int cleaned = 0;
        foreach ((GameLocation location, Vector2 tile, Chest chest) in GetWorldCarryTaggedChests())
        {
            if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
                continue;

            if (vault.Matches(carryId, chest))
            {
                vault.Remove(carryId);
                stateStore.Remove(carryId);
            }

            ChestMetadata.Clear(chest);
            cleaned++;
            monitor.Log($"Cleaned stale carry metadata from placed chest {carryId} at {location.NameOrUniqueName} {tile}.", LogLevel.Trace);
        }

        return cleaned;
    }

    private int RehydrateMissingCarriedChests(bool repairFromVault)
    {
        HashSet<string> activeCarryIds = [];
        foreach (Farmer farmer in GetKnownFarmers())
        {
            foreach (Chest chest in GetCarriedChests(farmer))
            {
                if (ChestMetadata.TryGetCarryId(chest, out string carryId))
                    activeCarryIds.Add(carryId);
            }
        }

        int rehydrated = 0;
        foreach (Chest backup in vault.All())
        {
            if (!ChestMetadata.TryGetCarryId(backup, out string carryId) || activeCarryIds.Contains(carryId))
                continue;

            if (!repairFromVault && !stateStore.IsKnownCarried(carryId))
                continue;

            Farmer? owner = TryGetOwner(backup);
            if (owner is null || !owner.isActive())
                continue;

            Chest restored = ChestMetadata.CloneChest(backup);
            ChestMetadata.MarkCarried(restored, carryId, owner.currentLocation, owner.Tile, owner);
            if (!owner.addItemToInventoryBool(restored, true))
            {
                monitor.Log($"Could not rehydrate carried chest {carryId} for player {owner.UniqueMultiplayerID}; their inventory is full. The durable vault copy remains.", LogLevel.Warn);
                continue;
            }

            if (owner == Game1.player)
            {
                int index = owner.Items.IndexOf(restored);
                if (index >= 0)
                    owner.CurrentToolIndex = index;
            }

            stateStore.Upsert(restored, carryId, owner.currentLocation, owner.Tile, owner, Constants.StateCarried);
            activeCarryIds.Add(carryId);
            rehydrated++;
            monitor.Log($"Rehydrated carried chest {carryId} for player {owner.UniqueMultiplayerID} from the durable vault.", LogLevel.Info);
        }

        return rehydrated;
    }

    private static IEnumerable<Chest> GetCarriedChests(Farmer farmer)
    {
        return farmer.Items.OfType<Chest>().Where(ChestMetadata.IsCarriedChest);
    }

    private static IEnumerable<Farmer> GetKnownFarmers()
    {
        if (Game1.player is not null)
            yield return Game1.player;

        foreach (Farmer farmer in Game1.otherFarmers.Values)
        {
            if (farmer is not null)
                yield return farmer;
        }
    }

    private static GameLocation? GetOriginLocation(Chest chest)
    {
        if (!chest.modData.TryGetValue(Constants.OriginLocationKey, out string locationName) || string.IsNullOrWhiteSpace(locationName))
            return null;

        return Game1.getLocationFromName(locationName);
    }

    private static Farmer? TryGetOwner(Chest chest)
    {
        if (!chest.modData.TryGetValue(Constants.OwnerKey, out string ownerText) || !long.TryParse(ownerText, out long ownerId))
            return null;

        return Game1.GetPlayer(ownerId);
    }

    private static IEnumerable<(GameLocation Location, Vector2 Tile, Chest Chest)> GetWorldCarryTaggedChests()
    {
        List<(GameLocation Location, Vector2 Tile, Chest Chest)> results = [];
        Utility.ForEachLocation(location =>
        {
            foreach (KeyValuePair<Vector2, SObject> pair in location.Objects.Pairs)
            {
                if (pair.Value is Chest chest && chest.modData.ContainsKey(Constants.CarryIdKey))
                    results.Add((location, pair.Key, chest));
            }

            return true;
        });

        return results;
    }
}

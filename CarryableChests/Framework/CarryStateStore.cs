using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal sealed class CarryStateStore
{
    private const int CurrentSchemaVersion = 1;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private CarryStateData data = new();

    public CarryStateStore(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
    }

    public void Load(ChestBackupStore vault)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        data = helper.Data.ReadSaveData<CarryStateData>(Constants.SaveDataKey) ?? new CarryStateData();
        if (data.SchemaVersion < CurrentSchemaVersion)
            data.SchemaVersion = CurrentSchemaVersion;

        int migrated = MergeVault(vault);
        if (migrated > 0)
        {
            Save();
            monitor.Log($"Migrated {migrated} legacy carried chest record(s) into SMAPI save data.", LogLevel.Info);
        }
    }

    public void ClearCachedState()
    {
        data = new CarryStateData();
    }

    public void Upsert(Chest chest, string carryId, GameLocation? location, Vector2 tile, Farmer owner, string state)
    {
        if (!Context.IsMainPlayer)
            return;

        data.Carries[carryId] = CreateRecord(chest, carryId, location, tile, owner, state);
    }

    public void Remove(string carryId)
    {
        if (!Context.IsMainPlayer)
            return;

        data.Carries.Remove(carryId);
    }

    public void MarkOwnerOffline(long playerId)
    {
        if (!Context.IsMainPlayer)
            return;

        foreach (CarryStateRecord record in data.Carries.Values)
        {
            if (record.OwnerId == playerId)
                record.OwnerOnline = false;
        }
    }

    public bool IsKnownCarried(string carryId)
    {
        return data.Carries.TryGetValue(carryId, out CarryStateRecord? record)
            && record.State == Constants.StateCarried;
    }

    public void SaveFromVault(ChestBackupStore vault)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        MergeVault(vault);
        Save();
    }

    private int MergeVault(ChestBackupStore vault)
    {
        int added = 0;
        foreach (Chest chest in vault.All())
        {
            if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
                continue;

            long ownerId = GetOwnerId(chest) ?? Game1.player.UniqueMultiplayerID;
            Farmer? owner = Game1.GetPlayer(ownerId);
            string locationName = chest.modData.TryGetValue(Constants.OriginLocationKey, out string originLocation)
                ? originLocation
                : owner?.currentLocation?.NameOrUniqueName ?? string.Empty;
            GameLocation? location = string.IsNullOrWhiteSpace(locationName) ? null : Game1.getLocationFromName(locationName);
            Vector2 tile = ChestMetadata.GetOriginTile(chest) ?? owner?.Tile ?? Game1.player.Tile;
            string state = data.Carries.TryGetValue(carryId, out CarryStateRecord? existing)
                ? existing.State
                : Constants.StateBackup;

            if (!data.Carries.ContainsKey(carryId))
                added++;

            data.Carries[carryId] = CreateRecord(chest, carryId, location, tile, ownerId, owner?.isActive() == true, state);
        }

        return added;
    }

    private void Save()
    {
        helper.Data.WriteSaveData(Constants.SaveDataKey, data);
    }

    private static long? GetOwnerId(Chest chest)
    {
        if (!chest.modData.TryGetValue(Constants.OwnerKey, out string ownerText) || !long.TryParse(ownerText, out long ownerId))
            return null;

        return ownerId;
    }

    private static CarryStateRecord CreateRecord(Chest chest, string carryId, GameLocation? location, Vector2 tile, Farmer owner, string state)
    {
        return new CarryStateRecord
        {
            CarryId = carryId,
            OwnerId = owner.UniqueMultiplayerID,
            OwnerOnline = owner.isActive(),
            State = state,
            LocationName = location?.NameOrUniqueName ?? string.Empty,
            TileX = (int)tile.X,
            TileY = (int)tile.Y,
            Fingerprint = ChestMetadata.GetFingerprint(chest),
            QualifiedItemId = chest.QualifiedItemId,
            SpecialChestType = chest.SpecialChestType.ToString(),
            GlobalInventoryId = chest.GlobalInventoryId
        };
    }

    private static CarryStateRecord CreateRecord(Chest chest, string carryId, GameLocation? location, Vector2 tile, long ownerId, bool ownerOnline, string state)
    {
        return new CarryStateRecord
        {
            CarryId = carryId,
            OwnerId = ownerId,
            OwnerOnline = ownerOnline,
            State = state,
            LocationName = location?.NameOrUniqueName ?? string.Empty,
            TileX = (int)tile.X,
            TileY = (int)tile.Y,
            Fingerprint = ChestMetadata.GetFingerprint(chest),
            QualifiedItemId = chest.QualifiedItemId,
            SpecialChestType = chest.SpecialChestType.ToString(),
            GlobalInventoryId = chest.GlobalInventoryId
        };
    }
}

internal sealed class CarryStateData
{
    public int SchemaVersion { get; set; } = 1;
    public Dictionary<string, CarryStateRecord> Carries { get; set; } = [];
}

internal sealed class CarryStateRecord
{
    public string CarryId { get; set; } = string.Empty;
    public long OwnerId { get; set; }
    public bool OwnerOnline { get; set; }
    public string State { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public string QualifiedItemId { get; set; } = string.Empty;
    public string SpecialChestType { get; set; } = string.Empty;
    public string? GlobalInventoryId { get; set; }
}

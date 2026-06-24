using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal sealed class ChestBackupStore
{
    private Inventory? backups;

    public ChestBackupStore(IMonitor monitor)
    {
        _ = monitor;
    }

    private Inventory Backups => backups ??= Game1.player.team.GetOrCreateGlobalInventory(Constants.BackupInventoryId);

    public IEnumerable<Chest> All()
    {
        return Backups.OfType<Chest>().ToList();
    }

    public void ClearCachedState()
    {
        backups = null;
    }

    public Chest? Find(string carryId)
    {
        return Backups.OfType<Chest>().FirstOrDefault(chest =>
            chest.modData.TryGetValue(Constants.CarryIdKey, out string id) && id == carryId);
    }

    public bool Matches(string carryId, Chest chest)
    {
        Chest? backup = Find(carryId);
        if (backup is null)
            return false;

        return backup.modData.TryGetValue(Constants.FingerprintKey, out string fingerprint)
            && fingerprint == ChestMetadata.GetFingerprint(chest);
    }

    public void Remove(string carryId)
    {
        Chest? backup = Find(carryId);
        if (backup is not null)
            Backups.Remove(backup);
    }

    public void Upsert(Chest source, string carryId, GameLocation? location, Vector2 tile, Farmer who)
    {
        Remove(carryId);

        Chest backup = ChestMetadata.CloneChest(source);
        backup.modData[Constants.CarryIdKey] = carryId;
        backup.modData[Constants.StateKey] = Constants.StateBackup;
        backup.modData[Constants.FingerprintKey] = ChestMetadata.GetFingerprint(source);
        backup.modData[Constants.OriginLocationKey] = location?.NameOrUniqueName ?? string.Empty;
        backup.modData[Constants.OriginTileXKey] = ((int)tile.X).ToString();
        backup.modData[Constants.OriginTileYKey] = ((int)tile.Y).ToString();
        backup.modData[Constants.OwnerKey] = who.UniqueMultiplayerID.ToString();
        Backups.Add(backup);
    }
}

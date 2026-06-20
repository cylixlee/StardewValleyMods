using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal sealed class WorldChestPlacer
{
    private readonly ChestBackupStore backups;

    public WorldChestPlacer(ChestBackupStore backups)
    {
        this.backups = backups;
    }

    public void PlaceCarriedChest(Chest chest, string carryId, GameLocation location, Vector2 tile, Farmer who, bool clearBackup)
    {
        location.Objects[tile] = chest;
        chest.localKickStartTile = null;
        chest.kickProgress = -1f;
        chest.shakeTimer = 50;

        if (who.Items.Contains(chest))
            who.removeItemFromInventory(chest);

        who.showNotCarrying();

        if (clearBackup)
            backups.Remove(carryId);

        ChestMetadata.Clear(chest);
    }
}

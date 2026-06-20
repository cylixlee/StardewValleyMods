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
        chest.setHealth(10);
        chest.Location = location;
        chest.TileLocation = tile;
        chest.owner.Value = who.UniqueMultiplayerID;
        chest.localKickStartTile = null;
        chest.kickProgress = -1f;
        chest.shakeTimer = 50;
        location.Objects[tile] = chest;
        location.playSound(chest.QualifiedItemId is "(BC)130" or "(BC)BigChest" or "(BC)108" or "(BC)248" or "(BC)256" or "(BC)275" ? "axe" : "hammer");

        if (who.Items.Contains(chest))
            who.removeItemFromInventory(chest);

        who.showNotCarrying();

        if (clearBackup)
            backups.Remove(carryId);

        ChestMetadata.Clear(chest);
    }
}

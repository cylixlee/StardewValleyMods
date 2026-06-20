using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal static class TileFinder
{
    public static bool TryFindReturnTile(Chest chest, out GameLocation? location, out Vector2 tile)
    {
        string locationName = chest.modData.TryGetValue(Constants.OriginLocationKey, out string originLocation)
            ? originLocation
            : string.Empty;

        Vector2? originTile = ChestMetadata.GetOriginTile(chest);
        location = string.IsNullOrWhiteSpace(locationName) ? null : Game1.getLocationFromName(locationName);
        if (location is not null && originTile is not null && IsTileAvailable(location, originTile.Value))
        {
            tile = originTile.Value;
            return true;
        }

        location = Game1.player.currentLocation;
        if (location is not null)
        {
            Vector2 playerTile = Game1.player.Tile;
            for (int radius = 1; radius <= 2; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        tile = playerTile + new Vector2(x, y);
                        if (IsTileAvailable(location, tile))
                            return true;
                    }
                }
            }
        }

        tile = Vector2.Zero;
        return false;
    }

    public static bool IsTileAvailable(GameLocation location, Vector2 tile)
    {
        return !location.Objects.ContainsKey(tile);
    }
}

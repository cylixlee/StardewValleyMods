using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal static class ChestMetadata
{
    public static bool IsCarriedChest(Item? item)
    {
        return item is Chest chest
            && chest.modData.TryGetValue(Constants.StateKey, out string state)
            && state == Constants.StateCarried
            && chest.modData.ContainsKey(Constants.CarryIdKey);
    }

    public static string EnsureCarryId(Chest chest)
    {
        if (TryGetCarryId(chest, out string carryId))
            return carryId;

        carryId = Constants.ModId + "." + Guid.NewGuid().ToString("N");
        chest.modData[Constants.CarryIdKey] = carryId;
        return carryId;
    }

    public static bool TryGetCarryId(Chest chest, out string carryId)
    {
        return chest.modData.TryGetValue(Constants.CarryIdKey, out carryId!) && !string.IsNullOrWhiteSpace(carryId);
    }

    public static void MarkCarried(Chest chest, string carryId, GameLocation? location, Vector2 tile, Farmer who)
    {
        chest.modData[Constants.CarryIdKey] = carryId;
        chest.modData[Constants.StateKey] = Constants.StateCarried;
        chest.modData[Constants.FingerprintKey] = GetFingerprint(chest);
        chest.modData[Constants.OriginLocationKey] = location?.Name ?? string.Empty;
        chest.modData[Constants.OriginTileXKey] = ((int)tile.X).ToString();
        chest.modData[Constants.OriginTileYKey] = ((int)tile.Y).ToString();
        chest.modData[Constants.OwnerKey] = who.UniqueMultiplayerID.ToString();
    }

    public static void Clear(Chest chest)
    {
        chest.modData.Remove(Constants.CarryIdKey);
        chest.modData.Remove(Constants.StateKey);
        chest.modData.Remove(Constants.FingerprintKey);
        chest.modData.Remove(Constants.OriginLocationKey);
        chest.modData.Remove(Constants.OriginTileXKey);
        chest.modData.Remove(Constants.OriginTileYKey);
        chest.modData.Remove(Constants.OwnerKey);
    }

    public static Vector2? GetOriginTile(Chest chest)
    {
        if (!chest.modData.TryGetValue(Constants.OriginTileXKey, out string xText)
            || !chest.modData.TryGetValue(Constants.OriginTileYKey, out string yText)
            || !int.TryParse(xText, out int x)
            || !int.TryParse(yText, out int y))
        {
            return null;
        }

        return new Vector2(x, y);
    }

    public static Chest CloneChest(Chest source)
    {
        Chest clone = new(source.playerChest.Value, source.ItemId)
        {
            GlobalInventoryId = null,
            fridge = { Value = source.fridge.Value },
            playerChoiceColor = { Value = source.playerChoiceColor.Value },
            SpecialChestType = source.SpecialChestType,
            Tint = source.Tint
        };

        clone.CopyFieldsFrom(source);
        clone.GlobalInventoryId = null;
        clone.Items.Clear();

        foreach (Item item in source.GetItemsForPlayer().Where(item => item is not null))
        {
            Item itemClone = item.getOne();
            itemClone.Stack = item.Stack;
            clone.Items.Add(itemClone);
        }

        return clone;
    }

    public static string GetFingerprint(Chest chest)
    {
        return string.Join("|", chest.GetItemsForPlayer()
            .Where(item => item is not null)
            .Select(GetItemFingerprint));
    }

    private static string GetItemFingerprint(Item item)
    {
        int quality = item is SObject obj ? obj.Quality : 0;
        string modData = string.Join(",", item.modData.Pairs
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));

        return $"{item.QualifiedItemId}:{item.Stack}:{quality}:{item.Name}:{modData}";
    }
}

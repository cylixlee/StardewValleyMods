using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Inventories;
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
        chest.modData[Constants.OriginLocationKey] = location?.NameOrUniqueName ?? string.Empty;
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
            fridge = { Value = source.fridge.Value },
            giftbox = { Value = source.giftbox.Value },
            giftboxIndex = { Value = source.giftboxIndex.Value },
            giftboxIsStarterGift = { Value = source.giftboxIsStarterGift.Value },
            dropContents = { Value = source.dropContents.Value },
            synchronized = { Value = source.synchronized.Value },
            playerChoiceColor = { Value = source.playerChoiceColor.Value },
            startingLidFrame = { Value = source.startingLidFrame.Value },
            lidFrameCount = { Value = source.lidFrameCount.Value },
            frameCounter = { Value = -1 },
            bigCraftableSpriteIndex = { Value = source.bigCraftableSpriteIndex.Value },
            SpecialChestType = source.SpecialChestType,
            GlobalInventoryId = source.GlobalInventoryId,
            Tint = source.Tint
        };

        clone.CopyFieldsFrom(source);
        clone.GlobalInventoryId = source.GlobalInventoryId;
        clone.SpecialChestType = source.SpecialChestType;
        clone.fridge.Value = source.fridge.Value;
        clone.giftbox.Value = source.giftbox.Value;
        clone.giftboxIndex.Value = source.giftboxIndex.Value;
        clone.giftboxIsStarterGift.Value = source.giftboxIsStarterGift.Value;
        clone.dropContents.Value = source.dropContents.Value;
        clone.synchronized.Value = source.synchronized.Value;
        clone.playerChoiceColor.Value = source.playerChoiceColor.Value;
        clone.startingLidFrame.Value = source.startingLidFrame.Value;
        clone.lidFrameCount.Value = source.lidFrameCount.Value;
        clone.frameCounter.Value = -1;
        clone.bigCraftableSpriteIndex.Value = source.bigCraftableSpriteIndex.Value;
        clone.Tint = source.Tint;

        clone.Items.Clear();
        foreach (Item item in CloneItems(source.Items))
        {
            clone.Items.Add(item);
        }

        clone.separateWalletItems.Clear();
        foreach (KeyValuePair<long, Inventory> wallet in source.separateWalletItems.Pairs)
        {
            Inventory copy = new();
            foreach (Item item in CloneItems(wallet.Value))
            {
                copy.Add(item);
            }

            clone.separateWalletItems[wallet.Key] = copy;
        }

        return clone;
    }

    public static string GetFingerprint(Chest chest)
    {
        string items = string.Join("|", chest.Items
            .Where(item => item is not null)
            .Select(GetItemFingerprint));

        string wallets = string.Join("|", chest.separateWalletItems.Pairs
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Key + ":" + string.Join(",", pair.Value
                .Where(item => item is not null)
                .Select(GetItemFingerprint))));

        return string.Join(";", new[]
        {
            chest.QualifiedItemId,
            chest.SpecialChestType.ToString(),
            chest.GlobalInventoryId ?? string.Empty,
            chest.playerChest.Value.ToString(),
            chest.fridge.Value.ToString(),
            chest.giftbox.Value.ToString(),
            chest.playerChoiceColor.Value.PackedValue.ToString(),
            chest.Tint.PackedValue.ToString(),
            items,
            wallets
        });
    }

    private static IEnumerable<Item> CloneItems(IEnumerable<Item?> items)
    {
        foreach (Item? item in items)
        {
            if (item is null)
                continue;

            Item clone = item.getOne();
            clone.CopyFieldsFrom(item);
            clone.Stack = item.Stack;
            yield return clone;
        }
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

using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal sealed class HeavyChestSlowdownService
{
    private const string BuffId = Constants.ModId + ".HeavyChestSlowdown";
    private const int RefreshIntervalTicks = 15;
    private const int BuffDurationMilliseconds = 1200;

    private readonly ModConfig config;
    private int ticksUntilRefresh;

    public HeavyChestSlowdownService(ModConfig config)
    {
        this.config = config;
    }

    public void Update()
    {
        if (!Context.IsWorldReady)
            return;

        ticksUntilRefresh--;
        if (ticksUntilRefresh > 0)
            return;

        ticksUntilRefresh = RefreshIntervalTicks;
        ApplyOrRemove(Game1.player);
    }

    private void ApplyOrRemove(Farmer player)
    {
        HeavyChestSlowdownConfig slowdown = config.HeavyChestSlowdown;
        if (!slowdown.Enabled || player.CurrentItem is not Chest chest || !ChestMetadata.IsCarriedChest(chest))
        {
            player.buffs.Remove(BuffId);
            return;
        }

        float penalty = GetPenalty(chest, player.UniqueMultiplayerID, slowdown);
        if (penalty <= 0f)
        {
            player.buffs.Remove(BuffId);
            return;
        }

        BuffEffects effects = new();
        effects.Speed.Value = -penalty;

        Buff buff = new(
            id: BuffId,
            source: Constants.ModId,
            displaySource: "Carryable Chests",
            duration: BuffDurationMilliseconds,
            effects: effects,
            isDebuff: true,
            displayName: I18n.HeavyChestSlowdownName,
            description: I18n.HeavyChestSlowdownDescription)
        {
            visible = slowdown.ShowIcon
        };

        player.applyBuff(buff);
    }

    private static float GetPenalty(Chest chest, long playerId, HeavyChestSlowdownConfig slowdown)
    {
        int capacity = Math.Max(1, chest.GetActualCapacity());
        int filledSlots = CountFilledSlots(chest.GetItemsForPlayer(playerId));
        float fillPercent = Math.Clamp(filledSlots * 100f / capacity, 0f, 100f);

        if (slowdown.StartsAt >= 100)
            return filledSlots >= capacity ? slowdown.MaxPenalty : 0f;

        if (fillPercent < slowdown.StartsAt)
            return 0f;

        float progress = (fillPercent - slowdown.StartsAt) / (100f - slowdown.StartsAt);
        return slowdown.MaxPenalty * Math.Clamp(progress, 0f, 1f);
    }

    private static int CountFilledSlots(IInventory inventory)
    {
        int count = 0;
        foreach (Item? item in inventory)
        {
            if (item is not null)
                count++;
        }

        return count;
    }
}

using StardewModdingAPI;

namespace CylixLee.StardewValley.CarryableChests;

internal static class I18n
{
    private static ITranslationHelper? translations;

    public static void Init(ITranslationHelper translationHelper)
    {
        translations = translationHelper;
    }

    public static string CannotCarryNonPlayerChest => Get("cannot-carry.non-player-chest");
    public static string CannotCarryOpenChest => Get("cannot-carry.open-chest");
    public static string CannotCarryInTransition => Get("cannot-carry.in-transition");
    public static string CannotCarryInventoryFull => Get("cannot-carry.inventory-full");
    public static string CannotCarryRemoveWorldFailed => Get("cannot-carry.remove-world-failed");
    public static string CannotCarryAddInventoryFailed => Get("cannot-carry.add-inventory-failed");

    public static string MultiplayerHostPickupOnly => Get("multiplayer.host-pickup-only");
    public static string MultiplayerHostPlaceOnly => Get("multiplayer.host-place-only");
    public static string MultiplayerPickupRejected => Get("multiplayer.pickup-rejected");
    public static string MultiplayerPlaceRejected => Get("multiplayer.place-rejected");

    public static string ReconcileSummary => Get("command.reconcile.summary");

    public static string ConfigMaximumReachName => Get("config.maximum-reach.name");
    public static string ConfigMaximumReachTooltip => Get("config.maximum-reach.tooltip");
    public static string ConfigRequireEmptyHandsName => Get("config.require-empty-hands.name");
    public static string ConfigRequireEmptyHandsTooltip => Get("config.require-empty-hands.tooltip");
    public static string ConfigOpenHeldChestName => Get("config.open-held-chest.name");
    public static string ConfigOpenHeldChestTooltip => Get("config.open-held-chest.tooltip");
    public static string ConfigHeavyChestSlowdownName => Get("config.heavy-chest-slowdown.name");
    public static string ConfigHeavyChestSlowdownTooltip => Get("config.heavy-chest-slowdown.tooltip");
    public static string ConfigHeavyChestSlowdownEnabledName => Get("config.heavy-chest-slowdown.enabled.name");
    public static string ConfigHeavyChestSlowdownEnabledTooltip => Get("config.heavy-chest-slowdown.enabled.tooltip");
    public static string ConfigSlowdownStartsAtName => Get("config.slowdown-starts-at.name");
    public static string ConfigSlowdownStartsAtTooltip => Get("config.slowdown-starts-at.tooltip");
    public static string ConfigMaxSpeedPenaltyName => Get("config.max-speed-penalty.name");
    public static string ConfigMaxSpeedPenaltyTooltip => Get("config.max-speed-penalty.tooltip");
    public static string ConfigShowSlowdownIconName => Get("config.show-slowdown-icon.name");
    public static string ConfigShowSlowdownIconTooltip => Get("config.show-slowdown-icon.tooltip");
    public static string HeavyChestSlowdownName => Get("buff.heavy-chest-slowdown.name");
    public static string HeavyChestSlowdownDescription => Get("buff.heavy-chest-slowdown.description");

    private static string Get(string key, object? tokens = null)
    {
        if (translations is null)
            return key;

        return tokens is null
            ? translations.Get(key)
            : translations.Get(key, tokens);
    }
}

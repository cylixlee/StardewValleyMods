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
    public static string CannotCarryGlobalInventoryChest => Get("cannot-carry.global-inventory-chest");
    public static string CannotCarryInventoryFull => Get("cannot-carry.inventory-full");
    public static string CannotCarryRemoveWorldFailed => Get("cannot-carry.remove-world-failed");
    public static string CannotCarryAddInventoryFailed => Get("cannot-carry.add-inventory-failed");

    public static string PlacementBackupKept => Get("placement.backup-kept");
    public static string MultiplayerHostPickupOnly => Get("multiplayer.host-pickup-only");
    public static string MultiplayerHostPlaceOnly => Get("multiplayer.host-place-only");

    public static string RecoveryCarriedChanged => Get("recovery.carried-changed");
    public static string RecoveryNoSafeTile => Get("recovery.no-safe-tile");
    public static string RecoveryOrphanFailed => Get("recovery.orphan-failed");
    public static string RecoveryRestoredOrigin => Get("recovery.restored-origin");
    public static string RecoveryRestoredInventory => Get("recovery.restored-inventory");

    public static string CommandRecoverSummary => Get("command.recover.summary");

    private static string Get(string key, object? tokens = null)
    {
        if (translations is null)
            return key;

        return tokens is null
            ? translations.Get(key)
            : translations.Get(key, tokens);
    }
}

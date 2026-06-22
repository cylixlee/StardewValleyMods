using StardewModdingAPI;

namespace CylixLee.StardewValley.FarmCleaner;

internal static class I18n
{
    private static ITranslationHelper? translations;

    public static void Init(ITranslationHelper translationHelper)
    {
        translations = translationHelper;
    }

    public static string NotOnFarm => Get("not-on-farm");
    public static string MainPlayerOnly => Get("main-player-only");
    public static string AlreadyCleaning => Get("already-cleaning");
    public static string NothingToClear => Get("nothing-to-clear");
    public static string ClearedItems(object count) => Get("cleared-items", new { count });

    public static string ConfigHotKeyName => Get("config.hot-key.name");
    public static string ConfigHotKeyTooltip => Get("config.hot-key.tooltip");
    public static string ConfigEnableOnNonFarmAreasName => Get("config.enable-on-non-farm-areas.name");
    public static string ConfigEnableOnNonFarmAreasTooltip => Get("config.enable-on-non-farm-areas.tooltip");
    public static string ConfigGainExperienceName => Get("config.gain-experience.name");
    public static string ConfigGainExperienceTooltip => Get("config.gain-experience.tooltip");
    public static string ConfigClearGrassName => Get("config.clear-grass.name");
    public static string ConfigClearGrassTooltip => Get("config.clear-grass.tooltip");
    public static string ConfigClearFruitTreesName => Get("config.clear-fruit-trees.name");
    public static string ConfigClearFruitTreesTooltip => Get("config.clear-fruit-trees.tooltip");
    public static string ConfigClearTappedTreesName => Get("config.clear-tapped-trees.name");
    public static string ConfigClearTappedTreesTooltip => Get("config.clear-tapped-trees.tooltip");
    public static string ConfigClearGrowingTreesName => Get("config.clear-growing-trees.name");
    public static string ConfigClearGrowingTreesTooltip => Get("config.clear-growing-trees.tooltip");
    public static string ConfigClearGiantCropsName => Get("config.clear-giant-crops.name");
    public static string ConfigClearGiantCropsTooltip => Get("config.clear-giant-crops.tooltip");
    public static string ConfigDropMultiplierName => Get("config.drop-multiplier.name");
    public static string ConfigDropMultiplierTooltip => Get("config.drop-multiplier.tooltip");
    public static string CommandClearfarmSummary => Get("command.clearfarm.summary");
    public static string CommandClearfarmUsage => Get("command.clearfarm.usage");

    private static string Get(string key, object? tokens = null)
    {
        if (translations is null)
            return key;

        return tokens is null
            ? translations.Get(key)
            : translations.Get(key, tokens);
    }
}

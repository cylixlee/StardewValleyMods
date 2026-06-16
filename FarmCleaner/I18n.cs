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
    public static string NothingToClear => Get("nothing-to-clear");
    public static string ClearedItems(object count) => Get("cleared-items", new { count });

    public static string Config_HotKey_Name => Get("config.hot-key.name");
    public static string Config_HotKey_Tooltip => Get("config.hot-key.tooltip");
    public static string Config_GainExperience_Name => Get("config.gain-experience.name");
    public static string Config_GainExperience_Tooltip => Get("config.gain-experience.tooltip");
    public static string Config_ClearFruitTrees_Name => Get("config.clear-fruit-trees.name");
    public static string Config_ClearFruitTrees_Tooltip => Get("config.clear-fruit-trees.tooltip");
    public static string Config_ClearTappedTrees_Name => Get("config.clear-tapped-trees.name");
    public static string Config_ClearTappedTrees_Tooltip => Get("config.clear-tapped-trees.tooltip");
    public static string Config_ClearGrowingTrees_Name => Get("config.clear-growing-trees.name");
    public static string Config_ClearGrowingTrees_Tooltip => Get("config.clear-growing-trees.tooltip");
    public static string Config_ClearPlantedTrees_Name => Get("config.clear-planted-trees.name");
    public static string Config_ClearPlantedTrees_Tooltip => Get("config.clear-planted-trees.tooltip");
    public static string Config_ClearGiantCrops_Name => Get("config.clear-giant-crops.name");
    public static string Config_ClearGiantCrops_Tooltip => Get("config.clear-giant-crops.tooltip");
    public static string Config_DropMultiplier_Name => Get("config.drop-multiplier.name");
    public static string Config_DropMultiplier_Tooltip => Get("config.drop-multiplier.tooltip");
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

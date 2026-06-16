using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CylixLee.StardewValley.FarmCleaner;

internal sealed class ModEntry : Mod
{
    private ModConfig config = null!;
    private FarmClearer farmClearer = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        config = Helper.ReadConfig<ModConfig>();
        farmClearer = new FarmClearer(Helper, Monitor);

        FarmCleanerPatches.Apply(ModManifest.UniqueID, Monitor);

        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        helper.ConsoleCommands.Add("clearfarm",
            I18n.CommandClearfarmSummary,
            (_, _) => DoClear());
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return;

        if (!config.HotKey.JustPressed())
            return;

        if (Game1.currentLocation is not Farm)
        {
            Monitor.Log(I18n.NotOnFarm, LogLevel.Info);
            return;
        }

        DoClear();
    }

    private void DoClear()
    {
        farmClearer.ClearFarm(
            config.GainExperience,
            config.ClearFruitTrees,
            config.ClearTappedTrees,
            config.ClearGrowingTrees,
            config.ClearPlantedTrees,
            config.ClearGiantCrops,
            config.DropMultiplier);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
            "spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        gmcm.Register(
            mod: ModManifest,
            reset: () => config = new ModConfig(),
            save: () => Helper.WriteConfig(config)
        );

        gmcm.AddKeybindList(
            mod: ModManifest,
            getValue: () => config.HotKey,
            setValue: val => config.HotKey = val,
            name: () => I18n.Config_HotKey_Name,
            tooltip: () => I18n.Config_HotKey_Tooltip
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.GainExperience,
            setValue: val => config.GainExperience = val,
            name: () => I18n.Config_GainExperience_Name,
            tooltip: () => I18n.Config_GainExperience_Tooltip
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearFruitTrees,
            setValue: val => config.ClearFruitTrees = val,
            name: () => I18n.Config_ClearFruitTrees_Name,
            tooltip: () => I18n.Config_ClearFruitTrees_Tooltip
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearTappedTrees,
            setValue: val => config.ClearTappedTrees = val,
            name: () => I18n.Config_ClearTappedTrees_Name,
            tooltip: () => I18n.Config_ClearTappedTrees_Tooltip
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearGrowingTrees,
            setValue: val => config.ClearGrowingTrees = val,
            name: () => I18n.Config_ClearGrowingTrees_Name,
            tooltip: () => I18n.Config_ClearGrowingTrees_Tooltip
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearPlantedTrees,
            setValue: val => config.ClearPlantedTrees = val,
            name: () => I18n.Config_ClearPlantedTrees_Name,
            tooltip: () => I18n.Config_ClearPlantedTrees_Tooltip
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearGiantCrops,
            setValue: val => config.ClearGiantCrops = val,
            name: () => I18n.Config_ClearGiantCrops_Name,
            tooltip: () => I18n.Config_ClearGiantCrops_Tooltip
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => config.DropMultiplier,
            setValue: val => config.DropMultiplier = val,
            name: () => I18n.Config_DropMultiplier_Name,
            tooltip: () => I18n.Config_DropMultiplier_Tooltip,
            min: 0.1f,
            max: 10f,
            interval: 0.1f
        );
    }
}

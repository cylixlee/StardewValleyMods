using CylixLee.StardewValley.FarmCleaner.Framework;
using CylixLee.StardewValley.FarmCleaner.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CylixLee.StardewValley.FarmCleaner;

internal sealed class ModEntry : Mod
{
    private ModConfig config = null!;
    private FarmClearer clearer = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        config = helper.ReadConfig<ModConfig>();
        clearer = new FarmClearer(helper, Monitor);

        FarmCleanerPatches.Apply(ModManifest.UniqueID, Monitor);

        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

        helper.ConsoleCommands.Add(
            "clearfarm",
            I18n.CommandClearfarmSummary,
            (_, _) => DoClear());
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return;

        if (config.HotKey.JustPressed())
            DoClear();
    }

    private void DoClear()
    {
        if (!Context.IsWorldReady)
            return;

        if (!Context.IsMainPlayer)
        {
            Monitor.Log(I18n.MainPlayerOnly, LogLevel.Info);
            return;
        }

        GameLocation? location = Game1.currentLocation;
        if (location is null)
            return;

        if (!config.EnableOnNonFarmAreas && location is not Farm)
        {
            Monitor.Log(I18n.NotOnFarm, LogLevel.Info);
            return;
        }

        clearer.ClearLocation(
            location,
            new CleanupOptions(
                config.GainExperience,
                config.ClearGrass,
                config.ClearFruitTrees,
                config.ClearTappedTrees,
                config.ClearGrowingTrees,
                config.ClearGiantCrops,
                config.DropMultiplier));
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IGenericModConfigMenuApi? gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
            "spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        gmcm.Register(
            mod: ModManifest,
            reset: ResetConfig,
            save: () => Helper.WriteConfig(config));

        gmcm.AddKeybindList(
            mod: ModManifest,
            getValue: () => config.HotKey,
            setValue: value => config.HotKey = value,
            name: () => I18n.ConfigHotKeyName,
            tooltip: () => I18n.ConfigHotKeyTooltip);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.EnableOnNonFarmAreas,
            setValue: value => config.EnableOnNonFarmAreas = value,
            name: () => I18n.ConfigEnableOnNonFarmAreasName,
            tooltip: () => I18n.ConfigEnableOnNonFarmAreasTooltip);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.GainExperience,
            setValue: value => config.GainExperience = value,
            name: () => I18n.ConfigGainExperienceName,
            tooltip: () => I18n.ConfigGainExperienceTooltip);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearGrass,
            setValue: value => config.ClearGrass = value,
            name: () => I18n.ConfigClearGrassName,
            tooltip: () => I18n.ConfigClearGrassTooltip);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearFruitTrees,
            setValue: value => config.ClearFruitTrees = value,
            name: () => I18n.ConfigClearFruitTreesName,
            tooltip: () => I18n.ConfigClearFruitTreesTooltip);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearTappedTrees,
            setValue: value => config.ClearTappedTrees = value,
            name: () => I18n.ConfigClearTappedTreesName,
            tooltip: () => I18n.ConfigClearTappedTreesTooltip);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearGrowingTrees,
            setValue: value => config.ClearGrowingTrees = value,
            name: () => I18n.ConfigClearGrowingTreesName,
            tooltip: () => I18n.ConfigClearGrowingTreesTooltip);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ClearGiantCrops,
            setValue: value => config.ClearGiantCrops = value,
            name: () => I18n.ConfigClearGiantCropsName,
            tooltip: () => I18n.ConfigClearGiantCropsTooltip);

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => config.DropMultiplier,
            setValue: value => config.DropMultiplier = Math.Clamp(value, 0.1f, 10.0f),
            name: () => I18n.ConfigDropMultiplierName,
            tooltip: () => I18n.ConfigDropMultiplierTooltip,
            min: 0.1f,
            max: 10.0f,
            interval: 0.1f);
    }

    private void ResetConfig()
    {
        config = new ModConfig();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        clearer.CancelActiveSession();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        clearer.CancelActiveSession();
    }
}

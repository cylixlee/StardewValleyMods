using CylixLee.StardewValley.CarryableChests.Framework;
using CylixLee.StardewValley.CarryableChests.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace CylixLee.StardewValley.CarryableChests;

internal sealed class ModEntry : Mod
{
    private ModConfig config = null!;
    private ChestBackupStore backups = null!;
    private CarryableChestCoordinator coordinator = null!;
    private RecoveryService recovery = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        config = helper.ReadConfig<ModConfig>();
        backups = new ChestBackupStore(Monitor);
        WorldChestPlacer placer = new(backups);
        coordinator = new CarryableChestCoordinator(Monitor, config, backups, placer);
        recovery = new RecoveryService(Monitor, backups, placer);

        CarryableChestPatches.Apply(ModManifest.UniqueID, Monitor, coordinator);

        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

        helper.ConsoleCommands.Add(
            "carryable_chests_recover",
            I18n.CommandRecoverSummary,
            (_, _) => recovery.RecoverOrphans());
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.Button.IsActionButton() && Context.IsPlayerFree && coordinator.TryOpenHeldChest(Game1.player))
        {
            Helper.Input.Suppress(e.Button);
            return;
        }

        if (!Context.IsPlayerFree || !e.Button.IsUseToolButton())
            return;

        if (Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            Game1.showRedMessage(I18n.MultiplayerHostPickupOnly);
            return;
        }

        if (config.RequireEmptyHands && Game1.player.CurrentItem is not null)
            return;

        var tile = e.Button.TryGetController(out _) ? e.Cursor.GrabTile : e.Cursor.Tile;
        if (coordinator.TryPickUp(Game1.currentLocation, tile, Game1.player))
            Helper.Input.Suppress(e.Button);
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
            save: () => Helper.WriteConfig(config));

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => config.MaximumReach,
            setValue: val => config.MaximumReach = Math.Clamp((int)val, 1, 4),
            name: () => I18n.ConfigMaximumReachName,
            tooltip: () => I18n.ConfigMaximumReachTooltip,
            min: 1,
            max: 4,
            interval: 1);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.RequireEmptyHands,
            setValue: val => config.RequireEmptyHands = val,
            name: () => I18n.ConfigRequireEmptyHandsName,
            tooltip: () => I18n.ConfigRequireEmptyHandsTooltip);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.OpenHeldChest,
            setValue: val => config.OpenHeldChest = val,
            name: () => I18n.ConfigOpenHeldChestName,
            tooltip: () => I18n.ConfigOpenHeldChestTooltip);

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => config.ReturnCarriedChestsBeforeSaving,
            setValue: val => config.ReturnCarriedChestsBeforeSaving = val,
            name: () => I18n.ConfigReturnBeforeSavingName,
            tooltip: () => I18n.ConfigReturnBeforeSavingTooltip);
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.OldMenu is ItemGrabMenu { sourceItem: Chest chest } && ChestMetadata.IsCarriedChest(chest))
            coordinator.SyncBackupForClosedMenu(chest, Game1.player);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        recovery.RecoverOrphans();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (config.ReturnCarriedChestsBeforeSaving)
            recovery.ReturnCarriedChestsBeforeSaving();
        else
            recovery.SyncBackupsFromCarriedChests();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        backups.ClearCachedState();
    }
}

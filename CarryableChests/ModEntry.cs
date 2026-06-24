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
    private CarryStateStore stateStore = null!;
    private CarryableChestCoordinator coordinator = null!;
    private ChestReconciliationService reconciliation = null!;
    private CarryableChestNetworkService network = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        config = helper.ReadConfig<ModConfig>();
        backups = new ChestBackupStore(Monitor);
        stateStore = new CarryStateStore(helper, Monitor);
        WorldChestPlacer placer = new(backups);
        coordinator = new CarryableChestCoordinator(Monitor, config, backups, placer, stateStore);
        network = new CarryableChestNetworkService(helper.Multiplayer, Monitor, ModManifest.UniqueID, coordinator);
        coordinator.SetNetwork(network);
        reconciliation = new ChestReconciliationService(Monitor, backups, stateStore);

        CarryableChestPatches.Apply(ModManifest.UniqueID, Monitor, coordinator, () => config);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.Multiplayer.ModMessageReceived += network.OnModMessageReceived;
        helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
        helper.Events.Multiplayer.PeerDisconnected += OnPeerDisconnected;

        helper.ConsoleCommands.Add(
            "carryable_chests_recover",
            I18n.ReconcileSummary,
            (_, _) => reconciliation.Reconcile(repairFromVault: true));
        helper.ConsoleCommands.Add(
            "carryable_chests_reconcile",
            I18n.ReconcileSummary,
            (_, _) => reconciliation.Reconcile(repairFromVault: true));
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
            "spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        gmcm.Register(
            mod: ModManifest,
            reset: ResetConfig,
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
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.OldMenu is ItemGrabMenu { sourceItem: Chest chest } && ChestMetadata.IsCarriedChest(chest))
            coordinator.SyncBackupForClosedMenu(chest, Game1.player);
    }

    private void ResetConfig()
    {
        ModConfig defaults = new();
        config.MaximumReach = defaults.MaximumReach;
        config.RequireEmptyHands = defaults.RequireEmptyHands;
        config.OpenHeldChest = defaults.OpenHeldChest;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        stateStore.Load(backups);
        reconciliation.Reconcile();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        reconciliation.SyncVaultFromCarriedChests();
        stateStore.SaveFromVault(backups);
    }

    private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        stateStore.MarkOwnerOffline(e.Peer.PlayerID);
        stateStore.SaveFromVault(backups);
    }

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        reconciliation.Reconcile();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        backups.ClearCachedState();
        stateStore.ClearCachedState();
    }
}

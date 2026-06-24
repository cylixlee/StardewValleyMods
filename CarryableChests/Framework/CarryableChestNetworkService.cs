using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace CylixLee.StardewValley.CarryableChests.Framework;

internal sealed class CarryableChestNetworkService
{
    private const string PickupAction = "Pickup";
    private const string PlaceAction = "Place";

    private readonly IMultiplayerHelper multiplayer;
    private readonly IMonitor monitor;
    private readonly string modId;
    private readonly CarryableChestCoordinator coordinator;

    public CarryableChestNetworkService(IMultiplayerHelper multiplayer, IMonitor monitor, string modId, CarryableChestCoordinator coordinator)
    {
        this.multiplayer = multiplayer;
        this.monitor = monitor;
        this.modId = modId;
        this.coordinator = coordinator;
    }

    public void RequestPickup(GameLocation location, Vector2 tile)
    {
        string requestId = NewRequestId();
        multiplayer.SendMessage(
            new PickupRequestMessage
            {
                RequestId = requestId,
                LocationName = location.NameOrUniqueName,
                TileX = (int)tile.X,
                TileY = (int)tile.Y
            },
            Constants.RequestPickupMessage,
            [modId],
            [Game1.MasterPlayer.UniqueMultiplayerID]);
    }

    public void RequestPlace(Chest chest, GameLocation location, Vector2 tile)
    {
        if (!ChestMetadata.TryGetCarryId(chest, out string carryId))
            return;

        string requestId = NewRequestId();
        multiplayer.SendMessage(
            new PlaceRequestMessage
            {
                RequestId = requestId,
                CarryId = carryId,
                LocationName = location.NameOrUniqueName,
                TileX = (int)tile.X,
                TileY = (int)tile.Y,
                Fingerprint = ChestMetadata.GetFingerprint(chest)
            },
            Constants.RequestPlaceMessage,
            [modId],
            [Game1.MasterPlayer.UniqueMultiplayerID]);
    }

    public void NotifyChestUpdated(string carryId)
    {
        if (!Context.IsMultiplayer || Context.IsMainPlayer)
            return;

        multiplayer.SendMessage(
            new ChestUpdatedMessage { CarryId = carryId },
            Constants.ChestUpdatedMessage,
            [modId],
            [Game1.MasterPlayer.UniqueMultiplayerID]);
    }

    public void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != modId)
            return;

        try
        {
            switch (e.Type)
            {
                case Constants.RequestPickupMessage:
                    HandlePickupRequest(e);
                    break;
                case Constants.PickupApprovedMessage:
                    HandlePickupApproved(e.ReadAs<PickupApprovedMessage>());
                    break;
                case Constants.PickupAppliedMessage:
                    HandlePickupApplied(e);
                    break;
                case Constants.RequestPlaceMessage:
                    HandlePlaceRequest(e);
                    break;
                case Constants.PlaceCommittedMessage:
                    HandlePlaceCommitted(e.ReadAs<PlaceCommittedMessage>());
                    break;
                case Constants.PlaceAppliedMessage:
                    HandlePlaceApplied(e);
                    break;
                case Constants.ChestUpdatedMessage:
                    HandleChestUpdated(e);
                    break;
                case Constants.ActionResultMessage:
                    HandleActionResult(e.ReadAs<ActionResultMessage>());
                    break;
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"Failed to handle Carryable Chests multiplayer message '{e.Type}' from {e.FromPlayerID}: {ex}", LogLevel.Error);
        }
    }

    private void HandlePickupRequest(ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        PickupRequestMessage message = e.ReadAs<PickupRequestMessage>();
        Farmer? who = Game1.GetPlayer(e.FromPlayerID);
        GameLocation? location = Game1.getLocationFromName(message.LocationName);
        monitor.Log($"Received pickup request {message.RequestId} from player {e.FromPlayerID} for {message.LocationName} ({message.TileX}, {message.TileY}).", LogLevel.Info);

        string carryId = string.Empty;
        CarryableChestCoordinator.CarryActionResult result = who is null
            ? CarryableChestCoordinator.CarryActionResult.Failed(I18n.MultiplayerPickupRejected)
            : coordinator.PrepareRemotePickup(location, new Vector2(message.TileX, message.TileY), who, out carryId);

        if (!result.Handled || !result.Success)
        {
            monitor.Log($"Rejected pickup request {message.RequestId} from player {e.FromPlayerID}: {result.ErrorMessage ?? "not handled"}.", LogLevel.Warn);
            SendResult(e.FromPlayerID, message.RequestId, PickupAction, accepted: false, result.ErrorMessage ?? I18n.MultiplayerPickupRejected);
            return;
        }

        multiplayer.SendMessage(
            new PickupApprovedMessage
            {
                RequestId = message.RequestId,
                CarryId = carryId,
                LocationName = message.LocationName,
                TileX = message.TileX,
                TileY = message.TileY
            },
            Constants.PickupApprovedMessage,
            [modId],
            [e.FromPlayerID]);
        monitor.Log($"Approved pickup request {message.RequestId} as carry {carryId}; waiting for client apply confirmation.", LogLevel.Info);
    }

    private void HandlePickupApproved(PickupApprovedMessage message)
    {
        GameLocation? location = Game1.getLocationFromName(message.LocationName);
        Vector2 tile = new(message.TileX, message.TileY);
        bool applied = false;
        string? error = null;

        if (location is null)
        {
            error = I18n.MultiplayerPickupRejected;
        }
        else if (!location.Objects.TryGetValue(tile, out SObject? obj) || obj is not Chest source)
        {
            error = I18n.MultiplayerPickupRejected;
        }
        else if (!Game1.player.couldInventoryAcceptThisItem(source))
        {
            error = I18n.CannotCarryInventoryFull;
        }
        else
        {
            ChestMetadata.MarkCarried(source, message.CarryId, location, tile, Game1.player);
            Chest carried = ChestMetadata.CloneChest(source);
            ChestMetadata.MarkCarried(carried, message.CarryId, location, tile, Game1.player);

            if (Game1.player.addItemToInventoryBool(carried, true))
            {
                int index = Game1.player.Items.IndexOf(carried);
                if (index >= 0)
                    Game1.player.CurrentToolIndex = index;
                Game1.playSound("pickUpItem");
                applied = true;
            }
            else
            {
                ChestMetadata.Clear(source);
                error = I18n.CannotCarryAddInventoryFailed;
            }
        }

        if (!applied && error is not null)
            Game1.showRedMessage(error);

        multiplayer.SendMessage(
            new PickupAppliedMessage
            {
                RequestId = message.RequestId,
                CarryId = message.CarryId,
                LocationName = message.LocationName,
                TileX = message.TileX,
                TileY = message.TileY,
                Applied = applied,
                ErrorMessage = error
            },
            Constants.PickupAppliedMessage,
            [modId],
            [Game1.MasterPlayer.UniqueMultiplayerID]);
        monitor.Log($"Pickup request {message.RequestId} local apply result: {(applied ? "applied" : "failed")}{(error is null ? string.Empty : ": " + error)}.", applied ? LogLevel.Info : LogLevel.Warn);
    }

    private void HandlePickupApplied(ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        PickupAppliedMessage message = e.ReadAs<PickupAppliedMessage>();
        GameLocation? location = Game1.getLocationFromName(message.LocationName);
        Vector2 tile = new(message.TileX, message.TileY);
        if (!message.Applied)
        {
            coordinator.CancelRemotePickup(location, tile, message.CarryId);
            monitor.Log($"Pickup request {message.RequestId} failed on player {e.FromPlayerID}: {message.ErrorMessage ?? "unknown error"}.", LogLevel.Warn);
            return;
        }

        Farmer? who = Game1.GetPlayer(e.FromPlayerID);
        if (who is null)
        {
            monitor.Log($"Pickup request {message.RequestId} applied by unknown player {e.FromPlayerID}; durable vault entry remains for carry {message.CarryId}.", LogLevel.Warn);
            return;
        }

        if (!coordinator.CommitRemotePickup(location, tile, message.CarryId, who))
            monitor.Log($"Pickup request {message.RequestId} was applied by player {e.FromPlayerID}, but host could not remove the world chest. Carry {message.CarryId} remains in durable state for reconciliation.", LogLevel.Warn);
    }

    private void HandlePlaceRequest(ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        PlaceRequestMessage message = e.ReadAs<PlaceRequestMessage>();
        Farmer? who = Game1.GetPlayer(e.FromPlayerID);
        GameLocation? location = Game1.getLocationFromName(message.LocationName);
        monitor.Log($"Received place request {message.RequestId} from player {e.FromPlayerID} for carry {message.CarryId} at {message.LocationName} ({message.TileX}, {message.TileY}).", LogLevel.Info);
        if (who is null || location is null)
        {
            monitor.Log($"Rejected place request {message.RequestId}: player or location not found.", LogLevel.Warn);
            SendResult(e.FromPlayerID, message.RequestId, PlaceAction, accepted: false, I18n.MultiplayerPlaceRejected);
            return;
        }

        Chest? chest = coordinator.FindCarriedChest(who, message.CarryId);
        bool usingVaultFallback = false;
        if (chest is null)
        {
            Chest? vaultChest = coordinator.FindVaultChest(message.CarryId);
            if (vaultChest is not null && vaultChest.modData.TryGetValue(Constants.FingerprintKey, out string vaultFingerprint) && vaultFingerprint == message.Fingerprint)
            {
                chest = vaultChest;
                usingVaultFallback = true;
                monitor.Log($"Place request {message.RequestId} is using durable vault fallback for carry {message.CarryId}; host has not seen the farmhand inventory item yet.", LogLevel.Info);
            }
        }

        Vector2 tile = new(message.TileX, message.TileY);
        if (chest is null || location.Objects.ContainsKey(tile))
        {
            monitor.Log($"Rejected place request {message.RequestId}: carried chest not found or tile already occupied.", LogLevel.Warn);
            SendResult(e.FromPlayerID, message.RequestId, PlaceAction, accepted: false, I18n.MultiplayerPlaceRejected);
            return;
        }

        string hostFingerprint = ChestMetadata.GetFingerprint(chest);
        if (!string.IsNullOrWhiteSpace(message.Fingerprint) && hostFingerprint != message.Fingerprint)
        {
            monitor.Log($"Rejected place request {message.RequestId}: host fingerprint does not match client fingerprint for carry {message.CarryId}.", LogLevel.Warn);
            SendResult(e.FromPlayerID, message.RequestId, PlaceAction, accepted: false, I18n.MultiplayerPlaceRejected);
            return;
        }

        Vector2 position = tile * Game1.tileSize;
        if (!Utility.playerCanPlaceItemHere(location, chest, (int)position.X, (int)position.Y, who, show_error: false))
        {
            monitor.Log($"Rejected place request {message.RequestId}: host placement validation failed.", LogLevel.Warn);
            SendResult(e.FromPlayerID, message.RequestId, PlaceAction, accepted: false, I18n.MultiplayerPlaceRejected);
            return;
        }

        coordinator.CompleteRemotePlacement(chest, location, tile, who);
        multiplayer.SendMessage(
            new PlaceCommittedMessage
            {
                RequestId = message.RequestId,
                CarryId = message.CarryId,
                UsedVaultFallback = usingVaultFallback
            },
            Constants.PlaceCommittedMessage,
            [modId],
            [e.FromPlayerID]);
        monitor.Log($"Committed place request {message.RequestId} for carry {message.CarryId}; waiting for client inventory removal.", LogLevel.Info);
    }

    private void HandlePlaceCommitted(PlaceCommittedMessage message)
    {
        Chest? chest = Game1.player.Items.OfType<Chest>().FirstOrDefault(item =>
            ChestMetadata.IsCarriedChest(item)
            && ChestMetadata.TryGetCarryId(item, out string carryId)
            && carryId == message.CarryId);

        bool removed = false;
        if (chest is not null)
        {
            removed = Game1.player.Items.Contains(chest);
            if (removed)
            {
                Game1.player.removeItemFromInventory(chest);
                Game1.player.showNotCarrying();
            }
        }

        if (!removed)
            Game1.showRedMessage(I18n.MultiplayerPlaceRejected);

        multiplayer.SendMessage(
            new PlaceAppliedMessage
            {
                RequestId = message.RequestId,
                CarryId = message.CarryId,
                RemovedFromInventory = removed
            },
            Constants.PlaceAppliedMessage,
            [modId],
            [Game1.MasterPlayer.UniqueMultiplayerID]);
        monitor.Log($"Place request {message.RequestId} local inventory removal result for carry {message.CarryId}: {(removed ? "removed" : "failed")}.", removed ? LogLevel.Info : LogLevel.Warn);
    }

    private void HandlePlaceApplied(ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        PlaceAppliedMessage message = e.ReadAs<PlaceAppliedMessage>();
        if (!message.RemovedFromInventory)
        {
            monitor.Log($"Place request {message.RequestId} committed on host, but player {e.FromPlayerID} failed to remove carry {message.CarryId} from inventory. Durable vault copy remains for reconciliation.", LogLevel.Warn);
            return;
        }

        coordinator.FinalizeRemotePlacement(message.CarryId);
        monitor.Log($"Place request {message.RequestId} completed for player {e.FromPlayerID}; carry {message.CarryId} finalized.", LogLevel.Info);
    }

    private void HandleChestUpdated(ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        ChestUpdatedMessage message = e.ReadAs<ChestUpdatedMessage>();
        Farmer? who = Game1.GetPlayer(e.FromPlayerID);
        if (who is null)
            return;

        Chest? chest = coordinator.FindCarriedChest(who, message.CarryId);
        if (chest is not null)
            coordinator.SyncBackupForClosedMenu(chest, who);
    }

    private void HandleActionResult(ActionResultMessage message)
    {
        if (!message.Accepted)
        {
            Game1.showRedMessage(message.Message ?? (message.Action == PlaceAction ? I18n.MultiplayerPlaceRejected : I18n.MultiplayerPickupRejected));
            return;
        }

        if (message.Action == PickupAction)
            Game1.playSound("pickUpItem");
    }

    private void SendResult(long playerId, string requestId, string action, bool accepted, string? message)
    {
        multiplayer.SendMessage(
            new ActionResultMessage
            {
                RequestId = requestId,
                Action = action,
                Accepted = accepted,
                Message = message
            },
            Constants.ActionResultMessage,
            [modId],
            [playerId]);
    }

    private static string NewRequestId()
    {
        return Constants.ModId + "." + Guid.NewGuid().ToString("N");
    }

    internal sealed class PickupRequestMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
    }

    internal sealed class PickupApprovedMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string CarryId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
    }

    internal sealed class PickupAppliedMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string CarryId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
        public bool Applied { get; set; }
        public string? ErrorMessage { get; set; }
    }

    internal sealed class PlaceRequestMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string CarryId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
        public string Fingerprint { get; set; } = string.Empty;
    }

    internal sealed class PlaceCommittedMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string CarryId { get; set; } = string.Empty;
        public bool UsedVaultFallback { get; set; }
    }

    internal sealed class PlaceAppliedMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string CarryId { get; set; } = string.Empty;
        public bool RemovedFromInventory { get; set; }
    }

    internal sealed class ChestUpdatedMessage
    {
        public string CarryId { get; set; } = string.Empty;
    }

    internal sealed class ActionResultMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public bool Accepted { get; set; }
        public string? Message { get; set; }
    }
}

using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

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
                TileY = (int)tile.Y
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
                case Constants.RequestPlaceMessage:
                    HandlePlaceRequest(e);
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
        CarryableChestCoordinator.CarryActionResult result = who is null
            ? CarryableChestCoordinator.CarryActionResult.Failed(I18n.MultiplayerPickupRejected)
            : coordinator.TryPickUpLocal(location, new Vector2(message.TileX, message.TileY), who, showMessages: false);

        if (!result.Handled || !result.Success)
        {
            SendResult(e.FromPlayerID, message.RequestId, PickupAction, accepted: false, result.ErrorMessage ?? I18n.MultiplayerPickupRejected);
            return;
        }

        SendResult(e.FromPlayerID, message.RequestId, PickupAction, accepted: true, null);
    }

    private void HandlePlaceRequest(ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        PlaceRequestMessage message = e.ReadAs<PlaceRequestMessage>();
        Farmer? who = Game1.GetPlayer(e.FromPlayerID);
        GameLocation? location = Game1.getLocationFromName(message.LocationName);
        if (who is null || location is null)
        {
            SendResult(e.FromPlayerID, message.RequestId, PlaceAction, accepted: false, I18n.MultiplayerPlaceRejected);
            return;
        }

        Chest? chest = coordinator.FindCarriedChest(who, message.CarryId);
        Vector2 tile = new(message.TileX, message.TileY);
        if (chest is null || location.Objects.ContainsKey(tile))
        {
            SendResult(e.FromPlayerID, message.RequestId, PlaceAction, accepted: false, I18n.MultiplayerPlaceRejected);
            return;
        }

        Vector2 position = tile * Game1.tileSize;
        if (!Utility.playerCanPlaceItemHere(location, chest, (int)position.X, (int)position.Y, who, show_error: false))
        {
            SendResult(e.FromPlayerID, message.RequestId, PlaceAction, accepted: false, I18n.MultiplayerPlaceRejected);
            return;
        }

        coordinator.CompletePlacement(chest, location, tile, who);
        SendResult(e.FromPlayerID, message.RequestId, PlaceAction, accepted: true, null);
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

    internal sealed class PlaceRequestMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string CarryId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
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

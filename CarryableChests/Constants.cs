namespace CylixLee.StardewValley.CarryableChests;

internal static class Constants
{
    public const string ModId = "CylixLee.StardewValley.CarryableChests";
    public const string BackupInventoryId = ModId + ".Backups";
    public const string SaveDataKey = "carry-state";

    public const string CarryIdKey = ModId + "/CarryId";
    public const string StateKey = ModId + "/State";
    public const string FingerprintKey = ModId + "/Fingerprint";
    public const string OriginLocationKey = ModId + "/OriginLocation";
    public const string OriginTileXKey = ModId + "/OriginTileX";
    public const string OriginTileYKey = ModId + "/OriginTileY";
    public const string OwnerKey = ModId + "/Owner";

    public const string StateCarried = "Carried";
    public const string StateBackup = "Backup";
    public const string StatePendingPlace = "PendingPlace";

    public const string RequestPickupMessage = "RequestPickup";
    public const string PickupApprovedMessage = "PickupApproved";
    public const string PickupAppliedMessage = "PickupApplied";
    public const string RequestPlaceMessage = "RequestPlace";
    public const string PlaceCommittedMessage = "PlaceCommitted";
    public const string PlaceAppliedMessage = "PlaceApplied";
    public const string ActionResultMessage = "ActionResult";
    public const string ChestUpdatedMessage = "ChestUpdated";
}

using System.Runtime.Serialization;
namespace CylixLee.StardewValley.CarryableChests;

public sealed class ModConfig
{
    public int MaximumReach { get; set; } = 1;
    public bool RequireEmptyHands { get; set; } = true;
    public bool OpenHeldChest { get; set; } = true;

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        MaximumReach = Math.Clamp(MaximumReach, 1, 4);
    }
}

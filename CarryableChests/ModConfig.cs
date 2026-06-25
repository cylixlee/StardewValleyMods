using System.Runtime.Serialization;
namespace CylixLee.StardewValley.CarryableChests;

public sealed class ModConfig
{
    public int MaximumReach { get; set; } = 1;
    public bool RequireEmptyHands { get; set; } = true;
    public bool OpenHeldChest { get; set; } = true;
    public HeavyChestSlowdownConfig HeavyChestSlowdown { get; set; } = new();

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        MaximumReach = Math.Clamp(MaximumReach, 1, 4);
        HeavyChestSlowdown ??= new HeavyChestSlowdownConfig();
        HeavyChestSlowdown.Normalize();
    }
}

public sealed class HeavyChestSlowdownConfig
{
    public bool Enabled { get; set; } = false;
    public int StartsAt { get; set; } = 50;
    public float MaxPenalty { get; set; } = 1f;
    public bool ShowIcon { get; set; } = false;

    public void Normalize()
    {
        StartsAt = Math.Clamp(StartsAt, 0, 100);
        MaxPenalty = Math.Clamp(MaxPenalty, 0f, 4f);
    }
}

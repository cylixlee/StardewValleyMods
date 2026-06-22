using System.Runtime.Serialization;
using StardewModdingAPI.Utilities;

namespace CylixLee.StardewValley.FarmCleaner;

public sealed class ModConfig
{
    public KeybindList HotKey { get; set; } = KeybindList.Parse("K");
    public bool EnableOnNonFarmAreas { get; set; }
    public bool GainExperience { get; set; } = true;
    public bool ClearGrass { get; set; }
    public bool ClearFruitTrees { get; set; }
    public bool ClearTappedTrees { get; set; }
    public bool ClearGrowingTrees { get; set; }
    public bool ClearGiantCrops { get; set; }
    public float DropMultiplier { get; set; } = 1.0f;

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        HotKey ??= KeybindList.Parse("K");
        DropMultiplier = Math.Clamp(DropMultiplier, 0.1f, 10.0f);
    }
}

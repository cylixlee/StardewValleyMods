using System.Runtime.Serialization;
using StardewModdingAPI.Utilities;

namespace CylixLee.StardewValley.FarmCleaner;

public sealed class ModConfig
{
    public KeybindList ClearKey { get; set; } = KeybindList.Parse("K");
    public bool ClearFruitTrees { get; set; }
    public float DropMultiplier { get; set; } = 1.0f;
    public bool EnableExperience { get; set; } = true;
    public bool ClearTappedTrees { get; set; }
    public bool ClearGrowingTrees { get; set; }
    public bool ClearPlantedTrees { get; set; } = true;
    public bool ClearGiantCrops { get; set; }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        DropMultiplier = Math.Clamp(DropMultiplier, 0.1f, 10.0f);
    }
}

namespace CylixLee.StardewValley.FarmCleaner.Framework;

internal sealed record CleanupOptions(
    bool GainExperience,
    bool ClearGrass,
    bool ClearBushes,
    bool ClearFruitTrees,
    bool ClearTappedTrees,
    bool ClearGrowingTrees,
    bool ClearGiantCrops,
    float DropMultiplier);

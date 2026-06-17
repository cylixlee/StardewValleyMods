using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace CylixLee.StardewValley.FarmCleaner;

internal class FarmClearer
{
    private const int MagneticRadiusBoost = 500000;
    private const float StopCheckRadius = 384f;

    private readonly IModHelper modHelper;
    private readonly IMonitor modMonitor;
    private bool magnetActive;
    private readonly List<Item> deferredItems = [];
    private int stopGraceTicks;

    public FarmClearer(IModHelper helper, IMonitor monitor)
    {
        modHelper = helper;
        modMonitor = monitor;
    }

    public void ClearFarm(
        bool gainExperience,
        bool clearGrass,
        bool clearFruitTrees,
        bool clearTappedTrees,
        bool clearGrowingTrees,
        bool clearGiantCrops,
        float dropMultiplier)
    {
        if (magnetActive)
            return;

        var farm = Game1.getFarm();
        if (farm is null)
            return;

        var existingDebris = new HashSet<Debris>(farm.debris);

        FarmCleanerPatches.blockExperience = !gainExperience;
        try
        {
            var total = ClearObjects(farm)
                      + ClearTerrainFeatures(farm, clearGrass, clearFruitTrees, clearTappedTrees, clearGrowingTrees)
                      + ClearResourceClumps(farm, clearGiantCrops);

            if (total == 0)
            {
                modMonitor.Log(I18n.NothingToClear, LogLevel.Info);
                return;
            }

            modMonitor.Log(I18n.ClearedItems(total), LogLevel.Info);

            foreach (var debris in farm.debris)
            {
                var isNew = !existingDebris.Contains(debris);

                if (debris.debrisType.Value == Debris.DebrisType.OBJECT
                    && debris.item is null
                    && !string.IsNullOrEmpty(debris.itemId.Value))
                {
                    var resolved = ItemRegistry.Create(debris.itemId.Value);
                    if (resolved is not null)
                        debris.item = resolved;
                }

                if (isNew && debris.item is not null && Math.Abs(dropMultiplier - 1.0f) > 0.01f)
                    debris.item.Stack = (int)(debris.item.Stack * dropMultiplier);

                if (debris.item is not null && debris.debrisType.Value == Debris.DebrisType.OBJECT)
                    debris.chunksMoveTowardPlayer = true;
            }

            magnetActive = true;
            FarmCleanerPatches.magnetBoostActive = true;
            FarmCleanerPatches.capturedItems.Clear();
            FarmCleanerPatches.overflowedItems.Clear();
            deferredItems.Clear();
            stopGraceTicks = 0;
            magnetTicks = 0;
            modHelper.Events.GameLoop.UpdateTicked += OnMagnetTick;
        }
        finally
        {
            FarmCleanerPatches.blockExperience = false;
        }
    }

    private int magnetTicks;

    private void OnMagnetTick(object? sender, UpdateTickedEventArgs e)
    {
        var farm = Game1.getFarm();
        if (farm is null)
        {
            StopMagnet();
            return;
        }

        magnetTicks++;

        if (magnetTicks % 60 == 0)
            modMonitor.Log($"Magnet tick {magnetTicks}, captured={FarmCleanerPatches.capturedItems.Count}", LogLevel.Debug);

        FarmCleanerPatches.skipIntercept = true;
        var overflow = new List<Item>();
        for (int i = FarmCleanerPatches.capturedItems.Count - 1; i >= 0; i--)
        {
            var leftover = Game1.player.addItemToInventory(FarmCleanerPatches.capturedItems[i]);
            if (leftover is null)
                FarmCleanerPatches.capturedItems.RemoveAt(i);
            else
            {
                overflow.Add(leftover);
                FarmCleanerPatches.capturedItems.RemoveAt(i);
            }
        }
        FarmCleanerPatches.skipIntercept = false;

        foreach (var item in overflow)
        {
            FarmCleanerPatches.overflowedItems.Add(item);
            deferredItems.Add(item);
        }

        var playerPos = Game1.player.Position;
        var shouldStop = FarmCleanerPatches.capturedItems.Count == 0;
        if (shouldStop)
        {
            foreach (var debris in farm.debris)
            {
                if (debris.item is not null || !string.IsNullOrEmpty(debris.itemId.Value))
                {
                    var chunk = debris.Chunks[0];
                    if (!chunk.hasPassedRestingLineOnce.Value)
                    {
                        shouldStop = false;
                        break;
                    }
                    if (Vector2.Distance(playerPos, chunk.position.Value) > StopCheckRadius)
                    {
                        shouldStop = false;
                        break;
                    }
                }
            }
        }

        if (shouldStop)
        {
            stopGraceTicks++;
            if (stopGraceTicks > 50)
            {
                foreach (var item in deferredItems)
                {
                    var offset = new Vector2(
                        (Random.Shared.NextSingle() - 0.5f) * 160f,
                        (Random.Shared.NextSingle() - 0.5f) * 160f - 64f);
                    Game1.createItemDebris(item, Game1.player.Position + offset, Game1.player.FacingDirection);
                }
                deferredItems.Clear();
                StopMagnet();
            }
        }
        else
        {
            stopGraceTicks = 0;
        }
    }

    private void StopMagnet()
    {
        var farm = Game1.getFarm();
        if (farm is not null)
        {
            var remaining = farm.debris.Count;
            var remainingTypes = farm.debris.GroupBy(d => d.debrisType.Value)
                .Select(g => $"{g.Key}={g.Count()}");
            modMonitor.Log($"StopMagnet: remaining debris={remaining}, types=[{string.Join(", ", remainingTypes)}]", LogLevel.Debug);
        }

        modHelper.Events.GameLoop.UpdateTicked -= OnMagnetTick;
        magnetTicks = 0;
        magnetActive = false;
        FarmCleanerPatches.magnetBoostActive = false;
        FarmCleanerPatches.capturedItems.Clear();
        FarmCleanerPatches.overflowedItems.Clear();
    }

    private int ClearObjects(Farm farm)
    {
        var player = Game1.player;
        var pickaxe = new Pickaxe
        {
            lastUser = player,
            UpgradeLevel = 4
        };
        var axe = new Axe
        {
            lastUser = player,
            UpgradeLevel = 4
        };

        var toRemove = new List<Vector2>();

        foreach (var (tile, obj) in farm.Objects.Pairs)
        {
            if (obj is null)
                continue;

            var qid = obj.QualifiedItemId;

            if (IsStone(qid))
            {
                modHelper.Reflection.GetField<int>(obj, "health").SetValue(1);
                obj.performToolAction(pickaxe);
            }
            else if (IsWeed(qid))
            {
                modHelper.Reflection.GetField<int>(obj, "health").SetValue(1);
                obj.performToolAction(axe);
            }
            else if (IsTwig(qid))
            {
                modHelper.Reflection.GetField<int>(obj, "health").SetValue(1);
                obj.performToolAction(axe);
            }
            else
                continue;

            toRemove.Add(tile);
        }

        foreach (var tile in toRemove)
            farm.Objects.Remove(tile);

        return toRemove.Count;
    }

    private int ClearTerrainFeatures(
        Farm farm,
        bool clearGrass,
        bool clearFruitTrees,
        bool clearTappedTrees,
        bool clearGrowingTrees)
    {
        var axe = new Axe
        {
            lastUser = Game1.player,
            UpgradeLevel = 4
        };

        var scythe = ItemRegistry.Create<MeleeWeapon>("(W)47");

        var toRemove = new List<Vector2>();

        foreach (var (tile, feature) in farm.terrainFeatures.Pairs)
        {
            if (feature is null)
                continue;

            switch (feature)
            {
                case Tree tree:
                    if (!clearTappedTrees && HasTapper(farm, tile))
                        break;
                    if (!clearGrowingTrees && tree.growthStage.Value < 5)
                        break;
                    if (tree.stump.Value)
                    {
                        tree.health.Value = 1;
                        tree.performToolAction(axe, explosion: 0, tile);
                    }
                    else
                    {
                        tree.health.Value = 1;
                        tree.stump.Value = false;
                        tree.performToolAction(axe, explosion: 0, tile);
                        tree.health.Value = 1;
                        tree.performToolAction(axe, explosion: 0, tile);
                    }
                    toRemove.Add(tile);
                    break;

                case FruitTree fruitTree when clearFruitTrees:
                    fruitTree.health.Value = 1;
                    fruitTree.stump.Value = true;
                    fruitTree.performToolAction(axe, explosion: 0, tile);
                    toRemove.Add(tile);
                    break;

                case Grass grass when clearGrass:
                    int weeds = grass.numberOfWeeds.Value;
                    for (int i = 0; i < weeds; i++)
                        grass.performToolAction(scythe, explosion: 0, tile);
                    toRemove.Add(tile);
                    break;
            }
        }

        foreach (var tile in toRemove)
            farm.terrainFeatures.Remove(tile);

        return toRemove.Count;
    }

    private static int ClearResourceClumps(Farm farm, bool clearGiantCrops)
    {
        var pickaxe = new Pickaxe
        {
            lastUser = Game1.player,
            UpgradeLevel = 4
        };
        var axe = new Axe
        {
            lastUser = Game1.player,
            UpgradeLevel = 4
        };
        var count = 0;

        var clumps = farm.resourceClumps.ToList();
        var toRemove = new List<ResourceClump>();

        foreach (var clump in clumps)
        {
            var index = clump.parentSheetIndex.Value;

            if (!clearGiantCrops && (index is >= 190 and <= 193 || index is 622))
                continue;

            var tile = new Vector2(
                (int)(clump.Tile.X),
                (int)(clump.Tile.Y)
            );

            var tool = index is 600 or 602
                ? (Tool)axe
                : pickaxe;

            clump.health.Value = 1;
            clump.performToolAction(tool, damage: 1, tile);
            toRemove.Add(clump);
            count++;
        }

        foreach (var clump in toRemove)
            farm.resourceClumps.Remove(clump);

        return count;
    }

    private static bool IsStone(string qualifiedItemId)
    {
        return qualifiedItemId is "(O)32" or "(O)34" or "(O)36" or "(O)38"
            or "(O)40" or "(O)42" or "(O)44" or "(O)46"
            or "(O)48" or "(O)50" or "(O)52" or "(O)54"
            or "(O)56" or "(O)58" or "(O)76" or "(O)77"
            or "(O)79" or "(O)95" or "(O)290" or "(O)343"
            or "(O)450" or "(O)668" or "(O)670" or "(O)751"
            or "(O)760" or "(O)762" or "(O)764" or "(O)765"
            or "(O)817" or "(O)818" or "(O)819" or "(O)843"
            or "(O)844" or "(O)845" or "(O)846" or "(O)847"
            or "(O)922" or "(O)923";
    }

    private static bool IsWeed(string qualifiedItemId)
    {
        return qualifiedItemId is "(O)313" or "(O)314" or "(O)315"
            or "(O)316" or "(O)317" or "(O)318" or "(O)319"
            or "(O)320" or "(O)321" or "(O)452" or "(O)674"
            or "(O)675" or "(O)676" or "(O)677" or "(O)678"
            or "(O)679" or "(O)750" or "(O)784" or "(O)785"
            or "(O)786" or "(O)792" or "(O)793" or "(O)794";
    }

    private static bool IsTwig(string qualifiedItemId)
    {
        return qualifiedItemId is "(O)294" or "(O)295";
    }

    private static bool HasTapper(Farm farm, Vector2 tile)
    {
        if (!farm.Objects.TryGetValue(tile, out var obj) || obj is null)
            return false;
        return obj.QualifiedItemId is "(BC)105" or "(BC)264";
    }
}

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
    private GameLocation? activeLocation;
    private bool magnetActive;
    private readonly List<Item> deferredItems = [];
    private int stopGraceTicks;

    public FarmClearer(IModHelper helper, IMonitor monitor)
    {
        modHelper = helper;
        modMonitor = monitor;
    }

    public void ClearLocation(
        GameLocation location,
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

        var existingDebris = new HashSet<Debris>(location.debris);

        FarmCleanerPatches.blockExperience = !gainExperience;
        try
        {
            var total = ClearObjects(location)
                      + ClearTerrainFeatures(location, clearGrass, clearFruitTrees, clearTappedTrees, clearGrowingTrees)
                      + ClearResourceClumps(location, clearGiantCrops);

            if (total == 0)
            {
                modMonitor.Log(I18n.NothingToClear, LogLevel.Info);
                return;
            }

            modMonitor.Log(I18n.ClearedItems(total), LogLevel.Info);

            foreach (var debris in location.debris)
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

            activeLocation = location;
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
        var location = activeLocation;
        if (location is null)
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
            foreach (var debris in location.debris)
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
        var location = activeLocation;
        if (location is not null)
        {
            var remaining = location.debris.Count;
            var remainingTypes = location.debris.GroupBy(d => d.debrisType.Value)
                .Select(g => $"{g.Key}={g.Count()}");
            modMonitor.Log($"StopMagnet: remaining debris={remaining}, types=[{string.Join(", ", remainingTypes)}]", LogLevel.Debug);
        }

        modHelper.Events.GameLoop.UpdateTicked -= OnMagnetTick;
        magnetTicks = 0;
        activeLocation = null;
        magnetActive = false;
        FarmCleanerPatches.magnetBoostActive = false;
        FarmCleanerPatches.capturedItems.Clear();
        FarmCleanerPatches.overflowedItems.Clear();
    }

    private int ClearObjects(GameLocation location)
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

        foreach (var (tile, obj) in location.Objects.Pairs)
        {
            if (obj is null)
                continue;

            obj.Location = location;
            obj.TileLocation = tile;

            if (obj.IsBreakableStone())
            {
                obj.MinutesUntilReady = 1;
                if (!obj.performToolAction(pickaxe))
                    continue;
                location.OnStoneDestroyed(obj.ItemId, (int)tile.X, (int)tile.Y, player);
            }
            else if (obj.IsWeeds())
            {
                modHelper.Reflection.GetField<int>(obj, "health").SetValue(1);
                obj.performToolAction(axe);
            }
            else if (obj.IsTwig())
            {
                modHelper.Reflection.GetField<int>(obj, "health").SetValue(1);
                obj.performToolAction(axe);
            }
            else
                continue;

            toRemove.Add(tile);
        }

        foreach (var tile in toRemove)
            location.Objects.Remove(tile);

        return toRemove.Count;
    }

    private int ClearTerrainFeatures(
        GameLocation location,
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

        foreach (var (tile, feature) in location.terrainFeatures.Pairs)
        {
            if (feature is null)
                continue;

            switch (feature)
            {
                case Tree tree:
                    if (!clearTappedTrees && HasTapper(location, tile))
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
            location.terrainFeatures.Remove(tile);

        return toRemove.Count;
    }

    private static int ClearResourceClumps(GameLocation location, bool clearGiantCrops)
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

        var clumps = location.resourceClumps.ToList();
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

            var tool = clump is GiantCrop || index is 600 or 602
                ? (Tool)axe
                : pickaxe;

            clump.health.Value = 1;
            if (!clump.performToolAction(tool, damage: 1, tile))
                continue;

            toRemove.Add(clump);
            count++;
        }

        foreach (var clump in toRemove)
            location.resourceClumps.Remove(clump);

        return count;
    }

    private static bool HasTapper(GameLocation location, Vector2 tile)
    {
        if (!location.Objects.TryGetValue(tile, out var obj) || obj is null)
            return false;
        return obj.QualifiedItemId is "(BC)105" or "(BC)264";
    }
}

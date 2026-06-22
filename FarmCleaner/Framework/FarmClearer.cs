using Microsoft.Xna.Framework;
using CylixLee.StardewValley.FarmCleaner.Patches;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace CylixLee.StardewValley.FarmCleaner.Framework;

internal sealed class FarmClearer
{
    private const int MatureTreeGrowthStage = 5;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private CleanupSession? activeSession;

    public FarmClearer(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
    }

    public bool IsBusy => activeSession?.IsActive == true;

    public void ClearLocation(GameLocation location, CleanupOptions options)
    {
        if (IsBusy)
        {
            monitor.Log(I18n.AlreadyCleaning, LogLevel.Info);
            return;
        }

        Farmer player = Game1.player;
        HashSet<Debris> existingDebris = new(location.debris);

        FarmCleanerPatches.BlockExperience = !options.GainExperience;
        try
        {
            int cleared = ClearObjects(location, player)
                + ClearTerrainFeatures(location, player, options)
                + ClearResourceClumps(location, player, options.ClearGiantCrops);

            if (cleared == 0)
            {
                monitor.Log(I18n.NothingToClear, LogLevel.Info);
                return;
            }

            List<Debris> newDebris = location.debris.Where(debris => !existingDebris.Contains(debris)).ToList();
            PrepareDebris(newDebris, options.DropMultiplier);

            monitor.Log(I18n.ClearedItems(cleared), LogLevel.Info);

            activeSession = new CleanupSession(helper, monitor, location, player, newDebris, options.DropMultiplier);
            activeSession.Start();
        }
        finally
        {
            FarmCleanerPatches.BlockExperience = false;
        }
    }

    public void CancelActiveSession()
    {
        activeSession?.Cancel();
        activeSession = null;
    }

    internal static void PrepareDebris(IEnumerable<Debris> debrisList, float dropMultiplier)
    {
        foreach (Debris debris in debrisList)
            PrepareDebris(debris, dropMultiplier);
    }

    internal static void PrepareDebris(Debris debris, float dropMultiplier)
    {
        if (debris.debrisType.Value is not (Debris.DebrisType.OBJECT or Debris.DebrisType.RESOURCE))
            return;

        debris.player.Value = Game1.player;
        debris.DroppedByPlayerID.Value = 0;
        debris.chunksMoveTowardPlayer = true;

        Item? item = ResolveItem(debris);
        if (item is null)
            return;

        if (Math.Abs(dropMultiplier - 1.0f) > 0.01f)
            item.Stack = Math.Max(1, (int)MathF.Round(item.Stack * dropMultiplier));

        debris.item = item;
    }

    private static Item? ResolveItem(Debris debris)
    {
        if (debris.item is not null)
            return debris.item;

        if (string.IsNullOrWhiteSpace(debris.itemId.Value))
            return null;

        return ItemRegistry.Create(debris.itemId.Value, 1, debris.itemQuality);
    }

    private int ClearObjects(GameLocation location, Farmer player)
    {
        Pickaxe pickaxe = CreateTool<Pickaxe>(player);
        Axe axe = CreateTool<Axe>(player);
        List<Vector2> candidates = [];

        foreach ((Vector2 tile, SObject obj) in location.Objects.Pairs.ToList())
        {
            if (obj is null)
                continue;

            if (obj.IsBreakableStone() || obj.IsWeeds() || obj.IsTwig())
                candidates.Add(tile);
        }

        int cleared = 0;
        foreach (Vector2 tile in candidates)
        {
            if (!location.Objects.TryGetValue(tile, out SObject? obj) || obj is null)
                continue;

            obj.Location = location;
            obj.TileLocation = tile;

            Tool tool = obj.IsBreakableStone() ? pickaxe : axe;
            if (obj.IsBreakableStone())
                obj.MinutesUntilReady = 1;
            else
                helper.Reflection.GetField<int>(obj, "health").SetValue(1);

            bool destroyed = obj.performToolAction(tool);
            if (obj.IsBreakableStone() && destroyed)
                location.OnStoneDestroyed(obj.ItemId, (int)tile.X, (int)tile.Y, player);

            if (destroyed || location.Objects.ContainsKey(tile))
            {
                location.Objects.Remove(tile);
                cleared++;
            }
        }

        return cleared;
    }

    private int ClearTerrainFeatures(GameLocation location, Farmer player, CleanupOptions options)
    {
        Axe axe = CreateTool<Axe>(player);
        MeleeWeapon scythe = ItemRegistry.Create<MeleeWeapon>("(W)47");
        List<Vector2> candidates = location.terrainFeatures.Pairs
            .Where(pair => IsCleanableTerrainFeature(location, pair.Key, pair.Value, options))
            .Select(pair => pair.Key)
            .ToList();

        int cleared = 0;
        foreach (Vector2 tile in candidates)
        {
            if (!location.terrainFeatures.TryGetValue(tile, out TerrainFeature? feature) || feature is null)
                continue;

            switch (feature)
            {
                case Tree tree:
                    CutTree(tree, axe, tile);
                    location.terrainFeatures.Remove(tile);
                    cleared++;
                    break;

                case FruitTree fruitTree:
                    fruitTree.health.Value = 1;
                    fruitTree.stump.Value = true;
                    fruitTree.performToolAction(axe, explosion: 0, tile);
                    location.terrainFeatures.Remove(tile);
                    cleared++;
                    break;

                case Grass grass:
                    int weeds = Math.Max(1, grass.numberOfWeeds.Value);
                    for (int i = 0; i < weeds; i++)
                        grass.performToolAction(scythe, explosion: 0, tile);
                    location.terrainFeatures.Remove(tile);
                    cleared++;
                    break;
            }
        }

        return cleared;
    }

    private static bool IsCleanableTerrainFeature(GameLocation location, Vector2 tile, TerrainFeature feature, CleanupOptions options)
    {
        return feature switch
        {
            Tree tree => (options.ClearTappedTrees || !HasTapper(location, tile))
                && (options.ClearGrowingTrees || tree.growthStage.Value >= MatureTreeGrowthStage),
            FruitTree => options.ClearFruitTrees,
            Grass => options.ClearGrass,
            _ => false
        };
    }

    private static void CutTree(Tree tree, Axe axe, Vector2 tile)
    {
        tree.health.Value = 1;

        DetachTapper(tree, axe, tile);

        if (tree.stump.Value)
        {
            tree.performToolAction(axe, explosion: 0, tile);
            return;
        }

        tree.stump.Value = false;
        tree.performToolAction(axe, explosion: 0, tile);

        if (!tree.stump.Value)
            return;

        tree.health.Value = 1;
        tree.performToolAction(axe, explosion: 0, tile);
    }

    private static void DetachTapper(Tree tree, Axe axe, Vector2 tile)
    {
        GameLocation? location = tree.Location;
        if (location is null || !tree.tapped.Value)
            return;

        if (location.Objects.TryGetValue(tile, out SObject? tapper) && tapper?.IsTapper() == true)
        {
            tapper.Location = location;
            tapper.TileLocation = tile;
            tapper.performToolAction(axe);
            tapper.performRemoveAction();
            tapper.dropItem(location, axe.getLastFarmerToUse().GetToolLocation(), Utility.PointToVector2(axe.getLastFarmerToUse().StandingPixel));
            location.Objects.Remove(tile);
        }

        tree.tapped.Value = false;
    }

    private static int ClearResourceClumps(GameLocation location, Farmer player, bool clearGiantCrops)
    {
        Pickaxe pickaxe = CreateTool<Pickaxe>(player);
        Axe axe = CreateTool<Axe>(player);
        List<ResourceClump> candidates = location.resourceClumps
            .Where(clump => clearGiantCrops || !IsGiantCropOrMeteorite(clump))
            .ToList();

        int cleared = 0;
        foreach (ResourceClump clump in candidates)
        {
            if (!location.resourceClumps.Contains(clump))
                continue;

            Vector2 tile = new((int)clump.Tile.X, (int)clump.Tile.Y);
            Tool tool = clump is GiantCrop || clump.parentSheetIndex.Value is ResourceClump.stumpIndex or ResourceClump.hollowLogIndex
                ? axe
                : pickaxe;

            clump.health.Value = 1;
            if (!clump.performToolAction(tool, damage: 1, tile))
                continue;

            location.resourceClumps.Remove(clump);
            cleared++;
        }

        return cleared;
    }

    private static bool IsGiantCropOrMeteorite(ResourceClump clump)
    {
        int index = clump.parentSheetIndex.Value;
        return clump is GiantCrop || index is >= 190 and <= 193 or ResourceClump.meteoriteIndex;
    }

    private static bool HasTapper(GameLocation location, Vector2 tile)
    {
        return location.Objects.TryGetValue(tile, out SObject? obj)
            && obj?.QualifiedItemId is "(BC)105" or "(BC)264";
    }

    private static TTool CreateTool<TTool>(Farmer player) where TTool : Tool, new()
    {
        return new TTool
        {
            lastUser = player,
            UpgradeLevel = Tool.iridium
        };
    }
}

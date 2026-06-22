using Microsoft.Xna.Framework;
using CylixLee.StardewValley.FarmCleaner.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CylixLee.StardewValley.FarmCleaner.Framework;

internal sealed class CleanupSession
{
    private const int MaxTicks = 3600;
    private const int EmptyGraceTicks = 45;
    private const int OverflowDropRadius = 160;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly HashSet<Debris> trackedDebris;
    private readonly HashSet<Debris> knownDebris;
    private readonly List<Item> overflowItems = [];
    private readonly long playerId;
    private readonly float dropMultiplier;
    private int tickCount;
    private int emptyTicks;

    public CleanupSession(IModHelper helper, IMonitor monitor, GameLocation location, Farmer player, IEnumerable<Debris> debris, float dropMultiplier)
    {
        this.helper = helper;
        this.monitor = monitor;
        Location = location;
        Player = player;
        trackedDebris = new HashSet<Debris>(debris);
        knownDebris = new HashSet<Debris>(location.debris);
        playerId = player.UniqueMultiplayerID;
        this.dropMultiplier = dropMultiplier;
    }

    public GameLocation Location { get; }
    public Farmer Player { get; }
    public bool IsActive { get; private set; }

    public bool Tracks(Debris debris)
    {
        return IsActive && trackedDebris.Contains(debris);
    }

    public bool Tracks(Farmer farmer)
    {
        return IsActive && farmer.UniqueMultiplayerID == playerId;
    }

    public void CaptureOverflow(Item item)
    {
        if (item.Stack <= 0)
            return;

        Item overflow = item.getOne();
        overflow.Stack = item.Stack;
        overflowItems.Add(overflow);
    }

    public void Start()
    {
        if (IsActive)
            return;

        IsActive = true;
        FarmCleanerPatches.ActiveSession = this;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    public void Cancel()
    {
        Stop(dropOverflow: true);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.player.UniqueMultiplayerID != playerId || Game1.currentLocation != Location)
        {
            Stop(dropOverflow: true);
            return;
        }

        tickCount++;
        TrackNewDebris();
        PruneCollectedDebris();

        if (trackedDebris.Count == 0)
            emptyTicks++;
        else
            emptyTicks = 0;

        if (emptyTicks >= EmptyGraceTicks || tickCount >= MaxTicks)
            Stop(dropOverflow: true);
    }

    private void PruneCollectedDebris()
    {
        trackedDebris.RemoveWhere(debris => !Location.debris.Contains(debris) || debris.Chunks.Count == 0);
    }

    private void TrackNewDebris()
    {
        foreach (Debris debris in Location.debris)
        {
            if (!knownDebris.Add(debris))
                continue;

            trackedDebris.Add(debris);
            FarmClearer.PrepareDebris(debris, dropMultiplier);
        }
    }

    private void Stop(bool dropOverflow)
    {
        if (!IsActive)
            return;

        helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        IsActive = false;

        if (ReferenceEquals(FarmCleanerPatches.ActiveSession, this))
            FarmCleanerPatches.ActiveSession = null;

        if (dropOverflow && Context.IsWorldReady)
            DropOverflowItems();

        trackedDebris.Clear();
        knownDebris.Clear();
        overflowItems.Clear();
    }

    private void DropOverflowItems()
    {
        GameLocation location = Player.currentLocation ?? Location;

        foreach (Item item in overflowItems.Where(item => item.Stack > 0))
        {
            Vector2 offset = new(
                (Game1.random.NextSingle() - 0.5f) * OverflowDropRadius,
                (Game1.random.NextSingle() - 0.5f) * OverflowDropRadius - 64f);

            Game1.createItemDebris(item, Player.Position + offset, Player.FacingDirection, location);
        }
    }
}

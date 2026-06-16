# SMAPI Best-Practice Templates

These are compact templates to adapt when implementing common SMAPI patterns.

## Minimal ModEntry With Services

```csharp
using StardewModdingAPI;
using StardewModdingAPI.Events;

internal sealed class ModEntry : Mod
{
    private ModConfig Config = null!; // set in Entry
    private FeatureManager FeatureManager = null!; // set in Entry

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        this.Config = helper.ReadConfig<ModConfig>();
        this.FeatureManager = new FeatureManager(helper, this.Monitor, this.Config);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.FeatureManager.RegisterIntegrations();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.FeatureManager.ResetForSave();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.FeatureManager.ClearSaveState();
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return;

        this.FeatureManager.HandleInput();
    }
}
```

## Config With Normalization

```csharp
using System.Runtime.Serialization;
using StardewModdingAPI.Utilities;

internal sealed class ModConfig
{
    public bool Enabled { get; set; } = true;

    public ModConfigKeys Controls { get; set; } = new();

    public Dictionary<string, bool> FeatureFlags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        this.Controls ??= new ModConfigKeys();

        this.FeatureFlags = new Dictionary<string, bool>(
            this.FeatureFlags?.Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                ?? Enumerable.Empty<KeyValuePair<string, bool>>(),
            StringComparer.OrdinalIgnoreCase
        );
    }
}

internal sealed class ModConfigKeys
{
    public KeybindList Toggle { get; set; } = KeybindList.Parse("F2");
}
```

## AssetRequested Edit

```csharp
using StardewModdingAPI.Events;

private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
{
    if (!e.NameWithoutLocale.IsEquivalentTo("Data/Example"))
        return;

    e.Edit(asset =>
    {
        IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
        data["Author.ModName/Entry"] = "value";
    });
}
```

## Host-Only Migration

```csharp
private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
{
    if (!Context.IsMainPlayer)
        return;

    string versionKey = $"{this.ModManifest.UniqueID}/LastMigrationVersion";
    string? rawVersion = Game1.CustomData.TryGetValue(versionKey, out string value) ? value : null;

    if (!SemanticVersion.TryParse(rawVersion, out ISemanticVersion? lastVersion))
        lastVersion = new SemanticVersion("0.0.0");

    if (lastVersion.IsOlderThan("1.1.0"))
    {
        this.Monitor.Log("Migrating save data to 1.1.0.", LogLevel.Info);
        // migrate here
    }

    Game1.CustomData[versionKey] = this.ModManifest.Version.ToString();
}
```

## Optional Integration Wrapper

```csharp
internal sealed class OtherModIntegration
{
    private readonly IMonitor Monitor;
    private readonly IModHelper Helper;
    private IOtherModApi? Api;

    public OtherModIntegration(IModHelper helper, IMonitor monitor)
    {
        this.Helper = helper;
        this.Monitor = monitor;
    }

    public void Register()
    {
        if (!this.Helper.ModRegistry.IsLoaded("Author.OtherMod"))
            return;

        this.Api = this.Helper.ModRegistry.GetApi<IOtherModApi>("Author.OtherMod");
        if (this.Api is null)
            this.Monitor.LogOnce("Author.OtherMod is installed, but its API is unavailable.", LogLevel.Warn);
    }

    public bool TryDoThing(string id)
    {
        if (this.Api is null)
            return false;

        try
        {
            return this.Api.DoThing(id);
        }
        catch (Exception ex)
        {
            this.Monitor.LogOnce($"Author.OtherMod failed while handling '{id}'. Technical details:\n{ex}", LogLevel.Warn);
            return false;
        }
    }
}
```

## Multiplayer Message Guard

```csharp
private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
{
    if (e.FromModID != this.ModManifest.UniqueID || e.Type != "RequestAction")
        return;

    if (!Context.IsWorldReady || !Context.IsMainPlayer)
        return;

    IMultiplayerPeer? peer = this.Helper.Multiplayer.GetConnectedPlayer(e.FromPlayerID);
    if (peer is null)
        return;

    RequestActionMessage message = e.ReadAs<RequestActionMessage>();
    this.FeatureManager.HandleFarmhandRequest(peer.PlayerID, message);
}
```

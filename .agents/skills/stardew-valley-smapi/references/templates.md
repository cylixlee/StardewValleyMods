# Templates

## C# Mod `manifest.json`

```json
{
  "Name": "Your Mod Name",
  "Author": "YourName",
  "Version": "%ProjectVersion%",
  "Description": "One or two sentences about the mod.",
  "UniqueID": "YourName.YourModName",
  "EntryDll": "YourModName.dll",
  "MinimumApiVersion": "4.0.0",
  "UpdateKeys": []
}
```

## Content Pack `manifest.json`

```json
{
  "Name": "Your Content Pack Name",
  "Author": "YourName",
  "Version": "1.0.0",
  "Description": "One or two sentences about the pack.",
  "UniqueID": "YourName.YourContentPackName",
  "ContentPackFor": {
    "UniqueID": "Pathoschild.ContentPatcher"
  },
  "UpdateKeys": []
}
```

## Minimal `.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Pathoschild.Stardew.ModBuildConfig" Version="4.3.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Adjust the package version to the current version appropriate for the target SMAPI and project.

## Minimal `ModEntry.cs`

```csharp
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace YourName.YourModName;

internal sealed class ModEntry : Mod
{
    private ModConfig Config = new();

    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>();
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.Monitor.Log("Mod loaded.", LogLevel.Info);
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !this.Config.ToggleKey.JustPressed())
            return;

        this.Monitor.Log("Keybind pressed.", LogLevel.Debug);
    }
}
```

## Minimal `ModConfig.cs`

```csharp
using StardewModdingAPI.Utilities;

namespace YourName.YourModName;

internal sealed class ModConfig
{
    public KeybindList ToggleKey { get; set; } = KeybindList.Parse("F8");
}
```

For Android or both-platform mods, consider a touch-friendly UI entry point in addition to keyboard defaults.

# AutoWorldLoader

A [Space Engineers](https://www.spaceengineersgame.com/) plugin that automatically loads a saved world when the game reaches the main menu.

## How it works

The plugin waits for the main menu to appear (polling every second after an initial 3-second delay), then calls the game's internal session loader to jump straight into the configured world — no manual clicking required.

## Architecture

| File | Role |
|---|---|
| `Plugin.cs` | Orchestrator — reads config, waits for main menu, delegates to `WorldLoader` |
| `WorldLoader.cs` | Stateless service — resolves and loads worlds by name or path |
| `PluginLog.cs` | File logger (`%APPDATA%\SpaceEngineers\AutoWorldLoader.log`) |
| `PluginConsts.cs` | All magic strings and timing constants in one place |

### WorldLoader API

`WorldLoader` is a `public static` class — call it from any plugin or in-game script:

```csharp
// Load by save folder name (resolves under Saves\<SteamID>\<name>)
WorldLoader.LoadByName("My Save Name");

// Load by full filesystem path (for saves in non-standard locations)
WorldLoader.LoadByName(@"C:\CustomSaves\MyWorld", isCustomPath: true);

// Or use the direct path overload
WorldLoader.LoadByPath(@"C:\Saves\123456789\MyWorld");

// Resolve path without loading
string path = WorldLoader.ResolveSavePath("My Save Name");
```

## Configuration

Create a file at `%APPDATA%\SpaceEngineers\AutoWorldLoader.json`:

```json
{
  "worldName": "My Save Name"
}
```

The save must exist under `%APPDATA%\SpaceEngineers\Saves\<SteamID64>\<worldName>`.

> **Important:** When copying and renaming a world, update the `<SessionName>` inside `Sandbox.sbc` and `Sandbox_config.sbc` to match the new folder name. SE resolves worlds by internal name, not folder name.

## Building

### Prerequisites

- Visual Studio 2022 (Community or Professional) or MSBuild
- Space Engineers installed via Steam

### Setup

Edit `build-config.json` to point to your Space Engineers `Bin64` folder:

```json
{
  "seBin64": "D:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Bin64"
}
```

### Build & Deploy

```powershell
.\build.ps1
```

The script will:

1. Kill Space Engineers if running
2. Build the project in Release mode
3. Copy `AutoWorldLoader.dll` to `<Bin64>\Plugins\`
4. Register the plugin in the PluginLoader's `config.xml`

## Logging

Debug output is written to `%APPDATA%\SpaceEngineers\AutoWorldLoader.log`.

View the last 20 lines from PowerShell:

```powershell
Get-Content "$env:APPDATA\SpaceEngineers\AutoWorldLoader.log" -Tail 20
```

Or tail it live (updates every second):

```powershell
Get-Content "$env:APPDATA\SpaceEngineers\AutoWorldLoader.log" -Wait -Tail 20
```

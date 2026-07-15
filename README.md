# AutoWorldLoader

A [Space Engineers](https://www.spaceengineersgame.com/) plugin that automatically loads a saved world when the game reaches the main menu.

## How it works

The plugin waits for the main menu to appear, then calls the game's internal session loader to jump straight into the configured world — no manual clicking required.

## Configuration

Create a file at `%APPDATA%\SpaceEngineers\AutoWorldLoader.json`:

```json
{
  "worldName": "My Save Name"
}
```

The save must exist under `%APPDATA%\SpaceEngineers\Saves\<SteamID64>\<worldName>`.

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

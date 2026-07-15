# AutoWorldLoader

A [Space Engineers](https://www.spaceengineersgame.com/) plugin that automatically loads a saved world when the game reaches the main menu.

## How it works

The plugin waits for the main menu to appear (polling every second after an initial 3-second delay), then calls the game's internal session loader to jump straight into the configured world — no manual clicking required.

## Architecture

```
Plugin.cs                        ← Presentation (IPlugin entry point)
├── Domain/                      ← Pure C#, no game deps
│   ├── WorldTemplate.cs         Enum
│   ├── IWorldTemplate.cs        Interface
│   ├── EmptyWorldTemplateImpl.cs Concrete template
│   ├── WorldTemplateRegistry.cs Dictionary<enum, IWorldTemplate>
│   ├── PluginConfig.cs          Config data model
│   └── PluginConsts.cs          Constants
├── Application/                 ← Orchestration & services
│   ├── WorldLoader.cs           World loading / copying / cleanup
│   ├── WorldSession.cs          IDisposable session (auto-cleanup)
│   └── PluginConfigReader.cs    JSON config parser
└── Infrastructure/              ← External concerns
    └── PluginLog.cs             File logger
```

### WorldLoader API

`WorldLoader` is a `public static` class — call it from any plugin or in-game script:

```csharp
// ── Load existing saves ──────────────────────────────────

WorldLoader.LoadByName("My Save Name");

// Direct path (for saves in non-standard locations)
WorldLoader.LoadByName(@"C:\CustomSaves\MyWorld", isCustomPath: true);
WorldLoader.LoadByPath(@"C:\Saves\123456789\MyWorld");

// Resolve path without loading (returns false if not found)
if (WorldLoader.TryResolveSavePath("My Save Name", out var path))
{
    // path is valid
}

// ── Launch from template ─────────────────────────────────

// Copy template → Saves, fix .sbc, launch
WorldLoader.LaunchFromTemplate(WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods, "TestRun_001");

// ── Disposable session (auto-cleanup) ─────────────────────

// Copy, launch, and auto-delete when done
using (var session = WorldLoader.CreateSession(
    WorldTemplate.EmptyWorld_NoLimits_WithPB_NoMods, "TestRun_002"))
{
    // world is loaded — run your tests
} // ← world deleted here

// From an arbitrary folder (no enum required)
using (var session = WorldLoader.CreateSession(@"C:\MyTemplates\MyWorld", "TestRun_003"))
{
    // ...
}

// ── Cleanup ──────────────────────────────────────────────

WorldLoader.Cleanup("TestRun_001");
```

## Configuration

Create `%APPDATA%\SpaceEngineers\AutoWorldLoader.json`:

### Direct mode — load an existing save

```json
{
  "worldName": "My Save Name"
}
```

### Template mode — copy a template world and load the copy

```json
{
  "template": "EmptyWorld_NoLimits_WithPB_NoMods",
  "targetName": "TestRun_001",
  "cleanupOnDispose": false
}
```

| Key | Required | Default | Description |
|---|---|---|---|
| `template` | yes | — | `WorldTemplate` enum name (case-insensitive) |
| `targetName` | no | template name | Folder name for the copy under Saves |
| `cleanupOnDispose` | no | `false` | Delete the copy when the plugin unloads |

> **Note:** `worldName` and `template` are mutually exclusive. If both are set, `template` wins.

### Available templates

| Template | Description |
|---|---|
| `EmptyWorld_NoLimits_WithPB_NoMods` | Empty world, no limits, programmable block on, no mods |

### Adding custom templates

Templates live in two places:
- **Repo:** `Templates/` folder in this project — versioned, ships with the plugin.
- **Runtime:** `%APPDATA%\SpaceEngineers\AutoWorldLoader\Templates\` — for personal templates.

To make a template from an existing world, copy its save folder and update these fields in the `.sbc` files:

| File | Element | What to change |
|---|---|---|
| `Sandbox.sbc` | `<SessionName>` | Set to the template folder name |
| `Sandbox_config.sbc` | `<SessionName>` (or `<WorldName>`) | Same as above |

> `LaunchFromTemplate` and `CreateSession` do this automatically. You only need to fix these manually if you copy worlds by hand.

Two ways to register a template:

#### Option A — Filesystem only, no recompile

Drop world files into the Templates folder and call `LoadByName` with `isCustomPath`:

```powershell
# 1. Copy your world into the templates folder
Copy-Item -Recurse "C:\MySaves\MyCustomWorld" `
    "$env:APPDATA\SpaceEngineers\AutoWorldLoader\Templates\MyCustomWorld"
```

```csharp
// 2. Load it from code (or another plugin)
var templatePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "SpaceEngineers", "AutoWorldLoader", "Templates", "MyCustomWorld");

WorldLoader.LaunchFromTemplate(templatePath, "TestRun_002");
```

#### Option B — Add to enum (recompile, full integration)

1. Copy world files to `%APPDATA%\SpaceEngineers\AutoWorldLoader\Templates\MyCustomWorld\`
2. Add a value to `WorldTemplate.cs`, implement `IWorldTemplate`, and register it:

```csharp
// 1. Add enum value
public enum WorldTemplate
{
    None,
    EmptyWorld_NoLimits_WithPB_NoMods,
    MyCustomWorld   // ← new
}

// 2. Create implementation
internal sealed class MyCustomWorldImpl : IWorldTemplate
{
    public string FolderName => "MyCustomWorld";
}
3. Register it in the dictionary

```csharp
// Option A — static registration (edit WorldTemplateRegistry.cs):
//   Add: [WorldTemplate.MyCustomWorld] = new MyCustomWorldImpl()

// Option B — runtime registration (from another plugin, no recompile):
WorldTemplateRegistry.Register(
    WorldTemplate.MyCustomWorld,
    new MyCustomWorldImpl());
```

3. Rebuild. Now usable from both code and JSON config:

```csharp
WorldLoader.LaunchFromTemplate(WorldTemplate.MyCustomWorld, "TestRun_003");
```

```json
{ "template": "MyCustomWorld", "targetName": "TestRun_003" }
```

> **Important:** `LaunchFromTemplate` automatically updates `<SessionName>` in `Sandbox.sbc` and `Sandbox_config.sbc`. If you copy a world manually, you must update those files yourself.

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
5. Copy `Templates\` to `%APPDATA%\SpaceEngineers\AutoWorldLoader\Templates\`

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

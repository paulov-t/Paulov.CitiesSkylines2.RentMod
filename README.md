# Paulov's Rent Mod

A [Cities: Skylines 2](https://paradoxplaza.com/cities-skylines-2) code mod that multiplies residential property rent by a configurable factor.

Built with the official [CS2 Modding Toolchain](https://cs2.paradoxwikis.com/Modding_Toolchain).

## Requirements

- [Cities: Skylines 2](https://store.steampowered.com/app/949230/Cities_Skylines_II/) (Steam or Game Pass)
- [Modding Toolchain](https://cs2.paradoxwikis.com/Modding_Toolchain) installed via **Options → Modding** in-game (installs Unity, .NET SDK, and the mod project template)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community or Pro) or [JetBrains Rider](https://www.jetbrains.com/rider/)

## Setup

### 1. Install the Modding Toolchain

1. Launch Cities: Skylines 2
2. Go to **Options → Modding** and let the toolchain install all dependencies (Unity, .NET SDK, Node.js)
3. Restart the game after installation completes

### 2. Set Up the Mod

1. Open a terminal in the project root:
   ```powershell
   cd G:\Work\CitiesSkylinesModding\PaulovRentMod
   ```
2. Open the solution in Visual Studio or Rider
3. The project reads game DLL paths from environment variables set by the toolchain (`CSII_INSTALLATIONPATH`, `CSII_MANAGEDPATH`, etc.)

### 3. Build

**In Visual Studio / Rider:**
- Select **Build → Build Solution** (or press `Ctrl+Shift+B`)
- The post-build step (`Mod.targets`) runs `ModPostProcessor.exe` and copies the output to `%LOCALAPPDATA%\Low\Colossal Order\Cities Skylines II\Mods\PaulovRentMod`

**From command line:**
```powershell
dotnet build
```

### 4. Run

Launch the game. The mod will appear in the **Mods** menu. Enable it and adjust settings under **Options → Paulov's Rent Mod**.

## Configuration

Settings are available directly in the in-game Options UI:

| Setting | Description | Default |
|---------|-------------|---------|
| ModEnabled | Enable or disable the rent multiplier | true |
| RentMultiplier | Rent multiplier (1.0–10.0, step 0.5) | 2.0 |

Settings are persisted automatically via the game's settings system.

## Project Structure

```
PaulovRentMod/
├── Mod.cs                          # Main entry point (IMod interface)
├── Setting.cs                      # Options UI + localization
├── PaulovRentMod.csproj            # MSBuild project (imports Mod.props/targets)
├── Properties/
│   └── PublishConfiguration.xml    # Paradox Mods publish metadata
├── Systems/
│   └── RentIncreaseSystem.cs       # ECS system scaffold (TODO)
├── CHANGELOG.md
└── README.md
```

## How to Implement the Rent Logic

The `RentIncreaseSystem` is a scaffold. To actually modify rent:

1. **Decompile `Game.dll`** from `Cities2_Data\Managed\` using [dnSpy](https://github.com/dnSpy/dnSpy) or [ILSpy](https://github.com/icsharpcode/ILSpy)
2. Find the rent component (search for `Rent`, `RentData`, or `m_Rent` in `Game.Economy`)
3. Find the system that calculates or writes rent values (likely `RentSystem`, `EconomySystem`, or similar)
4. Either:
   - Write a HarmonyX patch on the rent-calculating method, **or**
   - Query residential building entities in `RentIncreaseSystem.OnUpdate()` and modify the rent component directly

## Testing

### In-game verification

1. Load a save with residential buildings
2. Open the economy panel to see base rent values
3. Go to **Options → Paulov's Rent Mod**, confirm `ModEnabled = ON` and `RentMultiplier > 1`
4. Check that displayed rent values have increased

### Logging

Game/mod logs are at:
```
%LOCALAPPDATA%\Low\Colossal Order\Cities Skylines II\Logs\*
```
Filter for `PaulovRentMod`:
```powershell
Select-String -Path "$env:LOCALAPPDATA\Low\Colossal Order\Cities Skylines II\Logs\*.log" -Pattern "PaulovRentMod"
```

### Developer Mode

Add `--developerMode` to the game's launch options (Steam → Properties → Launch Options). Press **Tab** in-game to open the developer UI — useful for inspecting entities and debugging ECS components.

## Publishing to Paradox Mods

1. Ensure you're logged into your Paradox account in-game
2. In `Properties\PublishConfiguration.xml`, set `<ModId Value="" />` to empty for first publish
3. In Visual Studio: right-click the project → **Publish → PublishNewMod**
4. Copy the returned ModID from the console into `PublishConfiguration.xml`
5. For updates, use **Publish → PublishNewVersion**

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Build fails — missing references | Ensure the toolchain is fully installed in-game (Options → Modding). Check that `CSII_TOOLPATH` env var exists. |
| Mod not showing in-game | Verify the built DLL is in `%LOCALAPPDATA%\Low\Colossal Order\Cities Skylines II\Mods\PaulovRentMod\` |
| Harmony patches not applying | Check logs for patch errors. Ensure the method signature matches the game version exactly. |
| Settings not appearing | The `Setting` class and `LocaleEN` must be registered in `Mod.OnLoad` as shown. |

## License

MIT

# Dub's Heat Pumps

A RimWorld mod that adds realistic heat pump systems to Dub's Bad Hygiene climate control framework.

[![RimWorld](https://img.shields.io/badge/RimWorld-1.5%20|%201.6-blue.svg)](https://rimworldgame.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## Features

- **Split-type heat pump system** with indoor and outdoor units
- **Automatic mode switching** between heating and cooling based on room temperature
- **Realistic temperature limitations** - heating stops working below -25°C (-13°F) outdoor temperature
- **Intelligent operation** - switches modes when room temperature deviates from target by 2°C threshold
- **Efficient climate control** - provides both heating and cooling with a single system
- **Dedicated duct network** - uses separate heat pump ducts (not compatible with regular AC ducts)
- **Seamless integration** with Dub's Bad Hygiene HVAC network

## How It Works

The heat pump indoor unit automatically switches between heating and cooling modes:
- When room temperature is **below target - 2°C**: switches to **heating mode**
- When room temperature is **above target + 2°C**: switches to **cooling mode**
- When outdoor temperature drops **below -25°C (-13°F)**: heating mode **disabled** (realistic heat pump limitation)

The outdoor unit automatically reverses operation to match:
- **Cooling mode**: outdoor unit exhausts heat outside
- **Heating mode**: outdoor unit absorbs heat from outside air (only when outdoor temp ≥ -25°C)

**Important:** Heat pumps use a separate duct network from regular air conditioners. This prevents unrealistic scenarios where one unit heats while another cools on the same outdoor unit. All indoor units connected to the same outdoor unit will operate in the same mode.

## Requirements

- **[Dub's Bad Hygiene](https://steamcommunity.com/sharedfiles/filedetails/?id=836308268)** (required)
- RimWorld 1.5 or 1.6

## Installation

### Steam Workshop
1. Subscribe to this mod on Steam Workshop
2. Subscribe to [Dub's Bad Hygiene](https://steamcommunity.com/sharedfiles/filedetails/?id=836308268)
3. In mod manager, load Dub's Bad Hygiene BEFORE Dub's Heat Pumps
4. Start/continue your game

### Manual Installation
1. Download the latest release from [Releases](https://github.com/Sh0tybumbati/dubs-heatpump/releases)
2. Extract to `RimWorld/Mods/` folder
3. Enable in mod manager after Dub's Bad Hygiene

## Research & Crafting

**Research Required:** Heat Pumps (unlocked after Air Conditioning research)
- Research cost: 3000
- Tech level: Industrial

**Costs:**
- **Heat Pump Outdoor Unit**: 90 Steel, 5 Industrial Components (requires Construction 7)
- **Heat Pump Indoor Unit**: 25 Steel (requires Construction 6)

## Building from Source

### Prerequisites
- Visual Studio 2019+ or Visual Studio Code with C# extension
- .NET Framework 4.7.2
- RimWorld installed
- Dub's Bad Hygiene mod installed

### Compilation Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Sh0tybumbati/dubs-heatpump.git
   cd dubs-heatpump
   ```

2. **Update assembly references:**

   Edit `Source/DubsHeatPumps/DubsHeatPumps.csproj` and update these paths to match your installation:

   **Windows (Steam):**
   ```xml
   <HintPath>C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll</HintPath>
   <HintPath>C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll</HintPath>
   <HintPath>C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\836308268\Current\Assemblies\DubsBadHygiene.dll</HintPath>
   ```

   **Linux (Steam):**
   ```xml
   <HintPath>~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed/Assembly-CSharp.dll</HintPath>
   <HintPath>~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed/UnityEngine.CoreModule.dll</HintPath>
   <HintPath>~/.steam/steam/steamapps/workshop/content/294100/836308268/Current/Assemblies/DubsBadHygiene.dll</HintPath>
   ```

3. **Build:**

   **Visual Studio:**
   - Open `DubsHeatPumps.csproj` in Visual Studio
   - Build → Build Solution (F6)

   **Command Line (MSBuild):**
   ```bash
   cd Source/DubsHeatPumps
   msbuild DubsHeatPumps.csproj /p:Configuration=Release
   ```

   **Command Line (dotnet):**
   ```bash
   cd Source/DubsHeatPumps
   dotnet build -c Release
   ```

4. **Install:**
   - The compiled `DubsHeatPumps.dll` will be in `Assemblies/`
   - Copy the entire `dubs-heatpump` folder to `RimWorld/Mods/`

## Textures

Currently uses placeholder textures from Dub's Bad Hygiene. Custom textures may be added in future updates!

## Compatibility

- **Safe to add to existing saves:** Yes
- **Safe to remove from saves:** Requires removing all heat pump buildings first
- **Compatible with:** Most mods that don't drastically alter temperature systems
- **Multiplayer compatible:** Should work (untested)

## Known Issues

None currently. Please report issues on [GitHub Issues](https://github.com/Sh0tybumbati/dubs-heatpump/issues).

## Future Plans

- Custom textures for heat pump units
- Additional heat pump variants (e.g., industrial-size units)
- Efficiency degradation curve as outdoor temperature drops
- Integration with more HVAC mods

## Contributing

Pull requests welcome! Feel free to:
- Report bugs
- Suggest features
- Submit code improvements
- Add translations

## License

MIT License - See [LICENSE](LICENSE) file for details

## Credits

- **Sh0tybumbati** - Mod creator
- **Dubwise** - Creator of [Dub's Bad Hygiene](https://github.com/Dubwise56/Dubs-Bad-Hygiene)
- Built on the excellent HVAC framework from DBH

## Links

- **GitHub:** https://github.com/Sh0tybumbati/dubs-heatpump
- **Steam Workshop:** (coming soon)
- **Dub's Bad Hygiene:** https://steamcommunity.com/sharedfiles/filedetails/?id=836308268

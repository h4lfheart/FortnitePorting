<div align="center">

# <img src="FortnitePorting/Assets/LogoRebrand.png" width="48" height="48" style="margin-bottom: 6px; margin-right: 4px;" alt="Fortnite Porting logo" align="center" /> Fortnite Porting


[![Discord](https://img.shields.io/discord/866821077769781249?logo=discord&logoColor=white&label=Discord&color=7289da)](https://discord.gg/fortniteporting)
[![Blender](https://img.shields.io/badge/Blender-5.0+-blue?logo=blender&logoColor=white&color=orange)](https://www.blender.org/download/)
[![Unreal](https://img.shields.io/badge/Unreal-5.8-blue?logo=unreal-engine&logoColor=white&color=white)](https://www.unrealengine.com/en-US/download)
[![Release](https://img.shields.io/github/release/h4lfheart/FortnitePorting)](../../releases/latest)
[![Downloads](https://img.shields.io/github/downloads/h4lfheart/FortnitePorting/total?color=green)](../../releases)

<img alt="Fortnite Porting" src=".github/cover.png" />

</div>

## Features

- **Browse Fortnite assets** - Explore cosmetics, props, gameplay items, and more through a purpose-built interface.
- **Export directly to creative tools** - Send assets to Blender or Unreal Engine through companion plugins managed inside the app.
- **Work with complete environments** - Export maps, actors, landscapes, foliage, and more.
- **Choose your workflow** - Use an installed copy of Fortnite or load assets through On-Demand mode.
- **Preview before exporting** - Inspect models, materials, textures, audio, and raw file properties.
- **Automate the setup** - Fetch required AES keys and mappings automatically, with configurable export settings.

## Requirements

- Windows x64
- A local Fortnite installation or On-Demand mode
- [Blender 5.0+](https://www.blender.org/download/) and/or [Unreal Engine 5.8+](https://www.unrealengine.com/en-US/download) for live import

Blender and Unreal Engine are only required when exporting directly to those applications. Assets can also be exported to a folder.

> [!NOTE]
> Community-maintained forks are available for other platforms: [FortnitePortingMac](https://github.com/skythumbnails/FortnitePortingMac) and [FortnitePorting-linux](https://github.com/fclivaz42/FortnitePorting-linux). These are not officially supported by this repository.

## Installation

Download the latest release from [GitHub Releases](../../releases/latest).

> [!IMPORTANT]
> Direct export to **Blender** and **Unreal Engine** requires their respective companion plugins. Install and manage them from the **Plugin** Tab inside Fortnite Porting.

## Quick Start

1. Download and launch `FortnitePorting.exe`.
2. Complete the initial setup using **Latest Installed**, **On-Demand**, or a **Custom** installation.
3. If exporting to Blender or Unreal Engine, install the appropriate companion plugin from the **Plugin** Tab.
4. Open the **Assets** Tab, select an item, choose an export target, and export.

## Export Targets

| Target | Details |
| --- | --- |
| **Blender** | Live import through the Fortnite Porting Blender extension |
| **Unreal Engine** | Live import through the Fortnite Porting and UEFormat plugins |
| **Assets Folder** | Offline export to the default Fortnite Porting assets directory while maintaining folder structure |
| **Custom Folder** | Offline export to a directory of your choice without maintaining folder structure |

---

## Building from Source

Fortnite Porting requires the [.NET SDK](https://dotnet.microsoft.com/download) and currently targets Windows x64.

Clone the repository with its submodules:

```sh
git clone https://github.com/h4lfheart/FortnitePorting --recursive
```

From the repository root, publish a self-contained build:

```sh
dotnet publish FortnitePorting -c Release --self-contained -r win-x64 -o "./Release" -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:IncludeNativeLibrariesForSelfExtract=true
```

## Links

- [Website](https://fortniteporting.app)
- [Discord](https://discord.gg/fortniteporting)
- [GitHub](https://github.com/h4lfheart/FortnitePorting)
- [X / Twitter](https://twitter.com/FortnitePorting)
- [Support on Ko-fi](https://ko-fi.com/h4lfheart)

## License

Fortnite Porting is distributed under the [GNU General Public License v3.0](LICENSE).

Fortnite Porting is an independent project and is not affiliated with, endorsed by, or sponsored by Epic Games. Fortnite and related marks are trademarks of Epic Games. You are responsible for ensuring that your use of exported assets complies with applicable licenses and terms.

---

## Contributors

- [Chippy](https://github.com/Bmarquez1997) - Material system and export feature development.
- [Ghost](https://github.com/GhostScissors) - RADA and BINKA audio decoders and asset deserialization fixes.
- [Asval](https://github.com/4sval) - Inspiration for Fortnite Porting and a primary contributor to [CUE4Parse](https://github.com/FabianFG/CUE4Parse).
- [GMatrix](https://github.com/GMatrixGames) - AES key and mapping infrastructure through UEDB.
- [Marcel](https://github.com/Ka1serM) - Unreal Engine and UEFormat plugins, world export features, and project inspiration.
- [MountainFlash](https://github.com/MinshuG) - Inspiration for the project's early automation.
- [RedHaze](https://github.com/RedHaze) - Pose Asset processing and UEFormat pose export groundwork.

Thank you to everyone who has contributed to Fortnite Porting throughout its development!!

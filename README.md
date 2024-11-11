FortnitePorting - Automation of the Fortnite Porting Process
------------------------------------------

#### Powered by [Avalonia UI](https://avaloniaui.net/) and [CUE4Parse](https://github.com/FabianFG/CUE4Parse)

[![Discord](https://discord.com/api/guilds/866821077769781249/widget.png?style=shield)](https://discord.gg/DZ5YFXdBA6)
[![Blender](https://img.shields.io/badge/Blender-4.2+-blue?logo=blender&logoColor=white&color=orange)](https://www.blender.org/download/)
[![Unreal](https://img.shields.io/badge/Unreal-5.4+-blue?logo=unreal-engine&logoColor=white&color=white)](https://www.unrealengine.com/en-US/download)
[![Release](https://img.shields.io/github/release/halfuwu/FortnitePorting)]()
[![Downloads](https://img.shields.io/github/downloads/halfuwu/FortnitePorting/total?color=green)]()
***

![image](https://github.com/user-attachments/assets/960d0dc5-695f-43b6-be0c-db74efdf1a17)

## Building FortnitePorting

To build FortnitePorting from source, first clone the repository and all of its submodules.

```
git clone -b v3 https://github.com/halfuwu/FortnitePorting --recursive
```

Then open the project directory in a terminal window and publish

```
dotnet publish FortnitePorting -c Release --no-self-contained -r win-x64 -o "./Release" -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:IncludeNativeLibrariesForSelfExtract=true
```

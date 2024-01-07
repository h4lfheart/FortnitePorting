FortnitePorting - Automation of the Fortnite Porting Process
------------------------------------------

[![Discord](https://discord.com/api/guilds/866821077769781249/widget.png?style=shield)](https://discord.gg/DZ5YFXdBA6)
[![Blender](https://img.shields.io/badge/Blender-4.0+-blue?logo=blender&logoColor=white&color=orange)](https://www.blender.org/download/ )
[![Unreal](https://img.shields.io/badge/Unreal-5.3+-blue?logo=unreal-engine&logoColor=white&color=white)](https://www.unrealengine.com/en-US/download)
***

![image](https://github.com/halfuwu/FortnitePorting/assets/69497698/9b0ee275-21de-4e52-aafe-847b0231a9aa)

## Building FortnitePorting

To build FortnitePorting from source, first clone the repository and all of its submodules.

```
git clone -b avalonia-2.0 https://github.com/halfuwu/FortnitePorting --recursive
```

Then open the project directory in a terminal window and run this command

```
dotnet publish FortnitePorting -c Release --no-self-contained -r win-x64 -o "./Release" -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:IncludeNativeLibrariesForSelfExtract=true
```

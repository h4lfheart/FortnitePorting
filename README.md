FortnitePorting - Automation of the Fortnite Porting Process
------------------------------------------

[![Discord](https://discord.com/api/guilds/866821077769781249/widget.png?style=shield)](https://discord.gg/DZ5YFXdBA6)
[![Blender](https://img.shields.io/badge/Blender-4.0+-blue?logo=blender&logoColor=white&color=orange)](https://www.blender.org/download/ )
[![Unreal](https://img.shields.io/badge/Unreal-5.3+-blue?logo=unreal-engine&logoColor=white&color=white)](https://www.unrealengine.com/en-US/download)
***

![image](https://github.com/halfuwu/FortnitePorting/assets/69497698/71d8264d-e25d-4e6e-9793-661c7d945e1b)


## Building FortnitePorting

To be able to build FortnitePorting from source, first clone the repository and all of its submodules.

```
git clone -b avalonia-2.0 https://github.com/halfuwu/FortnitePorting --recursive

```

Then open the project directory in a terminal window and run this command

```
dotnet publish FortnitePorting.Desktop -c Release --no-self-contained -r win-x64 -o "./Release" -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:IncludeNativeLibrariesForSelfExtract=true

```

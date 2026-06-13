# Geometry Heightmap Exporter

This exporter rasterizes Fortnite world geometry inside FortnitePorting. It collects landscape, actor, instanced foliage, and optional HLOD meshes, projects them into a top-down grid, and writes a 16-bit PNG heightmap file in various resolutions.

## Export Usage

- Configure `Include Actors`, `Include Spawn`, and the resolution, then hit Generate.
- The exporter automatically includes the main level and all map chunks. Manual grid selection is not required.
- Terrain is always included for heightmap bounds. Enable `HLODs` only if proxy meshes are needed.
- The exported image will be generated, saved, and revealed in your file explorer.

## Options

- Geometry is automatically clipped to terrain bounds when landscape geometry is available, preventing far away meshes from drastically zooming out the heightmap.
- `Include Actors` includes buildings, trees, props, and other placed objects in `heightmap.png`. Turn it off to export only the terrain as `terrainmap.png`.
- `Include Spawn` includes detected Spawn Island packages and disables any crop-to-main-island functions. Enabled = include spawn island, Disabled = no spawn island

## Filtering

The collector tries to remove known visual/helper meshes before rasterization, including flat fog/water sheets, background landscape meshes, visual-effect meshes, etc. This prevents these non-collision assets from polluting the heightmap.

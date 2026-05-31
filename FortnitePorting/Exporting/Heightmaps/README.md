# Geometry Heightmap Exporter

This exporter rasterizes Fortnite world geometry inside FortnitePorting. It collects landscape, actor, instanced foliage, and optional HLOD meshes, projects them into a top-down grid, and writes a 16-bit PNG heightmap file in various resolutions.

## Export Usage

- Select the intended grid cells for the export.
- Enable `Inlude Main Level` AFTER selecing the grid cells.
- Keep `Actors`, `Instanced & Foliage Actors`, and `Landscape` enabled, with `HLODs` disabled.
- Configure `Spawn` and `Terrain`, then hit Generate.
- The resultant image will be generated, saved, and revealed in the your file explorer.

## Options

- Geometry is automatically clipped to terrain bounds when landscape geometry is available, preventing far away meshes from drastically zooming out the heightmap.
- `Spawn` includes detected Spawn Island packages and disables any crop-to-main-island functions. Enabled = include spawn island, Disabled = no spawn island
- `Terrain` writes `terrainmap.png` alongside the heightmap to the export folder. The terrainmap doesent get populated with buildings,trees,etc - so only represents terrain.

## Filtering

The collector tries to remove known visual/helper meshes before rasterization, including flat fog/water sheets, background landscape meshes, visual-effect meshes, etc. This prevents these non-collision assets from polluting the heightmap.
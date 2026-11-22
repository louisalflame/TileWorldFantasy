# TileWorldFantasy
a turn-based rogue-like  strategy game , generate random world

## Development

For project handoff, architecture, completed fixes and remaining work, read
[AGENTS.md](AGENTS.md) and [the project status](docs/PROJECT_STATUS.md).

- Unity Editor: **6000.0.76f1**.
- Open **Editor > WorldMapEditor** to configure and generate a world.
- Biome tables use humidity, height, then temperature. Resizing preserves overlapping
  cells; assign biomes to newly added cells before generating a world. Shrinking a
  dimension removes cells outside the new bounds.

## Layered terrain

In **WorldMapEditor > New Map > Parameter**, expand **地形分層與外海設定**.
The parameter list scrolls, and all terrain field tooltips are in Traditional Chinese.
`USE_LAYERED_TERRAIN` defaults to true; turn it off to compare the legacy terrain.
Existing assets are not rewritten to enable the new mode: missing fields use code
defaults until you explicitly edit/save the parameter asset.

- `CONTINENTS`: continental elevation, centered on sea level. Lower
  `LAND_THRESHOLD` for more land; this is a threshold, not an exact land percentage.
- `MOUNTAIN_REGIONS`: smooth mountain mask controlled by `MOUNTAIN_THRESHOLD`
  and `MOUNTAIN_BLEND`. Disabling it allows mountains across all land.
- `RIDGES`: ridged fractal noise, controlled by mountain height and ridge sharpness.
- `DETAILS`: small signed surface changes, with separate plain and mountain amplitudes.

Expand each noise layer to set its enable switch, wavelength, octave count,
frequency multiplier, and amplitude persistence. `WORLD_SCALE` (default 256) defines
a fixed reference domain. Wavelengths and coast widths divided by `WORLD_SCALE`
are fractions of each map side, independent of output resolution. For example,
wavelength 160 spans 62.5% of the world at the default reference scale.
Keep the seed, reference scale and terrain parameters fixed, then increase Width /
Height to sample the same world more finely. Small previews can miss fine islands
and ridges. Both endpoints are sampled (`x / (width - 1)` and `y / (height - 1)`),
so all four edges remain ocean. Changing the aspect ratio stretches the same
normalized world; keep the aspect ratio for a visually matching preview.
Disabling continents uses `BASE_LAND_HEIGHT`; disabling ridges/details removes their
height contribution. Sea beds skip mountain/detail sampling. The land blend height
fades mountains/details near the waterline. Outer ocean rules apply in either mode.
Legacy local uplift and power curves are only used with layered terrain disabled.
Changing generation modes changes seed output; saved maps are left intact.

## Ocean border and coast

Ocean fields are in the same **地形分層與外海設定** section.
The fields edit the assigned terrain parameter asset and are also available in its
Inspector. Sea level defaults to 0.23 and the fixed ocean floor to 0.05. Border width
3 is 3/256 (about 1.17%) of each side, followed by a 20/256 smooth transition,
extended inland by up to 8/256 of seeded coast noise (40/256 wavelength).
Set noise amplitude to zero for an unperturbed transition. Increasing resolution
keeps these proportions; it no longer makes the fixed-width coast relatively thinner.
If the coast proportions leave no unaffected interior, reduce their widths rather
than increasing resolution. A one-cell-wide dimension is entirely ocean.

Beyond the transition, heights retain the base terrain without radial falloff.
Sea level controls continental elevation and validates the floor height; biome height ranges, preview color ranges,
and river end height remain separate settings. Keep these consistent when changing
the waterline. Legacy surround-down values are retained but no longer used.
The previous tile-based settings retain their stored numbers but now use reference
world units; parameter assets are not automatically rewritten. New generation with
an old seed changes; existing map saves are not rewritten. Cross-resolution equality
applies to layered terrain at matching normalized coordinates. Rivers and population
are grid-based and do not have this guarantee.

## Map preview size

In **WorldMapEditor > View Map**, choose a map type, then use **縮放（%）**
(1–1600%) or enter **顯示寬度 / 顯示高度** and press Enter. Width and height
stay linked to the map's aspect ratio. **符合視窗** follows the available viewport;
**1:1** displays one map cell per Editor display point. Scrollbars reveal maps
larger than the viewport. **清晰格子** switches between sharp cells and smooth
interpolation. These controls affect display only and do not regenerate the map
or change its resolution. Control tooltips are in Traditional Chinese.

## Saved maps

Maps are stored as versioned JSON in `Application.persistentDataPath/world-map.json`.
Saving validates the map and flushes a temporary file before replacing the current
save. The previous save is kept as `world-map.json.bak`; loading falls back to that
backup if the main file is invalid. Save/load errors appear in the world editor.

If no save file or backup exists, the legacy `SAVE_KEY` PlayerPrefs map can be loaded
and migrated. The legacy preference is retained if migration fails or succeeds.

## Regression tests

Run `WorldGenerationRegressionTests` as EditMode tests in Unity's Test Runner.
The tests cover rectangular rivers, generation boundaries, preserving biome and
population settings when resizing, mana point counts, save validation, backups,
legacy migration, and reproducible rectangular worlds using the bundled assets.
File tests use temporary directories and do not overwrite normal saved maps.

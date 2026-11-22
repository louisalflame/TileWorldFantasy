# TileWorldFantasy
a turn-based rogue-like  strategy game , generate random world

## Development

- Unity Editor: **6000.0.76f1**.
- Open **Editor > WorldMapEditor** to configure and generate a world.
- Biome tables use humidity, height, then temperature. Resizing preserves overlapping
  cells; assign biomes to newly added cells before generating a world. Shrinking a
  dimension removes cells outside the new bounds.

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

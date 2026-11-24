using System;
using UnityEngine;

public static class MapSaveValidation
{
    public static void Validate(TileDataUnit data)
    {
        if (data == null) throw new ArgumentNullException("data");
        var count = GenerationValidation.CellCount(data.Width, data.Height);
        GenerationValidation.Map(data.Map, count, "saved tiles");
        GenerationValidation.Map(data.Populations, count, "saved populations");
        for (var i = 0; i < count; i++)
        {
            var tile = data.Map[i];
            if (tile == null) throw new ArgumentException("Saved tiles must not be null.");
            GenerationValidation.UnitValue(tile.Height, "saved height");
            GenerationValidation.UnitValue(tile.Humidity, "saved humidity");
            GenerationValidation.UnitValue(tile.Temperature, "saved temperature");
            GenerationValidation.UnitValue(tile.River, "saved river");
            GenerationValidation.Finite(tile.Mana, "saved mana");
            if (!Enum.IsDefined(typeof(Biome), tile.Biome) || data.Populations[i] < 0)
                throw new ArgumentException("Saved biome or population is invalid.");
        }
    }
}

[Serializable]
public class TileDataUnit
{
    public TileUnit[] Map;
    public int[] Populations;
    public int Width;
    public int Height;
}

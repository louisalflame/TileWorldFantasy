using System;
using UnityEngine;

public static class GenerationValidation
{
    public static int CellCount(int width, int height)
    {
        if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue)
            throw new ArgumentOutOfRangeException("width", "Map dimensions must be positive and fit in an array.");
        return width * height;
    }

    public static void Map(Array map, int count, string name)
    {
        if (map == null || map.Length != count)
            throw new ArgumentException("Map length must match its dimensions.", name);
    }

    public static void Finite(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentException("Value must be finite.", name);
    }

    public static void UnitValue(float value, string name)
    {
        Finite(value, name);
        if (value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(name, "Value must be between 0 and 1.");
    }

    public static void Noise(int octaves, int growth)
    {
        if (octaves <= 0 || growth <= 0)
            throw new ArgumentException("Noise octave count and frequency growth must be positive.");
    }

    public static void Terrain(TerrainParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException("parameter");
        Positive(parameter.WORLD_SCALE, "世界基準尺度");
        if (parameter.USE_LAYERED_TERRAIN)
        {
            TerrainLayer(parameter.CONTINENTS);
            TerrainLayer(parameter.MOUNTAIN_REGIONS);
            TerrainLayer(parameter.RIDGES);
            TerrainLayer(parameter.DETAILS);
            UnitValue(parameter.LAND_THRESHOLD, "海陸門檻");
            UnitValue(parameter.CONTINENT_RELIEF, "大陸高度幅度");
            UnitValue(parameter.BASE_LAND_HEIGHT, "基準陸地高度");
            UnitValue(parameter.MOUNTAIN_THRESHOLD, "山區門檻");
            UnitValue(parameter.MOUNTAIN_HEIGHT, "山脈高度");
            UnitValue(parameter.PLAIN_DETAIL_HEIGHT, "平原細節幅度");
            UnitValue(parameter.MOUNTAIN_DETAIL_HEIGHT, "山區細節幅度");
            Positive(parameter.LAND_BLEND_HEIGHT, "海岸緩衝高度");
            Positive(parameter.MOUNTAIN_BLEND, "山區過渡範圍");
            Positive(parameter.RIDGE_SHARPNESS, "山稜尖銳程度");
        }
        else
        {
            Noise(parameter.FREQ_COUNT_TIMES, parameter.FREQ_GROW_FACTOR);
            RandomPoints(parameter.RANDOM_POINT_GEN_PARA);
            Positive(parameter.SCALE, "舊版地形頻率");
            Positive(parameter.LOW_GROUND_FACTOR, "舊版低地次方");
            Positive(parameter.HIGH_MOUNTAIN_FACTOR, "舊版高地次方");
        }
        UnitValue(parameter.SEA_LEVEL, "海平面");
        UnitValue(parameter.OCEAN_FLOOR_HEIGHT, "海底高度");
        if (parameter.OCEAN_FLOOR_HEIGHT >= parameter.SEA_LEVEL)
            throw new ArgumentException("海底高度必須嚴格低於海平面。");
        Finite(parameter.COAST_TRANSITION_WIDTH, "coast transition width");
        Finite(parameter.COAST_NOISE_AMPLITUDE, "coast noise amplitude");
        Finite(parameter.COAST_NOISE_SCALE, "coast noise scale");
        Finite(parameter.COAST_TRANSITION_WIDTH + parameter.COAST_NOISE_AMPLITUDE, "maximum coast width");
        if (parameter.OCEAN_BORDER_WIDTH < 1 || parameter.COAST_TRANSITION_WIDTH <= 0f ||
            parameter.COAST_NOISE_AMPLITUDE < 0f || parameter.COAST_NOISE_SCALE <= 0f)
            throw new ArgumentException("外海帶基準寬度至少為 1；過渡寬度與噪聲波長必須大於零，噪聲幅度不可為負數。");
    }

    private static void Positive(float value, string name)
    {
        Finite(value, name);
        if (value <= 0f) throw new ArgumentException(name + "必須大於零。");
    }

    public static void TerrainLayer(TerrainNoiseLayer layer)
    {
        if (layer == null) throw new ArgumentException("地形噪聲層不可為空。");
        if (!layer.ENABLED) return;
        Positive(layer.WAVELENGTH, "噪聲波長");
        Finite(1f / layer.WAVELENGTH, "噪聲頻率");
        UnitValue(layer.PERSISTENCE, "噪聲振幅保留比例");
        Finite(layer.LACUNARITY, "噪聲頻率倍數");
        if (layer.OCTAVES < 1 || layer.OCTAVES > 12 || layer.LACUNARITY < 1f || layer.LACUNARITY > 4f)
            throw new ArgumentException("地形噪聲層數必須為 1～12，頻率倍數必須為 1～4。");
        Finite(Mathf.Pow(layer.LACUNARITY, layer.OCTAVES - 1) / layer.WAVELENGTH, "最高噪聲頻率");
    }

    public static void Rain(RainParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException("parameter");
        UnitValue(parameter.HEIGHT_LOCATIONS, "river source fraction");
        UnitValue(parameter.RIVER_END_HEIGHT, "river end height");
        Finite(parameter.RIVER_AVERAGE_FACTOR, "river average factor");
        if (parameter.RIVER_LENGTH < 0 || parameter.RIVER_AVERAGE_FACTOR <= 0f)
            throw new ArgumentException("River length must be nonnegative and average factor must be positive.");
    }

    public static void Population(PopulationParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException("parameter");
        UnitValue(parameter.BasicProbability, "basic population probability");
        Probabilities(parameter.HeightProbability, parameter.HeightVariety, "height probabilities");
        Probabilities(parameter.HumidityProbability, parameter.HumidityVariety, "humidity probabilities");
        Probabilities(parameter.TemperatureProbability, parameter.TemperatureVariety, "temperature probabilities");
    }

    private static void Probabilities(float[] values, int count, string name)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(name, "Classification count must be positive.");
        Map(values, count, name);
        foreach (var value in values) UnitValue(value, name);
    }

    public static void Mana(ManaParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException("parameter");
        RandomPoints(parameter.RANDOM_POINT_GEN_PARA);
        var counts = new[] { parameter.POSITIVE_HIGH_POINTS_NUM, parameter.POSITIVE_MID_POINTS_NUM,
            parameter.POSITIVE_LOW_POINTS_NUM, parameter.NEGATIVE_HIGH_POINTS_NUM,
            parameter.NEGATIVE_MID_POINTS_NUM, parameter.NEGATIVE_LOW_POINTS_NUM };
        long total = 0;
        foreach (var count in counts)
        {
            if (count < 0) throw new ArgumentException("Mana point counts must be nonnegative.");
            total += count;
        }
        if (total > int.MaxValue) throw new ArgumentException("Too many mana points.");
        foreach (var radius in new[] { parameter.POSITIVE_HIGH_RADIUS, parameter.POSITIVE_MID_RADIUS,
            parameter.POSITIVE_LOW_RADIUS, parameter.NEGATIVE_HIGH_RADIUS,
            parameter.NEGATIVE_MID_RADIUS, parameter.NEGATIVE_LOW_RADIUS })
        {
            Finite(radius, "mana radius");
            if (radius < 0f) throw new ArgumentException("Mana radii must be nonnegative.");
        }
        foreach (var factor in new[] { parameter.POSITIVE_HIGH_FACTOR, parameter.POSITIVE_MID_FACTOR,
            parameter.POSITIVE_LOW_FACTOR, parameter.NEGATIVE_HIGH_FACTOR,
            parameter.NEGATIVE_MID_FACTOR, parameter.NEGATIVE_LOW_FACTOR }) Finite(factor, "mana factor");
    }

    public static void RandomPoints(RandomPointParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException("random point parameter");
        Noise(parameter.LOCAL_FREQ_COUNT_TIMES, parameter.LOCAL_FREQ_GROW_FACTOR);
    }
}

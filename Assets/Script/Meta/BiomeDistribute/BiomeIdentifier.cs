using System;
using UnityEngine;

public interface IBiomeIdentifier
{
    BiomeData IdentifyBiome(float humidity, float height, float temperature);
    int GetHeightVarietyIndex(float height);
    int GetTemperatureVarietyIndex(float temperature);
    int GetHumidityVarietyIndex(float humidity);
    bool IsHeightInVarietyIndex(float height, int index);
    bool IsTemperatureInVarietyIndex(float temperature, int index);
    bool IsHumidityInVarietyIndex(float humidity, int index);
}

public class BasicBiomeIdentifier : IBiomeIdentifier
{
    private readonly BiomeDistribution _distribution;
    private readonly float[] _humidityVariety;
    private readonly float[] _heightVariety;
    private readonly float[] _temperatureVariety;

    public BasicBiomeIdentifier(BiomeDistribution distribution)
    {
        if (distribution == null) throw new ArgumentNullException("distribution");
        _distribution = distribution;
        _humidityVariety = BuildRanges(distribution.HumidityRange, distribution.HumidityVariety, "humidity ranges");
        _heightVariety = BuildRanges(distribution.HeightRange, distribution.HeightVariety, "height ranges");
        _temperatureVariety = BuildRanges(distribution.TemperatureRange, distribution.TemperatureVariety, "temperature ranges");
        // Older assets contain surplus array entries. Validate the active region;
        // the inspector can trim surplus entries without changing active cells.
        if (distribution.BiomeHumiditys == null || distribution.BiomeHumiditys.Length < distribution.HumidityVariety)
            throw new ArgumentException("Biome humidity table is incomplete.");
        for (var h = 0; h < distribution.HumidityVariety; h++)
        {
            var heights = distribution.BiomeHumiditys[h];
            if (heights == null || heights.BiomeHeights == null || heights.BiomeHeights.Length < distribution.HeightVariety)
                throw new ArgumentException("Biome height table is incomplete.");
            for (var y = 0; y < distribution.HeightVariety; y++)
            {
                var temperatures = heights.BiomeHeights[y];
                if (temperatures == null || temperatures.BiomeTemperatures == null || temperatures.BiomeTemperatures.Length < distribution.TemperatureVariety)
                    throw new ArgumentException("Biome temperature table is incomplete.");
                for (var t = 0; t < distribution.TemperatureVariety; t++)
                    if (temperatures.BiomeTemperatures[t] == null)
                        throw new ArgumentException(string.Format("Assign the biome at humidity {0}, height {1}, temperature {2}.", h, y, t));
            }
        }
    }

    private static float[] BuildRanges(int[] weights, int count, string name)
    {
        if (count <= 0 || weights == null || weights.Length < count)
            throw new ArgumentException("Classification ranges must match a positive classification count.", name);
        double total = 0;
        for (var i = 0; i < count; i++)
        {
            if (weights[i] < 0) throw new ArgumentException("Range weights must be nonnegative.", name);
            total += weights[i];
        }
        if (total <= 0) throw new ArgumentException("At least one range weight must be positive.", name);
        var result = new float[count];
        double cumulative = 0;
        for (var i = 0; i < count; i++)
        {
            cumulative += weights[i];
            result[i] = (float)(cumulative / total);
        }
        return result;
    }

    BiomeData IBiomeIdentifier.IdentifyBiome(float humidity, float height, float temperature)
    {
        return _distribution.BiomeHumiditys[GetHumidityVarietyIndex(humidity)]
            .BiomeHeights[GetHeightVarietyIndex(height)]
            .BiomeTemperatures[GetTemperatureVarietyIndex(temperature)];
    }

    private static int Index(float value, float[] ranges)
    {
        GenerationValidation.UnitValue(value, "biome sample");
        for (var i = 0; i < ranges.Length; i++)
            if (value <= ranges[i]) return i;
        return ranges.Length - 1;
    }

    public int GetHeightVarietyIndex(float height) { return Index(height, _heightVariety); }
    public int GetTemperatureVarietyIndex(float temperature) { return Index(temperature, _temperatureVariety); }
    public int GetHumidityVarietyIndex(float humidity) { return Index(humidity, _humidityVariety); }
    public bool IsHeightInVarietyIndex(float height, int index) { return GetHeightVarietyIndex(height) == index; }
    public bool IsTemperatureInVarietyIndex(float temperature, int index) { return GetTemperatureVarietyIndex(temperature) == index; }
    public bool IsHumidityInVarietyIndex(float humidity, int index) { return GetHumidityVarietyIndex(humidity) == index; }
}

using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public interface IBiomeIdentifier
{
    BiomeData IdentifyBiome(
        float height,
        float temperature,
        float humidity
    );

    int GetHeightVarietyIndex(float height);
    int GetTemperatureVarietyIndex(float temperature);
    int GetHumidityVarietyIndex(float humidity);

    bool IsHeightInVarietyIndex(float height, int index);
    bool IsTemperatureInVarietyIndex(float temperature, int index);
    bool IsHumidityInVarietyIndex(float humidity, int index);
}

public class BasicBiomeIdentifier : IBiomeIdentifier
{
    private BiomeDistribution _distribution;
    private float[] _humudityVariety;
    private float[] _heightVariety;
    private float[] _temperatureVariety;

    public BasicBiomeIdentifier(BiomeDistribution distribution)
    {
        _distribution = distribution;
        int humuditySum = _distribution.HumidityRange.Sum(i => i);
        int heightSum = _distribution.HeightRange.Sum(i => i);
        int temperatureSum = _distribution.TemperatureRange.Sum(i => i);
         
        float totalHumudity = 0;
        _humudityVariety = new float[_distribution.HumidityVariety];
        for (var i = 0; i < _distribution.HumidityVariety; i++)
        {
            totalHumudity += _distribution.HumidityRange[i];
            _humudityVariety[i] = totalHumudity / humuditySum;
        }

        float totalHeight = 0;
        _heightVariety = new float[_distribution.HeightVariety];
        for (var i = 0; i < _distribution.HeightVariety; i++)
        {
            totalHeight += _distribution.HeightRange[i];
            _heightVariety[i] = totalHeight / heightSum;
        }

        float totalTemperature = 0;
        _temperatureVariety = new float[_distribution.TemperatureVariety];
        for (var i = 0; i < _distribution.TemperatureVariety; i++)
        {
            totalTemperature += _distribution.TemperatureRange[i];
            _temperatureVariety[i] = totalTemperature / temperatureSum;
        }
    }

    BiomeData IBiomeIdentifier.IdentifyBiome(
        float humidity,
        float height,
        float temperature)
    {
        Assert.IsTrue(humidity >= 0 && humidity <= 1);
        Assert.IsTrue(height >= 0 && height <= 1);
        Assert.IsTrue(temperature >= 0 && temperature <= 1);

        var humudityIdx = GetHumidityVarietyIndex(humidity);
        var heightIdx = GetHeightVarietyIndex(height);
        var temperatureIdx = GetTemperatureVarietyIndex(temperature);

        var biome = _distribution
            .BiomeHumiditys[humudityIdx]
            .BiomeHeights[heightIdx]
            .BiomeTemperatures[temperatureIdx];

        return biome;
    }

    public int GetHeightVarietyIndex(float height)
    {
        var heightIdx = 0;
        for (; heightIdx < _heightVariety.Length; heightIdx++)
        {
            if (height <= _heightVariety[heightIdx]) break;
        }
        return heightIdx;
    }
    public int GetTemperatureVarietyIndex(float temperature)
    {
        var temperatureIdx = 0;
        for (; temperatureIdx < _temperatureVariety.Length; temperatureIdx++)
        {
            if (temperature <= _temperatureVariety[temperatureIdx]) break;
        }
        return temperatureIdx;
    }
    public int GetHumidityVarietyIndex(float humidity)
    {
        var humudityIdx = 0;
        for (; humudityIdx < _humudityVariety.Length; humudityIdx++)
        {
            if (humidity <= _humudityVariety[humudityIdx]) break;
        }
        return humudityIdx;
    }

    public bool IsHeightInVarietyIndex(float height, int index)
    {
        if (index == 0)
            return height <= _heightVariety[index];
        else
            return (height <= _heightVariety[index]) && (height > _heightVariety[index - 1]);
    }
    public bool IsTemperatureInVarietyIndex(float temperature, int index)
    {
        if (index == 0)
            return temperature <= _temperatureVariety[index];
        else
            return (temperature <= _temperatureVariety[index]) && (temperature > _temperatureVariety[index - 1]);
    }
    public bool IsHumidityInVarietyIndex(float humidity, int index)
    {
        if (index == 0)
            return humidity <= _humudityVariety[index];
        else
            return (humidity <= _humudityVariety[index]) && (humidity > _humudityVariety[index - 1]);
    }
}

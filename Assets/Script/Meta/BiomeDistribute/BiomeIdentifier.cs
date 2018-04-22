using UnityEngine;
using UnityEngine.Assertions;

public interface IBiomeIdentifier
{
    BiomeData IdentifyBiome(
        float height,
        float temperature,
        float humidity
    );
}

public class BasicBiomeIdentifier : IBiomeIdentifier
{
    private BiomeDistribution _distribution;
    private float _humudityUnit;
    private float _heightUnit;
    private float _temperatureUnit;

    public BasicBiomeIdentifier(BiomeDistribution distribution)
    {
        _distribution = distribution;
        int humudityVariety = _distribution.BiomeHumiditys.Length;
        int heightVariety = _distribution.BiomeHumiditys[0].BiomeHeights.Length;
        int temperatureVariety = _distribution.BiomeHumiditys[0].BiomeHeights[0].BiomeTemperatures.Length;
        _humudityUnit = 1.0f / humudityVariety;
        _heightUnit = 1.0f / heightVariety;
        _temperatureUnit = 1.0f / temperatureVariety;
    }

    BiomeData IBiomeIdentifier.IdentifyBiome(
        float humidity,
        float height,
        float temperature)
    {
        Assert.IsTrue(humidity >= 0 && humidity < 1);
        Assert.IsTrue(height >= 0 && height < 1);
        Assert.IsTrue(temperature >= 0 && temperature < 1);

        var humudityIdx = Mathf.FloorToInt(humidity / _humudityUnit);
        var heightIdx = Mathf.FloorToInt(height / _heightUnit);
        var temperatureIdx = Mathf.FloorToInt(temperature / _temperatureUnit);

        var biome = _distribution
            .BiomeHumiditys[humudityIdx]
            .BiomeHeights[heightIdx]
            .BiomeTemperatures[temperatureIdx];

        return biome;
    }

    public void Check(
        float humidity,
        float height,
        float temperature)
    {
        Debug.LogFormat("w:{0}, wu:{1}, idx:{2}",
            humidity, _humudityUnit,
            Mathf.FloorToInt(humidity / _humudityUnit));
        Debug.LogFormat("h:{0}, hu:{1}, idx:{2}",
            height, _heightUnit,
            Mathf.FloorToInt(height / _heightUnit));
        Debug.LogFormat("h:{0}, hu:{1}, idx:{2}",
            temperature, _temperatureUnit,
            Mathf.FloorToInt(temperature / _temperatureUnit));
    }
}

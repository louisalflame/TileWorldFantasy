using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public interface IPopulationGenerator
{
    IEnumerator GeneratePopulationMap(
           int width,
           int height,
           ITileUnit[] tileUnits,
           IBiomeIdentifier biomeIdentifier,
           PopulationParameter para,
           IReturn<int[]> ret);
}
public class PopulationGenerator : IPopulationGenerator
{
    private int _width;
    private int _height;
    private ITileUnit[] _tileUnits;
    private IBiomeIdentifier _biomeIdentifier;
    private PopulationParameter _para;

    private int[] _populationMap; 

    public IEnumerator GeneratePopulationMap(
           int width,
           int height,
           ITileUnit[] tileUnits,
           IBiomeIdentifier biomeIdentifier,
           PopulationParameter para,
           IReturn<int[]> ret)
    {
        var cellCount = GenerationValidation.CellCount(width, height);
        GenerationValidation.Map(tileUnits, cellCount, "tileUnits");
        GenerationValidation.Population(para);
        foreach (var tile in tileUnits)
        {
            if (tile == null) throw new System.ArgumentException("Tiles must not be null.", "tileUnits");
            GenerationValidation.UnitValue(tile.Height, "tile height");
            GenerationValidation.UnitValue(tile.Humidity, "tile humidity");
            GenerationValidation.UnitValue(tile.Temperature, "tile temperature");
        }
        _width = width;
        _height = height;
        _tileUnits = tileUnits;
        _biomeIdentifier = biomeIdentifier;
        _para = para;

        _populationMap = new int[_width * _height];

        yield return _GeneratePopulationMap();

        ret.Accept(_populationMap);
    }

    private IEnumerator _GeneratePopulationMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int idx = MathUtility.MapIndex(x, y, _height);
                var tileUnit = _tileUnits[idx];

                var heightIdx = Mathf.Min(Mathf.FloorToInt(tileUnit.Height * _para.HeightVariety), _para.HeightVariety - 1);
                if (!_PassChance(_para.HeightProbability[heightIdx])) continue;
                var humidityIdx = Mathf.Min(Mathf.FloorToInt(tileUnit.Humidity * _para.HumidityVariety), _para.HumidityVariety - 1);
                if (!_PassChance(_para.HumidityProbability[humidityIdx])) continue;
                var temperIdx = Mathf.Min(Mathf.FloorToInt(tileUnit.Temperature * _para.TemperatureVariety), _para.TemperatureVariety - 1);
                if (!_PassChance(_para.TemperatureProbability[temperIdx])) continue;

                if (_PassChance(_para.BasicProbability))
                {
                    _populationMap[idx] = 1;
                }
                else
                    _populationMap[idx] = 0;
            }
        }
        yield break; 
    }

    private static bool _PassChance(float probability)
    {
        return probability >= 1f || (probability > 0f && Random.value < probability);
    }
}

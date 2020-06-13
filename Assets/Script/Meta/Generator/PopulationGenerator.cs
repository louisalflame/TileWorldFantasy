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

                var heightIdx = Mathf.FloorToInt(tileUnit.Height * _para.HeightVariety);
                var heightRand = Random.value;
                if (heightRand > _para.HeightProbability[heightIdx]) continue;
                var humidityIdx = Mathf.FloorToInt(tileUnit.Humidity * _para.HumidityVariety);
                var humidityRand = Random.value;
                if (humidityRand > _para.HumidityProbability[humidityIdx]) continue;
                var temperIdx = Mathf.FloorToInt(tileUnit.Temperature * _para.TemperatureVariety);
                var temperRand = Random.value;
                if (temperRand > _para.TemperatureProbability[temperIdx]) continue;

                if (Random.value < _para.BasicProbability)
                {
                    _populationMap[idx] = 1;
                }
                else
                    _populationMap[idx] = 0;
            }
        }
        yield break; 
    }
}

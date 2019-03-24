using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public interface IPopulationGenerator
{
    IEnumerator GeneratePopulationMap(
           int width,
           int height,
           float xOffset,
           float yOffset,
           ITileUnit[] tileUnits,
           IBiomeIdentifier biomeIdentifier,
           PopulationParameter para,
           IReturn<int[]> ret);
}
public class PopulationGenerator : IPopulationGenerator
{
    private float _xOffset;
    private float _yOffset;
    private int _width;
    private int _height;
    private ITileUnit[] _tileUnits;
    private IBiomeIdentifier _biomeIdentifier;
    private PopulationParameter _para;

    private int[] _populationMap; 

    public IEnumerator GeneratePopulationMap(
           int width,
           int height,
           float xOffset,
           float yOffset,
           ITileUnit[] tileUnits,
           IBiomeIdentifier biomeIdentifier,
           PopulationParameter para,
           IReturn<int[]> ret)
    {
        _width = width;
        _height = height;
        _xOffset = xOffset;
        _yOffset = yOffset;
        _para = para;

        _populationMap = new int[_width * _height];

        yield return null;

        
    }

    private IEnumerator _GeneratePopulationMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int idx = MathUtility.MapIndex(x, y, _height);
                
                
            }
        }
        yield break; 
    }
}

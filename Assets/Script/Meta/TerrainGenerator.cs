
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITerrainGenerator
{
    float[] HeightMap { get; } 
    IEnumerator GenerateHeightMap(
        int width, 
        int height,
        float xOffset,
        float yOffset,
        TerrainGeneratorParameter para);
} 

public class TerrainGenerator : ITerrainGenerator
{
    private TerrainGeneratorParameter _para = new TerrainGeneratorConstParameter(); 
    
    private float _xOffset;
    private float _yOffset;
     
    private IRandomPointGenerator _randPointGen = new RandomPointGenerator();
    private float[] _localAreaMap;

    private int _width;
    private int _height;

    private float[] _heightMap;
    public float[] HeightMap {
        get { return _heightMap; }
    }

    private int _sleepCount = 0;
    private int _sleepMax = 50;

    public IEnumerator GenerateHeightMap(
        int width, 
        int height, 
        float xOffset,
        float yOffset,
        TerrainGeneratorParameter para)
    {
        _width = width;
        _height = height;
        _xOffset = xOffset;
        _yOffset = yOffset;
        _para = para;

        _heightMap = new float[_width * _height];
        yield return _randPointGen.GenerateRandomLocalAreaMap(
            width, height, _para.RANDOM_POINT_GEN_PARA);
        _localAreaMap = _randPointGen.LocalAreaMap;

        yield return _GenerateHeightMap();
    }

    private IEnumerator _GenerateHeightMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;

                var sample = _CountPerlinNoise(x, y, xCoord, yCoord);

                _heightMap[y * _width + x] = sample;

                if (_sleepCount++ > _sleepMax)
                {
                    yield return null;
                    _sleepCount = 0;
                }
            }
        }
    }

    private float _CountPerlinNoise(int x, int y, float xCoord, float yCoord)
    {
        float sample = 0f;

        sample += NoiseUtility.CountRecursivePerlinNoise(
            xCoord, 
            yCoord,
            _xOffset, 
            _yOffset, 
            _para.SCALE,
            _para.FREQ_COUNT_TIMES, 
            _para.FREQ_GROW_FACTOR);

        sample += _localAreaMap[y * _width + x];

        sample = Mathf.Pow(sample, _para.LOW_GROUND_FACTOR);
        sample = 1 - Mathf.Pow(1 - sample, _para.HIGH_MOUNTAIN_FACTOR);

        sample *= _CountSurroundDown(new Vector2(xCoord, yCoord));

        return sample;
    }

    private float _CountSurroundDown(Vector2 coord)
    {
        float dis = Vector2.Distance( new Vector2(0.5f, 0.5f), coord) / 0.5f;
        return 1 - _para.SURROUND_DOWN_OFFSET * Mathf.Pow(dis, _para.SURROUND_DOWN_SPEED);
    }
}

public class TerrainGeneratorParameter
{
    public float SCALE = 5;
    public float LOW_GROUND_FACTOR = 4;
    public float HIGH_MOUNTAIN_FACTOR = 6;

    public int FREQ_COUNT_TIMES = 5;
    public int FREQ_GROW_FACTOR = 2;

    public float SURROUND_DOWN_OFFSET = 0.6f;
    public float SURROUND_DOWN_SPEED = 2.0f;

    public RandomPointGeneratorParameter RANDOM_POINT_GEN_PARA = new RandomPointGeneratorParameter();
}

public class TerrainGeneratorConstParameter : TerrainGeneratorParameter
{
    public new readonly float SCALE = 5;
    public new readonly float LOW_GROUND_FACTOR = 4;
    public new readonly float HIGH_MOUNTAIN_FACTOR = 6;

    public new readonly int FREQ_COUNT_TIMES = 5;
    public new readonly int FREQ_GROW_FACTOR = 2;

    public new readonly float SURROUND_DOWN_OFFSET = 0.6f;
    public new readonly float SURROUND_DOWN_SPEED = 2.0f; 
}
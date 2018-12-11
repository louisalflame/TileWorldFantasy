
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public interface ITerrainGenerator
{
    IEnumerator GenerateHeightMap(
        int width, 
        int height,
        float xOffset,
        float yOffset,
        TerrainParameter para,
        IReturn<float[]> ret);
} 

public class TerrainGenerator : ITerrainGenerator
{
    private TerrainParameter _para; 
    
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
    private int _sleepMax = 500;

    public IEnumerator GenerateHeightMap(
        int width, 
        int height, 
        float xOffset,
        float yOffset,
        TerrainParameter para,
        IReturn<float[]> ret)
    {
        _width = width;
        _height = height;
        _xOffset = xOffset;
        _yOffset = yOffset;
        _para = para;

        _heightMap = new float[_width * _height];

        var genLocalMonad = new BlockMonad<float[]>( r => 
            _randPointGen.GenerateRandomLocalAreaMap( 
                width, height, _para.RANDOM_POINT_GEN_PARA, r));

        yield return genLocalMonad.Do();
        if (genLocalMonad.Error != null) {
            ret.Fail(genLocalMonad.Error);
            yield break;
        }
        _localAreaMap = genLocalMonad.Result;

        yield return _GenerateHeightMap();

        ret.Accept(_heightMap);
    }

    private IEnumerator _GenerateHeightMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int idx = x * _height + y;

                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;

                var sample = _CountPerlinNoise(idx, xCoord, yCoord);

                _heightMap[idx] = Mathf.Clamp(sample, 0f, 0.999f);

                if (_sleepCount++ > _sleepMax)
                {
                    yield return null;
                    _sleepCount = 0;
                }
            }
        }
    }

    private float _CountPerlinNoise(int idx, float xCoord, float yCoord)
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

        sample += _localAreaMap[idx];

        sample = MathUtility.CountPow(sample, _para.LOW_GROUND_FACTOR);
        sample = 1 - MathUtility.CountPow(1 - sample, _para.HIGH_MOUNTAIN_FACTOR);

        sample *= _CountSurroundDown(new Vector2(xCoord, yCoord));

        return sample;
    }

    private float _CountSurroundDown(Vector2 coord)
    {
        float dis = Vector2.Distance( new Vector2(0.5f, 0.5f), coord) / 0.5f;
        return 1 - _para.SURROUND_DOWN_OFFSET * MathUtility.CountPow(dis, _para.SURROUND_DOWN_SPEED);
    }
}

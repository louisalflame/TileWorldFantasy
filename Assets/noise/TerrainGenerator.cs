
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITerrainGenerator
{
}

public class TerrainGenerator : ITerrainGenerator
{
    private TerrainGeneratorParameter _para = new TerrainGeneratorConstParameter(); 

    private float _xOffset;
    private float _yOffset;

    private List<Vector2> _upPoints = new List<Vector2>();

    private int _width;
    private int _height;
    private float[] _heightMap;

    public IEnumerator MakeHeightMap(int width, int height)
    {
        TerrainGeneratorConstParameter a = new TerrainGeneratorConstParameter(); 

           _width = width;
        _height = height;
        _heightMap = new float[_width * _height];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;

                var sample = _CountPerlinNoise(xCoord, yCoord);

                _heightMap[y * _width + x] = sample;
            }
            yield return null;
        }
    }
    private float _CountPerlinNoise(float xCoord, float yCoord)
    {
        float sample = 0f;

        sample += _CountRecursivePerlinNoise(
            xCoord, yCoord,
            _xOffset, _yOffset, _para.SCALE,
            _para.FREQ_COUNT_TIMES, _para.FREQ_GROW_FACTOR);

        for (int i = 0; i < _upPoints.Count; i++)
        {
            var point = new Vector2(
                _upPoints[i].x * _width,
                _upPoints[i].y * _height);
            var coord = new Vector2(
                xCoord * _width,
                yCoord * _height);

            float distance = Vector2.Distance(point, coord);
            float upDegree = ((_width+ _height) / 2) * _para.UP_RADIUS - distance;
            float upHeight = _para.UP_SCALE * Mathf.Pow(_para.UP_SPEED, upDegree);
            float upNoise = _CountRecursivePerlinNoise(
                xCoord, yCoord,
                _upPoints[i].x, _upPoints[i].y,
                _para.SCALE * 2, 2, 2);

            sample += upNoise * upHeight;
        }

        sample = Mathf.Pow(sample, _para.LOW_GROUND_FACTOR);
        sample = 1 - Mathf.Pow(1 - sample, _para.HIGH_MOUNTAIN_FACTOR);

        float dis = Vector2.Distance(
            new Vector2(0.5f, 0.5f),
            new Vector2(xCoord, yCoord)) / 0.5f;
        sample = sample * (1 - _para.SURROUND_DOWN_OFFSET * Mathf.Pow(dis, _para.SURROUND_DOWN_SPEED));

        return sample;
    }

    private float _CountRecursivePerlinNoise(
        float xCoord, float yCoord,
        float xOffset, float yOffset, float scale,
        int freqCountTimes, float freqGrowFactor)
    {
        float sample = 0f;
        float freq = 1f;
        float sampleTimes = 0f;
        for (int i = 0; i < freqCountTimes; i++)
        {
            sample += (1 / freq) *
                Mathf.PerlinNoise(
                    scale * xCoord * freq + xOffset,
                    scale * yCoord * freq + yOffset);
            sampleTimes += (1 / freq);
            freq *= freqGrowFactor;
        }
        sample /= sampleTimes;

        return sample;
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

    public int UP_NUMS = 10;
    public float UP_RADIUS = 0.05f;
    public float UP_SCALE = 0.15f;
    public float UP_SPEED = 1000000000f;
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

    public new readonly int UP_NUMS = 10;
    public new readonly float UP_RADIUS = 0.05f;
    public new readonly float UP_SCALE = 0.15f;
    public new readonly float UP_SPEED = 1000000000f;
}
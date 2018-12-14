using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;

public interface IRiverGenerator
{
    IEnumerator GenerateRiverMap(
        int width,
        int height,
        float[] heightMap,
        RainParameter rainParameter,
        IReturn<float[]> ret);
}

public class RiverGenerator : IRiverGenerator
{
    private int _width;
    private int _height;
    private RainParameter _para;
    private int _sleepCount;

    private int[] _directionMap;
    private int[] _rainMap;

    public IEnumerator GenerateRiverMap(
        int width, 
        int height, 
        float[] heightMap,
        RainParameter para,
        IReturn<float[]> ret)
    {
        _width = width;
        _height = height;
        _para = para;

        var startLocations = heightMap
            .Select((h, idx) => new { h, idx })
            .OrderByDescending(i => i.h)
            .Take(heightMap.Length / (int)(1 / _para.HEIGHT_LOCATIONS))
            .Select(o => o.idx).ToArray();

        _directionMap = new int[heightMap.Length];
        _rainMap = new int[heightMap.Length];

        yield return _GenerateDirationMap(heightMap);

        yield return _GenerateRainMap(startLocations, heightMap);

        var rains = _rainMap.Where(i => i > 0);
        var average = (rains.Sum() / rains.Count()) * _para.RIVER_AVERAGE_FACTOR;

        var rainResult = _rainMap.Select(i => Mathf.Min(i / average, 1)).ToArray(); 
        ret.Accept(rainResult); 
    }

    private IEnumerator _GenerateDirationMap(float[] heightMap)
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var idx = _GetIndex(x, y);
                _directionMap[idx] = _FindDirection(x, y, idx, heightMap);

                if (_sleepCount++ > SettingUtility.MapRestCount)
                {
                    yield return null;
                    _sleepCount = 0;
                }
            }
        }
    }

    private IEnumerator _GenerateRainMap(int[] startLocations, float[] heightMap)
    {
        for (var i = 0; i < startLocations.Length; i++)
        {
            int start = startLocations[i];

            int rain = 1;
            for (int j = 0; j < _para.RIVER_LENGTH; j++)
            {
                var next = _directionMap[start];
                if (next < 0)
                {
                    break;
                }

                rain = Mathf.Max(_rainMap[next] + 1, rain);

                start = next;
                _rainMap[start] = rain;

                if (heightMap[start] < _para.RIVER_END_HEIGHT)
                    break;

            }

            if (_sleepCount++ > SettingUtility.MapRestCount)
            {
                yield return null;
                _sleepCount = 0;
            }
        }
    }

    private int _FindDirection(int x, int y, int idx, float[] heightMap)
    {
        var current = heightMap[idx];
        var direction = -1;
         
        if (y > 0)
        {
            var upIdx = _GetIndex(x, y - 1);
            if (heightMap[upIdx] < current)
            {
                current = heightMap[upIdx];
                direction = upIdx;
            }
        }
        if (y < _height - 1)
        {
            var downIdx = _GetIndex(x, y + 1);
            if (heightMap[downIdx] < current)
            {
                current = heightMap[downIdx];
                direction = downIdx;
            }
        }
        if (x > 0)
        {
            var leftIdx = _GetIndex(x - 1, y);
            if (heightMap[leftIdx] < current)
            {
                current = heightMap[leftIdx];
                direction = leftIdx;
            }
        }
        if (x < _width - 1)
        {
            var rightIdx = _GetIndex(x + 1, y);
            if (heightMap[rightIdx] < current)
            {
                current = heightMap[rightIdx];
                direction = rightIdx;
            }
        }

        return direction;
    }

    private int _GetIndex(int x, int y)
    {
        return y * _width + x;
    }
}

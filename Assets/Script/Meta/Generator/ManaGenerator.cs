using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;

public interface IManaGenerator
{
    IEnumerator GenerateManaMap(
        int width,
        int height,
        RandomPointParameter para,
        IReturn<float[]> ret);
}

public class ManaGenerator : IManaGenerator
{ 
    private IRandomPointGenerator _randPointGen = new RandomPointGenerator(); 
    private List<Vector2> _highPosPoints;
    private List<Vector2> _midPosPoints;
    private List<Vector2> _lowPosPoints;
    private List<Vector2> _highNegPoints;
    private List<Vector2> _midNegPoints;
    private List<Vector2> _lowNegPoints;

    private RandomPointParameter _para;
    private int _width;
    private int _height;
    private int _sleepCount = 0;

    private float[] _manaMap;
    private float[] _localAreaMap;

    public IEnumerator GenerateManaMap(
        int width,
        int height,
        RandomPointParameter para,
        IReturn<float[]> ret)
    {
        _width = width;
        _height = height;
        _para = para;

        _manaMap = new float[_width * _height];
        _localAreaMap = new float[_width * _height];

        yield return _randPointGen.GenerateRandomPoints(width, height, 20, _para);

        var points = _randPointGen.Points.OrderBy(i => Random.value).ToList();

        _highPosPoints = points.Take(2).ToList();
        points = points.Skip(2).ToList();
        _midPosPoints = points.Take(3).ToList();
        points = points.Skip(3).ToList();
        _lowPosPoints = points.Take(5).ToList();
        points = points.Skip(5).ToList();
        _highNegPoints = points.Take(2).ToList();
        points = points.Skip(2).ToList();
        _midNegPoints = points.Take(3).ToList();
        points = points.Skip(3).ToList();
        _lowNegPoints = points.Take(5).ToList();

        yield return _GenerateRandomLocalAreaMap(_highPosPoints, 0.1f, 3);
        yield return _GenerateRandomLocalAreaMap(_midPosPoints, 0.08f, 2);
        yield return _GenerateRandomLocalAreaMap(_lowPosPoints, 0.06f, 1);
        yield return _GenerateRandomLocalAreaMap(_highNegPoints, 0.1f, -3);
        yield return _GenerateRandomLocalAreaMap(_midNegPoints, 0.08f, -2);
        yield return _GenerateRandomLocalAreaMap(_lowNegPoints, 0.06f, -1);

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var idx = y * _width + x;
                _manaMap[idx] = _localAreaMap[idx] + 0.5f;
            }
        }

        ret.Accept(_manaMap);
    }

    private IEnumerator _GenerateRandomLocalAreaMap(List<Vector2> points, float radius, float posOrNegfactor)
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;
                var coord = new Vector2(xCoord, yCoord);

                float sample = 0;

                for (int i = 0; i < points.Count; i++)
                {
                    float distance = Vector2.Distance(points[i], coord);
                    float upDegree = radius > distance ?
                        radius - distance : 0;

                    float upHeight = _para.LOCAL_AREA_SCALE * upDegree;

                    float upNoise = NoiseUtility.CountRecursivePerlinNoise(
                        xCoord,
                        yCoord,
                        _para.LOCAL_XOFFSET,
                        _para.LOCAL_YOFFSET,
                        _para.LOCAL_SCALE,
                        _para.LOCAL_FREQ_COUNT_TIMES,
                        _para.LOCAL_FREQ_GROW_FACTOR);

                    sample += upNoise * upHeight * posOrNegfactor;
                }

                _localAreaMap[y * _width + x] += sample;

                if (_sleepCount++ > SettingUtility.MapRestCount)
                {
                    yield return null;
                    _sleepCount = 0;
                }
            }
        }
    }
}

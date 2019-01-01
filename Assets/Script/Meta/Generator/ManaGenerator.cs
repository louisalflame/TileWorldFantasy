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
        ManaParameter para,
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

    private ManaParameter _para;
    private RandomPointParameter _randomParam;
    private int _width;
    private int _height;
    private int _sleepCount = 0;

    private float[] _manaMap;
    private float[] _localAreaMap;

    public IEnumerator GenerateManaMap(
        int width,
        int height,
        ManaParameter para,
        IReturn<float[]> ret)
    {
        _width = width;
        _height = height;
        _para = para;
        _randomParam = para.RANDOM_POINT_GEN_PARA;

        _manaMap = new float[_width * _height];
        _localAreaMap = new float[_width * _height];

        var totalPoints =
            _para.POSITIVE_HIGH_POINTS_NUM +
            _para.POSITIVE_MID_POINTS_NUM +
            _para.POSITIVE_LOW_POINTS_NUM +
            _para.NEGATIVE_HIGH_POINTS_NUM +
            _para.NEGATIVE_MID_POINTS_NUM +
            _para.NEGATIVE_LOW_POINTS_NUM;

        yield return _randPointGen.GenerateRandomPoints(
            width, 
            height, 
            totalPoints,
            _randomParam);

        var points = _randPointGen.Points.OrderBy(i => Random.value).ToList();

        _highPosPoints = points.Take(_para.POSITIVE_HIGH_POINTS_NUM).ToList();
        points = points.Skip(_para.POSITIVE_HIGH_POINTS_NUM).ToList();
        _midPosPoints = points.Take(_para.POSITIVE_HIGH_POINTS_NUM).ToList();
        points = points.Skip(_para.POSITIVE_HIGH_POINTS_NUM).ToList();
        _lowPosPoints = points.Take(_para.POSITIVE_LOW_POINTS_NUM).ToList();
        points = points.Skip(_para.POSITIVE_LOW_POINTS_NUM).ToList();
        _highNegPoints = points.Take(_para.NEGATIVE_HIGH_POINTS_NUM).ToList();
        points = points.Skip(_para.NEGATIVE_HIGH_POINTS_NUM).ToList();
        _midNegPoints = points.Take(_para.NEGATIVE_MID_POINTS_NUM).ToList();
        points = points.Skip(_para.NEGATIVE_MID_POINTS_NUM).ToList();
        _lowNegPoints = points.Take(_para.NEGATIVE_LOW_POINTS_NUM).ToList();

        yield return _GenerateRandomLocalAreaMap(_highPosPoints, _para.POSITIVE_HIGH_RADIUS, _para.POSITIVE_HIGH_FACTOR);
        yield return _GenerateRandomLocalAreaMap(_midPosPoints, _para.POSITIVE_MID_RADIUS, _para.POSITIVE_MID_FACTOR);
        yield return _GenerateRandomLocalAreaMap(_lowPosPoints, _para.POSITIVE_LOW_RADIUS, _para.POSITIVE_LOW_FACTOR);
        yield return _GenerateRandomLocalAreaMap(_highNegPoints, _para.NEGATIVE_HIGH_RADIUS, _para.NEGATIVE_HIGH_FACTOR);
        yield return _GenerateRandomLocalAreaMap(_midNegPoints, _para.NEGATIVE_MID_RADIUS, _para.NEGATIVE_MID_FACTOR);
        yield return _GenerateRandomLocalAreaMap(_lowNegPoints, _para.NEGATIVE_LOW_RADIUS, _para.NEGATIVE_LOW_FACTOR);

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
                var idx = MathUtility.MapIndex(x, y, _height);
                var coord = new Vector2(xCoord, yCoord);

                float sample = 0;

                for (int i = 0; i < points.Count; i++)
                {
                    float distance = Vector2.Distance(points[i], coord);
                    if (distance >= radius)
                        continue;

                    float upDegree = radius - distance;

                    float upNoise = NoiseUtility.CountRecursivePerlinNoise(
                        xCoord,
                        yCoord,
                        _randomParam.LOCAL_XOFFSET,
                        _randomParam.LOCAL_YOFFSET,
                        _randomParam.LOCAL_SCALE,
                        _randomParam.LOCAL_FREQ_COUNT_TIMES,
                        _randomParam.LOCAL_FREQ_GROW_FACTOR);

                    sample += upNoise * upDegree * posOrNegfactor;
                }

                _localAreaMap[idx] += sample;

                if (_sleepCount++ > SettingUtility.MapRestCount)
                {
                    yield return null;
                    _sleepCount = 0;
                }
            }
        }
    }
}

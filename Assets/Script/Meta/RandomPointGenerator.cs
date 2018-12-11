using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public interface IRandomPointGenerator
{
    List<Vector2> Points { get; }
    IEnumerator GenerateRandomLocalAreaMap(
           int width,
           int height,
           RandomPointParameter para,
           IReturn<float[]> ret);
}

public class RandomPointGenerator : IRandomPointGenerator
{
    private RandomPointParameter _para;

    private int _width;
    private int _height;

    private float[] _localAreaMap;
    public float[] LocalAreaMap {
        get { return _localAreaMap; }
    }

    private List<Vector2> _points;
    public List<Vector2> Points {
        get { return _points; }
    }

    private List<Vector2> _moves;

    private int _sleepCount = 0;
    private int _sleepMax = 500;

    public IEnumerator GenerateRandomLocalAreaMap(
        int width,
        int height,
        RandomPointParameter para,
        IReturn<float[]> ret)
    {
        _width = width;
        _height = height;
        _para = para;

        _localAreaMap = new float[_width * _height];

        yield return _GetRandomLoosePoints();

        yield return _GenerateRandomLocalAreaMap();

        ret.Accept(_localAreaMap);
    }

    private IEnumerator _GenerateRandomLocalAreaMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;
                var coord = new Vector2(xCoord, yCoord);

                float sample = 0;

                for (int i = 0; i < _points.Count; i++)
                {
                    float distance = Vector2.Distance(_points[i], coord);
                    float upDegree = _para.LOCAL_AREA_RADIUS > distance ?
                        _para.LOCAL_AREA_RADIUS - distance : 0;

                    float upHeight = _para.LOCAL_AREA_SCALE * upDegree; 

                    float upNoise = NoiseUtility.CountRecursivePerlinNoise(
                        xCoord,
                        yCoord,
                        _para.LOCAL_XOFFSET,
                        _para.LOCAL_YOFFSET,
                        _para.LOCAL_SCALE, 
                        _para.LOCAL_FREQ_COUNT_TIMES, 
                        _para.LOCAL_FREQ_GROW_FACTOR);

                    sample += upNoise * upHeight;
                }
                
                _localAreaMap[y * _width + x] = sample;

                if (_sleepCount++ > _sleepMax)
                {
                    yield return null;
                    _sleepCount = 0;
                }
            }
        }
    }

    private IEnumerator _GetRandomLoosePoints()
    {
        _points = new List<Vector2>();

        for (int i = 0; i < _para.NUM; i++)
        {
            _points.Add(new Vector2(Random.value, Random.value));
        }

        yield return _CountLoosePoints(); 
    }

    private IEnumerator _CountLoosePoints()
    {
        for (int count = 0; count < _para.COUNT_TIME; count++)
        {
            _ResetMoves();

            for (int i = 0; i < _para.NUM; i++)
            {
                for (int j = 0; j < _para.NUM; j++)
                {
                    if (i == j) { continue; }
                    _checkOtherPointDistance(i, j);
                }
                _checkWallDistance(i);
            }

            _PointsMoves();

            if (_NoMoveNeed()) break;
            yield return null;
        }
    }

    private void _ResetMoves()
    {
        _moves = new List<Vector2>();
        for (int i = 0; i < _para.NUM; i++)
        {
            _moves.Add(Vector2.zero);
        }
    }

    private void _checkOtherPointDistance(int i, int j)
    {
        var selfPoint = _points[i];
        var otherPoint = _points[j];
        var distance = Vector2.Distance(selfPoint, otherPoint);

        if (distance < _para.POINTS_MIN_DISTANCE)
        {
            var selfMove = selfPoint - otherPoint;
            _moves[i] += selfMove.normalized * _para.POINTS_SEPARATE_SPEED;
        }
    }
    private void _checkWallDistance(int i)
    {
        var selfPoint = _points[i];

        if (selfPoint.x < _para.WALL_MIN_DISTANCE)
        {
            var selfMove = new Vector2(1, 0);
            _moves[i] += selfMove.normalized * _para.WALL_SEPARATE_SPEED;
        }
        else if (selfPoint.x > 1 - _para.WALL_MIN_DISTANCE)
        {
            var selfMove = new Vector2(-1, 0);
            _moves[i] += selfMove.normalized * _para.WALL_SEPARATE_SPEED;
        }

        if (selfPoint.y < _para.WALL_MIN_DISTANCE)
        {
            var selfMove = new Vector2(0, 1);
            _moves[i] += selfMove.normalized * _para.WALL_SEPARATE_SPEED;
        }
        else if (selfPoint.y > 1 - _para.WALL_MIN_DISTANCE)
        {
            var selfMove = new Vector2(0, -1);
            _moves[i] += selfMove.normalized * _para.WALL_SEPARATE_SPEED;
        }
    }

    private void _PointsMoves()
    {
        for (int i = 0; i < _para.NUM; i++)
        {
            _points[i] += _moves[i];
        }
    }

    private bool _NoMoveNeed()
    {
        for (int i = 0; i < _moves.Count; i++)
        {
            if (_moves[i].x != 0 || _moves[i].y != 0)
                return false; 
        }
        return true;
    }
}
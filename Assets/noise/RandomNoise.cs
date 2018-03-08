using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RandomNoise : MonoBehaviour {

    [SerializeField]
    private SpriteView _spriteView;

    [SerializeField]
    private List<Vector2> _points;

    [SerializeField]
    private int _nums = 5;

    [Header("Radius Parameter")]
    [SerializeField]
    private float _radius;
    [SerializeField]
    private float _upScale;
    [SerializeField]
    private float _upSpeed;

    [Header("Loose Parameter")]
    [SerializeField]
    private float _countTimes;
    [SerializeField]
    private float _minDistance;
    [SerializeField]
    private float _speed;
    [SerializeField]
    private float _wallDistance;
    [SerializeField]
    private float _wallSpeed;
    private List<Vector2> _moves;

    private Color[] _pix;

    private void Start()
    {
        _points = new List<Vector2>();
        for (int i = 0; i < _nums; i++) {
            _points.Add(new Vector2(Random.value, Random.value));
        }
    }

    public void DrawTexture()
    {
        _pix = new Color[_spriteView.Width * _spriteView.Height];
        _CountLoosePoints();

        for (float x = 0f; x < _spriteView.Width; x++) {
            for (float y = 0f; y < _spriteView.Height; y++) {

                float xCoord = (x / _spriteView.Width);
                float yCoord = (y / _spriteView.Height);
                float sample = 0;

                for (int i = 0; i < _points.Count; i++) {
                    var point = new Vector2(
                        _points[i].x * _spriteView.Width, 
                        _points[i].y * _spriteView.Height);
                    var coord = new Vector2( x, y );

                    float distance = Vector2.Distance(point, coord);
                    float upDegree = _radius - distance;
                    sample += _upScale * Mathf.Pow(_upSpeed, upDegree);
                }
                sample = Mathf.Min(1, sample);


                var c = new Color(sample, sample, sample);
                _pix[(int)y * _spriteView.Width + (int)x] = c;
            }
        }

        _spriteView.SetPixels(_pix);
    }

    public List<Vector2> GetRandomLoosePoints(int nums)
    {
        _nums = nums;
        _points = new List<Vector2>();
        for (int i = 0; i < _nums; i++) {
            _points.Add(new Vector2(Random.value, Random.value));
        }
        _CountLoosePoints();

        return _points;
    }

    private void _CountLoosePoints()
    {
        for(int count = 0; count < _countTimes; count++) {
            _ResetMoves();

            for (int i = 0; i < _nums; i++) {
                for (int j = 0; j < _nums; j++) {
                    if (i == j) { continue; } 
                    _checkOtherPointDistance(i, j);
                }
                _checkWallDistance(i); 
            }

            _PointsMoves();

            bool skip = true;
            for (int i = 0; i < _moves.Count; i++) {
                if (_moves[i].x != 0 || _moves[i].y != 0) {
                    skip = false; break;
                }
            }
            if (skip) break;
        } 
    }

    private void _ResetMoves() {
        _moves = new List<Vector2>();
        for (int i = 0; i < _nums; i++) {
            _moves.Add(Vector2.zero);
        }
    }
    private void _checkOtherPointDistance(int i, int j) {
        var selfPoint = _points[i];
        var otherPoint = _points[j];
        var distance = Vector2.Distance(selfPoint, otherPoint);
         
        if (distance < _minDistance)
        {
            var selfMove = new Vector2(selfPoint.x - otherPoint.x, selfPoint.y - otherPoint.y);
            _moves[i] += selfMove.normalized * _speed; 
            var otherMove = new Vector2(otherPoint.x - selfPoint.x, otherPoint.y - selfPoint.y);
            _moves[j] += otherMove.normalized * _speed; 
        }
    }
    private void _checkWallDistance(int i) {
        var selfPoint = _points[i];

        if (selfPoint.x < _wallDistance) {
            var selfMove = new Vector2(1, 0);
            _moves[i] += selfMove.normalized * _wallSpeed;
        } else if (selfPoint.x > 1 - _wallDistance) {
            var selfMove = new Vector2(-1, 0);
            _moves[i] += selfMove.normalized * _wallSpeed;
        }

        if (selfPoint.y < _wallDistance) {
            var selfMove = new Vector2(0, 1);
            _moves[i] += selfMove.normalized * _wallSpeed;
        } else if (selfPoint.y > 1 - _wallDistance) {
            var selfMove = new Vector2(0, -1);
            _moves[i] += selfMove.normalized * _wallSpeed;
        }
    }

    private void _PointsMoves() {
        for (int i = 0; i < _nums; i++) {
            _points[i] += _moves[i];
        }
    } 
}
 
[CustomEditor(typeof(RandomNoise))]
public class RandomNoiseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RandomNoise myRandomNoise = (RandomNoise)target;
        if (GUILayout.Button("DrawTexture"))
        {
            myRandomNoise.DrawTexture();
        }
    }
}
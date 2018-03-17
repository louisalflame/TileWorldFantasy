using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Utility;

public class RandomNoise : MonoBehaviour {

    [SerializeField]
    private SpriteView _spriteView;

    [SerializeField]
    private List<Vector2> _points;

    [SerializeField]
    private int _nums = 5;

    [Header("Radius Parameter")]
    [SerializeField]
    private float _localAreaRadius;
    [SerializeField]
    private float _localAreaScale;
    [SerializeField]
    private float _localAreaSpeed;

    [Header("Loose Parameter")]
    [SerializeField]
    private int _countTime;
    [SerializeField]
    private float _pointsMinDistance;
    [SerializeField]
    private float _pointsSeperateSpeed;
    [SerializeField]
    private float _wallMinDistance;
    [SerializeField]
    private float _wallSeperateSpeed;
     
    [Header("Random Parameter")]
    [SerializeField]
    private float _localScale;
    [SerializeField]
    private float _localXOffset;
    [SerializeField]
    private float _localYOffset;
    [SerializeField]
    private int _localFreqCountTimes;
    [SerializeField]
    private int _localFreqGrowFactor;

    private float[] _localAreaMap;

    private IRandomPointGenerator _randPointGen = new RandomPointGenerator();

    private Executor _executor = new Executor();

    void Update()
    {
        _executor.Resume(Time.deltaTime);
    }

    public void DrawTexture()
    {
        _executor.Clear();
        var MakeMapM = new Utility.Coroutine(_ShowLocalAreaMap());
        _executor.Add(MakeMapM);
    }

    private IEnumerator _ShowLocalAreaMap()
    {
        Debug.Log("[RandomPointGen] generate start");
        yield return _randPointGen.GenerateRandomLocalAreaMap(
            _spriteView.Width,
            _spriteView.Height,
            _GetRandPointGenPara()
        );

        Debug.Log("[RandomPointGen] generate complete");
        _localAreaMap = _randPointGen.LocalAreaMap;
        _points = _randPointGen.Points;
        _spriteView.SetLocalAreaMap(_localAreaMap);
    }

    private RandomPointGeneratorParameter _GetRandPointGenPara()
    {
        return new RandomPointGeneratorParameter()
        {
            NUM = _nums,

            COUNT_TIME = _countTime,
            POINTS_MIN_DISTANCE = _pointsMinDistance,
            POINTS_SEPARATE_SPEED = _pointsSeperateSpeed,
            WALL_MIN_DISTANCE = _wallMinDistance,
            WALL_SEPARATE_SPEED = _wallSeperateSpeed,

            LOCAL_SCALE = _localScale,
            LOCAL_XOFFSET = _localXOffset,
            LOCAL_YOFFSET = _localYOffset,

            LOCAL_FREQ_COUNT_TIMES = _localFreqCountTimes,
            LOCAL_FREQ_GROW_FACTOR = _localFreqGrowFactor,

            LOCAL_AREA_RADIUS = _localAreaRadius,
            LOCAL_AREA_SCALE = _localAreaScale,
            LOCAL_AREA_SPEED = _localAreaSpeed
        };
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
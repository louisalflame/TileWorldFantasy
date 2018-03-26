using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Utility;

public class SpriteNoise : MonoBehaviour {

    [SerializeField]
    private SpriteView _spriteView;
    [SerializeField]
    private RandomNoise _randomNoise;

    [Header("Basic Parameter")]
    [SerializeField]
    private float _xOffset;
    [SerializeField]
    private float _yOffset;
    [SerializeField]
    private float _scale;
    [SerializeField]
    private float _lowGroundFactor;
    [SerializeField]
    private float _highMountainFactor;
     
    [Header("Recursive Mode")]
    [SerializeField]
    private int _freqCountTimes;
    [SerializeField]
    private int _freqGrowFactor;

    [Header("Island Mode")]
    [SerializeField]
    private float _surroundDownOffset;
    [SerializeField]
    private float _surroundDownSpeed;

    [Header("Mountain Mode")] 
    [SerializeField]
    private int _loaclUpNums;
    [SerializeField]
    private float _localUpRadius;
    [SerializeField]
    private float _localUpScale;
    [SerializeField]
    private float _localUpSpeed;
    [SerializeField]
    private List<Vector2> _upPoints = new List<Vector2>();
     
    private int _width;
    private int _height;
    private float[] _heightMap;

    private ITerrainGenerator _terrainGen = new TerrainGenerator();

    private Executor _executor = new Executor();

    void Update()
    {
        _executor.Resume(Time.deltaTime);
    }

    public void SaveMap()
    {
        var data = new SaveData();
        data.SaveMap(_heightMap);
    }

    public void DrawTexture() {
        _executor.Clear();
        var MakeMapM = new Utility.Coroutine(_ShowHeightMap());
        _executor.Add(MakeMapM);
    }

    private IEnumerator _ShowHeightMap()
    {
        Debug.Log("[TerrainGen] generate start");
        yield return _terrainGen.GenerateHeightMap(
            _spriteView.Width, 
            _spriteView.Height,
            _xOffset,
            _yOffset,
            _GetTerrainGenPara());

        Debug.Log("[TerrainGen] generate complete");
        _heightMap = _terrainGen.HeightMap;
        _spriteView.SetHeightMap(_heightMap);
    }

    private TerrainGeneratorParameter _GetTerrainGenPara()
    {
        return new TerrainGeneratorParameter()
        {
            SCALE = _scale,
            LOW_GROUND_FACTOR = _lowGroundFactor,
            HIGH_MOUNTAIN_FACTOR = _highMountainFactor,
            FREQ_COUNT_TIMES = _freqCountTimes,
            FREQ_GROW_FACTOR = _freqGrowFactor,
            SURROUND_DOWN_OFFSET = _surroundDownOffset,
            SURROUND_DOWN_SPEED = _surroundDownSpeed,
        };
    }
}


[CustomEditor(typeof(SpriteNoise))]
public class SpriteNoiseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpriteNoise myScriptNoise = (SpriteNoise)target;
        if (GUILayout.Button("DrawTexture"))
        {
            myScriptNoise.DrawTexture();
        }

        if (GUILayout.Button("SaveMap"))
        {
            myScriptNoise.SaveMap();
        }
    }
}
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

    [SerializeField]
    private TerrainParameter _terrainParam;

    [Header("Basic Parameter")]
    [SerializeField]
    private float _xOffset;
    [SerializeField]
    private float _yOffset;
    
    private float[] _heightMap;
    private ITerrainGenerator _terrainGen = new TerrainGenerator();
    private Executor _executor = new Executor();

    void Update()
    {
        _executor.Resume(Time.deltaTime);
    }

    public void DrawTexture() {
        _executor.Clear();
        var MakeMapM = new Utility.Coroutine(_ShowHeightMap());
        _executor.Add(MakeMapM);
    }

    private IEnumerator _ShowHeightMap()
    {
        float t1 = Time.time;
        Debug.Log("[TerrainGen] generate start");
        var monad = new BlockMonad<float[]>( r => 
            _terrainGen.GenerateHeightMap(
                _spriteView.Width,
                _spriteView.Height,
                _xOffset,
                _yOffset,
                _terrainParam,
                r) );
        yield return monad.Do();

        Debug.Log("[TerrainGen] generate complete");
        _heightMap = monad.Result;
        _spriteView.SetHeightMap(_heightMap);

        float t2 = Time.time;
        Debug.LogFormat("{0}=>{1}  spent:{2}", t1, t2, t2 - t1);
    }
    
}

#if UNITY_EDITOR
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
    }
}
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Utility;

public class RandomNoise : MonoBehaviour {

    [SerializeField]
    private SpriteView _spriteView;

    [SerializeField]
    private List<Vector2> _points = new List<Vector2>();

    [SerializeField]
    private RandomPointParameter _randomParam;
    
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
        float t1 = Time.time;
        Debug.Log("[RandomPointGen] generate start");

        var monad = new BlockMonad<float[]>( r => 
            _randPointGen.GenerateRandomLocalAreaMap(
                _spriteView.Width,
                _spriteView.Height,
                _randomParam,
                r) );
        yield return monad.Do();

        Debug.Log("[RandomPointGen] generate complete");
        _localAreaMap = monad.Result;
        _points = _randPointGen.Points;
        _spriteView.SetLocalAreaMap(_localAreaMap);

        float t2 = Time.time;
        Debug.LogFormat("{0}=>{1}  spent:{2}", t1, t2, t2 - t1);
    }
}

#if UNITY_EDITOR
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
#endif
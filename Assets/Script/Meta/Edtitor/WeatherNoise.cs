using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Utility;

public class WeatherNoise : MonoBehaviour
{ 
    [SerializeField]
    private SpriteView _spriteView;

    [SerializeField]
    private WeatherParameter _weatherParam;

    [Header("Basic Parameter")]
    [SerializeField]
    private float _xOffset;
    [SerializeField]
    private float _yOffset; 
     
    private int _width;
    private int _height;
    private float[] _weatherMap;

    private IWeatherGenerator _weatherGen = new WeatherGenerator(); 

    private Executor _executor = new Executor();
    private Utility.Coroutine _weatherChange;

    void Update()
    {
        _executor.Resume(Time.deltaTime);
    }

    public void DrawTexture()
    {
        _executor.Clear();
        var MakeMapM = new Utility.Coroutine(_ShowWeatherMap());
        _executor.Add(MakeMapM);
    }
     
    public void StartWeatherChange()
    {
        _weatherChange = new Utility.Coroutine(_WeatherChangeUpdate());
        _executor.Add(_weatherChange);
    }
    public void StopWeatherChange()
    {
        if (_weatherChange != null)
            _executor.Remove(_weatherChange);
    }

    private IEnumerator _ShowWeatherMap()
    {
        Debug.Log("[WeatherGen] generate start");

        var monad = new BlockMonad<float[]>( r=> 
            _weatherGen.GenerateWeatherMap(
                _spriteView.Width,
                _spriteView.Height,
                _xOffset,
                _yOffset,
                _weatherParam,
                r) );
        yield return monad.Do();

        Debug.Log("[WeatherGen] generate complete");
        _weatherMap = monad.Result;
        _spriteView.SetTemperatureMap(_weatherMap);
    }

    private IEnumerator _WeatherChangeUpdate()
    {
        while (true)
        {
            float sleepTime = 3;
            while (sleepTime > 0)
            {
                yield return null;
                sleepTime -= Time.deltaTime;
            }

            Debug.Log("[WeatherGen] generate Next start");
            var monad = new BlockMonad<float[]>(r => _weatherGen.ChangeToNextWeather(r));
            yield return monad.Do();

            _xOffset = _weatherGen.VarietyStatus.XOffset;
            _yOffset = _weatherGen.VarietyStatus.YOffset;

            _weatherMap = monad.Result;
            _spriteView.SetTemperatureMap(_weatherMap);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WeatherNoise))]
public class WeatherNoiseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WeatherNoise myWeatherNoise = (WeatherNoise)target;
        if (GUILayout.Button("DrawTexture"))
        {
            myWeatherNoise.DrawTexture();
        }
        if (GUILayout.Button("StartWeatherChange"))
        {
            myWeatherNoise.StartWeatherChange();
        }
        if (GUILayout.Button("StopWeatherChange"))
        {
            myWeatherNoise.StopWeatherChange();
        }
    }
}
#endif
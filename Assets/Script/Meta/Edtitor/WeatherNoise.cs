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

    [Header("Basic Parameter")]
    [SerializeField]
    private float _xOffset;
    [SerializeField]
    private float _yOffset; 
    [SerializeField]
    private float _scale;
    [SerializeField]
    private int _freqCountTimes;
    [SerializeField]
    private int _freqGrowFactor;

    [Header("direction Variety")] 
    [SerializeField]
    private float _speedScaleMin;
    [SerializeField]
    private float _speedScaleMax; 
    [SerializeField]
    private float _accelerationScaleMin;
    [SerializeField]
    private float _accelerationScaleMax;

    [Header("Offset Variety")] 
    [SerializeField]
    private float _weatherShiftMin;
    [SerializeField]
    private float _weatherShiftMax;
    [SerializeField]
    private float _warmUpMax;
    [SerializeField]
    private float _coldDownMax;
     
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
        yield return _weatherGen.GenerateWeatherMap(
            _spriteView.Width, 
            _spriteView.Height,
            _xOffset,
            _yOffset,
            _GetWeatherGenPara()
            );

        Debug.Log("[WeatherGen] generate complete");
        _weatherMap = _weatherGen.WeatherMap;
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
            yield return _weatherGen.ChangeToNextWeather();

            _xOffset = _weatherGen.VarietyStatus.XOffset;
            _yOffset = _weatherGen.VarietyStatus.YOffset;

            _weatherMap = _weatherGen.WeatherMap;
            _spriteView.SetTemperatureMap(_weatherMap);
        }
    }

    private WeatherGeneratorParameter _GetWeatherGenPara()
    {
        return new WeatherGeneratorParameter()
        {
            SCALE = _scale,
            FREQ_COUNT_TIMES = _freqCountTimes,
            FREQ_GROW_FACTOR = _freqGrowFactor,

            SPEED_SCALE_MIN = _speedScaleMin,
            SPEED_SCALE_MAX = _speedScaleMax,

            ACCELERATION_SCALE_MIN = _accelerationScaleMin,
            ACCELERATION_SCALE_MAX = _accelerationScaleMax,

            WEATHER_SHIFT_MIN = _weatherShiftMin,
            WEATHER_SHIFT_MAX = _weatherShiftMax,

            WARMUP_MAX = _warmUpMax,
            COLDDOWN_MAX = _coldDownMax,
        };
    }
}


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
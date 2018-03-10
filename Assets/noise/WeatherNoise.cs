using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
    private int _times;
    [SerializeField]
    private int _freqTimes;

    [Header("direction Variety")]
    [SerializeField]
    private Vector2 _varySpeed;
    [SerializeField]
    private float _speedScaleMin;
    [SerializeField]
    private float _speedScaleMax;
    [SerializeField]
    private Vector2 _varyAcceleration;
    [SerializeField]
    private float _accelerationScaleMin;
    [SerializeField]
    private float _accelerationScaleMax;

    [Header("Offset Variety")]
    [SerializeField]
    private float _tempShift;
    [SerializeField]
    private float _tempShiftMin;
    [SerializeField]
    private float _tempShiftMax;
    [SerializeField]
    private bool _goWarm;
    [SerializeField]
    private float _warmUpMax;
    [SerializeField]
    private float _coldDownMax;

    [Header("Height Percent")]
    [SerializeField]
    private float[] _heightPercent;
    private int[] _nums;

    [System.Serializable]
    public class TerrainColor
    {
        [SerializeField]
        public int Grid;
        [SerializeField]
        public Color Color;
    }
    [Header("temperature Color")]
    [SerializeField]
    private int _grid;
    [SerializeField]
    private List<TerrainColor> _weathers;

    private int _width;
    private int _height;
    private float[] _weatherMap;

    private Color[] _colors;

    public void NewRandom()
    {
        _xOffset = Random.Range(-10000f, 10000f);
        _yOffset = Random.Range(-10000f, 10000f);
        _varySpeed = new Vector2(Random.value * 2f - 1f, Random.value * 2f - 1f).normalized;
        _varyAcceleration = new Vector2(Random.value * 2f - 1f, Random.value * 2f - 1f).normalized;

        _tempShift = Random.Range(_tempShiftMin, _tempShiftMax);
        _goWarm = Random.value > 0.5f;
    }
    public void NextRandom()
    { 
        var speed = _varySpeed * Random.Range(_speedScaleMin, _speedScaleMax);
        _xOffset += speed.x;
        _yOffset += speed.y;
        var accerleration = _varyAcceleration * Random.Range(_accelerationScaleMin, _accelerationScaleMax);
        _varySpeed = (_varySpeed + accerleration).normalized;
        _varyAcceleration = new Vector2(Random.value*2f-1f, Random.value*2f-1f).normalized;
         
        if (_goWarm) {
            _tempShift += Random.Range(_tempShiftMin, _tempShiftMax);
            _goWarm = _tempShift < _warmUpMax;
        }
        else {
            _tempShift -= Random.Range(_tempShiftMin, _tempShiftMax);
            _goWarm = _tempShift < _coldDownMax;
        }
    }

    public float[] MakeWeatherMap(int width, int height)
    {
        _width = width;
        _height = height;
        _weatherMap = new float[_width * _height];

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {

                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;

                float sample = _CountPerlinNoise( xCoord, yCoord );
                _weatherMap[ y * _width +  x] = sample;
            }
        }

        return _weatherMap;
    } 

    public void DrawTexture() {
        MakeWeatherMap(_spriteView.Width, _spriteView.Height);
        _SetColors();
        _spriteView.SetPixels(_colors);
    }

    public void PassNextTurn() {
        NextRandom();
        MakeWeatherMap(_spriteView.Width, _spriteView.Height);
        _tempratureChange();
        _SetColors();
        _spriteView.SetPixels(_colors);
    }

    private float _CountPerlinNoise(float xCoord, float yCoord) {

        float sample = 0f;
        sample += _CountRecursivePerlinNoise(
            xCoord, yCoord,
            _xOffset, _yOffset, _scale,
            _times, _freqTimes );

        return sample;
    }

    private float _CountRecursivePerlinNoise(
        float xCoord, float yCoord,
        float xOffset, float yOffset, float scale,
        int times, float freqTimes)
    {
        float sample = 0f;
        float freqTime = 1f;
        float sampleTimes = 0f;
        for (int i = 0; i < times; i++)
        {
            sample += (1 / freqTime) *
                Mathf.PerlinNoise(
                    scale * (xCoord + xOffset) * freqTime ,
                    scale * (yCoord + yOffset) * freqTime );
            sampleTimes += (1 / freqTime);
            freqTime *= freqTimes;
        }
        sample /= sampleTimes;

        return sample;
    }

    private void _tempratureChange() {
        
        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                  
                var sample = _weatherMap[ y * _width +  x];
                
                if (_tempShift > 0) {
                    _weatherMap[y * _width + x] = _tempShift + sample * (1 - _tempShift); 
                }
                else {
                    _weatherMap[y * _width + x] = sample * (1 + _tempShift);
                }
                
            }
        }
    }

    private Color[] _SetColors()
    {
        _nums = new int[_grid];
        _colors = new Color[_spriteView.Total];

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                  
                var sample = _weatherMap[ y * _width +  x];
                _CountGridNum(sample);
                _colors[y * _width + x] = _CountSampleColor(sample);
            }
        }
        _CountGridPercent();
        return _colors;
    }

    private void _CountGridNum(float heightSample)
    {
        for (int i = 0; i < _grid; i++) {
            if (heightSample < (float)(i + 1) / _grid) {
                _nums[i]++;
                break;
            }
        }
    }
    private void _CountGridPercent()
    {
        _heightPercent = new float[_grid];
        for (int i = 0; i < _grid; i++)
        {
            _heightPercent[i] = (float)_nums[i] / _spriteView.Total;
        }
    }
    private Color _CountSampleColor(float heightSample)
    {
        for (int i = 1; i < _weathers.Count; i++)
        {
            var weather1 = _weathers[i - 1];
            var weather2 = _weathers[i];
            if (heightSample < (float)weather2.Grid / (float)_grid)
            {
                var color1 = weather1.Color;
                var color2 = weather2.Color;
                float num1 = heightSample - (float)weather1.Grid / (float)_grid;
                float num2 = (float)weather2.Grid / (float)_grid - (float)weather1.Grid / (float)_grid;
                return Color.Lerp(color1, color2, Mathf.PingPong(num1 / num2, 1));
            }
        }
        return _weathers[_weathers.Count - 1].Color;
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
        if (GUILayout.Button("NextTurn"))
        {
            myWeatherNoise.PassNextTurn();
        }
        if (GUILayout.Button("NewRandom"))
        {
            myWeatherNoise.NewRandom();
        }
    }
}
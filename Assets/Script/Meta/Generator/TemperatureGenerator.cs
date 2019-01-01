using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public interface ITemperatureGenerator
{
    WeatherVarietyStatus VarietyStatus { get; }
    IEnumerator GenerateWeatherMap(
           int width,
           int height,
           float xOffset,
           float yOffset,
           TemperatureParameter para,
           IReturn<float[]> ret);
}

public class TemperatureGenerator : ITemperatureGenerator
{
    private IWeatherGenerator _weatherGen = new WeatherGenerator();
    private TemperatureParameter _para;

    private int _width;
    private int _height;

    private WeatherVarietyStatus _varietyStatus;
    public WeatherVarietyStatus VarietyStatus
    {
        get { return _varietyStatus; }
    }
    
    private float[] _temperatureMap;
    public float[] TemperatureMap
    {
        get { return _temperatureMap; }
    }

    public IEnumerator GenerateWeatherMap(
        int width,
        int height,
        float xOffset,
        float yOffset,
        TemperatureParameter para,
        IReturn<float[]> ret)
    {
        _width = width;
        _height = height;
        _para = para;

        _temperatureMap = new float[_width * _height];

        var weatherMonad = new BlockMonad<float[]>(r => 
            _weatherGen.GenerateWeatherMap(_width, _height, xOffset, yOffset, _para.WEATHER_GEN_PARA, r));
        yield return weatherMonad.Do();
        _varietyStatus = _weatherGen.VarietyStatus;
        var varietyMap = weatherMonad.Result;

        _GenerateMainTemperature(varietyMap);

        ret.Accept(_temperatureMap);
    }

    private void _GenerateMainTemperature(float[] varietyMap)
    { 
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;
                var idx = MathUtility.MapIndex(x, y, _height);
                var _paraTemperatureDelta = _para.MAIN_TEMPERATURE_MAX - _para.MAIN_TEMPERATURE_MIN;

                var temperature = _para.MAIN_TEMPERATURE_MIN +
                    _paraTemperatureDelta * _GetDistanceFromEquatorial(xCoord, yCoord);
                temperature += (varietyMap[idx] - 0.5f) * _para.VARIETY_TEMPERATURE_MAX;
                temperature = Mathf.Clamp01(temperature);

                _temperatureMap[idx] = temperature;
            }
        }
    }

    private float _GetDistanceFromEquatorial(float x, float y)
    {
        return 1 - (Mathf.Abs(y - 0.5f) / 0.5f);
    }
}

﻿

using System.Collections;
using UnityEngine;

public interface IWeatherGenerator
{
    float[] WeatherMap { get; }
    WeatherVarietyStatus VarietyStatus { get; }
    IEnumerator GenerateWeatherMap(
           int width,
           int height,
           float xOffset,
           float yOffset,
           WeatherGeneratorParameter para);
    IEnumerator ChangeToNextWeather();
}

public class WeatherGenerator : IWeatherGenerator
{ 
    private int _width;
    private int _height;

    private WeatherVarietyStatus _varietyStatus;
    public WeatherVarietyStatus VarietyStatus
    {
        get { return _varietyStatus; }
    }

    private WeatherGeneratorParameter _para;

    private float[] _weatherMap;
    public float[] WeatherMap
    {
        get { return _weatherMap; }
    }

    private int _sleepCount = 0;
    private int _sleepMax = 500;

    public IEnumerator GenerateWeatherMap(
        int width,
        int height,
        float xOffset,
        float yOffset,
        WeatherGeneratorParameter para)
    { 
        _width = width;
        _height = height;
        _para = para;

        _varietyStatus = new WeatherVarietyStatus(xOffset, yOffset);

        _weatherMap = new float[_width * _height];

        yield return _GenerateWeatherMap();
    }
    
    private IEnumerator _GenerateWeatherMap()
    { 
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;

                float sample = _CountPerlinNoise(xCoord, yCoord);

                sample = _TempratureChange(sample);

                _weatherMap[y * _width + x] = sample;
                 
                if (_sleepCount++ > _sleepMax)
                {
                    yield return null;
                    _sleepCount = 0;
                }
            }
        }
    }

    public IEnumerator ChangeToNextWeather()
    {
        if (_para == null) yield break;

        NextRandom();
        yield return _GenerateWeatherMap(); 
    }

    private float _CountPerlinNoise(float xCoord, float yCoord)
    {
        float sample = 0f;

        sample += NoiseUtility.CountRecursivePerlinNoise(
            xCoord + _varietyStatus.XOffset,
            yCoord + _varietyStatus.YOffset,
            0,
            0,
            _para.SCALE,
            _para.FREQ_COUNT_TIMES,
            _para.FREQ_GROW_FACTOR);

        return sample;
    }

    public void NextRandom()
    {
        var accerleration = new Vector2(Random.value * 2f - 1f, Random.value * 2f - 1f).normalized * 
            Random.Range(_para.ACCELERATION_SCALE_MIN, _para.ACCELERATION_SCALE_MAX);
        _varietyStatus.SpeedVariety = (_varietyStatus.SpeedVariety + accerleration).normalized;

        var speed = _varietyStatus.SpeedVariety * Random.Range(_para.SPEED_SCALE_MIN, _para.SPEED_SCALE_MAX);
        _varietyStatus.XOffset += speed.x;
        _varietyStatus.YOffset += speed.y;

        if (_varietyStatus.GoWarm)
        {
            _varietyStatus.WeatherShift += Random.Range(_para.WEATHER_SHIFT_MIN, _para.WEATHER_SHIFT_MAX);
            _varietyStatus.GoWarm = _varietyStatus.WeatherShift < _para.WARMUP_MAX;
        }
        else
        {
            _varietyStatus.WeatherShift -= Random.Range(_para.WEATHER_SHIFT_MIN, _para.WEATHER_SHIFT_MAX);
            _varietyStatus.GoWarm = _varietyStatus.WeatherShift < _para.COLDDOWN_MAX;
        }
    }
     
    private float _TempratureChange(float sample)
    {
        if (_varietyStatus.WeatherShift > 0)
        {
            return _varietyStatus.WeatherShift + sample * (1 - _varietyStatus.WeatherShift);
        }
        else if(_varietyStatus.WeatherShift < 0)
        {
            return sample * (1 + _varietyStatus.WeatherShift);
        }
        return sample;
    }
}

public class WeatherGeneratorParameter
{
    public float SCALE = 6;
    public int FREQ_COUNT_TIMES = 3;
    public int FREQ_GROW_FACTOR = 2;

    // Should Move Very SLOW
    public float SPEED_SCALE_MIN = 0;
    public float SPEED_SCALE_MAX = 0.001f;

    public float ACCELERATION_SCALE_MIN = 0.4f;
    public float ACCELERATION_SCALE_MAX = 0.8f;

    // Should Shift Very SLOW
    public float WEATHER_SHIFT_MIN = 0;
    public float WEATHER_SHIFT_MAX = 0.01f;

    public float WARMUP_MAX = 0.05f;
    public float COLDDOWN_MAX = -0.05f;
}

public class WeatherVarietyStatus
{
    public float XOffset;
    public float YOffset;

    public Vector2 SpeedVariety;

    public float WeatherShift;
    public bool GoWarm;

    public WeatherVarietyStatus(float xInitOffset, float yInitOffset)
    {
        XOffset = xInitOffset;
        YOffset = yInitOffset;
    }
}
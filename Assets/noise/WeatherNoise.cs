﻿using System.Collections;
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

    private int _width;
    private int _height;
    private float[] _weatherMap;

    private Color[] _colors;

    public void NewRandom()
    {
        _xOffset = Random.Range(-10000f, 10000f);
        _yOffset = Random.Range(-10000f, 10000f);
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

                float sample = _CountPerlinNoise(xCoord, yCoord);
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
                    scale * xCoord * freqTime + xOffset,
                    scale * yCoord * freqTime + yOffset);
            sampleTimes += (1 / freqTime);
            freqTime *= freqTimes;
        }
        sample /= sampleTimes;

        return sample;
    }

    private void _SetColors() {
        _colors = new Color[_spriteView.Total];
        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                  
                var sample = _weatherMap[ y * _width +  x];
                _colors[y * _width + x] = new Color(sample, sample, sample);
            }
        } 
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
        if (GUILayout.Button("NewRandom"))
        {
            myWeatherNoise.NewRandom();
        }
    }
}
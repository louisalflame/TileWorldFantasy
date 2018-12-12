using System.Collections.Generic;
using UnityEngine;

public class SpriteView : MonoBehaviour
{
    [Header("Sprite Basic")]
    public SpriteRenderer _spriteRenderer;
    [SerializeField]
    private int _pixWidth;
    [SerializeField]
    private int _pixHeight;

    [Header("Height Percent")]
    private int[] _heightNums;
    [SerializeField]
    private float[] _heightPercent;
    [SerializeField]
    private ColorRangeDistribution _terrainColor;

    [Header("Temperature Percent")]
    private int[] _temperatureNums;
    [SerializeField]
    private float[] _temperaturePercent;
    [SerializeField]
    private ColorRangeDistribution _temperatureColor;

    private Color[] _colors;
    private Texture2D _noiseTex;
    private Sprite _sprite;

    public Texture2D NoiseTex {
        get { return _noiseTex; }
    }
    public int Width {
        get { return _noiseTex.width; }
    }
    public int Height {
        get { return _noiseTex.height; }
    }
    public int Total {
        get { return _noiseTex.width * _noiseTex.height; }
    }

    public void SetPixels(Color[] pixels) {
        _noiseTex.SetPixels(pixels);
        _noiseTex.Apply();
    }

#region Terrain
    void Start()
    {
        ResetTexture(_pixWidth, _pixWidth);
    }

    public void ResetTexture(int width, int height)
    {
        _noiseTex = new Texture2D(width, height);

        var rect = new Rect(0, 0, _noiseTex.width, _noiseTex.height);
        _sprite = Sprite.Create(_noiseTex, rect, new Vector2(0.5f, 0.5f), 100);
        _spriteRenderer.sprite = _sprite;
    }

    public void SetHeightMap(float[] heightMap)
    {  
        if (Total > heightMap.Length) { return; }

        _heightPercent = new float[_terrainColor.TotalGrid];
        _heightNums = new int[_terrainColor.TotalGrid];
        _colors = new Color[Total];

        for (int x = 0; x < _noiseTex.width; x++) {
            for (int y = 0; y < _noiseTex.height; y++) {

                int index = y * _noiseTex.width + x; 
                float height = heightMap[index];

                _CountTerrainGridNum(height);
                _colors[index] = _terrainColor.GetLerpColor(height);
            }
        }
        _CountTerrainGridPercent();

        _noiseTex.SetPixels(_colors);
        _noiseTex.Apply();
    }

    private void _CountTerrainGridNum(float heightSample) {
        for (int i = 0; i < _terrainColor.TotalGrid; i++) {
            if (heightSample < (float)(i+1) / _terrainColor.TotalGrid) {
                _heightNums[i]++;
                break;
            }
        }
    }
    private void _CountTerrainGridPercent() {
        for (int i = 0; i < _terrainColor.TotalGrid; i++) {
            _heightPercent[i] = (float)_heightNums[i] / Total;
        }
    }

    private Color _CountTerrainColor(float heightSample) { 
        for (int i = 1; i < _terrainColor.Colors.Length; i++) {
            var terrain1 = _terrainColor.Colors[i-1];
            var terrain2 = _terrainColor.Colors[i];
            if (heightSample < (float)terrain2.Grid / _terrainColor.TotalGrid)
            {
                var color1 = terrain1.Color;
                var color2 = terrain2.Color;
                float num1 = heightSample - (float)terrain1.Grid / _terrainColor.TotalGrid;
                float num2 = (float)terrain2.Grid / _terrainColor.TotalGrid - (float)terrain1.Grid / _terrainColor.TotalGrid;
                return  Color.Lerp(color1, color2,  Mathf.PingPong(num1 / num2, 1));
            }
        }
        return _terrainColor.Colors[_terrainColor.Colors.Length - 1].Color;
    }
#endregion

#region Temperature
    public void SetTemperatureMap(float[] temperatureMap)
    {
        if (Total > temperatureMap.Length) { return; }

        _temperaturePercent = new float[_temperatureColor.TotalGrid];
        _temperatureNums = new int[_temperatureColor.TotalGrid];
        _colors = new Color[Total];

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++)  {

                int index = y * _noiseTex.width + x;
                var sample = temperatureMap[index];
                _CountTemperatureGridNum(sample);
                _colors[index] = _CountTemperatureSampleColor(sample);
            }
        }
        _CountTemperatureGridPercent();

        _noiseTex.SetPixels(_colors);
        _noiseTex.Apply();
    } 
    private void _CountTemperatureGridNum(float heightSample)
    {
        for (int i = 0; i < _temperatureColor.TotalGrid; i++) {
            if (heightSample < (float)(i + 1) / _temperatureColor.TotalGrid) {
                _temperatureNums[i]++;
                break;
            }
        }
    }
    private void _CountTemperatureGridPercent()
    {
        for (int i = 0; i < _temperatureColor.TotalGrid; i++) {
            _temperaturePercent[i] = (float)_temperatureNums[i] / Total;
        }
    }
    private Color _CountTemperatureSampleColor(float heightSample)
    {
        for (int i = 1; i < _temperatureColor.Colors.Length; i++)
        {
            var weather1 = _temperatureColor.Colors[i - 1];
            var weather2 = _temperatureColor.Colors[i];
            if (heightSample < (float)weather2.Grid / _temperatureColor.TotalGrid)
            {
                var color1 = weather1.Color;
                var color2 = weather2.Color;
                float num1 = heightSample - (float)weather1.Grid / _temperatureColor.TotalGrid;
                float num2 = (float)weather2.Grid / _temperatureColor.TotalGrid - (float)weather1.Grid / _temperatureColor.TotalGrid;
                return Color.Lerp(color1, color2, Mathf.PingPong(num1 / num2, 1));
            }
        }
        return _temperatureColor.Colors[_temperatureColor.Colors.Length - 1].Color;
    }
    #endregion

    #region RandomPoint
    public void SetLocalAreaMap(float[] localAreaMap)
    {
        if (Total > localAreaMap.Length) { return; }
         
        _colors = new Color[Total];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {

                int index = y * _noiseTex.width + x;
                var sample = localAreaMap[index];

                _colors[index] = new Color(sample, sample, sample);
            }
        } 

        _noiseTex.SetPixels(_colors);
        _noiseTex.Apply();
    }
    #endregion
}
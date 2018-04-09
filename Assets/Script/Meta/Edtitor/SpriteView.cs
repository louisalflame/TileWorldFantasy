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
    private Color[] _colors;

    private Texture2D _noiseTex;
    private Sprite _sprite;

    [Header("Height Percent")]
    [SerializeField]
    private float[] _heightPercent;
    private int[] _heightNums;

    [System.Serializable]
    public class TerrainColor
    {
        [SerializeField]
        public int Grid;
        [SerializeField]
        public Color Color;
    }
    [Header("Terrain Color")]
    [SerializeField]
    private int _terrainGrid;
    [SerializeField]
    private List<TerrainColor> _terrains;

    [Header("Temperature Percent")]
    [SerializeField]
    private float[] _temperaturePercent;
    private int[] _temperatureNums;

    [System.Serializable]
    public class TemperatureColor
    {
        [SerializeField]
        public int Grid;
        [SerializeField]
        public Color Color;
    }
    [Header("temperature Color")]
    [SerializeField]
    private int _temperatureGrid;
    [SerializeField]
    private List<TemperatureColor> _temperatures;

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

        _heightPercent = new float[_terrainGrid];
        _heightNums = new int[_terrainGrid];
        _colors = new Color[Total];

        for (int x = 0; x < _noiseTex.width; x++) {
            for (int y = 0; y < _noiseTex.height; y++) {

                int index = y * _noiseTex.width + x; 
                float height = heightMap[index];

                _CountTerrainGridNum(height);
                _colors[index] = _CountTerrainColor(height);
            }
        }
        _CountTerrainGridPercent();

        _noiseTex.SetPixels(_colors);
        _noiseTex.Apply();
    }

    private void _CountTerrainGridNum(float heightSample) {
        for (int i = 0; i < _terrainGrid; i++) {
            if (heightSample < (float)(i+1) / _terrainGrid) {
                _heightNums[i]++;
                break;
            }
        }
    }
    private void _CountTerrainGridPercent() {
        for (int i = 0; i < _terrainGrid; i++) {
            _heightPercent[i] = (float)_heightNums[i] / Total;
        }
    }

    private Color _CountTerrainColor(float heightSample) { 
        for (int i = 1; i < _terrains.Count; i++) {
            var terrain1 = _terrains[i-1];
            var terrain2 = _terrains[i];
            if (heightSample < (float)terrain2.Grid / _terrainGrid)
            {
                var color1 = terrain1.Color;
                var color2 = terrain2.Color;
                float num1 = heightSample - (float)terrain1.Grid / _terrainGrid;
                float num2 = (float)terrain2.Grid / _terrainGrid - (float)terrain1.Grid / _terrainGrid;
                return  Color.Lerp(color1, color2,  Mathf.PingPong(num1 / num2, 1));
            }
        }
        return _terrains[_terrains.Count - 1].Color;
    }
#endregion

#region Temperature
    public void SetTemperatureMap(float[] temperatureMap)
    {
        if (Total > temperatureMap.Length) { return; }

        _temperaturePercent = new float[_temperatureGrid];
        _temperatureNums = new int[_temperatureGrid];
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
        for (int i = 0; i < _temperatureGrid; i++) {
            if (heightSample < (float)(i + 1) / _temperatureGrid) {
                _temperatureNums[i]++;
                break;
            }
        }
    }
    private void _CountTemperatureGridPercent()
    {
        for (int i = 0; i < _temperatureGrid; i++) {
            _temperaturePercent[i] = (float)_temperatureNums[i] / Total;
        }
    }
    private Color _CountTemperatureSampleColor(float heightSample)
    {
        for (int i = 1; i < _temperatures.Count; i++)
        {
            var weather1 = _temperatures[i - 1];
            var weather2 = _temperatures[i];
            if (heightSample < (float)weather2.Grid / _temperatureGrid)
            {
                var color1 = weather1.Color;
                var color2 = weather2.Color;
                float num1 = heightSample - (float)weather1.Grid / _temperatureGrid;
                float num2 = (float)weather2.Grid / _temperatureGrid - (float)weather1.Grid / _temperatureGrid;
                return Color.Lerp(color1, color2, Mathf.PingPong(num1 / num2, 1));
            }
        }
        return _temperatures[_temperatures.Count - 1].Color;
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
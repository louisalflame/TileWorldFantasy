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
    private int[] _nums;

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
    private int _grid = 20;
    [SerializeField]
    private List<TerrainColor> _terrains;

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

    private void Start()
    { 
        _noiseTex = new Texture2D(_pixWidth, _pixHeight);

        var rect = new Rect(0, 0, _noiseTex.width, _noiseTex.height);
        _sprite = Sprite.Create(_noiseTex, rect, new Vector2(0.5f, 0.5f), 100);
        _spriteRenderer.sprite = _sprite;
    }

    public void SetHeightMap(float[] _heightMap)
    {  
        if (Total > _heightMap.Length) { return; }

        _nums = new int[_grid];
        _colors = new Color[Total];

        for (int x = 0; x < _noiseTex.width; x++) {
            for (int y = 0; y < _noiseTex.height; y++) {

                int index = y * _noiseTex.width + x; 
                float height = _heightMap[index];

                _CountGridNum(height);
                _colors[index] = _CountSampleColor(height);
            }
        }
        _CountGridPercent();

        _noiseTex.SetPixels(_colors);
        _noiseTex.Apply();
    }

    public void SetPixels(Color[] pixels) {
        _noiseTex.SetPixels(pixels);
        _noiseTex.Apply();
    }

    private void _CountGridNum(float heightSample) {
        for (int i = 0; i < _grid; i++) {
            if (heightSample < (float)(i+1) / _grid) {
                _nums[i]++;
                break;
            }
        }
    }
    private void _CountGridPercent() {
        _heightPercent = new float[_grid];
        for (int i = 0; i < _grid; i++) {
            _heightPercent[i] = (float)_nums[i] / Total;
        }
    }

    private Color _CountSampleColor(float heightSample) { 
        for (int i = 1; i < _terrains.Count; i++) {
            var terrain1 = _terrains[i-1];
            var terrain2 = _terrains[i];
            if (heightSample < (float)terrain2.Grid / (float)_grid)
            {
                var color1 = terrain1.Color;
                var color2 = terrain2.Color;
                float num1 = heightSample - (float)terrain1.Grid / (float)_grid;
                float num2 = (float)terrain2.Grid / (float)_grid - (float)terrain1.Grid / (float)_grid;
                return  Color.Lerp(color1, color2,  Mathf.PingPong(num1 / num2, 1));
            }
        }
        return _terrains[_terrains.Count - 1].Color;
    }
}
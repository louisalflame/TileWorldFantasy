using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SpriteNoise : MonoBehaviour {

    [SerializeField]
    private SpriteView _spriteView;
    [SerializeField]
    private RandomNoise _randomNoise;

    [Header("Basic Parameter")]
    [SerializeField]
    private float _xOffset;
    [SerializeField]
    private float _yOffset;
    [SerializeField]
    private float _scale;
    [SerializeField]
    private float _lowGround;
    [SerializeField]
    private float _highMountain;
     
    [Header("Recursive Mode")]
    [SerializeField]
    private int _times;
    [SerializeField]
    private int _freqTimes;

    [Header("Island Mode")]
    [SerializeField]
    private float _downOffset;
    [SerializeField]
    private float _downSpeed;

    [Header("Mountain Mode")] 
    [SerializeField]
    private int _upNums;
    [SerializeField]
    private float _radius;
    [SerializeField]
    private float _upScale;
    [SerializeField]
    private float _upSpeed;
    [SerializeField]
    private List<Vector2> _upPoints = new List<Vector2>();

    private int _width;
    private int _height;
    private float[] _heightMap;
         
    public void NewRandom()
    {
        _xOffset = Random.Range(-10000f, 10000f);
        _yOffset = Random.Range(-10000f, 10000f);
        _upPoints = _randomNoise.GetRandomLoosePoints(_upNums);
    }

    public float[] MakeHeightMap(int width, int height)
    {
        _width = width;
        _height = height;
        _heightMap = new float[_width * _height];

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {

                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;

                var sample = _CountPerlinNoise(xCoord, yCoord);

                _heightMap[ y * _width + x] = sample;
            }
        }

        return _heightMap;
    }

    public void DrawTexture() {
        MakeHeightMap(_spriteView.Width, _spriteView.Height);
        _spriteView.SetHeightMap(_heightMap);
    }

    private float _CountPerlinNoise(float xCoord, float yCoord)
    {
        float sample = 0f; 
        
        sample += _CountRecursivePerlinNoise(
            xCoord, yCoord,
            _xOffset, _yOffset, _scale,
            _times, _freqTimes );
         
        for (int i = 0; i < _upPoints.Count; i++)
        {
            var point = new Vector2(
                _upPoints[i].x * _spriteView.Width,
                _upPoints[i].y * _spriteView.Height);
            var coord = new Vector2(
                xCoord * _spriteView.Width,
                yCoord * _spriteView.Height);

            float distance = Vector2.Distance(point, coord);
            float upDegree = _radius - distance;
            sample += _upScale * Mathf.Pow(_upSpeed, upDegree);
        }

        sample = Mathf.Pow(sample, _lowGround);
        sample = 1 - Mathf.Pow(1 - sample, _highMountain);

        float dis = Vector2.Distance(
            new Vector2(5f, 5f),
            new Vector2(10 * xCoord, 10 * yCoord));
        sample = sample * (1 - _downOffset * Mathf.Pow(dis, _downSpeed));

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
}


[CustomEditor(typeof(SpriteNoise))]
public class SpriteNoiseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpriteNoise myScriptNoise = (SpriteNoise)target;
        if (GUILayout.Button("DrawTexture"))
        {
            myScriptNoise.DrawTexture();
        }

        if (GUILayout.Button("NewRandom"))
        {
            myScriptNoise.NewRandom();
        }
    }
}
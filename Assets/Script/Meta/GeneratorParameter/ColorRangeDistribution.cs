using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/ColorRange", order = 14)]
public class ColorRangeDistribution : ScriptableObject
{
    public int TotalGrid;
    public DistributionColor[] Colors;

    public Color GetLerpColor(float sample)
    {
        for (int i = 1; i < Colors.Length; i++)
        {
            var terrain1 = Colors[i - 1];
            var terrain2 = Colors[i];
            if (sample < (float)terrain2.Grid / TotalGrid)
            {
                var color1 = terrain1.Color;
                var color2 = terrain2.Color;
                float num1 = sample - (float)terrain1.Grid / TotalGrid;
                float num2 = (float)terrain2.Grid / TotalGrid - (float)terrain1.Grid / TotalGrid;
                return Color.Lerp(color1, color2, Mathf.PingPong(num1 / num2, 1));
            }
        }
        return Colors[Colors.Length - 1].Color;
    }
}

[System.Serializable]
public class DistributionColor
{
    [SerializeField]
    public int Grid;
    [SerializeField]
    public Color Color;
}
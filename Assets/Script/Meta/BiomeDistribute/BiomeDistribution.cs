using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/BiomeDistribution", order = 2)]
public class BiomeDistribution : ScriptableObject {

    const int DefaultSize = 1;
    
    [Serializable]
    public class BiomeHeight
    {
        public BiomeTemperature[] BiomeHeights = new BiomeTemperature[DefaultSize];
    }

    [Serializable]
    public class BiomeTemperature {
        public BiomeData[] BiomeTemperatures = new BiomeData[DefaultSize];
    }

    public int HumidityVariety = DefaultSize;
    public int HeightVariety = DefaultSize;
    public int TemperatureVariety = DefaultSize;

    public int[] HumidityRange = new int[DefaultSize];
    public int[] HeightRange = new int[DefaultSize];
    public int[] TemperatureRange = new int[DefaultSize];

    public BiomeHeight[] BiomeHumiditys = new BiomeHeight[DefaultSize];
}

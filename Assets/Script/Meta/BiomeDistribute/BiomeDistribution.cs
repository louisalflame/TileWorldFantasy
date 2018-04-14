using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/BiomeDistribution", order = 2)]
public class BiomeDistribution : ScriptableObject {

    const int DefaultSize = 1;

    [Serializable]
    public class BiomeTemperature {
        public BiomeData[] BiomeTemperatures = new BiomeData[DefaultSize];
    }
    
    [Serializable]
    public class BiomeHeight
    {
        public BiomeTemperature[] BiomeHeights = new BiomeTemperature[DefaultSize];
    }

    public int HumidityVariety = DefaultSize;
    public int HeightVariety = DefaultSize;
    public int TemperatureVariety = DefaultSize;

    public BiomeHeight[] BiomeHumuditys = new BiomeHeight[DefaultSize];
}

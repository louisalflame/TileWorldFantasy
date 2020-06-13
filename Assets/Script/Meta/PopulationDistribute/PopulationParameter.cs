using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/ParamPopulation", order = 16)]
public class PopulationParameter : ScriptableObject
{
    const int DefaultSize = 1;

    public int HumidityVariety = DefaultSize;
    public int HeightVariety = DefaultSize;
    public int TemperatureVariety = DefaultSize;

    public float BasicProbability;
    public float[] HumidityProbability = new float[DefaultSize];
    public float[] HeightProbability = new float[DefaultSize];
    public float[] TemperatureProbability = new float[DefaultSize];
    
}

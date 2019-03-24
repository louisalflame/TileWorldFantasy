using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/ParamPopulation", order = 16)]
public class PopulationParameter : ScriptableObject
{
    public int MinHeightIdx = 2;
    public int MaxHeightIdx = 4;
    public int MinHumidityIdx = 1;
    public int MaxHumidityIdx = 4;
    public int MinTemperatureIdx = 1;
    public int MaxTemperatureIdx = 5;
     
}

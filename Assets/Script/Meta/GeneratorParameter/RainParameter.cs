using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/ParamRain", order = 15)]
public class RainParameter : ScriptableObject
{
    public float HEIGHT_LOCATIONS;
    public int RAIN_COUNT_TIMES;
    public int RIVER_LENGTH;
    public float RIVER_END_HEIGHT;
    public float RIVER_AVERAGE_FACTOR;
}

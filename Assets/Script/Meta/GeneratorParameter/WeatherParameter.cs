using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
[CreateAssetMenu(menuName = "World/ParamWeather", order = 13)]
public class WeatherParameter : ScriptableObject
{
    public float SCALE;
    public int FREQ_COUNT_TIMES;
    public int FREQ_GROW_FACTOR;

    // Should Move Very SLOW
    public float SPEED_SCALE_MIN;
    public float SPEED_SCALE_MAX;

    public float ACCELERATION_SCALE_MIN;
    public float ACCELERATION_SCALE_MAX;

    // Should Shift Very SLOW
    public float WEATHER_SHIFT_MIN;
    public float WEATHER_SHIFT_MAX;

    public float WARMUP_MAX;
    public float COLDDOWN_MAX;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/ParamTemperature", order = 16)]
public class TemperatureParameter : ScriptableObject
{
    public float MAIN_TEMPERATURE_MIN;
    public float MAIN_TEMPERATURE_MAX;
    public float VARIETY_TEMPERATURE_MAX;
    public WeatherParameter WEATHER_GEN_PARA;
}

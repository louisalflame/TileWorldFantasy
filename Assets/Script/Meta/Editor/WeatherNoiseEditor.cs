using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeatherNoise))]
public class WeatherNoiseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WeatherNoise myWeatherNoise = (WeatherNoise)target;
        if (GUILayout.Button("DrawTexture"))
        {
            myWeatherNoise.DrawTexture();
        }
        if (GUILayout.Button("StartWeatherChange"))
        {
            myWeatherNoise.StartWeatherChange();
        }
        if (GUILayout.Button("StopWeatherChange"))
        {
            myWeatherNoise.StopWeatherChange();
        }
    }
}

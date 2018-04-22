using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomeDistribution))]
public class BiomeDistributionEditor : Editor
{ 
    private readonly Vector2 _defaultCellSize = new Vector2(150, 20);// px
    private Vector2 _scrollPos;

    private SerializedProperty _humidityVariety;
    private SerializedProperty _temperatureVariety;
    private SerializedProperty _heightVariety;
    private SerializedProperty _biomeHumiditys;

    private Rect _lastRect;

    void OnEnable()
    {
        _humidityVariety = serializedObject.FindProperty("HumidityVariety");
        _temperatureVariety = serializedObject.FindProperty("TemperatureVariety");
        _heightVariety = serializedObject.FindProperty("HeightVariety");
        _biomeHumiditys = serializedObject.FindProperty("BiomeHumiditys");
    }

    public override void OnInspectorGUI()
    {
        try
        {
            _OnInspectorGUI();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void _OnInspectorGUI()
    {
        // Always do this at the beginning of InspectorGUI.
        serializedObject.Update();

        _CheckAndUpdateVariety();

        EditorGUILayout.Space();

        if (Event.current.type == EventType.Repaint)
        {
            _lastRect = GUILayoutUtility.GetLastRect();
        }

        _DisplayGrid(_lastRect); 

        // Apply changes to all serializedProperties - always do this at the end of OnInspectorGUI.
        serializedObject.ApplyModifiedProperties();
    }

    private void _CheckAndUpdateVariety()
    {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_humidityVariety);
        EditorGUILayout.PropertyField(_heightVariety);
        EditorGUILayout.PropertyField(_temperatureVariety);
        if (EditorGUI.EndChangeCheck())
        {
            _UpdateBiomeVariety(_humidityVariety.intValue, _temperatureVariety.intValue, _heightVariety.intValue);
        }
    }

    private void _UpdateBiomeVariety(int humiditys, int temperatures, int heights)
    {
        _biomeHumiditys.ClearArray();

        for (int i = 0; i < humiditys; i++)
        {
            _biomeHumiditys.InsertArrayElementAtIndex(i);
            SerializedProperty biomeheights = _GetBiomeheightsAt(_biomeHumiditys, i);

            for (int j = 0; j < temperatures; j++)
            {
                biomeheights.InsertArrayElementAtIndex(j);
                SerializedProperty biomeTemperatures = _GetBiomeTemperaturesAt(biomeheights, j);

                for (int k = 0; k < heights; k++)
                {
                    biomeTemperatures.InsertArrayElementAtIndex(k);
                }
            }
        } 
    }

    private void _DisplayGrid(Rect startRect)
    {
        Rect cellPosition = startRect;

        cellPosition.y += _defaultCellSize.y; // Same as EditorGUILayout.Space(), but in Rect
        cellPosition.size = _defaultCellSize;
        
        float startLineX = cellPosition.x;
        for (int i = 0; i < _humidityVariety.intValue; i++)
        {
            SerializedProperty biomeheights = _GetBiomeheightsAt(_biomeHumiditys, i);
            
            for (int j = 0; j < _temperatureVariety.intValue; j++)
            {
                SerializedProperty biomeTemperatures = _GetBiomeTemperaturesAt(biomeheights, j);
                cellPosition.x = startLineX; // Get back to the beginning of the line

                for (int k = 0; k < _heightVariety.intValue; k++)
                {
                    SerializedProperty biome = _GetBiomeAt(biomeTemperatures, k);
                    EditorGUI.PropertyField(cellPosition, biome, GUIContent.none);
                    cellPosition.x += _defaultCellSize.x;
                }

                cellPosition.y += _defaultCellSize.y;
                // If we don't do this, the next things we're going to draw after the grid will be drawn on top of the grid
                GUILayout.Space(_defaultCellSize.y);
            }

            cellPosition.y += _defaultCellSize.y;
            GUILayout.Space(_defaultCellSize.y);
        }
    }

    private SerializedProperty _GetBiomeTemperaturesAt(SerializedProperty arrayProperty, int idx)
    {
        return arrayProperty.GetArrayElementAtIndex(idx).FindPropertyRelative("BiomeTemperatures");
    }

    private SerializedProperty _GetBiomeheightsAt(SerializedProperty arrayProperty, int idx)
    {
        return arrayProperty.GetArrayElementAtIndex(idx).FindPropertyRelative("BiomeHeights");
    }

    private SerializedProperty _GetBiomeAt(SerializedProperty arrayProperty, int idx)
    {
        return arrayProperty.GetArrayElementAtIndex(idx);
    }
}

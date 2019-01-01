using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomeDistribution))]
public class BiomeDistributionEditor : Editor
{
    private readonly Vector2 _defaultRangeSize = new Vector2(50, 20);// px
    private readonly Vector2 _defaultBiomeCellSize = new Vector2(150, 20);// px
    private Vector2 _scrollPos;

    private SerializedProperty _humidityVariety;
    private SerializedProperty _heightVariety;
    private SerializedProperty _temperatureVariety;
    private SerializedProperty _humidityRange;
    private SerializedProperty _heightRange;
    private SerializedProperty _temperatureRange;
    private SerializedProperty _biomeHumiditys;

    private Rect _lastRect;

    void OnEnable()
    {
        _humidityVariety = serializedObject.FindProperty("HumidityVariety");
        _heightVariety = serializedObject.FindProperty("HeightVariety");
        _temperatureVariety = serializedObject.FindProperty("TemperatureVariety");
        _humidityRange = serializedObject.FindProperty("HumidityRange");
        _heightRange = serializedObject.FindProperty("HeightRange");
        _temperatureRange = serializedObject.FindProperty("TemperatureRange");
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

        var rect = _DisplayRangeGrid(_lastRect);
        _DisplayGrid(rect);

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
            _UpdateRange(_humidityVariety.intValue, _heightVariety.intValue, _temperatureVariety.intValue);
            _UpdateBiomeVariety(_humidityVariety.intValue, _heightVariety.intValue, _temperatureVariety.intValue);
        }
    }

    private void _UpdateRange(int humiditys, int heights, int temperatures)
    {
        _humidityRange.ClearArray();
        for (int i = 0; i < humiditys; i++)
            _humidityRange.InsertArrayElementAtIndex(i);
        _heightRange.ClearArray();
        for (int i = 0; i < heights; i++)
            _heightRange.InsertArrayElementAtIndex(i);
        _temperatureRange.ClearArray();
        for (int i = 0; i < temperatures; i++)
            _temperatureRange.InsertArrayElementAtIndex(i);
    }

    private void _UpdateBiomeVariety(int humiditys, int heights, int temperatures)
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

    private Rect _DisplayRangeGrid(Rect startRect)
    {
        Rect cellPosition = startRect;
        float startLineX = cellPosition.x;
        var labelStyle = new GUIStyle { fixedWidth = 150 };
         
        // Same as EditorGUILayout.Space(), but in Rect
        cellPosition.y += _defaultRangeSize.y;
        cellPosition.size = _defaultRangeSize;

        EditorGUI.LabelField(cellPosition, "Humidity Range:", labelStyle);
        cellPosition.x = startLineX + labelStyle.fixedWidth;
        for (int i = 0; i < _humidityVariety.intValue; i++)
        {
            SerializedProperty humidityRange = _humidityRange.GetArrayElementAtIndex(i);
            EditorGUI.PropertyField(cellPosition, humidityRange, GUIContent.none);
            cellPosition.x += _defaultRangeSize.x;
        }

        cellPosition.y += _defaultRangeSize.y;
        cellPosition.x = startLineX;
        EditorGUI.LabelField(cellPosition, "Height Range:", labelStyle);
        cellPosition.x = startLineX + labelStyle.fixedWidth;
        for (int i = 0; i < _heightVariety.intValue; i++)
        {
            SerializedProperty heightRange = _heightRange.GetArrayElementAtIndex(i);
            EditorGUI.PropertyField(cellPosition, heightRange, GUIContent.none);
            cellPosition.x += _defaultRangeSize.x;
        }

        cellPosition.y += _defaultRangeSize.y;
        cellPosition.x = startLineX;
        EditorGUI.LabelField(cellPosition, "Temperaute Range:", labelStyle);
        cellPosition.x = startLineX + labelStyle.fixedWidth;
        for (int i = 0; i < _temperatureVariety.intValue; i++)
        {
            SerializedProperty temperatureRange = _temperatureRange.GetArrayElementAtIndex(i);
            EditorGUI.PropertyField(cellPosition, temperatureRange, GUIContent.none);
            cellPosition.x += _defaultRangeSize.x;
        }
         
        cellPosition.x = startLineX;
        cellPosition.y += _defaultRangeSize.y;
        return cellPosition;
    }

    private void _DisplayGrid(Rect startRect)
    {
        Rect cellPosition = startRect;
        float startLineX = cellPosition.x;

        cellPosition.y += _defaultBiomeCellSize.y;
        cellPosition.size = _defaultBiomeCellSize;

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
                    cellPosition.x += _defaultBiomeCellSize.x;
                }

                cellPosition.y += _defaultBiomeCellSize.y;
                // If we don't do this, the next things we're going to draw after the grid will be drawn on top of the grid
                GUILayout.Space(_defaultBiomeCellSize.y);
            }

            cellPosition.y += _defaultBiomeCellSize.y;
            GUILayout.Space(_defaultBiomeCellSize.y);
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

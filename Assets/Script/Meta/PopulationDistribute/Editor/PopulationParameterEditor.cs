using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PopulationParameter))]
public class PopulationParameterEditor : Editor
{
    private readonly Vector2 _defaultRangeSize = new Vector2(50, 20);// px
    private readonly Vector2 _defaultBiomeCellSize = new Vector2(150, 20);// px

    private SerializedProperty _humidityVariety;
    private SerializedProperty _heightVariety;
    private SerializedProperty _temperatureVariety;
    private SerializedProperty _basicProbability;
    private SerializedProperty _humidityProbability;
    private SerializedProperty _heightProbability;
    private SerializedProperty _temperatureProbability;

    private Rect _lastRect;

    void OnEnable()
    {
        _humidityVariety = serializedObject.FindProperty("HumidityVariety");
        _heightVariety = serializedObject.FindProperty("HeightVariety");
        _temperatureVariety = serializedObject.FindProperty("TemperatureVariety");
        _basicProbability = serializedObject.FindProperty("BasicProbability");
        _humidityProbability = serializedObject.FindProperty("HumidityProbability");
        _heightProbability = serializedObject.FindProperty("HeightProbability");
        _temperatureProbability = serializedObject.FindProperty("TemperatureProbability");
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
        _DisplayRangeGrid(_lastRect);

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
        }
    }

    private void _UpdateRange(int humiditys, int heights, int temperatures)
    {
        _humidityProbability.ClearArray();
        for (int i = 0; i < humiditys; i++)
            _humidityProbability.InsertArrayElementAtIndex(i);
        _heightProbability.ClearArray();
        for (int i = 0; i < heights; i++)
            _heightProbability.InsertArrayElementAtIndex(i);
        _temperatureProbability.ClearArray();
        for (int i = 0; i < temperatures; i++)
            _temperatureProbability.InsertArrayElementAtIndex(i);
    }
    private Rect _DisplayRangeGrid(Rect startRect)
    {
        Rect cellPosition = startRect;
        float startLineX = cellPosition.x;
        var labelStyle = new GUIStyle { fixedWidth = 150 };

        // Same as EditorGUILayout.Space(), but in Rect
        cellPosition.y += _defaultRangeSize.y;
        cellPosition.size = _defaultRangeSize;
        
        EditorGUILayout.PropertyField(_basicProbability);
        cellPosition.y += _defaultRangeSize.y;

        EditorGUI.LabelField(cellPosition, "Humidity Probability:", labelStyle);
        cellPosition.x = startLineX + labelStyle.fixedWidth;
        for (int i = 0; i < _humidityVariety.intValue; i++)
        {
            SerializedProperty humidityRange = _humidityProbability.GetArrayElementAtIndex(i);
            //EditorGUILayout.LabelField(" [" );
            EditorGUI.PropertyField(cellPosition, humidityRange, GUIContent.none);
            cellPosition.x += _defaultRangeSize.x;
        }

        cellPosition.y += _defaultRangeSize.y;
        cellPosition.x = startLineX;
        EditorGUI.LabelField(cellPosition, "Height Probability:", labelStyle);
        cellPosition.x = startLineX + labelStyle.fixedWidth;
        for (int i = 0; i < _heightVariety.intValue; i++)
        {
            SerializedProperty heightRange = _heightProbability.GetArrayElementAtIndex(i);
            EditorGUI.PropertyField(cellPosition, heightRange, GUIContent.none);
            cellPosition.x += _defaultRangeSize.x;
        }

        cellPosition.y += _defaultRangeSize.y;
        cellPosition.x = startLineX;
        EditorGUI.LabelField(cellPosition, "Temperaute Probability:", labelStyle);
        cellPosition.x = startLineX + labelStyle.fixedWidth;
        for (int i = 0; i < _temperatureVariety.intValue; i++)
        {
            SerializedProperty temperatureRange = _temperatureProbability.GetArrayElementAtIndex(i);
            EditorGUI.PropertyField(cellPosition, temperatureRange, GUIContent.none);
            cellPosition.x += _defaultRangeSize.x;
        }

        cellPosition.x = startLineX;
        cellPosition.y += _defaultRangeSize.y;
        GUILayout.Space(200);
        return cellPosition;
    }
}

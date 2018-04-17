using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Utility;

public class WorldMapEditor : EditorWindow
{
    [MenuItem("Editor/WorldMapEditor")]
    public static WorldMapEditor OpenOrCreate()
    {
        WorldMapEditor window = EditorWindow.GetWindow<WorldMapEditor>("WorldMapEditor");

        window.Show();
        window.Focus();
        window.Init();
        return window;
    }

    #region UnityFunction
    void OnDestroy()
    {
    }

    void OnGUI()
    {
        try
        {
            _OnGUI();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    void Update()
    {
    }
    #endregion

    private SerializedObject _serializedObj;
    private bool _isEditorCompiling = false;
    private bool _isPlaying = false;
    private bool _initialGUI = false;

    private enum MainMenuStatus {
        None,
        NewMap,
        LoadMap,
    }
    private MainMenuStatus _mainMenuStatus = MainMenuStatus.None;

    private Executor _executor = new Executor();

    //Create 
    private ITerrainGenerator _terrainGen = new TerrainGenerator();
    [SerializeField]
    private TerrainParameter _paramTerrain;
    [SerializeField]
    private RandomPointParameter _paramRandomPoint;
    [SerializeField]
    private WeatherParameter _paramWeather;
    private SerializedProperty _propertyParamTerrain;
    private SerializedProperty _propertyParamRandomPoint;
    private SerializedProperty _propertyParamWeather;

    private string _noticeTxt;
    private Texture2D _worldTexture = null;

    public void Init()
    {
        _serializedObj = new SerializedObject(this);
        _propertyParamTerrain = _serializedObj.FindProperty("_paramTerrain");
        _propertyParamRandomPoint = _serializedObj.FindProperty("_paramRandomPoint");
        _propertyParamWeather = _serializedObj.FindProperty("_paramWeather");

        var mapData = SaveData.LoadMap();
        if (mapData != null)
        {
            _LoadWorldMapTexture(mapData);
        }
    }

    #region DrawGUI
    private GUIStyle _mapStyle;
    private void _OnGUI()
    {
        if (EditorApplication.isCompiling)
        {
            _isEditorCompiling = true;
            return;
        }
        else if (_isEditorCompiling)
        {
            Init();
            _isEditorCompiling = false;
        }

        if (EditorApplication.isPlaying)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                Init();
            }
        }
        else if (_isPlaying)
        {
            _isPlaying = false;
            Init();
        }

        if (!_initialGUI)
        {
            _InitGUI();
            _initialGUI = true;
        }

        // draw like that
        // +--------------------+
        // |       header       |
        // +--------+-----------+
        // |  side  |   main    |
        // | search |  content  |
        // +--------+-----------+
        // |       bottom       |
        // +--------------------+
        EditorGUILayout.BeginVertical();
        {
            EditorGUILayout.BeginHorizontal();
            {
                _DrawHeader();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    _DrawMenu();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                {
                    _DrawMain();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        Repaint();

    }

    private void _InitGUI()
    {
        _mapStyle = new GUIStyle
        {
            fixedHeight = 150,
            fixedWidth = 150
        };
    }

    private void _DrawHeader()
    {
        if (GUILayout.Button("New Map"))
        {
            _mainMenuStatus = MainMenuStatus.NewMap;
        }
        if (GUILayout.Button("Load Map"))
        {
            _mainMenuStatus = MainMenuStatus.LoadMap;
        }
    }

    private void _DrawMenu()
    {
        switch (_mainMenuStatus)
        {
            case MainMenuStatus.NewMap:
                _DrawMenuNewMap();
                break;
            case MainMenuStatus.LoadMap:
                _DrawMenuLoadMap();
                break;
        }
    }

    private void _DrawMain()
    {
        switch (_mainMenuStatus)
        {
            case MainMenuStatus.NewMap:
                _DrawMainNewMap();
                break;
            case MainMenuStatus.LoadMap:
                _DrawMainLoadMap();
                break;
        }
    }

    private void _DrawMenuNewMap()
    {
        if (GUILayout.Button("Create"))
        {
            if (_CheckPreparedToCreate())
            {
                _CreateNewWorld();
            }
        }
        if (GUILayout.Button("Save"))
        {
        }

        EditorGUILayout.PropertyField(_propertyParamTerrain);
        EditorGUILayout.PropertyField(_propertyParamRandomPoint);
        EditorGUILayout.PropertyField(_propertyParamWeather);
    }

    private void _DrawMenuLoadMap()
    {
    }

    private void _DrawMainNewMap()
    {

    }

    private void _DrawMainLoadMap()
    {
        if (_worldTexture != null)
        {
            EditorGUILayout.LabelField(_noticeTxt);
            GUILayout.Box(_worldTexture, _mapStyle);
        }
    }
    #endregion



    private void _LoadWorldMapTexture(SaveDataUnit mapData)
    {
        _worldTexture = new Texture2D(mapData.Width, mapData.Height);
        for (int i = 0; i < mapData.Width; i++)
        {
            for (int j = 0; j < mapData.Height; j++)
            {
                var height = mapData.Map[i * mapData.Width + j];
                _worldTexture.SetPixel(i, j, new Color(height, height, height));
            }
        }
        _worldTexture.Apply();
    }

    private bool _CheckPreparedToCreate()
    {
        if (_propertyParamTerrain == null ||
            _propertyParamRandomPoint == null ||
            _propertyParamWeather == null)
        {
            _noticeTxt = "Parameter is null !";
            return false;
        }
        _noticeTxt = "";
        return true;
    }

    private void _CreateNewWorld()
    {
        _executor.Clear();
    }
}

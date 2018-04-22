
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
        _executor.Clear();
    }

    void OnGUI()
    {
        try
        {
            _OnGUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    void Update()
    {
        _executor.Resume(Time.deltaTime);
    }
    #endregion

    #region Initial
    private SerializedObject _serializedObj;
    private bool _isEditorCompiling = false;
    private bool _isPlaying = false;
    private bool _initialGUI = false;

    private enum MainMenuStatus {
        None,
        NewMap,
        LoadMap,
    }
    private enum NewMapStatus {
        None,
        Parameter,
        Create,
    }
    private MainMenuStatus _mainMenuStatus = MainMenuStatus.None;
    private NewMapStatus _newMapStatus = NewMapStatus.None;

    private Executor _executor = new Executor();

    //Create 
    private ITerrainGenerator _terrainGen = new TerrainGenerator();
    private IWeatherGenerator _temperatureGen = new WeatherGenerator();
    private IWeatherGenerator _humidityGen = new WeatherGenerator();
    private IWeatherGenerator _manaGen = new WeatherGenerator();
    [SerializeField]
    private TerrainParameter _paramTerrain;
    [SerializeField]
    private WeatherParameter _paramWeather;
    [SerializeField]
    private BiomeDistribution _biomeDistribution;
    [SerializeField]
    private int _width = 0;
    [SerializeField]
    private int _height = 0;
    private SerializedProperty _propertyParamTerrain;
    private SerializedProperty _propertyParamWeather;
    private SerializedProperty _propertyBiomeDistribution;
    private SerializedProperty _propertyWidth;
    private SerializedProperty _propertyHeight;

    private string _noticeTxt;
    private Texture2D _worldTexture = null;

    public void Init()
    {
        _serializedObj = new SerializedObject(this);
        _propertyParamTerrain = _serializedObj.FindProperty("_paramTerrain");
        _propertyParamWeather = _serializedObj.FindProperty("_paramWeather");
        _propertyBiomeDistribution = _serializedObj.FindProperty("_biomeDistribution");
        _propertyWidth = _serializedObj.FindProperty("_width");
        _propertyHeight = _serializedObj.FindProperty("_height");
    }
    #endregion

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

            var mapData = SaveData.LoadMap();
            if (mapData != null)
            {
                _LoadWorldMapTexture(mapData);
            }
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
        if (GUILayout.Button("Parameter"))
        {
            _newMapStatus = NewMapStatus.Parameter;
        }
        if (GUILayout.Button("Create"))
        {
            if (_CheckPreparedToCreate())
            {
                _CreateNewWorld();
                _newMapStatus = NewMapStatus.Create;
            }
        }
        if (GUILayout.Button("Save"))
        {
        }

    }

    private void _DrawMenuLoadMap()
    {
    }

    private void _DrawMainNewMap()
    {
        EditorGUILayout.LabelField(_noticeTxt);
        switch (_newMapStatus)
        {
            case NewMapStatus.Parameter:
                _EditParameter();
                break;
            case NewMapStatus.Create:
                break;
        }
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

    #region CreateWorld
    private void _EditParameter()
    {
        try
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_propertyParamTerrain);
            EditorGUILayout.PropertyField(_propertyParamWeather);
            EditorGUILayout.PropertyField(_propertyBiomeDistribution);
            EditorGUILayout.PropertyField(_propertyWidth);
            EditorGUILayout.PropertyField(_propertyHeight);
            if (EditorGUI.EndChangeCheck())
            {
                _serializedObj.ApplyModifiedProperties();
            }
        }
        catch(System.Exception e) { }
    }

    private bool _CheckPreparedToCreate()
    {
        if (_paramTerrain == null ||
            _paramWeather == null ||
            _biomeDistribution == null ||
            _width == 0 || _height == 0 )
        {
            _noticeTxt = "Parameter is null !";
            return false;
        }
        _noticeTxt = "Start create world ...";
        return true;
    }

    private void _CreateNewWorld()
    {
        _executor.Clear();
        var monad = new BlockMonad<None>(r => _CreateNewWorldMonad());
        _executor.Add(monad.Do());
    }

    private IEnumerator _CreateNewWorldMonad()
    {
        var genTerrainMonad = new BlockMonad<float[]>(r =>
             _terrainGen.GenerateHeightMap(
                 _width,
                 _height,
                 Random.Range(0, 10000),
                 Random.Range(0, 10000),
                 _paramTerrain,
                 r));
        var genHumidityMonad = new BlockMonad<float[]>(r =>
            _humidityGen.GenerateWeatherMap(
                _width,
                _height,
                Random.Range(0, 10000),
                Random.Range(0, 10000),
                _paramWeather,
                r));
        var genTemperatureMonad = new BlockMonad<float[]>(r =>
            _temperatureGen.GenerateWeatherMap(
                _width,
                _height,
                Random.Range(0, 10000),
                Random.Range(0, 10000),
                _paramWeather,
                r));
        Debug.Log("start genTerrainMonad");
        yield return genTerrainMonad.Do();
        Debug.Log("start genHumidityMonad");
        yield return genHumidityMonad.Do();
        Debug.Log("start genTemperatureMonad");
        yield return genTemperatureMonad.Do();

        var terrainMap = genTerrainMonad.Result;
        var humidityMap = genHumidityMonad.Result;
        var temperatureMap = genTemperatureMonad.Result;
        var tileUnitMap = new TileUnit[_width * _height];

        IBiomeIdentifier identifier = new BasicBiomeIdentifier(_biomeDistribution);
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int idx = x * _height + y;

                var height = terrainMap[idx];
                var humidity = humidityMap[idx];
                var temperature = temperatureMap[idx];
                
                BiomeData biome = identifier.IdentifyBiome(humidity, height, temperature);
                tileUnitMap[idx] = new TileUnit(
                    height,
                    humidity,
                    temperature,
                    biome.Biome);
            }
        }
        
        var dataUnit = new SaveDataUnit
        {
            Map = tileUnitMap,
            Width = _width,
            Height = _height,
        };
        SaveData.SaveMap(dataUnit);
        Debug.Log("save");
    }
    #endregion

    private void _LoadWorldMapTexture(SaveDataUnit mapData)
    {
        _noticeTxt = string.Format("Load Map  - {0}*{1}", mapData.Width, mapData.Height);
        _worldTexture = new Texture2D(mapData.Width, mapData.Height);
        for (int x = 0; x < mapData.Width; x++)
        {
            for (int y = 0; y < mapData.Height; y++)
            {
                int idx = x * mapData.Width + y;
                var tileUnit = mapData.Map[idx];
                _worldTexture.SetPixel(x, y, new Color(tileUnit.Humidity, tileUnit.Height, tileUnit.Temperature));
            }
        }
        _worldTexture.Apply();
    }
}

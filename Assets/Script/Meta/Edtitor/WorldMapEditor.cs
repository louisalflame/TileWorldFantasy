
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
    private enum MainMenuStatus {
        None,
        NewMap,
        ViewMap,
    }
    private enum NewMapStatus {
        None,
        Parameter,
        Create,
        Save,
        Load,
    }
    private enum ViewMapStatus
    {
        None,
        Terrain,
        Humudity,
        Temperature,
        Mana,
        River,
    }
    private MainMenuStatus _mainMenuStatus = MainMenuStatus.None;
    private NewMapStatus _newMapStatus = NewMapStatus.None;
    private ViewMapStatus _viewMapStatus = ViewMapStatus.None;

    [SerializeField]
    private TerrainParameter _paramTerrain;
    [SerializeField]
    private WeatherParameter _paramWeather;
    [SerializeField]
    private RainParameter _paramRain;
    [SerializeField]
    private BiomeDistribution _biomeDistribution;
    [SerializeField]
    private ColorRangeDistribution _terrainColor;
    [SerializeField]
    private ColorRangeDistribution _weatherColor;
    [SerializeField]
    private ColorRangeDistribution _manaColor;
    [SerializeField]
    private ColorRangeDistribution _rainColor;
    [SerializeField]
    private int _width = 0;
    [SerializeField]
    private int _height = 0;
    [SerializeField]
    private int _seed = 0;

    private SerializedObject _serializedObj;
    private SerializedProperty _propertyParamTerrain;
    private SerializedProperty _propertyParamWeather;
    private SerializedProperty _propertyParamRain;
    private SerializedProperty _propertyBiomeDistribution;
    private SerializedProperty _propertyTerrainColorRange;
    private SerializedProperty _propertyWeatherColorRange;
    private SerializedProperty _propertyManaColorRange;
    private SerializedProperty _propertyRainColorRange;
    private SerializedProperty _propertyWidth;
    private SerializedProperty _propertyHeight;
    private SerializedProperty _propertySeed;

    private const string _terrainParamPath = "Assets/Data/GeneratorParameter/BasicParamTerrain.asset";
    private const string _weatherParamPath = "Assets/Data/GeneratorParameter/BasicParamWeather.asset";
    private const string _rainParamPath = "Assets/Data/GeneratorParameter/BasicParamRain.asset";
    private const string _biomeDistributionPath = "Assets/Data/BiomeDistribute/NormalBiomeDistribution.asset";
    private const string _terrainColorRangePath = "Assets/Data/GeneratorParameter/TerrainColorRange.asset";
    private const string _weatherColorRangePath = "Assets/Data/GeneratorParameter/WeatherColorRange.asset";
    private const string _manaColorRangePath = "Assets/Data/GeneratorParameter/ManaColorRange.asset";
    private const string _rainColorRangePath = "Assets/Data/GeneratorParameter/RainColorRange.asset";

    private string _noticeTxt;
    private Texture2D _worldTexture = null;
    private TileDataUnit _tileData = null;
    private Executor _executor = new Executor();

    //Create 
    private ITerrainGenerator _terrainGen = new TerrainGenerator();
    private IWeatherGenerator _temperatureGen = new WeatherGenerator();
    private IWeatherGenerator _humidityGen = new WeatherGenerator();
    private IManaGenerator _manaGen = new ManaGenerator();
    private IRiverGenerator _riverGen = new RiverGenerator();

    public void Init()
    {
        _paramTerrain = (TerrainParameter)AssetDatabase.LoadAssetAtPath(_terrainParamPath, typeof(TerrainParameter));
        _paramWeather = (WeatherParameter)AssetDatabase.LoadAssetAtPath(_weatherParamPath, typeof(WeatherParameter));
        _paramRain = (RainParameter)AssetDatabase.LoadAssetAtPath(_rainParamPath, typeof(RainParameter));
        _biomeDistribution = (BiomeDistribution)AssetDatabase.LoadAssetAtPath(_biomeDistributionPath, typeof(BiomeDistribution));
        _terrainColor = (ColorRangeDistribution)AssetDatabase.LoadAssetAtPath(_terrainColorRangePath, typeof(ColorRangeDistribution));
        _weatherColor = (ColorRangeDistribution)AssetDatabase.LoadAssetAtPath(_weatherColorRangePath, typeof(ColorRangeDistribution));
        _manaColor = (ColorRangeDistribution)AssetDatabase.LoadAssetAtPath(_manaColorRangePath, typeof(ColorRangeDistribution));
        _rainColor = (ColorRangeDistribution)AssetDatabase.LoadAssetAtPath(_rainColorRangePath, typeof(ColorRangeDistribution));

        _serializedObj = new SerializedObject(this);
        _propertyParamTerrain = _serializedObj.FindProperty("_paramTerrain");
        _propertyParamWeather = _serializedObj.FindProperty("_paramWeather");
        _propertyParamRain = _serializedObj.FindProperty("_paramRain");
        _propertyBiomeDistribution = _serializedObj.FindProperty("_biomeDistribution");
        _propertyTerrainColorRange = _serializedObj.FindProperty("_terrainColor");
        _propertyWeatherColorRange = _serializedObj.FindProperty("_weatherColor");
        _propertyManaColorRange = _serializedObj.FindProperty("_manaColor");
        _propertyRainColorRange = _serializedObj.FindProperty("_rainColor");
        _propertyWidth = _serializedObj.FindProperty("_width");
        _propertyHeight = _serializedObj.FindProperty("_height");
        _propertySeed = _serializedObj.FindProperty("_seed");

        _noticeTxt = string.Empty;
        _worldTexture = null;
        _tileData = null;
        _executor.Clear();
    }
    #endregion

    #region DrawGUI
    private bool _isEditorCompiling = false;
    private bool _isPlaying = false;
    private bool _initialGUI = false;
    private GUIStyle _mapStyle;
    private GUIStyle _menuStyle;
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
                EditorGUILayout.BeginVertical(_menuStyle);
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
            fixedHeight = 0,
            fixedWidth = 0,
            stretchHeight = true,
            stretchWidth = true,
            border = new RectOffset (0,0,0,0),
            overflow = new RectOffset(0, 0, 0, 0),
        };
        _menuStyle = new GUIStyle
        {
            fixedWidth = 130
        };
    }

    private void _DrawHeader()
    {
        if (GUILayout.Button("New Map", GUILayout.Width(200)))
        {
            _mainMenuStatus = MainMenuStatus.NewMap;
            _noticeTxt = string.Empty;
        }
        if (GUILayout.Button("View Map", GUILayout.Width(200)))
        {
            _mainMenuStatus = MainMenuStatus.ViewMap;
            _worldTexture = null;
            _noticeTxt = string.Empty;
        }
    }

    private void _DrawMenu()
    {
        switch (_mainMenuStatus)
        {
            case MainMenuStatus.NewMap:
                _DrawMenuNewMap();
                break;
            case MainMenuStatus.ViewMap:
                _DrawMenuViewMap();
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
            case MainMenuStatus.ViewMap:
                _DrawMainViewMap();
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
            _newMapStatus = NewMapStatus.Create;
            if (_CheckPreparedToCreate())
            {
                _CreateNewWorld();
            }
        }
        if (GUILayout.Button("Save"))
        {
            _newMapStatus = NewMapStatus.Save;
            if (_tileData != null)
            {
                SaveData.SaveMap(_tileData);
                _noticeTxt = "Map saved success";
            }
            else
            {
                _noticeTxt = "Error! No map to saved";
            }
        }
        if (GUILayout.Button("Load"))
        {
            _newMapStatus = NewMapStatus.Load;
            _tileData = SaveData.LoadMap();
            if (_tileData != null)
            {
                _noticeTxt = "Map loaded success";
            }
            else
            {
                _noticeTxt = "Error! No map to saved";
            }
        }

    }

    private void _DrawMenuViewMap()
    {
        if (GUILayout.Button("Terrain"))
        {
            _viewMapStatus = ViewMapStatus.Terrain;
            if (_tileData != null && _terrainColor != null)
            {
                _ShowWorldMapTexture(_tileData);
                _noticeTxt = string.Format("Terrain map: {0}*{1}", _tileData.Width, _tileData.Height);
            }
        }
        if (GUILayout.Button("Humudity"))
        {
            _viewMapStatus = ViewMapStatus.Humudity;
            if (_tileData != null && _weatherColor != null)
            {
                _ShowWorldMapTexture(_tileData);
                _noticeTxt = string.Format("Humudity map: {0}*{1}", _tileData.Width, _tileData.Height);
            }
        }
        if (GUILayout.Button("Temperature"))
        {
            _viewMapStatus = ViewMapStatus.Temperature;
            if (_tileData != null && _weatherColor != null)
            {
                _ShowWorldMapTexture(_tileData);
                _noticeTxt = string.Format("Temperature map: {0}*{1}", _tileData.Width, _tileData.Height);
            }
        }
        if (GUILayout.Button("Mana"))
        {
            _viewMapStatus = ViewMapStatus.Mana;
            if (_tileData != null && _manaColor != null)
            {
                _ShowWorldMapTexture(_tileData);
                _noticeTxt = string.Format("Mana map: {0}*{1}", _tileData.Width, _tileData.Height);
            }
        }
        if (GUILayout.Button("River"))
        {
            _viewMapStatus = ViewMapStatus.River;
            if (_tileData != null && _rainColor != null)
            {
                _ShowWorldMapTexture(_tileData);
                _noticeTxt = string.Format("River map: {0}*{1}", _tileData.Width, _tileData.Height);
            }
        }
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
            case NewMapStatus.Save:
                break;
            case NewMapStatus.Load:
                break;
        }
    }

    private void _DrawMainViewMap()
    {
        if (_worldTexture != null)
        {
            EditorGUILayout.LabelField(_noticeTxt);
            var rect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(new Rect(rect.min.x, rect.min.y + rect.height +10, 200, 200), _worldTexture, ScaleMode.ScaleToFit);
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
            EditorGUILayout.PropertyField(_propertyParamRain);
            EditorGUILayout.PropertyField(_propertyBiomeDistribution);
            EditorGUILayout.PropertyField(_propertyTerrainColorRange);
            EditorGUILayout.PropertyField(_propertyWeatherColorRange);
            EditorGUILayout.PropertyField(_propertyManaColorRange);
            EditorGUILayout.PropertyField(_propertyRainColorRange);
            EditorGUILayout.PropertyField(_propertyWidth);
            EditorGUILayout.PropertyField(_propertyHeight);
            EditorGUILayout.PropertyField(_propertySeed);
            if (EditorGUI.EndChangeCheck())
            {
                _serializedObj.ApplyModifiedProperties();
            }
        }
        catch(System.Exception e)
        {
            Debug.LogError(e);
        }
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
        Random.InitState(_seed);

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
        var genManaMonad = new BlockMonad<float[]>(r =>
            _manaGen.GenerateManaMap(
                _width,
                _height, 
                _paramTerrain.RANDOM_POINT_GEN_PARA,
                r));

        Debug.Log("start genTerrainMonad");
        yield return genTerrainMonad.Do();
        Debug.Log("start genHumidityMonad");
        yield return genHumidityMonad.Do();
        Debug.Log("start genTemperatureMonad");
        yield return genTemperatureMonad.Do();
        Debug.Log("start genManaMonad");
        yield return genManaMonad.Do();

        var terrainMap = genTerrainMonad.Result;
        var humidityMap = genHumidityMonad.Result;
        var temperatureMap = genTemperatureMonad.Result;
        var manaMap = genManaMonad.Result;

        var genRiverMonad = new BlockMonad<float[]>(r =>
            _riverGen.GenerateRiverMap(
                _width,
                _height,
                terrainMap,
                _paramRain,
                r));
        Debug.Log("start genRiverMonad");
        yield return genRiverMonad.Do();
        var riverMap = genRiverMonad.Result;

        var tileUnitMap = new TileUnit[_width * _height];

        IBiomeIdentifier identifier = new BasicBiomeIdentifier(_biomeDistribution);
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int idx = y * _width + x;

                var height = terrainMap[idx];
                var humidity = humidityMap[idx];
                var temperature = temperatureMap[idx];
                var mana = manaMap[idx];
                var river = riverMap[idx];
                
                BiomeData biome = identifier.IdentifyBiome(humidity, height, temperature);
                tileUnitMap[idx] = new TileUnit(
                    height,
                    humidity,
                    temperature,
                    mana,
                    river,
                    biome.Biome);
            }
        }

        _tileData = new TileDataUnit
        {
            Map = tileUnitMap,
            Width = _width,
            Height = _height,
        };
        _noticeTxt = "New world created.";
    }
    #endregion

    private void _ShowWorldMapTexture(TileDataUnit mapData)
    {
        _noticeTxt = string.Format("Load Map  - {0}*{1}", mapData.Width, mapData.Height);
        _worldTexture = new Texture2D(mapData.Width, mapData.Height);
        for (int x = 0; x < mapData.Width; x++)
        {
            for (int y = 0; y < mapData.Height; y++)
            {
                int idx = x * mapData.Width + y;
                var tileUnit = mapData.Map[idx];
                var color = _GetColorOfTypeRange(tileUnit);

                _worldTexture.SetPixel(x, y, color);
            }
        }
        _worldTexture.Apply();
    }

    private Color _GetColorOfTypeRange(TileUnit unit)
    {
        switch (_viewMapStatus)
        {
            case ViewMapStatus.Terrain:
                return _terrainColor.GetLerpColor(unit.Height);
            case ViewMapStatus.Humudity:
                return _weatherColor.GetLerpColor(unit.Humidity);
            case ViewMapStatus.Temperature:
                return _weatherColor.GetLerpColor(unit.Temperature);
            case ViewMapStatus.Mana:
                return _manaColor.GetLerpColor(unit.Mana);
            case ViewMapStatus.River:
                return _rainColor.GetLerpColor(unit.River);
            default:
                return Color.clear;
        }
    }
}

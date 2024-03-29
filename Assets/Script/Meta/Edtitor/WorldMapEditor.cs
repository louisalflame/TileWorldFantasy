﻿using System.Collections;
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
        Biome,
        Humudity,
        Temperature,
        Mana,
        River,
        Population
    }
    private MainMenuStatus _mainMenuStatus = MainMenuStatus.None;
    private NewMapStatus _newMapStatus = NewMapStatus.None;
    private ViewMapStatus _viewMapStatus = ViewMapStatus.None;

    [SerializeField]
    private TerrainParameter _paramTerrain;
    [SerializeField]
    private WeatherParameter _paramWeather;
    [SerializeField]
    private TemperatureParameter _paramTemperature;
    [SerializeField]
    private ManaParameter _paramMana;
    [SerializeField]
    private RainParameter _paramRain;
    [SerializeField]
    private PopulationParameter _paramPopulation;
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
    private SerializedProperty _propertyParamTemperature;
    private SerializedProperty _propertyParamMana;
    private SerializedProperty _propertyParamRain;
    private SerializedProperty _propertyParamPopulation;
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
    private const string _temperatureParamPath = "Assets/Data/GeneratorParameter/BasicParamTemperature.asset";
    private const string _manaParamPath = "Assets/Data/GeneratorParameter/BasicParamMana.asset";
    private const string _rainParamPath = "Assets/Data/GeneratorParameter/BasicParamRain.asset";
    private const string _populationParamPath = "Assets/Data/PopulationDistribute/BasicParamPopulation.asset";
    private const string _biomeDistributionPath = "Assets/Data/BiomeDistribute/NormalBiomeDistribution.asset";
    private const string _terrainColorRangePath = "Assets/Data/GeneratorParameter/TerrainColorRange.asset";
    private const string _weatherColorRangePath = "Assets/Data/GeneratorParameter/WeatherColorRange.asset";
    private const string _manaColorRangePath = "Assets/Data/GeneratorParameter/ManaColorRange.asset";
    private const string _rainColorRangePath = "Assets/Data/GeneratorParameter/RainColorRange.asset";

    private string _noticeTxt;
    private string _descriptionTxt = string.Empty;
    private Texture2D _worldTexture = null;
    private TileDataUnit _tileData = null;
    private Executor _executor = new Executor();

    //Create 
    private ITerrainGenerator _terrainGen = new TerrainGenerator();
    private ITemperatureGenerator _temperatureGen = new TemperatureGenerator();
    private IWeatherGenerator _humidityGen = new WeatherGenerator();
    private IManaGenerator _manaGen = new ManaGenerator();
    private IRiverGenerator _riverGen = new RiverGenerator();
    private IPopulationGenerator _populationGen = new PopulationGenerator();

    public void Init()
    {
        _paramTerrain = (TerrainParameter)AssetDatabase.LoadAssetAtPath(_terrainParamPath, typeof(TerrainParameter));
        _paramWeather = (WeatherParameter)AssetDatabase.LoadAssetAtPath(_weatherParamPath, typeof(WeatherParameter));
        _paramTemperature = (TemperatureParameter)AssetDatabase.LoadAssetAtPath(_temperatureParamPath, typeof(TemperatureParameter));
        _paramMana = (ManaParameter)AssetDatabase.LoadAssetAtPath(_manaParamPath, typeof(ManaParameter));
        _paramRain = (RainParameter)AssetDatabase.LoadAssetAtPath(_rainParamPath, typeof(RainParameter));
        _paramPopulation = (PopulationParameter)AssetDatabase.LoadAssetAtPath(_populationParamPath, typeof(PopulationParameter));
        _biomeDistribution = (BiomeDistribution)AssetDatabase.LoadAssetAtPath(_biomeDistributionPath, typeof(BiomeDistribution));
        _terrainColor = (ColorRangeDistribution)AssetDatabase.LoadAssetAtPath(_terrainColorRangePath, typeof(ColorRangeDistribution));
        _weatherColor = (ColorRangeDistribution)AssetDatabase.LoadAssetAtPath(_weatherColorRangePath, typeof(ColorRangeDistribution));
        _manaColor = (ColorRangeDistribution)AssetDatabase.LoadAssetAtPath(_manaColorRangePath, typeof(ColorRangeDistribution));
        _rainColor = (ColorRangeDistribution)AssetDatabase.LoadAssetAtPath(_rainColorRangePath, typeof(ColorRangeDistribution));

        _serializedObj = new SerializedObject(this);
        _propertyParamTerrain = _serializedObj.FindProperty("_paramTerrain");
        _propertyParamWeather = _serializedObj.FindProperty("_paramWeather");
        _propertyParamTemperature = _serializedObj.FindProperty("_paramTemperature");
        _propertyParamMana = _serializedObj.FindProperty("_paramMana");
        _propertyParamRain = _serializedObj.FindProperty("_paramRain");
        _propertyParamPopulation = _serializedObj.FindProperty("_paramPopulation");
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
    private GUIStyle _subMenuStyle;
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
                EditorGUILayout.BeginVertical(_subMenuStyle);
                {
                    _DrawSubMenu();
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
        _menuStyle = new GUIStyle
        {
            fixedWidth = 130
        };
        _subMenuStyle = new GUIStyle
        {
            fixedWidth = 160
        };
        _mapStyle = new GUIStyle
        {
            fixedHeight = 200,
        };
    }

    private void _DrawHeader()
    {
        if (GUILayout.Button("New Map", GUILayout.Width(200)))
        {
            _viewMapStatus = ViewMapStatus.None;
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

    private void _DrawSubMenu()
    {
        if (_tileData == null) return;

        switch (_viewMapStatus)
        {
            case ViewMapStatus.Terrain:
                _DrawSubMenuTerrian();
                break;
            case ViewMapStatus.Biome:
                _DrawSubMenuBiome();
                break;
            case ViewMapStatus.Temperature:
                _DrawSubMenuTemperature();
                break;
            case ViewMapStatus.Humudity:
                _DrawSubMenuHumidity();
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
        if (GUILayout.Button("Biome"))
        {
            _viewMapStatus = ViewMapStatus.Biome;
            if (_tileData != null && _terrainColor != null)
            {
                _ShowWorldMapTexture(_tileData);
                _noticeTxt = string.Format("Biome map: {0}*{1}", _tileData.Width, _tileData.Height);
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
        if (GUILayout.Button("Population"))
        {
            _viewMapStatus = ViewMapStatus.Population;
            if (_tileData != null)
            {
                _ShowTerrainMapPopulation(_tileData);
                _noticeTxt = string.Format("Population map: {0}*{1}", _tileData.Width, _tileData.Height);
            }
        }
    }

    private void _DrawSubMenuBiome()
    {
        foreach (var biomeName in System.Enum.GetNames(typeof(Biome)))
        {
            if (GUILayout.Button(biomeName, GUILayout.MinWidth(200)))
            {
                var count = 0;
                var biome = (Biome)(System.Enum.Parse(typeof(Biome), biomeName));
                _ShowTerrainMapCondition(_tileData, (t) => {
                    bool valid = t.Biome == biome;
                    count += valid ? 1 : 0;
                    return valid;
                });
                _descriptionTxt = string.Format("num: {0}", count);
            }
        }
    }

    private void _DrawSubMenuTerrian()
    {
        if (_biomeDistribution == null) return;

        IBiomeIdentifier identifier = new BasicBiomeIdentifier(_biomeDistribution);
        for (var i = 0; i < _biomeDistribution.HeightVariety; i++)
        {
            var heightNo = string.Format("Height {0}", i.ToString());
            if (GUILayout.Button(heightNo, GUILayout.MinWidth(200)))
            {
                var count = 0;
                _ShowTerrainMapCondition(_tileData, (t) => {
                    bool valid = identifier.IsHeightInVarietyIndex(t.Height, i);
                    count += valid ? 1 : 0;
                    return valid;
                });
                _descriptionTxt = string.Format("num: {0}", count);
            }
        }
    }

    private void _DrawSubMenuTemperature()
    {
        if (_biomeDistribution == null) return;

        IBiomeIdentifier identifier = new BasicBiomeIdentifier(_biomeDistribution);
        for (var i = 0; i < _biomeDistribution.TemperatureVariety; i++)
        {
            var tempNo = string.Format("Temperature {0}", i.ToString());
            if (GUILayout.Button(tempNo, GUILayout.MinWidth(200)))
            {
                var count = 0;
                _ShowTerrainMapCondition(_tileData, (t) => {
                    bool valid = identifier.IsTemperatureInVarietyIndex(t.Temperature, i);
                    count += valid ? 1 : 0;
                    return valid;
                });
                _descriptionTxt = string.Format("num: {0}", count);
            }
        }
    }

    private void _DrawSubMenuHumidity()
    {
        if (_biomeDistribution == null) return;

        IBiomeIdentifier identifier = new BasicBiomeIdentifier(_biomeDistribution);
        for (var i = 0; i < _biomeDistribution.HumidityVariety; i++)
        {
            var humidityNo = string.Format("Humidity {0}", i.ToString());
            if (GUILayout.Button(humidityNo, GUILayout.MinWidth(200)))
            {
                var count = 0;
                _ShowTerrainMapCondition(_tileData, (t) => {
                    bool valid = identifier.IsHumidityInVarietyIndex(t.Humidity, i);
                    count += valid ? 1 : 0;
                    return valid;
                });
                _descriptionTxt = string.Format("num: {0}", count);
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
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.BeginVertical(_mapStyle);
            {
                if (_worldTexture != null)
                {
                    EditorGUILayout.LabelField(_noticeTxt);
                    var rect = GUILayoutUtility.GetLastRect();
                    GUI.DrawTexture(new Rect(rect.min.x, rect.min.y + rect.height + 10, 200, 200), _worldTexture, ScaleMode.ScaleToFit);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(_subMenuStyle);
            EditorGUILayout.LabelField(_descriptionTxt);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
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
            EditorGUILayout.PropertyField(_propertyParamTemperature);
            EditorGUILayout.PropertyField(_propertyParamMana);
            EditorGUILayout.PropertyField(_propertyParamRain);
            EditorGUILayout.PropertyField(_propertyParamPopulation);
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
                _paramTemperature,
                r));
        var genManaMonad = new BlockMonad<float[]>(r =>
            _manaGen.GenerateManaMap(
                _width,
                _height, 
                _paramMana,
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
                var idx = MathUtility.MapIndex(x, y, _height);

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

        var genPopulationMonad = new BlockMonad<int[]>(r =>
            _populationGen.GeneratePopulationMap(
                _width,
                _height,
                tileUnitMap,
                identifier,
                _paramPopulation,
                r));
        Debug.Log("start genPopulationMonad");
        yield return genPopulationMonad.Do();
        var populationMap = genPopulationMonad.Result;

        _tileData = new TileDataUnit
        {
            Map = tileUnitMap,
            Populations = populationMap,
            Width = _width,
            Height = _height,
        };
        _noticeTxt = "New world created.";
    }
    #endregion

    #region ViewMapTexture
    private void _ShowWorldMapTexture(TileDataUnit mapData)
    {
        _noticeTxt = string.Format("Load Map  - {0}*{1}", mapData.Width, mapData.Height);
        _worldTexture = new Texture2D(mapData.Width, mapData.Height);
        for (int x = 0; x < mapData.Width; x++)
        {
            for (int y = 0; y < mapData.Height; y++)
            {
                var idx = MathUtility.MapIndex(x, y, mapData.Height);
                var tileUnit = mapData.Map[idx];
                var color = _GetColorOfTypeRange(tileUnit);

                _worldTexture.SetPixel(x, y, color);
            }
        }
        _worldTexture.Apply();
    }

    private void _ShowTerrainMapCondition(TileDataUnit mapData, System.Func<TileUnit, bool> isValid)
    {
        _worldTexture = new Texture2D(mapData.Width, mapData.Height);
        for (int x = 0; x < mapData.Width; x++)
        {
            for (int y = 0; y < mapData.Height; y++)
            {
                var idx = MathUtility.MapIndex(x, y, mapData.Height);
                var tileUnit = mapData.Map[idx];
                var color = _terrainColor.GetLerpColor(tileUnit.Height);
                color.a = isValid(tileUnit) ? 1 : 0.2f;
                _worldTexture.SetPixel(x, y, color);
            }
        }
        _worldTexture.Apply();
    }

    private void _ShowTerrainMapPopulation(TileDataUnit mapData)
    {
        _worldTexture = new Texture2D(mapData.Width, mapData.Height);
        var count = 0;
        for (int x = 0; x < mapData.Width; x++)
        {
            for (int y = 0; y < mapData.Height; y++)
            {
                var idx = MathUtility.MapIndex(x, y, mapData.Height);
                var tileUnit = mapData.Map[idx];
                var color = _terrainColor.GetLerpColor(tileUnit.Height);
                if (mapData.Populations[idx] > 0)
                {
                    color = Color.black;
                    count++;
                }
                else
                {
                    color.a = 0.05f;
                }
                _worldTexture.SetPixel(x, y, color);
            }
        }
        _worldTexture.Apply();
        _descriptionTxt = string.Format("num: {0}", count);
    }
    #endregion

    private Color _GetColorOfTypeRange(TileUnit unit)
    {
        switch (_viewMapStatus)
        {
            case ViewMapStatus.Biome:
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

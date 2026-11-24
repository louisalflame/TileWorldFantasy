using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Utility;

public partial class WorldMapEditor : EditorWindow
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
        _StopGeneration();
        _ReleasePreviewTexture();
        if (_loadedSettings != null) _loadedSettings.Dispose();
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
        try
        {
            _executor.Resume(Time.deltaTime);
        }
        catch (System.Exception e)
        {
            _StopGeneration();
            _noticeTxt = "World generation failed: " + e.Message;
            Debug.LogException(e);
        }
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
    [SerializeField] private float _previewZoom = 1f;
    [SerializeField] private bool _previewFit = true;
    [SerializeField] private bool _previewPointFilter = true;
    private Vector2 _previewScroll;
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
        _StopGeneration();
        if (_loadedSettings != null) { _loadedSettings.Dispose(); _loadedSettings = null; }
        _generatedSettings = null;
        _currentSaveId = null;
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
        _ReleasePreviewTexture();
        _tileData = null;
        _StopGeneration();
    }
    #endregion

    #region DrawGUI
    private bool _isEditorCompiling = false;
    private bool _isPlaying = false;
    private bool _initialGUI = false;
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
            _ReleasePreviewTexture();
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
        }
        if (GUILayout.Button("Load"))
        {
            _newMapStatus = NewMapStatus.Load;
            _RefreshSaveList();
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
                _DrawSaveMap();
                break;
            case NewMapStatus.Load:
                _DrawLoadMaps();
                break;
        }
    }

    private void _DrawMainViewMap()
    {
        EditorGUILayout.LabelField(_noticeTxt, EditorStyles.wordWrappedLabel);
        if (_worldTexture == null)
        {
            EditorGUILayout.HelpBox("請先生成或讀取地圖，再選擇左側的預覽類型。", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        float zoomPercent = EditorGUILayout.Slider(new GUIContent("縮放（%）", "只改變顯示大小，不會重新生成地圖。"),
            _previewZoom * 100f, 1f, 1600f);
        if (EditorGUI.EndChangeCheck()) _SetPreviewZoom(zoomPercent / 100f);

        // Width and height are linked so rectangular maps retain their aspect ratio.
        EditorGUI.BeginChangeCheck();
        int displayWidth = EditorGUILayout.DelayedIntField(new GUIContent("顯示寬度", "輸入顯示寬度並按 Enter；高度會依地圖比例連動。單位為 Editor 顯示點。"),
            Mathf.RoundToInt(_worldTexture.width * _previewZoom));
        if (EditorGUI.EndChangeCheck()) _SetPreviewZoom((float)displayWidth / _worldTexture.width);
        EditorGUI.BeginChangeCheck();
        int displayHeight = EditorGUILayout.DelayedIntField(new GUIContent("顯示高度", "輸入顯示高度並按 Enter；寬度會依地圖比例連動，不影響生成解析度。"),
            Mathf.RoundToInt(_worldTexture.height * _previewZoom));
        if (EditorGUI.EndChangeCheck()) _SetPreviewZoom((float)displayHeight / _worldTexture.height);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("符合視窗", "隨可用預覽區域調整大小，顯示完整地圖。")))
        {
            _previewFit = true;
            _previewScroll = Vector2.zero;
        }
        if (GUILayout.Button(new GUIContent("1:1", "每個地圖格子以一個顯示點呈現。")))
        {
            _SetPreviewZoom(1f);
            _previewScroll = Vector2.zero;
        }
        EditorGUILayout.EndHorizontal();
        _previewPointFilter = EditorGUILayout.Toggle(new GUIContent("清晰格子", "開啟時放大顯示清楚的格子邊界；關閉時使用平滑插值。"), _previewPointFilter);
        if (!string.IsNullOrEmpty(_descriptionTxt))
            EditorGUILayout.LabelField(_descriptionTxt, EditorStyles.wordWrappedLabel);

        Rect viewport = GUILayoutUtility.GetRect(80f, 100000f, 80f, 100000f,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        // Reserve room for both scrollbars, so fit mode never oscillates as they appear.
        Vector2 available = new Vector2(Mathf.Max(1f, viewport.width - 18f), Mathf.Max(1f, viewport.height - 18f));
        if (_previewFit)
            _previewZoom = Mathf.Clamp(Mathf.Min(available.x / _worldTexture.width, available.y / _worldTexture.height), 0.01f, 16f);
        Vector2 size = new Vector2(_worldTexture.width, _worldTexture.height) * _previewZoom;
        Rect content = new Rect(0f, 0f, Mathf.Max(size.x, available.x), Mathf.Max(size.y, available.y));
        GUI.Box(viewport, GUIContent.none);
        _previewScroll = GUI.BeginScrollView(viewport, _previewScroll, content, true, true);
        try
        {
            _worldTexture.filterMode = _previewPointFilter ? FilterMode.Point : FilterMode.Bilinear;
            var image = new Rect(Mathf.Max(0f, (content.width - size.x) / 2f), Mathf.Max(0f, (content.height - size.y) / 2f), size.x, size.y);
            GUI.DrawTexture(image, _worldTexture, ScaleMode.StretchToFill);
        }
        finally { GUI.EndScrollView(); }
    }

    private void _SetPreviewZoom(float zoom)
    {
        _previewZoom = float.IsNaN(zoom) || float.IsInfinity(zoom) ? 1f : Mathf.Clamp(zoom, 0.01f, 16f);
        _previewFit = false;
    }
    #endregion

    #region CreateWorld
    private Vector2 _parameterScroll;
    private void _EditParameter()
    {
        _parameterScroll = EditorGUILayout.BeginScrollView(_parameterScroll);
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
            _DrawCoastParameters();
        }
        catch(System.Exception e)
        {
            Debug.LogError(e);
        }
        finally { EditorGUILayout.EndScrollView(); }
    }

    private bool _showCoastParameters = true;

    private void _DrawCoastParameters()
    {
        if (_paramTerrain == null) return;
        _showCoastParameters = EditorGUILayout.Foldout(_showCoastParameters, "地形分層與外海設定", true);
        if (!_showCoastParameters) return;
        EditorGUILayout.HelpBox((AssetDatabase.Contains(_paramTerrain) ? "以下欄位會編輯目前指定的地形參數資產，支援復原。" :
            "目前編輯的是載入設定的獨立副本，不會覆寫原參數資產。") + "分層波長與海岸寬度除以 WORLD SCALE 就是地圖比例；" +
            "只改地圖長寬可提高同一世界的解析度。" +
            "生態高度分段、預覽色階與河流終點仍需另行設定。將滑鼠停在欄位標籤上可查看中文說明。", MessageType.Info);
        using (var settings = new SerializedObject(_paramTerrain))
        {
            settings.Update();
            var field = settings.GetIterator();
            bool enterChildren = true;
            while (field.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (field.name == "m_Script") continue;
                EditorGUILayout.PropertyField(field, true);
            }
            settings.ApplyModifiedProperties();
        }
        try { GenerationValidation.Terrain(_paramTerrain); }
        catch (System.ArgumentException e) { EditorGUILayout.HelpBox(e.Message, MessageType.Error); }
        float interiorStart = _paramTerrain.OCEAN_BORDER_WIDTH +
            _paramTerrain.COAST_TRANSITION_WIDTH + _paramTerrain.COAST_NOISE_AMPLITUDE;
        if (_paramTerrain.WORLD_SCALE / 2f < interiorStart)
            EditorGUILayout.HelpBox("外海與過渡比例過大，可能無法保留不受海岸影響的內陸，甚至全部為海洋。" +
                "請減少寬度比例；提高輸出解析度不會改變這個比例。", MessageType.Warning);
    }

    private bool _CheckPreparedToCreate()
    {
        try
        {
            GenerationValidation.CellCount(_width, _height);
            if (_paramTerrain == null || _paramWeather == null || _paramTemperature == null ||
                _paramTemperature.WEATHER_GEN_PARA == null)
                throw new System.ArgumentException("Assign terrain, humidity and temperature parameters.");
            GenerationValidation.Terrain(_paramTerrain);
            GenerationValidation.Noise(_paramWeather.FREQ_COUNT_TIMES, _paramWeather.FREQ_GROW_FACTOR);
            GenerationValidation.Noise(_paramTemperature.WEATHER_GEN_PARA.FREQ_COUNT_TIMES,
                _paramTemperature.WEATHER_GEN_PARA.FREQ_GROW_FACTOR);
            GenerationValidation.Mana(_paramMana);
            GenerationValidation.Rain(_paramRain);
            GenerationValidation.Population(_paramPopulation);
            new BasicBiomeIdentifier(_biomeDistribution);
        }
        catch (System.ArgumentException e)
        {
            _noticeTxt = "Invalid world parameters: " + e.Message;
            return false;
        }
        _noticeTxt = "Start create world ...";
        return true;
    }

    private void _CreateNewWorld()
    {
        _StopGeneration();
        var monad = new BlockMonad<None>(r => _CreateNewWorldMonad());
        _generationCoroutine = new Utility.Coroutine(monad.Do());
        _executor.Add(_generationCoroutine);
    }

    private IEnumerator _CreateNewWorldMonad()
    {
        var captured = _CaptureSettings();
        using (var generationSettings = WorldSettingsSnapshot.Restore(captured))
        {
            int mapWidth = captured.Width, mapHeight = captured.Height, mapSeed = captured.Seed;
            Random.InitState(mapSeed);

            var genTerrainMonad = new BlockMonad<float[]>(r =>
                 _terrainGen.GenerateHeightMap(
                     mapWidth,
                     mapHeight,
                     Random.Range(0, 10000),
                     Random.Range(0, 10000),
                     (TerrainParameter)generationSettings.Roots[0],
                     r));
            var genHumidityMonad = new BlockMonad<float[]>(r =>
                _humidityGen.GenerateWeatherMap(
                    mapWidth,
                    mapHeight,
                    Random.Range(0, 10000),
                    Random.Range(0, 10000),
                    (WeatherParameter)generationSettings.Roots[1],
                    r));
            var genTemperatureMonad = new BlockMonad<float[]>(r =>
                _temperatureGen.GenerateWeatherMap(
                    mapWidth,
                    mapHeight,
                    Random.Range(0, 10000),
                    Random.Range(0, 10000),
                    (TemperatureParameter)generationSettings.Roots[2],
                    r));
            var genManaMonad = new BlockMonad<float[]>(r =>
                _manaGen.GenerateManaMap(
                    mapWidth,
                    mapHeight,
                    (ManaParameter)generationSettings.Roots[3],
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
                    mapWidth,
                    mapHeight,
                    terrainMap,
                    (RainParameter)generationSettings.Roots[4],
                    r));
            Debug.Log("start genRiverMonad");
            yield return genRiverMonad.Do();
            var riverMap = genRiverMonad.Result;

            var tileUnitMap = new TileUnit[mapWidth * mapHeight];

            IBiomeIdentifier identifier = new BasicBiomeIdentifier((BiomeDistribution)generationSettings.Roots[6]);
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    var idx = MathUtility.MapIndex(x, y, mapHeight);

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
                    mapWidth,
                    mapHeight,
                    tileUnitMap,
                    identifier,
                    (PopulationParameter)generationSettings.Roots[5],
                    r));
            Debug.Log("start genPopulationMonad");
            yield return genPopulationMonad.Do();
            var populationMap = genPopulationMonad.Result;

            _tileData = new TileDataUnit
            {
                Map = tileUnitMap,
                Populations = populationMap,
                Width = mapWidth,
                Height = mapHeight,
            };
            _noticeTxt = "New world created.";
            _generatedSettings = captured;
            if (string.IsNullOrEmpty(_currentSaveId)) _saveName = "地圖 " + mapSeed;
            _ReleasePreviewTexture();
        }
    }
    #endregion

    #region ViewMapTexture
    private void _ReleasePreviewTexture()
    {
        if (_worldTexture != null) DestroyImmediate(_worldTexture);
        _worldTexture = null;
    }

    private void _CreatePreviewTexture(int width, int height)
    {
        _ReleasePreviewTexture();
        _worldTexture = new Texture2D(width, height) { wrapMode = TextureWrapMode.Clamp };
    }

    private void _ShowWorldMapTexture(TileDataUnit mapData)
    {
        _noticeTxt = string.Format("Load Map  - {0}*{1}", mapData.Width, mapData.Height);
        _CreatePreviewTexture(mapData.Width, mapData.Height);
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
        _CreatePreviewTexture(mapData.Width, mapData.Height);
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
        _CreatePreviewTexture(mapData.Width, mapData.Height);
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

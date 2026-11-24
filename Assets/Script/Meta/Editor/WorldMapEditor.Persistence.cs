using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public partial class WorldMapEditor
{
    private WorldGenerationSnapshot _generatedSettings;
    private RestoredWorldSettings _loadedSettings;
    private WorldMapLibrary _mapLibrary;
    private string _currentSaveId;
    private string _saveName = "我的地圖";
    private List<WorldMapEntry> _saveEntries = new List<WorldMapEntry>();
    private Vector2 _saveScroll;
    private Utility.Coroutine _generationCoroutine;

    private WorldMapLibrary MapLibrary
    {
        get { return _mapLibrary ?? (_mapLibrary = new WorldMapLibrary(Path.Combine(Application.persistentDataPath, "Maps"))); }
    }

    private void _StopGeneration()
    {
        if (_generationCoroutine != null) { _generationCoroutine.Dispose(); _generationCoroutine = null; }
        _executor.Clear();
    }

    private WorldGenerationSnapshot _CaptureSettings()
    {
        return WorldSettingsSnapshot.Capture(_seed, _width, _height, new ScriptableObject[] {
            _paramTerrain, _paramWeather, _paramTemperature, _paramMana, _paramRain,
            _paramPopulation, _biomeDistribution, _terrainColor, _weatherColor, _manaColor, _rainColor
        });
    }

    private void _RefreshSaveList()
    {
        string error;
        _saveEntries = MapLibrary.List(out error);
        if (error != null) _noticeTxt = "存檔列表讀取失敗：" + error;
    }

    private void _DrawSaveMap()
    {
        EditorGUILayout.HelpBox("每份存檔包含地圖與生成當時的設定。修改參數後，請重新生成地圖再儲存，才會使用新設定。", MessageType.Info);
        _saveName = EditorGUILayout.TextField(new GUIContent("存檔名稱", "可使用中文；另存新檔會建立獨立地圖，同名也不互相覆寫。"), _saveName);
        if (_tileData != null)
            EditorGUILayout.LabelField("目前地圖", _tileData.Width + " × " + _tileData.Height +
                (_generatedSettings == null ? "（尚無設定快照，請重新生成）" : "，seed " + _generatedSettings.Seed));
        using (new EditorGUI.DisabledScope(_tileData == null || _generatedSettings == null ||
            (_generationCoroutine != null && !_generationCoroutine.Finished)))
        {
            if (GUILayout.Button("另存新地圖")) _SaveCurrentMap(false);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_currentSaveId)))
                if (GUILayout.Button("覆寫目前存檔")) _SaveCurrentMap(true);
        }
        if (_tileData == null) EditorGUILayout.HelpBox("請先生成或載入地圖。", MessageType.Info);
        if (GUILayout.Button("開啟存檔資料夾")) _OpenSaveFolder();
    }

    private void _SaveCurrentMap(bool overwrite)
    {
        string error, id = _currentSaveId;
        bool saved = overwrite
            ? MapLibrary.Overwrite(id, _saveName, _tileData, _generatedSettings, WorldSettingsSnapshot.Validate, out error)
            : MapLibrary.SaveNew(_saveName, _tileData, _generatedSettings, WorldSettingsSnapshot.Validate, out id, out error);
        if (saved) { _currentSaveId = id; _noticeTxt = "已儲存地圖與生成設定：" + _saveName; }
        else _noticeTxt = "存檔失敗：" + error;
    }

    private void _OpenSaveFolder()
    {
        try { Directory.CreateDirectory(MapLibrary.DirectoryPath); EditorUtility.RevealInFinder(MapLibrary.DirectoryPath); }
        catch (System.Exception e) { _noticeTxt = "無法開啟存檔資料夾：" + e.Message; }
    }

    private void _DrawLoadMaps()
    {
        if (GUILayout.Button("重新整理存檔列表")) _RefreshSaveList();
        if (GUILayout.Button("開啟存檔資料夾")) _OpenSaveFolder();
        _saveScroll = EditorGUILayout.BeginScrollView(_saveScroll);
        try
        {
            if (_saveEntries.Count == 0) EditorGUILayout.LabelField("尚無多地圖存檔。");
            foreach (var entry in _saveEntries)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var info = entry.Metadata;
                EditorGUILayout.LabelField(info == null ? entry.Id : info.Name, EditorStyles.boldLabel);
                if (info != null)
                {
                    System.DateTime savedAt;
                    string time = System.DateTime.TryParse(info.SavedAtUtc, out savedAt) ? savedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : info.SavedAtUtc;
                    EditorGUILayout.LabelField(info.Width + " × " + info.Height + "，seed " + info.Seed);
                    EditorGUILayout.LabelField("儲存時間：" + time);
                }
                if (entry.Warning != null) EditorGUILayout.HelpBox(entry.Warning, MessageType.Warning);
                if (GUILayout.Button("載入地圖與設定")) _LoadSavedMap(entry.Id);
                EditorGUILayout.EndVertical();
            }
        }
        finally { EditorGUILayout.EndScrollView(); }
    }

    private bool _LoadSavedMap(string id)
    {
        string message;
        var document = MapLibrary.Load(id, WorldSettingsSnapshot.Validate, out message);
        if (document == null) { _noticeTxt = "載入失敗，保留目前地圖與參數。 " + message; return false; }
        RestoredWorldSettings restored;
        try { restored = WorldSettingsSnapshot.Restore(document.Settings); }
        catch (System.Exception e) { _noticeTxt = "設定還原失敗，保留目前內容：" + e.Message; return false; }
        _StopGeneration();
        if (_loadedSettings != null) _loadedSettings.Dispose();
        _loadedSettings = restored;
        var roots = restored.Roots;
        _paramTerrain = (TerrainParameter)roots[0]; _paramWeather = (WeatherParameter)roots[1];
        _paramTemperature = (TemperatureParameter)roots[2]; _paramMana = (ManaParameter)roots[3];
        _paramRain = (RainParameter)roots[4]; _paramPopulation = (PopulationParameter)roots[5];
        _biomeDistribution = (BiomeDistribution)roots[6]; _terrainColor = (ColorRangeDistribution)roots[7];
        _weatherColor = (ColorRangeDistribution)roots[8]; _manaColor = (ColorRangeDistribution)roots[9];
        _rainColor = (ColorRangeDistribution)roots[10]; _seed = document.Settings.Seed;
        _tileData = document.Data; _generatedSettings = document.Settings;
        _width = document.Data.Width; _height = document.Data.Height;
        _currentSaveId = document.Metadata.Id; _saveName = document.Metadata.Name;
        _ReleasePreviewTexture(); _previewScroll = Vector2.zero; _descriptionTxt = string.Empty;
        _serializedObj.Update();
        _noticeTxt = "已載入：" + _saveName + "（已還原獨立設定副本） " + message;
        return true;
    }
}

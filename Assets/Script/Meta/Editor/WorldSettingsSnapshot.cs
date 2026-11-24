using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Snapshot values and their object graph instead of session-dependent instance IDs.
public static class WorldSettingsSnapshot
{
    private static readonly Type[] RootTypes = {
        typeof(TerrainParameter), typeof(WeatherParameter), typeof(TemperatureParameter), typeof(ManaParameter),
        typeof(RainParameter), typeof(PopulationParameter), typeof(BiomeDistribution),
        typeof(ColorRangeDistribution), typeof(ColorRangeDistribution), typeof(ColorRangeDistribution), typeof(ColorRangeDistribution)
    };
    private static readonly Type[] AllowedTypes = {
        typeof(TerrainParameter), typeof(WeatherParameter), typeof(TemperatureParameter), typeof(ManaParameter),
        typeof(RainParameter), typeof(PopulationParameter), typeof(BiomeDistribution), typeof(ColorRangeDistribution),
        typeof(RandomPointParameter), typeof(BiomeData)
    };

    public static WorldGenerationSnapshot Capture(int seed, int width, int height, ScriptableObject[] roots)
    {
        if (roots == null || roots.Length != RootTypes.Length) throw new ArgumentException("生成設定根節點不完整。");
        var nodes = new List<WorldSettingsNode>();
        var ids = new Dictionary<ScriptableObject, int>();
        var snapshot = new WorldGenerationSnapshot { Seed = seed, Width = width, Height = height, Roots = new int[roots.Length] };
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null || roots[i].GetType() != RootTypes[i]) throw new ArgumentException("請指定完整的生成與色階參數。");
            snapshot.Roots[i] = CaptureNode(roots[i], nodes, ids);
        }
        snapshot.Nodes = nodes.ToArray();
        return snapshot;
    }

    private static int CaptureNode(ScriptableObject source, List<WorldSettingsNode> nodes, Dictionary<ScriptableObject, int> ids)
    {
        int id;
        if (ids.TryGetValue(source, out id)) return id;
        if (Array.IndexOf(AllowedTypes, source.GetType()) < 0) throw new ArgumentException("不支援的參數類型：" + source.GetType().Name);
        id = nodes.Count;
        ids.Add(source, id);
        var node = new WorldSettingsNode { Type = source.GetType().Name };
        nodes.Add(node);
        var copy = Object.Instantiate(source);
        try
        {
            var references = new List<WorldSettingsReference>();
            using (var serialized = new SerializedObject(copy))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.propertyPath == "m_Script") continue;
                    var target = property.objectReferenceValue;
                    if (target != null && !(target is ScriptableObject)) throw new ArgumentException("設定包含無法內嵌的引用：" + property.propertyPath);
                    references.Add(new WorldSettingsReference {
                        Property = property.propertyPath, Node = target == null ? -1 : CaptureNode((ScriptableObject)target, nodes, ids)
                    });
                    property.objectReferenceValue = null;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            node.Json = JsonUtility.ToJson(copy);
            node.References = references.ToArray();
        }
        finally { Object.DestroyImmediate(copy); }
        return id;
    }

    public static RestoredWorldSettings Restore(WorldGenerationSnapshot snapshot)
    {
        WorldMapLibrary.ValidateSnapshot(snapshot);
        var restored = new RestoredWorldSettings();
        try
        {
            foreach (var node in snapshot.Nodes)
            {
                Type type = Array.Find(AllowedTypes, t => t.Name == node.Type);
                if (type == null) throw new InvalidDataException("不支援的生成設定類型：" + node.Type);
                if (!node.Json.TrimStart().StartsWith("{")) throw new InvalidDataException("生成設定內容不是 JSON 物件。");
                var item = ScriptableObject.CreateInstance(type);
                restored.Objects.Add(item);
                JsonUtility.FromJsonOverwrite(node.Json, item);
                item.hideFlags = HideFlags.DontSave; // Transient but still editable in the Inspector.
                item.name = "存檔設定：" + type.Name;
                using (var serialized = new SerializedObject(item))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.Float)
                            GenerationValidation.Finite(property.floatValue, "存檔設定：" + property.propertyPath);
                }
                if (item is BiomeData && !Enum.IsDefined(typeof(Biome), ((BiomeData)item).Biome))
                    throw new InvalidDataException("存檔生態類型無效。");
            }
            for (int i = 0; i < snapshot.Nodes.Length; i++)
            {
                using (var serialized = new SerializedObject(restored.Objects[i]))
                {
                    // Never allow an embedded instance ID to link to a live project asset.
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.propertyPath != "m_Script")
                            property.objectReferenceValue = null;
                    var seen = new HashSet<string>();
                    foreach (var reference in snapshot.Nodes[i].References)
                    {
                        property = serialized.FindProperty(reference.Property);
                        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference ||
                            reference.Property == "m_Script" || !seen.Add(reference.Property))
                            throw new InvalidDataException("設定引用欄位無效：" + reference.Property);
                        property.objectReferenceValue = reference.Node < 0 ? null : restored.Objects[reference.Node];
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            restored.Roots = new ScriptableObject[snapshot.Roots.Length];
            for (int i = 0; i < restored.Roots.Length; i++)
            {
                restored.Roots[i] = restored.Objects[snapshot.Roots[i]];
                if (restored.Roots[i].GetType() != RootTypes[i]) throw new InvalidDataException("生成設定根節點類型不符。");
            }
            ValidateRoots(restored.Roots);
            return restored;
        }
        catch { restored.Dispose(); throw; }
    }

    public static void Validate(WorldGenerationSnapshot snapshot) { using (Restore(snapshot)) { } }

    private static void ValidateRoots(ScriptableObject[] roots)
    {
        GenerationValidation.Terrain((TerrainParameter)roots[0]);
        var weather = (WeatherParameter)roots[1];
        GenerationValidation.Noise(weather.FREQ_COUNT_TIMES, weather.FREQ_GROW_FACTOR);
        var temperature = (TemperatureParameter)roots[2];
        if (temperature.WEATHER_GEN_PARA == null) throw new InvalidDataException("溫度的天氣參數遺失。");
        GenerationValidation.Noise(temperature.WEATHER_GEN_PARA.FREQ_COUNT_TIMES, temperature.WEATHER_GEN_PARA.FREQ_GROW_FACTOR);
        GenerationValidation.Mana((ManaParameter)roots[3]);
        GenerationValidation.Rain((RainParameter)roots[4]);
        GenerationValidation.Population((PopulationParameter)roots[5]);
        new BasicBiomeIdentifier((BiomeDistribution)roots[6]);
        for (int i = 7; i < roots.Length; i++)
        {
            var colors = (ColorRangeDistribution)roots[i];
            if (colors.TotalGrid <= 0 || colors.Colors == null || colors.Colors.Length == 0)
                throw new InvalidDataException("預覽色階不完整。");
            int previous = -1;
            foreach (var color in colors.Colors)
            {
                // Equal grid positions are intentional hard color boundaries in existing assets.
                if (color == null || color.Grid < 0 || color.Grid < previous || color.Grid > colors.TotalGrid)
                    throw new InvalidDataException("預覽色階範圍無效。");
                previous = color.Grid;
            }
        }
    }
}

public sealed class RestoredWorldSettings : IDisposable
{
    public ScriptableObject[] Roots { get; internal set; }
    internal readonly List<ScriptableObject> Objects = new List<ScriptableObject>();
    public void Dispose()
    {
        foreach (var item in Objects) if (item != null) Object.DestroyImmediate(item);
        Objects.Clear();
    }
}

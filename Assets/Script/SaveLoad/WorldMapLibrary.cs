using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class WorldGenerationSnapshot
{
    public int Version = 1;
    public int Seed;
    public int Width;
    public int Height;
    public string GeneratorVersion = "layered-world-v1";
    public int[] Roots;
    public WorldSettingsNode[] Nodes;
}

[Serializable]
public class WorldSettingsNode
{
    public string Type;
    public string Json;
    public WorldSettingsReference[] References;
}

[Serializable]
public class WorldSettingsReference
{
    public string Property;
    public int Node;
}

[Serializable]
public class WorldMapMetadata
{
    public string Id;
    public string Name;
    public string SavedAtUtc;
    public int Width;
    public int Height;
    public int Seed;
}

[Serializable]
public class WorldMapDocument
{
    public int Version;
    public WorldMapMetadata Metadata;
    public WorldGenerationSnapshot Settings;
    public TileDataUnit Data;
}

public class WorldMapEntry
{
    public string Id;
    public WorldMapMetadata Metadata;
    public string Warning;
}

// Each map is one JSON file. Its first line is a small metadata header so the
// library can list large maps without parsing or allocating their tile arrays.
public sealed class WorldMapLibrary
{
    public string DirectoryPath { get; private set; }
    public WorldMapLibrary(string directory) { DirectoryPath = Path.GetFullPath(directory); }

    private string MapPath(string id)
    {
        Guid parsed;
        if (id == null || id.Length != 32 || !Guid.TryParseExact(id, "N", out parsed))
            throw new ArgumentException("存檔 ID 無效。");
        return Path.Combine(DirectoryPath, id + ".json");
    }

    public bool SaveNew(string name, TileDataUnit data, WorldGenerationSnapshot settings,
        Action<WorldGenerationSnapshot> validateSettings, out string id, out string error)
    {
        id = Guid.NewGuid().ToString("N");
        if (Write(id, name, data, settings, false, validateSettings, out error)) return true;
        id = null;
        return false;
    }

    public bool Overwrite(string id, string name, TileDataUnit data, WorldGenerationSnapshot settings,
        Action<WorldGenerationSnapshot> validateSettings, out string error)
    {
        return Write(id, name, data, settings, true, validateSettings, out error);
    }

    private bool Write(string id, string name, TileDataUnit data, WorldGenerationSnapshot settings,
        bool overwrite, Action<WorldGenerationSnapshot> validateSettings, out string error)
    {
        try
        {
            string path = MapPath(id);
            bool exists = File.Exists(path) || File.Exists(path + ".bak");
            if (overwrite != exists) throw new IOException(overwrite ? "找不到要覆寫的存檔。" : "存檔 ID 已存在。");
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
                throw new ArgumentException("請輸入 1～80 個字的存檔名稱。");
            var metadata = new WorldMapMetadata {
                Id = id, Name = name.Trim(), SavedAtUtc = DateTime.UtcNow.ToString("O"),
                Width = data == null ? 0 : data.Width, Height = data == null ? 0 : data.Height,
                Seed = settings == null ? 0 : settings.Seed
            };
            Validate(new WorldMapDocument { Version = 2, Metadata = metadata, Settings = settings, Data = data }, id, validateSettings);
            string json = "{\"Version\":2,\"Metadata\":" + JsonUtility.ToJson(metadata) + ",\n\"Settings\":" +
                JsonUtility.ToJson(settings) + ",\n\"Data\":" + JsonUtility.ToJson(data) + "}";
            return WriteAtomic(path, json, out error);
        }
        catch (Exception e) { error = e.Message; return false; }
    }

    public WorldMapDocument Load(string id, Action<WorldGenerationSnapshot> validateSettings, out string message)
    {
        try
        {
            string path = MapPath(id);
            try { var document = Read(path, id, validateSettings); message = null; return document; }
            catch (Exception mainError)
            {
                try
                {
                    var document = Read(path + ".bak", id, validateSettings);
                    message = "主檔無法讀取，已載入備份：" + mainError.Message;
                    return document;
                }
                catch (Exception backupError)
                {
                    message = "載入失敗：" + mainError.Message + " 備份：" + backupError.Message;
                    return null;
                }
            }
        }
        catch (Exception e) { message = e.Message; return null; }
    }

    private static WorldMapDocument Read(string path, string id, Action<WorldGenerationSnapshot> validateSettings)
    {
        var document = JsonUtility.FromJson<WorldMapDocument>(File.ReadAllText(path));
        Validate(document, id, validateSettings);
        return document;
    }

    private static void Validate(WorldMapDocument document, string id, Action<WorldGenerationSnapshot> validateSettings)
    {
        if (document == null || document.Version != 2 || document.Metadata == null || document.Metadata.Id != id)
            throw new InvalidDataException("不支援的存檔版本或無效的存檔資料。");
        var metadata = document.Metadata;
        DateTime time;
        if (string.IsNullOrWhiteSpace(metadata.Name) || metadata.Name.Length > 80 || !DateTime.TryParse(metadata.SavedAtUtc, out time))
            throw new InvalidDataException("存檔資訊無效。");
        MapSaveValidation.Validate(document.Data);
        if (metadata.Width != document.Data.Width || metadata.Height != document.Data.Height)
            throw new InvalidDataException("存檔資訊與內容不一致。");
        ValidateSnapshot(document.Settings);
        if (document.Settings.Width != metadata.Width || document.Settings.Height != metadata.Height || document.Settings.Seed != metadata.Seed)
            throw new InvalidDataException("生成設定與地圖尺寸或種子不一致。");
        if (validateSettings != null) validateSettings(document.Settings);
    }

    public static void ValidateSnapshot(WorldGenerationSnapshot settings)
    {
        if (settings == null || settings.Version != 1 || string.IsNullOrEmpty(settings.GeneratorVersion) ||
            settings.Roots == null || settings.Roots.Length != 11 || settings.Nodes == null || settings.Nodes.Length == 0)
            throw new InvalidDataException("生成設定快照不完整或版本不支援。");
        GenerationValidation.CellCount(settings.Width, settings.Height);
        foreach (int root in settings.Roots)
            if (root < 0 || root >= settings.Nodes.Length) throw new InvalidDataException("生成設定根節點無效。");
        foreach (var node in settings.Nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.Type) || string.IsNullOrWhiteSpace(node.Json) || node.References == null)
                throw new InvalidDataException("生成設定節點不完整。");
            foreach (var reference in node.References)
                if (reference == null || string.IsNullOrEmpty(reference.Property) || reference.Node < -1 || reference.Node >= settings.Nodes.Length)
                    throw new InvalidDataException("生成設定引用無效。");
        }
    }

    public List<WorldMapEntry> List(out string error)
    {
        var result = new List<WorldMapEntry>();
        error = null;
        try
        {
            if (!Directory.Exists(DirectoryPath)) return result;
            var ids = Directory.GetFiles(DirectoryPath, "*.json").Select(Path.GetFileNameWithoutExtension)
                .Concat(Directory.GetFiles(DirectoryPath, "*.json.bak").Select(p => Path.GetFileName(p).Replace(".json.bak", "")))
                .Distinct().OrderBy(id => id);
            foreach (string id in ids)
            {
                string path;
                try { path = MapPath(id); } catch (ArgumentException) { continue; }
                var entry = new WorldMapEntry { Id = id };
                try { entry.Metadata = ReadHeader(path, id); }
                catch (Exception mainError)
                {
                    try { entry.Metadata = ReadHeader(path + ".bak", id); entry.Warning = "主檔資訊無效，顯示備份資訊。"; }
                    catch { entry.Warning = "存檔資訊無法讀取：" + mainError.Message; }
                }
                result.Add(entry);
            }
            return result.OrderByDescending(e => e.Metadata == null ? "" : e.Metadata.SavedAtUtc).ToList();
        }
        catch (Exception e) { error = e.Message; return result; }
    }

    private static WorldMapMetadata ReadHeader(string path, string id)
    {
        using (var reader = new StreamReader(path))
        {
            var line = new System.Text.StringBuilder();
            for (int i = 0; i < 4096; i++)
            {
                int c = reader.Read();
                if (c < 0 || c == '\n') break;
                line.Append((char)c);
            }
            var header = JsonUtility.FromJson<WorldMapDocument>(line.ToString() + "\"Settings\":null}");
            if (header == null || header.Version != 2 || header.Metadata == null || header.Metadata.Id != id || string.IsNullOrWhiteSpace(header.Metadata.Name))
                throw new InvalidDataException("存檔標頭無效。");
            return header.Metadata;
        }
    }

    private static bool WriteAtomic(string path, string json, out string error)
    {
        string temporaryPath = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 4096, true))
                { writer.Write(json); writer.Flush(); }
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporaryPath, path, path + ".bak");
            else File.Move(temporaryPath, path);
            error = null; return true;
        }
        catch (Exception e) { error = e.Message; return false; }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

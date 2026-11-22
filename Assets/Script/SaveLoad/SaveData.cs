using System;
using System.IO;
using UnityEngine;

public static class SaveData
{
    private const string LegacySaveKey = "SAVE_KEY";
    private static MapSaveStorage Storage
    {
        get { return new MapSaveStorage(Path.Combine(Application.persistentDataPath, "world-map.json")); }
    }

    public static bool SaveMap(TileDataUnit data)
    {
        string error;
        return SaveMap(data, out error);
    }

    public static bool SaveMap(TileDataUnit data, out string error)
    {
        return Storage.Save(data, out error);
    }

    public static TileDataUnit LoadMap()
    {
        string message;
        return LoadMap(out message);
    }

    public static TileDataUnit LoadMap(out string message)
    {
        var storage = Storage;
        if (storage.HasSave) return storage.Load(out message);
        // Keep the old preference intact, even if migration fails.
        try
        {
            if (!PlayerPrefs.HasKey(LegacySaveKey))
            {
                message = "No saved map exists.";
                return null;
            }
            return storage.MigrateLegacy(PlayerPrefs.GetString(LegacySaveKey), out message);
        }
        catch (Exception e)
        {
            message = "Invalid legacy save: " + e.Message;
            return null;
        }
    }
}

public sealed class MapSaveStorage
{
    public const int CurrentVersion = 1;
    private readonly string _path;
    public bool HasSave { get { return File.Exists(_path) || File.Exists(_path + ".bak"); } }

    [Serializable]
    private class SaveFile
    {
        public int Version;
        public TileDataUnit Data;
    }

    public MapSaveStorage(string path)
    {
        _path = Path.GetFullPath(path);
    }

    public bool Save(TileDataUnit data, out string error)
    {
        var temporaryPath = _path + ".tmp";
        try
        {
            Validate(data);
            var json = JsonUtility.ToJson(new SaveFile { Version = CurrentVersion, Data = data });
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            // Flush the complete new file before replacing the previous save.
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 4096, true))
                {
                    writer.Write(json);
                    writer.Flush();
                }
                stream.Flush(true);
            }
            if (File.Exists(_path)) File.Replace(temporaryPath, _path, _path + ".bak");
            else File.Move(temporaryPath, _path);
            error = null;
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public TileDataUnit Load(out string message)
    {
        try
        {
            var data = Read(_path);
            message = null;
            return data;
        }
        catch (Exception mainError)
        {
            try
            {
                var data = Read(_path + ".bak");
                message = "Loaded the backup because the main save was unavailable: " + mainError.Message;
                return data;
            }
            catch (Exception backupError)
            {
                message = "Could not load the map: " + mainError.Message + " Backup: " + backupError.Message;
                return null;
            }
        }
    }

    public TileDataUnit MigrateLegacy(string json, out string message)
    {
        try
        {
            var data = JsonUtility.FromJson<TileDataUnit>(json);
            Validate(data);
            string error;
            message = Save(data, out error)
                ? "Migrated the legacy save."
                : "Loaded the legacy save; migration failed: " + error;
            return data;
        }
        catch (Exception e)
        {
            message = "Invalid legacy save: " + e.Message;
            return null;
        }
    }

    private static TileDataUnit Read(string path)
    {
        var file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
        if (file == null || file.Version != CurrentVersion)
            throw new InvalidDataException("Unsupported or missing save format version.");
        Validate(file.Data);
        return file.Data;
    }

    public static void Validate(TileDataUnit data)
    {
        if (data == null) throw new ArgumentNullException("data");
        var count = GenerationValidation.CellCount(data.Width, data.Height);
        GenerationValidation.Map(data.Map, count, "saved tiles");
        GenerationValidation.Map(data.Populations, count, "saved populations");
        for (var i = 0; i < count; i++)
        {
            var tile = data.Map[i];
            if (tile == null) throw new ArgumentException("Saved tiles must not be null.");
            GenerationValidation.UnitValue(tile.Height, "saved height");
            GenerationValidation.UnitValue(tile.Humidity, "saved humidity");
            GenerationValidation.UnitValue(tile.Temperature, "saved temperature");
            GenerationValidation.UnitValue(tile.River, "saved river");
            GenerationValidation.Finite(tile.Mana, "saved mana");
            if (!Enum.IsDefined(typeof(Biome), tile.Biome) || data.Populations[i] < 0)
                throw new ArgumentException("Saved biome or population is invalid.");
        }
    }
}

[Serializable]
public class TileDataUnit
{
    public TileUnit[] Map;
    public int[] Populations;
    public int Width;
    public int Height;
}

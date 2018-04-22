using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public static class SaveData  {
    private const string SAVE_KEY = "SAVE_KEY";

    public static void SaveMap(SaveDataUnit saveDataUnit)
    {
        try
        {
            _SaveMap(saveDataUnit);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    private static void _SaveMap(SaveDataUnit saveDataUnit)
    {
        var saveJson = JsonUtility.ToJson(saveDataUnit);
        PlayerPrefs.SetString(SAVE_KEY, saveJson);
    }

    public static SaveDataUnit LoadMap()
    {
        try
        {
            return _LoadMap();
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return null;
        }
    }

    private static SaveDataUnit _LoadMap()
    {
        var dataString = PlayerPrefs.GetString(SAVE_KEY);
        var dataUnit = JsonUtility.FromJson<SaveDataUnit>(dataString);
        return dataUnit;
    }
}

[Serializable]
public class SaveDataUnit
{
    public TileUnit[] Map;
    public int Width;
    public int Height;
}
 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public static class SaveData  {
    private const string MAP_KEY = "MAP_KEY";

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
        var mapJson = JsonUtility.ToJson(saveDataUnit);
        PlayerPrefs.SetString(MAP_KEY, mapJson);
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
        var dataString = PlayerPrefs.GetString(MAP_KEY);
        var dataUnit = JsonUtility.FromJson<SaveDataUnit>(dataString);
        return dataUnit;
    }
}

[DataContract]
public class SaveDataUnit
{
    [DataMember]
    public float[] Map;
    [DataMember]
    public int Width;
    [DataMember]
    public int Height;
}